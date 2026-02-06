using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the 3D model preview stage for the character select screen.
    /// Creates a Camera + RenderTexture pipeline rendered into a UI Toolkit VisualElement.
    /// Currently spawns placeholder capsule (hero) + sphere (monster) with hero-colored materials.
    /// Supports click-drag rotation, scroll zoom, and auto-orbit after idle.
    /// </summary>
    public class HeroStageController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Stage Setup")]
        [SerializeField] private int _renderTextureWidth = 1024;
        [SerializeField] private int _renderTextureHeight = 1024;
        [SerializeField] private int _renderLayer = 31; // Use a high layer to avoid conflicts

        [Header("Camera")]
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0f, 0.8f, -3f);
        [SerializeField] private Vector3 _cameraLookAt = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float _cameraFOV = 30f;
        [SerializeField] private float _minZoom = -4.5f;
        [SerializeField] private float _maxZoom = -1.5f;
        [SerializeField] private float _zoomSpeed = 0.5f;

        [Header("Model Positions")]
        [SerializeField] private Vector3 _heroPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 _monsterPosition = new Vector3(0.7f, 0f, 0.3f);
        [SerializeField] private float _heroScale = 1f;
        [SerializeField] private float _monsterScale = 0.5f;

        [Header("Interaction")]
        [SerializeField] private float _rotationSpeed = 0.3f;
        [SerializeField] private float _autoOrbitSpeed = 5f;
        [SerializeField] private float _idleBeforeOrbit = 3f;

        [Header("Animation")]
        [SerializeField] private float _spawnDuration = 0.5f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _monsterDelay = 0.15f;

        // =============================================================================
        // STATE
        // =============================================================================

        private Camera _stageCamera;
        private RenderTexture _renderTexture;
        private GameObject _stageRoot;
        private GameObject _currentHeroModel;
        private GameObject _currentMonsterModel;
        private Light _stageLight;
        private VisualElement _renderTarget;

        private bool _isDragging;
        private float _currentYRotation;
        private float _idleTimer;
        private float _currentZoom;
        private bool _isDestroyed;
        private Coroutine _transitionCoroutine;

        // Cached materials for cleanup
        private Material _heroMaterial;
        private Material _monsterMaterial;

        // Cached renderers to avoid GetComponentsInChildren allocations
        private Renderer _heroRenderer;
        private Renderer _monsterRenderer;

        // Cached WaitForSeconds
        private WaitForSeconds _monsterDelayWait;

        // Track models/materials pending destruction (to handle interrupted coroutines)
        private readonly List<GameObject> _pendingDestroyModels = new List<GameObject>(4);
        private readonly List<Material> _pendingDestroyMaterials = new List<Material>(4);

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        public void Initialize(VisualElement renderTarget)
        {
            _renderTarget = renderTarget;
            _monsterDelayWait = new WaitForSeconds(_monsterDelay);
            CreateStage();
            SetupInteraction();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            // Unregister UI callbacks
            if (_renderTarget != null)
            {
                _renderTarget.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _renderTarget.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _renderTarget.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                _renderTarget.UnregisterCallback<WheelEvent>(OnWheel);
            }

            CleanupStage();
        }

        private void Update()
        {
            if (_isDestroyed || _stageRoot == null) return;

            // Auto-orbit when idle
            if (!_isDragging)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= _idleBeforeOrbit && _currentHeroModel != null)
                {
                    _currentYRotation += _autoOrbitSpeed * Time.deltaTime;
                    _stageRoot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
                }
            }
        }

        // =============================================================================
        // STAGE SETUP
        // =============================================================================

        private void CreateStage()
        {
            // Create render texture
            _renderTexture = new RenderTexture(_renderTextureWidth, _renderTextureHeight, 24);
            _renderTexture.antiAliasing = 4;
            _renderTexture.Create();

            // Create stage root (all models go under this for rotation)
            _stageRoot = new GameObject("CharacterStage");
            _stageRoot.transform.SetParent(transform);
            _stageRoot.transform.localPosition = Vector3.zero;

            // Create camera
            var camObj = new GameObject("StageCamera");
            camObj.transform.SetParent(transform); // Not under stageRoot so rotation doesn't affect it
            camObj.transform.localPosition = _cameraPosition;
            _currentZoom = _cameraPosition.z;

            _stageCamera = camObj.AddComponent<Camera>();
            _stageCamera.targetTexture = _renderTexture;
            _stageCamera.clearFlags = CameraClearFlags.SolidColor;
            _stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // Transparent
            _stageCamera.fieldOfView = _cameraFOV;
            _stageCamera.cullingMask = 1 << _renderLayer;
            _stageCamera.nearClipPlane = 0.1f;
            _stageCamera.farClipPlane = 50f;
            _stageCamera.transform.LookAt(_cameraLookAt);

            // Create lighting
            var lightObj = new GameObject("StageLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(1f, 2f, -1f);
            lightObj.transform.LookAt(Vector3.zero);
            _stageLight = lightObj.AddComponent<Light>();
            _stageLight.type = LightType.Directional;
            _stageLight.intensity = 1.2f;
            _stageLight.color = new Color(0.95f, 0.9f, 1f);
            _stageLight.cullingMask = 1 << _renderLayer;

            // Apply render texture to UI element
            ApplyRenderTexture();
        }

        private void ApplyRenderTexture()
        {
            if (_renderTarget == null || _renderTexture == null) return;

            // UI Toolkit: set background from RenderTexture
            _renderTarget.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
        }

        private void CleanupStage()
        {
            // Flush any models/materials from interrupted FadeOutModels
            FlushPendingDestroys();

            if (_stageCamera != null) Destroy(_stageCamera.gameObject);
            if (_stageRoot != null) Destroy(_stageRoot);
            if (_stageLight != null) Destroy(_stageLight.gameObject);

            if (_heroMaterial != null) Destroy(_heroMaterial);
            if (_monsterMaterial != null) Destroy(_monsterMaterial);

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        // =============================================================================
        // INTERACTION
        // =============================================================================

        private void SetupInteraction()
        {
            if (_renderTarget == null) return;

            _renderTarget.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _renderTarget.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _renderTarget.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _renderTarget.RegisterCallback<WheelEvent>(OnWheel);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _idleTimer = 0;
            _renderTarget?.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _stageRoot == null) return;

            _currentYRotation += evt.deltaPosition.x * _rotationSpeed;
            _stageRoot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
            _idleTimer = 0;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _isDragging = false;
            _idleTimer = 0;
            _renderTarget?.ReleasePointer(evt.pointerId);
        }

        private void OnWheel(WheelEvent evt)
        {
            if (_stageCamera == null) return;

            _currentZoom += evt.delta.y * _zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            _stageCamera.transform.localPosition = new Vector3(
                _cameraPosition.x, _cameraPosition.y, _currentZoom);
            _idleTimer = 0;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Show placeholder models for the given hero with animated transition.
        /// </summary>
        public void ShowHero(HeroData hero)
        {
            if (_isDestroyed || hero == null) return;

            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

            // Flush any models/materials left over from interrupted FadeOutModels coroutines
            FlushPendingDestroys();

            _transitionCoroutine = StartCoroutine(TransitionToHero(hero));
        }

        /// <summary>
        /// Immediately destroy any models and materials that were handed off to
        /// FadeOutModels but whose coroutine was interrupted before cleanup finished.
        /// </summary>
        private void FlushPendingDestroys()
        {
            for (int i = 0; i < _pendingDestroyModels.Count; i++)
            {
                if (_pendingDestroyModels[i] != null) Destroy(_pendingDestroyModels[i]);
            }
            _pendingDestroyModels.Clear();

            for (int i = 0; i < _pendingDestroyMaterials.Count; i++)
            {
                if (_pendingDestroyMaterials[i] != null) Destroy(_pendingDestroyMaterials[i]);
            }
            _pendingDestroyMaterials.Clear();
        }

        // =============================================================================
        // MODEL MANAGEMENT
        // =============================================================================

        private IEnumerator TransitionToHero(HeroData hero)
        {
            // Fade out existing models
            if (_currentHeroModel != null || _currentMonsterModel != null)
            {
                yield return StartCoroutine(FadeOutModels());
            }

            if (_isDestroyed) yield break;

            Color heroColor = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;

            // Spawn hero placeholder (capsule)
            _currentHeroModel = CreatePlaceholderHero(heroColor);
            yield return StartCoroutine(AnimateSpawn(_currentHeroModel, _heroRenderer));

            if (_isDestroyed) yield break;

            // Delay then spawn monster placeholder (sphere)
            yield return _monsterDelayWait;

            if (_isDestroyed) yield break;

            _currentMonsterModel = CreatePlaceholderMonster(heroColor);
            yield return StartCoroutine(AnimateSpawn(_currentMonsterModel, _monsterRenderer));

            // Reset rotation
            _currentYRotation = 0f;
            _idleTimer = 0f;
            if (_stageRoot != null)
                _stageRoot.transform.localRotation = Quaternion.identity;
        }

        private GameObject CreatePlaceholderHero(Color color)
        {
            var hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hero.name = "HeroPlaceholder";
            hero.transform.SetParent(_stageRoot.transform);
            hero.transform.localPosition = _heroPosition;
            hero.transform.localScale = Vector3.one * _heroScale;
            SetLayer(hero, _renderLayer);

            // Remove collider (not needed for display)
            var col = hero.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Apply hero-colored material
            _heroMaterial = CreateTransparentMaterial(color);
            _heroRenderer = hero.GetComponent<Renderer>();
            if (_heroMaterial != null) _heroRenderer.material = _heroMaterial;

            return hero;
        }

        private GameObject CreatePlaceholderMonster(Color color)
        {
            var monster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            monster.name = "MonsterPlaceholder";
            monster.transform.SetParent(_stageRoot.transform);
            monster.transform.localPosition = _monsterPosition;
            monster.transform.localScale = Vector3.one * _monsterScale;
            SetLayer(monster, _renderLayer);

            var col = monster.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Slightly lighter color for the monster
            Color monsterColor = Color.Lerp(color, Color.white, 0.3f);
            _monsterMaterial = CreateTransparentMaterial(monsterColor);
            _monsterRenderer = monster.GetComponent<Renderer>();
            if (_monsterMaterial != null) _monsterRenderer.material = _monsterMaterial;

            return monster;
        }

        /// <summary>
        /// Creates a URP Lit material configured for transparency.
        /// Falls back to Standard shader if URP Lit is unavailable.
        /// </summary>
        private static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                // Fallback for when URP Lit isn't available (e.g., stripped builds)
                shader = Shader.Find("Standard");
                if (shader == null)
                {
                    var fallbackShader = Shader.Find("Sprites/Default");
                    if (fallbackShader == null)
                    {
                        Debug.LogError("[HeroStage] No shaders available — cannot create material");
                        return null;
                    }
                    Debug.LogWarning("[HeroStage] No suitable shader found, using Sprites/Default");
                    return new Material(fallbackShader);
                }

                var mat = new Material(shader);
                // Standard shader transparency setup
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                mat.color = color;
                return mat;
            }

            var urpMat = new Material(shader);
            urpMat.SetColor("_BaseColor", color);

            // Configure for transparency
            urpMat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            urpMat.SetFloat("_Blend", 0f);   // 0 = Alpha
            urpMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            urpMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            urpMat.SetInt("_ZWrite", 0);
            urpMat.renderQueue = 3000;
            urpMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return urpMat;
        }

        private IEnumerator AnimateSpawn(GameObject model, Renderer cachedRenderer)
        {
            if (model == null) yield break;

            Vector3 targetPos = model.transform.localPosition;
            Vector3 targetScale = model.transform.localScale;
            Vector3 startPos = targetPos + Vector3.down * 0.5f;
            Vector3 startScale = targetScale * 0.3f;

            model.transform.localPosition = startPos;
            model.transform.localScale = startScale;
            SetMaterialAlpha(cachedRenderer, 0f);

            float elapsed = 0f;
            while (elapsed < _spawnDuration)
            {
                if (_isDestroyed || model == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _spawnDuration);
                // EaseOutBack for a bouncy entrance
                float eased = EaseOutBack(t);

                model.transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);
                model.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
                SetMaterialAlpha(cachedRenderer, t); // Linear alpha

                yield return null;
            }

            if (model != null)
            {
                model.transform.localPosition = targetPos;
                model.transform.localScale = targetScale;
                SetMaterialAlpha(cachedRenderer, 1f);
            }
        }

        private IEnumerator FadeOutModels()
        {
            float elapsed = 0f;
            GameObject hero = _currentHeroModel;
            GameObject monster = _currentMonsterModel;
            Material heroMat = _heroMaterial;
            Material monsterMat = _monsterMaterial;
            Renderer heroR = _heroRenderer;
            Renderer monsterR = _monsterRenderer;
            _currentHeroModel = null;
            _currentMonsterModel = null;
            _heroMaterial = null;
            _monsterMaterial = null;
            _heroRenderer = null;
            _monsterRenderer = null;

            // Track in pending list so FlushPendingDestroys can clean up if coroutine is interrupted
            if (hero != null) _pendingDestroyModels.Add(hero);
            if (monster != null) _pendingDestroyModels.Add(monster);
            if (heroMat != null) _pendingDestroyMaterials.Add(heroMat);
            if (monsterMat != null) _pendingDestroyMaterials.Add(monsterMat);

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeOutDuration);
                float alpha = 1f - t;

                SetMaterialAlpha(heroR, alpha);
                SetMaterialAlpha(monsterR, alpha);

                yield return null;
            }

            // Coroutine completed normally — destroy and remove from pending lists
            if (hero != null) { Destroy(hero); _pendingDestroyModels.Remove(hero); }
            if (monster != null) { Destroy(monster); _pendingDestroyModels.Remove(monster); }
            if (heroMat != null) { Destroy(heroMat); _pendingDestroyMaterials.Remove(heroMat); }
            if (monsterMat != null) { Destroy(monsterMat); _pendingDestroyMaterials.Remove(monsterMat); }
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        private static void SetLayer(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayer(child.gameObject, layer);
        }

        /// <summary>
        /// Sets the alpha on a cached renderer's material without allocating new material copies.
        /// </summary>
        private static void SetMaterialAlpha(Renderer r, float alpha)
        {
            if (r == null) return;
            var mat = r.sharedMaterial;
            if (mat == null) return;

            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}

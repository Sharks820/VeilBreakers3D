using System.Collections;
using System.Collections.Generic;
using System;
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
        private const string kHeroModelResourceRoot = "Art/3D_Models/Characters";
        private static readonly Dictionary<string, string> kHeroModelResourceById = new Dictionary<string, string>
        {
            { "vex", "vex_medieval_knight" }
        };

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Stage Setup")]
        [SerializeField] private int _renderTextureWidth = 1024;
        [SerializeField] private int _renderTextureHeight = 1024;
        [SerializeField] private int _renderLayer = 31; // Use a high layer to avoid conflicts

        [Header("Camera")]
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0f, 1.05f, -3f);
        [SerializeField] private Vector3 _cameraLookAt = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private float _cameraFOV = 22f;
        [SerializeField] private float _minZoom = -4.5f;
        [SerializeField] private float _maxZoom = -1.5f;
        [SerializeField] private float _zoomSpeed = 0.5f;
        [SerializeField] private float _framingPadding = 0.95f;
        [SerializeField] private float _framingMinDistance = 0.8f;
        [SerializeField] private float _framingMaxDistance = 3.8f;

        [Header("Model Positions")]
        [SerializeField] private Vector3 _heroPosition = new Vector3(0f, -1.45f, 0f);
        [SerializeField] private Vector3 _monsterPosition = new Vector3(0.7f, 0f, 0.3f);
        [SerializeField] private float _heroScale = 1.4f;
        [SerializeField] private float _monsterScale = 0.5f;
        [SerializeField] private bool _showCompanionPlaceholder = false;

        [Header("Interaction")]
        [SerializeField] private float _rotationSpeed = 0.3f;
        [SerializeField] private bool _enableAutoOrbit = false;
        [SerializeField] private float _autoOrbitSpeed = 5f;
        [SerializeField] private float _idleBeforeOrbit = 3f;
        [SerializeField] private float _rotateStepDegrees = 20f;
        [SerializeField] private float _initialFacingYaw = -90f;

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
        private Renderer[] _heroModelRenderers;

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
            if (_enableAutoOrbit && !_isDragging)
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

        public void RotateByStep(float direction)
        {
            if (_isDestroyed || _stageRoot == null) return;

            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            _currentYRotation += Mathf.Sign(direction) * _rotateStepDegrees;
            _stageRoot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
            _idleTimer = 0f;
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

            _currentHeroModel = TryCreateHeroModel(hero);
            if (_currentHeroModel == null)
            {
                _currentHeroModel = CreatePlaceholderHero(heroColor);
                yield return StartCoroutine(AnimateSpawn(_currentHeroModel, _heroMaterial));
            }
            else
            {
                _heroMaterial = null;
                _heroRenderer = null; // Imported models may use shared materials; do not alpha-fade them.
                _heroModelRenderers = _currentHeroModel.GetComponentsInChildren<Renderer>(true);
                yield return StartCoroutine(AnimateSpawn(_currentHeroModel, null));
            }

            if (_isDestroyed) yield break;

            FitCameraToCurrentHero();

            if (_showCompanionPlaceholder)
            {
                // Delay then spawn monster placeholder (sphere)
                yield return _monsterDelayWait;

                if (_isDestroyed) yield break;

                _currentMonsterModel = CreatePlaceholderMonster(heroColor);
                yield return StartCoroutine(AnimateSpawn(_currentMonsterModel, _monsterMaterial));
            }

            // Reset rotation to a forward-facing pose for imported models.
            _currentYRotation = _initialFacingYaw;
            _idleTimer = 0f;
            if (_stageRoot != null)
                _stageRoot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        }

        private GameObject TryCreateHeroModel(HeroData hero)
        {
            if (hero == null || string.IsNullOrWhiteSpace(hero.hero_id))
            {
                return null;
            }

            string heroId = hero.hero_id.Trim().ToLowerInvariant();
            if (!kHeroModelResourceById.TryGetValue(heroId, out string resourceName))
            {
                return null;
            }

            var prefab = LoadHeroPrefab(resourceName);
            if (prefab == null)
            {
                Debug.LogWarning($"[HeroStage] Missing model resource for hero '{heroId}' at {kHeroModelResourceRoot}/{resourceName}.");
                return null;
            }

            var model = Instantiate(prefab, _stageRoot.transform);
            model.name = $"Hero_{heroId}";
            model.transform.localRotation = Quaternion.identity;
            SetLayer(model, _renderLayer);

            // Remove colliders in character preview stage (runs once per hero switch, allocation acceptable).
            var colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) Destroy(colliders[i]);
            }

            PositionModelOnStage(model);
            return model;
        }

        private static GameObject LoadHeroPrefab(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            string normalizedName = resourceName.Trim();

            // Standard Resources path without extension.
            var prefab = Resources.Load<GameObject>($"{kHeroModelResourceRoot}/{normalizedName}");
            if (prefab != null)
            {
                return prefab;
            }

            // Some importers expose the source extension as part of the asset name.
            prefab = Resources.Load<GameObject>($"{kHeroModelResourceRoot}/{normalizedName}.glb");
            if (prefab != null)
            {
                return prefab;
            }

            var rawObject = Resources.Load($"{kHeroModelResourceRoot}/{normalizedName}");
            if (rawObject is GameObject directObject)
            {
                return directObject;
            }
            if (rawObject is Component directComponent && directComponent.gameObject != null)
            {
                return directComponent.gameObject;
            }

            rawObject = Resources.Load($"{kHeroModelResourceRoot}/{normalizedName}.glb");
            if (rawObject is GameObject extensionObject)
            {
                return extensionObject;
            }
            if (rawObject is Component extensionComponent && extensionComponent.gameObject != null)
            {
                return extensionComponent.gameObject;
            }

            // All standard Resources.Load paths exhausted; skip Resources.LoadAll to avoid loading every asset.
            Debug.LogWarning($"[HeroStage] Could not load hero model '{normalizedName}' via Resources.Load; trying editor fallback.");

#if UNITY_EDITOR
            // Editor-only fallback for model assets that are not exposed as Resources GameObjects.
            string[] directPaths =
            {
                $"Assets/Resources/{kHeroModelResourceRoot}/{normalizedName}.glb",
                $"Assets/Resources/{kHeroModelResourceRoot}/{normalizedName}.gltf",
                $"Assets/Resources/{kHeroModelResourceRoot}/{normalizedName}.prefab"
            };

            for (int i = 0; i < directPaths.Length; i++)
            {
                string assetPath = directPaths[i];
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    return prefab;
                }

                var mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
                if (mainAsset != null)
                {
                    return mainAsset;
                }

                var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int subIndex = 0; subIndex < subAssets.Length; subIndex++)
                {
                    if (subAssets[subIndex] is GameObject go)
                    {
                        return go;
                    }
                }
            }

            // Final fallback: find any imported GameObject named after this model under Resources.
            var guids = UnityEditor.AssetDatabase.FindAssets($"{normalizedName} t:GameObject", new[] { "Assets/Resources" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    return prefab;
                }
            }
#endif

            return null;
        }

        private void PositionModelOnStage(GameObject model)
        {
            if (model == null) return;

            model.transform.localPosition = _heroPosition;
            model.transform.localScale = Vector3.one;

            if (!TryGetModelBounds(model, out var bounds))
            {
                model.transform.localScale = Vector3.one * _heroScale;
                return;
            }

            float targetHeight = Mathf.Max(2.2f, 4.2f * _heroScale);
            float currentHeight = Mathf.Max(0.01f, bounds.size.y);
            float uniformScale = Mathf.Clamp(targetHeight / currentHeight, 0.01f, 50f);
            model.transform.localScale = Vector3.one * uniformScale;

            if (!TryGetModelBounds(model, out bounds))
            {
                return;
            }

            Vector3 desiredAnchor = new Vector3(_heroPosition.x, _heroPosition.y, _heroPosition.z);
            Vector3 currentAnchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.transform.position += desiredAnchor - currentAnchor;
        }

        private void FitCameraToCurrentHero()
        {
            if (_stageCamera == null || _currentHeroModel == null)
            {
                return;
            }

            if (!TryGetModelBounds(_currentHeroModel, out var bounds, _heroModelRenderers))
            {
                return;
            }

            float aspect = _renderTexture != null
                ? Mathf.Max(0.5f, _renderTexture.width / Mathf.Max(1f, (float)_renderTexture.height))
                : Mathf.Max(0.5f, _stageCamera.aspect);

            float verticalFovRad = Mathf.Deg2Rad * _stageCamera.fieldOfView;
            float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * aspect);

            float paddedHeight = bounds.size.y * _framingPadding;
            float paddedWidth = bounds.size.x * _framingPadding;

            float distanceByHeight = (paddedHeight * 0.5f) / Mathf.Max(0.001f, Mathf.Tan(verticalFovRad * 0.5f));
            float distanceByWidth = (paddedWidth * 0.5f) / Mathf.Max(0.001f, Mathf.Tan(horizontalFovRad * 0.5f));
            float distance = Mathf.Max(distanceByHeight, distanceByWidth) + Mathf.Max(0.25f, bounds.extents.z * 0.65f);
            distance *= 0.58f;
            distance = Mathf.Clamp(distance, _framingMinDistance, _framingMaxDistance);

            // Bias focus toward upper torso so the full character sits lower in frame.
            Vector3 lookTarget = bounds.center + Vector3.up * (bounds.size.y * 0.34f);
            float yOffset = _cameraPosition.y - _cameraLookAt.y;
            Vector3 camPosition = new Vector3(lookTarget.x + _cameraPosition.x, lookTarget.y + yOffset, lookTarget.z - distance);

            _stageCamera.transform.position = camPosition;
            _stageCamera.transform.LookAt(lookTarget);
            _currentZoom = _stageCamera.transform.localPosition.z;
        }

        private static bool TryGetModelBounds(GameObject model, out Bounds combinedBounds, Renderer[] cachedRenderers = null)
        {
            combinedBounds = default;
            if (model == null) return false;

            var renderers = cachedRenderers ?? model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return false;

            bool initialized = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;

                if (!initialized)
                {
                    combinedBounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
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

            // Apply hero-colored material (we own this instance, use sharedMaterial to avoid internal clone)
            _heroMaterial = CreateTransparentMaterial(color);
            _heroRenderer = hero.GetComponent<Renderer>();
            if (_heroMaterial != null) _heroRenderer.sharedMaterial = _heroMaterial;

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
            if (_monsterMaterial != null) _monsterRenderer.sharedMaterial = _monsterMaterial;

            return monster;
        }

        /// <summary>
        /// Creates a transparent-friendly material for the active render pipeline.
        /// Avoids magenta fallback by preferring pipeline-compatible shaders.
        /// </summary>
        private static Material CreateTransparentMaterial(Color color)
        {
            bool usingScriptablePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = null;

            if (usingScriptablePipeline)
            {
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                Debug.LogError("[HeroStage] No suitable shader found for placeholder material.");
                return null;
            }

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            // Configure transparency for known shader models.
            if (string.Equals(shader.name, "Standard", System.StringComparison.Ordinal))
            {
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            else if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            return mat;
        }

        private IEnumerator AnimateSpawn(GameObject model, Material mat)
        {
            if (model == null) yield break;

            Vector3 targetPos = model.transform.localPosition;
            Vector3 targetScale = model.transform.localScale;
            Vector3 startPos = targetPos + Vector3.down * 0.5f;
            Vector3 startScale = targetScale * 0.3f;

            model.transform.localPosition = startPos;
            model.transform.localScale = startScale;
            SetMaterialAlpha(mat, 0f);

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
                SetMaterialAlpha(mat, t); // Linear alpha

                yield return null;
            }

            if (model != null)
            {
                model.transform.localPosition = targetPos;
                model.transform.localScale = targetScale;
                SetMaterialAlpha(mat, 1f);
            }
        }

        private IEnumerator FadeOutModels()
        {
            float elapsed = 0f;
            GameObject hero = _currentHeroModel;
            GameObject monster = _currentMonsterModel;
            Material heroMat = _heroMaterial;
            Material monsterMat = _monsterMaterial;
            _currentHeroModel = null;
            _currentMonsterModel = null;
            _heroMaterial = null;
            _monsterMaterial = null;
            _heroRenderer = null;
            _monsterRenderer = null;
            _heroModelRenderers = null;

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

                SetMaterialAlpha(heroMat, alpha);
                SetMaterialAlpha(monsterMat, alpha);

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
        /// Sets the alpha on a material we own (created at runtime) without allocating copies.
        /// </summary>
        private static void SetMaterialAlpha(Material mat, float alpha)
        {
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

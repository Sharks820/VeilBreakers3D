using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private const string kMonsterModelResourceRoot = "Art/3D_Models/Monsters";
        private static readonly Dictionary<string, string[]> kHeroModelResourceById = new Dictionary<string, string[]>
        {
            { "vex", new[] { "Vex", "Vex_for_mixamo", "vex_medieval_knight", "Vex_mesh_only" } }
        };
        private static readonly Dictionary<string, string[]> kMonsterModelResourceById = new Dictionary<string, string[]>
        {
            { "skitter_teeth", new[] { "SkitterTeeth" } }
        };
        private static readonly Dictionary<string, float> kHeroFacingYawById = new Dictionary<string, float>
        {
            { "vex", 270f }
        };

        // Animation controller resource paths per hero
        private const string kAnimControllerResourceRoot = "Art/Animations/Controllers";
        private static readonly Dictionary<string, string> kHeroAnimControllerById = new Dictionary<string, string>
        {
            { "vex", "VexAnimatorController" }
        };
        private static readonly Dictionary<string, string> kHeroFallbackTextureById = new Dictionary<string, string>
        {
            { "vex", "Art/Textures/Characters/Vex/baseColor" }
        };
        private static readonly HashSet<string> kHeroForceTextureFallbackById = new HashSet<string>
        {
            "vex"
        };
        private static readonly Dictionary<string, Texture2D> kRuntimeFallbackTextureCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Clear the runtime texture cache to free memory (call on scene unload).
        /// </summary>
        public static void ClearTextureCache()
        {
            kRuntimeFallbackTextureCache.Clear();
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Stage Setup")]
        [SerializeField] private int _renderTextureWidth = 1024;
        [SerializeField] private int _renderTextureHeight = 1024;
        [SerializeField] private int _renderLayer = 31; // Use a high layer to avoid conflicts

        [Header("Camera")]
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0f, 1.2f, -3f);
        [SerializeField] private Vector3 _cameraLookAt = new Vector3(0f, 0.65f, 0f);
        [SerializeField] private float _cameraFOV = 24f;
        [SerializeField] private float _minZoom = -4.5f;
        [SerializeField] private float _maxZoom = -1.5f;
        [SerializeField] private float _zoomSpeed = 0.5f;
        [SerializeField] private float _framingPadding = 0.95f;
        [SerializeField] private float _framingMinDistance = 0.8f;
        [SerializeField] private float _framingMaxDistance = 5.5f;
        [SerializeField, Range(0f, 0.8f)] private float _framingLookTargetBias = 0.35f;

        [Header("Model Positions")]
        [SerializeField] private Vector3 _heroPosition = new Vector3(0f, -2.1f, 0f);
        [SerializeField] private Vector3 _monsterPosition = new Vector3(0.7f, -2.1f, 0f);
        [SerializeField] private float _heroScale = 1.4f;
        [SerializeField] private float _monsterScale = 0.4f;
        [SerializeField] private bool _showCompanionPlaceholder = true;

        [Header("Interaction")]
        [SerializeField] private float _rotationSpeed = 0.3f;
        [SerializeField] private bool _enableAutoOrbit = false;
        [SerializeField] private float _autoOrbitSpeed = 5f;
        [SerializeField] private float _idleBeforeOrbit = 3f;
        [SerializeField] private float _rotateStepDegrees = 20f;
        [SerializeField] private float _initialFacingYaw = 270f;

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
        private GameObject _heroPivot;
        private GameObject _currentHeroModel;
        private GameObject _currentMonsterModel;
        private Light _stageLight;
        private Light _fillLight;
        private Light _rimLight;
        private Light _faceLight;
        private Light _eyeFillLight;
        private VisualElement _renderTarget;

        private bool _isDragging;
        private float _currentYRotation;
        private float _idleTimer;
        private float _currentZoom;
        private Vector3 _cameraOrbitBasePosition;
        private bool _isDestroyed;
        private Coroutine _transitionCoroutine;

        // Cached materials for cleanup
        private Material _heroMaterial;
        private Material _monsterMaterial;

        // Cached renderers to avoid GetComponentsInChildren allocations
        private Renderer _heroRenderer;
        private Renderer _monsterRenderer;
        private Renderer[] _heroModelRenderers;
        private Renderer[] _monsterModelRenderers;

        // Monster hover state
        private bool _isMonsterHovered;
        private Material[] _monsterOriginalMaterials;

        // Animation
        private Animator _heroAnimator;
        private float _animIdleTimer;
        private static readonly int _animIdleTimerHash = Animator.StringToHash("IdleTimer");
        private static readonly int _animSelectedHash = Animator.StringToHash("Selected");
        private static readonly int _animIdleStateHash = Animator.StringToHash("Idle");

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

            // Force-set ALL visual values (overrides any stale serialized inspector data)
            _cameraFOV = 30f;
            _cameraPosition = new Vector3(0f, 1.2f, -3f);
            _cameraLookAt = new Vector3(0f, 0.65f, 0f);
            _framingPadding = 0.95f;
            _framingMinDistance = 0.8f;
            _framingMaxDistance = 20f;
            _framingLookTargetBias = 0f;
            _heroPosition = new Vector3(0f, -2.1f, 0f);
            _heroScale = 1.4f;
            _monsterPosition = new Vector3(1.5f, -2.1f, 0.3f);
            _monsterScale = 0.3f;

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
                _renderTarget.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
                _renderTarget.UnregisterCallback<WheelEvent>(OnWheel);
            }

            CleanupStage();
        }

        private void Update()
        {
            if (_isDestroyed || _heroPivot == null) return;

            // Auto-orbit when idle (hero only)
            if (_enableAutoOrbit && !_isDragging)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= _idleBeforeOrbit && _currentHeroModel != null)
                {
                    _currentYRotation += _autoOrbitSpeed * Time.deltaTime;
                    _heroPivot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
                }
            }

            // Drive animator IdleTimer for idle variant transitions
            if (_heroAnimator != null && _heroAnimator.runtimeAnimatorController != null)
            {
                _animIdleTimer += Time.deltaTime;
                _heroAnimator.SetFloat(_animIdleTimerHash, _animIdleTimer);

                // Reset when the animator transitions back to Idle (variant finished)
                var stateInfo = _heroAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Idle") && _animIdleTimer > 1f)
                {
                    // Only reset if we already passed the threshold (variant played and returned)
                    if (_animIdleTimer > 12f)
                        _animIdleTimer = 0f;
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

            // Create stage root (models go under this; does NOT rotate itself)
            _stageRoot = new GameObject("CharacterStage");
            _stageRoot.transform.SetParent(transform);
            _stageRoot.transform.localPosition = Vector3.zero;

            // Hero pivot sits under stageRoot and handles hero-only rotation
            _heroPivot = new GameObject("HeroPivot");
            _heroPivot.transform.SetParent(_stageRoot.transform);
            _heroPivot.transform.localPosition = Vector3.zero;

            // Create camera
            var camObj = new GameObject("StageCamera");
            camObj.transform.SetParent(transform); // Not under stageRoot so rotation doesn't affect it
            camObj.transform.localPosition = _cameraPosition;
            _currentZoom = _cameraPosition.z;
            _cameraOrbitBasePosition = camObj.transform.localPosition;

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
            _stageLight.intensity = 1.6f;
            _stageLight.color = new Color(1f, 0.97f, 0.93f);
            _stageLight.cullingMask = 1 << _renderLayer;

            var fillObj = new GameObject("StageFillLight");
            fillObj.transform.SetParent(transform);
            fillObj.transform.localPosition = new Vector3(-1.2f, 1.35f, -0.7f);
            fillObj.transform.LookAt(Vector3.zero);
            _fillLight = fillObj.AddComponent<Light>();
            _fillLight.type = LightType.Directional;
            _fillLight.intensity = 0.8f;
            _fillLight.color = new Color(0.82f, 0.9f, 1f);
            _fillLight.cullingMask = 1 << _renderLayer;

            var rimObj = new GameObject("StageRimLight");
            rimObj.transform.SetParent(transform);
            rimObj.transform.localPosition = new Vector3(0.2f, 1.7f, 1.4f);
            rimObj.transform.LookAt(Vector3.zero);
            _rimLight = rimObj.AddComponent<Light>();
            _rimLight.type = LightType.Directional;
            _rimLight.intensity = 0.55f;
            _rimLight.color = new Color(0.72f, 0.81f, 1f);
            _rimLight.cullingMask = 1 << _renderLayer;

            var faceObj = new GameObject("StageFaceLight");
            faceObj.transform.SetParent(transform);
            faceObj.transform.localPosition = new Vector3(0.15f, 1.6f, -1.4f);
            faceObj.transform.LookAt(new Vector3(0f, 1.3f, 0f));
            _faceLight = faceObj.AddComponent<Light>();
            _faceLight.type = LightType.Spot;
            _faceLight.intensity = 3.2f;
            _faceLight.spotAngle = 60f;
            _faceLight.range = 8f;
            _faceLight.color = new Color(1f, 0.96f, 0.9f);
            _faceLight.cullingMask = 1 << _renderLayer;
            _faceLight.shadows = LightShadows.None;

            var eyeFillObj = new GameObject("StageEyeFillLight");
            eyeFillObj.transform.SetParent(transform);
            eyeFillObj.transform.localPosition = new Vector3(0f, 1.45f, -1.1f);
            eyeFillObj.transform.LookAt(new Vector3(0f, 1.35f, 0f));
            _eyeFillLight = eyeFillObj.AddComponent<Light>();
            _eyeFillLight.type = LightType.Spot;
            _eyeFillLight.intensity = 1.8f;
            _eyeFillLight.spotAngle = 70f;
            _eyeFillLight.range = 7f;
            _eyeFillLight.color = new Color(1f, 0.99f, 0.96f);
            _eyeFillLight.cullingMask = 1 << _renderLayer;
            _eyeFillLight.shadows = LightShadows.None;

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
            _heroPivot = null;
            if (_stageLight != null) Destroy(_stageLight.gameObject);
            if (_fillLight != null) Destroy(_fillLight.gameObject);
            if (_rimLight != null) Destroy(_rimLight.gameObject);
            if (_faceLight != null) Destroy(_faceLight.gameObject);
            if (_eyeFillLight != null) Destroy(_eyeFillLight.gameObject);

            if (_heroMaterial != null) Destroy(_heroMaterial);
            if (_monsterMaterial != null) Destroy(_monsterMaterial);
            _monsterModelRenderers = null;
            _monsterOriginalMaterials = null;
            _isMonsterHovered = false;

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

            _renderTarget.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _renderTarget.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _renderTarget.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _renderTarget.UnregisterCallback<WheelEvent>(OnWheel);
            _renderTarget.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _renderTarget.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _renderTarget.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _renderTarget.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
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
            // Check monster hover on every pointer move (regardless of drag state)
            UpdateMonsterHover(evt.localPosition);

            if (!_isDragging || _heroPivot == null) return;

            _currentYRotation += evt.deltaPosition.x * _rotationSpeed;
            _heroPivot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
            _idleTimer = 0;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            // Clear monster hover when pointer leaves the render target
            if (_isMonsterHovered)
            {
                _isMonsterHovered = false;
                SetMonsterHighlight(false);
            }
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
                _cameraOrbitBasePosition.x, _cameraOrbitBasePosition.y, _currentZoom);
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

            if (_stageRoot == null || _stageCamera == null)
            {
                CreateStage();
                SetupInteraction();
            }

            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

            // Flush any models/materials left over from interrupted FadeOutModels coroutines
            FlushPendingDestroys();

            _transitionCoroutine = StartCoroutine(TransitionToHero(hero));
        }

        /// <summary>Current Y-axis rotation in degrees (for syncing UI indicators).</summary>
        public float CurrentYRotation => _currentYRotation;

        public void RotateByStep(float direction)
        {
            if (_isDestroyed || _heroPivot == null) return;

            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            _currentYRotation += Mathf.Sign(direction) * _rotateStepDegrees;
            _heroPivot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
            _idleTimer = 0f;
        }

        /// <summary>Rotate by a continuous delta (for drag input from UI).</summary>
        public void RotateByDelta(float deltaDegrees)
        {
            if (_isDestroyed || _heroPivot == null) return;
            _currentYRotation += deltaDegrees;
            _heroPivot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
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
                _heroModelRenderers = _currentHeroModel.GetComponentsInChildren<Renderer>(true);
                if (_heroMaterial == null)
                {
                    _heroRenderer = null; // Imported models keep authored materials as-is.
                }
                else if (_heroModelRenderers != null && _heroModelRenderers.Length > 0)
                {
                    _heroRenderer = _heroModelRenderers[0];
                }

                yield return StartCoroutine(AnimateSpawn(_currentHeroModel, _heroMaterial));
            }

            if (_isDestroyed) yield break;

            FitCameraToCurrentHero();

            // Reset rotation to face the camera on spawn, then re-fit framing after rotation.
            _currentYRotation = ResolveFacingYaw(hero, _currentHeroModel);
            _idleTimer = 0f;
            if (_heroPivot != null)
            {
                _heroPivot.transform.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
            }
            FitCameraToCurrentHero();

            if (_showCompanionPlaceholder)
            {
                yield return _monsterDelayWait;
                if (_isDestroyed) yield break;

                _currentMonsterModel = TryCreateMonsterModel(hero.starter_monster_id);
                if (_currentMonsterModel == null)
                {
                    _currentMonsterModel = CreatePlaceholderMonster(heroColor);
                }
                yield return StartCoroutine(AnimateSpawn(_currentMonsterModel, _monsterMaterial));

                // Re-fit camera to encompass both hero and monster
                FitCameraToFullStage();
            }
        }

        private GameObject TryCreateHeroModel(HeroData hero)
        {
            if (hero == null || string.IsNullOrWhiteSpace(hero.hero_id))
            {
                return null;
            }

            string heroId = hero.hero_id.Trim().ToLowerInvariant();
            if (!kHeroModelResourceById.TryGetValue(heroId, out string[] resourceNames)
                || resourceNames == null
                || resourceNames.Length == 0)
            {
                return null;
            }

            GameObject prefab = null;
            string resolvedResourceName = null;
            for (int i = 0; i < resourceNames.Length; i++)
            {
                prefab = LoadHeroPrefab(resourceNames[i]);
                if (prefab != null)
                {
                    resolvedResourceName = resourceNames[i];
                    break;
                }
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[HeroStage] Missing model resource for hero '{heroId}' under {kHeroModelResourceRoot}.");
                return null;
            }

            var model = Instantiate(prefab, _heroPivot.transform);
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
            EnsureModelHasMaterials(model, heroId);
            Debug.Log($"[HeroStage] Loaded hero model '{heroId}' from resource '{resolvedResourceName}'.");
            SetupAnimator(model, heroId, resolvedResourceName);
            return model;
        }

        private GameObject TryCreateMonsterModel(string monsterId)
        {
            if (string.IsNullOrWhiteSpace(monsterId)) return null;

            string id = monsterId.Trim().ToLowerInvariant();
            if (!kMonsterModelResourceById.TryGetValue(id, out string[] resourceNames)
                || resourceNames == null || resourceNames.Length == 0)
            {
                return null;
            }

            GameObject prefab = null;
            for (int i = 0; i < resourceNames.Length; i++)
            {
                string path = $"{kMonsterModelResourceRoot}/{resourceNames[i]}";
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null) break;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[HeroStage] Missing model resource for monster '{id}' under {kMonsterModelResourceRoot}.");
                return null;
            }

            var model = Instantiate(prefab, _stageRoot.transform);
            model.name = $"Monster_{id}";
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            SetLayer(model, _renderLayer);

            var colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) Destroy(colliders[i]);
            }

            PositionMonsterOnStage(model);

            // Keep authored materials from the FBX; cache all renderers for hover highlight
            _monsterModelRenderers = model.GetComponentsInChildren<Renderer>(true);
            _monsterRenderer = _monsterModelRenderers.Length > 0 ? _monsterModelRenderers[0] : null;
            _monsterMaterial = null;

            Debug.Log($"[HeroStage] Loaded monster model '{id}' at local pos {model.transform.localPosition}, world pos {model.transform.position}.");
            return model;
        }

        private float ResolveFacingYaw(HeroData hero, GameObject model)
        {
            if (hero == null || string.IsNullOrWhiteSpace(hero.hero_id))
            {
                if (TryResolveAutoFacingYaw(model, out float defaultAutoYaw))
                {
                    return defaultAutoYaw;
                }

                return _initialFacingYaw;
            }

            string heroId = hero.hero_id.Trim().ToLowerInvariant();
            if (kHeroFacingYawById.TryGetValue(heroId, out float yaw))
            {
                return yaw;
            }

            if (TryResolveAutoFacingYaw(model, out float autoYaw))
            {
                return autoYaw;
            }

            return _initialFacingYaw;
        }

        private bool TryResolveAutoFacingYaw(GameObject model, out float yaw)
        {
            yaw = _initialFacingYaw;
            if (model == null || _stageRoot == null || _stageCamera == null)
            {
                return false;
            }

            Transform facingTransform = model.transform;
            var animator = model.GetComponentInChildren<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    facingTransform = hips;
                }
            }
            else
            {
                var skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedRenderer != null && skinnedRenderer.rootBone != null)
                {
                    facingTransform = skinnedRenderer.rootBone;
                }
            }

            Vector3 sourceForward = Vector3.ProjectOnPlane(facingTransform.forward, Vector3.up);
            Vector3 desiredForward = Vector3.ProjectOnPlane(-_stageCamera.transform.forward, Vector3.up);
            if (sourceForward.sqrMagnitude < 0.0001f || desiredForward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            sourceForward.Normalize();
            desiredForward.Normalize();
            float deltaYaw = Vector3.SignedAngle(sourceForward, desiredForward, Vector3.up);
            yaw = Mathf.Repeat(_heroPivot.transform.localEulerAngles.y + deltaYaw, 360f);
            return true;
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
                $"Assets/Resources/{kHeroModelResourceRoot}/{normalizedName}.fbx",
                $"Assets/Resources/{kHeroModelResourceRoot}/{normalizedName}.blend",
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

            Vector3 desiredAnchor = _heroPivot != null
                ? _heroPivot.transform.TransformPoint(_heroPosition)
                : _heroPosition;
            Vector3 currentAnchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.transform.position += desiredAnchor - currentAnchor;
        }

        /// <summary>
        /// Positions and scales the monster model beside the hero using bounds-based sizing.
        /// Targets half the hero's display height and anchors feet to _monsterPosition.
        /// </summary>
        private void PositionMonsterOnStage(GameObject model)
        {
            if (model == null) return;

            model.transform.localPosition = _monsterPosition;
            model.transform.localScale = Vector3.one;

            if (!TryGetModelBounds(model, out var bounds))
            {
                model.transform.localScale = Vector3.one * _monsterScale;
                return;
            }

            // Target half the hero's display height
            float heroTargetHeight = Mathf.Max(2.2f, 4.2f * _heroScale);
            float monsterTargetHeight = heroTargetHeight * _monsterScale;
            float currentHeight = Mathf.Max(0.01f, bounds.size.y);
            float uniformScale = Mathf.Clamp(monsterTargetHeight / currentHeight, 0.01f, 50f);
            model.transform.localScale = Vector3.one * uniformScale;

            if (!TryGetModelBounds(model, out bounds))
            {
                return;
            }

            // Anchor the monster's feet to the desired position
            Vector3 desiredAnchor = _stageRoot != null
                ? _stageRoot.transform.TransformPoint(_monsterPosition)
                : _monsterPosition;
            Vector3 currentAnchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.transform.position += desiredAnchor - currentAnchor;
        }

        /// <summary>
        /// Checks if the pointer (in local render target coordinates) is over the monster
        /// model using bounds-based ray intersection, and toggles highlight accordingly.
        /// </summary>
        private void UpdateMonsterHover(Vector2 localPosition)
        {
            if (_stageCamera == null || _currentMonsterModel == null || _renderTarget == null) return;

            // Convert local position to UV (0-1) on the render target
            float w = _renderTarget.layout.width;
            float h = _renderTarget.layout.height;
            if (w < 1f || h < 1f) return;

            Vector2 uv = new Vector2(localPosition.x / w, 1f - (localPosition.y / h)); // Flip Y for viewport
            Ray ray = _stageCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));

            bool overMonster = false;
            if (TryGetModelBounds(_currentMonsterModel, out var monsterBounds, _monsterModelRenderers))
            {
                overMonster = monsterBounds.IntersectRay(ray);
            }

            if (overMonster != _isMonsterHovered)
            {
                _isMonsterHovered = overMonster;
                SetMonsterHighlight(overMonster);
            }
        }

        /// <summary>
        /// Returns the normalized Y position (0=top, 1=bottom) of the hero's feet
        /// in the render texture viewport. Used by UI to position the rotation ring.
        /// </summary>
        public float GetHeroFeetViewportY()
        {
            if (_stageCamera == null || _currentHeroModel == null) return 0.85f;

            Vector3 feetWorld;
            if (TryGetModelBounds(_currentHeroModel, out var bounds, _heroModelRenderers))
            {
                feetWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            else
            {
                feetWorld = _currentHeroModel.transform.position;
            }

            Vector3 viewportPoint = _stageCamera.WorldToViewportPoint(feetWorld);
            // Viewport Y: 0=bottom, 1=top. UI Y: 0=top, 1=bottom. Flip it.
            return Mathf.Clamp01(1f - viewportPoint.y);
        }

        /// <summary>
        /// Highlights the monster model in the 3D stage (e.g. on hover).
        /// </summary>
        public void SetMonsterHighlight(bool highlighted)
        {
            if (_monsterModelRenderers == null || _monsterModelRenderers.Length == 0) return;

            if (highlighted)
            {
                // Cache original materials if not yet cached
                if (_monsterOriginalMaterials == null)
                {
                    _monsterOriginalMaterials = new Material[_monsterModelRenderers.Length];
                    for (int i = 0; i < _monsterModelRenderers.Length; i++)
                    {
                        if (_monsterModelRenderers[i] == null) continue;
                        // Create material instances for highlight
                        _monsterOriginalMaterials[i] = new Material(_monsterModelRenderers[i].material);
                        _monsterModelRenderers[i].material = _monsterOriginalMaterials[i];
                    }
                }

                for (int i = 0; i < _monsterOriginalMaterials.Length; i++)
                {
                    if (_monsterOriginalMaterials[i] == null) continue;
                    _monsterOriginalMaterials[i].SetColor("_EmissionColor", new Color(0.35f, 0.28f, 0.1f, 1f));
                    _monsterOriginalMaterials[i].EnableKeyword("_EMISSION");
                }
            }
            else
            {
                if (_monsterOriginalMaterials == null) return;
                for (int i = 0; i < _monsterOriginalMaterials.Length; i++)
                {
                    if (_monsterOriginalMaterials[i] == null) continue;
                    _monsterOriginalMaterials[i].SetColor("_EmissionColor", Color.black);
                    _monsterOriginalMaterials[i].DisableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>
        /// Checks if the model's renderers have valid materials with textures.
        /// If the model appears untextured (default white), applies a stylized
        /// dark metallic fallback material so it looks presentable.
        /// </summary>
        private void EnsureModelHasMaterials(GameObject model, string heroId)
        {
            if (model == null) return;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            bool forceFallback = !string.IsNullOrWhiteSpace(heroId) && kHeroForceTextureFallbackById.Contains(heroId);

            bool hasAnyMaterialSlot = false;
            bool needsFallbackMaterial = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterials == null) continue;

                for (int m = 0; m < r.sharedMaterials.Length; m++)
                {
                    var mat = r.sharedMaterials[m];
                    if (mat == null)
                    {
                        continue;
                    }

                    hasAnyMaterialSlot = true;
                    if (!HasAnyAssignedTexture(mat))
                    {
                        needsFallbackMaterial = true;
                        break;
                    }
                }

                if (needsFallbackMaterial)
                {
                    break;
                }
            }

            if (!hasAnyMaterialSlot || (!needsFallbackMaterial && !forceFallback)) return;

            Texture2D fallbackTexture = LoadFallbackTexture(heroId);
            if (fallbackTexture == null)
            {
                Debug.LogWarning($"[HeroStage] '{heroId}' appears untextured and no fallback texture is configured.");
            }
            else
            {
                Debug.Log($"[HeroStage] Applying extracted fallback texture for '{heroId}'.");
            }

            var fallbackMat = CreateHeroFallbackMaterial(fallbackTexture);
            if (fallbackMat == null) return;

            _heroMaterial = fallbackMat;
            _heroRenderer = renderers[0];
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                var sharedMaterials = renderer.sharedMaterials;
                bool updated = false;
                for (int m = 0; m < sharedMaterials.Length; m++)
                {
                    if (forceFallback || sharedMaterials[m] == null || !HasAnyAssignedTexture(sharedMaterials[m]))
                    {
                        sharedMaterials[m] = fallbackMat;
                        updated = true;
                    }
                }

                if (updated)
                {
                    renderer.sharedMaterials = sharedMaterials;
                }
            }
        }

        private static Texture2D LoadFallbackTexture(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return null;
            if (!kHeroFallbackTextureById.TryGetValue(heroId, out string texturePath) || string.IsNullOrWhiteSpace(texturePath))
            {
                return null;
            }

            texturePath = texturePath.Trim();
            string normalizedPath = texturePath.Replace("\\", "/");
            string pathWithoutExtension = System.IO.Path.ChangeExtension(normalizedPath, null)?.Replace("\\", "/") ?? normalizedPath;

            Texture2D texture = Resources.Load<Texture2D>(normalizedPath);
            if (texture != null)
            {
                return texture;
            }

            texture = Resources.Load<Texture2D>(pathWithoutExtension);
            if (texture != null)
            {
                return texture;
            }

            string[] extensions = { ".png", ".jpg", ".jpeg", ".tga" };
#if UNITY_EDITOR
            for (int i = 0; i < extensions.Length; i++)
            {
                string assetPath = $"Assets/Resources/{pathWithoutExtension}{extensions[i]}";
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    return texture;
                }
            }
#endif

            string[] absoluteCandidates = new string[extensions.Length];
            for (int i = 0; i < extensions.Length; i++)
            {
                absoluteCandidates[i] = System.IO.Path.Combine(Application.dataPath, "Resources", $"{pathWithoutExtension}{extensions[i]}");
            }

            for (int i = 0; i < absoluteCandidates.Length; i++)
            {
                string absolutePath = absoluteCandidates[i];
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                if (kRuntimeFallbackTextureCache.TryGetValue(absolutePath, out var cachedTexture) && cachedTexture != null)
                {
                    return cachedTexture;
                }

                try
                {
                    byte[] bytes = File.ReadAllBytes(absolutePath);
                    if (bytes == null || bytes.Length == 0)
                    {
                        continue;
                    }

                    var runtimeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    runtimeTexture.name = $"{heroId}_fallback_texture";
                    if (runtimeTexture.LoadImage(bytes, false))
                    {
                        kRuntimeFallbackTextureCache[absolutePath] = runtimeTexture;
                        return runtimeTexture;
                    }
                    UnityEngine.Object.Destroy(runtimeTexture);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HeroStage] Failed loading fallback texture at '{absolutePath}': {ex.Message}");
                }
            }

            return null;
        }

        private static bool HasAnyAssignedTexture(Material mat)
        {
            if (mat == null) return false;

            if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) return true;
            if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null) return true;
            if (mat.HasProperty("_MetallicGlossMap") && mat.GetTexture("_MetallicGlossMap") != null) return true;
            if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null) return true;
            if (mat.HasProperty("_OcclusionMap") && mat.GetTexture("_OcclusionMap") != null) return true;

            return false;
        }

        /// <summary>
        /// Creates a dark metallic material that looks presentable as a placeholder.
        /// </summary>
        private static Material CreateHeroFallbackMaterial(Texture2D baseTexture = null)
        {
            bool usingScriptablePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = null;

            if (usingScriptablePipeline)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null) return null;

            var mat = new Material(shader);
            // Keep albedo neutral so fallback texture stays readable.
            Color baseColor = Color.white;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseColor);
            if (baseTexture != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", baseTexture);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", baseTexture);
            }

            // Keep fallback physically plausible but readable for character preview.
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.1f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.3f);
            if (mat.HasProperty("_GlossMapScale"))
                mat.SetFloat("_GlossMapScale", 0.3f);

            return mat;
        }

        /// <summary>
        /// Configures an Animator component on the hero model for idle/select animations.
        /// Loads a RuntimeAnimatorController from Resources if available.
        /// </summary>
        private void SetupAnimator(GameObject model, string heroId, string resolvedResourceName)
        {
            _heroAnimator = null;
            _animIdleTimer = 0f;

            if (model == null) return;

            // Check if model already has an Animator (from the FBX import)
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            Debug.Log($"[HeroStage] SetupAnimator: heroId='{heroId}', resource='{resolvedResourceName}', hasAnimator={animator != null}, avatar={animator.avatar?.name ?? "null"}, isHuman={animator.isHuman}");

            if (animator.avatar == null)
            {
                animator.avatar = LoadHeroAvatar(resolvedResourceName, heroId);
                Debug.Log($"[HeroStage] Loaded avatar: {animator.avatar?.name ?? "null"}, isHuman={animator.isHuman}");
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Try to load a hero-specific animator controller
            if (kHeroAnimControllerById.TryGetValue(heroId, out string controllerName))
            {
                string path = $"{kAnimControllerResourceRoot}/{controllerName}";
                var controller = Resources.Load<RuntimeAnimatorController>(path);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    _heroAnimator = animator;

                    _heroAnimator.Rebind();
                    _heroAnimator.Update(0f);
                    _heroAnimator.SetBool(_animSelectedHash, false);
                    _heroAnimator.SetFloat(_animIdleTimerHash, 0f);
                    bool hasIdleState = _heroAnimator.HasState(0, _animIdleStateHash);
                    Debug.Log($"[HeroStage] AnimController loaded! hasIdleState={hasIdleState}, avatar={animator.avatar?.name ?? "null"}, isHuman={animator.isHuman}");
                    if (hasIdleState)
                    {
                        _heroAnimator.Play(_animIdleStateHash, 0, 0f);
                    }
                    _heroAnimator.Update(0f);
                }
                else
                {
                    Debug.LogWarning($"[HeroStage] No AnimatorController found at Resources/{path}. Hero will be static.");
                }
            }
            else
            {
                Debug.LogWarning($"[HeroStage] No animator controller mapping for heroId '{heroId}'.");
            }
        }

        private Avatar LoadHeroAvatar(string resolvedResourceName, string heroId)
        {
            if (!string.IsNullOrWhiteSpace(resolvedResourceName))
            {
                var avatar = LoadAvatarByResourceName(resolvedResourceName);
                if (avatar != null)
                {
                    return avatar;
                }
            }

            if (string.IsNullOrWhiteSpace(heroId)) return null;
            if (!kHeroModelResourceById.TryGetValue(heroId, out var resourceNames) || resourceNames == null)
            {
                return null;
            }

            for (int i = 0; i < resourceNames.Length; i++)
            {
                var avatar = LoadAvatarByResourceName(resourceNames[i]);
                if (avatar != null)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static Avatar LoadAvatarByResourceName(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            var assets = Resources.LoadAll($"{kHeroModelResourceRoot}/{resourceName.Trim()}");
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
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
            distance *= 1.15f; // Show full hero head-to-toe with ~15% breathing room
            distance = Mathf.Clamp(distance, _framingMinDistance, _framingMaxDistance);

            // Push look target toward upper body so hero appears lower and more centered.
            Vector3 lookTarget = bounds.center + Vector3.up * (bounds.size.y * _framingLookTargetBias);
            float yOffset = _cameraPosition.y - _cameraLookAt.y;
            Vector3 camPosition = new Vector3(lookTarget.x + _cameraPosition.x, lookTarget.y + yOffset, lookTarget.z - distance);

            _stageCamera.transform.position = camPosition;
            _stageCamera.transform.LookAt(lookTarget);
            _cameraOrbitBasePosition = _stageCamera.transform.localPosition;
            _currentZoom = _cameraOrbitBasePosition.z;
            Debug.Log($"[HeroStage] FitCameraToCurrentHero: bounds.h={bounds.size.y:F2}, FOV={_stageCamera.fieldOfView}, dist={distance:F2}, camPos={camPosition}, lookAt={lookTarget}, bias={_framingLookTargetBias}");
        }

        /// <summary>
        /// Fits the camera to show both hero and monster. Called after monster spawn.
        /// Uses combined bounds so both models are visible in the render texture.
        /// </summary>
        private void FitCameraToFullStage()
        {
            if (_stageCamera == null || _currentHeroModel == null) return;

            // If no monster, fall back to hero-only framing
            if (_currentMonsterModel == null)
            {
                FitCameraToCurrentHero();
                return;
            }

            if (!TryGetModelBounds(_currentHeroModel, out var heroBounds, _heroModelRenderers)) return;
            if (!TryGetModelBounds(_currentMonsterModel, out var monsterBounds, _monsterModelRenderers))
            {
                FitCameraToCurrentHero();
                return;
            }

            // Combine both bounds
            Bounds combinedBounds = heroBounds;
            combinedBounds.Encapsulate(monsterBounds);

            float aspect = _renderTexture != null
                ? Mathf.Max(0.5f, _renderTexture.width / Mathf.Max(1f, (float)_renderTexture.height))
                : Mathf.Max(0.5f, _stageCamera.aspect);

            float verticalFovRad = Mathf.Deg2Rad * _stageCamera.fieldOfView;
            float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * aspect);

            float paddedHeight = combinedBounds.size.y * _framingPadding;
            float paddedWidth = combinedBounds.size.x * _framingPadding;

            float distanceByHeight = (paddedHeight * 0.5f) / Mathf.Max(0.001f, Mathf.Tan(verticalFovRad * 0.5f));
            float distanceByWidth = (paddedWidth * 0.5f) / Mathf.Max(0.001f, Mathf.Tan(horizontalFovRad * 0.5f));
            float distance = Mathf.Max(distanceByHeight, distanceByWidth) + Mathf.Max(0.25f, combinedBounds.extents.z * 0.65f);
            // Generous framing to show both hero and monster fully
            distance *= 1.2f;
            distance = Mathf.Clamp(distance, _framingMinDistance, _framingMaxDistance);

            // Center the look target between both models, biased slightly toward the hero's upper body
            Vector3 lookTarget = new Vector3(
                heroBounds.center.x * 0.7f + combinedBounds.center.x * 0.3f,
                combinedBounds.center.y + combinedBounds.size.y * _framingLookTargetBias * 0.5f,
                combinedBounds.center.z
            );

            float yOffset = _cameraPosition.y - _cameraLookAt.y;
            Vector3 camPosition = new Vector3(lookTarget.x + _cameraPosition.x, lookTarget.y + yOffset, lookTarget.z - distance);

            _stageCamera.transform.position = camPosition;
            _stageCamera.transform.LookAt(lookTarget);
            _cameraOrbitBasePosition = _stageCamera.transform.localPosition;
            _currentZoom = _cameraOrbitBasePosition.z;
            Debug.Log($"[HeroStage] FitCameraToFullStage: combined.h={combinedBounds.size.y:F2}, combined.w={combinedBounds.size.x:F2}, FOV={_stageCamera.fieldOfView}, dist={distance:F2}, camPos={camPosition}, lookAt={lookTarget}");
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
            hero.transform.SetParent(_heroPivot.transform);
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
            SetLayer(monster, _renderLayer);
            PositionMonsterOnStage(monster);

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
            _monsterModelRenderers = null;
            _monsterOriginalMaterials = null;
            _isMonsterHovered = false;

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

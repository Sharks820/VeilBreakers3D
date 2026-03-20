using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the 3D hero preview: camera, lighting rig, RenderTexture,
    /// model instantiation, and champion monster display.
    /// </summary>
    public class HeroStageController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const int kRenderTextureWidth = 1024;
        private const int kRenderTextureHeight = 1536;
        private const int kMSAASamples = 4;
        private const int kPreviewLayer = 31; // "CharacterPreview" layer
        private const string kPreviewLayerName = "CharacterPreview";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Shader _placeholderShader;

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private RenderTexture _renderTexture;
        private GameObject _currentModel;
        private GameObject _currentChampion;
        private Light _keyLight;
        private Light _fillLight;
        private Light _rimLight;
        private Light _faceLight;
        private Light _groundLight;
        private VisualElement _renderTarget;
        private Transform _stageRoot;
        private HeroDisplayConfig _currentConfig;

        // Drag / stick rotation
        private bool _isDragging;
        private float _dragStartX;
        private float _modelRotationY;
        private const float kStickRotationSpeed = 120f; // degrees per second
        private static readonly int kEmissionColor = Shader.PropertyToID("_EmissionColor");

        // Model state cache (avoid per-frame GetComponent)
        private bool _currentModelHasAnimator;
        private Vector3 _currentModelBaseScale;
        private Coroutine _swapCoroutine;

        // =============================================================================
        // LIGHTING LERP STATE
        // =============================================================================

        private Tween _lightLerpTween;
        private Tween _rimFlickerTween;
        private Color _srcFillColor, _dstFillColor;
        private float _srcFillIntensity, _dstFillIntensity;
        private Color _srcRimColor, _dstRimColor;
        private float _srcRimIntensity, _dstRimIntensity;
        private Color _srcAmbientColor, _dstAmbientColor;
        private bool _isRimFlickering;

        // Placeholder material (instance-level, not static, to avoid cross-instance destruction)
        private Material _placeholderMaterial;
        private Material _currentPlaceholderMat;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
            CharSelectEvents.OnScreenExiting += HandleScreenExiting;

            InitializeStage();
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
            CharSelectEvents.OnScreenExiting -= HandleScreenExiting;

            CleanupStage();
        }

        private void OnDestroy()
        {
            if (_currentPlaceholderMat != null) Destroy(_currentPlaceholderMat);
            if (_placeholderMaterial != null) Destroy(_placeholderMaterial);
        }

        private void Update()
        {
            HandleDragInput();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeStage()
        {
            // Create stage root
            _stageRoot = new GameObject("CharSelectStage").transform;
            _stageRoot.position = new Vector3(100f, 0f, 0f); // Off-screen position

            // Create RenderTexture
            _renderTexture = new RenderTexture(kRenderTextureWidth, kRenderTextureHeight, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = kMSAASamples;
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.Create();

            // Setup camera (re-activate if previously disabled by CleanupStage)
            if (_previewCamera == null)
            {
                var camObj = new GameObject("PreviewCamera");
                camObj.transform.SetParent(_stageRoot);
                _previewCamera = camObj.AddComponent<Camera>();
            }
            else
            {
                _previewCamera.gameObject.SetActive(true);
                _previewCamera.transform.SetParent(_stageRoot);
            }

            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.fieldOfView = 30f;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.clear;
            _previewCamera.cullingMask = 1 << kPreviewLayer;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 50f;

            // Create lighting rig
            CreateLightingRig();

            // Bind RenderTexture to UI
            BindRenderTextureToUI();
        }

        private void CreateLightingRig()
        {
            _keyLight = CreateLight("KeyLight", LightType.Directional, Color.white, 1.2f);
            _keyLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            _fillLight = CreateLight("FillLight", LightType.Point, new Color(0.4f, 0.5f, 0.6f), 0.6f);
            _fillLight.transform.localPosition = new Vector3(-2f, 1.5f, 1f);
            _fillLight.range = 10f;

            _rimLight = CreateLight("RimLight", LightType.Point, Color.cyan, 1.5f);
            _rimLight.transform.localPosition = new Vector3(0f, 2f, 2f);
            _rimLight.range = 8f;

            _faceLight = CreateLight("FaceLight", LightType.Spot, new Color(1f, 0.95f, 0.9f), 0.4f);
            _faceLight.transform.localPosition = new Vector3(0f, 2f, -1.5f);
            _faceLight.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            _faceLight.spotAngle = 45f;
            _faceLight.range = 5f;

            _groundLight = CreateLight("GroundLight", LightType.Point, new Color(0.3f, 0.3f, 0.4f), 0.3f);
            _groundLight.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            _groundLight.range = 5f;
        }

        private Light CreateLight(string name, LightType type, Color color, float intensity)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_stageRoot);
            obj.layer = kPreviewLayer;

            var light = obj.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << kPreviewLayer;

            return light;
        }

        private void BindRenderTextureToUI()
        {
            if (_uiDocument == null) return;

            _renderTarget = _uiDocument.rootVisualElement.Q<VisualElement>("hero-render-target");
            if (_renderTarget != null)
            {
                _renderTarget.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
                _renderTarget.usageHints |= UsageHints.DynamicTransform;

                // Register drag events
                _renderTarget.RegisterCallback<PointerDownEvent>(OnPointerDown);
                _renderTarget.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _renderTarget.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }

        // =============================================================================
        // HERO SWITCHING
        // =============================================================================

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            _currentConfig = config;
            if (_swapCoroutine != null) StopCoroutine(_swapCoroutine);
            _swapCoroutine = StartCoroutine(SwapHeroModel(data, config));
        }

        private IEnumerator SwapHeroModel(HeroData data, HeroDisplayConfig config)
        {
            // Destroy previous model
            if (_currentModel != null)
            {
                Destroy(_currentModel);
                _currentModel = null;
            }
            if (_currentChampion != null)
            {
                Destroy(_currentChampion);
                _currentChampion = null;
            }

            // Load new model
            GameObject prefab = config?.modelPrefab;
            if (prefab != null)
            {
                _currentModel = Instantiate(prefab, _stageRoot);
            }
            else
            {
                // Placeholder: brand-colored capsule
                _currentModel = CreatePlaceholderModel(data, config);
            }

            SetLayerRecursive(_currentModel, kPreviewLayer);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;
            _modelRotationY = 0f;

            // Cache animator state to avoid per-frame GetComponent
            _currentModelHasAnimator = _currentModel.GetComponent<Animator>() != null;
            _currentModelBaseScale = _currentModel.transform.localScale;

            // Load champion monster
            if (config?.championModelPrefab != null)
            {
                _currentChampion = Instantiate(config.championModelPrefab, _stageRoot);
                SetLayerRecursive(_currentChampion, kPreviewLayer);
                _currentChampion.transform.localPosition = config.championOffset;
                _currentChampion.transform.localScale = Vector3.one * config.championScale;
            }

            // Apply camera config
            ApplyCameraConfig(config);

            // Apply lighting config
            ApplyLightingConfig(config);

            yield return null;
        }

        private GameObject CreatePlaceholderModel(HeroData data, HeroDisplayConfig config)
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.transform.SetParent(_stageRoot);
            placeholder.transform.localPosition = new Vector3(0f, 1f, 0f);
            placeholder.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            placeholder.name = $"Placeholder_{data?.hero_id ?? "unknown"}";

            // Destroy collider (not needed for display)
            var collider = placeholder.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Apply brand-colored emissive material
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (_placeholderMaterial == null)
                {
                    // Use serialized shader reference (survives build stripping)
                    var shader = _placeholderShader;
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    if (shader == null)
                    {
                        Debug.LogWarning("[HeroStageController] No suitable shader found for placeholder.");
                        return placeholder;
                    }
                    _placeholderMaterial = new Material(shader);
                }

                if (_currentPlaceholderMat != null) Destroy(_currentPlaceholderMat);
                _currentPlaceholderMat = new Material(_placeholderMaterial);
                Color brandColor = config?.primaryColor ?? (data?.color_palette?.ToColor() ?? Color.gray);
                _currentPlaceholderMat.color = brandColor;
                _currentPlaceholderMat.SetColor(kEmissionColor, brandColor * 0.3f);
                _currentPlaceholderMat.EnableKeyword("_EMISSION");
                renderer.material = _currentPlaceholderMat;
            }

            return placeholder;
        }

        // =============================================================================
        // CAMERA & LIGHTING
        // =============================================================================

        private void ApplyCameraConfig(HeroDisplayConfig config)
        {
            if (_previewCamera == null) return;

            Vector3 offset = config?.cameraOffset ?? new Vector3(0f, 1.4f, -2.8f);
            float fov = config?.cameraFOV ?? 30f;

            _previewCamera.transform.localPosition = offset;
            _previewCamera.transform.LookAt(_stageRoot.position + Vector3.up * 1.1f);
            _previewCamera.fieldOfView = fov;
        }

        private void ApplyLightingConfig(HeroDisplayConfig config)
        {
            if (config == null) return;

            // Key light
            if (_keyLight != null)
            {
                _keyLight.color = config.keyLightColor;
                _keyLight.intensity = config.keyLightIntensity;
            }

            // Fill light
            if (_fillLight != null)
            {
                _fillLight.color = config.fillLightColor;
                _fillLight.intensity = config.fillLightIntensity;
            }

            // Rim light (brand accent)
            if (_rimLight != null)
            {
                _rimLight.color = config.rimLightColor;
                _rimLight.intensity = config.rimLightIntensity;
            }

            // Ground light (brand secondary)
            if (_groundLight != null)
            {
                _groundLight.color = config.secondaryColor;
            }
        }

        // =============================================================================
        // PER-HERO LIGHTING LERP (HeroThemeConfig-driven)
        // =============================================================================

        /// <summary>
        /// Lerps stage lighting from current values to the hero theme's lighting config.
        /// Starts/stops Nyx rim flicker based on theme.rimFlicker.
        /// </summary>
        public void TransitionLighting(HeroThemeConfig theme, float duration = 0.6f)
        {
            _lightLerpTween.Stop();

            // Cache source from current light values
            if (_fillLight != null)
            {
                _srcFillColor = _fillLight.color;
                _srcFillIntensity = _fillLight.intensity;
            }
            if (_rimLight != null)
            {
                _srcRimColor = _rimLight.color;
                _srcRimIntensity = _rimLight.intensity;
            }
            _srcAmbientColor = RenderSettings.ambientLight;

            // Cache destination from theme
            _dstFillColor = theme.fillLightColor;
            _dstFillIntensity = theme.fillLightIntensity;
            _dstRimColor = theme.rimLightColor;
            _dstRimIntensity = theme.rimLightIntensity;
            _dstAmbientColor = theme.ambientColor;

            _lightLerpTween = Tween.Custom(this, 0f, 1f, duration,
                onValueChange: (ctrl, t) => ctrl.ApplyLightLerp(t), ease: Ease.InOutSine);

            // Rim flicker for Nyx (unstable reality)
            if (theme.rimFlicker && !_isRimFlickering)
            {
                StartRimFlicker();
            }
            else if (!theme.rimFlicker && _isRimFlickering)
            {
                StopRimFlicker();
            }
        }

        private void ApplyLightLerp(float t)
        {
            if (_fillLight != null)
            {
                _fillLight.color = Color.Lerp(_srcFillColor, _dstFillColor, t);
                _fillLight.intensity = Mathf.Lerp(_srcFillIntensity, _dstFillIntensity, t);
            }
            if (_rimLight != null)
            {
                _rimLight.color = Color.Lerp(_srcRimColor, _dstRimColor, t);
                // Only lerp rim intensity if not flickering (flicker overrides intensity)
                if (!_isRimFlickering)
                {
                    _rimLight.intensity = Mathf.Lerp(_srcRimIntensity, _dstRimIntensity, t);
                }
            }

            // Ambient light tint per hero
            RenderSettings.ambientLight = Color.Lerp(_srcAmbientColor, _dstAmbientColor, t);
        }

        /// <summary>
        /// Starts Nyx-style rim light flicker: random intensity variation between 0.8x and 1.5x
        /// of target intensity every 0.1-0.3s, creating unstable electrical energy feel.
        /// </summary>
        private void StartRimFlicker()
        {
            _isRimFlickering = true;

            // Infinite loop flicker using short-duration tweens
            void DoFlickerCycle()
            {
                if (!_isRimFlickering || _rimLight == null) return;

                float baseIntensity = _dstRimIntensity;
                float targetFlicker = baseIntensity * UnityEngine.Random.Range(0.8f, 1.5f);
                float flickerDuration = UnityEngine.Random.Range(0.1f, 0.3f);

                _rimFlickerTween = Tween.Custom(this, _rimLight.intensity, targetFlicker, flickerDuration,
                    onValueChange: (ctrl, val) =>
                    {
                        if (ctrl._rimLight != null) ctrl._rimLight.intensity = val;
                    }, ease: Ease.Linear)
                    .OnComplete(() => DoFlickerCycle());
            }

            DoFlickerCycle();
        }

        /// <summary>
        /// Stops the rim light flicker and lerps back to stable target intensity.
        /// </summary>
        private void StopRimFlicker()
        {
            _isRimFlickering = false;
            _rimFlickerTween.Stop();

            if (_rimLight != null)
            {
                Tween.Custom(this, _rimLight.intensity, _dstRimIntensity, 0.3f,
                    onValueChange: (ctrl, val) =>
                    {
                        if (ctrl._rimLight != null) ctrl._rimLight.intensity = val;
                    });
            }
        }

        // =============================================================================
        // DRAG ROTATION
        // =============================================================================

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _dragStartX = evt.position.x;
            _renderTarget?.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _currentModel == null) return;

            float deltaX = evt.position.x - _dragStartX;
            _dragStartX = evt.position.x;
            _modelRotationY += deltaX * 0.5f;
            _currentModel.transform.localRotation = Quaternion.Euler(0f, _modelRotationY, 0f);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _isDragging = false;
            _renderTarget?.ReleasePointer(evt.pointerId);
        }

        private void HandleDragInput()
        {
            if (_currentModel == null) return;

            // Right-stick rotation via New Input System (no legacy Input.GetAxis)
            float stickX = 0f;
            if (InputManager.HasInstance)
            {
                var lookValue = InputManager.Instance.MousePosition; // Right stick mapped to look
                // If gamepad is active, use the stick value for rotation
                if (InputManager.Instance.IsGamepad)
                {
                    stickX = lookValue.x > Screen.width * 0.6f ? 1f :
                             lookValue.x < Screen.width * 0.4f ? -1f : 0f;
                }
            }
            if (Mathf.Abs(stickX) > 0.15f)
            {
                _modelRotationY += stickX * kStickRotationSpeed * Time.deltaTime;
                _currentModel.transform.localRotation = Quaternion.Euler(0f, _modelRotationY, 0f);
                return; // Stick takes priority, skip idle rotation
            }

            // Procedural idle for placeholder models (no Animator)
            if (!_currentModelHasAnimator)
            {
                float breath = 1f + Mathf.Sin(Time.time * 1.2f) * 0.005f;
                _currentModel.transform.localScale = _currentModelBaseScale * breath;

                if (!_isDragging)
                {
                    _modelRotationY += 5f * Time.deltaTime;
                    _currentModel.transform.localRotation = Quaternion.Euler(0f, _modelRotationY, 0f);
                }
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void HandleScreenExiting()
        {
            // Nothing to do -- cleanup happens in OnDisable
        }

        private void CleanupStage()
        {
            StopAllCoroutines();
            _swapCoroutine = null;

            if (_renderTarget != null)
            {
                _renderTarget.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _renderTarget.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _renderTarget.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                // Clear UI reference and force repaint BEFORE releasing texture to prevent race condition
                _renderTarget.style.backgroundImage = new StyleBackground(StyleKeyword.None);
                _renderTarget.MarkDirtyRepaint();
            }

            if (_currentPlaceholderMat != null) Destroy(_currentPlaceholderMat);
            if (_currentModel != null) Destroy(_currentModel);
            if (_currentChampion != null) Destroy(_currentChampion);

            // Detach camera from stageRoot before destroy so serialized camera survives
            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = null;
                _previewCamera.transform.SetParent(null);
                _previewCamera.gameObject.SetActive(false);
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_stageRoot != null) Destroy(_stageRoot.gameObject);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}

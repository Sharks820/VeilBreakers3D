using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Creates pulsing molten vein effects using sprite-based overlays.
    /// Veins pulse in waves from a central point (the girl's position) outward.
    /// </summary>
    public class MoltenVeinVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Vein Sprite")]
        [SerializeField] private Texture2D _veinSprite;

        [Header("Vein Layout")]
        [SerializeField] private int _veinCount = 8;
        [SerializeField] private float _veinWidth = 120f;
        [SerializeField] private float _veinHeight = 400f;
        [SerializeField] private float _veinSpacing = 180f;

        [Header("Pulse Settings")]
        [SerializeField] private float _pulseSpeed = 1.2f;
        [SerializeField] private float _pulseWaveSpeed = 0.3f;
        [SerializeField] private float _baseOpacity = 0.15f;
        [SerializeField] private float _pulseOpacity = 0.7f;
        [SerializeField] private float _glowIntensity = 1.5f;

        [Header("Colors")]
        [SerializeField] private Color _veinColorDim = new Color(0.4f, 0.15f, 0.05f, 0.3f);
        [SerializeField] private Color _veinColorBright = new Color(1f, 0.5f, 0.1f, 0.9f);
        [SerializeField] private Color _veinColorWhiteHot = new Color(1f, 0.9f, 0.7f, 1f);

        [Header("Position")]
        [SerializeField] private float _originY = 0.4f; // Normalized Y position (0.4 = 40% from top, where girl is)
        [SerializeField] private float _startX = 0.2f;  // Start veins at 20% from left
        [SerializeField] private float _endX = 0.8f;    // End veins at 80% from left

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _vfxContainer;
        private readonly List<VeinElement> _veins = new();
        private bool _isActive;
        private Coroutine _updateCoroutine;
        private float _pulseTime;
        private float _screenWidth;
        private float _screenHeight;

        // External pulse trigger (for heartbeat sync)
        private float _externalPulseIntensity;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_isActive && _vfxContainer == null)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (_vfxContainer != null)
            {
                StartVFX();
            }
        }

        private void OnDisable()
        {
            StopVFX();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;

            _screenWidth = root.resolvedStyle.width;
            _screenHeight = root.resolvedStyle.height;

            if (_screenWidth <= 0) _screenWidth = 1920;
            if (_screenHeight <= 0) _screenHeight = 1080;

            // Create VFX container
            _vfxContainer = new VisualElement();
            _vfxContainer.name = "molten-vein-container";
            _vfxContainer.style.position = Position.Absolute;
            _vfxContainer.style.left = 0;
            _vfxContainer.style.top = 0;
            _vfxContainer.style.right = 0;
            _vfxContainer.style.bottom = 0;
            _vfxContainer.style.overflow = Overflow.Hidden;
            _vfxContainer.pickingMode = PickingMode.Ignore;

            // Insert behind other elements but in front of background
            root.Insert(1, _vfxContainer);

            // Create veins
            CreateVeins();

            StartVFX();
            Debug.Log($"[MoltenVeinVFX] Initialized with {_veins.Count} veins");
        }

        private void CreateVeins()
        {
            float totalWidth = _screenWidth * (_endX - _startX);
            float startXPos = _screenWidth * _startX;

            for (int i = 0; i < _veinCount; i++)
            {
                // Main vein element
                var vein = new VisualElement();
                vein.name = $"vein-{i}";
                vein.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
                vein.style.position = Position.Absolute;
                vein.style.width = _veinWidth;
                vein.style.height = _veinHeight;
                vein.pickingMode = PickingMode.Ignore;

                // Position across screen
                float spacing = _veinCount > 1 ? totalWidth / (_veinCount - 1) : 0f;
                float xPos = startXPos + spacing * i - _veinWidth / 2f;
                float yPos = _screenHeight * _originY;

                vein.style.left = xPos;
                vein.style.top = yPos;

                // Apply vein sprite if available
                if (_veinSprite != null)
                {
                    vein.style.backgroundImage = new StyleBackground(_veinSprite);
                    vein.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }
                else
                {
                    // Fallback gradient look
                    vein.style.backgroundColor = _veinColorDim;
                    vein.style.borderTopLeftRadius = _veinWidth / 2f;
                    vein.style.borderTopRightRadius = _veinWidth / 2f;
                }

                vein.style.opacity = _baseOpacity;

                // Glow overlay
                var glow = new VisualElement();
                glow.name = $"vein-glow-{i}";
                glow.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
                glow.style.position = Position.Absolute;
                glow.style.left = -20;
                glow.style.top = -20;
                glow.style.right = -20;
                glow.style.bottom = -20;
                glow.style.backgroundColor = new Color(_veinColorBright.r, _veinColorBright.g, _veinColorBright.b, 0.2f);
                glow.style.borderTopLeftRadius = _veinWidth;
                glow.style.borderTopRightRadius = _veinWidth;
                glow.style.borderBottomLeftRadius = _veinWidth;
                glow.style.borderBottomRightRadius = _veinWidth;
                glow.style.opacity = 0;
                glow.pickingMode = PickingMode.Ignore;
                vein.Add(glow);

                _vfxContainer.Add(vein);

                // Calculate distance from center for wave timing
                float centerX = _screenWidth * 0.5f;
                float distanceFromCenter = Mathf.Abs(xPos + _veinWidth / 2f - centerX);
                float maxDistance = _screenWidth * 0.4f;
                float normalizedDistance = distanceFromCenter / maxDistance;

                var veinData = new VeinElement
                {
                    Element = vein,
                    GlowElement = glow,
                    BasePosition = new Vector2(xPos, yPos),
                    PhaseOffset = normalizedDistance * Mathf.PI, // Outer veins pulse later
                    Intensity = 0f,
                    VerticalOffset = Random.Range(-30f, 30f),
                    ScaleVariation = Random.Range(0.8f, 1.2f)
                };

                // Apply variations
                vein.style.top = yPos + veinData.VerticalOffset;
                vein.style.scale = new Scale(new Vector2(veinData.ScaleVariation, veinData.ScaleVariation));
                vein.style.rotate = new Rotate(Random.Range(-8f, 8f));

                _veins.Add(veinData);
            }
        }

        // =============================================================================
        // VFX CONTROL
        // =============================================================================

        public void StartVFX()
        {
            if (_isActive) return;
            _isActive = true;
            _updateCoroutine = StartCoroutine(UpdateVeins());
        }

        public void StopVFX()
        {
            _isActive = false;
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }

        private IEnumerator UpdateVeins()
        {
            while (_isActive)
            {
                float deltaTime = Time.unscaledDeltaTime;
                _pulseTime += deltaTime * _pulseSpeed;

                foreach (var vein in _veins)
                {
                    UpdateVein(vein, deltaTime);
                }

                // Decay external pulse
                _externalPulseIntensity = Mathf.Max(0, _externalPulseIntensity - deltaTime * 2f);

                yield return null;
            }
        }

        private void UpdateVein(VeinElement vein, float deltaTime)
        {
            // Calculate pulse wave (originates from center, spreads outward)
            float wave = Mathf.Sin(_pulseTime * Mathf.PI * 2f - vein.PhaseOffset * _pulseWaveSpeed);
            float normalizedWave = (wave + 1f) / 2f; // 0 to 1

            // Add external pulse (from heartbeat trigger)
            float totalIntensity = Mathf.Max(normalizedWave, _externalPulseIntensity);

            // Smooth intensity changes
            vein.Intensity = Mathf.Lerp(vein.Intensity, totalIntensity, deltaTime * 8f);

            // Dirty threshold: skip style writes when intensity change is negligible
            if (Mathf.Abs(vein.Intensity - vein.PreviousIntensity) < 0.005f)
            {
                return;
            }
            vein.PreviousIntensity = vein.Intensity;

            // Calculate opacity
            float opacity = Mathf.Lerp(_baseOpacity, _pulseOpacity, vein.Intensity);
            vein.Element.style.opacity = opacity;

            // Glow follows intensity
            float glowOpacity = vein.Intensity * 0.4f * _glowIntensity;
            vein.GlowElement.style.opacity = glowOpacity;

            // Color shift based on intensity
            Color currentColor = Color.Lerp(_veinColorDim, _veinColorBright, vein.Intensity);
            if (vein.Intensity > 0.8f)
            {
                // White-hot at peak
                float whiteHotBlend = (vein.Intensity - 0.8f) / 0.2f;
                currentColor = Color.Lerp(currentColor, _veinColorWhiteHot, whiteHotBlend);
            }

            // Apply tint via USS class or direct style
            vein.Element.style.unityBackgroundImageTintColor = currentColor;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Trigger a pulse wave (call this on heartbeat)
        /// </summary>
        public void TriggerPulse(float intensity = 1f)
        {
            _externalPulseIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Set the base pulse speed
        /// </summary>
        public void SetPulseSpeed(float speed)
        {
            _pulseSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Intensify all veins (for danger/alert states)
        /// </summary>
        public void SetIntensityMultiplier(float multiplier)
        {
            _glowIntensity = Mathf.Clamp(multiplier, 0.5f, 3f);
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        private class VeinElement
        {
            public VisualElement Element;
            public VisualElement GlowElement;
            public Vector2 BasePosition;
            public float PhaseOffset;
            public float Intensity;
            public float PreviousIntensity = -1f; // Force first-frame update
            public float VerticalOffset;
            public float ScaleVariation;
        }
    }
}

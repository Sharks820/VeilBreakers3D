using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// Pure UI Toolkit VFX controller for the main menu.
    /// Uses VisualElements with animated styles - no sprites or cameras needed.
    ///
    /// Effects:
    /// 1. Logo Glow - Pulsing crimson glow behind the title
    /// 2. Vignette - Screen edge darkening with pulse
    /// 3. Embers - Small particle-like elements rising
    /// 4. Rift Effect - Animated distortion behind logo
    /// </summary>
    public class MainMenuVFXController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Pulse Settings")]
        [SerializeField] private float _pulseFrequency = 0.15f; // Slow, ominous
        [SerializeField] private float _pulseMinValue = 0.3f;
        [SerializeField] private float _pulseMaxValue = 1.0f;

        [Header("Logo Glow")]
        [SerializeField] private float _logoGlowIntensity = 0.4f;
        [SerializeField] private Color _logoGlowColor = new Color(0.7f, 0.15f, 0.2f, 0.5f);

        [Header("Vignette")]
        [SerializeField] private float _vignetteBaseAlpha = 0.3f;
        [SerializeField] private float _vignettePulseAmount = 0.15f;

        [Header("Embers")]
        [SerializeField] private int _emberCount = 12;
        [SerializeField] private float _emberRiseSpeed = 30f; // pixels per second

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _logoGlowElement;
        private VisualElement _vignetteElement;
        private VisualElement _riftElement;
        private VisualElement _embersContainer;
        private VisualElement[] _embers;

        private float _pulsePhase;
        private float _currentPulse;
        private bool _isInitialized;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            StartCoroutine(InitializeAfterFrame());
        }

        private IEnumerator InitializeAfterFrame()
        {
            // Wait for UI to be ready
            yield return null;

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                Debug.LogWarning("[VB:VFX] No UIDocument found, VFX disabled");
                yield break;
            }

            _root = _uiDocument.rootVisualElement.Q<VisualElement>("menu-root");
            if (_root == null)
            {
                Debug.LogWarning("[VB:VFX] menu-root not found in UXML");
                yield break;
            }

            CreateVFXElements();
            _isInitialized = true;
            Debug.Log("[VB:VFX] Main Menu VFX initialized (UI Toolkit mode)");
        }

        private void Update()
        {
            if (!_isInitialized) return;

            UpdatePulse();
            UpdateLogoGlow();
            UpdateVignette();
            UpdateEmbers();
        }

        // =============================================================================
        // VFX ELEMENT CREATION
        // =============================================================================

        private void CreateVFXElements()
        {
            // Create VFX container at the back (insert at index 1, after background)
            var vfxContainer = new VisualElement();
            vfxContainer.name = "vfx-container";
            vfxContainer.pickingMode = PickingMode.Ignore;
            vfxContainer.style.position = Position.Absolute;
            vfxContainer.style.left = 0;
            vfxContainer.style.top = 0;
            vfxContainer.style.right = 0;
            vfxContainer.style.bottom = 0;

            // Insert after background element
            var background = _root.Q<VisualElement>("background");
            if (background != null)
            {
                int bgIndex = _root.IndexOf(background);
                _root.Insert(bgIndex + 1, vfxContainer);
            }
            else
            {
                _root.Insert(0, vfxContainer);
            }

            CreateLogoGlow(vfxContainer);
            CreateRiftEffect(vfxContainer);
            CreateEmbers(vfxContainer);
            CreateVignette(); // Vignette goes on top, in root
        }

        private void CreateLogoGlow(VisualElement container)
        {
            // Subtle glow behind logo - very faint, not distracting
            _logoGlowElement = new VisualElement();
            _logoGlowElement.name = "logo-glow-vfx";
            _logoGlowElement.pickingMode = PickingMode.Ignore;
            _logoGlowElement.style.position = Position.Absolute;
            _logoGlowElement.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _logoGlowElement.style.top = 60;
            _logoGlowElement.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _logoGlowElement.style.width = 800;
            _logoGlowElement.style.height = 120;

            // Very subtle glow - barely visible
            _logoGlowElement.style.borderTopLeftRadius = new Length(50, LengthUnit.Percent);
            _logoGlowElement.style.borderTopRightRadius = new Length(50, LengthUnit.Percent);
            _logoGlowElement.style.borderBottomLeftRadius = new Length(50, LengthUnit.Percent);
            _logoGlowElement.style.borderBottomRightRadius = new Length(50, LengthUnit.Percent);
            _logoGlowElement.style.backgroundColor = new Color(_logoGlowColor.r, _logoGlowColor.g, _logoGlowColor.b, 0.03f);

            container.Add(_logoGlowElement);
        }

        private void CreateRiftEffect(VisualElement container)
        {
            // Rift effect disabled - was too visible
            _riftElement = null;
        }

        private void CreateEmbers(VisualElement container)
        {
            _embersContainer = new VisualElement();
            _embersContainer.name = "embers-container";
            _embersContainer.pickingMode = PickingMode.Ignore;
            _embersContainer.style.position = Position.Absolute;
            _embersContainer.style.left = 0;
            _embersContainer.style.top = 0;
            _embersContainer.style.right = 0;
            _embersContainer.style.bottom = 0;
            _embersContainer.style.overflow = Overflow.Hidden;
            container.Add(_embersContainer);

            _embers = new VisualElement[_emberCount];
            for (int i = 0; i < _emberCount; i++)
            {
                var ember = new VisualElement();
                ember.name = $"ember-{i}";
                ember.pickingMode = PickingMode.Ignore;
                ember.style.position = Position.Absolute;

                // Random size
                float size = Random.Range(2f, 5f);
                ember.style.width = size;
                ember.style.height = size;
                ember.style.borderTopLeftRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderTopRightRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderBottomLeftRadius = new Length(50, LengthUnit.Percent);
                ember.style.borderBottomRightRadius = new Length(50, LengthUnit.Percent);

                // Random warm color (ash/ember tones)
                float hue = Random.Range(0.02f, 0.08f); // Orange-red
                float sat = Random.Range(0.3f, 0.6f);
                float val = Random.Range(0.2f, 0.4f);
                ember.style.backgroundColor = Color.HSVToRGB(hue, sat, val);
                ember.style.opacity = Random.Range(0.3f, 0.7f);

                // Random starting position
                ember.style.left = new StyleLength(new Length(Random.Range(20f, 80f), LengthUnit.Percent));
                ember.style.top = new StyleLength(new Length(Random.Range(80f, 120f), LengthUnit.Percent));

                // Store initial speed in userData
                ember.userData = new EmberData
                {
                    speed = Random.Range(0.5f, 1.5f),
                    drift = Random.Range(-0.3f, 0.3f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    startY = Random.Range(80f, 120f)
                };

                _embersContainer.Add(ember);
                _embers[i] = ember;
            }
        }

        private void CreateVignette()
        {
            _vignetteElement = new VisualElement();
            _vignetteElement.name = "vignette-vfx";
            _vignetteElement.pickingMode = PickingMode.Ignore;
            _vignetteElement.style.position = Position.Absolute;
            _vignetteElement.style.left = 0;
            _vignetteElement.style.top = 0;
            _vignetteElement.style.right = 0;
            _vignetteElement.style.bottom = 0;

            // Create vignette effect using border with large inset shadow
            _vignetteElement.style.borderLeftWidth = 100;
            _vignetteElement.style.borderRightWidth = 100;
            _vignetteElement.style.borderTopWidth = 80;
            _vignetteElement.style.borderBottomWidth = 80;
            _vignetteElement.style.borderLeftColor = new Color(0.03f, 0.02f, 0.05f, _vignetteBaseAlpha);
            _vignetteElement.style.borderRightColor = new Color(0.03f, 0.02f, 0.05f, _vignetteBaseAlpha);
            _vignetteElement.style.borderTopColor = new Color(0.03f, 0.02f, 0.05f, _vignetteBaseAlpha);
            _vignetteElement.style.borderBottomColor = new Color(0.03f, 0.02f, 0.05f, _vignetteBaseAlpha);

            // Insert before loading overlay
            var loadingOverlay = _root.Q<VisualElement>("loading-overlay");
            if (loadingOverlay != null)
            {
                _root.Insert(_root.IndexOf(loadingOverlay), _vignetteElement);
            }
            else
            {
                _root.Add(_vignetteElement);
            }
        }

        // =============================================================================
        // UPDATE METHODS
        // =============================================================================

        private void UpdatePulse()
        {
            _pulsePhase += Time.deltaTime * _pulseFrequency * Mathf.PI * 2f;

            // Sine wave pulse with slight asymmetry (faster rise, slower fall)
            float rawPulse = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
            rawPulse = Mathf.Pow(rawPulse, 0.8f); // Slight asymmetry

            _currentPulse = Mathf.Lerp(_pulseMinValue, _pulseMaxValue, rawPulse);
        }

        private void UpdateLogoGlow()
        {
            if (_logoGlowElement == null) return;

            // Pulse the glow alpha and scale
            float glowAlpha = _logoGlowIntensity * _currentPulse * 0.25f;
            _logoGlowElement.style.backgroundColor = new Color(
                _logoGlowColor.r,
                _logoGlowColor.g,
                _logoGlowColor.b,
                glowAlpha
            );

            // Subtle scale pulse
            float scale = 1f + (_currentPulse - 0.5f) * 0.05f;
            _logoGlowElement.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private void UpdateVignette()
        {
            if (_vignetteElement == null) return;

            // Pulse vignette darkness
            float alpha = _vignetteBaseAlpha + (_currentPulse - 0.5f) * _vignettePulseAmount;
            var vignetteColor = new Color(0.03f, 0.02f, 0.05f, alpha);
            _vignetteElement.style.borderLeftColor = vignetteColor;
            _vignetteElement.style.borderRightColor = vignetteColor;
            _vignetteElement.style.borderTopColor = vignetteColor;
            _vignetteElement.style.borderBottomColor = vignetteColor;
        }

        private void UpdateEmbers()
        {
            if (_embers == null) return;

            for (int i = 0; i < _embers.Length; i++)
            {
                var ember = _embers[i];
                if (ember?.userData is not EmberData data) continue;

                // Get current position
                float currentTop = ember.style.top.value.value;
                float currentLeft = ember.style.left.value.value;

                // Rise up
                currentTop -= _emberRiseSpeed * data.speed * Time.deltaTime * 0.1f;

                // Horizontal drift with sine wave
                float drift = Mathf.Sin(Time.time * 2f + data.phase) * data.drift * 0.5f;
                currentLeft += drift * Time.deltaTime;

                // Fade as rising
                float normalizedY = 1f - (currentTop / 100f);
                float opacity = Mathf.Lerp(0.6f, 0f, Mathf.Clamp01(normalizedY));
                ember.style.opacity = opacity;

                // Reset when off screen
                if (currentTop < -10f)
                {
                    currentTop = data.startY;
                    currentLeft = Random.Range(20f, 80f);
                    data.phase = Random.Range(0f, Mathf.PI * 2f);
                }

                ember.style.top = new StyleLength(new Length(currentTop, LengthUnit.Percent));
                ember.style.left = new StyleLength(new Length(currentLeft, LengthUnit.Percent));
            }
        }

        // =============================================================================
        // DATA CLASSES
        // =============================================================================

        private class EmberData
        {
            public float speed;
            public float drift;
            public float phase;
            public float startY;
        }
    }
}

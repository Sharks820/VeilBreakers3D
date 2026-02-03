using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA molten/lava button effects.
    /// Creates cracks that glow on hover, eruption effects on click,
    /// and lava sweep effects across the button bar.
    /// </summary>
    public class MoltenButtonVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Crack Settings")]
        [SerializeField] private int _cracksPerButton = 5;
        [SerializeField] private float _crackWidth = 2f;
        [SerializeField] private float _crackLengthMin = 20f;
        [SerializeField] private float _crackLengthMax = 60f;

        [Header("Colors")]
        [SerializeField] private Color _crackColorDim = new Color(0.3f, 0.1f, 0.0f, 0.3f);
        [SerializeField] private Color _crackColorGlow = new Color(1f, 0.5f, 0.1f, 0.9f);
        [SerializeField] private Color _crackColorWhiteHot = new Color(1f, 0.9f, 0.7f, 1f);
        [SerializeField] private Color _lavaColor = new Color(1f, 0.4f, 0.05f, 0.8f);
        [SerializeField] private Color _eruptionColor = new Color(1f, 0.6f, 0.2f, 1f);

        [Header("Animation")]
        [SerializeField] private float _hoverGlowSpeed = 4f;
        [SerializeField] private float _clickFlashDuration = 0.15f;
        [SerializeField] private float _lavaSweepDuration = 0.8f;
        [SerializeField] private float _particleLifetime = 0.6f;

        [Header("Button Classes")]
        [SerializeField] private string _primaryButtonClass = "vb-menu-btn-primary";
        [SerializeField] private string _secondaryButtonClass = "vb-menu-btn-secondary";

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _root;
        private VisualElement _lavaSweepElement;
        private readonly Dictionary<Button, ButtonVFXState> _buttonStates = new();
        private bool _isActive;

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
            if (!_isActive)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            CleanupCallbacks();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            _isActive = true;

            // Find all buttons
            var primaryButtons = _root.Query<Button>(className: _primaryButtonClass).ToList();
            var secondaryButtons = _root.Query<Button>(className: _secondaryButtonClass).ToList();

            foreach (var button in primaryButtons)
            {
                SetupButton(button, true);
            }

            foreach (var button in secondaryButtons)
            {
                SetupButton(button, false);
            }

            // Create lava sweep element
            CreateLavaSweepElement();

            Debug.Log($"[MoltenButtonVFX] Initialized {_buttonStates.Count} buttons");
        }

        private void SetupButton(Button button, bool isPrimary)
        {
            var state = new ButtonVFXState
            {
                Button = button,
                IsPrimary = isPrimary,
                CrackElements = new List<VisualElement>(),
                GlowIntensity = 0f
            };

            // Make button position relative for absolute children
            button.style.overflow = Overflow.Hidden;

            // Create crack overlay container
            var crackContainer = new VisualElement();
            crackContainer.name = "crack-container";
            crackContainer.style.position = Position.Absolute;
            crackContainer.style.left = 0;
            crackContainer.style.top = 0;
            crackContainer.style.right = 0;
            crackContainer.style.bottom = 0;
            crackContainer.pickingMode = PickingMode.Ignore;
            button.Add(crackContainer);
            state.CrackContainer = crackContainer;

            // Create cracks
            for (int i = 0; i < _cracksPerButton; i++)
            {
                var crack = CreateCrack(button);
                crackContainer.Add(crack);
                state.CrackElements.Add(crack);
            }

            // Create glow underlay
            var glowUnderlay = new VisualElement();
            glowUnderlay.name = "glow-underlay";
            glowUnderlay.style.position = Position.Absolute;
            glowUnderlay.style.left = -10;
            glowUnderlay.style.top = -10;
            glowUnderlay.style.right = -10;
            glowUnderlay.style.bottom = -10;
            glowUnderlay.style.backgroundColor = new Color(_lavaColor.r, _lavaColor.g, _lavaColor.b, 0.1f);
            glowUnderlay.style.borderTopLeftRadius = 10;
            glowUnderlay.style.borderTopRightRadius = 10;
            glowUnderlay.style.borderBottomLeftRadius = 10;
            glowUnderlay.style.borderBottomRightRadius = 10;
            glowUnderlay.style.opacity = 0;
            glowUnderlay.pickingMode = PickingMode.Ignore;
            button.Insert(0, glowUnderlay);
            state.GlowUnderlay = glowUnderlay;

            // Register events
            button.RegisterCallback<MouseEnterEvent>(evt => OnButtonHoverEnter(state));
            button.RegisterCallback<MouseLeaveEvent>(evt => OnButtonHoverExit(state));
            button.RegisterCallback<ClickEvent>(evt => OnButtonClick(state, evt));

            _buttonStates[button] = state;

            // Primary buttons get ambient glow
            if (isPrimary)
            {
                StartCoroutine(AmbientPulse(state));
            }
        }

        private VisualElement CreateCrack(Button button)
        {
            var crack = new VisualElement();
            crack.style.position = Position.Absolute;
            crack.style.backgroundColor = _crackColorDim;
            crack.pickingMode = PickingMode.Ignore;

            // Random crack properties
            float length = Random.Range(_crackLengthMin, _crackLengthMax);
            float angle = Random.Range(-45f, 45f);

            crack.style.width = length;
            crack.style.height = _crackWidth;
            crack.style.borderTopLeftRadius = _crackWidth / 2f;
            crack.style.borderTopRightRadius = _crackWidth / 2f;
            crack.style.borderBottomLeftRadius = _crackWidth / 2f;
            crack.style.borderBottomRightRadius = _crackWidth / 2f;

            // Random position within button
            crack.style.left = Random.Range(10f, 80f);
            crack.style.top = Random.Range(10f, 35f);
            crack.style.rotate = new Rotate(angle);
            crack.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));

            crack.style.opacity = 0.3f;

            return crack;
        }

        private void CreateLavaSweepElement()
        {
            _lavaSweepElement = new VisualElement();
            _lavaSweepElement.name = "lava-sweep";
            _lavaSweepElement.style.position = Position.Absolute;
            _lavaSweepElement.style.left = 0;
            _lavaSweepElement.style.right = 0;
            _lavaSweepElement.style.bottom = 0;
            _lavaSweepElement.style.height = 80;
            _lavaSweepElement.style.backgroundColor = _lavaColor;
            _lavaSweepElement.style.opacity = 0;
            _lavaSweepElement.pickingMode = PickingMode.Ignore;

            // Find button container and insert sweep behind it
            var buttonContainer = _root.Q<VisualElement>("button-container");
            if (buttonContainer != null)
            {
                var parent = buttonContainer.parent;
                int index = parent.IndexOf(buttonContainer);
                parent.Insert(index, _lavaSweepElement);
            }
            else
            {
                _root.Insert(0, _lavaSweepElement);
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnButtonHoverEnter(ButtonVFXState state)
        {
            StopCoroutine(nameof(AnimateGlow));
            StartCoroutine(AnimateGlow(state, 1f));
        }

        private void OnButtonHoverExit(ButtonVFXState state)
        {
            StartCoroutine(AnimateGlow(state, state.IsPrimary ? 0.2f : 0f));
        }

        private void OnButtonClick(ButtonVFXState state, ClickEvent evt)
        {
            StartCoroutine(ClickEruption(state, evt.localPosition));
            StartCoroutine(LavaSweep());
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private IEnumerator AnimateGlow(ButtonVFXState state, float targetIntensity)
        {
            float startIntensity = state.GlowIntensity;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                state.GlowIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                ApplyGlowState(state);

                yield return null;
            }

            state.GlowIntensity = targetIntensity;
            ApplyGlowState(state);
        }

        private void ApplyGlowState(ButtonVFXState state)
        {
            // Update crack colors
            foreach (var crack in state.CrackElements)
            {
                Color crackColor = Color.Lerp(_crackColorDim, _crackColorGlow, state.GlowIntensity);
                crack.style.backgroundColor = crackColor;
                crack.style.opacity = 0.3f + state.GlowIntensity * 0.7f;
            }

            // Update glow underlay
            state.GlowUnderlay.style.opacity = state.GlowIntensity * 0.4f;
        }

        private IEnumerator AmbientPulse(ButtonVFXState state)
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);

            while (_isActive && state.Button != null)
            {
                if (state.GlowIntensity < 0.3f) // Only pulse when not hovered
                {
                    float pulse = 0.1f + 0.1f * Mathf.Sin(Time.unscaledTime * 2f + phase);
                    foreach (var crack in state.CrackElements)
                    {
                        crack.style.opacity = 0.3f + pulse;
                    }
                }

                yield return null;
            }
        }

        private IEnumerator ClickEruption(ButtonVFXState state, Vector2 localPosition)
        {
            // Flash white-hot
            foreach (var crack in state.CrackElements)
            {
                crack.style.backgroundColor = _crackColorWhiteHot;
            }

            // Create eruption particles
            var particles = new List<VisualElement>();
            int particleCount = 12;

            for (int i = 0; i < particleCount; i++)
            {
                var particle = new VisualElement();
                particle.style.position = Position.Absolute;
                float size = Random.Range(4f, 10f);
                particle.style.width = size;
                particle.style.height = size;
                particle.style.borderTopLeftRadius = size / 2f;
                particle.style.borderTopRightRadius = size / 2f;
                particle.style.borderBottomLeftRadius = size / 2f;
                particle.style.borderBottomRightRadius = size / 2f;
                particle.style.backgroundColor = _eruptionColor;
                particle.style.left = localPosition.x - size / 2f;
                particle.style.top = localPosition.y - size / 2f;
                particle.pickingMode = PickingMode.Ignore;

                state.CrackContainer.Add(particle);
                particles.Add(particle);
            }

            // Animate particles outward
            float elapsed = 0f;
            var velocities = new List<Vector2>();
            foreach (var p in particles)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(100f, 300f);
                velocities.Add(new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed));
            }

            while (elapsed < _particleLifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _particleLifetime;

                for (int i = 0; i < particles.Count; i++)
                {
                    var p = particles[i];
                    var v = velocities[i];

                    float x = localPosition.x + v.x * t;
                    float y = localPosition.y + v.y * t + 50f * t * t; // Gravity

                    p.style.left = x - p.resolvedStyle.width / 2f;
                    p.style.top = y - p.resolvedStyle.height / 2f;
                    p.style.opacity = 1f - t;
                    p.style.scale = new Scale(Vector2.one * (1f - t * 0.5f));
                }

                yield return null;
            }

            // Cleanup particles
            foreach (var p in particles)
            {
                p.RemoveFromHierarchy();
            }

            // Return cracks to glow state
            yield return new WaitForSecondsRealtime(_clickFlashDuration);

            foreach (var crack in state.CrackElements)
            {
                crack.style.backgroundColor = Color.Lerp(_crackColorDim, _crackColorGlow, state.GlowIntensity);
            }
        }

        private IEnumerator LavaSweep()
        {
            if (_lavaSweepElement == null) yield break;

            float elapsed = 0f;

            // Sweep in
            while (elapsed < _lavaSweepDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (_lavaSweepDuration / 2f);
                t = t * t; // Ease in

                _lavaSweepElement.style.opacity = t * 0.6f;
                yield return null;
            }

            // Sweep out
            elapsed = 0f;
            while (elapsed < _lavaSweepDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (_lavaSweepDuration / 2f);
                t = 1f - Mathf.Pow(1f - t, 2f); // Ease out

                _lavaSweepElement.style.opacity = (1f - t) * 0.6f;
                yield return null;
            }

            _lavaSweepElement.style.opacity = 0;
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void CleanupCallbacks()
        {
            _buttonStates.Clear();
            _isActive = false;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Trigger a lava sweep effect manually
        /// </summary>
        public void TriggerLavaSweep()
        {
            StartCoroutine(LavaSweep());
        }

        /// <summary>
        /// Intensify all button glows
        /// </summary>
        public void SetGlobalIntensity(float intensity)
        {
            foreach (var state in _buttonStates.Values)
            {
                state.GlowIntensity = Mathf.Clamp01(intensity);
                ApplyGlowState(state);
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        private class ButtonVFXState
        {
            public Button Button;
            public bool IsPrimary;
            public VisualElement CrackContainer;
            public VisualElement GlowUnderlay;
            public List<VisualElement> CrackElements;
            public float GlowIntensity;
        }
    }
}

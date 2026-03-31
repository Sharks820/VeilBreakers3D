using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.VFX
{
    /// <summary>
    /// Title screen logo VFX subsystem.
    /// Manages logo glow, pulse, aura, shadow, smoke burst on click,
    /// and hover boost from button interactions.
    /// </summary>
    public class TitleScreenLogoVFX : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZEFIELD CONFIG
        // =============================================================================

        [Header("Logo (Reactive)")]
        [SerializeField] internal bool _enableLogoPulse = true;
        [SerializeField] internal bool _enableLogoSmoke = true;
        [SerializeField, Range(0f, 1f)] internal float _logoGlowBaseOpacity = 0.28f;
        [SerializeField, Range(0f, 1f)] internal float _logoGlowHoverOpacity = 0.46f;
        [SerializeField, Range(0f, 1f)] internal float _logoGlowClickOpacity = 0.78f;
        [SerializeField] internal float _logoPulseDuration = 0.22f;
        [SerializeField] internal Color _logoSmokeTint = new Color(0.22f, 0.18f, 0.16f, 0.30f);

        [Header("Logo Glow Texture")]
        [Tooltip("Logo glow texture. Auto-loads Art/UI/MainMenu/logo_veilbreakers_glow when empty.")]
        [SerializeField] internal Texture2D _logoGlowTexture;

        // =============================================================================
        // STATE
        // =============================================================================

        internal VisualElement _host;
        internal VisualElement _logoContainer;
        internal VisualElement _logoImage;
        internal VisualElement _logoGlowElement;
        internal VisualElement _logoFxLayer;
        internal bool _logoHover;
        internal float _logoPulseRemaining;
        internal float _logoPulseStrength;
        internal float _logoGlowCurrentOpacity;

        // Logo aura layers (programmatic glow behind logo)
        internal VisualElement _logoAuraInner;
        internal VisualElement _logoAuraOuter;
        internal VisualElement _logoAuraPulse;
        internal float _auraTime;
        internal float _logoButtonHoverBoost;

        // Back-reference to particles for smoke burst
        private TitleScreenParticles _particles;

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize logo VFX with references from the host.
        /// </summary>
        public void Initialize(VisualElement host, VisualElement logoContainer, VisualElement logoImage,
            TitleScreenParticles particles)
        {
            _host = host;
            _logoContainer = logoContainer;
            _logoImage = logoImage;
            _particles = particles;

            if (_logoContainer != null)
            {
                RemoveLogoArtifacts();
                AddLogoShadow();
                EnsureLogoGlow();
                EnsureLogoFxLayer();
            }
        }

        /// <summary>
        /// Start the logo breathing animation.
        /// </summary>
        public void StartBreathing()
        {
            // Breathing is driven by UpdateLogo called each frame.
        }

        /// <summary>
        /// Stop the logo breathing animation.
        /// </summary>
        public void StopBreathing()
        {
            if (_logoContainer != null)
            {
                _logoContainer.style.scale = new Scale(Vector2.one);
            }
        }

        /// <summary>
        /// Notify that a menu button is being hovered.
        /// </summary>
        public void OnButtonHovered(bool isHovered, bool isPrimary)
        {
            _logoButtonHoverBoost = isHovered ? (isPrimary ? 1.0f : 0.5f) : 0f;
        }

        /// <summary>
        /// Handle logo pointer down (pulse + smoke).
        /// </summary>
        public void OnLogoPointerDown()
        {
            if (_enableLogoPulse)
            {
                TriggerLogoPulse();
            }

            if (_enableLogoSmoke && _particles != null)
            {
                _particles.SpawnLogoSmokeBurst(_logoFxLayer, _logoContainer);
            }
        }

        /// <summary>
        /// Update logo for one frame. Called by orchestrator's update coroutine.
        /// </summary>
        public void UpdateLogo(float deltaTime)
        {
            if (_logoContainer == null) return;

            float targetGlow = 0f;

            if (_logoPulseRemaining > 0f)
            {
                _logoPulseRemaining -= deltaTime;
                float t = 1f - Mathf.Clamp01(_logoPulseRemaining / Mathf.Max(0.01f, _logoPulseDuration));
                float bump = Mathf.Sin(t * Mathf.PI);

                float scale = 1f + bump * 0.05f;
                _logoContainer.style.scale = new Scale(new Vector2(scale, scale));

                if (_logoPulseRemaining <= 0f)
                {
                    _logoContainer.style.scale = new Scale(Vector2.one);
                }
            }

            if (_logoGlowElement != null)
            {
                _logoGlowCurrentOpacity = Mathf.Lerp(_logoGlowCurrentOpacity, targetGlow, deltaTime * 10f);
                _logoGlowElement.style.opacity = _logoGlowCurrentOpacity;
            }

            // Animate logo aura layers with staggered pulsing
            _auraTime += deltaTime;
            float hoverBoost = _logoButtonHoverBoost;

            if (_logoAuraInner != null)
            {
                float innerPulse = 0.12f + Mathf.Sin(_auraTime * 2.0f) * 0.04f + hoverBoost * 0.1f;
                _logoAuraInner.style.opacity = innerPulse;
            }
            if (_logoAuraOuter != null)
            {
                float outerPulse = 0.06f + Mathf.Sin(_auraTime * 1.2f) * 0.025f + hoverBoost * 0.05f;
                _logoAuraOuter.style.opacity = outerPulse;
            }
            if (_logoAuraPulse != null)
            {
                float pulsePulse = 0.08f + Mathf.Sin(_auraTime * 3.0f) * 0.05f + hoverBoost * 0.08f;
                _logoAuraPulse.style.opacity = pulsePulse;
            }

            // Decay hover boost smoothly
            if (_logoButtonHoverBoost > 0f)
            {
                _logoButtonHoverBoost = Mathf.Lerp(_logoButtonHoverBoost, 0f, deltaTime * 5f);
                if (_logoButtonHoverBoost < 0.001f) _logoButtonHoverBoost = 0f;
            }
        }

        // =============================================================================
        // INTERNAL
        // =============================================================================

        private float _lastPulseTime;
        private const float kPulseCooldown = 0.35f; // Debounce rapid clicks

        private void TriggerLogoPulse()
        {
            // Debounce: don't restart pulse if one is still finishing or cooldown hasn't elapsed
            if (_logoPulseRemaining > 0f || Time.unscaledTime - _lastPulseTime < kPulseCooldown) return;
            _lastPulseTime = Time.unscaledTime;
            _logoPulseRemaining = Mathf.Max(0.05f, _logoPulseDuration);
            _logoPulseStrength = 1f;
        }

        private void RemoveLogoArtifacts()
        {
            if (_logoContainer == null) return;

            var glow = _logoContainer.Q<VisualElement>("logo-glow");
            if (glow != null) glow.RemoveFromHierarchy();

            var backplate = _logoContainer.Q<VisualElement>("logo-backplate");
            if (backplate != null) backplate.RemoveFromHierarchy();

            var shadow = _logoContainer.Q<VisualElement>("logo-shadow");
            if (shadow != null) shadow.RemoveFromHierarchy();

            if (_host != null)
            {
                var vignette = _host.Q<VisualElement>("top-vignette");
                if (vignette != null) vignette.RemoveFromHierarchy();
            }

            _logoGlowElement = null;
        }

        private void EnsureLogoGlow()
        {
            CreateLogoAura();
        }

        private void EnsureLogoFxLayer()
        {
            if (_logoContainer == null) return;
            if (_logoFxLayer != null) return;

            var fx = new VisualElement();
            fx.name = "logo-fx-layer";
            fx.style.position = Position.Absolute;
            fx.style.left = 0;
            fx.style.top = 0;
            fx.style.right = 0;
            fx.style.bottom = 0;
            fx.pickingMode = PickingMode.Ignore;
            _logoContainer.Add(fx);
            _logoFxLayer = fx;
        }

        /// <summary>
        /// Creates layered programmatic glow elements behind the logo.
        /// Each layer pulses at a different rate for organic, living feel.
        /// </summary>
        private void CreateLogoAura()
        {
            // Disabled - aura elements caused visual glitching during logo click pulse
            return;
        }

        private VisualElement CreateAuraElement(string elName, Color color, int extraWidth, int extraHeight, VisualElement parent)
        {
            var aura = new VisualElement();
            aura.name = elName;
            aura.pickingMode = PickingMode.Ignore;
            aura.style.position = Position.Absolute;
            aura.style.left = new Length(-extraWidth / 2, LengthUnit.Pixel);
            aura.style.top = new Length(-extraHeight / 2, LengthUnit.Pixel);
            aura.style.right = new Length(-extraWidth / 2, LengthUnit.Pixel);
            aura.style.bottom = new Length(-extraHeight / 2, LengthUnit.Pixel);
            aura.style.backgroundColor = color;
            aura.style.borderTopLeftRadius = extraWidth;
            aura.style.borderTopRightRadius = extraWidth;
            aura.style.borderBottomLeftRadius = extraWidth;
            aura.style.borderBottomRightRadius = extraWidth;

            if (parent.childCount > 0)
                parent.Insert(0, aura);
            else
                parent.Add(aura);

            return aura;
        }

        private void AddLogoShadow()
        {
            if (_logoImage == null || _logoContainer == null) return;

            var existingShadow = _logoContainer.Q<VisualElement>("logo-shadow");
            if (existingShadow != null) existingShadow.RemoveFromHierarchy();

            var logoTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers");
            if (logoTexture == null)
            {
                ErrorLogger.Warn("[TitleScreenLogoVFX] Could not load logo texture for shadow");
                return;
            }

            var shadow = new VisualElement();
            shadow.name = "logo-shadow";
            shadow.style.position = Position.Absolute;
            shadow.style.width = 1600;
            shadow.style.height = 400;
            shadow.style.left = Length.Percent(50);
            shadow.style.translate = new Translate(new Length(-796, LengthUnit.Pixel), new Length(6, LengthUnit.Pixel));
            shadow.style.top = 0;
            shadow.style.backgroundImage = new StyleBackground(logoTexture);
            shadow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            shadow.style.unityBackgroundImageTintColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.pickingMode = PickingMode.Ignore;

            int logoIndex = _logoContainer.IndexOf(_logoImage);
            if (logoIndex >= 0)
            {
                _logoContainer.Insert(logoIndex, shadow);
            }
        }
    }
}

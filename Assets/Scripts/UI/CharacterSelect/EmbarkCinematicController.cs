using System;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrates the full embark cinematic sequence per CONTEXT.md timeline (~1.2s).
    /// Plays after hold-to-embark completes, before save/scene transition.
    /// Fires OnCinematicComplete at sequence end for async flow bridging.
    /// </summary>
    public class EmbarkCinematicController : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private VeilTransitionController _veilTransition;
        [SerializeField] private HeroStageController _stageController;

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private HeroThemeTransitioner _themeTransitioner;
        private Camera _dollyCamera;
        private VisualElement _root;
        private VisualElement _leftPanel;
        private VisualElement _rightPanel;
        private VisualElement _carousel;
        private Label _cinematicNameLabel;
        private Sequence _cinematicSequence;

        /// <summary>
        /// Fired when the embark cinematic sequence is fully complete and the caller
        /// should proceed with save/scene load. Subscribers (CharacterSelectManager)
        /// use this to bridge PrimeTween completion into async/await flow.
        /// </summary>
        public event Action OnCinematicComplete;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initializes with UI element references for panel dismissal animations.
        /// Called by CharacterSelectManager after CacheUIReferences.
        /// </summary>
        public void Init(
            VisualElement root,
            VisualElement leftPanel,
            VisualElement rightPanel,
            VisualElement carousel,
            HeroThemeTransitioner themeTransitioner,
            Camera dollyCamera = null)
        {
            _root = root;
            _leftPanel = leftPanel;
            _rightPanel = rightPanel;
            _carousel = carousel;
            _themeTransitioner = themeTransitioner;
            _dollyCamera = dollyCamera;
        }

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnDisable()
        {
            _cinematicSequence.Stop();
            CleanupCinematicLabel();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Plays the full embark cinematic. Call BEFORE save/scene transition.
        /// When complete, fires OnCinematicComplete.
        /// </summary>
        public void PlayEmbarkCinematic(HeroThemeConfig theme, string heroName, string heroTitle)
        {
            _cinematicSequence.Stop();

            // Create centered name label for the flash
            CreateCinematicNameLabel(heroName, theme.primaryColor);

            var seq = Sequence.Create();

            // t=0ms: Full-screen hero accent flash (0.8 opacity, 150ms fade)
            seq.Insert(0f, BuildAccentFlash(theme.primaryColor));

            // t=100ms: Camera dolly into hero (FOV narrows 60->40 over 400ms)
            seq.InsertCallback(0.1f, () => DollyCamera(40f, 0.4f));

            // t=300ms: All UI panels dismiss (slide off-screen, 200ms)
            seq.Insert(0.3f, BuildPanelDismissal());

            // t=500ms: Veil cracks spread from center (300ms)
            if (_veilTransition != null)
            {
                seq.Insert(0.5f, _veilTransition.PlayCrackSpread(0.3f, theme.primaryColor));
            }

            // t=600ms: Hero name glitch text reveal
            if (_cinematicNameLabel != null)
            {
                seq.Insert(0.6f, GlitchTextEffect.BuildGlitchReveal(
                    _cinematicNameLabel, heroName, theme.glitchResolveSpeed));
            }

            // t=800ms: Cracks shatter + name fade
            if (_veilTransition != null)
            {
                seq.Insert(0.8f, _veilTransition.PlayShatter(theme.primaryColor, 0.2f));
            }
            if (_cinematicNameLabel != null)
            {
                seq.Insert(0.8f, Tween.VisualElementOpacity(_cinematicNameLabel, 0f, 0.15f, Ease.InQuad));
            }

            // t=1000ms: White-out
            if (_veilTransition != null)
            {
                seq.Insert(1.0f, _veilTransition.PlayWhiteOut(0.2f));
            }

            // t=1200ms: Complete -- fire OnCinematicComplete
            seq.InsertCallback(1.2f, () =>
            {
                CleanupCinematicLabel();
                OnCinematicComplete?.Invoke();
            });

            _cinematicSequence = seq;
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        /// <summary>
        /// Creates a centered, large hero name label dynamically for the cinematic flash.
        /// </summary>
        private void CreateCinematicNameLabel(string heroName, Color accentColor)
        {
            CleanupCinematicLabel();

            if (_root == null) return;

            _cinematicNameLabel = new Label(heroName);
            _cinematicNameLabel.name = "cinematic-hero-name";
            _cinematicNameLabel.style.position = Position.Absolute;
            _cinematicNameLabel.style.left = 0;
            _cinematicNameLabel.style.right = 0;
            _cinematicNameLabel.style.top = Length.Percent(40);
            _cinematicNameLabel.style.fontSize = 72;
            _cinematicNameLabel.style.color = accentColor;
            _cinematicNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _cinematicNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _cinematicNameLabel.style.opacity = 0f;
            _cinematicNameLabel.style.textShadow = new TextShadow
            {
                offset = new Vector2(0, 0),
                blurRadius = 20,
                color = accentColor
            };
            _cinematicNameLabel.pickingMode = PickingMode.Ignore;
            _cinematicNameLabel.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;

            _root.Add(_cinematicNameLabel);

            // Fade in the label
            _cinematicNameLabel.schedule.Execute(() =>
            {
                _cinematicNameLabel.style.opacity = 1f;
            }).ExecuteLater(10);
        }

        private void CleanupCinematicLabel()
        {
            if (_cinematicNameLabel != null)
            {
                _cinematicNameLabel.RemoveFromHierarchy();
                _cinematicNameLabel = null;
            }
        }

        /// <summary>
        /// Builds a full-screen accent flash: hero color at 0.8 opacity, fading out in 150ms.
        /// </summary>
        private Sequence BuildAccentFlash(Color accentColor)
        {
            if (_root == null) return Sequence.Create();

            var flash = new VisualElement();
            flash.name = "embark-accent-flash";
            flash.style.position = Position.Absolute;
            flash.style.width = Length.Percent(100);
            flash.style.height = Length.Percent(100);
            flash.style.backgroundColor = accentColor;
            flash.style.opacity = 0f;
            flash.pickingMode = PickingMode.Ignore;
            flash.usageHints = UsageHints.DynamicColor;
            _root.Add(flash);

            return Sequence.Create()
                .ChainCallback(() => flash.style.opacity = 0.8f)
                .Chain(Tween.VisualElementOpacity(flash, 0f, 0.15f, Ease.OutQuad))
                .ChainCallback(() => flash.RemoveFromHierarchy());
        }

        /// <summary>
        /// Builds the panel dismissal animation: panels slide off-screen in 200ms.
        /// </summary>
        private Sequence BuildPanelDismissal()
        {
            var seq = Sequence.Create();

            if (_leftPanel != null)
            {
                seq.Group(Tween.Position(_leftPanel, new Vector3(-300f, 0f, 0f), 0.2f, Ease.InCubic));
            }
            if (_rightPanel != null)
            {
                seq.Group(Tween.Position(_rightPanel, new Vector3(300f, 0f, 0f), 0.2f, Ease.InCubic));
            }
            if (_carousel != null)
            {
                seq.Group(Tween.Position(_carousel, new Vector3(0f, 200f, 0f), 0.2f, Ease.InCubic));
            }

            return seq;
        }

        /// <summary>
        /// Dolly camera into hero (narrow FOV for dramatic close-up).
        /// </summary>
        private void DollyCamera(float targetFOV, float duration)
        {
            var cam = _dollyCamera != null ? _dollyCamera : Camera.main;
            if (cam == null) return;

            float startFOV = cam.fieldOfView;
            Tween.Custom(cam, startFOV, targetFOV, duration, Ease.InOutQuad,
                (c, val) => c.fieldOfView = val);
        }
    }
}

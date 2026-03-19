using System;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Audio;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Central orchestrator that reads HeroThemeConfig and drives ALL visual subsystems
    /// on hero switch and screen entry. Subscribes to CharSelectEvents and coordinates:
    /// VolumeProfileTransitioner, VeilDissolveController, OverlayController, HeroStageController,
    /// HeroStatsPanelController, MusicManager, GlitchTextEffect, and ScreenEntryAnimator.
    /// </summary>
    public class HeroThemeTransitioner : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [Header("Theme Data")]
        [SerializeField] private HeroThemeConfig[] _heroThemes;

        [Header("Subsystem References")]
        [SerializeField] private VolumeProfileTransitioner _volumeTransitioner;
        [SerializeField] private VeilDissolveController _dissolveController;
        [SerializeField] private HeroStageController _stageController;

        // =============================================================================
        // INTERNAL SUBSYSTEMS (created in Init, not MonoBehaviours)
        // =============================================================================

        private OverlayController _overlayController;
        private HeroSwitchAnimator _switchAnimator;
        private ScreenEntryAnimator _entryAnimator;

        // =============================================================================
        // CACHED UI ELEMENTS
        // =============================================================================

        private VisualElement _infoPanel;
        private VisualElement _veilPulseFlash;
        private VisualElement _heroStage;
        private VisualElement _carousel;
        private Label _heroNameLabel;
        private Label _heroTitleLabel;

        // =============================================================================
        // STATE
        // =============================================================================

        private HeroThemeConfig _currentTheme;
        private Sequence _activeSequence;
        private bool _hasPlayedEntrance;
        private bool _isInitialized;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
            CharSelectEvents.OnScreenReady += HandleScreenReady;
            CharSelectEvents.OnEmbarkHoldProgress += HandleEmbarkHoldProgress;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnEmbarkHoldProgress -= HandleEmbarkHoldProgress;

            _activeSequence.Stop();
            _hasPlayedEntrance = false;
            _isInitialized = false;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initializes internal subsystems and caches UI element references.
        /// Called by CharacterSelectManager after CacheUIReferences.
        /// </summary>
        public void Init(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[HeroThemeTransitioner] Root VisualElement is null.");
                return;
            }

            // Create internal subsystems
            _overlayController = new OverlayController();
            _overlayController.Init(root);

            _switchAnimator = new HeroSwitchAnimator();
            _entryAnimator = new ScreenEntryAnimator();

            // Cache UI element references for animation
            _infoPanel = root.Q<VisualElement>("info-panel-container");
            _heroStage = root.Q<VisualElement>("hero-stage");
            _carousel = root.Q<VisualElement>("carousel-strip")?.parent
                        ?? root.Q<VisualElement>("carousel-container");
            _heroNameLabel = root.Q<Label>("hero-name");
            _heroTitleLabel = root.Q<Label>("hero-title");
            _veilPulseFlash = root.Q<VisualElement>("veil-pulse-flash");

            // Initialize parallax with panel references
            var leftPanel = _heroStage; // Hero stage is the left side of rule-of-thirds
            _overlayController.InitParallax(leftPanel, _infoPanel, _carousel);

            _isInitialized = true;
        }

        /// <summary>
        /// Returns the currently active HeroThemeConfig (used by embark cinematic).
        /// </summary>
        public HeroThemeConfig GetCurrentTheme() => _currentTheme;

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleScreenReady()
        {
            if (!_isInitialized) return;

            if (!_hasPlayedEntrance)
            {
                _hasPlayedEntrance = true;

                // Get the first hero's theme for the entrance sequence
                HeroThemeConfig firstTheme = (_heroThemes != null && _heroThemes.Length > 0)
                    ? _heroThemes[0]
                    : null;

                var entrySequence = _entryAnimator.BuildScreenEntrySequence(
                    _heroStage,
                    _heroStage, // left panel is the hero stage in the layout
                    _infoPanel,
                    _carousel,
                    _overlayController,
                    firstTheme,
                    onEntryComplete: () =>
                    {
                        // After entrance, transition to first hero's full theme
                        if (firstTheme != null)
                        {
                            _currentTheme = firstTheme;
                            ApplyFullTheme(firstTheme);
                        }
                    }
                );
            }
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig displayConfig)
        {
            if (!_isInitialized) return;

            // Look up HeroThemeConfig by heroId or index
            HeroThemeConfig newTheme = GetThemeForHero(data?.hero_id, index);
            if (newTheme == null) return;

            // Skip if same hero
            if (_currentTheme != null && _currentTheme.heroId == newTheme.heroId) return;

            // Stop any active sequence
            _activeSequence.Stop();

            // Build and play hero switch sequence
            _activeSequence = _switchAnimator.BuildHeroSwitchSequence(
                newTheme,
                _infoPanel,
                _veilPulseFlash,
                _heroNameLabel,
                _heroTitleLabel,
                _volumeTransitioner,
                _dissolveController,
                _overlayController,
                applyMusicParameters: () => ApplyMusicParameters(newTheme),
                applyLightingTransition: () => ApplyLightingTransition(newTheme),
                applyStatBarCascade: theme => ApplyStatBarCascade(theme)
            );

            _currentTheme = newTheme;
        }

        private void HandleEmbarkHoldProgress(float progress)
        {
            _overlayController?.SetEmbarkIntensification(progress);
        }

        // =============================================================================
        // THEME APPLICATION
        // =============================================================================

        /// <summary>
        /// Applies the full theme immediately (used after screen entry animation completes).
        /// </summary>
        private void ApplyFullTheme(HeroThemeConfig theme)
        {
            ApplyMusicParameters(theme);
            ApplyLightingTransition(theme);

            if (_volumeTransitioner != null)
            {
                _volumeTransitioner.LerpTo(theme, 0.5f);
            }

            _overlayController?.TransitionTo(theme, 0.5f);
        }

        private void ApplyMusicParameters(HeroThemeConfig theme)
        {
            if (!MusicManager.HasInstance) return;

            MusicManager.Instance.SetHeroMusicParameters(
                theme.musicIntensity,
                theme.musicWarmth,
                theme.musicTension,
                theme.musicSynth,
                theme.musicPerc,
                theme.musicPad,
                theme.musicFilter
            );
        }

        private void ApplyLightingTransition(HeroThemeConfig theme)
        {
            if (_stageController != null)
            {
                _stageController.TransitionLighting(theme, 0.6f);
            }
        }

        private void ApplyStatBarCascade(HeroThemeConfig theme)
        {
            // Stat bar cascade is driven by HeroStatsPanelController which already
            // subscribes to OnHeroChanged. The cascade animation is initiated there.
            // This callback is a placeholder for future per-theme stat animation tuning.
        }

        // =============================================================================
        // PARALLAX
        // =============================================================================

        /// <summary>
        /// Called per-frame from CharacterSelectManager.LateUpdate().
        /// Drives subtle panel parallax based on mouse/stick input.
        /// </summary>
        public void UpdateParallax()
        {
            _overlayController?.UpdateParallax();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        /// <summary>
        /// Searches _heroThemes array for a matching heroId, falls back to index,
        /// then to first entry with a warning.
        /// </summary>
        private HeroThemeConfig GetThemeForHero(string heroId, int index)
        {
            if (_heroThemes == null || _heroThemes.Length == 0)
            {
                Debug.LogWarning("[HeroThemeTransitioner] No HeroThemeConfig entries assigned.");
                return null;
            }

            // Try matching by heroId
            if (!string.IsNullOrEmpty(heroId))
            {
                for (int i = 0; i < _heroThemes.Length; i++)
                {
                    if (_heroThemes[i] != null && _heroThemes[i].heroId == heroId)
                    {
                        return _heroThemes[i];
                    }
                }
            }

            // Fall back to index
            if (index >= 0 && index < _heroThemes.Length && _heroThemes[index] != null)
            {
                return _heroThemes[index];
            }

            // Fall back to first available
            Debug.LogWarning($"[HeroThemeTransitioner] No theme found for hero '{heroId}' at index {index}. Using first.");
            return _heroThemes[0];
        }
    }
}

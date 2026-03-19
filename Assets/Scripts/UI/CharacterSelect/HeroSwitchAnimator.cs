using System;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Builds the hero switch PrimeTween Sequence per CONTEXT.md timeline (~1.2s).
    /// Pure C# class (not MonoBehaviour) -- produces composable Sequences.
    /// Timeline:
    ///   t=0ms:   Info panel fades out + music crossfade starts
    ///   t=100ms: Veil pulse flash (hero accent color, 0.1 opacity, 200ms fade)
    ///   t=200ms: 3D model dissolve OUT + post-process lerp starts
    ///   t=400ms: New hero materializes (dissolve IN) + new properties set
    ///   t=600ms: Info panel fades in + overlays shift + lighting transition
    ///   t=800ms: Stat bars cascade fill + glitch text reveal
    /// </summary>
    public class HeroSwitchAnimator
    {
        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Builds the full hero switch choreography sequence (~1.2s).
        /// All subsystem calls are injected as delegates to avoid tight coupling.
        /// </summary>
        public Sequence BuildHeroSwitchSequence(
            HeroThemeConfig newTheme,
            VisualElement infoPanel,
            VisualElement veilPulseFlash,
            Label heroNameLabel,
            Label heroTitleLabel,
            VolumeProfileTransitioner volumeTransitioner,
            VeilDissolveController dissolveController,
            OverlayController overlayController,
            Action applyMusicParameters,
            Action applyLightingTransition,
            Action<HeroThemeConfig> applyStatBarCascade
        )
        {
            var seq = Sequence.Create();

            // =====================================================================
            // t=0ms: Info panel fades out + music crossfade starts
            // =====================================================================
            if (infoPanel != null)
            {
                seq.Insert(0f, Tween.VisualElementOpacity(infoPanel, 0f, 0.2f, Ease.InQuad));
            }
            seq.InsertCallback(0f, () => applyMusicParameters?.Invoke());

            // =====================================================================
            // t=100ms: Veil pulse flash (hero accent color, 0.1 opacity, 200ms fade)
            // =====================================================================
            if (veilPulseFlash != null)
            {
                seq.Insert(0.1f, BuildVeilPulseFlash(veilPulseFlash, newTheme.primaryColor));
            }

            // =====================================================================
            // t=200ms: 3D model dissolve OUT + post-process lerp starts (0.8s)
            // =====================================================================
            if (dissolveController != null)
            {
                seq.Insert(0.2f, dissolveController.AnimateDissolveOut(newTheme.dissolveDuration));
            }
            if (volumeTransitioner != null)
            {
                seq.InsertCallback(0.2f, () => volumeTransitioner.LerpTo(newTheme, 0.8f));
            }

            // =====================================================================
            // t=400ms: New hero materializes (dissolve IN) + set new hero properties
            // =====================================================================
            if (dissolveController != null)
            {
                seq.InsertCallback(0.4f, () => dissolveController.SetHeroProperties(newTheme));
                seq.Insert(0.4f, dissolveController.AnimateDissolveIn(newTheme.dissolveDuration));
            }

            // =====================================================================
            // t=600ms: Info panel fades in + overlays shift + lighting transition
            // =====================================================================
            if (infoPanel != null)
            {
                seq.Insert(0.6f, Tween.VisualElementOpacity(infoPanel, 1f, 0.3f, Ease.OutQuad));
            }
            if (overlayController != null)
            {
                seq.InsertCallback(0.6f, () => overlayController.TransitionTo(newTheme, 0.5f));
            }
            seq.InsertCallback(0.6f, () => applyLightingTransition?.Invoke());

            // =====================================================================
            // t=800ms: Stat cascade + glitch text reveal
            // =====================================================================
            seq.InsertCallback(0.8f, () => applyStatBarCascade?.Invoke(newTheme));

            if (heroNameLabel != null && !string.IsNullOrEmpty(heroNameLabel.text))
            {
                seq.Insert(0.8f, GlitchTextEffect.BuildGlitchReveal(
                    heroNameLabel, heroNameLabel.text, newTheme.glitchResolveSpeed));
            }

            if (heroTitleLabel != null && !string.IsNullOrEmpty(heroTitleLabel.text))
            {
                seq.Insert(0.8f, GlitchTextEffect.BuildGlitchReveal(
                    heroTitleLabel, heroTitleLabel.text, newTheme.glitchResolveSpeed));
            }

            return seq;
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        /// <summary>
        /// Creates a brief full-screen veil pulse flash: hero accent color at 0.1 opacity,
        /// fading to 0 over 200ms. Provides dramatic "veil energy" feedback on hero switch.
        /// </summary>
        private static Sequence BuildVeilPulseFlash(VisualElement flash, Color accentColor)
        {
            // Set the background color to hero accent
            flash.style.backgroundColor = accentColor;

            // Animate opacity: snap to 0.1 then fade out over 200ms
            return Sequence.Create()
                .ChainCallback(() => flash.style.opacity = 0.1f)
                .Chain(Tween.VisualElementOpacity(flash, 0f, 0.2f, Ease.OutQuad));
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Builds the one-shot screen entrance PrimeTween Sequence.
    /// Pure C# class (not MonoBehaviour) -- produces composable Sequences.
    /// Timeline per CONTEXT.md:
    ///   t=0ms:   3D stage fades in (opacity 0->1 over 300ms)
    ///   t=100ms: Left panel slides from left edge (translateX -300px -> 0 over 400ms OutCubic)
    ///   t=200ms: Right panel slides from right edge (translateX 300px -> 0 over 400ms OutCubic)
    ///   t=350ms: Carousel rises from bottom (translateY 200px -> 0 over 350ms OutCubic)
    ///   t=500ms: Overlays fade in + post-process lerps to first hero's profile
    ///   t=700ms: onEntryComplete callback
    /// </summary>
    public class ScreenEntryAnimator
    {
        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Builds the screen entry staggered panel entrance sequence.
        /// Sets all animated elements to their start positions before building the sequence.
        /// </summary>
        public Sequence BuildScreenEntrySequence(
            VisualElement heroStage,
            VisualElement leftPanel,
            VisualElement rightPanel,
            VisualElement carousel,
            OverlayController overlayController,
            HeroThemeConfig firstHeroTheme,
            Action onEntryComplete
        )
        {
            // Set DynamicTransform | DynamicColor hints for GPU-accelerated animation
            SetAnimationHints(heroStage);
            SetAnimationHints(leftPanel);
            SetAnimationHints(rightPanel);
            SetAnimationHints(carousel);

            // Set start positions (everything off-screen or hidden) using CSS translate
            if (heroStage != null)
            {
                heroStage.style.opacity = 0f;
            }
            if (leftPanel != null)
            {
                leftPanel.style.translate = new Translate(-300f, 0);
                leftPanel.style.opacity = 0f;
            }
            if (rightPanel != null)
            {
                rightPanel.style.translate = new Translate(300f, 0);
                rightPanel.style.opacity = 0f;
            }
            if (carousel != null)
            {
                carousel.style.translate = new Translate(0, 200f);
                carousel.style.opacity = 0f;
            }

            var seq = Sequence.Create();

            // =====================================================================
            // t=0ms: 3D stage fades in (center, opacity 0->1 over 300ms)
            // =====================================================================
            if (heroStage != null)
            {
                seq.Insert(0f, Tween.VisualElementOpacity(heroStage, 1f, 0.3f, Ease.OutQuad));
            }

            // =====================================================================
            // t=100ms: Left panel slides from left edge
            // =====================================================================
            if (leftPanel != null)
            {
                seq.Insert(0.1f, Tween.Custom(leftPanel, -300f, 0f, 0.4f,
                    onValueChange: (el, val) => el.style.translate = new Translate(val, 0),
                    ease: Ease.OutCubic));
                seq.Insert(0.1f, Tween.VisualElementOpacity(leftPanel, 1f, 0.3f, Ease.OutQuad));
            }

            // =====================================================================
            // t=200ms: Right panel slides from right edge
            // =====================================================================
            if (rightPanel != null)
            {
                seq.Insert(0.2f, Tween.Custom(rightPanel, 300f, 0f, 0.4f,
                    onValueChange: (el, val) => el.style.translate = new Translate(val, 0),
                    ease: Ease.OutCubic));
                seq.Insert(0.2f, Tween.VisualElementOpacity(rightPanel, 1f, 0.3f, Ease.OutQuad));
            }

            // =====================================================================
            // t=350ms: Carousel rises from bottom
            // =====================================================================
            if (carousel != null)
            {
                seq.Insert(0.35f, Tween.Custom(carousel, 200f, 0f, 0.35f,
                    onValueChange: (el, val) => el.style.translate = new Translate(0, val),
                    ease: Ease.OutCubic));
                seq.Insert(0.35f, Tween.VisualElementOpacity(carousel, 1f, 0.25f, Ease.OutQuad));
            }

            // =====================================================================
            // t=500ms: Overlays fade in + post-process lerps to first hero's profile
            // =====================================================================
            if (overlayController != null && firstHeroTheme != null)
            {
                seq.InsertCallback(0.5f, () => overlayController.TransitionTo(firstHeroTheme, 0.5f));
            }

            // =====================================================================
            // t=700ms: Entry complete callback
            // =====================================================================
            seq.InsertCallback(0.7f, () => onEntryComplete?.Invoke());

            return seq;
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private static void SetAnimationHints(VisualElement element)
        {
            if (element == null) return;
            element.usageHints |= UsageHints.DynamicTransform | UsageHints.DynamicColor;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Builds the one-shot screen entrance PrimeTween Sequence.
    /// </summary>
    public class ScreenEntryAnimator
    {
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
            SetAnimationHints(heroStage);
            SetAnimationHints(leftPanel);
            SetAnimationHints(rightPanel);
            SetAnimationHints(carousel);

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

            // t=0ms: 3D stage fades in
            if (heroStage != null)
            {
                seq.Insert(0f, Tween.VisualElementOpacity(heroStage, 1f, 0.3f, Ease.OutQuad));
            }

            // t=100ms: Left panel slides from left edge
            if (leftPanel != null)
            {
                seq.Insert(0.1f, Tween.Custom(
                    -300f, 0f, 0.4f,
                    val => { if (leftPanel != null) leftPanel.style.translate = new Translate(val, 0); },
                    Ease.OutCubic));
                seq.Insert(0.1f, Tween.VisualElementOpacity(leftPanel, 1f, 0.3f, Ease.OutQuad));
            }

            // t=200ms: Right panel slides from right edge
            if (rightPanel != null)
            {
                seq.Insert(0.2f, Tween.Custom(
                    300f, 0f, 0.4f,
                    val => { if (rightPanel != null) rightPanel.style.translate = new Translate(val, 0); },
                    Ease.OutCubic));
                seq.Insert(0.2f, Tween.VisualElementOpacity(rightPanel, 1f, 0.3f, Ease.OutQuad));
            }

            // t=350ms: Carousel rises from bottom
            if (carousel != null)
            {
                seq.Insert(0.35f, Tween.Custom(
                    200f, 0f, 0.35f,
                    val => { if (carousel != null) carousel.style.translate = new Translate(0, val); },
                    Ease.OutCubic));
                seq.Insert(0.35f, Tween.VisualElementOpacity(carousel, 1f, 0.25f, Ease.OutQuad));
            }

            // t=500ms: Overlays fade in
            if (overlayController != null && firstHeroTheme != null)
            {
                seq.InsertCallback(0.5f, () => overlayController.TransitionTo(firstHeroTheme, 0.5f));
            }

            // t=700ms: Entry complete
            seq.InsertCallback(0.7f, () => onEntryComplete?.Invoke());

            return seq;
        }

        private static void SetAnimationHints(VisualElement element)
        {
            if (element == null) return;
            element.usageHints |= UsageHints.DynamicTransform | UsageHints.DynamicColor;
        }
    }
}

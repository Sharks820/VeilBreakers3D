using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Controls
{
    /// <summary>
    /// AAA button VFX helper for adding visual feedback to UI buttons.
    /// Provides hover glow, click ripple, and focus effects.
    /// </summary>
    public static class ButtonVFXHelper
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private const string kRippleClass = "button-ripple";
        private const string kGlowClass = "button-glow";
        private const string kClickClass = "button--clicked";
        private const string kPressedClass = "button--pressed";

        // =============================================================================
        // SETUP
        // =============================================================================

        /// <summary>
        /// Apply AAA visual effects to a button element.
        /// </summary>
        public static void ApplyEffects(Button button, ButtonVFXOptions options = null)
        {
            options ??= ButtonVFXOptions.Default;

            if (options.EnableClickRipple)
            {
                SetupClickRipple(button);
            }

            if (options.EnableHoverGlow)
            {
                SetupHoverGlow(button);
            }

            if (options.EnablePressEffect)
            {
                SetupPressEffect(button);
            }

            if (options.EnableAudioFeedback)
            {
                SetupAudioFeedback(button);
            }
        }

        /// <summary>
        /// Apply effects to all buttons matching a class name.
        /// </summary>
        public static void ApplyToAll(VisualElement root, string className = "vb-button", ButtonVFXOptions options = null)
        {
            var buttons = root.Query<Button>(className: className).ToList();
            foreach (var button in buttons)
            {
                ApplyEffects(button, options);
            }
        }

        // =============================================================================
        // CLICK RIPPLE EFFECT
        // =============================================================================

        private static void SetupClickRipple(Button button)
        {
            button.RegisterCallback<ClickEvent>(evt =>
            {
                CreateRipple(button, evt.localPosition);
            });
        }

        private static void CreateRipple(Button button, Vector2 localPosition)
        {
            var ripple = new VisualElement();
            ripple.AddToClassList(kRippleClass);
            ripple.style.position = Position.Absolute;
            ripple.style.left = localPosition.x;
            ripple.style.top = localPosition.y;
            ripple.style.width = 10;
            ripple.style.height = 10;
            ripple.style.borderTopLeftRadius = 50;
            ripple.style.borderTopRightRadius = 50;
            ripple.style.borderBottomLeftRadius = 50;
            ripple.style.borderBottomRightRadius = 50;
            ripple.style.backgroundColor = new Color(1f, 1f, 1f, 0.3f);
            ripple.style.translate = new Translate(-5, -5);
            ripple.style.opacity = 1f;

            // Add ripple to button
            button.Add(ripple);

            // Animate expansion and fade
            button.schedule.Execute(() =>
            {
                ripple.style.width = 100;
                ripple.style.height = 100;
                ripple.style.translate = new Translate(-50, -50);
                ripple.style.opacity = 0f;
            }).ExecuteLater(10);

            // Remove after animation
            button.schedule.Execute(() =>
            {
                ripple.RemoveFromHierarchy();
            }).ExecuteLater(400);
        }

        // =============================================================================
        // HOVER GLOW EFFECT
        // =============================================================================

        private static void SetupHoverGlow(Button button)
        {
            // Create glow element
            var glow = new VisualElement();
            glow.AddToClassList(kGlowClass);
            glow.style.position = Position.Absolute;
            glow.style.left = 0;
            glow.style.top = 0;
            glow.style.right = 0;
            glow.style.bottom = 0;
            var glowRadius = button.resolvedStyle.borderTopLeftRadius;
            glow.style.borderTopLeftRadius = glowRadius;
            glow.style.borderTopRightRadius = glowRadius;
            glow.style.borderBottomLeftRadius = glowRadius;
            glow.style.borderBottomRightRadius = glowRadius;
            glow.style.opacity = 0;
            glow.style.backgroundColor = new Color(0.6f, 0.3f, 0.8f, 0.1f);
            glow.pickingMode = PickingMode.Ignore;

            button.Insert(0, glow);

            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                glow.style.opacity = 1f;
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                glow.style.opacity = 0f;
            });
        }

        // =============================================================================
        // PRESS EFFECT
        // =============================================================================

        private static void SetupPressEffect(Button button)
        {
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                button.AddToClassList(kPressedClass);
            });

            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                button.RemoveFromClassList(kPressedClass);
            });

            button.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                button.RemoveFromClassList(kPressedClass);
            });
        }

        // =============================================================================
        // AUDIO FEEDBACK
        // =============================================================================

        private static void SetupAudioFeedback(Button button)
        {
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                // Play hover sound via audio system
                // AudioManager.Instance?.PlayUISound("button_hover");
            });

            button.RegisterCallback<ClickEvent>(evt =>
            {
                // Play click sound via audio system
                // AudioManager.Instance?.PlayUISound("button_click");
            });
        }

        // =============================================================================
        // SPECIALTY EFFECTS
        // =============================================================================

        /// <summary>
        /// Apply a "shimmer" effect that moves across the button surface.
        /// </summary>
        public static void AddShimmer(Button button, float interval = 3f)
        {
            var shimmer = new VisualElement();
            shimmer.style.position = Position.Absolute;
            shimmer.style.top = 0;
            shimmer.style.bottom = 0;
            shimmer.style.width = 50;
            shimmer.style.left = -50;
            shimmer.style.opacity = 0.3f;
            shimmer.style.backgroundImage = null; // Would be a gradient texture
            shimmer.pickingMode = PickingMode.Ignore;

            button.Insert(0, shimmer);

            // Schedule shimmer animation
            void DoShimmer()
            {
                float buttonWidth = button.resolvedStyle.width;
                shimmer.style.left = -50;

                button.schedule.Execute(() =>
                {
                    shimmer.style.left = buttonWidth + 50;
                }).ExecuteLater(50);

                // Reset for next shimmer
                button.schedule.Execute(() =>
                {
                    shimmer.style.left = -50;
                }).ExecuteLater(800);
            }

            // Initial shimmer after a delay
            button.schedule.Execute(DoShimmer).ExecuteLater((long)(interval * 1000));

            // Repeat
            button.schedule.Execute(DoShimmer).Every((long)(interval * 1000));
        }

        /// <summary>
        /// Add a pulsing border effect for important buttons.
        /// </summary>
        public static void AddPulseBorder(Button button, Color pulseColor)
        {
            bool pulseIn = true;

            void DoPulse()
            {
                var color = pulseIn
                    ? pulseColor
                    : new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0.3f);

                button.style.borderTopColor = color;
                button.style.borderBottomColor = color;
                button.style.borderLeftColor = color;
                button.style.borderRightColor = color;

                pulseIn = !pulseIn;
            }

            button.schedule.Execute(DoPulse).Every(800);
        }
    }

    /// <summary>
    /// Options for button VFX configuration.
    /// </summary>
    public class ButtonVFXOptions
    {
        public bool EnableClickRipple { get; set; } = true;
        public bool EnableHoverGlow { get; set; } = true;
        public bool EnablePressEffect { get; set; } = true;
        public bool EnableAudioFeedback { get; set; } = true;

        public static ButtonVFXOptions Default => new();

        public static ButtonVFXOptions Minimal => new()
        {
            EnableClickRipple = false,
            EnableHoverGlow = true,
            EnablePressEffect = true,
            EnableAudioFeedback = false
        };

        public static ButtonVFXOptions Silent => new()
        {
            EnableClickRipple = true,
            EnableHoverGlow = true,
            EnablePressEffect = true,
            EnableAudioFeedback = false
        };
    }
}

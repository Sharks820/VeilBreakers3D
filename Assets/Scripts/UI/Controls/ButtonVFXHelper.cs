using System;
using System.Collections.Generic;
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

        // Cached transition lists to avoid per-call allocation
        private static readonly List<StylePropertyName> kShimmerProperties = new()
        {
            new StylePropertyName("left"),
            new StylePropertyName("opacity")
        };
        private static readonly List<TimeValue> kShimmerDurations = new()
        {
            new TimeValue(0.6f, TimeUnit.Second),
            new TimeValue(0.15f, TimeUnit.Second)
        };
        private static readonly List<EasingFunction> kShimmerEasing = new()
        {
            new EasingFunction(EasingMode.EaseInOut),
            new EasingFunction(EasingMode.EaseOut)
        };

        private static readonly List<StylePropertyName> kBurstProperties = new()
        {
            new StylePropertyName("translate"),
            new StylePropertyName("opacity"),
            new StylePropertyName("scale")
        };
        private static readonly List<TimeValue> kBurstDurations = new()
        {
            new TimeValue(0.4f, TimeUnit.Second),
            new TimeValue(0.35f, TimeUnit.Second),
            new TimeValue(0.4f, TimeUnit.Second)
        };
        private static readonly List<EasingFunction> kBurstEasing = new()
        {
            new EasingFunction(EasingMode.EaseOut),
            new EasingFunction(EasingMode.EaseIn),
            new EasingFunction(EasingMode.EaseOut)
        };

        private static readonly List<StylePropertyName> kChargeProperties = new()
        {
            new StylePropertyName("width"),
            new StylePropertyName("opacity")
        };
        private static readonly List<TimeValue> kChargeDurations = new()
        {
            new TimeValue(0.8f, TimeUnit.Second),
            new TimeValue(0.15f, TimeUnit.Second)
        };
        private static readonly List<EasingFunction> kChargeEasing = new()
        {
            new EasingFunction(EasingMode.EaseOut),
            new EasingFunction(EasingMode.EaseOut)
        };

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
            ripple.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
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
        /// Detects pointer entry side for directional sweep.
        /// </summary>
        public static void AddShimmer(Button button, float interval = 3f)
        {
            var shimmer = new VisualElement();
            shimmer.style.position = Position.Absolute;
            shimmer.style.top = 0;
            shimmer.style.bottom = 0;
            shimmer.style.width = 30;
            shimmer.style.left = -30;
            shimmer.style.opacity = 0;
            shimmer.style.backgroundColor = new Color(1f, 0.85f, 0.6f, 0.25f);
            shimmer.style.rotate = new Rotate(15f);
            shimmer.style.scale = new Scale(new Vector2(1f, 1.5f));
            shimmer.pickingMode = PickingMode.Ignore;

            shimmer.style.transitionProperty = kShimmerProperties;
            shimmer.style.transitionDuration = kShimmerDurations;
            shimmer.style.transitionTimingFunction = kShimmerEasing;

            button.Insert(0, shimmer);

            // Track pointer entry direction for directional shimmer on hover
            bool _sweepRightToLeft = false;
            button.RegisterCallback<PointerEnterEvent>(evt =>
            {
                float buttonWidth = button.resolvedStyle.width;
                if (buttonWidth <= 0) return;
                float centerX = buttonWidth * 0.5f;
                _sweepRightToLeft = evt.localPosition.x > centerX;

                // Trigger a quick hover shimmer in the entry direction
                DoDirectionalShimmer(shimmer, button, _sweepRightToLeft);
            });

            // Periodic auto-shimmer (always left-to-right)
            void DoAutoShimmer()
            {
                DoDirectionalShimmer(shimmer, button, false);
            }

            // Single repeating schedule (removed duplicate ExecuteLater that caused double-fire)
            button.schedule.Execute(DoAutoShimmer).Every((long)(interval * 1000));
        }

        private static void DoDirectionalShimmer(VisualElement shimmer, Button button, bool rightToLeft)
        {
            float buttonWidth = button.resolvedStyle.width;
            if (buttonWidth <= 0) return;

            float startPos = rightToLeft ? buttonWidth + 30 : -30;
            float endPos = rightToLeft ? -30 : buttonWidth + 30;

            shimmer.style.left = startPos;
            shimmer.style.opacity = 0;

            button.schedule.Execute(() =>
            {
                shimmer.style.opacity = 1f;
                shimmer.style.left = endPos;
            }).ExecuteLater(50);

            button.schedule.Execute(() =>
            {
                shimmer.style.opacity = 0;
            }).ExecuteLater(600);

            button.schedule.Execute(() =>
            {
                shimmer.style.left = startPos;
            }).ExecuteLater(800);
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

        // =============================================================================
        // CLICK BURST EFFECT
        // =============================================================================

        /// <summary>
        /// Add a radial particle burst on button click for impact feedback.
        /// Spawns small dots that radiate outward from click point and fade.
        /// </summary>
        public static void AddClickBurst(Button button, int particleCount = 8)
        {
            button.RegisterCallback<ClickEvent>(evt =>
            {
                SpawnClickBurst(button, evt.localPosition, particleCount);
            });
        }

        private static void SpawnClickBurst(VisualElement host, Vector2 origin, int count)
        {
            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                var particle = new VisualElement();
                particle.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
                particle.style.position = Position.Absolute;
                float size = UnityEngine.Random.Range(3f, 6f);
                particle.style.width = size;
                particle.style.height = size;
                particle.style.borderTopLeftRadius = size;
                particle.style.borderTopRightRadius = size;
                particle.style.borderBottomLeftRadius = size;
                particle.style.borderBottomRightRadius = size;
                particle.style.backgroundColor = new Color(1f, 0.7f, 0.3f, 0.9f);
                particle.style.left = origin.x - size * 0.5f;
                particle.style.top = origin.y - size * 0.5f;
                particle.style.opacity = 1f;
                particle.pickingMode = PickingMode.Ignore;

                // CSS transitions for outward movement + fade
                particle.style.transitionProperty = kBurstProperties;
                particle.style.transitionDuration = kBurstDurations;
                particle.style.transitionTimingFunction = kBurstEasing;

                host.Add(particle);

                float angle = (angleStep * i + UnityEngine.Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(25f, 50f);
                float tx = Mathf.Cos(angle) * distance;
                float ty = Mathf.Sin(angle) * distance;

                host.schedule.Execute(() =>
                {
                    particle.style.translate = new Translate(tx, ty);
                    particle.style.opacity = 0;
                    particle.style.scale = new Scale(new Vector2(0.3f, 0.3f));
                }).ExecuteLater(10);

                host.schedule.Execute(() =>
                {
                    particle.RemoveFromHierarchy();
                }).ExecuteLater(500);
            }
        }

        // =============================================================================
        // BREATHING EFFECT
        // =============================================================================

        /// <summary>
        /// Add a gentle idle breathing (scale oscillation) to a visual element.
        /// Creates organic, alive feel for static UI containers.
        /// </summary>
        public static IVisualElementScheduledItem AddBreathing(VisualElement element, float amplitude = 0.008f, float periodMs = 3000f)
        {
            if (element == null) return null;

            float startTime = Time.time;

            return element.schedule.Execute(() =>
            {
                float elapsed = Time.time - startTime;
                float t = Mathf.Sin(elapsed * (2f * Mathf.PI) / (periodMs / 1000f));
                float scale = 1f + t * amplitude;
                element.style.scale = new Scale(new Vector2(scale, scale));
            }).Every(50);
        }

        // =============================================================================
        // CHARGE LINE EFFECT
        // =============================================================================

        /// <summary>
        /// Add a charge line that fills along the bottom edge when button is held down.
        /// Provides visual feedback for prolonged press interactions.
        /// </summary>
        public static void AddChargeEffect(Button button)
        {
            var chargeLine = new VisualElement();
            chargeLine.style.position = Position.Absolute;
            chargeLine.style.bottom = 0;
            chargeLine.style.left = 0;
            chargeLine.style.height = 2;
            chargeLine.style.width = Length.Percent(0);
            chargeLine.style.backgroundColor = new Color(1f, 0.6f, 0.2f, 0.9f);
            chargeLine.style.opacity = 0;
            chargeLine.pickingMode = PickingMode.Ignore;

            chargeLine.style.transitionProperty = kChargeProperties;
            chargeLine.style.transitionDuration = kChargeDurations;
            chargeLine.style.transitionTimingFunction = kChargeEasing;

            button.Add(chargeLine);

            bool isCharging = false;

            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                isCharging = true;
                chargeLine.style.opacity = 1;
                chargeLine.style.width = Length.Percent(100);
            });

            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!isCharging) return;
                isCharging = false;

                // If charge completed (give it time to fill), trigger a pulse
                chargeLine.style.width = Length.Percent(0);
                chargeLine.style.opacity = 0;
            });

            button.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (!isCharging) return;
                isCharging = false;
                chargeLine.style.width = Length.Percent(0);
                chargeLine.style.opacity = 0;
            });
        }

        // =============================================================================
        // FOCUS EFFECT (GAMEPAD/KEYBOARD)
        // =============================================================================

        // =============================================================================
        // TOP HIGHLIGHT EFFECT
        // =============================================================================

        /// <summary>
        /// Adds a decorative gradient highlight line at the top of a button.
        /// Creates the ornate look for main menu buttons.
        /// </summary>
        public static void AddTopHighlight(VisualElement button)
        {
            if (button == null) return;
            // Avoid duplicates on re-init
            if (button.Q("btn-top-highlight") != null) return;

            var highlight = new VisualElement();
            highlight.name = "btn-top-highlight";
            highlight.pickingMode = PickingMode.Ignore;
            highlight.style.position = Position.Absolute;
            highlight.style.top = 0;
            highlight.style.left = Length.Percent(15);
            highlight.style.right = Length.Percent(15);
            highlight.style.height = 1;
            highlight.style.backgroundColor = new Color(0.78f, 0.59f, 0.27f, 0.3f);
            button.Add(highlight);
        }

        // =============================================================================
        // FOCUS EFFECT (GAMEPAD/KEYBOARD)
        // =============================================================================

        /// <summary>
        /// Add premium focus effects for gamepad/keyboard navigation.
        /// Applies glow border, scale bump, and shimmer on FocusIn.
        /// </summary>
        public static void AddFocusEffect(Button button)
        {
            button.RegisterCallback<FocusInEvent>(evt =>
            {
                button.style.scale = new Scale(new Vector2(1.04f, 1.04f));
                button.style.borderTopColor = new Color(1f, 0.7f, 0.3f, 0.9f);
                button.style.borderBottomColor = new Color(1f, 0.7f, 0.3f, 0.9f);
                button.style.borderLeftColor = new Color(1f, 0.7f, 0.3f, 0.9f);
                button.style.borderRightColor = new Color(1f, 0.7f, 0.3f, 0.9f);
                button.AddToClassList("vb-button-hover-glow");
            });

            button.RegisterCallback<FocusOutEvent>(evt =>
            {
                button.style.scale = new Scale(Vector2.one);
                button.RemoveFromClassList("vb-button-hover-glow");
                // Border colors will be restored by hover callbacks
            });
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

using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Manages UI theming, brand colors, and corruption state styling.
    /// Provides a centralized API for accessing theme tokens at runtime.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static ThemeManager _instance;
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ThemeManager");
                    _instance = go.AddComponent<ThemeManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            InitializeColors();
        }

        // =============================================================================
        // BRAND COLORS
        // =============================================================================

        [Serializable]
        public struct BrandColorSet
        {
            public Color primary;
            public Color glow;
            public Color dark;

            public BrandColorSet(Color primary, Color glow, Color dark)
            {
                this.primary = primary;
                this.glow = glow;
                this.dark = dark;
            }
        }

        private Dictionary<Brand, BrandColorSet> _brandColors;

        private void InitializeColors()
        {
            _brandColors = new Dictionary<Brand, BrandColorSet>
            {
                { Brand.Iron, new BrandColorSet(
                    new Color(140/255f, 150/255f, 165/255f),
                    new Color(180/255f, 190/255f, 205/255f),
                    new Color(80/255f, 90/255f, 100/255f))
                },
                { Brand.Savage, new BrandColorSet(
                    new Color(180/255f, 45/255f, 45/255f),
                    new Color(220/255f, 70/255f, 70/255f),
                    new Color(120/255f, 25/255f, 25/255f))
                },
                { Brand.Surge, new BrandColorSet(
                    new Color(60/255f, 140/255f, 220/255f),
                    new Color(100/255f, 180/255f, 255/255f),
                    new Color(30/255f, 80/255f, 140/255f))
                },
                { Brand.Venom, new BrandColorSet(
                    new Color(80/255f, 180/255f, 60/255f),
                    new Color(120/255f, 220/255f, 100/255f),
                    new Color(40/255f, 100/255f, 30/255f))
                },
                { Brand.Dread, new BrandColorSet(
                    new Color(120/255f, 60/255f, 160/255f),
                    new Color(160/255f, 100/255f, 200/255f),
                    new Color(70/255f, 30/255f, 100/255f))
                },
                { Brand.Leech, new BrandColorSet(
                    new Color(140/255f, 40/255f, 80/255f),
                    new Color(180/255f, 60/255f, 110/255f),
                    new Color(90/255f, 20/255f, 50/255f))
                },
                { Brand.Grace, new BrandColorSet(
                    new Color(220/255f, 220/255f, 240/255f),
                    new Color(255/255f, 255/255f, 255/255f),
                    new Color(160/255f, 160/255f, 180/255f))
                },
                { Brand.Mend, new BrandColorSet(
                    new Color(200/255f, 170/255f, 80/255f),
                    new Color(240/255f, 210/255f, 120/255f),
                    new Color(140/255f, 110/255f, 40/255f))
                },
                { Brand.Ruin, new BrandColorSet(
                    new Color(220/255f, 120/255f, 40/255f),
                    new Color(255/255f, 160/255f, 80/255f),
                    new Color(160/255f, 70/255f, 20/255f))
                },
                { Brand.Void, new BrandColorSet(
                    new Color(40/255f, 20/255f, 60/255f),
                    new Color(100/255f, 60/255f, 140/255f),
                    new Color(15/255f, 5/255f, 25/255f))
                }
            };
        }

        /// <summary>
        /// Get the color set for a brand.
        /// </summary>
        public BrandColorSet GetBrandColors(Brand brand)
        {
            if (_brandColors.TryGetValue(brand, out var colors))
            {
                return colors;
            }
            return _brandColors[Brand.Dread]; // Default fallback
        }

        /// <summary>
        /// Get the primary color for a brand.
        /// </summary>
        public Color GetBrandColor(Brand brand) => GetBrandColors(brand).primary;

        /// <summary>
        /// Get the glow color for a brand.
        /// </summary>
        public Color GetBrandGlow(Brand brand) => GetBrandColors(brand).glow;

        /// <summary>
        /// Get the dark color for a brand.
        /// </summary>
        public Color GetBrandDark(Brand brand) => GetBrandColors(brand).dark;

        // =============================================================================
        // CORRUPTION STATE COLORS
        // =============================================================================

        [Serializable]
        public struct CorruptionColorSet
        {
            public Color primary;
            public Color glow;
            public Color background;

            public CorruptionColorSet(Color primary, Color glow, Color background)
            {
                this.primary = primary;
                this.glow = glow;
                this.background = background;
            }
        }

        /// <summary>
        /// Get colors for a corruption state.
        /// </summary>
        public CorruptionColorSet GetCorruptionColors(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.Ascended => new CorruptionColorSet(
                    new Color(255/255f, 215/255f, 0),
                    new Color(255/255f, 240/255f, 100/255f),
                    new Color(255/255f, 215/255f, 0, 0.15f)),

                CorruptionState.Purified => new CorruptionColorSet(
                    new Color(180/255f, 210/255f, 255/255f),
                    new Color(220/255f, 240/255f, 255/255f),
                    new Color(180/255f, 210/255f, 255/255f, 0.15f)),

                CorruptionState.Unstable => new CorruptionColorSet(
                    new Color(160/255f, 160/255f, 170/255f),
                    new Color(200/255f, 200/255f, 210/255f),
                    new Color(160/255f, 160/255f, 170/255f, 0.15f)),

                CorruptionState.Corrupted => new CorruptionColorSet(
                    new Color(140/255f, 60/255f, 180/255f),
                    new Color(180/255f, 100/255f, 220/255f),
                    new Color(140/255f, 60/255f, 180/255f, 0.2f)),

                CorruptionState.Abyssal or CorruptionState.Untamed => new CorruptionColorSet(
                    new Color(60/255f, 0, 100/255f),
                    new Color(100/255f, 40/255f, 160/255f),
                    new Color(60/255f, 0, 100/255f, 0.3f)),

                _ => new CorruptionColorSet(
                    new Color(160/255f, 160/255f, 170/255f),
                    new Color(200/255f, 200/255f, 210/255f),
                    new Color(160/255f, 160/255f, 170/255f, 0.15f))
            };
        }

        /// <summary>
        /// Get corruption color based on percentage (0-100).
        /// </summary>
        public CorruptionColorSet GetCorruptionColorsFromPercent(float percent)
        {
            CorruptionState state = percent switch
            {
                <= 10 => CorruptionState.Ascended,
                <= 25 => CorruptionState.Purified,
                <= 50 => CorruptionState.Unstable,
                <= 75 => CorruptionState.Corrupted,
                _ => CorruptionState.Abyssal
            };
            return GetCorruptionColors(state);
        }

        // =============================================================================
        // RARITY COLORS
        // =============================================================================

        public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

        /// <summary>
        /// Get color for item/monster rarity.
        /// </summary>
        public Color GetRarityColor(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => new Color(180/255f, 180/255f, 180/255f),
                Rarity.Uncommon => new Color(80/255f, 180/255f, 80/255f),
                Rarity.Rare => new Color(80/255f, 140/255f, 220/255f),
                Rarity.Epic => new Color(160/255f, 80/255f, 200/255f),
                Rarity.Legendary => new Color(255/255f, 180/255f, 0),
                Rarity.Mythic => new Color(255/255f, 100/255f, 100/255f),
                _ => Color.white
            };
        }

        // =============================================================================
        // HEALTH COLORS
        // =============================================================================

        /// <summary>
        /// Get health bar color based on health percentage (0-1).
        /// </summary>
        public Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > 0.75f)
                return new Color(60/255f, 180/255f, 80/255f); // Full - Green
            if (healthPercent > 0.5f)
                return new Color(140/255f, 200/255f, 60/255f); // High - Yellow-green
            if (healthPercent > 0.25f)
                return new Color(220/255f, 180/255f, 40/255f); // Medium - Yellow
            if (healthPercent > 0.1f)
                return new Color(220/255f, 100/255f, 40/255f); // Low - Orange
            return new Color(200/255f, 40/255f, 40/255f); // Critical - Red
        }

        // =============================================================================
        // SURFACE COLORS
        // =============================================================================

        /// <summary>
        /// Get surface color by depth level (0-5).
        /// </summary>
        public Color GetSurfaceColor(int level)
        {
            return level switch
            {
                0 => new Color(8/255f, 6/255f, 12/255f),
                1 => new Color(15/255f, 12/255f, 22/255f),
                2 => new Color(25/255f, 20/255f, 35/255f),
                3 => new Color(38/255f, 30/255f, 50/255f),
                4 => new Color(50/255f, 40/255f, 65/255f),
                5 => new Color(65/255f, 52/255f, 82/255f),
                _ => new Color(25/255f, 20/255f, 35/255f)
            };
        }

        // =============================================================================
        // TEXT COLORS
        // =============================================================================

        public Color TextPrimary => new Color(235/255f, 225/255f, 215/255f);
        public Color TextSecondary => new Color(165/255f, 155/255f, 145/255f);
        public Color TextDisabled => new Color(75/255f, 68/255f, 65/255f);
        public Color TextHighlight => new Color(255/255f, 245/255f, 220/255f);
        public Color TextError => new Color(255/255f, 100/255f, 100/255f);
        public Color TextSuccess => new Color(100/255f, 200/255f, 100/255f);
        public Color TextWarning => new Color(255/255f, 200/255f, 80/255f);

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Convert Color to hex string.
        /// </summary>
        public string ColorToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        /// <summary>
        /// Get a color with modified alpha.
        /// </summary>
        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Lerp between two colors.
        /// </summary>
        public Color LerpColor(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }
    }
}

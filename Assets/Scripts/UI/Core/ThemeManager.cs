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
        private static bool _isQuitting = false;

        public static ThemeManager Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ThemeManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ThemeManager");
                        _instance = go.AddComponent<ThemeManager>();
                        DontDestroyOnLoad(go);
                    }
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

        private void OnApplicationQuit()
        {
            _isQuitting = true;
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

        private BrandColorSet[] _brandColorLookup;

        private void InitializeColors()
        {
            // Use array for O(1) lookup performance
            _brandColorLookup = new BrandColorSet[Enum.GetNames(typeof(Brand)).Length];
            
            SetBrandColors(Brand.IRON, new Color(0.55f, 0.59f, 0.65f), new Color(0.71f, 0.75f, 0.80f), new Color(0.31f, 0.35f, 0.39f));
            SetBrandColors(Brand.SAVAGE, new Color(0.71f, 0.18f, 0.18f), new Color(0.86f, 0.27f, 0.27f), new Color(0.47f, 0.10f, 0.10f));
            SetBrandColors(Brand.SURGE, new Color(0.24f, 0.55f, 0.86f), new Color(0.39f, 0.71f, 1.00f), new Color(0.12f, 0.31f, 0.55f));
            SetBrandColors(Brand.VENOM, new Color(0.31f, 0.71f, 0.24f), new Color(0.47f, 0.86f, 0.39f), new Color(0.16f, 0.39f, 0.12f));
            SetBrandColors(Brand.DREAD, new Color(0.47f, 0.24f, 0.63f), new Color(0.63f, 0.39f, 0.78f), new Color(0.27f, 0.12f, 0.39f));
            SetBrandColors(Brand.LEECH, new Color(0.55f, 0.16f, 0.31f), new Color(0.71f, 0.24f, 0.43f), new Color(0.35f, 0.08f, 0.20f));
            SetBrandColors(Brand.GRACE, new Color(0.86f, 0.86f, 0.94f), new Color(1.00f, 1.00f, 1.00f), new Color(0.63f, 0.63f, 0.71f));
            SetBrandColors(Brand.MEND, new Color(0.78f, 0.67f, 0.31f), new Color(0.94f, 0.82f, 0.47f), new Color(0.55f, 0.43f, 0.16f));
            SetBrandColors(Brand.RUIN, new Color(0.86f, 0.47f, 0.16f), new Color(1.00f, 0.63f, 0.31f), new Color(0.63f, 0.27f, 0.08f));
            SetBrandColors(Brand.VOID, new Color(0.16f, 0.08f, 0.24f), new Color(0.39f, 0.24f, 0.55f), new Color(0.06f, 0.02f, 0.10f));
        }

        private void SetBrandColors(Brand brand, Color primary, Color glow, Color dark)
        {
            _brandColorLookup[(int)brand] = new BrandColorSet(primary, glow, dark);
        }

        /// <summary>
        /// Get the color set for a brand.
        /// </summary>
        public BrandColorSet GetBrandColors(Brand brand)
        {
            int index = (int)brand;
            if (index >= 0 && index < _brandColorLookup.Length)
            {
                return _brandColorLookup[index];
            }
            return _brandColorLookup[(int)Brand.DREAD]; // Default fallback
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

        private CorruptionColorSet _ascendedColors = new CorruptionColorSet(new Color(1.00f, 0.84f, 0.00f), new Color(1.00f, 0.94f, 0.39f), new Color(1.00f, 0.84f, 0.00f, 0.15f));
        private CorruptionColorSet _purifiedColors = new CorruptionColorSet(new Color(0.71f, 0.82f, 1.00f), new Color(0.86f, 0.94f, 1.00f), new Color(0.71f, 0.82f, 1.00f, 0.15f));
        private CorruptionColorSet _unstableColors = new CorruptionColorSet(new Color(0.63f, 0.63f, 0.67f), new Color(0.78f, 0.78f, 0.82f), new Color(0.63f, 0.63f, 0.67f, 0.15f));
        private CorruptionColorSet _corruptedColors = new CorruptionColorSet(new Color(0.55f, 0.24f, 0.71f), new Color(0.71f, 0.39f, 0.86f), new Color(0.55f, 0.24f, 0.71f, 0.20f));
        private CorruptionColorSet _abyssalColors = new CorruptionColorSet(new Color(0.24f, 0.00f, 0.39f), new Color(0.39f, 0.16f, 0.63f), new Color(0.24f, 0.00f, 0.39f, 0.30f));

        /// <summary>
        /// Get colors for a corruption state.
        /// </summary>
        public CorruptionColorSet GetCorruptionColors(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.ASCENDED => _ascendedColors,
                CorruptionState.PURIFIED => _purifiedColors,
                CorruptionState.UNSTABLE => _unstableColors,
                CorruptionState.CORRUPTED => _corruptedColors,
                CorruptionState.ABYSSAL => _abyssalColors,
                _ => _unstableColors
            };
        }

        /// <summary>
        /// Get corruption color based on percentage (0-100).
        /// </summary>
        public CorruptionColorSet GetCorruptionColorsFromPercent(float percent)
        {
            if (percent <= 10) return _ascendedColors;
            if (percent <= 25) return _purifiedColors;
            if (percent <= 50) return _unstableColors;
            if (percent <= 75) return _corruptedColors;
            return _abyssalColors;
        }

        // =============================================================================
        // RARITY COLORS
        // =============================================================================

        private Color _rarityCommon = new Color(0.71f, 0.71f, 0.71f);
        private Color _rarityUncommon = new Color(0.31f, 0.71f, 0.31f);
        private Color _rarityRare = new Color(0.31f, 0.55f, 0.86f);
        private Color _rarityEpic = new Color(0.63f, 0.31f, 0.78f);
        private Color _rarityLegendary = new Color(1.00f, 0.71f, 0.00f);
        private Color _rarityMythic = new Color(1.00f, 0.39f, 0.39f);

        /// <summary>
        /// Get color for item/monster rarity.
        /// Uses VeilBreakers.Data.Rarity enum.
        /// </summary>
        public Color GetRarityColor(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.COMMON => _rarityCommon,
                Rarity.UNCOMMON => _rarityUncommon,
                Rarity.RARE => _rarityRare,
                Rarity.EPIC => _rarityEpic,
                Rarity.LEGENDARY => _rarityLegendary,
                Rarity.MYTHIC => _rarityMythic,
                _ => Color.white
            };
        }

        // =============================================================================
        // HEALTH COLORS
        // =============================================================================

        private Color _healthFull = new Color(0.24f, 0.71f, 0.31f);
        private Color _healthHigh = new Color(0.55f, 0.78f, 0.24f);
        private Color _healthMedium = new Color(0.86f, 0.71f, 0.16f);
        private Color _healthLow = new Color(0.86f, 0.39f, 0.16f);
        private Color _healthCritical = new Color(0.78f, 0.16f, 0.16f);

        /// <summary>
        /// Get health bar color based on health percentage (0-1).
        /// </summary>
        public Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > 0.75f) return _healthFull;
            if (healthPercent > 0.5f) return _healthHigh;
            if (healthPercent > 0.25f) return _healthMedium;
            if (healthPercent > 0.1f) return _healthLow;
            return _healthCritical;
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

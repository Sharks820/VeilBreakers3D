using System;
using UnityEngine;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Configuration and styling for Combat UI.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatUIConfig", menuName = "VeilBreakers/UI/Combat UI Config")]
    public class CombatUIConfig : ScriptableObject
    {
        // =============================================================================
        // COLORS
        // =============================================================================

        [Header("Panel Colors")]
        public Color panelBackground = new Color(0f, 0f, 0f, 0.6f);
        public Color textPrimary = new Color(0.96f, 0.90f, 0.83f, 1f); // #F5E6D3
        public Color textSecondary = new Color(0.75f, 0.75f, 0.75f, 1f); // #C0C0C0

        [Header("Health/MP Bars")]
        public Color hpFull = new Color(1f, 0f, 0f, 1f); // #FF0000
        public Color hpEmpty = new Color(0.55f, 0f, 0f, 1f); // #8B0000
        public Color mpFull = new Color(0.25f, 0.41f, 0.88f, 1f); // #4169E1
        public Color mpEmpty = new Color(0f, 0f, 0.55f, 1f); // #00008B

        [Header("Skill States")]
        public Color skillReady = Color.white;
        public Color skillCooldown = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color skillLowMP = new Color(0.5f, 0.5f, 1f, 1f);
        public Color skillInUse = new Color(1f, 1f, 0.8f, 1f);
        public Color cooldownOverlay = new Color(0f, 0f, 0f, 0.7f);

        [Header("Ultimate")]
        public Color ultimateReady = new Color(1f, 0.84f, 0f, 1f); // #FFD700 Gold
        public Color ultimateGlowPulse = new Color(1f, 0.84f, 0f, 0.5f);

        [Header("Corruption Colors")]
        public Color corruptionLow = new Color(0.30f, 0.69f, 0.31f, 1f); // #4CAF50 Green
        public Color corruptionMid = new Color(1f, 0.92f, 0.23f, 1f); // #FFEB3B Yellow
        public Color corruptionHigh = new Color(0.96f, 0.26f, 0.21f, 1f); // #F44336 Red

        [Header("Danger/Alerts")]
        public Color dangerPulse = new Color(1f, 0f, 0f, 1f);
        public Color captureReady = new Color(1f, 0.84f, 0f, 1f);

        // =============================================================================
        // SIZES
        // =============================================================================

        [Header("Panel Sizes (Base 1920x1080)")]
        public Vector2 playerPanelSize = new Vector2(220f, 90f);
        public Vector2 enemyPanelSize = new Vector2(280f, 70f);
        public Vector2 allyPanelSize = new Vector2(180f, 45f);
        public Vector2 menuIconSize = new Vector2(32f, 32f);
        public Vector2 skillIconSize = new Vector2(48f, 48f);

        [Header("Portrait Sizes")]
        public Vector2 playerPortraitSize = new Vector2(48f, 48f);
        public Vector2 enemyPortraitSize = new Vector2(48f, 48f);
        public Vector2 allyPortraitSize = new Vector2(32f, 32f);

        [Header("Bar Sizes")]
        public Vector2 playerHPBarSize = new Vector2(160f, 12f);
        public Vector2 playerMPBarSize = new Vector2(160f, 10f);
        public Vector2 enemyHPBarSize = new Vector2(180f, 12f);
        public Vector2 allyHPBarSize = new Vector2(80f, 8f);

        [Header("Icon Sizes")]
        public Vector2 statusIconSize = new Vector2(20f, 20f);
        public Vector2 allyStatusIconSize = new Vector2(16f, 16f);
        public Vector2 allySkillIconSize = new Vector2(20f, 20f);

        // =============================================================================
        // ANIMATION TIMINGS
        // =============================================================================

        [Header("Animation Durations")]
        public float cooldownSweepDuration = 1f; // Base, actual matches cooldown time
        public float hpChangeDuration = 0.3f;
        public float ultimateGlowDuration = 2f;
        public float lowHPPulseDuration = 1.5f;
        public float captureBannerBreatheDuration = 1.5f;
        public float skillReadyGlowDuration = 1f;
        public float targetPopupDuration = 0.15f;

        [Header("Animation Intensities")]
        [Range(1f, 1.1f)]
        public float captureBreathScale = 1.03f;
        [Range(0f, 1f)]
        public float lowHPThreshold = 0.25f;
        [Range(0f, 1f)]
        public float lowMPThreshold = 0.20f;

        // =============================================================================
        // FONTS
        // =============================================================================

        [Header("Font Sizes")]
        public int namesFontSize = 14;
        public int numbersFontSize = 12;
        public int labelsFontSize = 10;
        public int keybindsFontSize = 10;

        // =============================================================================
        // HELPERS
        // =============================================================================

        /// <summary>
        /// Get corruption color based on percentage.
        /// </summary>
        public Color GetCorruptionColor(float corruption)
        {
            if (corruption <= 25f) return corruptionLow;
            if (corruption <= 50f) return Color.Lerp(corruptionLow, corruptionMid, (corruption - 25f) / 25f);
            if (corruption <= 75f) return Color.Lerp(corruptionMid, corruptionHigh, (corruption - 50f) / 25f);
            return corruptionHigh;
        }

        /// <summary>
        /// Get HP bar color based on health percentage.
        /// </summary>
        public Color GetHPColor(float hpPercent)
        {
            return Color.Lerp(hpEmpty, hpFull, hpPercent);
        }

        /// <summary>
        /// Get MP bar color based on mana percentage.
        /// </summary>
        public Color GetMPColor(float mpPercent)
        {
            return Color.Lerp(mpEmpty, mpFull, mpPercent);
        }

        /// <summary>
        /// Get scale factor for current resolution.
        /// </summary>
        public static float GetScaleFactor()
        {
            float baseWidth = 1920f;
            float currentWidth = Screen.width;
            return currentWidth / baseWidth;
        }
    }

    /// <summary>
    /// Skill slot input bindings.
    /// </summary>
    [Serializable]
    public class SkillSlotBinding
    {
        public KeyCode keyCode;
        public string displayText;
        public int slotIndex;
    }

    /// <summary>
    /// Default skill slot bindings configuration.
    /// </summary>
    public static class CombatUIDefaults
    {
        public static readonly SkillSlotBinding[] DefaultBindings = new SkillSlotBinding[]
        {
            new SkillSlotBinding { keyCode = KeyCode.Q, displayText = "Q", slotIndex = 0 },  // Basic Attack
            new SkillSlotBinding { keyCode = KeyCode.E, displayText = "E", slotIndex = 1 },  // Defend
            new SkillSlotBinding { keyCode = KeyCode.Alpha1, displayText = "1", slotIndex = 2 },  // Skill 1
            new SkillSlotBinding { keyCode = KeyCode.Alpha2, displayText = "2", slotIndex = 3 },  // Skill 2
            new SkillSlotBinding { keyCode = KeyCode.Alpha3, displayText = "3", slotIndex = 4 },  // Skill 3
            new SkillSlotBinding { keyCode = KeyCode.Alpha4, displayText = "4", slotIndex = 5 },  // Skill 4 (optional)
            new SkillSlotBinding { keyCode = KeyCode.R, displayText = "R", slotIndex = 6 }   // Ultimate
        };

        public static readonly KeyCode[] AllyUltimateKeys = new KeyCode[]
        {
            KeyCode.F1,
            KeyCode.F2,
            KeyCode.F3
        };

        public const KeyCode TargetNextKey = KeyCode.Tab;
        public const KeyCode TargetPrevKey = KeyCode.Tab; // With Shift
        public const KeyCode CaptureKey = KeyCode.C;
        public const KeyCode QuickCommandKey = KeyCode.LeftShift;
    }
}

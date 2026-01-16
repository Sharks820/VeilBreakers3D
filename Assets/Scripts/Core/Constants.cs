using UnityEngine;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Global constants for VeilBreakers
    /// All magic numbers should be defined here
    /// </summary>
    public static class Constants
    {
        // =============================================================================
        // GAME SETTINGS
        // =============================================================================

        public const int MAX_PARTY_SIZE = 3;              // Monsters only (hero is separate)
        public const int MAX_LEVEL = 100;
        public const int MAX_CORRUPTION = 100;
        public const int BASE_EXP_REQUIRED = 100;
        public const float EXP_GROWTH_RATE = 1.15f;

        // =============================================================================
        // BATTLE SETTINGS
        // =============================================================================

        public const float BASE_CRIT_RATE = 0.05f;        // 5% base crit
        public const float BASE_CRIT_DAMAGE = 1.5f;       // 150% damage on crit
        public const float BASE_ACCURACY = 1.0f;          // 100% base accuracy
        public const float MIN_DAMAGE = 1f;               // Minimum damage dealt
        public const int MAX_STATUS_STACKS = 5;
        public const int MAX_SKILL_SLOTS = 8;

        // Turn order
        public const float SPEED_WEIGHT = 1.0f;
        public const float RANDOM_SPEED_VARIANCE = 0.1f;

        // Capture
        public const float BASE_CAPTURE_RATE = 0.1f;      // 10% base
        public const float LOW_HP_CAPTURE_BONUS = 0.3f;   // +30% when low HP
        public const float STATUS_CAPTURE_BONUS = 0.1f;   // +10% per status

        // =============================================================================
        // TIMING CONSTANTS (SECONDS)
        // =============================================================================

        // Wait durations
        public const float WAIT_MICRO = 0.1f;
        public const float WAIT_SHORT = 0.3f;
        public const float WAIT_STANDARD = 0.5f;
        public const float WAIT_MEDIUM = 0.75f;
        public const float WAIT_LONG = 1.0f;

        // UI animations
        public const float UI_BUTTON_HOVER = 0.15f;
        public const float UI_BUTTON_PRESS = 0.1f;
        public const float UI_SCENE_FADE = 0.3f;
        public const float UI_MENU_SLIDE = 0.25f;
        public const float UI_POPUP_ENTRANCE = 0.2f;
        public const float UI_TEXT_TYPEWRITER = 0.03f;

        // Battle animations
        public const float BATTLE_ATTACK_DURATION = 0.5f;
        public const float BATTLE_DAMAGE_FLASH = 0.15f;
        public const float BATTLE_DEATH_DURATION = 0.8f;
        public const float BATTLE_STATUS_DURATION = 0.3f;

        // =============================================================================
        // UI SCALES & SIZES
        // =============================================================================

        // Button sizes
        public static readonly Vector2 BUTTON_SMALL = new Vector2(120, 40);
        public static readonly Vector2 BUTTON_MEDIUM = new Vector2(200, 50);
        public static readonly Vector2 BUTTON_LARGE = new Vector2(300, 60);

        // Hover scales
        public const float HOVER_SCALE = 1.05f;
        public const float PRESS_SCALE = 0.95f;
        public const float NORMAL_SCALE = 1.0f;

        // =============================================================================
        // COLORS
        // =============================================================================

        // UI Colors
        public static readonly Color COLOR_GOLD = new Color(1f, 0.84f, 0f);
        public static readonly Color COLOR_SILVER = new Color(0.75f, 0.75f, 0.75f);
        public static readonly Color COLOR_BRONZE = new Color(0.8f, 0.5f, 0.2f);

        // Damage colors
        public static readonly Color COLOR_DAMAGE = new Color(1f, 0.3f, 0.3f);
        public static readonly Color COLOR_HEAL = new Color(0.3f, 1f, 0.3f);
        public static readonly Color COLOR_CRITICAL = new Color(1f, 0.8f, 0.2f);
        public static readonly Color COLOR_MISS = new Color(0.7f, 0.7f, 0.7f);

        // HP bar colors
        public static readonly Color HP_HIGH = new Color(0.2f, 0.8f, 0.3f);
        public static readonly Color HP_MEDIUM = new Color(0.9f, 0.7f, 0.2f);
        public static readonly Color HP_LOW = new Color(0.9f, 0.2f, 0.2f);

        // MP bar color
        public static readonly Color MP_COLOR = new Color(0.3f, 0.5f, 0.9f);

        // =============================================================================
        // RESOURCE PATHS
        // =============================================================================

        public const string DATA_PATH = "Data/";
        public const string MONSTERS_JSON = "Data/monsters";
        public const string SKILLS_JSON = "Data/skills";
        public const string HEROES_JSON = "Data/heroes";
        public const string ITEMS_JSON = "Data/items";

        public const string SPRITES_PATH = "Art/Sprites/";
        public const string AUDIO_PATH = "Audio/";
        public const string UI_PATH = "Art/UI/";

        // =============================================================================
        // TAGS & LAYERS
        // =============================================================================

        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_INTERACTABLE = "Interactable";
        public const string TAG_PROJECTILE = "Projectile";

        public const int LAYER_DEFAULT = 0;
        public const int LAYER_PLAYER = 8;
        public const int LAYER_ENEMY = 9;
        public const int LAYER_UI = 5;

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        /// <summary>
        /// Get HP bar color based on percentage
        /// </summary>
        public static Color GetHPColor(float percent)
        {
            if (percent > 0.5f)
                return HP_HIGH;
            if (percent > 0.25f)
                return HP_MEDIUM;
            return HP_LOW;
        }

        /// <summary>
        /// Calculate experience required for next level
        /// </summary>
        public static int GetExpForLevel(int level)
        {
            return Mathf.RoundToInt(BASE_EXP_REQUIRED * Mathf.Pow(EXP_GROWTH_RATE, level - 1));
        }
    }
}

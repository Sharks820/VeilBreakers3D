using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Audio priority layers for mixing.
    /// </summary>
    public enum AudioPriority
    {
        VOICE = 1,          // Ducks everything
        UI = 2,             // Always audible
        COMBAT_CRITICAL = 3,// Player hits, boss attacks
        COMBAT_GENERAL = 4, // Monster attacks, abilities
        MUSIC = 5,          // Ducks for voice
        AMBIENT = 6,        // Environment, weather
        FOLEY = 7           // Footsteps, rustling
    }

    /// <summary>
    /// Music layer states for adaptive music.
    /// </summary>
    public enum MusicState
    {
        EXPLORATION,    // Base ambient melody
        TENSION,        // Enemy nearby / pre-combat
        COMBAT_LOW,     // Battle started
        COMBAT_HIGH,    // Low HP / boss phase 2
        VICTORY,        // Battle won
        DEFEAT          // Party wiped
    }

    /// <summary>
    /// Audio compression tiers.
    /// </summary>
    public enum AudioCompression
    {
        UNCOMPRESSED,   // UI/Combat SFX - WAV
        HIGH,           // Monster attacks - Vorbis 192kbps
        MEDIUM_HIGH,    // Voice lines - Vorbis 128kbps
        MEDIUM,         // Ambient loops - Vorbis 96kbps
        STREAMING       // Music - Vorbis 160kbps
    }

    /// <summary>
    /// Configuration ScriptableObject for audio system settings.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "VeilBreakers/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        // =============================================================================
        // MEMORY BUDGETS
        // =============================================================================

        [Header("Memory Budgets (MB)")]
        [Tooltip("Core sounds: UI, VERA, combat basics")]
        public int budgetCore = 50;

        [Tooltip("Zone-specific: ambient, music, NPCs, monsters")]
        public int budgetZone = 100;

        [Tooltip("Combat: enemy sounds during battle")]
        public int budgetCombat = 75;

        [Tooltip("Voice: NPC dialogue banks")]
        public int budgetVoice = 25;

        /// <summary>Total memory budget in bytes.</summary>
        public long TotalBudgetBytes => (budgetCore + budgetZone + budgetCombat + budgetVoice) * 1024L * 1024L;

        // =============================================================================
        // VOLUME DEFAULTS
        // =============================================================================

        [Header("Default Volume Levels")]
        [Range(0f, 1f)] public float defaultMaster = 0.8f;
        [Range(0f, 1f)] public float defaultMusic = 0.7f;
        [Range(0f, 1f)] public float defaultSFX = 1.0f;
        [Range(0f, 1f)] public float defaultVoice = 1.0f;
        [Range(0f, 1f)] public float defaultAmbient = 0.6f;

        // =============================================================================
        // LOW HEALTH AUDIO
        // =============================================================================

        [Header("Low Health Audio")]
        [Tooltip("HP percentage where heartbeat starts")]
        [Range(0f, 0.5f)] public float lowHealthThreshold = 0.25f;

        [Tooltip("HP percentage for medium intensity")]
        [Range(0f, 0.25f)] public float mediumHealthThreshold = 0.15f;

        [Tooltip("HP percentage for critical intensity")]
        [Range(0f, 0.15f)] public float criticalHealthThreshold = 0.05f;

        // =============================================================================
        // PREDICTIVE LOADING
        // =============================================================================

        [Header("Predictive Loading")]
        [Tooltip("Seconds before zone boundary to preload")]
        public float zoneBoundaryPreloadTime = 10f;

        [Tooltip("Distance to preload NPC voices")]
        public float npcVoicePreloadDistance = 20f;

        [Tooltip("Seconds after combat to unload enemy sounds")]
        public float combatUnloadDelay = 30f;

        [Tooltip("Seconds after leaving zone to unload")]
        public float zoneUnloadDelay = 30f;

        // =============================================================================
        // 3D SPATIAL
        // =============================================================================

        [Header("3D Spatial Audio")]
        [Tooltip("Maximum distance for 3D sounds")]
        public float maxAudioDistance = 100f;

        [Tooltip("Distance for full volume")]
        public float minAudioDistance = 1f;

        [Tooltip("Rolloff mode for 3D sounds")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        // =============================================================================
        // VERA VOICE
        // =============================================================================

        [Header("VERA Voice Processing")]
        [Tooltip("Veil Integrity above this = clean voice")]
        [Range(0f, 100f)] public float veraCleanThreshold = 80f;

        [Tooltip("Veil Integrity above this = subtle glitches")]
        [Range(0f, 100f)] public float veraMildGlitchThreshold = 60f;

        [Tooltip("Veil Integrity above this = noticeable distortion")]
        [Range(0f, 100f)] public float veraDistortionThreshold = 40f;

        [Tooltip("Veil Integrity above this = dual voice")]
        [Range(0f, 100f)] public float veraDualVoiceThreshold = 20f;

        // =============================================================================
        // ACCESSIBILITY
        // =============================================================================

        [Header("Accessibility Defaults")]
        public bool defaultSubtitles = true;
        public bool defaultSpeakerLabels = true;
        public bool defaultSoundDescriptions = false;
        public bool defaultMonoAudio = false;
        public bool defaultReduceIntenseAudio = false;
        public bool defaultVisualizeAudioCues = false;

        // =============================================================================
        // PERFORMANCE
        // =============================================================================

        [Header("Performance")]
        [Tooltip("Maximum concurrent voices")]
        public int maxConcurrentVoices = 64;

        [Tooltip("Target latency for combat SFX (ms)")]
        public float combatLatencyTarget = 5f;

        [Tooltip("Target latency for voice (ms)")]
        public float voiceLatencyTarget = 50f;

        [Tooltip("Target bank load time (ms)")]
        public float bankLoadTimeTarget = 100f;

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        /// <summary>Core bank name (always loaded).</summary>
        public const string CORE_BANK = "Core";

        /// <summary>Zone bank prefix.</summary>
        public const string ZONE_BANK_PREFIX = "Zone_";

        /// <summary>Monster bank prefix.</summary>
        public const string MONSTER_BANK_PREFIX = "Monster_";

        /// <summary>NPC bank prefix.</summary>
        public const string NPC_BANK_PREFIX = "NPC_";

        /// <summary>Encounter bank prefix.</summary>
        public const string ENCOUNTER_BANK_PREFIX = "Encounter_";

        // =============================================================================
        // FMOD EVENT PATHS
        // =============================================================================

        [Header("FMOD Event Paths")]
        public string musicExplorationPath = "event:/Music/Exploration";
        public string musicCombatPath = "event:/Music/Combat";
        public string veraDialoguePath = "event:/Voice/VERA/Dialogue_";
        public string combatHitPath = "event:/SFX/Combat/Hit_";
        public string combatCriticalPath = "event:/SFX/Combat/Critical";
        public string combatBlockPath = "event:/SFX/Combat/Block";
        public string combatMissPath = "event:/SFX/Combat/Miss";
        public string uiConfirmPath = "event:/SFX/UI/Confirm";
        public string uiCancelPath = "event:/SFX/UI/Cancel";
        public string victoryStingerPath = "event:/Music/Stingers/Victory";
        public string defeatStingerPath = "event:/Music/Stingers/Defeat";
        public string heartbeatPath = "event:/SFX/Player/Heartbeat";

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        /// <summary>
        /// Get the low health intensity based on current HP percentage.
        /// </summary>
        public float GetLowHealthIntensity(float healthPercent)
        {
            if (healthPercent > lowHealthThreshold) return 0f;
            if (healthPercent <= criticalHealthThreshold) return 1f;
            if (healthPercent <= mediumHealthThreshold) return 0.7f;

            // Linear interpolation between threshold and medium
            return Mathf.InverseLerp(lowHealthThreshold, mediumHealthThreshold, healthPercent) * 0.7f;
        }

        /// <summary>
        /// Get VERA voice corruption level based on Veil Integrity.
        /// </summary>
        public float GetVERACorruptionLevel(float veilIntegrity)
        {
            if (veilIntegrity >= veraCleanThreshold) return 0f;
            if (veilIntegrity >= veraMildGlitchThreshold) return 0.2f;
            if (veilIntegrity >= veraDistortionThreshold) return 0.5f;
            if (veilIntegrity >= veraDualVoiceThreshold) return 0.8f;
            return 1f;
        }

        /// <summary>
        /// Get zone bank name from zone ID.
        /// </summary>
        public static string GetZoneBankName(string zoneId)
        {
            return $"{ZONE_BANK_PREFIX}{zoneId}";
        }

        /// <summary>
        /// Get monster bank name from monster ID.
        /// </summary>
        public static string GetMonsterBankName(string monsterId)
        {
            return $"{MONSTER_BANK_PREFIX}{monsterId}";
        }

        /// <summary>
        /// Get NPC bank name from NPC ID.
        /// </summary>
        public static string GetNPCBankName(string npcId)
        {
            return $"{NPC_BANK_PREFIX}{npcId}";
        }
    }
}

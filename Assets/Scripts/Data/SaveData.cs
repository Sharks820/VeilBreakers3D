using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Current save file version. Increment when save format changes.
    /// </summary>
    public static class SaveVersion
    {
        public const int CURRENT = 1;
    }

    /// <summary>
    /// Main save data structure containing all game state.
    /// Designed for JSON serialization with GZip compression and AES encryption.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // =============================================================================
        // META
        // =============================================================================

        /// <summary>Save file version for migration support</summary>
        public int version = SaveVersion.CURRENT;

        /// <summary>ISO 8601 timestamp when save was created</summary>
        public string saveDate;

        /// <summary>Total play time in seconds</summary>
        public float playtimeSeconds;

        /// <summary>Current area/location name for display</summary>
        public string currentLocation;

        /// <summary>Unique save identifier (for telemetry)</summary>
        public string saveId;

        // =============================================================================
        // HERO
        // =============================================================================

        /// <summary>Hero template ID (references HeroData)</summary>
        public string heroId;

        /// <summary>Player-chosen hero name</summary>
        public string heroName;

        /// <summary>Current hero level</summary>
        public int heroLevel = 1;

        /// <summary>Current HP (transient state)</summary>
        public int heroCurrentHp;

        /// <summary>Current MP (transient state)</summary>
        public int heroCurrentMp;

        /// <summary>Total experience points</summary>
        public int heroExperience;

        /// <summary>Path progression (0.0 to 1.0)</summary>
        public float heroPathLevel;

        /// <summary>Hero's chosen path</summary>
        public Path heroPath;

        /// <summary>List of learned skill IDs</summary>
        public List<string> heroLearnedSkills = new List<string>();

        // =============================================================================
        // PARTY (Monsters)
        // =============================================================================

        /// <summary>Active party monsters (max 3)</summary>
        public List<SavedMonster> party = new List<SavedMonster>();

        /// <summary>Monsters in storage</summary>
        public List<SavedMonster> storage = new List<SavedMonster>();

        // =============================================================================
        // WORLD STATE
        // =============================================================================

        /// <summary>IDs of discovered shrines (permanent unlock)</summary>
        public List<string> discoveredShrines = new List<string>();

        /// <summary>IDs of completed quests</summary>
        public List<string> completedQuests = new List<string>();

        /// <summary>Narrative story flags</summary>
        public List<string> storyFlags = new List<string>();

        /// <summary>Current currency amount</summary>
        public int currency;

        // =============================================================================
        // INVENTORY
        // =============================================================================

        /// <summary>Items in inventory</summary>
        public List<SavedItem> inventory = new List<SavedItem>();

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        /// <summary>
        /// Creates a new save data with default values and generated IDs.
        /// </summary>
        public static SaveData CreateNew(string heroId, string heroName, Path heroPath)
        {
            return new SaveData
            {
                version = SaveVersion.CURRENT,
                saveDate = DateTime.UtcNow.ToString("o"), // ISO 8601
                saveId = Guid.NewGuid().ToString("N"),
                playtimeSeconds = 0f,
                currentLocation = "Unknown",
                heroId = heroId,
                heroName = heroName,
                heroLevel = 1,
                heroCurrentHp = 100, // Will be overwritten by actual stats
                heroCurrentMp = 50,
                heroExperience = 0,
                heroPathLevel = 0f,
                heroPath = heroPath,
                heroLearnedSkills = new List<string>(),
                party = new List<SavedMonster>(),
                storage = new List<SavedMonster>(),
                discoveredShrines = new List<string>(),
                completedQuests = new List<string>(),
                storyFlags = new List<string>(),
                currency = 0,
                inventory = new List<SavedItem>()
            };
        }

        /// <summary>
        /// Updates the save timestamp to current time.
        /// </summary>
        public void UpdateTimestamp()
        {
            saveDate = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// Validates the save data for required fields.
        /// </summary>
        /// <returns>True if valid, false if corrupted/incomplete</returns>
        public bool Validate()
        {
            // Critical fields must exist
            if (string.IsNullOrEmpty(heroId)) return false;
            if (heroLevel < 1) return false;
            if (version < 1) return false;

            // Initialize null lists to empty (defensive)
            party ??= new List<SavedMonster>();
            storage ??= new List<SavedMonster>();
            discoveredShrines ??= new List<string>();
            completedQuests ??= new List<string>();
            storyFlags ??= new List<string>();
            heroLearnedSkills ??= new List<string>();
            inventory ??= new List<SavedItem>();

            return true;
        }

        /// <summary>
        /// Gets the strongest monster in the party by level.
        /// </summary>
        public SavedMonster GetStrongestMonster()
        {
            if (party == null || party.Count == 0) return null;

            SavedMonster strongest = party[0];
            foreach (var monster in party)
            {
                if (monster.level > strongest.level)
                {
                    strongest = monster;
                }
            }
            return strongest;
        }
    }

    /// <summary>
    /// Serializable monster data for save files.
    /// Stores only IDs and runtime values - stats are recalculated on load.
    /// </summary>
    [Serializable]
    public class SavedMonster
    {
        /// <summary>Monster template ID (references MonsterData)</summary>
        public string monsterId;

        /// <summary>Unique instance ID for this captured monster</summary>
        public string instanceId;

        /// <summary>Player-given nickname (empty = use default name)</summary>
        public string nickname;

        /// <summary>Current level</summary>
        public int level = 1;

        /// <summary>Current HP (transient state)</summary>
        public int currentHp;

        /// <summary>Current MP (transient state)</summary>
        public int currentMp;

        /// <summary>Corruption percentage (0-100)</summary>
        public float corruption;

        /// <summary>Total experience points</summary>
        public int experience;

        /// <summary>List of learned skill IDs</summary>
        public List<string> learnedSkills = new List<string>();

        /// <summary>
        /// Creates a new saved monster from capture.
        /// </summary>
        public static SavedMonster Create(string monsterId, int level, float corruption)
        {
            return new SavedMonster
            {
                monsterId = monsterId,
                instanceId = Guid.NewGuid().ToString("N"),
                nickname = "",
                level = level,
                currentHp = 100, // Will be recalculated
                currentMp = 50,
                corruption = corruption,
                experience = 0,
                learnedSkills = new List<string>()
            };
        }

        /// <summary>
        /// Gets display name (nickname if set, otherwise default).
        /// </summary>
        public string GetDisplayName(string defaultName)
        {
            return string.IsNullOrEmpty(nickname) ? defaultName : nickname;
        }
    }

    /// <summary>
    /// Serializable item data for save files.
    /// </summary>
    [Serializable]
    public class SavedItem
    {
        /// <summary>Item template ID (references ItemData)</summary>
        public string itemId;

        /// <summary>Stack quantity</summary>
        public int quantity = 1;

        public static SavedItem Create(string itemId, int quantity = 1)
        {
            return new SavedItem
            {
                itemId = itemId,
                quantity = quantity
            };
        }
    }

    /// <summary>
    /// Lightweight metadata for displaying save slots without loading full data.
    /// Extracted from the save file header.
    /// </summary>
    [Serializable]
    public class SaveSlotMetadata
    {
        /// <summary>Slot index (0-2 for manual, -1 for auto)</summary>
        public int slotIndex;

        /// <summary>True if slot has save data</summary>
        public bool hasData;

        /// <summary>Save file version</summary>
        public int version;

        /// <summary>Hero template ID for portrait lookup</summary>
        public string heroId;

        /// <summary>Player-chosen hero name</summary>
        public string heroName;

        /// <summary>Hero level</summary>
        public int heroLevel;

        /// <summary>Hero path for icon display</summary>
        public Path heroPath;

        /// <summary>Current location name</summary>
        public string currentLocation;

        /// <summary>Total playtime in seconds</summary>
        public float playtimeSeconds;

        /// <summary>Save timestamp</summary>
        public string saveDate;

        /// <summary>Strongest monster ID for portrait</summary>
        public string strongestMonsterId;

        /// <summary>Strongest monster level</summary>
        public int strongestMonsterLevel;

        /// <summary>True if save file appears corrupted</summary>
        public bool isCorrupted;

        /// <summary>Error message if corrupted</summary>
        public string corruptionError;

        /// <summary>
        /// Formats playtime as HH:MM:SS
        /// </summary>
        public string GetFormattedPlaytime()
        {
            var timeSpan = TimeSpan.FromSeconds(playtimeSeconds);
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// Formats save date for display
        /// </summary>
        public string GetFormattedDate()
        {
            if (DateTime.TryParse(saveDate, out var date))
            {
                return date.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
            }
            return saveDate ?? "Unknown";
        }

        /// <summary>
        /// Creates metadata from full save data.
        /// </summary>
        public static SaveSlotMetadata FromSaveData(SaveData data, int slotIndex)
        {
            var strongest = data.GetStrongestMonster();
            return new SaveSlotMetadata
            {
                slotIndex = slotIndex,
                hasData = true,
                version = data.version,
                heroId = data.heroId,
                heroName = data.heroName,
                heroLevel = data.heroLevel,
                heroPath = data.heroPath,
                currentLocation = data.currentLocation,
                playtimeSeconds = data.playtimeSeconds,
                saveDate = data.saveDate,
                strongestMonsterId = strongest?.monsterId,
                strongestMonsterLevel = strongest?.level ?? 0,
                isCorrupted = false,
                corruptionError = null
            };
        }

        /// <summary>
        /// Creates empty metadata for unused slot.
        /// </summary>
        public static SaveSlotMetadata Empty(int slotIndex)
        {
            return new SaveSlotMetadata
            {
                slotIndex = slotIndex,
                hasData = false
            };
        }

        /// <summary>
        /// Creates corrupted metadata.
        /// </summary>
        public static SaveSlotMetadata Corrupted(int slotIndex, string error)
        {
            return new SaveSlotMetadata
            {
                slotIndex = slotIndex,
                hasData = true,
                isCorrupted = true,
                corruptionError = error
            };
        }
    }
}

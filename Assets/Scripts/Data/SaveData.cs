using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Current save file version. Increment when save format changes.
    /// </summary>
    public static class SaveVersion
    {
        public const int CURRENT = 3;
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

        /// <summary>Path progression (0.0 to 100.0)</summary>
        public float heroPathLevel;

        /// <summary>Hero's chosen path</summary>
        public Path heroPath;

        /// <summary>List of learned skill IDs</summary>
        public List<string> heroLearnedSkills = new List<string>();

        /// <summary>Hero's equipped ability loadout — skill IDs mapped to the 6 AbilitySlots</summary>
        public List<SavedAbilitySlot> heroAbilityLoadout = new List<SavedAbilitySlot>();

        // =============================================================================
        // HERO EQUIPMENT
        // =============================================================================

        /// <summary>Hero weapon item ID (null = none)</summary>
        public string heroWeaponId;

        /// <summary>Hero armor item ID (null = none)</summary>
        public string heroArmorId;

        /// <summary>Hero accessory item ID (null = none)</summary>
        public string heroAccessoryId;

        /// <summary>Hero ring item ID (null = none)</summary>
        public string heroRingId;

        // =============================================================================
        // PARTY (Monsters)
        // =============================================================================

        /// <summary>Active party monsters (max 3)</summary>
        public List<SavedMonster> party = new List<SavedMonster>();

        /// <summary>Backpack monsters — quick-swap reserves (max 3)</summary>
        public List<SavedMonster> backpack = new List<SavedMonster>();

        /// <summary>Monsters in long-term storage</summary>
        public List<SavedMonster> storage = new List<SavedMonster>();

        // =============================================================================
        // WORLD STATE
        // =============================================================================

        /// <summary>IDs of discovered shrines (permanent unlock)</summary>
        public List<string> discoveredShrines = new List<string>();

        /// <summary>IDs of completed quests</summary>
        public List<string> completedQuests = new List<string>();

        /// <summary>Active quests with current objective progress</summary>
        public List<SavedActiveQuest> activeQuests = new List<SavedActiveQuest>();

        /// <summary>Narrative story flags</summary>
        public List<string> storyFlags = new List<string>();

        /// <summary>Current currency amount</summary>
        public int currency;

        /// <summary>Shrine ID where game was last saved (for respawn positioning)</summary>
        public string lastSaveShrineId;

        /// <summary>Player world position X at save time</summary>
        public float playerPositionX;

        /// <summary>Player world position Y at save time</summary>
        public float playerPositionY;

        /// <summary>Player world position Z at save time</summary>
        public float playerPositionZ;

        /// <summary>Player rotation Y (facing direction) at save time</summary>
        public float playerRotationY;

        /// <summary>Current scene/level name for loading the correct scene</summary>
        public string currentSceneName;

        /// <summary>IDs of defeated bosses (prevents respawn)</summary>
        public List<string> defeatedBosses = new List<string>();

        /// <summary>Unlocked fast travel destinations</summary>
        public List<string> unlockedFastTravel = new List<string>();

        // =============================================================================
        // MOUNTS
        // =============================================================================

        /// <summary>IDs of unlocked mount types</summary>
        public List<string> unlockedMounts = new List<string>();

        /// <summary>Currently equipped mount ID (null = on foot)</summary>
        public string activeMountId;

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
                currentSceneName = "",
                heroId = heroId,
                heroName = heroName,
                heroLevel = 1,
                heroCurrentHp = 100, // Will be overwritten by actual stats
                heroCurrentMp = 50,
                heroExperience = 0,
                heroPathLevel = 0f,
                heroPath = heroPath,
                heroLearnedSkills = new List<string>(),
                heroAbilityLoadout = new List<SavedAbilitySlot>(),
                heroWeaponId = null,
                heroArmorId = null,
                heroAccessoryId = null,
                heroRingId = null,
                party = new List<SavedMonster>(),
                backpack = new List<SavedMonster>(),
                storage = new List<SavedMonster>(),
                discoveredShrines = new List<string>(),
                completedQuests = new List<string>(),
                activeQuests = new List<SavedActiveQuest>(),
                storyFlags = new List<string>(),
                currency = 0,
                lastSaveShrineId = null,
                playerPositionX = 0f,
                playerPositionY = 0f,
                playerPositionZ = 0f,
                playerRotationY = 0f,
                defeatedBosses = new List<string>(),
                unlockedFastTravel = new List<string>(),
                unlockedMounts = new List<string>(),
                activeMountId = null,
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
        /// Validates the save data for required fields and repairs invalid values.
        /// Mutates state: initializes null lists, clamps out-of-range values, resets invalid enums.
        /// </summary>
        /// <returns>True if valid, false if corrupted/incomplete</returns>
        public bool ValidateAndRepair()
        {
            // Critical fields must exist
            if (string.IsNullOrEmpty(heroId)) return false;
            if (heroLevel < 1) return false;
            if (version < 1) return false;

            // Validate hero path enum
            if (!System.Enum.IsDefined(typeof(Path), heroPath)) heroPath = Path.NONE;

            // Repair missing/invalid hero name
            if (string.IsNullOrEmpty(heroName))
            {
                heroName = "Unknown Hero";
                ErrorLogger.Warn("[SaveData] Repaired missing heroName");
            }

            // Clamp playtime (negative values are invalid)
            if (playtimeSeconds < 0f)
            {
                playtimeSeconds = 0f;
                ErrorLogger.Warn("[SaveData] Repaired negative playtimeSeconds");
            }

            // Repair missing/invalid saveDate
            if (string.IsNullOrEmpty(saveDate) || !DateTime.TryParse(saveDate, out _))
            {
                saveDate = DateTime.UtcNow.ToString("o");
                ErrorLogger.Warn("[SaveData] Repaired missing/invalid saveDate");
            }

            // Repair missing currentLocation
            if (string.IsNullOrEmpty(currentLocation))
            {
                currentLocation = "Unknown";
            }

            // Repair missing saveId
            if (string.IsNullOrEmpty(saveId))
            {
                saveId = Guid.NewGuid().ToString("N");
                ErrorLogger.Warn("[SaveData] Repaired missing saveId");
            }

            // Clamp hero level to sane range
            if (heroLevel > 100) heroLevel = 100;

            // Clamp hero stats
            if (heroCurrentHp < 0) heroCurrentHp = 0;
            if (heroCurrentMp < 0) heroCurrentMp = 0;
            if (heroExperience < 0) heroExperience = 0;
            heroPathLevel = Mathf.Clamp(heroPathLevel, 0f, 100f);
            if (currency < 0) currency = 0;

            // Initialize null lists to empty (defensive)
            party ??= new List<SavedMonster>();
            backpack ??= new List<SavedMonster>();
            storage ??= new List<SavedMonster>();
            discoveredShrines ??= new List<string>();
            completedQuests ??= new List<string>();
            activeQuests ??= new List<SavedActiveQuest>();
            storyFlags ??= new List<string>();
            heroLearnedSkills ??= new List<string>();
            heroAbilityLoadout ??= new List<SavedAbilitySlot>();
            inventory ??= new List<SavedItem>();
            defeatedBosses ??= new List<string>();
            unlockedFastTravel ??= new List<string>();
            unlockedMounts ??= new List<string>();

            // Enforce party size limits (3 active, 3 backpack)
            while (party.Count > 3)
            {
                ErrorLogger.Warn("[SaveData] Party exceeds max size 3, moving overflow to backpack");
                backpack.Add(party[party.Count - 1]);
                party.RemoveAt(party.Count - 1);
            }
            while (backpack.Count > 3)
            {
                ErrorLogger.Warn("[SaveData] Backpack exceeds max size 3, moving overflow to storage");
                storage.Add(backpack[backpack.Count - 1]);
                backpack.RemoveAt(backpack.Count - 1);
            }

            // Validate all monsters in party, backpack, and storage
            ValidateMonsterList(party);
            ValidateMonsterList(backpack);
            ValidateMonsterList(storage);

            // Validate active quests
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = activeQuests[i];
                if (quest == null || string.IsNullOrEmpty(quest.questId))
                {
                    activeQuests.RemoveAt(i);
                    continue;
                }
                quest.objectiveProgress ??= new List<int>();
            }

            return true;
        }

        private static void ValidateMonsterList(List<SavedMonster> monsters)
        {
            if (monsters == null) return;
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                var monster = monsters[i];
                if (monster == null) { monsters.RemoveAt(i); continue; }
                if (string.IsNullOrEmpty(monster.monsterId))
                {
                    ErrorLogger.Error("[SaveData] Monster with null ID found, removing");
                    monsters.RemoveAt(i);
                    continue;
                }
                monster.corruption = Mathf.Clamp(monster.corruption, 0f, 100f);
                if (monster.level < 1) monster.level = 1;
                if (monster.level > 100) monster.level = 100;
                if (monster.currentHp < 0) monster.currentHp = 0;
                if (monster.currentMp < 0) monster.currentMp = 0;
                if (monster.experience < 0) monster.experience = 0;
                if (monster.evolutionStage < 0) monster.evolutionStage = 0;
                if (monster.evolutionStage > 2) monster.evolutionStage = 2;
                monster.learnedSkills ??= new List<string>();
                monster.abilityLoadout ??= new List<SavedAbilitySlot>();
                if (string.IsNullOrEmpty(monster.instanceId))
                {
                    monster.instanceId = Guid.NewGuid().ToString("N");
                    ErrorLogger.Warn($"[SaveData] Repaired missing instanceId for monster {monster.monsterId}");
                }
            }
        }

        /// <summary>
        /// Gets the strongest monster in the party by level.
        /// </summary>
        public SavedMonster GetStrongestMonster()
        {
            if (party == null || party.Count == 0) return null;

            SavedMonster strongest = null;
            foreach (var monster in party)
            {
                if (monster == null) continue;
                if (strongest == null || monster.level > strongest.level)
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

        /// <summary>Ability loadout — which learned skill is slotted in which AbilitySlot</summary>
        public List<SavedAbilitySlot> abilityLoadout = new List<SavedAbilitySlot>();

        /// <summary>Current evolution stage (0 = Birth, 1 = Evo2, 2 = Evo3)</summary>
        public int evolutionStage;

        /// <summary>Equipped accessory item ID (null = none)</summary>
        public string equippedAccessoryId;

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
                corruption = Mathf.Clamp(corruption, 0f, 100f),
                experience = 0,
                learnedSkills = new List<string>(),
                abilityLoadout = new List<SavedAbilitySlot>(),
                evolutionStage = 0,
                equippedAccessoryId = null
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
    /// Serializable ability slot assignment for save files.
    /// Maps an AbilitySlot to a skill ID.
    /// </summary>
    [Serializable]
    public class SavedAbilitySlot
    {
        /// <summary>AbilitySlot index (0-5, matching AbilitySlot enum)</summary>
        public int slotIndex;

        /// <summary>Skill ID assigned to this slot (empty = default/none)</summary>
        public string skillId;

        public static SavedAbilitySlot Create(int slot, string skillId)
        {
            return new SavedAbilitySlot { slotIndex = slot, skillId = skillId };
        }
    }

    /// <summary>
    /// Serializable active quest data for save files.
    /// Tracks in-progress quest state.
    /// </summary>
    [Serializable]
    public class SavedActiveQuest
    {
        /// <summary>Quest template ID (references QuestData)</summary>
        public string questId;

        /// <summary>Current objective index within the quest</summary>
        public int currentObjectiveIndex;

        /// <summary>Per-objective progress counters (e.g., 3/5 enemies killed)</summary>
        public List<int> objectiveProgress = new List<int>();

        /// <summary>ISO 8601 timestamp when quest was accepted</summary>
        public string acceptedDate;

        public static SavedActiveQuest Create(string questId)
        {
            return new SavedActiveQuest
            {
                questId = questId,
                currentObjectiveIndex = 0,
                objectiveProgress = new List<int>(),
                acceptedDate = DateTime.UtcNow.ToString("o")
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
        /// <summary>Slot index (0-2 for manual, -1/-2 for auto slots)</summary>
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

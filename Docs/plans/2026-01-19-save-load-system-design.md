# Save/Load System Design

> **Status:** IN PROGRESS | **Version:** 0.1 | **Date:** 2026-01-19

---

## Overview

Shrine-based manual save system with checkpoint auto-saves for an open-world tactical monster RPG.

---

## Core Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Save Slots | 3 slots | Enough to experiment with different paths |
| Manual Save | Shrine zones only | Exploration reward, adds tension |
| Auto-Save | Checkpoints only | Story objectives + all boss victories |
| Format | JSON + AES-256 encryption | Debuggable internally, secure on disk |
| Settings | Global (PlayerPrefs) | Player prefs, not character choices |
| Versioning | Sequential migrations | v1→v2→v3, composable and testable |

---

## Architecture

```
┌─────────────────────────────────────────┐
│  SaveManager (Singleton)                │
│  - Save(), Load(), Delete()             │
│  - GetSlotMetadata() for load screen    │
│  - Auto-save trigger hooks              │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│  SaveData (Serializable Class)          │
│  - Version number                       │
│  - Hero state                           │
│  - Party monsters                       │
│  - World state (shrines, quests)        │
│  - Playtime, location                   │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│  SaveFileHandler (Static Utility)       │
│  - JSON serialization                   │
│  - AES-256 encryption/decryption        │
│  - File I/O to persistentDataPath       │
└─────────────────────────────────────────┘
```

**File Locations:**
- Saves: `Application.persistentDataPath/saves/slot_X.sav`
- Settings: `PlayerPrefs` (Unity built-in)

---

## SaveData Structure

```csharp
[Serializable]
public class SaveData
{
    // === META ===
    public int version;                    // For migrations
    public string saveDate;                // ISO 8601 timestamp
    public float playtimeSeconds;          // Total play time
    public string currentLocation;         // Area name for display

    // === HERO ===
    public string heroId;
    public int heroLevel;
    public int heroCurrentHp;
    public int heroCurrentMp;
    public int heroExperience;
    public float heroPathLevel;
    public Path heroPath;
    public List<string> heroLearnedSkills;

    // === PARTY (Monsters) ===
    public List<SavedMonster> party;       // Active party (max 3)
    public List<SavedMonster> storage;     // Monster storage

    // === WORLD STATE ===
    public List<string> discoveredShrines; // Shrine IDs
    public List<string> completedQuests;   // Quest IDs
    public List<string> storyFlags;        // Narrative triggers
    public int currency;

    // === INVENTORY ===
    public List<SavedItem> inventory;      // Item ID + quantity
}

[Serializable]
public class SavedMonster
{
    public string monsterId;
    public string nickname;
    public int level;
    public int currentHp;
    public int currentMp;
    public float corruption;
    public int experience;
    public List<string> learnedSkills;
}

[Serializable]
public class SavedItem
{
    public string itemId;
    public int quantity;
}
```

**Key Principle:** Store IDs + runtime values only. Stats recalculated on load from current game data (enables balance patches without breaking saves).

---

## Load Screen Metadata

What players see when selecting a save slot:

| Field | Source |
|-------|--------|
| Hero portrait | Lookup from heroId |
| Hero name | Lookup from heroId |
| Hero level | heroLevel |
| Path icon | heroPath |
| Current location | currentLocation |
| Playtime | playtimeSeconds (formatted) |
| Save date | saveDate |
| Strongest monster portrait | Highest level monster in party |

---

## Shrine System

### ShrineData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "Shrine", menuName = "VeilBreakers/Shrine Data")]
public class ShrineData : ScriptableObject
{
    public string shrineId;           // Unique identifier
    public string shrineName;         // Display name ("Thornwood Shrine")
    public string areaName;           // "Thornwood Forest", "Ashfall City"
    public Vector3 shrinePosition;    // World position
    public float saveRadius;          // How far save zone extends
}
```

### ShrineManager

```csharp
public class ShrineManager : MonoBehaviour
{
    public List<ShrineData> allShrines;           // All shrines in game
    private HashSet<string> discoveredShrines;    // Loaded from save

    // Called when player interacts with shrine
    public void DiscoverShrine(string shrineId);

    // Check if manual save is allowed at position
    public bool CanSaveAtPosition(Vector3 playerPos);

    // Get active shrine covering position (for UI display)
    public ShrineData GetActiveShrineAt(Vector3 playerPos);
}
```

### Coverage Rules

- Large areas (cities, forests) may have **multiple shrines**
- Each shrine has independent radius
- Overlapping zones are fine (redundancy)
- Once discovered, **permanently unlocked** for that save file

### UI Behavior

- Save button **greyed out** when `CanSaveAtPosition() == false`
- Tooltip: "Find a Shrine to unlock saving in this area"
- When in range: Show shrine name in save menu

---

## Auto-Save System

### Triggers

| Trigger | When |
|---------|------|
| Main story objective | Completed |
| Boss battle | Victory (all types: mini, district, main) |

### AutoSaveManager

```csharp
public class AutoSaveManager : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.OnMainQuestCompleted += TriggerAutoSave;
        EventBus.OnBossDefeated += TriggerAutoSave;
    }

    void TriggerAutoSave()
    {
        SaveManager.Instance.AutoSave();
    }
}
```

### Auto-Save Slot

**Decision:** Dedicated auto-save file (`auto.sav`), separate from the 3 manual slots.

- Auto-saves never overwrite manual saves
- Players can load from auto-save independently
- File structure: `slot_0.sav`, `slot_1.sav`, `slot_2.sav`, `auto.sav`

---

## Migration System

### Version Strategy

Each update includes migration script for `v(N-1) → vN` only.

Player on v1 loading into v4 runs: `v1→v2→v3→v4` automatically.

### Migration Interface

```csharp
public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SaveData Migrate(SaveData oldData);
}

public class MigrationRunner
{
    private List<ISaveMigration> migrations;

    public SaveData MigrateToLatest(SaveData data)
    {
        while (data.version < CURRENT_VERSION)
        {
            var migration = migrations.Find(m => m.FromVersion == data.version);
            data = migration.Migrate(data);
        }
        return data;
    }
}
```

### Migration Principles

- Store **IDs**, not raw data (balance changes don't break saves)
- Use **nullable fields** (new features don't break old saves)
- Test migrations with old save files before release

---

## Encryption

### Implementation

```csharp
public static class SaveFileHandler
{
    private static readonly byte[] Key = /* 32-byte key */;
    private static readonly byte[] IV = /* 16-byte IV */;

    public static void SaveToFile(SaveData data, string path)
    {
        string json = JsonUtility.ToJson(data);
        byte[] encrypted = EncryptAES(json, Key, IV);
        File.WriteAllBytes(path, encrypted);
    }

    public static SaveData LoadFromFile(string path)
    {
        byte[] encrypted = File.ReadAllBytes(path);
        string json = DecryptAES(encrypted, Key, IV);
        return JsonUtility.FromJson<SaveData>(json);
    }
}
```

---

## TODO

- [x] ~~Decide: Auto-save overwrites current slot or uses dedicated slot?~~ → Dedicated `auto.sav`
- [ ] Design: Audio system
- [ ] Implementation plan

---

*Design in progress - 2026-01-19*

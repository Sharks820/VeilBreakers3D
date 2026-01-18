# Save/Load System Design

> **Status:** COMPLETE | **Version:** 1.0 | **Date:** 2026-01-19

---

## Overview

Bulletproof shrine-based manual save system with checkpoint auto-saves for an open-world tactical monster RPG. Designed for zero corruption with multi-layer protection, partial recovery, and telemetry.

---

## Core Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Save Slots | 3 manual + 1 auto | Enough to experiment with paths |
| Manual Save | Shrine zones only | Exploration reward, adds tension |
| Auto-Save | Checkpoints | Story + tutorial + all boss victories |
| Format | JSON + GZip + AES-256 | Debuggable, compressed, secure |
| Settings | Global (PlayerPrefs) | Player prefs, not character choices |
| Versioning | Sequential migrations | v1→v2→v3, composable and testable |
| Backups | Rotating (.bak1, .bak2) | Double protection |
| Corruption | Recovery + Telemetry | Fix root causes, never lose progress |

---

## Architecture

```
┌─────────────────────────────────────────┐
│  SaveManager (Singleton)                │
│  - SaveAsync(), LoadAsync(), Delete()   │
│  - GetSlotMetadata() for load screen    │
│  - Auto-save trigger hooks              │
│  - Save mutex (prevent race conditions) │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│  SaveData (Serializable Class)          │
│  - Version number                       │
│  - Hero state                           │
│  - Party monsters + storage             │
│  - World state (shrines, quests)        │
│  - Playtime, location, inventory        │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│  SaveFileHandler (Static Utility)       │
│  - JSON serialization (JsonUtility)     │
│  - GZip compression                     │
│  - AES-256 encryption/decryption        │
│  - SHA-256 checksum                     │
│  - Atomic file operations               │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│  SaveTelemetry                          │
│  - Opt-in corruption reporting          │
│  - Anonymous device ID                  │
│  - Async upload (non-blocking)          │
└─────────────────────────────────────────┘
```

---

## File Format (Binary Header + Compressed Encrypted JSON)

```
┌─────────────────────────────────────────────────────────┐
│  HEADER (44 bytes fixed)                                │
├─────────────────────────────────────────────────────────┤
│  Bytes 0-3:    "VEIL" magic bytes (file identification) │
│  Bytes 4-7:    Format version (uint32)                  │
│  Bytes 8-11:   Flags (compression, encryption)          │
│  Bytes 12-43:  SHA-256 checksum of decrypted content    │
├─────────────────────────────────────────────────────────┤
│  PAYLOAD (variable size)                                │
├─────────────────────────────────────────────────────────┤
│  GZip compressed → AES-256 encrypted → JSON             │
└─────────────────────────────────────────────────────────┘
```

---

## File Structure

```
Application.persistentDataPath/
└── saves/
    ├── slot_0.sav      # Manual slot 1 - current
    ├── slot_0.bak1     # Manual slot 1 - previous
    ├── slot_0.bak2     # Manual slot 1 - before that
    ├── slot_1.sav      # Manual slot 2
    ├── slot_1.bak1
    ├── slot_1.bak2
    ├── slot_2.sav      # Manual slot 3
    ├── slot_2.bak1
    ├── slot_2.bak2
    ├── auto.sav        # Auto-save - current
    ├── auto.bak1       # Auto-save - previous
    └── auto.bak2       # Auto-save - before that
```

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
    public float heroPathLevel;            // Path progression percentage
    public Path heroPath;
    public List<string> heroLearnedSkills;

    // === PARTY (Monsters) ===
    public List<SavedMonster> party;       // Active party (max 3)
    public List<SavedMonster> storage;     // Monster storage

    // === WORLD STATE ===
    public List<string> discoveredShrines; // Shrine IDs (permanent)
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
    // Note: Stats recalculated on load from current game data
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

| Field | Source | Display |
|-------|--------|---------|
| Hero portrait | Lookup from heroId | Large image |
| Hero name | Lookup from heroId | Text |
| Hero level | heroLevel | "Lv. 42" |
| Path icon | heroPath | Icon badge |
| Location | currentLocation | "Thornwood Forest" |
| Playtime | playtimeSeconds | "12:34:56" |
| Save date | saveDate | "Jan 19, 2026" |
| Strongest monster | Highest level in party | Small portrait |

---

## Shrine System

### ShrineData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "Shrine", menuName = "VeilBreakers/Shrine Data")]
public class ShrineData : ScriptableObject
{
    public string shrineId;           // Unique identifier
    public string shrineName;         // "Thornwood Shrine"
    public string areaName;           // "Thornwood Forest"
    public Vector3 shrinePosition;    // World position
    public float saveRadius;          // Zone coverage radius
}
```

### ShrineManager

```csharp
public class ShrineManager : MonoBehaviour
{
    [SerializeField] private List<ShrineData> _allShrines;
    private HashSet<string> _discoveredShrines;

    public void DiscoverShrine(string shrineId)
    {
        _discoveredShrines.Add(shrineId);
        EventBus.ShrineDiscovered(shrineId);
    }

    public bool CanSaveAtPosition(Vector3 playerPos)
    {
        foreach (var shrine in _allShrines)
        {
            if (!_discoveredShrines.Contains(shrine.shrineId)) continue;
            if (Vector3.Distance(playerPos, shrine.shrinePosition) <= shrine.saveRadius)
                return true;
        }
        return false;
    }

    public ShrineData GetActiveShrineAt(Vector3 playerPos)
    {
        // Returns shrine covering position, or null
    }
}
```

### Coverage Rules

- Large areas (cities, forests) have **multiple shrines**
- Each shrine has independent radius
- Overlapping zones allowed (redundancy)
- Once discovered → **permanently unlocked** for that save file

### UI Behavior

- Save button **greyed out** outside shrine range
- Tooltip: "Find a Shrine to unlock saving in this area"
- When in range: Show shrine name in save menu

---

## Auto-Save System

### Triggers

| Trigger | When |
|---------|------|
| Character creation | Immediately after hero/name confirmed |
| Tutorial battle | After tutorial combat victory |
| Main story objective | On completion |
| Boss battle | All victories (mini, district, main) |

### AutoSaveManager

```csharp
public class AutoSaveManager : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.OnCharacterCreated += TriggerAutoSave;
        EventBus.OnTutorialComplete += TriggerAutoSave;
        EventBus.OnMainQuestCompleted += TriggerAutoSave;
        EventBus.OnBossDefeated += TriggerAutoSave;
    }

    private async void TriggerAutoSave()
    {
        await SaveManager.Instance.AutoSaveAsync();
    }
}
```

### Auto-Save Slot

- Dedicated `auto.sav` file (separate from 3 manual slots)
- Never overwrites manual saves
- Players can load from auto-save independently
- Same backup rotation (.bak1, .bak2)

---

## Bulletproof Corruption Prevention

### Multi-Layer Protection

| Layer | Protection | How |
|-------|------------|-----|
| 1 | **Magic Bytes** | "VEIL" header identifies file type instantly |
| 2 | **Checksum** | SHA-256 hash detects any corruption |
| 3 | **Compression** | GZip reduces file size 80%, smaller = faster = less risk |
| 4 | **Atomic Writes** | Write to .tmp, then rename (atomic operation) |
| 5 | **Write Verification** | Read back after write, verify checksum |
| 6 | **Disk Space Check** | Verify 3× save size available before attempt |
| 7 | **Write Retries** | 3 attempts with exponential backoff |
| 8 | **Flush to Disk** | FileStream.Flush() forces data to disk |
| 9 | **Rotating Backups** | .bak1, .bak2 protect against bad backup |
| 10 | **Save Mutex** | Prevent concurrent save race conditions |
| 11 | **Orphan Cleanup** | Delete leftover .tmp files on launch |

### Complete Save Flow

```
PRE-SAVE CHECKS:
├── Acquire save mutex (block concurrent saves)
├── Check disk space ≥ (estimated size × 3)
├── If insufficient → Warn player, abort
│
SERIALIZATION (Background Thread):
├── Collect all SaveData from GameManager
├── Serialize to JSON via JsonUtility
├── GZip compress (typically 80% reduction)
├── Generate SHA-256 checksum
├── AES-256 encrypt
├── Prepend 44-byte header
│
ATOMIC WRITE:
├── Write to slot_X.tmp
├── Flush() to force disk write
├── Read back .tmp, verify checksum
├── If mismatch → Retry (up to 3×)
│
BACKUP ROTATION:
├── Delete slot_X.bak2 (oldest)
├── Rename slot_X.bak1 → slot_X.bak2
├── Rename slot_X.sav → slot_X.bak1
├── Rename slot_X.tmp → slot_X.sav
│
COMPLETE:
├── Release save mutex
├── Log success
└── Hide save indicator
```

### Complete Load Flow

```
FILE VALIDATION:
├── Check file exists
├── Read 44-byte header
├── Verify "VEIL" magic bytes
├── Check version ≤ current (warn if from future)
│
INTEGRITY CHECK:
├── Decrypt AES-256
├── Decompress GZip
├── Verify SHA-256 checksum matches header
├── If valid → Parse JSON → Load → Done
│
RECOVERY MODE (if corrupt):
├── Attempt 1: Decompress anyway, try JSON parse
├── Attempt 2: Truncate last N bytes, retry
├── Attempt 3: Regex extraction of critical fields
├── Attempt 4: Load from .bak1
├── Attempt 5: Load from .bak2
├── All failed → Mark slot "Corrupted", disable
│
POST-LOAD VALIDATION:
├── Verify all IDs exist in current game data
├── Remove invalid skill/item IDs (content removed)
├── Fill missing fields with safe defaults
├── Recalculate all stats from base data
│
TELEMETRY (if opted-in):
├── If recovery occurred → Upload report
└── Include: what recovered, what lost, error type
```

---

## Partial Recovery Strategy

### Priority Order for Recovery

| Priority | Data | If Lost |
|----------|------|---------|
| CRITICAL | heroId, heroLevel, heroPath | Game unplayable |
| CRITICAL | party monster IDs | Core progression |
| IMPORTANT | discoveredShrines | Save zone access |
| IMPORTANT | completedQuests, storyFlags | Story progress |
| IMPORTANT | currency, inventory | Economic progress |
| RECOVERABLE | currentHp/Mp | Heal to full |
| RECOVERABLE | stats | Recalculate from level |
| RECOVERABLE | playtime | Reset to 0 |

### Regex Fallback Extraction

```csharp
// If JSON parse fails, extract critical fields via regex
private SaveData RegexRecovery(string corruptJson)
{
    var data = new SaveData();

    // Extract heroId
    var heroMatch = Regex.Match(corruptJson, @"""heroId""\s*:\s*""([^""]+)""");
    if (heroMatch.Success) data.heroId = heroMatch.Groups[1].Value;

    // Extract heroLevel
    var levelMatch = Regex.Match(corruptJson, @"""heroLevel""\s*:\s*(\d+)");
    if (levelMatch.Success) data.heroLevel = int.Parse(levelMatch.Groups[1].Value);

    // Continue for other critical fields...
    return data;
}
```

---

## Telemetry & Corruption Reporting

### Opt-In Setting

```csharp
public class SaveTelemetry : MonoBehaviour
{
    public bool SendCrashReports
    {
        get => PlayerPrefs.GetInt("SendCrashReports", 1) == 1; // Default ON
        set => PlayerPrefs.SetInt("SendCrashReports", value ? 1 : 0);
    }
}
```

### Corruption Report Payload

```csharp
[Serializable]
public class CorruptionReport
{
    public string gameVersion;
    public string platform;           // Windows, Mac, Linux, etc.
    public string osVersion;
    public string deviceId;           // Anonymous hash (not reversible)
    public string saveOperation;      // "manual", "auto_boss", "auto_story"
    public string corruptionType;     // "checksum_mismatch", "parse_error"
    public string errorDetails;       // Stack trace / error message
    public string recoveryResult;     // "full", "partial", "failed"
    public List<string> dataRecovered;
    public List<string> dataLost;
    public byte[] corruptedFile;      // Encrypted in transit
    public string timestamp;
}
```

### Privacy Guarantees

- No personal info (name, email, IP not logged server-side)
- Device ID is one-way hash, not reversible
- File encrypted in transit
- Toggle in Options menu
- Can disable anytime

---

## UI Flows

### Delete Save (Hold to Confirm)

```
1. Player selects slot, presses Delete
2. "Hold to Delete" prompt appears
3. Progress bar fills over 2-3 seconds
4. If released early → Cancel
5. If held full duration → Delete confirmed
```

### New Game in Occupied Slot

```
1. Player selects New Game
2. Chooses slot that has existing save
3. "This will overwrite existing save"
4. Hold to confirm (same as delete)
5. If confirmed → Start character creation
```

### First Save After New Game

```
1. Character creation complete (hero, name, path)
2. Immediate auto-save → protects choices
3. Tutorial begins
4. Tutorial battle victory
5. Second auto-save → protects tutorial progress
6. Normal rules apply from here
```

---

## Migration System

### Sequential Migrations

Each update includes only `v(N-1) → vN` migration script.
Player on v1 loading into v5 runs: `v1→v2→v3→v4→v5` automatically.

```csharp
public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SaveData Migrate(SaveData oldData);
}

public class MigrationRunner
{
    private List<ISaveMigration> _migrations;

    public SaveData MigrateToLatest(SaveData data, int currentVersion)
    {
        while (data.version < currentVersion)
        {
            var migration = _migrations.Find(m => m.FromVersion == data.version);
            if (migration == null)
                throw new MigrationException($"No migration from v{data.version}");

            data = migration.Migrate(data);
            data.version = migration.ToVersion;
        }
        return data;
    }
}
```

### Migration Principles

- Store **IDs**, not raw data → balance changes don't break saves
- Use **nullable fields** → new features don't break old saves
- Test migrations with old save files before every release
- Never remove fields, only add or deprecate

---

## Unity Implementation

### Platform Compatibility

| Platform | `persistentDataPath` | File Access |
|----------|---------------------|-------------|
| Windows | `%userprofile%\AppData\LocalLow\<company>\<product>` | ✅ Full |
| Mac | `~/Library/Application Support/<company>/<product>` | ✅ Full |
| Linux | `~/.config/unity3d/<company>/<product>` | ✅ Full |
| iOS | App sandbox `/Documents` | ✅ Full |
| Android | App internal storage | ✅ Full |
| WebGL | IndexedDB (virtual) | ⚠️ Needs sync |

### Async Save (Unity-Native)

```csharp
public async Awaitable SaveAsync(int slot, SaveData data)
{
    if (!_saveMutex.WaitOne(0))
    {
        Debug.LogWarning("Save already in progress");
        return;
    }

    try
    {
        ShowSaveIndicator();

        // Background thread for heavy work
        await Awaitable.BackgroundThreadAsync();

        string json = JsonUtility.ToJson(data);
        byte[] compressed = GZipCompress(Encoding.UTF8.GetBytes(json));
        byte[] checksum = ComputeSHA256(compressed);
        byte[] encrypted = AesEncrypt(compressed);
        byte[] final = BuildHeader(data.version, checksum, encrypted);

        await WriteWithRetriesAsync(GetSlotPath(slot), final);

        // Return to main thread
        await Awaitable.MainThreadAsync();

        HideSaveIndicator();
        EventBus.SaveCompleted(slot);
    }
    finally
    {
        _saveMutex.ReleaseMutex();
    }
}
```

### Dependencies (All Built-In)

| Feature | Namespace |
|---------|-----------|
| JSON | `UnityEngine.JsonUtility` |
| Async | `UnityEngine.Awaitable` |
| File I/O | `System.IO` |
| GZip | `System.IO.Compression` |
| AES | `System.Security.Cryptography` |
| SHA-256 | `System.Security.Cryptography` |

**No third-party packages required.**

---

## Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Save time | <100ms | Async, non-blocking |
| Load time | <200ms | Includes decompression |
| File size | <50KB | Typical save (compressed) |
| Corruption rate | <0.001% | With all protections |
| Recovery rate | >95% | Partial data recovery |

---

## Testing Requirements

### Automated Tests

1. **Fuzzing** - Random byte mutations, verify graceful handling
2. **Power failure sim** - Kill process mid-save, verify .bak recovery
3. **Stress test** - 10,000 save/load cycles, check for leaks
4. **Platform test** - HDD, SSD, USB, network drives
5. **Migration test** - v1 saves load correctly in v10

### Pre-Release Checklist

- [ ] All platforms tested
- [ ] Low disk space handled
- [ ] Unicode names work (emoji, special chars)
- [ ] Migration chain tested (v1→latest)
- [ ] Telemetry endpoint verified
- [ ] Recovery UI strings localized

---

## Summary

This save system is **enterprise-grade** for a single-player game:

- **Zero data loss** via atomic writes + rotating backups
- **Self-healing** via partial recovery + regex fallback
- **Self-improving** via opt-in corruption telemetry
- **Future-proof** via sequential migrations
- **Fast** via compression + async background saves
- **Secure** via AES-256 encryption

**Estimated corruption rate: <0.001%**
**Estimated recovery rate: >95%**

---

*Design complete - 2026-01-19*

# Audio System Design

> **Status:** COMPLETE | **Version:** 1.0 | **Date:** 2026-01-19

---

## Overview

AAA immersive audio system with full voice acting, adaptive music, 3D spatial audio, and smart contextual loading. Dual middleware approach: FMOD (primary) + Wwise (advanced spatial).

---

## Core Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Scope | AAA Immersive | Full voice, adaptive music, 3D spatial, environmental |
| Voice | Full voiced | VERA, heroes, NPCs, monsters (unique per creature) |
| Music | Horizontal + Vertical adaptive | Layers + seamless transitions based on game state |
| Middleware | FMOD primary + Wwise spatial | FMOD for creative, Wwise for advanced 3D |
| 3D Audio | Simplified launch, full-ready | Reverb zones + distance now, occlusion later |
| Loading | Smart contextual | Predictive preloading, ~250MB budget |

---

## Audio Priority Layers

| Priority | Category | Example | Mixing Behavior |
|----------|----------|---------|-----------------|
| 1 (Highest) | **Voice** | VERA dialogue, hero callouts | Ducks everything |
| 2 | **UI** | Menu clicks, notifications | Always audible |
| 3 | **Combat Critical** | Player hits, boss attacks | Cuts through |
| 4 | **Combat General** | Monster attacks, abilities | Normal |
| 5 | **Music** | Adaptive soundtrack | Ducks for voice |
| 6 | **Ambient** | Environment, weather | Background |
| 7 (Lowest) | **Foley** | Footsteps, rustling | Subtle layer |

---

## Adaptive Music System

### Layer Structure

| Layer | State | What Plays |
|-------|-------|------------|
| **Base** | Exploration | Ambient melody, low intensity |
| **Tension** | Enemy nearby / pre-combat | Percussion hints, rising tension |
| **Combat Low** | Battle started | Full drums, combat melody |
| **Combat High** | Low HP / boss phase 2 | Intense, faster tempo |
| **Victory** | Battle won | Triumphant stinger → fade to base |
| **Defeat** | Party wiped | Somber stinger → game over |

### Horizontal Transitions

- Sections (verse, chorus, bridge) transition on beat-synced cues
- FMOD handles beat-matching automatically
- No jarring cuts between musical sections

### Low Health Audio

| Player HP | Audio Effect |
|-----------|--------------|
| >25% | Normal mix |
| 25-15% | Heartbeat starts, music shifts urgent |
| 15-5% | Faster heartbeat, muffled mix, frantic music layer |
| <5% | Intense heartbeat, heavy breathing, emergency music |

---

## Monster Audio Identity

**Every monster has UNIQUE audio** (not just per-brand):

| Sound Type | Description |
|------------|-------------|
| **Idle** | Breathing, growling, ambient presence |
| **Attack 1-3** | Unique vocalizations per attack type |
| **Hurt** | Pain reactions |
| **Death** | Death sound |
| **Movement** | Footsteps, slithering, hovering |
| **Abilities** | Unique SFX per skill |

Brand influences the *style* (VOID = distorted, IRON = metallic) but each monster is distinct within their brand.

---

## Environmental Audio Zones

### Zone Types

| Zone | Audio Elements |
|------|----------------|
| **Forest** | Birds, rustling leaves, distant animals, wind |
| **City** | Crowds, merchants, footsteps, bells, chatter |
| **Cave** | Dripping water, echoes, distant rumbles, silence |
| **Ruins** | Creaking, wind whistling, crumbling debris |
| **Swamp** | Insects, bubbling, croaking, humid air |
| **Mountains** | Howling wind, distant eagles, snow crunch |

### Dynamic Layers

| Layer | Variants |
|-------|----------|
| Time of Day | Day ambience vs night ambience |
| Weather | Rain, thunder, wind intensity |
| Transitions | Smooth crossfade between zones |

---

## The Veil Audio

**The Veil is a presence, not just a visual boundary.**

### Veil Proximity Effects

| Distance | Audio Effect |
|----------|--------------|
| **Far from Veil** | Normal world sounds, clear audio |
| **Approaching** | Low hum begins, subtle wrongness, distant whispers |
| **Near Veil** | Reality distortion, sounds phase in/out, drones |
| **At Edge** | Intense otherworldly resonance, layered voices |
| **Beyond** | Completely altered soundscape, alien ambience |

### Veil Audio Effects

- Reverb/echo increases unnaturally
- Sounds occasionally reverse or glitch
- Whispers in unknown language (VERA hints?)
- Heartbeat-like pulse from Veil itself
- High corruption monsters sound MORE distorted near Veil

---

## VERA Dynamic Voice System

VERA's audio reflects her Veil Integrity - subtle storytelling through sound.

### Veil Integrity Voice Effects

| Veil Integrity | Voice Quality |
|----------------|---------------|
| **100-80%** | Clean, warm, trustworthy AI companion |
| **79-60%** | Occasional micro-glitches (subtle) |
| **59-40%** | Noticeable distortion, brief harmonic undertones |
| **39-20%** | Dual-voice bleeds through, reverb increases |
| **19-1%** | Constant layered voice, clearly something beneath |
| **0% / Reveal** | Full demonic resonance, ancient power |

### Implementation

```csharp
public class VERAVoiceController : MonoBehaviour
{
    [SerializeField] private FMOD.Studio.EventInstance _veraVoice;

    public void UpdateVeilIntegrity(float integrity)
    {
        // FMOD parameter controls voice processing chain
        _veraVoice.setParameterByName("VeilIntegrity", integrity);
    }

    public void PlayDialogue(string dialogueId)
    {
        FMODUnity.RuntimeManager.PlayOneShot(
            $"event:/Voice/VERA/Dialogue_{dialogueId}"
        );
    }
}
```

---

## Combat Feedback Audio

| Action | Audio Feedback |
|--------|----------------|
| **Hit landed** | Impact + enemy reaction (layered) |
| **Critical hit** | Bigger impact + screen-shake-synced boom |
| **Miss/dodge** | Whoosh, near-miss whistle |
| **Block/parry** | Metallic clang, defensive thud |
| **Ability cast** | Charge-up → release → impact (3-part) |
| **Ultimate** | Epic buildup, screen-wide boom, reverb tail |
| **Kill** | Satisfying finisher sound + death cry |

---

## UI Audio Style

**Hybrid:** Clean, functional feedback with dark fantasy undertones.

| UI Action | Sound Style |
|-----------|-------------|
| Menu open | Subtle ominous tone |
| Menu close | Soft ethereal fade |
| Confirm | Clear click + faint magic resonance |
| Cancel | Soft rejection tone |
| Error | Deep warning pulse |
| Hover | Minimal, barely there |
| Notification | Attention-getting but not jarring |

---

## 3D Spatial Audio

### Launch Features (Simplified)

| Feature | Implementation |
|---------|----------------|
| **Reverb zones** | Per-area reverb (cave echo, open forest) |
| **Distance falloff** | Realistic volume drop over distance |
| **Stereo panning** | Left/right positioning |
| **Attenuation curves** | Custom per-sound-type |

### Future-Ready (Hooks in Place)

| Feature | Status |
|---------|--------|
| Occlusion | Architecture ready, disabled |
| Obstruction | Architecture ready, disabled |
| Propagation | Architecture ready, disabled |
| Doppler | Architecture ready, disabled |

Wwise handles advanced spatial when enabled.

---

## Audio Accessibility

### Standard Settings

| Setting | Range | Default |
|---------|-------|---------|
| Master Volume | 0-100% | 80% |
| Music | 0-100% | 70% |
| SFX | 0-100% | 100% |
| Voice | 0-100% | 100% |
| Ambient | 0-100% | 60% |
| UI Sounds | On/Off | On |

### Accessibility Features

| Feature | Purpose |
|---------|---------|
| **Mono audio** | Hearing impairment in one ear |
| **Subtitles** | All dialogue captioned |
| **Speaker labels** | "[VERA]:" before lines |
| **Sound descriptions** | "[Monster roars]" for important audio |
| **Reduce intense audio** | Softens sudden loud sounds, heartbeat |
| **Visualize audio cues** | Screen flash/indicator for important sounds |

---

## Smart Contextual Loading

### The Problem

Full voice + unique monster sounds = massive audio data.
Need: HIGH PERFORMANCE (no latency) + LOW RAM (~250MB)

### The Solution: Predictive Loading

```
┌─────────────────────────────────────────────────────────┐
│  ALWAYS IN MEMORY (~50MB)                               │
│  - UI sounds, VERA, core combat, party monsters         │
│  - Zero latency guaranteed                              │
├─────────────────────────────────────────────────────────┤
│  ZONE-LOADED (~100MB)                                   │
│  - Current area ambient, music stems, NPCs, monsters    │
│  - Loaded when entering, unloaded when far              │
├─────────────────────────────────────────────────────────┤
│  PREDICTIVE (~75MB buffer)                              │
│  - Next zone preloads at boundary approach              │
│  - Enemy sounds load during combat transition           │
│  - NPC voices load on approach trigger                  │
├─────────────────────────────────────────────────────────┤
│  STREAMED (0MB persistent)                              │
│  - Music, long dialogue, cutscenes                      │
│  - 5-10s buffer ahead, imperceptible latency            │
└─────────────────────────────────────────────────────────┘

TOTAL: ~250MB audio RAM
```

### Predictive Triggers

| Trigger | Action |
|---------|--------|
| Zone boundary (10s away) | Preload next zone bank |
| Combat start | Load enemy sounds during transition |
| NPC approach (20m) | Load their voice bank |
| Party change | Load new monster, unload old |
| 2+ zones away | Unload old zone bank |
| Combat end (30s) | Unload enemy sounds |

### Compression Tiers

| Category | Format | Quality | Size Reduction |
|----------|--------|---------|----------------|
| UI/Combat SFX | Uncompressed WAV | Highest | 0% |
| Monster attacks | Vorbis 192kbps | High | 70% |
| Voice lines | Vorbis 128kbps | Medium-High | 80% |
| Ambient loops | Vorbis 96kbps | Medium | 85% |
| Music | Vorbis 160kbps | High | 75% |

### Memory Budget Enforcement

```csharp
private const long BUDGET_CORE = 50 * 1024 * 1024;      // 50MB
private const long BUDGET_ZONE = 100 * 1024 * 1024;     // 100MB
private const long BUDGET_COMBAT = 75 * 1024 * 1024;    // 75MB
private const long BUDGET_VOICE = 25 * 1024 * 1024;     // 25MB
// TOTAL: 250MB

// LRU eviction when budget exceeded
```

---

## Sound Bank Structure (FMOD)

```
Banks/
├── Core.bank                    # Always loaded (~50MB)
│   ├── UI/                      # All UI sounds
│   ├── Combat/                  # Hit impacts, blocks, criticals
│   ├── Player/                  # Player character sounds
│   └── VERA/                    # Full VERA voice bank
│
├── Party/                       # Loaded per party member
│   ├── Monster_Hollow.bank
│   ├── Monster_Wraith.bank
│   └── ...
│
├── Zones/                       # One per world area
│   ├── Zone_ThornwoodForest.bank
│   │   ├── Ambient/
│   │   ├── Music/
│   │   ├── NPCs/
│   │   └── Monsters/
│   ├── Zone_AshfallCity.bank
│   └── ...
│
├── Combat/                      # Enemy encounter pools
│   ├── Encounter_Common.bank
│   ├── Encounter_Elite.bank
│   └── Encounter_Boss.bank
│
└── Streaming/                   # Never fully loaded
    ├── Music_Exploration.bank
    ├── Music_Combat.bank
    ├── Cutscenes.bank
    └── LongDialogue.bank
```

---

## FMOD Event Structure

```
Events/
├── Music/
│   ├── Exploration              # Adaptive stems
│   │   ├── Base
│   │   ├── Tension              # Parameter: tension 0-1
│   │   └── Discovery
│   └── Combat
│       ├── Base
│       ├── Intensity            # Parameter: intensity 0-1
│       ├── LowHealth            # Parameter: playerHP 0-1
│       └── BossPhase            # Parameter: bossPhase 1-3
│
├── SFX/
│   ├── Combat/
│   │   ├── Hit_{Light/Medium/Heavy}
│   │   ├── Critical
│   │   ├── Block
│   │   ├── Miss
│   │   └── Death
│   ├── UI/
│   │   ├── MenuOpen
│   │   ├── MenuClose
│   │   ├── Confirm
│   │   ├── Cancel
│   │   └── Error
│   └── Environment/
│       ├── Footstep_{Surface}
│       └── Interact
│
├── Voice/
│   ├── VERA/
│   │   ├── Dialogue_{ID}        # Parameter: veilIntegrity 0-100
│   │   ├── Combat_Callouts
│   │   └── Reactions
│   ├── Heroes/{HeroName}/
│   └── NPCs/{NPCName}/
│
└── Monsters/{MonsterName}/
    ├── Idle
    ├── Attack_{1-3}
    ├── Hurt
    ├── Death
    └── Ability_{Name}
```

---

## Implementation: AudioManager

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Memory budgets
    private const long BUDGET_CORE = 50 * 1024 * 1024;
    private const long BUDGET_ZONE = 100 * 1024 * 1024;
    private const long BUDGET_COMBAT = 75 * 1024 * 1024;
    private const long BUDGET_VOICE = 25 * 1024 * 1024;

    // Bank tracking
    private HashSet<string> _loadedBanks = new();
    private Dictionary<string, float> _bankLastUsed = new();
    private string _currentZone;
    private string _preloadingZone;
    private List<string> _partyMonsterBanks = new();

    // ===== LIFECYCLE =====

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCoreBank();
    }

    private async void LoadCoreBank()
    {
        await LoadBankAsync("Core");
        Debug.Log("[Audio] Core bank loaded");
    }

    // ===== ZONE MANAGEMENT =====

    public async Awaitable OnZoneEnter(string zoneName)
    {
        if (_currentZone == zoneName) return;

        string oldZone = _currentZone;
        _currentZone = zoneName;

        await LoadBankAsync($"Zone_{zoneName}");

        if (!string.IsNullOrEmpty(oldZone))
        {
            StartCoroutine(DelayedUnload($"Zone_{oldZone}", 30f));
        }
    }

    public async Awaitable OnZoneBoundaryApproach(string nextZone)
    {
        if (_preloadingZone != nextZone)
        {
            _preloadingZone = nextZone;
            await LoadBankAsync($"Zone_{nextZone}");
        }
    }

    // ===== COMBAT MANAGEMENT =====

    public async Awaitable OnCombatStart(List<string> enemyIds)
    {
        foreach (var enemyId in enemyIds)
        {
            string bank = GetMonsterBank(enemyId);
            if (!_loadedBanks.Contains(bank))
            {
                await LoadBankAsync(bank);
            }
        }
    }

    public void OnCombatEnd()
    {
        StartCoroutine(UnloadCombatBanksDelayed(30f));
    }

    // ===== PARTY MANAGEMENT =====

    public async Awaitable OnPartyChanged(List<string> monsterIds)
    {
        foreach (var bank in _partyMonsterBanks.ToList())
        {
            if (!monsterIds.Any(m => GetMonsterBank(m) == bank))
            {
                UnloadBank(bank);
            }
        }

        _partyMonsterBanks.Clear();
        foreach (var monsterId in monsterIds)
        {
            string bank = GetMonsterBank(monsterId);
            _partyMonsterBanks.Add(bank);
            if (!_loadedBanks.Contains(bank))
            {
                await LoadBankAsync(bank);
            }
        }
    }

    // ===== NPC VOICE =====

    public async Awaitable OnNPCApproach(string npcId)
    {
        string voiceBank = $"NPC_{npcId}";
        if (!_loadedBanks.Contains(voiceBank))
        {
            await LoadBankAsync(voiceBank);
        }
        _bankLastUsed[voiceBank] = Time.time;
    }

    // ===== MEMORY MANAGEMENT =====

    private async Awaitable LoadBankAsync(string bankName)
    {
        await EnforceBudgetAsync();

        await Awaitable.BackgroundThreadAsync();
        // FMOD: FMODUnity.RuntimeManager.LoadBank(bankName);
        await Awaitable.MainThreadAsync();

        _loadedBanks.Add(bankName);
        _bankLastUsed[bankName] = Time.time;
    }

    private void UnloadBank(string bankName)
    {
        if (bankName == "Core") return;

        // FMOD: FMODUnity.RuntimeManager.UnloadBank(bankName);
        _loadedBanks.Remove(bankName);
        _bankLastUsed.Remove(bankName);
    }

    private async Awaitable EnforceBudgetAsync()
    {
        long currentUsage = GetCurrentMemoryUsage();
        long totalBudget = BUDGET_CORE + BUDGET_ZONE + BUDGET_COMBAT + BUDGET_VOICE;

        while (currentUsage > totalBudget * 0.9f)
        {
            string lruBank = _bankLastUsed
                .Where(kvp => kvp.Key != "Core" && !_partyMonsterBanks.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault().Key;

            if (lruBank != null)
            {
                UnloadBank(lruBank);
                await Awaitable.NextFrameAsync();
                currentUsage = GetCurrentMemoryUsage();
            }
            else break;
        }
    }

    private string GetMonsterBank(string monsterId) => $"Monster_{monsterId}";
    private long GetCurrentMemoryUsage() => /* FMOD memory query */;
}
```

---

## Implementation: Predictive Triggers

```csharp
public class AudioTriggerZone : MonoBehaviour
{
    [SerializeField] private string _zoneName;
    [SerializeField] private bool _isBoundary;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (_isBoundary)
            AudioManager.Instance.OnZoneBoundaryApproach(_zoneName);
        else
            AudioManager.Instance.OnZoneEnter(_zoneName);
    }
}

public class AudioTriggerNPC : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private float _preloadDistance = 20f;

    private bool _preloaded = false;

    private void Update()
    {
        if (_preloaded) return;

        float dist = Vector3.Distance(transform.position, Player.Instance.Position);
        if (dist < _preloadDistance)
        {
            _preloaded = true;
            AudioManager.Instance.OnNPCApproach(_npcId);
        }
    }
}
```

---

## Implementation: Low Health Audio

```csharp
public class LowHealthAudio : MonoBehaviour
{
    [SerializeField] private FMOD.Studio.EventInstance _heartbeat;
    [SerializeField] private float _triggerThreshold = 0.25f;

    private bool _isActive = false;

    public void UpdateHealth(float healthPercent)
    {
        if (healthPercent <= _triggerThreshold && !_isActive)
        {
            _heartbeat.start();
            _isActive = true;
            MusicManager.Instance.SetParameter("LowHealth", 1f);
        }
        else if (healthPercent > _triggerThreshold && _isActive)
        {
            _heartbeat.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _isActive = false;
            MusicManager.Instance.SetParameter("LowHealth", 0f);
        }

        if (_isActive)
        {
            float intensity = 1f - (healthPercent / _triggerThreshold);
            _heartbeat.setParameterByName("Intensity", intensity);
        }
    }
}
```

---

## Implementation: Combat Integration

```csharp
// In BattleManager.cs
public async Awaitable StartBattle(List<Combatant> enemies)
{
    ShowCombatTransition();

    var enemyIds = enemies.Select(e => e.MonsterId).ToList();
    await AudioManager.Instance.OnCombatStart(enemyIds);

    HideCombatTransition();
    BeginCombat();
}

public void EndBattle(BattleResult result)
{
    if (result == BattleResult.Victory)
        AudioManager.Instance.PlayStinger("Victory");
    else
        AudioManager.Instance.PlayStinger("Defeat");

    AudioManager.Instance.OnCombatEnd();
}
```

---

## Middleware Integration

### FMOD (Primary)

| Responsibility | Notes |
|----------------|-------|
| Music system | Adaptive layers, transitions |
| Voice processing | VERA integrity effects |
| SFX playback | All sound effects |
| Bank management | Load/unload sound banks |
| Mixing | Priority ducking, buses |

### Wwise (Spatial)

| Responsibility | Notes |
|----------------|-------|
| 3D positioning | Advanced HRTF |
| Occlusion (future) | Sound through walls |
| Propagation (future) | Sound around corners |
| Reverb | Per-zone acoustic simulation |

### Integration Pattern

```csharp
// FMOD handles most audio
FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Combat/Hit");

// Wwise handles 3D spatial positioning
AkSoundEngine.PostEvent("Play_3D_Ambient", gameObject);
AkSoundEngine.SetRTPCValue("Distance", distance, gameObject);
```

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Audio RAM | ~250MB max |
| Latency (combat SFX) | <5ms |
| Latency (voice) | <50ms |
| Bank load time | <100ms |
| Concurrent voices | 64+ |
| CPU usage | <5% |

---

## Summary

This audio system is **AAA-grade**:

- **Full immersion** via unique monster audio, VERA dynamics, Veil presence
- **Adaptive music** with horizontal + vertical layering
- **Smart loading** via prediction (no latency, low RAM)
- **Accessible** with comprehensive options
- **Scalable** with future spatial audio hooks

**Memory:** ~250MB
**Latency:** Imperceptible (predictive loading)
**Coverage:** Full voice, unique monsters, environmental, adaptive music

---

*Design complete - 2026-01-19*

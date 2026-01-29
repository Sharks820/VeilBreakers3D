# VEILBREAKERS - Project Memory

> **SINGLE SOURCE OF TRUTH** | Version: **v4.25** | Last updated: 2026-01-28

---

## Project Overview

| Field | Value |
|-------|-------|
| Engine | **Unity 3D** (UI Toolkit) |
| Genre | AAA 3D Real-Time Tactical Monster RPG |
| Combat Style | Dragon Age: Inquisition action-forward |
| Art Style | Dark Fantasy Horror |
| GitHub | Sharks820/VeilBreakers3D |
| Branch | `backup/pre-unity6` |

---

## Core Game Systems

### 10-Brand Combat System

| Brand | Role | Strong Against (2x) | Weak Against (0.5x) |
|-------|------|---------------------|---------------------|
| **IRON** | Tank | SURGE, DREAD | SAVAGE, RUIN |
| **SAVAGE** | Melee Burst | IRON, MEND | LEECH, GRACE |
| **SURGE** | Ranged DPS | VENOM, LEECH | IRON, VOID |
| **VENOM** | DoT/Debuff | GRACE, MEND | SURGE, RUIN |
| **DREAD** | CC/Terror | SAVAGE, GRACE | IRON, VOID |
| **LEECH** | Drain Tank | SAVAGE, RUIN | SURGE, VENOM |
| **GRACE** | Battle Healer | VOID, RUIN | SAVAGE, VENOM |
| **MEND** | Ward Healer | VOID, LEECH | SAVAGE, VENOM |
| **RUIN** | AOE Devastator | IRON, VENOM | LEECH, GRACE |
| **VOID** | Chaos Mage | SURGE, DREAD | GRACE, MEND |

### 4 Veilbreaker Paths

| Path | Strong Synergy Brands | Weak Synergy Brands | Starter Hero | Locked Hero |
|------|----------------------|---------------------|--------------|-------------|
| IRONBOUND | IRON, MEND, LEECH | VOID, SAVAGE, RUIN | Bastion | Warden |
| FANGBORN | SAVAGE, VENOM, RUIN | GRACE, MEND, IRON | Rend | Vex |
| VOIDTOUCHED | VOID, DREAD, SURGE | IRON, GRACE, MEND | Marrow | Shade |
| UNCHAINED | All Neutral | None (flex) | Mirage | Flux |

### Path-Brand Synergy (Tiered)

| Tier | Requirement | Damage | Defense | Corruption | Combo? |
|------|-------------|--------|---------|------------|--------|
| **FULL** | 3/3 match | +8% | +8% | 0.5x | YES |
| **PARTIAL** | 2/3 match | +5% | +5% | 0.75x | NO |
| **NEUTRAL** | 0-1/3 match | +0% | +0% | 1.0x | NO |
| **ANTI** | Any Weak brand | +0% | +0% | 1.5x each | NO |

### Corruption System

| Range | Status | Effect |
|-------|--------|--------|
| 0-10% | ASCENDED | +25% all stats |
| 11-25% | Purified | +10% all stats |
| 26-50% | Unstable | Normal |
| 51-75% | Corrupted | -10% all stats |
| 76-79% | Abyssal | -20% all stats |
| **80-100%** | **UNTAMED** | Monster uncontrollable |

### Monster Evolution

**Pure/Hybrid:** 3 stages (Birth 1-25 → Evo2 26-50 → Evo3 51-100)
**PRIMAL:** 2 stages (Birth 1-35 → Evolved 36-109 → Overflow 110-120)

### Party Structure
- **3 Active** + **3 Backpack** + Unlimited Storage
- Swap cooldown: 3-5s for abilities, instant for basic attacks

### 6-Slot Ability Structure

| Slot | Type | Cooldown |
|------|------|----------|
| 1 | Basic Attack | None |
| 2 | Defend/Guard | None |
| 3 | Skill 1 | 4-6s |
| 4 | Skill 2 | 10-15s |
| 5 | Skill 3 | 18-25s |
| 6 | Ultimate | 45-90s |

### Capture System (Post-Battle)
```
Capture Chance = f(HP%, Corruption%, Item Tier) + QTE Bonus
```
Items: Veil Shard (+0%) → Crystal (+15%) → Core (+30%) → Heart (+50%)
Failure: Flee (low corrupt) or Berserk (high corrupt)

---

## Project Structure

```
Assets/
├── Scripts/           # C# code (VeilBreakers.* namespaces)
│   ├── Core/          # GameManager, EventBus, Constants, GameBootstrap
│   ├── Combat/        # BattleManager, DamageCalculator, Combatant
│   ├── Systems/       # BrandSystem, SynergySystem, CorruptionSystem, VERASystem
│   ├── AI/            # GambitController, GambitEvaluator, AIPersonality
│   ├── Capture/       # CaptureManager, QTEController, CaptureFormulaCalculator
│   ├── Commands/      # QuickCommandManager, RadialMenuController, TimeSlowController
│   ├── Managers/      # SaveManager, SettingsManager, AutoSaveManager, ShrineManager
│   ├── Audio/         # AudioManager, MusicManager, VERAVoiceController
│   ├── UI/            # UI controllers (Menus/, Combat/, Core/, Controls/, Effects/)
│   ├── Data/          # Enums, ScriptableObjects (MonsterData, HeroData, etc.)
│   ├── Utils/         # ObjectPool, Extensions, SingletonMonoBehaviour
│   ├── Editor/        # Editor scripts (TestArenaSetup, UITextSettingsSetup)
│   └── Test/          # Test scripts (CombatTests, SaveTests, etc.)
├── Art/               # Visual assets (3D_Models/, Textures/, VFX/)
├── Audio/             # Music/, SFX/, Voice/
├── Data/              # JSON data, ScriptableObjects
├── Prefabs/           # Prefab assets
├── Scenes/            # Unity scenes
└── UI/                # UI Toolkit (Styles/, Templates/)
Docs/                  # Documentation (plans/, MIGRATION_PLAN.md)
screenshots/           # Debug screenshots only
```

### Naming Conventions
- 3D Models: `[type]_[name]_[variant].fbx` (e.g., `monster_hollow_base.fbx`)
- Animations: `[character]@[action]_[variant].anim` (e.g., `hollow@attack_slash.anim`)
- Namespaces: `VeilBreakers.Core`, `.Combat`, `.Systems`, `.UI`, `.Data`

---

## C# Code Standards

```csharp
namespace VeilBreakers.Combat
{
    public class BattleManager : MonoBehaviour
    {
        private const int kMaxPartySize = 3;        // Constants: k prefix
        [SerializeField] private int _currentTurn;  // Private: _ prefix
        public int CurrentTurn => _currentTurn;     // Properties: PascalCase
        public event Action<int> OnTurnChanged;     // Events: On prefix
    }
}
```

**Patterns:**
- Singletons: `Instance` property with `DontDestroyOnLoad`
- Data: ScriptableObjects for monsters, skills, items
- Events: C# `Action<T>` or `UnityEvent` for Inspector binding
- Pooling: `ObjectPool<T>` for frequently spawned objects

---

## UI System (UI Toolkit)

| Screen | Controller | Status |
|--------|------------|--------|
| Main Menu | MainMenuController.cs | ✅ |
| Settings | SettingsPanelController.cs | ✅ |
| Character Select | CharacterSelectController.cs | ✅ |
| Inventory | InventoryController.cs | ✅ |
| Monster Collection | MonsterCollectionController.cs | ✅ |
| VERA Dialogue | VERADialogueController.cs | ✅ |

**USS Classes:** `.vb-` (core), `.menu-`, `.dialogue-`, `.vera-`, `.inventory-`, `.monster-`, `.rarity-`, `.corruption-`

**Rarity Colors:** Common (#555), Uncommon (#40ff40), Rare (#4080ff), Epic (#a040ff), Legendary (#ffa040), Mythic (#ff4080)

---

## MCP & Tools

### Local MCPs (6)
| MCP | Purpose |
|-----|---------|
| sequential-thinking | Complex problem solving |
| mcp-unity | Unity Editor control |
| github | PRs, issues, CI |
| blender | 3D modeling |
| image-process | Image manipulation |
| notion | Project management |

### Plugin MCPs
Serena (code), Context7 (Unity docs), Greptile (search), Episodic Memory, Chrome

### Custom Agents
unity-architect, unity-code-reviewer, unity-debugger, unity-performance-profiler, balance-analyzer, vera-dialogue-tester, bug-hunter, asset-generator, commit-helper, documentation-writer

---

## Critical Lessons (Don't Repeat)

### Unity Gotchas
- **NEVER** create files named `nul`, `con`, `prn`, `aux`, `com1-9`, `lpt1-9` (Windows reserved - causes infinite import loop)
- **NEVER** reference fonts that don't exist in project (UI disappears)
- **NEVER** use `Find()` or `FindObjectOfType()` in Update - cache references
- **NEVER** allocate in Update (no `new`, no LINQ, no string concat)
- Delete `Library/` folder to fix import loops (Unity rebuilds it)
- **CHECK** if Bootstrap/Manager components are ENABLED in scene (`m_Enabled: 1`) - disabled components = silent failure
- **AFTER deleting scripts**, check scene for orphan GameObjects with missing script references

### UI Toolkit Lessons
- Scrollbar positioning buggy in programmatic popups - hide scrollbar, use mouse wheel
- UXML files need valid asset GUIDs, not placeholders
- Set `alphaIsTransparency: 1` in texture .meta files for transparency
- Set `overflow: visible` on particle containers (not hidden)

### Save System
- Use `Path.ChangeExtension()` not `.Replace()` for file paths
- Add timeout to mutex operations (prevent silent failures)

---

## User Preferences

- Visual verification with screenshots
- Auto-commit + push on all commits
- AAA quality, pixel-perfect alignment
- Fresh Unity UI (don't port Godot patterns)
- **Use Sonnet** for primary work, research tasks, and agent spawns (Opus wastes tokens)
- **Art Style:** Grimdark Painterly / Dark Fantasy Stylized Realism

---

## Technical Pipelines

### Render Pipeline Upgrade
| Current | Target | When |
|---------|--------|------|
| Built-in (2022.3 LTS) | **URP** (Unity 6) | Before heavy VFX work |

### Asset Pipeline
```
Scenario (2D concept) → Tripo (2D→3D + auto-rig) → Cascadeur (physics animation) → Blender (polish) → Unity
```

### VFX Pipeline
- **Primary:** Unity VFX Graph (requires URP)
- **AI Assist:** God Mode AI
- **Docs:** `Docs/VFX_AI_TOOLS_2026.md`

### Map/Environment Pipeline
- **Terrain:** Gaea (FREE→$99 Indie) for 8K heightmaps
- **In-Engine:** MapMagic 2 (FREE) for procedural refinement
- **Props/Landmarks:** Scenario→Tripo (your existing pipeline)
- **Atmosphere:** Unity VFX Graph + URP fog/lighting
- **Docs:** `Docs/MAP_TERRAIN_AI_TOOLS_2026.md`

### 6 Biomes (Light → Red Veil Progression)

| # | Biome | Path | Brands | Theme |
|---|-------|------|--------|-------|
| 1 | **The Waking Shore** | Start | Mixed | Last light, fading hope |
| 2 | **The Ironmaw** | IRONBOUND | IRON, MEND, LEECH | Rusted fortresses, parasitic growth |
| 3 | **The Ravaged** | FANGBORN | SAVAGE, VENOM, RUIN | Primal carnage, toxic wastelands |
| 4 | **The Unbound** | UNCHAINED | GRACE + flex | Contested twilight, unstable |
| 5 | **The Hollowing** | VOIDTOUCHED | VOID, DREAD, SURGE | Reality tears, nightmare realm |
| 6 | **The Bleeding Veil** | Endgame | UNTAMED | Pure red veil energy, max corruption |

**Visual Progression:** Red veil energy intensifies from subtle hints (Biome 1) to overwhelming crimson (Biome 6)

---

## Design Documents

| Document | Location |
|----------|----------|
| Combat System | Docs/plans/2026-01-15-combat-system-design.md |
| Combat Implementation | Docs/plans/2026-01-17-combat-implementation-plan.md |
| Combat UI | Docs/plans/2026-01-17-combat-ui-design.md |
| MCP Arsenal | Docs/plans/2026-01-17-mcp-arsenal-design.md |
| Rigging/Animation | Docs/plans/2026-01-17-rigging-animation-facial-design.md |
| Gambits AI | Docs/plans/2026-01-18-gambits-ai-design.md |
| Status Effects | Docs/plans/2026-01-18-status-effects-design.md |
| Quick Commands | Docs/plans/2026-01-18-quick-command-design.md |
| Monster Capture | Docs/plans/2026-01-18-monster-capture-design.md |
| Save/Load | Docs/plans/2026-01-19-save-load-system-design.md |
| Audio System | Docs/plans/2026-01-19-audio-system-design.md |
| Hero Design | Docs/plans/2026-01-19-hero-character-design.md |
| Implementation | Docs/plans/2026-01-19-implementation-strategy.md |
| Migration | Docs/MIGRATION_PLAN.md |
| Character Select | Docs/plans/2026-01-27-character-select-design.md |

---

## Current Status

- **UI System:** Complete (all 6 screens)
- **Core Systems:** Implemented (Brand, Synergy, Corruption, EventBus)
- **Combat:** Framework ready, needs 3D integration
- **Title Screen:** Monster art with transparent PNG, lightning effects
- **Migration:** ~92% complete to Unity standards
- **Next:** Unity 6 upgrade prep

---

*Memory optimized v4.0 - Reduced from 1300+ lines to essential reference*

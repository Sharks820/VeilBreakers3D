# VEILBREAKERS - Project Memory

> **THE SINGLE SOURCE OF TRUTH** | Version: **v1.42** | Last updated: 2026-01-17

---

## Project Overview

| Field | Value |
|-------|-------|
| Engine | **Unity 3D** |
| Genre | AAA 3D Real-Time Tactical Monster RPG |
| Combat Style | Dragon Age: Inquisition action-forward |
| Art Style | Dark Fantasy Horror (3D models from 2D art) |
| Resolution | 1920x1080 |
| GitHub | Sharks820/VeilBreakers3D |

### Core Systems
- Real-time tactical combat with party command system
- Monster capturing (post-battle phase with QTE)
- VERA/VERATH demon-in-disguise system
- 4 Veilbreaker Paths (IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED)
- **10-Brand system** (complete redesign from 12-brand)
- Path/Brand synergy (buff-only, no penalties)
- Corruption system (affects monster obedience)

---

## 🎮 UNITY 3D PROJECT STRUCTURE

### Root Folders
```
VeilBreakers3D/
├── Assets/                    # ALL game assets
│   ├── Art/                   # Visual assets
│   │   ├── 2D_Reference/      # Original 2D art for 3D conversion
│   │   │   └── Monsters/      # 2D monster concept art
│   │   ├── 3D_Models/         # 3D model files
│   │   │   ├── Characters/    # Player, heroes
│   │   │   ├── Monsters/      # Monster 3D models
│   │   │   ├── Props/         # Environmental props
│   │   │   └── Weapons/       # Weapon models
│   │   ├── Animations/        # Animation clips
│   │   │   ├── Characters/    # Player/hero animations
│   │   │   ├── Monsters/      # Monster animations
│   │   │   └── Shared/        # Reusable animations
│   │   ├── Rigs/              # Rigging files
│   │   │   ├── Characters/    # Character rigs
│   │   │   └── Monsters/      # Monster rigs
│   │   ├── Materials/         # Material definitions
│   │   ├── Textures/          # Texture files
│   │   └── VFX/               # Visual effects
│   ├── Audio/                 # Sound assets
│   │   ├── Music/             # Background music
│   │   ├── SFX/               # Sound effects
│   │   └── Voice/             # Voice lines
│   ├── Data/                  # Game data (JSON, ScriptableObjects)
│   │   ├── Monsters/          # Monster definitions
│   │   ├── Skills/            # Skill definitions
│   │   ├── Items/             # Item definitions
│   │   └── Brands/            # Brand configurations
│   ├── Prefabs/               # Prefab assets
│   │   ├── Characters/        # Character prefabs
│   │   ├── Monsters/          # Monster prefabs
│   │   ├── UI/                # UI prefabs
│   │   └── VFX/               # Effect prefabs
│   ├── Scenes/                # Unity scenes
│   │   ├── Main/              # Core game scenes
│   │   ├── Battle/            # Combat scenes
│   │   └── Test/              # Testing scenes
│   ├── Scripts/               # C# scripts
│   │   ├── Core/              # Core systems
│   │   ├── Combat/            # Battle logic
│   │   ├── UI/                # UI controllers
│   │   ├── Monsters/          # Monster logic
│   │   ├── Characters/        # Character logic
│   │   └── Utils/             # Utility scripts
│   └── UI/                    # UI assets
│       ├── Sprites/           # UI sprites
│       ├── Fonts/             # Font files
│       └── Prefabs/           # UI prefabs
├── Docs/                      # Documentation
│   ├── ArtReference/          # Art style guides
│   ├── Design/                # Design documents
│   └── LEGACY_Godot/          # [OUTDATED] Old Godot docs
├── screenshots/               # In-game screenshots (debug)
├── CLAUDE.md                  # AI agent instructions
└── VEILBREAKERS.md            # This file (single source of truth)
```

### 3D Model Naming Convention
```
[type]_[name]_[variant].fbx
monster_hollow_base.fbx
monster_hollow_corrupted.fbx
hero_bastion_armor01.fbx
```

### Animation Naming Convention
```
[character]@[action]_[variant].anim
hollow@idle_loop.anim
hollow@attack_slash.anim
hollow@death_fall.anim
bastion@walk_forward.anim
```

### Rig File Naming
```
rig_[character]_[type].prefab
rig_hollow_generic.prefab
rig_bastion_humanoid.prefab
```

---

## 📸 SCREENSHOT PROTOCOL

**Location:** `screenshots/` (root folder)

**Naming Convention:**
```
screenshot_[date]_[description].png
screenshot_2026-01-15_battle_ui_test.png
screenshot_2026-01-15_monster_hollow_ingame.png
```

**Use Cases:**
- Visual verification of in-game assets
- UI layout debugging
- Before/after comparisons
- Bug documentation

**NEVER save screenshots to:** Assets/, Docs/, or project root

---

## 🛠️ UNITY DEVELOPMENT BEST PRACTICES

### C# Code Style (Unity Standard)
```csharp
// Namespace all scripts
namespace VeilBreakers.Combat
{
    // Class names: PascalCase
    public class BattleManager : MonoBehaviour
    {
        // Constants: PascalCase with k prefix
        private const int kMaxPartySize = 3;

        // Private fields: _camelCase
        [SerializeField] private int _currentTurn;

        // Public properties: PascalCase
        public int CurrentTurn => _currentTurn;

        // Events: PascalCase with On prefix
        public event Action<int> OnTurnChanged;

        // Methods: PascalCase
        public void StartBattle() { }
        private void ProcessTurn() { }
    }
}
```

### ScriptableObject Pattern (Use for Data)
```csharp
// Assets/Data/Monsters/
[CreateAssetMenu(fileName = "Monster", menuName = "VeilBreakers/Monster Data")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public Brand primaryBrand;
    public int baseHealth;
    public Sprite portrait;
    public GameObject modelPrefab;
}
```

### Required Namespaces
| Namespace | Purpose |
|-----------|---------|
| `VeilBreakers.Core` | Core systems, managers |
| `VeilBreakers.Combat` | Battle logic, damage |
| `VeilBreakers.Monsters` | Monster classes, AI |
| `VeilBreakers.Characters` | Player, heroes |
| `VeilBreakers.UI` | UI controllers |
| `VeilBreakers.Data` | ScriptableObjects, configs |
| `VeilBreakers.Utils` | Helper classes |

### Manager Pattern (Singleton)
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### Event System (Use UnityEvents for Inspector)
```csharp
// For inspector binding
[SerializeField] private UnityEvent<int> onDamageDealt;

// For code-only events
public event Action<Monster, int> OnMonsterDamaged;
```

### Prefab Workflow
1. Create prefab in `Assets/Prefabs/[Category]/`
2. Configure components in Inspector
3. Use prefab variants for differences
4. Never modify prefab instances directly (use overrides)

### Animation Controller Setup
```
Assets/Art/Animations/[Character]/
├── [Character]_AnimController.controller
├── [Character]@idle.anim
├── [Character]@walk.anim
├── [Character]@attack.anim
└── [Character]@death.anim
```

### Layer Management
| Layer | Purpose |
|-------|---------|
| Default | Environment |
| Player | Player character |
| Enemies | Enemy monsters |
| Allies | Allied monsters |
| UI | UI elements |
| Projectiles | Attacks |
| Triggers | Interaction zones |

### Tags
| Tag | Purpose |
|-----|---------|
| Player | Main player |
| Enemy | Enemy units |
| Ally | Allied units |
| Interactable | Clickable objects |
| Spawn | Spawn points |

### Input System (New Input System)
Use Unity's new Input System package for:
- Action maps: `Combat`, `UI`, `Exploration`
- Rebindable controls
- Multiple input device support

### Addressables (Recommended)
Use Addressables for:
- Monster models (load on demand)
- Audio files
- Large textures
- Level assets

---

## ⚠️ LEGACY: Godot Utilities (5,285 lines) - [OUTDATED]

> **⚠️ THIS SECTION IS FROM GODOT 2D - NOT APPLICABLE TO UNITY 3D**
> Kept for reference during transition. Do NOT use in Unity project.

### UIStyleFactory (889 lines) - `scripts/utils/ui_style_factory.gd` [GODOT ONLY]
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| `var label = Label.new()` + font overrides | `UIStyleFactory.create_label("text", FONT_NORMAL, COLOR_PARCHMENT)` |
| `add_theme_font_size_override("font_size", 14)` | Use `UIStyleFactory.FONT_*` constants |
| `add_theme_color_override("font_color", Color(...))` | Use `UIStyleFactory.COLOR_*` constants |
| `var bar = ProgressBar.new()` + styling | `UIStyleFactory.create_hp_bar()` or `create_mp_bar()` |
| `var style = StyleBoxFlat.new()` + manual setup | `UIStyleFactory.create_dark_panel()` or `create_panel_style()` |
| `control.mouse_filter = MOUSE_FILTER_PASS` | `UIStyleFactory.set_mouse_pass(control)` |
| `control.size_flags_horizontal = SIZE_EXPAND_FILL` | `UIStyleFactory.expand_horizontal(control)` |
| `var btn = Button.new()` + manual styling | `UIStyleFactory.create_button("text")` |

**Key Constants:** `FONT_TINY(9)`, `FONT_SMALL(10)`, `FONT_NORMAL(14)`, `FONT_HEADING(18)`, `FONT_HERO(42)`
**Key Colors:** `COLOR_PARCHMENT`, `COLOR_GOLD`, `COLOR_HP_VALUE`, `COLOR_MP_VALUE`, `COLOR_DAMAGE`, `COLOR_HEAL`

### AnimationEffects (783 lines) - `scripts/utils/animation_effects.gd`
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| Manual popup fade+scale tween | `AnimationEffects.popup_entrance(popup)` / `popup_exit(popup)` |
| Button hover scale+modulate tween | `AnimationEffects.button_hover(btn)` / `button_unhover(btn)` |
| `node.modulate = Color(1.5,...)` flash | `AnimationEffects.flash_white(node)` or `flash_color(node, color)` |
| Death fade sequence | `AnimationEffects.death_animation(sprite)` |
| `.set_ease(EASE_OUT).set_trans(TRANS_BACK)` | `AnimationEffects.ease_out_back(tween)` |
| `node.create_tween().set_parallel(true)` | `AnimationEffects.create_parallel_tween(node)` |
| Manual position tween | `AnimationEffects.move_to(node, pos)` or `knockback(node, dir)` |
| Looping modulate pulse | `AnimationEffects.color_pulse_loop(node, color)` |

**Key Functions:** `fade_in()`, `fade_out()`, `slide_in()`, `slide_out()`, `button_press()`, `skill_announcement()`

### NodeHelpers (385 lines) - `scripts/utils/node_helpers.gd`
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| `if is_instance_valid(node): node.queue_free()` | `NodeHelpers.safe_free(node)` |
| `for child in parent.get_children(): child.queue_free()` | `NodeHelpers.clear_children(parent)` |
| Manual type filtering in loops | `NodeHelpers.get_children_of_type(parent, Label)` |
| `if is_instance_valid(node) and node.visible` | `NodeHelpers.is_valid_visible(node)` |
| `node.visible = true` with validity check | `NodeHelpers.show(node)` / `hide(node)` |
| `scene.instantiate(); parent.add_child(inst)` | `NodeHelpers.instantiate_to(scene, parent)` |
| Manual signal connection with duplicate check | `NodeHelpers.safe_connect(source, "signal", callable)` |

### StringHelpers (304 lines) - `scripts/utils/string_helpers.gd`
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| `"%d/%d" % [hp, max_hp]` | `StringHelpers.format_hp(hp, max_hp)` |
| `"+%d" % value` or `"-%d" % value` | `StringHelpers.format_stat_change(value)` |
| `"%.0f%%" % (value * 100)` | `StringHelpers.format_percent(value)` |
| `"Lv. %d" % level` | `StringHelpers.format_level(level)` |
| `name.replace("_", " ").capitalize()` | `StringHelpers.enum_to_display(name)` |
| `"[color=#...]text[/color]"` | `StringHelpers.bbcode_color(text, color)` |
| Manual pluralization | `StringHelpers.pluralize(count, "item")` |

### MathHelpers (228 lines) - `scripts/utils/math_helpers.gd`
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| `float(hp) / float(max_hp)` | `MathHelpers.get_hp_percent(hp, max_hp)` |
| `clampf(value, 0.05, 0.95)` | `MathHelpers.clamp_probability(value)` |
| Division without zero check | `MathHelpers.safe_divide(a, b, default)` |
| Manual damage variance | `MathHelpers.apply_damage_variance(damage)` |

### Constants (635 lines) - `scripts/utils/constants.gd`
| ❌ DON'T DO THIS | ✅ DO THIS INSTEAD |
|------------------|-------------------|
| `await get_tree().create_timer(0.3).timeout` | `await get_tree().create_timer(Constants.WAIT_SHORT).timeout` |
| `await get_tree().create_timer(0.5).timeout` | `await get_tree().create_timer(Constants.WAIT_STANDARD).timeout` |
| Hardcoded animation duration | Use `Constants.ANIM_*` or `Constants.UI_*` |
| Hardcoded damage multipliers | Use `Constants.SKILL_*` or `Constants.DAMAGE_*` |

**Wait Constants:** `WAIT_INSTANT(0.1)`, `WAIT_QUICK(0.2)`, `WAIT_SHORT(0.3)`, `WAIT_STANDARD(0.5)`, `WAIT_LONG(0.8)`

---

## LEGACY: Main Menu UI [GODOT 2D - OUTDATED]

> **⚠️ These values are from Godot 2D project - rebuild in Unity**

### Logo
| Property | Value |
|----------|-------|
| Scale | 0.11 |
| Pulse Range | 0.11 - 0.112 |
| Position | (960, 160) |

### Buttons
| Property | Value |
|----------|-------|
| Size | 480x220 |
| Spacing | -200 (HBoxContainer separation) |
| Container Offset | left=-420, top=-320, right=1100, bottom=-100 |
| NewGame/Continue Y Offset | -20 (alignment fix) |
| Position | Right side over lava field, raised to cliff edge |

### Monster Eye Animation (Sprite Sheet)
| Property | Value |
|----------|-------|
| Sprite Sheet | assets/ui/demon_eyes_angular_stretch.png |
| Dimensions | 1536x1792 (6x7 grid) |
| Frame Count | 42 frames |
| Frame Size | 256x256 |
| Position | (480, 400) |
| Scale | 0.5 |
| Frame Rate | 12 FPS |
| Z-Index | 10 |
| Source Video | asset_xfXLxe1WPDrs9pbE4TvEjUPz.mp4 |
| Background | Transparent (white removed via Python PIL) |

---

## LEGACY: Battle System [GODOT 2D - OUTDATED]

> **⚠️ This was 2D turn-based. Transitioning to 3D real-time tactical.**

**Status: DEPRECATED - Rebuilding in Unity 3D**

### Architecture
- BattleManager, TurnManager, DamageCalculator
- StatusEffectManager, AIController
- 17 status effects (POISON, BURN, FREEZE, etc.)
- Lock-in turn order system (v2.8)

### Turn Order System (Lock-in)
```
1. PARTY LOCK-IN: All allies select actions (protagonist + AI monsters)
2. PARTY EXECUTION: All queued actions execute in order
3. ENEMY PHASE: Enemies get attack phases = alive party count
4. ROUND END: Return to step 1
```

**Key States:** PARTY_LOCK_IN, PARTY_EXECUTING, ENEMY_EXECUTING

### Damage Formula
```
power * ATK/DEF * level * element * variance * crits
```

### Battle UI (v4.1)
| Element | Size | Details |
|---------|------|---------|
| Party Sidebar | 170px wide | Left side, green HP/MP bars, death state fading |
| Enemy Sidebar | 170px wide | Right side, red HP bars, death state fading |
| Action Bar | 700px centered | Attack, Skills, Purify, Item, Defend, Flee (transparent bg) |
| Combat Log | 300x220 | Bottom-right corner, draggable, skill names shown |
| Player Sprite | 0.16 scale | Protagonist |
| Enemy Sprite | 0.15 scale | Regular monsters |
| Allied Monster | 0.14 scale | Captured monsters |
| Boss Sprite | 0.22 scale | Boss encounters |
| Turn Order | Top bar | Shows character names |

### Recent Fixes (Dec 2025)
- Lock-in turn system: party selects all actions before execution
- Turn order display with actual character portraits + arrows
- Enemy attacks = alive party members count
- Fixed stat modifier cleanup bug in StatusEffectManager
- Created KnockbackAnimator with multiple types (normal, critical, heavy, flinch, shake, death)
- AI healer targeting: +100 threat for healers, +30 for support
- AOE skill evaluation fixed: per-target bonus 15.0, base bonus 10.0
- Hit flash shader on damage with fallback modulate
- Screen shake: 12.0 intensity (crits), 4.0 (big hits)
- Status effect icons above characters with tooltips

---

## Heroes (Path-Based, No Brands)

| Hero | Path | Role | Signature Skills |
|------|------|------|------------------|
| Bastion | IRONBOUND | Tank | shield_bash, taunt, iron_wall, fortress_stance |
| Rend | FANGBORN | DPS | rending_strike, bloodletting, execute, frenzy |
| Marrow | VOIDTOUCHED | Healer | life_tap, siphon_heal, essence_transfer, life_link |
| Mirage | UNCHAINED | Illusionist | minor_illusion, fear_touch, mirror_image, mass_confusion |

### Path-Brand Synergy (v7.0 - TIERED)

**Core Philosophy:** Synergy = BUFF. Non-synergy = NEUTRAL. Partial synergy now rewards building toward full composition.

| Tier | Requirement | Damage | Defense | Corruption | Combo? |
|------|-------------|--------|---------|------------|--------|
| **FULL** | 3/3 monsters match | +8% | +8% | 0.5x | ✅ YES |
| **PARTIAL** | 2/3 monsters match | +5% | +5% | 0.75x | ❌ No |
| **NEUTRAL** | 0-1/3 monsters match | +0% | +0% | 1.0x | ❌ No |
| **ANTI** | Any Weak brand | +0% | +0% | 1.5x each | ❌ No |

| Path | Strong Synergy Brands | Weak Synergy Brands |
|------|----------------------|---------------------|
| IRONBOUND | IRON, MEND, LEECH | VOID, SAVAGE, RUIN |
| FANGBORN | SAVAGE, VENOM, RUIN | GRACE, MEND, IRON |
| VOIDTOUCHED | VOID, DREAD, SURGE | IRON, GRACE, MEND |
| UNCHAINED | All Neutral | None (flex path) |

### Combo Abilities (Require FULL 3/3 Synergy)

| Path | Combo | Effect | Cooldown |
|------|-------|--------|----------|
| IRONBOUND | Bulwark Formation | +12% defense, 25% damage redirects to tank, 6s | 60s |
| FANGBORN | Blood Frenzy | +10% damage, attacks heal 3% dealt, 5s | 60s |
| VOIDTOUCHED | Reality Fracture | 15% chance reset cooldowns on ability use, 8s | 75s |
| UNCHAINED | Adaptive Surge | Next ability copies to random ally at 40% (instant) | 60s |

- Heroes get Paths (not Brands) - synergize with multiple monster brands
- 15 stats per character with growth rates
- Equipment: weapon, armor, 2 accessories

---

## 10-Brand Combat System (v6.0 - NEW)

### The 10 Brands

| Brand | Role | Archetype | Primary Stat |
|-------|------|-----------|--------------|
| **IRON** | Tank | Defensive Wall | Defense |
| **SAVAGE** | Melee Burst | Berserker | Attack |
| **SURGE** | Ranged DPS | Artillery | Attack |
| **VENOM** | DoT/Debuff | Poison Master | Effect Power |
| **DREAD** | CC/Terror | Fear Mage | Control |
| **LEECH** | Drain Tank | Lifesteal Bruiser | Sustain |
| **GRACE** | Battle Healer | Combat Medic | Healing |
| **MEND** | Ward Healer | Shield Support | Healing |
| **RUIN** | AOE Devastator | Explosion Mage | AOE Damage |
| **VOID** | Chaos Mage | Reality Warper | Chaos/Random |

### Brand Effectiveness Matrix

Each brand deals **2x damage** to 2 brands, **0.5x damage** to 2 brands, and **1x damage** to 6 brands.

| Attacker | Strong Against (2x) | Weak Against (0.5x) |
|----------|---------------------|---------------------|
| IRON | SURGE, DREAD | SAVAGE, RUIN |
| SAVAGE | IRON, MEND | LEECH, GRACE |
| SURGE | VENOM, LEECH | IRON, VOID |
| VENOM | GRACE, MEND | SURGE, RUIN |
| DREAD | SAVAGE, GRACE | IRON, VOID |
| LEECH | SAVAGE, RUIN | SURGE, VENOM |
| GRACE | VOID, RUIN | SAVAGE, VENOM |
| MEND | VOID, LEECH | SAVAGE, VENOM |
| RUIN | IRON, VENOM | LEECH, GRACE |
| VOID | SURGE, DREAD | GRACE, MEND |

### Universal Monster Actions

All monsters have these regardless of Brand:

| Action | Effect | Cooldown |
|--------|--------|----------|
| Basic Attack | Brand-flavored auto-attack | None |
| Defend Self | 50% damage reduction | None |
| Guard Ally | Intercept, 75% damage redirected | None |
| Guard Champion | Intercept, 100% damage taken | None |

### 6-Slot Ability Structure

| Slot | Type | Cooldown | Purpose |
|------|------|----------|---------|
| 1 | Basic Attack | None | Always available damage |
| 2 | Defend/Guard | None | Always available defense |
| 3 | Skill 1 | 4-6 seconds | Spammable utility |
| 4 | Skill 2 | 10-15 seconds | Core rotation |
| 5 | Skill 3 | 18-25 seconds | Situational power |
| 6 | Ultimate | 45-90 seconds | Fight-changer |

### Party Structure
- **3 Active** + **3 Backpack** + Unlimited Storage
- Swap cooldown: 3-5 seconds (for abilities)
- Basic attacks/defense available immediately on swap

---

## Evolution & Leveling System (v5.0 - LOCKED)

### Pure/Hybrid Monsters (3 Stages)

| Stage | Level Range | XP Multiplier | Stat Growth/Level |
|-------|-------------|---------------|-------------------|
| Birth | 1-25 | 1.0x | Normal |
| Evo 2 | 26-50 | 1.5x (slower) | +15% bonus |
| **Evo 3** | 51-100 | **2.5x (much slower)** | **+30% bonus** |

**Evo 3 Pure Brand Bonus:**
- Brand bonus increases to 120% (was 100%)
- Unlocks ultimate skill
- Visual transformation (aura, size, effects)

### PRIMAL Monsters (2 Stages)

| Stage | Level Range | XP Multiplier | Stat Growth/Level |
|-------|-------------|---------------|-------------------|
| Birth | 1-35 | **0.6x (very fast)** | Normal |
| **Evolved** | 36-109 | **0.8x (fast)** | +10% bonus |
| **Overflow** | 110-120 | 1.0x | **+5% ALL stats/level** |

**PRIMAL Bonuses:**
- Level cap: 120 (vs 100 for Pure/Hybrid)
- No Path weakness (neutral to all paths)
- Both brands count as PRIMARY for skill scaling (50%+50%)
- Overflow stats Lv110-120: +50% all stats total
- Brands assigned at Evolution based on monster behavior

### Endgame Power Comparison (~150 hours)

| Monster Type | Likely Level | Strength |
|--------------|--------------|----------|
| Pure Evo 3 | ~85 | MASSIVE stats, ultimate skills, specialist |
| Hybrid Evo 3 | ~88 | Strong stats, versatile coverage |
| PRIMAL Evolved | ~115 | Highest level, no weakness, flexible |

---

## Corruption System (v6.0 - NEW)

### Overview
Player choices corrupt MONSTERS, not the player. Corruption affects monster obedience and power.

### Corruption Sources
| Source | Points |
|--------|--------|
| Conversational choices | 1-3 |
| Quest decisions | 8-10 |
| Major story choices | 25+ |

### Corruption Thresholds

| Corruption % | Status | Effect |
|--------------|--------|--------|
| 0-10% | ASCENDED | +25% all stats |
| 11-25% | Purified | +10% all stats |
| 26-50% | Unstable | Normal stats |
| 51-75% | Corrupted | -10% all stats |
| 76-79% | Abyssal | -20% all stats |
| **80-100%** | **UNTAMED** | Monster becomes uncontrollable |

### Untamed State (80%+)
- Monster no longer obeys commands
- May attack allies or flee
- Must be purified or released

---

## Capture System (v6.0 - NEW)

### Post-Battle Capture Phase
Capturing occurs AFTER combat ends, not during.

### Capture Formula
```
Capture Chance = f(HP%, Corruption%, Item Tier) + QTE Bonus
```

### Capture Items

| Tier | Name | Base Modifier |
|------|------|---------------|
| Basic | Veil Shard | +0% |
| Strong | Veil Crystal | +15% |
| Master | Veil Core | +30% |
| Legendary | Veil Heart | +50% |

### Capture Failure Outcomes
| Monster State | Failure Result |
|---------------|----------------|
| Low Corruption | Escapes (flees) |
| High Corruption | Berserk (+30-50% damage, fight again) |

### QTE Bonus
Successful Quick Time Event adds +5-15% to capture chance.

---

## MCP & Plugin Arsenal

### Active MCP Servers (2 Local + 5 Plugin)

**Local (.mcp.json):**
| Server | Purpose | Usage Trigger |
|--------|---------|---------------|
| sequential-thinking | Complex problem decomposition | "Design...", "Plan...", system architecture |
| image-process | Image manipulation (crop, resize) | 2D UI asset pipeline |

**Plugin-provided:**
| Server | Purpose | Usage Trigger |
|--------|---------|---------------|
| Context7 | Unity API docs (23k snippets) | Before writing ANY Unity C# |
| Serena | C# semantic code tools | Any code modification |
| Greptile | Codebase-wide search | "Where is X used?" |
| Episodic Memory | Cross-session conversation history | Every session start |
| Chrome | Browser automation | Web research (rare) |

**TO ADD:**
| Server | Purpose | Status |
|--------|---------|--------|
| **mcp-unity** | Unity Editor control | Install via Package Manager |

**DELETED (2026-01-17):**
- ~~memory~~ - Redundant (VEILBREAKERS.md is single source of truth)
- ~~github~~ - Broken (requires Copilot)
- ~~sentry~~ - Not configured

### Installed Claude Code Plugins (26 - USE ALL)

#### Official Plugins (19)
| Plugin | Purpose |
|--------|---------|
| ralph-wiggum | Fun/personality |
| frontend-design | UI/UX design assistance |
| context7 | Context management |
| serena | Development assistant |
| github | GitHub integration |
| code-review | Code review assistance |
| typescript-lsp | TypeScript language server |
| security-guidance | Security best practices |
| feature-dev | Feature development |
| commit-commands | Git commit assistance |
| pr-review-toolkit | Pull request reviews |
| agent-sdk-dev | Agent development |
| pyright-lsp | Python language server |
| explanatory-output-style | Better explanations |
| greptile | Code search |
| sentry | Error monitoring |
| gopls-lsp | Go language server |
| **csharp-lsp** | **C# language server (Unity!)** |
| clangd-lsp | C/C++ language server |

#### Superpowers Marketplace (7)
| Plugin | Purpose |
|--------|---------|
| double-shot-latte | Enhanced responses |
| superpowers-chrome | Browser automation |
| episodic-memory | Session memory |
| elements-of-style | Writing style |
| superpowers | Core enhancements |
| superpowers-developing-for-claude-code | Meta development |
| superpowers-lab | Experimental features |

### Memory Protocol
**VEILBREAKERS.md is THE source of truth.** Memory MCP should reflect this file's content.

When memory MCP is available:
1. Create entities for: Heroes, Brands, Paths, Systems
2. Relations should match Brand effectiveness wheel
3. Observations should match Lessons Learned section

**Tools are LOCAL ONLY** - never committed to git:
- `.claude/skills/` - Claude Code skills
- `.claude/rules/` - Claude Code rules

### Asset Pipeline
1. Generate art with AI tools
2. Image Process resizes/crops to spec
3. Unity imports to Assets/
4. Verify in-game with Play mode

---

## Trello Board

**Board:** VEILBREAKERS GAME DEV
**URL:** https://trello.com/b/6VhzFXH3/veilbreakers

### Lists
| List | ID |
|------|-----|
| BACKLOG | 6950d4eb393f78982014d8e5 |
| SPRINT | 6950d4ec787ad16c300203c9 |
| IN_PROGRESS | 6950d4ecff7a641faf2597c6 |
| TESTING | 6950d4ec9a6a7a3910f3fd16 |
| BUGS | 6950d4ed076082243950cf2e |
| IDEAS | 6950d4edb39f6b5aeed96742 |

### Labels
battle, ui, art, audio, vera, monsters, critical

---

## User Preferences

- Prefers visual verification with screenshots
- Wants auto-commit + push on all commits
- Likes AAA quality animations
- Prefers subtle, fast button animations
- Values pixel-perfect alignment
- **Use gdtoolkit (gdlint/gdparse)** for GDScript validation before committing
- Add all new requirements/tools to memory files

---

## Lessons Learned

### FAILED (Don't Repeat)
- Lightning effects - background already has them
- Custom eye drawing - artwork has them
- Complex logo animation - caused glitching
- Fake transparency (checker pattern) - use REAL alpha
- **Spine/Cutout rigging for Godot** - Too complex, animations glitchy, STICK TO SPRITE SHEETS

### WORKS
- TextureButton with texture_disabled
- **Run Godot --check-only** to catch script errors before testing
- Autoloads CANNOT have class_name matching singleton name (Godot 4.x)
- Child classes CANNOT redeclare signals from parent class
- Simple scale/modulate tweens
- Clean button transparency
- Subtle, fast animations
- GSAP power3.out = Godot EASE_OUT + TRANS_CUBIC
- Sprite sheet animation with hframes/vframes
- Python PIL for removing white backgrounds (threshold >220)
- Force Godot reimport: `godot --headless --path PROJECT --import --quit`
- **Popup centering**: Don't use anchors (0.5) with direct position - calculate position dynamically: `position = (viewport_size - panel_size) / 2.0`
- **VERADialoguePortrait cache**: Track sheet changes along with row/col to prevent glitching on character swap

---

## Version History

| Commit | Description |
|--------|-------------|
| bebbda3 | VEILBREAKERSV2 - Complete system restoration |
| 7c71562 | Add GDAI MCP, minimal-godot MCP, godot-docs MCP |
| e89dfed | Enable Godot VCS integration + Git MCP |
| d67bf4e | Main menu button layout finalized |

---

## Session Log

| Date | Summary |
|------|---------|
| 2025-12-29 | Consolidated memory to single VEILBREAKERS.md file |
| 2025-12-29 | v2.3: Security fix - removed API keys from git history |
| 2025-12-29 | v2.4: Screenshot organization - all screenshots to screenshots/ folder |
| 2025-12-30 | v3.0: New Capture System - 4 methods (ORB, PURIFY, BARGAIN, FORCE), 4 orb tiers, 80%+ corruption baseline |
| 2025-12-31 | v3.7: Monster XP/leveling system, level up notifications, XP distribution to allied monsters |
| 2025-12-31 | v3.8: Battle animation interface in CharacterBase, victory->overworld transition for dev testing |
| 2025-12-31 | v4.0: Battle UI overhaul - party/enemy sidebars with portraits, centered action bar, combat log with scroll |
| 2025-12-31 | v4.1: Sidebar HP/MP bars now update on damage/heal/skill use via metadata references |
| 2025-12-31 | v4.2: Bug fixes - debug code removal, tween memory leaks, BargainUI Color fix, EventBus.skill_used signal |
| 2025-12-31 | v4.3: Critical fixes - DataManager innate_skills, status icon display on panels, CrashHandler init, SaveManager integration |
| 2025-12-31 | v4.5: Target highlighting - RED for enemies, BLUE for allies with glow effects |
| 2026-01-01 | v5.0: **MAJOR** - Brand System v5.0 LOCKED (12 brands, 3 tiers, evolution system) |
| 2026-01-01 | v5.2: Removed deprecated Element system, modernized to Brand-only damage |
| 2026-01-01 | v5.2.4: Fixed game_manager.gd to use new 4-Path system |
| 2026-01-01 | v0.53: Fixed Variant type inference errors in player_character.gd, added AGENTS.md + opencode.json |
| 2026-01-02 | **v0.60: MAJOR** - Agent architecture (6 agents), documentation system, style guide, version format change |
| 2026-01-02 | v0.61: Battle UI polish - VERA tutorial panel repositioned with continue button, combat log skill names + battle header, damage rebalanced (15-35 dmg), larger monster sprites, transparent action bar, sidebar death state handling |
| 2026-01-04 | **v0.96: CODE DEDUPLICATION** - Major refactoring, utility systems created |
| 2026-01-04 | **v1.16: CLEANUP** - Removed tools/ from git (258K lines). Tools are LOCAL DEV ONLY. Documented rigging failure. |
| 2026-01-04 | **v1.21: HOLLOW EXPANSION** - 8 sprite sheets configured, 7 new skills, Scenario MCP added |
| 2026-01-12 | v1.28: Lint fixes - renamed shadowed `material` to `particle_material` in battle_monster_sprite.gd |
| 2026-01-13 | v1.29: Memory protocol - VEILBREAKERS.md is single source of truth, tools stay LOCAL (not in git) |
| 2026-01-14 | v1.34: Character Select fixes - confirmation popup centering, VERA portrait caching, hero card idle animation, highlight state management |
| 2026-01-15 | **v1.35: MAJOR REDESIGN** - VeilBreakers3D combat system, 10-Brand system (from 12), real-time tactical combat, Path/Brand synergy (buff-only), corruption mechanics, post-battle capture with QTE |
| 2026-01-15 | v1.36: Added 26 Claude Code plugins (19 official + 7 superpowers), updated MCP servers (3 active), added mandatory session protocols |
| 2026-01-15 | **v1.37: 3D ORGANIZATION** - Complete Unity 3D folder structure, 3D model/animation/rig naming conventions, marked all Godot sections as LEGACY/OUTDATED, screenshot protocol added |
| 2026-01-15 | **v1.38: LEGACY CLEANUP** - Moved Godot docs to Docs/LEGACY_Godot/ with README warning, created 40+ Unity 3D asset folders with .gitkeep files, full branch/file structure documented |
| 2026-01-17 | **v1.39: MCP ARSENAL OPTIMIZATION** - Deleted 3 redundant MCPs (memory, github, sentry), documented usage triggers for all 7 MCPs, added mcp-unity installation plan, created Docs/plans/2026-01-17-mcp-arsenal-design.md |
| 2026-01-17 | **v1.40: COMBAT SYSTEM v2.0** - 6-slot abilities (basic, defend, 3 skills, ultimate), tiered synergy (full/partial/neutral/anti), rebalanced combo abilities, AAA rigging/animation/facial pipeline design doc |
| 2026-01-17 | **v1.41: COMBAT IMPLEMENTATION** - Implemented 10-brand effectiveness matrix (2x/0.5x), SynergySystem (full/partial/neutral/anti tiers), AbilityLoadout (6-slot with cooldowns), Combatant base class, DamageCalculator, BattleManager, EventBus combat events, comprehensive test script |
| 2026-01-17 | **v1.42: SERENA PROTOCOL** - Added mandatory Serena Code Intelligence Protocol to CLAUDE.md. Serena now required for all code operations to save 70-90% tokens. Documented when to use Serena vs basic tools. |

---

## NEXT SESSION: Unity 3D Implementation

**Transition Status:**
- ~100 files transferred to Unity
- ~135 files pending (hero sprites, backgrounds, title, UI)
- Design document: `docs/plans/2026-01-15-combat-system-design.md`

**Implementation Status (v1.41):**
- ✅ 10-Brand effectiveness system (BrandSystem.cs)
- ✅ Tiered synergy system (SynergySystem.cs)
- ✅ 6-slot ability structure (AbilityData.cs, Enums.cs)
- ✅ Combatant base class (Combatant.cs)
- ✅ Damage calculation (DamageCalculator.cs)
- ✅ Real-time battle manager (BattleManager.cs)
- ✅ Combat events in EventBus
- ✅ Comprehensive test script (CombatTestSetup.cs)

**Next Priority:**
1. Create test scene in Unity and run CombatTestSetup
2. Implement party swapping system (swap cooldown, backup monster list)
3. Implement command hierarchy (AI modes, quick commands, hotkeys, ping system)
4. Integrate corruption mechanics with synergy system
5. Implement capture system (post-battle phase with QTE)

---

## LEGACY: Hollow Sprite Sheets [GODOT 2D - OUTDATED]

> **⚠️ 2D sprite sheets - Now using 3D models. Keep as reference for 3D conversion.**

**Original Task**: Use Scenario MCP to remove gray backgrounds from 5 sprite sheets.

**Files to create** (save to assets/sprites/monsters/sheets/):
- hollow_claw_sheet.png (4x4) - X-slash, beam, death
- hollow_hurt_sheet.png (4x4) - Hurt reactions, stagger  
- hollow_tendril_sheet.png (4x4) - Sweep, lance, orb
- hollow_vortex_sheet.png (4x4) - Channel, whip, vortex
- hollow_power_sheet.png (4x5) - Rage mode, heavy attacks

**Skills created**: shadow_rend, void_orb, tendril_lash, tendril_sweep, consuming_vortex, dread_surge, abyssal_rage

---

## LEGACY: Recent Godot Changes (v0.96) [OUTDATED]

> **⚠️ These changes were for Godot 2D project**

### New Utility Systems (scripts/utils/) [GODOT]
- **BrandSystem** - Centralized brand effectiveness, colors, and classification
- **UIStyleFactory** - Standardized StyleBoxFlat creation for panels, buttons, bars
- **AnimationEffects** - Reusable flash, shake, fade, scale, button hover effects

### Deleted Files (archive/deprecated_scripts/)
- bargain_system.gd (duplicate of active)
- bargain_ui.gd (duplicate of active)
- battle_camera_controller.gd (older version)
- battle_ui_animator.gd (older version)
- battle_sequencer.gd (older version)
- screen_effects_manager.gd (older version)
- vfx_manager.gd (byte-for-byte duplicate)
- damage_number_system.gd (refactored into damage_number.gd + damage_number_spawner.gd)

### Refactored Files
- `damage_calculator.gd` - Uses BrandSystem for effectiveness calculations
- `ai_controller.gd` - Uses BrandSystem (removed 50+ lines of duplicate code)
- `game_manager.gd` - Removed duplicate window setup (handled by ErrorLogger)
- `audio_manager.gd` - Added ambience caching to prevent disk reloads

---

## LEGACY: Recent Godot Changes (v0.53-v0.60) [OUTDATED]

> **⚠️ These changes were for Godot 2D project**

### Removed Systems [GODOT]
- **Element System** - Completely removed (was deprecated)
  - Deleted `Element_DEPRECATED` enum and `Element` alias from enums.gd
  - Removed legacy Path values (SHADE, TWILIGHT, NEUTRAL, LIGHT, SERAPH)
  - Removed `BRAND_BONUSES_DEPRECATED` dictionary from constants.gd
  - Updated damage_calculator.gd for Brand-only effectiveness

### Fixed Files
- `game_manager.gd` - `get_current_path()` renamed to `get_dominant_path()`, uses PathSystem
- `player_character.gd` - Fixed Variant type inference (line 167, 175)
- `damage_calculator.gd` - Rewritten for Brand-only effectiveness
- `character_base.gd` - Removed `elements` property
- `monster.gd`, `monster_data.gd` - Removed element references
- `skill_data.gd` - Removed deprecated `element` export
- `item_data.gd` - Removed `element_affinity` export
- `helpers.gd` - Removed `get_element_color()` function
- `data_manager.gd` - Removed `get_skills_by_element()`
- `status_effect_manager.gd` - Removed element-based immunities

### New Files
- `AGENTS.md` - Instructions for AI coding agents (OpenCode format)
- `opencode.json` - MCP server configuration for OpenCode (15 servers)

---

*Claude Code reads this file at the start of every session per CLAUDE.md protocol.*

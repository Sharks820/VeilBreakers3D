# VEILBREAKERS 3D - UNITY PROJECT

## Mission: AAA 2D Turn-Based RPG. NO COMPROMISES. NO RIVALS.

**Engine:** Unity (migrated from Godot)
**Project Path:** `C:/Users/Conner/Downloads/VeilBreakers3D`

---

# ⚠️ CRITICAL: MANDATORY RULES (READ FIRST)

## 🔴 THE THREE ABSOLUTES - NEVER SKIP

### 1. SAVE MEMORIES EVERY 15 MINUTES
- Update `VEILBREAKERS.md` with any new decisions, values, or lessons
- This is the SINGLE SOURCE OF TRUTH across sessions
- If you learned something, WRITE IT DOWN

### 2. COMMIT EVERY 15 MINUTES
- `git add -A && git commit -m "descriptive message" && git push`
- NO EXCEPTIONS. Losing work is UNACCEPTABLE.
- Increment version in VEILBREAKERS.md header before each commit

### 3. ORGANIZE FILES INTO CORRECT GIT LOCATIONS
- EVERY file goes to its designated folder
- EVERY commit goes to the correct branch
- NEVER dump files randomly

---

# ⚠️ MANDATORY: SESSION PROTOCOLS

## 1. Memory Protocol
**EVERY SESSION MUST:**
1. **READ `VEILBREAKERS.md`** at the start of every conversation
2. **ACKNOWLEDGE** current project state before taking any action
3. **UPDATE `VEILBREAKERS.md`** when making significant changes (new systems, UI values, lessons learned)

> **VEILBREAKERS.md is THE SINGLE SOURCE OF TRUTH for cross-session memory.**
> Do NOT rely on MCP memory server. Do NOT create new memory files.
> All persistent project knowledge lives in VEILBREAKERS.md.

## 2. Auto-Save Protocol (EVERY 15 MINUTES - NO EXCEPTIONS)

⏰ **COMMIT AND PUSH EVERY 15 MINUTES. PERIOD.**

This is NON-NEGOTIABLE. Every 15 minutes of active work:
1. Increment version in VEILBREAKERS.md header (v2.3 → v2.4 → v2.5...)
2. `git add -A`
3. `git commit -m "v2.X: [brief description]"`
4. `git push`

### Version Format
- **Major:** v3.0, v4.0 = Major milestones (new systems, releases)
- **Minor:** v2.1, v2.2, v2.3 = Every single commit increments by 0.1
- **Current version is in VEILBREAKERS.md header** - ALWAYS increment before commit

### Commit Message Rules
- **NO** "Generated with Claude Code" tags
- **NO** "Co-Authored-By: Claude" tags
- **NO** mentions of Claude or AI in commits
- Keep messages clean and professional

**Why:** Unexpected shutdowns happen. Losing work is unacceptable.

**Track time mentally.** After approximately 15 minutes of work, stop and commit.
If unsure, err on the side of committing MORE often, not less.

## 3. File Naming Rules (MANDATORY)

**NEVER create files with Windows reserved names:**
- `NUL`, `CON`, `PRN`, `AUX`
- `COM1`-`COM9`, `LPT1`-`LPT9`
- These cause undeletable phantom files on Windows

**NEVER redirect output to `nul` in scripts** - use `$null` in PowerShell or `> NUL` only in pure CMD.

## 4. Screenshot Protocol (MANDATORY)

**ALL screenshots MUST go to: `screenshots/`**

When capturing screenshots:
```
screenshots/screenshot_[descriptive_name].png
```

**NEVER save screenshots to:**
- Project root ❌
- assets/ folder ❌
- Any other location ❌

This keeps the project clean. Screenshots are debug artifacts, not game assets.

### What to Update in VEILBREAKERS.md:
- UI position/scale values that work
- New systems implemented
- Bug fixes worth remembering
- User preferences discovered
- Lessons learned (failed approaches)
- Version history (important commits)

### What NOT to Put in VEILBREAKERS.md:
- Temporary debugging info
- Session-specific context
- Duplicate info from CLAUDE.md

## 5. Code Utilities Protocol (MANDATORY)

⚠️ **ALL new code MUST use the centralized utilities in `scripts/utils/`**

This is NON-NEGOTIABLE. Before writing ANY:
- UI styling → **Use UIStyleFactory** (889 lines)
- Tween animations → **Use AnimationEffects** (783 lines)
- Node operations → **Use NodeHelpers** (385 lines)
- String formatting → **Use StringHelpers** (304 lines)
- Math calculations → **Use MathHelpers** (228 lines)
- Magic numbers → **Use Constants** (635 lines)

**NEVER manually:**
- Create `StyleBoxFlat.new()` → use `UIStyleFactory.create_*`
- Add font/color overrides → use `UIStyleFactory.FONT_*` and `COLOR_*`
- Write `is_instance_valid(x): x.queue_free()` → use `NodeHelpers.safe_free(x)`
- Format `"%d/%d" % [hp, max]` → use `StringHelpers.format_hp()`
- Hardcode `0.3` durations → use `Constants.WAIT_SHORT`

**Full reference in VEILBREAKERS.md → "MANDATORY UTILITIES" section**

### Prevention Tools

**Before writing new code:**
1. **Check `docs/CODE_PATTERNS.md`** - Quick anti-pattern reference
2. **Check `scripts/utils/README.md`** - Utility overview

**When adding new patterns:**
1. If pattern is used **3+ times**, add it to appropriate utility file
2. Update `docs/CODE_PATTERNS.md` with the new pattern
3. Follow the utility function template in `scripts/utils/README.md`

**Duplication detector:** `scripts/tools/duplication_detector.gd`
- Run in Godot Editor (Script > Run) to scan for anti-patterns
- Identifies code that should use utilities instead

## 6. High-Risk Items (MUST ASK USER)

- Change Brand/Path system design
- Modify save file format
- Remove or rename core classes
- Change corruption philosophy
- Major UI flow changes
- Game function/story/big script changes
- Delete ANY file (archive only, never delete)

## 7. Git Organization Protocol (MANDATORY)

### Branch Naming Convention
| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/battle-system` |
| `bugfix/` | Bug fixes | `bugfix/hp-display` |
| `docs/` | Documentation only | `docs/api-reference` |
| `hotfix/` | Urgent production fixes | `hotfix/crash-on-load` |
| `refactor/` | Code cleanup | `refactor/manager-classes` |

### File Organization Rules
| File Type | Location | Branch |
|-----------|----------|--------|
| C# Scripts | `Assets/Scripts/[category]/` | `feature/*` or `bugfix/*` |
| Documentation | `Docs/` | `docs/*` or current feature branch |
| Art Assets | `Assets/Art/[category]/` | `feature/*` |
| Prefabs | `Assets/Prefabs/[category]/` | `feature/*` |
| Scenes | `Assets/Scenes/` | `feature/*` |
| Config/Data | `Assets/Data/` | `feature/*` |

### Commit Rules
1. **NEVER commit directly to `main`** - Always use feature branches
2. **One logical change per commit** - Don't bundle unrelated changes
3. **Descriptive messages** - What and why, not just what
4. **Push immediately after commit** - Don't let commits sit locally

### Before Every Commit Checklist
- [ ] Files are in correct folders
- [ ] On correct branch for this work type
- [ ] VEILBREAKERS.md updated if needed
- [ ] No debug/test code left in

---

# THE ARSENAL - MCP SERVERS

## GODOT ENGINE CONTROL

### 1. godot-screenshots (THE EYES)
**Run game + capture in-game screenshots for visual verification**
```
- Run/stop Godot projects
- Capture viewport screenshots
- Search scenes and monitor output
- Visual feedback loop - SEE what's happening
```
[Source: fernforestgames/mcp-server-godot](https://github.com/fernforestgames/mcp-server-godot)

### 2. godot-editor (THE HANDS)
**Direct Godot editor manipulation**
```
- Launch editor, run projects
- Create scenes, add nodes
- Load sprites, save scenes
- Get project info and debug output
```
[Source: Coding-Solo/godot-mcp](https://github.com/Coding-Solo/godot-mcp)

---

## 2D ANIMATION POWERHOUSE

### 3. gsap-animation (THE CHOREOGRAPHER)
**Animation expertise - natural language to code**
```
- 100+ production patterns
- 60fps optimization
- Timeline choreography
- Scroll effects, parallax, reveals
- Transferable to Godot Tween
```
[Source: bruzethegreat/gsap-master-mcp-server](https://github.com/bruzethegreat/gsap-master-mcp-server)

### 4. spine2d-animation (DISABLED - NOT RECOMMENDED)
**2D skeletal animation - REMOVED FROM WORKFLOW**
```
- Attempted bone rigging approach
- Too complex for Godot 2D integration
- Cutout animation didn't work reliably
- STICK TO SPRITE SHEETS INSTEAD
```
[Source: ampersante/spine2d-animation-mcp](https://github.com/ampersante/spine2d-animation-mcp)

### 5. lottiefiles (ANIMATION LIBRARY)
**Search and retrieve Lottie animations**
```
- Search 500,000+ free animations
- Get animation details and data
- Popular animations discovery
- JSON-based, lightweight
```
[Source: junmer/mcp-server-lottiefiles](https://github.com/junmer/mcp-server-lottiefiles)

---

## AI & REASONING

### 6. sequential-thinking (THE LOGIC)
**Complex problem decomposition**
```
- Break down battle systems
- Architectural decisions
- Multi-step problem solving
- Game balance calculations
```
[Source: @modelcontextprotocol/server-sequential-thinking](https://github.com/modelcontextprotocol/servers)

### 7. memory (THE MEMORY)
**Persistent knowledge graph across sessions**
```
- Remember project decisions
- Track entity relationships
- User preferences
- Cross-session context
```
[Source: @modelcontextprotocol/server-memory](https://github.com/modelcontextprotocol/servers)

---

## ASSET CREATION & PROCESSING

### 8. image-process (THE PROCESSOR)
**Image manipulation without external tools**
```
- Crop, resize, rotate
- Format conversion
- Batch processing
- Sharp library powered
```
[Source: x007xyz/image-process-mcp-server](https://github.com/x007xyz/image-process-mcp-server)

### 9. color-palette (THE ARTIST)
**Generate harmonious color schemes**
```
- Monochrome, analogic, complementary
- Hex, RGB, HSL input
- Game art style consistency
- Battle Chasers palette matching
```
[Source: deepakkumardewani/color-scheme-mcp](https://github.com/deepakkumardewani/color-scheme-mcp)

### 10. huggingface (THE GENERATOR)
**FREE AI image/sprite generation**
```
- Generate sprites from text prompts
- Access to FLUX models
- Free Hugging Face credits
- "pixel art sword" → instant asset
```
[Source: Hugging Face MCP Server](https://huggingface.co/docs/hub/en/hf-mcp-server)

### 11. pixel-mcp (ASEPRITE MASTER)
**40+ tools for pixel art with Aseprite**
```
- Canvas & layer management
- Drawing primitives, flood fill
- 15 dithering patterns
- Spritesheet export (PNG, GIF, JSON)
- Animation frames
```
[Source: willibrandon/pixel-mcp](https://github.com/willibrandon/pixel-mcp)

---

## UI & DESIGN

### 12. figma (UI DESIGN-TO-CODE)
**Convert Figma designs to code**
```
- Design-to-code workflow
- Extract variables, components
- Layout data for UI
- Works with design systems
```
[Source: Figma MCP](https://www.figma.com/blog/introducing-figma-mcp-server/)

---

## PROJECT MANAGEMENT

### 13. trello (TASK TRACKING)
**Manage boards, lists, and cards**
```
- Create/update cards
- Track tasks
- Sprint planning
- AI-assisted project management
```
[Source: delorenj/mcp-server-trello](https://github.com/delorenj/mcp-server-trello)

---

## LOCALIZATION

### 14. i18n (TRANSLATION)
**Internationalization for games**
```
- Manage JSON language files
- Generate translations
- Multi-language support
- Game localization workflow
```
[Source: reinier-millo/i18n-mcp-server](https://github.com/reinier-millo/i18n-mcp-server)

---

## MATH & PHYSICS

### 15. wolfram (CALCULATIONS)
**Wolfram Alpha computational engine**
```
- Complex math formulas
- Physics calculations
- Game balance math
- Scientific queries
```
[Source: cnosuke/mcp-wolfram-alpha](https://github.com/cnosuke/mcp-wolfram-alpha)

---

## ALREADY ACTIVE (Built-in)

### Playwright MCP
**Browser automation for web-based testing**

### Cloudflare Docs MCP
**Cloud infrastructure reference**

---

# TECHNICAL REFERENCE

## Godot Executable
```
C:/Users/Conner/AppData/Local/Microsoft/WinGet/Packages/GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.5.1-stable_win64_console.exe
```

## Project Structure
```
VeilBreakers3D/
├── Assets/
│   ├── Scripts/              # C# scripts
│   │   ├── Battle/           # Battle system
│   │   ├── Managers/         # Game managers
│   │   ├── UI/               # UI controllers
│   │   └── Utils/            # Utility classes
│   ├── Art/
│   │   ├── Sprites/          # 2D sprites
│   │   ├── UI/               # UI elements
│   │   └── Effects/          # VFX
│   ├── Prefabs/              # Reusable prefabs
│   ├── Scenes/               # Unity scenes
│   └── Data/                 # ScriptableObjects, JSON
├── Docs/                     # Documentation
│   ├── ArtReference/         # Art style guides
│   ├── plans/                # Design plans
│   └── generated/            # Auto-generated docs
├── .mcp.json                 # MCP server config
├── CLAUDE.md                 # This file (AI instructions)
└── VEILBREAKERS.md           # Cross-session memory
```

## Autoload Singletons
| Singleton | Purpose |
|-----------|---------|
| ErrorLogger | Debug logging |
| DataManager | JSON data |
| GameManager | Game state |
| SaveManager | Save/load |
| AudioManager | Sound/music |
| SettingsManager | Settings |
| VeraSystem | VERA AI |
| InventorySystem | Inventory |
| SceneManager | Scenes |

---

# ART STYLE & GENERATION

## Style Reference
**Dark Fantasy Horror** - NOT Battle Chasers style for generation
- Hand-painted 2D, atmospheric
- Dynamic lighting, dramatic shadows
- Glowing eyes/cores, ominous mood
- High detail, painterly quality

## Art Generation Rules (MANDATORY)

### Model Rules
| Status | Model | Notes |
|--------|-------|-------|
| **PREFERRED** | Seedream (`model_bytedance-seedream-4-5`) | Best quality, use for all new art |
| **ALLOWED** | VeilBreakersV1 (`model_fmiEeZzx1zUMf4dhKeo16nqK`) | Fallback option |
| **FORBIDDEN** | ~~Scenario V2 / Dark Fantasy V2~~ | INACCURATE - needs retraining |

### Style Prompt (INCLUDE IN EVERY PROMPT)
```
dark fantasy horror, [creature description], dark atmospheric,
glowing [color] eyes/core, dramatic lighting, deep shadows,
high detail, painterly quality, ominous mood,
2D game sprite, transparent background
```

### DO NOT Use in Prompts
- ❌ "Battle Chasers" or "Joe Madureira" (WRONG STYLE)
- ❌ "thick linework" or "comic book" (WRONG STYLE)
- ❌ anime/cel-shaded style
- ❌ flat colors or comic halftone
- ❌ checker pattern backgrounds

### Output Specs
| Parameter | Value |
|-----------|-------|
| Resolution | 1024-2048px |
| Format | PNG with transparency |
| Save to | `assets/sprites/monsters/` |
| Scale in-game | 0.08 |

---

# CODE STYLE

## File Structure
```gdscript
class_name ClassName
extends ParentClass
## Brief description of the class.

# =============================================================================
# SIGNALS
# =============================================================================

signal something_happened(param: Type)

# =============================================================================
# CONSTANTS
# =============================================================================

const MY_CONSTANT := 10

# =============================================================================
# EXPORTS
# =============================================================================

@export var my_var: int = 0

# =============================================================================
# STATE
# =============================================================================

var internal_state: Dictionary = {}
```

## Naming Conventions
- **Classes:** PascalCase (`BattleManager`, `CharacterBase`)
- **Functions:** snake_case (`calculate_damage`, `get_stat`)
- **Variables:** snake_case (`current_hp`, `turn_order`)
- **Constants:** SCREAMING_SNAKE_CASE (`MAX_PARTY_SIZE`, `BRAND_STRONG`)
- **Signals:** snake_case past tense (`damage_dealt`, `turn_ended`)
- **Enums:** PascalCase enum, SCREAMING_SNAKE values
- **Private functions:** prefix with `_` (`_on_button_pressed`)

## Type Hints (REQUIRED)
```gdscript
# Always use explicit types - Godot treats Variant inference as warning/error
var value: float = some_dict.get("key", 0.0)  # Correct
var value := some_dict.get("key", 0.0)        # ERROR - infers Variant

# Function signatures
func calculate_damage(attacker: CharacterBase, defender: CharacterBase) -> int:

# Arrays with types
var party: Array[CharacterBase] = []
```

## Null Safety
```gdscript
# Always check before accessing
var node := get_node_or_null("/root/GameManager")
if node and node.has_method("some_method"):
    node.some_method()

# Safe dictionary access
var value: float = data.get("key", default_value)
```

---

# KEY SYSTEMS (DO NOT BREAK)

## Autoload Order (project.godot)
1. ErrorLogger
2. EventBus
3. DataManager
4. GameManager
5. SaveManager
6. AudioManager
7. SceneManager
8. SettingsManager
9. VERASystem
10. InventorySystem
11. PathSystem
12. CrashHandler

## Brand System (12 Brands - LOCKED)
**Pure:** SAVAGE, IRON, VENOM, SURGE, DREAD, LEECH
**Hybrid:** BLOODIRON, CORROSIVE, VENOMSTRIKE, TERRORFLUX, NIGHTLEECH, RAVENOUS

Effectiveness wheel: SAVAGE > IRON > VENOM > SURGE > DREAD > LEECH > SAVAGE

## Path System (4 Paths)
IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED, NONE

## Corruption System
- 0-10%: ASCENDED (+25% stats)
- 11-25%: Purified (+10% stats)
- 26-50%: Unstable (normal)
- 51-75%: Corrupted (-10% stats)
- 76-100%: Abyssal (-20% stats)

**Core philosophy:** Lower corruption = STRONGER monster. Goal is ASCENSION.

---

# COMMON PATTERNS

## Signals via EventBus
```gdscript
# Emit
EventBus.damage_dealt.emit(source, target, amount, is_critical)

# Connect (use NodeHelpers for safe connection)
NodeHelpers.safe_connect(EventBus, "damage_dealt", _on_damage_dealt)
```

## Using Constants
```gdscript
# Wait timers - USE CONSTANTS
await get_tree().create_timer(Constants.WAIT_SHORT).timeout
await get_tree().create_timer(Constants.WAIT_STANDARD).timeout

# DON'T use Constants in const declarations (load order issues)
const BAD := [Constants.SOME_VALUE]  # Parse error!
const GOOD: Array[float] = [75.0, 50.0, 25.0]  # Hardcode instead
```

## Using Enums
```gdscript
var state: Enums.BattleState = Enums.BattleState.INITIALIZING
var brand: Enums.Brand = Enums.Brand.SAVAGE
```

## Using UIStyleFactory
```gdscript
var label := UIStyleFactory.create_label("Text", UIStyleFactory.FONT_HEADING, UIStyleFactory.COLOR_GOLD)
var hp_bar := UIStyleFactory.create_hp_bar()
var vbox := UIStyleFactory.create_vbox(8)
var button := UIStyleFactory.create_button("Click Me")
```

## Using NodeHelpers
```gdscript
NodeHelpers.safe_free(node)
NodeHelpers.clear_children(container)
NodeHelpers.show(node)
var labels := NodeHelpers.get_children_of_type(parent, Label)
```

## Using StringHelpers
```gdscript
var hp_text := StringHelpers.format_hp(current_hp, max_hp)  # "45/100"
var change := StringHelpers.format_stat_change(5)  # "+5"
var pct := StringHelpers.format_percent(0.75)  # "75%"
```

## Using AnimationEffects
```gdscript
AnimationEffects.button_hover(button)
AnimationEffects.popup_entrance(popup)
AnimationEffects.flash_white(node)
AnimationEffects.death_animation(sprite)
```

---

# ANIMATION PATTERNS

## Button Hover (USE AnimationEffects!)
```gdscript
# ❌ OLD WAY - DON'T DO THIS
func _on_button_hover(button: BaseButton) -> void:
    create_tween().tween_property(button, "scale", Vector2(1.05, 1.05), 0.15)

# ✅ NEW WAY - USE THIS
func _on_button_hover(button: BaseButton) -> void:
    AnimationEffects.button_hover(button)

func _on_button_unhover(button: BaseButton) -> void:
    AnimationEffects.button_unhover(button)
```

## Standard Timings (USE Constants!)
| Animation | Constant | Value |
|-----------|----------|-------|
| Button hover | `Constants.UI_BUTTON_HOVER` | 0.15s |
| Button press | `Constants.UI_BUTTON_PRESS` | 0.1s |
| Scene fade | `Constants.UI_SCENE_FADE` | 0.3s |
| Menu slide | `Constants.UI_MENU_SLIDE` | 0.25s |
| Wait short | `Constants.WAIT_SHORT` | 0.3s |
| Wait standard | `Constants.WAIT_STANDARD` | 0.5s |

---

# LESSONS LEARNED

## FAILED (Don't Repeat)
- Lightning effects - background has them
- Custom eye drawing - artwork has them
- Complex logo animation - caused glitching
- Fake transparency (checker pattern) - use REAL alpha
- **Spine/Cutout rigging for Godot** - Too complex, animations glitchy, STICK TO SPRITE SHEETS

## WORKS
- TextureButton with texture_disabled
- Simple scale/modulate tweens
- Clean button transparency
- Subtle, fast animations
- GSAP power3.out = Godot EASE_OUT + TRANS_CUBIC
- Sprite sheet animation with hframes/vframes
- Python PIL for removing white backgrounds (threshold >220)
- Force Godot reimport: `godot --headless --path PROJECT --import --quit`

---

# SETUP

## Increase MCP Token Limit (Windows)
```cmd
set MAX_MCP_OUTPUT_TOKENS=100000
cd C:\Users\Conner\Downloads\VeilbreakersGame
claude
```

## Make Permanent
```cmd
setx MAX_MCP_OUTPUT_TOKENS 100000
```

---

# PHILOSOPHY

1. **AAA or nothing** - No shortcuts
2. **Visual verification** - Use screenshots
3. **Working > Fancy broken** - Simple wins
4. **Don't duplicate** - Check artwork first
5. **User is judge** - They see what matters

---

# MCP SOURCES

All FREE and open-source:
- [PulseMCP Directory (7460+ servers)](https://www.pulsemcp.com/servers)
- [Awesome MCP Servers](https://github.com/punkpeye/awesome-mcp-servers)
- [Official MCP Registry](https://registry.modelcontextprotocol.io)
- [mcpservers.org](https://mcpservers.org/)

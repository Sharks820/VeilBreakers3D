# VEILBREAKERS - Changelog

> Version Format: v0.XX (+0.01 minor, +0.10 major, v1.00 = release)

---

## [v1.00] - 2026-01-04

### Fixed
- **CRITICAL: Broken Helpers references** - `helpers.gd` was archived in v0.95 but 5 files still referenced it
  - Migrated brand color functions to `BrandSystem` class:
    - `get_brand_color(brand: Enums.Brand) -> Color`
    - `get_brand_color_by_name(brand_name: String) -> Color`
    - `get_brand_glow_color(brand: Enums.Brand) -> Color`
  - Updated all references from `Helpers.get_brand_color*()` to `BrandSystem.get_brand_color*()`
  - Files fixed: `animation_effects.gd`, `battle_sequencer.gd`, `battle_ui_animator.gd`, `battle_ui_controller.gd`, `character_select_controller.gd`, `damage_number_spawner.gd`

### Fixed (v0.99)
- **Duplicate `_exit_tree()` functions** causing signal leaks
  - `battle_arena.gd` - Removed duplicate at line 2022 (first one at line 64 was correct)
  - `main_menu_controller.gd` - Removed duplicate at line 480 (first one at line 146 was correct)

---

## [v0.97] - 2026-01-04

### Added
- **COLOR_TRANSPARENT constant** - `Color(0, 0, 0, 0)` for consistent transparency
- **COLOR_NORMAL constant** - `Color(1, 1, 1, 1)` for resetting modulation
- Both constants added to `scripts/utils/constants.gd` for use across codebase

---

## [v0.96] - 2026-01-04

### Added
- **Style validation tool** (`tools/validate_style_constants.py`) - Analyzes codebase for hardcoded colors and styles

### Changed
- **StyleManager autoload** - Added then reverted (caused issues)
  - Attempted to centralize color/style management
  - Reverted due to integration complexity
  - Constants approach (v0.97) used instead

---

## [v0.95] - 2026-01-04

### Removed (Dead Code Cleanup)
- **Archived helpers.gd** - 291 lines, 35+ functions, ZERO usage across codebase
- **Archived debug.gd** - 235 lines, debug commands + logging, ZERO usage
- **Archived ui_colors.gd** - Created but never integrated, ZERO usage

### Fixed
- **Signal Leaks in BattleArena** (CRITICAL - 17+ connections)
  - Added comprehensive `_exit_tree()` with all signal disconnections
  - Disconnects: EventBus (4), battle_sequencer (6), vfx_manager (2)
  - Disconnects: damage_number_spawner (1), battle_camera (2), battle_manager (4)
  - Disconnects: battle_ui target_highlight_changed signal
  - Kills `_highlight_breathing_tween` infinite loop on exit
  - Clears `_sprite_hitboxes` dictionary to prevent memory leaks
- **Signal Leaks in MainMenuController** (CRITICAL)
  - Added `_exit_tree()` to kill `logo_tween` infinite pulse loop
  - Disconnects all button hover/unhover signals (4 buttons × 4 signals)
  - Disconnects settings_menu.settings_closed signal

---

## [v0.94] - 2026-01-04

### Added
- **UIStyleCache Autoload** - Caches and reuses StyleBox instances
  - Reduces 97+ StyleBoxFlat.new() calls to ~20 cached styles
  - Predefined styles for HP/MP bars, buttons, panels, tooltips
  - Eliminates GC pressure from repeated style allocations
- **UIColors Constants** - Centralized color definitions
  - 80+ color constants for consistent theming
  - Helper functions: get_brand_color(), get_action_color()
  - Utility functions: with_alpha(), brighten(), darken()

### Fixed
- **Signal Connection Leaks in BattleManager** (CRITICAL)
  - Added `_cleanup_battle_signals()` function
  - Properly disconnects character `died` signals on battle end
  - Disconnects capture system signals to prevent memory leaks
  - Called in both `_on_battle_victory()` and `_on_battle_defeat()`
- **Signal Connection Leaks in BattleUIController** (CRITICAL)
  - Added BattleManager signal disconnections in `_exit_tree()`
  - Added EventBus signal disconnections (action_executed, level_up)
  - Added viewport size_changed disconnection
  - Fixes 212 connects vs 23 disconnects imbalance
- **Duplicate DamageCalculator in AIController** (CRITICAL)
  - AIController now gets reference from parent BattleManager
  - Fallback creation only if parent reference unavailable
  - Eliminates redundant node allocation
- **Tween Memory Leaks in CharacterBattleAnimator** (CRITICAL)
  - Added tween tracking via sprite metadata
  - `_kill_existing_tweens()` cleans up before new animations
  - `_create_tracked_tween()` tracks all created tweens
  - Prevents orphaned tweens from interrupted animations

### Technical Debt Identified (Future Work)
- battle_ui_controller.gd: 4611 lines, 162 functions (God Class)
- 54 StyleBoxFlat.new() calls in battle_ui_controller (use UIStyleCache)
- 273 hardcoded Color() calls (use UIColors)
- Duplicate panel creation functions need consolidation

---

## [v0.93] - 2026-01-03

### Added
- **Test Battle Starter Items** - Test battles now include starter inventory
  - 5x Minor Potion, 3x Standard Potion, 1x Greater Potion
  - 3x Ether (MP restore)
  - 2x Antidote, 1x Remedy (status cures)
  - 1x Phoenix Down (revival)
  - 5x Capture Orb, 2x Greater Capture Orb, 1x Master Capture Orb
  - 2x each Attack/Defense/Speed Tonic (buffs)

---

## [v0.92] - 2026-01-03

### Added
- **Defend System Overhaul** - Defend can now target self OR any ally
  - Added GUARDED status effect (30% damage reduction)
  - Defend button opens ally target selection (blue highlights)
  - Self-defend: Character gets GUARDED status
  - Guard ally: Ally gets GUARDED, damage redirects to guardian
  - GUARDED consumed after 1 hit OR at end of round
  - Cannot double-guard (can't guard someone already guarded)

---

## [v0.85] - 2026-01-02

### Fixed
- **Character Select Breathing Animation** - Fixed frame skipping/glitching
  - Added `TWEEN_PROCESS_IDLE` for smoother frame timing
  - Smooth scale transitions instead of abrupt resets
  - Prevents visual jump when starting/stopping breathing animation
- **Attack Animation Visibility** - Slowed sprite sheet attack animations
  - Hollow attack: 12fps → 6fps (red beam effect now visible)
  - Chainbound attack: 10fps → 7fps
  - Mawling attack: 14fps → 8fps
  - Effects like Hollow's red chest beam now display properly

### Changed
- Moved backup files to archive/ui_elements/

---

## [v0.84] - 2026-01-02

### Fixed
- **Attack Animations Not Making Contact** - Sprites now move toward targets
  - Added `_play_attack_movement_tween()` for sprite sheet animations
  - Added `_play_skill_movement_tween()` for skill animations
  - Movement runs alongside sprite sheet animations
  - Slowed movement timings: pullback 0.25s, strike 0.3s, hold 0.4s, recovery 0.5s

---

## [v0.83] - 2026-01-02

### Fixed
- **Taunt Not Working** - Implemented force_target_self special effect
  - Added forced_target tracking in BattleManager
  - AI now respects forced_target when selecting targets
  - Forced target decrements at end of round

### Changed
- **DPS Balance** - Added MP management to prevent skill spam
  - Rend MP: 8 → 20
  - Mawling/Ravener MP: 0 → 15
  - Added MP regeneration: 2 + 5% max MP per turn
  - Defending grants +2 bonus MP regen

---

## [v0.82] - 2026-01-02

### Fixed
- **Heroes Only Had 3 Starting Skills** - Added 4th innate skill to each hero
  - Bastion: +taunt
  - Rend: +bloodletting
  - Marrow: +siphon_heal
  - Mirage: +fear_touch
- **Defend Appearing in Skills Menu** - Filtered out attack_basic and defend
- **Combat Log Not Auto-Scrolling** - Set scroll_following = true
- **Combat Log Showing "Skill" Instead of Skill Name** - Fixed to show actual skill name

---

## [v0.81] - 2026-01-02

### Fixed
- **Heroes Showing "Brand" Instead of "Path"** - UI now correctly shows Path for heroes
  - Removed Brand display from player character tooltips
  - Fixed current_path not being set from hero_data.primary_path

---

## [v0.80] - 2026-01-02

### Fixed
- **Sprite Sheet Animations Not Playing** - Fixed animator creation issue
  - Animator was being created twice (setup() and _ready())
  - Added check in _ready() to only create if not already exists
  - Animations now play correctly for Hollow, Mawling, Chainbound

---

## [v0.79] - 2026-01-02

### Added
- **Sprite Sheet Death Animation Integration** - Death animations now use sprite sheets when available
  - `_play_death_animation()` checks for animated_battle_sprite with play_death() method
  - Waits for death_animation_complete signal before hiding sprite
  - Falls back to tween-based death for monsters without sprite sheets
- **Animation Debug Logging** - Added print statements to sprite_sheet_animator.gd
  - Traces animation flow: play(), _switch_sheet(), play_attack()
  - Logs sheet switching, frame changes, and fallback usage
  - Helps diagnose animation issues in development

### Verified Working
- **Sprite Sheet Animation System** - Confirmed fully functional
  - Mawling: 2 sheets, 13 animations (idle, attack, hurt, death, skills)
  - Hollow: 3 sheets, 13 animations
  - Idle animations cycling at 6 FPS
  - Sheet switching works correctly between animations

---

## [v0.65] - 2026-01-02

### Added
- **Victory Fanfare** - Enhanced victory screen with dramatic animations
  - Screen flash effect on victory
  - Animated "VICTORY!" title with letter-by-letter reveal
  - Title pulse and glow effects
  - Battle stats summary (turns, damage dealt/taken, captures)
  - Staggered reward reveals
- **Defeat Screen Polish** - Enhanced defeat screen
  - Red screen flash effect
  - Animated "DEFEAT" title with shake effect
  - Somber fade-in timing
- **Character Select → Game Flow** - Full integration
  - GameManager stores selected hero
  - PlayerCharacter.initialize_from_hero_data() method
  - Character select transitions to test battle
  - Hero's Path determines starting Brand alignment

### Changed
- PlayerCharacter now initializes from HeroData resource
- GameManager tracks selected_hero_id and player_character

---

## [v0.64] - 2026-01-02

### Added
- **Character Select HUD** - Full hero selection screen
  - 4 heroes displayed: Bastion (Tank), Rend (DPS), Marrow (Healer), Mirage (Illusionist)
  - Left panel: Hero cards with portraits, names, titles, roles
  - Center: Large animated hero sprite with breathing animation
  - Right panel: Path/Brand alignment, description, 8 base stats, combat style, starting skills
  - Path → Brand mapping visualization (IRONBOUND→IRON, FANGBORN→SAVAGE, etc.)
  - Role-colored cards (Tank=blue, DPS=red, Healer=green, Illusionist=purple)
  - Keyboard (up/down/enter/escape) and mouse navigation
  - Animated hero transitions with fade + scale pop
- New files: `scripts/ui/character_select_controller.gd`, `scenes/ui/character_select.tscn`

### Changed
- Main Menu "New Game" now goes to Character Select instead of directly to battle

---

## [v0.63] - 2026-01-02

### Added
- **Capture UI Animations** - Full visual feedback for capture system
  - Corruption bar pulse/glow during capture attempts
  - Floating "-X% CORRUPTION" popup on corruption reduction
  - Semi-transparent overlay during capture phase
  - Success popup with green flash ("CAPTURED!")
  - Failure popup with red flash ("ESCAPED!")
  - Monster panel color flash effects
  - Smooth corruption bar decrease animation
- Connected CaptureSystem signals to BattleUIController

---

## [v0.62] - 2026-01-02

### Changed
- **Monster Sprites Overhaul** - All 19 monster sprites regenerated
  - User-approved horror art with proper transparency
  - All sprites resized to 1024x1024 PNG format
  - More terror/sinister aesthetic (not just zombie style)
  - Backgrounds removed using external tool (not Python PIL)
  - Archived old sprites to assets/sprites/monsters/archive/

### Added
- 10 new monsters with complete art: Bloodshade, Chainbound, Corrodex, Crackling, Ironjaw, Needlefang, Sporecaller, The Broodmother, The Bulwark, Voltgeist

---

## [v0.61] - 2026-01-02

### Fixed
- Background transparency for needlefang and sporecaller sprites
- Brand display in battle UI tooltips and party sidebar

---

## [v0.60] - 2026-01-02 (MAJOR)

### Added
- **Agent Architecture** - 6 primary agents with specialized roles
  - Builder (Opus 4.5 + extended thinking) - Autonomous coding
  - Operations - Task orchestration, documentation management
  - Scenario/Assets - Art generation and style consistency
  - Monster/Lore - Story, creatures, quests
  - Map Creator - World design, environments
  - Code Review - Quality analysis, optimization
- **Documentation System**
  - `docs/STYLE_GUIDE.md` - Comprehensive art bible
  - `docs/CHANGELOG.md` - Version history (this file)
  - `docs/CURRENT_STATE.md` - Current project state
  - `docs/ROADMAP.md` - Planned features
  - `docs/DECISIONS.md` - Architectural decisions log
- **Version Format Overhaul** - Changed from v5.X to v0.XX format
- **Scenario.gg Integration** - Full API access with trained models

### Changed
- Updated `AGENTS.md` with agent summaries and merged CLAUDE.md content
- Archived `CLAUDE.md` to `archive/CLAUDE.md`

---

## [v0.56] - 2026-01-01

### Fixed
- Combat log doubling - removed duplicate log_action() call
- Enemy sidebar HP labels - added missing HPLabel nodes
- Blue ally highlighting - fixed panel meta key checks
- Defend ally mechanic - full UI + backend implementation
- Hit rate investigation - confirmed formula is correct (~90%)

---

## [v0.55] - 2026-01-01

### Fixed
- Variant type inference errors in player_character.gd

---

## [v0.54] - 2026-01-01

### Fixed
- game_manager.gd to use new 4-Path system

---

## [v0.53] - 2026-01-01

### Added
- AGENTS.md - Instructions for AI coding agents
- opencode.json - MCP server configuration (15 servers)

---

## [v0.52] - 2026-01-01

### Removed
- **Element System** - Completely deprecated
  - Removed Element_DEPRECATED enum
  - Removed legacy Path values (SHADE, TWILIGHT, etc.)
  - Removed BRAND_BONUSES_DEPRECATED dictionary
  - Updated damage_calculator.gd for Brand-only effectiveness

### Changed
- Modernized all files to Brand-only damage system

---

## [v0.50] - 2026-01-01 (MAJOR)

### Added
- **Brand System v5.0** - LOCKED
  - 12 brands (6 Pure + 6 Hybrid)
  - 3 tiers (Pure, Hybrid, Primal)
  - Brand effectiveness wheel
  - Evolution system (3 stages Pure/Hybrid, 2 stages Primal)
- AGENT_SYSTEMS.md - Complete game systems documentation
- docs/BRAND_REFERENCE.md - Quick brand reference
- docs/EVOLUTION_REFERENCE.md - Evolution system reference

---

## [v0.45] - 2025-12-31

### Added
- Target highlighting - RED for enemies, BLUE for allies with glow effects

---

## [v0.43] - 2025-12-31

### Fixed
- DataManager innate_skills loading
- Status icon display on panels
- CrashHandler initialization
- SaveManager integration

---

## [v0.42] - 2025-12-31

### Fixed
- Debug code removal
- Tween memory leaks
- BargainUI Color fix
- Added EventBus.skill_used signal

---

## [v0.41] - 2025-12-31

### Fixed
- Sidebar HP/MP bars now update on damage/heal/skill use

---

## [v0.40] - 2025-12-31 (MAJOR)

### Added
- **Battle UI Overhaul**
  - Party sidebar (170px, left side, green bars)
  - Enemy sidebar (170px, right side, red bars)
  - Centered action bar (700px)
  - Combat log with scroll (210x170, bottom-right)
  - Uniform sprite scale (0.08)
  - Turn order display (top bar)

---

## [v0.38] - 2025-12-31

### Added
- Battle animation interface in CharacterBase
- Victory -> overworld transition for dev testing

---

## [v0.37] - 2025-12-31

### Added
- Monster XP/leveling system
- Level up notifications
- XP distribution to allied monsters

---

## [v0.30] - 2025-12-30 (MAJOR)

### Added
- **New Capture System**
  - 4 methods: ORB, PURIFY, BARGAIN, FORCE
  - 4 orb tiers: Basic, Greater, Master, Legendary
  - 80%+ corruption baseline

---

## [v0.24] - 2025-12-29

### Changed
- Screenshot organization - all screenshots to screenshots/ folder

---

## [v0.23] - 2025-12-29

### Security
- Removed API keys from git history

---

## [v0.22] - 2025-12-29

### Changed
- Consolidated memory to single VEILBREAKERS.md file

---

## Earlier Versions

| Version | Date | Summary |
|---------|------|---------|
| v0.20 | 2025-12-28 | Lock-in turn order system |
| v0.15 | 2025-12-27 | Main menu button layout finalized |
| v0.10 | 2025-12-26 | Initial project setup |

---

*Maintained by Operations Agent*

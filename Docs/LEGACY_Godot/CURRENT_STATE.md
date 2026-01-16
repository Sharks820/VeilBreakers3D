# VEILBREAKERS - Current State

> **Version:** v1.00 | **Last Updated:** 2026-01-04

---

## Project Overview

| Field | Value |
|-------|-------|
| Engine | Godot 4.5.1 |
| Language | GDScript (strict typing) |
| Genre | Dark Fantasy Horror RPG |
| Resolution | 1920x1080 |
| Version Format | v0.XX (+0.01 minor, +0.10 major) |

---

## System Status

### Working Systems

| System | Status | Notes |
|--------|--------|-------|
| Main Menu | 100% | Logo, buttons, eye animation working |
| Character Select | 100% | 4 heroes, Path/Brand display, animated |
| Battle Manager | 90% | Core loop complete, victory/defeat polished |
| Turn System | 100% | Lock-in party selection working |
| Damage Calculator | 100% | Brand effectiveness implemented |
| Status Effects | 100% | 17 effects, icons with tooltips |
| Capture System | 80% | 4 methods, UI animations complete |
| Brand System | 100% | 12 brands, 3 tiers, LOCKED |
| Path System | 100% | 4 paths, synergies defined |
| Monster Data | 100% | Resource system working |
| Skill Data | 100% | 99+ skills in data files |
| Item Data | 100% | 33+ items defined |
| UI Framework | 90% | Battle UI complete, menus need work |
| Save/Load | 50% | Framework exists, needs testing |
| Audio | 25% | Framework only |

### In Progress

| System | Status | Blocker |
|--------|--------|---------|
| Capture Overhaul | Pending | User planning new system |
| Overworld | 0% | Not started |
| Town Hubs | 0% | Not started |
| VERA Dialogue | 20% | Framework exists |
| Quest System | 0% | Not started |

### Known Issues

| Issue | Severity | Workaround |
|-------|----------|------------|
| Missing font: main_font.tres | Low | Uses fallback |
| Some .uid files untracked | Low | Godot-generated, ignore |

### Recent Fixes (v0.92-v0.97)

| Fix | Version | Status |
|-----|---------|--------|
| Defend system overhaul (target self or allies) | v0.92 | ✅ Fixed |
| Test battle starter items added | v0.93 | ✅ Fixed |
| Signal leaks in BattleUIController | v0.94 | ✅ Fixed |
| Duplicate DamageCalculator in AIController | v0.94 | ✅ Fixed |
| Tween memory leaks in CharacterBattleAnimator | v0.94 | ✅ Fixed |
| Signal leaks in BattleArena (17+ connections) | v0.95 | ✅ Fixed |
| Signal leaks in MainMenuController | v0.95 | ✅ Fixed |
| Archived dead code (helpers.gd, debug.gd, ui_colors.gd) | v0.95 | ✅ Fixed |
| StyleManager autoload added then reverted | v0.96 | ⚠️ Reverted |
| Style validation tool added | v0.96 | ✅ Added |
| COLOR_TRANSPARENT/COLOR_NORMAL constants | v0.97 | ✅ Added |

---

## File Structure

```
VeilbreakersGame/
├── .opencode/
│   └── agent/           # 6 agent configurations
├── archive/             # Archived files (never deleted)
├── assets/
│   ├── characters/      # Heroes (4) + VERA stages (4)
│   ├── environments/    # Battle backgrounds, hubs
│   ├── sprites/         # Monsters (19)
│   └── ui/              # Buttons, frames, icons, sheets
├── data/
│   ├── heroes/          # 4 hero .tres files
│   ├── items/           # Consumables + equipment
│   ├── monsters/        # 19 monster .tres files
│   └── skills/          # 99+ skill .tres files
├── docs/
│   ├── BRAND_REFERENCE.md
│   ├── CHANGELOG.md
│   ├── CURRENT_STATE.md  # This file
│   ├── DECISIONS.md
│   ├── EVOLUTION_REFERENCE.md
│   ├── ROADMAP.md
│   └── STYLE_GUIDE.md
├── scenes/
│   ├── battle/          # Battle arena, UI
│   ├── main/            # Game, main menu
│   ├── test/            # Test scenes
│   └── ui/              # Dialogue, pause menu
├── scripts/
│   ├── autoload/        # 12 singletons
│   ├── battle/          # Combat system
│   ├── characters/      # Entity classes
│   ├── data/            # Resource classes
│   ├── systems/         # Game systems
│   ├── ui/              # UI controllers
│   └── utils/           # Enums, Constants, Helpers
├── AGENTS.md            # Agent instructions
├── AGENT_SYSTEMS.md     # Game systems (READ ONLY)
├── opencode.json        # MCP configuration
├── project.godot        # Godot project file
└── VEILBREAKERS.md      # Project memory
```

---

## Autoload Order

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

---

## Content Counts

| Category | Count |
|----------|-------|
| Heroes | 4 (Bastion, Rend, Marrow, Mirage) |
| Monsters | 19 (Hollow, Bloodshade, Chainbound, Corrodex, Crackling, Flicker, Gluttony Polyp, Ironjaw, Mawling, Needlefang, Ravener, Skitter Teeth, Sporecaller, The Broodmother, The Bulwark, The Congregation [BOSS], The Vessel, The Weeping, Voltgeist) |
| Skills | 99+ defined |
| Items | 33+ defined |
| Status Effects | 17 |
| Brands | 12 (6 Pure + 6 Hybrid) |
| Paths | 4 + NONE |

---

## Agent Configuration

| Agent | Model | Temperature | Role |
|-------|-------|-------------|------|
| Builder | Opus 4.5 | 0.3 | Autonomous coding |
| Operations | Opus 4 | 0.3 | Task orchestration |
| Scenario/Assets | Opus 4 | 0.8 | Art generation |
| Monster/Lore | Opus 4 | 0.9 | Story/creatures |
| Map Creator | Opus 4 | 0.7 | World design |
| Code Review | Opus 4 | 0.2 | Security, anti-cheat, file protection |

---

## MCP Servers (15 Active)

- godot-screenshots, godot-editor, gdai-mcp, minimal-godot, godot-docs
- git, memory, sequential-thinking
- gsap-animation, image-process, lottiefiles
- wolfram, figma, trello, fal-ai

---

## Quick Commands

```bash
# Godot path
GODOT="C:/Users/Conner/AppData/Local/Microsoft/WinGet/Packages/GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.5.1-stable_win64_console.exe"

# Check for errors
"$GODOT" --path . --check-only --headless

# Run game
"$GODOT" --path .

# Open editor
"$GODOT" --path . --editor
```

---

*Updated by Operations Agent after each significant change.*

# Refactoring Guide

> **Priority files and patterns for code consolidation**
>
> Run `scripts/tools/duplication_detector.gd` to scan the codebase

---

## Critical Files Requiring Refactoring

### 1. `ui/battle_ui_controller.gd` (4,583 lines)

**Status**: CRITICAL - This file is a monolith that needs splitting

**Anti-patterns detected:**
| Pattern | Count | Should Use |
|---------|-------|------------|
| `Label.new()` | 73 | UIStyleFactory.create_label() |
| `add_theme_color_override` | 105 | UIStyleFactory.COLOR_* |
| `add_theme_font_size_override` | 80 | UIStyleFactory.FONT_* |
| `add_theme_stylebox_override` | 57 | UIStyleFactory.apply_*_style() |
| `modulate.a =` | 14 | NodeHelpers.set_alpha() |
| **Total theme overrides** | **315+** | Centralized utilities |

**Recommended split:**
1. `battle_ui_controller.gd` - Main coordinator (keep ~500 lines)
2. `battle_party_display.gd` - Party member cards
3. `battle_enemy_display.gd` - Enemy cards and targeting
4. `battle_skill_menu.gd` - Skill selection UI
5. `battle_results_display.gd` - Victory/defeat screens
6. `battle_status_display.gd` - Status effect indicators

---

### 2. `battle/battle_arena.gd` (2,012 lines)

**Status**: HIGH - Large file with significant duplication

**Anti-patterns detected:**
| Pattern | Count | Should Use |
|---------|-------|------------|
| `PanelContainer.new()` | 6 | UIStyleFactory.create_styled_panel() |
| `Label.new()` | 6 | UIStyleFactory.create_label() |
| `add_theme_stylebox_override` | 14 | UIStyleFactory utilities |
| `.set_ease(Tween.EASE_)` | 14 | AnimationEffects.ease_*() |
| `position = Vector2` | 21 | AnimationEffects.move_to() |

**Recommended split:**
1. `battle_arena.gd` - Main arena coordination
2. `battle_arena_ui.gd` - UI element creation/management
3. `battle_arena_layout.gd` - Position calculations

---

### 3. `ui/character_select_controller.gd`

**Anti-patterns detected:**
| Pattern | Count | Should Use |
|---------|-------|------------|
| `Label.new()` | 24 | UIStyleFactory.create_label() |
| `add_theme_font_size_override` | 25 | UIStyleFactory.FONT_* |
| `add_theme_color_override` | 26 | UIStyleFactory.COLOR_* |
| `PanelContainer.new()` | 10 | UIStyleFactory.create_styled_panel() |
| `custom_minimum_size` | 21 | UIStyleFactory size helpers |

---

### 4. `battle/animation/battle_sequencer.gd`

**Anti-patterns detected:**
| Pattern | Count | Should Use |
|---------|-------|------------|
| `.has_method(` | 61 | Type checking or interfaces |
| `.emit(` | 128 | EventBus patterns |

---

## Comprehensive Pattern Summary

### Codebase-Wide Statistics

| Pattern Category | Total Occurrences | Utility Available |
|-----------------|-------------------|-------------------|
| **UI Elements** |||
| Label.new() | 115+ | UIStyleFactory |
| StyleBoxFlat.new() | 110+ | UIStyleFactory |
| Button.new() | 11+ | UIStyleFactory |
| HBoxContainer.new() | 36+ | UIStyleFactory |
| VBoxContainer.new() | 36+ | UIStyleFactory |
| PanelContainer.new() | 41+ | UIStyleFactory |
| ProgressBar.new() | 20+ | UIStyleFactory |
| **Theme Overrides** |||
| add_theme_font_size_override | 129+ | UIStyleFactory.FONT_* |
| add_theme_color_override | 178+ | UIStyleFactory.COLOR_* |
| add_theme_stylebox_override | 124+ | UIStyleFactory |
| **Node Operations** |||
| is_instance_valid() | 148+ | NodeHelpers.is_valid() |
| queue_free() | 45+ | NodeHelpers.safe_free() |
| .visible = true/false | 50+ | NodeHelpers.show/hide() |
| .instantiate() + add_child() | 35+ | NodeHelpers.instantiate_to() |
| **Animations** |||
| .set_ease(Tween.EASE_) | 137+ | AnimationEffects.ease_*() |
| modulate.a = | 69+ | NodeHelpers.set_alpha() |
| create_tween() | 165+ | AnimationEffects utilities |
| Vector2(1.0, 1.0) | 57+ | Vector2.ONE |
| **Timings** |||
| create_timer(N) | 129+ | Constants.WAIT_* |
| **Colors** |||
| Color(...) literals | 536+ | UIStyleFactory.COLOR_* |
| **Formatting** |||
| "%d/%d" % | 181+ | StringHelpers.format_*() |

**Total identified patterns**: 2,000+ consolidation opportunities

---

## Refactoring Process

### Step 1: Incremental Utility Adoption
1. Start with NEW code - always use utilities
2. When touching old code, refactor as you go
3. Run duplication_detector.gd weekly

### Step 2: File Splitting (for 1000+ line files)
1. Identify logical groupings of functions
2. Extract to new files with clear responsibilities
3. Use composition over inheritance
4. Maintain backwards compatibility with forwarding methods

### Step 3: Verification
1. Run the game and test affected features
2. Check for visual regressions
3. Verify performance (shouldn't change)

---

## Quick Wins (Low-Hanging Fruit)

These can be fixed quickly with find-replace:

1. **Vector2 constants**
   - `Vector2(1.0, 1.0)` → `Vector2.ONE`
   - `Vector2(0.0, 0.0)` → `Vector2.ZERO`

2. **Array checks**
   - `.size() > 0` → `not .is_empty()`
   - `.size() == 0` → `.is_empty()`

3. **Timer constants**
   - `create_timer(0.3)` → `create_timer(Constants.WAIT_SHORT)`
   - `create_timer(0.5)` → `create_timer(Constants.WAIT_STANDARD)`

---

*Generated: v1.08 | See also: CODE_PATTERNS.md, scripts/tools/duplication_detector.gd*

# Code Patterns Reference

> **Quick lookup for the RIGHT way to do things in VeilbreakersGame**
>
> Before writing code, CTRL+F this document for your pattern!

---

## 🚫 Anti-Pattern Detection

If you find yourself writing ANY of these, STOP and use the utility:

### StyleBoxFlat Creation
```gdscript
# 🚫 ANTI-PATTERN - Found 110+ times in codebase
var style = StyleBoxFlat.new()
style.bg_color = Color(...)
style.set_corner_radius_all(8)

# ✅ USE THIS
var style = UIStyleFactory.create_dark_panel()
var style = UIStyleFactory.create_panel_style(bg_color, border_color)
```

### Label Creation with Styling
```gdscript
# 🚫 ANTI-PATTERN - Found 100+ times in codebase
var label = Label.new()
label.text = "Hello"
label.add_theme_font_size_override("font_size", 14)
label.add_theme_color_override("font_color", Color(0.95, 0.9, 0.8))

# ✅ USE THIS
var label = UIStyleFactory.create_label("Hello", UIStyleFactory.FONT_NORMAL, UIStyleFactory.COLOR_PARCHMENT)
```

### Instance Validity + Queue Free
```gdscript
# 🚫 ANTI-PATTERN - Found 148+ times in codebase
if is_instance_valid(node):
    node.queue_free()

# ✅ USE THIS
NodeHelpers.safe_free(node)
```

### Children Cleanup Loop
```gdscript
# 🚫 ANTI-PATTERN - Found 14+ times in codebase
for child in container.get_children():
    child.queue_free()

# ✅ USE THIS
NodeHelpers.clear_children(container)
```

### HP/MP Formatting
```gdscript
# 🚫 ANTI-PATTERN - Found 181+ times in codebase
var text = "%d/%d" % [current_hp, max_hp]

# ✅ USE THIS
var text = StringHelpers.format_hp(current_hp, max_hp)
```

### Percentage Formatting
```gdscript
# 🚫 ANTI-PATTERN
var text = "%.0f%%" % (value * 100)

# ✅ USE THIS
var text = StringHelpers.format_percent(value)
```

### HP Percentage Calculation
```gdscript
# 🚫 ANTI-PATTERN - Found 50+ times in codebase
var percent = float(hp) / float(max_hp)

# ✅ USE THIS
var percent = MathHelpers.get_hp_percent(hp, max_hp)
```

### Tween Easing Setup
```gdscript
# 🚫 ANTI-PATTERN - Found 124+ times in codebase
tween.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)

# ✅ USE THIS
AnimationEffects.ease_out_back(tween)
```

### Popup Animation
```gdscript
# 🚫 ANTI-PATTERN - Found 17+ times in codebase
popup.modulate.a = 0.0
popup.scale = Vector2(0.8, 0.8)
var tween = popup.create_tween()
tween.set_parallel(true)
tween.tween_property(popup, "modulate:a", 1.0, 0.25)
tween.tween_property(popup, "scale", Vector2.ONE, 0.25)

# ✅ USE THIS
AnimationEffects.popup_entrance(popup)
```

### Button Hover Animation
```gdscript
# 🚫 ANTI-PATTERN - Found 25+ times in codebase
func _on_hover():
    create_tween().tween_property(button, "scale", Vector2(1.05, 1.05), 0.15)
    create_tween().tween_property(button, "modulate", Color(1.3, 1.1, 0.9), 0.15)

# ✅ USE THIS
func _on_hover():
    AnimationEffects.button_hover(button)
```

### Wait Timer
```gdscript
# 🚫 ANTI-PATTERN - Found 129+ times in codebase
await get_tree().create_timer(0.3).timeout
await get_tree().create_timer(0.5).timeout

# ✅ USE THIS
await get_tree().create_timer(Constants.WAIT_SHORT).timeout
await get_tree().create_timer(Constants.WAIT_STANDARD).timeout
```

### Color Literals
```gdscript
# 🚫 ANTI-PATTERN - Found 536+ times in codebase
Color(0.95, 0.9, 0.8)   # Parchment
Color(1.0, 0.85, 0.4)   # Gold
Color(0.4, 0.9, 0.4)    # HP Green
Color(0.4, 0.6, 1.0)    # MP Blue

# ✅ USE THIS
UIStyleFactory.COLOR_PARCHMENT
UIStyleFactory.COLOR_GOLD
UIStyleFactory.COLOR_HP_VALUE
UIStyleFactory.COLOR_MP_VALUE
```

### Font Size Literals
```gdscript
# 🚫 ANTI-PATTERN - Found 120+ times in codebase
add_theme_font_size_override("font_size", 14)
add_theme_font_size_override("font_size", 18)

# ✅ USE THIS
UIStyleFactory.FONT_NORMAL  # 14
UIStyleFactory.FONT_HEADING # 18
```

### Mouse Filter Assignment
```gdscript
# 🚫 ANTI-PATTERN - Found 48+ times in codebase
control.mouse_filter = Control.MOUSE_FILTER_PASS
control.mouse_filter = Control.MOUSE_FILTER_IGNORE

# ✅ USE THIS
UIStyleFactory.set_mouse_pass(control)
UIStyleFactory.set_mouse_ignore(control)
```

### Visibility with Validity Check
```gdscript
# 🚫 ANTI-PATTERN
if is_instance_valid(node):
    node.visible = true

# ✅ USE THIS
NodeHelpers.show(node)
NodeHelpers.hide(node)
```

### Scene Instantiation
```gdscript
# 🚫 ANTI-PATTERN - Found 35+ times in codebase
var instance = my_scene.instantiate()
parent.add_child(instance)

# ✅ USE THIS
var instance = NodeHelpers.instantiate_to(my_scene, parent)
```

### Container Creation
```gdscript
# 🚫 ANTI-PATTERN - Found 72+ times in codebase
var hbox = HBoxContainer.new()
var vbox = VBoxContainer.new()
var panel = PanelContainer.new()

# ✅ USE THIS
var hbox = UIStyleFactory.create_hbox(8)  # with separation
var vbox = UIStyleFactory.create_vbox(4)
var panel = UIStyleFactory.create_styled_panel(style)
```

### Vector Constants
```gdscript
# 🚫 ANTI-PATTERN - Found 57+ times in codebase
Vector2(1.0, 1.0)
Vector2(0.0, 0.0)

# ✅ USE THIS
Vector2.ONE
Vector2.ZERO
```

### Alpha/Modulate Assignment
```gdscript
# 🚫 ANTI-PATTERN - Found 69+ times in codebase
node.modulate.a = 0.5
node.modulate.a = 1.0

# ✅ USE THIS
NodeHelpers.set_alpha(node, 0.5)
AnimationEffects.fade_in(node)  # for animated
```

### Array Emptiness Check
```gdscript
# 🚫 ANTI-PATTERN - Found 39+ times in codebase
if array.size() > 0:
if array.size() == 0:

# ✅ USE THIS
if not array.is_empty():
if array.is_empty():
```

### Tween Easing Chain
```gdscript
# 🚫 ANTI-PATTERN - Found 137+ times in codebase
tween.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
tween.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)

# ✅ USE THIS
AnimationEffects.ease_out(tween)
AnimationEffects.ease_in(tween)
AnimationEffects.ease_out_back(tween)
AnimationEffects.ease_elastic(tween)
```

### Damage/Heal Formatting
```gdscript
# 🚫 ANTI-PATTERN
var text = "-%d" % damage
var text = "+%d" % heal_amount

# ✅ USE THIS
var text = StringHelpers.format_damage(damage)
var text = StringHelpers.format_heal(heal_amount)
```

---

## 🔴 Priority Refactoring Files

These files have the most anti-patterns and need attention:

| File | Lines | Issues | Priority |
|------|-------|--------|----------|
| `ui/battle_ui_controller.gd` | 4583 | 200+ | 🔴 CRITICAL |
| `battle/battle_arena.gd` | 2012 | 80+ | 🔴 HIGH |
| `ui/character_select_controller.gd` | ~800 | 50+ | 🟡 MEDIUM |
| `battle/animation/battle_sequencer.gd` | ~600 | 40+ | 🟡 MEDIUM |

**Recommendation**: Split large files into focused components before refactoring.

---

## Utility Quick Reference

### UIStyleFactory (889 lines)
```gdscript
# Labels
UIStyleFactory.create_label(text, font_size, color)
UIStyleFactory.create_centered_label(text, font_size, color)
UIStyleFactory.create_title_label(text)
UIStyleFactory.create_header_label(text)

# Bars
UIStyleFactory.create_hp_bar()
UIStyleFactory.create_mp_bar()
UIStyleFactory.create_xp_bar()
UIStyleFactory.create_corruption_bar()

# Containers
UIStyleFactory.create_vbox(separation)
UIStyleFactory.create_hbox(separation)
UIStyleFactory.create_margin_container(margin)
UIStyleFactory.create_styled_panel(style)

# Buttons
UIStyleFactory.create_button(text, font_size, min_size)
UIStyleFactory.create_menu_button(text)
UIStyleFactory.create_action_button(text)
UIStyleFactory.apply_button_style(button)

# Panels
UIStyleFactory.create_dark_panel()
UIStyleFactory.create_transparent_panel()
UIStyleFactory.create_panel_style(bg, border, border_width, radius, margin)

# Size/Mouse
UIStyleFactory.expand_horizontal(control)
UIStyleFactory.expand_vertical(control)
UIStyleFactory.set_mouse_pass(control)
UIStyleFactory.set_mouse_ignore(control)
```

### AnimationEffects (783 lines)
```gdscript
# Popups
AnimationEffects.popup_entrance(popup)
AnimationEffects.popup_exit(popup, duration, destroy)

# Buttons
AnimationEffects.button_hover(button)
AnimationEffects.button_unhover(button)
AnimationEffects.button_press(button)
AnimationEffects.button_click_bounce(button)

# Fades
AnimationEffects.fade_in(node, duration)
AnimationEffects.fade_out(node, duration)
AnimationEffects.fade_out_destroy(node, duration)
AnimationEffects.crossfade(out_node, in_node, duration)

# Flashes
AnimationEffects.flash_white(node)
AnimationEffects.flash_color(node, color)
AnimationEffects.apply_hit_flash(sprite)
AnimationEffects.apply_heal_flash(sprite)
AnimationEffects.death_animation(sprite)

# Movement
AnimationEffects.move_to(node, position, duration)
AnimationEffects.move_by(node, offset, duration)
AnimationEffects.knockback(node, direction, distance, duration)
AnimationEffects.jump(node, height, duration)
AnimationEffects.slide_in(node, direction, distance, duration)
AnimationEffects.slide_out(node, direction, distance, duration)

# Easing Helpers
AnimationEffects.ease_out_back(tween)
AnimationEffects.ease_out(tween)
AnimationEffects.ease_in(tween)
AnimationEffects.ease_in_out(tween)
AnimationEffects.ease_elastic(tween)

# Loops
AnimationEffects.breathing_loop(node)
AnimationEffects.pulse_loop(node)
AnimationEffects.color_pulse_loop(node, color)
AnimationEffects.low_hp_pulse_loop(node)

# Staggered
AnimationEffects.stagger_fade_in(nodes, delay, duration)
AnimationEffects.stagger_scale_in(nodes, delay, duration)
```

### NodeHelpers (385 lines)
```gdscript
# Cleanup
NodeHelpers.safe_free(node)
NodeHelpers.safe_free_all(nodes)
NodeHelpers.clear_children(parent)
NodeHelpers.remove_and_free(node)

# Children
NodeHelpers.get_children_of_type(parent, type)
NodeHelpers.get_first_child_of_type(parent, type)
NodeHelpers.count_children_of_type(parent, type)
NodeHelpers.for_each_child(parent, callback)

# Visibility
NodeHelpers.show(node)
NodeHelpers.hide(node)
NodeHelpers.set_visible(node, visible)
NodeHelpers.toggle_visible(node)

# Modulate
NodeHelpers.set_modulate(node, color)
NodeHelpers.reset_modulate(node)
NodeHelpers.set_alpha(node, alpha)

# Finding
NodeHelpers.safe_find_child(parent, name)
NodeHelpers.safe_get_node(node, path)
NodeHelpers.find_ancestor_of_type(node, type)

# Validation
NodeHelpers.is_valid(node)
NodeHelpers.is_valid_in_tree(node)
NodeHelpers.is_valid_visible(node)

# Instantiation
NodeHelpers.instantiate_to(scene, parent)
NodeHelpers.instantiate_at(scene, parent, position)
NodeHelpers.instantiate_deferred(scene, parent)

# Signals
NodeHelpers.safe_connect(source, signal_name, callable)
NodeHelpers.disconnect_all_signals(node, signal_name)
```

### StringHelpers (304 lines)
```gdscript
# Stats
StringHelpers.format_hp(current, max)        # "45/100"
StringHelpers.format_mp(current, max)        # "30/50"
StringHelpers.format_stat_change(value)      # "+5" or "-3"
StringHelpers.format_percent(value)          # "75%"
StringHelpers.format_level(level)            # "Lv. 10"
StringHelpers.format_xp(current, required)   # "100/500 XP"

# Battle
StringHelpers.format_damage(amount)          # "-45"
StringHelpers.format_heal(amount)            # "+30"
StringHelpers.format_attack(attacker, target, skill)
StringHelpers.format_critical(target, damage)

# Text
StringHelpers.capitalize_first(text)
StringHelpers.title_case(text)
StringHelpers.enum_to_display(enum_name)     # "SAVAGE_BRAND" -> "Savage Brand"
StringHelpers.truncate(text, max_length)
StringHelpers.pluralize(count, singular)     # "1 item" / "3 items"

# BBCode
StringHelpers.bbcode_color(text, color)
StringHelpers.bbcode_bold(text)
StringHelpers.bbcode_stat_change(value, stat_name, pos_color, neg_color)
```

### MathHelpers (228 lines)
```gdscript
MathHelpers.get_hp_percent(current, max)
MathHelpers.clamp_probability(value)         # Clamp to 0.05-0.95
MathHelpers.safe_divide(a, b, default)
MathHelpers.apply_damage_variance(damage)
MathHelpers.lerp_color(from, to, weight)
MathHelpers.calculate_level_from_xp(xp)
```

### Constants (635 lines)
```gdscript
# Wait timers
Constants.WAIT_INSTANT   # 0.1
Constants.WAIT_QUICK     # 0.2
Constants.WAIT_SHORT     # 0.3
Constants.WAIT_STANDARD  # 0.5
Constants.WAIT_LONG      # 0.8
Constants.WAIT_EXTENDED  # 1.0

# UI Animation
Constants.UI_BUTTON_HOVER  # 0.15
Constants.UI_BUTTON_PRESS  # 0.1
Constants.UI_SCENE_FADE    # 0.3
Constants.UI_MENU_SLIDE    # 0.25

# Battle Animation
Constants.ANIM_FLASH       # 0.1
Constants.ANIM_QUICK       # 0.2
Constants.ANIM_MEDIUM      # 0.3
Constants.ANIM_HALF        # 0.5
```

---

## Before Writing New Code

1. **CTRL+F this document** for your pattern
2. **Check scripts/utils/** for existing helpers
3. **Ask: "Has someone solved this before?"**
4. If you create a new pattern used 3+ times, **add it to a utility file**
5. **Run the duplication detector**: `scripts/tools/duplication_detector.gd`

---

## Duplication Statistics Summary

| Pattern Type | Occurrences | Utility |
|--------------|-------------|---------|
| Label.new() | 115+ | UIStyleFactory |
| StyleBoxFlat.new() | 110+ | UIStyleFactory |
| .set_ease(Tween.EASE_) | 137+ | AnimationEffects |
| create_timer(N) | 129+ | Constants |
| add_theme_stylebox_override | 124+ | UIStyleFactory |
| is_instance_valid() | 148+ | NodeHelpers |
| modulate.a = | 69+ | NodeHelpers/AnimationEffects |
| Vector2(1.0, 1.0) | 57+ | Vector2.ONE |
| Color(...) | 536+ | UIStyleFactory.COLOR_* |
| "%d/%d" % | 181+ | StringHelpers |

**Total identified patterns**: 1,500+ opportunities for consolidation

---

*Last updated: v1.08 | Total utility lines: 5,285 | Anti-patterns tracked: 40+*

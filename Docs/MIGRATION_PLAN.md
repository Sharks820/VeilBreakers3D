# VEILBREAKERS - Godot to Unity Migration Plan

> **Migration Status: 49% Complete** | Last Updated: 2026-01-19
>
> **Target:** 100% migration before full Unity development begins

---

## Quick Status

| Category | Progress | Status |
|----------|----------|--------|
| Core Systems | 80% | 🟢 Nearly Complete |
| Combat Systems | 70% | 🟢 Good Progress |
| Data Models | 90% | 🟢 Nearly Complete |
| UI Systems | 0% | 🔴 Not Started |
| Audio Systems | 0% | 🔴 Not Started |
| Save/Load | 0% | 🔴 Not Started |
| Managers | 20% | 🟡 In Progress |
| Utilities | 25% | 🟡 In Progress |
| Unity-Specific | 25% | 🟡 In Progress |

**Overall: 49%** (weighted average)

---

## Category Breakdown

### 1. CORE SYSTEMS (80% Complete) - Weight: 15%

| Task | Status | Notes |
|------|--------|-------|
| GameManager.cs | ✅ 100% | States, party, currency, hero selection |
| EventBus.cs | ✅ 100% | 50 events implemented |
| Constants.cs | ✅ 100% | Game constants |
| GameDatabase.cs | ✅ 100% | Data container |
| ErrorLogger | ❌ 0% | Need Unity equivalent (Debug.Log wrapper) |

**Subtotal: 80%**

---

### 2. COMBAT SYSTEMS (70% Complete) - Weight: 20%

| Task | Status | Notes |
|------|--------|-------|
| BattleManager.cs | ✅ 100% | Real-time battle, party swap |
| DamageCalculator.cs | ✅ 100% | Brand-aware damage |
| Combatant.cs | ✅ 100% | Base combatant class |
| AIController.cs | ❌ 0% | Need to port AI logic |
| StatusEffectManager.cs | ❌ 0% | OVERHAUL - Use ScriptableObjects |
| TurnManager.cs | ❌ 0% | May not need (real-time now) |
| CombatTestSetup.cs | ✅ 100% | Test harness ready |

**Subtotal: 70%**

---

### 3. GAME SYSTEMS (75% Complete) - Weight: 15%

| Task | Status | Notes |
|------|--------|-------|
| BrandSystem.cs | ✅ 100% | 10-brand effectiveness matrix |
| SynergySystem.cs | ✅ 100% | Tiered path/brand synergy |
| CorruptionSystem.cs | ✅ 100% | 5 states, stat modifiers |
| PathSystem.cs | ✅ 100% | 4 paths implemented |
| CaptureSystem.cs | ❌ 0% | Post-battle capture with QTE |
| VERASystem.cs | ❌ 0% | AI dialogue system |

**Subtotal: 75%**

---

### 4. DATA MODELS (90% Complete) - Weight: 10%

| Task | Status | Notes |
|------|--------|-------|
| Enums.cs | ✅ 100% | 16 enums (Brand, Path, etc.) |
| AbilityData.cs | ✅ 100% | 6-slot ability structure |
| HeroData.cs | ✅ 100% | Hero definitions |
| MonsterData.cs | ✅ 100% | Monster definitions |
| SkillData.cs | ✅ 100% | Skill definitions |
| ItemData.cs | ✅ 100% | Item definitions |
| StatusEffectData.cs | ❌ 0% | ScriptableObject for effects |

**Subtotal: 90%**

---

### 5. UI SYSTEMS (0% Complete) - Weight: 15%

> **OVERHAUL:** Using Unity UI Toolkit instead of porting UIStyleFactory

| Task | Status | Notes |
|------|--------|-------|
| UI Toolkit Setup | ❌ 0% | USS stylesheets, UXML templates |
| Main Menu | ❌ 0% | New Game, Continue, Settings, Exit |
| Battle UI | ❌ 0% | Party sidebars, action bar, combat log |
| Party Sidebar | ❌ 0% | HP/MP bars, portraits |
| Enemy Sidebar | ❌ 0% | HP bars, targeting |
| Action Bar | ❌ 0% | 6-slot ability bar |
| Combat Log | ❌ 0% | Scrollable damage/skill log |
| Turn Order Display | ❌ 0% | Character portraits + arrows |
| Status Effect Icons | ❌ 0% | Above-character icons |
| Character Select | ❌ 0% | Hero selection screen |
| Inventory UI | ❌ 0% | Item management |
| Monster Collection UI | ❌ 0% | Monster roster |
| Settings UI | ❌ 0% | Audio, graphics, controls |
| Dialogue UI | ❌ 0% | VERA conversation panels |

**Subtotal: 0%**

---

### 6. AUDIO SYSTEMS (0% Complete) - Weight: 5%

> **OVERHAUL:** Using Unity Audio Mixer instead of custom AudioManager

| Task | Status | Notes |
|------|--------|-------|
| Audio Mixer Setup | ❌ 0% | Master, Music, SFX, Voice groups |
| Audio Snapshots | ❌ 0% | Combat, Exploration, Menu states |
| Music System | ❌ 0% | BGM with crossfade |
| SFX System | ❌ 0% | Pooled audio sources |
| AudioManager.cs | ❌ 0% | Wrapper for mixer control |

**Subtotal: 0%**

---

### 7. SAVE/LOAD SYSTEMS (0% Complete) - Weight: 5%

> **OVERHAUL:** Using ScriptableObjects + JsonUtility

| Task | Status | Notes |
|------|--------|-------|
| SaveData Structure | ❌ 0% | Define save file format |
| SaveManager.cs | ❌ 0% | Save/Load/Delete operations |
| Auto-Save System | ❌ 0% | Checkpoint-based auto-save |
| Save Slots | ❌ 0% | Multiple save slot support |
| Settings Persistence | ❌ 0% | PlayerPrefs for settings |

**Subtotal: 0%**

---

### 8. MANAGERS (20% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| GameManager.cs | ✅ 100% | Already in Core |
| SceneManager.cs | ❌ 0% | Scene transitions, loading screens |
| SettingsManager.cs | ❌ 0% | Graphics, audio, control settings |
| InventoryManager.cs | ❌ 0% | Item management |

**Subtotal: 20%**

---

### 9. UTILITY CLASSES (0% Complete) - Weight: 5%

> **OVERHAUL:** Most Godot utilities replaced by Unity/C# built-ins

| Task | Status | Notes |
|------|--------|-------|
| DOTween Integration | ❌ 0% | Replaces AnimationEffects |
| ObjectPool.cs | ✅ 100% | Generic pooling with IPoolable interface |
| Extensions.cs | ❌ 0% | C# extension methods |
| Helpers.cs | ❌ 0% | Minimal utility functions |

**Subtotal: 25%**

---

### 10. UNITY-SPECIFIC SYSTEMS (10% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| Project Structure | ✅ 100% | Folders created |
| Addressables Setup | ❌ 0% | Async asset loading |
| Object Pooling | ✅ 100% | Generic ObjectPool<T> with IPoolable |
| New Input System | ❌ 0% | Rebindable controls |
| Cinemachine Setup | ❌ 0% | Camera system |
| Timeline Setup | ❌ 0% | Cutscene/animation system |
| NavMesh Setup | ❌ 0% | 3D pathfinding |
| Shader Graph | ❌ 0% | Visual shaders |

**Subtotal: 25%**

---

## Calculation Formula

```
Overall % = (Core×15 + Combat×20 + Systems×15 + Data×10 + UI×15 +
             Audio×5 + Save×5 + Managers×5 + Utils×5 + Unity×5) / 100

Current:  = (80×15 + 70×20 + 75×15 + 90×10 + 0×15 +
             0×5 + 0×5 + 20×5 + 25×5 + 25×5) / 100
          = (1200 + 1400 + 1125 + 900 + 0 + 0 + 0 + 100 + 125 + 125) / 100
          = 4975 / 100
          = 49.75% ≈ 49%
```

**Corrected Overall: 49% Complete**

---

## Priority Order for Remaining Work

### Phase 1: Critical Path (Get to Playable)
1. **UI Systems** - Can't test without UI
2. **Audio Systems** - Essential for feel
3. **Save/Load** - Need persistence

### Phase 2: Combat Complete
4. **AIController** - Enemy behavior
5. **StatusEffectManager** - Buffs/debuffs
6. **CaptureSystem** - Post-battle capture

### Phase 3: Polish
7. **VERASystem** - AI dialogue
8. **Unity-Specific** - Performance, cameras
9. **Utilities** - Quality of life

---

## Godot Reference Files (DO NOT DELETE)

These files contain game logic that needs to be understood for migration:

| Godot File | Purpose | Unity Target |
|------------|---------|--------------|
| `scripts/ui/ui_style_factory.gd` | UI patterns | UI Toolkit reference |
| `scripts/utils/animation_effects.gd` | Animation patterns | DOTween reference |
| `scripts/battle/ai_controller.gd` | AI logic | AIController.cs |
| `scripts/battle/status_effect_manager.gd` | Effect logic | StatusEffectManager.cs |
| `scripts/vera/vera_system.gd` | Dialogue logic | VERASystem.cs |
| `scripts/capture/capture_system.gd` | Capture logic | CaptureSystem.cs |

> **Location:** `Docs/LEGACY_Godot/` - Keep until migration complete

---

## Daily Update Protocol

Each session, update this file:
1. Check off completed tasks (❌ → ✅)
2. Update percentage for each category
3. Recalculate overall percentage
4. Update "Last Updated" date in header
5. Add notes for any blockers or decisions

---

## Migration Complete Criteria

Migration is **100% COMPLETE** when:
- [ ] All categories show 100%
- [ ] Game is playable start-to-finish in Unity
- [ ] No Godot code dependencies remain
- [ ] All UI functional
- [ ] Save/Load working
- [ ] Audio playing correctly
- [ ] Combat fully functional with AI
- [ ] VERA system operational

**Then:** Archive `Docs/LEGACY_Godot/` and begin full Unity development.

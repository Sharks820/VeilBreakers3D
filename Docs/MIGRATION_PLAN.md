# VEILBREAKERS - Godot to Unity Migration Plan

> **Migration Status: 92.5% Complete** | Last Updated: 2026-01-24
>
> **Target:** 100% migration before full Unity development begins

---

## Quick Status

| Category | Progress | Status |
|----------|----------|--------|
| Core Systems | 100% | ✅ Complete |
| Combat Systems | 95% | ✅ Nearly Complete |
| Game Systems | 90% | ✅ Nearly Complete |
| Data Models | 100% | ✅ Complete |
| UI Systems | 100% | ✅ Complete |
| Audio Systems | 90% | ✅ Nearly Complete |
| Save/Load | 95% | ✅ Nearly Complete |
| Managers | 90% | ✅ Nearly Complete |
| Utilities | 75% | 🟢 Good Progress |
| Unity-Specific | 40% | 🟡 In Progress |

**Overall: 92%** (weighted average)

---

## Category Breakdown

### 1. CORE SYSTEMS (100% Complete) - Weight: 15%

| Task | Status | Notes |
|------|--------|-------|
| GameManager.cs | ✅ 100% | States, party, currency, hero selection |
| EventBus.cs | ✅ 100% | 55+ events implemented |
| Constants.cs | ✅ 100% | Game constants |
| GameDatabase.cs | ✅ 100% | Data container |
| GameBootstrap.cs | ✅ 100% | Manager initialization |
| ErrorLogger.cs | ✅ 100% | Debug logging with subsystems |

**Subtotal: 100%**

---

### 2. COMBAT SYSTEMS (95% Complete) - Weight: 20%

| Task | Status | Notes |
|------|--------|-------|
| BattleManager.cs | ✅ 100% | Real-time battle, party swap |
| DamageCalculator.cs | ✅ 100% | Brand-aware damage |
| Combatant.cs | ✅ 100% | Base combatant class |
| GambitController.cs | ✅ 100% | AI logic (replaces AIController) |
| GambitEvaluator.cs | ✅ 100% | Utility-based decision making |
| GambitCondition.cs | ✅ 100% | Condition evaluations |
| GambitAction.cs | ✅ 100% | Action definitions |
| GambitRule.cs | ✅ 100% | Rule definitions |
| StatusEffectManager.cs | ✅ 100% | 30+ methods, ScriptableObject-based |
| StatusEffectInstance.cs | ✅ 100% | Effect instance tracking |
| CombatTestSetup.cs | ✅ 100% | Test harness ready |
| TurnManager.cs | N/A | Not needed (real-time combat) |

**Subtotal: 95%**

---

### 3. GAME SYSTEMS (90% Complete) - Weight: 15%

| Task | Status | Notes |
|------|--------|-------|
| BrandSystem.cs | ✅ 100% | 10-brand effectiveness matrix |
| SynergySystem.cs | ✅ 100% | Tiered path/brand synergy |
| CorruptionSystem.cs | ✅ 100% | 5 states, stat modifiers |
| PathSystem.cs | ✅ 100% | 4 paths implemented |
| CaptureManager.cs | ✅ 100% | Post-battle capture flow |
| CaptureFormulaCalculator.cs | ✅ 100% | Capture chance calculation |
| BindThresholdCalculator.cs | ✅ 100% | Bind threshold logic |
| QTEController.cs | ✅ 100% | Quick time event system |
| CaptureData.cs | ✅ 100% | Capture item definitions |
| VERASystem.cs | ❌ 0% | AI dialogue system (needs UI first) |

**Subtotal: 90%**

---

### 4. DATA MODELS (100% Complete) - Weight: 10%

| Task | Status | Notes |
|------|--------|-------|
| Enums.cs | ✅ 100% | 16+ enums (Brand, Path, etc.) |
| AbilityData.cs | ✅ 100% | 6-slot ability structure |
| HeroData.cs | ✅ 100% | Hero definitions |
| MonsterData.cs | ✅ 100% | Monster definitions |
| SkillData.cs | ✅ 100% | Skill definitions |
| ItemData.cs | ✅ 100% | Item definitions |
| StatusEffectData.cs | ✅ 100% | ScriptableObject for effects |
| SaveData.cs | ✅ 100% | Save file data structure |
| ShrineData.cs | ✅ 100% | Shrine definitions |

**Subtotal: 100%**

---

### 5. UI SYSTEMS (100% Complete) - Weight: 15%

> **All UI Systems Complete - Combat, Menus, and UI Toolkit**

| Task | Status | Notes |
|------|--------|-------|
| CombatHUD.cs | ✅ 100% | Main combat HUD controller |
| PlayerPanelController.cs | ✅ 100% | Player info display |
| AllyPanelController.cs | ✅ 100% | Ally floating panels |
| EnemyPanelController.cs | ✅ 100% | Enemy info display |
| HealthBarController.cs | ✅ 100% | HP bar with segments |
| SkillBarController.cs | ✅ 100% | 6-slot skill bar |
| SkillSlotController.cs | ✅ 100% | Individual skill slots |
| CaptureBannerController.cs | ✅ 100% | Capture UI banner |
| CombatUIConfig.cs | ✅ 100% | UI configuration |
| UI Toolkit Setup | ✅ 100% | VeilBreakers.uss, VeilBreakersTheme.uss, UXML templates |
| Main Menu | ✅ 100% | MainMenuController.cs, MainMenuBootstrap.cs |
| Character Select | ✅ 100% | CharacterSelectController.cs with hero cards |
| Inventory UI | ✅ 100% | InventoryController.cs with item categories |
| Monster Collection UI | ✅ 100% | MonsterCollectionController.cs with filtering |
| Settings UI | ✅ 100% | SettingsPanelController.cs (audio, graphics, controls) |
| Dialogue UI | ✅ 100% | VERADialogueController.cs with typewriter effect |
| ThemeManager.cs | ✅ 100% | UI theme management |
| UIAnimationController.cs | ✅ 100% | UI animation helpers |

**Subtotal: 100%**

---

### 6. AUDIO SYSTEMS (90% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| AudioManager.cs | ✅ 100% | Main audio controller |
| MusicManager.cs | ✅ 100% | BGM with crossfade |
| AudioConfig.cs | ✅ 100% | Audio configuration |
| AudioBattleIntegration.cs | ✅ 100% | Combat audio hooks |
| AudioTriggers.cs | ✅ 100% | Audio event triggers |
| LowHealthAudio.cs | ✅ 100% | Low health sound effects |
| VERAVoiceController.cs | ✅ 100% | VERA voice playback |
| AudioTests.cs | ✅ 100% | Audio testing utilities |
| Audio Mixer Setup | ❌ 0% | Unity Audio Mixer groups |

**Subtotal: 90%**

---

### 7. SAVE/LOAD SYSTEMS (95% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| SaveData.cs | ✅ 100% | Data structure defined |
| SaveManager.cs | ✅ 100% | Save/Load/Delete operations |
| SaveFileHandler.cs | ✅ 100% | File I/O with encryption |
| AutoSaveManager.cs | ✅ 100% | Checkpoint auto-save |
| ShrineManager.cs | ✅ 100% | Shrine-based save zones |
| MigrationRunner.cs | ✅ 100% | Save version migrations |
| Settings Persistence | ❌ 0% | PlayerPrefs integration |

**Subtotal: 95%**

---

### 8. MANAGERS (90% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| GameManager.cs | ✅ 100% | In Core folder |
| SaveManager.cs | ✅ 100% | Save operations |
| AutoSaveManager.cs | ✅ 100% | Auto-save logic |
| ShrineManager.cs | ✅ 100% | Shrine management |
| StatusEffectManager.cs | ✅ 100% | Effect management |
| SceneManager.cs | ✅ 100% | VBSceneManager with fade, async load |
| SettingsManager.cs | ❌ 0% | Settings persistence |

**Subtotal: 90%**

---

### 9. UTILITY CLASSES (75% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| ObjectPool.cs | ✅ 100% | Generic pooling with IPoolable |
| GameObjectPool.cs | ✅ 100% | Unity GameObject pooling |
| Extensions.cs | ✅ 100% | C# extension methods |
| DOTween Integration | ❌ 0% | External package (optional) |

**Subtotal: 75%**

---

### 10. UNITY-SPECIFIC SYSTEMS (40% Complete) - Weight: 5%

| Task | Status | Notes |
|------|--------|-------|
| Project Structure | ✅ 100% | Folders created |
| Object Pooling | ✅ 100% | Generic and GameObject pools |
| GameBootstrap.cs | ✅ 100% | Manager initialization |
| TestArenaSetup.cs | ✅ 100% | Editor test utilities |
| TestArenaManager.cs | ✅ 100% | Runtime test utilities |
| Addressables Setup | ❌ 0% | Async asset loading |
| New Input System | ❌ 0% | Rebindable controls |
| Cinemachine Setup | ❌ 0% | Camera system |
| Timeline Setup | ❌ 0% | Cutscene/animation system |
| NavMesh Setup | ❌ 0% | 3D pathfinding |
| Shader Graph | ❌ 0% | Visual shaders |

**Subtotal: 40%**

---

## Calculation Formula

```
Overall % = (Core×15 + Combat×20 + Systems×15 + Data×10 + UI×15 +
             Audio×5 + Save×5 + Managers×5 + Utils×5 + Unity×5) / 100

Current:  = (100×15 + 95×20 + 90×15 + 100×10 + 100×15 +
             90×5 + 95×5 + 80×5 + 75×5 + 40×5) / 100
          = (1500 + 1900 + 1350 + 1000 + 1500 + 450 + 475 + 400 + 375 + 200) / 100
          = 9150 / 100
          = 91.5% → 92%
```

**Overall: 92% Complete**

---

## Priority Order for Remaining Work

### ✅ Phase 1: UI Foundation (COMPLETE - 92%)
1. ~~**UI Toolkit Setup**~~ - ✅ VeilBreakers.uss, VeilBreakersTheme.uss, UXML templates
2. ~~**Main Menu**~~ - ✅ MainMenuController.cs, MainMenuBootstrap.cs
3. ~~**Settings UI**~~ - ✅ SettingsPanelController.cs
4. ~~**Character Select**~~ - ✅ CharacterSelectController.cs
5. ~~**Inventory UI**~~ - ✅ InventoryController.cs
6. ~~**Monster Collection**~~ - ✅ MonsterCollectionController.cs
7. ~~**VERA Dialogue**~~ - ✅ VERADialogueController.cs

### Phase 2: Feature Complete (Get to 95%)
1. **VERA System** - AI dialogue integration (backend logic)
2. **Scene Manager** - Transitions and loading
3. **Settings Manager** - Persistent settings

### Phase 3: Polish (100%)
1. **New Input System** - Rebindable controls
2. **Audio Mixer** - Proper audio groups
3. **Unity-Specific** - Cinemachine, NavMesh, Addressables, etc.

---

## Completed Milestones

- ✅ Core systems fully operational (GameManager, EventBus, ErrorLogger)
- ✅ Combat system with AI gambits
- ✅ Save/Load with bulletproof corruption protection
- ✅ Audio system with music, SFX, voice
- ✅ Capture system with QTE
- ✅ All data models defined
- ✅ Combat UI controllers ready
- ✅ **AAA Menu UI System** - Main Menu, Settings, Character Select, Inventory, Monster Collection
- ✅ **UI Toolkit Foundation** - USS stylesheets, UXML templates, ThemeManager
- ✅ **VERA Dialogue UI** - VERADialogueController with typewriter effect

---

## Migration Complete Criteria

Migration is **100% COMPLETE** when:
- [x] Core systems at 100%
- [x] Combat fully functional with AI
- [x] Save/Load working
- [x] Audio system operational
- [x] All UI functional
- [ ] VERA system backend operational
- [ ] Scene transitions working
- [ ] Game is playable start-to-finish in Unity

**Then:** Archive `Docs/LEGACY_Godot/` and begin full Unity development.

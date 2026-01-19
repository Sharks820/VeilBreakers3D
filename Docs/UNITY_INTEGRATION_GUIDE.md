# VeilBreakers Unity Integration Guide

> **Version:** 1.0 | **Date:** 2026-01-19

---

## Quick Start

### 1. Open Unity Project
```
Unity Hub → Add → Select VeilBreakers3D folder
Open with Unity 2022.3 LTS or newer
```

### 2. Create Test Arena Scene
In Unity Editor:
1. Go to `VeilBreakers → Create Test Arena Scene` (menu)
2. This creates `Assets/Scenes/Test/TestArena.unity`
3. The scene includes:
   - GameBootstrap (initializes all managers)
   - TestArenaManager (spawns test battles)
   - Arena floor and spawn points
   - UI Canvas placeholder

### 3. Run Test Battle
1. Open `Assets/Scenes/Test/TestArena.unity`
2. Press Play
3. Battle auto-starts with test combatants
4. Use context menu options on TestArenaManager for debugging

---

## System Architecture

### Manager Initialization Order
```
GameBootstrap initializes in this order:
1. GameManager (singleton)
2. EventBus (static class - always available)
3. GameDatabase (loads JSON data)
4. AudioManager (sound system)
5. MusicManager (adaptive music)
6. VERAVoiceController (AI companion voice)
7. LowHealthAudio (heartbeat effects)
8. BattleManager (combat)
9. AudioBattleIntegration (connects audio to combat)
```

### Key Singletons

| Manager | Access | Purpose |
|---------|--------|---------|
| GameManager | `GameManager.Instance` | Game state, party, currency |
| BattleManager | `BattleManager.Instance` | Combat logic, turns |
| AudioManager | `AudioManager.Instance` | Sound effects, music |
| MusicManager | `MusicManager.Instance` | Adaptive music layers |
| GameDatabase | `GameDatabase.Instance` | Monster/skill/hero data |

### Static Classes (No Instance)

| Class | Purpose |
|-------|---------|
| EventBus | Game-wide event system |
| BrandSystem | Brand effectiveness (2x/0.5x/1x) |
| SynergySystem | Path-brand synergy tiers |
| DamageCalculator | Combat math |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, EventBus, Constants, GameBootstrap
│   ├── Combat/         # BattleManager, DamageCalculator, Combatant
│   ├── Systems/        # BrandSystem, SynergySystem, CorruptionSystem
│   ├── Data/           # Enums, ScriptableObjects, SaveData
│   ├── AI/             # GambitController, GambitEvaluator
│   ├── Audio/          # AudioManager, MusicManager, VERAVoice
│   ├── Commands/       # QuickCommand, RadialMenu
│   ├── Capture/        # CaptureManager, QTEController
│   ├── UI/Combat/      # CombatHUD, HealthBar, SkillBar
│   ├── Managers/       # SaveManager, AutoSaveManager
│   ├── Test/           # CombatTestSetup, TestArenaManager
│   └── Editor/         # TestArenaSetup (editor tools)
├── Data/
│   └── *.json          # monsters.json, skills.json, heroes.json
├── Resources/Data/     # Runtime-loadable JSON
├── Scenes/Test/        # TestArena.unity
└── Prefabs/            # (To be created)
```

---

## Testing in Editor

### Run All Combat Tests
```
VeilBreakers → Run All System Tests (menu)
```
Tests:
- Brand effectiveness matrix (10 brands)
- Synergy tier calculation
- Damage calculation with multipliers
- Ability loadout and cooldowns
- Combatant lifecycle (HP, MP, death, revive)

### System Health Check
```
VeilBreakers → System Health Check (menu)
```
Verifies all managers are initialized correctly.

### TestArenaManager Debug Commands
Right-click TestArenaManager in Inspector:
- `Spawn Test Battle` - Start new battle
- `Force End Battle - Victory` - Kill all enemies
- `Force End Battle - Defeat` - Kill player party
- `Damage All Enemies (50%)` - Deal half HP to enemies
- `Heal All Party` - Full heal party
- `Log Battle State` - Print battle info to console

---

## Creating Prefabs

### Combatant Prefab
1. Create empty GameObject
2. Add `Combatant` component
3. Configure in inspector or via code:
```csharp
combatant.Initialize(id, name, brand, hp, mp, atk, def, mag, res, spd, isPlayer);
combatant.SetLevel(level);
combatant.SetRarity(rarity);
combatant.SetCorruption(corruption);
```

### UI Prefabs Needed
- `HealthBar.prefab` - Uses HealthBarController
- `SkillSlot.prefab` - Uses SkillSlotController
- `PlayerPanel.prefab` - Uses PlayerPanelController
- `EnemyPanel.prefab` - Uses EnemyPanelController
- `CombatHUD.prefab` - Main battle UI

---

## Audio System Setup

### FMOD/Wwise Integration
The audio system is designed for FMOD/Wwise but works without them:
- Bank loading methods are stubs (implement when audio middleware added)
- Event paths use FMOD convention: `event:/Category/Sound`
- Memory budget tracking ready

### Audio Events to Implement
```
event:/SFX/Combat/Hit_Light
event:/SFX/Combat/Hit_Medium
event:/SFX/Combat/Hit_Heavy
event:/SFX/Combat/Critical
event:/SFX/Combat/Block
event:/SFX/Combat/Miss
event:/SFX/Combat/Heal
event:/Music/Combat_Low
event:/Music/Combat_High
event:/Music/Victory
event:/Music/Defeat
event:/VERA/Clean/*
event:/VERA/Glitched/*
event:/VERA/Corrupted/*
```

---

## Known Issues / TODO

### High Priority
1. UI prefabs not created - need visual implementation
2. Audio banks not implemented - stubs only
3. No 3D models - using primitives for testing

### Medium Priority
1. Save/Load not wired to UI
2. Capture QTE needs visual feedback
3. Gambit AI needs tuning

### Low Priority
1. VFX prefabs empty
2. Monster prefabs need art
3. Zone audio triggers need level design

---

## Performance Optimization

### Already Implemented
- Object pooling in ObjectPool.cs
- Event-driven architecture (no polling)
- Cached components in managers
- Pre-allocated lists for frequent operations

### Unity Settings Recommendations
```
Edit → Project Settings → Player:
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1

Edit → Project Settings → Quality:
- VSync: Every V Blank
- Anti-Aliasing: 4x MSAA
- Shadow Distance: 50

Edit → Project Settings → Time:
- Fixed Timestep: 0.02 (50 FPS physics)
```

---

## Next Steps

1. **Art Integration** - Add 3D models for characters/monsters
2. **UI Implementation** - Build out CombatHUD with proper visuals
3. **Audio Middleware** - Integrate FMOD or Wwise
4. **Scene Flow** - Create main menu, character select scenes
5. **VERA System** - Implement AI companion dialogue
6. **Vertical Slice** - Complete one full battle loop

---

*Last updated: 2026-01-19*

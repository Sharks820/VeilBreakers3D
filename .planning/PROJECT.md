# VeilBreakers 3D

## What This Is

VeilBreakers is a 3D monster RPG where players capture and battle corrupted creatures in a dark fantasy world. Players choose a hero, build a party of monsters with brand-based synergies, and navigate a corruption system that rewards purification over power. The game targets AAA visual quality as an AI-assisted indie title.

## Core Value

The game flow from title screen through character selection to gameplay must work flawlessly with AAA-quality visuals at every step.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. Inferred from existing codebase analysis 2026-02-21. -->

- [x] 10-Brand combat system with 2x/0.5x/1x effectiveness matrix (IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID + 6 hybrids) — `BrandSystem.cs`
- [x] 4-Path stat bonus system (IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED) — `PathSystem.cs`
- [x] Corruption system (0-100%) with 5 tiers affecting stat multipliers — `CorruptionSystem.cs`
- [x] Synergy system with party composition bonuses (FULL/PARTIAL/NEUTRAL/ANTI) — `SynergySystem.cs`
- [x] VERA AI companion with veil integrity degradation and glitch effects — `VERASystem.cs`
- [x] Real-time cooldown-based combat with damage calculation pipeline — `BattleManager.cs`, `DamageCalculator.cs`
- [x] Monster capture mechanics — `Assets/Scripts/Capture/`
- [x] Combat AI with gambit-based decision making — `CombatAI.cs`, `GambitController.cs`
- [x] Status effect system with manager and data definitions — `StatusEffectManager.cs`, `StatusEffectData.cs`
- [x] Save system with AES encryption, GZip compression, HMAC-SHA256 integrity, atomic writes — `SaveManager.cs`, `SaveFileHandler.cs`
- [x] Bootstrap → phased singleton initialization (13+ managers) — `GameBootstrap.cs`
- [x] Async scene management with fade transitions — `VBSceneManager.cs`
- [x] GameDatabase with async parallel JSON loading (monsters, skills, heroes, items) — `GameDatabase.cs`
- [x] EventBus for decoupled game-wide communication (50+ events) — `EventBus.cs`
- [x] SingletonMonoBehaviour<T> infrastructure with DontDestroyOnLoad — `SingletonMonoBehaviour.cs`
- [x] UI Toolkit screens: MainMenu, CharacterSelect, Inventory, MonsterCollection, VERA Dialogue, Combat HUD — `Assets/Scripts/UI/`
- [x] Input system with gamepad support and action map switching — `InputManager.cs`
- [x] Theme system with per-hero styling and brand colors — `ThemeManager.cs`
- [x] HeroDisplayConfig ScriptableObjects for character select data — `Assets/Resources/CharacterSelect/`
- [x] Audio system with battle integration and music crossfading — `AudioManager.cs`, `MusicManager.cs`
- [x] Save migration framework with versioning — `MigrationRunner.cs`
- [x] Error logging with subsystem prefixes and conditional compilation — `ErrorLogger.cs`

### Active

<!-- Current scope. Building toward these. -->

- [ ] Character Select screen rebuilt with AAA visuals (layout, animations, effects)
- [ ] Character Select buttons functional (Back, Prev, Next, Embark, Confirm, Cancel)
- [ ] Character Select layout fixed (text overlap, monster area, hero stories, synergy/brands display)
- [ ] Character Select performance optimized (no GC pressure, cached references, no per-frame allocations)
- [ ] Title screen loading order fixed (title loads first, no battle screen flash)
- [ ] Settings button wired and functional from main menu
- [ ] Full codebase debug pass (zero errors, zero warnings where possible)
- [ ] Security audit pass (no injection vectors, no unsafe patterns, proper encryption)
- [ ] Visual amplification of title screen (AAA quality without code degradation)
- [ ] Visual amplification of character select (AAA quality effects and animations)
- [ ] Tech debt cleanup (duplicate USS files, stale backups, deprecated events, stub components)
- [ ] Game flow verified end-to-end: Bootstrap → MainMenu → CharacterSelect → Overworld

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- Multiplayer/online features — single-player RPG, no server infrastructure
- Mobile/console ports — Windows standalone is the target platform for now
- New hero characters — focus on fixing existing 4 heroes (Vex, Seraphina, Orion, Nyx)
- New monster species — existing monster data is sufficient for current milestone
- Inventory/MonsterCollection save integration — TODOs exist but not part of this rebuild milestone
- Overworld gameplay implementation — scene exists but out of scope for character select rebuild
- New combat abilities or balance changes — combat system is validated, don't modify

## Context

**Brownfield project** with ~50+ C# scripts, 6 scenes, 4 hero characters, and complete combat/save/AI systems. The game compiles and runs but has visual bugs, broken UI interactions, and loading order issues in the character select and title screen flows.

**Codebase state (2026-02-21):**
- Unity 6000.3.6f1 with URP 17.3.0
- UI Toolkit for all screens (UXML/USS, not legacy UGUI)
- Character Select has 8 controllers + 1 event bus, zero test coverage
- 4 duplicate/stale CharacterSelect USS stylesheets causing confusion
- 2 overlapping global USS files (VeilBreakers.uss vs VeilBreakersUI.uss)
- TransitionController is an empty stub subscribed to events
- 20+ TODO comments indicating unfinished integration points
- Legacy Input API (`Input.mousePosition`) mixed with New Input System
- FPSCounter and LowHealthAudio use inconsistent singleton patterns
- HeroDisplayConfigs all have null modelPrefab (placeholders active)
- `_Recovery/0.unity` junk file from crash recovery

**Key technical decisions already made:**
- Event-driven architecture with static EventBus
- ScriptableObjects for all game data configurations
- Namespace convention: `VeilBreakers.[Category]`
- Code style: `_private`, `kConstant`, `PascalProperty`, `OnEvent`

## Constraints

- **Engine**: Unity 6000.3.6f1 — locked, do not upgrade mid-project
- **Rendering**: URP 17.3.0 — all visual work must use URP-compatible shaders/effects
- **UI Framework**: UI Toolkit only — no legacy UGUI or IMGUI for new UI code
- **Platform**: Windows Standalone primary — 1920x1080 minimum, D3D11/Vulkan
- **Performance**: No allocations in Update/hot paths — use cached references, pre-allocated buffers
- **Architecture**: Singleton pattern via `SingletonMonoBehaviour<T>` — no custom singleton implementations
- **Input**: Route all input through `InputManager` wrapping `VeilBreakersInputActions` — no direct `Input.*` calls
- **Security**: Save files use AES-CBC + HMAC-SHA256 — maintain encryption integrity on any save format changes

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| UI Toolkit over UGUI | Modern Unity UI, better CSS-like styling, runtime binding | -- Pending |
| Static EventBus over UnityEvents | Decoupled communication, type-safe, easy to subscribe/unsubscribe | -- Pending |
| ScriptableObjects for data configs | Inspector-friendly, asset workflow, no code changes for tuning | [x] Good |
| AES encryption for saves | Prevent casual tampering in single-player RPG | [x] Good |
| Coroutine-Task bridge for async | Unity coroutines can't await Tasks natively; busy-wait polling used | [!] Revisit |
| HeroDisplayConfig per hero | Each hero has unique visual config (colors, descriptions, stats) | [x] Good |
| CharSelectEvents scoped bus | Prevents character select controllers from polluting global EventBus | [x] Good |
| 4 parallel codebase mapper agents | Faster analysis, each agent writes directly to reduce context load | [x] Good |

---
*Last updated: 2026-02-21 after GSD project initialization (brownfield)*

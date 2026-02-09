# VeilBreakers 3D - Comprehensive Codebase Audit
**Date:** 2026-02-08
**Scope:** Performance, bugs, character select issues, startup glitch, test coverage

---

## 1. PERFORMANCE FINDINGS

### CRITICAL (Fix Now)

| # | Issue | File | Impact |
|---|-------|------|--------|
| P1 | **SoulSwarmVFX: 180 style mutations/frame** | `Scripts/UI/Core/SoulSwarmVFX.cs:392-407` | ~2-4ms/frame. `style.left`/`style.top` triggers layout recalc per particle. Switch to `style.translate`. |
| P2 | **ParallaxBackground: unconditional style updates** | `Scripts/UI/Core/ParallaxBackground.cs:145-153` | 4 style writes/frame even when mouse hasn't moved. Add dirty check. |

### RECOMMENDED (Fix Soon)

| # | Issue | File | Impact |
|---|-------|------|--------|
| P3 | **VERADialogueController: WaitForSeconds allocs** | `Scripts/UI/Menus/VERADialogueController.cs:410,418` | ~40-80B per iteration at 10-20Hz during glitch. Cache or use manual timer. |
| P4 | **HeroStageController: 6 sync Resources.Load per switch** | `Scripts/UI/CharacterSelect/HeroStageController.cs:608-648` | Blocks main thread. Pre-cache hero prefabs on init. |
| P5 | **AudioTriggerCombatTension: Physics query every frame** | `Scripts/Audio/AudioTriggers.cs:381-397` | ~0.3-0.5ms/frame for audio param that changes slowly. Throttle to ~10Hz. |
| P6 | **MusicManager: uncached WaitForSeconds** | `Scripts/Audio/MusicManager.cs:300,347` | Minor GC pressure on state transitions. |

### LOW PRIORITY

| # | Issue | File | Impact |
|---|-------|------|--------|
| P7 | SkillSlotController: uncached WaitForSeconds(0.1f) | `Scripts/UI/Combat/SkillSlotController.cs:417` | 1 alloc per skill use |
| P8 | FPSCounter: string/struct allocs in OnGUI | `Scripts/UI/Core/FPSCounter.cs:62-72` | Debug tool only |
| P9 | AudioBattleIntegration: polling instead of event-driven | `Scripts/Audio/AudioBattleIntegration.cs:65-69` | 3 singleton lookups/frame |
| P10 | StatusEffectManager: Dictionary.Keys enumerator alloc | `Scripts/Managers/StatusEffectManager.cs:655-658` | ~40B/frame during combat |
| P11 | AudioTriggerVeil: Vector3.Distance vs sqrMagnitude | `Scripts/Audio/AudioTriggers.cs:239-265` | Minor, use sqrMagnitude |

### CLEAN PATTERNS (No Issues)

All core combat systems (BattleManager, CaptureManager, QuickCommandManager) use pre-allocated buffers, manual loops, cached references. Camera.main properly cached. No Find() in Update. No LINQ in hot paths. Object pooling present.

**Estimated total savings from fixes: ~3-5ms/frame → ~0.5-1ms/frame**

---

## 2. STARTUP MENU GLITCH (1-2s visual glitch on load)

### Root Causes

**CAUSE 1 (PRIMARY): Dual competing animation systems**
- `MainMenuBootstrap.PlayEntranceAnimation()` and `MainMenuController.PlayEntranceAnimation()` BOTH animate the same title/button elements simultaneously
- Two independent coroutines fight over opacity, scale, and translate on the same elements
- Creates visible flickering as styles are overwritten frame-by-frame
- File: `MainMenuBootstrap.cs:244` + `MainMenuController.cs:613`
- **Fix:** Remove one of the two animation systems

**CAUSE 2: Massive synchronous Resources.Load burst**
- `TitleScreenVFX.Awake()` does 10+ `Resources.Load` calls including an AudioClip
- `MoltenButtonVFX.Initialize()` adds 6 more
- Total: ~16-22 synchronous asset loads on first frame
- **Fix:** Preload during the Bootstrap 1.0s splash wait

**CAUSE 3: 200+ VisualElements created in single frame**
- TitleScreenVFX: 140 embers + 40 sparks + 16 ash + smoke + lightning = ~200 elements
- MenuVFXController: +30 elements
- SoulSwarmVFX: +90 elements (30 particles x 3 children)
- MoltenVeinVFX: +16 elements
- All created in 1-2 frames, triggering massive layout recalc
- **Fix:** Stagger creation over 10+ frames (batch 20-30/frame)

**CAUSE 4: Video player initialization**
- 2 VideoPlayers with RenderTexture creation (GPU alloc + codec setup)
- **Fix:** Defer to after entrance animation completes

**CAUSE 5: Duplicate manager creation**
- `MainMenuBootstrap.DeferredStartupInit()` re-creates 7 managers already created by `GameBootstrap`
- Redundant `FindFirstObjectByType` calls
- **Fix:** Remove duplicate creation

### Timeline
```
0.0s  Bootstrap: 13 managers created synchronously (black screen)
1.0s  Splash timer expires, fade begins
1.5s  MainMenu LoadScene fires
1.5s  16+ Resources.Load calls block main thread
1.5s  TWO animation systems fight over same elements → VISIBLE FLICKER
1.7s  200+ VisualElements created → FRAME SPIKE / STUTTER
2.5s  Everything stabilizes → menu looks normal
```

### Recommendation
**Optimize first** (fix the dual animation conflict - biggest visual impact, simplest fix), then stagger VFX creation. Loading screen is a last resort that masks but doesn't fix the issue.

---

## 3. CHARACTER SELECT BUTTON ISSUES

> NOTE: Codex is also working on character select. These are analysis-only findings.

### CRITICAL

**CS1: Rotation buttons reparented, breaking CSS positioning**
- UXML places `btn-rotate-model-left/right` inside `hero-stage` with `position: absolute; left: 8px / right: 8px`
- `EnsureRuntimeControlsVisible()` (line 819-820) reparents them to `_root`
- After reparenting, CSS `left/right` is relative to full screen, not hero stage
- `ApplyRotationButtonLayout()` tries to compute absolute positions but depends on `resolvedStyle` which may not be ready on first frame
- File: `CharacterSelectController.cs:817-820, 852-858`

**CS2: BringToFront() called every 0.33s in LateUpdate**
- `EnsureNavigationButtonVisuals()` runs in LateUpdate every 0.33s
- Calls `BringToFront()` on 5+ buttons each time
- Continuously reshuffles DOM order of root children
- `screen-fade` element (should be on top) gets pushed behind buttons
- Buttons remain clickable above fade overlay during transitions
- File: `CharacterSelectController.cs:825,831,842,883,898,913,950,966,1008`

**CS3: screen-fade can get stuck blocking all input**
- If `EntranceSequence` coroutine is interrupted between adding/removing `active` class, fade stays at opacity 1
- File: `CharacterSelectController.cs:1991-1997`

### MEDIUM

**CS4: Navigation buttons positioned at x=0 on first frame**
- `resolvedStyle` returns 0 for width/height before layout is computed
- Buttons get placed at far left of screen initially
- File: `CharacterSelectController.cs:861-915, 970-994`

**CS5: Carousel slots are VisualElements, not Buttons**
- Not focusable (keyboard nav skips them)
- Child elements may intercept click events before reaching slot handler
- File: `CharacterSelectController.cs:1237-1242`, `CharacterSelect.uxml:191-215`

**CS6: Info panel starts opacity:0, may never appear**
- Depends on `UIAnimationController.Instance` which may be null
- Fallback `ForceVisible()` works but can be undone by subsequent style resets
- File: `CharacterSelect.uss:203`, `CharacterSelectController.cs:2054,2125-2133`

**CS7: Embark button has transparent background**
- Visual appearance comes from child `.vb-embark-glow`, not the button itself
- Hit area may not match visual cues
- File: `CharacterSelect.uss:658-670`

### LOW

**CS8: EnsureTextReadability overrides ALL text colors**
- Forces a single fallback color on every TextElement, killing hover/active states
- File: `CharacterSelectController.cs:2135-2163`

**CS9: Duplicate event registration guard is one-way**
- `_eventHandlersBound` flag prevents re-registration if buttons are recreated
- File: `CharacterSelectController.cs:1220-1246`

**CS10: Carousel slot count vs hero count mismatch**
- 4 slots exist but if fewer heroes, extra slots stay clickable
- Guard exists but UX is confusing
- File: `CharacterSelectController.cs:349-355,1746-1772`

---

## 4. TEST COVERAGE

### Current State: 8 formal NUnit tests, ~250 informal MonoBehaviour assertions

| Category | Formal Tests | Informal Tests | Status |
|----------|-------------|----------------|--------|
| Main Menu assets | 2 EditMode | - | Covered |
| Scene integrity | 2 EditMode | - | Covered |
| Character Select UI | 2 PlayMode | - | Limited |
| Main Menu overlay | 2 PlayMode | - | Limited |
| **Brand System** | **NONE** | Yes (CombatTestSetup) | **Informal only** |
| **Synergy System** | **NONE** | Yes (CombatTestSetup) | **Informal only** |
| **Damage Calculator** | **NONE** | Yes (CombatTestSetup) | **Informal only** |
| **Capture System** | **NONE** | Yes (CaptureTests) | **Informal only** |
| **Status Effects** | **NONE** | Yes (StatusEffectTests) | **Informal only** |
| **AI / Gambits** | **NONE** | Yes (GambitTests) | **Informal only** |
| **Save System** | **NONE** | Yes (SaveSystemTests) | **Informal only** |
| **Audio System** | **NONE** | Yes (AudioTests) | **Informal only** |
| **Quick Commands** | **NONE** | Yes (QuickCommandTests) | **Informal only** |
| **Corruption System** | **NONE** | **NONE** | **UNTESTED** |
| **Path System** | **NONE** | **NONE** | **UNTESTED** |
| **BattleManager** | **NONE** | **NONE** | **UNTESTED** |
| **VERA System** | **NONE** | **NONE** | **UNTESTED** |
| **Settings** | **NONE** | **NONE** | **UNTESTED** |
| **Scene Management** | **NONE** | **NONE** | **UNTESTED** |
| **Core Framework** | **NONE** | **NONE** | **UNTESTED** |

### CI Pipeline
- EditMode + PlayMode tests via game-ci
- 35% minimum line coverage gate (EditMode only)
- Only 8 tests contribute to coverage

### Priority Test Conversion Plan
1. **BrandSystem** → NUnit EditMode (pure static methods, easiest win)
2. **SynergySystem** → NUnit EditMode (pure static methods)
3. **DamageCalculator** → NUnit EditMode (needs Combatant setup)
4. **CaptureFormula** → NUnit EditMode (uses structs, portable)
5. **SaveData/SaveFileHandler** → NUnit EditMode (high-risk per CLAUDE.md)
6. **CorruptionSystem** → NUnit EditMode (currently zero coverage)
7. **StatusEffects** → NUnit EditMode
8. **PathSystem** → NUnit EditMode (currently zero coverage)

---

## 5. PRIORITIZED ACTION PLAN

### Phase 1: Quick Wins (1-2 hours)
- [ ] Fix dual animation conflict in MainMenu (remove one system)
- [ ] SoulSwarmVFX: `style.left/top` → `style.translate`
- [ ] ParallaxBackground: add dirty check
- [ ] AudioTriggerCombatTension: throttle to 10Hz
- [ ] Cache all WaitForSeconds allocations

### Phase 2: Test Infrastructure (2-3 hours)
- [ ] Convert BrandSystem informal tests → NUnit EditMode
- [ ] Convert SynergySystem informal tests → NUnit EditMode
- [ ] Convert DamageCalculator informal tests → NUnit EditMode
- [ ] Convert CaptureFormula informal tests → NUnit EditMode
- [ ] Add CorruptionSystem tests (currently zero coverage)

### Phase 3: Character Select (coordinate with Codex)
- [ ] Stop reparenting rotation buttons (keep in hero-stage)
- [ ] Remove BringToFront() calls from LateUpdate
- [ ] Ensure screen-fade always stays on top
- [ ] Defer button positioning until GeometryChangedEvent
- [ ] Make carousel slots focusable

### Phase 4: Startup Optimization
- [ ] Stagger VFX element creation over multiple frames
- [ ] Preload Resources during Bootstrap splash wait
- [ ] Defer VideoPlayer creation
- [ ] Remove duplicate manager creation in MainMenuBootstrap

---

*Generated by comprehensive codebase audit - 2026-02-08*

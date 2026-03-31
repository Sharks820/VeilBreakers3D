# Requirements: VeilBreakers 3D - v6.0 Bug Fixes & Code Quality Hardening

**Defined:** 2026-03-30
**Milestone:** v6.0
**Core Value:** Fix all combat correctness bugs, stabilize UI, then rebuild title + char select to AAA quality.
**Prior:** v5.3 complete (5 phases, 14 plans, 2026-03-19)

---

## Verification Key
- ✓ Verified in source code
- ⚠ Partially verified / exaggerated claim
- ? Not yet verified (listed from bug scan, needs Phase A check)

---

## Phase A: Critical Combat & UI Bugs

| ID | Bug | Source File(s) | Status |
|----|-----|----------------|--------|
| BUG-A-01 | Defender synergy defense never applied — BattleManager passes only `_currentSynergyTier` for attacker; `defenderSynergyTier` defaults to NEUTRAL | BattleManager.cs:341, DamageCalculator.cs:67-70 | ✓ Verified |
| BUG-A-02 | Brand matrix 3 bidirectional violations: DREAD↔SAVAGE, DREAD↔GRACE, MEND↔LEECH — attacker gets 2x but defender returns 1x | BrandSystem.cs:31-44 | ✓ Verified |
| BUG-A-03 | UNTAMED corruption tier (80-100%) missing from enum and logic — 76-100% all treated as ABYSSAL | Enums.cs:68-75, CorruptionSystem.cs:52-54 | ✓ Verified |
| BUG-A-04 | CharSelectFocusManager `_heroCount` timing risk — field is set via `SetHeroCount()` but if navigation fires before init, guard may not protect all paths | CharSelectFocusManager.cs:85,339 | ⚠ Exaggerated |
| BUG-A-05 | CharSelectVisualEnhancer hover lambdas leak — MouseEnter/MouseLeave on embark button never unregistered in OnDisable | CharSelectVisualEnhancer.cs:211-218 | ✓ Verified |

**Success criteria:** All 5 fixed, one commit each, compile check after each.

---

## Phase B: High-Priority Bugs

| ID | Bug | Source File(s) | Status |
|----|-----|----------------|--------|
| BUG-B-01 | Enemy synergy uses player's tier — `_currentSynergyTier` is player-specific, used in ALL DamageCalculator.Calculate calls regardless of who's attacking | BattleManager.cs:341,545 | ✓ Verified |
| BUG-B-02 | DEFENSE skill ignores skillData — `case SkillType.DEFENSE` uses `user.Abilities.currentDefenseAction` instead of the skill's action | BattleManager.cs:302-303 | ✓ Verified |
| BUG-B-03 | Unsafe enum casts without Enum.IsDefined in HeroData, SkillData, ItemData | ? Needs verification | ? |
| BUG-B-04 | GameDatabase async init fire-and-forget (unawaited Task) | ? Needs verification | ? |
| BUG-B-05 | SaveData path level clamp [0,1] vs PathSystem [0,100] | ? Needs verification | ? |
| BUG-B-06 | UIAnimationController DontDestroyOnLoad inconsistency | ? Needs verification | ? |
| BUG-B-07 | Texture2D leak in MainMenuBootstrap (line 602), runtime PanelSettings never destroyed in MenuBootstrap (lines 86, 138) | MainMenuBootstrap.cs:602, MenuBootstrap.cs:86,138 | ✓ Verified |
| BUG-B-08 | EmbarkCinematicController event nulling hangs async flow | ? Needs verification | ? |
| BUG-B-09 | Shared AudioSource conflict (HoldToEmbark vs CharSelectFocusManager) | ? Needs verification | ? |
| BUG-B-10 | Static event fields persist across scenes — CharSelectEvents has 10 static events; subscribers in Start/Awake instead of OnEnable risk stale delegates | CharSelectEvents.cs, 10+ subscriber files | ✓ Verified |
| BUG-B-11 | Collection modification during iteration — StatusEffectManager already uses temp lists (line 654); check OTHER managers | StatusEffectManager.cs:654 | ⚠ May be fixed |

**Success criteria:** All verified bugs fixed. Unverified items triaged (fix, defer, or dismiss).

---

## Phase C: Code Quality Hardening

| ID | Item | Rationale | Status |
|----|------|-----------|--------|
| QUAL-01 | Classify and replace unguarded Debug.Log (146+ calls) — NOT batch replace, each needs individual severity assessment | Debug.Log is not [Conditional]; ErrorLogger.Log is. Semantic difference matters. | ? |
| QUAL-02 | Convert closure-based PrimeTween to target-based (StatNumberAnimator, ScreenEntryAnimator) | Avoids GC allocations in hot paths | ? |
| QUAL-03 | Standardize singletons — VERASystem, FPSCounter use hand-rolled patterns instead of SingletonMonoBehaviour<T> | Inconsistent init, no duplicate checks | ✓ VERASystem confirmed not using base |
| QUAL-04 | Fix DontDestroyOnLoad without duplicate checks (6 instances) | SingletonMonoBehaviour<T> handles this already | ? |
| QUAL-05 | Add CancellationToken to MonoBehaviour async methods | Unity 6 destroyCancellationToken for clean async cancellation | ? |
| QUAL-06 | Cap damage buff compounding in Combatant.ApplyDamageBuff | Prevent unbounded stacking | ? |
| QUAL-07 | Remove dead code (duplicate Rarity enum, GetRarityModifier returning 0, legacy StatusEffect enum) | Enums.cs has both Rarity and MonsterRarity (lines 331-352), plus [Obsolete] StatusEffect | ✓ Verified |
| QUAL-08 | Install Microsoft.Unity.Analyzers DLL | Roslyn diagnostics for Unity anti-patterns | Tooling |
| QUAL-09 | Create .editorconfig at project root | IDE-level convention enforcement | Tooling |

**Success criteria:** All singletons standardized. No unguarded Debug.Log. Dead code removed.

---

## Phase D: Title Screen & CharSelect Bug Fixes

| ID | Item | Status |
|----|------|--------|
| UIFIX-01 | Consolidate CharSelect USS from 4+ files to 1 canonical file | ? |
| UIFIX-02 | Merge VeilBreakersUI.uss into VeilBreakers.uss (global) | ? |
| UIFIX-03 | Fix title screen loading order (battle screen flash) | ? |
| UIFIX-04 | Fix button highlight glitch (dual :hover vs C# focus conflict) | ? |
| UIFIX-05 | Fix readability (font sizes, opacity, spacing) | ? |
| UIFIX-06 | Gamepad navigation crash-free | ? |
| UIFIX-07 | Wire Settings button on main menu | ? |
| UIFIX-08 | Fix SkillSlotController showing '7' instead of 'R' | ? |
| UIFIX-09 | Fix CaptureBannerController Unity null-conditional bypass | ? |

**Success criteria:** 3 USS files total. Title loads clean. Gamepad works.

---

## Phase E: Title Screen AAA Rebuild

Design-driven phase. Requirements will be defined during planning based on:
- VERA audio spec (randomized interactions, ambient drone, weighted pool + cooldowns)
- Title screen visual mockup
- Existing TitleScreenVFX (3145 lines) decomposition plan

Architecture notes for planning (NOT requirements):
- May create utility classes (UIGlowOverlay, texture helpers) as needed during implementation
- Native `filter: blur()` via Unity 6000.3 FilterFunction should be tested early
- Texture cleanup pattern must be established before adding new textures

---

## Phase F: Character Select AAA Rebuild

Design-driven phase. Requirements will be defined during planning based on:
- CharSelect visual mockup
- Per-hero theming (colors, lighting, audio, particles)
- Embark feedback layer design

Architecture notes for planning (NOT requirements):
- VolumeProfile assets per hero need Editor authoring
- VeilDissolveController needs real shader (placeholder active)
- CharSelect audio (hero-specific drones) if designed

---

## Phase G: 3D Model Audit & Integration

| ID | Item | Status |
|----|------|--------|
| MODEL-01 | Audit 28 GLB models (polycount, UVs, normals, rig integrity) | ? |
| MODEL-02 | Delete stale iterations, keep best variant per character | ? |
| MODEL-03 | Decimate to budget: 50K tris (hero), 30K tris (monster) | ? |
| MODEL-04 | Populate HeroDisplayConfig.modelPrefab for all 4 heroes | ? |
| MODEL-05 | Wire at least 1 champion monster model | ? |
| MODEL-06 | Integrate basic idle animation | ? |

---

## Phase H: End-to-End Verification

| ID | Item |
|----|------|
| VERIFY-01 | Full flow: Title -> CharSelect -> Embark -> Overworld |
| VERIFY-02 | VB Code Reviewer: zero CRITICAL/HIGH in modified files |
| VERIFY-03 | Unity Profiler: Texture2D count stable across 10 transitions |
| VERIFY-04 | Frame time: no GC in hot paths, stable 60fps at 1080p |

---

## What Changed From First Draft

The first REQUIREMENTS.md (67 items) mixed:
1. **Verified bugs** from source code analysis
2. **Architecture suggestions** from research agents (UITextureRegistry, UIGlowOverlay, UIVFXContainer, CharSelectAudio — these are planning decisions, not requirements)
3. **Code reviewer noise** (3378 findings, most LOW/INFO level)

This revision:
- Keeps only verified bugs with source file references
- Marks unverified items honestly with `?`
- Moves architecture decisions to phase planning where they belong
- Phases E/F are design-driven — requirements defined during planning, not pre-guessed

---
*Requirements v2 — 2026-03-30 — source-verified, bloat-stripped*

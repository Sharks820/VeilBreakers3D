# Roadmap: VeilBreakers 3D - v6.0 Bug Fixes & Code Quality Hardening + UI Rebuild

**Defined:** 2026-03-30
**Milestone:** v6.0
**Core Value:** Fix all combat correctness bugs, stabilize UI, then rebuild title + char select to AAA quality.
**Prior:** v5.3 complete (5 phases, 14 plans, 2026-03-19)

---

## Phases

- [x] **Phase 1: Critical Combat Bug Fixes** — Fix 5 verified bugs breaking combat correctness
- [x] **Phase 2: High-Priority Bug Fixes** — Fix 11 high-priority bugs (stability, events, memory)
- [x] **Phase 3: Code Quality Hardening** — Debug.Log, singletons, Roslyn analyzer, dead code
- [x] **Phase 4: Title Screen & CharSelect Bug Fixes** — USS consolidation, interaction bugs, gamepad
- [ ] **Phase 5: Title Screen AAA Rebuild** — VFX, VERA audio, gradients, glow effects
- [ ] **Phase 6: Character Select AAA Rebuild** — Per-hero theming, gradients, embark polish
- [ ] **Phase 7: 3D Model Audit & Integration** — Audit 24 GLB models, wire into HeroDisplayConfig
- [ ] **Phase 8: End-to-End Verification** — Full flow test, code review pass, performance check

---

## Phase 1: Critical Combat Bug Fixes

**Goal:** Fix 5 verified bugs that break combat correctness — brand matrix, synergy, corruption, CharSelect crashes
**Depends on:** Nothing (first phase)
**Requirements:** BUG-A-01, BUG-A-02, BUG-A-03, BUG-A-04, BUG-A-05
**Success Criteria:**
1. DamageCalculator receives and applies defender synergy tier for damage reduction (BUG-A-01 ✓)
2. All 40 bidirectional brand relationships are correct — if A is 2x vs B, B is 0.5x vs A (BUG-A-02 ✓)
3. UNTAMED corruption tier (80-100%) exists in enum, CorruptionSystem, and DamageCalculator (BUG-A-03 ✓)
4. CharSelectFocusManager _heroCount initialized before use (BUG-A-04 ✓)
5. CharSelectVisualEnhancer hover lambdas properly unregistered in OnDisable (BUG-A-05 ✓)
**Plans:** 1 plan (all 5 bugs in single commit)

## Phase 2: High-Priority Bug Fixes

**Goal:** Fix 11 high-priority bugs — enemy synergy, unsafe casts, async hangs, memory leaks, static events
**Depends on:** Phase 1 (correctness before stability)
**Requirements:** BUG-B-01 through BUG-B-11
**Success Criteria:**
1. Enemy attacks use NEUTRAL synergy, not player's tier (BUG-B-01 ✓)
2. DEFENSE skill uses skillData, not stale loadout (BUG-B-02 ✓)
3. All enum casts guarded with Enum.IsDefined (BUG-B-03 ✓)
4. GameDatabase async init has error handling (BUG-B-04 ✓)
5. Static events subscribed in OnEnable, unsubscribed in OnDisable (BUG-B-10 ✓)
6. Texture2D/PanelSettings leaks fixed (BUG-B-07 ✓)
**Plans:** 1 plan (all bugs in single commit)

## Phase 3: Code Quality Hardening

**Goal:** Install tooling, standardize patterns, classify Debug.Log calls, remove dead code
**Depends on:** Phase 2 (bug fixes before quality pass)
**Requirements:** QUAL-01 through QUAL-09
**Success Criteria:**
1. All unguarded Debug.Log classified and replaced with ErrorLogger (QUAL-01)
2. VERASystem and FPSCounter use SingletonMonoBehaviour<T> (QUAL-03)
3. Microsoft.Unity.Analyzers DLL installed (QUAL-08)
4. .editorconfig at project root (QUAL-09)
5. Duplicate Rarity enum marked [Obsolete] (QUAL-07)
**Plans:** 1 plan (quality pass in single commit)

## Phase 4: Title Screen & CharSelect Bug Fixes

**Goal:** Consolidate USS, fix stuck highlights, ensure gamepad works, wire Settings
**Depends on:** Phase 3 (quality infrastructure before UI work)
**Requirements:** UIFIX-01 through UIFIX-09
**Success Criteria:**
1. 3 USS files total (global, TitleScreen, CharacterSelect) (UIFIX-01, UIFIX-02 ✓)
2. Right-click and overlay-close no longer leave stuck highlights (UIFIX-04 ✓)
3. Title loads without battle screen flash (UIFIX-03)
4. Settings button wired and functional (UIFIX-07)
5. Gamepad navigation crash-free (UIFIX-06)
**Plans:** 1 plan (USS + interaction fixes)

## Phase 5: Title Screen AAA Rebuild

**Goal:** AAA title screen with VFX, VERA audio, runtime gradients, glow effects
**Depends on:** Phase 4 (USS cleanup before new styles)
**Requirements:** TITLE-01 through TITLE-10
**Success Criteria:**
1. VERA audio plays randomized interactions with cooldowns, not looping (TITLE-06)
2. Native filter:blur tested and used for panel glows (TITLE-07)
3. TitleScreenVFX decomposed from god class (TITLE-04)
4. Zero Texture2D leaks via UITextureRegistry pattern (TITLE-01)
**Plans:** 4 plans

Plans:
- [x] 05-01-PLAN.md — UITextureRegistry utility + UIGradientHelper leak fix (TITLE-01, TITLE-02, TITLE-08)
- [x] 05-02-PLAN.md — FilterFunction.Blur smoke test (TITLE-07)
- [x] 05-03-PLAN.md — TitleScreenVFX decomposition into 5 subsystems (TITLE-03, TITLE-04, TITLE-05)
- [x] 05-04-PLAN.md — UIVFXContainer + VERA audio randomization + blur glow integration (TITLE-05, TITLE-06, TITLE-09, TITLE-10)

## Phase 6: Character Select AAA Rebuild

**Goal:** AAA CharSelect with per-hero theming, gradients, glow, embark feedback
**Depends on:** Phase 4 (can overlap with Phase 5 — different scenes)
**Requirements:** CHARSEL-01 through CHARSEL-07
**Success Criteria:**
1. Hero card carousel with gradient/glow effects (CHARSEL-01) — DONE in prior sessions
2. Per-hero VolumeProfile assets created (CHARSEL-04) — Plan 01
3. VeilDissolveController wired to real shader (CHARSEL-05) — Plan 02
4. Embark hold has layered visual feedback (CHARSEL-06) — DONE in prior sessions
**Plans:** 2 plans (gap analysis showed most work already complete)

Plans:
- [x] 06-01-PLAN.md — Create per-hero VolumeProfile assets + assign to HeroThemeConfig SOs (CHARSEL-04)
- [x] 06-02-PLAN.md — Wire VeilDissolveController to real shader + Renderer (CHARSEL-05)

## Phase 7: 3D Model Audit & Integration

**Goal:** Audit all 24 GLB models, wire hero + champion monster prefabs into HeroDisplayConfig
**Depends on:** Phase 6 (UI done before model integration)
**Requirements:** MODEL-01 through MODEL-09
**Success Criteria:**
1. All 24 models audited (polycount, UVs, normals, rig) (MODEL-01)
2. All 4 heroes display real 3D models in CharSelect (MODEL-04)
3. Models within budget: 50K tris hero, 30K tris monster (MODEL-03)
4. At least 1 champion monster wired (MODEL-05)
**Plans:** 2 plans

Plans:
- [ ] 07-01-PLAN.md — Create VB_ModelAuditor + wire hero models (MODEL-01, MODEL-02, MODEL-04)
- [ ] 07-02-PLAN.md — Wire champion monsters + budget check (MODEL-03, MODEL-05)

## Phase 8: End-to-End Verification

**Goal:** Full flow verified, zero CRIT/HIGH findings, memory stable
**Depends on:** Phase 7 (everything done before final check)
**Requirements:** VERIFY-01 through VERIFY-07
**Success Criteria:**
1. Full flow: Title -> CharSelect -> Embark -> Overworld (VERIFY-01)
2. VB Code Reviewer: zero CRITICAL/HIGH in modified files (VERIFY-02)
3. Texture2D count stable across 10 scene transitions (VERIFY-03)
4. No GC allocations in hot paths, stable 60fps (VERIFY-04)
**Plans:** 1-2 plans

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Critical Combat Bug Fixes | 1/1 | Done | 2026-03-30 |
| 2. High-Priority Bug Fixes | 1/1 | Done | 2026-03-30 |
| 3. Code Quality Hardening | 1/1 | Done | 2026-03-30 |
| 4. Title Screen & CharSelect Bug Fixes | 1/1 | Done | 2026-03-30 |
| 5. Title Screen AAA Rebuild | 0/4 | Planning Complete | - |
| 6. Character Select AAA Rebuild | 0/2 | Planning Complete | - |
| 7. 3D Model Audit & Integration | 0/2 | Planning Complete | - |
| 8. End-to-End Verification | 0/? | Not Started | - |

---
*Roadmap defined: 2026-03-30 — v6.0 Bug Fixes & Code Quality Hardening + UI Rebuild*

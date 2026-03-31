---
phase: 02-layout-structure
plan: 02
subsystem: ui
tags: [unity-ui-toolkit, uss, transitions, gpu-acceleration, usagehints, character-select]

# Dependency graph
requires:
  - phase: 02-layout-structure
    plan: 01
    provides: "UXML structure, USS styles, CacheUIReferences with all element queries"
provides:
  - "All USS transitions use explicit GPU-safe property lists (zero transition: all)"
  - "UsageHints.DynamicTransform set on all animated VisualElements at init time"
  - "stat-bar-fill width transition removed (deferred to Phase 4 PrimeTween)"
affects: [phase-3-animations, phase-4-polish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Explicit GPU-safe transition property lists instead of transition: all"
    - "UsageHints.DynamicTransform via |= to preserve existing flags (DynamicColor)"
    - "One-time SetAnimationHints() in init flow, not in Update"

key-files:
  created: []
  modified:
    - "Assets/UI/Styles/CharacterSelect.uss"
    - "Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs"

key-decisions:
  - "Used |= for UsageHints assignment to preserve existing DynamicColor flags (reviewer mandate)"
  - "Removed stat-bar-fill width transition entirely; Phase 4 PrimeTween will use scaleX instead"
  - "Added embark-text to animation hints (reviewer gap finding)"

patterns-established:
  - "GPU-safe transitions: only translate, scale, rotate, opacity, color, background-color, border-color"
  - "SetHint helper: static method with null check and |= assignment"
  - "Animation hints set once during init between CacheUIReferences and BindUI"

requirements-completed: [LAYOUT-06, LAYOUT-07]

# Metrics
duration: 20min
completed: 2026-02-22
---

# Phase 2 Plan 2: USS Transition Audit & UsageHints Summary

**Rewrote all 10 transition: all violations to explicit GPU-safe property lists and set UsageHints.DynamicTransform on all animated elements**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-02-22
- **Completed:** 2026-02-22
- **Tasks:** 2/2
- **Files modified:** 2

## Accomplishments
- Rewrote all 10 `transition: all` declarations in CharacterSelect.uss to explicit GPU-safe property lists
- Removed stat-bar-fill width transition (layout-triggering; deferred to Phase 4 PrimeTween scaleX)
- Added SetAnimationHints() method to CharacterSelectManager.cs covering 10 named elements + 5 class-based queries
- Used |= for UsageHints assignment to preserve existing DynamicColor flags (mandatory reviewer fix)
- Added embark-text to hint targets (mandatory reviewer fix — gap in original plan)
- Added kHeroInfoPanel, kStatsPanel, kEmbarkHexBg constants for new element queries

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite All USS Transition Declarations** - `00f4ba0` (feat)
2. **Task 2: Set UsageHints.DynamicTransform on All Animated VisualElements** - `2709055` (feat)

## Files Modified
- `Assets/UI/Styles/CharacterSelect.uss` - 10 transition declarations rewritten from `transition: all` to explicit property lists; stat-bar-fill width transition removed
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` - Added SetAnimationHints() method, SetHint() helper, 3 new element name constants

## Transition Rewrites (Task 1)

| Selector | Before | After |
|----------|--------|-------|
| `.glass-panel` | `transition: all ...` | `border-color` |
| `.hero-name` | `transition: all ...` | `color` |
| `.stat-bar-fill` | `transition: width ...` | REMOVED |
| `.ability-slot` | `transition: all ...` | `translate, background-color, border-left-color` |
| `.nav-arrow` | `transition: all ...` | `color, border-color, scale` |
| `.hero-card` | `transition: all ...` | `scale, opacity, border-color` |
| `.hero-card-initial` | `transition: all ...` | `opacity` |
| `.embark-hex-bg` | `transition: all ...` | `border-color, background-color, scale` |
| `.embark-text` | `transition: all ...` | `color, scale` |
| `.btn-back` | `transition: all ...` | `color, border-color` |

## SetAnimationHints Coverage (Task 2)

**Named elements:** hero-info-panel, stats-panel, btn-back, btn-embark, embark-hex-bg, embark-glow, btn-prev, btn-next, confirm-overlay, embark-text

**Class queries:** ability-slot, hero-card, nav-arrow, glass-panel, fog-particles

## Reviewer Notes Addressed

1. **CRITICAL: |= not = in SetHint** - Implemented: `el.usageHints |= UsageHints.DynamicTransform` preserves existing flags
2. **GAP: embark-text missing** - Added: `SetHint(_embarkText)` included in SetAnimationHints()
3. **stat-bar-fill removal** - Confirmed: transition line removed entirely, deferred to Phase 4

## Deviations from Plan
None. Both tasks executed as specified with mandatory reviewer fixes incorporated.

## Verification Results

- `transition: all` count in CharacterSelect.uss: **0** (PASS)
- Layout-triggering transitions (width/height/margin): **0** (PASS)
- Total transition declarations: **16** (within expected 15-17 range)
- Unity compilation: **0 errors** (PASS)
- SetAnimationHints method definition: **FOUND** (line 599)
- SetAnimationHints call site: **FOUND** (line 179, after CacheUIReferences)
- UsageHints.DynamicTransform with |=: **FOUND** (line 623)

## Issues Encountered
- Plan 02-02 executor agent hit rate limit; tasks executed directly by orchestrator

## Next Phase Readiness
- All USS transitions are GPU-safe; no layout thrashing during animations
- UsageHints enable GPU-accelerated compositing for all animated elements
- Ready for Phase 3 (animations) which will add PrimeTween-based animations on these hinted elements

## Self-Check: PASSED

- FOUND: Assets/UI/Styles/CharacterSelect.uss (modified)
- FOUND: Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs (modified)
- FOUND: .planning/phases/02-layout-structure/02-02-SUMMARY.md
- FOUND: Commit 00f4ba0 (Task 1)
- FOUND: Commit 2709055 (Task 2)

---
*Phase: 02-layout-structure*
*Completed: 2026-02-22*

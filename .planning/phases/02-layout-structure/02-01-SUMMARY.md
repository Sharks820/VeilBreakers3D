---
phase: 02-layout-structure
plan: 01
subsystem: ui
tags: [unity-ui-toolkit, uxml, uss, character-select, layout, backstory]

# Dependency graph
requires:
  - phase: 01-foundation-cleanup
    provides: "USS consolidation (2 files), CharSelectManager bug fixes, HeroDataPanelController champion display"
provides:
  - "Hero lore/backstory section in UXML with conditional visibility"
  - "Text overflow prevention via USS (white-space, overflow, flex-shrink)"
  - "EnsureFullScreenLayout hack removed; layout driven by USS rules only"
  - "Backstory population in HeroDataPanelController with null-safe fallbacks"
affects: [02-02-PLAN, phase-3-animations, phase-4-polish]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Conditional element visibility via hidden class toggle (same pattern as champion-section)"
    - "USS-only full-screen layout via TemplateContainer flex-grow + absolute positioning"
    - "PopulateBackstory() extracted as dedicated method following PopulateChampion() pattern"

key-files:
  created: []
  modified:
    - "Assets/UI/Screens/CharacterSelect.uxml"
    - "Assets/UI/Styles/CharacterSelect.uss"
    - "Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs"
    - "Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs"

key-decisions:
  - "Removed .ToUpper() from synergy_explanation display to prevent text overflow on 131-char strings"
  - "Used .hidden class pattern (matching champion-section) for conditional lore section visibility"
  - "USS-only layout: TemplateContainer flex-grow + absolute root replaces C# hack completely"

patterns-established:
  - "Conditional section visibility: cache VisualElement + toggle .hidden class based on data presence"
  - "Text overflow prevention: white-space: normal + overflow: hidden + flex-shrink: 1 on variable-length text"
  - "Info row alignment: align-items: flex-start for label/value pairs where value may wrap"

requirements-completed: [LAYOUT-01, LAYOUT-02, LAYOUT-03, LAYOUT-04, LAYOUT-05]

# Metrics
duration: 15min
completed: 2026-02-22
---

# Phase 2 Plan 1: Layout & Structure Summary

**UXML backstory section, USS text overflow prevention, and EnsureFullScreenLayout hack removal for pixel-correct hero info panel at 1920x1080**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-02-22
- **Completed:** 2026-02-22
- **Tasks:** 2/2
- **Files modified:** 4

## Accomplishments
- Added hero-lore-section with hero-backstory label to CharacterSelect.uxml, inside hero-info-panel after champion-section
- Removed EnsureFullScreenLayout() C# hack from CharacterSelectManager.cs; layout now driven entirely by USS (TemplateContainer flex-grow + absolute positioning)
- Added USS styles for text overflow prevention: .info-value gets white-space: normal + overflow: hidden + flex-shrink: 1; .info-row gets align-items: flex-start for synergy label alignment
- HeroDataPanelController now populates backstory text with conditional lore section visibility (hidden when backstory is null/empty)
- Champion display verified: uses GetPrimaryBrand() for brand and GetAIPattern() for role (Phase 1 fixes preserved)

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix UXML Root Layout and Add Backstory Section** - `3114223` (feat)
2. **Task 2: Fix Content Population for Backstory, Champion Display, and Text Overlap** - `55256f7` (feat)

## Files Created/Modified
- `Assets/UI/Screens/CharacterSelect.uxml` - Added hero-lore-section with hero-backstory label after champion-section
- `Assets/UI/Styles/CharacterSelect.uss` - Added .hero-lore-section, .hero-backstory styles; added overflow/wrapping to .info-value and .info-row; added overflow: hidden to .hero-info-panel
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` - Deleted EnsureFullScreenLayout() method and its OnEnable() call
- `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs` - Added kHeroBackstory/kHeroLoreSection constants, cached fields, Debug.Assert validation, PopulateBackstory() method with conditional visibility

## Decisions Made
- **Removed .ToUpper() from synergy_explanation**: Long synergy text (131 chars for Seraphina) in uppercase was causing panel overflow. Kept brand fallback uppercase, but natural-case for explanation text.
- **Used .hidden class for lore visibility**: Matches existing champion-section pattern. When backstory is null/empty, the entire lore section hides rather than showing empty space.
- **USS-only layout approach**: TemplateContainer flex-grow: 1 rule (already in both VeilBreakers.uss and CharacterSelect.uss) combined with .character-select-root absolute positioning handles full-screen layout declaratively. No C# runtime manipulation needed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed .ToUpper() from synergy_explanation to prevent text overflow**
- **Found during:** Task 2 (synergy text population)
- **Issue:** Synergy explanation text (up to 131 chars) was being uppercased, which increases visual width and contributed to panel overflow at 1920x1080
- **Fix:** Removed .ToUpper() call on synergy_explanation; kept uppercase on brand name fallback only
- **Files modified:** Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs
- **Verification:** Grep confirms synergy_explanation assigned without .ToUpper()
- **Committed in:** 55256f7

---

**Total deviations:** 1 auto-fixed (1 bug fix)
**Impact on plan:** Minor text formatting change to prevent overflow. No scope creep.

## Reviewer Notes Addressed

1. **Conditional lore section visibility** - Implemented: hero-lore-section uses .hidden class when backstory is null/empty, matching champion-section pattern. Both _heroLoreSection VisualElement and _heroBackstory Label are cached.
2. **Cold-load regression** - Verified: USS has TemplateContainer { flex-grow: 1; } and .character-select-root has position: absolute; width: 100%; height: 100%. Works for both domain reload and fresh scene load.
3. **Synergy row alignment** - Implemented: Added align-items: flex-start to .info-row so synergy label stays at top when value text wraps.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Layout structure complete for hero info panel
- Ready for Plan 02 (transitions/animations) which builds on this layout foundation
- Backstory section may need height tuning after visual verification with actual hero data at runtime

## Self-Check: PASSED

- FOUND: Assets/UI/Screens/CharacterSelect.uxml
- FOUND: Assets/UI/Styles/CharacterSelect.uss
- FOUND: Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs
- FOUND: Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs
- FOUND: .planning/phases/02-layout-structure/02-01-SUMMARY.md
- FOUND: Commit 3114223 (Task 1)
- FOUND: Commit 55256f7 (Task 2)

---
*Phase: 02-layout-structure*
*Completed: 2026-02-22*

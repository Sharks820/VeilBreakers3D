---
phase: 03-controller-behavior
plan: 01
subsystem: ui
tags: [unity-ui-toolkit, uxml, uss, character-select, dark-fantasy, tabs, gamepad-focus, toast, skeleton-shimmer]

# Dependency graph
requires:
  - phase: 02-layout-structure
    provides: "UXML structure with content population, USS-only layout, GPU-safe transitions"
provides:
  - "Rule-of-thirds UXML layout with hero-stage (50% left) and info-panel-container (right half)"
  - "Tabbed info system with 3 tabs: Overview, Abilities, Lore"
  - "Veil-torn dark fantasy USS with per-hero accent glow borders (4 theme classes)"
  - "Tab system styles (tab-btn, tab-btn-active, tab-content-section)"
  - "Focus ring styles (focusable-zone, focus-ring-active) with per-hero colors"
  - "Toast notification styles (toast-container, toast-visible, toast-hidden)"
  - "Skeleton shimmer loading state styles"
  - "Embark progress ring element and styles for hold-to-confirm"
  - "Extended CharSelectEvents: OnTabChanged, OnEmbarkHoldProgress, OnErrorOccurred, OnLoadingStarted, OnLoadingComplete, OnFocusZoneChanged, OnEmbarkTriggered"
  - "Confirm overlay completely removed from UXML and CharacterSelectManager"
affects: [03-02-PLAN, 03-03-PLAN, phase-4-visual-amplification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Veil-panel material: rgba(12,10,18,0.92) bg with per-hero accent border-color via .theme-X .veil-panel selectors"
    - "Tab system: display:none/.tab-active toggle for content sections, underline-style tab buttons"
    - "Focus ring: always-present transparent border + .focus-ring-active class for color change (no layout shift)"
    - "Toast notification: translate-based slide-up from bottom via toast-visible/toast-hidden classes"
    - "Event-driven embark: OnEmbarkTriggered replaces old popup-based OnEmbarkRequested/Confirmed/Cancelled"

key-files:
  created: []
  modified:
    - "Assets/UI/Screens/CharacterSelect.uxml"
    - "Assets/UI/Styles/CharacterSelect.uss"
    - "Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs"
    - "Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs"

key-decisions:
  - "Hero theme colors updated to match dark fantasy: Vex=amber/iron, Seraphina=violet/void, Orion=crimson/fang, Nyx=cyan/chaos"
  - "Info panel uses position:absolute right:0 with 46% width for rule-of-thirds composition"
  - "Tab content sections use display:none/flex toggle (not opacity) for clean content switching"
  - "Focus ring always has 2px border (transparent when inactive) to prevent layout shift on focus"
  - "Embark subtitle changed from 'Begin Your Journey' to 'HOLD TO EMBARK' to communicate hold-to-confirm interaction"
  - "OnEmbarkTriggered replaces old 3-event popup flow (Requested/Confirmed/Cancelled) -- single event for hold completion"
  - "CharacterSelectManager.TriggerEmbark() is event-driven via OnEmbarkTriggered subscription instead of direct click handler"

patterns-established:
  - "Veil-panel material system: .veil-panel base + .theme-X .veil-panel per-hero overrides"
  - "Tab system pattern: tab-header-strip > tab-btn buttons, tab-content > tab-content-section with .tab-active class"
  - "Focus ring pattern: .focusable-zone base with transparent border + .focus-ring-active for highlight"
  - "Toast notification pattern: translate-based animation via toast-visible/toast-hidden class toggle"
  - "Event-driven embark: subscribe to OnEmbarkTriggered for embark completion"

requirements-completed: [CTRL-01, CTRL-09]

# Metrics
duration: 14min
completed: 2026-03-18
---

# Phase 3 Plan 01: UXML/USS/Events Foundation Summary

**Rule-of-thirds UXML restructure with veil-torn dark fantasy USS, tabbed info panel (Overview/Abilities/Lore), and extended CharSelectEvents for hold-to-embark, tab, error toast, and loading events**

## Performance

- **Duration:** 14 min
- **Started:** 2026-03-18T23:55:06Z
- **Completed:** 2026-03-19T00:08:57Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Restructured UXML from symmetric dual-panel to rule-of-thirds composition with hero-stage (50% left) and tabbed info-panel-container (right half)
- Completely rewrote USS with veil-torn dark fantasy aesthetic: 4 per-hero accent themes, tab system, focus ring, toast notification, skeleton shimmer, embark progress ring styles
- Extended CharSelectEvents with 7 new events (OnTabChanged, OnEmbarkHoldProgress, OnErrorOccurred, OnLoadingStarted, OnLoadingComplete, OnFocusZoneChanged, OnEmbarkTriggered) and removed 3 old confirm popup events
- Removed confirm overlay from UXML and cleaned all references from CharacterSelectManager

## Task Commits

Each task was committed atomically:

1. **Task 1: Restructure UXML to Rule-of-Thirds** - `93de401` (feat)
2. **Task 2: Veil-Torn Dark Fantasy USS** - `eae3976` (feat)
3. **Task 3: Extend CharSelectEvents + Manager Cleanup** - `20ea901` (feat)

## Files Created/Modified
- `Assets/UI/Screens/CharacterSelect.uxml` - Rule-of-thirds layout with tabbed info, toast, skeleton overlay, embark progress ring
- `Assets/UI/Styles/CharacterSelect.uss` - Complete rewrite with veil-torn dark fantasy aesthetic, tab/focus/toast/shimmer styles
- `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs` - 7 new events, 3 old events removed, all ClearAll nulled
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` - Confirm overlay removed, TriggerEmbark event-driven, animation hints updated

## Decisions Made
- Updated hero theme colors to dark fantasy palette (Vex amber, Seraphina violet, Orion crimson, Nyx cyan) -- previous colors were generic RGB primaries
- Chose display:none/flex toggle for tab content instead of opacity to ensure clean DOM layout
- Focus ring uses always-present transparent border to prevent layout shift (per research pitfall #3)
- Renamed ExecuteEmbark to TriggerEmbark and made it event-driven via OnEmbarkTriggered subscription
- Removed BindUI registration for embark click and navigation move/submit handlers (will be handled by HoldToEmbarkController and FocusManager in Plan 03-03)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed confirm overlay references from CharacterSelectManager**
- **Found during:** Task 3 (CharSelectEvents extension)
- **Issue:** CharacterSelectManager.cs referenced removed events (RaiseEmbarkRequested/Confirmed/Cancelled) and removed UXML elements (confirm-overlay, btn-confirm, btn-cancel) which would cause compile errors
- **Fix:** Removed all confirm overlay constants, fields, Q() queries, Debug.Assert checks, bind/unbind handlers, ShowConfirmPopup, HideConfirmPopup, OnConfirmClicked, OnCancelClicked methods. Replaced ExecuteEmbark with TriggerEmbark. Updated navigation handlers to remove confirm overlay gating.
- **Files modified:** Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs
- **Verification:** Zero matches for all old event/overlay references in grep
- **Committed in:** 20ea901 (Task 3 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix was necessary to prevent compile errors from removed UXML elements and events. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- UXML structure is ready for Plan 03-02 (controller rewrite for async embark, tab management, toast, skeleton loading)
- All new element names (info-panel-container, tab-*, toast-*, skeleton-overlay, embark-progress-ring) are in place for Q() caching
- All new events (OnTabChanged, OnEmbarkHoldProgress, OnErrorOccurred, OnLoadingStarted, OnLoadingComplete, OnFocusZoneChanged, OnEmbarkTriggered) are ready for subscription
- USS classes for tab active/inactive, focus ring, toast visible/hidden, shimmer active are defined
- Plan 03-03 will create CharSelectFocusManager and HoldToEmbarkController that use these foundations

## Self-Check: PASSED

All 5 files verified present. All 3 task commits verified in git log.

---
*Phase: 03-controller-behavior*
*Completed: 2026-03-18*

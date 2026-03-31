---
phase: 04-visual-amplification
plan: 02
subsystem: ui
tags: [primetween, urp, volume-profile, dissolve, materialpropertyblock, overlay, glitch-text, music-parameters, uss]

# Dependency graph
requires:
  - phase: 04-visual-amplification/plan-01
    provides: PrimeTween installed, HeroThemeConfig ScriptableObject, VeilDissolve shader, URP assembly references
provides:
  - VolumeProfileTransitioner for runtime URP Volume override interpolation between per-hero profiles
  - VeilDissolveController for MaterialPropertyBlock-driven dissolve shader animation
  - MusicManager extended with 5 hero-specific parameters (warmth, synth, perc, pad, filter) and SetHeroMusicParameters
  - OverlayController for per-hero scanline/vignette/glow intensity management with embark hold intensification
  - GlitchTextEffect for composable PrimeTween text scramble-to-resolve sequences
  - USS panel-inactive-dim class for visual depth hierarchy
affects: [04-03, 04-04]

# Tech tracking
tech-stack:
  added: []
  patterns: [Tween.Custom with explicit target for allocation-free volume override lerping, MaterialPropertyBlock for per-object shader customization without material instances, Pure C# classes (non-MonoBehaviour) for UI Toolkit visual system controllers, Deterministic glyph cycling in GlitchTextEffect avoiding Random allocation]

key-files:
  created:
    - Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs
    - Assets/Scripts/UI/CharacterSelect/VeilDissolveController.cs
    - Assets/Scripts/UI/CharacterSelect/OverlayController.cs
    - Assets/Scripts/UI/CharacterSelect/GlitchTextEffect.cs
  modified:
    - Assets/Scripts/Audio/MusicManager.cs
    - Assets/UI/Styles/CharacterSelect.uss

key-decisions:
  - "Vignette intensity animated via border-color alpha instead of border-width (GPU-safe, avoids layout recalculation)"
  - "GlitchTextEffect uses deterministic glyph cycling (Mathf.FloorToInt) instead of UnityEngine.Random to avoid allocation"
  - "OverlayController and GlitchTextEffect are pure C# classes (not MonoBehaviour) since they operate on VisualElements"

patterns-established:
  - "VolumeProfileTransitioner: Instantiate sharedProfile at runtime, cache source/destination values, drive all properties from single Tween.Custom lerp factor"
  - "VeilDissolveController: Static Shader.PropertyToID caching, MaterialPropertyBlock reuse, Tween.Custom with explicit target for allocation-free dissolve animation"
  - "OverlayController: Pure C# class managing VisualElement overlays with TransitionTo() returning composable PrimeTween Sequences"
  - "MusicManager extension: Follow existing named-field target/current/lerp pattern for new parameters"

requirements-completed: [VISUAL-03, VISUAL-04, VISUAL-05, VISUAL-07, VISUAL-09]

# Metrics
duration: 8min
completed: 2026-03-19
---

# Phase 4 Plan 2: Visual Subsystems Summary

**URP Volume Profile interpolation, MaterialPropertyBlock dissolve controller, per-hero overlay intensity management, glitch text reveal effect, and hero-specific music parameter crossfade -- the building blocks for Plan 03 choreography**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-19T03:51:57Z
- **Completed:** 2026-03-19T04:00:22Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- VolumeProfileTransitioner lerps Bloom/Vignette/DoF/ColorAdjustments/ChromaticAberration between per-hero URP VolumeProfiles via PrimeTween with runtime profile instantiation (prevents Editor asset mutation)
- VeilDissolveController drives dissolve shader threshold, edge color, and noise scale via MaterialPropertyBlock with allocation-free AnimateDissolveOut/In tweens
- MusicManager extended with 5 new hero-specific parameters (warmth, synth, perc, pad, filter) following existing target/current/lerp pattern, plus SetHeroMusicParameters convenience method
- OverlayController manages per-hero scanline/vignette/glow overlay intensities with composable PrimeTween Sequences and embark hold intensification (scales overlays proportional to hold progress)
- GlitchTextEffect builds composable PrimeTween sequences for text scramble-to-resolve animation using deterministic glyph cycling through unicode block drawing characters
- USS updated with panel-inactive-dim class providing opacity 0.6 with 0.3s CSS transition for visual depth hierarchy

## Task Commits

Each task was committed atomically:

1. **Task 1: VolumeProfileTransitioner, VeilDissolveController, and MusicManager Extension** - `49e58d9` (feat)
2. **Task 2: OverlayController, GlitchTextEffect, and USS Overlay Updates** - `365907f` (feat)
3. **Meta files for new scripts** - `1a7c3a2` (chore)

## Files Created/Modified
- `Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs` - Runtime URP Volume override interpolation between per-hero profiles
- `Assets/Scripts/UI/CharacterSelect/VeilDissolveController.cs` - MaterialPropertyBlock-driven VeilDissolve shader controller with PrimeTween animation
- `Assets/Scripts/UI/CharacterSelect/OverlayController.cs` - Scanline/vignette/glow overlay management with per-hero intensity and embark intensification
- `Assets/Scripts/UI/CharacterSelect/GlitchTextEffect.cs` - Text scramble-to-resolve animation builder producing composable PrimeTween Sequences
- `Assets/Scripts/Audio/MusicManager.cs` - Extended with warmth/synth/perc/pad/filter parameters and SetHeroMusicParameters
- `Assets/UI/Styles/CharacterSelect.uss` - Added panel-inactive-dim class for visual depth hierarchy

## Decisions Made
- **Vignette animation via border-color alpha:** Border-width is NOT a GPU-safe animation property in UI Toolkit. Instead of animating border-width, OverlayController keeps it static and animates the border-color alpha to control vignette intensity. This avoids layout recalculation on every frame.
- **Deterministic glyph cycling:** GlitchTextEffect uses `Mathf.FloorToInt(t * kGlitchChars.Length * 2f) % kGlitchChars.Length` instead of `UnityEngine.Random.Range()` to avoid per-frame Random allocation during the glitch animation.
- **Pure C# classes for UI controllers:** OverlayController and GlitchTextEffect are pure C# classes (not MonoBehaviour) since they operate exclusively on VisualElements and don't need Unity lifecycle hooks. This follows the plan specification and keeps them lightweight.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All visual subsystem building blocks are ready for Plan 03 (HeroThemeTransitioner choreography)
- VolumeProfileTransitioner.LerpTo() returns a Tween for direct insertion into Plan 03's hero switch Sequence
- VeilDissolveController.AnimateDissolveOut/In() return Tweens for dissolve timeline composition
- OverlayController.TransitionTo() returns a Sequence for overlay intensity choreography
- GlitchTextEffect.BuildGlitchReveal() returns a Sequence for hero name text reveal
- MusicManager.SetHeroMusicParameters() provides one-call music crossfade on hero switch
- **Note:** Plan 03 will compose these systems into timed sequences via HeroThemeTransitioner

## Self-Check: PASSED

- All 4 created files verified on disk
- All 3 task commits verified in git history (49e58d9, 365907f, 1a7c3a2)

---
*Phase: 04-visual-amplification*
*Completed: 2026-03-19*

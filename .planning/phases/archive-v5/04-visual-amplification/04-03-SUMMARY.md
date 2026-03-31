---
phase: 04-visual-amplification
plan: 03
status: complete
completed: 2026-03-19
---

# Plan 04-03 Summary: Wire Choreography

## What Was Built

### New Files
- **HeroThemeTransitioner.cs** - Central orchestrator reading HeroThemeConfig and driving all visual systems on hero switch and screen entry
- **HeroSwitchAnimator.cs** - Pure C# class building the full 1.2s choreographed hero switch PrimeTween Sequence per CONTEXT.md timeline
- **ScreenEntryAnimator.cs** - Pure C# class building the one-shot staggered panel entrance animation

### Modified Files
- **HeroStageController.cs** - Added TransitionLighting(HeroThemeConfig) with per-hero lighting lerp over 0.6s and Nyx rim flicker
- **HeroStatsPanelController.cs** - Added BuildStatBarCascade with 100ms stagger per stat bar using PrimeTween scaleX
- **CarouselController.cs** - Added PrimeTween card animations (selected 1.15x scale, unselected 0.9x dimmed, breathing on active card)
- **CharacterSelectManager.cs** - Added _themeTransitioner wiring, InitVisualSystems, LateUpdate for parallax, veil-pulse-flash element, embark button breathing glow
- **OverlayController.cs** - Added InitParallax and UpdateParallax for subtle panel parallax (3-5px panels, 0.5-2px overlays)

## Key Decisions
- HeroThemeTransitioner subscribes to CharSelectEvents itself (no explicit event forwarding from CharacterSelectManager needed)
- Parallax driven per-frame from CharacterSelectManager.LateUpdate -> HeroThemeTransitioner.UpdateParallax -> OverlayController.UpdateParallax
- Stat bar cascade uses scaleX animation (not width) per Phase 2 decision
- Carousel uses ButtonVFXHelper.AddBreathing on selected card only
- Embark button gets breathing glow via ButtonVFXHelper.AddBreathing(0.012f amplitude, 2500ms period)

## Requirements Covered
- VISUAL-02: Per-hero post-processing + stat bar cascade + stage lighting transitions
- VISUAL-05: Music crossfade via MusicManager.SetHeroMusicParameters
- VISUAL-08: Embark button breathing glow + carousel card animations

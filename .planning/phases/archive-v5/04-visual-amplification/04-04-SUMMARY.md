---
phase: 04-visual-amplification
plan: 04
status: complete
completed: 2026-03-19
---

# Plan 04-04 Summary: Embark Cinematic

## What Was Built

### New Files
- **VeilCrack.shader** - URP-compatible fullscreen procedural Voronoi crack pattern shader with HDR glow, shatter displacement, and white-out. SRP Batcher compatible via CBUFFER_START.
- **VeilTransitionController.cs** - Reusable MonoBehaviour driving VeilCrack shader on a fullscreen quad. Public API: PlayCrackSpread, PlayShatter, PlayWhiteOut, PlayMaterialize, Reset. OnTransitionComplete event.
- **EmbarkCinematicController.cs** - Orchestrates the full 1.2s embark cinematic per CONTEXT.md timeline. Declares OnCinematicComplete event (distinct from VeilTransitionController.OnTransitionComplete).

### Modified Files
- **CharacterSelectManager.cs** - Added [SerializeField] _embarkCinematic field, TaskCompletionSource<bool> bridge in ExecuteEmbarkAsync to await cinematic completion before save/scene load
- **HeroThemeTransitioner.cs** - Added GetCurrentTheme() method for embark flow access

## Embark Cinematic Timeline
```
t=0ms:    Hero accent flash (0.8 opacity, 150ms fade)
t=100ms:  Camera dolly into hero (FOV 60->40, 400ms)
t=300ms:  UI panels dismiss (left/right/carousel slide out, 200ms)
t=500ms:  Veil cracks spread from center (300ms, hero accent HDR glow)
t=600ms:  Hero name glitch text reveal (centered, large, accent color)
t=800ms:  Cracks shatter outward + name fades
t=1000ms: White-out overlay
t=1200ms: OnCinematicComplete fires -> save + scene load proceeds
```

## Key Decisions
- VeilCrack uses Voronoi noise cells for crack pattern (standard technique, no texture dependency)
- VeilTransitionController creates a fullscreen quad at near clip distance (works without Full Screen Pass Renderer Feature)
- EmbarkCinematicController uses InsertCallback pattern (not ternary with Tween.Delay) for null-safe optional subsystem calls
- TaskCompletionSource bridges PrimeTween callback -> async/await in ExecuteEmbarkAsync
- Cinematic plays BEFORE save/load, with white-out already at full opacity when scene transition starts

## Requirements Covered
- VISUAL-06: Embark cinematic sequence before scene transition

# Phase 6: Character Select AAA Rebuild - Context

**Gathered:** 2026-03-31
**Status:** Ready for planning
**Mode:** Auto-generated (autonomous session, research skipped)

<domain>
## Phase Boundary

AAA Character Select screen with per-hero theming, runtime gradients, glow effects, and layered embark visual feedback. The screen already has substantial infrastructure from prior sessions — the planning focus should be on gap analysis: what remains to meet AAA quality.

</domain>

<decisions>
## Implementation Decisions

### Pre-existing Work (Already Implemented)
- Hero card carousel with per-hero gradient backgrounds via CharSelectVisualEnhancer
- Per-hero theming via HeroThemeConfig ScriptableObjects + USS `.theme-*` CSS classes
- Tab system (Overview/Abilities/Lore) with hero-colored underlines and glow text-shadow
- HoldToEmbarkController with progress ring and layered feedback
- EmbarkCinematicController with extended name popup (1.7s, hero theme color)
- VeilDissolveController and VolumeProfileTransitioner already exist
- Model rotation via HeroStageController drag interaction
- Orion differentiated from Seraphina with darker blue (35,55,200)
- Abilities tab upgraded with 4px hero-themed left borders, brand dots, tag pills
- Lore tab upgraded with drop-caps, deeper card backgrounds, section dividers

### Claude's Discretion
- Assess what still needs VolumeProfile assets vs runtime defaults
- Determine if VeilDissolveController needs real shader wiring or if current approach suffices
- Evaluate embark hold visual feedback completeness
- All implementation choices for remaining gaps at Claude's discretion

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- UITextureRegistry for texture lifecycle management
- UIGradientHelper for runtime gradient textures
- ButtonVFXHelper for breathing/shimmer effects
- HeroThemeConfig SO with per-hero colors, lighting, dissolve params
- CharSelectVisualEnhancer applies per-hero card gradients at runtime

### Established Patterns
- USS `.theme-{heroId}` classes on root for CSS variable-based theming
- HeroThemeTransitioner manages theme transitions with VolumeProfile swaps
- Event-driven architecture via CharSelectEvents static class
- Auto-wiring in CharacterSelectManager.EnsureCharSelectComponents()

### Integration Points
- CharacterSelectManager.cs — orchestrator (1323 lines)
- CharSelectVisualEnhancer.cs — runtime gradient/glow application
- HeroThemeTransitioner.cs — theme switching + VolumeProfile transitions
- HoldToEmbarkController.cs — hold-to-confirm pattern
- EmbarkCinematicController.cs — cinematic name popup + veil effect

</code_context>

<specifics>
## Specific Ideas

Success criteria from ROADMAP:
1. Hero card carousel with gradient/glow effects (CHARSEL-01) — MOSTLY DONE
2. Per-hero VolumeProfile assets created (CHARSEL-04) — RUNTIME DEFAULTS EXIST
3. VeilDissolveController wired to real shader (CHARSEL-05) — EXISTS, may need shader
4. Embark hold has layered visual feedback (CHARSEL-06) — DONE

Focus should be: verify existing implementations meet AAA quality, fill any gaps.

</specifics>

<deferred>
## Deferred Ideas

- 3D monster model display (no models integrated yet, text-only champion info)
- Per-hero VolumeProfile ScriptableObject assets (currently using runtime defaults)

</deferred>

---

*Phase: 06-character-select-aaa-rebuild*
*Context gathered: 2026-03-31 via autonomous skip*

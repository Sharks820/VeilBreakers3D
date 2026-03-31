# Phase 5: Title Screen AAA Rebuild - Context

**Gathered:** 2026-03-31
**Status:** Ready for planning
**Mode:** Auto-generated (discuss skipped via workflow.skip_discuss)

<domain>
## Phase Boundary

AAA title screen rebuild with VFX, VERA audio, runtime gradients, and glow effects.
Decompose TitleScreenVFX from god class, implement UITextureRegistry for leak-free texture management.
Test native filter:blur for panel glows. Wire VERA audio for randomized interactions with cooldowns.

**Success Criteria (from ROADMAP):**
1. VERA audio plays randomized interactions with cooldowns, not looping (TITLE-06)
2. Native filter:blur tested and used for panel glows (TITLE-07)
3. TitleScreenVFX decomposed from god class (TITLE-04)
4. Zero Texture2D leaks via UITextureRegistry pattern (TITLE-01)

**Existing Code Context:**
- MainMenuController.cs (~1600 lines): Manages title screen UI with gradient textures, button VFX, demon effects, entrance animations, save slot browsing
- TitleScreenVFX.cs: Logo container breathing, ember particles, veil effects (needs decomposition)
- ButtonVFXHelper.cs: Static helper for ripple, glow, shimmer, burst effects
- UIGradientHelper.cs: Runtime gradient texture generation (vertical, horizontal, radial)
- VERASystem.cs / VERAVoiceController.cs: AI personality voice system
- MainMenuVFXOverlayController.cs: VFX overlay for title screen
- MenuBootstrap.cs: Bootstrapper that wires MainMenuController

**Key files to modify:**
- Assets/Scripts/UI/Core/TitleScreenVFX.cs — decompose
- Assets/Scripts/UI/Menus/MainMenuController.cs — integrate UITextureRegistry
- Assets/Scripts/UI/Core/MenuBootstrap.cs — wire new components
- New: Assets/Scripts/UI/Core/UITextureRegistry.cs — texture lifecycle management
- New: Assets/Scripts/Audio/VERATitleAudio.cs — title-specific VERA audio

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — discuss phase was skipped per user setting.
Use ROADMAP phase goal, success criteria, and codebase conventions to guide decisions.

Key guidelines from CLAUDE.md:
- Use Context7 for PrimeTween/UI Toolkit/URP/Cinemachine APIs before writing code
- Visual QA pipeline: design → spec → implement → screenshot → compare
- Read before edit, test every 3-5 changes
- UI Toolkit only (NOT IMGUI)
- Runtime Texture2D generation for gradients (USS can't do gradients)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- UIGradientHelper: CreateVerticalGradient, CreateVerticalGradient3, CreateHorizontalGradient, CreateRadialGradient + ApplyGradient
- ButtonVFXHelper: ApplyEffects, AddShimmer, AddClickBurst, AddTopHighlight, AddFocusEffect, AddBreathing, AddChargeEffect
- MainMenuController already has ApplyButtonGradients(), ApplyDemonEffects(), ApplyAtmosphericOverlays()
- VERASystem: Singleton for VERA voice, handles personality state
- CharSelectVisualEnhancer: Pattern for stored hover callbacks + texture cleanup (fixed in Phase 1)

### Established Patterns
- Texture2D created at runtime, stored in fields, destroyed in OnDisable/CleanupTextures
- EventCallbacks stored in dictionaries for proper unregistration
- StyleKeyword.Null to reset inline styles to USS defaults
- Dictionary<string, Texture2D> for per-hero gradient textures
- DestroyTex(ref Texture2D) helper pattern

### Integration Points
- MainMenuBootstrap creates MainMenuController, wires UIDocument
- TitleScreenVFX sits on same GameObject as MainMenuController
- MainMenuVFXOverlayController handles fade-out during scene transitions
- VERASystem singleton accessible from any MonoBehaviour

</code_context>

<specifics>
## Specific Ideas

From user memory (feedback_vera_title_audio.md):
- VERA title audio: randomized interactions with cooldowns, NOT looping
- Each interaction should be unique and feel organic

From existing code patterns:
- CharSelectVisualEnhancer pattern for stored callbacks + texture cleanup should be replicated

</specifics>

<deferred>
## Deferred Ideas

None — discuss phase skipped.

</deferred>

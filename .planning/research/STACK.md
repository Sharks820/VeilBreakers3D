# Technology Stack: VeilBreakers v6.0 -- AAA UI Rebuild, 3D Model Integration, Code Quality Hardening

**Project:** VeilBreakers 3D v6.0 Milestone
**Researched:** 2026-03-30
**Overall Confidence:** HIGH (verified against existing codebase, Unity 6 docs, and community patterns)

## Context

This research covers stack additions/changes needed for three domains in the v6.0 milestone:

1. **AAA UI Effects** -- Runtime Texture2D gradients, layered glow VisualElements, procedural audio synthesis
2. **3D Model Display in UI** -- RenderTexture pipeline for character previews in UI Toolkit
3. **Code Quality Tooling** -- Static analysis, Roslyn analyzers, test coverage enforcement

This is a **brownfield** analysis. The project already has a validated stack (Unity 6000.3.6f1, URP 17.3, UI Toolkit, PrimeTween 1.3.8, Cinemachine, glTFast 6.14.1, etc.). The goal is to identify what new capabilities are needed and what already exists that can be extended.

---

## Existing Stack (DO NOT CHANGE)

These are locked. Documented here as integration context for new additions.

| Technology | Version | Role |
|------------|---------|------|
| Unity | 6000.3.6f1 | Engine (locked) |
| URP | 17.3.0 | Rendering pipeline |
| UI Toolkit (com.unity.modules.uielements) | Built-in | All UI screens |
| PrimeTween | 1.3.8 | Animation/tweening |
| Input System | 1.18.0 | Input handling |
| glTFast | 6.14.1 | GLB/glTF model loading |
| Addressables | 2.8.1 | Asset management |
| Unity Test Framework | 1.6.0 | EditMode + PlayMode tests |
| Code Coverage | 1.2.7 | Test coverage reporting |
| Memory Profiler | 1.1.12 | Memory leak detection |
| Profile Analyzer | 1.3.3 | Performance profiling |
| Burst | 1.8.27 (transitive) | Compute acceleration |
| Mathematics | 1.3.3 (transitive) | Fast math |

---

## New Stack Additions

### Domain 1: AAA UI Effects

**Key finding: No new packages needed.** The project already has the correct approach and implementation. What is needed is hardening and extension of existing custom C# utilities.

#### UIGradientHelper (EXISTS -- extend, do not replace)

| Component | Status | Location | Action |
|-----------|--------|----------|--------|
| `UIGradientHelper.CreateVerticalGradient()` | Implemented | `Assets/Scripts/UI/Core/UIGradientHelper.cs` | Harden: add `makeNoLongerReadable: true` to `Apply()` for production to free CPU-side memory |
| `UIGradientHelper.CreateVerticalGradient3()` | Implemented | Same | Good as-is |
| `UIGradientHelper.CreateRadialGradient()` | Implemented | Same | Good as-is |
| `UIGradientHelper.CreateGlowOverlay()` | Implemented | Same | Harden: texture cleanup on parent removal |
| `UIGradientHelper.CreateTopHighlight()` | Implemented | Same | Fix: leaks unused vertical texture (creates then destroys, but allocation is wasteful) |
| Multi-stop gradient (N stops) | MISSING | -- | Add: `CreateGradient(GradientStop[] stops)` for complex gradients |
| Horizontal gradient | MISSING | -- | Add: `CreateHorizontalGradient()` for button effects |
| Texture pool/cache | MISSING | -- | Add: avoid regenerating identical gradients; cache by color+size key |

**Why no external library:** USS has no gradient support (confirmed via Unity forums, still true in Unity 6000.3.x). The Texture2D approach used by UIGradientHelper is the canonical workaround endorsed by the Unity community. No third-party package exists that does this better -- it is 30 lines of SetPixels/Apply. Adding a package would add dependency weight for trivial functionality.

**Confidence:** HIGH -- verified against [Unity forum discussions](https://discussions.unity.com/t/background-gradients/810125) and [USS gradient limitations](https://discussions.unity.com/t/uss-gradients-linear-gradient-and-image-gradients/934218).

#### UIGlowOverlay (NEW utility class)

The existing `CreateGlowOverlay()` in UIGradientHelper creates a single glow element. The v6.0 UI rebuild requires a more structured overlay system.

| Component | Status | Purpose |
|-----------|--------|---------|
| `UIGlowOverlay` class | NEEDS CREATION | Dedicated class for managing layered glow effects with PrimeTween animation |
| Inner glow layer | NEEDS CREATION | Tight glow around element border (small spread) |
| Outer glow layer | NEEDS CREATION | Wider ambient glow (large spread, low opacity) |
| Pulse animation | NEEDS CREATION | PrimeTween-driven opacity cycling on glow layers |
| Color transition | NEEDS CREATION | Smooth glow color changes on hero selection |

**Implementation approach:** Pure C# VisualElement manipulation + PrimeTween (target-based, no closures). No shaders, no custom render passes. This keeps everything in UI Toolkit's retained-mode renderer.

**Why not a URP shader-based glow:** UI Toolkit VisualElements do not participate in the 3D rendering pipeline. You cannot apply post-processing bloom to a VisualElement. The layered-element approach (radial gradient Texture2D on an oversized child element) is the correct technique for UI Toolkit glow effects.

**Confidence:** HIGH -- this is the pattern already used in `MoltenVeinVFX.cs` and `ButtonVFXHelper.cs` in the existing codebase.

#### Procedural Audio (EXISTS -- extend, do not replace)

| Component | Status | Location | Action |
|-----------|--------|----------|--------|
| `TitleScreenAudio` | Implemented (655 lines) | `Assets/Scripts/UI/Core/TitleScreenAudio.cs` | Already generates all audio procedurally via `AudioClip.Create` + `SetData`. Complete system. |
| Dark drone generation | Implemented | `GenerateDarkDrone()` | Stereo, 30s loop, crossfade, LFO modulation -- production quality |
| Wind texture generation | Implemented | `GenerateWindTexture()` | Low-pass filtered noise, breathing LFO |
| Bell/rumble generation | Implemented | `GenerateDistantBell()`, `GenerateLowRumble()` | Inharmonic partials, FM synthesis |
| Demon laugh generation | Implemented | `GenerateDemonLaugh()` | Pulsed envelope, harmonic distortion |
| VERA whisper generation | Implemented | `GenerateWhisper()` | Tremolo + formant approximation |
| Character select audio | MISSING | -- | Needs new `CharSelectAudio` class with hero-themed ambient layers |
| Audio generation thread safety | CONCERN | Uses `Random.value` in generation | `Random.value` uses Unity's PRNG, which is not thread-safe. For AudioClip generation this is fine (runs on main thread during Start), but flag if anyone moves generation to a background thread. |

**AudioClip.Create vs OnAudioFilterRead decision:**

The existing `TitleScreenAudio` uses `AudioClip.Create` + `SetData` (pre-generate all audio upfront, then play). This is the correct choice because:
- Audio is generated once during scene load, not every frame
- No audio thread concerns (all generation happens on main thread in `Start`)
- Clips are finite duration, looped via AudioSource
- `OnAudioFilterRead` would be better for real-time synthesis that responds to gameplay (not needed here)

**Do NOT switch to OnAudioFilterRead.** The pre-generation pattern is simpler, more predictable, and sufficient for ambient/atmospheric audio.

**Confidence:** HIGH -- verified against existing 655-line implementation.

### Domain 2: 3D Model Display in UI

**Key finding: Already implemented.** `HeroStageController.cs` is a complete RenderTexture-to-UI-Toolkit pipeline.

| Component | Status | Location | Action |
|-----------|--------|----------|--------|
| RenderTexture creation | Implemented | `HeroStageController.InitializeStage()` | 1024x1536, 4x MSAA, ARGB32 |
| Camera setup | Implemented | Same | Dedicated layer 31 "CharacterPreview", FOV 30, clear to transparent |
| 5-point lighting rig | Implemented | `CreateLightingRig()` | Key + Fill + Rim + Face + Ground lights with hero-themed colors |
| UI binding | Implemented | `BindRenderTextureToUI()` | `Background.FromRenderTexture()` on "hero-render-target" element |
| Drag-to-rotate | Implemented | Pointer events on render target | Mouse + gamepad stick rotation |
| Hero model swapping | Implemented | `SwapHeroModel()` (coroutine) | Fade-out, destroy, instantiate, fade-in |
| Lighting color transitions | Implemented | PrimeTween-driven lerp | Fill, rim, and ambient color transitions per hero |
| Placeholder fallback | Implemented | `CreatePlaceholderModel()` | Procedural capsule with emission when GLB unavailable |

**What needs fixing (not new stack):**
- `HeroDisplayConfig.modelPrefab` is null for all 4 heroes (placeholder active). This is a data issue, not a stack issue.
- 28 GLB models exist but need decimation (500k-1M tris down to 10k-50k) before they can be assigned as prefabs.
- glTFast 6.14.1 is already installed for runtime GLB loading. No additional packages needed.

**RenderTexture performance note:** The 1024x1536 @ 4x MSAA RenderTexture costs ~24 MB VRAM. This is acceptable for a single character preview. If multiple previews are needed simultaneously (e.g., party display), reduce to 512x768 or share a single RT with sequential rendering.

**Confidence:** HIGH -- verified against existing 400+ line implementation.

### Domain 3: Code Quality Tooling

This is the domain that requires the most new additions.

#### Roslyn Analyzers (NEW)

| Technology | Version | Purpose | How to Install | Confidence |
|------------|---------|---------|----------------|------------|
| Microsoft.Unity.Analyzers | 1.26.0 | Unity-specific C# diagnostics (UNT0001-UNT0026) | Download DLL from NuGet, place in `Assets/Plugins/Analyzers/`, label as `RoslynAnalyzer` | HIGH |

**Installation procedure (Unity-specific, not NuGet install):**

1. Download `Microsoft.Unity.Analyzers.1.26.0.nupkg` from [NuGet](https://www.nuget.org/packages/Microsoft.Unity.Analyzers)
2. Rename `.nupkg` to `.zip`, extract
3. Copy `analyzers/dotnet/cs/Microsoft.Unity.Analyzers.dll` to `Assets/Plugins/Analyzers/`
4. In Unity Inspector: select DLL, disable "Any Platform", disable "Editor", disable "Standalone"
5. Add Asset Label: `RoslynAnalyzer` (case-sensitive)
6. Unity regenerates .csproj files with analyzer reference

**Key rules this enables:**
- UNT0001: Empty Unity message (e.g., empty `Update()`)
- UNT0002: Inefficient tag comparison (use `CompareTag`)
- UNT0003: Usage of non-generic `GetComponent`
- UNT0005: Wrong `Time.time` usage
- UNT0006: Incorrect message signature
- UNT0010: `MonoBehaviour` type should not be abstract
- UNT0014: Invalid type for `SetPixels`
- UNT0017: `SetPixels` invocation is slow
- UNT0022: Inefficient `Material` property access
- UNT0026: Avoid using `GetComponent` in hot paths

**Why this specific analyzer:** Microsoft maintains it, it is the only Roslyn analyzer specifically designed for Unity projects. It understands Unity's execution model (Update/LateUpdate as hot paths, MonoBehaviour lifecycle, SerializeField semantics). Generic C# analyzers would generate false positives on Unity patterns.

**Confidence:** HIGH -- [Unity 6 official documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/roslyn-analyzers.html) confirms this installation method. [NuGet confirms v1.26.0](https://www.nuget.org/packages/Microsoft.Unity.Analyzers) (published 2026-02-03).

#### .editorconfig (NEW)

| Technology | Purpose | Confidence |
|------------|---------|------------|
| `.editorconfig` file at project root | Enforce naming conventions, formatting, analyzer severity | HIGH |

The project currently has NO `.editorconfig`. Adding one enforces the conventions documented in `.planning/codebase/CONVENTIONS.md` at the IDE level (Visual Studio, Rider).

**Key rules to configure:**

```ini
# Enforce VeilBreakers naming conventions
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.severity = warning
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.style = underscore_prefix

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case

# Constants with k prefix (enforced via code review, not .editorconfig -- regex needed)
# .editorconfig cannot enforce k-prefix; rely on VB_CodeReviewer for this
```

**Confidence:** HIGH -- .editorconfig is a standard mechanism supported by Visual Studio, Rider, and Unity's project generation.

#### VB_CodeReviewer (EXISTS -- extend)

| Component | Status | Location | Action |
|-----------|--------|----------|--------|
| Regex-based code review | Implemented (4006 lines, 60+ rules) | `Assets/Editor/VeilBreakers/VB_CodeReviewer.cs` | Already covers CRITICAL/HIGH/MEDIUM/LOW findings |
| Hot path detection | Implemented | `LineClassifier.HotMethodSig` | Detects Update/LateUpdate/FixedUpdate |
| Anti-pattern suppression | Implemented | `AntiPatterns` + `AntiPatternRadius` | Reduces false positives |
| FindingType classification | Implemented | `Error/Bug/Optimization/Strengthening` | Categorized findings |
| GUI reviewer window | Implemented | `MenuItem("VeilBreakers/Code Review/Open Reviewer")` | Interactive UI |
| Headless mode | Implemented | `RunHeadless()` | Console output for CI/automation |

**What to add for v6.0:**
- New rules for the bugs identified in Phase A-C (defender synergy, brand matrix violations, etc.)
- Rule for `Texture2D` without `Destroy()` (memory leak detection)
- Rule for `RenderTexture` without `Release()` + `Destroy()`
- Rule for `AudioClip.Create` without matching `Destroy()` in OnDestroy
- Rule for `style.backgroundImage` set without corresponding cleanup

**Why not add SonarQube/SonarAnalyzer:** The VB_CodeReviewer is already a comprehensive, Unity-aware static analyzer with 60+ rules, hot-path awareness, and confidence scoring. Adding SonarAnalyzer would duplicate effort and generate noise from rules that don't understand Unity patterns. Extend the existing reviewer instead.

**Confidence:** HIGH -- verified against the existing 4006-line implementation.

#### Test Infrastructure (EXISTS -- extend)

| Component | Status | Count | Action |
|-----------|--------|-------|--------|
| EditMode tests | Implemented | 10 test files | Covers: Brand, Corruption, Synergy, Damage, Capture, MainMenu, Scene, PrimeTween, HeroTheme |
| PlayMode tests | Implemented | 2 test files | Covers: CharacterSelect, MainMenuOverlay |
| RuntimeTests | Implemented | 8 test files | Covers: Audio, Capture, Combat, CombatUI, Gambit, QuickCommand, SaveSystem, StatusEffect |
| Code Coverage package | Installed | 1.2.7 | Already in packages-lock.json |

**What to add for v6.0:**
- EditMode tests for `UIGradientHelper` (verify pixel colors, texture dimensions, cleanup)
- EditMode tests for `DamageCalculator` bug fixes (defender synergy)
- EditMode tests for `BrandSystem` matrix corrections
- PlayMode test for title screen audio initialization (verify AudioClip generation does not throw)
- Use `com.unity.testtools.codecoverage` to track coverage percentage in CI

**No new test packages needed.** Unity Test Framework 1.6.0 + Code Coverage 1.2.7 are already installed.

**Confidence:** HIGH -- verified against existing test files.

---

## What NOT to Add

| Technology | Why Not | Confidence |
|------------|---------|------------|
| Shader Graph custom UI effects | UI Toolkit elements do not participate in URP rendering. Cannot apply bloom/post-processing to VisualElements. Use Texture2D gradients instead. | HIGH |
| Custom Render Passes for UI glow | Same reason. UI Toolkit has its own rendering backend, separate from URP's ScriptableRenderPass system. | HIGH |
| TextMeshPro | UI Toolkit uses its own text rendering (not TMP). Adding TMP would create two text systems. All text stays in UI Toolkit. | HIGH |
| DOTween / LeanTween | PrimeTween 1.3.8 is already installed and used throughout. Adding a second tweening library would cause API confusion and increase bundle size. | HIGH |
| FMOD / Wwise | Overkill for procedural audio. The project already generates all audio via `AudioClip.Create` + math. External audio middleware would add massive SDK overhead for features not needed (spatial audio authoring, middleware bus routing). | HIGH |
| NaughtyAttributes / Odin Inspector | Nice-to-have editor QoL, but adds dependency weight. The project uses custom ScriptableObjects and SerializeField patterns that work without editor extensions. Defer unless the team grows. | MEDIUM |
| UniRx / R3 / UniTask | The project uses C# events + EventBus + coroutines. Introducing reactive programming would require rewriting the event system. UniTask could help with async, but the existing coroutine-Task bridge works (flagged as "Revisit" in PROJECT.md, not "Replace"). | MEDIUM |
| SonarAnalyzer.CSharp | VB_CodeReviewer is a 4000-line Unity-aware analyzer already. Adding SonarAnalyzer would generate noise from non-Unity-aware rules and create duplicate findings. | MEDIUM |
| StyleCop.Analyzers | Overlaps with .editorconfig naming rules and VB_CodeReviewer quality rules. Would generate false positives on Unity patterns (e.g., SerializeField without documentation). | MEDIUM |
| Any new Unity packages | The packages-lock.json already has everything needed. Adaptive Performance, Memory Profiler, Profile Analyzer, and Code Coverage cover the tooling gap. No new packages to install. | HIGH |

---

## Integration Points

### How New Code Integrates with Existing Stack

```
UIGradientHelper (extended)
    |-- Used by: TitleScreenVFX, CharSelectVisualEnhancer, MoltenButtonVFX
    |-- Uses: Texture2D (Unity built-in)
    |-- Animated by: PrimeTween 1.3.8 (target-based opacity/color tweens)

UIGlowOverlay (new)
    |-- Used by: ButtonVFXHelper, CharSelectVisualEnhancer, TitleScreenVFX
    |-- Uses: UIGradientHelper.CreateRadialGradient()
    |-- Animated by: PrimeTween 1.3.8

TitleScreenAudio (extended)
    |-- Uses: AudioClip.Create (Unity built-in)
    |-- Respects: SettingsManager.Instance.Settings (volume, mute)
    |-- Pattern: Pre-generate in Start(), play via AudioSource

CharSelectAudio (new, follows TitleScreenAudio pattern)
    |-- Uses: AudioClip.Create (Unity built-in)
    |-- Subscribes: CharSelectEvents (hero change -> ambient color change)
    |-- Pattern: Same pre-generation approach as TitleScreenAudio

HeroStageController (fix data, not code)
    |-- Uses: RenderTexture, Camera, Background.FromRenderTexture() (all Unity built-in)
    |-- Needs: Decimated GLB models assigned to HeroDisplayConfig.modelPrefab
    |-- Loads via: glTFast 6.14.1 (already installed)

Microsoft.Unity.Analyzers (new)
    |-- Installed as: DLL in Assets/Plugins/Analyzers/ with RoslynAnalyzer label
    |-- Integrates with: Visual Studio / Rider IDE
    |-- Complements: VB_CodeReviewer (runtime regex) + .editorconfig (IDE enforcement)

.editorconfig (new)
    |-- Placed at: Project root
    |-- Enforces: _prefix naming, PascalCase properties, indentation
    |-- Integrates with: Visual Studio / Rider auto-format
```

### Dependency Chain

```
No new Unity packages
No new NuGet packages (Roslyn analyzer is a DLL drop, not a NuGet reference)
No new npm packages
No new Python packages

The only new FILES added to the project:
1. Assets/Plugins/Analyzers/Microsoft.Unity.Analyzers.dll (~200 KB)
2. .editorconfig (~2 KB)
3. Assets/Scripts/UI/Core/UIGlowOverlay.cs (new utility class)
4. Assets/Scripts/UI/CharacterSelect/CharSelectAudio.cs (new audio class)
```

---

## Version Compatibility Matrix

| Component | Required | Current | Status |
|-----------|----------|---------|--------|
| Unity | 6000.3.6f1 | 6000.3.6f1 | LOCKED |
| URP | 17.3.0 | 17.3.0 | LOCKED |
| PrimeTween | 1.3.8 | 1.3.8 | LOCKED |
| glTFast | 6.14.1 | 6.14.1 | LOCKED |
| Unity Test Framework | 1.6.0 | 1.6.0 | EXISTING |
| Code Coverage | 1.2.7 | 1.2.7 | EXISTING |
| Microsoft.Unity.Analyzers | 1.26.0 | Not installed | NEW -- DLL drop |
| .editorconfig | N/A | Not present | NEW -- file creation |

---

## Implementation Priority

Based on the v6.0 milestone phases (A through H), here is the order in which stack-related work should happen:

1. **Phase A-C (Bug Fixes + Hardening):** Add Microsoft.Unity.Analyzers + .editorconfig first. These catch issues while fixing bugs. Extend VB_CodeReviewer rules for Texture2D/RenderTexture leaks.

2. **Phase D (Title/CharSelect Bug Fixes):** No new stack. Use existing tools.

3. **Phase E (Title Screen UI Rebuild):** Extend UIGradientHelper (multi-stop, horizontal, texture cache). Create UIGlowOverlay. Harden TitleScreenAudio (already complete, just fix edge cases).

4. **Phase F (CharSelect UI Rebuild):** Create CharSelectAudio. Use extended UIGradientHelper + UIGlowOverlay.

5. **Phase G (3D Model Integration):** No new stack. HeroStageController already works. Work is model decimation + data assignment, not code.

6. **Phase H (E2E Verification):** Add new tests. Use Code Coverage to validate.

---

## Sources

### Verified (HIGH confidence)
- Existing codebase analysis: `UIGradientHelper.cs`, `TitleScreenVFX.cs`, `TitleScreenAudio.cs`, `HeroStageController.cs`, `VB_CodeReviewer.cs`, `ButtonVFXHelper.cs`, `MoltenVeinVFX.cs`
- [Unity 6 Roslyn Analyzers Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/roslyn-analyzers.html) -- Installation procedure
- [Unity 6 Install Existing Analyzer](https://docs.unity3d.com/6000.2/Documentation/Manual/install-existing-analyzer.html) -- DLL placement, RoslynAnalyzer label
- [Microsoft.Unity.Analyzers v1.26.0 on NuGet](https://www.nuget.org/packages/Microsoft.Unity.Analyzers) -- Latest version, published 2026-02-03
- [Microsoft.Unity.Analyzers GitHub](https://github.com/microsoft/Microsoft.Unity.Analyzers) -- Rule documentation
- [Unity Forum: Background Gradients](https://discussions.unity.com/t/background-gradients/810125) -- Confirms USS has no gradient support
- [Unity Forum: USS Gradients](https://discussions.unity.com/t/uss-gradients-linear-gradient-and-image-gradients/934218) -- Community confirms Texture2D workaround
- [Unity Forum: RenderTexture in VisualElement](https://discussions.unity.com/t/how-to-set-a-rendertexture-as-a-background-image-at-runtime/906830) -- Background.FromRenderTexture pattern
- [Unity AudioClip.Create API](https://docs.unity3d.com/ScriptReference/AudioClip.Create.html) -- Official API docs
- Unity `packages-lock.json` (local file) -- All package versions verified

### Community Research (MEDIUM confidence)
- [Unity How-To: Roslyn Analyzers](https://unity.com/how-to/debugging-with-rosyln-analyzers) -- Setup guide
- [Procedural Audio in Unity (Gamasutra)](https://www.gamedeveloper.com/audio/procedural-audio-in-unity) -- AudioClip.Create patterns
- [Procedural Audio in Unity (PixelEuphoria)](https://pixeleuphoria.com/blog/index.php/2021/01/23/synthesizing-procedural-audio/) -- Synthesis techniques

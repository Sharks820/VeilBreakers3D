# Project Research Summary

**Project:** VeilBreakers 3D v6.0 — AAA UI Rebuild, 3D Model Integration, Code Quality Hardening
**Domain:** Unity 6 dark fantasy RPG — brownfield milestone (existing codebase, targeted improvements)
**Researched:** 2026-03-30
**Confidence:** HIGH

---

## Executive Summary

VeilBreakers v6.0 is a brownfield milestone, not a greenfield build. The existing codebase is approximately 70% feature-complete in infrastructure terms: the RenderTexture pipeline for 3D model display exists and works, per-hero visual theming is implemented, all audio is generated procedurally, particle VFX systems are live, and the combat/capture/save systems are functional. The primary gap is not "build new systems" — it is "connect real assets to existing systems, fix known bugs, and harden code quality before the next major feature phase."

The recommended approach is strict phase isolation: fix the 5 critical bugs first (Phase A), then the 11 high-priority bugs (Phase B), then harden code quality infrastructure (Phase C), then repair UI inconsistencies (Phase D), then rebuild Title Screen VFX (Phase E), then rebuild Character Select VFX (Phase F), then integrate real 3D models (Phase G), then validate end-to-end (Phase H). This ordering is non-negotiable — bug fixes must precede UI rebuilds because the UI rebuilds will add 20+ new Texture2D objects and several new event subscribers, and those additions will interact badly with the known static-event leaks and texture memory issues that phases A-C must resolve.

The three key risks are: (1) cascade regressions from fixing interconnected combat systems simultaneously, (2) GPU memory leaks from the runtime gradient pipeline (every hero switch generates Texture2D objects without cleanup), and (3) USS stylesheet specificity conflicts that silently override runtime gradients. All three are preventable by following the isolation protocol in CLAUDE.md, implementing the UITextureRegistry pattern before UI rebuild work begins, and consolidating duplicate USS files before adding new styles.

---

## Key Findings

### Stack Additions Needed (Minimal)

The existing stack is almost entirely complete. Unity 6000.3.6f1, URP 17.3, UI Toolkit, PrimeTween 1.3.8, glTFast 6.14.1, and Unity Test Framework 1.6.0 cover all v6.0 needs. **No new Unity packages are required.**

The only net-new items are two files, not packages:

- **Microsoft.Unity.Analyzers v1.26.0** — DLL drop into `Assets/Plugins/Analyzers/`, labeled `RoslynAnalyzer`. Provides 10+ Unity-specific Roslyn diagnostics (inefficient tag comparison, GetComponent in hot paths, empty Unity messages). Install in Phase C.
- **.editorconfig at project root** — Enforces `_prefix` field naming and PascalCase properties at the IDE level. Zero runtime impact.

Two new C# utility classes with no external dependencies:

- **UIGlowOverlay.cs** — Standardizes layered glow effects driven by PrimeTween. Replaces ad-hoc patterns in CharSelectVisualEnhancer and ButtonVFXHelper.
- **CharSelectAudio.cs** — Hero-specific ambient drones following the existing TitleScreenAudio pre-generation pattern.

**What NOT to add:** No Shader Graph effects on UI (UI Toolkit elements cannot receive URP post-processing bloom), no TextMeshPro (UI Toolkit has its own text renderer), no DOTween (PrimeTween 1.3.8 is already installed), no FMOD/Wwise (procedural AudioClip.Create is sufficient for this scope), no SonarAnalyzer (VB_CodeReviewer is a 4000-line Unity-aware analyzer already covering this domain).

**Key discovery:** Unity 6000.3 supports `filter: blur()` natively in USS/C# via `FilterFunction`. The existing codebase comment stating "UI Toolkit can't replicate blur cleanly" is outdated. Panel glow halos and background defocus effects that currently use layered VisualElements with radial gradient textures can be replaced with native blur — reducing Texture2D allocations.

### Expected Features

**Must have (table stakes) — most already built:**

- Animated background on title screen (video + particle VFX) — EXISTS via TitleScreenVFX
- Atmospheric particle effects on title (embers, ash, sparks, 196 VisualElements) — EXISTS, may need polish
- Menu buttons with hover/press feedback — EXISTS via MoltenButtonVFX
- Ambient audio (drone + environmental) — EXISTS via TitleScreenAudio (655 lines, 10 procedural clips)
- 3D character model display in character select — INFRASTRUCTURE EXISTS (HeroStageController, 1024x1536 RT, 5-light rig); placeholder capsules active because `modelPrefab` is null on all 4 HeroDisplayConfigs
- Per-hero visual theming (colors, lighting, particles, audio) — EXISTS via HeroThemeConfig + HeroThemeTransitioner (15+ parameters per hero)
- Stat panel and hero name/class display — EXISTS via HeroStatsPanelController
- Hero navigation carousel — EXISTS via CarouselController
- Hold-to-embark confirmation — EXISTS via HoldToEmbarkController (2.5s hold, energy fill animation)
- Idle animation on 3D models — MISSING; all animation config slots are null; requires animation assets

**Should have (differentiators) — most already built:**

- Veil dissolve transitions between heroes — EXISTS via VeilDissolveController; needs shader material assigned
- Champion monster displayed alongside hero — CONFIG EXISTS (`championModelPrefab` in HeroDisplayConfig); needs model wiring
- Interactive model rotation (drag + gamepad stick) — EXISTS in HeroStageController
- URP post-processing per hero (bloom, vignette, chromatic aberration) — EXISTS via VolumeProfileTransitioner; needs VolumeProfile assets per hero
- VERA glitch text on title screen — EXISTS via GlitchTextEffect (used in CharSelect); port to title
- Per-hero particle systems on 3D stage — CONFIG EXISTS (`particlePrefab`); needs prefab assets

**Defer to v7.0:**

- Real-time 3D scene background on title (replace video loop)
- Full hero animation suite (showcase, embark cinematics, idle variants)
- Character creator / hero customization
- New heroes or monsters beyond the existing 4

### Architecture Approach

The codebase follows a well-structured layered architecture: scene controllers communicate through a typed event bus (`CharSelectEvents` for scene-scoped, `EventBus` for global), visual effects are scene-scoped MonoBehaviours that attach VisualElements to the UIDocument root, 3D content is rendered via RenderTexture and displayed in UI Toolkit via `Background.FromRenderTexture()`, and all UI animation uses PrimeTween target-based overloads. The main architectural work for v6.0 is standardizing inconsistent lifecycle patterns — singleton instantiation, Texture2D cleanup, event subscription placement — rather than introducing new patterns.

**Major components and their v6.0 status:**

1. **UIGradientHelper** — Static utility for runtime Texture2D gradient generation. Extend with multi-stop gradient support, horizontal gradient, and a texture cache keyed by color+size. Critical fix: refactor `CreateGlowOverlay()` to return the Texture2D it creates (currently the texture leaks — no caller can destroy it).
2. **UITextureRegistry** (NEW) — Per-MonoBehaviour disposal bag for runtime textures. Create in Phase C. All Phase E/F texture generation must route through it.
3. **UIVFXContainer** (NEW) — Named z-order layer manager (`Background`, `EnvironmentFX`, `ContentBehind`, `Content`, `ContentFront`, `OverlayFX`, `Overlay`). Eliminates the fragile `root.Insert(0, ...)` pattern used by 6 independent VFX controllers.
4. **HeroStageController** — RenderTexture-to-UI-Toolkit 3D preview pipeline. Architecture is sound and complete. Work is data (populate `modelPrefab` references), not code.
5. **TitleScreenAudio / CharSelectAudio** — Pre-generated procedural AudioClip pipeline. Pattern is correct. Fix: spread synchronous generation (~30ms spike) across frames using coroutine yields.
6. **EventBus / CharSelectEvents** — 65+ static event fields. Pattern rule: subscribe in `OnEnable`, unsubscribe in `OnDisable`, call `ClearAllListeners()` at the start of every scene load.

**Key patterns to follow:**

- Deferred VFX initialization via `GeometryChangedEvent` (layout dimensions are 0 until after first layout pass)
- Dirty-threshold animation updates (skip style writes when change delta is under 0.5%)
- Scene-scoped events for scene-scoped systems; never route scene events through the global EventBus
- RenderTexture cleanup order: clear `backgroundImage` on the VisualElement first, then `Release()`, then `Destroy()`

### Critical Pitfalls

1. **Cascade regressions from fixing interconnected combat systems simultaneously (CRIT-1)** — BattleManager, DamageCalculator, SynergySystem, BrandSystem, and CaptureManager are deeply coupled through 65+ static events. Fix in tier batches: Phase A only, then Phase B only. One commit per fix. Compile-check after every 3-5 changes. Never batch Phase A and B work in the same session.

2. **Texture2D GPU memory leaks from runtime gradient generation (CRIT-2)** — `UIGradientHelper.CreateGlowOverlay()` and `CreateTopHighlight()` create Texture2D objects but do not return them. Callers have no reference to `Destroy()` the texture. `MainMenuBootstrap` already has a confirmed leak. Create UITextureRegistry in Phase C, refactor all callers before Phase E/F add 20+ new textures per load.

3. **USS `background-color` silently overrides runtime Texture2D (CRIT-4)** — In Unity's UI Toolkit, USS `background-color` renders on top of `style.backgroundImage` (opposite of web CSS behavior). The gradient code runs without error but the element shows a flat color. Fix structurally: add `element.style.backgroundColor = StyleKeyword.None` inside `UIGradientHelper.ApplyGradient()` itself so it cannot be forgotten.

4. **Static event fields persist across scene loads (CRIT-5)** — 17 known instances of MonoBehaviours subscribing in `Start`/`Awake` instead of `OnEnable`, or unsubscribing in `OnDestroy` instead of `OnDisable`. Scene unload can destroy MonoBehaviours before `OnDisable` fires, leaving stale delegates in static events. Fix the pattern in Phase B before Phase E/F add new subscribers.

5. **Singleton migration breaks initialization order silently (HIGH-2)** — Migrating to `SingletonMonoBehaviour<T>` fails silently if the subclass declares `private void Awake()` instead of `protected override void OnSingletonAwake()`. C# method hiding means the base `Awake` never runs, `Instance` returns null, and 13+ downstream managers break with no obvious cause. Migrate one singleton per commit, enter play mode and verify `Instance != null` after each.

---

## Implications for Roadmap

The phase ordering validated by research maps directly to the PROJECT.md phases A through H. Research confirms the ordering is correct and adds specific implementation guidance per phase.

### Phase A: Critical Bug Fixes (5 bugs)

**Rationale:** Combat correctness must precede all other work. The defender synergy defense bug, brand matrix asymmetry, and CharSelectFocusManager div-by-zero are correctness errors that invalidate any testing done on top of them.

**Delivers:** Core game loop (combat, capture, character select) produces correct results.

**Key bugs to fix:** Defender synergy defense never applied, brand effectiveness matrix bidirectional violations, CharSelectFocusManager div-by-zero on empty hero list, CharSelectVisualEnhancer callback leak on re-enable.

**Avoids:** CRIT-1 cascade regressions — fix only these 5, one commit each, do not proceed to Phase B until all 5 pass.

**Research flag:** Well-documented bugs, patterns clear. No deeper research needed. Standard approach.

---

### Phase B: High-Priority Bug Fixes (11 bugs)

**Rationale:** Static event leaks (CRIT-5) must be fixed before Phase E/F add new event subscribers. Collection modification crashes (HIGH-3) affect combat reliability during extended sessions. Singleton duplicate-instance bugs affect all scene transitions.

**Delivers:** Scene transitions are clean, combat is stable, singletons are reliable.

**Key bugs to fix:** Static event persistence (17 instances — standardize OnEnable/OnDisable), collection modification in StatusEffectManager (snapshot before iteration), DontDestroyOnLoad duplicates in UIAnimationController, EmbarkCinematicController async hang, SharedAudioSource conflict between HoldToEmbark and CharSelectFocusManager.

**Avoids:** CRIT-5 stale delegate `MissingReferenceException` in Phases E/F.

**Research flag:** Mostly standard patterns. The async cancellation fix (MOD-3) uses `destroyCancellationToken` which is Unity 6-specific. One quick Context7 lookup to confirm the property name before writing.

---

### Phase C: Code Quality Hardening

**Rationale:** Install tooling and establish patterns BEFORE the UI rebuild phases create large amounts of new code. The Roslyn analyzer catches anti-patterns as code is written. UITextureRegistry must exist before Phase E/F create 20+ runtime textures. Singleton migration must complete before new singletons are added.

**Delivers:** Microsoft.Unity.Analyzers DLL installed, .editorconfig created, all singletons using `SingletonMonoBehaviour<T>`, PrimeTween closure-based animations converted to target-based, CancellationTokens added to MonoBehaviour async methods, Debug.Log calls individually classified and migrated to ErrorLogger, UITextureRegistry utility class created, UIGradientHelper.ApplyGradient patched to clear `background-color`.

**Avoids:** HIGH-1 (Debug.Log semantic change — classify each of the 146+ calls individually, not batch replace), HIGH-2 (singleton migration breaks init — one per commit, play mode verify after each).

**Stack additions in this phase:** Microsoft.Unity.Analyzers DLL drop, .editorconfig file.

**Research flag:** Debug.Log classification is judgment-intensive. 146+ calls across 30+ files must each be read in context to determine the correct ErrorLogger severity level. Cannot be automated or rushed. Budget significant time.

---

### Phase D: Title Screen + Character Select Bug Fixes

**Rationale:** Consolidate USS stylesheets and fix scene-level bugs before the UI rebuild adds new styles. Four duplicate CharacterSelect USS stylesheets (HIGH-5) will cause invisible specificity conflicts in Phase F if not resolved first.

**Delivers:** One canonical USS file per screen (TitleScreen.uss, CharacterSelect.uss, one global VeilBreakers.uss), VeilBreakersUI.uss merged and deleted, title screen visual bugs fixed, CharSelect interaction bugs fixed, gamepad navigation verified.

**Avoids:** HIGH-5 USS specificity conflicts invisible to developers.

**Research flag:** Mechanical file consolidation work. No research needed.

---

### Phase E: Title Screen AAA Rebuild

**Rationale:** Title screen is independent of CharSelect and 3D models — different scene, different files. Safe to overlap with Phase F if needed. Must come after Phase D USS consolidation.

**Delivers:** UITextureRegistry integrated into all gradient callers, UIGradientHelper extended (multi-stop gradient, horizontal gradient, texture cache), UIVFXContainer named layer system deployed, TitleScreenVFX decomposed from 3145-line god class into named subsystems (EmberParticleSystem, LightningSystem, etc.), TitleScreenAudio generation spread across frames to eliminate frame spike, native `filter: blur()` tested and used for panel glows, VERA glitch text ported from CharSelect to title, logo veil energy effect added, UIGlowOverlay utility class created.

**Avoids:** CRIT-2 texture leaks (UITextureRegistry routes all new textures), CRIT-4 USS override (ApplyGradient() clears background-color), MOD-5 god class fragility (decompose before enhancing).

**Research flag:** Native `filter: blur()` via `FilterFunction` in Unity 6000.3 is newly available and the codebase has an outdated comment saying it is not possible. One targeted Context7 lookup (`/needle-mirror/com.unity.ui`) for the exact `IStyle` property before writing any blur code.

---

### Phase F: Character Select AAA Rebuild

**Rationale:** Depends on Phase C (UITextureRegistry, singleton fixes) and Phase D (USS consolidation). Can overlap with Phase E since scenes are independent.

**Delivers:** Hero card carousel rebuilt with gradient/glow effects, UIGlowOverlay deployed on panels, CharSelectAudio class created (hero-specific ambient drones), VolumeProfile ScriptableObject assets created and tuned per hero, per-hero particle system prefabs for 3D stage, VeilDissolveController wired to actual dissolve shader (replaces VeilDissolvePlaceholder.mat), embark visual feedback layers polished.

**Avoids:** CRIT-3 RenderTexture race condition (follow HeroStageController cleanup pattern exactly: clear backgroundImage, MarkDirtyRepaint, then Release, then Destroy), MOD-2 PrimeTween API hallucination (Context7 lookup before every PrimeTween call).

**Research flag:** VolumeProfile tuning per hero requires visual iteration in Unity Editor. Not an engineering problem but budget significant editor art time.

---

### Phase G: 3D Model Integration

**Rationale:** Must come after Phase F — models display inside the rebuilt UI. The HeroStageController architecture is complete; this phase is entirely asset quality and data wiring.

**Delivers:** All 28 GLB models audited (polycount, normals, UVs, materials), ~500MB stale iterations deleted, best variant selected per hero from v1-v4, models decimated to 50K tris hero/30K tris monster budget, URP/Lit materials verified, `HeroDisplayConfig.modelPrefab` populated for all 4 heroes, camera FOV/offset tuned per hero, basic idle animation integrated (Mixamo humanoid idle as minimum viable path), champion monster model wired to `championModelPrefab` on at least one hero config.

**Avoids:** MOD-4 bad AI-generated geometry (run `blender_mesh action=game_check` per model before import), CRIT-3 GPU memory exhaustion (budget maximum 3 active RenderTextures; secondary previews at 512x768 not 1024x1536).

**Research flag:** Animation retargeting is the highest-risk gap. Mixamo retargeting assumes standard humanoid bone naming; AI-generated hero rigs may use non-standard names. Verify bone naming on one hero rig before committing to the Mixamo approach for all four. If incompatible, Blender rigging work will be required.

---

### Phase H: End-to-End Verification

**Rationale:** Validates all phases together. Each prior phase should run the verification items relevant to its changes; Phase H is the comprehensive final pass.

**Delivers:** Full flow tested (Title -> CharSelect -> Embark), Unity Profiler-verified memory stability (Texture2D count stable across 10 scene transitions, RenderTexture count never exceeds 3), frame time profiled and within target, all 10 items in the PITFALLS.md "Looks Working But Isn't" checklist passing, new EditMode tests written for DamageCalculator fixes and UIGradientHelper, Code Coverage report generated via `com.unity.testtools.codecoverage`.

**Research flag:** Standard verification patterns. No research needed.

---

### Phase Ordering Rationale

Three dependency chains drive the ordering:

1. **Correctness before aesthetics.** Bug fixes (A-D) precede UI rebuilds (E-F). A visually polished CharSelect built on top of a div-by-zero crash is a liability, not a feature.

2. **Infrastructure before feature code.** UITextureRegistry, Roslyn analyzer, and singleton migration (Phase C) must exist before Phases E-F generate large amounts of new code that would violate the patterns they enforce.

3. **USS cleanup before USS editing.** Consolidating duplicate stylesheets (Phase D) before adding new styles (Phases E-F) prevents invisible specificity conflicts.

The only true parallelism opportunity is Phase E (title screen) and Phase F (character select) — different scenes, different files, safe to overlap. All other phases are strictly sequential.

---

### Research Flags

**Needs targeted verification before implementation:**

- **Phase B:** `destroyCancellationToken` property on Unity 6 MonoBehaviour — one Context7 lookup before writing async cancellation code
- **Phase E:** `FilterFunction` and `filter: blur()` C# API in Unity 6000.3 — one Context7 lookup to confirm exact property names before any blur code
- **Phase G:** Mixamo retargeting compatibility with AI-generated hero rigs — test one rig manually before committing to this approach for all four heroes

**Standard patterns, skip research-phase:**

- **Phase A/B:** Bug fix patterns are well-documented in PITFALLS.md and CONCERNS.md
- **Phase C:** Singleton migration and Debug.Log classification patterns are clear
- **Phase D:** USS file consolidation is mechanical merge work
- **Phase F:** VolumeProfile tuning is Unity Editor art work, not engineering research
- **Phase H:** Unity Test Framework and Profiler usage are standard

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Verified against packages-lock.json, Unity 6 official docs, NuGet. No new packages — only DLL drops and new C# files. |
| Features | MEDIUM-HIGH | Codebase analysis (20+ files read directly) is HIGH confidence. AAA RPG pattern analysis synthesized from reference games is MEDIUM. 3D model import quality is unverified until models are tested in-editor. |
| Architecture | HIGH | Verified against 15+ source files. Patterns identified are already in use in the project — research is confirming and standardizing, not introducing new patterns. Native blur API availability confirmed via official Unity 6000.3 docs. |
| Pitfalls | HIGH | All five critical pitfalls are verified against the existing codebase (CONCERNS.md, bug lists, project history in CLAUDE.md). These are documented real bugs, not theoretical risks. |

**Overall confidence: HIGH**

### Gaps to Address

- **Idle animation assets** — The hardest unsolved gap. HeroDisplayConfig has config slots (`idleClip`, `idleVariantClips`, etc.) but all are null. Mixamo is the recommended path for humanoid idles but retargeting compatibility with AI-generated rigs is unverified. Budget 1-2 days for potential rig compatibility work.

- **VolumeProfile assets per hero** — `HeroThemeConfig.volumeProfile` field exists but no per-hero VolumeProfile ScriptableObjects have been created. These need creation and visual tuning in the Unity Editor. Not a code problem but requires editor art time.

- **VeilDissolveController shader** — `VeilDissolvePlaceholder.mat` is a placeholder material. The actual dissolve shader must be created (or a URP custom shader graph pass must be authored) and assigned. The controller C# exists; the shader does not.

- **Per-hero particle prefabs** — Config slots exist (`particlePrefab`, `maxParticleCount`) but no per-hero VFX prefabs have been authored. Minimum viable: simple point emitter with hero brand color per hero.

- **Model decimation scope** — 28 GLB models at 500K-1M triangles each require decimation to 10K-50K. VB-Toolkit provides `blender_mesh action=decimate`. Budget approximately 20-30 minutes per model for quality review and decimation — significant total time for 28 models.

---

## Sources

### Primary (HIGH confidence)

- Existing codebase (28+ source files read directly): UIGradientHelper.cs, TitleScreenAudio.cs, HeroStageController.cs, CharSelectVisualEnhancer.cs, TitleScreenVFX.cs, VB_CodeReviewer.cs, CharSelectEvents.cs, HeroThemeConfig.cs, HeroDisplayConfig.cs, MoltenButtonVFX.cs, ButtonVFXHelper.cs, VeilDissolveController.cs, CharacterSelectManager.cs, plus 15+ more
- Unity 6 Roslyn Analyzers Manual: https://docs.unity3d.com/6000.3/Documentation/Manual/roslyn-analyzers.html
- Unity 6 USS Filter Property: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/uss-filter.html
- Unity 6 Built-in Filters: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/built-in-filters.html
- Microsoft.Unity.Analyzers v1.26.0 on NuGet: https://www.nuget.org/packages/Microsoft.Unity.Analyzers
- Unity packages-lock.json (local): all package version verification
- Unity Forum: Background Gradients in UI Toolkit: https://discussions.unity.com/t/background-gradients/810125
- Unity Forum: USS Gradient Limitations: https://discussions.unity.com/t/uss-gradients-linear-gradient-and-image-gradients/934218
- Unity AudioClip.Create API: https://docs.unity3d.com/ScriptReference/AudioClip.Create.html
- Unity Render Camera to RenderTexture: https://docs.unity3d.com/6000.0/Documentation/Manual/output-to-render-texture.html

### Secondary (MEDIUM confidence)

- Game UI Database — Diablo IV, Elden Ring, Monster Hunter: World, Baldur's Gate 3 UI reference screenshots and pattern analysis
- Game Developer: Building BG3 Character Creator (Larian Studios)
- Gamasutra: Procedural Audio in Unity patterns
- ArtStation: Diablo IV UI art direction analysis

### Tertiary (LOW confidence, validate before use)

- Mixamo retargeting compatibility with AI-generated hero rigs — unverified until tested against actual VeilBreakers hero bone structures

---

*Research completed: 2026-03-30*
*Ready for roadmap: yes*

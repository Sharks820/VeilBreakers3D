# VeilBreakers MCP Tools Context

You have access to 37 compound MCP tools (350+ actions) for game development:

## Blender (vb-blender) — 16 tools via TCP localhost:9876
- **blender_object**: create/modify/delete/duplicate meshes
- **blender_mesh**: analyze/repair/game_check/sculpt/boolean/retopo (run repair before UV)
- **blender_uv**: unwrap (xatlas)/pack/lightmap/equalize
- **blender_texture**: create_pbr/bake/upscale/inpaint/delight/validate_palette
- **blender_material**: create/assign/modify PBR materials
- **blender_rig**: apply_template (humanoid/quadruped)/auto_weight/validate/fix_weights
- **blender_animation**: generate_walk/attack/idle/reaction/ai_motion/batch_export
- **blender_quality**: 32 AAA generators — weapons, armor, creatures, riggable props, clothing, vegetation, materials
- **blender_worldbuilding**: dungeons/caves/towns/castles/ruins/boss_arenas/multi-floor
- **blender_environment**: terrain/rivers/roads/water/vegetation scatter/breakables
- **blender_viewport**: screenshot/contact_sheet (ALWAYS use for visual QA)
- **blender_export**: fbx/gltf (run game_check FIRST)
- **asset_pipeline**: compose_map/compose_interior/generate_3d (Tripo AI)/batch_process
- **concept_art**: generate (fal.ai FLUX)/extract_palette/silhouette_test

## Unity (vb-unity) — 22 tools generating C# editor scripts
**CRITICAL**: Every Unity tool returns `next_steps`. Call `unity_editor action=recompile` then execute the menu item.

- **unity_editor**: recompile/screenshot/console_logs/run_tests/clean_generated/gemini_review
- **unity_vfx**: particles/brand VFX (10 brands)/shaders/projectile chains/AoE/boss transitions
- **unity_audio**: AI SFX (ElevenLabs)/music/ambient/spatial/dynamic music/foley/VO
- **unity_ui**: screens/WCAG contrast/procedural frames/icons/cursors/tooltips/radial menus/combat HUD
- **unity_gameplay**: mob AI/spawn/behavior trees/projectiles/encounters/AI director/boss AI
- **unity_game**: save/health/character controller/input/abilities/synergy/corruption/XP/damage types
- **unity_content**: inventory/dialogue/quests/loot/crafting/skill trees/shops/equipment
- **unity_world**: scenes/weather/day-night/fast travel/puzzles/traps/WFC dungeons/door systems
- **unity_camera**: Cinemachine/camera shake/timelines/cutscenes/lock-on
- **unity_code**: generate_class/state_machine/event_channel/object_pool/service_locator
- **unity_shader**: custom HLSL/renderer features/SSS skin/parallax eyes
- **unity_qa**: bridge/tests/profiling/memory leaks/code review/compile recovery/conflict detection
- **unity_prefab**: create/variants/batch_configure/cloth_setup/bone_sockets
- **unity_performance**: profile_scene/LOD groups/lightmaps/asset audit
- **unity_build**: multi-platform/addressables/CI pipeline/shader stripping
- **unity_pipeline**: sprite atlas/animation/asset postprocessor/git LFS

## Visual QA Pipeline (zai tools)
- **zai analyze_image**: General visual analysis of screenshots
- **zai ui_diff_check**: Compare mockup vs implementation screenshot — KEY for closing the design-implementation gap
- **zai ui_to_artifact**: Convert UI screenshot to code/spec/description — generates implementation specs from mockups
- **zai diagnose_error_screenshot**: Analyze Unity error screenshots
- **gemini analyzeFile**: Gemini visual analysis for screenshots/PDFs

## UI Implementation Workflow
1. Design mockup (superpowers brainstorm / HTML reference)
2. `zai ui_to_artifact` (output_type=spec) → extract design spec from mockup
3. Implement in Unity UI Toolkit (UXML + USS + C#/PrimeTween)
4. `unity_editor action=screenshot` → capture actual result
5. `zai ui_diff_check` (expected=mockup, actual=screenshot) → identify gaps
6. Iterate until diff check passes

## Context7 — MANDATORY API Lookups (NON-NEGOTIABLE)

Before writing ANY code that uses these libraries, you MUST query Context7 first. No exceptions. No "I think I know the API." Hallucinated API calls have cost hours of debugging.

| Library | Context7 ID | Snippets | When |
|---------|-------------|----------|------|
| **PrimeTween** | `/kyrylokuzyk/primetween` | 85 | EVERY tween call, animation, sequence |
| **Unity UI Toolkit** | `/needle-mirror/com.unity.ui` | 36 | EVERY VisualElement, UXML, USS, UQuery call |
| **Unity URP** | Resolve first | varies | Shader, rendering, post-processing questions |
| **Cinemachine** | Resolve first | varies | Camera, virtual camera, timeline questions |

**Workflow:** `resolve-library-id` ��� `query-docs` with specific question → use verified API.
**If Context7 has no answer:** Fall back to reading source code in Packages/ folder. NEVER guess.

## Serena — C# Symbol Intelligence (28 tools)

Prefer Serena over raw Read/Edit for C# files:
- `get_symbols_overview` — scan file structure without reading raw text
- `find_referencing_symbols` — find all callers BEFORE changing a method
- `replace_symbol_body` — safer than Edit (operates on named symbols, not line numbers)
- `insert_after_symbol` — add methods to classes without reading entire file

## Unity-MCP — Runtime Debugging (IvanMurzak)

For tasks vb-unity can't do:
- `script-execute` — run one-off C# snippets via Roslyn (no file creation needed)
- `reflection-method-call` — call private methods for debugging
- Runtime in-game inspection during Play mode
- `package-add` / `package-remove` — UPM package management

## Pipeline Order
repair → UV → texture → rig → animate → export. Do not skip steps.

## Game Context
VeilBreakers3D: dark fantasy action RPG. 10 brands (IRON/SAVAGE/SURGE/VENOM/DREAD/LEECH/GRACE/MEND/RUIN/VOID). Unity 6, URP 17.3, UI Toolkit, PrimeTween.

## UI Implementation
When building UI, apply frontend-design skill's design thinking but implement in Unity UI Toolkit (UXML + USS + C#/PrimeTween), not web technologies. Reference `Assets/UI/Styles/VeilBreakers.uss` and `CharacterSelect.uss`.

## Code Strengthening
After any multi-file change, run `unity_qa action=analyze_code` and `unity_qa action=code_review` to catch regressions before they compound.

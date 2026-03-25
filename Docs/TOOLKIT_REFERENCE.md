# VB-Toolkit Quick Reference

> **37 compound tools with 330+ actions.** Use these FIRST for any game development task.

## VB-Unity (22 tools — generates C# scripts to `Assets/Editor/Generated/`)

| When you need to... | Use this tool | Action |
|---------------------|---------------|--------|
| **Create a monster AI** | `unity_gameplay` | `create_mob_controller` (FSM: Patrol/Chase/Attack/Flee) |
| **Create a boss** | `unity_gameplay` | `create_boss_ai` (multi-phase hierarchical FSM) |
| **Set up combat** | `unity_game` | `create_player_combat`, `create_ability_system`, `create_synergy_engine` |
| **Brand damage system** | `unity_game` | `create_damage_types` (all 10 brands pre-configured) |
| **Corruption mechanics** | `unity_game` | `create_corruption_gameplay` (0-100% with tier thresholds) |
| **VFX for brands** | `unity_vfx` | `create_brand_vfx` (unique VFX per brand) |
| **Particle effects** | `unity_vfx` | `create_particle_vfx`, `create_environmental_vfx`, `create_aura_vfx` |
| **Shaders** | `unity_vfx` | `create_shader` (dissolve/force field/water/foliage/outline/damage) |
| **Post-processing** | `unity_vfx` | `setup_post_processing` (bloom/vignette/AO/DOF) |
| **UI screens** | `unity_ui` | `generate_ui_screen` (UXML+USS with dark fantasy theming) |
| **Damage numbers** | `unity_ux` | `create_damage_numbers` (PrimeTween + ObjectPool) |
| **Minimap** | `unity_ux` | `create_minimap` (orthographic camera + markers) |
| **Character select** | `unity_ux` | `create_character_select` (hero path carousel) |
| **Inventory** | `unity_content` | `create_inventory_system` (grid + equipment + UI) |
| **Dialogue** | `unity_content` | `create_dialogue_system` (branching, YarnSpinner-compatible) |
| **Quests** | `unity_content` | `create_quest_system` (state machine + tracker + UI) |
| **Loot tables** | `unity_content` | `create_loot_table` (weighted with brand affinity) |
| **Save system** | `unity_game` | `create_save_system` (JSON + AES encryption + migration) |
| **Scene lighting** | `unity_scene` | `setup_lighting` (time-of-day presets) |
| **NavMesh** | `unity_scene` | `bake_navmesh` (agent settings) |
| **Animator** | `unity_scene` | `create_animator` (states/transitions/blend trees) |
| **Terrain** | `unity_scene` | `setup_terrain` (from heightmap + splatmaps) |
| **Camera system** | `unity_camera` | `create_virtual_camera` (Cinemachine 3.x) |
| **Cutscenes** | `unity_camera` | `create_cutscene` (PlayableDirector) |
| **Prefabs** | `unity_prefab` | `create` (auto-wire: hero/monster/prop/ui profiles) |
| **C# classes** | `unity_code` | `generate_class` (any: MonoBehaviour, SO, interface, enum) |
| **State machine** | `unity_code` | `state_machine` (IState/StateMachine/BaseState) |
| **Event system** | `unity_code` | `event_channel` (SO event channels) |
| **Object pool** | `unity_code` | `object_pool` (ObjectPool<T>) |
| **FBX import** | `unity_assets` | `configure_fbx`, `remap_materials`, `auto_materials` |
| **Code quality** | `unity_qa` | `analyze_code` (anti-pattern detection, no Unity needed) |
| **Performance** | `unity_performance` | `profile_scene`, `audit_assets` |
| **AAA audit** | `unity_quality` | `aaa_audit` (combined quality check) |
| **Build** | `unity_build` | `build_multi_platform` (6 platforms + IL2CPP) |
| **Weather/day-night** | `unity_world` | `create_weather`, `create_day_night` |

## VB-Blender (15 tools — requires Blender running with addon)

| When you need to... | Use this tool | Action |
|---------------------|---------------|--------|
| **Generate 3D model from text** | `asset_pipeline` | `generate_3d` (Tripo3D API) |
| **Clean up AI model** | `asset_pipeline` | `cleanup` (repair→UV→PBR pipeline) |
| **Generate LODs** | `asset_pipeline` | `generate_lods` (LOD0-LOD3 decimation) |
| **Generate weapons** | `asset_pipeline` | `generate_weapon` (parametric mesh) |
| **Split character for equipment** | `asset_pipeline` | `split_character` (modular parts) |
| **Concept art** | `concept_art` | `generate` (fal.ai FLUX, needs FAL_KEY) |
| **Color palette** | `concept_art` | `extract_palette` (from image) |
| **Rig a creature** | `blender_rig` | `apply_template` (humanoid/quadruped/bird/etc.) |
| **Auto weight paint** | `blender_rig` | `auto_weight` |
| **Walk/run cycle** | `blender_animation` | `generate_walk` (biped/quadruped/hexapod/serpent) |
| **Attack animations** | `blender_animation` | `generate_attack` (8 types) |
| **Idle/death/spawn** | `blender_animation` | `generate_idle`, `generate_reaction` |
| **Batch export FBX** | `blender_animation` | `batch_export` (Unity-ready clips) |
| **Generate terrain** | `blender_environment` | `generate_terrain` (noise + erosion) |
| **Scatter vegetation** | `blender_environment` | `scatter_vegetation` (biome-aware Poisson) |
| **Generate dungeon** | `blender_worldbuilding` | `generate_dungeon` (BSP rooms + corridors) |
| **Generate town** | `blender_worldbuilding` | `generate_town` (Voronoi districts) |
| **Generate castle** | `blender_worldbuilding` | `generate_castle` (walls/towers/keep) |
| **Boss arena** | `blender_worldbuilding` | `generate_boss_arena` (cover/hazards/phases) |
| **UV unwrap** | `blender_uv` | `unwrap` (xatlas high-quality) |
| **Texture bake** | `blender_texture` | `bake` (normal/AO/combined maps) |
| **Mesh analysis** | `blender_mesh` | `analyze` (topology grading A-F) |
| **Retopology** | `blender_mesh` | `retopo` (Quadriflow) |
| **Export for Unity** | `blender_export` | FBX with Unity-compatible settings |

## VeilBreakers-Specific Pipelines

**Monster Creation:** `concept_art generate` → `asset_pipeline generate_3d` → `asset_pipeline cleanup` → `blender_rig apply_template` → `blender_animation generate_walk/attack/idle` → `blender_animation batch_export` → `unity_assets configure_fbx` → `unity_prefab create` (monster profile) → `unity_gameplay create_mob_controller` → `unity_vfx create_brand_vfx`

**Level Building:** `blender_environment generate_terrain` → `blender_environment paint_terrain` → `blender_worldbuilding generate_dungeon` → `blender_environment export_heightmap` → `unity_scene setup_terrain` → `unity_scene scatter_objects` → `unity_scene setup_lighting` → `unity_scene bake_navmesh`

**AAA World Pipeline:** `blender_environment generate_terrain` → `blender_environment terrain_spline_deform` → `blender_environment terrain_stamp` → `blender_worldbuilding generate_town` / `generate_castle` / `generate_location` → `blender_worldbuilding generate_linked_interior` → `blender_worldbuilding generate_boss_arena` → `blender_worldbuilding generate_multi_floor_dungeon` → `blender_environment export_heightmap` → `unity_scene setup_terrain` → `unity_scene scatter_objects` → `unity_scene setup_lighting` → `unity_scene bake_navmesh` → `mcp-unity` validation and screenshot critique loop

**Combat System:** `unity_game create_damage_types` → `unity_game create_player_combat` → `unity_game create_ability_system` → `unity_game create_synergy_engine` → `unity_game create_corruption_gameplay` → `unity_content create_loot_table` → `unity_content create_skill_tree`

**Note:** 2 stubs exist: `blender_texture inpaint` and `blender_animation generate_ai_motion`. ElevenLabs/Gemini/Tripo/fal.ai gracefully degrade to stubs when API keys are missing.

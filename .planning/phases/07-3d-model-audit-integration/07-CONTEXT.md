# Phase 7: 3D Model Audit & Integration - Context

## Current State

**GLB Models:** 24 files in `Assets/Art/Models/`
- Heroes: Nyx (v1-v4), Orion (v1-v4), Seraphina (v1-v4) -- **Vex MISSING** (no folder)
- Monsters: Bloodshade (v1-v4), Grimthorn (v1-v4), Voltgeist (v1-v4)
- File sizes: 16-29 MB per model (compressed GLB)

**HeroDisplayConfig SOs:** 4 assets at `Assets/Resources/CharacterSelect/HeroDisplayConfigs/`
- NyxDisplayConfig, OrionDisplayConfig, SeraphinaDisplayConfig, VexDisplayConfig
- All have `modelPrefab: {fileID: 0}` (null) -- placeholder capsules shown
- All have `championModelPrefab: {fileID: 0}` (null)
- Hero-stage-mergers champion display in left panel, not on main stage

**Hero-Monster Mapping (from heroes.json):**
- Vex -> skitter_teeth (not in Models/)
- Seraphina -> grimthorn
- Orion -> voltgeist
- Nyx -> bloodshade

**Key Code:**
- `HeroDisplayConfig.cs`: `modelPrefab` (GameObject) and `championModelPrefab` (GameObject) fields
- `HeroStageController.cs`: Instantiates `config.modelPrefab`, falls back to capsule placeholder if null
- Editor script pattern: MenuItem -> AssetDatabase ops (established in Phase 6)

## Decisions

- **v4 = latest variant** for each model (user-specified)
- **Vex model gap**: No GLB exists; mark as TODO, keep placeholder
- **Budget**: 50K tris hero, 30K tris monster
- **Approach**: Editor scripts via MenuItem (matches Phase 6 pattern)

## Constraints

- GLB import is Unity-native (glTFast or Unity GLTF package) -- no manual FBX conversion
- Model prefabs must be created via Unity Editor (AssetDatabase) -- cannot hand-edit .prefab YAML reliably
- Each hero needs exactly 1 prefab reference in their HeroDisplayConfig SO
- Champion monsters display in left data panel, wired per-hero via championModelPrefab

## Discovery Level: Level 1

GLB import is Unity-native. Editor MenuItem pattern established. No new libraries or external APIs needed. Quick verification of Unity's GLB import API and ModelImporter settings is sufficient.

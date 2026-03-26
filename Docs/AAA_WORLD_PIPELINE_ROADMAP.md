# AAA World Pipeline Roadmap

> **Goal:** Build an AI-operable world pipeline that can author AAA-grade terrain, biomes, cities, castles, monuments, encounter spaces, and interiors with strong editability, strong topology, and strong visual quality.

## Non-Negotiables

- The pipeline must be usable by AI agents end to end.
- The pipeline must produce editable source assets, not one-shot baked output only.
- The pipeline must support map-aware placement, not generic template dumps.
- The pipeline must support terrain-first composition and structure placement that conforms to terrain.
- The pipeline must support hero-quality architecture, including enterable buildings and large landmark structures.
- The pipeline must support texture and material edits after generation.
- The pipeline must validate visual quality with screenshots and structural checks before assets are accepted.
- The pipeline must not waste Tripo credits on world generation; Tripo stays for characters, monsters, NPCs, weapons, armor, clothing, and other smaller meshes.

## AAA Quality Standard

The target is not "correct geometry."
The target is:

- Strong silhouette at distance.
- Strong readable form language.
- Strong material layering and surface breakup.
- Terrain that feels art-directed rather than noise-generated.
- Buildings that feel designed, not stamped.
- Castle and city layouts that respond to the local landform.
- Interior spaces that are coherent, navigable, and furnishable.
- Encounter spaces with intentional player flow and narrative framing.
- Performance that stays stable enough for real iteration on a 32 GB / RTX 4060 Ti machine.

## Current Reality

The toolkit already has real depth in the lower layers:

- `blender_addon/handlers/environment.py` already generates terrain, biomes, water, roads, rivers, and heightmap export.
- `blender_addon/handlers/terrain_advanced.py` already has spline deformation, erosion paint, terrain stamps, and terrain-layer logic.
- `blender_addon/handlers/terrain_chunking.py` already has chunking and LOD metadata for streaming.
- `blender_addon/handlers/building_quality.py` already generates detailed walls, roofs, windows, arches, stairs, chimneys, battlements, and interior trim.
- `blender_addon/handlers/modular_building_kit.py` already provides modular architecture pieces for reusable large-scale construction.
- `blender_addon/handlers/settlement_generator.py` already has settlement placement, foundations, roads, props, and interior furnishing logic.
- `blender_addon/handlers/encounter_spaces.py` already has encounter templates and validation.
- `blender_addon/handlers/facial_topology.py`, `hair_system.py`, `clothing_system.py`, and `riggable_objects.py` already support anatomy-safe editable character and prop construction.

The missing piece is orchestration and quality enforcement.

## Core Problem To Fix

High-level commands still sometimes stop at layout markers, empties, or partial scaffolds instead of materializing finished geometry.

That is acceptable for planning tools.
It is not acceptable for the final AAA authoring path.

The fix is to make the top-level world commands call the real geometry, dressing, and validation layers before they return success.

## Target Pipeline

### 1. Intent Capture

Input should be a structured brief, not a vague prompt.

Required fields:

- World region type
- Biome type
- Terrain anchor
- Structure role
- Gameplay role
- Visual tone
- Scale target
- Traversal expectations
- Edit constraints
- Performance budget

Example:

- `castle`
- `cliffside`
- `wizard-experimentation-lab`
- `overhang above abyss`
- `storm-warped, overgrown, arcane-industrial`
- `playable interior, multiple levels, exterior landing zones`

### 2. Terrain Authoring

Terrain should be built first, then structures should conform to it.

Required capabilities:

- Heightmap generation
- Erosion pass
- Cliff sculpting
- Spline roads and rivers
- Biome masks
- Terrain stamps for mesas, craters, valleys, and ruins
- Terrain chunking for streaming

Acceptance criteria:

- Cliffside structures can inherit the slope and ledge profile below them.
- Roads and approaches can be graded to a target slope.
- Terrain seams remain hidden across chunks.
- Large terrain edits remain performant on the target machine.

### 3. Biome Composition

Biomes need to be layered, not single-noise regions.

Required capabilities:

- Primary biome selection
- Edge blending between biomes
- Ground material variation
- Scatter rules for rocks, trees, bones, debris, and ruin dressing
- Weathering and overgrowth intensity controls
- Biome-specific prop libraries

Acceptance criteria:

- Biome transitions do not look like hard binary borders.
- Forests, swamps, mountains, ruins, and corrupted zones can merge naturally.
- Scatter density can be art-directed by zone and by local slope.

### 4. World Layout

World layout should be spec-driven and terrain-aware.

Required capabilities:

- World graph generation
- Settlement placement
- Dungeon and monument placement
- Landmark clustering
- Road network generation
- POI density control
- Minimum distance enforcement

Acceptance criteria:

- Towns appear in plausible landforms.
- Castles appear on meaningful defensive or symbolic terrain.
- Monuments can be aligned to vistas, cliffs, roads, or ritual nodes.
- Random encounters can be tied to the world graph and biome logic.

### 5. Structure Generation

This is the most important quality layer for castles, cities, and enterable spaces.

Required capabilities:

- Modular wall, roof, tower, stair, arch, trim, and battlement assembly
- Footprint fitment against terrain
- Vertical stacking and floor planning
- Door and window placement logic
- Interior/exterior consistency
- Structural hierarchy that remains editable
- Hero variations per brief, not just generic presets

Acceptance criteria:

- A castle requested for a cliff face can overhang, terrace, bridge, and anchor itself intentionally to the cliff geometry.
- A wizard lab can bias toward observatories, hanging catwalks, ritual chambers, and asymmetrical tower additions.
- A city district can be generated from neighborhood logic, not a repeated block pattern.

### 6. Interiors And Furnishings

Interiors must be generated as part of the world, not afterthoughts.

Required capabilities:

- Room-type-based furnishing
- Door trigger and room linkage metadata
- Multi-floor interiors
- Furnishing by role and style
- Prop placement with collision and clearance rules
- Interactable object hooks

Acceptance criteria:

- Enterable buildings have believable internal circulation.
- Large structures can contain multiple meaningful rooms.
- Furnishings reinforce function and story.

### 7. Encounter and Event Areas

Encounter spaces need more than a layout template.

Required capabilities:

- Arena, gauntlet, stealth, siege, boss, and puzzle templates
- Trigger volumes
- Cover placement
- Line-of-sight validation
- Spawn placement
- Phase trigger support
- Event-space dressing

Acceptance criteria:

- Each encounter type reads clearly in silhouette and navigation.
- Cover and hazard placement supports gameplay intent.
- The same encounter can be re-authored against a different terrain anchor.

### 8. Dressing And Texture

Generation is not complete until it is visually dressed.

Required capabilities:

- PBR material assignment
- Texture atlas or trim-sheet support
- Decal placement
- Wear, moss, dirt, soot, and leak layers
- Window, trim, roof, stone, timber, metal, cloth, and magic-material variants
- Texture edits after generation

Acceptance criteria:

- The same castle can be restyled without rebuilding the full geometry.
- Material edits can respond to narrative changes and biome changes.
- Hero structures can receive unique surface treatment.

### 9. Validation And Critique

Every major generation step needs a quality gate.

Required capabilities:

- Screenshot capture
- Bounds validation
- Material presence checks
- Mesh count and topology checks
- Door clearance checks
- Floating prop detection
- Terrain intersection checks
- LOD presence checks
- AI critique loop for composition and style

Acceptance criteria:

- Bad assets fail fast before they waste time or credits.
- Agent-facing output includes a clear reason for rejection.
- A regenerated asset can be compared against the previous pass.

### 10. Export And Runtime Integration

The final output must remain usable in Unity.

Required capabilities:

- Additive scene structure
- Addressables-based chunk and district loading
- Terrain and building streaming
- NavMesh support
- Lighting and fog support
- Performance budgets for target hardware

Acceptance criteria:

- Large worlds load in pieces.
- Hero structures remain editable in source form.
- Runtime scenes stay stable and responsive.

## Workstreams

| Workstream | Scope | Priority |
|---|---|---|
| World orchestration | Wire top-level world commands to real terrain, building, and dressing pipelines | P0 |
| Terrain and biome | Terrain Tools-style editing, spline roads, cliffs, biome blending, chunking | P0 |
| Large architecture | Castles, cliffside structures, towns, ruins, monuments, modular cities | P0 |
| Interiors | Room planning, furnishings, triggers, multi-floor linking | P1 |
| Encounter spaces | Arena, siege, stealth, boss, event, and random encounter generation | P1 |
| Texture and material edits | PBR layering, decals, wear, biome-specific material passes | P1 |
| Validation loop | Screenshot critique, topology gating, fitment checks, performance gating | P0 |
| Runtime export | Unity scenes, Addressables, NavMesh, LOD, additive loading | P1 |
| Character editability | Face, hands, feet, hair, clothing, and corrective topology protection | P1 |

## Implementation Order

### Phase 0 - Safety And Credit Protection

- Make generation fail loudly when the asset file is missing, empty, or invalid.
- Validate imports before any cleanup or refinement steps.
- Add a quarantine path for bad Tripo outputs.
- Add anatomy-protected cleanup paths for characters and hero props.

### Phase 1 - Terrain First

- Expose and standardize terrain generation, erosion, spline deformation, and stamping.
- Add biome mask and scatter policy layers.
- Add road, river, and cliff alignment tools.
- Add chunking and LOD metadata export for large maps.

### Phase 2 - Hero Architecture

- Wire the modular building kit into the top-level world commands.
- Add terrain-aware castle and city fitment.
- Add cliffside and overhang-aware footprint solving.
- Add style variants that react to the brief.

### Phase 3 - Interiors And Encounters

- Materialize linked interiors instead of only placing markers.
- Generate furnishings by room role and story role.
- Materialize encounter spaces with cover, hazards, and triggers.
- Add event-area dressing and interactables.

### Phase 4 - Texture, Mood, And Dressing

- Add texture and material edit passes after mesh creation.
- Add biome-specific dirt, moss, weathering, and decal rules.
- Add hero pass lighting and silhouette validation.

### Phase 5 - Runtime Integration

- Export chunks and districts for Unity.
- Wire additive loading and Addressables for large regions.
- Add NavMesh and LOD validation for production scenes.
- Keep source assets editable after export.

## Immediate Code Targets

These are the first files that should be upgraded for AAA output:

- `blender_addon/handlers/worldbuilding.py`
- `blender_addon/handlers/worldbuilding_layout.py`
- `blender_addon/handlers/settlement_generator.py`
- `blender_addon/handlers/map_composer.py`
- `blender_addon/handlers/terrain_advanced.py`
- `blender_addon/handlers/terrain_chunking.py`
- `blender_addon/handlers/building_quality.py`
- `blender_addon/handlers/modular_building_kit.py`
- `blender_addon/handlers/encounter_spaces.py`
- `blender_addon/handlers/riggable_objects.py`
- `blender_addon/handlers/autonomous_loop.py`
- `src/veilbreakers_mcp/blender_server.py`
- `Docs/TOOLKIT_REFERENCE.md`

## AAA Editing Rules

- Every generated castle, town, or monument must accept a terrain anchor.
- Every large structure must support post-generation alignment and scaling edits.
- Every world asset must preserve a readable hierarchy for agents.
- Every world asset must support material and texture follow-up.
- Every world asset must have a screenshot validation step.
- Every model must remain editable after generation until the user explicitly bakes it.

## Character And Prop Editability Rules

- Faces must keep protected loops around eyes, mouth, nose, and jaw.
- Hands must preserve finger topology and knuckle deformation.
- Feet must preserve toes, arches, and ankle structure.
- Hair must remain card-based or groom-based and not collapse into unusable blobs.
- Clothing must keep seam regions and simulation-friendly structure.
- Weapons, armor, and props must preserve pivots and attachment points.

## Definition Of Done

- A user can ask for a specific world brief and get a terrain-aware, visually coherent, editable result.
- A user can ask for a cliffside castle and get a castle that conforms to the cliff, supports interior use, and reads as a deliberate AAA structure.
- A user can ask for a biome transition and get a seamless authored blend rather than a hard procedural edge.
- A user can ask for a city district, arena, or monument and get a playable layout with dressing, validation, and runtime export.
- Agents can inspect, modify, and refine the generated result without starting over.


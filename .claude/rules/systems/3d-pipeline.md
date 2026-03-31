---
paths:
  - "Assets/Art/Models/**/*"
  - "Assets/Art/**/*.glb"
  - "Assets/Art/**/*.fbx"
---

# 3D Model Pipeline (Blender -> GLB -> Unity)

## VB-Toolkit Pipeline Order (MUST follow)
1. Create/Import -> `blender_object` or `asset_pipeline generate_3d`
2. Repair -> `blender_mesh action=repair` (remove doubles, fix normals, fill holes)
3. UV Unwrap -> `blender_uv action=unwrap` (xatlas preferred)
4. Texture -> `blender_texture action=create_pbr`
5. Rig -> `blender_rig action=apply_template` (humanoid/quadruped)
6. Animate -> `blender_animation action=generate_walk/attack/idle`
7. Validate -> `blender_mesh action=game_check` (BEFORE export)
8. Export -> `blender_export format=glb`

## Polycount Targets
- Hero characters: 20K-50K | Enemy mobs: 5K-15K
- Boss characters: 75K-120K | Environment: 100K-500K (use LODs)

## Quality Gates
- Run `game_check` before ANY export
- Use `blender_viewport action=contact_sheet` for multi-angle visual QA
- After Unity import: verify materials + animation with `unity_editor action=screenshot`
- Never commit GLB files with validation errors

## Git LFS Required
- GLB/FBX files MUST use Git LFS (596MB+ of models currently untracked)

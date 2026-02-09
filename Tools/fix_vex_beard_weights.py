"""
Fix Vex Model - Beard Weights, Hand Weights, Texture, and FBX Export
====================================================================
Run this script in Blender's Scripting workspace with Vex.blend open.

Fixes:
1. Beard vertices incorrectly weighted to Clavicle/Upperarm (should be Head)
2. Hand vertices with poor weighting (should be L_Hand/R_Hand)
3. Packs textures into the blend file (they're in a temp directory)
4. Exports a clean FBX for Unity with embedded textures

Usage:
1. Open Vex.blend in Blender
2. Switch to the Scripting workspace (top tab bar)
3. Click "Open" in the text editor, navigate to this file
4. Click "Run Script" (play button)
5. Check the console (Window > Toggle System Console) for results
"""

import bpy
import os
from mathutils import Vector

# ===== CONFIGURATION =====
MESH_NAME = "tripo_mesh_a3f84655_c1d4_4471_86e4_b78fec5006f9"

# Bones
HEAD_BONE = "Head"
NECK_BONE = "NeckTwist02"
NECK1_BONE = "NeckTwist01"
L_HAND_BONE = "L_Hand"
R_HAND_BONE = "R_Hand"
L_FOREARM_BONE = "L_Forearm"
R_FOREARM_BONE = "R_Forearm"
L_FOREARM_TWIST1 = "L_ForearmTwist01"
R_FOREARM_TWIST1 = "R_ForearmTwist01"
L_FOREARM_TWIST2 = "L_ForearmTwist02"
R_FOREARM_TWIST2 = "R_ForearmTwist02"

# Known bone positions from diagnostic (Blender world space, Z-up, Y-forward)
# Head: (0.038, 0.003, 0.858)
# Neck: (0.039, 0.003, 0.836)

# Beard fix thresholds
BEARD_HEAD_WEIGHT_THRESHOLD = 0.3  # Fix vertices with less than this Head weight
BEARD_PROXIMITY_RADIUS = 0.12     # Max XY distance from head center for beard

# Hand fix thresholds
HAND_WEIGHT_THRESHOLD = 0.2       # Fix vertices with less than this Hand weight
HAND_PROXIMITY_RADIUS = 0.08      # Radius around hand bone to check

# FBX export path
FBX_EXPORT_PATH = os.path.join(
    os.path.dirname(bpy.data.filepath),
    "Vex.fbx"
)


def get_mesh():
    obj = bpy.data.objects.get(MESH_NAME)
    if obj and obj.type == 'MESH':
        return obj
    # Fallback: find any mesh with 'tripo' in name
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and 'tripo' in obj.name.lower():
            print(f"Found mesh: {obj.name}")
            return obj
    # Last fallback: any mesh
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            print(f"Using mesh: {obj.name}")
            return obj
    return None


def get_armature():
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            return obj
    return None


def get_bone_world_pos(armature, bone_name):
    bone = armature.data.bones.get(bone_name)
    if bone is None:
        return None
    return armature.matrix_world @ bone.head_local


def ensure_vg(mesh_obj, name):
    vg = mesh_obj.vertex_groups.get(name)
    if vg is None:
        vg = mesh_obj.vertex_groups.new(name=name)
    return vg


def get_weight(mesh_obj, vert_idx, vg_idx):
    try:
        return mesh_obj.vertex_groups[vg_idx].weight(vert_idx)
    except (RuntimeError, IndexError):
        return 0.0


def get_vert_groups(mesh_obj, vert):
    """Return dict of {group_name: weight} for a vertex."""
    result = {}
    for g in vert.groups:
        if g.group < len(mesh_obj.vertex_groups):
            result[mesh_obj.vertex_groups[g.group].name] = g.weight
    return result


# =====================================================================
# FIX 1: BEARD WEIGHTS
# =====================================================================
def fix_beard_weights(mesh_obj, armature):
    print("\n" + "=" * 50)
    print("FIX 1: BEARD VERTEX WEIGHTS")
    print("=" * 50)

    head_pos = get_bone_world_pos(armature, HEAD_BONE)
    neck_pos = get_bone_world_pos(armature, NECK_BONE)
    neck1_pos = get_bone_world_pos(armature, NECK1_BONE)

    if head_pos is None:
        print("ERROR: Head bone not found!")
        return 0

    print(f"Head bone: {head_pos}")
    print(f"Neck bone: {neck_pos}")

    head_vg = ensure_vg(mesh_obj, HEAD_BONE)
    neck_vg = ensure_vg(mesh_obj, NECK_BONE)
    neck1_vg = ensure_vg(mesh_obj, NECK1_BONE)
    head_vg_idx = head_vg.index
    neck_vg_idx = neck_vg.index

    mesh = mesh_obj.data
    world_matrix = mesh_obj.matrix_world

    # Also remove incorrect clavicle/upperarm weights from head-area vertices
    bad_groups = ["L_Clavicle", "R_Clavicle", "L_Upperarm", "R_Upperarm",
                  "L_UpperarmTwist01", "R_UpperarmTwist01",
                  "L_UpperarmTwist02", "R_UpperarmTwist02"]
    bad_vg_indices = set()
    for name in bad_groups:
        vg = mesh_obj.vertex_groups.get(name)
        if vg:
            bad_vg_indices.add(vg.index)

    fixed = 0
    cleaned = 0

    for vert in mesh.vertices:
        world_pos = world_matrix @ vert.co

        dz = world_pos.z - head_pos.z
        dy = world_pos.y - head_pos.y
        dx = abs(world_pos.x - head_pos.x)

        # Beard zone: below head, in front of face, near center X axis
        # Also include chin area and sides of jaw
        is_beard_zone = (
            dz < -0.01 and dz > -0.15 and  # below head but above chest
            dy < 0.0 and                     # in front of or at head center
            dx < 0.07 and                    # near center (not ears)
            world_pos.z > (neck_pos.z - 0.03 if neck_pos else head_pos.z - 0.15)
        )

        # Broader face zone: anywhere near the head that should have Head weight
        is_face_zone = (
            abs(dz) < 0.08 and  # within 8cm of head vertically
            dx < 0.08 and       # near center
            dy < 0.01           # front or center
        )

        if not (is_beard_zone or is_face_zone):
            continue

        head_weight = get_weight(mesh_obj, vert.index, head_vg_idx)

        # Clean up: remove shoulder/arm weights from face/beard vertices
        has_bad_weights = False
        for g in vert.groups:
            if g.group in bad_vg_indices and g.weight > 0.05:
                has_bad_weights = True
                break

        if has_bad_weights:
            # Remove bad bone influences from this face/beard vertex
            for g in vert.groups:
                if g.group in bad_vg_indices and g.weight > 0.01:
                    mesh_obj.vertex_groups[g.group].remove([vert.index])
                    cleaned += 1

        # Fix low Head weights
        if head_weight < BEARD_HEAD_WEIGHT_THRESHOLD:
            # Calculate weight based on distance from head bone
            dist = (world_pos - head_pos).length
            max_dist = 0.15

            # Gradient: closer to head = higher weight
            t = max(0.0, 1.0 - (dist / max_dist))

            # Beard area gets strong Head weight
            if is_beard_zone:
                w_head = 0.85 + (t * 0.15)  # 0.85 to 1.0
            else:
                w_head = 0.5 + (t * 0.45)  # 0.5 to 0.95

            # Smooth blend with neck for vertices near the boundary
            if neck_pos and world_pos.z < neck_pos.z + 0.02:
                neck_blend = max(0.0, 1.0 - ((world_pos.z - neck_pos.z + 0.02) / 0.04))
                w_neck = neck_blend * 0.3
                w_head = max(0.3, w_head - w_neck)
            else:
                w_neck = 0.0

            head_vg.add([vert.index], w_head, 'REPLACE')
            if w_neck > 0.01:
                neck_vg.add([vert.index], w_neck, 'REPLACE')

            fixed += 1

    print(f"Fixed {fixed} beard/face vertices with proper Head weights")
    print(f"Cleaned {cleaned} incorrect shoulder/arm weights from face area")
    return fixed


# =====================================================================
# FIX 2: HAND WEIGHTS
# =====================================================================
def fix_hand_weights(mesh_obj, armature):
    print("\n" + "=" * 50)
    print("FIX 2: HAND VERTEX WEIGHTS")
    print("=" * 50)

    results = {"fixed": 0, "cleaned": 0}

    for side, hand_bone, forearm_bone, twist1, twist2 in [
        ("Left", L_HAND_BONE, L_FOREARM_BONE, L_FOREARM_TWIST1, L_FOREARM_TWIST2),
        ("Right", R_HAND_BONE, R_FOREARM_BONE, R_FOREARM_TWIST1, R_FOREARM_TWIST2),
    ]:
        hand_pos = get_bone_world_pos(armature, hand_bone)
        forearm_pos = get_bone_world_pos(armature, forearm_bone)

        if hand_pos is None:
            print(f"WARNING: {hand_bone} not found, skipping {side} hand")
            continue

        print(f"\n{side} hand bone: {hand_pos}")

        hand_vg = ensure_vg(mesh_obj, hand_bone)
        hand_vg_idx = hand_vg.index
        forearm_vg = ensure_vg(mesh_obj, forearm_bone)
        twist1_vg = mesh_obj.vertex_groups.get(twist1)
        twist2_vg = mesh_obj.vertex_groups.get(twist2)

        mesh = mesh_obj.data
        world_matrix = mesh_obj.matrix_world

        fixed = 0

        for vert in mesh.vertices:
            world_pos = world_matrix @ vert.co
            dist_to_hand = (world_pos - hand_pos).length

            if dist_to_hand > HAND_PROXIMITY_RADIUS:
                continue

            hand_weight = get_weight(mesh_obj, vert.index, hand_vg_idx)

            if hand_weight >= HAND_WEIGHT_THRESHOLD:
                continue

            # Check what this vertex IS weighted to
            groups = get_vert_groups(mesh_obj, vert)
            total_weight = sum(groups.values())

            # Vertices very close to hand bone should be mostly Hand
            closeness = 1.0 - (dist_to_hand / HAND_PROXIMITY_RADIUS)

            # Strong hand weight for vertices near the hand bone
            w_hand = 0.6 + (closeness * 0.4)  # 0.6 to 1.0

            # If vertex had forearm twist weights, redistribute
            twist_weight = groups.get(twist1, 0) + groups.get(twist2, 0)
            if twist_weight > 0.3:
                # These are wrist area verts misweighted to forearm twist
                # Reduce twist, increase hand
                if twist1_vg:
                    twist1_vg.add([vert.index], max(0, groups.get(twist1, 0) * 0.3), 'REPLACE')
                if twist2_vg:
                    twist2_vg.add([vert.index], max(0, groups.get(twist2, 0) * 0.3), 'REPLACE')

            hand_vg.add([vert.index], w_hand, 'REPLACE')
            fixed += 1

        print(f"  Fixed {fixed} {side.lower()} hand vertices")
        results["fixed"] += fixed

    print(f"\nTotal hand vertices fixed: {results['fixed']}")
    return results["fixed"]


# =====================================================================
# FIX 3: PACK TEXTURES
# =====================================================================
def pack_textures():
    print("\n" + "=" * 50)
    print("FIX 3: PACK TEXTURES INTO BLEND FILE")
    print("=" * 50)

    packed = 0
    for img in bpy.data.images:
        if img.packed_file:
            print(f"  Already packed: {img.name}")
            continue
        if img.filepath and ("Temp" in img.filepath or "tmp" in img.filepath.lower()
                             or "blender_" in img.filepath):
            try:
                img.pack()
                packed += 1
                print(f"  Packed: {img.name} (was at temp path)")
            except Exception as e:
                print(f"  ERROR packing {img.name}: {e}")
        elif img.filepath and not img.packed_file:
            try:
                img.pack()
                packed += 1
                print(f"  Packed: {img.name}")
            except Exception as e:
                print(f"  WARN: Could not pack {img.name}: {e}")

    if packed == 0:
        print("  No textures needed packing")
    else:
        print(f"  Packed {packed} texture(s)")

    return packed


# =====================================================================
# FIX 4: NORMALIZE ALL WEIGHTS
# =====================================================================
def normalize_weights(mesh_obj):
    print("\n" + "=" * 50)
    print("FIX 4: NORMALIZE VERTEX WEIGHTS")
    print("=" * 50)

    # Select the mesh and normalize
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_obj

    bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    bpy.ops.object.mode_set(mode='OBJECT')

    print("  All vertex weights normalized")


# =====================================================================
# FIX 5: EXPORT FBX
# =====================================================================
def export_fbx():
    print("\n" + "=" * 50)
    print("FIX 5: EXPORT FBX FOR UNITY")
    print("=" * 50)

    print(f"  Export path: {FBX_EXPORT_PATH}")

    # Select armature and mesh for export
    bpy.ops.object.select_all(action='DESELECT')
    armature = get_armature()
    mesh_obj = get_mesh()

    if armature:
        armature.select_set(True)
    if mesh_obj:
        mesh_obj.select_set(True)
    if armature:
        bpy.context.view_layer.objects.active = armature

    bpy.ops.export_scene.fbx(
        filepath=FBX_EXPORT_PATH,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'ARMATURE', 'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_anim=False,  # We use Mixamo anims separately
        path_mode='COPY',
        embed_textures=True,  # Embed textures in FBX
        batch_mode='OFF',
        axis_forward='-Z',
        axis_up='Y',
    )

    print(f"  FBX exported successfully!")
    print(f"  Textures embedded: YES")

    # Also save the blend file with packed textures
    bpy.ops.wm.save_mainfile()
    print(f"  Blend file saved with packed textures")


# =====================================================================
# MAIN
# =====================================================================
def main():
    print("\n" + "#" * 60)
    print("  VEX MODEL FIX - Beard, Hands, Textures, Export")
    print("#" * 60)

    mesh_obj = get_mesh()
    if mesh_obj is None:
        print("ERROR: No mesh found! Is Vex.blend open?")
        return

    armature = get_armature()
    if armature is None:
        print("ERROR: No armature found!")
        return

    print(f"\nMesh: {mesh_obj.name} ({len(mesh_obj.data.vertices)} verts)")
    print(f"Armature: {armature.name}")

    # Ensure we're in object mode
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    # Run all fixes
    beard_fixed = fix_beard_weights(mesh_obj, armature)
    hand_fixed = fix_hand_weights(mesh_obj, armature)
    pack_textures()
    normalize_weights(mesh_obj)
    export_fbx()

    # Summary
    print("\n" + "#" * 60)
    print("  SUMMARY")
    print("#" * 60)
    print(f"  Beard/face vertices fixed: {beard_fixed}")
    print(f"  Hand vertices fixed:       {hand_fixed}")
    print(f"  FBX exported to:           {FBX_EXPORT_PATH}")
    print(f"  Textures:                  Packed + Embedded")
    print("\n  Unity will auto-reimport the FBX on next focus.")
    print("  Run 'VeilBreakers > Animation > Reimport Vex Animations'")
    print("  then 'VeilBreakers > Animation > Build Vex Animator Controller'")
    print("#" * 60)


if __name__ == "__main__":
    main()

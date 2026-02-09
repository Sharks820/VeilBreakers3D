# VeilBreakers Character Template for Blender
# Run this script in Blender to set up a character-ready scene

import bpy
import os

bl_info = {
    "name": "VeilBreakers Character Template",
    "blender": (3, 6, 0),
    "category": "Object",
}


def setup_veilbreakers_scene():
    """Set up a VeilBreakers-optimized character scene"""
    
    # Clear existing objects
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    
    # Setup units for game scale (1 unit = 1 meter)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'
    
    # Setup frame rate for game animations (60fps)
    scene.render.fps = 60
    scene.render.fps_base = 1.0
    
    # Create reference objects
    
    # 1. Grid floor (for scale reference)
    bpy.ops.mesh.primitive_grid_add(
        size=10,
        x_subdivisions=20,
        y_subdivisions=20,
        location=(0, 0, 0)
    )
    grid = bpy.context.active_object
    grid.name = "Reference_Grid"
    grid.display_type = 'WIRE'
    grid.hide_render = True
    
    # 2. Height reference (typical character height ~1.7m)
    bpy.ops.mesh.primitive_cube_add(
        size=1,
        location=(2, 0, 0.85)
    )
    height_ref = bpy.context.active_object
    height_ref.name = "Height_Reference_1.7m"
    height_ref.scale = (0.3, 0.3, 1.7)
    height_ref.display_type = 'WIRE'
    height_ref.hide_render = True
    
    # 3. Camera at typical game view angle
    bpy.ops.object.camera_add(
        location=(3, -5, 2.5),
        rotation=(1.1, 0, 0.5)
    )
    camera = bpy.context.active_object
    camera.name = "Game_Camera"
    scene.camera = camera
    
    # 4. Three-point lighting setup
    # Key light
    bpy.ops.object.light_add(
        type='SUN',
        location=(5, 5, 10),
        rotation=(0.9, 0, 0.8)
    )
    key_light = bpy.context.active_object
    key_light.name = "Key_Light"
    key_light.data.energy = 5.0
    key_light.data.color = (1.0, 0.98, 0.95)
    
    # Fill light
    bpy.ops.object.light_add(
        type='AREA',
        location=(-5, 0, 5),
        rotation=(1.0, 0, -1.0)
    )
    fill_light = bpy.context.active_object
    fill_light.name = "Fill_Light"
    fill_light.data.energy = 2.0
    fill_light.data.color = (0.9, 0.95, 1.0)
    
    # Rim light
    bpy.ops.object.light_add(
        type='SPOT',
        location=(0, 5, 5),
        rotation=(1.2, 0, 3.14)
    )
    rim_light = bpy.context.active_object
    rim_light.name = "Rim_Light"
    rim_light.data.energy = 10.0
    rim_light.data.color = (1.0, 1.0, 1.0)
    rim_light.data.spot_size = 1.0
    
    # 5. Placeholder character mesh (to be replaced)
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=32,
        ring_count=16,
        radius=0.5,
        location=(0, 0, 1)
    )
    placeholder = bpy.context.active_object
    placeholder.name = "Character_Mesh_Placeholder"
    
    # Add subdivision for smooth preview
    subsurf = placeholder.modifiers.new(name="Subdivision", type='SUBSURF')
    subsurf.levels = 2
    subsurf.render_levels = 2
    
    # Setup viewport shading
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            for space in area.spaces:
                if space.type == 'VIEW_3D':
                    space.shading.type = 'MATERIAL'
                    space.shading.use_scene_lights = True
                    space.shading.use_scene_world = True
    
    # Create collections for organization
    if "Character" not in bpy.data.collections:
        char_col = bpy.data.collections.new("Character")
        bpy.context.scene.collection.children.link(char_col)
        char_col.objects.link(placeholder)
        bpy.context.scene.collection.objects.unlink(placeholder)
    
    if "Reference" not in bpy.data.collections:
        ref_col = bpy.data.collections.new("Reference")
        bpy.context.scene.collection.children.link(ref_col)
        ref_col.objects.link(grid)
        ref_col.objects.link(height_ref)
        bpy.context.scene.collection.objects.unlink(grid)
        bpy.context.scene.collection.objects.unlink(height_ref)
        ref_col.hide_render = True
    
    if "Lighting" not in bpy.data.collections:
        light_col = bpy.data.collections.new("Lighting")
        bpy.context.scene.collection.children.link(light_col)
        light_col.objects.link(key_light)
        light_col.objects.link(fill_light)
        light_col.objects.link(rim_light)
        bpy.context.scene.collection.objects.unlink(key_light)
        bpy.context.scene.collection.objects.unlink(fill_light)
        bpy.context.scene.collection.objects.unlink(rim_light)
        light_col.hide_render = True
    
    # Setup export-ready collections
    if "Export_Character" not in bpy.data.collections:
        export_col = bpy.data.collections.new("Export_Character")
        bpy.context.scene.collection.children.link(export_col)
    
    print("=" * 50)
    print("VeilBreakers Character Template Loaded!")
    print("=" * 50)
    print("\nScene setup complete:")
    print("- Units: Metric (1 unit = 1 meter)")
    print("- Frame rate: 60fps")
    print("- Three-point lighting configured")
    print("- Reference objects added for scale")
    print("\nNext steps:")
    print("1. Replace the placeholder sphere with your character mesh")
    print("2. Move final mesh to 'Export_Character' collection")
    print("3. Use VeilBreakers DCC Bridge to export")
    print("=" * 50)


if __name__ == "__main__":
    setup_veilbreakers_scene()

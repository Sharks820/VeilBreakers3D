# VeilBreakers DCC Bridge - Blender to Unity Integration
# This addon provides one-click export from Blender to Unity for VeilBreakers3D

bl_info = {
    "name": "VeilBreakers DCC Bridge",
    "author": "VeilBreakers Team",
    "version": (1, 0, 0),
    "blender": (3, 6, 0),
    "location": "View3D > Sidebar > VeilBreakers",
    "description": "One-click export from Blender to Unity for VeilBreakers3D project",
    "category": "Import-Export",
    "support": "COMMUNITY",
}

import bpy
import os
import json
from bpy.props import StringProperty, BoolProperty, EnumProperty, FloatProperty
from bpy.types import Panel, Operator, PropertyGroup


# =============================================================================
# PROPERTIES
# =============================================================================

class VeilBreakersDCCProperties(PropertyGroup):
    """Properties for the DCC Bridge"""
    
    unity_project_path: StringProperty(
        name="Unity Project Path",
        description="Path to the VeilBreakers3D Unity project",
        default="C:/Users/Conner/OneDrive/Documents/VeilBreakers3DCurrent",
        subtype='DIR_PATH'
    )
    
    export_subfolder: StringProperty(
        name="Export Subfolder",
        description="Subfolder within Assets/Resources/Art/3D_Models/",
        default="Characters",
        subtype='NONE'
    )
    
    export_format: EnumProperty(
        name="Export Format",
        description="Export file format",
        items=[
            ('FBX', "FBX (.fbx)", "Export as FBX for Unity"),
            ('GLTF', "glTF 2.0 (.gltf)", "Export as glTF for Unity (requires glTFast)"),
        ],
        default='FBX'
    )
    
    # FBX Export Settings
    apply_modifiers: BoolProperty(
        name="Apply Modifiers",
        description="Apply modifiers before export",
        default=True
    )
    
    use_triangles: BoolProperty(
        name="Triangulate Mesh",
        description="Convert all geometry to triangles (recommended for game assets)",
        default=True
    )
    
    use_armature: BoolProperty(
        name="Include Armature",
        description="Export armature/rig",
        default=True
    )
    
    use_animation: BoolProperty(
        name="Include Animation",
        description="Export animations",
        default=True
    )
    
    use_shape_keys: BoolProperty(
        name="Include Shape Keys",
        description="Export blend shapes/morph targets (ARKit compatible)",
        default=True
    )
    
    bake_animation: BoolProperty(
        name="Bake Animation",
        description="Bake space transform into animations",
        default=True
    )
    
    add_leaf_bones: BoolProperty(
        name="Add Leaf Bones",
        description="Add leaf bones (disable for cleaner exports)",
        default=False
    )
    
    axis_forward: EnumProperty(
        name="Forward",
        items=[
            ('X', "X", ""),
            ('Y', "Y", ""),
            ('Z', "Z", ""),
            ('-X', "-X", ""),
            ('-Y', "-Y", ""),
            ('-Z', "-Z", ""),
        ],
        default='-Z'
    )
    
    axis_up: EnumProperty(
        name="Up",
        items=[
            ('X', "X", ""),
            ('Y', "Y", ""),
            ('Z', "Z", ""),
            ('-X', "-X", ""),
            ('-Y', "-Y", ""),
            ('-Z', "-Z", ""),
        ],
        default='Y'
    )
    
    global_scale: FloatProperty(
        name="Scale",
        description="Scale all data",
        default=1.0,
        min=0.001,
        max=1000.0
    )
    
    # Quick export presets
    preset: EnumProperty(
        name="Export Preset",
        description="Quick export preset",
        items=[
            ('CUSTOM', "Custom", "Use custom settings"),
            ('CHARACTER', "Character", "Optimized for character models with rig"),
            ('PROP', "Prop/Static", "Optimized for static props"),
            ('ENVIRONMENT', "Environment", "Optimized for environment pieces"),
            ('ANIMATION', "Animation Only", "Export animation data only"),
        ],
        default='CHARACTER'
    )


# =============================================================================
# OPERATORS
# =============================================================================

class VEILBREAKERS_OT_export_to_unity(Operator):
    """Export selected objects to Unity project"""
    bl_idname = "veilbreakers.export_to_unity"
    bl_label = "Export to Unity"
    bl_description = "Export selected objects to the Unity project"
    bl_options = {'REGISTER', 'UNDO'}
    
    @classmethod
    def poll(cls, context):
        return context.selected_objects
    
    def execute(self, context):
        props = context.scene.veilbreakers_dcc_props
        
        # Build export path
        base_path = os.path.normpath(props.unity_project_path)
        export_path = os.path.join(
            base_path, 
            "Assets", 
            "Resources", 
            "Art", 
            "3D_Models",
            props.export_subfolder
        )
        
        # Ensure directory exists
        os.makedirs(export_path, exist_ok=True)
        
        # Get selected objects
        selected = context.selected_objects
        if not selected:
            self.report({'ERROR'}, "No objects selected")
            return {'CANCELLED'}
        
        # Generate filename from active object or collection
        active_obj = context.active_object
        if active_obj:
            filename = active_obj.name
        else:
            filename = "exported_model"
        
        # Clean filename
        filename = "".join(c for c in filename if c.isalnum() or c in ('_', '-')).rstrip()
        
        # Export based on format
        if props.export_format == 'FBX':
            filepath = os.path.join(export_path, f"{filename}.fbx")
            self.export_fbx(context, filepath)
        else:
            filepath = os.path.join(export_path, f"{filename}.gltf")
            self.export_gltf(context, filepath)
        
        self.report({'INFO'}, f"Exported to: {filepath}")
        return {'FINISHED'}
    
    def export_fbx(self, context, filepath):
        props = context.scene.veilbreakers_dcc_props
        
        # Apply preset settings
        preset_settings = self.get_preset_settings(props.preset)
        
        bpy.ops.export_scene.fbx(
            filepath=filepath,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_NONE',
            axis_forward=props.axis_forward,
            axis_up=props.axis_up,
            global_scale=props.global_scale,
            apply_modifiers=props.apply_modifiers,
            mesh_smooth_type='FACE',
            use_mesh_edges=False,
            use_tspace=True,
            use_custom_props=False,
            add_leaf_bones=props.add_leaf_bones,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            use_armature_deform_only=props.use_armature,
            armature_nodetype='NULL',
            bake_anim=props.use_animation and props.bake_animation,
            bake_anim_use_all_bones=True,
            bake_anim_use_nla_strips=True,
            bake_anim_use_all_actions=True,
            bake_anim_force_startend_keying=True,
            path_mode='AUTO',
            embed_textures=False,
            batch_mode='OFF',
            use_triangles=props.use_triangles,
        )
    
    def export_gltf(self, context, filepath):
        props = context.scene.veilbreakers_dcc_props
        
        bpy.ops.export_scene.gltf(
            filepath=filepath,
            export_format='GLTF_SEPARATE',
            use_selection=True,
            export_yup=True,
            export_apply=props.apply_modifiers,
            export_animations=props.use_animation,
            export_skins=props.use_armature,
            export_morph=props.use_shape_keys,
            export_morph_normal=True,
            export_morph_tangent=False,
        )
    
    def get_preset_settings(self, preset):
        """Get preset-specific settings"""
        presets = {
            'CHARACTER': {
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': True,
                'use_animation': True,
                'use_shape_keys': True,
                'bake_animation': True,
                'add_leaf_bones': False,
            },
            'PROP': {
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': False,
                'use_animation': False,
                'use_shape_keys': False,
                'bake_animation': False,
                'add_leaf_bones': False,
            },
            'ENVIRONMENT': {
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': False,
                'use_animation': False,
                'use_shape_keys': False,
                'bake_animation': False,
                'add_leaf_bones': False,
            },
            'ANIMATION': {
                'apply_modifiers': False,
                'use_triangles': False,
                'use_armature': True,
                'use_animation': True,
                'use_shape_keys': True,
                'bake_animation': True,
                'add_leaf_bones': False,
            },
        }
        return presets.get(preset, presets['CHARACTER'])


class VEILBREAKERS_OT_apply_preset(Operator):
    """Apply export preset settings"""
    bl_idname = "veilbreakers.apply_preset"
    bl_label = "Apply Preset"
    bl_description = "Apply preset export settings"
    bl_options = {'REGISTER', 'UNDO'}
    
    preset_name: StringProperty(default="CHARACTER")
    
    def execute(self, context):
        props = context.scene.veilbreakers_dcc_props
        preset = self.preset_name
        
        presets = {
            'CHARACTER': {
                'export_subfolder': "Characters",
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': True,
                'use_animation': True,
                'use_shape_keys': True,
                'bake_animation': True,
                'add_leaf_bones': False,
            },
            'PROP': {
                'export_subfolder': "Props",
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': False,
                'use_animation': False,
                'use_shape_keys': False,
                'bake_animation': False,
                'add_leaf_bones': False,
            },
            'ENVIRONMENT': {
                'export_subfolder': "Environment",
                'apply_modifiers': True,
                'use_triangles': True,
                'use_armature': False,
                'use_animation': False,
                'use_shape_keys': False,
                'bake_animation': False,
                'add_leaf_bones': False,
            },
            'ANIMATION': {
                'export_subfolder': "Animations",
                'apply_modifiers': False,
                'use_triangles': False,
                'use_armature': True,
                'use_animation': True,
                'use_shape_keys': True,
                'bake_animation': True,
                'add_leaf_bones': False,
            },
        }
        
        if preset in presets:
            settings = presets[preset]
            for key, value in settings.items():
                setattr(props, key, value)
            props.preset = preset
        
        self.report({'INFO'}, f"Applied {preset} preset")
        return {'FINISHED'}


class VEILBREAKERS_OT_quick_export_character(Operator):
    """Quick export selected as character"""
    bl_idname = "veilbreakers.quick_export_character"
    bl_label = "Export Character"
    bl_description = "Quick export selected objects as character"
    bl_options = {'REGISTER', 'UNDO'}
    
    @classmethod
    def poll(cls, context):
        return context.selected_objects
    
    def execute(self, context):
        bpy.ops.veilbreakers.apply_preset(preset_name="CHARACTER")
        bpy.ops.veilbreakers.export_to_unity()
        return {'FINISHED'}


class VEILBREAKERS_OT_quick_export_prop(Operator):
    """Quick export selected as prop"""
    bl_idname = "veilbreakers.quick_export_prop"
    bl_label = "Export Prop"
    bl_description = "Quick export selected objects as prop"
    bl_options = {'REGISTER', 'UNDO'}
    
    @classmethod
    def poll(cls, context):
        return context.selected_objects
    
    def execute(self, context):
        bpy.ops.veilbreakers.apply_preset(preset_name="PROP")
        bpy.ops.veilbreakers.export_to_unity()
        return {'FINISHED'}


# =============================================================================
# PANELS
# =============================================================================

class VEILBREAKERS_PT_dcc_bridge(Panel):
    """VeilBreakers DCC Bridge Panel"""
    bl_label = "VeilBreakers DCC Bridge"
    bl_idname = "VEILBREAKERS_PT_dcc_bridge"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'VeilBreakers'
    
    def draw(self, context):
        layout = self.layout
        props = context.scene.veilbreakers_dcc_props
        
        # Project Path
        box = layout.box()
        box.label(text="Unity Project", icon='FILE_FOLDER')
        box.prop(props, "unity_project_path")
        
        # Quick Export Buttons
        box = layout.box()
        box.label(text="Quick Export", icon='EXPORT')
        row = box.row(align=True)
        row.operator("veilbreakers.quick_export_character", icon='ARMATURE_DATA')
        row.operator("veilbreakers.quick_export_prop", icon='OBJECT_DATA')
        
        # Export Settings
        box = layout.box()
        box.label(text="Export Settings", icon='SETTINGS')
        box.prop(props, "preset")
        box.prop(props, "export_subfolder")
        box.prop(props, "export_format")
        
        if props.export_format == 'FBX':
            box.prop(props, "axis_forward")
            box.prop(props, "axis_up")
            box.prop(props, "global_scale")
        
        # FBX Options
        box = layout.box()
        box.label(text="FBX Options", icon='CHECKBOX_HLT')
        col = box.column(align=True)
        col.prop(props, "apply_modifiers")
        col.prop(props, "use_triangles")
        col.prop(props, "use_armature")
        col.prop(props, "use_animation")
        col.prop(props, "use_shape_keys")
        col.prop(props, "bake_animation")
        col.prop(props, "add_leaf_bones")
        
        # Export Button
        layout.separator()
        row = layout.row()
        row.scale_y = 1.5
        row.operator("veilbreakers.export_to_unity", icon='EXPORT')
        
        # Info
        layout.separator()
        col = layout.column(align=True)
        col.label(text="Select objects to export", icon='INFO')
        col.label(text=f"Output: Assets/Resources/Art/3D_Models/{props.export_subfolder}")


# =============================================================================
# REGISTRATION
# =============================================================================

classes = (
    VeilBreakersDCCProperties,
    VEILBREAKERS_OT_export_to_unity,
    VEILBREAKERS_OT_apply_preset,
    VEILBREAKERS_OT_quick_export_character,
    VEILBREAKERS_OT_quick_export_prop,
    VEILBREAKERS_PT_dcc_bridge,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    
    bpy.types.Scene.veilbreakers_dcc_props = bpy.props.PointerProperty(type=VeilBreakersDCCProperties)

def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    
    del bpy.types.Scene.veilbreakers_dcc_props

if __name__ == "__main__":
    register()

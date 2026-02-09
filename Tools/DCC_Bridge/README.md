# VeilBreakers DCC Bridge - Unity ↔ Blender Integration

This DCC (Digital Content Creation) Bridge provides seamless workflow integration between Blender and Unity for the VeilBreakers3D project.

## 🚀 Quick Start

### 1. Install the Blender Addon

1. Open Unity
2. Go to **VeilBreakers > DCC Bridge**
3. Click **"Install/Update Addon"**
4. The addon will be automatically installed in Blender

### 2. Export from Blender

1. Open Blender
2. Select your model(s)
3. Open the **VeilBreakers** sidebar panel (press N if hidden)
4. Click **"Export Character"** or **"Export Prop"**
5. Files are automatically exported to the correct Unity folder

### 3. Import in Unity

Models are automatically imported with optimized settings based on their folder location:
- `Characters/` - Rigged characters with blend shapes
- `Props/` - Static props with high compression
- `Environment/` - Environment pieces with colliders
- `Animations/` - Animation-only files
- `VFX/` - VFX meshes (readable)

## 📑 Folder Structure

```
Assets/
├── Resources/
│   └── Art/
│       └── 3D_Models/
│           ├── Characters/     # Rigged character models
│           ├── Props/          # Static props
│           ├── Environment/    # Environment geometry
│           ├── Animations/     # Animation files
│           └── VFX/            # VFX meshes
Tools/
└── DCC_Bridge/
    ├── BlenderAddon/              # VeilBreakers Blender addon
    ├── Tripo3d_Blender_Bridge/    # Tripo3D AI addon
    ├── Templates/                 # Blender template files
    ├── install_tripo3d_bridge.bat # Tripo3D installer
    ├── TRIPO3D_README.md          # Tripo3D documentation
    └── README.md                  # This file
```

## ⚙️ Blender Addon Features

### Quick Export Buttons
- **Export Character** - Optimized for rigged characters with blend shapes
- **Export Prop** - Optimized for static props

### Export Settings
- **Format**: FBX (recommended) or glTF 2.0
- **Apply Modifiers**: Apply all modifiers before export
- **Triangulate**: Convert to triangles (recommended for game assets)
- **Include Armature**: Export rig/skeleton
- **Include Animation**: Export animations
- **Include Shape Keys**: Export blend shapes (ARKit compatible)

### Coordinate System
- **Forward**: -Z (Blender default)
- **Up**: Y (Unity compatible)

## 🎨 Unity Editor Features

### DCC Bridge Window
Access via: **VeilBreakers > DCC Bridge**

#### Features:
- **Blender Path**: Auto-detects or manual set Blender installation
- **Install/Update Addon**: One-click addon installation
- **Open Blender**: Launch Blender
- **Open with Selection**: Open selected FBX in Blender
- **Reimport All Models**: Batch reimport with optimized settings
- **Create Folder Structure**: Generate standard folders
- **Validate Import Settings**: Check for non-optimal settings

## 📄 Export Presets

### Character Preset
```
- Format: FBX
- Apply Modifiers: Yes
- Triangulate: Yes
- Armature: Yes
- Animation: Yes
- Shape Keys: Yes
- Destination: Assets/Resources/Art/3D_Models/Characters/
```

### Prop Preset
```
- Format: FBX
- Apply Modifiers: Yes
- Triangulate: Yes
- Armature: No
- Animation: No
- Shape Keys: No
- Destination: Assets/Resources/Art/3D_Models/Props/
```

### Environment Preset
```
- Format: FBX
- Apply Modifiers: Yes
- Triangulate: Yes
- Armature: No
- Animation: No
- Shape Keys: No
- Destination: Assets/Resources/Art/3D_Models/Environment/
```

## 🧪 Advanced Usage

### Custom Blender Scripts

You can extend the addon by adding custom scripts to the Blender addon folder:

```python
# Example: Custom export with specific settings
def export_custom():
    bpy.ops.veilbreakers.export_to_unity(
        export_subfolder="Custom",
        apply_modifiers=True,
        use_triangles=True
    )
```

### Batch Export from Blender

```python
import bpy

# Export all visible objects
for obj in bpy.context.visible_objects:
    obj.select_set(True)
    bpy.ops.veilbreakers.export_to_unity()
    obj.select_set(False)
```

### Import Post-Processing

The `BlenderFBXImportPostprocessor.cs` automatically:
1. Applies compression based on asset type
2. Configures animation settings
3. Sets up materials
4. Adds colliders for environment pieces
5. Marks VFX meshes as readable

## 🔧 Troubleshooting

### Blender Not Found
1. Verify Blender is installed at `C:\Program Files\Blender Foundation\`
2. In Unity DCC Bridge window, click **Browse...** to set the path manually

### Addon Not Installing
1. Check the Console for error messages
2. Ensure Blender is not running during installation
3. Try running Blender as Administrator

### Models Not Importing Correctly
1. Check the folder structure matches the presets
2. Use **Validate Import Settings** in DCC Bridge
3. Try **Reimport All Models** to reset settings

### Coordinate Issues
- Blender uses: Y-up, -Z-forward
- Unity uses: Y-up, Z-forward
- The addon automatically handles this conversion

## 📚 Workflow Best Practices

1. **Naming**: Use PascalCase for models (e.g., `HeroCharacter.fbx`)
2. **Organization**: Keep source .blend files in a separate folder
3. **Materials**: Use Blender's Principled BSDF for best Unity compatibility
4. **Textures**: Export textures separately to `Assets/Resources/Art/Textures/`
5. **Scale**: Work at 1 unit = 1 meter in Blender
6. **Rigging**: Use Rigify for characters, then export with "Add Leaf Bones" disabled

## 🤖 Tripo3D AI Integration

The DCC Bridge now includes **Tripo3D Blender Bridge** for AI-powered 3D model generation!

### What is Tripo3D?
Tripo3D is an AI service that generates 3D models from text prompts or images.

### Setup
1. Go to **VeilBreakers > Tripo3D Bridge**
2. Click **"Install Tripo Addon"**
3. Open Blender and look for the "Tripo" panel
4. Visit https://studio.tripo3d.ai/ to generate models
5. Click "Send to Blender" and models appear automatically!

See `TRIPO3D_README.md` for detailed instructions.

## 🔗 Integration with Other Tools

### BlenderMCP (AI-Assisted Rigging)
The DCC Bridge works alongside BlenderMCP for automated rigging:
1. Export base mesh from Blender using DCC Bridge
2. Use BlenderMCP for AI-assisted rigging
3. Re-export rigged character using DCC Bridge

### glTFast (glTF Support)
For glTF exports:
1. Set export format to glTF 2.0 in addon settings
2. Requires Unity glTFast package (already installed)

## 📋 Version History

### v1.0.0 (2026-02-07)
- Initial release
- Blender addon with export presets
- Unity DCC Bridge editor window
- Automatic import post-processing
- Support for FBX and glTF formats

## 📞 Support

For issues or feature requests, check:
- Unity Console for error messages
- Blender System Console (Window > Toggle System Console)
- Project documentation in `Docs/`

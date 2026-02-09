# Tripo3D Blender Bridge Integration

This folder contains the **Tripo3D Blender Bridge** - an AI-powered 3D model generation addon that connects Blender to Tripo Studio.

## 🌟 What is Tripo3D?

Tripo3D is an AI service that generates 3D models from:
- Text prompts (e.g., "a futuristic robot warrior")
- Images (convert 2D images to 3D models)

The Blender Bridge allows you to receive these AI-generated models directly in Blender.

## 🚀 Quick Setup

### Option 1: Unity Editor (Recommended)

1. Open Unity
2. Go to **VeilBreakers → Tripo3D Bridge**
3. Click **"Install Tripo Addon"**
4. Click **"Open Blender"**
5. Done!

### Option 2: Batch Script

```batch
Tools\DCC_Bridge\install_tripo3d_bridge.bat
```

### Option 3: Manual Install

1. Open Blender
2. Edit → Preferences → Add-ons → Install
3. Select the folder: `Tools/DCC_Bridge/Tripo3d_Blender_Bridge`
4. Enable the addon

## 📝 How to Use

### Prerequisites
- Blender 4.1 or higher
- Tripo3D account (get one at https://www.tripo3d.ai/)

### Workflow

1. **Open Blender**
   - Look for the **"Tripo"** panel in the 3D View sidebar (press `N`)
   - The addon will automatically start a WebSocket server on port 60600

2. **Open Tripo Studio**
   - Visit https://studio.tripo3d.ai/workspace/generate
   - Login to your account

3. **Generate a Model**
   - Enter a text prompt or upload an image
   - Wait for the AI to generate your 3D model

4. **Send to Blender**
   - In Tripo Studio, click **"Send to Blender"**
   - The model will appear in your Blender scene automatically!

## 🔧 Technical Details

### WebSocket Connection
- **Host**: 127.0.0.1 (localhost)
- **Port**: 60600
- **Protocol**: WebSocket

### File Transfer
Models are transferred as binary data and automatically imported into Blender with:
- Proper materials
- UV maps
- Normals
- Scale correction

## 📦 Folder Structure

```
Tools/DCC_Bridge/
├── Tripo3d_Blender_Bridge/          # Tripo3D addon files
│   ├── __init__.py                  # Addon entry point
│   ├── core/                        # Core functionality
│   ├── ui/                          # UI panels
│   ├── lib/                         # WebSocket library
│   └── ...
├── install_tripo3d_bridge.bat    # Windows installer
├── TRIPO3D_README.md             # This file
└── README.md                      # Main DCC Bridge docs
```

## 🔄 Integration with VeilBreakers DCC Bridge

The Tripo3D Bridge works seamlessly with the VeilBreakers DCC Bridge:

1. Generate model in Tripo Studio → Received in Blender
2. Edit/enhance in Blender
3. Export to Unity using VeilBreakers DCC Bridge panel
4. Model appears in Unity with optimized settings!

## 📋 Troubleshooting

### Port 60600 Already in Use
If you see "Port 60600 is already in use":
- Another instance of Blender with the addon is running
- Close other Blender instances and try again

### Connection Failed
- Ensure Blender is running with the addon enabled
- Check that the Tripo panel shows "Connected"
- Verify firewall isn't blocking port 60600

### Models Not Appearing
- Check the Blender System Console (Window → Toggle System Console)
- Verify you're logged into Tripo Studio
- Try regenerating the model

### Blender Version Issues
The addon requires **Blender 4.1 or higher**. If you have an older version:
- Download latest Blender from https://www.blender.org/

## 📚 Resources

- **Tripo3D Website**: https://www.tripo3d.ai/
- **Tripo Studio**: https://studio.tripo3d.ai/
- **Documentation**: https://www.tripo3d.ai/blog/tripo-dcc-bridge-for-blender

## 📄 Version Info

- **Addon Version**: 1.0.0
- **Requires**: Blender 4.1+
- **License**: See Tripo3D terms of service

# AAA Rigging, Animation & Facial Pipeline Design

> **Created:** 2026-01-17 | **Status:** Ready for Implementation

---

## Executive Summary

Automated pipeline for AAA-quality character rigging, body animation, and facial animation with 99% of work handled by Claude via MCP tools. Three paths for facial animation based on use case and available tools.

---

## Pipeline Overview

```
3D Model (from User)
    ↓
[PHASE 1: RIGGING]
BlenderMCP + Rigify Python Scripts
    ↓
Rigged FBX with ARKit-compatible blend shapes
    ↓
[PHASE 2: BODY ANIMATION]
Mixamo MCP / ActorCore (4500+ animations)
    ↓
Retargeted animations in Unity
    ↓
[PHASE 3: FACIAL ANIMATION]
Path A: Audio2Face → Maya → Unity (Cinematics)
Path B: Audio2Face SDK Unity Plugin (Future)
Path C: uLipSync (Runtime Dialogue)
    ↓
Fully animated character in Unity
```

---

## Phase 1: Rigging Automation

### Tool Stack
- **BlenderMCP** - Execute Python scripts in Blender
- **Rigify** - Blender's auto-rigging add-on
- **Custom Python Scripts** - Written by Claude

### Workflow

```
1. User provides: 3D model (FBX/OBJ) in T-pose or A-pose
2. Claude writes: Rigify automation script
3. BlenderMCP executes:
   - Import model
   - Generate metarig
   - Fit metarig to mesh
   - Generate final rig
   - Create ARKit blend shapes (52 expressions)
   - Export rigged FBX
4. Output: Unity-ready rigged character with blend shapes
```

### Command Line Automation
```bash
blender.exe model.blend --python rig_automation.py --background
```

### Rigify Python API Key Functions
```python
import bpy
from rigify.utils.bones import *
from rigify.utils.mechanism import *

# Generate rig from metarig
bpy.ops.pose.rigify_generate()

# Key modules:
# - rigify.utils.bones - Bone creation/manipulation
# - rigify.utils.mechanism - Constraints/drivers
# - rigify.utils.naming - Bone naming conventions
```

### Blend Shape Requirements (ARKit 52)
| Category | Count | Examples |
|----------|-------|----------|
| Eye | 14 | eyeBlinkLeft, eyeWideRight, eyeLookUp |
| Brow | 4 | browInnerUp, browDownLeft |
| Jaw | 4 | jawOpen, jawForward, jawLeft |
| Mouth | 22 | mouthSmileLeft, mouthPucker, mouthFunnel |
| Cheek | 4 | cheekPuff, cheekSquintLeft |
| Nose | 2 | noseSneerLeft, noseSneerRight |
| Tongue | 2 | tongueOut |

### My Automation Scope
- [x] Write Rigify Python scripts
- [x] Configure metarig fitting
- [x] Generate blend shape drivers
- [x] Set up export settings
- [ ] User provides 3D model

---

## Phase 2: Body Animation

### Tool Stack
- **Mixamo MCP** - Unity integration for Mixamo library
- **ActorCore** - 4500+ animations via AccuRIG 2
- **Unity Animation Rigging** - Procedural adjustments

### Animation Library Access

| Source | Count | Access Method |
|--------|-------|---------------|
| Mixamo | 2000+ | Web upload or Mixamo MCP |
| ActorCore | 4500+ | AccuRIG 2 in-app AI search |

### Standard Animation Set (Per Character)
| Category | Animations |
|----------|------------|
| Locomotion | Idle, Walk, Run, Sprint, Strafe |
| Combat | Attack1, Attack2, Attack3, Skill, Ultimate |
| Defense | Block, Dodge, Hit_React, Stagger |
| Death | Death_Forward, Death_Backward |
| Emotes | Victory, Taunt, Interact |

### Animation Retargeting in Unity
```csharp
// Unity Humanoid Avatar handles retargeting automatically
// Just ensure source and target use Humanoid rig type

// For non-humanoid (monsters):
// Use Animation Rigging package for procedural adjustments
```

### My Automation Scope
- [x] Select appropriate animations from library
- [x] Configure retargeting in Unity
- [x] Set up Animation Controller states
- [x] Write AnimatorController.cs logic
- [ ] User approves animation selection

---

## Phase 3: Facial Animation

### Three Paths

---

### Path A: Audio2Face → Maya → Unity (AAA Cinematics)

**Best For:** Pre-rendered cutscenes, trailers, high-quality dialogue

**Requirements:**
- NVIDIA GPU (CUDA 12.8+, 4GB+ VRAM)
- Maya 2020-2025 (Windows)
- Audio2Face SDK + Maya Plugin

**Pipeline:**
```
Dialogue Audio (.wav)
    ↓
Audio2Face SDK (batch process)
    ↓
52 ARKit blend shape weights per frame
    ↓
Maya imports via A2F Plugin
    ↓
Bake to animation curves
    ↓
Export FBX with facial animation
    ↓
Unity imports as Animation Clip
```

**Audio2Face SDK Features:**
- Faster than 60 FPS processing
- Multi-track support (multiple characters)
- GPU accelerated (CUDA/TensorRT)
- Batch and interactive modes
- Audio2Emotion (emotional state detection)

**GitHub:** https://github.com/NVIDIA/Audio2Face-3D-SDK

**My Automation Scope:**
- [x] Write batch processing scripts
- [x] Configure Audio2Face parameters
- [x] Set up Maya export automation
- [x] Configure Unity import pipeline
- [ ] User has Maya + NVIDIA GPU

---

### Path B: Custom Unity Plugin (Future Investment)

**Best For:** Real-time AAA facial animation in Unity

**Requirements:**
- NVIDIA GPU (CUDA 12.8+)
- Visual Studio 2019+ for C++ development
- Unity Native Plugin expertise

**Architecture:**
```
┌─────────────────────────────────────┐
│         Unity C# Layer             │
│   FacialAnimationController.cs     │
└──────────────┬──────────────────────┘
               │ P/Invoke
┌──────────────▼──────────────────────┐
│      C++ Unity Native Plugin        │
│   Audio2FaceUnityPlugin.dll         │
└──────────────┬──────────────────────┘
               │ Wraps
┌──────────────▼──────────────────────┐
│      Audio2Face-3D-SDK (C++)        │
│   CUDA + TensorRT inference         │
└─────────────────────────────────────┘
```

**Implementation Steps:**
1. Clone Audio2Face-3D-SDK
2. Create Unity native plugin project
3. Wrap SDK initialization and inference
4. Export blend shape weights to C#
5. Apply weights to SkinnedMeshRenderer

**My Automation Scope:**
- [x] Write C++ wrapper code
- [x] Write C# integration layer
- [x] Create build scripts
- [ ] User compiles plugin (guided)

---

### Path C: uLipSync (Runtime Dialogue)

**Best For:** Gameplay conversations, NPC dialogue, any-hardware support

**Requirements:**
- Unity 2019.4+
- Burst Compiler package
- Character with lip blend shapes (at minimum)

**GitHub:** https://github.com/hecomi/uLipSync

**Features:**
- Real-time MFCC audio analysis
- Customizable phoneme profiles
- Pre-bake option for performance
- Timeline integration
- VRM support
- WebGL compatible
- **FREE (MIT License)**

**Pipeline:**
```
Audio Source (clip or microphone)
    ↓
uLipSync Component (MFCC analysis)
    ↓
Phoneme detection (a, i, u, e, o, etc.)
    ↓
Blend shape weight mapping
    ↓
SkinnedMeshRenderer updates
```

**My Automation Scope:**
- [x] Configure uLipSync profiles
- [x] Create phoneme calibration data
- [x] Pre-bake audio files for performance
- [x] Write FacialController.cs wrapper
- [x] Set up Timeline integration
- [x] Create emotion blend presets
- [ ] Nothing required from user

---

## Expression System (All Paths)

### Emotion Presets (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "EmotionPreset", menuName = "VeilBreakers/Emotion Preset")]
public class EmotionPreset : ScriptableObject
{
    public string emotionName;
    public float[] blendShapeWeights = new float[52]; // ARKit 52
    public float transitionDuration = 0.3f;
}
```

### Standard Emotion Library
| Emotion | Key Blend Shapes |
|---------|------------------|
| Neutral | All zero |
| Happy | mouthSmile, cheekSquint, eyeSquint |
| Sad | mouthFrown, browInnerUp, eyeLookDown |
| Angry | browDown, noseSneer, jawClench, eyeWide |
| Surprised | eyeWide, browInnerUp, jawOpen, mouthOpen |
| Fearful | eyeWide, browInnerUp, mouthStretch |
| Disgusted | noseSneer, browDown, mouthUpperUp |

### FacialAnimationController.cs
```csharp
public class FacialAnimationController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _faceMesh;
    [SerializeField] private uLipSync _lipSync;
    [SerializeField] private EmotionPreset[] _emotionPresets;

    private EmotionPreset _currentEmotion;
    private float[] _targetWeights = new float[52];

    public void SetEmotion(string emotionName, float blend = 1f)
    {
        // Find preset, lerp blend shapes
    }

    public void PlayDialogue(AudioClip clip)
    {
        // Trigger uLipSync, combine with emotion
    }
}
```

---

## Recommended Implementation Order

| Priority | Task | Path |
|----------|------|------|
| 1 | Set up uLipSync for runtime dialogue | C |
| 2 | Create emotion preset ScriptableObjects | All |
| 3 | Write FacialAnimationController.cs | All |
| 4 | Configure BlenderMCP rigging scripts | Phase 1 |
| 5 | Set up Mixamo MCP for animations | Phase 2 |
| 6 | (If Maya) Configure Audio2Face pipeline | A |
| 7 | (Future) Build custom Unity plugin | B |

---

## Tool Installation Checklist

### Required (Phase 1 & 2)
- [ ] BlenderMCP configured in Claude Code
- [ ] Blender 3.0+ installed with Rigify enabled
- [ ] Mixamo MCP added to project

### Required (Path C - Runtime)
- [ ] uLipSync package installed in Unity
  ```
  https://github.com/hecomi/uLipSync.git
  ```
- [ ] Burst Compiler package installed

### Optional (Path A - Maya)
- [ ] Maya 2020-2025 installed
- [ ] Audio2Face SDK downloaded
- [ ] Maya plugin installed
- [ ] NVIDIA GPU with CUDA 12.8+

### Optional (Path B - Custom Plugin)
- [ ] Visual Studio 2019+ with C++ workload
- [ ] Audio2Face-3D-SDK cloned
- [ ] CUDA Toolkit 12.8 installed

---

## Quality Benchmarks

### AAA Reference Standards
| Aspect | Target | Achieved By |
|--------|--------|-------------|
| Lip Sync Accuracy | 90%+ phoneme match | Audio2Face or tuned uLipSync |
| Expression Blending | Smooth 60fps transitions | Lerped blend shapes |
| Rig Deformation | No mesh tearing | Proper weight painting |
| Animation Retargeting | No foot sliding | Humanoid avatar + root motion |

### Performance Targets
| Platform | Facial Animation Budget |
|----------|------------------------|
| PC (High) | 0.5ms per character |
| PC (Low) | 0.2ms per character |
| Mobile | 0.1ms per character (pre-baked only) |

---

## Sources

- [Audio2Face-3D-SDK](https://github.com/NVIDIA/Audio2Face-3D-SDK)
- [uLipSync](https://github.com/hecomi/uLipSync)
- [Rigify Documentation](https://developer.blender.org/docs/features/animation/rigify/)
- [ARKit Blend Shapes Guide](https://arkit-face-blendshapes.com/)
- [AccuRIG 2](https://actorcore.reallusion.com/auto-rig)
- [Mixamo MCP](https://www.pulsemcp.com/servers/hadoyun-mixamo)
- [SALSA LipSync](https://crazyminnowstudio.com/unity-3d/lip-sync-salsa/)

---

*Document generated from brainstorming session 2026-01-17*

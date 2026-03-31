---
name: unity-editor-control
description: Use when you need to interact with Unity Editor - run builds, get debug output, manipulate scenes, or check for compile errors.
---

# Unity Editor Control

## Two MCP Servers — Use the Right One

### vb-unity (Script Generation — 22 tools, 258 actions)
Generates C# editor scripts. 3-step workflow: generate -> recompile -> invoke menu item.

**Use for:** VFX setup, audio pipelines, scene configuration, terrain, UI generation, gameplay systems, bulk content creation, procedural generation.

**Key tools:** `unity_vfx`, `unity_audio`, `unity_ui`, `unity_scene`, `unity_gameplay`, `unity_game`, `unity_content`, `unity_world`, `unity_editor` (recompile, screenshot)

### unity-mcp (Direct Editor Control — 60+ tools)
Real-time editor manipulation. Immediate results, no script generation needed.

**Use for:** Play mode control, console log reading, scene queries, GameObject inspection, live debugging, test running, batch property changes.

**Key tools:**
| Tool | Purpose |
|------|---------|
| `editor-application-set-state` | Start/stop/pause play mode |
| `console-get-logs` | Read runtime errors and logs |
| `scene-get-data` / `scene-list-opened` | Query scene state |
| `gameobject-find` / `gameobject-modify` | Find and change GameObjects |
| `editor-selection-get/set` | Control editor selection |
| `screenshot-game-view` / `screenshot-scene-view` | Capture views |
| `script-execute` | Run C# code directly (Roslyn) |
| `reflection-method-call` | Call methods on live objects |
| `batch-execute` | Multiple ops in one round-trip |
| `run-tests` | Execute and read test results |

## When to Use Which

| Task | Use | Why |
|------|-----|-----|
| Create brand VFX for all 10 types | **vb-unity** | Bulk generation |
| Check if VFX plays correctly | **unity-mcp** | Instant play mode + console |
| Set up terrain with lighting | **vb-unity** | Full pipeline |
| Query current scene during debug | **unity-mcp** | Immediate read |
| Modify 50 GameObject properties | **unity-mcp** | batch_execute |
| Generate procedural dungeon | **vb-unity** | Worldbuilding tools |
| Run tests and read results | **unity-mcp** | Console access |
| Check compile errors | **unity-mcp** | console-get-logs |
| Take screenshot for visual QA | **Either** | Both support screenshots |
| Recompile after vb-unity script | **unity-mcp** | Faster than vb-unity recompile |

## Workflow Integration

```
1. Use vb-unity to GENERATE (VFX, audio, UI, scenes)
2. Use unity-mcp to VERIFY (play mode, console, screenshots)
3. Fix issues found, repeat
```

## Requirements
- Unity Editor must be running with VeilBreakers3D project loaded
- unity-mcp bridge must be installed (IvanMurzak/Unity-MCP package)
- vb-unity connects via generated scripts; unity-mcp connects via WebSocket (port 8080)

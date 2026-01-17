# MCP Arsenal Design for VeilBreakers Unity Development

> **Created:** 2026-01-17 | **Status:** Ready for Implementation

---

## Executive Summary

Streamlined MCP setup that eliminates idle tools and maps each server to concrete VeilBreakers development tasks. Removes 3 redundant/broken MCPs, keeps 5 essential ones, adds 1 game-changer.

---

## DELETE (3 MCPs)

| MCP | Reason | Action |
|-----|--------|--------|
| **github** | Broken - requires Copilot subscription | `claude plugins uninstall github` |
| **sentry** | Needs auth, not critical for solo dev | `claude plugins uninstall sentry` |
| **memory** | Redundant - VEILBREAKERS.md is single source of truth per CLAUDE.md | Remove from `.mcp.json` |

---

## KEEP (5 MCPs) - With Usage Plans

### 1. Context7 (Unity Docs)

**Library ID:** `/websites/unity3d_manual` (23,395 snippets)

| When | How | Example |
|------|-----|---------|
| Writing unfamiliar Unity APIs | Query Context7 | "MonoBehaviour lifecycle order" |
| Input System setup | Query docs | "Unity Input System action maps" |
| Animation triggers | Query docs | "Animator.SetTrigger vs SetBool" |
| Physics/Collision | Query docs | "OnTriggerEnter vs OnCollisionEnter" |

**Triggers:** Before writing ANY Unity C# code I haven't used recently.

---

### 2. Serena (C# Semantic Tools)

| When | Tool | Example |
|------|------|---------|
| Find class/method | `find_symbol` | Find BattleManager.StartCombat |
| Impact analysis | `find_referencing_symbols` | What calls CalculateDamage? |
| Safe refactoring | `rename_symbol` | Rename Brand → BrandType globally |
| Rewrite functions | `replace_symbol_body` | Update entire method safely |
| Code overview | `get_symbols_overview` | What's in this file? |

**Triggers:** Any code modification in `Assets/Scripts/`

---

### 3. Greptile (Codebase Search)

| When | How | Example |
|------|-----|---------|
| Find patterns | Regex search | All `// TODO` comments |
| Find usages | Text search | Where is "SAVAGE" brand used? |
| Find files | Pattern match | All `*Manager.cs` files |

**Triggers:** Open-ended "where is X" questions, codebase-wide searches

---

### 4. Sequential Thinking (Problem Decomposition)

| When | How | Example |
|------|-----|---------|
| System design | Multi-step breakdown | Design combat loop architecture |
| Complex decisions | Pros/cons analysis | Which brand matrix implementation? |
| Bug investigation | Root cause analysis | Why is damage calculating wrong? |

**Triggers:** "Design...", "Plan...", "How should we architect...", "Why is X broken?"

---

### 5. Episodic Memory (Conversation History)

| When | How | Example |
|------|-----|---------|
| Session start | Search history | What did we decide about brands? |
| Stuck on problem | Find past solution | How did we fix the corruption bug? |
| Recall context | Search conversations | What was the damage formula? |

**Triggers:** Every session start, when revisiting past decisions

---

### 6. Image Process (Asset Pipeline)

| When | How | Example |
|------|-----|---------|
| UI sprite prep | Resize | Scale portrait to 256x256 |
| Format conversion | Convert | PNG to WebP for smaller builds |
| Texture cropping | Crop | Extract icon from sprite sheet |

**Triggers:** 2D asset pipeline work (UI, textures, sprites)

---

## ADD (1 MCP) - Game Changer

### mcp-unity (CoderGamester)

**Source:** https://github.com/CoderGamester/mcp-unity

| Capability | What It Enables |
|------------|-----------------|
| `create_gameobject` | Create GameObjects via prompt |
| `add_component` | Attach scripts to objects |
| `update_component` | Modify serialized fields |
| `create_scene` | Create test scenes |
| `save_scene` | Save changes |
| `run_tests` | Execute EditMode/PlayMode tests |
| `get_console_logs` | Read Unity console |
| `create_prefab` | Create prefabs from objects |
| `create_material` | Create and assign materials |

**Workflow Transformation:**

```
BEFORE (Manual):
1. I write BattleManager.cs
2. You open Unity
3. You create empty GameObject
4. You attach BattleManager script
5. You configure Inspector fields
6. You save scene

AFTER (Automated):
1. I write BattleManager.cs AND create GameObject AND attach script AND configure fields AND save scene
2. You verify it works
```

**Installation:**
1. Unity Editor → Window → Package Manager
2. Click "+" → Add package from git URL
3. Enter: `https://github.com/CoderGamester/mcp-unity.git`
4. Wait for import
5. MCP server auto-starts when Unity opens

**Claude Code Config** (after Unity package installed):
```json
{
  "mcpServers": {
    "mcp-unity": {
      "type": "stdio",
      "command": "node",
      "args": ["path/to/mcp-unity/server.js"]
    }
  }
}
```

---

## Usage Map by Implementation Priority

### Priority 1: Core Combat Loop
| Task | MCPs Used |
|------|-----------|
| Research Unity physics | Context7 |
| Write BattleManager.cs | Serena |
| Create battle scene | mcp-unity |
| Add combat GameObjects | mcp-unity |
| Run combat tests | mcp-unity |

### Priority 2: Brand Effectiveness System
| Task | MCPs Used |
|------|-----------|
| Design 10-brand matrix | Sequential Thinking |
| Write BrandSystem.cs | Serena |
| Find damage calc touchpoints | Greptile |
| Update all damage references | Serena (rename_symbol) |

### Priority 3: Universal Actions (Defend, Guard)
| Task | MCPs Used |
|------|-----------|
| Research Input System | Context7 |
| Write ActionController.cs | Serena |
| Create action prefabs | mcp-unity |

### Priority 4: 5-Slot Ability Structure
| Task | MCPs Used |
|------|-----------|
| Design slot architecture | Sequential Thinking |
| Write AbilitySlot.cs | Serena |
| Create ability UI prefabs | mcp-unity |

### Priority 5: Party Swapping
| Task | MCPs Used |
|------|-----------|
| Research animation blending | Context7 |
| Write PartyManager.cs | Serena |
| Create swap UI | mcp-unity |

---

## Final Arsenal Summary

| MCP | Purpose | Idle? |
|-----|---------|-------|
| Context7 | Unity API docs | No - used every coding session |
| Serena | C# code intelligence | No - used every code change |
| Greptile | Codebase search | No - used for exploration |
| Sequential Thinking | Complex reasoning | No - used for design decisions |
| Episodic Memory | Session continuity | No - used every session start |
| Image Process | Asset pipeline | Occasional - 2D UI work |
| **mcp-unity** | Unity Editor control | No - used every Unity task |

**Deleted:** github, sentry, memory (3 removed)
**Added:** mcp-unity (1 added)
**Net:** 7 focused MCPs, zero idle tools

---

## Implementation Checklist

- [ ] Run `claude plugins uninstall github`
- [ ] Run `claude plugins uninstall sentry`
- [ ] Update `.mcp.json` (remove memory)
- [ ] Install mcp-unity in Unity Package Manager
- [ ] Configure mcp-unity in Claude Code
- [ ] Verify all MCPs connected with `claude mcp list`
- [ ] Update VEILBREAKERS.md MCP section

---

*This design ensures every MCP earns its place through concrete, regular usage.*

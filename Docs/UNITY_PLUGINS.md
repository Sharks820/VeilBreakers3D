# VeilBreakers3D - Unity Plugin Recommendations

## Overview
This document outlines recommended Unity Asset Store plugins and packages for implementing the real-time tactical combat system (Dragon Age: Inquisition style).

---

## TIER 1: ESSENTIAL PLUGINS

### AI & Behavior Trees

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Behavior Designer** | $80 | Industry-standard behavior trees, visual editor, extensive documentation | [Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/behavior-designer-behavior-trees-for-everyone-15277) |
| **AI Tree** | $40 | Modern alternative, Unity 6 ready, great value | [Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/ai-tree-behavior-trees-for-unity-229578) |
| **Generic Behavior Tree** | FREE | Open source option on GitHub | [GitHub](https://github.com/nicloay/generic-behavior-tree) |

**Recommendation:** Behavior Designer for production quality, Generic Behavior Tree for budget option.

### Navigation & Pathfinding

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Unity AI Navigation 2.0** | FREE | Official Unity package, NavMesh baking, agent navigation | [Unity Package](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/) |
| **A* Pathfinding Project Pro** | $100 | Advanced pathfinding, local avoidance, grid graphs | [Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744) |

**Recommendation:** Start with Unity AI Navigation 2.0 (free), upgrade to A* if needed.

### Party Formation & Tactics

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Unit Formation** | $25 | Military-style formations, customizable patterns | [Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/unit-formation-210479) |
| **Unity-Formation-Movement 2.0** | FREE | Open source formation system | [GitHub](https://github.com/Goodgulf281/Unity-Formation-Movement2.0) |

**Recommendation:** Unity-Formation-Movement 2.0 for flexibility, Unit Formation for quick setup.

---

## TIER 2: RPG FRAMEWORKS

### Complete RPG Systems

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **ORK Framework 3** | $105 | Complete turn-based + real-time RPG, inventory, quests, dialogue | [Asset Store](https://assetstore.unity.com/packages/tools/game-toolkits/rpg-editor-ork-framework-3-19-x-2022-209685) |
| **AnyRPG** | FREE | Open source RPG framework, action combat, AI | [Website](https://www.anyrpg.org/) |
| **Game Creator 2** | $45 | Visual scripting, modular systems | [Asset Store](https://assetstore.unity.com/packages/tools/game-toolkits/game-creator-2-203069) |

**Recommendation:** AnyRPG (free, open source) for maximum flexibility, ORK Framework 3 for rapid prototyping.

---

## TIER 3: COMBAT & VFX

### Combat Systems

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Invector Third Person Controller** | $55 | Character controller with combat | [Asset Store](https://assetstore.unity.com/packages/tools/game-toolkits/third-person-controller-melee-combat-template-44227) |
| **UFPS 2** | $85 | Advanced character controller | [Asset Store](https://assetstore.unity.com/packages/templates/systems/ufps-2-ultimate-first-person-shooter-205548) |

### Visual Effects

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Magic Arsenal** | $45 | 500+ spell effects | [Asset Store](https://assetstore.unity.com/packages/vfx/particles/spells/magic-arsenal-32176) |
| **Epic Toon FX** | $40 | Stylized combat effects | [Asset Store](https://assetstore.unity.com/packages/vfx/particles/spells/epic-toon-fx-57772) |

---

## TIER 4: UI & DIALOGUE

### UI Systems

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **UI Toolkit** | FREE | Built-in Unity UI system | Built-in |
| **Odin Inspector** | $55 | Advanced inspector, editor tools | [Asset Store](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041) |

### Dialogue Systems

| Plugin | Price | Purpose | Link |
|--------|-------|---------|------|
| **Dialogue System for Unity** | $85 | Industry-standard dialogue | [Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672) |
| **Yarn Spinner** | FREE | Open source narrative tool | [Website](https://yarnspinner.dev/) |

**Recommendation:** Yarn Spinner (free) for VERA dialogue system.

---

## CUSTOM SYSTEMS NEEDED

### Monster Capture System
**No dedicated plugin found.** Will need custom implementation:
- WILL stat tracking (separate from HP)
- Capture phases: Mark → Subdue → Bind → Capture
- Capture success calculations
- Mid-battle capture mechanics

### Tactical Pause System
**Custom implementation required:**
- Radial menu for party commands
- Time scale control (0 = paused)
- AI state freezing
- Command queuing

### Corruption System
**Already implemented in C#** - Port from existing code.

---

## RECOMMENDED STACK (Budget)

| Category | Choice | Price |
|----------|--------|-------|
| Behavior Trees | Generic Behavior Tree | FREE |
| Navigation | Unity AI Navigation 2.0 | FREE |
| Formation | Unity-Formation-Movement 2.0 | FREE |
| RPG Framework | AnyRPG | FREE |
| Dialogue | Yarn Spinner | FREE |
| **TOTAL** | | **FREE** |

## RECOMMENDED STACK (Production)

| Category | Choice | Price |
|----------|--------|-------|
| Behavior Trees | Behavior Designer | $80 |
| Navigation | A* Pathfinding Pro | $100 |
| Formation | Unit Formation | $25 |
| RPG Framework | ORK Framework 3 | $105 |
| Dialogue | Dialogue System for Unity | $85 |
| VFX | Magic Arsenal | $45 |
| Editor | Odin Inspector | $55 |
| **TOTAL** | | **$495** |

---

## UNITY PACKAGES (Built-in/Free)

Always install these from Package Manager:
- **AI Navigation** - NavMesh and agents
- **Cinemachine** - Camera system
- **Input System** - Modern input handling
- **TextMeshPro** - UI text
- **Addressables** - Asset management
- **Universal Render Pipeline (URP)** - Modern rendering

---

## NOTES

1. **Start with free options** - Prove the concept before investing
2. **AnyRPG is comprehensive** - May reduce need for other plugins
3. **Custom capture system is key** - This is our unique feature
4. **3D models from Meshy** - User will create these separately
5. **Existing C# scripts** - CorruptionSystem, PathSystem already ported

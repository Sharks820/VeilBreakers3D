# VeilBreakers - Unity 6 Migration Plan

> **Status:** Ready to Begin | **Created:** 2026-01-25
>
> **From:** Unity 2022.3.62f3 (LTS) → **To:** Unity 6.x LTS

---

## Why Upgrade to Unity 6?

| Feature | Unity 2022.3 | Unity 6 | VeilBreakers Impact |
|---------|--------------|---------|---------------------|
| GPU Resident Drawer | ❌ | ✅ | 2x draw call performance for battles |
| GPU Occlusion Culling | ❌ | ✅ | Better overworld performance |
| UI Toolkit Shaders | ❌ | ✅ | Animated borders, glow effects in USS |
| Sentis AI | Preview | Production | Local AI for VERA (optional) |
| Build Profiles | ❌ | ✅ | Easy PC/Console/Mobile configs |
| Input System | 1.x | Enhanced | Better rebinding, device detection |
| Addressables | Standard | Improved | Faster async loading |

---

## Pre-Migration Checklist

### Before Starting

- [ ] **Backup entire project** to separate location
- [ ] **Create git branch:** `feature/unity6-migration`
- [ ] **Document current Unity settings** (screenshots of Project Settings)
- [ ] **Export package list** from Package Manager
- [ ] **Note all custom editor scripts** that may need updating

### Preparation Steps

```bash
# 1. Create backup branch
git checkout -b backup/pre-unity6
git push origin backup/pre-unity6

# 2. Create migration branch
git checkout master
git checkout -b feature/unity6-migration
```

---

## Phase 1: Project Upgrade (Day 1)

### 1.1 Download Unity 6

1. Open Unity Hub
2. Go to Installs → Install Editor
3. Select Unity 6 LTS (6.0.x or latest stable)
4. Include these modules:
   - Windows Build Support
   - Documentation
   - Visual Studio Integration

### 1.2 Open Project in Unity 6

1. Open Unity Hub
2. Select VeilBreakers3DCurrent
3. Click dropdown next to version
4. Select Unity 6.x
5. **Choose "Make a backup and upgrade"**

### 1.3 Initial Fixes

**Expected Issues:**

| Issue | Solution |
|-------|----------|
| Obsolete API warnings | Review and update deprecated calls |
| Package compatibility | Update packages to Unity 6 versions |
| Shader errors | Update to URP 17+ shaders |
| Editor script errors | Update EditorGUI calls |

---

## Phase 2: Core Systems Update (Day 1-2)

### 2.1 Rendering Pipeline

**Current:** Universal Render Pipeline (URP)
**Target:** URP 17+ with GPU Resident Drawer

```
Window → Package Manager → Universal RP → Update
```

**Enable GPU Resident Drawer:**
1. Edit → Project Settings → Graphics
2. Enable "GPU Resident Drawer"
3. Enable "GPU Occlusion Culling"

### 2.2 Input System

**Current:** Legacy Input (mixed)
**Target:** New Input System 1.8+

**Steps:**
1. Package Manager → Input System → Update
2. Edit → Project Settings → Player → Active Input Handling → "Both"
3. Create InputActions asset for VeilBreakers
4. Define action maps:
   - **Gameplay**: Movement, Attack, Skill1-6, Menu
   - **UI**: Navigate, Submit, Cancel, Tab
   - **Combat**: Target, Swap, Capture
5. Update all input reading code to use InputActions

### 2.3 Addressables

**Steps:**
1. Package Manager → Addressables → Install/Update
2. Create Addressables Groups:
   - **Core** (always loaded)
   - **UI** (menus, HUD)
   - **Audio** (music, SFX banks)
   - **Monsters** (per-monster assets)
   - **Zones** (per-zone assets)
3. Mark assets as Addressable
4. Update loading code to use async Addressables

---

## Phase 3: UI Toolkit Enhancements (Day 2-3)

### 3.1 Custom Shaders for UI

Unity 6 allows custom materials/shaders on UI Toolkit elements!

**New USS Features:**
```css
/* Animated glow border - NOW POSSIBLE IN UNITY 6 */
.glow-border {
    --unity-background-material: url("Materials/GlowBorder.mat");
    transition: border-color 0.3s ease;
}

/* Animated corruption effect */
.corrupted-panel {
    --unity-background-material: url("Materials/CorruptionPulse.mat");
}
```

**Create Materials:**
1. `Assets/Materials/UI/GlowBorder.mat` - Animated glow shader
2. `Assets/Materials/UI/CorruptionPulse.mat` - Corruption visual
3. `Assets/Materials/UI/VeilShimmer.mat` - Veil effect

### 3.2 UI Performance

- Enable UI Toolkit batching
- Use virtual scrolling for monster collection
- Profile UI with Frame Debugger

---

## Phase 4: AI & VERA (Day 3-4, Optional)

### 4.1 Unity Sentis Integration

**What Sentis Enables:**
- Local AI inference (no API calls)
- VERA can "think" in real-time
- Works offline
- Faster response times

**Implementation:**
1. Package Manager → Sentis → Install
2. Export/train small language model for VERA
3. Create `VERASentisController.cs`
4. Integrate with existing `VERASystem.cs`

**Model Options:**
- Fine-tuned Phi-2 (small, fast)
- Custom trained on VERA dialogue
- Hybrid: Local for common, API for complex

---

## Phase 5: Build & Deploy (Day 4-5)

### 5.1 Build Profiles

Unity 6 feature - create profiles for each target:

| Profile | Settings | Use |
|---------|----------|-----|
| Development | Debug, no compression | Testing |
| QA | Debug symbols, compressed | QA builds |
| Release-PC | Full optimization | Steam release |
| Release-Console | Platform-specific | Console builds |

### 5.2 Shader Warmup

Unity 6 improves shader warmup:
```csharp
// In loading screen
ShaderWarmup.WarmupAllShaders();
```

---

## Migration Commands Checklist

```bash
# Phase 1: Backup
git checkout -b backup/pre-unity6
git push origin backup/pre-unity6

# Phase 1: Start migration
git checkout master
git checkout -b feature/unity6-migration

# After each major change
git add -A
git commit -m "Unity 6: [description]"

# When complete
git checkout master
git merge feature/unity6-migration
git push origin master

# Cleanup
git branch -d feature/unity6-migration
```

---

## Testing Checklist

### Core Functionality

- [ ] Main Menu loads and animates
- [ ] Character Select works with hero colors
- [ ] Settings save/load correctly
- [ ] Game can start new game
- [ ] Combat HUD displays correctly

### Combat System

- [ ] Battle starts correctly
- [ ] Damage calculation works
- [ ] AI gambits function
- [ ] Status effects apply/remove
- [ ] Capture system works

### Audio

- [ ] AudioMixer volume controls work
- [ ] Music plays and crossfades
- [ ] SFX play correctly
- [ ] VERA voice works

### VERA System

- [ ] VERA responds to events
- [ ] Personality changes with veil integrity
- [ ] Glitch effects appear
- [ ] Dialogue queues correctly

### Save/Load

- [ ] New saves create correctly
- [ ] Saves load correctly
- [ ] Settings persist
- [ ] Auto-save triggers

### Performance

- [ ] Frame rate stable (60+ FPS)
- [ ] No shader stutter
- [ ] Memory usage reasonable
- [ ] Load times acceptable

---

## Rollback Plan

If critical issues arise:

```bash
# Revert to backup
git checkout backup/pre-unity6
git checkout -b hotfix/unity6-rollback
git push origin hotfix/unity6-rollback

# In Unity Hub
# Open project with Unity 2022.3.62f3
```

---

## Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Upgrade | 4-6 hours | None |
| Phase 2: Core | 6-8 hours | Phase 1 |
| Phase 3: UI | 4-6 hours | Phase 2 |
| Phase 4: VERA AI | 8-12 hours | Optional |
| Phase 5: Build | 4-6 hours | Phases 1-3 |

**Total: 2-3 days** (excluding optional VERA AI)

---

## Resources

- [Unity 6 Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/index.html)
- [Unity 6 Upgrade Guide](https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuide6.html)
- [URP 17 Features](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
- [Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@latest)
- [Input System 1.8](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/index.html)

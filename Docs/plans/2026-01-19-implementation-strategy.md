# VeilBreakers Implementation Strategy

> **Status:** PENDING APPROVAL | **Version:** 1.0 | **Date:** 2026-01-19

---

## Executive Summary

This document defines the **coordinated implementation strategy** for VeilBreakers 3D. With 11 design documents complete, we now have a clear blueprint. This strategy ensures quality, prevents chaos, and coordinates all agents and tools effectively.

**Core Philosophy:** One feature at a time. Complete before proceeding. Quality over speed.

---

## The Problem We're Solving

Without a strategy, we risk:
- Agents conflicting on the same files
- Half-finished features accumulating
- Lost context between sessions
- Untested code merging to main
- No clear progress tracking

This strategy eliminates all of these risks.

---

## Implementation Order (Dependency-Based)

### Layer 0: Foundation (COMPLETE)
Already implemented:
- Enums.cs, Constants.cs
- EventBus.cs, GameManager.cs, GameDatabase.cs
- BrandSystem.cs, SynergySystem.cs, CorruptionSystem.cs, PathSystem.cs
- BattleManager.cs, DamageCalculator.cs, Combatant.cs
- Data classes (AbilityData, HeroData, MonsterData, SkillData, ItemData)

### Layer 1: Data Classes (No Dependencies)
```
SaveData.cs          → Only needs Enums
ShrineData.cs        → ScriptableObject (Unity only)
StatusEffectData.cs  → ScriptableObject (Enums)
GambitData.cs        → ScriptableObject (Enums)
```

### Layer 2: Utilities (Depends on Layer 1)
```
SaveFileHandler.cs   → Pure utility (compression, encryption, checksum)
MigrationRunner.cs   → Needs SaveData
ObjectPool.cs        → Generic pooling (pure utility)
```

### Layer 3: Managers (Depends on Layers 1-2)
```
SaveManager.cs       → SaveData, SaveFileHandler, MigrationRunner
ShrineManager.cs     → ShrineData, EventBus
AutoSaveManager.cs   → SaveManager, EventBus
StatusEffectManager.cs → StatusEffectData, existing combat
AudioManager.cs      → Audio design specs
```

### Layer 4: Systems (Depends on Layer 3)
```
CaptureSystem.cs     → SaveManager, EventBus, combat
QuickCommandSystem.cs → Combat, UI foundation
GambitController.cs  → GambitData, StatusEffectManager
```

### Layer 5: UI (Depends on Layers 3-4)
```
All UI components    → Backend systems must exist first
```

---

## Implementation Phases

### Phase 1: Foundation Completion
**Goal:** Ensure existing code is solid, add missing core pieces

| Task | Description | Est. Time |
|------|-------------|-----------|
| EventBus expansion | Add save/load/shrine events | 30 min |
| ObjectPool utility | Generic pooling system | 1 hour |
| Compile verification | Ensure all code compiles | 30 min |

**Deliverable:** Solid foundation to build on

---

### Phase 2: Save/Load System (CRITICAL PATH)
**Goal:** Player can save and load progress

| Task | Layer | Dependencies |
|------|-------|--------------|
| SaveData.cs | 1 | Enums only |
| SavedMonster.cs | 1 | Enums only |
| SavedItem.cs | 1 | None |
| SaveFileHandler.cs | 2 | None (pure utility) |
| MigrationRunner.cs | 2 | SaveData |
| SaveManager.cs | 3 | All above |
| ShrineData.cs | 1 | None (ScriptableObject) |
| ShrineManager.cs | 3 | ShrineData, EventBus |
| AutoSaveManager.cs | 3 | SaveManager, EventBus |
| SaveTelemetry.cs | 3 | SaveManager |
| Test scene | - | All above |

**Quality Gate:** Full save/load cycle works with compression, encryption, backups

---

### Phase 3: Status Effects System
**Goal:** Buffs, debuffs, and control effects work in combat

| Task | Layer | Dependencies |
|------|-------|--------------|
| StatusEffectData.cs | 1 | Enums |
| StatusEffectManager.cs | 3 | StatusEffectData, Combatant |
| Integration with combat | 4 | BattleManager, Combatant |
| Test scene | - | All above |

**Quality Gate:** Apply/remove/tick effects work, stacking rules enforced

---

### Phase 4: Gambits AI System
**Goal:** Monsters use intelligent brand-specific AI

| Task | Layer | Dependencies |
|------|-------|--------------|
| GambitData.cs | 1 | Enums |
| GambitCondition.cs | 1 | Enums |
| GambitController.cs | 4 | GambitData, StatusEffectManager |
| Brand AI profiles | 4 | GambitController |
| Integration | 4 | BattleManager |
| Test scene | - | All above |

**Quality Gate:** All 10 brand AIs behave correctly per design doc

---

### Phase 5: Quick Command System
**Goal:** Player can command allies with radial menu

| Task | Layer | Dependencies |
|------|-------|--------------|
| QuickCommandSystem.cs | 4 | Combat foundation |
| Command types | 4 | QuickCommandSystem |
| Radial menu UI | 5 | UI Toolkit |
| Integration | 4 | BattleManager |
| Test scene | - | All above |

**Quality Gate:** All commands work, cooldowns enforced, time slow works

---

### Phase 6: Capture System
**Goal:** Player can mark and capture monsters

| Task | Layer | Dependencies |
|------|-------|--------------|
| CaptureSystem.cs | 4 | Combat, SaveManager |
| Bind mode integration | 4 | Combatant, AI |
| QTE system | 4 | Input system |
| Post-battle phase | 4 | BattleManager |
| Test scene | - | All above |

**Quality Gate:** Full capture flow works per design doc

---

### Phase 7: Combat UI
**Goal:** Full AAA combat interface

| Task | Layer | Dependencies |
|------|-------|--------------|
| UI Toolkit setup | 5 | None |
| USS style sheets | 5 | None |
| Player info panel | 5 | GameManager |
| Enemy info panel | 5 | BattleManager |
| Ally floating panels | 5 | Party system |
| Skill bar | 5 | AbilityData |
| All integrations | 5 | All systems |

**Quality Gate:** All UI elements update correctly, AAA polish

---

### Phase 8: Audio System
**Goal:** Immersive adaptive audio

| Task | Layer | Dependencies |
|------|-------|--------------|
| FMOD integration | External | Package install |
| AudioManager.cs | 3 | FMOD |
| Sound bank structure | - | AudioManager |
| Smart loading system | 3 | AudioManager |
| Integration | 3 | All systems |

**Quality Gate:** Audio plays correctly, memory budget met (~250MB)

---

## Agent Coordination Protocol

### Role Assignment (NO OVERLAP)

| Agent | Role | When Used |
|-------|------|-----------|
| **Main Claude + Serena** | Implementation | Writing all code |
| **unity-architect** | Design Review | Before each phase |
| **unity-code-reviewer** | Code Review | After implementation |
| **unity-performance-profiler** | Performance | Complex systems only |
| **bug-hunter** | Bug Scanning | Background, read-only |
| **commit-helper** | Commits | After approval |
| **balance-analyzer** | Balance Check | Damage/capture/synergy code |
| **vera-dialogue-tester** | VERA Testing | VERA system (Phase 9+) |

### Workflow Per Feature

```
┌─────────────────────────────────────────────────────────┐
│  1. DESIGN REVIEW                                       │
│     → unity-architect verifies design is implementable  │
│     → Resolve any gaps before proceeding                │
└───────────────────────────┬─────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│  2. IMPLEMENTATION                                      │
│     → Main Claude writes code                           │
│     → Serena for code intelligence                      │
│     → Context7 for Unity API questions                  │
└───────────────────────────┬─────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│  3. CODE REVIEW                                         │
│     → unity-code-reviewer checks implementation         │
│     → Fix any issues before proceeding                  │
└───────────────────────────┬─────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│  4. TESTING                                             │
│     → Create/run test scene                             │
│     → Verify all functionality works                    │
│     → bug-hunter scans for issues (background)          │
└───────────────────────────┬─────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│  5. DOCUMENTATION                                       │
│     → Update MIGRATION_PLAN.md                          │
│     → Update VEILBREAKERS.md session log                │
└───────────────────────────┬─────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│  6. COMMIT                                              │
│     → commit-helper creates clean commit                │
│     → Push to remote                                    │
│     → Increment version                                 │
└─────────────────────────────────────────────────────────┘
```

---

## Quality Gates (7 Checkpoints Per Feature)

Every feature MUST pass all gates before proceeding:

| Gate | Check | Who |
|------|-------|-----|
| 1. Design Verified | Design doc reviewed, gaps resolved | unity-architect |
| 2. Code Complete | All files created, compiles | Main Claude |
| 3. Code Review Passed | Follows standards, no issues | unity-code-reviewer |
| 4. Tests Pass | Test scene works, edge cases | Main Claude |
| 5. Integration Works | No regressions, events fire | Main Claude |
| 6. Docs Updated | MIGRATION_PLAN + VEILBREAKERS | Main Claude |
| 7. Committed | Clean commit, pushed, versioned | commit-helper |

**RULE:** No proceeding to next feature until ALL gates pass.

---

## Version Control Strategy

### Branch Structure
```
master          → Production-ready code only
feature/[name]  → Active feature development
```

### Workflow
1. Create `feature/[name]` from master
2. Implement feature (multiple commits OK)
3. Pass all quality gates
4. Merge to master (squash commit)
5. Delete feature branch
6. Start next feature

### One Feature at a Time
- No parallel branches
- Clear ownership
- Easy rollback if needed

---

## Session Protocol

### Every Session Start
1. Read VEILBREAKERS.md
2. Read MIGRATION_PLAN.md
3. Identify current phase/feature
4. Continue from last checkpoint

### During Session
- Work on ONE feature only
- Follow quality gate workflow
- Commit every 15 minutes (protocol)
- Update session log

### Session End
1. Commit current progress
2. Update VEILBREAKERS.md session log
3. Note where to resume
4. Push to remote

---

## Estimated Timeline

| Phase | Features | Est. Sessions |
|-------|----------|---------------|
| 1 | Foundation | 1 |
| 2 | Save/Load | 2-3 |
| 3 | Status Effects | 2 |
| 4 | Gambits AI | 2-3 |
| 5 | Quick Command | 1-2 |
| 6 | Capture System | 2 |
| 7 | Combat UI | 3-4 |
| 8 | Audio System | 2-3 |
| **TOTAL** | | **15-20 sessions** |

---

## Success Metrics

### Per Feature
- [ ] Compiles without errors
- [ ] Test scene runs correctly
- [ ] Code review approved
- [ ] Documentation updated
- [ ] Committed and pushed

### Per Phase
- [ ] All features in phase complete
- [ ] Integration tested
- [ ] MIGRATION_PLAN.md updated
- [ ] Ready for next phase

### Project Complete
- [ ] All 8 phases done
- [ ] MIGRATION_PLAN.md shows 100%
- [ ] Game is playable start-to-finish
- [ ] All systems integrated

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Agent conflicts | Sequential workflow, no parallel |
| Lost context | VEILBREAKERS.md + session logs |
| Broken code | Quality gates + test scenes |
| Scope creep | Stick to design docs |
| Burnout | One feature at a time |

---

## Approval Checklist

Before proceeding with implementation:

- [ ] User approves implementation order
- [ ] User approves agent coordination protocol
- [ ] User approves quality gate workflow
- [ ] User approves version control strategy
- [ ] User confirms ready to begin Phase 1

---

## Summary

**THE PLAN:**
1. One feature at a time
2. Layer-based dependency order
3. Sequential agent workflow (no conflicts)
4. 7 quality gates per feature
5. Simple branch strategy
6. Clear session protocol

**NEXT STEP:** User approval, then begin Phase 1 (Foundation Completion)

---

*Strategy document complete - 2026-01-19*

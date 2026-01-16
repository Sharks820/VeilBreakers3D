# VEILBREAKERS - Architectural Decisions Log

> Record of significant design decisions and their reasoning.

---

## Decision Format

```
## [DECISION-XXX] Title
**Date:** YYYY-MM-DD
**Status:** Accepted / Superseded / Deprecated
**Context:** Why this decision was needed
**Decision:** What was decided
**Consequences:** Impact of this decision
```

---

## [DECISION-001] Brand System Design (LOCKED)

**Date:** 2026-01-01
**Status:** ACCEPTED - LOCKED

**Context:**
Needed a type system for monsters that provides strategic depth without overwhelming complexity. Traditional elemental systems (fire/water/etc.) felt generic.

**Decision:**
- 12 Brands: 6 Pure + 6 Hybrid
- Effectiveness wheel: SAVAGE > IRON > VENOM > SURGE > DREAD > LEECH > SAVAGE
- 3 Tiers: Pure (specialist), Hybrid (versatile), Primal (endgame)
- Lower corruption = stronger (ASCENSION goal)

**Consequences:**
- All combat balance derived from this system
- Monster design tied to brand identity
- Equipment can be brand-locked
- System is LOCKED - no changes without user approval

---

## [DECISION-002] Path System for Heroes

**Date:** 2026-01-01
**Status:** ACCEPTED

**Context:**
Heroes needed differentiation from monsters. Using same Brand system would be confusing.

**Decision:**
- 4 Paths: IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED
- Paths synergize with multiple monster brands
- Each hero has one Path
- Paths have brand strengths/weaknesses (1.35x/0.70x)

**Consequences:**
- Heroes feel distinct from monsters
- Party composition matters
- Each hero has clear role identity

---

## [DECISION-003] Corruption Philosophy

**Date:** 2026-01-01
**Status:** ACCEPTED - CORE DESIGN

**Context:**
Needed to differentiate from Pokemon's "catch-em-all" approach. Theme is about redemption vs exploitation.

**Decision:**
- Lower corruption = STRONGER monster
- Goal is ASCENSION (0-10% corruption)
- DOMINATE method works but costs power
- PURIFY method rewards patience

**Consequences:**
- Unique gameplay loop
- Moral choice in capture method
- VERA's manipulation becomes ironic (she IS corruption)
- Cannot be changed without fundamentally altering game

---

## [DECISION-004] Version Format Change

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
Old format (v5.3) suggested game was nearly complete. Needed clearer pre-release versioning.

**Decision:**
- v0.XX format until release
- +0.01 for minor changes
- +0.10 for major updates
- v1.00 = release candidate

**Consequences:**
- Clearer development stage indication
- v5.3 became v0.53
- Major agent update = v0.60

---

## [DECISION-005] Agent Architecture

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
Complex game development requires specialized AI agents for different tasks.

**Decision:**
- 6 Primary agents with distinct roles
- Builder uses Opus 4.5 with extended thinking (best reasoning)
- Creative agents use higher temperature
- Operations manages coordination
- Agents can invoke subagents via @mention

**Consequences:**
- Parallel development possible
- Specialized expertise per domain
- Coordination overhead required
- Memory sync needed between agents

---

## [DECISION-006] Dark Fantasy Horror Art Style

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
Initially inspired by Battle Chasers comic style, but AI generation produced different results. Needed to define actual style.

**Decision:**
- Dark Fantasy Horror (not comic book)
- AI-generated digital painting
- Glowing elements, deep shadows
- Ominous, unsettling mood
- NOT: comic outlines, pixel art, bright fantasy

**Consequences:**
- All assets must match this style
- Scenario.gg models trained on this aesthetic
- Style guide created for consistency
- Battle Chasers refs are inspiration only, not target

---

## [DECISION-007] Operations Agent Cannot Delete

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
User concerned about unintended file deletion during autonomous operation.

**Decision:**
- Operations can reorganize, move, rename files
- Operations can archive files to `archive/` folder
- Operations can NEVER delete any file
- Archive acts as soft-delete

**Consequences:**
- archive/ folder may grow over time
- No accidental permanent data loss
- User retains final delete authority

---

## [DECISION-008] High-Risk Items Require User Approval

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
Autonomous agents could make breaking changes without user knowledge.

**Decision:**
High-risk items that MUST ask user:
- Change Brand/Path system design
- Modify save file format
- Remove or rename core classes
- Change corruption philosophy
- Major UI flow changes
- Game function/story/big script changes
- Delete ANY file

**Consequences:**
- Agents work autonomously on most tasks
- Critical changes have human oversight
- User trusts agents for routine work

---

## [DECISION-009] Single Source of Truth Files

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
Multiple documentation files caused confusion about which to update.

**Decision:**
- `AGENT_SYSTEMS.md` - Game systems (READ ONLY)
- `docs/STYLE_GUIDE.md` - Art direction
- `docs/CURRENT_STATE.md` - What's working now
- `docs/CHANGELOG.md` - Version history
- `VEILBREAKERS.md` - UI values, session notes

**Consequences:**
- Clear ownership per file
- Agents know where to read/write
- Reduces duplication

---

## [DECISION-010] Scenario.gg Direct API

**Date:** 2026-01-02
**Status:** ACCEPTED

**Context:**
No official Scenario.gg MCP server exists. Needed integration path.

**Decision:**
- Use direct API calls via curl/webfetch
- User-trained models free to use
- Public Scenario models require approval
- Credentials stored in AGENT_SYSTEMS.md

**Consequences:**
- Full Scenario.gg access without MCP
- Can use VeilBreakersV1 trained model
- V2 model (Dark Fantasy Creatures) training in progress

---

*Add new decisions at the bottom with incrementing IDs.*

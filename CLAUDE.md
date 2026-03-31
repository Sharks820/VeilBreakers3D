# VEILBREAKERS 3D - CLAUDE CONFIGURATION

## Mission
Build an AAA-quality 3D monster RPG using Unity. Quality over speed, but don't overthink simple tasks.

**Engine:** Unity 3D (UI Toolkit) | **Memory:** `VEILBREAKERS.md` | **Migration:** `Docs/MIGRATION_PLAN.md`

---

# CORE PRINCIPLES

## Anti-Regression Protocol (MANDATORY)
- **ALWAYS read a file before editing it.** No exceptions.
- **Test after every 3-5 changes.** Run compile check or relevant tests.
- **Max 2 attempts per approach.** If fix #2 fails, re-read context, try fundamentally different approach.
- **Never guess API signatures.** Use Context7, Serena, or read the source.
- **If you break something while fixing something else, revert immediately.**

## Context7 — HARD RULE
Before writing ANY PrimeTween (`/kyrylokuzyk/primetween`), UI Toolkit (`/needle-mirror/com.unity.ui`), Cinemachine (`/websites/unity3d_packages_com_unity_cinemachine_3_1`), or URP (`/unity-technologies/graphics`) code: call `resolve-library-id` then `query-docs`. NON-NEGOTIABLE. Hallucinated APIs have cost entire sessions. If Context7 has no answer, read `Packages/` source — NEVER guess.

## Visual QA Pipeline
1. **Design** -> brainstorm / HTML mockup / reference screenshot
2. **Extract spec** -> `zai ui_to_artifact` (output_type=spec)
3. **Implement** -> Unity UI Toolkit (UXML + USS + C#/PrimeTween)
4. **Capture** -> `unity_editor action=screenshot`
5. **Compare** -> `zai ui_diff_check` (expected=mockup, actual=screenshot)
6. **Iterate** -> fix gaps until it passes

## Reasoning Budget
- **Default:** 2-pass (hypothesis -> targeted verification)
- **Deep mode:** `sequential-thinking` only for high-risk changes, 3+ interacting systems, or unclear repro
- **Loop detection:** 3+ failed attempts -> STOP, summarize, ask user or change approach entirely

---

# PROJECT CONTEXT

## Key Systems (Don't Break These)

**10-Brand Combat:** IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID
- Each: 2x to 2 brands, 0.5x to 2 brands, 1x to 6 brands

**4 Paths:** IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED

**Corruption (0-100%):**
- 0-10% ASCENDED (+25%) | 11-25% Purified (+10%) | 26-50% Unstable (+0%)
- 51-75% Corrupted (-10%) | 76-79% Abyssal (-20%) | **80-100% UNTAMED (uncontrollable)**

**Synergy Tiers:** FULL (3/3): +8% dmg/def, 0.5x corruption scaling | PARTIAL (2/3): +5% | NEUTRAL/ANTI: +0%

**Party:** 3 Active + 3 Backpack slots (hard constraint). Swap cooldown: 3-5s abilities, instant basic.

**Brand Matrix Rule:** Effectiveness is bidirectional — if IRON is 2x vs SURGE, SURGE must be 0.5x vs IRON.

## Code Style
```csharp
namespace VeilBreakers.[Category]
{
    public class Example : MonoBehaviour
    {
        private const int kMaxValue = 10;      // Constants: k prefix
        [SerializeField] private int _value;   // Private: _ prefix
        public int Value => _value;            // Properties: PascalCase
        public event Action<int> OnChanged;    // Events: On prefix
    }
}
```

## Project Structure
- Scripts: `Assets/Scripts/[Combat|Core|Systems|UI|Data]/`
- Art: `Assets/Art/` | Docs: `Docs/` | Screenshots: `screenshots/`

---

# HIGH-RISK CHANGES (Ask User First)
- Brand/Path system design changes
- Save file format modifications (test with old saves via MigrationRunner)
- Core class renames/removals
- Major architectural changes
- Corruption tier threshold changes (80% = UNTAMED is a hard game state boundary)
- Capture formula modifications (deterministic: HP% + Corruption% + Item Tier + QTE)
- Party slot structure changes (breaks save compatibility)
- Synergy multiplier adjustments (cascades across all brand/path combos)
- Deleting files (archive instead)

# SECURITY (Game-Critical)
- SaveManager uses AES-CBC + HMAC-SHA256 — maintain on all format changes
- Validate deserialized save data against gameplay constraints (corruption 0-100, brand multipliers 0.5-2x)
- No `Path.Combine` with user input, no `JSON.Parse` of untrusted strings
- Event unsubscription on cleanup (memory leak vector)

# LESSONS LEARNED
**Don't:** `Find()` in Update, allocations in Update, missing font refs, disabled components, editing without reading, guessing PrimeTween/Cinemachine APIs, retrying same broken approach 5+ times, stacking fixes on broken base, Windows reserved filenames (nul/con/aux)
**Do:** ScriptableObjects for data, event-driven architecture, visual verification via screenshots, read before edit, test every 3-5 changes, parallel agents for research / sequential for edits

# GIT WORKFLOW
- `master` -- production truth | `develop` -- mirrors master | `feature/<name>` -- from master
- **After every commit:** `git branch -f develop master`
- **Before ending session:** verify all branches synced

---
*Configuration v8.1 - Slim + game design rules. Path-scoped rules in .claude/rules/*

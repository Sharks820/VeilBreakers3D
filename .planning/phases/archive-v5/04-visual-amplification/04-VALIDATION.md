---
phase: 4
slug: visual-amplification
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.6.0 (NUnit) + Unity compilation check |
| **Config file** | `Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef` |
| **Quick run command** | `mcp-unity recompile_scripts` (zero compile errors) |
| **Full suite command** | `mcp-unity run_tests --mode EditMode` |
| **Estimated runtime** | ~15 seconds (compilation) + ~10 seconds (tests) |

---

## Sampling Rate

- **After every task commit:** Run `mcp-unity recompile_scripts` (zero errors)
- **After every plan wave:** Run `mcp-unity run_tests --mode EditMode` + visual spot check via Unity screenshot
- **Before `/gsd:verify-work`:** Full suite must be green + manual visual verification of all 5 success criteria
- **Max feedback latency:** 25 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 04-01-01 | 01 | 1 | VISUAL-01 | compile + import | `mcp-unity recompile_scripts` | N/A (package) | pending |
| 04-01-02 | 01 | 1 | VISUAL-01 | compile | `mcp-unity recompile_scripts` | N/A (asmdef) | pending |
| 04-02-01 | 02 | 1 | N/A (infra) | compile | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-02-02 | 02 | 1 | VISUAL-04 | compile + visual | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-03-01 | 03 | 2 | VISUAL-02 | compile + visual | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-03-02 | 03 | 2 | VISUAL-03 | compile + visual | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-04-01 | 04 | 2 | VISUAL-05, VISUAL-07 | compile + visual | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-05-01 | 05 | 3 | VISUAL-06, VISUAL-08 | compile + visual | `mcp-unity recompile_scripts` | pending W0 | pending |
| 04-06-01 | 06 | 3 | VISUAL-09 | compile + audio | `mcp-unity recompile_scripts` | pending W0 | pending |

*Status: pending · green · red · flaky*

---

## Wave 0 Requirements

- [ ] PrimeTween 1.3.8 installed via Package Manager — VISUAL-01
- [ ] `VeilBreakers.Runtime.asmdef` updated with URP + PrimeTween assembly references
- [ ] Shader compilation verified (VeilDissolve.shader, VeilCrack.shader)

*Framework already exists — no new test infrastructure needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Staggered panel entrance animation | VISUAL-02 | Visual timing feel | Enter CharacterSelect, verify left panel slides from left, right from right, staggered timing |
| Stat bar cascade fill | VISUAL-03 | Visual timing feel | Switch heroes, verify bars fill one-by-one with 100ms stagger |
| Per-hero post-process shift | VISUAL-04 | Visual color/mood | Switch between all 4 heroes, verify Bloom/DoF/Vignette/Color change per hero |
| Cinematic overlays visible | VISUAL-05 | Visual atmosphere | Verify scanlines, vignette, veil glow visible and per-hero intensity differs |
| Embark cinematic sequence | VISUAL-06 | Complex VFX timing | Hold embark, verify veil shatter sequence plays (flash→zoom→cracks→shatter→white-out) |
| Inactive panel dimming | VISUAL-07 | Visual depth hierarchy | Verify unfocused panels are dimmed/desaturated vs active panel |
| Embark button breathing glow | VISUAL-08 | Visual animation | Verify embark button has continuous breathing glow while idle |
| Music crossfade | VISUAL-09 | Audio perception | Switch heroes, verify music parameters shift without hard cuts |
| Dissolve/materialize shader | CONTEXT | Visual shader quality | Switch heroes, verify 3D model dissolves into veil energy and reforms |
| Parallax depth | CONTEXT | Visual depth feel | Move mouse/stick, verify UI layers shift at different rates |

---

## Validation Sign-Off

- [ ] All tasks have automated compile verification
- [ ] Sampling continuity: compilation check after every task
- [ ] Wave 0 covers PrimeTween install + asmdef + shader compilation
- [ ] No watch-mode flags
- [ ] Feedback latency < 25s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

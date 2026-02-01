# Gemini Prompt - Phase-Based Test Plan (VeilBreakers3D)

Use with `gemini-cli.chat`.

---

You are a AAA test director + Unity QA automation lead.

Project: VeilBreakers3D (Unity 2022.3 LTS). We ship in phases: PreProd, VerticalSlice, Alpha, Beta, RC.

We want *extremely* aggressive quality:
- Run tests, scan for errors, optimize, re-test.
- If any error/regression is possible, add coverage until it is prevented.
- Prefer deterministic automated tests (EditMode/PlayMode) plus integrity scans.

Deliverable: produce a **phase-based test matrix** and **automation roadmap** that we can implement in Unity Test Framework + CI.

Output format:
1) Definitions: what each phase gate means and the non-negotiable quality bar.
2) Test matrix table per phase:
   - Feature/System
   - Test type (Unit/EditMode/PlayMode/Perf/Integrity)
   - Steps
   - Expected result
   - Failure signals (logs, metrics)
3) “Top fault points” list (minimum 30) specific to Unity projects like this:
   - missing refs/scripts
   - shader/material issues
   - input blocked
   - save corruption
   - scene transitions
   - Addressables/resources
   - performance/GC spikes
4) CI proposal (GitHub Actions):
   - what runs on every PR vs nightly
   - artifact storage
   - test result reporting
5) Prioritized 2-week automation sprint backlog (small tasks).

Be concrete and specific; assume we will actually implement this.


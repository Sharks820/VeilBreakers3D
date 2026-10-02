# VeilBreakers Repo Unification Plan

**Written:** 2026-10-02 (cloud session, read-only analysis of both GitHub repos)
**Question:** Two repos exist for the same game. Which is current, and how do we end up with one?
**Answer:** `Sharks820/VeilBreakers-V4`, branch `overnight/20260806-3d-pipeline`, is the source of truth. `Sharks820/VeilBreakers3D` is a strict subset of it and should be archived.

---

## 1. The verdict in one table

| | VeilBreakers3D (this repo) | VeilBreakers-V4 |
|---|---|---|
| Visibility | **Public** | **Private** |
| Default branch | `master` (= `develop`) | `main` (stale orphan snapshot, 4 commits, June 2026) |
| Real trunk | `master` @ `e8734e6`, 2026-03-31 | `overnight/20260806-3d-pipeline` @ `fcf176b6`, 2026-09-26 |
| Commits on trunk | 607 (2026-01-15 to 03-31) | 1,229 = all 607 of 3D **plus 622 more** (05-30 to 09-26) |
| Shares history? | — | Yes. 3D's `master` tip `e8734e6` is a direct ancestor of V4's trunk. V4's trunk is a fast-forward of 3D's master. |
| Unity | 6000.3.6f1 | 6000.4.11f1 |
| Git LFS | Rules declared, zero objects (binaries stored raw) | Working: 1,661 pointers, ~3.8 GB of objects in V4's LFS store |
| Content only here | One commit, `2ac116d` (2026-09-28): `Docs/ASTRA_SESSION_HANDOFF.md` | 622 commits of world gen, wolf/monster stack, save v3, Gambit AI, 3D pipeline, guardrail tooling |
| Self-declared status | — | `VEILBREAKERS.md` line 17 calls 3D "legacy history". `.claude/skills/github-workflow/SKILL.md` line 18: "VeilBreakers3D is LEGACY, do not target it". |
| Open PRs | 0 | 0 |

**Why two repos exist.** Work ran on 3D until 2026-03-31. On 2026-05-30 a commit gitignored commercial asset packs because the repo was public. On 2026-06-02 V4 was created private, and the full 3D history was pushed into it as `overnight/20260601-frontend-world`. On 2026-06-15 V4's `main` was squashed to a single "condensed" commit, which is why `main` shares no ancestry with the real work. On 2026-06-21 the owner decided to promote the real history to `main` (decision "D1 Option A" in `.planning/REPO_REORG_PLAN_2026-06-21.md`). That step was never executed. The Sep 28 handoff was then written by a cloud session that could only see 3D, so it landed in the wrong repo.

**Nothing was lost going from 3D to V4.** With full rename detection the delta is 296 deletions, all intentional: 69 2D monster sprite sheets removed in an owner-approved cleanup (`28f3ac46`), a vendored Tripo Blender add-on, scratch files, retired editor bridges, empty-folder metas. Every deleted blob is still in history. No Unity package was removed, only upgraded or added.

---

## 2. Neither remote has the newest work

`Docs/ASTRA_SESSION_HANDOFF.md` (the Sep 28 commit on this repo's `claude/gifted-ptolemy-sgjgvx` branch) says plainly that the wolf bite pass-2 fix, the `verification/` folder, the render-verify hook and the saved memory had **not been pushed to GitHub**. None of that is in V4 either. V4's tip has wolf bite **pass 1** only (`2355ede6`).

V4's own `AGENTS.md` also names an untracked `Docs/plans/2026-09-22-AUTONOMOUS-UPGRADE-PLAN.md` and an unmerged local branch `overnight/20260922-toolkit-audit`.

**So the newest state of the game lives only on the owner's local machine.** Step 0 below is not optional.

---

## 3. Options, ranked

1. **Unify into V4 (private). Archive 3D.** Recommended. No LFS migration, no exposure of paid or private content, matches the owner's own June 21 decision and the project docs.
2. **Unify into 3D, keep the name.** Only if 3D is made private first. Costs: fetch ~3.8 GB of LFS objects from V4 and push them to 3D (doubles LFS storage on the account), push a ~0.8 GB pack, recreate CI secrets, and rewrite dozens of files that hard-code the V4 repo identity.
3. **Leave both, add pointer READMEs.** Cheap, fixes nothing. The wrong-repo footgun stays.
4. **Push V4's trunk into public 3D as-is. Rejected.** It is technically a trivial fast-forward, which is exactly what makes it dangerous. It would publish paid MicroSplat modules (licence violation), files containing local user paths and the owner's email, and thousands of raw AI transcripts. Clones would also be broken because the LFS objects would not be there. Irreversible once public.

---

## 4. Step-by-step for Option 1

**Step 0. Owner, on the local machine: push everything, then back up.**
- Commit and push all local WIP and branches to V4 (`git push github <branch>`): wolf pass 2, `verification/`, the autonomous upgrade plan, `overnight/20260922-toolkit-audit`, any Grok WIP.
- `git bundle create vb-all.bundle --all` plus a copy of `.git/lfs`. V4's own docs say a real backup does not exist yet.

**Step 1. Land 3D's one unique commit on V4's trunk.**
- `git cherry-pick 2ac116d` applies cleanly on `fcf176b6` (verified in this session; result is one new file, no conflicts).
- Suggested re-home to match V4's layout: `Docs/plans/2026-09-28-ASTRA-WOLF-SESSION-HANDOFF.md`. Two small fixes while doing so: the doc cites "CLAUDE.md §1.1–1.2" but in V4 those rules live in `AGENTS.md`; and "nothing pushed" is only partly true, pass 1 is on V4.
- Confirm the handoff is still current first. It is dated 09-28.

**Step 2. Promote the trunk to V4 `main`.** This is the June 21 decision, finally executed. It replaces a disjoint `main`, so it is a forced update that needs explicit owner approval, and V4's guard hooks will block it unless the lease form of the force flag is used.
```
git tag archive/orphan-main 9dd2d9e1
git push github archive/orphan-main
git push github <trunk-tip>:main --force-with-lease=main:9dd2d9e1
# rollback if needed:
git push github archive/orphan-main:main --force
```
- `main`'s only unique content is `.github/workflows/unity-license-check.yml` (19 lines, a temporary buildalon licence test). Cherry-pick `9dd2d9e1` only if that route is still wanted.

**Step 3. Retire side branches in V4.** Tag, then delete `condense-to-main`, `fix/codex-review` and `overnight/20260601-frontend-world`. All three are fully contained in the trunk. Keep `overnight/20260806-3d-pipeline` until `main` is confirmed good, then work from `main`.

**Step 4. Fix the V4 docs and hooks that say "never branch from main".** `AGENTS.md` lines 133-135, `CONTRIBUTING.md` trunk line, `.claude/hooks/context-reinject.js` line 81, `.claude/agents/commit-helper.md` line 36, `.claude/skills/github-workflow/SKILL.md`, `.serena/memories/core.md` line 7.

**Step 5. Retire 3D.**
- Merge a tombstone README to `master` ("moved to a private repo 2026-06-02; history through 2026-03-31 preserved here").
- **Archive the repo on GitHub. Do not delete it.** Reasons: the project's own deletion gate (`VEILBREAKERS.md` line 250), the unmerged head of closed PR #7 (synergy system v1.39) exists only here, and it is the history of record. An archived repo also rejects pushes, which stops cloud sessions from landing work here by mistake.
- Remove the `oldrepo` remote from the local clone.
- Point claude.ai/code cloud sessions at V4.

**Step 6. Later, optional.** Rename V4 (GitHub redirects). Prune raw transcripts under `Docs/plans`. Any history-rewriting LFS migration only with a fresh backup and one re-clone.

---

## 5. Risk audit (public-exposure, secrets, licences, LFS)

_PENDING_SONNET_

---

## 6. What this session did and did not do

- Read-only analysis of both repos, plus a local scratch branch (V4 trunk + cherry-picked `2ac116d`) to prove Step 1 applies cleanly.
- Nothing from V4 was pushed to this public repo, and nothing was pushed to V4. This session only has push rights to `claude/amazing-hamilton-unkm5y` on VeilBreakers3D.
- The 50 "modified" binaries that `git status` shows in a fresh clone of this repo are not real changes. `.gitattributes` declares LFS rules for png/mp3/wav/ttf but the files were committed raw, so the LFS clean filter wants to convert them. Irrelevant once this repo is archived.

**Unification does not make a fresh clone build.** V4's own docs say a fresh clone does not compile without gitignored paid packs, Unity CI has never been green, and GitHub Actions has been refused for billing since 2026-09-23. Track that separately.

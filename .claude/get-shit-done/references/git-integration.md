<overview>
Git integration for GSD framework.
</overview>

<core_principle>

**Commit outcomes, not process.**

The git log should read like a changelog of what shipped, not a diary of planning activity.
</core_principle>

<commit_points>

**IMPORTANT: This project uses plan-level commits (not per-task). Do NOT commit after each task. Batch all task work into a single commit per plan.**

| Event                   | Commit? | Why                                              |
| ----------------------- | ------- | ------------------------------------------------ |
| BRIEF + ROADMAP created | YES     | Project initialization                           |
| PLAN.md created         | NO      | Intermediate - commit with plan completion       |
| RESEARCH.md created     | NO      | Intermediate                                     |
| DISCOVERY.md created    | NO      | Intermediate                                     |
| **Task completed**      | NO      | Batched into plan commit (plan-level granularity)|
| **Plan completed**      | YES     | Single commit: all code + metadata together      |
| Handoff created         | YES     | WIP state preserved                              |

**IMPORTANT: Do NOT push to remote automatically. Do NOT pull from remote automatically. Only push/pull when explicitly requested by the user.**

</commit_points>

<git_check>

```bash
[ -d .git ] && echo "GIT_EXISTS" || echo "NO_GIT"
```

If NO_GIT: Run `git init` silently. GSD projects always get their own repo.
</git_check>

<commit_formats>

<format name="initialization">
## Project Initialization (brief + roadmap together)

```
docs: initialize [project-name] ([N] phases)

[One-liner from PROJECT.md]

Phases:
1. [phase-name]: [goal]
2. [phase-name]: [goal]
3. [phase-name]: [goal]
```

What to commit:

```bash
node ./.claude/get-shit-done/bin/gsd-tools.cjs commit "docs: initialize [project-name] ([N] phases)" --files .planning/
```

</format>

<format name="task-completion">
## Task Completion (During Plan Execution)

**Tasks do NOT get individual commits.** All task work is batched and committed once when the plan completes. This reduces commit noise while maintaining meaningful history.

```
{type}({phase}-{plan}): {plan-name}

- [Task 1: key change]
- [Task 2: key change]
- [Task 3: key change]
```

**Commit types:**
- `feat` - New feature/functionality
- `fix` - Bug fix
- `test` - Test-only (TDD RED phase)
- `refactor` - Code cleanup (TDD REFACTOR phase)
- `perf` - Performance improvement
- `chore` - Dependencies, config, tooling

**Examples:**

```bash
# Standard task
git add src/api/auth.ts src/types/user.ts
git commit -m "feat(08-02): create user registration endpoint

- POST /auth/register validates email and password
- Checks for duplicate users
- Returns JWT token on success
"

# TDD task - RED phase
git add src/__tests__/jwt.test.ts
git commit -m "test(07-02): add failing test for JWT generation

- Tests token contains user ID claim
- Tests token expires in 1 hour
- Tests signature verification
"

# TDD task - GREEN phase
git add src/utils/jwt.ts
git commit -m "feat(07-02): implement JWT generation

- Uses jose library for signing
- Includes user ID and expiry claims
- Signs with HS256 algorithm
"
```

</format>

<format name="plan-completion">
## Plan Completion (After All Tasks Done)

After all tasks committed, one final metadata commit captures plan completion.

```
docs({phase}-{plan}): complete [plan-name] plan

Tasks completed: [N]/[N]
- [Task 1 name]
- [Task 2 name]
- [Task 3 name]

SUMMARY: .planning/phases/XX-name/{phase}-{plan}-SUMMARY.md
```

What to commit:

```bash
node ./.claude/get-shit-done/bin/gsd-tools.cjs commit "docs({phase}-{plan}): complete [plan-name] plan" --files .planning/phases/XX-name/{phase}-{plan}-PLAN.md .planning/phases/XX-name/{phase}-{plan}-SUMMARY.md .planning/STATE.md .planning/ROADMAP.md
```

**Note:** Code files NOT included - already committed per-task.

</format>

<format name="handoff">
## Handoff (WIP)

```
wip: [phase-name] paused at task [X]/[Y]

Current: [task name]
[If blocked:] Blocked: [reason]
```

What to commit:

```bash
node ./.claude/get-shit-done/bin/gsd-tools.cjs commit "wip: [phase-name] paused at task [X]/[Y]" --files .planning/
```

</format>
</commit_formats>

<example_log>

**Old approach (per-plan commits):**
```
a7f2d1 feat(checkout): Stripe payments with webhook verification
3e9c4b feat(products): catalog with search, filters, and pagination
8a1b2c feat(auth): JWT with refresh rotation using jose
5c3d7e feat(foundation): Next.js 15 + Prisma + Tailwind scaffold
2f4a8d docs: initialize ecommerce-app (5 phases)
```

**VeilBreakers approach (per-plan commits):**
```
# Phase 04 - Checkout
1a2b3c feat(04-01): checkout flow with Stripe webhooks

# Phase 03 - Products
3m4n5o feat(03-02): product listing with search, filters, pagination
2v3w4x feat(03-01): product catalog schema and API

# Phase 02 - Auth
5y6z7a feat(02-02): JWT refresh token rotation
7k8l9m feat(02-01): JWT generation, validation, and jose setup

# Phase 01 - Foundation
2z3a4b feat(01-01): Next.js 15 + Prisma + Tailwind scaffold

# Initialization
5c6d7e docs: initialize ecommerce-app (5 phases)
```

Each plan produces 1 commit (all tasks batched). Clean, readable, focused.

</example_log>

<anti_patterns>

**Still don't commit (intermediate artifacts):**
- PLAN.md creation (commit with plan completion)
- RESEARCH.md (intermediate)
- DISCOVERY.md (intermediate)
- Minor planning tweaks
- "Fixed typo in roadmap"

**Do commit (outcomes):**
- Plan completion (all tasks batched into one feat/fix/refactor commit)
- Project initialization (docs)

**Key principle:** Commit working code at plan boundaries, not after every task. Never auto-push or auto-pull.

</anti_patterns>

<commit_strategy_rationale>

## Why Per-Plan Commits? (VeilBreakers Override)

This project uses plan-level commits instead of per-task commits to reduce git noise.

**Cleaner history:**
- Each commit represents a complete, coherent unit of work (a full plan)
- `git log` reads like a changelog, not a diary
- Less noise = easier manual review for solo developer

**Still recoverable:**
- SUMMARY.md tracks individual tasks within each plan
- STATE.md preserves progress context
- One commit per plan is still granular enough for `git bisect`

**No auto-push/pull:**
- Never push to remote unless user explicitly asks
- Never pull from remote unless user explicitly asks
- The user controls when remote sync happens

</commit_strategy_rationale>

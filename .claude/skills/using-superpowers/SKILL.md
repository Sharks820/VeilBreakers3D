---
name: using-superpowers
description: Smart skill usage - use judgment, not rigid rules
---

# Smart Skill & Tool Usage (VeilBreakers Override)

**This overrides the default "1% = MUST invoke" rule with intelligent judgment.**

## Core Principle

Use skills and tools when they **genuinely help**, not because a rule forces you to.

## Decision Framework

### USE a skill/tool when:
- The task is complex enough to benefit from structure
- You're unsure how to approach something
- The user explicitly requests it
- It would prevent a mistake or catch an issue

### SKIP a skill/tool when:
- The task is simple and obvious
- You already know exactly what to do
- Using it would add overhead without value
- The user wants a quick answer

## Serena (Code Intelligence)

| Situation | Action |
|-----------|--------|
| Need to understand a file's structure | `get_symbols_overview` |
| Looking for a specific method/class | `find_symbol` |
| Need to see where something is used | `find_referencing_symbols` |
| Simple file read (you know what's there) | Just use `Read` tool |
| Quick grep for a string | Just use `Grep` tool |

**Don't use Serena for:** Simple file reads, known file locations, quick searches.
**Do use Serena for:** Understanding code structure, refactoring, finding dependencies.

## Superpowers Skills

| Skill | Use When | Skip When |
|-------|----------|-----------|
| brainstorming | Complex features, unclear requirements | Simple/obvious tasks |
| writing-plans | Multi-step implementations | Single-file changes |
| systematic-debugging | Mysterious bugs, multiple potential causes | Obvious error with clear fix |
| test-driven-development | New features that need tests | Quick fixes, config changes |
| verification-before-completion | Major changes, PR-ready work | Small edits, exploration |

## Red Flags (You're Over-Using Tools)

- Invoking 3+ skills for a simple question
- Using brainstorming for "add a button"
- Running full TDD loop for a typo fix
- Using Serena to read a file you just need to glance at

## Red Flags (You're Under-Using Tools)

- Implementing a complex feature without any planning
- Claiming something is "fixed" without running tests
- Making architectural changes without understanding dependencies
- Guessing at code structure instead of checking

## The Balance

**Lean toward action** for simple tasks.
**Lean toward structure** for complex tasks.
**Trust your judgment** - you're an expert developer.

---
name: gemini-collab
description: Collaborate with Gemini for enhanced problem-solving. Use when stuck, need validation, or want a second perspective on complex decisions.
---

# Claude + Gemini Collaboration Protocol

Leverage both AI systems for maximum development power.

## When to Invoke This Skill

| Trigger | Situation |
|---------|-----------|
| "I'm stuck on..." | Complex debugging or design problem |
| "Should I..." | Architecture or approach decisions |
| "Validate this..." | Balance calculations, logic verification |
| "Research..." | Need current web information |
| "Review my plan..." | Pre-implementation validation |

## Collaboration Modes

### Mode 1: Second Opinion
Ask Gemini to analyze your proposed solution:

```bash
gemini -p "I'm about to [action]. Here's my reasoning: [reasoning]. What am I missing?"
```

### Mode 2: Parallel Analysis
When facing a complex problem:

1. **Claude analyzes** using codebase context (Serena, local files)
2. **Gemini analyzes** the abstract problem (no codebase needed)
3. **Synthesize** both perspectives

### Mode 3: Research Delegation
For current information Claude doesn't have:

```bash
gemini -p "Search for: [topic]. What are the latest best practices for [specific question]?"
```

### Mode 4: Adversarial Review
Have Gemini try to break your solution:

```bash
gemini -p "Find problems with this approach: [solution]. Be critical and look for edge cases."
```

### Mode 5: Balance Validation
For game balance decisions:

```bash
gemini -p "Analyze this game balance change:
- Current: [values]
- Proposed: [new values]
- Systems affected: [list]
Calculate edge cases and identify broken combinations."
```

## Workflow Example

### Complex Feature Implementation

1. **Claude brainstorms** initial approach using codebase knowledge
2. **Gemini validates** architecture with fresh perspective:
   ```bash
   gemini -p "Review this Unity architecture for [feature]: [design]. Is this scalable? What patterns am I missing?"
   ```
3. **Claude implements** using Serena and local tools
4. **Gemini reviews** the implementation logic:
   ```bash
   gemini -p "Review this implementation for bugs and edge cases: [key code]"
   ```
5. **Claude finalizes** and commits

## Strength Mapping

| Task | Primary AI | Support AI |
|------|------------|------------|
| Codebase navigation | Claude (Serena) | - |
| Unity-specific code | Claude (Context7) | - |
| Algorithm design | Either | Other validates |
| Web research | Gemini | Claude applies |
| Balance math | Either | Other validates |
| Architecture | Claude proposes | Gemini reviews |
| Debugging | Claude investigates | Gemini suggests alternatives |
| Documentation | Claude writes | Gemini reviews clarity |

## Quick Commands

```bash
# Quick second opinion
gemini -p "Quick check: Is this a good approach? [1-sentence description]"

# Deep analysis
gemini -p "Analyze in depth: [full context]"

# Research mode
gemini -p "What are the 2026 best practices for [topic]?"

# Adversarial mode
gemini -p "Try to break this: [solution]"
```

## Anti-Patterns (Don't Do This)

- Don't ask Gemini about YOUR codebase (it can't see it)
- Don't ask Gemini to write code you'll paste directly (context mismatch)
- Don't use for trivial decisions (wastes time)
- Don't ignore Gemini's valid criticisms just because Claude said something different

## Integration Philosophy

**Claude = Deep codebase expert with execution power**
**Gemini = Fresh perspective with web access**

Together: **Comprehensive analysis + Validation + Current knowledge**

# Gemini Full Scope Prompt (VeilBreakers3D)

Use this prompt with `gemini-cli.chat` to generate a complete production scope (systems, content, phases, build pipeline, risks).

---

You are a senior AAA game producer + technical director + Unity build engineer.

Project: **VeilBreakers3D** (Unity 2022.3 LTS). We want an "AAA-feeling" game produced with heavy AI-assisted content generation, but shippable and stable.

Constraints / realities:
- Small team, but we want AAA *presentation* via smart scope + high impact polish.
- We use Unity UI Toolkit for menus and have custom VFX overlays for the title screen.
- We want a clear, phased plan that ties to builds, CI, and release gates.
- Target platform(s): PC first (Windows), with future console possibility.

Please output:
1) **One-paragraph vision** + 5 non-negotiable pillars (feel, art, combat, progression, atmosphere).
2) **Full scope breakdown** as a numbered list of Epics, each with:
   - Goal
   - Player-facing outcome
   - Key systems/components in Unity (scenes, scripts, shaders, data)
   - Dependencies
   - "Definition of Done" checklist
3) **Phase plan** (Pre-production, Vertical Slice, Alpha, Beta, RC) with:
   - Timebox recommendation
   - Deliverables
   - Quality gates
4) **Build + CI plan** (GitHub Actions) including:
   - Branching strategy
   - Automated Unity build steps
   - Artifact naming/versioning
   - Test strategy (EditMode/PlayMode, smoke tests)
   - Performance budgets and automated checks
5) **AI generation pipeline**:
   - What we can safely AI-generate (textures, concept, SFX, voice, copy, UI art, VFX textures)
   - What should NOT be AI-generated (or must be heavily reviewed)
   - Naming conventions, source control strategy, and "human review" gates
6) **Top 15 risks** (technical + scope + quality) and a mitigation plan for each.
7) **Immediate next 14-day plan**: a prioritized sprint backlog with tasks sized small enough to complete.

Tone: direct, production-ready, not generic. Prefer concrete checklists and measurable acceptance criteria.


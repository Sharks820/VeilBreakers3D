---
name: mockup-ui
description: Generate UI mockup images for VeilBreakers using AI before implementation
---

# UI Mockup Generator for VeilBreakers

Generate visual mockups using FLUX AI before writing any UI code.

## When to Use
- Before implementing any new UI screen
- When designing HUD elements
- For monster/character portraits
- Menu layouts and panels

## Process

1. **Understand the request** - What UI element needs mockup?
2. **Generate prompt** following VeilBreakers style guide
3. **Call FLUX** via mcp-hfspace to generate image
4. **Show result** for user approval before coding

## VeilBreakers Style Guide

**Art Direction:**
- Dark fantasy horror aesthetic
- Deep blacks, purples, crimsons
- Glowing neon accents (cyan, magenta, orange)
- Hand-painted/painterly quality
- Atmospheric fog and particles
- Dramatic rim lighting

**UI Specific:**
- Semi-transparent dark panels
- Glowing borders on interactive elements
- Corruption effects (cracks, tendrils) for high corruption
- Clean readability despite dark theme

## Prompt Template

```
dark fantasy horror game UI, [ELEMENT DESCRIPTION],
semi-transparent dark panel, glowing [COLOR] accents,
dramatic lighting, atmospheric, high detail,
game interface mockup, 1920x1080, professional quality
```

## Example Usage

User: `/mockup-ui health bar with corruption indicator`

Generate:
```
dark fantasy horror game UI, health bar with dual indicators,
red HP bar with purple corruption meter below,
semi-transparent dark panel, glowing purple corruption cracks,
dramatic lighting, atmospheric, high detail,
game interface mockup, 1920x1080, professional quality
```

Then call: `mcp__mcp-hfspace__FLUX_1-schnell-infer` with the prompt

## After Generation

Ask user:
1. Does this match your vision?
2. Any adjustments needed?
3. Ready to implement?

Only proceed to code after approval.

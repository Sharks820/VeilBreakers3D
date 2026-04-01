# VeilBreakers Systems Rules

## Save System (SECURITY CRITICAL)
- Uses AES-CBC + HMAC-SHA256 — maintain on ALL format changes
- Create backup at `PersistentDataPath/veilbreakers.save.bak` on load
- Validate deserialized save data: corruption 0-100, brand multipliers 0.5-2x, party slots max 3+3, stats non-negative
- Increment version field on any schema change
- Do NOT save: temporary scene state, animation tweens, UI state
- Test with old saves via MigrationRunner after ANY format change
- No Path.Combine with user input, no JSON.Parse of untrusted strings
- Event unsubscription on cleanup (memory leak vector)

## Audio System
- See `.claude/rules/systems/audio.md` for full audio rules
- VERA title audio: randomized interactions, NOT looping

## 3D Pipeline
- See `.claude/rules/systems/3d-pipeline.md` for Blender pipeline rules
- GLB/FBX binary assets: never commit without LFS

## 4 Paths
- IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED
- Path abilities and bonuses are game-critical design — never modify without user approval

## Character Select
- Hero volumes use per-hero VolumeProfile assets
- Dissolve controller wired to shader + model renderer
- Brand-specific visual effects

## Code Style
- Namespace: VeilBreakers.Systems (or VeilBreakers.Core for core systems)
- kConstant, _private, PascalProperty, OnEvent
- ScriptableObjects for all game data
- Event-driven architecture — no direct coupling between systems

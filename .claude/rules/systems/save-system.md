---
paths:
  - "Assets/Scripts/Systems/Save/**/*.cs"
  - "Assets/Scripts/Core/Save*"
  - "Assets/Scripts/Core/GameManager*"
---

# Save System Rules (HIGH RISK)

## Security
- SaveManager uses AES-CBC + HMAC-SHA256 — maintain on ALL format changes
- Never deserialize untrusted save data without HMAC verification first
- Validate all deserialized values against gameplay constraints:
  - Corruption: [0, 100]
  - Brand multipliers: [0.5, 2.0]
  - Party slots: max 3 active + 3 backpack
  - Stats: non-negative, within level-appropriate ranges

## Format Changes (Ask User First)
- Increment version field on any schema change
- Test loading old saves via MigrationRunner
- Create backup at `PersistentDataPath/veilbreakers.save.bak` on load
- Never break backward compatibility without migration path

## What NOT to Save
- Temporary scene state, animation tweens, UI state
- Reconstruct UI from character data on load

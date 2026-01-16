# VEILBREAKERS - Complete Systems Design Document

> **Agent Reference File** | Version: 1.0 | Last Updated: 2026-01-01
> 
> This file contains ALL game systems for cross-agent memory. Read completely before making changes.

---

# TABLE OF CONTENTS

1. [Story & Lore](#story--lore)
2. [The Soulbind Capture System](#the-soulbind-capture-system)
3. [Corruption & Ascension](#corruption--ascension)
4. [The 4 Capture Methods](#the-4-capture-methods)
5. [Capture Animation System](#capture-animation-system)
6. [Sanctum Shrine System](#sanctum-shrine-system)
7. [Path System (Heroes)](#path-system-heroes)
8. [Brand System (Monsters)](#brand-system-monsters)
9. [Player Brand Alignment](#player-brand-alignment)
10. [Brand-Locked Equipment](#brand-locked-equipment)
11. [Path-Brand Synergy](#path-brand-synergy)
12. [Monster Morale System](#monster-morale-system)
13. [The 5 Major Story Choices](#the-5-major-story-choices)
14. [VERA System](#vera-system)
15. [Scenario.gg API Integration](#scenariogg-api-integration)

---

# STORY & LORE

## The Veil
A barrier created by 6 gods who couldn't kill VERATH. They took 90% of her power and created the Veil, with the Veil Bringer as its guardian. The remaining 10% was sealed in a human infant.

## The Compact
Organization that monitors Veil cracks and employs hunters to contain escaped monsters. They found VERA as a teenager showing demonic traits and employed her rather than eliminating her.

## VERA / VERATH
- **VERA**: The player's guide, assigned by The Compact. Brilliant (top 1% of 22 billion tested). Beautiful. Helpful.
- **VERATH**: Dimension-devouring demon sealed inside VERA. Slowly regaining memories and power.
- **The Glitches**: Red eyes, horns, demonic voice, sharp teeth - brushed off by Compact as "errors"
- **The Truth**: VERA has been manipulating the player to reach the Veil and absorb the Veil Bringer's power

## The Player's Journey
1. Assigned to hunt monsters with VERA as guide
2. Told they need a Veil Shard to "cure" VERA's condition
3. Bond with VERA deepens as they approach the Veil
4. VERA transforms against an Archdemon (first major reveal)
5. Fight the Veil Bringer (final boss)
6. VERA absorbs the Veil Bringer, reveals true form as VERATH
7. 12 endings based on player Path choices

## Core Theme
**Corruption is the enemy.** The player is a VEILBREAKER - their purpose is to FREE creatures from demonic corruption, not exploit it. The irony: their trusted guide IS corruption incarnate.

---

# THE SOULBIND CAPTURE SYSTEM

## Philosophy
- No "Pokeballs" - these are **Soul Vessels** and dark rituals
- Capturing = binding a corrupted creature's essence
- Goal is to REDUCE corruption and ASCEND monsters to their true form
- DOMINATE is the dark path - it works, but at a cost

## The Capture Bar (Not a Loading Bar)

Visual representation of the **Corruption Battle** - a tug-of-war between corruption and freedom:

```
CORRUPTION ████████████░░░░░░░░ FREEDOM
            ←─ Monster struggling ─→
```

### Capture Formula
```
CAPTURE_CHANCE = BASE + HEALTH_MOD + CORRUPTION_MOD + METHOD_MOD + ITEM_MOD

BASE: Common 50%, Uncommon 40%, Rare 30%, Elite 15%, Legendary 5%
HEALTH_MOD: +1% per 2% HP missing (max +40%)
CORRUPTION_MOD: Alignment bonus/penalty (see methods)
METHOD_MOD: Per-method bonuses
ITEM_MOD: Soul Vessel tier bonus
```

### Number of Passes Required
The corruption battle requires multiple "passes" where the light pushes back the darkness:

| Monster Rarity | Passes Needed | Time Per Pass |
|----------------|---------------|---------------|
| Common | 1 | 1.5 seconds |
| Uncommon | 2 | 1.5 seconds |
| Rare | 3 | 2 seconds |
| Elite | 4 | 2 seconds |
| Legendary | 5 | 2.5 seconds |

**Factors That Reduce Passes:**
- Lower HP: -1 pass if <25% HP
- Lower Corruption: -1 pass if <25 corruption
- PURIFY method: -1 pass
- Pristine Soul Vessel: -1 pass

---

# CORRUPTION & ASCENSION

## Corruption States (0-100)

| Range | State | Effect on Monster |
|-------|-------|-------------------|
| **0-10** | **ASCENDED** | TRUE form, freed from Veil, +25% all stats, Ascended ability, perfect loyalty |
| **11-25** | Purified | Stable, healthy, +10% stats |
| **26-50** | Unstable | Flickering, confused, normal stats |
| **51-75** | Corrupted | Aggressive, in pain, -10% stats, 10% Instability |
| **76-100** | Abyssal | Lost to darkness, -20% stats, 20% Instability, dark abilities |

## ASCENDED State (The Goal)

When a monster reaches 0-10% corruption:

| Bonus | Effect |
|-------|--------|
| **+25% All Stats** | Their TRUE power, uncorrupted |
| **Ascended Ability** | Unique light-based skill unlocked |
| **Perfect Loyalty** | 0% Instability |
| **Veil Resonance** | +10% capture rate on nearby enemies |
| **Visual** | Glowing aura, purified colors, serene animations |

## Corruption Drift (In-Battle)

| Action | Corruption Change |
|--------|-------------------|
| Attack/damage monster | +5 corruption |
| Wait/Defend (don't attack) | -5 corruption |
| Use healing item on enemy | -10 corruption |
| PURIFY attempt (even on fail) | -15 corruption |
| DOMINATE attempt | +10 corruption |

## Captured Monster States

| State | Corruption | Stats | Special |
|-------|------------|-------|---------|
| **Ascended** | 0-10% | +25% | Ascended ability, perfect loyalty |
| **Purified** | 11-25% | +10% | "Inner Light" - immune to corruption debuffs |
| **Soulbound** | 26-50% | Normal | Standard capture |
| **Corrupted** | 51-75% | -10% | 10% Instability (reducible) |
| **Dominated** | 76-100% | -20% | 20% Instability (reducible), dark abilities |

## Instability
- % chance each battle turn to ignore command and attack random target
- Reducible by purifying at Sanctum Shrines
- Fully removed when corruption drops below 26%

---

# THE 4 CAPTURE METHODS

## 1. SOULBIND (Item-Based)

**The Standard Method**

| Soul Vessel Tier | Capture Bonus | Cost | Source |
|------------------|---------------|------|--------|
| Cracked | +10% | 100g | Common shops |
| Standard | +20% | 400g | Mid-game shops |
| Pristine | +35%, -1 pass | 1000g | Late-game, crafting |
| Covenant | +50%, -2 passes | N/A | Boss drops only |

- No drawbacks, resource-limited
- Works at any corruption level (best at 26-50)
- **Visual**: Golden chains wrap monster gently

**Vera Reaction**: *"A leash made of someone else's chains. Practical."*

---

## 2. PURIFY (Sanctum Energy Cost)

**The Righteous Method**

- Costs: 10 + (Corruption ÷ 2) Sanctum Energy
- Best at: Low corruption (0-25)
- **Bonus**: +20% if corruption <25
- **Penalty**: -30% if corruption >75, costs double energy
- Even on fail: Reduces corruption by 15
- Monster joins at Purified state if successful

**Visual**: Light envelops monster, they dissolve into peaceful motes, reform purified

**Vera Reaction**: *(voice skips)* *"You've... cleaned it. How... nice."*

---

## 3. DOMINATE (HP Cost)

**The Dark Method**

- Costs: **25% of CURRENT HP**
- Best at: High corruption (76-100)
- **Bonus**: +20% if corruption >75
- **Restriction**: Unavailable if corruption <26
- On fail: Take HP damage, monster gains +15 corruption, Defiant buff (+20% damage for 3 turns)
- Monster joins at Corrupted/Abyssal state

**Visual**: Red chains SLAM around monster, drag them down violently, they submit

**Vera Reaction**: *(eyes flicker red for one frame)* *"Now you're speaking a language worth hearing."*

---

## 4. BARGAIN (Immediate Price)

**The Gambler's Method**

1. Initiate BARGAIN
2. Game reveals **The Price** (random from table)
3. Accept or decline BEFORE knowing capture result
4. If accept: +25% capture bonus
5. Price paid immediately on success OR failure

### The Price Table (d20)

| Roll | The Price | Duration |
|------|-----------|----------|
| 1-5 | **Blood Tithe**: -15% max HP until next Shrine rest | Temporary |
| 6-8 | **Soul Fragment**: One party member can't use ultimate this battle | Immediate |
| 9-11 | **Whispered Secret**: Vera learns something (hidden, affects ending) | Permanent |
| 12-14 | **Echoed Pain**: Monster's ability costs doubled for 3 battles | Medium |
| 15-17 | **Borrowed Time**: Monster starts battles with random debuff | Permanent* |
| 18-19 | **The Watcher's Interest**: Next boss +10% stats | One-time |
| 20 | **True Name Given**: Perfect loyalty, Vera becomes more unstable | Permanent |

*Removable with **Absolution Stone** (rare drop/craft)

**Visual**: Shadows merge, contract runes flash, monster walks to your side

**Vera Reaction**: *(freezes, glitches hard)* *"Careful. Bargains have... witnesses."*

---

# CAPTURE ANIMATION SYSTEM

## Phase 1: INITIATION
*Player uses capture method*
- Dark tendrils (corruption) wrap around monster
- Glowing light (your influence) appears at monster's core

## Phase 2: THE STRUGGLE
*The Corruption Battle*
- Tug-of-war bar between CORRUPTION (red/left) and FREEDOM (gold/right)
- Monster visually struggles - twitching, roaring, moments of calm
- Each "pass" = light pushes back darkness
- Number of passes based on rarity + factors

## Phase 3: THE BREAK
*Corruption shatters*
- Corruption crystallizes
- Cracks appear
- Monster lets out final roar/cry
- Building tension, heartbeat audio
- 0.5s SILENCE

## Phase 4: THE PULL
*Method-specific capture animation*

| Method | Animation |
|--------|-----------|
| SOULBIND | Golden chains wrap gently, pull into Soul Vessel |
| PURIFY | Light envelops, dissolve to motes, reform purified |
| DOMINATE | Red chains slam, drag down violently, submission |
| BARGAIN | Shadows merge, runes flash, monster walks to you |

## Phase 5: RESULT
- Party celebration pose
- New monster appears in formation
- Corruption state displayed
- **Failure**: Corruption SURGES back (+15), monster enrages (bonus action)

---

# SANCTUM SHRINE SYSTEM

## Shrine Functions

| Function | Effect |
|----------|--------|
| **Rest** | Restore HP/SP, clear temp debuffs |
| **Purify Monster** | Reduce corruption by amount based on tier |
| **Attune** | Save game |
| **Commune** | Optional Vera dialogue (hints, glitch opportunities) |

## Shrine Tiers

| Tier | Cooldown | Corruption Reduced | Location |
|------|----------|-------------------|----------|
| Minor Shrine | 3 battles | -10 | Common, overworld |
| Major Shrine | Instant (1x per visit) | -15 | Towns, dungeons |
| Sacred Sanctum | No cooldown | -20 | Rare, story locations |

## Purification Rules
- Each monster can only be cleansed **once per shrine visit**
- Costs 10 Sanctum Energy per cleansing
- Cannot reduce below 0 corruption (Ascended at 0-10)

## Instability Removal via Purification

| Corruption After Purifying | Instability % |
|----------------------------|---------------|
| 76-100 | 20% |
| 51-75 | 10% |
| 26-50 | 5% |
| 0-25 | 0% (removed) |

---

# PATH SYSTEM (HEROES)

## The 4 Paths

| Path | Philosophy | Playstyle | Hero |
|------|------------|-----------|------|
| **IRONBOUND** | Endure, protect, outlast | Tank, shields, damage reduction | Bastion |
| **FANGBORN** | Hunt, strike, kill | DPS, crits, execute mechanics | Rend |
| **VOIDTOUCHED** | Drain, corrupt, sustain | Lifesteal, debuffs, healing | Marrow |
| **UNCHAINED** | Deceive, evade, chaos | Evasion, illusions, status effects | Mirage |

## Path Combat Bonuses

| Path | Strong vs Brand (1.35x) | Weak vs Brand (0.70x) |
|------|-------------------------|------------------------|
| IRONBOUND | SAVAGE | VENOM |
| FANGBORN | LEECH | IRON |
| VOIDTOUCHED | SURGE | DREAD |
| UNCHAINED | DREAD | LEECH |

---

# BRAND SYSTEM (MONSTERS)

## Pure Brands (6)

| Brand | Stat Bonus | Theme |
|-------|------------|-------|
| SAVAGE | +25% ATK | Raw destruction |
| IRON | +30% HP | Unyielding defense |
| VENOM | +20% Crit, +15% Status proc | Precision poison |
| SURGE | +25% SPD | Lightning speed |
| DREAD | +20% Evasion, +15% Fear proc | Terror incarnate |
| LEECH | +20% Lifesteal | Life drain |

## Hybrid Brands (6)

| Hybrid | Primary | Secondary | Effective Bonus |
|--------|---------|-----------|-----------------|
| BLOODIRON | SAVAGE | IRON | +17.5% ATK, +9% HP |
| CORROSIVE | IRON | VENOM | +21% HP, +6% Crit, +4.5% Status |
| VENOMSTRIKE | VENOM | SURGE | +14% Crit, +10.5% Status, +7.5% SPD |
| TERRORFLUX | SURGE | DREAD | +17.5% SPD, +6% Eva, +4.5% Fear |
| NIGHTLEECH | DREAD | LEECH | +14% Eva, +10.5% Fear, +6% Lifesteal |
| RAVENOUS | LEECH | SAVAGE | +14% Lifesteal, +7.5% ATK |

## Brand Effectiveness Wheel

```
SAVAGE ──► IRON ──► VENOM ──► SURGE ──► DREAD ──► LEECH ──┐
   ▲                                                       │
   └───────────────────────────────────────────────────────┘
```

Attacker deals 1.5x to next in chain, 0.67x to previous.

## Corruption & Brand Power

| Corruption Range | Brand Bonus Multiplier |
|------------------|------------------------|
| 0-10 (Ascended) | 130% + Ascended ability |
| 11-25 (Purified) | 110% |
| 26-50 (Unstable) | 100% |
| 51-75 (Corrupted) | 90% + dark ability access |
| 76-100 (Abyssal) | 80% + enhanced dark ability |

**Lower corruption = stronger Brand expression**

---

# PLAYER BRAND ALIGNMENT

## The 6 Alignment Scores (0-100% each)

Players develop alignment with Brands based on choices:

```
SAVAGE:  ████████░░ 45%
IRON:    ██████████ 72%
VENOM:   ███░░░░░░░ 18%
SURGE:   █████░░░░░ 32%
DREAD:   ██░░░░░░░░ 12%
LEECH:   ████░░░░░░ 28%
```

## How Alignment Changes

### Story Choices (Major Impact)

| Choice Type | Brand Effect |
|-------------|--------------|
| Protect the weak | IRON +15, SAVAGE -5 |
| Show no mercy | SAVAGE +15, IRON -5 |
| Use poison/deception | VENOM +15, IRON -10 |
| Act swiftly, no hesitation | SURGE +15, DREAD -5 |
| Intimidate/terrorize | DREAD +15, LEECH -5 |
| Take what you need | LEECH +15, SAVAGE -5 |

### Battle Choices (Minor Impact)

| Action | Brand Effect |
|--------|--------------|
| Use DOMINATE | DREAD +3, LEECH +2 |
| Use PURIFY | IRON +2, VENOM -1 |
| Let monster flee | SURGE -2, IRON +1 |
| Execute (no capture) | SAVAGE +3 |
| Heal enemy before capture | IRON +3, SAVAGE -2 |

### Party Composition (Passive Drift)
- +1% per 10 battles toward party's dominant Brand

---

# BRAND-LOCKED EQUIPMENT

## Alignment Requirement: 55%+

| Weapon | Brand Required | Bonus When Aligned |
|--------|----------------|-------------------|
| Fang of the Ravager | SAVAGE 55%+ | +30% ATK, Bleed on crit |
| Ironwall Greatshield | IRON 55%+ | +40% DEF, Taunt ability |
| Viper's Kiss Dagger | VENOM 55%+ | +25% Crit, Poison proc |
| Stormrunner Boots | SURGE 55%+ | +30% SPD, First strike |
| Mask of Nightmares | DREAD 55%+ | +25% EVA, Fear aura |
| Siphon Gauntlet | LEECH 55%+ | +20% Lifesteal, Drain ability |

## Hybrid Equipment (40%+ in BOTH parent Brands)

| Equipment | Brands Required | Bonus |
|-----------|-----------------|-------|
| Bloodiron Plate | SAVAGE 40% & IRON 40% | +20% ATK, +25% HP |
| Corrosive Edge | IRON 40% & VENOM 40% | DEF shred on hit |
| Terrorflux Cloak | SURGE 40% & DREAD 40% | Phase through first attack |

## Alignment Decay (Equipped Below Threshold)

| Time Below 55% | Stage | Effects |
|----------------|-------|---------|
| 0-3 battles | **Resistance** | -5% SPD, gear pulses dark |
| 4-6 battles | **Rejection** | -10% SPD, -10% weapon bonus |
| 7-10 battles | **Corruption** | -20% SPD, bonus negated, random debuff |
| 11+ battles | **Shatter** | Gear BREAKS, 25% max HP damage, gone forever |

## Warning Signs

| Stage | Visual/Audio Warning |
|-------|---------------------|
| Resistance | Gear pulses with dark veins, grinding sound |
| Rejection | Visible cracks, character grunts on attack |
| Corruption | Oozes darkness, whispers, Vera comments |
| Pre-Shatter | Screen shake, red glow, "UNEQUIP NOW" prompt |

---

# PATH-BRAND SYNERGY

## Party Synergy Bonuses

| Hero Path | Synergy Brands | Party Bonus |
|-----------|----------------|-------------|
| IRONBOUND | IRON, BLOODIRON, CORROSIVE | +10% DEF, +5% Max HP |
| FANGBORN | SAVAGE, RAVENOUS, BLOODIRON | +10% ATK, +5% Crit Rate |
| VOIDTOUCHED | LEECH, NIGHTLEECH, DREAD | +10% Lifesteal, +5% SP Regen |
| UNCHAINED | SURGE, TERRORFLUX, VENOMSTRIKE | +10% EVA, +5% SPD |

## Synergy Stacking

| Synergy Monsters | Bonus Tier |
|------------------|------------|
| 1 | Tier 1: Half bonus (+5%) |
| 2 | Tier 2: Full bonus (+10%) |
| 3+ | Tier 3: Full + 5% secondary |

## Ascended Synergy Bonus
- ASCENDED monsters count as 2 for synergy stacking
- 1 Ascended synergy monster = Tier 2 bonus

## Player Alignment Synergy
- If player alignment 55%+ with monster's Brand: +15% additional synergy

---

# MONSTER MORALE SYSTEM

## Brand Beliefs

| Monster Brand | Approves Of | Disapproves Of |
|---------------|-------------|----------------|
| SAVAGE | Aggressive choices, no mercy | Showing weakness, retreating |
| IRON | Protecting allies, standing ground | Abandoning others, cowardice |
| VENOM | Clever solutions, deception | Brute force, honesty when lying helps |
| SURGE | Quick decisions, rushing in | Hesitation, lengthy planning |
| DREAD | Intimidation, fear tactics | Showing compassion to enemies |
| LEECH | Taking resources, self-preservation | Sacrifice, giving away power |

## Morale Levels

| Level | Trigger | Effect |
|-------|---------|--------|
| **Inspired** | 3+ approved choices recently | +10% stats, unique dialogue |
| **Content** | Neutral | Normal performance |
| **Conflicted** | 2+ disapproved choices | -5% stats, hesitant animations |
| **Rebellious** | 4+ disapproved choices | +10% Instability, may refuse Ascension |

## Morale Decay
- 1 choice worth of morale fades per 5 battles
- Resets to Content over time

---

# THE 5 MAJOR STORY CHOICES

## Choice 1: The Fallen Village
*A village is overrun. Save people OR recover ancient Brand artifact.*

| Option | Brand Effect | Consequence |
|--------|--------------|-------------|
| Save villagers | IRON +25, SAVAGE -10 | Villagers become allies |
| Take artifact | SAVAGE +20, LEECH +15, IRON -20 | Powerful equipment, haunted by ghosts |

## Choice 2: The Poisoned Well
*Enemy army approaches. Poison their water OR face them directly.*

| Option | Brand Effect | Consequence |
|--------|--------------|-------------|
| Poison them | VENOM +25, IRON -15, DREAD +10 | Easy victory, reputation suffers |
| Face them | IRON +20, SAVAGE +15, VENOM -10 | Hard battle, respected by NPCs |

## Choice 3: The Terrified Informant
*NPC has info but is too scared to talk.*

| Option | Brand Effect | Consequence |
|--------|--------------|-------------|
| Intimidate | DREAD +25, IRON -10 | Info gained, NPC becomes enemy later |
| Reassure | IRON +20, DREAD -15 | Slow info, NPC becomes ally |
| Bribe | LEECH +15, SURGE +10 | Quick info, costs resources |

## Choice 4: The Dying Companion
*Ally mortally wounded. Drain their life force OR let them die peacefully.*

| Option | Brand Effect | Consequence |
|--------|--------------|-------------|
| Drain them | LEECH +30, IRON -25 | Power boost, party morale drops |
| Mercy | IRON +25, LEECH -20 | Peaceful death, spiritual buff |
| Self-sacrifice | IRON +15, LEECH +10 | Lose 20% max HP permanently, ally survives |

## Choice 5: The Veil Fragment
*Find a piece of Vera's power.*

| Option | Brand Effect | Vera Effect |
|--------|--------------|-------------|
| Absorb it | LEECH +30, DREAD +20 | Vera hostile subtly, Veil Integrity -20 |
| Destroy it | IRON +25, SAVAGE +15 | Vera glitches HARD, Veil Integrity +15 |
| Give to Vera | DREAD +15, LEECH +10 | Vera "grateful"... too grateful. Ending affected. |

---

# VERA SYSTEM

## Veil Integrity (Hidden, 100 → 0)

| Action | Veil Integrity Change |
|--------|-----------------------|
| DOMINATE capture | -3 |
| BARGAIN capture | -5 |
| PURIFY capture | +1 |
| Ascend a monster | +3 |
| Dominated monster dies | -2 |
| Defeat boss | +5 |
| Give Vera power (Choice 5) | -20 |
| Destroy Vera's power (Choice 5) | +15 |

## Veil Integrity Effects

| Level | Vera's State |
|-------|--------------|
| 75-100 | Stable, helpful, human |
| 50-74 | Glitches more frequent, contradictory advice |
| 25-49 | Knows things she shouldn't, unmissable glitches |
| 0-24 | Speaks in plural ("We think..."), affects ending |

## Glitch Triggers

| Trigger | Glitch Type | Subtlety |
|---------|-------------|----------|
| DOMINATE used | Red pixel scatter (0.3s) | Very subtle |
| Multiple DOMINATEs | Bass undertone in voice | Subtle |
| PURIFY on Abyssal | No tactical advice for 2 turns | Subtle |
| BARGAIN accepted | Eyes wrong in next cutscene | Noticeable |
| Party corruption avg >60 | Shadow doesn't match pose | Creepy |
| Party corruption avg <20 | Passive-aggressive dialogue | Behavioral |
| Roll natural 1 on capture | Second voice whispers "interesting" | Very subtle |

## Vera Dialogue Reactions

### To Capture Methods
- **SOULBIND**: *"A leash made of someone else's chains."*
- **PURIFY**: *(voice skips)* *"You've... cleaned it."*
- **DOMINATE**: *(red flicker)* *"Now you're speaking a language worth hearing."*
- **BARGAIN**: *(freezes)* *"Bargains have... witnesses."*

### To Brand Alignment

| Dominant Brand | Vera Says |
|----------------|-----------|
| SAVAGE >60% | *"You've developed a taste for violence. Good."* |
| IRON >60% | *"Still playing the hero? How... exhausting."* |
| VENOM >60% | *"Clever. Underhanded. I approve."* |
| SURGE >60% | *"Always rushing. You'll miss something important."* |
| DREAD >60% | *"Fear is a weapon. You're learning."* *(wrong smile)* |
| LEECH >60% | *"Taking what you need. We're not so different."* *(glitch)* |

### To Ascension
When player Ascends monsters frequently:
- Visible discomfort
- Gives excuses to leave conversations
- Veil Integrity increases (she's weakened by purity)

---

# SCENARIO.GG API INTEGRATION

## Authentication
```
API Key: [SET IN ENVIRONMENT - SCENARIO_API_KEY]
Secret: [SET IN ENVIRONMENT - SCENARIO_SECRET]
Format: Basic Auth (base64 of "api_key:secret")
```

## Endpoints

### Generate Image (txt2img)
```
POST https://api.cloud.scenario.com/v1/generate/txt2img
Headers:
  Authorization: Basic <base64(api_key:secret)>
  Content-Type: application/json

Body:
{
  "modelId": "<model_id>",
  "prompt": "<prompt>",
  "negativePrompt": "<negative>",
  "negativePromptStrength": 0.7,
  "width": 512,
  "height": 512,
  "numImages": 1,
  "guidance": 7,
  "numInferenceSteps": 30
}
```

### Check Job Status
```
GET https://api.cloud.scenario.com/v1/jobs/<jobId>
```

### Get Asset
```
GET https://api.cloud.scenario.com/v1/assets/<assetId>
```

## Recommended Models for Battle Chasers Style

| Model ID | Name | Best For |
|----------|------|----------|
| model_HPBK2PFdRBKbyqfNV6wpsmk4 | Edgy Comic | Gritty characters |
| model_XwfVMqkagvJSVodYHCQcFkm3 | Vibrant Comic | Bold scenes |
| model_mzcBAJ1AtwgykkpmKUmmpY7o | Euro Comix | Hand-drawn style |
| model_LV55BM7fh16jQgXi4DCTamUe | Vibrant Heroics | Heroes |
| model_cruQJSR5YfnWBs6d7jM1rpTL | The Revengers | Comic characters |
| model_1Nhnp4C7eJShiFmSpvopxyUa | Legendary TCG 2.0 | Fantasy cards |

## Prompt Template for VEILBREAKERS

```
[subject], battle chasers art style, joe madureira, hand-painted 2D, 
dramatic lighting, bold black outlines, saturated colors, dark fantasy,
[brand-specific elements], game asset, transparent background
```

---

# QUICK REFERENCE

## Capture Flow
```
1. Battle → manipulate corruption via damage/waiting
2. Choose method based on corruption range
3. Corruption Battle animation (passes based on rarity)
4. Pull animation (method-specific)
5. Monster joins at current corruption state
6. Purify at Shrines to reduce corruption over time
7. Goal: Reach ASCENDED (0-10%) for max power
```

## Key Numbers

| Stat | Value |
|------|-------|
| Ascension threshold | 0-10% corruption |
| Equipment alignment requirement | 55%+ |
| Hybrid equipment requirement | 40%+ both |
| DOMINATE HP cost | 25% current HP |
| Shrine cooldown (Minor) | 3 battles |
| Morale decay | 1 choice per 5 battles |
| Instability removed at | <26% corruption |
| Equipment shatters after | 11+ battles below threshold |

## Bosses
- **NOT CATCHABLE**
- Drop Covenant Vessels (best Soul Vessel)
- Drop Brand Essences (evolution items)
- Drop Path Fragments (hero upgrades)

---

*This document is the source of truth for all game systems. Update version number when modified.*

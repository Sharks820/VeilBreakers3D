# VeilBreakers Monster Skill Specification v3
## Brand-Aligned Skills for All Monsters

---

## Skill Structure Standards

### 6-Slot Ability System
| Slot | Type | Cooldown | Purpose |
|------|------|----------|---------|
| 1 | Basic Attack | 0s | Primary damage, builds resource |
| 2 | Defend | 0s | Defense action, 50% damage reduction |
| 3 | Skill 1 | 4-6s | Core brand identity skill |
| 4 | Skill 2 | 10-15s | Secondary brand expression |
| 5 | Skill 3 | 18-25s | Utility/CC/survival |
| 6 | Ultimate | 45-90s | Powerful brand culmination |

### Skill Type Enum
- 0 = ATTACK (damage dealing)
- 1 = DEFENSE (protection)
- 2 = BUFF (self/ally enhancement)
- 3 = DEBUFF (enemy weakening)
- 4 = HEAL (restoration)
- 5 = UTILITY (special mechanics)
- 6 = ULTIMATE (powerful once-per-battle effects)

---

## Monster Skill Specifications

### 1. BLOODSHADE (VOID + DREAD)
**Visual:** Shadow pool, shifting darkness, multiple whispering voices
**Lore:** Collects endings, speaks with voices of the drained

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | shadow_touch | Shadow Touch | 0 | Drain HP + small fear chance |
| 2 | void_phase | Void Phase | 2 | 50% dodge, can pass through enemies |
| 3 | choir_of_dying | Choir of Dying | 3 | AOE fear + random debuff |
| 4 | soulthirst | Soulthirst | 0 | Heavy drain, heal 100% of damage |
| 5 | stolen_vitality | Stolen Vitality | 2 | Drain stat from enemy, buff self |
| 6 | the_final_drink | The Final Drink | 6 | Execute below 30% HP, full heal |

---

### 2. CHAINBOUND (IRON + CORROSIVE)
**Visual:** Rusted chains, padlock heart, binding movement
**Lore:** Imprisoned something terrible, now binds everything

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | chain_lash | Chain Lash | 0 | Mid damage + bind (root) |
| 2 | rusted_embrace | Rusted Embrace | 1 | Shield + thorns (damage attackers) |
| 3 | shackle_slam | Shackle Slam | 0 | Heavy damage + stun |
| 4 | chain_wall | Chain Wall | 2 | Protect ally, redirect damage |
| 5 | the_wardens_grip | The Warden's Grip | 3 | Mass bind in AOE |
| 6 | eternal_imprisonment | Eternal Imprisonment | 6 | Trap enemy (can't act/die for 5s) |

---

### 3. VENOMKNIGHT (VENOM + IRON) - REBRANDED
**Visual:** Acid-dripping armor, bubbling vents, corroded plates
**Lore:** Guards nothing but its own toxic existence

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | venomous_jab | Venomous Jab | 0 | Damage + poison stack |
| 2 | acid_carapace | Acid Carapace | 1 | Shield + poison thorns |
| 3 | corroding_guard | Corroding Guard | 2 | DEF buff + poison AOE around self |
| 4 | toxic_embrace | Toxic Embrace | 0 | Grab + heavy poison DOT |
| 5 | acid_flood | Acid Flood | 3 | Ground AOE, poison + slow |
| 6 | the_dissolved_oath | The Dissolved Oath | 6 | Mass poison + self fortify |

---

### 4. CRACKLING (SURGE + CORROSIVE)
**Visual:** Child-sized lightning, forking electricity, flickering
**Lore:** Doesn't understand why touching hurts

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | static_touch | Static Touch | 0 | Fast lightning damage |
| 2 | flicker_dodge | Flicker Dodge | 2 | Phase out (untargetable) 2s |
| 3 | chain_lightning | Chain Lightning | 0 | Hits 3 targets, bounces |
| 4 | surge_dash | Surge Dash | 0 | Dash through enemy + damage |
| 5 | stormkin_call | Stormkin Call | 2 | Summon lightning clone (attacks once) |
| 6 | tempest_incarnate | Tempest Incarnate | 6 | Transform, all attacks chain + stun |

---

### 5. FLICKER (SURGE)
**Visual:** Motion blur, layered wings, insectoid speed
**Lore:** Cannot stop - stillness is death

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | blur_strike | Blur Strike | 0 | Fast attack + evasion buff |
| 2 | phase_shift | Phase Shift | 2 | Dodge next attack + counter |
| 3 | between_seconds | Between Seconds | 0 | Attack twice, reduced damage each |
| 4 | interrupt | Interrupt | 0 | Damage + cancel enemy action |
| 5 | hit_and_run | Hit and Run | 0 | Attack + reposition + speed buff |
| 6 | quicksilver_blitz | Quicksilver Blitz | 6 | 5 rapid attacks, each applies bleed |

---

### 6. GLUTTONY POLYP (LEECH + DREAD)
**Visual:** Translucent sac, digesting contents, floating
**Lore:** Drains will to live while "healing"

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | hungering_touch | Hungering Touch | 0 | Drain HP + heal self |
| 2 | bloat | Bloat | 2 | Max HP boost + damage reduction |
| 3 | nurturing_drain | Nurturing Drain | 4 | Ally heal + drain enemy HP |
| 4 | consume_pain | Consume Pain | 4 | Absorb ally damage as healing |
| 5 | life_returned | Life Returned | 4 | Strong heal, costs enemy HP |
| 6 | devouring_care | Devouring Care | 6 | Full party heal + execute low-HP enemy |

---

### 7. GRIMTHORN (SAVAGE)
**Visual:** Thorny vines, dripping toxin, barbed whips
**Lore:** Whispers last words of consumed victims

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | poison_sting | Poison Sting | 0 | Damage + poison |
| 2 | thorn_barrier | Thorn Barrier | 1 | Shield + counter-attack |
| 3 | vine_whip | Vine Whip | 0 | Ranged damage + pull |
| 4 | toxic_spores | Toxic Spores | 3 | AOE poison cloud |
| 5 | venomous_bloom | Venomous Bloom | 0 | Heavy DOT burst |
| 6 | natures_wrath | Nature's Wrath | 6 | Mass entangle + poison explosion |

---

### 8. THE HOLLOW VOID (VOID + DREAD) - REBRANDED
**Visual:** Reality tear, shadow tendrils, chaotic energy
**Lore:** Possibilities that never were press against its surface

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | void_rend | Void Rend | 0 | Damage + random debuff |
| 2 | phase_out | Phase Out | 2 | 50% dodge + heal |
| 3 | consuming_vortex | Consuming Vortex | 0 | Pull enemies + drain |
| 4 | dread_surge | Dread Surge | 2 | ATK buff + fear aura |
| 5 | void_touch | Void Touch | 3 | Ignore defense + chaos effect |
| 6 | abyssal_unraveling | Abyssal Unraveling | 6 | Mass damage + random chaos effects |

---

### 9. IRONJAW (LEECH + IRON)
**Visual:** Bear with metal trap jaw, blackened plates
**Lore:** Every bite is permanent - it collects

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | trap_bite | Trap Bite | 0 | Damage + bleed + heal |
| 2 | iron_guard | Iron Guard | 1 | Shield + thorns |
| 3 | iron_charge | Iron Charge | 0 | Charge + stun |
| 4 | steelrend | Steelrend | 0 | Armor shred + drain |
| 5 | berserker_plate | Berserker Plate | 2 | ATK buff + DEF trade |
| 6 | unstoppable_maw | Unstoppable Maw | 6 | Execute + full heal + ATK buff |

---

### 10. MAWLING (LEECH) - REBRANDED
**Visual:** Floating mouth, dark matter, single eye
**Lore:** Hunger is instructive - develops loyalty

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | gnaw | Gnaw | 0 | Damage + small heal |
| 2 | desperate_lunge | Desperate Lunge | 0 | Gap close + heal |
| 3 | frenzy_bite | Frenzy Bite | 0 | Multi-hit + heal per hit |
| 4 | devour_scraps | Devour Scraps | 2 | Consume corpse to heal |
| 5 | pack_feast | Pack Feast | 2 | Drain from enemy, heal allies |
| 6 | eternal_hunger | Eternal Hunger | 6 | Mass drain + permanent growth buff |

---

### 11. NEEDLEFANG (VENOM + SURGE) - REBRANDED
**Visual:** Needle-spine serpent, vibrating spines, venomous
**Lore:** Fires needles in millisecond bursts

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | needle_storm | Needle Storm | 0 | Rapid hits + poison |
| 2 | venomous_acceleration | Venomous Acceleration | 2 | Speed + evasion buff |
| 3 | spine_flurry | Spine Flurry | 0 | Multi-hit + poison stacks |
| 4 | toxin_overdrive | Toxin Overdrive | 2 | Speed buff + poison spread on hit |
| 5 | thousand_stings | Thousand Stings | 0 | 10 rapid hits, each applies poison |
| 6 | death_of_a_thousand_cuts | Death of a Thousand Cuts | 6 | Detonate all poison stacks on all enemies |

---

### 12. RAVENER (SAVAGE + VOID) - REBRANDED
**Visual:** Predator amalgam, six eyes, fused wrong
**Lore:** Hunts for joy of the chase

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | rend | Rend | 0 | Damage + bleed |
| 2 | bloodscent | Bloodscent | 2 | Mark low-HP enemy + buff vs them |
| 3 | pounce | Pounce | 0 | Gap close + stun |
| 4 | prey_on_weak | Prey on Weak | 0 | Bonus damage vs debuffed |
| 5 | pack_tactics | Pack Tactics | 2 | Buff allies + coordinate attacks |
| 6 | apex_strike | Apex Strike | 6 | Execute + ignore defense + overkill heals |

---

### 13. SKITTER-TEETH (IRON)
**Visual:** Bone cage, finger-legs, splitting jaw
**Lore:** Collects bones, builds itself from remains

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | bone_snap | Bone Snap | 0 | Damage + high crit |
| 2 | harvest | Harvest | 2 | Heal from corpse |
| 3 | leg_trap | Leg Trap | 3 | Root enemy |
| 4 | bone_wall | Bone Wall | 1 | Protect ally |
| 5 | reassemble | Reassemble | 2 | Self-heal + cleanse |
| 6 | final_form | Final Form | 6 | Massive buff + passive revive once |

---

### 14. SPORECALLER (SAVAGE + CORROSIVE)
**Visual:** Fungal deer, bioluminescent spores, broken legs
**Lore:** Doesn't know it died seasons ago

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | spore_cloud | Spore Cloud | 0 | AOE poison |
| 2 | fungal_touch | Fungal Touch | 4 | Heal ally + poison enemy |
| 3 | decomposer | Decomposer | 3 | DOT + weaken |
| 4 | bloom_of_rot | Bloom of Rot | 0 | Mass poison field |
| 5 | mycelium_network | Mycelium Network | 2 | Link allies, shared healing |
| 6 | the_rot_sovereign | The Rot Sovereign | 6 | Mass poison + summon spore minions |

---

### 15. THE BROODMOTHER (SAVAGE + CORROSIVE)
**Visual:** Spider-wasp hybrid, visible larvae, translucent chitin
**Lore:** Maternal instinct twisted, wants children to thrive

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | parasitic_injection | Parasitic Injection | 0 | Damage + summon egg (hatches if target dies) |
| 2 | spawn_broodling | Spawn Broodling | 5 | Summon minion |
| 3 | venom_spray | Venom Spray | 3 | Cone poison |
| 4 | venomous_nursery | Venomous Nursery | 2 | Buff minions + poison AOE |
| 5 | infestation_wave | Infestation Wave | 3 | Mass poison + slow |
| 6 | hive_queen_ascension | Hive Queen Ascension | 6 | Transform + mass spawn + poison aura |

---

### 16. THE BULWARK (IRON + CORROSIVE)
**Visual:** Fused armor, welded helmet, dragging chains
**Lore:** Meant to protect something, now only endures

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | iron_stance | Iron Stance | 0 | Damage + self-DEF buff |
| 2 | guardian_chains | Guardian Chains | 1 | Protect ally, take their damage |
| 3 | fortress_body | Fortress Body | 2 | Massive DEF buff |
| 4 | immovable_will | Immovable Will | 2 | Taunt + heal |
| 5 | bastion_form | Bastion Form | 2 | Party DEF buff |
| 6 | citadel_eternal | Citadel Eternal | 6 | Party invulnerability 3s + heal |

---

### 17. THE CONGREGATION (DREAD) - BOSS
**Visual:** Pillar of fused bodies, reaching faces, 15-foot tall
**Lore:** Introduces itself using all their names

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | the_smiths_hammer | The Smith's Hammer | 0 | Heavy single-target |
| 2 | the_elders_wisdom | The Elder's Wisdom | 2 | Buff + heal |
| 3 | the_mothers_embrace | The Mother's Embrace | 0 | Grab + DOT |
| 4 | chorus_of_names | Chorus of Names | 3 | Mass fear + debuff |
| 5 | desperate_consumption | Desperate Consumption | 0 | High damage, may self-damage (phase 3) |
| 6 | final_absolution | Final Absolution | 6 | Phase transition + mass effect |

---

### 18. THE VESSEL (DREAD + MEND) - REBRANDED
**Visual:** Floating robes, open chest cavity, stolen light
**Lore:** Shields are cocoons - consumptive protection

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | stolen_grace | Stolen Grace | 0 | Damage + grant ally shield |
| 2 | merciful_cocoon | Merciful Cocoon | 1 | Large shield + slow heal |
| 3 | absorb_suffering | Absorb Suffering | 1 | Take ally damage + heal |
| 4 | blessed_barrier | Blessed Barrier | 2 | Party shields |
| 5 | sanctuary | Sanctuary | 2 | Party cleanse + small shield |
| 6 | divine_devotion | Divine Devotion | 6 | Full party shield + cleanse + revive fallen |

---

### 19. THE WEEPING (VENOM)
**Visual:** Cluster of eyes, red threads, crying dark fluid
**Lore:** Every eye carries fragments of last sight

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | witnessed | Witnessed | 0 | Mark + vulnerability |
| 2 | many_eyes | Many Eyes | 2 | Evasion + counter |
| 3 | glimpse_of_death | Glimpse of Death | 3 | Fear + damage debuff |
| 4 | many_eyed_gaze | Many-Eyed Gaze | 3 | Multi-debuff |
| 5 | shared_vision | Shared Vision | 2 | Reveal enemy + buff allies |
| 6 | absolute_vision | Absolute Vision | 6 | Mass expose + cannot evade + execute threshold up |

---

### 20. VOLTGEIST (RUIN + SURGE) - REBRANDED
**Visual:** Lightning ghost, scorched afterimages, burning
**Lore:** Explosions follow where it touches

**Skill Set:**
| Slot | Skill ID | Name | Type | Effect |
|------|----------|------|------|--------|
| 1 | death_vision | Death Vision | 0 | Damage + fear |
| 2 | lightning_haunt | Lightning Haunt | 2 | Phase + shock aura |
| 3 | storm_terror | Storm Terror | 0 | AOE damage + fear |
| 4 | inevitable_strike | Inevitable Strike | 0 | Guaranteed crit + explosion |
| 5 | thunderwraith_scream | Thunderwraith Scream | 3 | Mass stun + damage |
| 6 | the_storm_remembers | The Storm Remembers | 6 | Massive AOE + DOT field + self-respawn on death |

---

## New Skills to Create

### VENOMKNIGHT (Corrodex replacement)
- `venomous_jab` - Basic poison attack
- `acid_carapace` - Shield with poison thorns
- `corroding_guard` - DEF buff + poison AOE
- `toxic_embrace` - Grab + heavy poison
- `acid_flood` - Ground AOE poison
- `the_dissolved_oath` - Ultimate: mass poison + fortify

### THE HOLLOW VOID (Hollow replacement)
- `void_rend` - Damage + random debuff
- `phase_out` - Dodge + heal
- `dread_surge` - ATK buff + fear aura (exists, may need adjust)
- `void_touch` - Ignore defense + chaos
- `abyssal_unraveling` - Ultimate: mass chaos

### MAWLING
- `pack_feast` - Team drain heal
- `eternal_hunger` - Ultimate: mass drain + growth

### RAVENER
- Skills mostly exist, verify brand alignment

### NEEDLEFANG
- `death_of_a_thousand_cuts` - Ultimate: poison detonation

### THE VESSEL
- `merciful_cocoon` - Shield + heal
- `blessed_barrier` - Party shields

### VOLTGEIST
- `thunderwraith_scream` - Mass stun
- `the_storm_remembers` - Ultimate: AOE field + respawn

---

## Balance Guidelines

### Base Power by Tier
| Tier | Basic | Skill 1 | Skill 2 | Skill 3 | Ultimate |
|------|-------|---------|---------|---------|----------|
| 0 | 10-12 | 15-18 | 20-25 | 25-30 | 40-50 |
| 1 | 12-15 | 18-22 | 25-30 | 30-35 | 50-60 |
| 2 | 15-18 | 22-28 | 30-38 | 38-45 | 60-75 |
| 3+ | 18-22 | 28-35 | 38-45 | 45-55 | 75-100 |

### Cooldown Standards
| Skill Type | Cooldown (s) |
|------------|--------------|
| Basic | 0 |
| Defend | 0 |
| Skill 1 | 4-6 |
| Skill 2 | 10-15 |
| Skill 3 | 18-25 |
| Ultimate | 60-90 |

### MP Costs
| Tier | Skill 1 | Skill 2 | Skill 3 | Ultimate |
|------|---------|---------|---------|----------|
| 0 | 5-8 | 10-15 | 15-20 | 25-35 |
| 1 | 8-12 | 15-20 | 20-25 | 35-45 |
| 2 | 12-15 | 20-25 | 25-35 | 45-60 |

---

*Skill Specification v3 - All monsters mapped to brand-appropriate skills*

# VeilBreakers3D Combat System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the complete real-time tactical combat system with 10-brand effectiveness, tiered synergy, and 6-slot abilities.

**Architecture:** Unity C# with static systems for calculations, MonoBehaviour managers for runtime, ScriptableObjects for data. Event-driven communication via EventBus.

**Tech Stack:** Unity 2022+, C# 10, UniTask (async), DOTween (animations)

---

## Phase 1: Update Brand System to 10-Brand

### Task 1.1: Update Brand Enum

**Files:**
- Modify: `Assets/Scripts/Data/Enums.cs:14-36`

**Step 1: Replace Brand enum with 10-brand system**

```csharp
public enum Brand
{
    NONE = 0,

    // The 10 Brands
    IRON = 1,       // Tank - Defensive Wall
    SAVAGE = 2,     // Melee Burst - Berserker
    SURGE = 3,      // Ranged DPS - Artillery
    VENOM = 4,      // DoT/Debuff - Poison Master
    DREAD = 5,      // CC/Terror - Fear Mage
    LEECH = 6,      // Drain Tank - Lifesteal Bruiser
    GRACE = 7,      // Battle Healer - Combat Medic
    MEND = 8,       // Ward Healer - Shield Support
    RUIN = 9,       // AOE Devastator - Explosion Mage
    VOID = 10       // Chaos Mage - Reality Warper
}
```

**Step 2: Remove BrandTier enum (no longer needed)**

Delete lines 38-44.

**Step 3: Commit**

```bash
git add Assets/Scripts/Data/Enums.cs
git commit -m "feat: update Brand enum to 10-brand system"
```

---

### Task 1.2: Update BrandSystem Effectiveness Matrix

**Files:**
- Modify: `Assets/Scripts/Systems/BrandSystem.cs`

**Step 1: Replace entire BrandSystem.cs with new 10-brand matrix**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Brand System - 10 Brands with 2x/0.5x effectiveness matrix
    /// Each brand is strong against 2, weak against 2, neutral against 6
    /// </summary>
    public static class BrandSystem
    {
        public const float SUPER_EFFECTIVE = 2.0f;
        public const float NOT_EFFECTIVE = 0.5f;
        public const float NEUTRAL = 1.0f;

        // Effectiveness matrix: Attacker -> (Strong against, Weak against)
        private static readonly Dictionary<Brand, (Brand[] strong, Brand[] weak)> EffectivenessMatrix =
            new Dictionary<Brand, (Brand[], Brand[])>
        {
            { Brand.IRON,   (new[] { Brand.SURGE, Brand.DREAD }, new[] { Brand.SAVAGE, Brand.RUIN }) },
            { Brand.SAVAGE, (new[] { Brand.IRON, Brand.MEND },   new[] { Brand.LEECH, Brand.GRACE }) },
            { Brand.SURGE,  (new[] { Brand.VENOM, Brand.LEECH }, new[] { Brand.IRON, Brand.VOID }) },
            { Brand.VENOM,  (new[] { Brand.GRACE, Brand.MEND },  new[] { Brand.SURGE, Brand.RUIN }) },
            { Brand.DREAD,  (new[] { Brand.SAVAGE, Brand.GRACE },new[] { Brand.IRON, Brand.VOID }) },
            { Brand.LEECH,  (new[] { Brand.SAVAGE, Brand.RUIN }, new[] { Brand.SURGE, Brand.VENOM }) },
            { Brand.GRACE,  (new[] { Brand.VOID, Brand.RUIN },   new[] { Brand.SAVAGE, Brand.VENOM }) },
            { Brand.MEND,   (new[] { Brand.VOID, Brand.LEECH },  new[] { Brand.SAVAGE, Brand.VENOM }) },
            { Brand.RUIN,   (new[] { Brand.IRON, Brand.VENOM },  new[] { Brand.LEECH, Brand.GRACE }) },
            { Brand.VOID,   (new[] { Brand.SURGE, Brand.DREAD }, new[] { Brand.GRACE, Brand.MEND }) }
        };

        /// <summary>
        /// Get damage multiplier between attacker and defender brands
        /// </summary>
        public static float GetEffectiveness(Brand attacker, Brand defender)
        {
            if (attacker == Brand.NONE || defender == Brand.NONE)
                return NEUTRAL;

            if (!EffectivenessMatrix.TryGetValue(attacker, out var matrix))
                return NEUTRAL;

            foreach (var strong in matrix.strong)
            {
                if (strong == defender) return SUPER_EFFECTIVE;
            }

            foreach (var weak in matrix.weak)
            {
                if (weak == defender) return NOT_EFFECTIVE;
            }

            return NEUTRAL;
        }

        /// <summary>
        /// Check if attacker has advantage over defender
        /// </summary>
        public static bool HasAdvantage(Brand attacker, Brand defender)
        {
            return GetEffectiveness(attacker, defender) >= SUPER_EFFECTIVE;
        }

        /// <summary>
        /// Check if attacker has disadvantage against defender
        /// </summary>
        public static bool HasDisadvantage(Brand attacker, Brand defender)
        {
            return GetEffectiveness(attacker, defender) <= NOT_EFFECTIVE;
        }

        /// <summary>
        /// Get brand color for UI
        /// </summary>
        public static Color GetBrandColor(Brand brand)
        {
            return brand switch
            {
                Brand.IRON =>   new Color(0.6f, 0.6f, 0.7f),    // Steel gray
                Brand.SAVAGE => new Color(0.9f, 0.2f, 0.1f),    // Blood red
                Brand.SURGE =>  new Color(0.2f, 0.6f, 0.95f),   // Electric blue
                Brand.VENOM =>  new Color(0.3f, 0.8f, 0.2f),    // Toxic green
                Brand.DREAD =>  new Color(0.5f, 0.2f, 0.6f),    // Dark purple
                Brand.LEECH =>  new Color(0.6f, 0.1f, 0.3f),    // Crimson
                Brand.GRACE =>  new Color(1f, 0.95f, 0.7f),     // Warm gold
                Brand.MEND =>   new Color(0.4f, 0.9f, 0.9f),    // Cyan
                Brand.RUIN =>   new Color(0.95f, 0.5f, 0.1f),   // Orange flame
                Brand.VOID =>   new Color(0.2f, 0.1f, 0.3f),    // Deep void
                _ => Color.white
            };
        }

        /// <summary>
        /// Get brand display name
        /// </summary>
        public static string GetBrandName(Brand brand)
        {
            return brand.ToString().Substring(0, 1) + brand.ToString().Substring(1).ToLower();
        }

        /// <summary>
        /// Get brand archetype description
        /// </summary>
        public static string GetBrandArchetype(Brand brand)
        {
            return brand switch
            {
                Brand.IRON =>   "Defensive Wall",
                Brand.SAVAGE => "Berserker",
                Brand.SURGE =>  "Artillery",
                Brand.VENOM =>  "Poison Master",
                Brand.DREAD =>  "Fear Mage",
                Brand.LEECH =>  "Lifesteal Bruiser",
                Brand.GRACE =>  "Combat Medic",
                Brand.MEND =>   "Shield Support",
                Brand.RUIN =>   "Explosion Mage",
                Brand.VOID =>   "Reality Warper",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get effectiveness text for UI
        /// </summary>
        public static string GetEffectivenessText(float multiplier)
        {
            if (multiplier >= SUPER_EFFECTIVE) return "Super Effective! (2x)";
            if (multiplier <= NOT_EFFECTIVE) return "Not Very Effective... (0.5x)";
            return "";
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Systems/BrandSystem.cs
git commit -m "feat: implement 10-brand effectiveness matrix (2x/0.5x)"
```

---

## Phase 2: Implement Tiered Synergy System

### Task 2.1: Create SynergySystem.cs

**Files:**
- Create: `Assets/Scripts/Systems/SynergySystem.cs`

**Step 1: Create the synergy system**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Tiered Synergy System
    /// Full (3/3): +8%/+8%, 0.5x corruption, combo unlocked
    /// Partial (2/3): +5%/+5%, 0.75x corruption
    /// Neutral (0-1/3): No bonus
    /// Anti (weak brands): 1.5x corruption per weak brand
    /// </summary>
    public static class SynergySystem
    {
        // Synergy tier definitions
        public enum SynergyTier
        {
            NONE,
            ANTI,
            NEUTRAL,
            PARTIAL,
            FULL
        }

        // Path -> Strong synergy brands
        private static readonly Dictionary<Path, Brand[]> PathSynergyBrands = new Dictionary<Path, Brand[]>
        {
            { Path.IRONBOUND,   new[] { Brand.IRON, Brand.MEND, Brand.LEECH } },
            { Path.FANGBORN,    new[] { Brand.SAVAGE, Brand.VENOM, Brand.RUIN } },
            { Path.VOIDTOUCHED, new[] { Brand.VOID, Brand.DREAD, Brand.SURGE } },
            { Path.UNCHAINED,   new Brand[] { } }  // All neutral
        };

        // Path -> Weak synergy brands (cause faster corruption)
        private static readonly Dictionary<Path, Brand[]> PathWeakBrands = new Dictionary<Path, Brand[]>
        {
            { Path.IRONBOUND,   new[] { Brand.VOID, Brand.SAVAGE, Brand.RUIN } },
            { Path.FANGBORN,    new[] { Brand.GRACE, Brand.MEND, Brand.IRON } },
            { Path.VOIDTOUCHED, new[] { Brand.IRON, Brand.GRACE, Brand.MEND } },
            { Path.UNCHAINED,   new Brand[] { } }  // No weakness
        };

        /// <summary>
        /// Calculate synergy tier for a party composition
        /// </summary>
        public static SynergyTier GetSynergyTier(Path championPath, Brand[] partyBrands)
        {
            if (championPath == Path.NONE || partyBrands == null || partyBrands.Length == 0)
                return SynergyTier.NONE;

            // UNCHAINED path is always neutral (flex path)
            if (championPath == Path.UNCHAINED)
                return SynergyTier.NEUTRAL;

            // Check for weak brands (anti-synergy)
            if (PathWeakBrands.TryGetValue(championPath, out var weakBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (weakBrands.Contains(brand))
                        return SynergyTier.ANTI;
                }
            }

            // Count strong synergy matches
            int matchCount = 0;
            if (PathSynergyBrands.TryGetValue(championPath, out var strongBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (strongBrands.Contains(brand))
                        matchCount++;
                }
            }

            // Determine tier based on match count
            return matchCount switch
            {
                3 => SynergyTier.FULL,
                2 => SynergyTier.PARTIAL,
                _ => SynergyTier.NEUTRAL
            };
        }

        /// <summary>
        /// Get damage bonus multiplier for synergy tier
        /// </summary>
        public static float GetDamageBonus(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 1.08f,     // +8%
                SynergyTier.PARTIAL => 1.05f,  // +5%
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get defense bonus multiplier for synergy tier
        /// </summary>
        public static float GetDefenseBonus(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 1.08f,     // +8%
                SynergyTier.PARTIAL => 1.05f,  // +5%
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get corruption rate multiplier for synergy tier
        /// </summary>
        public static float GetCorruptionRateMultiplier(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 0.5f,      // Half corruption gain
                SynergyTier.PARTIAL => 0.75f,  // 75% corruption gain
                SynergyTier.ANTI => 1.5f,      // 150% corruption gain
                _ => 1.0f
            };
        }

        /// <summary>
        /// Check if combo ability is available
        /// </summary>
        public static bool IsComboUnlocked(SynergyTier tier)
        {
            return tier == SynergyTier.FULL;
        }

        /// <summary>
        /// Get synergy tier display name
        /// </summary>
        public static string GetTierName(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => "Full Synergy",
                SynergyTier.PARTIAL => "Partial Synergy",
                SynergyTier.ANTI => "Anti-Synergy",
                SynergyTier.NEUTRAL => "Neutral",
                _ => "None"
            };
        }

        /// <summary>
        /// Get synergy tier color for UI
        /// </summary>
        public static Color GetTierColor(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => new Color(0.2f, 0.9f, 0.3f),    // Green
                SynergyTier.PARTIAL => new Color(0.9f, 0.8f, 0.2f), // Yellow
                SynergyTier.ANTI => new Color(0.9f, 0.2f, 0.2f),    // Red
                _ => Color.gray
            };
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Systems/SynergySystem.cs
git commit -m "feat: implement tiered synergy system (full/partial/neutral/anti)"
```

---

## Phase 3: Create 6-Slot Ability System

### Task 3.1: Create AbilitySlot Enum and Data Structures

**Files:**
- Modify: `Assets/Scripts/Data/Enums.cs` (add new enums)
- Create: `Assets/Scripts/Data/AbilityData.cs`

**Step 1: Add AbilitySlot enum to Enums.cs**

Add after SkillType enum:

```csharp
// =============================================================================
// 6-SLOT ABILITY SYSTEM
// =============================================================================

public enum AbilitySlot
{
    BASIC_ATTACK = 0,   // Slot 1 - No cooldown
    DEFEND = 1,         // Slot 2 - No cooldown
    SKILL_1 = 2,        // Slot 3 - 4-6s cooldown
    SKILL_2 = 3,        // Slot 4 - 10-15s cooldown
    SKILL_3 = 4,        // Slot 5 - 18-25s cooldown
    ULTIMATE = 5        // Slot 6 - 45-90s cooldown
}

public enum DefenseAction
{
    DEFEND_SELF,        // 50% damage reduction
    GUARD_ALLY,         // Intercept for ally, 75% damage to self
    GUARD_CHAMPION      // Full intercept for champion, 100% damage to self
}
```

**Step 2: Create AbilityData.cs**

```csharp
using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Runtime ability instance with cooldown tracking
    /// </summary>
    [Serializable]
    public class AbilityInstance
    {
        public string skillId;
        public AbilitySlot slot;
        public float cooldownRemaining;
        public float maxCooldown;
        public bool isReady => cooldownRemaining <= 0f;

        public AbilityInstance(string skillId, AbilitySlot slot, float maxCooldown)
        {
            this.skillId = skillId;
            this.slot = slot;
            this.maxCooldown = maxCooldown;
            this.cooldownRemaining = 0f;
        }

        public void TriggerCooldown()
        {
            cooldownRemaining = maxCooldown;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
            }
        }

        public float GetCooldownPercent()
        {
            if (maxCooldown <= 0f) return 0f;
            return cooldownRemaining / maxCooldown;
        }
    }

    /// <summary>
    /// 6-slot ability loadout for a combatant
    /// </summary>
    [Serializable]
    public class AbilityLoadout
    {
        public AbilityInstance basicAttack;
        public AbilityInstance defend;
        public AbilityInstance skill1;
        public AbilityInstance skill2;
        public AbilityInstance skill3;
        public AbilityInstance ultimate;

        // Current defense action selection
        public DefenseAction currentDefenseAction = DefenseAction.DEFEND_SELF;

        public AbilityInstance GetAbility(AbilitySlot slot)
        {
            return slot switch
            {
                AbilitySlot.BASIC_ATTACK => basicAttack,
                AbilitySlot.DEFEND => defend,
                AbilitySlot.SKILL_1 => skill1,
                AbilitySlot.SKILL_2 => skill2,
                AbilitySlot.SKILL_3 => skill3,
                AbilitySlot.ULTIMATE => ultimate,
                _ => null
            };
        }

        public void UpdateAllCooldowns(float deltaTime)
        {
            basicAttack?.UpdateCooldown(deltaTime);
            defend?.UpdateCooldown(deltaTime);
            skill1?.UpdateCooldown(deltaTime);
            skill2?.UpdateCooldown(deltaTime);
            skill3?.UpdateCooldown(deltaTime);
            ultimate?.UpdateCooldown(deltaTime);
        }

        /// <summary>
        /// Create default loadout from monster skills
        /// </summary>
        public static AbilityLoadout CreateFromSkills(string basicId, string skill1Id, string skill2Id, string skill3Id, string ultimateId)
        {
            return new AbilityLoadout
            {
                basicAttack = new AbilityInstance(basicId, AbilitySlot.BASIC_ATTACK, 0f),
                defend = new AbilityInstance("defend", AbilitySlot.DEFEND, 0f),
                skill1 = new AbilityInstance(skill1Id, AbilitySlot.SKILL_1, 5f),
                skill2 = new AbilityInstance(skill2Id, AbilitySlot.SKILL_2, 12f),
                skill3 = new AbilityInstance(skill3Id, AbilitySlot.SKILL_3, 20f),
                ultimate = new AbilityInstance(ultimateId, AbilitySlot.ULTIMATE, 60f)
            };
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Data/Enums.cs Assets/Scripts/Data/AbilityData.cs
git commit -m "feat: add 6-slot ability system data structures"
```

---

## Phase 4: Create Combat Foundation

### Task 4.1: Create Combatant.cs (Base Class)

**Files:**
- Create: `Assets/Scripts/Combat/Combatant.cs`

**Step 1: Create Combatant base class**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Base class for all combat participants (monsters, heroes)
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string _combatantId;
        [SerializeField] private string _displayName;
        [SerializeField] private Brand _brand = Brand.NONE;
        [SerializeField] private bool _isPlayerControlled = true;

        [Header("Stats")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _currentHp = 100;
        [SerializeField] private int _maxMp = 50;
        [SerializeField] private int _currentMp = 50;
        [SerializeField] private int _attack = 10;
        [SerializeField] private int _defense = 10;
        [SerializeField] private int _magic = 10;
        [SerializeField] private int _resistance = 10;
        [SerializeField] private int _speed = 10;

        [Header("State")]
        [SerializeField] private bool _isAlive = true;
        [SerializeField] private bool _isDefending = false;
        [SerializeField] private Combatant _guardTarget = null;

        // Abilities
        public AbilityLoadout Abilities { get; private set; }

        // Status effects
        private List<StatusEffectInstance> _statusEffects = new List<StatusEffectInstance>();

        // Properties
        public string CombatantId => _combatantId;
        public string DisplayName => _displayName;
        public Brand Brand => _brand;
        public bool IsPlayerControlled => _isPlayerControlled;
        public bool IsAlive => _isAlive;
        public bool IsDefending => _isDefending;
        public Combatant GuardTarget => _guardTarget;

        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public int MaxMp => _maxMp;
        public int CurrentMp => _currentMp;
        public int Attack => _attack;
        public int Defense => _defense;
        public int Magic => _magic;
        public int Resistance => _resistance;
        public int Speed => _speed;

        public float HpPercent => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;
        public float MpPercent => _maxMp > 0 ? (float)_currentMp / _maxMp : 0f;

        // Events
        public event Action<int, int> OnHpChanged;          // current, max
        public event Action<int, int> OnMpChanged;          // current, max
        public event Action<int, bool> OnDamageReceived;    // amount, isCritical
        public event Action<int> OnHealed;                   // amount
        public event Action OnDeath;
        public event Action OnRevive;

        /// <summary>
        /// Initialize combatant with data
        /// </summary>
        public void Initialize(string id, string name, Brand brand, int maxHp, int maxMp,
            int atk, int def, int mag, int res, int spd, bool isPlayer)
        {
            _combatantId = id;
            _displayName = name;
            _brand = brand;
            _maxHp = maxHp;
            _currentHp = maxHp;
            _maxMp = maxMp;
            _currentMp = maxMp;
            _attack = atk;
            _defense = def;
            _magic = mag;
            _resistance = res;
            _speed = spd;
            _isPlayerControlled = isPlayer;
            _isAlive = true;

            // Initialize default abilities (will be set properly by spawner)
            Abilities = new AbilityLoadout();
        }

        /// <summary>
        /// Set ability loadout
        /// </summary>
        public void SetAbilities(AbilityLoadout loadout)
        {
            Abilities = loadout;
        }

        /// <summary>
        /// Take damage (after all calculations)
        /// </summary>
        public void TakeDamage(int amount, bool isCritical = false)
        {
            if (!_isAlive) return;

            // Apply defense reduction if defending
            if (_isDefending)
            {
                amount = Mathf.RoundToInt(amount * 0.5f);
            }

            _currentHp = Mathf.Max(0, _currentHp - amount);
            OnDamageReceived?.Invoke(amount, isCritical);
            OnHpChanged?.Invoke(_currentHp, _maxHp);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal HP
        /// </summary>
        public void Heal(int amount)
        {
            if (!_isAlive) return;

            int previousHp = _currentHp;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            int actualHeal = _currentHp - previousHp;

            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
                OnHpChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        /// <summary>
        /// Use MP
        /// </summary>
        public bool UseMp(int amount)
        {
            if (_currentMp < amount) return false;

            _currentMp -= amount;
            OnMpChanged?.Invoke(_currentMp, _maxMp);
            return true;
        }

        /// <summary>
        /// Restore MP
        /// </summary>
        public void RestoreMp(int amount)
        {
            _currentMp = Mathf.Min(_maxMp, _currentMp + amount);
            OnMpChanged?.Invoke(_currentMp, _maxMp);
        }

        /// <summary>
        /// Start defending
        /// </summary>
        public void StartDefend(DefenseAction action, Combatant guardTarget = null)
        {
            _isDefending = true;
            _guardTarget = action != DefenseAction.DEFEND_SELF ? guardTarget : null;
            Abilities.currentDefenseAction = action;
        }

        /// <summary>
        /// Stop defending
        /// </summary>
        public void StopDefend()
        {
            _isDefending = false;
            _guardTarget = null;
        }

        /// <summary>
        /// Handle death
        /// </summary>
        private void Die()
        {
            _isAlive = false;
            _isDefending = false;
            _guardTarget = null;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Revive combatant
        /// </summary>
        public void Revive(float hpPercent = 0.5f)
        {
            _isAlive = true;
            _currentHp = Mathf.RoundToInt(_maxHp * hpPercent);
            OnRevive?.Invoke();
            OnHpChanged?.Invoke(_currentHp, _maxHp);
        }

        /// <summary>
        /// Update cooldowns (called each frame during combat)
        /// </summary>
        public void UpdateCooldowns(float deltaTime)
        {
            Abilities?.UpdateAllCooldowns(deltaTime);
        }
    }

    /// <summary>
    /// Runtime status effect instance
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffect effect;
        public float duration;
        public float tickInterval;
        public float lastTickTime;
        public int stacks;
        public Combatant source;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Combat/Combatant.cs
git commit -m "feat: create Combatant base class for combat system"
```

---

### Task 4.2: Create DamageCalculator.cs

**Files:**
- Create: `Assets/Scripts/Combat/DamageCalculator.cs`

**Step 1: Create damage calculator**

```csharp
using UnityEngine;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Calculates all damage values for combat
    /// Formula: BasePower * (ATK/DEF) * BrandMult * SynergyMult * Variance * CritMult
    /// </summary>
    public static class DamageCalculator
    {
        // Constants
        private const float VARIANCE_MIN = 0.9f;
        private const float VARIANCE_MAX = 1.1f;
        private const float CRIT_MULTIPLIER = 1.5f;
        private const float BASE_CRIT_CHANCE = 0.05f;

        /// <summary>
        /// Calculate damage for an attack
        /// </summary>
        public static DamageResult Calculate(
            Combatant attacker,
            Combatant defender,
            int basePower,
            DamageType damageType,
            SynergySystem.SynergyTier synergyTier = SynergySystem.SynergyTier.NEUTRAL)
        {
            var result = new DamageResult();

            // Get offensive and defensive stats
            int offensiveStat = damageType == DamageType.MAGICAL ? attacker.Magic : attacker.Attack;
            int defensiveStat = damageType == DamageType.MAGICAL ? defender.Resistance : defender.Defense;

            // TRUE damage ignores defense
            if (damageType == DamageType.TRUE)
            {
                defensiveStat = 0;
            }

            // Base damage calculation
            float statRatio = defensiveStat > 0 ? (float)offensiveStat / defensiveStat : 2f;
            statRatio = Mathf.Clamp(statRatio, 0.5f, 2.0f);

            float damage = basePower * statRatio;

            // Brand effectiveness
            result.brandMultiplier = BrandSystem.GetEffectiveness(attacker.Brand, defender.Brand);
            damage *= result.brandMultiplier;

            // Synergy bonus
            result.synergyMultiplier = SynergySystem.GetDamageBonus(synergyTier);
            damage *= result.synergyMultiplier;

            // Variance
            result.variance = Random.Range(VARIANCE_MIN, VARIANCE_MAX);
            damage *= result.variance;

            // Critical hit
            float critChance = BASE_CRIT_CHANCE; // TODO: Add luck stat influence
            result.isCritical = Random.value < critChance;
            if (result.isCritical)
            {
                damage *= CRIT_MULTIPLIER;
            }

            result.finalDamage = Mathf.RoundToInt(damage);
            result.finalDamage = Mathf.Max(1, result.finalDamage); // Minimum 1 damage

            return result;
        }

        /// <summary>
        /// Calculate healing amount
        /// </summary>
        public static int CalculateHeal(Combatant healer, int basePower)
        {
            float healing = basePower * (1f + healer.Magic * 0.01f);
            healing *= Random.Range(VARIANCE_MIN, VARIANCE_MAX);
            return Mathf.RoundToInt(healing);
        }
    }

    /// <summary>
    /// Result of a damage calculation
    /// </summary>
    public struct DamageResult
    {
        public int finalDamage;
        public float brandMultiplier;
        public float synergyMultiplier;
        public float variance;
        public bool isCritical;

        public bool IsSuperEffective => brandMultiplier >= BrandSystem.SUPER_EFFECTIVE;
        public bool IsNotEffective => brandMultiplier <= BrandSystem.NOT_EFFECTIVE;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Combat/DamageCalculator.cs
git commit -m "feat: create DamageCalculator with brand and synergy integration"
```

---

### Task 4.3: Create BattleManager.cs

**Files:**
- Create: `Assets/Scripts/Combat/BattleManager.cs`

**Step 1: Create battle manager**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Manages real-time tactical combat
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Battle State")]
        [SerializeField] private BattleState _state = BattleState.INITIALIZING;

        [Header("Combatants")]
        [SerializeField] private List<Combatant> _playerParty = new List<Combatant>();
        [SerializeField] private List<Combatant> _enemyParty = new List<Combatant>();
        [SerializeField] private List<Combatant> _backupMonsters = new List<Combatant>();

        [Header("Synergy")]
        [SerializeField] private Path _championPath = Path.NONE;
        [SerializeField] private SynergySystem.SynergyTier _currentSynergyTier;

        // Properties
        public BattleState State => _state;
        public IReadOnlyList<Combatant> PlayerParty => _playerParty;
        public IReadOnlyList<Combatant> EnemyParty => _enemyParty;
        public SynergySystem.SynergyTier SynergyTier => _currentSynergyTier;
        public bool IsComboAvailable => SynergySystem.IsComboUnlocked(_currentSynergyTier);

        // Events
        public event Action OnBattleStart;
        public event Action OnBattleEnd;
        public event Action<Combatant, Combatant, DamageResult> OnDamageDealt;
        public event Action<Combatant, int> OnHealApplied;
        public event Action<Combatant> OnCombatantDeath;
        public event Action<SynergySystem.SynergyTier> OnSynergyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Initialize and start battle
        /// </summary>
        public void StartBattle(List<Combatant> players, List<Combatant> enemies, Path championPath)
        {
            _playerParty = players;
            _enemyParty = enemies;
            _championPath = championPath;

            // Subscribe to death events
            foreach (var combatant in _playerParty.Concat(_enemyParty))
            {
                combatant.OnDeath += () => HandleCombatantDeath(combatant);
            }

            // Calculate initial synergy
            RecalculateSynergy();

            _state = BattleState.PLAYER_TURN; // Real-time, so this just means "active"
            OnBattleStart?.Invoke();

            Debug.Log($"[BattleManager] Battle started! Synergy: {_currentSynergyTier}");
        }

        /// <summary>
        /// Update loop for real-time combat
        /// </summary>
        private void Update()
        {
            if (_state != BattleState.PLAYER_TURN && _state != BattleState.ENEMY_TURN)
                return;

            float dt = Time.deltaTime;

            // Update all combatant cooldowns
            foreach (var combatant in _playerParty.Where(c => c.IsAlive))
            {
                combatant.UpdateCooldowns(dt);
            }

            foreach (var combatant in _enemyParty.Where(c => c.IsAlive))
            {
                combatant.UpdateCooldowns(dt);
            }

            // Check victory/defeat conditions
            CheckBattleEnd();
        }

        /// <summary>
        /// Execute an ability from a combatant
        /// </summary>
        public void ExecuteAbility(Combatant user, AbilitySlot slot, Combatant target)
        {
            if (!user.IsAlive) return;

            var ability = user.Abilities.GetAbility(slot);
            if (ability == null || !ability.isReady) return;

            // Get skill data
            var skillData = GameDatabase.Instance?.GetSkill(ability.skillId);
            if (skillData == null)
            {
                Debug.LogWarning($"[BattleManager] Skill not found: {ability.skillId}");
                return;
            }

            // Check MP cost
            if (!user.UseMp(skillData.mp_cost))
            {
                Debug.Log($"[BattleManager] Not enough MP for {ability.skillId}");
                return;
            }

            // Trigger cooldown
            ability.TriggerCooldown();

            // Execute based on skill type
            switch (skillData.skill_type)
            {
                case SkillType.ATTACK:
                    ExecuteAttack(user, target, skillData);
                    break;
                case SkillType.HEAL:
                    ExecuteHeal(user, target, skillData);
                    break;
                case SkillType.DEFENSE:
                    user.StartDefend(user.Abilities.currentDefenseAction, target);
                    break;
                // TODO: Buff, Debuff, Utility
            }

            Debug.Log($"[BattleManager] {user.DisplayName} used {skillData.display_name}");
        }

        /// <summary>
        /// Execute attack ability
        /// </summary>
        private void ExecuteAttack(Combatant attacker, Combatant defender, SkillData skill)
        {
            // Check for guard intercept
            var interceptor = GetGuardInterceptor(defender);
            if (interceptor != null)
            {
                defender = interceptor;
            }

            // Calculate damage
            var result = DamageCalculator.Calculate(
                attacker, defender,
                skill.base_power,
                skill.damage_type,
                _currentSynergyTier
            );

            // Apply damage
            defender.TakeDamage(result.finalDamage, result.isCritical);

            OnDamageDealt?.Invoke(attacker, defender, result);
        }

        /// <summary>
        /// Execute heal ability
        /// </summary>
        private void ExecuteHeal(Combatant healer, Combatant target, SkillData skill)
        {
            int healAmount = DamageCalculator.CalculateHeal(healer, skill.base_power);
            target.Heal(healAmount);
            OnHealApplied?.Invoke(target, healAmount);
        }

        /// <summary>
        /// Find any combatant guarding the target
        /// </summary>
        private Combatant GetGuardInterceptor(Combatant target)
        {
            // Check player party
            foreach (var combatant in _playerParty.Where(c => c.IsAlive && c.IsDefending))
            {
                if (combatant.GuardTarget == target)
                {
                    return combatant;
                }
            }

            // Check enemy party
            foreach (var combatant in _enemyParty.Where(c => c.IsAlive && c.IsDefending))
            {
                if (combatant.GuardTarget == target)
                {
                    return combatant;
                }
            }

            return null;
        }

        /// <summary>
        /// Swap a party member with a backup
        /// </summary>
        public bool SwapPartyMember(int activeIndex, int backupIndex)
        {
            if (activeIndex < 0 || activeIndex >= _playerParty.Count) return false;
            if (backupIndex < 0 || backupIndex >= _backupMonsters.Count) return false;

            var temp = _playerParty[activeIndex];
            _playerParty[activeIndex] = _backupMonsters[backupIndex];
            _backupMonsters[backupIndex] = temp;

            // Recalculate synergy
            RecalculateSynergy();

            return true;
        }

        /// <summary>
        /// Recalculate synergy tier based on current party
        /// </summary>
        private void RecalculateSynergy()
        {
            var partyBrands = _playerParty
                .Where(c => c.IsAlive)
                .Select(c => c.Brand)
                .ToArray();

            var oldTier = _currentSynergyTier;
            _currentSynergyTier = SynergySystem.GetSynergyTier(_championPath, partyBrands);

            if (oldTier != _currentSynergyTier)
            {
                OnSynergyChanged?.Invoke(_currentSynergyTier);
                Debug.Log($"[BattleManager] Synergy changed: {oldTier} -> {_currentSynergyTier}");
            }
        }

        /// <summary>
        /// Handle combatant death
        /// </summary>
        private void HandleCombatantDeath(Combatant combatant)
        {
            OnCombatantDeath?.Invoke(combatant);
            RecalculateSynergy();
        }

        /// <summary>
        /// Check if battle should end
        /// </summary>
        private void CheckBattleEnd()
        {
            bool allPlayersDead = _playerParty.All(c => !c.IsAlive);
            bool allEnemiesDead = _enemyParty.All(c => !c.IsAlive);

            if (allPlayersDead)
            {
                EndBattle(BattleState.DEFEAT);
            }
            else if (allEnemiesDead)
            {
                EndBattle(BattleState.VICTORY);
            }
        }

        /// <summary>
        /// End the battle
        /// </summary>
        private void EndBattle(BattleState endState)
        {
            _state = endState;

            // Unsubscribe from events
            foreach (var combatant in _playerParty.Concat(_enemyParty))
            {
                // Note: In production, properly unsubscribe death events
            }

            OnBattleEnd?.Invoke();
            Debug.Log($"[BattleManager] Battle ended: {endState}");
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Combat/BattleManager.cs
git commit -m "feat: create BattleManager for real-time tactical combat"
```

---

## Phase 5: Update EventBus

### Task 5.1: Add Combat Events to EventBus

**Files:**
- Modify: `Assets/Scripts/Core/EventBus.cs`

**Step 1: Read current EventBus and add combat events**

Add these events to EventBus.cs:

```csharp
// =============================================================================
// COMBAT EVENTS
// =============================================================================

public static event Action OnBattleStarted;
public static event Action OnBattleEnded;
public static event Action<string, string, int, bool> OnDamageDealt;  // attackerId, targetId, damage, isCrit
public static event Action<string, int> OnHealReceived;                // targetId, amount
public static event Action<string> OnCombatantDied;                    // combatantId
public static event Action<int> OnSynergyTierChanged;                  // tier as int

public static void BattleStarted() => OnBattleStarted?.Invoke();
public static void BattleEnded() => OnBattleEnded?.Invoke();
public static void DamageDealt(string attacker, string target, int damage, bool isCrit)
    => OnDamageDealt?.Invoke(attacker, target, damage, isCrit);
public static void HealReceived(string target, int amount) => OnHealReceived?.Invoke(target, amount);
public static void CombatantDied(string combatantId) => OnCombatantDied?.Invoke(combatantId);
public static void SynergyTierChanged(int tier) => OnSynergyTierChanged?.Invoke(tier);
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Core/EventBus.cs
git commit -m "feat: add combat events to EventBus"
```

---

## Phase 6: Final Integration & Testing

### Task 6.1: Create Combat Test Scene

**Files:**
- Create: `Assets/Scenes/Test/CombatTest.unity` (via Unity Editor)
- Create: `Assets/Scripts/Test/CombatTestSetup.cs`

**Step 1: Create test setup script**

```csharp
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test script for combat system verification
    /// </summary>
    public class CombatTestSetup : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private Path _testChampionPath = Path.IRONBOUND;

        private void Start()
        {
            Debug.Log("=== COMBAT SYSTEM TEST ===");

            TestBrandEffectiveness();
            TestSynergySystem();
            TestDamageCalculation();

            Debug.Log("=== ALL TESTS COMPLETE ===");
        }

        private void TestBrandEffectiveness()
        {
            Debug.Log("\n--- Brand Effectiveness Tests ---");

            // IRON vs SURGE should be 2x
            float ironVsSurge = BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE);
            Debug.Log($"IRON vs SURGE: {ironVsSurge}x (expected 2x) - {(ironVsSurge == 2f ? "PASS" : "FAIL")}");

            // IRON vs SAVAGE should be 0.5x
            float ironVsSavage = BrandSystem.GetEffectiveness(Brand.IRON, Brand.SAVAGE);
            Debug.Log($"IRON vs SAVAGE: {ironVsSavage}x (expected 0.5x) - {(ironVsSavage == 0.5f ? "PASS" : "FAIL")}");

            // IRON vs GRACE should be 1x (neutral)
            float ironVsGrace = BrandSystem.GetEffectiveness(Brand.IRON, Brand.GRACE);
            Debug.Log($"IRON vs GRACE: {ironVsGrace}x (expected 1x) - {(ironVsGrace == 1f ? "PASS" : "FAIL")}");
        }

        private void TestSynergySystem()
        {
            Debug.Log("\n--- Synergy System Tests ---");

            // IRONBOUND with IRON, MEND, LEECH should be FULL
            var fullSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            Debug.Log($"IRONBOUND + IRON/MEND/LEECH: {fullSynergy} (expected FULL) - {(fullSynergy == SynergySystem.SynergyTier.FULL ? "PASS" : "FAIL")}");

            // IRONBOUND with IRON, MEND, GRACE should be PARTIAL
            var partialSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.GRACE }
            );
            Debug.Log($"IRONBOUND + IRON/MEND/GRACE: {partialSynergy} (expected PARTIAL) - {(partialSynergy == SynergySystem.SynergyTier.PARTIAL ? "PASS" : "FAIL")}");

            // IRONBOUND with VOID should be ANTI
            var antiSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.VOID, Brand.GRACE, Brand.DREAD }
            );
            Debug.Log($"IRONBOUND + VOID/GRACE/DREAD: {antiSynergy} (expected ANTI) - {(antiSynergy == SynergySystem.SynergyTier.ANTI ? "PASS" : "FAIL")}");

            // Check bonuses
            Debug.Log($"FULL damage bonus: {SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.FULL)} (expected 1.08)");
            Debug.Log($"PARTIAL defense bonus: {SynergySystem.GetDefenseBonus(SynergySystem.SynergyTier.PARTIAL)} (expected 1.05)");
            Debug.Log($"ANTI corruption rate: {SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.ANTI)} (expected 1.5)");
        }

        private void TestDamageCalculation()
        {
            Debug.Log("\n--- Damage Calculation Tests ---");

            // Create test combatants
            var attacker = gameObject.AddComponent<Combatant>();
            attacker.Initialize("test_attacker", "Test Attacker", Brand.IRON, 100, 50, 20, 10, 10, 10, 10, true);

            var defender = gameObject.AddComponent<Combatant>();
            defender.Initialize("test_defender", "Test Defender", Brand.SURGE, 100, 50, 10, 15, 10, 10, 10, false);

            // Calculate damage (IRON vs SURGE = 2x)
            var result = DamageCalculator.Calculate(
                attacker, defender,
                basePower: 50,
                DamageType.PHYSICAL,
                SynergySystem.SynergyTier.FULL
            );

            Debug.Log($"Base 50 power attack:");
            Debug.Log($"  Brand multiplier: {result.brandMultiplier}x");
            Debug.Log($"  Synergy multiplier: {result.synergyMultiplier}x");
            Debug.Log($"  Final damage: {result.finalDamage}");
            Debug.Log($"  Is critical: {result.isCritical}");
            Debug.Log($"  Is super effective: {result.IsSuperEffective}");

            // Clean up
            Destroy(attacker);
            Destroy(defender);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Test/CombatTestSetup.cs
git commit -m "feat: add combat system test script"
```

---

### Task 6.2: Final Commit and Push

**Step 1: Ensure all files are committed**

```bash
git status
```

**Step 2: Push to remote**

```bash
git push origin feature/godot-documentation-transfer
```

**Step 3: Update VEILBREAKERS.md with implementation status**

Add to session log:
```
| 2026-01-17 | v1.41: COMBAT IMPLEMENTATION - 10-brand system, tiered synergy, 6-slot abilities, BattleManager, DamageCalculator, Combatant class |
```

---

## Summary

### Files Created/Modified

| File | Action | Purpose |
|------|--------|---------|
| `Enums.cs` | Modified | 10-brand enum, AbilitySlot enum |
| `BrandSystem.cs` | Replaced | 10-brand 2x/0.5x effectiveness matrix |
| `SynergySystem.cs` | Created | Tiered synergy (full/partial/neutral/anti) |
| `AbilityData.cs` | Created | 6-slot ability loadout |
| `Combatant.cs` | Created | Base class for all combatants |
| `DamageCalculator.cs` | Created | Damage calculation with brand/synergy |
| `BattleManager.cs` | Created | Real-time tactical combat manager |
| `EventBus.cs` | Modified | Combat events |
| `CombatTestSetup.cs` | Created | Test script for verification |

### Execution Order

1. Task 1.1 → Task 1.2 (Brand System)
2. Task 2.1 (Synergy System)
3. Task 3.1 (Ability System)
4. Task 4.1 → Task 4.2 → Task 4.3 (Combat Foundation)
5. Task 5.1 (EventBus)
6. Task 6.1 → Task 6.2 (Testing & Final)

### Estimated Tasks: 10 commits, ~2 hours of Ralph execution

---

*Plan generated 2026-01-17 for Ralph Wiggum execution*

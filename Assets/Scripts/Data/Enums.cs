using System;

namespace VeilBreakers.Data
{
    /// <summary>
    /// All game enumerations for VeilBreakers
    /// </summary>

    // =============================================================================
    // BRAND SYSTEM - 10 BRANDS
    // Each brand deals 2x to 2 brands, 0.5x to 2 brands, 1x to 6 brands
    // =============================================================================

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

    // =============================================================================
    // PATH SYSTEM - 4 PATHS
    // =============================================================================

    public enum Path
    {
        NONE = 0,
        IRONBOUND = 1,      // Tank/Defender - Defense mastery
        FANGBORN = 2,       // Attacker/Hunter - Offense mastery
        VOIDTOUCHED = 3,    // Mage/Caster - Magic mastery
        UNCHAINED = 4       // Wildcard/Hybrid - Versatility
    }

    // =============================================================================
    // CORRUPTION SYSTEM
    // Lower = STRONGER (inverts typical dark power tropes)
    // =============================================================================

    public enum CorruptionState
    {
        ASCENDED = 0,       // 0-10% - +25% stats (goal state)
        PURIFIED = 1,       // 11-25% - +10% stats
        UNSTABLE = 2,       // 26-50% - Normal stats
        CORRUPTED = 3,      // 51-75% - -10% stats
        ABYSSAL = 4         // 76-100% - -20% stats
    }

    // =============================================================================
    // SKILL TYPES
    // =============================================================================

    public enum SkillType
    {
        ATTACK = 0,
        DEFENSE = 1,
        BUFF = 2,
        DEBUFF = 3,
        HEAL = 4,
        UTILITY = 5,
        ULTIMATE = 6
    }

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

    public enum DamageType
    {
        PHYSICAL = 0,
        MAGICAL = 1,
        TRUE = 2,       // Ignores defense
        DRAIN = 3       // Lifesteal
    }

    public enum TargetType
    {
        SELF = 0,
        SINGLE_ENEMY = 1,
        ALL_ENEMIES = 2,
        SINGLE_ALLY = 3,
        ALL_ALLIES = 4,
        ALL = 5
    }

    // =============================================================================
    // STATS
    // =============================================================================

    public enum Stat
    {
        HP = 0,
        MP = 1,
        ATTACK = 2,
        DEFENSE = 3,
        MAGIC = 4,
        RESISTANCE = 5,
        SPEED = 6,
        LUCK = 7,
        CRIT_RATE = 8,
        CRIT_DAMAGE = 9
    }

    // =============================================================================
    // STATUS EFFECTS SYSTEM
    // =============================================================================

    /// <summary>
    /// Category of effect (determines behavior rules)
    /// </summary>
    public enum EffectCategory
    {
        DAMAGE = 0,         // DoTs, burst damage
        CONTROL = 1,        // CC effects (stun, root, etc.)
        BUFF = 2,           // Positive effects on allies
        DEBUFF = 3          // Negative effects on enemies
    }

    /// <summary>
    /// Duration tier for effects
    /// </summary>
    public enum DurationTier
    {
        SHORT = 0,          // 3-5 seconds
        MEDIUM = 1,         // 8-12 seconds
        LONG = 2,           // 15-20 seconds
        EXTENDED = 3        // 30+ seconds
    }

    /// <summary>
    /// All status effect types in the game
    /// </summary>
    public enum StatusEffectType
    {
        NONE = 0,

        // ===== DAMAGE EFFECTS (DoTs) =====
        POISON = 1,         // VENOM brand
        BURN = 2,           // RUIN brand
        BLEED = 3,          // SAVAGE brand
        MARKED = 4,         // Bonus damage on next hit
        CURSED_DAMAGE = 5,  // Damage when acting

        // ===== CONTROL EFFECTS (CC) =====
        STUN = 10,          // Can't act
        SLOW = 11,          // Reduced speed
        ROOT = 12,          // Can't move, can act
        SILENCE = 13,       // Can't use skills
        BLIND = 14,         // Attacks miss
        TAUNT = 15,         // Forced to attack taunter
        FEAR = 16,          // Forced to flee
        CHARM = 17,         // Fights for enemy
        CONFUSE = 18,       // Random targets
        SLEEP = 19,         // CC, breaks on damage
        PETRIFY = 20,       // Long stun + damage resist

        // ===== STAT BUFFS =====
        ATTACK_UP = 30,
        DEFENSE_UP = 31,
        SPEED_UP = 32,
        ACCURACY_UP = 33,
        EVASION_UP = 34,
        CRIT_RATE_UP = 35,
        CRIT_DAMAGE_UP = 36,

        // ===== DEFENSIVE BUFFS =====
        SHIELD = 40,        // Absorbs X damage before HP
        BARRIER = 41,       // Caps damage per hit
        REGEN = 42,         // Healing over time
        FORTIFY = 43,       // CC resistance
        THORNS = 44,        // Damages attackers

        // ===== OFFENSIVE BUFFS =====
        LIFESTEAL = 50,     // Heal on damage dealt
        EMPOWER = 51,       // Next skill deals bonus damage
        FOCUS = 52,         // Increased accuracy + crit
        BERSERK = 53,       // Damage up, AI uncontrollable

        // ===== UTILITY BUFFS =====
        HASTE = 60,         // Faster cooldowns
        IMMUNITY = 61,      // Blocks next debuff
        STEALTH = 62,       // Untargetable
        REFLECT = 63,       // Returns portion of damage

        // ===== EMERGENCY BUFFS =====
        SECOND_WIND = 70,   // Auto-revive at 1 HP once
        QUICKEN = 71,       // Instant cooldown reset
        UNDYING = 72,       // Survive next lethal hit at 1 HP

        // ===== STAT DEBUFFS =====
        ATTACK_DOWN = 80,
        DEFENSE_DOWN = 81,
        SPEED_DOWN = 82,
        ACCURACY_DOWN = 83,
        EVASION_DOWN = 84,
        CRIT_RATE_DOWN = 85,
        CRIT_DAMAGE_DOWN = 86,

        // ===== VULNERABILITY DEBUFFS =====
        EXPOSE = 90,        // Take +X% damage from all sources
        FRAGILE = 91,       // Increased crit damage taken
        ARMOR_SHRED = 92,   // Defense stat ignored
        BRAND_WEAKNESS = 93,// Takes 2x from specific brand

        // ===== RESTRICTION DEBUFFS =====
        EXHAUSTED = 100,    // Can't receive buffs
        SEALED = 101,       // Can't use ultimate
        GROUNDED = 102,     // Can't use movement skills

        // ===== ANTI-SUSTAIN DEBUFFS =====
        HEAL_BLOCK = 110,   // Cannot receive healing
        CURSED = 111,       // Healing reduced by X%
        DECAY = 112,        // Stats drop over time
        WITHER = 113,       // Regen effects deal damage instead

        // ===== DEATH SENTENCE DEBUFFS =====
        DOOM = 120,         // Death when timer expires
        MARKED_DEATH = 121, // Execute threshold raised
        CONDEMNED = 122     // Die if not cleansed before expiry
    }

    /// <summary>
    /// Legacy StatusEffect enum - DO NOT USE
    /// Use StatusEffectType instead
    /// </summary>
    [Obsolete("Use StatusEffectType instead")]
    public enum StatusEffect
    {
        NONE = 0,

        // Debuffs
        POISON = 1,
        BURN = 2,
        BLEED = 3,
        STUN = 4,
        SILENCE = 5,
        BLIND = 6,
        FEAR = 7,
        SLOW = 8,
        WEAKNESS = 9,
        VULNERABILITY = 10,
        CURSE = 11,

        // Buffs
        FRENZY = 12,
        SHIELD = 13,
        REGEN = 14,
        HASTE = 15,
        STRENGTH = 16,
        FORTIFY = 17,
        FOCUS = 18,
        TAUNT = 19,
        REFLECT = 20,
        INVULNERABLE = 21
    }

    // =============================================================================
    // ITEM TYPES
    // =============================================================================

    public enum ItemCategory
    {
        CONSUMABLE = 0,
        EQUIPMENT = 1,
        KEY_ITEM = 2,
        MATERIAL = 3
    }

    public enum EquipmentSlot
    {
        WEAPON = 0,
        ARMOR = 1,
        ACCESSORY = 2,
        RING = 3
    }

    // =============================================================================
    // MONSTER RARITY
    // =============================================================================

    public enum Rarity
    {
        COMMON = 0,
        UNCOMMON = 1,
        RARE = 2,
        EPIC = 3,
        LEGENDARY = 4,
        MYTHIC = 5
    }

    // =============================================================================
    // AI PATTERNS
    // =============================================================================

    public enum AIPattern
    {
        AGGRESSIVE = 0,     // Prioritize damage
        DEFENSIVE = 1,      // Prioritize survival
        SUPPORT = 2,        // Prioritize healing/buffs
        BALANCED = 3,       // Mix of all
        BERSERKER = 4,      // Attack at all costs
        OPPORTUNIST = 5     // Target weakened enemies
    }

    // =============================================================================
    // HERO ROLES
    // =============================================================================

    public enum HeroRole
    {
        TANK = 0,
        DPS = 1,
        SUPPORT = 2,
        HYBRID = 3
    }

    // =============================================================================
    // BATTLE STATES
    // =============================================================================

    public enum BattleState
    {
        INITIALIZING = 0,
        PLAYER_TURN = 1,
        ENEMY_TURN = 2,
        ANIMATING = 3,
        VICTORY = 4,
        DEFEAT = 5,
        ESCAPED = 6,
        CAPTURE = 7
    }
}

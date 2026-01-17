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
    // STATUS EFFECTS
    // =============================================================================

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

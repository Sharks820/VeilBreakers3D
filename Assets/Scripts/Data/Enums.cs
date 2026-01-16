using System;

namespace VeilBreakers.Data
{
    /// <summary>
    /// All game enumerations for VeilBreakers
    /// </summary>

    // =============================================================================
    // BRAND SYSTEM - 12 BRANDS (6 Pure + 6 Hybrid)
    // Effectiveness wheel: SAVAGE > IRON > VENOM > SURGE > DREAD > LEECH > SAVAGE
    // =============================================================================

    public enum Brand
    {
        NONE = 0,

        // Pure Brands (Primary)
        SAVAGE = 1,     // Red - Raw damage, berserk, crits
        IRON = 2,       // Silver - Defense, armor, resilience
        VENOM = 3,      // Green - Poison, decay, DoT
        SURGE = 4,      // Blue - Speed, lightning, tempo
        DREAD = 5,      // Purple - Fear, debuffs, control
        LEECH = 6,      // Crimson - Lifesteal, drain, sustain

        // Hybrid Brands (SAVAGE base + Secondary)
        BLOODIRON = 7,      // SAVAGE + IRON - Tank that rages
        CORROSIVE = 8,      // SAVAGE + VENOM - Poison rampage
        VENOMSTRIKE = 9,    // VENOM + SURGE - Fast poison
        TERRORFLUX = 10,    // DREAD + SURGE - Lightning fear
        NIGHTLEECH = 11,    // DREAD + LEECH - Terrifying drain
        RAVENOUS = 12,      // LEECH + SAVAGE - Hungry berserker

        // Ascended tier (0% corruption)
        PRIMAL = 99
    }

    public enum BrandTier
    {
        PURE = 0,       // Single brand
        HYBRID = 1,     // Two brands combined
        PRIMAL = 2      // Ascended (0% corruption)
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

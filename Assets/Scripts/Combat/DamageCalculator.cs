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

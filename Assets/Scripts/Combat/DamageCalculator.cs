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
        private const float kVarianceMin = 0.9f;
        private const float kVarianceMax = 1.1f;
        private const float kCritMultiplier = 1.5f;
        private const float kBaseCritChance = 0.05f;

        /// <summary>
        /// Calculate damage for an attack
        /// </summary>
        public static DamageResult Calculate(
            Combatant attacker,
            Combatant defender,
            int basePower,
            DamageType damageType,
            SynergySystem.SynergyTier synergyTier = SynergySystem.SynergyTier.NEUTRAL,
            SynergySystem.SynergyTier defenderSynergyTier = SynergySystem.SynergyTier.NEUTRAL)
        {
            var result = new DamageResult();

            // Guard against null combatants
            if (attacker == null || defender == null)
            {
                Debug.LogWarning("[DamageCalculator] Null combatant in damage calculation");
                result.finalDamage = basePower > 0 ? basePower : 1;
                return result;
            }

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

            // Attacker damage multiplier (buffs/debuffs on the attacker)
            damage *= attacker.DamageMultiplier;

            // Brand effectiveness
            result.brandMultiplier = BrandSystem.GetEffectiveness(attacker.Brand, defender.Brand);
            damage *= result.brandMultiplier;

            // Attacker synergy damage bonus
            result.synergyMultiplier = SynergySystem.GetDamageBonus(synergyTier);
            damage *= result.synergyMultiplier;

            // Defender synergy defense bonus (reduces incoming damage)
            float defenderSynergyDefense = SynergySystem.GetDefenseBonus(defenderSynergyTier);
            damage /= defenderSynergyDefense;

            // Corruption modifiers (affects both attacker output and defender resistance)
            float attackerCorruptionMod = GetCorruptionModifier(attacker.Corruption);
            float defenderCorruptionMod = GetCorruptionModifier(defender.Corruption);
            damage *= (1f + attackerCorruptionMod);
            damage *= (1f - defenderCorruptionMod);

            // Variance
            result.variance = Random.Range(kVarianceMin, kVarianceMax);
            damage *= result.variance;

            // Critical hit
            float critChance = kBaseCritChance; // TODO: Add luck stat influence
            result.isCritical = Random.value < critChance;
            if (result.isCritical)
            {
                damage *= kCritMultiplier;
            }

            result.finalDamage = Mathf.RoundToInt(damage);
            result.finalDamage = Mathf.Max(1, result.finalDamage); // Minimum 1 damage

            return result;
        }

        /// <summary>
        /// Get stat modifier based on corruption level.
        /// ASCENDED (0-10%): +25%, Purified (11-25%): +10%, Unstable (26-50%): +0%,
        /// Corrupted (51-75%): -10%, Abyssal (76-100%): -20%
        /// </summary>
        private static float GetCorruptionModifier(float corruption)
        {
            if (corruption <= 10f) return 0.25f;       // ASCENDED
            if (corruption <= 25f) return 0.10f;       // Purified
            if (corruption <= 50f) return 0f;           // Unstable
            if (corruption <= 75f) return -0.10f;      // Corrupted
            return -0.20f;                              // Abyssal
        }

        /// <summary>
        /// Calculate healing amount
        /// </summary>
        public static int CalculateHeal(Combatant healer, int basePower)
        {
            if (healer == null)
            {
                return Mathf.Max(1, basePower);
            }

            float healing = basePower * (1f + healer.Magic * 0.01f);
            healing *= Random.Range(kVarianceMin, kVarianceMax);
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
        public bool wasBlocked;
        public bool wasDodged;

        public bool IsSuperEffective => brandMultiplier >= BrandSystem.SUPER_EFFECTIVE;
        public bool IsNotEffective => brandMultiplier <= BrandSystem.NOT_EFFECTIVE;
    }
}

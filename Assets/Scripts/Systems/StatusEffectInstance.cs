using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Runtime instance of a status effect applied to a target.
    /// Tracks duration, stacks, and calculated values.
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        // =============================================================================
        // IDENTIFICATION
        // =============================================================================

        /// <summary>Unique instance ID for this application</summary>
        public string instanceId;

        /// <summary>Reference to the effect data</summary>
        public StatusEffectData effectData;

        /// <summary>Who applied this effect (for tracking/scaling)</summary>
        public GameObject source;

        /// <summary>Who has this effect (the target)</summary>
        public GameObject target;

        // =============================================================================
        // TIMING STATE
        // =============================================================================

        /// <summary>Total duration in seconds</summary>
        public float duration;

        /// <summary>Remaining time until expiry</summary>
        public float remainingTime;

        /// <summary>Time until next tick (for DoT/HoT)</summary>
        public float tickTimer;

        /// <summary>When this effect was applied</summary>
        public float applicationTime;

        // =============================================================================
        // POTENCY STATE
        // =============================================================================

        /// <summary>Final calculated potency value</summary>
        public float potency;

        /// <summary>Current stack count</summary>
        public int stacks = 1;

        /// <summary>Skill rank used when applying (1.0 to 2.0)</summary>
        public float skillRank = 1f;

        /// <summary>Brand effectiveness multiplier at time of application</summary>
        public float brandEffectiveness = 1f;

        // =============================================================================
        // SPECIAL STATE
        // =============================================================================

        /// <summary>Shield remaining HP (for SHIELD effects)</summary>
        public float shieldRemaining;

        /// <summary>Has the consumable trigger been used?</summary>
        public bool consumed;

        /// <summary>Source of taunt (for TAUNT effects)</summary>
        public GameObject tauntSource;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>The effect type</summary>
        public StatusEffectType EffectType => effectData?.effectType ?? StatusEffectType.NONE;

        /// <summary>The effect category</summary>
        public EffectCategory Category => effectData?.category ?? EffectCategory.DEBUFF;

        /// <summary>True if the effect has expired</summary>
        public bool IsExpired => remainingTime <= 0f;

        /// <summary>Progress from 0 (just applied) to 1 (expired)</summary>
        public float Progress => duration > 0 ? 1f - (remainingTime / duration) : 1f;

        /// <summary>Progress from 1 (just applied) to 0 (expired) - for UI countdown</summary>
        public float RemainingProgress => duration > 0 ? remainingTime / duration : 0f;

        /// <summary>True if this effect ticks over time</summary>
        public bool IsTicking => effectData != null && effectData.tickInterval > 0f;

        /// <summary>True if this effect should be cleansed (harmful to target)</summary>
        public bool ShouldBeCleansed => effectData != null && effectData.IsHarmful;

        /// <summary>True if this effect should be dispelled (beneficial to target)</summary>
        public bool ShouldBeDispelled => effectData != null && effectData.IsBeneficial;

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Creates a new status effect instance.
        /// </summary>
        public static StatusEffectInstance Create(
            StatusEffectData data,
            GameObject source,
            GameObject target,
            float statModifier = 0f,
            float skillRank = 1f,
            float brandEffectiveness = 1f)
        {
            if (data == null)
            {
                Debug.LogError("[StatusEffectInstance] Cannot create instance with null data!");
                return null;
            }

            var instance = new StatusEffectInstance
            {
                instanceId = Guid.NewGuid().ToString(),
                effectData = data,
                source = source,
                target = target,
                skillRank = skillRank,
                brandEffectiveness = brandEffectiveness,
                applicationTime = Time.time,
                stacks = 1,
                consumed = false
            };

            // Calculate duration
            instance.duration = data.CalculateDuration(statModifier);
            instance.remainingTime = instance.duration;

            // Calculate potency
            instance.potency = data.CalculatePotency(statModifier, skillRank, brandEffectiveness);

            // Initialize tick timer
            if (data.IsTicking)
            {
                instance.tickTimer = data.tickInterval;
            }

            // Initialize special state
            if (data.effectType == StatusEffectType.SHIELD)
            {
                instance.shieldRemaining = instance.potency;
            }

            if (data.effectType == StatusEffectType.TAUNT)
            {
                instance.tauntSource = source;
            }

            return instance;
        }

        // =============================================================================
        // UPDATE METHODS
        // =============================================================================

        /// <summary>
        /// Updates the effect timer. Call this every frame.
        /// Returns true if a tick occurred.
        /// </summary>
        public bool UpdateTimer(float deltaTime)
        {
            remainingTime -= deltaTime;

            if (!IsTicking)
                return false;

            tickTimer -= deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = effectData.tickInterval;
                return true; // Tick occurred
            }

            return false;
        }

        /// <summary>
        /// Refreshes the duration (used when reapplying same effect).
        /// </summary>
        public void RefreshDuration()
        {
            remainingTime = duration;
            tickTimer = effectData?.tickInterval ?? 0f;
        }

        /// <summary>
        /// Adds a stack (if stacking is allowed).
        /// </summary>
        public bool AddStack()
        {
            if (effectData == null || !effectData.canStack)
                return false;

            if (stacks >= effectData.maxStacks)
                return false;

            stacks++;
            return true;
        }

        /// <summary>
        /// Removes a stack. Returns true if stacks remain.
        /// </summary>
        public bool RemoveStack()
        {
            stacks--;
            return stacks > 0;
        }

        /// <summary>
        /// Consumes the effect (for one-time trigger effects).
        /// </summary>
        public void Consume()
        {
            consumed = true;
            remainingTime = 0f;
        }

        // =============================================================================
        // DAMAGE HANDLING
        // =============================================================================

        /// <summary>
        /// Called when the target takes damage.
        /// Returns true if the effect should be removed.
        /// </summary>
        public bool OnDamageTaken(float damage)
        {
            if (effectData == null)
                return false;

            // Break on damage (Sleep, etc.)
            if (effectData.breaksOnDamage)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Absorbs damage with shield. Returns remaining damage after absorption.
        /// </summary>
        public float AbsorbDamage(float damage)
        {
            if (effectData?.effectType != StatusEffectType.SHIELD)
                return damage;

            if (shieldRemaining <= 0f)
                return damage;

            float absorbed = Mathf.Min(damage, shieldRemaining);
            shieldRemaining -= absorbed;
            return damage - absorbed;
        }

        // =============================================================================
        // VALUE CALCULATIONS
        // =============================================================================

        /// <summary>
        /// Gets the current value for stat modification effects.
        /// </summary>
        public float GetStatModValue()
        {
            float value = potency * stacks;

            // Apply sign based on category
            if (Category == EffectCategory.DEBUFF)
            {
                return -value; // Debuffs reduce stats
            }

            return value;
        }

        /// <summary>
        /// Gets the damage per tick for DoT effects.
        /// </summary>
        public float GetTickDamage()
        {
            if (Category != EffectCategory.DAMAGE)
                return 0f;

            return potency * stacks;
        }

        /// <summary>
        /// Gets the heal per tick for HoT effects.
        /// </summary>
        public float GetTickHeal()
        {
            if (EffectType != StatusEffectType.REGEN)
                return 0f;

            return potency * stacks;
        }

        // =============================================================================
        // QUERY METHODS
        // =============================================================================

        /// <summary>
        /// Checks if this effect is the same type as another.
        /// </summary>
        public bool IsSameType(StatusEffectInstance other)
        {
            return other != null && EffectType == other.EffectType;
        }

        /// <summary>
        /// Checks if this effect is the same type as a data definition.
        /// </summary>
        public bool IsSameType(StatusEffectData data)
        {
            return data != null && EffectType == data.effectType;
        }

        /// <summary>
        /// Gets a debug string representation.
        /// </summary>
        public override string ToString()
        {
            return $"[{effectData?.displayName ?? "Unknown"}] " +
                   $"Potency: {potency:F1}, " +
                   $"Time: {remainingTime:F1}/{duration:F1}s, " +
                   $"Stacks: {stacks}";
        }
    }
}

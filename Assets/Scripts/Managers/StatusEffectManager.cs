using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages all status effects in combat.
    /// Handles application, updates, removal, and validation.
    /// </summary>
    public class StatusEffectManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static StatusEffectManager _instance;
        public static StatusEffectManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[StatusEffectManager] Instance not found!");
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Maximum effects per target (safety cap)")]
        private int _maxEffectsPerTarget = 10;

        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool _debugLogging = true;

        // =============================================================================
        // STATE
        // =============================================================================

        /// <summary>Effects mapped by target</summary>
        private Dictionary<GameObject, List<StatusEffectInstance>> _effectsByTarget
            = new Dictionary<GameObject, List<StatusEffectInstance>>();

        /// <summary>All loaded effect data (cached)</summary>
        private Dictionary<StatusEffectType, StatusEffectData> _effectDataCache
            = new Dictionary<StatusEffectType, StatusEffectData>();

        /// <summary>Reusable lists to avoid allocations in hot paths</summary>
        private readonly List<GameObject> _tempTargetList = new List<GameObject>();
        private readonly List<StatusEffectInstance> _tempEffectList = new List<StatusEffectInstance>();
        private readonly List<(GameObject, StatusEffectInstance)> _tempRemoveList = new List<(GameObject, StatusEffectInstance)>();

        /// <summary>Application quit flag to suppress errors during shutdown</summary>
        private static bool _isQuitting = false;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when an effect is applied</summary>
        public event Action<GameObject, StatusEffectInstance> OnEffectApplied;

        /// <summary>Fired when an effect ticks</summary>
        public event Action<GameObject, StatusEffectInstance, float> OnEffectTick;

        /// <summary>Fired when an effect is removed</summary>
        public event Action<GameObject, StatusEffectInstance, EffectRemovalReason> OnEffectRemoved;

        /// <summary>Fired when an effect application is blocked</summary>
        public event Action<GameObject, StatusEffectData, EffectBlockReason> OnEffectBlocked;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEffectDataCache();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void Update()
        {
            UpdateAllEffects(Time.deltaTime);
        }

        // =============================================================================
        // PUBLIC API - APPLICATION
        // =============================================================================

        /// <summary>
        /// Attempts to apply a status effect to a target.
        /// Returns the applied instance, or null if blocked.
        /// </summary>
        public StatusEffectInstance ApplyEffect(
            StatusEffectData effectData,
            GameObject source,
            GameObject target,
            float statModifier = 0f,
            float skillRank = 1f,
            float brandEffectiveness = 1f)
        {
            if (effectData == null || target == null)
            {
                Log($"ApplyEffect failed: null data or target");
                return null;
            }

            // Get or create target's effect list
            if (!_effectsByTarget.TryGetValue(target, out var effects))
            {
                effects = new List<StatusEffectInstance>();
                _effectsByTarget[target] = effects;
            }

            // Check max effects cap
            if (effects.Count >= _maxEffectsPerTarget)
            {
                Log($"Effect blocked on {target.name}: max effects reached ({_maxEffectsPerTarget})");
                OnEffectBlocked?.Invoke(target, effectData, EffectBlockReason.MAX_EFFECTS);
                return null;
            }

            // NO-STACK RULE: Check for existing effect of same type
            var existingEffect = effects.FirstOrDefault(e => e.IsSameType(effectData));

            if (existingEffect != null)
            {
                // Same effect exists - handle based on settings
                if (effectData.canStack && existingEffect.stacks < effectData.maxStacks)
                {
                    // Add stack
                    existingEffect.AddStack();
                    Log($"Added stack to {effectData.displayName} on {target.name}. Stacks: {existingEffect.stacks}");
                    return existingEffect;
                }
                else if (effectData.refreshOnReapply)
                {
                    // Refresh duration
                    existingEffect.RefreshDuration();
                    Log($"Refreshed {effectData.displayName} on {target.name}");
                    return existingEffect;
                }
                else
                {
                    // Blocked - same effect already exists
                    Log($"Effect blocked on {target.name}: {effectData.displayName} already active");
                    OnEffectBlocked?.Invoke(target, effectData, EffectBlockReason.ALREADY_ACTIVE);
                    return null;
                }
            }

            // Check for IMMUNITY buff
            if (effectData.IsHarmful && HasEffect(target, StatusEffectType.IMMUNITY))
            {
                // Consume immunity and block effect
                RemoveEffect(target, StatusEffectType.IMMUNITY, EffectRemovalReason.CONSUMED);
                Log($"Effect blocked on {target.name}: IMMUNITY consumed");
                OnEffectBlocked?.Invoke(target, effectData, EffectBlockReason.IMMUNITY);
                return null;
            }

            // Check for EXHAUSTED (can't receive buffs)
            if (effectData.IsBeneficial && HasEffect(target, StatusEffectType.EXHAUSTED))
            {
                Log($"Buff blocked on {target.name}: target is EXHAUSTED");
                OnEffectBlocked?.Invoke(target, effectData, EffectBlockReason.EXHAUSTED);
                return null;
            }

            // Create and apply the effect
            var instance = StatusEffectInstance.Create(
                effectData, source, target, statModifier, skillRank, brandEffectiveness);

            if (instance == null)
            {
                return null;
            }

            effects.Add(instance);

            Log($"Applied {effectData.displayName} to {target.name}. " +
                $"Potency: {instance.potency:F1}, Duration: {instance.duration:F1}s");

            // Fire events
            OnEffectApplied?.Invoke(target, instance);
            EventBus.StatusEffectApplied(target, effectData.effectType);

            return instance;
        }

        /// <summary>
        /// Applies an effect by type (uses cached data).
        /// </summary>
        public StatusEffectInstance ApplyEffect(
            StatusEffectType effectType,
            GameObject source,
            GameObject target,
            float statModifier = 0f,
            float skillRank = 1f,
            float brandEffectiveness = 1f)
        {
            if (!_effectDataCache.TryGetValue(effectType, out var data))
            {
                Debug.LogWarning($"[StatusEffectManager] No cached data for effect type: {effectType}");
                return null;
            }

            return ApplyEffect(data, source, target, statModifier, skillRank, brandEffectiveness);
        }

        // =============================================================================
        // PUBLIC API - REMOVAL
        // =============================================================================

        /// <summary>
        /// Removes a specific effect instance.
        /// </summary>
        public bool RemoveEffect(StatusEffectInstance instance, EffectRemovalReason reason = EffectRemovalReason.MANUAL)
        {
            if (instance?.target == null)
                return false;

            if (!_effectsByTarget.TryGetValue(instance.target, out var effects))
                return false;

            if (!effects.Remove(instance))
                return false;

            Log($"Removed {instance.effectData?.displayName} from {instance.target.name}. Reason: {reason}");

            OnEffectRemoved?.Invoke(instance.target, instance, reason);
            EventBus.StatusEffectRemoved(instance.target, instance.EffectType);

            return true;
        }

        /// <summary>
        /// Removes all effects of a specific type from target.
        /// </summary>
        public int RemoveEffect(GameObject target, StatusEffectType effectType, EffectRemovalReason reason = EffectRemovalReason.MANUAL)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return 0;

            var toRemove = effects.Where(e => e.EffectType == effectType).ToList();

            foreach (var effect in toRemove)
            {
                effects.Remove(effect);
                OnEffectRemoved?.Invoke(target, effect, reason);
                EventBus.StatusEffectRemoved(target, effectType);
            }

            Log($"Removed {toRemove.Count} instances of {effectType} from {target.name}");
            return toRemove.Count;
        }

        /// <summary>
        /// Removes all effects from a target.
        /// </summary>
        public void RemoveAllEffects(GameObject target, EffectRemovalReason reason = EffectRemovalReason.MANUAL)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return;

            var toRemove = effects.ToList();
            effects.Clear();

            foreach (var effect in toRemove)
            {
                OnEffectRemoved?.Invoke(target, effect, reason);
                EventBus.StatusEffectRemoved(target, effect.EffectType);
            }

            Log($"Removed all {toRemove.Count} effects from {target.name}");
        }

        /// <summary>
        /// Cleanses debuffs from target (used by GRACE abilities).
        /// Returns number of effects cleansed.
        /// </summary>
        public int Cleanse(GameObject target, int maxCleanse = 1)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return 0;

            // Get harmful effects sorted by cleanse priority
            var debuffs = effects
                .Where(e => e.ShouldBeCleansed)
                .OrderByDescending(e => e.effectData?.cleansePriority ?? 0)
                .Take(maxCleanse)
                .ToList();

            foreach (var debuff in debuffs)
            {
                RemoveEffect(debuff, EffectRemovalReason.CLEANSED);
            }

            Log($"Cleansed {debuffs.Count} debuffs from {target.name}");
            return debuffs.Count;
        }

        /// <summary>
        /// Dispels buffs from target (used against enemies).
        /// Returns number of effects dispelled.
        /// </summary>
        public int Dispel(GameObject target, int maxDispel = 1)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return 0;

            var buffs = effects
                .Where(e => e.ShouldBeDispelled)
                .Take(maxDispel)
                .ToList();

            foreach (var buff in buffs)
            {
                RemoveEffect(buff, EffectRemovalReason.DISPELLED);
            }

            Log($"Dispelled {buffs.Count} buffs from {target.name}");
            return buffs.Count;
        }

        /// <summary>
        /// VOID special: Steals buffs from target.
        /// Returns stolen effects (caller should apply to VOID user).
        /// </summary>
        public List<StatusEffectInstance> StealBuffs(GameObject target, int maxSteal = 1)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return new List<StatusEffectInstance>();

            var buffs = effects
                .Where(e => e.ShouldBeDispelled)
                .Take(maxSteal)
                .ToList();

            foreach (var buff in buffs)
            {
                effects.Remove(buff);
                OnEffectRemoved?.Invoke(target, buff, EffectRemovalReason.STOLEN);
            }

            Log($"Stole {buffs.Count} buffs from {target.name}");
            return buffs;
        }

        // =============================================================================
        // PUBLIC API - QUERIES
        // =============================================================================

        /// <summary>
        /// Checks if target has a specific effect type.
        /// </summary>
        public bool HasEffect(GameObject target, StatusEffectType effectType)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return false;

            return effects.Any(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Checks if target has any effects of a category.
        /// </summary>
        public bool HasEffectCategory(GameObject target, EffectCategory category)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return false;

            return effects.Any(e => e.Category == category);
        }

        /// <summary>
        /// Gets a specific effect instance.
        /// </summary>
        public StatusEffectInstance GetEffect(GameObject target, StatusEffectType effectType)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return null;

            return effects.FirstOrDefault(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Gets all effects on a target.
        /// </summary>
        public IReadOnlyList<StatusEffectInstance> GetEffects(GameObject target)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return Array.Empty<StatusEffectInstance>();

            return effects;
        }

        /// <summary>
        /// Gets all effects of a category on target.
        /// </summary>
        public List<StatusEffectInstance> GetEffectsByCategory(GameObject target, EffectCategory category)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return new List<StatusEffectInstance>();

            return effects.Where(e => e.Category == category).ToList();
        }

        /// <summary>
        /// Gets the total stat modifier from all effects for a specific stat.
        /// </summary>
        public float GetStatModifier(GameObject target, Stat stat)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return 0f;

            return effects
                .Where(e => e.effectData?.targetStat == stat)
                .Sum(e => e.GetStatModValue());
        }

        /// <summary>
        /// Checks if target can act (not stunned, sleeping, etc.).
        /// </summary>
        public bool CanAct(GameObject target)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return true;

            return !effects.Any(e => e.effectData?.PreventsAction ?? false);
        }

        /// <summary>
        /// Gets the taunt source if target is taunted.
        /// </summary>
        public GameObject GetTauntSource(GameObject target)
        {
            var taunt = GetEffect(target, StatusEffectType.TAUNT);
            return taunt?.tauntSource;
        }

        /// <summary>
        /// Checks if target is stealthed.
        /// </summary>
        public bool IsStealthed(GameObject target)
        {
            return HasEffect(target, StatusEffectType.STEALTH);
        }

        /// <summary>
        /// Gets effect count on target.
        /// </summary>
        public int GetEffectCount(GameObject target)
        {
            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return 0;

            return effects.Count;
        }

        // =============================================================================
        // PUBLIC API - DAMAGE INTERACTION
        // =============================================================================

        /// <summary>
        /// Processes damage through effects (shields, damage modifiers).
        /// Returns modified damage.
        /// </summary>
        public float ProcessDamage(GameObject target, float damage, out float shieldAbsorbed)
        {
            shieldAbsorbed = 0f;

            if (target == null || !_effectsByTarget.TryGetValue(target, out var effects))
                return damage;

            float remainingDamage = damage;

            // Use reusable list for effects to remove (avoids allocation)
            _tempEffectList.Clear();

            // Apply shields first (iterate manually to avoid LINQ allocation)
            for (int i = 0; i < effects.Count && remainingDamage > 0f; i++)
            {
                var effect = effects[i];
                if (effect.EffectType != StatusEffectType.SHIELD)
                    continue;

                float absorbed = remainingDamage - effect.AbsorbDamage(remainingDamage);
                shieldAbsorbed += absorbed;
                remainingDamage -= absorbed;

                // Mark depleted shields for removal
                if (effect.shieldRemaining <= 0f)
                {
                    _tempEffectList.Add(effect);
                }
            }

            // Apply damage resistance (like Petrify)
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.effectData != null && effect.effectData.damageResistance > 0f)
                {
                    remainingDamage *= (1f - effect.effectData.damageResistance);
                }
            }

            // Find break-on-damage effects
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.effectData != null && effect.effectData.breaksOnDamage)
                {
                    _tempEffectList.Add(effect);
                }
            }

            // Remove effects after iteration
            for (int i = 0; i < _tempEffectList.Count; i++)
            {
                var effect = _tempEffectList[i];
                var reason = effect.EffectType == StatusEffectType.SHIELD
                    ? EffectRemovalReason.CONSUMED
                    : EffectRemovalReason.BROKEN;
                RemoveEffect(effect, reason);
            }

            return remainingDamage;
        }

        /// <summary>
        /// Gets lifesteal percentage for target.
        /// </summary>
        public float GetLifestealPercent(GameObject target)
        {
            var lifesteal = GetEffect(target, StatusEffectType.LIFESTEAL);
            return lifesteal?.potency ?? 0f;
        }

        /// <summary>
        /// Gets thorns damage for attacker when target is hit.
        /// </summary>
        public float GetThornsDamage(GameObject target)
        {
            var thorns = GetEffect(target, StatusEffectType.THORNS);
            return thorns?.potency ?? 0f;
        }

        // =============================================================================
        // PRIVATE - UPDATE LOOP
        // =============================================================================

        private void UpdateAllEffects(float deltaTime)
        {
            // Use reusable list to avoid allocation and handle collection modification
            _tempRemoveList.Clear();
            _tempTargetList.Clear();

            // Copy targets to avoid collection-modified-during-iteration
            foreach (var target in _effectsByTarget.Keys)
            {
                _tempTargetList.Add(target);
            }

            foreach (var target in _tempTargetList)
            {
                if (target == null)
                    continue;

                if (!_effectsByTarget.TryGetValue(target, out var effects))
                    continue;

                for (int i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    bool ticked = effect.UpdateTimer(deltaTime);

                    // Process tick
                    if (ticked && effect.IsTicking)
                    {
                        ProcessTick(target, effect);
                    }

                    // Check expiry
                    if (effect.IsExpired)
                    {
                        _tempRemoveList.Add((target, effect));
                    }
                }
            }

            // Remove expired effects
            foreach (var (target, effect) in _tempRemoveList)
            {
                RemoveEffect(effect, EffectRemovalReason.EXPIRED);
            }
        }

        private void ProcessTick(GameObject target, StatusEffectInstance effect)
        {
            switch (effect.Category)
            {
                case EffectCategory.DAMAGE:
                    float tickDamage = effect.GetTickDamage();
                    OnEffectTick?.Invoke(target, effect, tickDamage);
                    EventBus.StatusEffectTick(target, effect.EffectType, tickDamage);
                    break;

                case EffectCategory.BUFF:
                    if (effect.EffectType == StatusEffectType.REGEN)
                    {
                        float tickHeal = effect.GetTickHeal();
                        OnEffectTick?.Invoke(target, effect, tickHeal);
                        EventBus.StatusEffectTick(target, effect.EffectType, tickHeal);
                    }
                    break;
            }
        }

        // =============================================================================
        // PRIVATE - INITIALIZATION
        // =============================================================================

        private void LoadEffectDataCache()
        {
            // Load all StatusEffectData from Resources/StatusEffects/
            var allEffects = Resources.LoadAll<StatusEffectData>("StatusEffects");

            foreach (var effect in allEffects)
            {
                if (!_effectDataCache.ContainsKey(effect.effectType))
                {
                    _effectDataCache[effect.effectType] = effect;
                }
            }

            Log($"Loaded {_effectDataCache.Count} status effect definitions into cache");
        }

        /// <summary>
        /// Registers an effect data definition (for runtime creation).
        /// </summary>
        public void RegisterEffectData(StatusEffectData data)
        {
            if (data != null && !_effectDataCache.ContainsKey(data.effectType))
            {
                _effectDataCache[data.effectType] = data;
            }
        }

        // =============================================================================
        // PRIVATE - UTILITY
        // =============================================================================

        private void Log(string message)
        {
            if (_debugLogging)
            {
                Debug.Log($"[StatusEffectManager] {message}");
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Clears all effects (call on battle end).
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var kvp in _effectsByTarget)
            {
                var target = kvp.Key;
                var effects = kvp.Value;

                foreach (var effect in effects)
                {
                    // Check for null target (destroyed GameObject)
                    if (target != null)
                    {
                        OnEffectRemoved?.Invoke(target, effect, EffectRemovalReason.BATTLE_END);
                    }
                }
            }

            _effectsByTarget.Clear();
            Log("Cleared all status effects");
        }

        /// <summary>
        /// Removes a target from tracking (call when entity is destroyed).
        /// </summary>
        public void UnregisterTarget(GameObject target)
        {
            if (target == null)
                return;

            if (_effectsByTarget.TryGetValue(target, out var effects))
            {
                foreach (var effect in effects)
                {
                    OnEffectRemoved?.Invoke(target, effect, EffectRemovalReason.TARGET_DESTROYED);
                }

                _effectsByTarget.Remove(target);
            }
        }
    }

    // =============================================================================
    // ENUMS
    // =============================================================================

    /// <summary>
    /// Reason why an effect was blocked from being applied.
    /// </summary>
    public enum EffectBlockReason
    {
        MAX_EFFECTS,        // Hit effect cap
        ALREADY_ACTIVE,     // Same effect already on target
        IMMUNITY,           // Blocked by immunity buff
        EXHAUSTED,          // Can't receive buffs
        RESISTED            // Resisted due to high resistance
    }

    /// <summary>
    /// Reason why an effect was removed.
    /// </summary>
    public enum EffectRemovalReason
    {
        EXPIRED,            // Duration ended
        CLEANSED,           // Removed by cleanse ability
        DISPELLED,          // Dispelled by enemy
        STOLEN,             // Stolen by VOID
        CONSUMED,           // One-time effect consumed
        BROKEN,             // Broke on damage
        MANUAL,             // Manually removed
        BATTLE_END,         // Battle ended
        TARGET_DESTROYED    // Target was destroyed
    }
}

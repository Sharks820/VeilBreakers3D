using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Main AI controller that manages a combatant's decision-making.
    /// Attaches to a Combatant to provide autonomous combat behavior.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class GambitController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("AI Configuration")]
        [Tooltip("AI personality defining behavior weights")]
        [SerializeField] private AIPersonality _personality;

        [Tooltip("Custom rule set (optional, uses defaults if null)")]
        [SerializeField] private GambitRuleSetAsset _customRuleSet;

        [Header("Quick Presets")]
        [Tooltip("Focus Attack: +30% damage weights")]
        [SerializeField] private bool _focusAttack = false;

        [Tooltip("Focus Defend: +30% survival weights")]
        [SerializeField] private bool _focusDefend = false;

        [Tooltip("Focus Heal: Priority healing (support only)")]
        [SerializeField] private bool _focusHeal = false;

        [Tooltip("Protect specific ally")]
        [SerializeField] private Combatant _protectedAlly;

        [Header("Ultimate Override")]
        [Tooltip("Time window for player to override ultimate target")]
        [SerializeField] private float _ultimateOverrideWindow = 5f;

        [Tooltip("Is ultimate ready and waiting for player input?")]
        [SerializeField] private bool _ultimateReady = false;

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private Combatant _combatant;
        private GambitEvaluator _evaluator;
        private GambitRuleSet _activeRuleSet;
        private BattleContext _battleContext;

        private bool _isInitialized = false;
        private float _ultimateReadyTime;
        private bool _ultimateOverridden = false;
        private Combatant _playerOverrideTarget;

        // Event tracking
        private int _killCount = 0;
        private float _lastKillTime;
        private const float MOMENTUM_WINDOW = 5f;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _combatant = GetComponent<Combatant>();
            if (_combatant == null)
            {
                Debug.LogError($"[GambitController] No Combatant component found on {gameObject.name}");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (_combatant == null) return;
            Initialize();
        }

        private void OnEnable()
        {
            // Subscribe to events
            EventBus.OnUnitDefeated += OnUnitDefeated;
        }

        private void OnDisable()
        {
            EventBus.OnUnitDefeated -= OnUnitDefeated;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the AI controller with personality and rules.
        /// </summary>
        public void Initialize()
        {
            if (_combatant == null) return;

            // Auto-assign personality based on brand if not set
            if (_personality == null)
            {
                _personality = AIPersonality.CreateDefault(_combatant.Brand);
            }

            // Create rule set
            _activeRuleSet = _customRuleSet != null
                ? _customRuleSet.CreateRuleSet()
                : CreateDefaultRuleSet(_combatant.Brand);

            // Create evaluator
            _evaluator = new GambitEvaluator(_personality, _activeRuleSet);

            _isInitialized = true;

            Debug.Log($"[GambitController] Initialized AI for {_combatant.DisplayName} with {_personality.personalityName}");
        }

        /// <summary>
        /// Set battle context for this controller.
        /// </summary>
        public void SetBattleContext(Combatant[] allies, Combatant[] enemies, Managers.StatusEffectManager statusManager)
        {
            _battleContext = new BattleContext(allies, enemies, statusManager);
        }

        // =============================================================================
        // DECISION MAKING
        // =============================================================================

        /// <summary>
        /// Decide and execute the best action for this turn.
        /// Returns true if an action was executed.
        /// </summary>
        public bool DecideAndAct()
        {
            if (!_isInitialized || !_combatant.IsAlive)
            {
                Debug.LogWarning($"[GambitController] Cannot act: initialized={_isInitialized}, alive={_combatant.IsAlive}");
                return false;
            }

            if (_battleContext == null)
            {
                Debug.LogError("[GambitController] Battle context not set!");
                return false;
            }

            // Check for ultimate override
            if (_ultimateReady && !_ultimateOverridden)
            {
                if (Time.time - _ultimateReadyTime > _ultimateOverrideWindow)
                {
                    // Window expired, auto-target ultimate
                    ExecuteUltimate(GetAutoUltimateTarget());
                    return true;
                }
                // Still waiting for player input
                return false;
            }

            // Normal decision making
            ScoredAction best = _evaluator.EvaluateBestAction(_combatant, _battleContext);

            if (!best.IsValid)
            {
                Debug.LogWarning($"[GambitController] No valid action found for {_combatant.DisplayName}");
                return false;
            }

            // Apply quick presets
            ApplyQuickPresets(ref best);

            // Execute the action
            return ExecuteAction(best);
        }

        /// <summary>
        /// Execute a scored action.
        /// </summary>
        private bool ExecuteAction(ScoredAction action)
        {
            if (action.rule == null) return false;

            ActionResult result;
            bool success = action.rule.Execute(_combatant, _battleContext, out result);

            if (success)
            {
                Debug.Log($"[GambitController] {_combatant.DisplayName}: {result.message}");

                // Fire appropriate events based on action type
                EventBus.SkillUsed(_combatant.CombatantId, action.rule.ruleName);
            }
            else
            {
                Debug.LogWarning($"[GambitController] Action failed: {result.message}");
            }

            return success;
        }

        // =============================================================================
        // ULTIMATE HANDLING
        // =============================================================================

        /// <summary>
        /// Called when ultimate becomes ready.
        /// Starts the player override window.
        /// </summary>
        public void OnUltimateReady()
        {
            _ultimateReady = true;
            _ultimateReadyTime = Time.time;
            _ultimateOverridden = false;

            // Visual feedback
            Debug.Log($"[GambitController] {_combatant.DisplayName}'s Ultimate is ready! (F1/F2/F3 to override)");
        }

        /// <summary>
        /// Player overrides the ultimate target.
        /// </summary>
        public void SetUltimateTarget(Combatant target)
        {
            if (!_ultimateReady) return;

            _playerOverrideTarget = target;
            _ultimateOverridden = true;
            ExecuteUltimate(target);
        }

        /// <summary>
        /// Execute ultimate ability.
        /// </summary>
        private void ExecuteUltimate(Combatant target)
        {
            _ultimateReady = false;

            Debug.Log($"[GambitController] {_combatant.DisplayName} uses Ultimate on {target?.DisplayName ?? "auto-target"}!");

            // TODO: Integrate with ability system when available
            EventBus.SkillUsed(_combatant.CombatantId, "Ultimate");
        }

        /// <summary>
        /// Get automatic ultimate target based on personality.
        /// </summary>
        private Combatant GetAutoUltimateTarget()
        {
            switch (_personality.ultimateTargetMode)
            {
                case AIPersonality.UltimateTargetMode.LOWEST_HP_ENEMY:
                    return _battleContext.GetLowestHpEnemy();

                case AIPersonality.UltimateTargetMode.HIGHEST_HP_ENEMY:
                    return _battleContext.GetHighestHpEnemy();

                case AIPersonality.UltimateTargetMode.LOWEST_HP_ALLY:
                    return _battleContext.GetLowestHpAlly(_combatant);

                case AIPersonality.UltimateTargetMode.ENEMY_HEALER:
                    return FindEnemyHealer() ?? _battleContext.GetLowestHpEnemy();

                case AIPersonality.UltimateTargetMode.ENEMY_CLUSTER:
                    // TODO: Implement cluster detection
                    return _battleContext.GetLowestHpEnemy();

                default:
                    return _battleContext.GetLowestHpEnemy();
            }
        }

        private Combatant FindEnemyHealer()
        {
            var enemies = _battleContext.GetEnemies();
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && _battleContext.IsHealer(enemy))
                    return enemy;
            }
            return null;
        }

        // =============================================================================
        // QUICK PRESETS
        // =============================================================================

        /// <summary>
        /// Apply quick preset modifiers to action selection.
        /// </summary>
        private void ApplyQuickPresets(ref ScoredAction action)
        {
            // Focus Attack: prefer damage actions
            if (_focusAttack && action.rule != null)
            {
                if (action.rule.action.actionType == GambitAction.ActionType.BASIC_ATTACK ||
                    action.rule.action.actionType == GambitAction.ActionType.USE_ABILITY ||
                    action.rule.action.actionType == GambitAction.ActionType.EXECUTE)
                {
                    // Already attacking, boost score mentally
                }
            }

            // Focus Defend: prefer survival
            if (_focusDefend)
            {
                // Could swap to defensive action if close score
            }

            // Protect Ally: override target for guard/heal
            if (_protectedAlly != null && _protectedAlly.IsAlive)
            {
                if (action.rule?.action.actionType == GambitAction.ActionType.GUARD_ALLY ||
                    action.rule?.action.actionType == GambitAction.ActionType.HEAL_ALLY)
                {
                    // Could override target to protected ally
                }
            }
        }

        /// <summary>
        /// Enable Focus Attack preset.
        /// </summary>
        public void SetFocusAttack(bool enabled)
        {
            _focusAttack = enabled;
            if (enabled)
            {
                _focusDefend = false;
                _focusHeal = false;
            }
        }

        /// <summary>
        /// Enable Focus Defend preset.
        /// </summary>
        public void SetFocusDefend(bool enabled)
        {
            _focusDefend = enabled;
            if (enabled)
            {
                _focusAttack = false;
                _focusHeal = false;
            }
        }

        /// <summary>
        /// Enable Focus Heal preset.
        /// </summary>
        public void SetFocusHeal(bool enabled)
        {
            _focusHeal = enabled;
            if (enabled)
            {
                _focusAttack = false;
                _focusDefend = false;
            }
        }

        /// <summary>
        /// Set protected ally for guard priority.
        /// </summary>
        public void SetProtectedAlly(Combatant ally)
        {
            _protectedAlly = ally;
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnUnitDefeated(string unitId)
        {
            // Track kills for momentum (SAVAGE brand)
            if (_personality.tracksMomentum)
            {
                // Check if we caused the kill
                // TODO: Implement kill tracking when damage attribution is available
                _killCount++;
                _lastKillTime = Time.time;
            }
        }

        /// <summary>
        /// Check if we have momentum bonus (recent kills).
        /// </summary>
        public bool HasMomentum()
        {
            if (!_personality.tracksMomentum) return false;
            return _killCount > 0 && (Time.time - _lastKillTime) < MOMENTUM_WINDOW;
        }

        /// <summary>
        /// Get current momentum bonus multiplier.
        /// </summary>
        public float GetMomentumBonus()
        {
            if (!HasMomentum()) return 1f;
            return 1f + (Mathf.Min(_killCount, 5) * 0.1f); // 10% per kill, max 50%
        }

        // =============================================================================
        // DEFAULT RULE SETS
        // =============================================================================

        /// <summary>
        /// Creates default gambit rules for a brand.
        /// </summary>
        private GambitRuleSet CreateDefaultRuleSet(Brand brand)
        {
            var ruleSet = new GambitRuleSet { setName = $"{brand} Default" };
            var rules = new List<GambitRule>();

            // Universal rules (all brands)
            AddUniversalRules(rules);

            // Brand-specific rules
            switch (brand)
            {
                case Brand.IRON:
                    AddIronRules(rules);
                    break;
                case Brand.SAVAGE:
                    AddSavageRules(rules);
                    break;
                case Brand.SURGE:
                    AddSurgeRules(rules);
                    break;
                case Brand.VENOM:
                    AddVenomRules(rules);
                    break;
                case Brand.DREAD:
                    AddDreadRules(rules);
                    break;
                case Brand.LEECH:
                    AddLeechRules(rules);
                    break;
                case Brand.GRACE:
                    AddGraceRules(rules);
                    break;
                case Brand.MEND:
                    AddMendRules(rules);
                    break;
                case Brand.RUIN:
                    AddRuinRules(rules);
                    break;
                case Brand.VOID:
                    AddVoidRules(rules);
                    break;
            }

            // Fallback rule
            rules.Add(GambitRule.CreateAlways(
                "Fallback Attack",
                GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK),
                PriorityBucket.LOW,
                1
            ));

            ruleSet.rules = rules.ToArray();
            return ruleSet;
        }

        private void AddUniversalRules(List<GambitRule> rules)
        {
            // Execute low HP enemies
            rules.Add(GambitRule.Create(
                "Execute Low HP",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 25f),
                GambitAction.Create(GambitAction.ActionType.EXECUTE, GambitAction.TargetSelection.LOWEST_HP_ENEMY),
                PriorityBucket.HIGH,
                90
            ));

            // Focus debuffed targets
            rules.Add(GambitRule.Create(
                "Focus Debuffed",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ENEMY_HAS_STATUS, StatusEffectType.ARMOR_SHRED),
                GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK, GambitAction.TargetSelection.DEBUFFED_ENEMY),
                PriorityBucket.STANDARD,
                80
            ));
        }

        private void AddIronRules(List<GambitRule> rules)
        {
            // Emergency guard for critical ally
            rules.Add(GambitRule.Create(
                "Emergency Guard",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_CRITICAL),
                GambitAction.Create(GambitAction.ActionType.GUARD_ALLY, GambitAction.TargetSelection.LOWEST_HP_ALLY),
                PriorityBucket.CRITICAL,
                95
            ));

            // Taunt when ally being attacked
            rules.Add(GambitRule.Create(
                "Taunt Threat",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_BEING_ATTACKED),
                GambitAction.Create(GambitAction.ActionType.TAUNT),
                PriorityBucket.HIGH,
                80
            ));

            // Self defend when low
            rules.Add(GambitRule.Create(
                "Self Defend",
                GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 30f),
                GambitAction.Create(GambitAction.ActionType.DEFEND_SELF),
                PriorityBucket.HIGH,
                70
            ));
        }

        private void AddSavageRules(List<GambitRule> rules)
        {
            // All-in on execute range
            rules.Add(GambitRule.Create(
                "Savage Execute",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 30f),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_1, GambitAction.TargetSelection.LOWEST_HP_ENEMY),
                PriorityBucket.HIGH,
                95
            ));

            // Target healers
            rules.Add(GambitRule.Create(
                "Kill Healer",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_IS_HEALER),
                GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK, GambitAction.TargetSelection.ENEMY_HEALER),
                PriorityBucket.STANDARD,
                75
            ));
        }

        private void AddSurgeRules(List<GambitRule> rules)
        {
            // AOE on clustered enemies
            rules.Add(GambitRule.Create(
                "AOE Cluster",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMIES_CLUSTERED, countValue: 3),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_2, GambitAction.TargetSelection.ENEMY_CLUSTER),
                PriorityBucket.HIGH,
                85
            ));

            // Kite when in danger
            rules.Add(GambitRule.Create(
                "Retreat",
                GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 40f),
                GambitAction.Create(GambitAction.ActionType.RETREAT),
                PriorityBucket.HIGH,
                80
            ));
        }

        private void AddVenomRules(List<GambitRule> rules)
        {
            // Heal reduction on healers first
            rules.Add(GambitRule.Create(
                "Shutdown Healer",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_IS_HEALER),
                new GambitAction
                {
                    actionType = GambitAction.ActionType.DEBUFF_ENEMY,
                    targetSelection = GambitAction.TargetSelection.ENEMY_HEALER,
                    statusEffect = StatusEffectType.HEAL_BLOCK
                },
                PriorityBucket.HIGH,
                95
            ));

            // Spread DoTs
            rules.Add(GambitRule.Create(
                "Apply Poison",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ENEMY_NOT_HAS_STATUS, StatusEffectType.POISON),
                new GambitAction
                {
                    actionType = GambitAction.ActionType.DEBUFF_ENEMY,
                    statusEffect = StatusEffectType.POISON
                },
                PriorityBucket.STANDARD,
                80
            ));
        }

        private void AddDreadRules(List<GambitRule> rules)
        {
            // Interrupt casts
            rules.Add(GambitRule.Create(
                "Interrupt Cast",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_IS_CASTING),
                GambitAction.Create(GambitAction.ActionType.INTERRUPT, GambitAction.TargetSelection.CASTING_ENEMY),
                PriorityBucket.CRITICAL,
                100
            ));

            // CC uncontrolled threats
            rules.Add(GambitRule.Create(
                "Lock Down Threat",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ENEMY_NOT_HAS_STATUS, StatusEffectType.STUN),
                new GambitAction
                {
                    actionType = GambitAction.ActionType.DEBUFF_ENEMY,
                    statusEffect = StatusEffectType.STUN
                },
                PriorityBucket.HIGH,
                85
            ));
        }

        private void AddLeechRules(List<GambitRule> rules)
        {
            // Desperate drain when low
            rules.Add(GambitRule.Create(
                "Desperate Drain",
                GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 25f),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_1, GambitAction.TargetSelection.HIGHEST_HP_ENEMY),
                PriorityBucket.CRITICAL,
                95
            ));

            // Sustain drain when mid HP
            rules.Add(GambitRule.Create(
                "Sustain Drain",
                GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 50f),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_1, GambitAction.TargetSelection.HIGHEST_HP_ENEMY),
                PriorityBucket.HIGH,
                75
            ));
        }

        private void AddGraceRules(List<GambitRule> rules)
        {
            // Emergency heal
            rules.Add(GambitRule.Create(
                "Emergency Heal",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_HP_BELOW, 25f),
                GambitAction.Create(GambitAction.ActionType.HEAL_ALLY, GambitAction.TargetSelection.LOWEST_HP_ALLY),
                PriorityBucket.CRITICAL,
                100
            ));

            // Cleanse dangerous debuffs
            rules.Add(GambitRule.Create(
                "Cleanse Death",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ALLY_HAS_STATUS, StatusEffectType.DOOM),
                GambitAction.Create(GambitAction.ActionType.CLEANSE_ALLY, GambitAction.TargetSelection.DEBUFFED_ALLY),
                PriorityBucket.CRITICAL,
                99
            ));

            // Normal healing
            rules.Add(GambitRule.Create(
                "Heal Wounded",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_HP_BELOW, 70f),
                GambitAction.Create(GambitAction.ActionType.HEAL_ALLY, GambitAction.TargetSelection.LOWEST_HP_ALLY),
                PriorityBucket.STANDARD,
                80
            ));
        }

        private void AddMendRules(List<GambitRule> rules)
        {
            // Proactive shield on low HP ally
            rules.Add(GambitRule.Create(
                "Emergency Shield",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_HP_BELOW, 40f),
                GambitAction.Create(GambitAction.ActionType.BUFF_ALLY, GambitAction.TargetSelection.LOWEST_HP_ALLY),
                PriorityBucket.HIGH,
                90
            ));

            // Shield coverage
            rules.Add(GambitRule.Create(
                "Shield Ally",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ALLY_HAS_STATUS, StatusEffectType.SHIELD) { negate = true },
                new GambitAction
                {
                    actionType = GambitAction.ActionType.BUFF_ALLY,
                    statusEffect = StatusEffectType.SHIELD
                },
                PriorityBucket.STANDARD,
                70
            ));
        }

        private void AddRuinRules(List<GambitRule> rules)
        {
            // Big AOE on cluster
            rules.Add(GambitRule.Create(
                "Mass Destruction",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMIES_CLUSTERED, countValue: 4),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_2, GambitAction.TargetSelection.ENEMY_CLUSTER),
                PriorityBucket.HIGH,
                95
            ));

            // Cleanup sweep
            rules.Add(GambitRule.CreateMultiCondition(
                "Cleanup Sweep",
                new[]
                {
                    GambitCondition.Create(GambitCondition.ConditionType.ENEMY_COUNT_ABOVE, countValue: 2),
                    GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 30f)
                },
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_1),
                PriorityBucket.HIGH,
                90
            ));
        }

        private void AddVoidRules(List<GambitRule> rules)
        {
            // Desperate chaos when team losing
            rules.Add(GambitRule.Create(
                "Desperate Chaos",
                GambitCondition.Create(GambitCondition.ConditionType.ALLY_CRITICAL),
                GambitAction.CreateAbilityAction(AbilitySlot.SKILL_3),
                PriorityBucket.CRITICAL,
                95
            ));

            // Dispel strong buffs
            rules.Add(GambitRule.Create(
                "Steal Buff",
                GambitCondition.CreateStatusCondition(GambitCondition.ConditionType.ENEMY_HAS_STATUS, StatusEffectType.ATTACK_UP),
                GambitAction.Create(GambitAction.ActionType.DEBUFF_ENEMY),
                PriorityBucket.HIGH,
                80
            ));
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        public AIPersonality Personality => _personality;
        public bool IsInitialized => _isInitialized;
        public bool UltimateReady => _ultimateReady;
        public Combatant ProtectedAlly => _protectedAlly;
    }

    /// <summary>
    /// ScriptableObject wrapper for GambitRuleSet.
    /// Allows defining custom rule sets in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "GambitRuleSet", menuName = "VeilBreakers/Gambit Rule Set")]
    public class GambitRuleSetAsset : ScriptableObject
    {
        [SerializeField] private string setName = "Custom";
        [SerializeField] private GambitRule[] rules;

        public GambitRuleSet CreateRuleSet()
        {
            return new GambitRuleSet
            {
                setName = setName,
                rules = rules ?? Array.Empty<GambitRule>()
            };
        }
    }
}

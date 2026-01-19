using System;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Defines an action that can be executed by a gambit rule.
    /// Actions are the THEN part of "IF condition THEN action".
    /// </summary>
    [Serializable]
    public class GambitAction
    {
        // =============================================================================
        // ACTION TYPES
        // =============================================================================

        public enum ActionType
        {
            // Combat actions
            BASIC_ATTACK,           // Use basic attack
            USE_ABILITY,            // Use specific ability slot
            USE_ULTIMATE,           // Use ultimate ability
            DEFEND_SELF,            // Enter defensive stance
            GUARD_ALLY,             // Guard specific ally

            // Support actions
            HEAL_ALLY,              // Heal target ally
            BUFF_ALLY,              // Apply buff to ally
            CLEANSE_ALLY,           // Remove debuffs from ally

            // Offensive support
            DEBUFF_ENEMY,           // Apply debuff to enemy

            // Movement actions
            MOVE_TO_TARGET,         // Move toward target
            RETREAT,                // Move away from enemies
            REPOSITION,             // Find better position

            // Special actions
            WAIT,                   // Skip turn / delay
            INTERRUPT,              // Interrupt enemy cast
            TAUNT,                  // Force enemies to target self
            EXECUTE,                // All-in burst on low HP target
        }

        // =============================================================================
        // TARGET SELECTION
        // =============================================================================

        public enum TargetSelection
        {
            // Enemy targets
            LOWEST_HP_ENEMY,        // Target with lowest HP
            HIGHEST_HP_ENEMY,       // Target with highest HP
            NEAREST_ENEMY,          // Closest enemy
            ENEMY_HEALER,           // Enemy healer if exists
            ENEMY_DPS,              // Enemy damage dealer
            DEBUFFED_ENEMY,         // Enemy with debuffs
            CASTING_ENEMY,          // Enemy currently casting

            // Ally targets
            LOWEST_HP_ALLY,         // Ally with lowest HP
            SELF,                   // Target self
            SPECIFIC_ALLY,          // Designated ally (by index)
            DEBUFFED_ALLY,          // Ally with debuffs to cleanse

            // Area targets
            ENEMY_CLUSTER,          // Center of enemy cluster
            ALLY_CLUSTER,           // Center of ally cluster

            // Auto selection
            AUTO                    // Let AI decide based on action type
        }

        // =============================================================================
        // FIELDS
        // =============================================================================

        [Tooltip("Type of action to perform")]
        public ActionType actionType = ActionType.BASIC_ATTACK;

        [Tooltip("How to select the target")]
        public TargetSelection targetSelection = TargetSelection.AUTO;

        [Tooltip("Ability slot to use (for USE_ABILITY)")]
        public AbilitySlot abilitySlot = AbilitySlot.SKILL_1;

        [Tooltip("Specific ally index (for SPECIFIC_ALLY target)")]
        public int allyIndex = 0;

        [Tooltip("Status effect to apply/check (for buff/debuff actions)")]
        public StatusEffectType statusEffect = StatusEffectType.NONE;

        // =============================================================================
        // EXECUTION
        // =============================================================================

        /// <summary>
        /// Execute this action for the given combatant.
        /// Returns true if the action was executed successfully.
        /// </summary>
        public bool Execute(Combatant self, BattleContext context, out ActionResult result)
        {
            result = new ActionResult();
            Combatant target = SelectTarget(self, context);

            switch (actionType)
            {
                case ActionType.BASIC_ATTACK:
                    return ExecuteBasicAttack(self, target, context, ref result);

                case ActionType.USE_ABILITY:
                    return ExecuteAbility(self, target, abilitySlot, context, ref result);

                case ActionType.USE_ULTIMATE:
                    return ExecuteAbility(self, target, AbilitySlot.ULTIMATE, context, ref result);

                case ActionType.DEFEND_SELF:
                    return ExecuteDefend(self, null, context, ref result);

                case ActionType.GUARD_ALLY:
                    return ExecuteGuard(self, target, context, ref result);

                case ActionType.HEAL_ALLY:
                    return ExecuteHeal(self, target, context, ref result);

                case ActionType.BUFF_ALLY:
                    return ExecuteBuff(self, target, context, ref result);

                case ActionType.CLEANSE_ALLY:
                    return ExecuteCleanse(self, target, context, ref result);

                case ActionType.DEBUFF_ENEMY:
                    return ExecuteDebuff(self, target, context, ref result);

                case ActionType.WAIT:
                    result.success = true;
                    result.message = $"{self.DisplayName} waits.";
                    return true;

                case ActionType.INTERRUPT:
                    return ExecuteInterrupt(self, target, context, ref result);

                case ActionType.TAUNT:
                    return ExecuteTaunt(self, context, ref result);

                case ActionType.EXECUTE:
                    return ExecuteExecute(self, target, context, ref result);

                case ActionType.MOVE_TO_TARGET:
                case ActionType.RETREAT:
                case ActionType.REPOSITION:
                    // Movement actions - placeholder until positioning system
                    result.success = true;
                    result.message = $"{self.DisplayName} repositions.";
                    return true;

                default:
                    result.success = false;
                    result.message = $"Unknown action type: {actionType}";
                    return false;
            }
        }

        /// <summary>
        /// Select appropriate target based on target selection type and action.
        /// </summary>
        private Combatant SelectTarget(Combatant self, BattleContext context)
        {
            // Auto-select based on action type
            if (targetSelection == TargetSelection.AUTO)
            {
                return AutoSelectTarget(self, context);
            }

            switch (targetSelection)
            {
                case TargetSelection.LOWEST_HP_ENEMY:
                    return context.GetLowestHpEnemy();

                case TargetSelection.HIGHEST_HP_ENEMY:
                    return context.GetHighestHpEnemy();

                case TargetSelection.ENEMY_HEALER:
                    return FindEnemyHealer(context);

                case TargetSelection.DEBUFFED_ENEMY:
                    return FindDebuffedEnemy(context);

                case TargetSelection.CASTING_ENEMY:
                    return FindCastingEnemy(context);

                case TargetSelection.LOWEST_HP_ALLY:
                    return context.GetLowestHpAlly(self);

                case TargetSelection.SELF:
                    return self;

                case TargetSelection.SPECIFIC_ALLY:
                    var allies = context.GetAllies();
                    if (allyIndex >= 0 && allyIndex < allies.Length)
                        return allies[allyIndex];
                    return self;

                case TargetSelection.DEBUFFED_ALLY:
                    return FindDebuffedAlly(self, context);

                default:
                    return context.GetLowestHpEnemy();
            }
        }

        /// <summary>
        /// Auto-select the most appropriate target based on action type.
        /// </summary>
        private Combatant AutoSelectTarget(Combatant self, BattleContext context)
        {
            switch (actionType)
            {
                case ActionType.BASIC_ATTACK:
                case ActionType.USE_ABILITY:
                case ActionType.DEBUFF_ENEMY:
                case ActionType.EXECUTE:
                    return context.GetLowestHpEnemy();

                case ActionType.HEAL_ALLY:
                case ActionType.BUFF_ALLY:
                case ActionType.CLEANSE_ALLY:
                    return context.GetLowestHpAlly(self) ?? self;

                case ActionType.GUARD_ALLY:
                    return context.GetLowestHpAlly(self);

                case ActionType.DEFEND_SELF:
                case ActionType.WAIT:
                    return self;

                case ActionType.INTERRUPT:
                    return FindCastingEnemy(context) ?? context.GetLowestHpEnemy();

                case ActionType.TAUNT:
                    return context.GetLowestHpEnemy();

                default:
                    return context.GetLowestHpEnemy();
            }
        }

        // =============================================================================
        // TARGET FINDERS
        // =============================================================================

        private Combatant FindEnemyHealer(BattleContext context)
        {
            var enemies = context.GetEnemies();
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && context.IsHealer(enemy))
                    return enemy;
            }
            return context.GetLowestHpEnemy();
        }

        private Combatant FindDebuffedEnemy(BattleContext context)
        {
            var enemies = context.GetEnemies();
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive &&
                    context.HasStatus(enemy, StatusEffectType.ARMOR_SHRED))
                    return enemy;
            }
            return context.GetLowestHpEnemy();
        }

        private Combatant FindCastingEnemy(BattleContext context)
        {
            var enemies = context.GetEnemies();
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && context.IsCasting(enemy))
                    return enemy;
            }
            return null;
        }

        private Combatant FindDebuffedAlly(Combatant self, BattleContext context)
        {
            var allies = context.GetAllies();
            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                if (ally != null && ally != self && ally.IsAlive)
                {
                    // Check for harmful debuffs
                    if (context.HasStatus(ally, StatusEffectType.POISON) ||
                        context.HasStatus(ally, StatusEffectType.BURN) ||
                        context.HasStatus(ally, StatusEffectType.STUN))
                        return ally;
                }
            }
            return null;
        }

        // =============================================================================
        // ACTION EXECUTORS
        // =============================================================================

        private bool ExecuteBasicAttack(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for attack.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.abilitySlot = AbilitySlot.BASIC_ATTACK;
            result.success = true;
            result.message = $"{self.DisplayName} attacks {target.DisplayName}!";
            return true;
        }

        private bool ExecuteAbility(Combatant self, Combatant target, AbilitySlot slot, BattleContext context, ref ActionResult result)
        {
            if (!context.IsAbilityReady(self, slot))
            {
                result.success = false;
                result.message = $"Ability {slot} is on cooldown.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.abilitySlot = slot;
            result.success = true;
            result.message = $"{self.DisplayName} uses {slot}!";
            return true;
        }

        private bool ExecuteDefend(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            self.StartDefend(DefenseAction.DEFEND_SELF);

            result.action = actionType;
            result.source = self;
            result.target = self;
            result.success = true;
            result.message = $"{self.DisplayName} takes a defensive stance!";
            return true;
        }

        private bool ExecuteGuard(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No ally to guard.";
                return false;
            }

            self.StartDefend(DefenseAction.GUARD_ALLY, target);

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.success = true;
            result.message = $"{self.DisplayName} guards {target.DisplayName}!";
            return true;
        }

        private bool ExecuteHeal(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for healing.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.success = true;
            result.message = $"{self.DisplayName} heals {target.DisplayName}!";
            return true;
        }

        private bool ExecuteBuff(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for buff.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.statusEffect = statusEffect;
            result.success = true;
            result.message = $"{self.DisplayName} buffs {target.DisplayName}!";
            return true;
        }

        private bool ExecuteCleanse(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for cleanse.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.success = true;
            result.message = $"{self.DisplayName} cleanses {target.DisplayName}!";
            return true;
        }

        private bool ExecuteDebuff(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for debuff.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.statusEffect = statusEffect;
            result.success = true;
            result.message = $"{self.DisplayName} debuffs {target.DisplayName}!";
            return true;
        }

        private bool ExecuteInterrupt(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for interrupt.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.success = true;
            result.message = $"{self.DisplayName} interrupts {target.DisplayName}!";
            return true;
        }

        private bool ExecuteTaunt(Combatant self, BattleContext context, ref ActionResult result)
        {
            result.action = actionType;
            result.source = self;
            result.target = self;
            result.success = true;
            result.message = $"{self.DisplayName} taunts all enemies!";
            return true;
        }

        private bool ExecuteExecute(Combatant self, Combatant target, BattleContext context, ref ActionResult result)
        {
            if (target == null || !target.IsAlive)
            {
                result.success = false;
                result.message = "No valid target for execute.";
                return false;
            }

            result.action = actionType;
            result.source = self;
            result.target = target;
            result.isExecute = true;
            result.success = true;
            result.message = $"{self.DisplayName} executes {target.DisplayName}!";
            return true;
        }

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Creates an action programmatically.
        /// </summary>
        public static GambitAction Create(ActionType type, TargetSelection target = TargetSelection.AUTO)
        {
            return new GambitAction
            {
                actionType = type,
                targetSelection = target
            };
        }

        /// <summary>
        /// Creates an ability action.
        /// </summary>
        public static GambitAction CreateAbilityAction(AbilitySlot slot, TargetSelection target = TargetSelection.AUTO)
        {
            return new GambitAction
            {
                actionType = ActionType.USE_ABILITY,
                abilitySlot = slot,
                targetSelection = target
            };
        }

        public override string ToString()
        {
            return actionType switch
            {
                ActionType.BASIC_ATTACK => $"Attack {targetSelection}",
                ActionType.USE_ABILITY => $"Use {abilitySlot} on {targetSelection}",
                ActionType.DEFEND_SELF => "Defend",
                ActionType.GUARD_ALLY => $"Guard {targetSelection}",
                ActionType.HEAL_ALLY => $"Heal {targetSelection}",
                ActionType.EXECUTE => $"Execute {targetSelection}",
                _ => $"{actionType} -> {targetSelection}"
            };
        }
    }

    /// <summary>
    /// Result of executing a gambit action.
    /// </summary>
    public struct ActionResult
    {
        public bool success;
        public string message;
        public GambitAction.ActionType action;
        public Combatant source;
        public Combatant target;
        public AbilitySlot abilitySlot;
        public StatusEffectType statusEffect;
        public bool isExecute;
        public float damageDealt;
        public float healingDone;
    }
}

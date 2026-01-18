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

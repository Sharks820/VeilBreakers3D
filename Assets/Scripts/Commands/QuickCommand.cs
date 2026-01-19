using System;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Commands
{
    /// <summary>
    /// Types of quick commands that can be issued to allies.
    /// </summary>
    public enum QuickCommandType
    {
        NONE = 0,

        // Direct Commands
        ATTACK_TARGET = 1,          // Attack player's current target
        DEFEND_TARGET = 2,          // Guard a specific ally
        DEFEND_PLAYER = 3,          // Guard the player
        ON_ME = 4,                  // Come to player, auto-defend
        FALL_BACK = 5,              // Retreat from current position
        REPOSITION = 6,             // Move to specific location
        RETURN_TO_FORMATION = 7,    // Go back to default position

        // Tactical Presets
        PRESET_AGGRESSIVE = 10,     // Prioritize damage
        PRESET_DEFENSIVE = 11,      // Prioritize survival
        PRESET_SUPPORT = 12,        // Prioritize healing/buffs
        PRESET_FOCUS_TARGET = 13,   // All attacks on player's target
        PRESET_PROTECT_PLAYER = 14  // Stay near player
    }

    /// <summary>
    /// Target type required by a command.
    /// </summary>
    public enum CommandTargetType
    {
        NONE,           // No target needed
        ENEMY,          // Requires enemy target
        ALLY,           // Requires ally target
        GROUND,         // Requires ground position
        AUTO            // Uses current/default target
    }

    /// <summary>
    /// State of a quick command during execution.
    /// </summary>
    public enum CommandState
    {
        IDLE,           // Not executing
        MOVING,         // Moving to position
        EXECUTING,      // Performing action
        COMPLETED,      // Finished successfully
        CANCELLED       // Interrupted/cancelled
    }

    /// <summary>
    /// Data structure for a quick command instance.
    /// </summary>
    [Serializable]
    public class QuickCommandInstance
    {
        // =============================================================================
        // FIELDS
        // =============================================================================

        public QuickCommandType commandType;
        public CommandState state;
        public Combatant issuer;        // Player
        public Combatant executor;      // Ally executing command
        public Combatant targetUnit;    // Target ally/enemy if applicable
        public Vector3 targetPosition;  // Ground position if applicable
        public float startTime;
        public float duration;          // Estimated duration

        // On Me specific
        public bool onMeAutoDefend;
        public bool onMeReformPending;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public bool IsActive => state == CommandState.MOVING || state == CommandState.EXECUTING;
        public bool NeedsTarget => GetTargetType(commandType) != CommandTargetType.NONE &&
                                  GetTargetType(commandType) != CommandTargetType.AUTO;

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Creates a new quick command instance.
        /// </summary>
        public static QuickCommandInstance Create(QuickCommandType type, Combatant issuer,
            Combatant executor, Combatant target = null, Vector3? position = null)
        {
            return new QuickCommandInstance
            {
                commandType = type,
                state = CommandState.IDLE,
                issuer = issuer,
                executor = executor,
                targetUnit = target,
                targetPosition = position ?? Vector3.zero,
                startTime = Time.time,
                duration = GetEstimatedDuration(type)
            };
        }

        /// <summary>
        /// Gets the target type required by a command.
        /// </summary>
        public static CommandTargetType GetTargetType(QuickCommandType type)
        {
            return type switch
            {
                QuickCommandType.ATTACK_TARGET => CommandTargetType.AUTO,      // Uses player's current target
                QuickCommandType.DEFEND_TARGET => CommandTargetType.ALLY,       // Requires ally selection
                QuickCommandType.DEFEND_PLAYER => CommandTargetType.AUTO,       // Always targets player
                QuickCommandType.ON_ME => CommandTargetType.AUTO,               // Targets player position
                QuickCommandType.FALL_BACK => CommandTargetType.AUTO,           // Auto-calculates retreat
                QuickCommandType.REPOSITION => CommandTargetType.GROUND,        // Requires ground click
                QuickCommandType.RETURN_TO_FORMATION => CommandTargetType.AUTO, // Uses saved position
                QuickCommandType.PRESET_FOCUS_TARGET => CommandTargetType.ENEMY,// Requires enemy target
                _ => CommandTargetType.NONE
            };
        }

        /// <summary>
        /// Gets estimated duration for a command type.
        /// </summary>
        public static float GetEstimatedDuration(QuickCommandType type)
        {
            return type switch
            {
                QuickCommandType.ATTACK_TARGET => 3f,
                QuickCommandType.DEFEND_TARGET => 5f,
                QuickCommandType.DEFEND_PLAYER => 5f,
                QuickCommandType.ON_ME => 8f,
                QuickCommandType.FALL_BACK => 3f,
                QuickCommandType.REPOSITION => 4f,
                QuickCommandType.RETURN_TO_FORMATION => 4f,
                _ => 0f // Presets are instant
            };
        }

        /// <summary>
        /// Gets display name for a command type.
        /// </summary>
        public static string GetDisplayName(QuickCommandType type)
        {
            return type switch
            {
                QuickCommandType.ATTACK_TARGET => "Attack Target",
                QuickCommandType.DEFEND_TARGET => "Defend Ally",
                QuickCommandType.DEFEND_PLAYER => "Defend Me",
                QuickCommandType.ON_ME => "On Me!",
                QuickCommandType.FALL_BACK => "Fall Back",
                QuickCommandType.REPOSITION => "Move Here",
                QuickCommandType.RETURN_TO_FORMATION => "Reform",
                QuickCommandType.PRESET_AGGRESSIVE => "Aggressive",
                QuickCommandType.PRESET_DEFENSIVE => "Defensive",
                QuickCommandType.PRESET_SUPPORT => "Support",
                QuickCommandType.PRESET_FOCUS_TARGET => "Focus Target",
                QuickCommandType.PRESET_PROTECT_PLAYER => "Protect Me",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets description for a command type.
        /// </summary>
        public static string GetDescription(QuickCommandType type)
        {
            return type switch
            {
                QuickCommandType.ATTACK_TARGET => "Attack your current target",
                QuickCommandType.DEFEND_TARGET => "Guard a specific ally",
                QuickCommandType.DEFEND_PLAYER => "Guard the player",
                QuickCommandType.ON_ME => "Come to me and defend",
                QuickCommandType.FALL_BACK => "Retreat from enemies",
                QuickCommandType.REPOSITION => "Move to clicked location",
                QuickCommandType.RETURN_TO_FORMATION => "Return to formation position",
                QuickCommandType.PRESET_AGGRESSIVE => "Prioritize dealing damage",
                QuickCommandType.PRESET_DEFENSIVE => "Prioritize survival",
                QuickCommandType.PRESET_SUPPORT => "Prioritize healing/buffs",
                QuickCommandType.PRESET_FOCUS_TARGET => "All attacks on target",
                QuickCommandType.PRESET_PROTECT_PLAYER => "Stay near and protect player",
                _ => "No description"
            };
        }

        public override string ToString()
        {
            return $"[{commandType}] {executor?.DisplayName ?? "?"} -> {targetUnit?.DisplayName ?? targetPosition.ToString()} ({state})";
        }
    }

    /// <summary>
    /// Data for a command option in the radial menu.
    /// </summary>
    [Serializable]
    public class CommandOption
    {
        public QuickCommandType commandType;
        public string displayName;
        public string description;
        public Sprite icon;
        public bool requiresTarget;
        public CommandTargetType targetType;

        public CommandOption(QuickCommandType type)
        {
            commandType = type;
            displayName = QuickCommandInstance.GetDisplayName(type);
            description = QuickCommandInstance.GetDescription(type);
            targetType = QuickCommandInstance.GetTargetType(type);
            requiresTarget = targetType != CommandTargetType.NONE && targetType != CommandTargetType.AUTO;
        }
    }
}

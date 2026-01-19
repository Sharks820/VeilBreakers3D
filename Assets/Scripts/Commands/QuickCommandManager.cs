using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.Commands
{
    /// <summary>
    /// Manages quick command execution for all party allies.
    /// Handles cooldowns, command queue, and command state machine.
    /// </summary>
    public class QuickCommandManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static QuickCommandManager _instance;
        public static QuickCommandManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[QuickCommandManager] Instance is null. Ensure QuickCommandManager exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [Tooltip("Cooldown in seconds after commanding an ally")]
        [SerializeField] private float _commandCooldown = 12f;

        [Tooltip("Maximum command queue depth per ally")]
        [SerializeField] private int _maxQueueDepth = 1;

        [Tooltip("Distance threshold to consider 'arrived' at position")]
        [SerializeField] private float _arrivalThreshold = 1.5f;

        [Tooltip("On Me defense range")]
        [SerializeField] private float _onMeDefenseRange = 5f;

        // =============================================================================
        // STATE
        // =============================================================================

        private Dictionary<Combatant, QuickCommandInstance> _activeCommands;
        private Dictionary<Combatant, float> _cooldowns;
        private Combatant _player;
        private Combatant[] _allies;
        private Combatant _currentTarget;

        // Pre-allocated buffers to avoid GC in Update loop
        private readonly List<Combatant> _expiredCooldownsBuffer = new List<Combatant>(8);
        private readonly List<KeyValuePair<Combatant, QuickCommandInstance>> _commandsBuffer =
            new List<KeyValuePair<Combatant, QuickCommandInstance>>(8);
        private readonly List<Combatant> _cancelBuffer = new List<Combatant>(8);

        // Cached command options (allocated once)
        private static CommandOption[] _cachedCommandOptions;

        // Events
        public event Action<Combatant, QuickCommandType> OnCommandIssued;
        public event Action<Combatant, QuickCommandType> OnCommandCompleted;
        public event Action<Combatant, QuickCommandType> OnCommandCancelled;
        public event Action<Combatant, float> OnCooldownStarted;
        public event Action<Combatant> OnCooldownExpired;

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

            _activeCommands = new Dictionary<Combatant, QuickCommandInstance>();
            _cooldowns = new Dictionary<Combatant, float>();
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
            UpdateCooldowns();
            UpdateActiveCommands();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the manager with party members.
        /// </summary>
        public void Initialize(Combatant player, Combatant[] allies)
        {
            _player = player;
            _allies = allies ?? Array.Empty<Combatant>();

            _activeCommands.Clear();
            _cooldowns.Clear();

            Debug.Log($"[QuickCommandManager] Initialized with {_allies.Length} allies");
        }

        /// <summary>
        /// Set the player's current target.
        /// </summary>
        public void SetCurrentTarget(Combatant target)
        {
            _currentTarget = target;
        }

        // =============================================================================
        // COMMAND ISSUING
        // =============================================================================

        /// <summary>
        /// Check if an ally can receive a command (not on cooldown, not incapacitated).
        /// </summary>
        public bool CanCommand(Combatant ally)
        {
            if (ally == null) return false;
            if (!ally.IsAlive) return false;
            if (IsOnCooldown(ally)) return false;
            return true;
        }

        /// <summary>
        /// Check if an ally is on cooldown.
        /// </summary>
        public bool IsOnCooldown(Combatant ally)
        {
            if (_cooldowns.TryGetValue(ally, out float cooldownEnd))
            {
                return Time.time < cooldownEnd;
            }
            return false;
        }

        /// <summary>
        /// Get remaining cooldown time for an ally.
        /// </summary>
        public float GetRemainingCooldown(Combatant ally)
        {
            if (_cooldowns.TryGetValue(ally, out float cooldownEnd))
            {
                return Mathf.Max(0f, cooldownEnd - Time.time);
            }
            return 0f;
        }

        /// <summary>
        /// Issue a command to an ally.
        /// </summary>
        public bool IssueCommand(QuickCommandType type, Combatant ally, Combatant target = null, Vector3? position = null)
        {
            if (!CanCommand(ally))
            {
                Debug.LogWarning($"[QuickCommandManager] Cannot command {ally?.DisplayName ?? "null"}: on cooldown or incapacitated");
                return false;
            }

            // Cancel any existing command
            if (_activeCommands.TryGetValue(ally, out var existing))
            {
                CancelCommand(ally);
            }

            // Create new command
            var command = QuickCommandInstance.Create(type, _player, ally, target, position);

            // Validate target if needed
            if (!ValidateCommandTarget(command))
            {
                Debug.LogWarning($"[QuickCommandManager] Invalid target for {type}");
                return false;
            }

            // For AUTO target types, set the target
            ResolveAutoTarget(command);

            // Store and start command
            _activeCommands[ally] = command;
            StartCommand(command);

            // Start cooldown
            StartCooldown(ally);

            // Fire event
            OnCommandIssued?.Invoke(ally, type);

            Debug.Log($"[QuickCommandManager] {ally.DisplayName} received command: {type}");
            return true;
        }

        /// <summary>
        /// Cancel an ally's current command.
        /// </summary>
        public void CancelCommand(Combatant ally)
        {
            if (_activeCommands.TryGetValue(ally, out var command))
            {
                command.state = CommandState.CANCELLED;
                _activeCommands.Remove(ally);

                OnCommandCancelled?.Invoke(ally, command.commandType);

                Debug.Log($"[QuickCommandManager] Cancelled {ally.DisplayName}'s command: {command.commandType}");
            }
        }

        /// <summary>
        /// Cancel all active commands.
        /// </summary>
        public void CancelAllCommands()
        {
            // Use pre-allocated buffer to avoid modification during iteration
            _cancelBuffer.Clear();
            foreach (var ally in _activeCommands.Keys)
            {
                _cancelBuffer.Add(ally);
            }

            foreach (var ally in _cancelBuffer)
            {
                CancelCommand(ally);
            }
        }

        // =============================================================================
        // COMMAND VALIDATION
        // =============================================================================

        private bool ValidateCommandTarget(QuickCommandInstance command)
        {
            var targetType = QuickCommandInstance.GetTargetType(command.commandType);

            switch (targetType)
            {
                case CommandTargetType.NONE:
                case CommandTargetType.AUTO:
                    return true;

                case CommandTargetType.ENEMY:
                    return command.targetUnit != null && command.targetUnit.IsAlive &&
                           !IsAlly(command.targetUnit);

                case CommandTargetType.ALLY:
                    return command.targetUnit != null && command.targetUnit.IsAlive &&
                           IsAlly(command.targetUnit);

                case CommandTargetType.GROUND:
                    return command.targetPosition != Vector3.zero;

                default:
                    return false;
            }
        }

        private void ResolveAutoTarget(QuickCommandInstance command)
        {
            switch (command.commandType)
            {
                case QuickCommandType.ATTACK_TARGET:
                    // Use player's current target
                    if (command.targetUnit == null)
                    {
                        command.targetUnit = _currentTarget;
                    }
                    break;

                case QuickCommandType.DEFEND_PLAYER:
                case QuickCommandType.ON_ME:
                    // Target is player
                    command.targetUnit = _player;
                    if (_player != null)
                    {
                        command.targetPosition = _player.transform.position;
                    }
                    break;

                case QuickCommandType.RETURN_TO_FORMATION:
                    // TODO: Get saved formation position
                    // For now, use player position offset
                    if (_player != null)
                    {
                        int allyIndex = Array.IndexOf(_allies, command.executor);
                        float offset = (allyIndex + 1) * 2f;
                        command.targetPosition = _player.transform.position + new Vector3(offset, 0, -2f);
                    }
                    break;

                case QuickCommandType.FALL_BACK:
                    // Calculate retreat position
                    if (command.executor != null && _player != null)
                    {
                        Vector3 awayFromEnemy = command.executor.transform.position - _player.transform.position;
                        command.targetPosition = command.executor.transform.position - awayFromEnemy.normalized * 5f;
                    }
                    break;
            }
        }

        private bool IsAlly(Combatant unit)
        {
            if (unit == _player) return true;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i] == unit) return true;
            }
            return false;
        }

        // =============================================================================
        // COMMAND EXECUTION
        // =============================================================================

        private void StartCommand(QuickCommandInstance command)
        {
            command.state = CommandState.MOVING;
            command.startTime = Time.time;

            // Handle instant preset commands
            if (IsPresetCommand(command.commandType))
            {
                ApplyPreset(command);
                CompleteCommand(command);
                return;
            }

            // Start movement/action based on command type
            switch (command.commandType)
            {
                case QuickCommandType.ATTACK_TARGET:
                    StartAttackTarget(command);
                    break;

                case QuickCommandType.DEFEND_TARGET:
                case QuickCommandType.DEFEND_PLAYER:
                    StartDefend(command);
                    break;

                case QuickCommandType.ON_ME:
                    StartOnMe(command);
                    break;

                case QuickCommandType.FALL_BACK:
                case QuickCommandType.REPOSITION:
                case QuickCommandType.RETURN_TO_FORMATION:
                    StartMove(command);
                    break;
            }
        }

        private void StartAttackTarget(QuickCommandInstance command)
        {
            if (command.targetUnit == null)
            {
                Debug.LogWarning("[QuickCommandManager] Attack Target: No target");
                CompleteCommand(command);
                return;
            }

            command.state = CommandState.EXECUTING;
            // Ally will handle actual attack through their AI
        }

        private void StartDefend(QuickCommandInstance command)
        {
            if (command.executor != null && command.targetUnit != null)
            {
                command.executor.StartDefend(DefenseAction.GUARD_ALLY, command.targetUnit);
                command.state = CommandState.EXECUTING;
            }
        }

        private void StartOnMe(QuickCommandInstance command)
        {
            command.onMeAutoDefend = false;
            command.onMeReformPending = false;
            command.state = CommandState.MOVING;
            // Will transition through states in Update
        }

        private void StartMove(QuickCommandInstance command)
        {
            command.state = CommandState.MOVING;
            // Movement handled externally by NavMeshAgent or similar
        }

        private bool IsPresetCommand(QuickCommandType type)
        {
            return type >= QuickCommandType.PRESET_AGGRESSIVE &&
                   type <= QuickCommandType.PRESET_PROTECT_PLAYER;
        }

        private void ApplyPreset(QuickCommandInstance command)
        {
            // Apply the preset to the ally's AI controller
            var controller = command.executor?.GetComponent<AI.GambitController>();
            if (controller == null) return;

            switch (command.commandType)
            {
                case QuickCommandType.PRESET_AGGRESSIVE:
                    controller.SetFocusAttack(true);
                    break;

                case QuickCommandType.PRESET_DEFENSIVE:
                    controller.SetFocusDefend(true);
                    break;

                case QuickCommandType.PRESET_SUPPORT:
                    controller.SetFocusHeal(true);
                    break;

                case QuickCommandType.PRESET_PROTECT_PLAYER:
                    controller.SetProtectedAlly(_player);
                    break;

                case QuickCommandType.PRESET_FOCUS_TARGET:
                    // Focus target would be handled by AI system
                    break;
            }

            Debug.Log($"[QuickCommandManager] Applied preset {command.commandType} to {command.executor.DisplayName}");
        }

        private void CompleteCommand(QuickCommandInstance command)
        {
            command.state = CommandState.COMPLETED;

            // Guard against null executor (could be destroyed during command execution)
            if (command.executor != null && _activeCommands.ContainsKey(command.executor))
            {
                _activeCommands.Remove(command.executor);
            }

            // Only invoke event if executor still exists
            if (command.executor != null)
            {
                OnCommandCompleted?.Invoke(command.executor, command.commandType);
                Debug.Log($"[QuickCommandManager] {command.executor.DisplayName} completed: {command.commandType}");
            }
        }

        // =============================================================================
        // UPDATE LOOP
        // =============================================================================

        private void UpdateCooldowns()
        {
            // Check for expired cooldowns using pre-allocated buffer
            _expiredCooldownsBuffer.Clear();

            foreach (var kvp in _cooldowns)
            {
                if (Time.time >= kvp.Value)
                {
                    _expiredCooldownsBuffer.Add(kvp.Key);
                }
            }

            foreach (var ally in _expiredCooldownsBuffer)
            {
                _cooldowns.Remove(ally);
                OnCooldownExpired?.Invoke(ally);
            }
        }

        private void UpdateActiveCommands()
        {
            // Use pre-allocated buffer to avoid modification during iteration
            _commandsBuffer.Clear();
            foreach (var kvp in _activeCommands)
            {
                _commandsBuffer.Add(kvp);
            }

            foreach (var kvp in _commandsBuffer)
            {
                var ally = kvp.Key;
                var command = kvp.Value;

                // Check if ally died
                if (ally == null || !ally.IsAlive)
                {
                    CancelCommand(ally);
                    continue;
                }

                // Update based on command type
                UpdateCommand(command);
            }
        }

        private void UpdateCommand(QuickCommandInstance command)
        {
            switch (command.commandType)
            {
                case QuickCommandType.ATTACK_TARGET:
                    UpdateAttackTarget(command);
                    break;

                case QuickCommandType.DEFEND_TARGET:
                case QuickCommandType.DEFEND_PLAYER:
                    UpdateDefend(command);
                    break;

                case QuickCommandType.ON_ME:
                    UpdateOnMe(command);
                    break;

                case QuickCommandType.FALL_BACK:
                case QuickCommandType.REPOSITION:
                case QuickCommandType.RETURN_TO_FORMATION:
                    UpdateMove(command);
                    break;
            }
        }

        private void UpdateAttackTarget(QuickCommandInstance command)
        {
            // Complete if target dies
            if (command.targetUnit == null || !command.targetUnit.IsAlive)
            {
                CompleteCommand(command);
            }
        }

        private void UpdateDefend(QuickCommandInstance command)
        {
            // Defend continues until cancelled or duration expires
            if (Time.time - command.startTime > command.duration)
            {
                command.executor.StopDefend();
                CompleteCommand(command);
            }
        }

        private void UpdateOnMe(QuickCommandInstance command)
        {
            if (command.executor == null || _player == null) return;

            float distToPlayer = Vector3.Distance(
                command.executor.transform.position,
                _player.transform.position
            );

            switch (command.state)
            {
                case CommandState.MOVING:
                    // Check if arrived at player
                    if (distToPlayer <= _arrivalThreshold)
                    {
                        command.state = CommandState.EXECUTING;
                        command.onMeAutoDefend = true;
                        command.executor.StartDefend(DefenseAction.GUARD_CHAMPION, _player);
                    }
                    break;

                case CommandState.EXECUTING:
                    // Check for enemies in defense range
                    // TODO: Check for threats and auto-attack

                    // Check if should reform
                    if (command.onMeReformPending)
                    {
                        command.executor.StopDefend();
                        CompleteCommand(command);
                    }
                    break;
            }
        }

        private void UpdateMove(QuickCommandInstance command)
        {
            if (command.executor == null) return;

            float dist = Vector3.Distance(
                command.executor.transform.position,
                command.targetPosition
            );

            if (dist <= _arrivalThreshold)
            {
                CompleteCommand(command);
            }
            else if (Time.time - command.startTime > command.duration * 2f)
            {
                // Timeout - couldn't reach destination
                Debug.LogWarning($"[QuickCommandManager] {command.executor.DisplayName} timed out moving to position");
                CompleteCommand(command);
            }
        }

        private void StartCooldown(Combatant ally)
        {
            float cooldownEnd = Time.time + _commandCooldown;
            _cooldowns[ally] = cooldownEnd;

            OnCooldownStarted?.Invoke(ally, _commandCooldown);
        }

        // =============================================================================
        // QUERIES
        // =============================================================================

        /// <summary>
        /// Get the current command for an ally.
        /// </summary>
        public QuickCommandInstance GetActiveCommand(Combatant ally)
        {
            _activeCommands.TryGetValue(ally, out var command);
            return command;
        }

        /// <summary>
        /// Check if any ally has an active command.
        /// </summary>
        public bool HasActiveCommands()
        {
            return _activeCommands.Count > 0;
        }

        /// <summary>
        /// Get all available command options (cached, allocates only once).
        /// </summary>
        public CommandOption[] GetAvailableCommands()
        {
            if (_cachedCommandOptions == null)
            {
                _cachedCommandOptions = new CommandOption[]
                {
                    new CommandOption(QuickCommandType.ATTACK_TARGET),
                    new CommandOption(QuickCommandType.DEFEND_TARGET),
                    new CommandOption(QuickCommandType.DEFEND_PLAYER),
                    new CommandOption(QuickCommandType.ON_ME),
                    new CommandOption(QuickCommandType.FALL_BACK),
                    new CommandOption(QuickCommandType.REPOSITION),
                    new CommandOption(QuickCommandType.RETURN_TO_FORMATION),
                    new CommandOption(QuickCommandType.PRESET_AGGRESSIVE),
                    new CommandOption(QuickCommandType.PRESET_DEFENSIVE),
                    new CommandOption(QuickCommandType.PRESET_SUPPORT),
                    new CommandOption(QuickCommandType.PRESET_FOCUS_TARGET),
                    new CommandOption(QuickCommandType.PRESET_PROTECT_PLAYER)
                };
            }
            return _cachedCommandOptions;
        }

        // =============================================================================
        // COMBAT EVENTS
        // =============================================================================

        /// <summary>
        /// Called when combat ends - cancel all commands and reset cooldowns.
        /// </summary>
        public void OnCombatEnd()
        {
            CancelAllCommands();
            _cooldowns.Clear();
            Debug.Log("[QuickCommandManager] Combat ended - all commands cancelled, cooldowns reset");
        }

        /// <summary>
        /// Called when an enemy dies - check for On Me reform conditions.
        /// </summary>
        public void OnEnemyDefeated(Combatant enemy)
        {
            // Check if any On Me commands should trigger reform
            foreach (var kvp in _activeCommands)
            {
                var command = kvp.Value;
                if (command.commandType == QuickCommandType.ON_ME &&
                    command.state == CommandState.EXECUTING)
                {
                    // Check if this was the last nearby threat
                    // TODO: Implement threat detection
                    command.onMeReformPending = true;
                }
            }
        }
    }
}

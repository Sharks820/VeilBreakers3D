using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.Capture
{
    /// <summary>
    /// Manages the capture system including marking, binding, and capture attempts.
    /// </summary>
    public class CaptureManager : SingletonMonoBehaviour<CaptureManager>
    {
        // This is a scene-specific singleton
        protected override bool IsPersistent => false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Input")]
        [SerializeField] private KeyCode _markKey = KeyCode.C;
        [SerializeField] private KeyCode _cancelMarkKey = KeyCode.Escape;

        [Header("Bind Settings")]
        [Tooltip("Range at which allies will attempt to bind a marked target")]
        [SerializeField] private float _bindRange = 5f;

        [Tooltip("Time window to execute bind after threshold reached")]
        [SerializeField] private float _bindWindowDuration = 3f;

        [Tooltip("Duration of bind animation before monster is fully bound")]
        [SerializeField] private float _bindDuration = 1.5f;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private Combatant[] _allies;
        private Combatant[] _enemies;
        private int _playerLevel;

        // Marked targets (can mark multiple)
        private readonly List<Combatant> _markedTargets = new List<Combatant>();

        // Bound monsters awaiting capture
        private readonly List<BoundMonsterData> _boundMonsters = new List<BoundMonsterData>();

        // Currently active bind attempts (ally -> target)
        private readonly Dictionary<Combatant, Combatant> _bindAttempts = new Dictionary<Combatant, Combatant>();

        // Bind start times for duration tracking (ally -> start time)
        private readonly Dictionary<Combatant, float> _bindStartTimes = new Dictionary<Combatant, float>();

        // Capture phase state
        private bool _inCapturePhase = false;
        private BoundMonsterData _currentCaptureTarget;
        private CaptureItem _selectedItem = CaptureItem.NONE;

        // Target override for custom targeting
        private Combatant _targetOverride;

        // Pre-allocated buffer for iteration
        private readonly List<Combatant> _iterationBuffer = new List<Combatant>(8);

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<Combatant> OnTargetMarked;
        public event Action<Combatant> OnTargetUnmarked;
        public event Action<Combatant, float> OnBindThresholdReached;
        public event Action<Combatant, Combatant> OnBindStarted;  // target, ally
        public event Action<Combatant, float> OnBindProgress;     // target, progress (0-1)
        public event Action<Combatant> OnMonsterBound;
        public event Action OnCapturePhaseStarted;
        public event Action OnCapturePhaseEnded;
        public event Action<BoundMonsterData, CaptureItem> OnCaptureAttemptStarted;
        public event Action<BoundMonsterData, CaptureOutcome> OnCaptureAttemptComplete;
        public event Action<Combatant> OnMonsterBerserk;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public bool InCapturePhase => _inCapturePhase;
        public bool HasMarkedTargets => _markedTargets.Count > 0;
        public bool HasBoundMonsters => _boundMonsters.Count > 0;
        public IReadOnlyList<Combatant> MarkedTargets => _markedTargets;
        public IReadOnlyList<BoundMonsterData> BoundMonsters => _boundMonsters;
        public BoundMonsterData CurrentCaptureTarget => _currentCaptureTarget;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void OnDisable()
        {
            // NOTE: Do NOT set events to null here - that breaks the event pattern
            // by clearing ALL subscribers globally. Events are cleaned up when
            // subscribers unsubscribe in their own OnDestroy/OnDisable.
            // Only clear runtime state.
            _bindAttempts.Clear();
            _bindStartTimes.Clear();
        }

        private void Update()
        {
            if (_inCapturePhase) return;

            HandleInput();
            UpdateBindAttempts();
            CheckBindThresholds();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize for a combat encounter.
        /// </summary>
        public void Initialize(Combatant player, Combatant[] allies, Combatant[] enemies, int playerLevel)
        {
            _player = player;
            _allies = allies ?? Array.Empty<Combatant>();
            _enemies = enemies ?? Array.Empty<Combatant>();
            _playerLevel = playerLevel;

            _markedTargets.Clear();
            _boundMonsters.Clear();
            _bindAttempts.Clear();
            _bindStartTimes.Clear();
            _inCapturePhase = false;
            _currentCaptureTarget = null;
            _selectedItem = CaptureItem.NONE;

            Debug.Log($"[CaptureManager] Initialized with {_enemies.Length} enemies");
        }

        // =============================================================================
        // INPUT HANDLING
        // =============================================================================

        private void HandleInput()
        {
            // C key to mark/unmark current target
            if (Input.GetKeyDown(_markKey))
            {
                var target = GetCurrentTarget();
                if (target != null && IsValidCaptureTarget(target))
                {
                    ToggleMark(target);
                }
            }

            // Escape to clear all marks
            if (Input.GetKeyDown(_cancelMarkKey))
            {
                ClearAllMarks();
            }
        }

        private Combatant GetCurrentTarget()
        {
            // Check for custom targeting override first
            if (_targetOverride != null && _targetOverride.IsAlive)
                return _targetOverride;

            // Fall back to BattleManager's current target
            return BattleManager.Instance?.CurrentTarget;
        }

        /// <summary>
        /// Override the current target for capture marking (used by UI/input systems)
        /// </summary>
        public void SetTargetOverride(Combatant target)
        {
            _targetOverride = target;
        }

        /// <summary>
        /// Clear the target override
        /// </summary>
        public void ClearTargetOverride()
        {
            _targetOverride = null;
        }

        // =============================================================================
        // MARKING SYSTEM
        // =============================================================================

        /// <summary>
        /// Mark an enemy for capture.
        /// </summary>
        public void MarkForCapture(Combatant target)
        {
            if (target == null || !IsValidCaptureTarget(target)) return;

            if (!_markedTargets.Contains(target))
            {
                _markedTargets.Add(target);
                OnTargetMarked?.Invoke(target);
                Debug.Log($"[CaptureManager] Marked {target.DisplayName} for capture");
            }
        }

        /// <summary>
        /// Remove capture mark from enemy.
        /// </summary>
        public void UnmarkForCapture(Combatant target)
        {
            if (target == null) return;

            if (_markedTargets.Remove(target))
            {
                OnTargetUnmarked?.Invoke(target);
                Debug.Log($"[CaptureManager] Unmarked {target.DisplayName}");
            }
        }

        /// <summary>
        /// Toggle mark on enemy.
        /// </summary>
        public void ToggleMark(Combatant target)
        {
            if (_markedTargets.Contains(target))
            {
                UnmarkForCapture(target);
            }
            else
            {
                MarkForCapture(target);
            }
        }

        /// <summary>
        /// Clear all capture marks.
        /// </summary>
        public void ClearAllMarks()
        {
            _iterationBuffer.Clear();
            _iterationBuffer.AddRange(_markedTargets);

            foreach (var target in _iterationBuffer)
            {
                UnmarkForCapture(target);
            }
        }

        /// <summary>
        /// Check if an enemy is marked for capture.
        /// </summary>
        public bool IsMarkedForCapture(Combatant target)
        {
            return _markedTargets.Contains(target);
        }

        /// <summary>
        /// Check if target is valid for capture (alive, enemy, not already bound).
        /// </summary>
        public bool IsValidCaptureTarget(Combatant target)
        {
            if (target == null) return false;
            if (!target.IsAlive) return false;
            if (target.IsPlayer) return false;
            if (IsAlly(target)) return false;
            if (IsBound(target)) return false;
            return true;
        }

        private bool IsAlly(Combatant unit)
        {
            if (unit == _player) return true;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i] != null && _allies[i] == unit) return true;
            }
            return false;
        }

        // =============================================================================
        // BIND SYSTEM
        // =============================================================================

        private void CheckBindThresholds()
        {
            foreach (var target in _markedTargets)
            {
                if (target == null || !target.IsAlive || IsBound(target)) continue;

                // Get nearest ally for threshold calculation
                var nearestAlly = GetNearestAlly(target);
                float threshold = BindThresholdCalculator.CalculateThreshold(target, nearestAlly);

                float currentHP = target.MaxHP > 0 ? target.CurrentHP / (float)target.MaxHP : 0f;

                // Check if reached threshold
                if (currentHP <= threshold && !_bindAttempts.ContainsValue(target))
                {
                    OnBindThresholdReached?.Invoke(target, threshold);
                    // Ally will pick up the bind attempt
                    AssignBindAttempt(target, nearestAlly);
                }
            }
        }

        private void UpdateBindAttempts()
        {
            // Check for completed bind attempts
            _iterationBuffer.Clear();
            float currentTime = Time.time;

            foreach (var kvp in _bindAttempts)
            {
                var ally = kvp.Key;
                var target = kvp.Value;

                // Check if bind should be cancelled
                if (target == null || !target.IsAlive)
                {
                    _iterationBuffer.Add(ally);
                    continue;
                }

                // Check if ally is still able to bind (alive and in range)
                if (ally == null || !ally.IsAlive)
                {
                    _iterationBuffer.Add(ally);
                    continue;
                }

                // Get bind start time
                if (!_bindStartTimes.TryGetValue(ally, out float startTime))
                {
                    // Should not happen, but handle gracefully
                    _iterationBuffer.Add(ally);
                    continue;
                }

                // Calculate progress
                float elapsed = currentTime - startTime;
                float progress = Mathf.Clamp01(elapsed / _bindDuration);

                // Fire progress event for UI updates
                OnBindProgress?.Invoke(target, progress);

                // Check if bind is complete
                if (elapsed >= _bindDuration)
                {
                    BindMonster(target, ally);
                    _iterationBuffer.Add(ally);
                }
            }

            // Clean up completed attempts
            foreach (var ally in _iterationBuffer)
            {
                _bindAttempts.Remove(ally);
                _bindStartTimes.Remove(ally);
            }

            // Clear buffer to release references for GC
            _iterationBuffer.Clear();
        }

        private void AssignBindAttempt(Combatant target, Combatant ally)
        {
            if (ally == null || _bindAttempts.ContainsKey(ally)) return;

            _bindAttempts[ally] = target;
            _bindStartTimes[ally] = Time.time;

            OnBindStarted?.Invoke(target, ally);
            Debug.Log($"[CaptureManager] {ally.DisplayName} attempting to bind {target.DisplayName} (duration: {_bindDuration}s)");
        }

        /// <summary>
        /// Execute binding on a target.
        /// </summary>
        public void BindMonster(Combatant target, Combatant binder)
        {
            if (target == null || IsBound(target)) return;

            float hpPercent = target.MaxHP > 0 ? target.CurrentHP / (float)target.MaxHP : 0f;
            float threshold = BindThresholdCalculator.CalculateThreshold(target, binder);

            var boundData = BoundMonsterData.Create(target, hpPercent, threshold);
            boundData.wasIntimidated = BindThresholdCalculator.IsIntimidated(target, binder);

            _boundMonsters.Add(boundData);

            // Apply Bound status to prevent actions
            target.ApplyStatus(StatusEffectType.BOUND, BindThresholdConfig.BIND_STATUS_DURATION, null);

            OnMonsterBound?.Invoke(target);
            Debug.Log($"[CaptureManager] {target.DisplayName} is now BOUND at {hpPercent:P0} HP");
        }

        /// <summary>
        /// Check if a monster is bound.
        /// </summary>
        public bool IsBound(Combatant target)
        {
            foreach (var bound in _boundMonsters)
            {
                if (bound.combatant == target) return true;
            }
            return false;
        }

        /// <summary>
        /// Get the bind threshold for a target.
        /// </summary>
        public float GetBindThreshold(Combatant target)
        {
            var nearestAlly = GetNearestAlly(target);
            return BindThresholdCalculator.CalculateThreshold(target, nearestAlly);
        }

        private Combatant GetNearestAlly(Combatant target)
        {
            if (_allies == null || _allies.Length == 0) return _player;

            Combatant nearest = _player;
            float nearestDist = float.MaxValue;

            if (_player != null && target != null)
            {
                nearestDist = Vector3.Distance(_player.transform.position, target.transform.position);
            }

            foreach (var ally in _allies)
            {
                if (ally == null || !ally.IsAlive) continue;

                float dist = Vector3.Distance(ally.transform.position, target.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = ally;
                }
            }

            return nearest;
        }

        // =============================================================================
        // CAPTURE PHASE
        // =============================================================================

        /// <summary>
        /// Start the capture phase (called when battle ends with bound monsters).
        /// </summary>
        public void StartCapturePhase()
        {
            if (_boundMonsters.Count == 0)
            {
                Debug.LogWarning("[CaptureManager] No bound monsters for capture phase");
                return;
            }

            _inCapturePhase = true;
            _currentCaptureTarget = _boundMonsters.Count > 0 ? _boundMonsters[0] : null;
            _selectedItem = CaptureItem.NONE;

            OnCapturePhaseStarted?.Invoke();
            Debug.Log("[CaptureManager] Capture phase started");
        }

        /// <summary>
        /// End the capture phase.
        /// </summary>
        public void EndCapturePhase()
        {
            _inCapturePhase = false;
            _currentCaptureTarget = null;
            _selectedItem = CaptureItem.NONE;

            OnCapturePhaseEnded?.Invoke();
            Debug.Log("[CaptureManager] Capture phase ended");
        }

        /// <summary>
        /// Select a bound monster for capture attempt.
        /// </summary>
        public void SelectCaptureTarget(int index)
        {
            if (index >= 0 && index < _boundMonsters.Count)
            {
                _currentCaptureTarget = _boundMonsters[index];
            }
        }

        /// <summary>
        /// Select capture item.
        /// </summary>
        public void SelectCaptureItem(CaptureItem item)
        {
            _selectedItem = item;
        }

        /// <summary>
        /// Preview capture chance for current selection.
        /// </summary>
        public CaptureCalculationResult PreviewCaptureChance()
        {
            if (_currentCaptureTarget == null || _selectedItem == CaptureItem.NONE)
            {
                // Return a safe default instead of null to prevent NullReferenceException in callers
                return new CaptureCalculationResult { finalChance = 0f };
            }

            return CaptureFormulaCalculator.PreviewChance(
                _currentCaptureTarget,
                _selectedItem,
                _playerLevel
            );
        }

        /// <summary>
        /// Execute capture attempt after QTE.
        /// </summary>
        public CaptureOutcome ExecuteCapture(QTEResult qteResult)
        {
            if (_currentCaptureTarget == null || _selectedItem == CaptureItem.NONE)
            {
                Debug.LogError("[CaptureManager] Cannot execute capture: no target or item selected");
                return CaptureOutcome.FLEE;
            }

            OnCaptureAttemptStarted?.Invoke(_currentCaptureTarget, _selectedItem);

            // Calculate final chance
            float chance = CaptureFormulaCalculator.CalculateQuick(
                _currentCaptureTarget,
                _selectedItem,
                _playerLevel,
                qteResult
            );

            // Roll for capture
            float roll = UnityEngine.Random.value;
            CaptureOutcome outcome;

            if (roll <= chance)
            {
                outcome = CaptureOutcome.SUCCESS;
                HandleCaptureSuccess(_currentCaptureTarget);
            }
            else
            {
                outcome = CaptureFormulaCalculator.DetermineFailureOutcome(_currentCaptureTarget, _playerLevel);
                HandleCaptureFailure(_currentCaptureTarget, outcome);
            }

            OnCaptureAttemptComplete?.Invoke(_currentCaptureTarget, outcome);

            Debug.Log($"[CaptureManager] Capture attempt: {chance:P0} chance, rolled {roll:P0} = {outcome}");

            return outcome;
        }

        private void HandleCaptureSuccess(BoundMonsterData monster)
        {
            Debug.Log($"[CaptureManager] Successfully captured {monster.combatant.DisplayName}!");

            // Remove from bound list
            _boundMonsters.Remove(monster);

            // Add monster to player's party using GameManager
            // Cache instance to prevent race condition between null check and usage
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                string monsterId = monster.combatant?.MonsterId;
                if (!string.IsNullOrEmpty(monsterId))
                {
                    bool added = gameManager.AddToParty(
                        monsterId,
                        monster.combatant.Level,
                        monster.combatant.Corruption
                    );

                    if (added)
                    {
                        Debug.Log($"[CaptureManager] Monster added to party: {monster.combatant.DisplayName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CaptureManager] Failed to add monster to party (party full or invalid?)");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CaptureManager] Monster has no MonsterId, cannot add to party");
                }
            }

            // Remove captured monster from battle enemies
            RemoveMonsterFromBattle(monster.combatant);

            // Check if more monsters to capture
            if (_boundMonsters.Count > 0)
            {
                _currentCaptureTarget = _boundMonsters[0];
            }
            else
            {
                _currentCaptureTarget = null;
                EndCapturePhase();
            }
        }

        private void HandleCaptureFailure(BoundMonsterData monster, CaptureOutcome outcome)
        {
            // Remove from bound list
            _boundMonsters.Remove(monster);

            if (outcome == CaptureOutcome.FLEE)
            {
                Debug.Log($"[CaptureManager] {monster.combatant.DisplayName} fled!");
                // Monster escapes - remove from combat
                monster.combatant.RemoveStatus(StatusEffectType.BOUND);

                // Remove fleeing monster from battle (it escaped)
                RemoveMonsterFromBattle(monster.combatant);
            }
            else if (outcome == CaptureOutcome.BERSERK)
            {
                Debug.Log($"[CaptureManager] {monster.combatant.DisplayName} went BERSERK!");

                // Monster breaks free and gets damage buff
                monster.combatant.RemoveStatus(StatusEffectType.BOUND);

                float damageBuff = CaptureFormulaCalculator.GetBerserkDamageBuff();
                monster.combatant.ApplyStatus(StatusEffectType.BERSERK, 30f, null);
                monster.combatant.ApplyDamageBuff(damageBuff);

                OnMonsterBerserk?.Invoke(monster.combatant);

                // Resume combat - monster stays in battle as berserk enemy
                EndCapturePhase();

                // Signal battle to resume via EventBus
                EventBus.BattleStarted(); // Re-signal to resume combat flow
            }

            // Check for more bound monsters
            if (_boundMonsters.Count > 0)
            {
                _currentCaptureTarget = _boundMonsters[0];
            }
            else
            {
                _currentCaptureTarget = null;
                if (outcome != CaptureOutcome.BERSERK)
                {
                    EndCapturePhase();
                }
            }
        }

        // =============================================================================
        // COMBAT EVENTS
        // =============================================================================

        /// <summary>
        /// Remove a monster from the battle (captured or fled).
        /// </summary>
        private void RemoveMonsterFromBattle(Combatant monster)
        {
            if (monster == null) return;

            // Remove from marked targets
            _markedTargets.Remove(monster);

            // Remove from bind attempts if in progress
            _iterationBuffer.Clear();
            foreach (var kvp in _bindAttempts)
            {
                if (kvp.Value == monster)
                {
                    _iterationBuffer.Add(kvp.Key);
                }
            }
            foreach (var ally in _iterationBuffer)
            {
                _bindAttempts.Remove(ally);
                _bindStartTimes.Remove(ally);
            }
            _iterationBuffer.Clear();

            // Disable the game object (monster is removed from battle)
            // BattleManager checks IsAlive, so deactivating + killing handles enemy tracking
            if (monster.gameObject != null)
            {
                // Mark as dead to trigger any death-related cleanup
                monster.TakeDamage(monster.MaxHP + 1);

                // Disable instead of destroy for object pooling later
                monster.gameObject.SetActive(false);
            }

            Debug.Log($"[CaptureManager] Removed {monster.DisplayName} from battle");
        }

        /// <summary>
        /// Called when combat ends.
        /// </summary>
        public void OnCombatEnd(bool victory)
        {
            if (victory && _boundMonsters.Count > 0)
            {
                StartCapturePhase();
            }
            else
            {
                // Clear state on defeat or no bound monsters
                _markedTargets.Clear();
                _boundMonsters.Clear();
                _bindAttempts.Clear();
                _bindStartTimes.Clear();
            }
        }

        /// <summary>
        /// Called when an enemy dies.
        /// </summary>
        public void OnEnemyDefeated(Combatant enemy)
        {
            // Remove from marked if killed (not bound)
            _markedTargets.Remove(enemy);
        }

        // =============================================================================
        // AI INTEGRATION
        // =============================================================================

        /// <summary>
        /// Check if ally should switch to bind mode for a target.
        /// </summary>
        public bool ShouldBindTarget(Combatant ally, Combatant target)
        {
            if (!IsMarkedForCapture(target)) return false;
            if (IsBound(target)) return false;
            return BindThresholdCalculator.CanBind(target, ally);
        }

        /// <summary>
        /// Get the priority target for binding (if any).
        /// </summary>
        public Combatant GetBindPriorityTarget(Combatant ally)
        {
            foreach (var target in _markedTargets)
            {
                if (target == null || !target.IsAlive || IsBound(target)) continue;

                if (BindThresholdCalculator.CanBind(target, ally))
                {
                    return target;
                }
            }
            return null;
        }

        // =============================================================================
        // DEBUG
        // =============================================================================

        /// <summary>
        /// Get debug info about current state.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Capture Manager State:\n" +
                   $"  Marked Targets: {_markedTargets.Count}\n" +
                   $"  Bound Monsters: {_boundMonsters.Count}\n" +
                   $"  Active Binds: {_bindAttempts.Count}\n" +
                   $"  In Capture Phase: {_inCapturePhase}\n" +
                   $"  Current Target: {_currentCaptureTarget?.combatant?.DisplayName ?? "None"}";
        }
    }
}

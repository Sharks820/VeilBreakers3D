using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages automatic save triggers based on game events.
    /// Hooks into EventBus to auto-save at checkpoints.
    /// </summary>
    public class AutoSaveManager : SingletonMonoBehaviour<AutoSaveManager>
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Auto-Save Settings")]
        [SerializeField]
        [Tooltip("Enable auto-saving on character creation")]
        private bool _saveOnCharacterCreated = true;

        [SerializeField]
        [Tooltip("Enable auto-saving after tutorial completion")]
        private bool _saveOnTutorialComplete = true;

        [SerializeField]
        [Tooltip("Enable auto-saving after boss defeats")]
        private bool _saveOnBossDefeated = true;

        [SerializeField]
        [Tooltip("Enable auto-saving on main quest completion")]
        private bool _saveOnMainQuestComplete = true;

        [SerializeField]
        [Tooltip("Minimum seconds between auto-saves (debounce)")]
        private float _minAutoSaveInterval = 30f;

        [Header("Debug")]
        [SerializeField]
        private bool _logAutoSaves = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _lastAutoSaveTime;
        private bool _isEnabled = true;
        private CancellationTokenSource _cts;
        private bool _isSubscribed;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>True if auto-saving is enabled</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>Time since last auto-save</summary>
        public float TimeSinceLastAutoSave => Time.realtimeSinceStartup - _lastAutoSaveTime;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            // Replace CTS if previously cancelled (a cancelled CTS cannot be reused)
            if (_cts == null || _cts.IsCancellationRequested)
            {
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
            }
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            // Cancel any in-flight auto-saves when disabled
            _cts?.Cancel();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            base.OnDestroy();
        }

        // =============================================================================
        // EVENT SUBSCRIPTIONS
        // =============================================================================

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return; // Prevent stacking on repeated OnEnable
            _isSubscribed = true;
            EventBus.OnCharacterCreated += OnCharacterCreated;
            EventBus.OnTutorialCompleted += OnTutorialCompleted;
            EventBus.OnBossDefeated += OnBossDefeated;
            EventBus.OnMainQuestCompleted += OnMainQuestCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;
            EventBus.OnCharacterCreated -= OnCharacterCreated;
            EventBus.OnTutorialCompleted -= OnTutorialCompleted;
            EventBus.OnBossDefeated -= OnBossDefeated;
            EventBus.OnMainQuestCompleted -= OnMainQuestCompleted;
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnCharacterCreated(string heroId)
        {
            if (!_saveOnCharacterCreated) return;
            TriggerAutoSave($"character_created:{heroId}");
        }

        private void OnTutorialCompleted()
        {
            if (!_saveOnTutorialComplete) return;
            TriggerAutoSave("tutorial_complete");
        }

        private void OnBossDefeated(string bossId)
        {
            if (!_saveOnBossDefeated) return;
            TriggerAutoSave($"boss_defeated:{bossId}");
        }

        private void OnMainQuestCompleted(string questId)
        {
            if (!_saveOnMainQuestComplete) return;
            TriggerAutoSave($"quest_complete:{questId}");
        }

        // =============================================================================
        // AUTO-SAVE LOGIC
        // =============================================================================

        /// <summary>
        /// Triggers an auto-save with debouncing.
        /// </summary>
        public void TriggerAutoSave(string reason)
        {
            if (!_isEnabled)
            {
                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Auto-save disabled, skipping: {reason}");
                return;
            }

            if (SaveManager.Instance == null || !SaveManager.Instance.HasActiveSave)
            {
                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] No active save, skipping: {reason}");
                return;
            }

            if (SaveManager.Instance.IsSaving || SaveManager.Instance.IsLoading)
            {
                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Save/Load in progress, skipping: {reason}");
                return;
            }

            // Debounce check
            float timeSinceLast = Time.realtimeSinceStartup - _lastAutoSaveTime;
            if (timeSinceLast < _minAutoSaveInterval)
            {
                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Debounce active ({timeSinceLast:F1}s < {_minAutoSaveInterval}s), skipping: {reason}");
                return;
            }

            // Update debounce time BEFORE fire-and-forget to prevent double-trigger window
            _lastAutoSaveTime = Time.realtimeSinceStartup;

            // Perform auto-save with error handling
            PerformAutoSaveAsync(reason, _cts?.Token ?? CancellationToken.None).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                    Debug.LogError($"[AutoSave] Failed: {t.Exception.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task PerformAutoSaveAsync(string reason, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Auto-saving: {reason}");

                // Cache reference to avoid null between await points
                var saveManager = SaveManager.Instance;
                if (saveManager == null)
                {
                    Debug.LogWarning($"[AutoSaveManager] SaveManager destroyed during auto-save: {reason}");
                    return;
                }

                bool success = await saveManager.AutoSaveAsync(reason);
                token.ThrowIfCancellationRequested();

                if (_logAutoSaves)
                {
                    if (success)
                        Debug.Log($"[AutoSaveManager] Auto-save completed: {reason}");
                    else
                        Debug.LogWarning($"[AutoSaveManager] Auto-save failed: {reason}");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when component is disabled or destroyed — not an error
                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Auto-save cancelled: {reason}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoSaveManager] Exception during auto-save '{reason}': {ex.Message}");
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Forces an immediate auto-save (bypasses debounce).
        /// </summary>
        public void ForceAutoSave(string reason)
        {
            if (!_isEnabled || SaveManager.Instance == null || !SaveManager.Instance.HasActiveSave) return;
            _lastAutoSaveTime = Time.realtimeSinceStartup;
            PerformAutoSaveAsync($"forced:{reason}", _cts?.Token ?? CancellationToken.None).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                    Debug.LogError($"[AutoSave] Forced save failed: {t.Exception.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Resets the debounce timer.
        /// </summary>
        public void ResetDebounceTimer()
        {
            _lastAutoSaveTime = 0f;
        }
    }
}

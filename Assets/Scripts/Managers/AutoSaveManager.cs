using System;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages automatic save triggers based on game events.
    /// Hooks into EventBus to auto-save at checkpoints.
    /// </summary>
    public class AutoSaveManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static AutoSaveManager _instance;
        public static AutoSaveManager Instance => _instance;

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

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            UnsubscribeFromEvents();
        }

        // =============================================================================
        // EVENT SUBSCRIPTIONS
        // =============================================================================

        private void SubscribeToEvents()
        {
            EventBus.OnCharacterCreated += OnCharacterCreated;
            EventBus.OnTutorialCompleted += OnTutorialCompleted;
            EventBus.OnBossDefeated += OnBossDefeated;
            EventBus.OnMainQuestCompleted += OnMainQuestCompleted;
        }

        private void UnsubscribeFromEvents()
        {
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

            // Perform auto-save
            PerformAutoSave(reason);
        }

        private async void PerformAutoSave(string reason)
        {
            try
            {
                _lastAutoSaveTime = Time.realtimeSinceStartup;

                if (_logAutoSaves)
                    Debug.Log($"[AutoSaveManager] Auto-saving: {reason}");

                bool success = await SaveManager.Instance.AutoSaveAsync(reason);

                if (_logAutoSaves)
                {
                    if (success)
                        Debug.Log($"[AutoSaveManager] Auto-save completed: {reason}");
                    else
                        Debug.LogWarning($"[AutoSaveManager] Auto-save failed: {reason}");
                }
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
            if (!_isEnabled || SaveManager.Instance == null) return;
            PerformAutoSave($"forced:{reason}");
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

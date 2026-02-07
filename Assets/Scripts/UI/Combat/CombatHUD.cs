using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Capture;
using VeilBreakers.Commands;
using VeilBreakers.Core;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Main combat HUD controller that manages all UI panels.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static CombatHUD _instance;
        private static bool _isQuitting = false;

        public static CombatHUD Instance
        {
            get
            {
                if (_isQuitting) return null;
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Panels")]
        [SerializeField] private PlayerPanelController _playerPanel;
        [SerializeField] private EnemyPanelController _enemyPanel;
        [SerializeField] private SkillBarController _skillBar;
        [SerializeField] private CaptureBannerController _captureBanner;

        [Header("Ally Panels")]
        [SerializeField] private List<AllyPanelController> _allyPanels;

        [Header("Menu Icons")]
        [SerializeField] private Transform _menuIconContainer;

        [Header("Config")]
        [SerializeField] private CombatUIConfig _uiConfig;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private Combatant[] _allies;
        private Combatant[] _enemies;
        private Combatant _currentTarget;
        private int _targetIndex = 0;
        private int _selectedAllyIndex = -1;
        private bool _isInitialized = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public Combatant Player => _player;
        public Combatant CurrentTarget => _currentTarget;
        public int SelectedAllyIndex => _selectedAllyIndex;
        public bool IsInitialized => _isInitialized;
        public CombatUIConfig Config => _uiConfig;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<int> OnSkillActivated;          // Skill slot index
        public event Action<int> OnAllyUltimateTriggered;   // Ally index
        public event Action<Combatant> OnTargetChanged;
        public event Action<int, Combatant> OnSelectedAllyChanged;
        public event Action OnCaptureRequested;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _isQuitting = false; // Reset for Editor play mode re-entry
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Events are now subscribed at the end of Initialize() to ensure state is set first.
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            UnsubscribeFromEvents();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void Update()
        {
            HandleTargetCycling();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize the combat HUD for a battle.
        /// </summary>
        public void Initialize(Combatant player, Combatant[] allies, Combatant[] enemies)
        {
            _player = player;
            _allies = allies ?? Array.Empty<Combatant>();
            _enemies = enemies ?? Array.Empty<Combatant>();

            // Initialize player panel
            if (_playerPanel != null)
            {
                _playerPanel.Initialize(_player);
            }

            // Initialize ally panels
            InitializeAllyPanels();

            // Initialize skill bar
            if (_skillBar != null)
            {
                _skillBar.Initialize(_player);
            }

            // Set initial target
            if (_enemies.Length > 0 && _enemies[0] != null)
            {
                SetTarget(_enemies[0]);
            }

            // Default ally selection and sync with BattleManager.
            SelectFirstAliveAlly();
            SyncBattleManagerActiveAlly();

            _isInitialized = true;
            SubscribeToEvents();
            SetVisible(true);

            Debug.Log($"[CombatHUD] Initialized with {_allies.Length} allies and {_enemies.Length} enemies");
        }

        /// <summary>
        /// Clean up after combat ends.
        /// </summary>
        public void Cleanup()
        {
            // Unbind ally panels from combatant events before clearing references
            if (_allyPanels != null)
            {
                foreach (var panel in _allyPanels)
                {
                    if (panel != null) panel.Cleanup();
                }
            }

            // Unsubscribe from BattleManager ally change events
            if (BattleManager.HasInstance)
            {
                BattleManager.Instance.OnActiveAllyChanged -= OnBattleManagerAllyChanged;
            }

            _player = null;
            _allies = Array.Empty<Combatant>();
            _enemies = Array.Empty<Combatant>();
            _currentTarget = null;
            _targetIndex = 0;
            _selectedAllyIndex = -1;
            _isInitialized = false;

            SetVisible(false);
        }

        /// <summary>
        /// Set the current target.
        /// </summary>
        public void SetTarget(Combatant target)
        {
            if (_currentTarget == target) return;

            _currentTarget = target;

            // Update enemy panel
            if (_enemyPanel != null)
            {
                _enemyPanel.SetTarget(_currentTarget);
            }

            // Update capture banner
            if (_captureBanner != null)
            {
                _captureBanner.UpdateForTarget(_currentTarget);
            }

            OnTargetChanged?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Cycle to next enemy target.
        /// </summary>
        public void CycleTargetNext()
        {
            if (_enemies == null || _enemies.Length == 0) return;

            // Find next valid target
            int startIndex = _targetIndex;
            do
            {
                _targetIndex = (_targetIndex + 1) % _enemies.Length;
                if (_enemies[_targetIndex] != null && _enemies[_targetIndex].IsAlive)
                {
                    SetTarget(_enemies[_targetIndex]);
                    return;
                }
            } while (_targetIndex != startIndex);

            // If no valid target found after full cycle, clear the stale reference
            SetTarget(null);
        }

        /// <summary>
        /// Cycle to previous enemy target.
        /// </summary>
        public void CycleTargetPrevious()
        {
            if (_enemies == null || _enemies.Length == 0) return;

            int startIndex = _targetIndex;
            do
            {
                _targetIndex = (_targetIndex - 1 + _enemies.Length) % _enemies.Length;
                if (_enemies[_targetIndex] != null && _enemies[_targetIndex].IsAlive)
                {
                    SetTarget(_enemies[_targetIndex]);
                    return;
                }
            } while (_targetIndex != startIndex);

            // If no valid target found after full cycle, clear the stale reference
            SetTarget(null);
        }

        /// <summary>
        /// Update skill cooldown display.
        /// </summary>
        public void UpdateSkillCooldown(int slotIndex, float remaining, float total)
        {
            if (_skillBar != null)
            {
                _skillBar.SetCooldown(slotIndex, remaining, total);
            }
        }

        /// <summary>
        /// Update ally skill cooldown display.
        /// </summary>
        public void UpdateAllySkillCooldown(int allyIndex, int skillIndex, float remaining, float total)
        {
            if (allyIndex >= 0 && allyIndex < _allyPanels.Count)
            {
                _allyPanels[allyIndex].UpdateSkillCooldown(skillIndex, remaining, total);
            }
        }

        /// <summary>
        /// Set ally ultimate ready state.
        /// </summary>
        public void SetAllyUltimateReady(int allyIndex, bool ready)
        {
            if (allyIndex >= 0 && allyIndex < _allyPanels.Count)
            {
                _allyPanels[allyIndex].SetUltimateReady(ready);
            }
        }

        /// <summary>
        /// Cycle ally selection to next available ally.
        /// </summary>
        public void CycleAllyNext()
        {
            SelectAdjacentAlly(1);
        }

        /// <summary>
        /// Cycle ally selection to previous available ally.
        /// </summary>
        public void CycleAllyPrevious()
        {
            SelectAdjacentAlly(-1);
        }

        /// <summary>
        /// Select ally panel by index.
        /// </summary>
        public void SelectAllyByIndex(int allyIndex)
        {
            if (_allies == null || allyIndex < 0 || allyIndex >= _allies.Length) return;
            if (_allies[allyIndex] == null || !_allies[allyIndex].IsAlive) return;

            SetSelectedAllyIndex(allyIndex);
        }

        /// <summary>
        /// Refresh status effects display.
        /// </summary>
        public void RefreshStatusEffects()
        {
            if (_playerPanel != null)
            {
                _playerPanel.RefreshStatusEffects();
            }

            foreach (var panel in _allyPanels)
            {
                if (panel != null)
                {
                    panel.RefreshStatusEffects();
                }
            }
        }

        /// <summary>
        /// Show or hide the entire HUD.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeAllyPanels()
        {
            if (_allyPanels == null) return;

            if (_allies.Length > _allyPanels.Count)
            {
                Debug.LogWarning($"[CombatHUD] More allies ({_allies.Length}) than ally panels ({_allyPanels.Count}). Some allies will not have UI.");
            }

            for (int i = 0; i < _allyPanels.Count; i++)
            {
                if (_allyPanels[i] != null)
                {
                    if (i < _allies.Length && _allies[i] != null)
                    {
                        _allyPanels[i].Initialize(_allies[i], i);
                        _allyPanels[i].SetVisible(true);
                    }
                    else
                    {
                        _allyPanels[i].SetVisible(false);
                    }
                }
            }

            UpdateAllyPanelSelectionVisuals();
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void HandleTargetCycling()
        {
            if (!_isInitialized) return;
            if (!InputManager.HasInstance) return;

            // Cycle targets
            if (InputManager.Instance.GetActionDown(InputManager.GameAction.TargetNext))
            {
                CycleTargetNext();
            }
            else if (InputManager.Instance.GetActionDown(InputManager.GameAction.TargetPrev))
            {
                CycleTargetPrevious();
            }

            // Cycle selected allied monster.
            if (InputManager.Instance.GetActionDown(InputManager.GameAction.CycleAlly))
            {
                CycleAllyNext();
            }
        }

        // =============================================================================
        // EVENT SUBSCRIPTION
        // =============================================================================

        private void SubscribeToEvents()
        {
            // Skill bar events
            if (_skillBar != null)
            {
                _skillBar.OnSkillActivated += HandleSkillActivated;
            }

            // Ally panel events
            if (_allyPanels != null)
            {
                foreach (var panel in _allyPanels)
                {
                    if (panel != null)
                    {
                        panel.OnUltimateTriggered += HandleAllyUltimateTriggered;
                    }
                }
            }

            // Capture banner events
            if (_captureBanner != null)
            {
                _captureBanner.OnCaptureRequested += HandleCaptureRequested;
            }

            // Player panel events
            if (_playerPanel != null)
            {
                _playerPanel.OnPlayerDeath += HandlePlayerDeath;
                _playerPanel.OnLowHP += HandleLowHP;
            }

            // Bidirectional sync: listen for BattleManager ally changes (e.g. from AI or swap)
            if (BattleManager.HasInstance)
            {
                BattleManager.Instance.OnActiveAllyChanged += OnBattleManagerAllyChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_skillBar != null)
            {
                _skillBar.OnSkillActivated -= HandleSkillActivated;
            }

            if (_allyPanels != null)
            {
                foreach (var panel in _allyPanels)
                {
                    if (panel != null)
                    {
                        panel.OnUltimateTriggered -= HandleAllyUltimateTriggered;
                    }
                }
            }

            if (_captureBanner != null)
            {
                _captureBanner.OnCaptureRequested -= HandleCaptureRequested;
            }

            if (_playerPanel != null)
            {
                _playerPanel.OnPlayerDeath -= HandlePlayerDeath;
                _playerPanel.OnLowHP -= HandleLowHP;
            }

            // Unsubscribe from BattleManager ally changes
            if (BattleManager.HasInstance)
            {
                BattleManager.Instance.OnActiveAllyChanged -= OnBattleManagerAllyChanged;
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleSkillActivated(int slotIndex)
        {
            OnSkillActivated?.Invoke(slotIndex);
        }

        private void HandleAllyUltimateTriggered(int allyIndex)
        {
            OnAllyUltimateTriggered?.Invoke(allyIndex);
        }

        private void HandleCaptureRequested()
        {
            OnCaptureRequested?.Invoke();
        }

        private void HandlePlayerDeath()
        {
            Debug.Log("[CombatHUD] Player died!");
            // Combat system will handle defeat
        }

        private void HandleLowHP()
        {
            Debug.Log("[CombatHUD] Player HP is low!");
            // Could trigger warning effects
        }

        private void OnBattleManagerAllyChanged(Combatant newAlly)
        {
            if (_allies == null) return;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i] == newAlly)
                {
                    _selectedAllyIndex = i;
                    UpdateAllyPanelSelectionVisuals();
                    return;
                }
            }
        }

        private void SelectFirstAliveAlly()
        {
            _selectedAllyIndex = -1;
            if (_allies == null) return;

            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i] != null && _allies[i].IsAlive)
                {
                    _selectedAllyIndex = i;
                    break;
                }
            }

            UpdateAllyPanelSelectionVisuals();
        }

        private void SelectAdjacentAlly(int direction)
        {
            if (_allies == null || _allies.Length == 0) return;

            if (_selectedAllyIndex < 0 || _selectedAllyIndex >= _allies.Length)
            {
                SelectFirstAliveAlly();
                SyncBattleManagerActiveAlly();
                return;
            }

            int total = _allies.Length;
            int scanIndex = _selectedAllyIndex;
            for (int hops = 0; hops < total; hops++)
            {
                scanIndex = (scanIndex + direction + total) % total;
                if (_allies[scanIndex] == null || !_allies[scanIndex].IsAlive) continue;

                SetSelectedAllyIndex(scanIndex);
                return;
            }
        }

        private void SetSelectedAllyIndex(int newIndex)
        {
            if (newIndex == _selectedAllyIndex) return;

            _selectedAllyIndex = newIndex;
            UpdateAllyPanelSelectionVisuals();
            SyncBattleManagerActiveAlly();

            Combatant selected = (_allies != null && newIndex >= 0 && newIndex < _allies.Length)
                ? _allies[newIndex]
                : null;
            OnSelectedAllyChanged?.Invoke(newIndex, selected);
        }

        private void UpdateAllyPanelSelectionVisuals()
        {
            if (_allyPanels == null) return;

            for (int i = 0; i < _allyPanels.Count; i++)
            {
                var panel = _allyPanels[i];
                if (panel == null) continue;

                bool shouldSelect = i == _selectedAllyIndex
                    && _allies != null
                    && i < _allies.Length
                    && _allies[i] != null
                    && _allies[i].IsAlive;
                panel.SetSelected(shouldSelect);
            }
        }

        private void SyncBattleManagerActiveAlly()
        {
            if (!BattleManager.HasInstance || _allies == null) return;
            if (_selectedAllyIndex < 0 || _selectedAllyIndex >= _allies.Length) return;

            var ally = _allies[_selectedAllyIndex];
            if (ally == null || !ally.IsAlive) return;

            BattleManager.Instance.SetActiveAlly(ally);
        }
    }
}

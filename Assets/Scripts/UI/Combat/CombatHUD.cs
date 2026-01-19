using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Capture;
using VeilBreakers.Commands;

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
        public static CombatHUD Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[CombatHUD] Instance is null. Ensure CombatHUD exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

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

        [Header("Settings")]
        [SerializeField] private KeyCode _targetNextKey = KeyCode.Tab;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private Combatant[] _allies;
        private Combatant[] _enemies;
        private Combatant _currentTarget;
        private int _targetIndex = 0;
        private bool _isInitialized = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public Combatant Player => _player;
        public Combatant CurrentTarget => _currentTarget;
        public bool IsInitialized => _isInitialized;
        public CombatUIConfig Config => _uiConfig;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<int> OnSkillActivated;          // Skill slot index
        public event Action<int> OnAllyUltimateTriggered;   // Ally index
        public event Action<Combatant> OnTargetChanged;
        public event Action OnCaptureRequested;

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
        }

        private void Start()
        {
            SubscribeToEvents();
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
            if (_enemies.Length > 0)
            {
                SetTarget(_enemies[0]);
            }

            _isInitialized = true;
            SetVisible(true);

            Debug.Log($"[CombatHUD] Initialized with {_allies.Length} allies and {_enemies.Length} enemies");
        }

        /// <summary>
        /// Clean up after combat ends.
        /// </summary>
        public void Cleanup()
        {
            _player = null;
            _allies = Array.Empty<Combatant>();
            _enemies = Array.Empty<Combatant>();
            _currentTarget = null;
            _targetIndex = 0;
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
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void HandleTargetCycling()
        {
            if (!_isInitialized) return;

            // TAB to cycle targets
            if (Input.GetKeyDown(_targetNextKey))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    CycleTargetPrevious();
                }
                else
                {
                    CycleTargetNext();
                }
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
            foreach (var panel in _allyPanels)
            {
                if (panel != null)
                {
                    panel.OnUltimateTriggered += HandleAllyUltimateTriggered;
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
        }

        private void UnsubscribeFromEvents()
        {
            if (_skillBar != null)
            {
                _skillBar.OnSkillActivated -= HandleSkillActivated;
            }

            foreach (var panel in _allyPanels)
            {
                if (panel != null)
                {
                    panel.OnUltimateTriggered -= HandleAllyUltimateTriggered;
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
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using static VeilBreakers.Core.InputManager; // Using static for direct access to GameAction enum

namespace VeilBreakers.Commands
{
    /// <summary>
    /// State of the radial menu.
    /// </summary>
    public enum RadialMenuState
    {
        CLOSED,
        SELECTING_ALLY,
        SELECTING_COMMAND,
        SELECTING_TARGET,
        SELECTING_GROUND
    }

    /// <summary>
    /// Controls the radial menu UI for quick commands.
    /// Handles input, selection, and visual feedback.
    /// </summary>
    public class RadialMenuController : SingletonMonoBehaviour<RadialMenuController>
    {
        // This is a scene-specific singleton
        protected override bool IsPersistent => false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Visual")]
        [SerializeField] private float _wheelRadius = 200f;
        [SerializeField] private float _innerDeadzone = 50f;

        [Header("References")]
        [SerializeField] private RectTransform _menuContainer;
        [SerializeField] private RectTransform _allyWheelContainer;
        [SerializeField] private RectTransform _commandWheelContainer;

        // =============================================================================
        // STATE
        // =============================================================================

        private RadialMenuState _state = RadialMenuState.CLOSED;
        private Combatant _selectedAlly;
        private QuickCommandType _selectedCommand;
        private Combatant _selectedTarget;
        private Vector3 _selectedGroundPosition;
        private int _highlightedIndex = -1;

        private Combatant _player;
        private Combatant[] _allies;
        private Combatant[] _enemies;
        private CommandOption[] _commandOptions;
        private readonly GameAction[] _allyHotkeys = { GameAction.Ally1, GameAction.Ally2, GameAction.Ally3 };

        private Vector2 _menuCenter;
        private bool _inputLocked = false;

        // Cached references
        private InputManager _inputManager;
        private Camera _mainCamera;
        private int _groundLayerMask;

        // Events
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;
        public event Action<Combatant> OnAllySelected;
        public event Action<QuickCommandType> OnCommandSelected;
        public event Action<Combatant, QuickCommandType, Combatant, Vector3> OnCommandConfirmed;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public RadialMenuState State => _state;
        public bool IsOpen => _state != RadialMenuState.CLOSED;
        public Combatant SelectedAlly => _selectedAlly;
        public int HighlightedIndex => _highlightedIndex;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            _groundLayerMask = LayerMask.GetMask("Ground");
        }

        private void Start()
        {
            _inputManager = InputManager.Instance;
            _mainCamera = Camera.main;
            _commandOptions = QuickCommandManager.Instance?.GetAvailableCommands();
            SetMenuVisible(false);
        }

        private void Update()
        {
            if (_inputManager == null) return;
            
            HandleInput();
            
            if (IsOpen)
            {
                UpdateSelection();
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        public void Initialize(Combatant player, Combatant[] allies, Combatant[] enemies)
        {
            _player = player;
            _allies = allies ?? Array.Empty<Combatant>();
            _enemies = enemies ?? Array.Empty<Combatant>();
            _commandOptions = QuickCommandManager.Instance?.GetAvailableCommands();
        }

        // =============================================================================
        // INPUT HANDLING
        // =============================================================================

        private void HandleInput()
        {
            if (_inputLocked) return;

            switch (_state)
            {
                case RadialMenuState.CLOSED:
                    if (_inputManager.GetActionDown(GameAction.OpenMenu)) OpenMenu();
                    break;

                case RadialMenuState.SELECTING_ALLY:
                    HandleAllySelectionInput();
                    break;

                case RadialMenuState.SELECTING_COMMAND:
                    HandleCommandSelectionInput();
                    break;

                case RadialMenuState.SELECTING_TARGET:
                    HandleTargetSelectionInput();
                    break;

                case RadialMenuState.SELECTING_GROUND:
                    HandleGroundSelectionInput();
                    break;
            }
        }

        private void HandleAllySelectionInput()
        {
            if (_inputManager.GetActionDown(GameAction.Cancel) || _inputManager.GetActionDown(GameAction.OpenMenu) || _inputManager.GetMouseButtonDown(1))
            {
                CloseMenu();
                return;
            }

            for (int i = 0; i < _allyHotkeys.Length && i < _allies.Length; i++)
            {
                if (_inputManager.GetActionDown(_allyHotkeys[i]))
                {
                    if (QuickCommandManager.Instance?.CanCommand(_allies[i]) ?? false)
                    {
                        SelectAlly(_allies[i]);
                    }
                    return;
                }
            }

            if ((_inputManager.GetMouseButtonDown(0) || _inputManager.GetActionDown(GameAction.Confirm)) && _highlightedIndex >= 0)
            {
                if (_highlightedIndex < _allies.Length && (QuickCommandManager.Instance?.CanCommand(_allies[_highlightedIndex]) ?? false))
                {
                    SelectAlly(_allies[_highlightedIndex]);
                }
            }
        }

        private void HandleCommandSelectionInput()
        {
            if (_inputManager.GetActionDown(GameAction.Cancel) || _inputManager.GetMouseButtonDown(1))
            {
                GoBackToAllySelection();
                return;
            }
            if (_inputManager.GetActionDown(GameAction.OpenMenu))
            {
                CloseMenu();
                return;
            }

            if ((_inputManager.GetMouseButtonDown(0) || _inputManager.GetActionDown(GameAction.Confirm)) && _highlightedIndex >= 0)
            {
                if (_commandOptions != null && _highlightedIndex < _commandOptions.Length)
                {
                    SelectCommand(_commandOptions[_highlightedIndex].commandType);
                }
            }
        }

        private void HandleTargetSelectionInput()
        {
            if (_inputManager.GetActionDown(GameAction.Cancel) || _inputManager.GetMouseButtonDown(1))
            {
                GoBackToCommandSelection();
                return;
            }
            if (_inputManager.GetActionDown(GameAction.OpenMenu))
            {
                CloseMenu();
                return;
            }
            if (_inputManager.GetActionDown(GameAction.TargetNext))
            {
                CycleEnemyTarget();
            }
            if (_inputManager.GetActionDown(GameAction.CycleAlly))
            {
                CycleAllyTarget();
            }
            if (_inputManager.GetMouseButtonDown(0) || _inputManager.GetActionDown(GameAction.Confirm))
            {
                ConfirmTarget();
            }
        }

        private void HandleGroundSelectionInput()
        {
            if (_inputManager.GetActionDown(GameAction.Cancel) || _inputManager.GetMouseButtonDown(1))
            {
                GoBackToCommandSelection();
                return;
            }
            if (_inputManager.GetActionDown(GameAction.OpenMenu))
            {
                CloseMenu();
                return;
            }
            if (_inputManager.GetMouseButtonDown(0))
            {
                if (GetGroundPositionFromMouse(out Vector3 position))
                {
                    _selectedGroundPosition = position;
                    ConfirmCommand();
                }
            }
        }

        // =============================================================================
        // SELECTION UPDATES
        // =============================================================================

        private void UpdateSelection()
        {
            Vector2 mousePos = _inputManager.MousePosition;
            Vector2 direction = mousePos - _menuCenter;
            float distance = direction.magnitude;

            if (distance > _innerDeadzone && distance < _wheelRadius)
            {
                float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 360f) % 360f;
                int itemCount = _state == RadialMenuState.SELECTING_ALLY ? _allies.Length : (_commandOptions?.Length ?? 0);
                _highlightedIndex = GetIndexFromAngle(angle, itemCount);
            }
            else if (distance < _innerDeadzone)
            {
                _highlightedIndex = -1;
            }

            int currentItemCount = _state == RadialMenuState.SELECTING_ALLY ? _allies.Length : (_commandOptions?.Length ?? 0);
            if (currentItemCount > 0)
            {
                if (_inputManager.GetActionDown(GameAction.MoveRight)) _highlightedIndex = (_highlightedIndex + 1) % currentItemCount;
                if (_inputManager.GetActionDown(GameAction.MoveLeft)) _highlightedIndex = (_highlightedIndex - 1 + currentItemCount) % currentItemCount;
            }
        }

        private int GetIndexFromAngle(float angle, int itemCount)
        {
            if (itemCount <= 0) return -1;
            float segmentAngle = 360f / itemCount;
            float adjustedAngle = (angle - 90f - (segmentAngle / 2f) + 360f) % 360f;
            return (itemCount - 1) - (int)(adjustedAngle / segmentAngle);
        }

        // =============================================================================
        // MENU CONTROL
        // =============================================================================

        public void OpenMenu()
        {
            if (IsOpen || _allies == null || _allies.Length == 0) return;
            
            _state = RadialMenuState.SELECTING_ALLY;
            _highlightedIndex = -1;
            _menuCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            SetMenuVisible(true);
            ShowAllyWheel();

            TimeSlowController.Instance?.RequestTimeSlow();
            OnMenuOpened?.Invoke();
            Debug.Log("[RadialMenuController] Menu opened - selecting ally");
        }

        public void CloseMenu()
        {
            if (!IsOpen) return;

            _state = RadialMenuState.CLOSED;
            _selectedAlly = null;
            _selectedCommand = QuickCommandType.NONE;
            _selectedTarget = null;
            _highlightedIndex = -1;

            SetMenuVisible(false);
            TimeSlowController.Instance?.ReleaseTimeSlow();
            OnMenuClosed?.Invoke();
            Debug.Log("[RadialMenuController] Menu closed");
        }

        private void SelectAlly(Combatant ally)
        {
            _selectedAlly = ally;
            _state = RadialMenuState.SELECTING_COMMAND;
            _highlightedIndex = -1;

            HideAllyWheel();
            ShowCommandWheel();
            OnAllySelected?.Invoke(ally);
            Debug.Log($"[RadialMenuController] Selected ally: {ally.DisplayName}");
        }

        private void SelectCommand(QuickCommandType command)
        {
            _selectedCommand = command;
            OnCommandSelected?.Invoke(command);
            var targetType = QuickCommandInstance.GetTargetType(command);

            switch (targetType)
            {
                case CommandTargetType.ENEMY:
                    _state = RadialMenuState.SELECTING_TARGET;
                    _selectedTarget = _enemies.Length > 0 ? _enemies[0] : null;
                    HideCommandWheel();
                    break;
                case CommandTargetType.ALLY:
                    _state = RadialMenuState.SELECTING_TARGET;
                    _selectedTarget = _allies.Length > 0 ? _allies[0] : null;
                    HideCommandWheel();
                    break;
                case CommandTargetType.GROUND:
                    _state = RadialMenuState.SELECTING_GROUND;
                    HideCommandWheel();
                    break;
                default:
                    ConfirmCommand();
                    break;
            }
            Debug.Log($"[RadialMenuController] Selected command: {command} - now {State}");
        }

        private void GoBackToAllySelection()
        {
            _state = RadialMenuState.SELECTING_ALLY;
            _selectedCommand = QuickCommandType.NONE;
            _highlightedIndex = -1;
            HideCommandWheel();
            ShowAllyWheel();
        }

        private void GoBackToCommandSelection()
        {
            _state = RadialMenuState.SELECTING_COMMAND;
            _selectedTarget = null;
            _highlightedIndex = -1;
            ShowCommandWheel();
        }

        private void CycleEnemyTarget()
        {
            if (_enemies.Length == 0) return;
            int currentIndex = Array.IndexOf(_enemies, _selectedTarget);
            int nextIndex = (currentIndex + 1) % _enemies.Length;
            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[nextIndex];
                if (enemy != null && enemy.IsAlive)
                {
                    _selectedTarget = enemy;
                    return;
                }
                nextIndex = (nextIndex + 1) % _enemies.Length;
            }
        }

        private void CycleAllyTarget()
        {
            if (_allies.Length <= 1) return;
            int currentIndex = Array.IndexOf(_allies, _selectedTarget);
            int nextIndex = (currentIndex + 1) % _allies.Length;
            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[nextIndex];
                if (ally != null && ally.IsAlive && ally != _selectedAlly)
                {
                    _selectedTarget = ally;
                    return;
                }
                nextIndex = (nextIndex + 1) % _allies.Length;
            }
        }

        private void ConfirmTarget()
        {
            if (_selectedTarget != null && _selectedTarget.IsAlive) ConfirmCommand();
        }

        private void ConfirmCommand()
        {
            if (_selectedAlly == null || _selectedCommand == QuickCommandType.NONE)
            {
                CloseMenu();
                return;
            }

            bool success = QuickCommandManager.Instance?.IssueCommand(
                _selectedCommand, _selectedAlly, _selectedTarget,
                _state == RadialMenuState.SELECTING_GROUND ? _selectedGroundPosition : (Vector3?)null
            ) ?? false;

            if (success)
            {
                OnCommandConfirmed?.Invoke(_selectedAlly, _selectedCommand, _selectedTarget, _selectedGroundPosition);
            }
            CloseMenu();
        }

        // =============================================================================
        // VISUAL & UTILITY
        // =============================================================================

        private void SetMenuVisible(bool visible) => _menuContainer?.gameObject.SetActive(visible);
        private void ShowAllyWheel() => _allyWheelContainer?.gameObject.SetActive(true);
        private void HideAllyWheel() => _allyWheelContainer?.gameObject.SetActive(false);
        private void ShowCommandWheel() => _commandWheelContainer?.gameObject.SetActive(true);
        private void HideCommandWheel() => _commandWheelContainer?.gameObject.SetActive(false);

        private bool GetGroundPositionFromMouse(out Vector3 position)
        {
            position = Vector3.zero;
            if (_mainCamera == null) return false;
            Ray ray = _mainCamera.ScreenPointToRay(_inputManager.MousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, _groundLayerMask))
            {
                position = hit.point;
                return true;
            }
            return false;
        }
    }
}

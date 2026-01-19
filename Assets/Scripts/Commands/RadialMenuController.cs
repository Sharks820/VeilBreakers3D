using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;

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
    public class RadialMenuController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static RadialMenuController _instance;
        public static RadialMenuController Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[RadialMenuController] Instance is null. Ensure RadialMenuController exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Input")]
        [SerializeField] private KeyCode _openKey = KeyCode.Q;
        [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;
        [SerializeField] private KeyCode _confirmKey = KeyCode.Return;
        [SerializeField] private KeyCode[] _allyHotkeys = { KeyCode.F1, KeyCode.F2, KeyCode.F3 };
        [SerializeField] private KeyCode _cycleEnemyKey = KeyCode.Tab;
        [SerializeField] private KeyCode _cycleAllyKey = KeyCode.LeftControl;

        [Header("Visual")]
        [SerializeField] private float _wheelRadius = 200f;
        [SerializeField] private float _innerDeadzone = 50f;
        [SerializeField] private float _selectionAngleThreshold = 30f;

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

        private Vector2 _menuCenter;
        private bool _inputLocked = false;

        // Cached references to avoid per-frame allocations
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

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Cache layer mask once (avoid string lookup every frame)
            _groundLayerMask = LayerMask.GetMask("Ground");
        }

        private void Start()
        {
            // Cache camera and command options in Start (after all Awakes complete)
            _mainCamera = Camera.main;
            _commandOptions = QuickCommandManager.Instance?.GetAvailableCommands();

            // Start closed
            SetMenuVisible(false);
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
            HandleInput();
            UpdateSelection();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize with combat participants.
        /// </summary>
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
                    HandleClosedInput();
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

        private void HandleClosedInput()
        {
            // Q to open menu
            if (Input.GetKeyDown(_openKey))
            {
                OpenMenu();
            }
        }

        private void HandleAllySelectionInput()
        {
            // Cancel
            if (Input.GetKeyDown(_cancelKey) || Input.GetKeyDown(_openKey) || Input.GetMouseButtonDown(1))
            {
                CloseMenu();
                return;
            }

            // Hotkey selection (F1, F2, F3)
            for (int i = 0; i < _allyHotkeys.Length && i < _allies.Length; i++)
            {
                if (Input.GetKeyDown(_allyHotkeys[i]))
                {
                    if (QuickCommandManager.Instance?.CanCommand(_allies[i]) ?? false)
                    {
                        SelectAlly(_allies[i]);
                    }
                    return;
                }
            }

            // Click or Enter to confirm highlighted
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(_confirmKey)) && _highlightedIndex >= 0)
            {
                if (_highlightedIndex < _allies.Length &&
                    (QuickCommandManager.Instance?.CanCommand(_allies[_highlightedIndex]) ?? false))
                {
                    SelectAlly(_allies[_highlightedIndex]);
                }
            }
        }

        private void HandleCommandSelectionInput()
        {
            // Cancel - go back to ally selection
            if (Input.GetKeyDown(_cancelKey) || Input.GetMouseButtonDown(1))
            {
                GoBackToAllySelection();
                return;
            }

            // Q closes entirely
            if (Input.GetKeyDown(_openKey))
            {
                CloseMenu();
                return;
            }

            // Click or Enter to confirm highlighted
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(_confirmKey)) && _highlightedIndex >= 0)
            {
                if (_highlightedIndex < _commandOptions.Length)
                {
                    SelectCommand(_commandOptions[_highlightedIndex].commandType);
                }
            }
        }

        private void HandleTargetSelectionInput()
        {
            // Cancel - go back to command selection
            if (Input.GetKeyDown(_cancelKey) || Input.GetMouseButtonDown(1))
            {
                GoBackToCommandSelection();
                return;
            }

            if (Input.GetKeyDown(_openKey))
            {
                CloseMenu();
                return;
            }

            // TAB cycles enemies
            if (Input.GetKeyDown(_cycleEnemyKey))
            {
                CycleEnemyTarget();
            }

            // CTRL cycles allies
            if (Input.GetKeyDown(_cycleAllyKey))
            {
                CycleAllyTarget();
            }

            // Click or Enter to confirm
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(_confirmKey))
            {
                ConfirmTarget();
            }
        }

        private void HandleGroundSelectionInput()
        {
            // Cancel - go back to command selection
            if (Input.GetKeyDown(_cancelKey) || Input.GetMouseButtonDown(1))
            {
                GoBackToCommandSelection();
                return;
            }

            if (Input.GetKeyDown(_openKey))
            {
                CloseMenu();
                return;
            }

            // Click to confirm ground position
            if (Input.GetMouseButtonDown(0))
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
            if (_state == RadialMenuState.CLOSED) return;

            // Calculate highlighted index based on mouse position
            Vector2 mousePos = Input.mousePosition;
            Vector2 direction = mousePos - _menuCenter;
            float distance = direction.magnitude;

            // Check if within wheel area but outside deadzone
            if (distance > _innerDeadzone && distance < _wheelRadius)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                // Calculate highlighted index based on state
                switch (_state)
                {
                    case RadialMenuState.SELECTING_ALLY:
                        _highlightedIndex = GetIndexFromAngle(angle, _allies.Length);
                        break;

                    case RadialMenuState.SELECTING_COMMAND:
                        _highlightedIndex = GetIndexFromAngle(angle, _commandOptions?.Length ?? 0);
                        break;

                    default:
                        _highlightedIndex = -1;
                        break;
                }
            }
            else if (distance < _innerDeadzone)
            {
                _highlightedIndex = -1;
            }

            // Keyboard navigation (arrow keys)
            int itemCount = _state == RadialMenuState.SELECTING_ALLY ? _allies.Length :
                           _state == RadialMenuState.SELECTING_COMMAND ? (_commandOptions?.Length ?? 0) : 0;

            if (itemCount > 0)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    _highlightedIndex = (_highlightedIndex + 1) % itemCount;
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    _highlightedIndex = (_highlightedIndex - 1 + itemCount) % itemCount;
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    _highlightedIndex = Mathf.Max(0, _highlightedIndex - 3);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    _highlightedIndex = Mathf.Min(itemCount - 1, _highlightedIndex + 3);
                }
            }
        }

        private int GetIndexFromAngle(float angle, int itemCount)
        {
            if (itemCount <= 0) return -1;

            float segmentAngle = 360f / itemCount;
            float startOffset = 90f; // Start from top

            // Adjust angle to start from top
            angle = (angle - startOffset + 360f) % 360f;

            return (int)(angle / segmentAngle) % itemCount;
        }

        // =============================================================================
        // MENU CONTROL
        // =============================================================================

        /// <summary>
        /// Open the quick command menu.
        /// </summary>
        public void OpenMenu()
        {
            if (_state != RadialMenuState.CLOSED) return;
            if (_allies == null || _allies.Length == 0)
            {
                Debug.LogWarning("[RadialMenuController] No allies available for commands");
                return;
            }

            _state = RadialMenuState.SELECTING_ALLY;
            _highlightedIndex = 0;
            _menuCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            SetMenuVisible(true);
            ShowAllyWheel();

            // Request time slow
            TimeSlowController.Instance?.RequestTimeSlow();

            OnMenuOpened?.Invoke();

            Debug.Log("[RadialMenuController] Menu opened - selecting ally");
        }

        /// <summary>
        /// Close the quick command menu.
        /// </summary>
        public void CloseMenu()
        {
            if (_state == RadialMenuState.CLOSED) return;

            _state = RadialMenuState.CLOSED;
            _selectedAlly = null;
            _selectedCommand = QuickCommandType.NONE;
            _selectedTarget = null;
            _highlightedIndex = -1;

            SetMenuVisible(false);

            // Release time slow
            TimeSlowController.Instance?.ReleaseTimeSlow();

            OnMenuClosed?.Invoke();

            Debug.Log("[RadialMenuController] Menu closed");
        }

        private void SelectAlly(Combatant ally)
        {
            _selectedAlly = ally;
            _state = RadialMenuState.SELECTING_COMMAND;
            _highlightedIndex = 0;

            HideAllyWheel();
            ShowCommandWheel();

            OnAllySelected?.Invoke(ally);

            Debug.Log($"[RadialMenuController] Selected ally: {ally.DisplayName}");
        }

        private void SelectCommand(QuickCommandType command)
        {
            _selectedCommand = command;

            OnCommandSelected?.Invoke(command);

            // Check if command needs target
            var targetType = QuickCommandInstance.GetTargetType(command);

            switch (targetType)
            {
                case CommandTargetType.ENEMY:
                    _state = RadialMenuState.SELECTING_TARGET;
                    _selectedTarget = _enemies.Length > 0 ? _enemies[0] : null;
                    HideCommandWheel();
                    Debug.Log($"[RadialMenuController] Selected command: {command} - selecting enemy target");
                    break;

                case CommandTargetType.ALLY:
                    _state = RadialMenuState.SELECTING_TARGET;
                    _selectedTarget = _allies.Length > 0 ? _allies[0] : null;
                    HideCommandWheel();
                    Debug.Log($"[RadialMenuController] Selected command: {command} - selecting ally target");
                    break;

                case CommandTargetType.GROUND:
                    _state = RadialMenuState.SELECTING_GROUND;
                    HideCommandWheel();
                    Debug.Log($"[RadialMenuController] Selected command: {command} - selecting ground position");
                    break;

                default:
                    // No target needed, execute immediately
                    ConfirmCommand();
                    break;
            }
        }

        private void GoBackToAllySelection()
        {
            _state = RadialMenuState.SELECTING_ALLY;
            _selectedCommand = QuickCommandType.NONE;
            _highlightedIndex = 0;

            HideCommandWheel();
            ShowAllyWheel();

            Debug.Log("[RadialMenuController] Back to ally selection");
        }

        private void GoBackToCommandSelection()
        {
            _state = RadialMenuState.SELECTING_COMMAND;
            _selectedTarget = null;
            _highlightedIndex = 0;

            ShowCommandWheel();

            Debug.Log("[RadialMenuController] Back to command selection");
        }

        private void CycleEnemyTarget()
        {
            if (_enemies.Length == 0) return;

            int currentIndex = Array.IndexOf(_enemies, _selectedTarget);
            int nextIndex = (currentIndex + 1) % _enemies.Length;

            // Skip dead enemies
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
            if (_allies.Length == 0) return;

            int currentIndex = Array.IndexOf(_allies, _selectedTarget);
            int nextIndex = (currentIndex + 1) % _allies.Length;

            // Skip dead allies
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
            if (_selectedTarget != null && _selectedTarget.IsAlive)
            {
                ConfirmCommand();
            }
        }

        private void ConfirmCommand()
        {
            if (_selectedAlly == null || _selectedCommand == QuickCommandType.NONE)
            {
                Debug.LogWarning("[RadialMenuController] Cannot confirm: no ally or command selected");
                CloseMenu();
                return;
            }

            // Issue the command
            bool success = QuickCommandManager.Instance?.IssueCommand(
                _selectedCommand,
                _selectedAlly,
                _selectedTarget,
                _state == RadialMenuState.SELECTING_GROUND ? _selectedGroundPosition : (Vector3?)null
            ) ?? false;

            if (success)
            {
                OnCommandConfirmed?.Invoke(_selectedAlly, _selectedCommand, _selectedTarget, _selectedGroundPosition);
                Debug.Log($"[RadialMenuController] Command confirmed: {_selectedAlly.DisplayName} -> {_selectedCommand}");
            }

            CloseMenu();
        }

        // =============================================================================
        // VISUAL HELPERS
        // =============================================================================

        private void SetMenuVisible(bool visible)
        {
            if (_menuContainer != null)
            {
                _menuContainer.gameObject.SetActive(visible);
            }
        }

        private void ShowAllyWheel()
        {
            if (_allyWheelContainer != null)
            {
                _allyWheelContainer.gameObject.SetActive(true);
            }
        }

        private void HideAllyWheel()
        {
            if (_allyWheelContainer != null)
            {
                _allyWheelContainer.gameObject.SetActive(false);
            }
        }

        private void ShowCommandWheel()
        {
            if (_commandWheelContainer != null)
            {
                _commandWheelContainer.gameObject.SetActive(true);
            }
        }

        private void HideCommandWheel()
        {
            if (_commandWheelContainer != null)
            {
                _commandWheelContainer.gameObject.SetActive(false);
            }
        }

        private bool GetGroundPositionFromMouse(out Vector3 position)
        {
            position = Vector3.zero;

            // Use cached camera reference (avoid FindGameObjectWithTag every call)
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return false;
            }

            // Raycast from mouse to ground using cached layer mask
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayerMask))
            {
                position = hit.point;
                return true;
            }

            return false;
        }

        // =============================================================================
        // QUERIES
        // =============================================================================

        /// <summary>
        /// Get the world positions for ally portraits on the wheel.
        /// </summary>
        public Vector2[] GetAllyWheelPositions()
        {
            if (_allies == null || _allies.Length == 0) return Array.Empty<Vector2>();

            var positions = new Vector2[_allies.Length];
            float angleStep = 360f / _allies.Length;

            for (int i = 0; i < _allies.Length; i++)
            {
                float angle = (90f - i * angleStep) * Mathf.Deg2Rad;
                positions[i] = _menuCenter + new Vector2(
                    Mathf.Cos(angle) * _wheelRadius * 0.7f,
                    Mathf.Sin(angle) * _wheelRadius * 0.7f
                );
            }

            return positions;
        }

        /// <summary>
        /// Get the world positions for command options on the wheel.
        /// </summary>
        public Vector2[] GetCommandWheelPositions()
        {
            if (_commandOptions == null || _commandOptions.Length == 0) return Array.Empty<Vector2>();

            var positions = new Vector2[_commandOptions.Length];
            float angleStep = 360f / _commandOptions.Length;

            for (int i = 0; i < _commandOptions.Length; i++)
            {
                float angle = (90f - i * angleStep) * Mathf.Deg2Rad;
                positions[i] = _menuCenter + new Vector2(
                    Mathf.Cos(angle) * _wheelRadius * 0.7f,
                    Mathf.Sin(angle) * _wheelRadius * 0.7f
                );
            }

            return positions;
        }
    }
}

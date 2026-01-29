using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Centralized input handling for VeilBreakers.
    /// Abstracts legacy Input API to prepare for Unity Input System migration.
    /// All game systems should use this instead of Input.* directly.
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        // =============================================================================
        // INPUT ACTIONS (logical names)
        // =============================================================================

        public enum GameAction
        {
            // Combat & UI
            Confirm,
            Cancel,
            OpenMenu,

            // Movement/Navigation
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,

            // Combat specific
            TargetNext,
            TargetPrevious,
            CycleEnemy,
            CycleAlly,

            // Quick commands
            Skill1,
            Skill2,
            Skill3,
            Skill4,
            Ultimate,

            // Capture
            MarkForCapture,
            CancelMark,
            CaptureAction,

            // Ally hotkeys
            Ally1,
            Ally2,
            Ally3,

            // Dialogue
            AdvanceDialogue,
            SkipDialogue,

            // Modifier
            ShiftModifier
        }

        // =============================================================================
        // DEFAULT BINDINGS
        // =============================================================================

        private readonly Dictionary<GameAction, KeyCode[]> _defaultBindings = new()
        {
            // Combat & UI
            { GameAction.Confirm, new[] { KeyCode.Return, KeyCode.Space, KeyCode.E } },
            { GameAction.Cancel, new[] { KeyCode.Escape, KeyCode.Backspace } },
            { GameAction.OpenMenu, new[] { KeyCode.Tab, KeyCode.Q } },

            // Movement/Navigation
            { GameAction.MoveUp, new[] { KeyCode.W, KeyCode.UpArrow } },
            { GameAction.MoveDown, new[] { KeyCode.S, KeyCode.DownArrow } },
            { GameAction.MoveLeft, new[] { KeyCode.A, KeyCode.LeftArrow } },
            { GameAction.MoveRight, new[] { KeyCode.D, KeyCode.RightArrow } },

            // Combat specific
            { GameAction.TargetNext, new[] { KeyCode.Tab } },
            { GameAction.TargetPrevious, new[] { KeyCode.Tab } },  // + Shift
            { GameAction.CycleEnemy, new[] { KeyCode.E } },
            { GameAction.CycleAlly, new[] { KeyCode.Q } },

            // Quick commands (number keys)
            { GameAction.Skill1, new[] { KeyCode.Alpha1 } },
            { GameAction.Skill2, new[] { KeyCode.Alpha2 } },
            { GameAction.Skill3, new[] { KeyCode.Alpha3 } },
            { GameAction.Skill4, new[] { KeyCode.Alpha4 } },
            { GameAction.Ultimate, new[] { KeyCode.R } },

            // Capture
            { GameAction.MarkForCapture, new[] { KeyCode.C } },
            { GameAction.CancelMark, new[] { KeyCode.X } },
            { GameAction.CaptureAction, new[] { KeyCode.Space, KeyCode.Return } },

            // Ally hotkeys
            { GameAction.Ally1, new[] { KeyCode.F1 } },
            { GameAction.Ally2, new[] { KeyCode.F2 } },
            { GameAction.Ally3, new[] { KeyCode.F3 } },

            // Dialogue
            { GameAction.AdvanceDialogue, new[] { KeyCode.Space, KeyCode.Return } },
            { GameAction.SkipDialogue, new[] { KeyCode.Space } },

            // Modifier
            { GameAction.ShiftModifier, new[] { KeyCode.LeftShift, KeyCode.RightShift } }
        };

        // =============================================================================
        // STATE
        // =============================================================================

        private Dictionary<GameAction, KeyCode[]> _currentBindings;
        private bool _inputEnabled = true;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current mouse position in screen coordinates</summary>
        public Vector2 MousePosition => Input.mousePosition;

        /// <summary>Whether input processing is enabled</summary>
        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when any game action is triggered</summary>
        public event Action<GameAction> OnActionTriggered;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            _currentBindings = new Dictionary<GameAction, KeyCode[]>(_defaultBindings);
            LoadBindings();
        }

        // =============================================================================
        // PUBLIC API - ACTION QUERIES
        // =============================================================================

        /// <summary>
        /// Returns true during the frame the action was pressed.
        /// Equivalent to Input.GetKeyDown for the action's bound keys.
        /// </summary>
        public bool GetActionDown(GameAction action)
        {
            if (!_inputEnabled) return false;

            if (!_currentBindings.TryGetValue(action, out var keys)) return false;

            foreach (var key in keys)
            {
                if (Input.GetKeyDown(key))
                {
                    OnActionTriggered?.Invoke(action);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true while the action's key is held down.
        /// Equivalent to Input.GetKey for the action's bound keys.
        /// </summary>
        public bool GetAction(GameAction action)
        {
            if (!_inputEnabled) return false;

            if (!_currentBindings.TryGetValue(action, out var keys)) return false;

            foreach (var key in keys)
            {
                if (Input.GetKey(key)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true during the frame the action was released.
        /// Equivalent to Input.GetKeyUp for the action's bound keys.
        /// </summary>
        public bool GetActionUp(GameAction action)
        {
            if (!_inputEnabled) return false;

            if (!_currentBindings.TryGetValue(action, out var keys)) return false;

            foreach (var key in keys)
            {
                if (Input.GetKeyUp(key)) return true;
            }
            return false;
        }

        // =============================================================================
        // PUBLIC API - MOUSE
        // =============================================================================

        /// <summary>
        /// Returns true during the frame the mouse button was pressed.
        /// </summary>
        /// <param name="button">0 = left, 1 = right, 2 = middle</param>
        public bool GetMouseButtonDown(int button)
        {
            if (!_inputEnabled) return false;
            return Input.GetMouseButtonDown(button);
        }

        /// <summary>
        /// Returns true while the mouse button is held down.
        /// </summary>
        public bool GetMouseButton(int button)
        {
            if (!_inputEnabled) return false;
            return Input.GetMouseButton(button);
        }

        /// <summary>
        /// Returns true during the frame the mouse button was released.
        /// </summary>
        public bool GetMouseButtonUp(int button)
        {
            if (!_inputEnabled) return false;
            return Input.GetMouseButtonUp(button);
        }

        /// <summary>
        /// Returns a ray from the camera through the mouse position.
        /// </summary>
        public Ray GetMouseRay(Camera camera)
        {
            return camera.ScreenPointToRay(MousePosition);
        }

        // =============================================================================
        // PUBLIC API - RAW KEY ACCESS (for specific KeyCode checks)
        // =============================================================================

        /// <summary>
        /// Direct KeyCode check - use sparingly, prefer GameAction queries.
        /// </summary>
        public bool GetKeyDown(KeyCode key)
        {
            if (!_inputEnabled) return false;
            return Input.GetKeyDown(key);
        }

        /// <summary>
        /// Direct KeyCode check - use sparingly, prefer GameAction queries.
        /// </summary>
        public bool GetKey(KeyCode key)
        {
            if (!_inputEnabled) return false;
            return Input.GetKey(key);
        }

        // =============================================================================
        // PUBLIC API - MODIFIERS
        // =============================================================================

        /// <summary>Returns true if shift is held</summary>
        public bool IsShiftHeld => GetAction(GameAction.ShiftModifier);

        /// <summary>Returns true if control is held</summary>
        public bool IsControlHeld => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        /// <summary>Returns true if alt is held</summary>
        public bool IsAltHeld => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // =============================================================================
        // PUBLIC API - BINDING MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Rebinds an action to new keys.
        /// </summary>
        public void SetBinding(GameAction action, params KeyCode[] keys)
        {
            _currentBindings[action] = keys;
            SaveBindings();
        }

        /// <summary>
        /// Gets the current keys bound to an action.
        /// </summary>
        public KeyCode[] GetBinding(GameAction action)
        {
            return _currentBindings.TryGetValue(action, out var keys) ? keys : Array.Empty<KeyCode>();
        }

        /// <summary>
        /// Resets all bindings to defaults.
        /// </summary>
        public void ResetBindingsToDefault()
        {
            _currentBindings = new Dictionary<GameAction, KeyCode[]>(_defaultBindings);
            SaveBindings();
        }

        /// <summary>
        /// Gets a display string for the primary key of an action.
        /// </summary>
        public string GetBindingDisplayName(GameAction action)
        {
            if (!_currentBindings.TryGetValue(action, out var keys) || keys.Length == 0)
                return "???";

            return GetKeyDisplayName(keys[0]);
        }

        // =============================================================================
        // PRIVATE HELPERS
        // =============================================================================

        private string GetKeyDisplayName(KeyCode key)
        {
            return key switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Return => "Enter",
                KeyCode.Escape => "Esc",
                KeyCode.LeftShift => "Shift",
                KeyCode.RightShift => "Shift",
                KeyCode.LeftControl => "Ctrl",
                KeyCode.RightControl => "Ctrl",
                KeyCode.LeftAlt => "Alt",
                KeyCode.RightAlt => "Alt",
                KeyCode.UpArrow => "Up",
                KeyCode.DownArrow => "Down",
                KeyCode.LeftArrow => "Left",
                KeyCode.RightArrow => "Right",
                KeyCode.Space => "Space",
                KeyCode.Tab => "Tab",
                KeyCode.Backspace => "Back",
                _ => key.ToString()
            };
        }

        private void SaveBindings()
        {
            // TODO: Serialize bindings to PlayerPrefs or save file
            // For now, bindings reset each session
            Debug.Log("[InputManager] Bindings would be saved here");
        }

        private void LoadBindings()
        {
            // TODO: Load bindings from PlayerPrefs or save file
            // For now, use defaults
            Debug.Log("[InputManager] Using default bindings");
        }
    }
}

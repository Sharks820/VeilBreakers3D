using System;
using System.Collections.Generic;
using System.Linq;
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
            { GameAction.Confirm, new[] { KeyCode.Return, KeyCode.Space, KeyCode.E } },
            { GameAction.Cancel, new[] { KeyCode.Escape, KeyCode.Backspace } },
            { GameAction.OpenMenu, new[] { KeyCode.Tab, KeyCode.Q } },
            { GameAction.MoveUp, new[] { KeyCode.W, KeyCode.UpArrow } },
            { GameAction.MoveDown, new[] { KeyCode.S, KeyCode.DownArrow } },
            { GameAction.MoveLeft, new[] { KeyCode.A, KeyCode.LeftArrow } },
            { GameAction.MoveRight, new[] { KeyCode.D, KeyCode.RightArrow } },
            { GameAction.TargetNext, new[] { KeyCode.Tab } },
            { GameAction.TargetPrevious, new[] { KeyCode.Tab } },
            { GameAction.CycleEnemy, new[] { KeyCode.E } },
            { GameAction.CycleAlly, new[] { KeyCode.Q } },
            { GameAction.Skill1, new[] { KeyCode.Alpha1 } },
            { GameAction.Skill2, new[] { KeyCode.Alpha2 } },
            { GameAction.Skill3, new[] { KeyCode.Alpha3 } },
            { GameAction.Skill4, new[] { KeyCode.Alpha4 } },
            { GameAction.Ultimate, new[] { KeyCode.R } },
            { GameAction.MarkForCapture, new[] { KeyCode.C } },
            { GameAction.CancelMark, new[] { KeyCode.X } },
            { GameAction.CaptureAction, new[] { KeyCode.Space, KeyCode.Return } },
            { GameAction.Ally1, new[] { KeyCode.F1 } },
            { GameAction.Ally2, new[] { KeyCode.F2 } },
            { GameAction.Ally3, new[] { KeyCode.F3 } },
            { GameAction.AdvanceDialogue, new[] { KeyCode.Space, KeyCode.Return } },
            { GameAction.SkipDialogue, new[] { KeyCode.Space } },
            { GameAction.ShiftModifier, new[] { KeyCode.LeftShift, KeyCode.RightShift } }
        };

        // =============================================================================
        // STATE
        // =============================================================================

        private Dictionary<GameAction, KeyCode[]> _currentBindings;
        private bool _inputEnabled = true;

        // Caches for input state per frame
        private readonly Dictionary<GameAction, bool> _actionDownCache = new Dictionary<GameAction, bool>();
        private readonly GameAction[] _allGameActions;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public Vector2 MousePosition => Input.mousePosition;
        public bool InputEnabled { get => _inputEnabled; set => _inputEnabled = value; }

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<GameAction> OnActionTriggered;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================
        
        public InputManager()
        {
            _allGameActions = (GameAction[])Enum.GetValues(typeof(GameAction));
        }

        protected override void OnSingletonAwake()
        {
            _currentBindings = new Dictionary<GameAction, KeyCode[]>(_defaultBindings);
            LoadBindings();
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                _actionDownCache.Clear();
                return;
            }

            _actionDownCache.Clear();
            foreach (var action in _allGameActions)
            {
                if (!_currentBindings.TryGetValue(action, out var keys)) continue;

                if (keys.Any(key => Input.GetKeyDown(key)))
                {
                    _actionDownCache[action] = true;
                    OnActionTriggered?.Invoke(action);
                }
            }
        }

        // =============================================================================
        // PUBLIC API - ACTION QUERIES
        // =============================================================================

        public bool GetActionDown(GameAction action)
        {
            return _actionDownCache.TryGetValue(action, out bool isDown) && isDown;
        }

        public bool GetAction(GameAction action)
        {
            if (!_inputEnabled || !_currentBindings.TryGetValue(action, out var keys)) return false;
            return keys.Any(key => Input.GetKey(key));
        }

        public bool GetActionUp(GameAction action)
        {
            if (!_inputEnabled || !_currentBindings.TryGetValue(action, out var keys)) return false;
            return keys.Any(key => Input.GetKeyUp(key));
        }

        // =============================================================================
        // PUBLIC API - MOUSE
        // =============================================================================

        public bool GetMouseButtonDown(int button)
        {
            return _inputEnabled && Input.GetMouseButtonDown(button);
        }

        public bool GetMouseButton(int button)
        {
            return _inputEnabled && Input.GetMouseButton(button);
        }

        public bool GetMouseButtonUp(int button)
        {
            return _inputEnabled && Input.GetMouseButtonUp(button);
        }

        public Ray GetMouseRay(Camera camera)
        {
            return camera.ScreenPointToRay(MousePosition);
        }
        
        // =============================================================================
        // PUBLIC API - BINDING MANAGEMENT
        // =============================================================================

        public void SetBinding(GameAction action, params KeyCode[] keys)
        {
            _currentBindings[action] = keys;
            SaveBindings();
        }

        public KeyCode[] GetBinding(GameAction action)
        {
            return _currentBindings.TryGetValue(action, out var keys) ? keys : Array.Empty<KeyCode>();
        }

        public void ResetBindingsToDefault()
        {
            _currentBindings = new Dictionary<GameAction, KeyCode[]>(_defaultBindings);
            SaveBindings();
        }

        public string GetBindingDisplayName(GameAction action)
        {
            if (!_currentBindings.TryGetValue(action, out var keys) || keys.Length == 0)
                return "???";
            return GetKeyDisplayName(keys[0]);
        }
        
        // =============================================================================
        // SERIALIZATION & HELPERS
        // =============================================================================

        [Serializable] private class Binding { public GameAction action; public KeyCode[] keys; }
        [Serializable] private class BindingsWrapper { public List<Binding> bindings = new List<Binding>(); }
        private const string KEY_BINDINGS = "Input_Bindings_JSON";

        private void SaveBindings()
        {
            var wrapper = new BindingsWrapper();
            foreach (var pair in _currentBindings)
            {
                wrapper.bindings.Add(new Binding { action = pair.Key, keys = pair.Value });
            }
            string json = JsonUtility.ToJson(wrapper, true);
            PlayerPrefs.SetString(KEY_BINDINGS, json);
            PlayerPrefs.Save();
            Debug.Log("[InputManager] Bindings saved.");
        }

        private void LoadBindings()
        {
            if (!PlayerPrefs.HasKey(KEY_BINDINGS))
            {
                Debug.Log("[InputManager] No saved bindings found, using defaults.");
                return;
            }

            try
            {
                string json = PlayerPrefs.GetString(KEY_BINDINGS);
                var wrapper = JsonUtility.FromJson<BindingsWrapper>(json);
                _currentBindings.Clear();
                foreach (var binding in wrapper.bindings)
                {
                    _currentBindings[binding.action] = binding.keys;
                }
                Debug.Log("[InputManager] Custom bindings loaded.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputManager] Failed to load bindings: {ex.Message}. Using defaults.");
                _currentBindings = new Dictionary<GameAction, KeyCode[]>(_defaultBindings);
            }
        }
        
        private string GetKeyDisplayName(KeyCode key)
        {
            return key switch {
                KeyCode.Alpha0 => "0", KeyCode.Alpha1 => "1", KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3", KeyCode.Alpha4 => "4", KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6", KeyCode.Alpha7 => "7", KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9", KeyCode.Return => "Enter", KeyCode.Escape => "Esc",
                KeyCode.LeftShift => "Shift", KeyCode.RightShift => "Shift",
                KeyCode.LeftControl => "Ctrl", KeyCode.RightControl => "Ctrl",
                KeyCode.LeftAlt => "Alt", KeyCode.RightAlt => "Alt",
                KeyCode.UpArrow => "Up", KeyCode.DownArrow => "Down",
                KeyCode.LeftArrow => "Left", KeyCode.RightArrow => "Right",
                KeyCode.Space => "Space", KeyCode.Tab => "Tab",
                KeyCode.Backspace => "Back", _ => key.ToString()
            };
        }
    }
}

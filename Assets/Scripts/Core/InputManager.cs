using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Centralized input handling for VeilBreakers.
    /// Manages the new Unity Input System and provides a bridge for legacy systems.
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        // =============================================================================
        // INPUT ACTIONS (logical names)
        // =============================================================================

        public enum GameAction
        {
            // System & UI
            Confirm,
            Cancel,
            Pause,
            OpenMenu,

            // Navigation
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,

            // Combat - Targeting
            TargetNext,
            TargetPrev,
            CycleAlly,

            // Combat - Skills
            BasicAttack,
            Defend,
            Skill1,
            Skill2,
            Skill3,
            Skill4,
            Ultimate,

            // Combat - Capture
            Mark,
            Capture,

            // Ally hotkeys
            Ally1,
            Ally2,
            Ally3,

            // Dialogue
            DialogueAdvance,
            DialogueSkip
        }

        // =============================================================================
        // STATE
        // =============================================================================

        private VeilBreakersInputActions _inputActions;
        private bool _isGamepad = false;
        private bool _inputEnabled = true;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public bool IsGamepad => _isGamepad;
        public bool InputEnabled { get => _inputEnabled; set => ToggleInput(value); }
        public Vector2 MousePosition => _inputActions.UI.Point.ReadValue<Vector2>();

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<bool> OnInputDeviceChanged;
        public event Action<GameAction> OnActionTriggered;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            _inputActions = new VeilBreakersInputActions();
            _inputActions.Enable();

            // Track input device changes
            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDestroy()
        {
            InputSystem.onActionChange -= OnActionChange;
            _inputActions?.Dispose();
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed && obj is InputAction action)
            {
                var device = action.activeControl?.device;
                bool wasGamepad = _isGamepad;
                _isGamepad = device is Gamepad;

                if (wasGamepad != _isGamepad)
                {
                    OnInputDeviceChanged?.Invoke(_isGamepad);
                }
            }
        }

        private void ToggleInput(bool enabled)
        {
            _inputEnabled = enabled;
            if (enabled) _inputActions.Enable();
            else _inputActions.Disable();
        }

        // =============================================================================
        // PUBLIC API - ACTION QUERIES
        // =============================================================================

        public bool GetActionDown(GameAction action)
        {
            if (!_inputEnabled) return false;

            bool triggered = action switch
            {
                GameAction.Confirm => _inputActions.Gameplay.Confirm.WasPressedThisFrame(),
                GameAction.Cancel => _inputActions.Gameplay.Cancel.WasPressedThisFrame(),
                GameAction.Pause => _inputActions.Gameplay.Pause.WasPressedThisFrame(),
                GameAction.OpenMenu => _inputActions.Gameplay.OpenMenu.WasPressedThisFrame(),
                
                GameAction.MoveUp => _inputActions.UI.Navigate.ReadValue<Vector2>().y > 0.5f && _inputActions.UI.Navigate.WasPerformedThisFrame(),
                GameAction.MoveDown => _inputActions.UI.Navigate.ReadValue<Vector2>().y < -0.5f && _inputActions.UI.Navigate.WasPerformedThisFrame(),
                GameAction.MoveLeft => _inputActions.UI.Navigate.ReadValue<Vector2>().x < -0.5f && _inputActions.UI.Navigate.WasPerformedThisFrame(),
                GameAction.MoveRight => _inputActions.UI.Navigate.ReadValue<Vector2>().x > 0.5f && _inputActions.UI.Navigate.WasPerformedThisFrame(),

                GameAction.TargetNext => _inputActions.Gameplay.TargetNext.WasPressedThisFrame(),
                GameAction.TargetPrev => _inputActions.Gameplay.TargetPrev.WasPressedThisFrame(),
                GameAction.CycleAlly => _inputActions.Gameplay.CycleAlly.WasPressedThisFrame(),

                GameAction.BasicAttack => _inputActions.Gameplay.BasicAttack.WasPressedThisFrame(),
                GameAction.Defend => _inputActions.Gameplay.Defend.WasPressedThisFrame(),
                GameAction.Skill1 => _inputActions.Gameplay.Skill1.WasPressedThisFrame(),
                GameAction.Skill2 => _inputActions.Gameplay.Skill2.WasPressedThisFrame(),
                GameAction.Skill3 => _inputActions.Gameplay.Skill3.WasPressedThisFrame(),
                GameAction.Skill4 => _inputActions.Gameplay.Skill4.WasPressedThisFrame(),
                GameAction.Ultimate => _inputActions.Gameplay.Ultimate.WasPressedThisFrame(),

                GameAction.Mark => _inputActions.Gameplay.Mark.WasPressedThisFrame(),
                GameAction.Capture => _inputActions.Gameplay.Capture.WasPressedThisFrame(),

                GameAction.Ally1 => _inputActions.Gameplay.Ally1.WasPressedThisFrame(),
                GameAction.Ally2 => _inputActions.Gameplay.Ally2.WasPressedThisFrame(),
                GameAction.Ally3 => _inputActions.Gameplay.Ally3.WasPressedThisFrame(),

                GameAction.DialogueAdvance => _inputActions.Gameplay.DialogueAdvance.WasPressedThisFrame(),
                GameAction.DialogueSkip => _inputActions.Gameplay.DialogueSkip.WasPressedThisFrame(),
                
                _ => false
            };

            if (triggered) OnActionTriggered?.Invoke(action);
            return triggered;
        }

        public bool GetAction(GameAction action)
        {
            if (!_inputEnabled) return false;

            return action switch
            {
                GameAction.Confirm => _inputActions.Gameplay.Confirm.IsPressed(),
                GameAction.Cancel => _inputActions.Gameplay.Cancel.IsPressed(),
                GameAction.DialogueSkip => _inputActions.Gameplay.DialogueSkip.IsPressed(),
                GameAction.DialogueAdvance => _inputActions.Gameplay.DialogueAdvance.IsPressed(),
                _ => false
            };
        }

        public bool GetActionUp(GameAction action)
        {
            if (!_inputEnabled) return false;

            return action switch
            {
                GameAction.Confirm => _inputActions.Gameplay.Confirm.WasReleasedThisFrame(),
                GameAction.Cancel => _inputActions.Gameplay.Cancel.WasReleasedThisFrame(),
                _ => false
            };
        }

        // =============================================================================
        // PUBLIC API - MOUSE
        // =============================================================================

        public bool GetMouseButtonDown(int button)
        {
            if (!_inputEnabled) return false;
            return button switch
            {
                0 => _inputActions.UI.Click.WasPressedThisFrame(),
                1 => _inputActions.UI.RightClick.WasPressedThisFrame(),
                2 => _inputActions.UI.MiddleClick.WasPressedThisFrame(),
                _ => false
            };
        }

        public bool GetMouseButton(int button)
        {
            if (!_inputEnabled) return false;
            return button switch
            {
                0 => _inputActions.UI.Click.IsPressed(),
                1 => _inputActions.UI.RightClick.IsPressed(),
                2 => _inputActions.UI.MiddleClick.IsPressed(),
                _ => false
            };
        }

        public Ray GetMouseRay(Camera camera)
        {
            return camera.ScreenPointToRay(MousePosition);
        }

        // =============================================================================
        // ACTION MAP MANAGEMENT
        // =============================================================================

        public void EnableGameplay() => _inputActions.Gameplay.Enable();
        public void DisableGameplay() => _inputActions.Gameplay.Disable();
        public void EnableUI() => _inputActions.UI.Enable();
        public void DisableUI() => _inputActions.UI.Disable();
    }
}

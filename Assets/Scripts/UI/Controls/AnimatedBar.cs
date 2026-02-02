using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Controls
{
    /// <summary>
    /// AAA animated stat bar with ghost damage/heal effect.
    /// Shows a trailing "ghost" bar that follows value changes with delay,
    /// creating a satisfying visual feedback effect.
    /// </summary>
    [UxmlElement]
    public partial class AnimatedBar : VisualElement
    {
        // USS class names
        private const string kBaseClass = "animated-bar";
        private const string kFillClass = "animated-bar__fill";
        private const string kGhostClass = "animated-bar__ghost";
        private const string kBackgroundClass = "animated-bar__background";

        // Child elements
        private readonly VisualElement _background;
        private readonly VisualElement _ghostBar;
        private readonly VisualElement _fillBar;

        // State
        private float _currentValue = 1f;
        private float _maxValue = 100f;
        private float _ghostValue = 1f;
        private IVisualElementScheduledItem _ghostSchedule;

        // =============================================================================
        // UXML ATTRIBUTES
        // =============================================================================

        [UxmlAttribute]
        public float Value
        {
            get => _currentValue;
            set => SetValue(value);
        }

        [UxmlAttribute]
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = Mathf.Max(1f, value);
                UpdateDisplay();
            }
        }

        [UxmlAttribute]
        public Color FillColor
        {
            get => _fillBar.resolvedStyle.backgroundColor;
            set => _fillBar.style.backgroundColor = value;
        }

        [UxmlAttribute]
        public Color GhostColor
        {
            get => _ghostBar.resolvedStyle.backgroundColor;
            set => _ghostBar.style.backgroundColor = value;
        }

        [UxmlAttribute]
        public float GhostDelay { get; set; } = 300f; // ms

        [UxmlAttribute]
        public BarType Type { get; set; } = BarType.Health;

        // =============================================================================
        // CONSTRUCTOR
        // =============================================================================

        public AnimatedBar()
        {
            AddToClassList(kBaseClass);

            // Background (shows empty portion)
            _background = new VisualElement();
            _background.AddToClassList(kBackgroundClass);
            Add(_background);

            // Ghost bar (trails behind fill during damage, leads during heal)
            _ghostBar = new VisualElement();
            _ghostBar.AddToClassList(kGhostClass);
            Add(_ghostBar);

            // Fill bar (current value)
            _fillBar = new VisualElement();
            _fillBar.AddToClassList(kFillClass);
            Add(_fillBar);

            // Set initial state
            _fillBar.style.width = Length.Percent(100f);
            _ghostBar.style.width = Length.Percent(100f);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set value with optional animation control.
        /// </summary>
        public void SetValue(float newValue, bool animate = true)
        {
            float previousValue = _currentValue;
            _currentValue = Mathf.Clamp(newValue, 0f, _maxValue);

            if (!animate)
            {
                // Immediate update without ghost effect
                float percent = (_currentValue / _maxValue) * 100f;
                _fillBar.style.width = Length.Percent(percent);
                _ghostBar.style.width = Length.Percent(percent);
                _ghostValue = _currentValue;
                return;
            }

            bool isDamage = _currentValue < previousValue;
            UpdateDisplay();

            // Cancel any pending ghost update
            _ghostSchedule?.Pause();

            if (isDamage)
            {
                // Damage: Fill drops immediately, ghost follows with delay
                _ghostSchedule = schedule.Execute(UpdateGhostToCurrent).ExecuteLater((long)GhostDelay);
            }
            else
            {
                // Heal: Ghost jumps immediately, fill follows via CSS transition
                _ghostValue = _currentValue;
                _ghostBar.style.width = Length.Percent((_ghostValue / _maxValue) * 100f);
            }
        }

        /// <summary>
        /// Flash the bar to draw attention (e.g., critical health).
        /// </summary>
        public void Flash()
        {
            AddToClassList("animated-bar--flash");
            schedule.Execute(() => RemoveFromClassList("animated-bar--flash")).ExecuteLater(300);
        }

        /// <summary>
        /// Set bar type which applies appropriate styling.
        /// </summary>
        public void SetType(BarType type)
        {
            // Remove existing type classes
            RemoveFromClassList("animated-bar--health");
            RemoveFromClassList("animated-bar--mana");
            RemoveFromClassList("animated-bar--guard");
            RemoveFromClassList("animated-bar--fury");
            RemoveFromClassList("animated-bar--corruption");

            Type = type;

            // Add new type class
            string typeClass = type switch
            {
                BarType.Health => "animated-bar--health",
                BarType.Mana => "animated-bar--mana",
                BarType.Guard => "animated-bar--guard",
                BarType.Fury => "animated-bar--fury",
                BarType.Corruption => "animated-bar--corruption",
                _ => "animated-bar--health"
            };
            AddToClassList(typeClass);
        }

        // =============================================================================
        // PRIVATE METHODS
        // =============================================================================

        private void UpdateDisplay()
        {
            float percent = (_currentValue / _maxValue) * 100f;
            _fillBar.style.width = Length.Percent(percent);
        }

        private void UpdateGhostToCurrent()
        {
            _ghostValue = _currentValue;
            float percent = (_ghostValue / _maxValue) * 100f;
            _ghostBar.style.width = Length.Percent(percent);
        }
    }

    /// <summary>
    /// Bar type for automatic styling.
    /// </summary>
    public enum BarType
    {
        Health,
        Mana,
        Guard,
        Fury,
        Corruption,
        Experience
    }
}

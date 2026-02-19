using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrates visual transition sequences during hero switching.
    /// Manages panel slide-in/out timing and USS class toggling.
    /// </summary>
    public class TransitionController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _heroInfoPanel;
        private VisualElement _statsPanel;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _heroInfoPanel = root.Q<VisualElement>("hero-info-panel");
            _statsPanel = root.Q<VisualElement>("stats-panel");

            // Set usage hints for animated panels
            if (_heroInfoPanel != null) _heroInfoPanel.usageHints = UsageHints.DynamicTransform;
            if (_statsPanel != null) _statsPanel.usageHints = UsageHints.DynamicTransform;
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            // Panels slide out then back in
            // The individual panel controllers handle their own slide-in
            // This controller coordinates the timing if needed

            // Currently, each panel controller does its own animate.
            // This controller is reserved for future veil tear effects
            // and more complex multi-element sequencing.
        }
    }
}

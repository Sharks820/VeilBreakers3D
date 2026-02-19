using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages background gradients and ambient atmosphere.
    /// Changes background tint based on hero's theme colors.
    /// </summary>
    public class CharSelectEnvironmentController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _backgroundGradient;

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
            _backgroundGradient = root.Q<VisualElement>("background-gradient");

            if (_backgroundGradient != null)
            {
                _backgroundGradient.usageHints = UsageHints.DynamicColor;
            }
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (config == null || _backgroundGradient == null) return;

            // Dark tinted background based on hero's secondary color
            Color bgColor = config.secondaryColor;
            bgColor.r *= 0.15f;
            bgColor.g *= 0.15f;
            bgColor.b *= 0.15f;
            bgColor.a = 1f;

            _backgroundGradient.style.backgroundColor = bgColor;
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Centralized reference holder for UI assets.
    /// Eliminates Resources.Load calls by using direct references.
    /// Mark this asset as Addressable with key "UIAssets" for async loading.
    /// </summary>
    [CreateAssetMenu(fileName = "UIAssets", menuName = "VeilBreakers/UI/UIAssets")]
    public class UIAssets : ScriptableObject
    {
        private static UIAssets _instance;
        private static bool _triedResourcesLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; _triedResourcesLoad = false; }

        /// <summary>
        /// Singleton instance. Must be initialized via Initialize() before use.
        /// For Addressables, load this asset first then call Initialize().
        /// </summary>
        public static UIAssets Instance
        {
            get
            {
                if (_instance == null && !_triedResourcesLoad)
                {
                    _triedResourcesLoad = true;
                    // Fallback to Resources.Load for backwards compatibility (one attempt only)
                    _instance = Resources.Load<UIAssets>("UI/UIAssets");
                    if (_instance == null)
                    {
                        Debug.LogError("[UIAssets] UIAssets not found! Create via Assets > Create > VeilBreakers > UI > UIAssets");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize the singleton with a loaded instance.
        /// Call this after loading via Addressables.
        /// </summary>
        public static void Initialize(UIAssets instance)
        {
            _instance = instance;
            _triedResourcesLoad = instance != null;
        }

        // =============================================================================
        // PANEL SETTINGS
        // =============================================================================

        [Header("Panel Settings")]
        [SerializeField] private PanelSettings _defaultPanelSettings;
        public PanelSettings DefaultPanelSettings => _defaultPanelSettings;

        // =============================================================================
        // STYLE SHEETS
        // =============================================================================

        [Header("Style Sheets")]
        [SerializeField] private StyleSheet _themeStyle;
        [SerializeField] private StyleSheet _componentStyle;

        public StyleSheet ThemeStyle => _themeStyle;
        public StyleSheet ComponentStyle => _componentStyle;

        // =============================================================================
        // MENU TEMPLATES
        // =============================================================================

        [Header("Menu Templates")]
        [SerializeField] private VisualTreeAsset _mainMenuTemplate;
        [SerializeField] private VisualTreeAsset _characterSelectTemplate;
        [SerializeField] private VisualTreeAsset _settingsTemplate;
        [SerializeField] private VisualTreeAsset _inventoryTemplate;
        [SerializeField] private VisualTreeAsset _collectionTemplate;

        public VisualTreeAsset MainMenuTemplate => _mainMenuTemplate;
        public VisualTreeAsset CharacterSelectTemplate => _characterSelectTemplate;
        public VisualTreeAsset SettingsTemplate => _settingsTemplate;
        public VisualTreeAsset InventoryTemplate => _inventoryTemplate;
        public VisualTreeAsset CollectionTemplate => _collectionTemplate;

        // =============================================================================
        // COMBAT UI TEMPLATES
        // =============================================================================

        [Header("Combat UI Templates")]
        [SerializeField] private VisualTreeAsset _combatHUDTemplate;
        [SerializeField] private VisualTreeAsset _dialogueTemplate;

        public VisualTreeAsset CombatHUDTemplate => _combatHUDTemplate;
        public VisualTreeAsset DialogueTemplate => _dialogueTemplate;

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Get a template by menu name (for UIAutoSetup compatibility).
        /// </summary>
        public VisualTreeAsset GetTemplate(string menuName)
        {
            return menuName switch
            {
                "MainMenu" => _mainMenuTemplate,
                "CharacterSelect" => _characterSelectTemplate,
                "Settings" => _settingsTemplate,
                "Inventory" => _inventoryTemplate,
                "Collection" => _collectionTemplate,
                "CombatHUD" => _combatHUDTemplate,
                "Dialogue" => _dialogueTemplate,
                _ => null
            };
        }

        /// <summary>
        /// Apply standard styles to a UI root.
        /// </summary>
        public void ApplyStandardStyles(VisualElement root)
        {
            if (root == null) return;

            if (_themeStyle != null)
                root.styleSheets.Add(_themeStyle);

            if (_componentStyle != null)
                root.styleSheets.Add(_componentStyle);
        }
    }
}

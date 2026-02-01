using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Auto-setup for UI scenes. Automatically loads and assigns UI assets at runtime.
    /// Add this to any scene and it will set up the UI system automatically.
    /// Migrated to use UIAssets for centralized asset references (no more Resources.Load).
    /// </summary>
    public class UIAutoSetup : MonoBehaviour
    {
        [Header("Auto-Load Settings")]
        [SerializeField] private bool _autoLoadAssets = true;
        [SerializeField] private string _menuToLoad = "MainMenu";

        [Header("Manual Overrides (Optional)")]
        [SerializeField] private VisualTreeAsset _manualTemplate;
        [SerializeField] private PanelSettings _manualPanelSettings;

        private UIDocument _uiDocument;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // Get or create UIDocument
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Get UIAssets singleton
            var uiAssets = UIAssets.Instance;

            // Load panel settings
            PanelSettings panelSettings = _manualPanelSettings;
            if (panelSettings == null && uiAssets != null)
            {
                panelSettings = uiAssets.DefaultPanelSettings;
            }
            if (panelSettings == null)
            {
                // Create default panel settings at runtime
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                Debug.LogWarning("[UIAutoSetup] Using runtime-created PanelSettings. For better performance, assign one in Inspector or configure UIAssets.");
            }
            _uiDocument.panelSettings = panelSettings;

            // Load template
            VisualTreeAsset template = _manualTemplate;
            if (template == null && _autoLoadAssets && uiAssets != null)
            {
                template = uiAssets.GetTemplate(_menuToLoad);
                if (template == null)
                {
                    Debug.LogError($"[UIAutoSetup] Template '{_menuToLoad}' not found in UIAssets");
                    CreateFallbackUI();
                    return;
                }
            }

            if (template != null)
            {
                _uiDocument.visualTreeAsset = template;
            }

            // Apply styles from UIAssets
            if (uiAssets != null)
            {
                uiAssets.ApplyStandardStyles(_uiDocument.rootVisualElement);
            }

            Debug.Log($"[UIAutoSetup] UI initialized: {_menuToLoad}");
        }

        private void CreateFallbackUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.style.backgroundColor = new Color(0.05f, 0.03f, 0.08f);
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            root.Add(container);

            var title = new Label("VEILBREAKERS");
            title.style.fontSize = 48;
            title.style.color = new Color(0.9f, 0.85f, 0.8f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(title);

            var error = new Label($"Could not load UI template: {_menuToLoad}");
            error.style.fontSize = 16;
            error.style.color = new Color(0.8f, 0.4f, 0.4f);
            error.style.marginTop = 20;
            container.Add(error);

            var hint = new Label("Check that template is assigned in UIAssets ScriptableObject");
            hint.style.fontSize = 14;
            hint.style.color = new Color(0.6f, 0.6f, 0.6f);
            hint.style.marginTop = 10;
            container.Add(hint);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.UI.Menus;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Auto-bootstraps the UI system when a menu scene loads.
    /// Uses RuntimeInitializeOnLoadMethod to work without scene references.
    /// Migrated to use UIAssets for centralized asset references (no more Resources.Load).
    /// </summary>
    public static class MenuBootstrap
    {
        private const string kMainMenuScene = "MainMenu";
        private const string kCharacterSelectScene = "CharacterSelect";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // Subscribe to scene changes for the application lifetime
            SceneManager.sceneLoaded += HandleSceneLoaded;

            // Clean up subscription when application quits (prevents editor warnings)
            Application.quitting += OnApplicationQuitting;

            // Also handle the current scene if it's a menu
            SetupCurrentScene();
        }

        private static void OnApplicationQuitting()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Application.quitting -= OnApplicationQuitting;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetupCurrentScene();
        }

        private static void SetupCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            switch (sceneName)
            {
                case kMainMenuScene:
                    SetupMainMenu();
                    break;
                case kCharacterSelectScene:
                    SetupCharacterSelect();
                    break;
            }
        }

        private static void SetupMainMenu()
        {
            // Check if UI is already set up
            if (Object.FindFirstObjectByType<UIDocument>() != null)
            {
                Debug.Log("[MenuBootstrap] UIDocument already exists in MainMenu");
                return;
            }

            Debug.Log("[MenuBootstrap] Setting up MainMenu UI...");

            var uiAssets = UIAssets.Instance;
            if (uiAssets == null)
            {
                Debug.LogError("[MenuBootstrap] UIAssets not found! Create via Assets > Create > VeilBreakers > UI > UIAssets");
                return;
            }

            // Create UI Manager object
            var uiManagerObj = new GameObject("UIManager");

            // Add UIDocument
            var uiDocument = uiManagerObj.AddComponent<UIDocument>();

            // Use centralized panel settings
            var panelSettings = uiAssets.DefaultPanelSettings;
            if (panelSettings == null)
            {
                // Create default panel settings as fallback
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                Debug.LogWarning("[MenuBootstrap] Created runtime PanelSettings - assign DefaultPanelSettings in UIAssets");
            }
            uiDocument.panelSettings = panelSettings;

            // Use centralized template
            var template = uiAssets.MainMenuTemplate;
            if (template != null)
            {
                uiDocument.visualTreeAsset = template;
            }
            else
            {
                Debug.LogError("[MenuBootstrap] MainMenuTemplate not assigned in UIAssets!");
                CreateFallbackMainMenu(uiDocument);
                return;
            }

            // Apply standard styles from UIAssets
            uiAssets.ApplyStandardStyles(uiDocument.rootVisualElement);

            // Add controller
            uiManagerObj.AddComponent<MainMenuController>();

            Debug.Log("[MenuBootstrap] MainMenu UI setup complete");
        }

        private static void SetupCharacterSelect()
        {
            if (Object.FindFirstObjectByType<UIDocument>() != null)
            {
                Debug.Log("[MenuBootstrap] UIDocument already exists in CharacterSelect");
                return;
            }

            Debug.Log("[MenuBootstrap] Setting up CharacterSelect UI...");

            var uiAssets = UIAssets.Instance;
            if (uiAssets == null)
            {
                Debug.LogError("[MenuBootstrap] UIAssets not found!");
                return;
            }

            var uiManagerObj = new GameObject("UIManager");
            var uiDocument = uiManagerObj.AddComponent<UIDocument>();

            var panelSettings = uiAssets.DefaultPanelSettings;
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            }
            uiDocument.panelSettings = panelSettings;

            var template = uiAssets.CharacterSelectTemplate;
            if (template != null)
            {
                uiDocument.visualTreeAsset = template;
            }
            else
            {
                Debug.LogError("[MenuBootstrap] CharacterSelectTemplate not assigned in UIAssets!");
                return;
            }

            // Apply standard styles
            uiAssets.ApplyStandardStyles(uiDocument.rootVisualElement);

            uiManagerObj.AddComponent<VeilBreakers.UI.CharacterSelect.CharacterSelectManager>();

            Debug.Log("[MenuBootstrap] CharacterSelect UI setup complete");
        }

        private static void CreateFallbackMainMenu(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;
            root.style.backgroundColor = new Color(0.05f, 0.03f, 0.08f);
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            root.Add(container);

            var title = new Label("VEILBREAKERS");
            title.style.fontSize = 64;
            title.style.color = new Color(0.9f, 0.85f, 0.8f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 40;
            container.Add(title);

            var subtitle = new Label("The Veil Awaits...");
            subtitle.style.fontSize = 24;
            subtitle.style.color = new Color(0.6f, 0.5f, 0.7f);
            subtitle.style.marginBottom = 60;
            container.Add(subtitle);

            // Create buttons
            CreateMenuButton(container, "NEW GAME", () => Debug.Log("New Game clicked"));
            CreateMenuButton(container, "CONTINUE", () => Debug.Log("Continue clicked"));
            CreateMenuButton(container, "SETTINGS", () => Debug.Log("Settings clicked"));
            CreateMenuButton(container, "QUIT", () => Application.Quit());

            var version = new Label("v2.00 - UI Test Build");
            version.style.fontSize = 14;
            version.style.color = new Color(0.4f, 0.4f, 0.4f);
            version.style.marginTop = 60;
            container.Add(version);
        }

        private static void CreateMenuButton(VisualElement parent, string text, System.Action onClick)
        {
            var button = new Button(onClick);
            button.text = text;
            button.style.width = 300;
            button.style.height = 50;
            button.style.fontSize = 20;
            button.style.marginTop = 10;
            button.style.marginBottom = 10;
            button.style.backgroundColor = new Color(0.15f, 0.12f, 0.2f);
            button.style.color = new Color(0.9f, 0.85f, 0.8f);
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopColor = new Color(0.4f, 0.3f, 0.5f);
            button.style.borderBottomColor = new Color(0.4f, 0.3f, 0.5f);
            button.style.borderLeftColor = new Color(0.4f, 0.3f, 0.5f);
            button.style.borderRightColor = new Color(0.4f, 0.3f, 0.5f);
            parent.Add(button);
        }
    }
}

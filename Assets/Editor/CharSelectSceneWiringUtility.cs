using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;
using VeilBreakers.UI.CharacterSelect;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// One-shot utility to wire the Character Select scene with all controllers
    /// and create HeroDisplayConfig ScriptableObject assets.
    /// Run via menu: VeilBreakers > Character Select > Wire Scene.
    /// Safe to re-run: skips existing assets, replaces existing GameObjects.
    /// </summary>
    public static class CharSelectSceneWiringUtility
    {
        private const string kConfigFolder = "Assets/Resources/CharacterSelect/HeroDisplayConfigs";
        private const string kUxmlPath = "Assets/UI/Screens/CharacterSelect.uxml";
        private const string kUssPath = "Assets/UI/Styles/CharacterSelect.uss";
        private const string kPanelSettingsPath = "Assets/UI/VeilBreakersPanelSettings.asset";
        private const string kSystemObjectName = "CharacterSelectSystem";

        // =====================================================================
        // Menu Items
        // =====================================================================

        [MenuItem("VeilBreakers/Character Select/Wire Scene (Full Setup)", false, 100)]
        public static void WireScene()
        {
            CreateHeroDisplayConfigs();
            CreateSystemGameObject();
            Debug.Log("[CharSelect] Scene wiring complete. Save the scene.");
        }

        [MenuItem("VeilBreakers/Character Select/Create HeroDisplayConfigs Only", false, 101)]
        public static void CreateHeroDisplayConfigsOnly()
        {
            CreateHeroDisplayConfigs();
        }

        [MenuItem("VeilBreakers/Character Select/Create System GameObject Only", false, 102)]
        public static void CreateSystemGameObjectOnly()
        {
            CreateSystemGameObject();
        }

        // =====================================================================
        // HeroDisplayConfig Creation
        // =====================================================================

        private static void CreateHeroDisplayConfigs()
        {
            EnsureFolderExists(kConfigFolder);

            // Vex: IRONBOUND | Deep crimson | Ember orange | Warm gold
            CreateConfigIfMissing("VexDisplayConfig", "vex",
                primaryColor:   HexColor("C41E3A"),  // Deep crimson
                secondaryColor: HexColor("D4622A"),  // Ember orange
                accentColor:    HexColor("D4A843"),  // Warm gold
                particleColor:  HexColor("C41E3A"),
                keyLightColor:  new Color(1f, 0.85f, 0.75f),
                rimLightColor:  HexColor("D4A843"),
                cameraOffset:   new Vector3(0f, 1.2f, -3f)
            );

            // Seraphina: FANGBORN | Toxic green | Swamp purple | Sickly yellow
            CreateConfigIfMissing("SeraphinaDisplayConfig", "seraphina",
                primaryColor:   HexColor("3CB043"),  // Toxic green
                secondaryColor: HexColor("6B3FA0"),  // Swamp purple
                accentColor:    HexColor("C5B93C"),  // Sickly yellow
                particleColor:  HexColor("3CB043"),
                keyLightColor:  new Color(0.8f, 1f, 0.8f),
                rimLightColor:  HexColor("3CB043"),
                cameraOffset:   new Vector3(0f, 1.2f, -3f)
            );

            // Orion: VOIDTOUCHED | Deep indigo | Cosmic blue | Electric cyan
            CreateConfigIfMissing("OrionDisplayConfig", "orion",
                primaryColor:   HexColor("3F00A0"),  // Deep indigo
                secondaryColor: HexColor("1E3A8A"),  // Cosmic blue
                accentColor:    HexColor("00E5FF"),  // Electric cyan
                particleColor:  HexColor("3F00A0"),
                keyLightColor:  new Color(0.75f, 0.8f, 1f),
                rimLightColor:  HexColor("00E5FF"),
                cameraOffset:   new Vector3(0f, 1.2f, -3f)
            );

            // Nyx: UNCHAINED | Blood red | Shadow black | Violet
            CreateConfigIfMissing("NyxDisplayConfig", "nyx",
                primaryColor:   HexColor("8B0000"),  // Blood red
                secondaryColor: HexColor("1A1A1A"),  // Shadow black
                accentColor:    HexColor("8A2BE2"),  // Violet
                particleColor:  HexColor("8B0000"),
                keyLightColor:  new Color(0.9f, 0.8f, 0.85f),
                rimLightColor:  HexColor("8A2BE2"),
                cameraOffset:   new Vector3(0f, 1.2f, -3f)
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharSelect] HeroDisplayConfig assets created/verified.");
        }

        private static void CreateConfigIfMissing(
            string assetName, string heroId,
            Color primaryColor, Color secondaryColor, Color accentColor,
            Color particleColor, Color keyLightColor, Color rimLightColor,
            Vector3 cameraOffset)
        {
            string path = $"{kConfigFolder}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<HeroDisplayConfig>(path) != null)
            {
                Debug.Log($"[CharSelect] Config already exists: {path}");
                return;
            }

            var config = ScriptableObject.CreateInstance<HeroDisplayConfig>();
            config.heroId = heroId;
            config.primaryColor = primaryColor;
            config.secondaryColor = secondaryColor;
            config.accentColor = accentColor;
            config.particleColor = particleColor;
            config.keyLightColor = keyLightColor;
            config.rimLightColor = rimLightColor;
            config.cameraOffset = cameraOffset;

            // Defaults from the ScriptableObject handle the rest
            // (cameraFOV=30, fillLightColor, keyLightIntensity, etc.)

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[CharSelect] Created: {path}");
        }

        // =====================================================================
        // System GameObject Creation
        // =====================================================================

        private static void CreateSystemGameObject()
        {
            // Remove existing system object if present (safe re-run)
            var existing = GameObject.Find(kSystemObjectName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[CharSelect] Removed existing CharacterSelectSystem.");
            }

            // Create root object
            var systemGO = new GameObject(kSystemObjectName);
            Undo.RegisterCreatedObjectUndo(systemGO, "Create CharacterSelectSystem");

            // Add UIDocument first (shared by all controllers)
            var uiDoc = systemGO.AddComponent<UIDocument>();

            // Assign UXML source asset
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kUxmlPath);
            if (uxmlAsset != null)
            {
                uiDoc.visualTreeAsset = uxmlAsset;
            }
            else
            {
                Debug.LogWarning($"[CharSelect] UXML not found at {kUxmlPath}");
            }

            // Assign PanelSettings
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(kPanelSettingsPath);
            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
            }
            else
            {
                Debug.LogWarning($"[CharSelect] PanelSettings not found at {kPanelSettingsPath}");
            }

            // Add controllers in dependency order
            var manager = systemGO.AddComponent<CharacterSelectManager>();
            systemGO.AddComponent<HeroStageController>();
            systemGO.AddComponent<HeroDataPanelController>();
            systemGO.AddComponent<HeroStatsPanelController>();
            var carousel = systemGO.AddComponent<CarouselController>();
            systemGO.AddComponent<CharSelectEnvironmentController>();
            systemGO.AddComponent<TransitionController>();

            // Wire UIDocument references via SerializedObject
            WireUIDocumentReferences(systemGO, uiDoc);

            // Wire CarouselController._manager reference
            WireCarouselManager(carousel, manager);

            // Wire HeroDisplayConfig array on manager
            WireHeroConfigs(manager);

            Debug.Log("[CharSelect] CharacterSelectSystem created with all controllers.");
        }

        private static void WireUIDocumentReferences(GameObject go, UIDocument uiDoc)
        {
            // All controllers have a serialized _uiDocument field
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                var so = new SerializedObject(comp);
                var prop = so.FindProperty("_uiDocument");
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    prop.objectReferenceValue = uiDoc;
                    so.ApplyModifiedProperties();
                }
            }
            Debug.Log("[CharSelect] UIDocument wired to all controllers.");
        }

        private static void WireCarouselManager(CarouselController carousel, CharacterSelectManager manager)
        {
            var so = new SerializedObject(carousel);
            var prop = so.FindProperty("_manager");
            if (prop != null)
            {
                prop.objectReferenceValue = manager;
                so.ApplyModifiedProperties();
            }
            Debug.Log("[CharSelect] CarouselController._manager wired.");
        }

        private static void WireHeroConfigs(CharacterSelectManager manager)
        {
            string[] configNames = {
                "NyxDisplayConfig",
                "OrionDisplayConfig",
                "SeraphinaDisplayConfig",
                "VexDisplayConfig"
            };

            var so = new SerializedObject(manager);
            var prop = so.FindProperty("_heroConfigs");
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning("[CharSelect] _heroConfigs property not found on manager.");
                return;
            }

            prop.arraySize = configNames.Length;
            for (int i = 0; i < configNames.Length; i++)
            {
                string path = $"{kConfigFolder}/{configNames[i]}.asset";
                var config = AssetDatabase.LoadAssetAtPath<HeroDisplayConfig>(path);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = config;

                if (config == null)
                {
                    Debug.LogWarning($"[CharSelect] Config not found: {path}");
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log("[CharSelect] HeroDisplayConfigs wired to manager.");
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color color);
            color.a = 1f;
            return color;
        }

        private static void EnsureFolderExists(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}

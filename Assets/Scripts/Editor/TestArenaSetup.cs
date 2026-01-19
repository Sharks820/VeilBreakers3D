#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VeilBreakers.Core;
using VeilBreakers.Test;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Editor utility to create and configure the Test Arena scene.
    /// </summary>
    public static class TestArenaSetup
    {
        [MenuItem("VeilBreakers/Create Test Arena Scene", false, 100)]
        public static void CreateTestArenaScene()
        {
            // Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default objects except camera and light
            var defaultCam = GameObject.Find("Main Camera");
            var defaultLight = GameObject.Find("Directional Light");

            // Setup camera
            if (defaultCam != null)
            {
                defaultCam.transform.position = new Vector3(0f, 10f, -15f);
                defaultCam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                var cam = defaultCam.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }

            // Setup directional light
            if (defaultLight != null)
            {
                defaultLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = defaultLight.GetComponent<Light>();
                light.intensity = 1.2f;
            }

            // Create GameBootstrap
            var bootstrap = new GameObject("[GameBootstrap]");
            bootstrap.AddComponent<GameBootstrap>();

            // Create TestArenaManager
            var arenaManager = new GameObject("[TestArenaManager]");
            arenaManager.AddComponent<TestArenaManager>();

            // Create arena floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(3f, 1f, 2f);

            // Create floor material
            var floorRenderer = floor.GetComponent<Renderer>();
            var floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.2f, 0.2f, 0.25f);
            floorRenderer.material = floorMat;

            // Create spawn point parent
            var spawnPoints = new GameObject("SpawnPoints");

            // Player spawn point
            var playerSpawn = new GameObject("PlayerSpawnPoint");
            playerSpawn.transform.parent = spawnPoints.transform;
            playerSpawn.transform.position = new Vector3(-5f, 0.5f, 0f);
            CreateSpawnPointVisual(playerSpawn, Color.blue);

            // Party spawn points
            for (int i = 0; i < 3; i++)
            {
                var partySpawn = new GameObject($"PartySpawnPoint_{i}");
                partySpawn.transform.parent = spawnPoints.transform;
                partySpawn.transform.position = new Vector3(-3f, 0.5f, -2f + i * 2f);
                CreateSpawnPointVisual(partySpawn, Color.cyan);
            }

            // Enemy spawn points
            for (int i = 0; i < 3; i++)
            {
                var enemySpawn = new GameObject($"EnemySpawnPoint_{i}");
                enemySpawn.transform.parent = spawnPoints.transform;
                enemySpawn.transform.position = new Vector3(5f, 0.5f, -2f + i * 2f);
                CreateSpawnPointVisual(enemySpawn, Color.red);
            }

            // Create UI Canvas placeholder
            var uiCanvas = new GameObject("UI Canvas");
            var canvas = uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            uiCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create battle info text
            var battleInfo = new GameObject("BattleInfo");
            battleInfo.transform.SetParent(uiCanvas.transform);
            var infoText = battleInfo.AddComponent<UnityEngine.UI.Text>();
            infoText.text = "Test Arena - Press Play to Start Battle";
            infoText.fontSize = 24;
            infoText.alignment = TextAnchor.UpperCenter;
            infoText.color = Color.white;
            var infoRect = battleInfo.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 1f);
            infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(0f, -30f);
            infoRect.sizeDelta = new Vector2(600f, 50f);

            // Save scene
            string scenePath = "Assets/Scenes/Test/TestArena.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"[TestArenaSetup] Created Test Arena scene at {scenePath}");
            Debug.Log("[TestArenaSetup] Scene setup complete! Press Play to run the test battle.");
        }

        private static void CreateSpawnPointVisual(GameObject parent, Color color)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.parent = parent.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.3f;

            // Remove collider
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Set color
            var renderer = visual.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.5f);
            renderer.material = mat;
        }

        [MenuItem("VeilBreakers/Run All System Tests", false, 200)]
        public static void RunAllSystemTests()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestArenaSetup] Tests must be run in Play mode. Enter Play mode first.");
                return;
            }

            var testSetup = Object.FindFirstObjectByType<CombatTestSetup>();
            if (testSetup != null)
            {
                testSetup.RunAllTests();
            }
            else
            {
                Debug.Log("[TestArenaSetup] Creating temporary test runner...");
                var tempObj = new GameObject("[TempTestRunner]");
                var test = tempObj.AddComponent<CombatTestSetup>();
                test.RunAllTests();
                Object.Destroy(tempObj);
            }
        }

        [MenuItem("VeilBreakers/System Health Check", false, 201)]
        public static void RunSystemHealthCheck()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestArenaSetup] Health check must be run in Play mode. Enter Play mode first.");
                return;
            }

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.RunSystemTests();
            }
            else
            {
                Debug.LogError("[TestArenaSetup] GameBootstrap not found in scene!");
            }
        }

        [MenuItem("VeilBreakers/Open Project Documentation", false, 300)]
        public static void OpenDocumentation()
        {
            string docsPath = Application.dataPath.Replace("/Assets", "/Docs");
            EditorUtility.RevealInFinder(docsPath);
        }
    }
}
#endif

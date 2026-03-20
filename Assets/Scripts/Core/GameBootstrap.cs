using System.Collections;
using UnityEngine;
using VeilBreakers.Managers;
using VeilBreakers.Audio;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Bootstrap script that initializes all game systems in the correct order.
    /// Attach this to a single GameObject in your initial scene.
    /// </summary>
    public class GameBootstrap : SingletonMonoBehaviour<GameBootstrap>
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [SerializeField] private AudioConfig _audioConfig;
        [SerializeField] private bool _initializeOnAwake = true;

        [Header("Scene Loading")]
        [SerializeField] private string _firstSceneToLoad = "MainMenu";
        [SerializeField] private bool _autoLoadFirstScene = true;
        [SerializeField] private float _minimumSplashTime = 1.0f;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _runTestsOnStart = false;

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private WaitForSeconds _splashWait;
        private static readonly WaitForSeconds kTestDelayWait = new WaitForSeconds(0.5f);

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            if (_initializeOnAwake)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (_runTestsOnStart)
            {
                StartCoroutine(RunTestsDelayed());
            }

            if (_autoLoadFirstScene && _isInitialized)
            {
                StartCoroutine(LoadFirstSceneAfterDelay());
            }
        }

        private IEnumerator LoadFirstSceneAfterDelay()
        {
            // Wait for minimum splash time (allows splash screen to be visible)
            yield return _splashWait ??= new WaitForSeconds(_minimumSplashTime);

            // Wait for VBSceneManager to be ready (max 2 seconds)
            float waitTime = 0f;
            while (!VBSceneManager.HasInstance && waitTime < 2f)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // Load the first scene
            if (VBSceneManager.HasInstance)
            {
                Log($"Loading first scene: {_firstSceneToLoad}");
                VBSceneManager.Instance.LoadSceneWithFade(_firstSceneToLoad);
            }
            else
            {
                // Fallback if VBSceneManager isn't ready
                LogWarning("VBSceneManager not ready, using direct load");
                UnityEngine.SceneManagement.SceneManager.LoadScene(_firstSceneToLoad);
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize all game systems in the correct order by accessing their singletons.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Log("GameBootstrap already initialized");
                return;
            }

            Log("=== INITIALIZING VEILBREAKERS ===");

            // Explicitly ensure manager GameObjects exist. SingletonMonoBehaviour.Instance
            // does not auto-create by itself.

            // Phase 1: Core Managers
            Log("Phase 1: Core Managers");
            EnsureManager<GameManager>("[GameManager]");
            EnsureManager<GameDatabase>("[GameDatabase]");
            EnsureManager<InputManager>("[InputManager]");
            Log("  - Core managers initialized.");

            // Phase 2: Persistence & Settings
            Log("Phase 2: Persistence & Settings");
            EnsureManager<SettingsManager>("[SettingsManager]");
            EnsureManager<VBSceneManager>("[VBSceneManager]");
            EnsureManager<SaveManager>("[SaveManager]");
            EnsureManager<AutoSaveManager>("[AutoSaveManager]");
            Log("  - Persistence systems initialized.");

            // Phase 3: Audio System
            Log("Phase 3: Audio System");
            var audioManager = EnsureManager<AudioManager>("[AudioManager]");
            if (audioManager != null)
            {
                audioManager.SetConfig(_audioConfig);
            }
            EnsureManager<MusicManager>("[MusicManager]");
            EnsureManager<VERAVoiceController>("[VERAVoiceController]");
            EnsureLowHealthAudio("[LowHealthAudio]");
            Log("  - Audio systems initialized.");
            
            // Phase 4: Gameplay Systems
            Log("Phase 4: Gameplay Systems");
            EnsureManager<StatusEffectManager>("[StatusEffectManager]");
            EnsureManager<ShrineManager>("[ShrineManager]");
            EnsureFPSCounter("[FPSCounter]");
            Log("  - Gameplay systems initialized.");

            _isInitialized = true;
            Log("=== INITIALIZATION COMPLETE ===");
        }

        // =============================================================================
        // TESTS
        // =============================================================================

        private IEnumerator RunTestsDelayed()
        {
            yield return kTestDelayWait;
            RunSystemTests();
        }

        [ContextMenu("Run System Tests")]
        public void RunSystemTests()
        {
            Log("\n=== SYSTEM HEALTH CHECK ===");

            CheckManager("GameManager", GameManager.HasInstance);
            CheckManager("GameDatabase", GameDatabase.HasInstance);
            CheckManager("InputManager", InputManager.HasInstance);
            CheckManager("SettingsManager", SettingsManager.HasInstance);
            CheckManager("VBSceneManager", VBSceneManager.HasInstance);
            CheckManager("SaveManager", SaveManager.HasInstance);
            CheckManager("AutoSaveManager", AutoSaveManager.HasInstance);
            CheckManager("AudioManager", AudioManager.HasInstance);
            CheckManager("MusicManager", MusicManager.HasInstance);
            CheckManager("VERAVoiceController", VERAVoiceController.HasInstance);
            CheckManager("LowHealthAudio", LowHealthAudio.HasInstance);
            CheckManager("StatusEffectManager", StatusEffectManager.HasInstance);
            CheckManager("ShrineManager", ShrineManager.HasInstance);

            Log("=== HEALTH CHECK COMPLETE ===\n");
        }

        private void CheckManager(string name, bool exists)
        {
            if (exists)
            {
                Log($"  [OK] {name}");
            }
            else
            {
                LogError($"  [MISSING] {name}");
            }
        }

        private T EnsureManager<T>(string objectName) where T : SingletonMonoBehaviour<T>
        {
            if (!SingletonMonoBehaviour<T>.HasInstance)
            {
                var managerObject = new GameObject(objectName);
                managerObject.AddComponent<T>();
                Log($"  - Created {typeof(T).Name}");
            }

            var instance = SingletonMonoBehaviour<T>.Instance;
            if (instance == null)
            {
                LogError($"  - Failed to initialize {typeof(T).Name}");
            }

            return instance;
        }

        private LowHealthAudio EnsureLowHealthAudio(string objectName)
        {
            if (!LowHealthAudio.HasInstance)
            {
                var managerObject = new GameObject(objectName);
                managerObject.AddComponent<LowHealthAudio>();
                Log("  - Created LowHealthAudio");
            }

            var instance = LowHealthAudio.Instance;
            if (instance == null)
            {
                LogError("  - Failed to initialize LowHealthAudio");
            }

            return instance;
        }

        private UI.Menus.FPSCounter EnsureFPSCounter(string objectName)
        {
            if (UI.Menus.FPSCounter.Instance == null)
            {
                var fpsObject = new GameObject(objectName);
                fpsObject.AddComponent<UI.Menus.FPSCounter>();
                Log("  - Created FPSCounter");
            }
            return UI.Menus.FPSCounter.Instance;
        }

        // =============================================================================
        // LOGGING
        // =============================================================================

        private void Log(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[GameBootstrap] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GameBootstrap] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[GameBootstrap] {message}");
        }
    }
}

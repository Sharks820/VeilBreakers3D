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
    public class GameBootstrap : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static GameBootstrap _instance;
        public static GameBootstrap Instance => _instance;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [SerializeField] private AudioConfig _audioConfig;
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _dontDestroyOnLoad = true;

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

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Log("Duplicate GameBootstrap found, destroying...");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

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
            yield return new WaitForSeconds(_minimumSplashTime);

            // Wait for VBSceneManager to be ready (max 2 seconds)
            float waitTime = 0f;
            while (VBSceneManager.Instance == null && waitTime < 2f)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // Load the first scene
            if (VBSceneManager.Instance != null)
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
        /// Initialize all game systems in the correct order.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Log("GameBootstrap already initialized");
                return;
            }

            Log("=== INITIALIZING VEILBREAKERS ===");

            // Phase 1: Create core managers
            InitializeCoreManagers();

            // Phase 2: Initialize persistence systems (before anything that might save)
            InitializePersistenceSystems();

            // Phase 3: Initialize audio system
            InitializeAudioSystem();

            // Phase 4: Load game data
            InitializeGameData();

            // Phase 5: Initialize combat systems
            InitializeCombatSystems();

            // Phase 6: Initialize gameplay systems
            InitializeGameplaySystems();

            _isInitialized = true;
            Log("=== INITIALIZATION COMPLETE ===");
        }

        private void InitializeCoreManagers()
        {
            Log("Phase 1: Core Managers");

            // GameManager
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("[GameManager]");
                gmObj.AddComponent<GameManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(gmObj);
                Log("  - Created GameManager");
            }
            else
            {
                Log("  - GameManager exists");
            }

            // EventBus is a static class, no instantiation needed
            Log("  - EventBus (static class - always available)");

            // GameDatabase
            if (GameDatabase.Instance == null)
            {
                var dbObj = new GameObject("[GameDatabase]");
                dbObj.AddComponent<GameDatabase>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(dbObj);
                Log("  - Created GameDatabase");
            }
            else
            {
                Log("  - GameDatabase exists");
            }
        }

        private void InitializePersistenceSystems()
        {
            Log("Phase 2: Persistence Systems");

            // VBSceneManager (needs to exist before any scene operations)
            if (VBSceneManager.Instance == null)
            {
                var smObj = new GameObject("[VBSceneManager]");
                smObj.AddComponent<VBSceneManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(smObj);
                Log("  - Created VBSceneManager");
            }
            else
            {
                Log("  - VBSceneManager exists");
            }

            // SaveManager
            if (SaveManager.Instance == null)
            {
                var saveObj = new GameObject("[SaveManager]");
                saveObj.AddComponent<SaveManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(saveObj);
                Log("  - Created SaveManager");
            }
            else
            {
                Log("  - SaveManager exists");
            }

            // AutoSaveManager
            if (AutoSaveManager.Instance == null)
            {
                var autoSaveObj = new GameObject("[AutoSaveManager]");
                autoSaveObj.AddComponent<AutoSaveManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(autoSaveObj);
                Log("  - Created AutoSaveManager");
            }
            else
            {
                Log("  - AutoSaveManager exists");
            }
        }

        private void InitializeAudioSystem()
        {
            Log("Phase 2: Audio System");

            // AudioManager
            if (AudioManager.Instance == null)
            {
                var amObj = new GameObject("[AudioManager]");
                var am = amObj.AddComponent<AudioManager>();
                if (_audioConfig != null)
                {
                    am.SetConfig(_audioConfig);
                }
                if (_dontDestroyOnLoad) DontDestroyOnLoad(amObj);
                Log("  - Created AudioManager");
            }
            else
            {
                Log("  - AudioManager exists");
            }

            // MusicManager
            if (MusicManager.Instance == null)
            {
                var mmObj = new GameObject("[MusicManager]");
                mmObj.AddComponent<MusicManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(mmObj);
                Log("  - Created MusicManager");
            }
            else
            {
                Log("  - MusicManager exists");
            }

            // VERAVoiceController
            if (VERAVoiceController.Instance == null)
            {
                var vcObj = new GameObject("[VERAVoiceController]");
                vcObj.AddComponent<VERAVoiceController>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(vcObj);
                Log("  - Created VERAVoiceController");
            }
            else
            {
                Log("  - VERAVoiceController exists");
            }

            // LowHealthAudio
            if (LowHealthAudio.Instance == null)
            {
                var lhObj = new GameObject("[LowHealthAudio]");
                lhObj.AddComponent<LowHealthAudio>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(lhObj);
                Log("  - Created LowHealthAudio");
            }
            else
            {
                Log("  - LowHealthAudio exists");
            }
        }

        private void InitializeGameData()
        {
            Log("Phase 3: Game Data");

            // Load data from GameDatabase
            if (GameDatabase.Instance != null)
            {
                Log($"  - Monsters loaded: {GameDatabase.Instance.MonsterCount}");
                Log($"  - Skills loaded: {GameDatabase.Instance.SkillCount}");
                Log($"  - Heroes loaded: {GameDatabase.Instance.HeroCount}");
                Log($"  - Items loaded: {GameDatabase.Instance.ItemCount}");
            }
            else
            {
                LogWarning("  - GameDatabase not available");
            }
        }

        private void InitializeCombatSystems()
        {
            Log("Phase 4: Combat Systems");

            // BattleManager
            if (BattleManager.Instance == null)
            {
                var bmObj = new GameObject("[BattleManager]");
                bmObj.AddComponent<BattleManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(bmObj);
                Log("  - Created BattleManager");
            }
            else
            {
                Log("  - BattleManager exists");
            }

            // AudioBattleIntegration
            var existingIntegration = FindFirstObjectByType<AudioBattleIntegration>();
            if (existingIntegration == null)
            {
                var aiObj = new GameObject("[AudioBattleIntegration]");
                aiObj.AddComponent<AudioBattleIntegration>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(aiObj);
                Log("  - Created AudioBattleIntegration");
            }
            else
            {
                Log("  - AudioBattleIntegration exists");
            }
        }

        private void InitializeGameplaySystems()
        {
            Log("Phase 6: Gameplay Systems");

            // StatusEffectManager
            if (StatusEffectManager.Instance == null)
            {
                var seObj = new GameObject("[StatusEffectManager]");
                seObj.AddComponent<StatusEffectManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(seObj);
                Log("  - Created StatusEffectManager");
            }
            else
            {
                Log("  - StatusEffectManager exists");
            }

            // ShrineManager
            if (ShrineManager.Instance == null)
            {
                var shrineObj = new GameObject("[ShrineManager]");
                shrineObj.AddComponent<ShrineManager>();
                if (_dontDestroyOnLoad) DontDestroyOnLoad(shrineObj);
                Log("  - Created ShrineManager");
            }
            else
            {
                Log("  - ShrineManager exists");
            }
        }

        // =============================================================================
        // TESTS
        // =============================================================================

        private IEnumerator RunTestsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            RunSystemTests();
        }

        [ContextMenu("Run System Tests")]
        public void RunSystemTests()
        {
            Log("\n=== SYSTEM HEALTH CHECK ===");

            // Check core managers
            CheckManager("GameManager", GameManager.Instance != null);
            Log("  [OK] EventBus (static class)"); // EventBus is static, always available
            CheckManager("GameDatabase", GameDatabase.Instance != null);

            // Check persistence
            CheckManager("VBSceneManager", VBSceneManager.Instance != null);
            CheckManager("SaveManager", SaveManager.Instance != null);
            CheckManager("AutoSaveManager", AutoSaveManager.Instance != null);

            // Check audio
            CheckManager("AudioManager", AudioManager.Instance != null);
            CheckManager("MusicManager", MusicManager.Instance != null);
            CheckManager("VERAVoiceController", VERAVoiceController.Instance != null);
            CheckManager("LowHealthAudio", LowHealthAudio.Instance != null);

            // Check combat
            CheckManager("BattleManager", BattleManager.Instance != null);

            // Check gameplay
            CheckManager("StatusEffectManager", StatusEffectManager.Instance != null);
            CheckManager("ShrineManager", ShrineManager.Instance != null);

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

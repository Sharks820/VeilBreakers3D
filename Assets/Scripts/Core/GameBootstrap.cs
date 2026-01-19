using System.Collections;
using UnityEngine;
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

            // Phase 2: Initialize audio system
            InitializeAudioSystem();

            // Phase 3: Load game data
            InitializeGameData();

            // Phase 4: Initialize combat systems
            InitializeCombatSystems();

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
                    // Config will be set via inspector on the manager
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

            // Check audio
            CheckManager("AudioManager", AudioManager.Instance != null);
            CheckManager("MusicManager", MusicManager.Instance != null);
            CheckManager("VERAVoiceController", VERAVoiceController.Instance != null);
            CheckManager("LowHealthAudio", LowHealthAudio.Instance != null);

            // Check combat
            CheckManager("BattleManager", BattleManager.Instance != null);

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

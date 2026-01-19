using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Test suite for audio system components.
    /// </summary>
    public class AudioTests : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Test Mode")]
        [SerializeField] private bool _runTestsOnStart = false;

        [Header("Test Config")]
        [SerializeField] private AudioConfig _testConfig;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }

        // =============================================================================
        // TEST RUNNER
        // =============================================================================

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== AUDIO SYSTEM TESTS ===");

            TestAudioConfig();
            TestMemoryBudgets();
            TestVERAVoiceThresholds();
            TestLowHealthIntensity();
            TestMusicStates();
            TestBankNaming();
            TestSingletons();

            Debug.Log("=== AUDIO SYSTEM TESTS COMPLETE ===");
        }

        // =============================================================================
        // CONFIG TESTS
        // =============================================================================

        [ContextMenu("Test Audio Config")]
        public void TestAudioConfig()
        {
            Debug.Log("--- Testing AudioConfig ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  No test config assigned - create AudioConfig asset");
                return;
            }

            Debug.Log($"  Memory Budget (Core): {_testConfig.budgetCore}MB");
            Debug.Log($"  Memory Budget (Zone): {_testConfig.budgetZone}MB");
            Debug.Log($"  Memory Budget (Combat): {_testConfig.budgetCombat}MB");
            Debug.Log($"  Memory Budget (Voice): {_testConfig.budgetVoice}MB");
            Debug.Log($"  Total Budget: {_testConfig.TotalBudgetBytes / (1024 * 1024)}MB");

            Debug.Log($"  Default Master Vol: {_testConfig.defaultMaster:P0}");
            Debug.Log($"  Default Music Vol: {_testConfig.defaultMusic:P0}");
            Debug.Log($"  Default SFX Vol: {_testConfig.defaultSFX:P0}");
            Debug.Log($"  Default Voice Vol: {_testConfig.defaultVoice:P0}");
        }

        [ContextMenu("Test Memory Budgets")]
        public void TestMemoryBudgets()
        {
            Debug.Log("--- Testing Memory Budgets ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            long totalBytes = _testConfig.TotalBudgetBytes;
            Debug.Log($"  Total Budget: {totalBytes / (1024 * 1024)}MB ({totalBytes:N0} bytes)");

            // Test budget constants match
            int expectedTotal = _testConfig.budgetCore + _testConfig.budgetZone +
                               _testConfig.budgetCombat + _testConfig.budgetVoice;
            Debug.Log($"  Expected Total: {expectedTotal}MB");
            Debug.Log($"  Match: {expectedTotal * 1024 * 1024 == totalBytes}");
        }

        // =============================================================================
        // VERA VOICE TESTS
        // =============================================================================

        [ContextMenu("Test VERA Voice Thresholds")]
        public void TestVERAVoiceThresholds()
        {
            Debug.Log("--- Testing VERA Voice Thresholds ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            // Test corruption levels at various integrity values
            float[] testValues = { 100f, 80f, 70f, 60f, 50f, 40f, 30f, 20f, 10f, 0f };

            foreach (float integrity in testValues)
            {
                float corruption = _testConfig.GetVERACorruptionLevel(integrity);
                string state = GetVERAStateDescription(integrity, _testConfig);
                Debug.Log($"  VI {integrity:F0}%: Corruption {corruption:F2} ({state})");
            }
        }

        private string GetVERAStateDescription(float integrity, AudioConfig config)
        {
            if (integrity >= config.veraCleanThreshold) return "Clean";
            if (integrity >= config.veraMildGlitchThreshold) return "Mild Glitches";
            if (integrity >= config.veraDistortionThreshold) return "Distortion";
            if (integrity >= config.veraDualVoiceThreshold) return "Dual Voice";
            return "Full Corruption";
        }

        // =============================================================================
        // LOW HEALTH TESTS
        // =============================================================================

        [ContextMenu("Test Low Health Intensity")]
        public void TestLowHealthIntensity()
        {
            Debug.Log("--- Testing Low Health Intensity ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            Debug.Log($"  Low HP Threshold: {_testConfig.lowHealthThreshold:P0}");
            Debug.Log($"  Medium Threshold: {_testConfig.mediumHealthThreshold:P0}");
            Debug.Log($"  Critical Threshold: {_testConfig.criticalHealthThreshold:P0}");

            // Test intensity at various health values
            float[] testValues = { 1f, 0.5f, 0.25f, 0.2f, 0.15f, 0.1f, 0.05f, 0.01f };

            foreach (float hp in testValues)
            {
                float intensity = _testConfig.GetLowHealthIntensity(hp);
                string state = hp > _testConfig.lowHealthThreshold ? "Normal" :
                              hp > _testConfig.mediumHealthThreshold ? "Low" :
                              hp > _testConfig.criticalHealthThreshold ? "Medium" : "Critical";
                Debug.Log($"  HP {hp:P0}: Intensity {intensity:F2} ({state})");
            }
        }

        // =============================================================================
        // MUSIC STATE TESTS
        // =============================================================================

        [ContextMenu("Test Music States")]
        public void TestMusicStates()
        {
            Debug.Log("--- Testing Music States ---");

            var states = Enum.GetValues(typeof(MusicState));
            Debug.Log($"  Total Music States: {states.Length}");

            foreach (MusicState state in states)
            {
                Debug.Log($"  MusicState.{state}");
            }

            // Test priority layers
            Debug.Log("\n  Audio Priority Layers:");
            var priorities = Enum.GetValues(typeof(AudioPriority));
            foreach (AudioPriority priority in priorities)
            {
                Debug.Log($"  {(int)priority}. {priority}");
            }
        }

        // =============================================================================
        // BANK NAMING TESTS
        // =============================================================================

        [ContextMenu("Test Bank Naming")]
        public void TestBankNaming()
        {
            Debug.Log("--- Testing Bank Naming ---");

            Debug.Log($"  Core Bank: {AudioConfig.CORE_BANK}");
            Debug.Log($"  Zone Bank Prefix: {AudioConfig.ZONE_BANK_PREFIX}");
            Debug.Log($"  Monster Bank Prefix: {AudioConfig.MONSTER_BANK_PREFIX}");
            Debug.Log($"  NPC Bank Prefix: {AudioConfig.NPC_BANK_PREFIX}");

            // Test bank name generation
            Debug.Log($"  Zone 'ThornwoodForest': {AudioConfig.GetZoneBankName("ThornwoodForest")}");
            Debug.Log($"  Monster 'Hollow': {AudioConfig.GetMonsterBankName("Hollow")}");
            Debug.Log($"  NPC 'Elder': {AudioConfig.GetNPCBankName("Elder")}");
        }

        // =============================================================================
        // SINGLETON TESTS
        // =============================================================================

        [ContextMenu("Test Singletons")]
        public void TestSingletons()
        {
            Debug.Log("--- Testing Audio Singletons ---");

            // AudioManager
            if (AudioManager.Instance != null)
            {
                Debug.Log($"  AudioManager: Found (Initialized: {AudioManager.Instance.IsInitialized})");
                Debug.Log($"    Memory: {AudioManager.Instance.GetMemoryUsageString()}");
                Debug.Log($"    Loaded Banks: {AudioManager.Instance.LoadedBanks.Count}");
            }
            else
            {
                Debug.Log("  AudioManager: Not found (expected in non-game context)");
            }

            // MusicManager
            if (MusicManager.Instance != null)
            {
                Debug.Log($"  MusicManager: Found (State: {MusicManager.Instance.CurrentState})");
                Debug.Log($"    {MusicManager.Instance.GetStateString()}");
            }
            else
            {
                Debug.Log("  MusicManager: Not found (expected in non-game context)");
            }

            // VERAVoiceController
            if (VERAVoiceController.Instance != null)
            {
                Debug.Log($"  VERAVoice: Found (VI: {VERAVoiceController.Instance.VeilIntegrity:F0}%)");
                Debug.Log($"    {VERAVoiceController.Instance.GetStateString()}");
            }
            else
            {
                Debug.Log("  VERAVoice: Not found (expected in non-game context)");
            }

            // LowHealthAudio
            if (LowHealthAudio.Instance != null)
            {
                Debug.Log($"  LowHealthAudio: Found (Active: {LowHealthAudio.Instance.IsActive})");
                Debug.Log($"    {LowHealthAudio.Instance.GetStateString()}");
            }
            else
            {
                Debug.Log("  LowHealthAudio: Not found (expected in non-game context)");
            }
        }

        // =============================================================================
        // RUNTIME TESTS
        // =============================================================================

        [ContextMenu("Test Play Combat Hit")]
        public void TestPlayCombatHit()
        {
            Debug.Log("--- Testing Combat Hit Sound ---");

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("  AudioManager not found");
                return;
            }

            AudioManager.Instance.PlayCombatHit("Light");
            Debug.Log("  Played: Hit_Light");

            AudioManager.Instance.PlayCombatHit("Medium");
            Debug.Log("  Played: Hit_Medium");

            AudioManager.Instance.PlayCombatHit("Heavy");
            Debug.Log("  Played: Hit_Heavy");
        }

        [ContextMenu("Test VERA Voice")]
        public void TestVERAVoice()
        {
            Debug.Log("--- Testing VERA Voice ---");

            if (VERAVoiceController.Instance == null)
            {
                Debug.LogWarning("  VERAVoiceController not found");
                return;
            }

            // Test at different integrity levels
            Debug.Log("  Testing at 100% integrity...");
            VERAVoiceController.Instance.SetVeilIntegrityImmediate(100f);
            VERAVoiceController.Instance.PlayDialogue("Test_Clean");

            Debug.Log("  Testing at 50% integrity...");
            VERAVoiceController.Instance.SetVeilIntegrityImmediate(50f);
            VERAVoiceController.Instance.PlayDialogue("Test_Distorted");

            Debug.Log("  Testing at 10% integrity...");
            VERAVoiceController.Instance.SetVeilIntegrityImmediate(10f);
            VERAVoiceController.Instance.PlayDialogue("Test_Corrupted");
        }

        [ContextMenu("Test Music Transitions")]
        public void TestMusicTransitions()
        {
            Debug.Log("--- Testing Music Transitions ---");

            if (MusicManager.Instance == null)
            {
                Debug.LogWarning("  MusicManager not found");
                return;
            }

            Debug.Log($"  Current State: {MusicManager.Instance.CurrentState}");

            Debug.Log("  Transitioning to TENSION...");
            MusicManager.Instance.SetMusicState(MusicState.TENSION);

            Debug.Log("  Transitioning to COMBAT_LOW...");
            MusicManager.Instance.SetMusicState(MusicState.COMBAT_LOW);

            Debug.Log("  Setting intensity to 0.8...");
            MusicManager.Instance.SetCombatIntensity(0.8f);
        }

        [ContextMenu("Test Low Health Audio")]
        public void TestLowHealthAudio()
        {
            Debug.Log("--- Testing Low Health Audio ---");

            if (LowHealthAudio.Instance == null)
            {
                Debug.LogWarning("  LowHealthAudio not found");
                return;
            }

            Debug.Log("  Testing at 100% health (should not activate)...");
            LowHealthAudio.Instance.UpdateHealth(1f);
            Debug.Log($"    Active: {LowHealthAudio.Instance.IsActive}");

            Debug.Log("  Testing at 20% health (should activate)...");
            LowHealthAudio.Instance.UpdateHealth(0.2f);
            Debug.Log($"    Active: {LowHealthAudio.Instance.IsActive}");
            Debug.Log($"    Intensity: {LowHealthAudio.Instance.Intensity:F2}");

            Debug.Log("  Testing at 5% health (critical)...");
            LowHealthAudio.Instance.UpdateHealth(0.05f);
            Debug.Log($"    Intensity: {LowHealthAudio.Instance.Intensity:F2}");

            Debug.Log("  Resetting to 100%...");
            LowHealthAudio.Instance.UpdateHealth(1f);
            Debug.Log($"    Active: {LowHealthAudio.Instance.IsActive}");
        }

        // =============================================================================
        // VOLUME TESTS
        // =============================================================================

        [ContextMenu("Test Volume Controls")]
        public void TestVolumeControls()
        {
            Debug.Log("--- Testing Volume Controls ---");

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("  AudioManager not found");
                return;
            }

            Debug.Log($"  Current Master: {AudioManager.Instance.MasterVolume:P0}");
            Debug.Log($"  Current Music: {AudioManager.Instance.MusicVolume:P0}");
            Debug.Log($"  Current SFX: {AudioManager.Instance.SFXVolume:P0}");
            Debug.Log($"  Current Voice: {AudioManager.Instance.VoiceVolume:P0}");
            Debug.Log($"  Current Ambient: {AudioManager.Instance.AmbientVolume:P0}");

            // Test volume changes
            Debug.Log("  Setting Master to 50%...");
            AudioManager.Instance.SetMasterVolume(0.5f);
            Debug.Log($"    New Master: {AudioManager.Instance.MasterVolume:P0}");

            Debug.Log("  Resetting Master to 80%...");
            AudioManager.Instance.SetMasterVolume(0.8f);
        }

        // =============================================================================
        // ZONE TESTS
        // =============================================================================

        [ContextMenu("Test Zone Loading")]
        public void TestZoneLoading()
        {
            Debug.Log("--- Testing Zone Loading ---");

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("  AudioManager not found");
                return;
            }

            Debug.Log($"  Currently Loaded Banks: {AudioManager.Instance.LoadedBanks.Count}");
            foreach (var bank in AudioManager.Instance.LoadedBanks)
            {
                Debug.Log($"    - {bank}");
            }

            Debug.Log("  Simulating zone enter: ThornwoodForest...");
            AudioManager.Instance.OnZoneEnter("ThornwoodForest");

            Debug.Log("  Simulating boundary approach: AshfallCity...");
            AudioManager.Instance.OnZoneBoundaryApproach("AshfallCity");
        }

        // =============================================================================
        // COMBAT INTEGRATION TESTS
        // =============================================================================

        [ContextMenu("Test Combat Audio Integration")]
        public void TestCombatAudioIntegration()
        {
            Debug.Log("--- Testing Combat Audio Integration ---");

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("  AudioManager not found");
                return;
            }

            // Simulate combat start
            Debug.Log("  Simulating combat start with enemies...");
            var enemies = new List<string> { "Hollow", "Wraith", "Ghoul" };
            AudioManager.Instance.OnCombatStart(enemies);

            // Music transition
            MusicManager.Instance?.StartCombatMusic();

            // Play some combat sounds
            Debug.Log("  Playing combat sounds...");
            AudioManager.Instance.PlayCombatHit("Medium");
            AudioManager.Instance.PlayBlock();

            // Simulate combat end
            Debug.Log("  Simulating combat end...");
            AudioManager.Instance.OnCombatEnd();
            MusicManager.Instance?.PlayVictory();
        }
    }
}

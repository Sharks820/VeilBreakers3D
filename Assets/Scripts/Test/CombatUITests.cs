using System;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.UI.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test suite for Combat UI components.
    /// </summary>
    public class CombatUITests : MonoBehaviour
    {
        [Header("Test Mode")]
        [SerializeField] private bool _runTestsOnStart = false;

        [Header("Test Config")]
        [SerializeField] private CombatUIConfig _testConfig;

        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== COMBAT UI TESTS ===");

            TestCombatUIConfig();
            TestHealthBarColors();
            TestCorruptionColors();
            TestSkillSlotBindings();

            Debug.Log("=== COMBAT UI TESTS COMPLETE ===");
        }

        // =============================================================================
        // CONFIG TESTS
        // =============================================================================

        private void TestCombatUIConfig()
        {
            Debug.Log("--- Testing CombatUIConfig ---");

            // Test scale factor
            float scaleFactor = CombatUIConfig.GetScaleFactor();
            Debug.Log($"  Scale Factor: {scaleFactor} (Screen: {Screen.width}x{Screen.height})");

            // Test default config values
            if (_testConfig != null)
            {
                Debug.Log($"  Panel Background: {_testConfig.panelBackground}");
                Debug.Log($"  HP Full Color: {_testConfig.hpFull}");
                Debug.Log($"  MP Full Color: {_testConfig.mpFull}");
                Debug.Log($"  Ultimate Ready Color: {_testConfig.ultimateReady}");
            }
            else
            {
                Debug.LogWarning("  No test config assigned - create CombatUIConfig asset");
            }
        }

        private void TestHealthBarColors()
        {
            Debug.Log("--- Testing Health Bar Colors ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            // Test HP color gradient
            Debug.Log($"  HP at 100%: {_testConfig.GetHPColor(1f)}");
            Debug.Log($"  HP at 50%: {_testConfig.GetHPColor(0.5f)}");
            Debug.Log($"  HP at 0%: {_testConfig.GetHPColor(0f)}");

            // Test MP color gradient
            Debug.Log($"  MP at 100%: {_testConfig.GetMPColor(1f)}");
            Debug.Log($"  MP at 50%: {_testConfig.GetMPColor(0.5f)}");
            Debug.Log($"  MP at 0%: {_testConfig.GetMPColor(0f)}");
        }

        private void TestCorruptionColors()
        {
            Debug.Log("--- Testing Corruption Colors ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            // Test corruption color at various levels
            Debug.Log($"  Corruption 0%: {_testConfig.GetCorruptionColor(0f)} (Green)");
            Debug.Log($"  Corruption 25%: {_testConfig.GetCorruptionColor(25f)} (Green)");
            Debug.Log($"  Corruption 50%: {_testConfig.GetCorruptionColor(50f)} (Yellow)");
            Debug.Log($"  Corruption 75%: {_testConfig.GetCorruptionColor(75f)} (Red)");
            Debug.Log($"  Corruption 100%: {_testConfig.GetCorruptionColor(100f)} (Red)");
        }

        private void TestSkillSlotBindings()
        {
            Debug.Log("--- Testing Skill Slot Bindings ---");

            var bindings = CombatUIDefaults.DefaultBindings;
            Debug.Log($"  Total bindings: {bindings.Length}");

            foreach (var binding in bindings)
            {
                Debug.Log($"  Slot {binding.slotIndex}: {binding.displayText} ({binding.keyCode})");
            }

            // Test ally ultimate keys
            var allyKeys = CombatUIDefaults.AllyUltimateKeys;
            Debug.Log($"  Ally Ultimate Keys: F1={allyKeys[0]}, F2={allyKeys[1]}, F3={allyKeys[2]}");

            // Test special keys
            Debug.Log($"  Target Next: {CombatUIDefaults.TargetNextKey}");
            Debug.Log($"  Capture Key: {CombatUIDefaults.CaptureKey}");
            Debug.Log($"  Quick Command: {CombatUIDefaults.QuickCommandKey}");
        }

        // =============================================================================
        // SKILL SLOT STATE TESTS
        // =============================================================================

        [ContextMenu("Test Skill Slot States")]
        public void TestSkillSlotStates()
        {
            Debug.Log("--- Testing Skill Slot States ---");

            // Test all possible states
            var states = Enum.GetValues(typeof(SkillSlotState));
            foreach (SkillSlotState state in states)
            {
                Debug.Log($"  SkillSlotState.{state}");
            }
        }

        // =============================================================================
        // CAPTURE BANNER STATE TESTS
        // =============================================================================

        [ContextMenu("Test Capture Banner States")]
        public void TestCaptureBannerStates()
        {
            Debug.Log("--- Testing Capture Banner States ---");

            var states = Enum.GetValues(typeof(CaptureBannerState));
            foreach (CaptureBannerState state in states)
            {
                Debug.Log($"  CaptureBannerState.{state}");
            }
        }

        // =============================================================================
        // INTEGRATION TESTS
        // =============================================================================

        [ContextMenu("Test HUD Integration")]
        public void TestHUDIntegration()
        {
            Debug.Log("--- Testing HUD Integration ---");

            // Check if CombatHUD instance exists
            if (CombatHUD.Instance != null)
            {
                Debug.Log($"  CombatHUD found: Initialized={CombatHUD.Instance.IsInitialized}");
                Debug.Log($"  Player: {CombatHUD.Instance.Player?.DisplayName ?? "None"}");
                Debug.Log($"  Current Target: {CombatHUD.Instance.CurrentTarget?.DisplayName ?? "None"}");
            }
            else
            {
                Debug.Log("  CombatHUD not found in scene - this is expected outside of combat");
            }

            // Check BattleManager
            if (BattleManager.Instance != null)
            {
                Debug.Log($"  BattleManager found: State={BattleManager.Instance.State}");
                Debug.Log($"  Player: {BattleManager.Instance.Player?.DisplayName ?? "None"}");
                Debug.Log($"  Target: {BattleManager.Instance.CurrentTarget?.DisplayName ?? "None"}");
            }
            else
            {
                Debug.Log("  BattleManager not found - this is expected outside of combat");
            }
        }

        // =============================================================================
        // ANIMATION TIMING TESTS
        // =============================================================================

        [ContextMenu("Test Animation Timings")]
        public void TestAnimationTimings()
        {
            Debug.Log("--- Testing Animation Timings ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            Debug.Log($"  HP Change Duration: {_testConfig.hpChangeDuration}s");
            Debug.Log($"  Ultimate Glow Duration: {_testConfig.ultimateGlowDuration}s");
            Debug.Log($"  Low HP Pulse Duration: {_testConfig.lowHPPulseDuration}s");
            Debug.Log($"  Capture Banner Breathe: {_testConfig.captureBannerBreatheDuration}s");
            Debug.Log($"  Skill Ready Glow: {_testConfig.skillReadyGlowDuration}s");
            Debug.Log($"  Target Popup: {_testConfig.targetPopupDuration}s");
            Debug.Log($"  Capture Breath Scale: {_testConfig.captureBreathScale}");
            Debug.Log($"  Low HP Threshold: {_testConfig.lowHPThreshold:P0}");
            Debug.Log($"  Low MP Threshold: {_testConfig.lowMPThreshold:P0}");
        }

        // =============================================================================
        // PANEL SIZE TESTS
        // =============================================================================

        [ContextMenu("Test Panel Sizes")]
        public void TestPanelSizes()
        {
            Debug.Log("--- Testing Panel Sizes ---");

            if (_testConfig == null)
            {
                Debug.LogWarning("  Skipped - no config");
                return;
            }

            Debug.Log($"  Player Panel: {_testConfig.playerPanelSize}");
            Debug.Log($"  Enemy Panel: {_testConfig.enemyPanelSize}");
            Debug.Log($"  Ally Panel: {_testConfig.allyPanelSize}");
            Debug.Log($"  Menu Icon: {_testConfig.menuIconSize}");
            Debug.Log($"  Skill Icon: {_testConfig.skillIconSize}");
            Debug.Log($"  Player Portrait: {_testConfig.playerPortraitSize}");
            Debug.Log($"  Ally Portrait: {_testConfig.allyPortraitSize}");
            Debug.Log($"  Player HP Bar: {_testConfig.playerHPBarSize}");
            Debug.Log($"  Ally HP Bar: {_testConfig.allyHPBarSize}");
        }
    }
}

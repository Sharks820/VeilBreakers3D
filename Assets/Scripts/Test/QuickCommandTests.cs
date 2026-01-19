using System;
using System.Collections;
using UnityEngine;
using VeilBreakers.Commands;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test suite for the Quick Command System.
    /// Validates commands, cooldowns, time slow, and menu behavior.
    /// </summary>
    public class QuickCommandTests : MonoBehaviour
    {
        // =============================================================================
        // TEST CONFIGURATION
        // =============================================================================

        [Header("Test Configuration")]
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private bool _logDetailedResults = true;

        private int _passCount = 0;
        private int _failCount = 0;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_runOnStart)
            {
                StartCoroutine(RunTestsDelayed());
            }
        }

        private IEnumerator RunTestsDelayed()
        {
            yield return new WaitForSeconds(0.1f);
            try
            {
                RunAllTests();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuickCommandTests] Exception during tests: {ex.Message}");
            }
        }

        // =============================================================================
        // TEST RUNNER
        // =============================================================================

        public void RunAllTests()
        {
            _passCount = 0;
            _failCount = 0;

            Debug.Log("========================================");
            Debug.Log("[QuickCommandTests] Starting Quick Command Test Suite");
            Debug.Log("========================================");

            // Command Data Tests
            TestCommandTypes();
            TestTargetTypes();
            TestCommandInstanceCreation();
            TestCommandDisplayInfo();

            // Manager Tests
            TestManagerInitialization();
            TestCooldownSystem();
            TestCommandExecution();
            TestPresetCommands();

            // Time Slow Tests
            TestTimeSlowActivation();
            TestTimeSlowReferenceCount();

            // Integration Tests
            TestFullCommandFlow();
            TestOnMeCommand();

            // Summary
            Debug.Log("========================================");
            Debug.Log($"[QuickCommandTests] COMPLETE: {_passCount} passed, {_failCount} failed");
            Debug.Log("========================================");

            if (_failCount > 0)
            {
                Debug.LogError($"[QuickCommandTests] {_failCount} TEST(S) FAILED!");
            }
        }

        // =============================================================================
        // ASSERTION HELPERS
        // =============================================================================

        private void Assert(bool condition, string testName, string details = "")
        {
            if (condition)
            {
                _passCount++;
                if (_logDetailedResults)
                {
                    Debug.Log($"[PASS] {testName}");
                }
            }
            else
            {
                _failCount++;
                Debug.LogError($"[FAIL] {testName} - {details}");
            }
        }

        private void AssertEqual<T>(T expected, T actual, string testName)
        {
            bool equal = expected == null ? actual == null : expected.Equals(actual);
            Assert(equal, testName, $"Expected {expected}, got {actual}");
        }

        private void AssertApprox(float expected, float actual, float tolerance, string testName)
        {
            bool close = Mathf.Abs(expected - actual) <= tolerance;
            Assert(close, testName, $"Expected ~{expected}, got {actual}");
        }

        // =============================================================================
        // COMMAND DATA TESTS
        // =============================================================================

        private void TestCommandTypes()
        {
            Debug.Log("\n--- Command Type Tests ---");

            // Test all command types have valid display names
            foreach (QuickCommandType type in Enum.GetValues(typeof(QuickCommandType)))
            {
                if (type == QuickCommandType.NONE) continue;

                string name = QuickCommandInstance.GetDisplayName(type);
                Assert(!string.IsNullOrEmpty(name) && name != "Unknown",
                    $"Command {type} has valid display name",
                    $"Got: {name}");
            }
        }

        private void TestTargetTypes()
        {
            Debug.Log("\n--- Target Type Tests ---");

            // Attack Target uses player's current target (AUTO)
            var attackTargetType = QuickCommandInstance.GetTargetType(QuickCommandType.ATTACK_TARGET);
            AssertEqual(CommandTargetType.AUTO, attackTargetType, "ATTACK_TARGET uses AUTO target");

            // Defend Target requires ally selection
            var defendTargetType = QuickCommandInstance.GetTargetType(QuickCommandType.DEFEND_TARGET);
            AssertEqual(CommandTargetType.ALLY, defendTargetType, "DEFEND_TARGET requires ALLY target");

            // Reposition requires ground position
            var repositionTargetType = QuickCommandInstance.GetTargetType(QuickCommandType.REPOSITION);
            AssertEqual(CommandTargetType.GROUND, repositionTargetType, "REPOSITION requires GROUND target");

            // Presets don't need targets (except FOCUS_TARGET)
            var aggressiveTargetType = QuickCommandInstance.GetTargetType(QuickCommandType.PRESET_AGGRESSIVE);
            AssertEqual(CommandTargetType.NONE, aggressiveTargetType, "PRESET_AGGRESSIVE needs no target");
        }

        private void TestCommandInstanceCreation()
        {
            Debug.Log("\n--- Command Instance Creation Tests ---");

            GameObject playerGO = null;
            GameObject allyGO = null;

            try
            {
                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);

                var command = QuickCommandInstance.Create(
                    QuickCommandType.ATTACK_TARGET,
                    player,
                    ally
                );

                Assert(command != null, "Command instance created");
                AssertEqual(QuickCommandType.ATTACK_TARGET, command.commandType, "Command type set correctly");
                AssertEqual(player, command.issuer, "Issuer set correctly");
                AssertEqual(ally, command.executor, "Executor set correctly");
                AssertEqual(CommandState.IDLE, command.state, "Initial state is IDLE");
                Assert(command.duration > 0, "Duration is positive");
            }
            finally
            {
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
            }
        }

        private void TestCommandDisplayInfo()
        {
            Debug.Log("\n--- Command Display Info Tests ---");

            // Test display name
            string attackName = QuickCommandInstance.GetDisplayName(QuickCommandType.ATTACK_TARGET);
            AssertEqual("Attack Target", attackName, "Attack Target display name correct");

            string onMeName = QuickCommandInstance.GetDisplayName(QuickCommandType.ON_ME);
            AssertEqual("On Me!", onMeName, "On Me display name correct");

            // Test description
            string attackDesc = QuickCommandInstance.GetDescription(QuickCommandType.ATTACK_TARGET);
            Assert(attackDesc.Contains("target"), "Attack description mentions target",
                $"Got: {attackDesc}");

            // Test estimated duration
            float attackDuration = QuickCommandInstance.GetEstimatedDuration(QuickCommandType.ATTACK_TARGET);
            Assert(attackDuration > 0, "Attack Target has positive duration");

            float presetDuration = QuickCommandInstance.GetEstimatedDuration(QuickCommandType.PRESET_AGGRESSIVE);
            AssertApprox(0f, presetDuration, 0.01f, "Preset commands have zero duration (instant)");
        }

        // =============================================================================
        // MANAGER TESTS
        // =============================================================================

        private void TestManagerInitialization()
        {
            Debug.Log("\n--- Manager Initialization Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject[] allyGOs = null;

            try
            {
                managerGO = new GameObject("Manager");
                var manager = managerGO.AddComponent<QuickCommandManager>();

                playerGO = new GameObject("Player");
                allyGOs = new GameObject[]
                {
                    new GameObject("Ally1"),
                    new GameObject("Ally2"),
                    new GameObject("Ally3")
                };

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var allies = new Combatant[]
                {
                    CreateTestCombatant(allyGOs[0], "Ally1", Brand.IRON, 100f),
                    CreateTestCombatant(allyGOs[1], "Ally2", Brand.GRACE, 100f),
                    CreateTestCombatant(allyGOs[2], "Ally3", Brand.SURGE, 100f)
                };

                manager.Initialize(player, allies);

                // All allies should be commandable initially
                Assert(manager.CanCommand(allies[0]), "Ally1 can be commanded after init");
                Assert(manager.CanCommand(allies[1]), "Ally2 can be commanded after init");
                Assert(manager.CanCommand(allies[2]), "Ally3 can be commanded after init");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGOs != null)
                {
                    foreach (var go in allyGOs) DestroyImmediate(go);
                }
            }
        }

        private void TestCooldownSystem()
        {
            Debug.Log("\n--- Cooldown System Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject allyGO = null;
            GameObject enemyGO = null;

            try
            {
                managerGO = new GameObject("Manager");
                var manager = managerGO.AddComponent<QuickCommandManager>();

                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");
                enemyGO = new GameObject("Enemy");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);
                var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.DREAD, 100f);

                manager.Initialize(player, new[] { ally });
                manager.SetCurrentTarget(enemy);

                // Initially not on cooldown
                Assert(!manager.IsOnCooldown(ally), "Ally not on cooldown initially");
                AssertApprox(0f, manager.GetRemainingCooldown(ally), 0.01f, "No remaining cooldown initially");

                // Issue command - should start cooldown
                bool issued = manager.IssueCommand(QuickCommandType.ATTACK_TARGET, ally);
                Assert(issued, "Command issued successfully");
                Assert(manager.IsOnCooldown(ally), "Ally on cooldown after command");
                Assert(manager.GetRemainingCooldown(ally) > 0f, "Remaining cooldown is positive");

                // Can't issue another command to same ally while on cooldown
                bool secondIssue = manager.IssueCommand(QuickCommandType.DEFEND_PLAYER, ally);
                Assert(!secondIssue, "Cannot issue second command while on cooldown");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
                if (enemyGO != null) DestroyImmediate(enemyGO);
            }
        }

        private void TestCommandExecution()
        {
            Debug.Log("\n--- Command Execution Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject allyGO = null;
            GameObject enemyGO = null;

            try
            {
                managerGO = new GameObject("Manager");
                var manager = managerGO.AddComponent<QuickCommandManager>();

                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");
                enemyGO = new GameObject("Enemy");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);
                var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.DREAD, 100f);

                manager.Initialize(player, new[] { ally });
                manager.SetCurrentTarget(enemy);

                // Issue attack command
                bool issued = manager.IssueCommand(QuickCommandType.ATTACK_TARGET, ally);
                Assert(issued, "Attack command issued");

                // Check active command
                var activeCommand = manager.GetActiveCommand(ally);
                Assert(activeCommand != null, "Active command exists");
                AssertEqual(QuickCommandType.ATTACK_TARGET, activeCommand.commandType, "Active command type is ATTACK_TARGET");
                Assert(activeCommand.IsActive, "Command is active");

                // Cancel command
                manager.CancelCommand(ally);
                activeCommand = manager.GetActiveCommand(ally);
                Assert(activeCommand == null, "Command cancelled - no active command");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
                if (enemyGO != null) DestroyImmediate(enemyGO);
            }
        }

        private void TestPresetCommands()
        {
            Debug.Log("\n--- Preset Command Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject allyGO = null;

            try
            {
                managerGO = new GameObject("Manager");
                var manager = managerGO.AddComponent<QuickCommandManager>();

                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);

                // Add GambitController for preset tests
                allyGO.AddComponent<AI.GambitController>();

                manager.Initialize(player, new[] { ally });

                // Issue preset command - should complete instantly
                bool issued = manager.IssueCommand(QuickCommandType.PRESET_AGGRESSIVE, ally);
                Assert(issued, "Preset command issued");

                // Preset commands should complete immediately (no active command)
                var activeCommand = manager.GetActiveCommand(ally);
                Assert(activeCommand == null, "Preset command completed immediately");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
            }
        }

        // =============================================================================
        // TIME SLOW TESTS
        // =============================================================================

        private void TestTimeSlowActivation()
        {
            Debug.Log("\n--- Time Slow Activation Tests ---");

            GameObject controllerGO = null;

            try
            {
                controllerGO = new GameObject("TimeController");
                var controller = controllerGO.AddComponent<TimeSlowController>();

                // Store original time scale
                float originalTimeScale = Time.timeScale;

                // Initially not slowed
                Assert(!controller.IsSlowed, "Not slowed initially");
                AssertApprox(1f, Time.timeScale, 0.01f, "Time scale is 1 initially");

                // Request time slow
                controller.RequestTimeSlow();
                Assert(controller.IsSlowed, "Slowed after request");

                // Release time slow
                controller.ReleaseTimeSlow();
                Assert(!controller.IsSlowed, "Not slowed after release");

                // Restore time scale
                Time.timeScale = originalTimeScale;
            }
            finally
            {
                Time.timeScale = 1f;
                if (controllerGO != null) DestroyImmediate(controllerGO);
            }
        }

        private void TestTimeSlowReferenceCount()
        {
            Debug.Log("\n--- Time Slow Reference Count Tests ---");

            GameObject controllerGO = null;

            try
            {
                controllerGO = new GameObject("TimeController");
                var controller = controllerGO.AddComponent<TimeSlowController>();

                // Multiple requests
                controller.RequestTimeSlow();
                controller.RequestTimeSlow();
                Assert(controller.IsSlowed, "Still slowed with 2 requests");

                // First release - still slowed
                controller.ReleaseTimeSlow();
                Assert(controller.IsSlowed, "Still slowed after first release");

                // Second release - now resumed
                controller.ReleaseTimeSlow();
                Assert(!controller.IsSlowed, "Resumed after both releases");

                // Force resume
                controller.RequestTimeSlow();
                controller.RequestTimeSlow();
                controller.ForceResumeTime();
                Assert(!controller.IsSlowed, "Force resume clears all requests");
            }
            finally
            {
                Time.timeScale = 1f;
                if (controllerGO != null) DestroyImmediate(controllerGO);
            }
        }

        // =============================================================================
        // INTEGRATION TESTS
        // =============================================================================

        private void TestFullCommandFlow()
        {
            Debug.Log("\n--- Full Command Flow Tests ---");

            GameObject managerGO = null;
            GameObject timeGO = null;
            GameObject playerGO = null;
            GameObject allyGO = null;
            GameObject enemyGO = null;

            try
            {
                managerGO = new GameObject("Manager");
                timeGO = new GameObject("TimeController");
                var manager = managerGO.AddComponent<QuickCommandManager>();
                var timeController = timeGO.AddComponent<TimeSlowController>();

                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");
                enemyGO = new GameObject("Enemy");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);
                var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.DREAD, 100f);

                manager.Initialize(player, new[] { ally });
                manager.SetCurrentTarget(enemy);

                // Track events
                bool commandIssued = false;
                manager.OnCommandIssued += (a, c) => commandIssued = true;

                // Simulate menu open (time slow)
                timeController.RequestTimeSlow();
                Assert(timeController.IsSlowed, "Time slowed when menu opens");

                // Issue command
                bool success = manager.IssueCommand(QuickCommandType.ATTACK_TARGET, ally, enemy);
                Assert(success, "Command issued successfully");
                Assert(commandIssued, "OnCommandIssued event fired");

                // Close menu (time resumes)
                timeController.ReleaseTimeSlow();
                Assert(!timeController.IsSlowed, "Time resumed when menu closes");

                // Command is active
                Assert(manager.GetActiveCommand(ally) != null, "Command is active after issue");
            }
            finally
            {
                Time.timeScale = 1f;
                if (managerGO != null) DestroyImmediate(managerGO);
                if (timeGO != null) DestroyImmediate(timeGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
                if (enemyGO != null) DestroyImmediate(enemyGO);
            }
        }

        private void TestOnMeCommand()
        {
            Debug.Log("\n--- On Me Command Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject allyGO = null;

            try
            {
                managerGO = new GameObject("Manager");
                var manager = managerGO.AddComponent<QuickCommandManager>();

                playerGO = new GameObject("Player");
                allyGO = new GameObject("Ally");

                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f);
                var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 100f);

                manager.Initialize(player, new[] { ally });

                // Issue On Me command
                bool success = manager.IssueCommand(QuickCommandType.ON_ME, ally);
                Assert(success, "On Me command issued");

                var command = manager.GetActiveCommand(ally);
                Assert(command != null, "On Me command is active");
                AssertEqual(QuickCommandType.ON_ME, command.commandType, "Command type is ON_ME");
                AssertEqual(player, command.targetUnit, "Target is player");

                // Duration should be longer for On Me (complex behavior chain)
                float duration = QuickCommandInstance.GetEstimatedDuration(QuickCommandType.ON_ME);
                Assert(duration >= 8f, "On Me has longer duration for behavior chain");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (allyGO != null) DestroyImmediate(allyGO);
            }
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        private Combatant CreateTestCombatant(GameObject go, string name, Brand brand, float hpPercent)
        {
            var combatant = go.AddComponent<Combatant>();
            // Initialize(id, name, brand, maxHp, maxMp, atk, def, mag, res, spd, isPlayer)
            combatant.Initialize(name, name, brand, 100, 50, 10, 10, 10, 10, 10, false);

            // Set HP to percentage
            int targetHp = Mathf.RoundToInt((hpPercent / 100f) * combatant.MaxHp);
            int damage = combatant.MaxHp - targetHp;
            if (damage > 0)
            {
                combatant.TakeDamage(damage);
            }

            return combatant;
        }
    }
}

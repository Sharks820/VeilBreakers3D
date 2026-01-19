using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Data;
using VeilBreakers.Managers;
using VeilBreakers.Systems;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Comprehensive test script for the Status Effects system.
    /// Attach to a GameObject and call RunAllTests() or use the Inspector button.
    /// </summary>
    public class StatusEffectTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private bool _cleanupAfterTests = true;

        [Header("Test Results")]
        [SerializeField] private int _testsPassed;
        [SerializeField] private int _testsFailed;
        [SerializeField] private List<string> _failedTests = new List<string>();

        // Test objects
        private GameObject _testSource;
        private GameObject _testTarget;
        private StatusEffectManager _manager;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private async void Start()
        {
            if (_runOnStart)
            {
                try
                {
                    await Task.Delay(500); // Wait for managers to initialize
                    await RunAllTestsAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        [ContextMenu("Run All Tests")]
        public async void RunAllTests()
        {
            await RunAllTestsAsync();
        }

        public async Task RunAllTestsAsync()
        {
            _testsPassed = 0;
            _testsFailed = 0;
            _failedTests.Clear();

            Debug.Log("=== STATUS EFFECT TESTS STARTING ===");

            // Setup test environment
            SetupTestEnvironment();

            // Run tests in order
            await Test_EffectInstance_Create();
            await Test_EffectInstance_Timer();
            await Test_Manager_ApplyEffect();
            await Test_Manager_NoStackRule();
            await Test_Manager_RefreshOnReapply();
            await Test_Manager_RemoveEffect();
            await Test_Manager_Cleanse();
            await Test_Manager_Dispel();
            await Test_Manager_ShieldAbsorption();
            await Test_Manager_BreakOnDamage();
            await Test_Manager_Immunity();
            await Test_Manager_Exhausted();
            await Test_Manager_CanAct();
            await Test_Manager_StatModifiers();
            await Test_Manager_EffectCount();

            // Report results
            Debug.Log("=== STATUS EFFECT TESTS COMPLETE ===");
            Debug.Log($"Passed: {_testsPassed} | Failed: {_testsFailed}");

            if (_testsFailed > 0)
            {
                Debug.LogError("FAILED TESTS:");
                foreach (var test in _failedTests)
                {
                    Debug.LogError($"  - {test}");
                }
            }

            // Cleanup
            if (_cleanupAfterTests)
            {
                CleanupTestEnvironment();
            }
        }

        // =============================================================================
        // SETUP / CLEANUP
        // =============================================================================

        private void SetupTestEnvironment()
        {
            // Create test GameObjects
            _testSource = new GameObject("TestSource");
            _testTarget = new GameObject("TestTarget");

            // Ensure StatusEffectManager exists
            if (StatusEffectManager.Instance == null)
            {
                var go = new GameObject("StatusEffectManager");
                _manager = go.AddComponent<StatusEffectManager>();
            }
            else
            {
                _manager = StatusEffectManager.Instance;
            }
        }

        private void CleanupTestEnvironment()
        {
            // Cleanup test objects
            if (_testSource != null) DestroyImmediate(_testSource);
            if (_testTarget != null) DestroyImmediate(_testTarget);

            // Clear effects
            _manager?.ClearAllEffects();

            Debug.Log("[Test] Cleanup complete");
        }

        // =============================================================================
        // INDIVIDUAL TESTS
        // =============================================================================

        private async Task Test_EffectInstance_Create()
        {
            string testName = "EffectInstance.Create";
            try
            {
                var data = CreateTestEffectData(StatusEffectType.POISON, EffectCategory.DAMAGE, Brand.VENOM);
                data.baseValue = 10f;
                data.baseDuration = 5f;

                var instance = StatusEffectInstance.Create(data, _testSource, _testTarget, 0.5f, 1.5f, 2.0f);

                Assert(instance != null, "Instance should not be null");
                Assert(instance.EffectType == StatusEffectType.POISON, "Effect type should match");
                Assert(instance.Category == EffectCategory.DAMAGE, "Category should match");
                Assert(!instance.IsExpired, "Should not be expired initially");
                Assert(instance.stacks == 1, "Should start with 1 stack");

                // Check potency calculation: Base * (1 + StatMod) * SkillRank * BrandEff
                // 10 * (1 + 0.5) * 1.5 * 2.0 = 10 * 1.5 * 1.5 * 2.0 = 45
                float expectedPotency = 10f * (1f + 0.5f) * 1.5f * 2.0f;
                Assert(Mathf.Approximately(instance.potency, expectedPotency), $"Potency should be {expectedPotency}, got {instance.potency}");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_EffectInstance_Timer()
        {
            string testName = "EffectInstance.Timer";
            try
            {
                var data = CreateTestEffectData(StatusEffectType.BURN, EffectCategory.DAMAGE, Brand.RUIN);
                data.baseDuration = 3f;
                data.tickInterval = 1f;

                var instance = StatusEffectInstance.Create(data, _testSource, _testTarget);

                Assert(instance.remainingTime == 3f, "Should start with full duration");

                // Simulate 1 second
                bool ticked = instance.UpdateTimer(1f);
                Assert(ticked, "Should tick after 1 second");
                Assert(Mathf.Approximately(instance.remainingTime, 2f), "Should have 2 seconds remaining");

                // Simulate another second (not quite at tick)
                instance.UpdateTimer(0.5f);
                Assert(Mathf.Approximately(instance.remainingTime, 1.5f), "Should have 1.5 seconds remaining");

                // Expire
                instance.UpdateTimer(2f);
                Assert(instance.IsExpired, "Should be expired");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_ApplyEffect()
        {
            string testName = "Manager.ApplyEffect";
            try
            {
                _manager.ClearAllEffects();

                var data = CreateTestEffectData(StatusEffectType.REGEN, EffectCategory.BUFF, Brand.GRACE);
                _manager.RegisterEffectData(data);

                var instance = _manager.ApplyEffect(data, _testSource, _testTarget);

                Assert(instance != null, "Should return instance on success");
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.REGEN), "Target should have effect");
                Assert(_manager.GetEffectCount(_testTarget) == 1, "Target should have 1 effect");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_NoStackRule()
        {
            string testName = "Manager.NoStackRule";
            try
            {
                _manager.ClearAllEffects();

                var data = CreateTestEffectData(StatusEffectType.STUN, EffectCategory.CONTROL, Brand.DREAD);
                data.canStack = false;
                data.refreshOnReapply = false;

                // Apply first
                var first = _manager.ApplyEffect(data, _testSource, _testTarget);
                Assert(first != null, "First application should succeed");

                // Try to apply again - should be blocked
                var second = _manager.ApplyEffect(data, _testSource, _testTarget);
                Assert(second == null, "Second application should be blocked (no-stack rule)");
                Assert(_manager.GetEffectCount(_testTarget) == 1, "Should still have only 1 effect");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_RefreshOnReapply()
        {
            string testName = "Manager.RefreshOnReapply";
            try
            {
                _manager.ClearAllEffects();

                var data = CreateTestEffectData(StatusEffectType.SLOW, EffectCategory.CONTROL, Brand.SURGE);
                data.canStack = false;
                data.refreshOnReapply = true;
                data.baseDuration = 10f;

                var first = _manager.ApplyEffect(data, _testSource, _testTarget);

                // Simulate time passing
                first.UpdateTimer(5f);
                Assert(Mathf.Approximately(first.remainingTime, 5f), "Should have 5 seconds remaining");

                // Reapply - should refresh duration
                var refreshed = _manager.ApplyEffect(data, _testSource, _testTarget);
                Assert(refreshed == first, "Should return same instance");
                Assert(Mathf.Approximately(refreshed.remainingTime, 10f), "Duration should be refreshed");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_RemoveEffect()
        {
            string testName = "Manager.RemoveEffect";
            try
            {
                _manager.ClearAllEffects();

                var data = CreateTestEffectData(StatusEffectType.ATTACK_UP, EffectCategory.BUFF, Brand.SAVAGE);
                _manager.ApplyEffect(data, _testSource, _testTarget);

                Assert(_manager.HasEffect(_testTarget, StatusEffectType.ATTACK_UP), "Should have effect");

                int removed = _manager.RemoveEffect(_testTarget, StatusEffectType.ATTACK_UP);
                Assert(removed == 1, "Should remove 1 effect");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.ATTACK_UP), "Should not have effect");

                DestroyImmediate(data);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_Cleanse()
        {
            string testName = "Manager.Cleanse";
            try
            {
                _manager.ClearAllEffects();

                // Apply multiple debuffs
                var poison = CreateTestEffectData(StatusEffectType.POISON, EffectCategory.DAMAGE, Brand.VENOM);
                poison.cleansePriority = 70;
                var stun = CreateTestEffectData(StatusEffectType.STUN, EffectCategory.CONTROL, Brand.DREAD);
                stun.cleansePriority = 80;
                var slow = CreateTestEffectData(StatusEffectType.SLOW, EffectCategory.CONTROL, Brand.SURGE);
                slow.cleansePriority = 50;

                _manager.ApplyEffect(poison, _testSource, _testTarget);
                _manager.ApplyEffect(stun, _testSource, _testTarget);
                _manager.ApplyEffect(slow, _testSource, _testTarget);

                Assert(_manager.GetEffectCount(_testTarget) == 3, "Should have 3 effects");

                // Cleanse 1 (should remove highest priority - stun)
                int cleansed = _manager.Cleanse(_testTarget, 1);
                Assert(cleansed == 1, "Should cleanse 1");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.STUN), "Stun should be removed (highest priority)");
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.POISON), "Poison should remain");

                DestroyImmediate(poison);
                DestroyImmediate(stun);
                DestroyImmediate(slow);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_Dispel()
        {
            string testName = "Manager.Dispel";
            try
            {
                _manager.ClearAllEffects();

                // Apply buffs
                var shield = CreateTestEffectData(StatusEffectType.SHIELD, EffectCategory.BUFF, Brand.MEND);
                var regen = CreateTestEffectData(StatusEffectType.REGEN, EffectCategory.BUFF, Brand.GRACE);

                _manager.ApplyEffect(shield, _testSource, _testTarget);
                _manager.ApplyEffect(regen, _testSource, _testTarget);

                Assert(_manager.GetEffectCount(_testTarget) == 2, "Should have 2 buffs");

                int dispelled = _manager.Dispel(_testTarget, 1);
                Assert(dispelled == 1, "Should dispel 1");
                Assert(_manager.GetEffectCount(_testTarget) == 1, "Should have 1 buff remaining");

                DestroyImmediate(shield);
                DestroyImmediate(regen);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_ShieldAbsorption()
        {
            string testName = "Manager.ShieldAbsorption";
            try
            {
                _manager.ClearAllEffects();

                var shield = CreateTestEffectData(StatusEffectType.SHIELD, EffectCategory.BUFF, Brand.MEND);
                shield.baseValue = 100f; // 100 HP shield

                var instance = _manager.ApplyEffect(shield, _testSource, _testTarget);
                Assert(instance.shieldRemaining == 100f, "Shield should have 100 HP");

                // Take 30 damage
                float remaining = _manager.ProcessDamage(_testTarget, 30f, out float absorbed);
                Assert(Mathf.Approximately(absorbed, 30f), "Should absorb 30 damage");
                Assert(Mathf.Approximately(remaining, 0f), "Should have 0 remaining damage");
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.SHIELD), "Shield should still exist");

                // Take 100 damage (breaks shield)
                remaining = _manager.ProcessDamage(_testTarget, 100f, out absorbed);
                Assert(Mathf.Approximately(absorbed, 70f), "Should absorb remaining 70");
                Assert(Mathf.Approximately(remaining, 30f), "Should have 30 remaining damage");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.SHIELD), "Shield should be consumed");

                DestroyImmediate(shield);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_BreakOnDamage()
        {
            string testName = "Manager.BreakOnDamage";
            try
            {
                _manager.ClearAllEffects();

                var sleep = CreateTestEffectData(StatusEffectType.SLEEP, EffectCategory.CONTROL, Brand.DREAD);
                sleep.breaksOnDamage = true;

                _manager.ApplyEffect(sleep, _testSource, _testTarget);
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.SLEEP), "Should have Sleep");

                // Take damage - should break sleep
                _manager.ProcessDamage(_testTarget, 10f, out _);
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.SLEEP), "Sleep should be broken by damage");

                DestroyImmediate(sleep);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_Immunity()
        {
            string testName = "Manager.Immunity";
            try
            {
                _manager.ClearAllEffects();

                var immunity = CreateTestEffectData(StatusEffectType.IMMUNITY, EffectCategory.BUFF, Brand.GRACE);
                var poison = CreateTestEffectData(StatusEffectType.POISON, EffectCategory.DAMAGE, Brand.VENOM);

                _manager.ApplyEffect(immunity, _testSource, _testTarget);
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.IMMUNITY), "Should have Immunity");

                // Try to apply poison - should be blocked and consume immunity
                var result = _manager.ApplyEffect(poison, _testSource, _testTarget);
                Assert(result == null, "Poison should be blocked");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.POISON), "Should not have Poison");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.IMMUNITY), "Immunity should be consumed");

                DestroyImmediate(immunity);
                DestroyImmediate(poison);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_Exhausted()
        {
            string testName = "Manager.Exhausted";
            try
            {
                _manager.ClearAllEffects();

                var exhausted = CreateTestEffectData(StatusEffectType.EXHAUSTED, EffectCategory.DEBUFF, Brand.RUIN);
                var attackUp = CreateTestEffectData(StatusEffectType.ATTACK_UP, EffectCategory.BUFF, Brand.SAVAGE);

                _manager.ApplyEffect(exhausted, _testSource, _testTarget);
                Assert(_manager.HasEffect(_testTarget, StatusEffectType.EXHAUSTED), "Should have Exhausted");

                // Try to apply buff - should be blocked
                var result = _manager.ApplyEffect(attackUp, _testSource, _testTarget);
                Assert(result == null, "Buff should be blocked when Exhausted");
                Assert(!_manager.HasEffect(_testTarget, StatusEffectType.ATTACK_UP), "Should not have Attack Up");

                DestroyImmediate(exhausted);
                DestroyImmediate(attackUp);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_CanAct()
        {
            string testName = "Manager.CanAct";
            try
            {
                _manager.ClearAllEffects();

                Assert(_manager.CanAct(_testTarget), "Should be able to act with no effects");

                var stun = CreateTestEffectData(StatusEffectType.STUN, EffectCategory.CONTROL, Brand.DREAD);
                _manager.ApplyEffect(stun, _testSource, _testTarget);

                Assert(!_manager.CanAct(_testTarget), "Should not be able to act when Stunned");

                _manager.RemoveEffect(_testTarget, StatusEffectType.STUN);
                Assert(_manager.CanAct(_testTarget), "Should be able to act after Stun removed");

                DestroyImmediate(stun);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_StatModifiers()
        {
            string testName = "Manager.StatModifiers";
            try
            {
                _manager.ClearAllEffects();

                var attackUp = CreateTestEffectData(StatusEffectType.ATTACK_UP, EffectCategory.BUFF, Brand.SAVAGE);
                attackUp.targetStat = Stat.ATTACK;
                attackUp.baseValue = 20f;
                attackUp.isPercentage = true;

                var attackDown = CreateTestEffectData(StatusEffectType.ATTACK_DOWN, EffectCategory.DEBUFF, Brand.DREAD);
                attackDown.targetStat = Stat.ATTACK;
                attackDown.baseValue = 10f;
                attackDown.isPercentage = true;

                _manager.ApplyEffect(attackUp, _testSource, _testTarget);
                float mod = _manager.GetStatModifier(_testTarget, Stat.ATTACK);
                Assert(mod > 0, "Attack modifier should be positive with Attack Up");

                _manager.ApplyEffect(attackDown, _testSource, _testTarget);
                float combinedMod = _manager.GetStatModifier(_testTarget, Stat.ATTACK);
                Assert(combinedMod < mod, "Combined modifier should be less than buff alone");

                DestroyImmediate(attackUp);
                DestroyImmediate(attackDown);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_Manager_EffectCount()
        {
            string testName = "Manager.EffectCount";
            try
            {
                _manager.ClearAllEffects();

                Assert(_manager.GetEffectCount(_testTarget) == 0, "Should start with 0 effects");

                var poison = CreateTestEffectData(StatusEffectType.POISON, EffectCategory.DAMAGE, Brand.VENOM);
                var burn = CreateTestEffectData(StatusEffectType.BURN, EffectCategory.DAMAGE, Brand.RUIN);
                var shield = CreateTestEffectData(StatusEffectType.SHIELD, EffectCategory.BUFF, Brand.MEND);

                _manager.ApplyEffect(poison, _testSource, _testTarget);
                Assert(_manager.GetEffectCount(_testTarget) == 1, "Should have 1 effect");

                _manager.ApplyEffect(burn, _testSource, _testTarget);
                _manager.ApplyEffect(shield, _testSource, _testTarget);
                Assert(_manager.GetEffectCount(_testTarget) == 3, "Should have 3 effects");

                Assert(_manager.HasEffectCategory(_testTarget, EffectCategory.DAMAGE), "Should have damage effects");
                Assert(_manager.HasEffectCategory(_testTarget, EffectCategory.BUFF), "Should have buff effects");
                Assert(!_manager.HasEffectCategory(_testTarget, EffectCategory.CONTROL), "Should not have control effects");

                DestroyImmediate(poison);
                DestroyImmediate(burn);
                DestroyImmediate(shield);
                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private StatusEffectData CreateTestEffectData(StatusEffectType type, EffectCategory category, Brand brand)
        {
            var data = ScriptableObject.CreateInstance<StatusEffectData>();
            data.effectId = $"test_{type.ToString().ToLower()}";
            data.displayName = type.ToString();
            data.effectType = type;
            data.category = category;
            data.sourceBrand = brand;
            data.baseDuration = 10f;
            data.baseValue = 10f;
            data.durationTier = DurationTier.MEDIUM;
            data.canStack = false;
            data.refreshOnReapply = true;

            return data;
        }

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        private void Pass(string testName)
        {
            _testsPassed++;
            Debug.Log($"<color=green>[PASS]</color> {testName}");
        }

        private void Fail(string testName, string error)
        {
            _testsFailed++;
            _failedTests.Add($"{testName}: {error}");
            Debug.LogError($"<color=red>[FAIL]</color> {testName}: {error}");
        }
    }
}

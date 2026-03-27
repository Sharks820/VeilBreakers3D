using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Comprehensive test script for combat system verification
    /// Tests all 10 brands, synergy tiers, damage calculations, and cooldowns
    /// </summary>
    public class CombatTestSetup : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private Path _testChampionPath = Path.IRONBOUND;
        [SerializeField] private bool _runTestsOnStart = true;

        private int _passedTests = 0;
        private int _failedTests = 0;

        private void Start()
        {
            if (_runTestsOnStart)
            {
                RunAllTests();
            }
        }

        public void RunAllTests()
        {
            _passedTests = 0;
            _failedTests = 0;

            Debug.Log("========================================");
            Debug.Log("= VEILBREAKERS COMBAT SYSTEM TESTS =");
            Debug.Log("========================================\n");

            TestBrandEffectiveness();
            TestSynergySystem();
            TestDamageCalculation();
            TestAbilityLoadout();
            TestCombatantLifecycle();

            Debug.Log("\n========================================");
            Debug.Log($"= TEST RESULTS: {_passedTests} PASSED, {_failedTests} FAILED =");
            Debug.Log("========================================");

            if (_failedTests == 0)
            {
                Debug.Log("<color=green>ALL TESTS PASSED!</color>");
            }
            else
            {
                Debug.LogError($"<color=red>{_failedTests} TESTS FAILED!</color>");
            }
        }

        // ========================================================================
        // BRAND EFFECTIVENESS TESTS
        // ========================================================================

        private void TestBrandEffectiveness()
        {
            Debug.Log("\n--- BRAND EFFECTIVENESS TESTS ---");

            // Test IRON effectiveness (strong vs SURGE, DREAD; weak vs SAVAGE, RUIN)
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE), 2.0f, "IRON vs SURGE = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.DREAD), 2.0f, "IRON vs DREAD = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.SAVAGE), 0.5f, "IRON vs SAVAGE = 0.5x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.RUIN), 0.5f, "IRON vs RUIN = 0.5x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.GRACE), 1.0f, "IRON vs GRACE = 1x (neutral)");

            // Test SAVAGE effectiveness (strong vs IRON, MEND; weak vs LEECH, GRACE)
            AssertEqual(BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.IRON), 2.0f, "SAVAGE vs IRON = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.MEND), 2.0f, "SAVAGE vs MEND = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.LEECH), 0.5f, "SAVAGE vs LEECH = 0.5x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.GRACE), 0.5f, "SAVAGE vs GRACE = 0.5x");

            // Test new brands (GRACE, MEND, RUIN, VOID)
            AssertEqual(BrandSystem.GetEffectiveness(Brand.GRACE, Brand.VOID), 2.0f, "GRACE vs VOID = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.GRACE, Brand.RUIN), 2.0f, "GRACE vs RUIN = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.MEND, Brand.VOID), 2.0f, "MEND vs VOID = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.MEND, Brand.LEECH), 2.0f, "MEND vs LEECH = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.RUIN, Brand.IRON), 2.0f, "RUIN vs IRON = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.RUIN, Brand.VENOM), 2.0f, "RUIN vs VENOM = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.VOID, Brand.SURGE), 2.0f, "VOID vs SURGE = 2x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.VOID, Brand.DREAD), 2.0f, "VOID vs DREAD = 2x");

            // Test NONE brand
            AssertEqual(BrandSystem.GetEffectiveness(Brand.NONE, Brand.IRON), 1.0f, "NONE vs IRON = 1x");
            AssertEqual(BrandSystem.GetEffectiveness(Brand.IRON, Brand.NONE), 1.0f, "IRON vs NONE = 1x");

            // Test helper functions
            AssertTrue(BrandSystem.HasAdvantage(Brand.IRON, Brand.SURGE), "HasAdvantage: IRON vs SURGE");
            AssertTrue(BrandSystem.HasDisadvantage(Brand.IRON, Brand.SAVAGE), "HasDisadvantage: IRON vs SAVAGE");
            AssertFalse(BrandSystem.HasAdvantage(Brand.IRON, Brand.GRACE), "HasAdvantage false: IRON vs GRACE");
        }

        // ========================================================================
        // SYNERGY SYSTEM TESTS
        // ========================================================================

        private void TestSynergySystem()
        {
            Debug.Log("\n--- SYNERGY SYSTEM TESTS ---");

            // Test FULL synergy (3/3 matching brands)
            var fullSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            AssertEqual((int)fullSynergy, (int)SynergySystem.SynergyTier.FULL, "IRONBOUND + IRON/MEND/LEECH = FULL");

            // Test PARTIAL synergy (2/3 matching brands)
            var partialSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.GRACE }  // GRACE is neutral
            );
            AssertEqual((int)partialSynergy, (int)SynergySystem.SynergyTier.PARTIAL, "IRONBOUND + IRON/MEND/GRACE = PARTIAL");

            // Test NEUTRAL synergy (1/3 or 0/3 matching brands)
            var neutralSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.GRACE, Brand.SURGE }  // Only IRON matches
            );
            AssertEqual((int)neutralSynergy, (int)SynergySystem.SynergyTier.NEUTRAL, "IRONBOUND + IRON/GRACE/SURGE = NEUTRAL");

            // Test ANTI synergy (weak brand present)
            var antiSynergy = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.VOID, Brand.GRACE, Brand.DREAD }  // VOID is weak for IRONBOUND
            );
            AssertEqual((int)antiSynergy, (int)SynergySystem.SynergyTier.ANTI, "IRONBOUND + VOID/GRACE/DREAD = ANTI");

            // Test UNCHAINED path (always neutral)
            var unchainedSynergy = SynergySystem.GetSynergyTier(
                Path.UNCHAINED,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            AssertEqual((int)unchainedSynergy, (int)SynergySystem.SynergyTier.NEUTRAL, "UNCHAINED is always NEUTRAL");

            // Test FANGBORN synergy
            var fangbornFull = SynergySystem.GetSynergyTier(
                Path.FANGBORN,
                new[] { Brand.SAVAGE, Brand.VENOM, Brand.RUIN }
            );
            AssertEqual((int)fangbornFull, (int)SynergySystem.SynergyTier.FULL, "FANGBORN + SAVAGE/VENOM/RUIN = FULL");

            // Test VOIDTOUCHED synergy
            var voidtouchedFull = SynergySystem.GetSynergyTier(
                Path.VOIDTOUCHED,
                new[] { Brand.VOID, Brand.DREAD, Brand.SURGE }
            );
            AssertEqual((int)voidtouchedFull, (int)SynergySystem.SynergyTier.FULL, "VOIDTOUCHED + VOID/DREAD/SURGE = FULL");

            // Test synergy bonuses
            AssertEqual(SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.FULL), 1.08f, "FULL damage bonus = 1.08 (+8%)");
            AssertEqual(SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.PARTIAL), 1.05f, "PARTIAL damage bonus = 1.05 (+5%)");
            AssertEqual(SynergySystem.GetDefenseBonus(SynergySystem.SynergyTier.FULL), 1.08f, "FULL defense bonus = 1.08 (+8%)");
            AssertEqual(SynergySystem.GetDefenseBonus(SynergySystem.SynergyTier.PARTIAL), 1.05f, "PARTIAL defense bonus = 1.05 (+5%)");

            // Test corruption multipliers
            AssertEqual(SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.FULL), 0.5f, "FULL corruption rate = 0.5x");
            AssertEqual(SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.PARTIAL), 0.75f, "PARTIAL corruption rate = 0.75x");
            AssertEqual(SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.ANTI), 1.5f, "ANTI corruption rate = 1.5x");

            // Test combo unlock
            AssertTrue(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.FULL), "Combo unlocked at FULL");
            AssertFalse(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.PARTIAL), "Combo NOT unlocked at PARTIAL");
        }

        // ========================================================================
        // DAMAGE CALCULATION TESTS
        // ========================================================================

        private void TestDamageCalculation()
        {
            Debug.Log("\n--- DAMAGE CALCULATION TESTS ---");

            // Create test combatants
            var attackerGO = new GameObject("TestAttacker");
            var defenderGO = new GameObject("TestDefender");

            var attacker = attackerGO.AddComponent<Combatant>();
            attacker.Initialize("test_attacker", "Test Attacker", Brand.IRON, 100, 50, 20, 10, 10, 10, 10, true);

            var defender = defenderGO.AddComponent<Combatant>();
            defender.Initialize("test_defender", "Test Defender", Brand.SURGE, 100, 50, 10, 15, 10, 10, 10, false);

            // Test damage with super effective (IRON vs SURGE = 2x)
            var result = DamageCalculator.Calculate(
                attacker, defender,
                basePower: 50,
                DamageType.PHYSICAL,
                SynergySystem.SynergyTier.NEUTRAL
            );

            AssertTrue(result.brandMultiplier == 2.0f, "Brand multiplier = 2x for IRON vs SURGE");
            AssertTrue(result.finalDamage > 0, "Final damage > 0");
            Debug.Log($"  Damage result: {result.finalDamage} (brand: {result.brandMultiplier}x, synergy: {result.synergyMultiplier}x, crit: {result.isCritical})");

            // Test damage with synergy bonus
            var resultWithSynergy = DamageCalculator.Calculate(
                attacker, defender,
                basePower: 50,
                DamageType.PHYSICAL,
                SynergySystem.SynergyTier.FULL
            );
            AssertTrue(resultWithSynergy.synergyMultiplier == 1.08f, "Synergy multiplier = 1.08 at FULL");

            // Test not effective damage (reverse attacker/defender brands conceptually)
            var weakAttacker = attackerGO.AddComponent<Combatant>();
            weakAttacker.Initialize("weak_attacker", "Weak Attacker", Brand.SURGE, 100, 50, 20, 10, 10, 10, 10, true);

            var strongDefender = defenderGO.AddComponent<Combatant>();
            strongDefender.Initialize("strong_defender", "Strong Defender", Brand.IRON, 100, 50, 10, 15, 10, 10, 10, false);

            var weakResult = DamageCalculator.Calculate(
                weakAttacker, strongDefender,
                basePower: 50,
                DamageType.PHYSICAL,
                SynergySystem.SynergyTier.NEUTRAL
            );
            AssertTrue(weakResult.brandMultiplier == 0.5f, "Brand multiplier = 0.5x for SURGE vs IRON");
            Debug.Log($"  Weak result: {weakResult.finalDamage} (brand: {weakResult.brandMultiplier}x)");

            // Test TRUE damage (ignores defense)
            var trueResult = DamageCalculator.Calculate(
                attacker, defender,
                basePower: 50,
                DamageType.TRUE,
                SynergySystem.SynergyTier.NEUTRAL
            );
            Debug.Log($"  TRUE damage result: {trueResult.finalDamage}");

            // Test healing calculation
            int healAmount = DamageCalculator.CalculateHeal(attacker, 30);
            AssertTrue(healAmount > 0, "Heal amount > 0");
            Debug.Log($"  Heal result: {healAmount}");

            // Cleanup
            Destroy(attackerGO);
            Destroy(defenderGO);
        }

        // ========================================================================
        // ABILITY LOADOUT TESTS
        // ========================================================================

        private void TestAbilityLoadout()
        {
            Debug.Log("\n--- ABILITY LOADOUT TESTS ---");

            // Test ability creation
            var loadout = AbilityLoadout.CreateFromSkills(
                "basic_attack",
                "skill_slash",
                "skill_power_strike",
                "skill_whirlwind",
                "ultimate_devastation"
            );

            AssertTrue(loadout.basicAttack != null, "Basic attack created");
            AssertTrue(loadout.defend != null, "Defend created");
            AssertTrue(loadout.skill1 != null, "Skill 1 created");
            AssertTrue(loadout.skill2 != null, "Skill 2 created");
            AssertTrue(loadout.skill3 != null, "Skill 3 created");
            AssertTrue(loadout.ultimate != null, "Ultimate created");

            // Test ability slot retrieval
            AssertTrue(loadout.GetAbility(AbilitySlot.BASIC_ATTACK) == loadout.basicAttack, "GetAbility(BASIC_ATTACK) returns basicAttack");
            AssertTrue(loadout.GetAbility(AbilitySlot.SKILL_1) == loadout.skill1, "GetAbility(SKILL_1) returns skill1");
            AssertTrue(loadout.GetAbility(AbilitySlot.ULTIMATE) == loadout.ultimate, "GetAbility(ULTIMATE) returns ultimate");

            // Test cooldowns
            AssertTrue(loadout.basicAttack.maxCooldown == 0f, "Basic attack has 0 cooldown");
            AssertTrue(loadout.defend.maxCooldown == 0f, "Defend has 0 cooldown");
            AssertTrue(loadout.skill1.maxCooldown == 5f, "Skill 1 has 5s cooldown");
            AssertTrue(loadout.skill2.maxCooldown == 12f, "Skill 2 has 12s cooldown");
            AssertTrue(loadout.skill3.maxCooldown == 20f, "Skill 3 has 20s cooldown");
            AssertTrue(loadout.ultimate.maxCooldown == 60f, "Ultimate has 60s cooldown");

            // Test cooldown trigger and update
            loadout.skill1.TriggerCooldown();
            AssertTrue(loadout.skill1.cooldownRemaining == 5f, "Skill 1 cooldown triggered to 5s");
            AssertFalse(loadout.skill1.isReady, "Skill 1 is NOT ready after trigger");

            loadout.skill1.UpdateCooldown(3f);
            AssertTrue(Mathf.Approximately(loadout.skill1.cooldownRemaining, 2f), "Skill 1 cooldown reduced to 2s");

            loadout.skill1.UpdateCooldown(5f);
            AssertTrue(loadout.skill1.cooldownRemaining == 0f, "Skill 1 cooldown at 0");
            AssertTrue(loadout.skill1.isReady, "Skill 1 is ready after cooldown");

            // Test batch cooldown update
            loadout.skill2.TriggerCooldown();
            loadout.ultimate.TriggerCooldown();
            loadout.UpdateAllCooldowns(10f);
            AssertTrue(Mathf.Approximately(loadout.skill2.cooldownRemaining, 2f), "Skill 2 reduced by UpdateAllCooldowns");
            AssertTrue(Mathf.Approximately(loadout.ultimate.cooldownRemaining, 50f), "Ultimate reduced by UpdateAllCooldowns");
        }

        // ========================================================================
        // COMBATANT LIFECYCLE TESTS
        // ========================================================================

        private void TestCombatantLifecycle()
        {
            Debug.Log("\n--- COMBATANT LIFECYCLE TESTS ---");

            var go = new GameObject("TestCombatant");
            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize("test", "Test Monster", Brand.IRON, 100, 50, 15, 12, 8, 10, 10, true);

            // Test initial state
            AssertTrue(combatant.IsAlive, "Combatant starts alive");
            AssertTrue(combatant.CurrentHp == 100, "Combatant starts at max HP");
            AssertTrue(combatant.CurrentMp == 50, "Combatant starts at max MP");
            AssertTrue(combatant.Brand == Brand.IRON, "Brand is IRON");

            // Test taking damage
            combatant.TakeDamage(30);
            AssertTrue(combatant.CurrentHp == 70, "HP reduced to 70 after 30 damage");

            // Test healing
            combatant.Heal(20);
            AssertTrue(combatant.CurrentHp == 90, "HP increased to 90 after 20 heal");

            // Test overheal cap
            combatant.Heal(50);
            AssertTrue(combatant.CurrentHp == 100, "HP capped at max (100)");

            // Test MP usage
            bool usedMp = combatant.UseMp(20);
            AssertTrue(usedMp, "Successfully used 20 MP");
            AssertTrue(combatant.CurrentMp == 30, "MP reduced to 30");

            // Test MP insufficient
            bool failedMp = combatant.UseMp(50);
            AssertFalse(failedMp, "Cannot use 50 MP when only 30 available");

            // Test MP restore
            combatant.RestoreMp(15);
            AssertTrue(combatant.CurrentMp == 45, "MP restored to 45");

            // Test defending
            combatant.StartDefend(DefenseAction.DEFEND_SELF);
            AssertTrue(combatant.IsDefending, "Combatant is defending");

            // Test damage reduction while defending (50%)
            combatant.TakeDamage(40);
            AssertTrue(combatant.CurrentHp == 80, "Only 20 damage taken while defending (50% reduction)");

            combatant.StopDefend();
            AssertFalse(combatant.IsDefending, "Combatant stopped defending");

            // Test death
            combatant.TakeDamage(80);
            AssertFalse(combatant.IsAlive, "Combatant died at 0 HP");
            AssertTrue(combatant.CurrentHp == 0, "HP is 0");

            // Test revive
            combatant.Revive(0.5f);
            AssertTrue(combatant.IsAlive, "Combatant revived");
            AssertTrue(combatant.CurrentHp == 50, "Revived at 50% HP");

            // Cleanup
            Destroy(go);
        }

        // ========================================================================
        // ASSERTION HELPERS
        // ========================================================================

        private void AssertEqual(float actual, float expected, string testName)
        {
            bool passed = Mathf.Approximately(actual, expected);
            LogResult(passed, testName, $"Expected {expected}, got {actual}");
        }

        private void AssertEqual(int actual, int expected, string testName)
        {
            bool passed = actual == expected;
            LogResult(passed, testName, $"Expected {expected}, got {actual}");
        }

        private void AssertTrue(bool condition, string testName)
        {
            LogResult(condition, testName, "Expected true, got false");
        }

        private void AssertFalse(bool condition, string testName)
        {
            LogResult(!condition, testName, "Expected false, got true");
        }

        private void LogResult(bool passed, string testName, string failureDetails)
        {
            if (passed)
            {
                _passedTests++;
                Debug.Log($"  <color=green>[PASS]</color> {testName}");
            }
            else
            {
                _failedTests++;
                Debug.LogError($"  <color=red>[FAIL]</color> {testName} - {failureDetails}");
            }
        }
    }
}

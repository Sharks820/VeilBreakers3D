using System;
using System.Collections;
using UnityEngine;
using VeilBreakers.Capture;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test suite for the Capture System.
    /// Validates bind thresholds, capture formulas, QTE, and full capture flow.
    /// </summary>
    public class CaptureTests : MonoBehaviour
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
                Debug.LogError($"[CaptureTests] Exception during tests: {ex.Message}\n{ex.StackTrace}");
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
            Debug.Log("[CaptureTests] Starting Capture System Test Suite");
            Debug.Log("========================================");

            // Data Tests
            TestCaptureItemEffectiveness();
            TestCaptureItemDisplayInfo();

            // Bind Threshold Tests
            TestBaseBindThreshold();
            TestCorruptionThresholdModifier();
            TestRarityThresholdModifier();
            TestIntimidation();

            // Capture Formula Tests
            TestBaseCaptureFormula();
            TestItemEffectivenessByRarity();
            TestCorruptionCaptureModifier();
            TestLevelModifier();
            TestQTEModifier();

            // Failure Outcome Tests
            TestFailureOutcomeWeighting();

            // Manager Tests
            TestCaptureManagerInitialization();
            TestMarkingSystem();

            // QTE Tests
            TestQTEZones();

            // Integration Tests
            TestFullCaptureFlow();

            // Summary
            Debug.Log("========================================");
            Debug.Log($"[CaptureTests] COMPLETE: {_passCount} passed, {_failCount} failed");
            Debug.Log("========================================");

            if (_failCount > 0)
            {
                Debug.LogError($"[CaptureTests] {_failCount} TEST(S) FAILED!");
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
                Debug.LogError($"[FAIL] {testName}{(string.IsNullOrEmpty(details) ? "" : $" - {details}")}");
            }
        }

        private void AssertApprox(float expected, float actual, float tolerance, string testName)
        {
            bool pass = Mathf.Abs(expected - actual) <= tolerance;
            Assert(pass, testName, $"Expected {expected}, got {actual}");
        }

        private void AssertRange(float value, float min, float max, string testName)
        {
            bool pass = value >= min && value <= max;
            Assert(pass, testName, $"Expected {min}-{max}, got {value}");
        }

        // =============================================================================
        // DATA TESTS
        // =============================================================================

        private void TestCaptureItemEffectiveness()
        {
            Debug.Log("\n--- Capture Item Effectiveness Tests ---");

            // Common monsters
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, MonsterRarity.COMMON) == 40,
                "Shard effectiveness on Common");
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, MonsterRarity.COMMON) == 99,
                "Heart effectiveness on Common");

            // Legendary monsters
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, MonsterRarity.LEGENDARY) == 0,
                "Shard ineffective on Legendary");
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CRYSTAL, MonsterRarity.LEGENDARY) == 0,
                "Crystal ineffective on Legendary");
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CORE, MonsterRarity.LEGENDARY) == 15,
                "Core effectiveness on Legendary");
            Assert(CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, MonsterRarity.LEGENDARY) == 25,
                "Heart effectiveness on Legendary");

            // Progression (each tier better than previous)
            for (int r = 0; r < 4; r++)
            {
                var rarity = (MonsterRarity)r;
                int shard = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, rarity);
                int crystal = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CRYSTAL, rarity);
                int core = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CORE, rarity);
                int heart = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, rarity);

                Assert(crystal >= shard, $"Crystal >= Shard for {rarity}");
                Assert(core >= crystal, $"Core >= Crystal for {rarity}");
                Assert(heart >= core, $"Heart >= Core for {rarity}");
            }
        }

        private void TestCaptureItemDisplayInfo()
        {
            Debug.Log("\n--- Capture Item Display Tests ---");

            Assert(CaptureItemConfig.GetDisplayName(CaptureItem.VEIL_SHARD) == "Veil Shard",
                "Shard display name");
            Assert(CaptureItemConfig.GetDisplayName(CaptureItem.VEIL_HEART) == "Veil Heart",
                "Heart display name");

            Assert(!string.IsNullOrEmpty(CaptureItemConfig.GetDescription(CaptureItem.VEIL_CORE)),
                "Core has description");
        }

        // =============================================================================
        // BIND THRESHOLD TESTS
        // =============================================================================

        private void TestBaseBindThreshold()
        {
            Debug.Log("\n--- Base Bind Threshold Tests ---");

            Assert(BindThresholdConfig.BASE_THRESHOLD == 0.25f,
                "Base threshold is 25%");
            Assert(BindThresholdConfig.MIN_THRESHOLD == 0.05f,
                "Min threshold is 5%");
            Assert(BindThresholdConfig.MAX_THRESHOLD == 0.50f,
                "Max threshold is 50%");
        }

        private void TestCorruptionThresholdModifier()
        {
            Debug.Log("\n--- Corruption Threshold Modifier Tests ---");

            // Create test combatants
            GameObject go1 = new GameObject("LowCorruption");
            GameObject go2 = new GameObject("HighCorruption");

            try
            {
                var lowCorruption = CreateTestCombatant(go1, "LowCorr", Brand.IRON, 100f, MonsterRarity.COMMON);
                lowCorruption.SetCorruption(10f); // ASCENDED

                var highCorruption = CreateTestCombatant(go2, "HighCorr", Brand.IRON, 100f, MonsterRarity.COMMON);
                highCorruption.SetCorruption(80f); // Abyssal

                float lowThreshold = BindThresholdCalculator.CalculateThreshold(lowCorruption, null);
                float highThreshold = BindThresholdCalculator.CalculateThreshold(highCorruption, null);

                Assert(lowThreshold > BindThresholdConfig.BASE_THRESHOLD,
                    "Low corruption increases threshold (easier bind)");
                Assert(highThreshold < BindThresholdConfig.BASE_THRESHOLD,
                    "High corruption decreases threshold (harder bind)");
                Assert(lowThreshold > highThreshold,
                    "Low corruption threshold > high corruption threshold");
            }
            finally
            {
                DestroyImmediate(go1);
                DestroyImmediate(go2);
            }
        }

        private void TestRarityThresholdModifier()
        {
            Debug.Log("\n--- Rarity Threshold Modifier Tests ---");

            GameObject go1 = new GameObject("Common");
            GameObject go2 = new GameObject("Legendary");

            try
            {
                var common = CreateTestCombatant(go1, "Common", Brand.IRON, 100f, MonsterRarity.COMMON);
                common.SetCorruption(35f); // Neutral corruption

                var legendary = CreateTestCombatant(go2, "Legendary", Brand.IRON, 100f, MonsterRarity.LEGENDARY);
                legendary.SetCorruption(35f);

                float commonThreshold = BindThresholdCalculator.CalculateThreshold(common, null);
                float legendaryThreshold = BindThresholdCalculator.CalculateThreshold(legendary, null);

                Assert(commonThreshold > legendaryThreshold,
                    "Common threshold > Legendary threshold (legendary harder to bind)");
            }
            finally
            {
                DestroyImmediate(go1);
                DestroyImmediate(go2);
            }
        }

        private void TestIntimidation()
        {
            Debug.Log("\n--- Intimidation Tests ---");

            GameObject go1 = new GameObject("HighLevelAlly");
            GameObject go2 = new GameObject("LowLevelEnemy");

            try
            {
                var ally = CreateTestCombatant(go1, "Ally", Brand.SAVAGE, 100f, MonsterRarity.EPIC);
                ally.SetLevel(20);

                var enemy = CreateTestCombatant(go2, "Enemy", Brand.IRON, 100f, MonsterRarity.COMMON);
                enemy.SetLevel(10);
                enemy.SetCorruption(35f);

                // Ally has level advantage (10+ levels) and rarity advantage
                bool intimidated = BindThresholdCalculator.IsIntimidated(enemy, ally);
                Assert(intimidated, "Enemy is intimidated by much stronger ally");

                float thresholdWithAlly = BindThresholdCalculator.CalculateThreshold(enemy, ally);
                float thresholdWithoutAlly = BindThresholdCalculator.CalculateThreshold(enemy, null);

                Assert(thresholdWithAlly > thresholdWithoutAlly,
                    "Intimidation increases bind threshold");
            }
            finally
            {
                DestroyImmediate(go1);
                DestroyImmediate(go2);
            }
        }

        // =============================================================================
        // CAPTURE FORMULA TESTS
        // =============================================================================

        private void TestBaseCaptureFormula()
        {
            Debug.Log("\n--- Base Capture Formula Tests ---");

            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            var result = CaptureFormulaCalculator.Calculate(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);

            Assert(result.finalChance > 0f, "Capture chance is positive");
            Assert(result.finalChance <= CaptureFormulaConfig.MAX_CHANCE, "Capture chance within max");
            Assert(result.finalChance >= CaptureFormulaConfig.MIN_CHANCE, "Capture chance within min");

            if (_logDetailedResults)
            {
                Debug.Log(result.ToString());
            }
        }

        private void TestItemEffectivenessByRarity()
        {
            Debug.Log("\n--- Item Effectiveness by Rarity Tests ---");

            // Test that higher tier items always give better chances
            var commonMonster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float shardChance = CaptureFormulaCalculator.CalculateQuick(commonMonster, CaptureItem.VEIL_SHARD, 10, QTEResult.MISS);
            float crystalChance = CaptureFormulaCalculator.CalculateQuick(commonMonster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);
            float coreChance = CaptureFormulaCalculator.CalculateQuick(commonMonster, CaptureItem.VEIL_CORE, 10, QTEResult.MISS);
            float heartChance = CaptureFormulaCalculator.CalculateQuick(commonMonster, CaptureItem.VEIL_HEART, 10, QTEResult.MISS);

            Assert(crystalChance > shardChance, "Crystal > Shard on Common");
            Assert(coreChance > crystalChance, "Core > Crystal on Common");
            Assert(heartChance > coreChance, "Heart > Core on Common");

            // Test legendary requires high tier items
            var legendaryMonster = new BoundMonsterData
            {
                boundAtHPPercent = 0.10f,
                currentCorruption = 10f,
                rarity = MonsterRarity.LEGENDARY,
                monsterLevel = 10
            };

            float legendaryShardChance = CaptureFormulaCalculator.CalculateQuick(legendaryMonster, CaptureItem.VEIL_SHARD, 10, QTEResult.PERFECT);

            Assert(legendaryShardChance <= CaptureFormulaConfig.MIN_CHANCE,
                "Shard essentially useless on Legendary");

            Assert(CaptureFormulaCalculator.IsItemEffective(CaptureItem.VEIL_CORE, MonsterRarity.LEGENDARY),
                "Core is effective on Legendary");
            Assert(!CaptureFormulaCalculator.IsItemEffective(CaptureItem.VEIL_SHARD, MonsterRarity.LEGENDARY),
                "Shard is not effective on Legendary");
        }

        private void TestCorruptionCaptureModifier()
        {
            Debug.Log("\n--- Corruption Capture Modifier Tests ---");

            var baseMonster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            // Test ASCENDED (0-10%)
            baseMonster.currentCorruption = 5f;
            float ascendedChance = CaptureFormulaCalculator.CalculateQuick(baseMonster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);

            // Test Abyssal (76-100%)
            baseMonster.currentCorruption = 90f;
            float abyssalChance = CaptureFormulaCalculator.CalculateQuick(baseMonster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);

            Assert(ascendedChance > abyssalChance,
                "ASCENDED capture chance > Abyssal capture chance");

            float difference = ascendedChance - abyssalChance;
            AssertApprox(0.40f, difference, 0.05f,
                "Corruption difference ~40% (20% bonus vs 20% penalty)");
        }

        private void TestLevelModifier()
        {
            Debug.Log("\n--- Level Modifier Tests ---");

            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            // Player higher level
            float higherLevelChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 15, QTEResult.MISS);

            // Player same level
            float sameLevelChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);

            // Player lower level
            float lowerLevelChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 5, QTEResult.MISS);

            Assert(higherLevelChance > sameLevelChance, "Higher level player = better chance");
            Assert(sameLevelChance > lowerLevelChance, "Lower level player = worse chance");
        }

        private void TestQTEModifier()
        {
            Debug.Log("\n--- QTE Modifier Tests ---");

            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float missChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);
            float okayChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.OKAY);
            float goodChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);
            float perfectChance = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.PERFECT);

            Assert(okayChance > missChance, "Okay > Miss");
            Assert(goodChance > okayChance, "Good > Okay");
            Assert(perfectChance > goodChance, "Perfect > Good");

            float perfectBonus = perfectChance - missChance;
            AssertApprox(CaptureFormulaConfig.QTE_PERFECT_BONUS, perfectBonus, 0.01f,
                "Perfect bonus is 15%");
        }

        // =============================================================================
        // FAILURE OUTCOME TESTS
        // =============================================================================

        private void TestFailureOutcomeWeighting()
        {
            Debug.Log("\n--- Failure Outcome Weighting Tests ---");

            // Low corruption = more likely to flee
            var lowCorruption = new BoundMonsterData
            {
                currentCorruption = 10f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };
            float lowBerserkChance = CaptureFormulaCalculator.GetBerserkChance(lowCorruption, 10);
            Assert(lowBerserkChance < 0.5f, "Low corruption = low berserk chance");

            // High corruption = more likely to berserk
            var highCorruption = new BoundMonsterData
            {
                currentCorruption = 80f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };
            float highBerserkChance = CaptureFormulaCalculator.GetBerserkChance(highCorruption, 10);
            Assert(highBerserkChance > 0.5f, "High corruption = high berserk chance");

            // High rarity increases berserk chance
            var epic = new BoundMonsterData
            {
                currentCorruption = 35f,
                rarity = MonsterRarity.EPIC,
                monsterLevel = 10
            };
            float epicBerserkChance = CaptureFormulaCalculator.GetBerserkChance(epic, 10);
            Assert(epicBerserkChance > 0.5f, "Epic rarity increases berserk chance");

            // High level monster increases berserk chance
            var highLevel = new BoundMonsterData
            {
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 20
            };
            float highLevelBerserkChance = CaptureFormulaCalculator.GetBerserkChance(highLevel, 10);
            Assert(highLevelBerserkChance > 0.5f, "High level difference increases berserk chance");
        }

        // =============================================================================
        // MANAGER TESTS
        // =============================================================================

        private void TestCaptureManagerInitialization()
        {
            Debug.Log("\n--- Capture Manager Initialization Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject enemyGO = null;

            try
            {
                managerGO = new GameObject("CaptureManager");
                var manager = managerGO.AddComponent<CaptureManager>();

                playerGO = new GameObject("Player");
                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f, MonsterRarity.COMMON);

                enemyGO = new GameObject("Enemy");
                var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.IRON, 100f, MonsterRarity.COMMON);

                manager.Initialize(player, new Combatant[0], new[] { enemy }, 10);

                Assert(!manager.InCapturePhase, "Not in capture phase after init");
                Assert(!manager.HasMarkedTargets, "No marked targets after init");
                Assert(!manager.HasBoundMonsters, "No bound monsters after init");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (enemyGO != null) DestroyImmediate(enemyGO);
            }
        }

        private void TestMarkingSystem()
        {
            Debug.Log("\n--- Marking System Tests ---");

            GameObject managerGO = null;
            GameObject playerGO = null;
            GameObject enemyGO = null;

            try
            {
                managerGO = new GameObject("CaptureManager");
                var manager = managerGO.AddComponent<CaptureManager>();

                playerGO = new GameObject("Player");
                var player = CreateTestCombatant(playerGO, "Player", Brand.SAVAGE, 100f, MonsterRarity.COMMON);
                player.SetPlayer(true);

                enemyGO = new GameObject("Enemy");
                var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.IRON, 100f, MonsterRarity.COMMON);

                manager.Initialize(player, new Combatant[0], new[] { enemy }, 10);

                // Mark enemy
                Assert(manager.IsValidCaptureTarget(enemy), "Enemy is valid capture target");
                Assert(!manager.IsValidCaptureTarget(player), "Player is not valid capture target");

                manager.MarkForCapture(enemy);
                Assert(manager.IsMarkedForCapture(enemy), "Enemy is marked after marking");
                Assert(manager.HasMarkedTargets, "Has marked targets");

                // Toggle mark
                manager.ToggleMark(enemy);
                Assert(!manager.IsMarkedForCapture(enemy), "Enemy unmarked after toggle");

                // Clear all
                manager.MarkForCapture(enemy);
                manager.ClearAllMarks();
                Assert(!manager.HasMarkedTargets, "No marked targets after clear");
            }
            finally
            {
                if (managerGO != null) DestroyImmediate(managerGO);
                if (playerGO != null) DestroyImmediate(playerGO);
                if (enemyGO != null) DestroyImmediate(enemyGO);
            }
        }

        // =============================================================================
        // QTE TESTS
        // =============================================================================

        private void TestQTEZones()
        {
            Debug.Log("\n--- QTE Zone Tests ---");

            GameObject qteGO = null;

            try
            {
                qteGO = new GameObject("QTEController");
                var qte = qteGO.AddComponent<QTEController>();

                // Check zone sizes
                Assert(qte.PerfectZoneWidth < qte.GoodZoneWidth, "Perfect zone < Good zone");
                Assert(qte.GoodZoneWidth < qte.OkayZoneWidth, "Good zone < Okay zone");

                // Check bonuses
                Assert(QTEController.GetBonus(QTEResult.PERFECT) > QTEController.GetBonus(QTEResult.GOOD),
                    "Perfect bonus > Good bonus");
                Assert(QTEController.GetBonus(QTEResult.GOOD) > QTEController.GetBonus(QTEResult.OKAY),
                    "Good bonus > Okay bonus");
                Assert(QTEController.GetBonus(QTEResult.OKAY) > QTEController.GetBonus(QTEResult.MISS),
                    "Okay bonus > Miss bonus");

                // Check result text
                Assert(QTEController.GetResultText(QTEResult.PERFECT) == "PERFECT!",
                    "Perfect result text");
            }
            finally
            {
                if (qteGO != null) DestroyImmediate(qteGO);
            }
        }

        // =============================================================================
        // INTEGRATION TESTS
        // =============================================================================

        private void TestFullCaptureFlow()
        {
            Debug.Log("\n--- Full Capture Flow Test ---");

            // This tests the complete flow from marking to capture
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.15f,
                currentCorruption = 10f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            // Calculate chance with best conditions
            var result = CaptureFormulaCalculator.Calculate(
                monster,
                CaptureItem.VEIL_HEART,
                15, // Player 5 levels higher
                QTEResult.PERFECT
            );

            if (_logDetailedResults)
            {
                Debug.Log($"Best case scenario:\n{result}");
            }

            // Should have very high chance
            Assert(result.finalChance > 0.90f, "Best case has >90% chance");

            // Calculate with worst conditions
            var hardMonster = new BoundMonsterData
            {
                boundAtHPPercent = 0.45f,
                currentCorruption = 90f,
                rarity = MonsterRarity.LEGENDARY,
                monsterLevel = 20
            };

            var hardResult = CaptureFormulaCalculator.Calculate(
                hardMonster,
                CaptureItem.VEIL_CORE,
                10, // Player 10 levels lower
                QTEResult.MISS
            );

            if (_logDetailedResults)
            {
                Debug.Log($"Worst case scenario:\n{hardResult}");
            }

            // Should have very low chance
            Assert(hardResult.finalChance < 0.10f, "Worst case has <10% chance");
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        private Combatant CreateTestCombatant(GameObject go, string name, Brand brand, float hp, MonsterRarity rarity)
        {
            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize(name, name, brand, (int)hp, 50, 10, 10, 10, 10, 10, false);
            combatant.SetRarity(rarity);
            return combatant;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using VeilBreakers.AI;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test suite for the Gambits AI System.
    /// Validates conditions, actions, rules, utility scoring, and brand-specific behavior.
    /// </summary>
    public class GambitTests : MonoBehaviour
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
                Debug.LogError($"[GambitTests] Exception during tests: {ex.Message}");
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
            Debug.Log("[GambitTests] Starting Gambits AI Test Suite");
            Debug.Log("========================================");

            // Condition Tests
            TestConditionCreation();
            TestConditionEvaluation();
            TestConditionNegation();

            // Action Tests
            TestActionCreation();
            TestTargetSelection();
            TestActionExecution();

            // Rule Tests
            TestRuleCreation();
            TestRuleEvaluation();
            TestRulePriority();
            TestRuleSetSorting();

            // Personality Tests
            TestPersonalityWeights();
            TestBrandPersonalities();
            TestMultiplierCalculation();

            // Evaluator Tests
            TestEvaluatorScoring();
            TestBucketPriority();
            TestExecutePriority();

            // Integration Tests
            TestIronBehavior();
            TestSavageBehavior();
            TestGraceBehavior();
            TestVoidDesperation();

            // Summary
            Debug.Log("========================================");
            Debug.Log($"[GambitTests] COMPLETE: {_passCount} passed, {_failCount} failed");
            Debug.Log("========================================");

            if (_failCount > 0)
            {
                Debug.LogError($"[GambitTests] {_failCount} TEST(S) FAILED!");
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
        // CONDITION TESTS
        // =============================================================================

        private void TestConditionCreation()
        {
            Debug.Log("\n--- Condition Creation Tests ---");

            // Test basic condition creation
            var condition = GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 50f);
            AssertEqual(GambitCondition.ConditionType.SELF_HP_BELOW, condition.conditionType, "Condition type set correctly");
            AssertApprox(50f, condition.threshold, 0.01f, "Condition threshold set correctly");

            // Test status condition creation
            var statusCondition = GambitCondition.CreateStatusCondition(
                GambitCondition.ConditionType.ENEMY_HAS_STATUS,
                StatusEffectType.POISON
            );
            AssertEqual(StatusEffectType.POISON, statusCondition.statusEffectType, "Status effect type set correctly");

            // Test ALWAYS condition
            var alwaysCondition = GambitCondition.Create(GambitCondition.ConditionType.ALWAYS);
            AssertEqual(GambitCondition.ConditionType.ALWAYS, alwaysCondition.conditionType, "ALWAYS condition created");
        }

        private void TestConditionEvaluation()
        {
            Debug.Log("\n--- Condition Evaluation Tests ---");

            GameObject selfGO = null;
            GameObject targetGO = null;
            try
            {
                // Create test combatants
                selfGO = new GameObject("Self");
                targetGO = new GameObject("Target");
                var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 50f);
                var target = CreateTestCombatant(targetGO, "Target", Brand.IRON, 25f);

                var context = new BattleContext(new[] { self }, new[] { target }, null);

                // Test SELF_HP_BELOW (self at 50%)
                var belowCondition = GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 60f);
                Assert(belowCondition.Evaluate(self, target, context), "SELF_HP_BELOW 60% evaluates true at 50%");

                var aboveCondition = GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 40f);
                Assert(!aboveCondition.Evaluate(self, target, context), "SELF_HP_BELOW 40% evaluates false at 50%");

                // Test ENEMY_HP_BELOW (target at 25%)
                var enemyLowCondition = GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 30f);
                Assert(enemyLowCondition.Evaluate(self, target, context), "ENEMY_HP_BELOW 30% evaluates true at 25%");

                // Test ALWAYS
                var alwaysCondition = GambitCondition.Create(GambitCondition.ConditionType.ALWAYS);
                Assert(alwaysCondition.Evaluate(self, target, context), "ALWAYS evaluates true");

                // Test NEVER
                var neverCondition = GambitCondition.Create(GambitCondition.ConditionType.NEVER);
                Assert(!neverCondition.Evaluate(self, target, context), "NEVER evaluates false");
            }
            finally
            {
                // Cleanup even if tests fail
                if (selfGO != null) DestroyImmediate(selfGO);
                if (targetGO != null) DestroyImmediate(targetGO);
            }
        }

        private void TestConditionNegation()
        {
            Debug.Log("\n--- Condition Negation Tests ---");

            var selfGO = new GameObject("Self");
            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 50f);
            var context = new BattleContext(new[] { self }, new Combatant[0], null);

            // Test negated condition
            var condition = GambitCondition.Create(GambitCondition.ConditionType.SELF_HP_BELOW, 60f);
            condition.negate = false;
            Assert(condition.Evaluate(self, null, context), "Non-negated condition works");

            condition.negate = true;
            Assert(!condition.Evaluate(self, null, context), "Negated condition inverts result");

            DestroyImmediate(selfGO);
        }

        // =============================================================================
        // ACTION TESTS
        // =============================================================================

        private void TestActionCreation()
        {
            Debug.Log("\n--- Action Creation Tests ---");

            // Test basic action creation
            var attack = GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK);
            AssertEqual(GambitAction.ActionType.BASIC_ATTACK, attack.actionType, "Basic attack action created");
            AssertEqual(GambitAction.TargetSelection.AUTO, attack.targetSelection, "Default target selection is AUTO");

            // Test ability action creation
            var ability = GambitAction.CreateAbilityAction(AbilitySlot.SKILL_1, GambitAction.TargetSelection.LOWEST_HP_ENEMY);
            AssertEqual(GambitAction.ActionType.USE_ABILITY, ability.actionType, "Ability action type set");
            AssertEqual(AbilitySlot.SKILL_1, ability.abilitySlot, "Ability slot set correctly");
            AssertEqual(GambitAction.TargetSelection.LOWEST_HP_ENEMY, ability.targetSelection, "Target selection set");
        }

        private void TestTargetSelection()
        {
            Debug.Log("\n--- Target Selection Tests ---");

            var selfGO = new GameObject("Self");
            var enemy1GO = new GameObject("Enemy1");
            var enemy2GO = new GameObject("Enemy2");

            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 80f);
            var enemy1 = CreateTestCombatant(enemy1GO, "Weak Enemy", Brand.IRON, 20f);
            var enemy2 = CreateTestCombatant(enemy2GO, "Strong Enemy", Brand.IRON, 80f);

            var context = new BattleContext(new[] { self }, new[] { enemy1, enemy2 }, null);

            // Test lowest HP selection
            var lowestHp = context.GetLowestHpEnemy();
            AssertEqual("Weak Enemy", lowestHp?.DisplayName, "GetLowestHpEnemy returns lowest HP enemy");

            // Test highest HP selection
            var highestHp = context.GetHighestHpEnemy();
            AssertEqual("Strong Enemy", highestHp?.DisplayName, "GetHighestHpEnemy returns highest HP enemy");

            DestroyImmediate(selfGO);
            DestroyImmediate(enemy1GO);
            DestroyImmediate(enemy2GO);
        }

        private void TestActionExecution()
        {
            Debug.Log("\n--- Action Execution Tests ---");

            var selfGO = new GameObject("Self");
            var targetGO = new GameObject("Target");

            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 80f);
            var target = CreateTestCombatant(targetGO, "Target", Brand.IRON, 50f);

            var context = new BattleContext(new[] { self }, new[] { target }, null);

            // Test basic attack execution
            var attack = GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK);
            bool success = attack.Execute(self, context, out ActionResult result);
            Assert(success, "Basic attack executes successfully");
            Assert(result.success, "Action result reports success");
            AssertEqual(self, result.source, "Action result source is self");
            AssertEqual(target, result.target, "Action result target is enemy");

            // Test defend execution
            var defend = GambitAction.Create(GambitAction.ActionType.DEFEND_SELF);
            success = defend.Execute(self, context, out result);
            Assert(success, "Defend executes successfully");
            Assert(self.IsDefending, "Self is now defending");

            self.StopDefend();

            DestroyImmediate(selfGO);
            DestroyImmediate(targetGO);
        }

        // =============================================================================
        // RULE TESTS
        // =============================================================================

        private void TestRuleCreation()
        {
            Debug.Log("\n--- Rule Creation Tests ---");

            var condition = GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 25f);
            var action = GambitAction.Create(GambitAction.ActionType.EXECUTE);
            var rule = GambitRule.Create("Execute Low HP", condition, action, PriorityBucket.HIGH, 90);

            AssertEqual("Execute Low HP", rule.ruleName, "Rule name set correctly");
            AssertEqual(PriorityBucket.HIGH, rule.bucket, "Rule bucket set correctly");
            AssertEqual(90, rule.priority, "Rule priority set correctly");
            Assert(rule.enabled, "Rule enabled by default");
        }

        private void TestRuleEvaluation()
        {
            Debug.Log("\n--- Rule Evaluation Tests ---");

            var selfGO = new GameObject("Self");
            var targetGO = new GameObject("Target");

            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 80f);
            var target = CreateTestCombatant(targetGO, "Target", Brand.IRON, 20f);

            var context = new BattleContext(new[] { self }, new[] { target }, null);

            // Test rule that should match
            var matchRule = GambitRule.Create(
                "Execute",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 25f),
                GambitAction.Create(GambitAction.ActionType.EXECUTE),
                PriorityBucket.HIGH
            );
            Assert(matchRule.Evaluate(self, target, context), "Rule evaluates true when condition met");

            // Test rule that should not match
            var noMatchRule = GambitRule.Create(
                "Execute",
                GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 15f),
                GambitAction.Create(GambitAction.ActionType.EXECUTE),
                PriorityBucket.HIGH
            );
            Assert(!noMatchRule.Evaluate(self, target, context), "Rule evaluates false when condition not met");

            // Test disabled rule
            matchRule.enabled = false;
            Assert(!matchRule.Evaluate(self, target, context), "Disabled rule evaluates false");

            DestroyImmediate(selfGO);
            DestroyImmediate(targetGO);
        }

        private void TestRulePriority()
        {
            Debug.Log("\n--- Rule Priority Tests ---");

            var criticalRule = GambitRule.Create("Critical", null, null, PriorityBucket.CRITICAL, 50);
            var highRule = GambitRule.Create("High", null, null, PriorityBucket.HIGH, 90);
            var standardRule = GambitRule.Create("Standard", null, null, PriorityBucket.STANDARD, 50);

            // Critical (0 * 1000 + 50) < High (1 * 1000 + 10) < Standard (2 * 1000 + 50)
            Assert(criticalRule.GetCombinedPriority() < highRule.GetCombinedPriority(),
                "CRITICAL bucket has lower combined priority than HIGH");
            Assert(highRule.GetCombinedPriority() < standardRule.GetCombinedPriority(),
                "HIGH bucket has lower combined priority than STANDARD");
        }

        private void TestRuleSetSorting()
        {
            Debug.Log("\n--- Rule Set Sorting Tests ---");

            var ruleSet = new GambitRuleSet
            {
                setName = "Test Set",
                rules = new[]
                {
                    GambitRule.Create("Standard", null, null, PriorityBucket.STANDARD, 50),
                    GambitRule.Create("Critical", null, null, PriorityBucket.CRITICAL, 80),
                    GambitRule.Create("High", null, null, PriorityBucket.HIGH, 60),
                }
            };

            ruleSet.SortByPriority();

            AssertEqual("Critical", ruleSet.rules[0].ruleName, "Critical rule sorted first");
            AssertEqual("High", ruleSet.rules[1].ruleName, "High rule sorted second");
            AssertEqual("Standard", ruleSet.rules[2].ruleName, "Standard rule sorted third");
        }

        // =============================================================================
        // PERSONALITY TESTS
        // =============================================================================

        private void TestPersonalityWeights()
        {
            Debug.Log("\n--- Personality Weight Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.SAVAGE);

            int totalWeight = personality.damageWeight + personality.survivalWeight +
                            personality.teamValueWeight + personality.positioningWeight +
                            personality.controlWeight;

            AssertEqual(100, totalWeight, "SAVAGE personality weights sum to 100");
            Assert(personality.damageWeight >= 40, "SAVAGE has high damage weight");
        }

        private void TestBrandPersonalities()
        {
            Debug.Log("\n--- Brand Personality Tests ---");

            // Test IRON (tank)
            var iron = AIPersonality.CreateDefault(Brand.IRON);
            Assert(iron.canAutoDefend, "IRON can auto-defend");
            Assert(iron.survivalWeight > iron.damageWeight, "IRON prioritizes survival over damage");

            // Test SAVAGE (DPS)
            var savage = AIPersonality.CreateDefault(Brand.SAVAGE);
            Assert(savage.tracksMomentum, "SAVAGE tracks momentum");
            Assert(savage.damageWeight >= savage.survivalWeight, "SAVAGE prioritizes damage");

            // Test GRACE (healer)
            var grace = AIPersonality.CreateDefault(Brand.GRACE);
            Assert(grace.teamValueWeight > 50, "GRACE prioritizes team value");
            AssertEqual(AIPersonality.UltimateTargetMode.LOWEST_HP_ALLY, grace.ultimateTargetMode,
                "GRACE ultimate targets lowest HP ally");

            // Test VOID (chaos)
            var voidP = AIPersonality.CreateDefault(Brand.VOID);
            Assert(voidP.desperationBonus, "VOID has desperation bonus");
        }

        private void TestMultiplierCalculation()
        {
            Debug.Log("\n--- Multiplier Calculation Tests ---");

            var savage = AIPersonality.CreateDefault(Brand.SAVAGE);

            // Test target multiplier for low HP enemy
            float lowHpMult = savage.GetTargetMultiplier(20f, false, false, false, false, false);
            Assert(lowHpMult > 2f, "Low HP target gets high multiplier");

            // Test target multiplier for tank (should be low)
            float tankMult = savage.GetTargetMultiplier(80f, false, false, false, true, false);
            Assert(tankMult < 0.5f, "Tank target gets low multiplier");

            // Test armor shred tank (should be viable)
            float shredTankMult = savage.GetTargetMultiplier(80f, false, true, false, true, false);
            Assert(shredTankMult > tankMult, "Armor-shredded tank gets higher multiplier than normal tank");

            // Test self survival multiplier
            float criticalMult = savage.GetSelfSurvivalMultiplier(15f);
            Assert(criticalMult >= savage.selfCriticalMultiplier, "Critical HP gives high survival multiplier");
        }

        // =============================================================================
        // EVALUATOR TESTS
        // =============================================================================

        private void TestEvaluatorScoring()
        {
            Debug.Log("\n--- Evaluator Scoring Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.SAVAGE);
            var ruleSet = CreateTestRuleSet();
            var evaluator = new GambitEvaluator(personality, ruleSet);

            var selfGO = new GameObject("Self");
            var targetGO = new GameObject("Target");

            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 80f);
            var target = CreateTestCombatant(targetGO, "Target", Brand.IRON, 20f);

            var context = new BattleContext(new[] { self }, new[] { target }, null);

            var best = evaluator.EvaluateBestAction(self, context);

            Assert(best.IsValid, "Evaluator returns valid action");
            Assert(best.score > 0, "Best action has positive score");
            Assert(best.target != null, "Best action has target");

            DestroyImmediate(selfGO);
            DestroyImmediate(targetGO);
        }

        private void TestBucketPriority()
        {
            Debug.Log("\n--- Bucket Priority Tests ---");

            // Test that CRITICAL bucket is evaluated before STANDARD
            var personality = AIPersonality.CreateDefault(Brand.GRACE);
            var ruleSet = new GambitRuleSet
            {
                rules = new[]
                {
                    // Standard rule with high base score
                    GambitRule.Create("Attack", GambitCondition.Create(GambitCondition.ConditionType.ALWAYS),
                        GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK),
                        PriorityBucket.STANDARD, 90) { baseUtility = 100f },

                    // Critical rule with lower base score
                    GambitRule.Create("Emergency Heal", GambitCondition.Create(GambitCondition.ConditionType.ALLY_CRITICAL),
                        GambitAction.Create(GambitAction.ActionType.HEAL_ALLY),
                        PriorityBucket.CRITICAL, 100) { baseUtility = 50f }
                }
            };

            var evaluator = new GambitEvaluator(personality, ruleSet);

            var selfGO = new GameObject("Self");
            var allyGO = new GameObject("Ally");
            var enemyGO = new GameObject("Enemy");

            var self = CreateTestCombatant(selfGO, "Self", Brand.GRACE, 80f);
            var ally = CreateTestCombatant(allyGO, "Ally", Brand.IRON, 15f); // Critical HP
            var enemy = CreateTestCombatant(enemyGO, "Enemy", Brand.SAVAGE, 80f);

            var context = new BattleContext(new[] { self, ally }, new[] { enemy }, null);

            var best = evaluator.EvaluateBestAction(self, context);

            // Should pick critical heal over standard attack despite lower base utility
            AssertEqual("Emergency Heal", best.rule?.ruleName, "Critical bucket evaluated before Standard");

            DestroyImmediate(selfGO);
            DestroyImmediate(allyGO);
            DestroyImmediate(enemyGO);
        }

        private void TestExecutePriority()
        {
            Debug.Log("\n--- Execute Priority Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.SAVAGE);
            var ruleSet = CreateTestRuleSet();
            var evaluator = new GambitEvaluator(personality, ruleSet);

            var selfGO = new GameObject("Self");
            var target1GO = new GameObject("FullHP");
            var target2GO = new GameObject("LowHP");

            var self = CreateTestCombatant(selfGO, "Self", Brand.SAVAGE, 80f);
            var fullHp = CreateTestCombatant(target1GO, "FullHP", Brand.IRON, 90f);
            var lowHp = CreateTestCombatant(target2GO, "LowHP", Brand.IRON, 15f);

            var context = new BattleContext(new[] { self }, new[] { fullHp, lowHp }, null);

            var best = evaluator.EvaluateBestAction(self, context);

            // Should target the low HP enemy for execute
            AssertEqual("LowHP", best.target?.DisplayName, "Execute targets lowest HP enemy");

            DestroyImmediate(selfGO);
            DestroyImmediate(target1GO);
            DestroyImmediate(target2GO);
        }

        // =============================================================================
        // INTEGRATION TESTS
        // =============================================================================

        private void TestIronBehavior()
        {
            Debug.Log("\n--- IRON Behavior Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.IRON);

            Assert(personality.canAutoDefend, "IRON can auto-defend");
            Assert(personality.survivalWeight >= 35, "IRON has high survival weight");
            Assert(personality.teamValueWeight >= 30, "IRON has high team value weight");
            AssertEqual(AIPersonality.UltimateTargetMode.MOST_ENEMIES_ON_ALLY, personality.ultimateTargetMode,
                "IRON ultimate targets ally with most enemies");
        }

        private void TestSavageBehavior()
        {
            Debug.Log("\n--- SAVAGE Behavior Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.SAVAGE);

            Assert(personality.tracksMomentum, "SAVAGE tracks kill momentum");
            Assert(personality.lowHpTargetMultiplier >= 2.5f, "SAVAGE has high execute multiplier");
            Assert(personality.tankTargetMultiplier < 0.5f, "SAVAGE avoids tanks");
            AssertEqual(AIPersonality.UltimateTargetMode.LOWEST_HP_ENEMY, personality.ultimateTargetMode,
                "SAVAGE ultimate targets lowest HP enemy");
        }

        private void TestGraceBehavior()
        {
            Debug.Log("\n--- GRACE Behavior Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.GRACE);

            Assert(personality.teamValueWeight >= 50, "GRACE has very high team value weight");
            Assert(personality.allyCriticalMultiplier >= 3f, "GRACE has high emergency heal multiplier");
            Assert(personality.damageWeight < 10, "GRACE has low damage weight");
            AssertEqual(AIPersonality.UltimateTargetMode.LOWEST_HP_ALLY, personality.ultimateTargetMode,
                "GRACE ultimate targets lowest HP ally");
        }

        private void TestVoidDesperation()
        {
            Debug.Log("\n--- VOID Desperation Tests ---");

            var personality = AIPersonality.CreateDefault(Brand.VOID);
            var ruleSet = CreateTestRuleSet();
            var evaluator = new GambitEvaluator(personality, ruleSet);

            var selfGO = new GameObject("Self");
            var ally1GO = new GameObject("Ally1");
            var ally2GO = new GameObject("Ally2");

            var self = CreateTestCombatant(selfGO, "Self", Brand.VOID, 80f);
            var ally1 = CreateTestCombatant(ally1GO, "Ally1", Brand.IRON, 20f); // Low HP
            var ally2 = CreateTestCombatant(ally2GO, "Ally2", Brand.GRACE, 25f); // Low HP

            var context = new BattleContext(new[] { self, ally1, ally2 }, new Combatant[0], null);

            // Test desperation bonus calculation
            float bonus = evaluator.CalculateDesperationBonus(self, context);
            Assert(bonus > 1f, "VOID gets desperation bonus when team is losing");

            DestroyImmediate(selfGO);
            DestroyImmediate(ally1GO);
            DestroyImmediate(ally2GO);
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

        private GambitRuleSet CreateTestRuleSet()
        {
            return new GambitRuleSet
            {
                setName = "Test Rules",
                rules = new[]
                {
                    // Execute rule
                    GambitRule.Create(
                        "Execute",
                        GambitCondition.Create(GambitCondition.ConditionType.ENEMY_HP_BELOW, 25f),
                        GambitAction.Create(GambitAction.ActionType.EXECUTE),
                        PriorityBucket.HIGH,
                        90
                    ) { baseUtility = 80f },

                    // Basic attack fallback
                    GambitRule.CreateAlways(
                        "Attack",
                        GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK),
                        PriorityBucket.LOW,
                        10
                    ) { baseUtility = 20f }
                }
            };
        }
    }
}

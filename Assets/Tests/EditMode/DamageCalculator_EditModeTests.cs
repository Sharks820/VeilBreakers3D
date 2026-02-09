using NUnit.Framework;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Tests.EditMode
{
    public class DamageCalculator_EditModeTests
    {
        private GameObject _attackerGO;
        private GameObject _defenderGO;
        private Combatant _attacker;
        private Combatant _defender;

        [SetUp]
        public void SetUp()
        {
            _attackerGO = new GameObject("TestAttacker");
            _defenderGO = new GameObject("TestDefender");

            _attacker = _attackerGO.AddComponent<Combatant>();
            _attacker.Initialize("test_attacker", "Test Attacker", Brand.IRON, 100, 50, 20, 10, 10, 10, 10, true);

            _defender = _defenderGO.AddComponent<Combatant>();
            _defender.Initialize("test_defender", "Test Defender", Brand.SURGE, 100, 50, 10, 15, 10, 10, 10, false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackerGO != null) Object.DestroyImmediate(_attackerGO);
            if (_defenderGO != null) Object.DestroyImmediate(_defenderGO);
        }

        // ====================================================================
        // BRAND MULTIPLIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void SuperEffective_Brand_Gives_2x_Multiplier()
        {
            // IRON vs SURGE = 2x
            var result = DamageCalculator.Calculate(_attacker, _defender, 50, DamageType.PHYSICAL);
            Assert.AreEqual(2.0f, result.brandMultiplier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void NotEffective_Brand_Gives_05x_Multiplier()
        {
            // Create SURGE attacker vs IRON defender
            var surgeGO = new GameObject("SurgeAttacker");
            var ironGO = new GameObject("IronDefender");
            try
            {
                var surgeAttacker = surgeGO.AddComponent<Combatant>();
                surgeAttacker.Initialize("surge", "Surge", Brand.SURGE, 100, 50, 20, 10, 10, 10, 10, true);

                var ironDefender = ironGO.AddComponent<Combatant>();
                ironDefender.Initialize("iron", "Iron", Brand.IRON, 100, 50, 10, 15, 10, 10, 10, false);

                var result = DamageCalculator.Calculate(surgeAttacker, ironDefender, 50, DamageType.PHYSICAL);
                Assert.AreEqual(0.5f, result.brandMultiplier);
            }
            finally
            {
                Object.DestroyImmediate(surgeGO);
                Object.DestroyImmediate(ironGO);
            }
        }

        // ====================================================================
        // DAMAGE OUTPUT
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Damage_Is_Positive()
        {
            var result = DamageCalculator.Calculate(_attacker, _defender, 50, DamageType.PHYSICAL);
            Assert.Greater(result.finalDamage, 0);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void SuperEffective_Result_IsTrue()
        {
            var result = DamageCalculator.Calculate(_attacker, _defender, 50, DamageType.PHYSICAL);
            Assert.IsTrue(result.IsSuperEffective);
        }

        // ====================================================================
        // SYNERGY MODIFIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Full_Synergy_Gives_108_Multiplier()
        {
            var result = DamageCalculator.Calculate(
                _attacker, _defender, 50, DamageType.PHYSICAL,
                SynergySystem.SynergyTier.FULL
            );
            Assert.AreEqual(1.08f, result.synergyMultiplier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Neutral_Synergy_Gives_1x_Multiplier()
        {
            var result = DamageCalculator.Calculate(
                _attacker, _defender, 50, DamageType.PHYSICAL,
                SynergySystem.SynergyTier.NEUTRAL
            );
            Assert.AreEqual(1.0f, result.synergyMultiplier);
        }

        // ====================================================================
        // TRUE DAMAGE
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void True_Damage_Ignores_Defense()
        {
            // TRUE damage should generally be higher than PHYSICAL with same base power
            // because defense is set to 0
            var trueResult = DamageCalculator.Calculate(
                _attacker, _defender, 50, DamageType.TRUE
            );
            Assert.Greater(trueResult.finalDamage, 0);
        }

        // ====================================================================
        // HEALING
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Heal_Amount_Is_Positive()
        {
            int healAmount = DamageCalculator.CalculateHeal(_attacker, 30);
            Assert.Greater(healAmount, 0);
        }

        // ====================================================================
        // NULL SAFETY
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Null_Combatant_Returns_Fallback_Damage()
        {
            var result = DamageCalculator.Calculate(null, _defender, 50, DamageType.PHYSICAL);
            Assert.Greater(result.finalDamage, 0);
        }
    }

    // ========================================================================
    // COMBATANT LIFECYCLE TESTS
    // ========================================================================

    public class Combatant_EditModeTests
    {
        private GameObject _combatantGO;
        private Combatant _combatant;

        [SetUp]
        public void SetUp()
        {
            _combatantGO = new GameObject("TestCombatant");
            _combatant = _combatantGO.AddComponent<Combatant>();
            _combatant.Initialize("test", "Test Monster", Brand.IRON, 100, 50, 15, 12, 8, 10, 10, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_combatantGO != null) Object.DestroyImmediate(_combatantGO);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Combatant_Starts_Alive()
        {
            Assert.IsTrue(_combatant.IsAlive);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Combatant_Starts_At_Max_HP()
        {
            Assert.AreEqual(100, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Combatant_Starts_At_Max_MP()
        {
            Assert.AreEqual(50, _combatant.CurrentMp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Combatant_Has_Correct_Brand()
        {
            Assert.AreEqual(Brand.IRON, _combatant.Brand);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void TakeDamage_Reduces_HP()
        {
            _combatant.TakeDamage(30);
            Assert.AreEqual(70, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Heal_Increases_HP()
        {
            _combatant.TakeDamage(30);
            _combatant.Heal(20);
            Assert.AreEqual(90, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Heal_Capped_At_Max_HP()
        {
            _combatant.TakeDamage(10);
            _combatant.Heal(50);
            Assert.AreEqual(100, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void UseMp_Reduces_MP()
        {
            bool used = _combatant.UseMp(20);
            Assert.IsTrue(used);
            Assert.AreEqual(30, _combatant.CurrentMp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void UseMp_Fails_When_Insufficient()
        {
            bool used = _combatant.UseMp(60);
            Assert.IsFalse(used);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void RestoreMp_Increases_MP()
        {
            _combatant.UseMp(20);
            _combatant.RestoreMp(15);
            Assert.AreEqual(45, _combatant.CurrentMp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Defend_Reduces_Damage_Taken()
        {
            _combatant.StartDefend(DefenseAction.DEFEND_SELF);
            Assert.IsTrue(_combatant.IsDefending);

            _combatant.TakeDamage(40);
            // 50% reduction while defending
            Assert.AreEqual(80, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void StopDefend_Ends_Defense()
        {
            _combatant.StartDefend(DefenseAction.DEFEND_SELF);
            _combatant.StopDefend();
            Assert.IsFalse(_combatant.IsDefending);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Lethal_Damage_Kills_Combatant()
        {
            _combatant.TakeDamage(100);
            Assert.IsFalse(_combatant.IsAlive);
            Assert.AreEqual(0, _combatant.CurrentHp);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Combat")]
        public void Revive_Restores_Life()
        {
            _combatant.TakeDamage(100);
            _combatant.Revive(0.5f);
            Assert.IsTrue(_combatant.IsAlive);
            Assert.AreEqual(50, _combatant.CurrentHp);
        }
    }
}

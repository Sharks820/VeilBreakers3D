using NUnit.Framework;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Tests.EditMode
{
    public class SynergySystem_EditModeTests
    {
        // ====================================================================
        // IRONBOUND PATH
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Ironbound_Full_Synergy_With_Iron_Mend_Leech()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.FULL, tier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Ironbound_Partial_Synergy_With_Two_Strong_Brands()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.MEND, Brand.GRACE }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.PARTIAL, tier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Ironbound_Neutral_Synergy_With_One_Strong_Brand()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.IRON, Brand.GRACE, Brand.SURGE }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.NEUTRAL, tier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Ironbound_Anti_Synergy_With_Weak_Brand()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.IRONBOUND,
                new[] { Brand.VOID, Brand.GRACE, Brand.DREAD }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.ANTI, tier);
        }

        // ====================================================================
        // FANGBORN PATH
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Fangborn_Full_Synergy_With_Savage_Venom_Ruin()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.FANGBORN,
                new[] { Brand.SAVAGE, Brand.VENOM, Brand.RUIN }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.FULL, tier);
        }

        // ====================================================================
        // VOIDTOUCHED PATH
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Voidtouched_Full_Synergy_With_Void_Dread_Surge()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.VOIDTOUCHED,
                new[] { Brand.VOID, Brand.DREAD, Brand.SURGE }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.FULL, tier);
        }

        // ====================================================================
        // UNCHAINED PATH
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Unchained_Always_Neutral()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.UNCHAINED,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.NEUTRAL, tier);
        }

        // ====================================================================
        // EDGE CASES
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void None_Path_Returns_None_Tier()
        {
            var tier = SynergySystem.GetSynergyTier(
                Path.NONE,
                new[] { Brand.IRON, Brand.MEND, Brand.LEECH }
            );
            Assert.AreEqual(SynergySystem.SynergyTier.NONE, tier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Null_Brands_Returns_None_Tier()
        {
            var tier = SynergySystem.GetSynergyTier(Path.IRONBOUND, null);
            Assert.AreEqual(SynergySystem.SynergyTier.NONE, tier);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Empty_Brands_Returns_None_Tier()
        {
            var tier = SynergySystem.GetSynergyTier(Path.IRONBOUND, new Brand[0], 0);
            Assert.AreEqual(SynergySystem.SynergyTier.NONE, tier);
        }

        // ====================================================================
        // DAMAGE & DEFENSE BONUSES
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Full_Synergy_Gives_8_Percent_Damage_Bonus()
        {
            Assert.AreEqual(1.08f, SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.FULL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Partial_Synergy_Gives_5_Percent_Damage_Bonus()
        {
            Assert.AreEqual(1.05f, SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.PARTIAL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Neutral_And_Anti_Give_No_Damage_Bonus()
        {
            Assert.AreEqual(1.0f, SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.NEUTRAL));
            Assert.AreEqual(1.0f, SynergySystem.GetDamageBonus(SynergySystem.SynergyTier.ANTI));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Full_Synergy_Gives_8_Percent_Defense_Bonus()
        {
            Assert.AreEqual(1.08f, SynergySystem.GetDefenseBonus(SynergySystem.SynergyTier.FULL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Partial_Synergy_Gives_5_Percent_Defense_Bonus()
        {
            Assert.AreEqual(1.05f, SynergySystem.GetDefenseBonus(SynergySystem.SynergyTier.PARTIAL));
        }

        // ====================================================================
        // CORRUPTION RATE MULTIPLIERS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Full_Synergy_Halves_Corruption_Rate()
        {
            Assert.AreEqual(0.5f, SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.FULL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Partial_Synergy_Reduces_Corruption_Rate()
        {
            Assert.AreEqual(0.75f, SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.PARTIAL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Anti_Synergy_Increases_Corruption_Rate()
        {
            Assert.AreEqual(1.5f, SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.ANTI));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Neutral_Synergy_No_Corruption_Change()
        {
            Assert.AreEqual(1.0f, SynergySystem.GetCorruptionRateMultiplier(SynergySystem.SynergyTier.NEUTRAL));
        }

        // ====================================================================
        // COMBO UNLOCK
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Synergy")]
        public void Combo_Unlocked_Only_At_Full()
        {
            Assert.IsTrue(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.FULL));
            Assert.IsFalse(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.PARTIAL));
            Assert.IsFalse(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.NEUTRAL));
            Assert.IsFalse(SynergySystem.IsComboUnlocked(SynergySystem.SynergyTier.ANTI));
        }
    }
}

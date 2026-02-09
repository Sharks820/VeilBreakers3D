using NUnit.Framework;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Tests.EditMode
{
    public class BrandSystem_EditModeTests
    {
        // ====================================================================
        // EFFECTIVENESS MATRIX - SUPER EFFECTIVE (2x)
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Iron_SuperEffective_Against_Surge_And_Dread()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.DREAD));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Savage_SuperEffective_Against_Iron_And_Mend()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.IRON));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.MEND));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Surge_SuperEffective_Against_Venom_And_Leech()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.SURGE, Brand.VENOM));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.SURGE, Brand.LEECH));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Venom_SuperEffective_Against_Grace_And_Mend()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.VENOM, Brand.GRACE));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.VENOM, Brand.MEND));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Dread_SuperEffective_Against_Savage_And_Grace()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.DREAD, Brand.SAVAGE));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.DREAD, Brand.GRACE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Leech_SuperEffective_Against_Dread_And_Surge()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.LEECH, Brand.DREAD));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.LEECH, Brand.SURGE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Grace_SuperEffective_Against_Void_And_Ruin()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.GRACE, Brand.VOID));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.GRACE, Brand.RUIN));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Mend_SuperEffective_Against_Void_And_Leech()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.MEND, Brand.VOID));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.MEND, Brand.LEECH));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Ruin_SuperEffective_Against_Iron_And_Venom()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.RUIN, Brand.IRON));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.RUIN, Brand.VENOM));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Void_SuperEffective_Against_Surge_And_Dread()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.VOID, Brand.SURGE));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.VOID, Brand.DREAD));
        }

        // ====================================================================
        // EFFECTIVENESS MATRIX - NOT EFFECTIVE (0.5x)
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Iron_NotEffective_Against_Savage_And_Ruin()
        {
            Assert.AreEqual(0.5f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.SAVAGE));
            Assert.AreEqual(0.5f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.RUIN));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Savage_NotEffective_Against_Leech_And_Grace()
        {
            Assert.AreEqual(0.5f, BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.LEECH));
            Assert.AreEqual(0.5f, BrandSystem.GetEffectiveness(Brand.SAVAGE, Brand.GRACE));
        }

        // ====================================================================
        // NEUTRAL MATCHUPS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Neutral_Matchup_Returns_1x()
        {
            Assert.AreEqual(1.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.GRACE));
            Assert.AreEqual(1.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.IRON));
        }

        // ====================================================================
        // NONE BRAND
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void None_Brand_Always_Neutral()
        {
            Assert.AreEqual(1.0f, BrandSystem.GetEffectiveness(Brand.NONE, Brand.IRON));
            Assert.AreEqual(1.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.NONE));
            Assert.AreEqual(1.0f, BrandSystem.GetEffectiveness(Brand.NONE, Brand.NONE));
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void HasAdvantage_Returns_True_For_SuperEffective()
        {
            Assert.IsTrue(BrandSystem.HasAdvantage(Brand.IRON, Brand.SURGE));
            Assert.IsTrue(BrandSystem.HasAdvantage(Brand.SAVAGE, Brand.IRON));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void HasAdvantage_Returns_False_For_Neutral()
        {
            Assert.IsFalse(BrandSystem.HasAdvantage(Brand.IRON, Brand.GRACE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void HasDisadvantage_Returns_True_For_NotEffective()
        {
            Assert.IsTrue(BrandSystem.HasDisadvantage(Brand.IRON, Brand.SAVAGE));
            Assert.IsTrue(BrandSystem.HasDisadvantage(Brand.IRON, Brand.RUIN));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void HasDisadvantage_Returns_False_For_Neutral()
        {
            Assert.IsFalse(BrandSystem.HasDisadvantage(Brand.IRON, Brand.GRACE));
        }

        // ====================================================================
        // HYBRID BRANDS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void IsHybridBrand_Identifies_Hybrids()
        {
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.BLOODIRON));
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.RAVENOUS));
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.CORROSIVE));
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.TERRORFLUX));
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.VENOMSTRIKE));
            Assert.IsTrue(BrandSystem.IsHybridBrand(Brand.NIGHTLEECH));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void IsHybridBrand_Returns_False_For_Core_Brands()
        {
            Assert.IsFalse(BrandSystem.IsHybridBrand(Brand.IRON));
            Assert.IsFalse(BrandSystem.IsHybridBrand(Brand.SAVAGE));
            Assert.IsFalse(BrandSystem.IsHybridBrand(Brand.NONE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void GetCoreBrand_Resolves_Hybrid_To_Core()
        {
            Assert.AreEqual(Brand.IRON, BrandSystem.GetCoreBrand(Brand.BLOODIRON));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void GetCoreBrand_Returns_Same_For_Core_Brands()
        {
            Assert.AreEqual(Brand.IRON, BrandSystem.GetCoreBrand(Brand.IRON));
            Assert.AreEqual(Brand.SAVAGE, BrandSystem.GetCoreBrand(Brand.SAVAGE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Hybrid_Brand_Uses_Core_Effectiveness()
        {
            // BLOODIRON -> IRON core, so should be super effective vs SURGE
            float hybridEff = BrandSystem.GetEffectiveness(Brand.BLOODIRON, Brand.SURGE);
            float coreEff = BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE);
            Assert.AreEqual(coreEff, hybridEff);
        }

        // ====================================================================
        // DISPLAY NAMES
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void GetBrandName_Returns_TitleCase()
        {
            Assert.AreEqual("Iron", BrandSystem.GetBrandName(Brand.IRON));
            Assert.AreEqual("Savage", BrandSystem.GetBrandName(Brand.SAVAGE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void GetEffectivenessText_Returns_Correct_Strings()
        {
            Assert.IsTrue(BrandSystem.GetEffectivenessText(2.0f).Contains("2x"));
            Assert.IsTrue(BrandSystem.GetEffectivenessText(0.5f).Contains("0.5x"));
            Assert.AreEqual("", BrandSystem.GetEffectivenessText(1.0f));
        }

        // ====================================================================
        // MATRIX COMPLETENESS - Every brand has exactly 2 strengths and 2 weaknesses
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Every_Core_Brand_Has_Exactly_Two_Strengths_And_Two_Weaknesses()
        {
            Brand[] coreBrands = {
                Brand.IRON, Brand.SAVAGE, Brand.SURGE, Brand.VENOM, Brand.DREAD,
                Brand.LEECH, Brand.GRACE, Brand.MEND, Brand.RUIN, Brand.VOID
            };

            foreach (var attacker in coreBrands)
            {
                int strengths = 0;
                int weaknesses = 0;
                int neutrals = 0;

                foreach (var defender in coreBrands)
                {
                    float eff = BrandSystem.GetEffectiveness(attacker, defender);
                    if (eff >= 2.0f) strengths++;
                    else if (eff <= 0.5f) weaknesses++;
                    else neutrals++;
                }

                Assert.AreEqual(2, strengths,
                    $"{attacker} should have exactly 2 super-effective matchups, got {strengths}");
                Assert.AreEqual(2, weaknesses,
                    $"{attacker} should have exactly 2 not-effective matchups, got {weaknesses}");
                Assert.AreEqual(6, neutrals,
                    $"{attacker} should have exactly 6 neutral matchups, got {neutrals}");
            }
        }
    }
}

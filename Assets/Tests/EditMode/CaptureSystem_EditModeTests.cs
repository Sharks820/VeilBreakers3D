using NUnit.Framework;
using VeilBreakers.Capture;
using VeilBreakers.Data;

namespace VeilBreakers.Tests.EditMode
{
    public class CaptureSystem_EditModeTests
    {
        // ====================================================================
        // CAPTURE ITEM EFFECTIVENESS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Shard_Effective_On_Common()
        {
            Assert.AreEqual(40, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, MonsterRarity.COMMON));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Heart_Max_On_Common()
        {
            Assert.AreEqual(99, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, MonsterRarity.COMMON));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Shard_Ineffective_On_Legendary()
        {
            Assert.AreEqual(0, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, MonsterRarity.LEGENDARY));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Crystal_Ineffective_On_Legendary()
        {
            Assert.AreEqual(0, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CRYSTAL, MonsterRarity.LEGENDARY));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Core_Effective_On_Legendary()
        {
            Assert.AreEqual(15, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CORE, MonsterRarity.LEGENDARY));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Heart_Effective_On_Legendary()
        {
            Assert.AreEqual(25, CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, MonsterRarity.LEGENDARY));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Higher_Tier_Items_Always_Better_Or_Equal()
        {
            MonsterRarity[] rarities = {
                MonsterRarity.COMMON, MonsterRarity.UNCOMMON,
                MonsterRarity.RARE, MonsterRarity.EPIC
            };

            foreach (var rarity in rarities)
            {
                int shard = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_SHARD, rarity);
                int crystal = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CRYSTAL, rarity);
                int core = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_CORE, rarity);
                int heart = CaptureItemConfig.GetEffectiveness(CaptureItem.VEIL_HEART, rarity);

                Assert.GreaterOrEqual(crystal, shard, $"Crystal >= Shard for {rarity}");
                Assert.GreaterOrEqual(core, crystal, $"Core >= Crystal for {rarity}");
                Assert.GreaterOrEqual(heart, core, $"Heart >= Core for {rarity}");
            }
        }

        // ====================================================================
        // CAPTURE ITEM DISPLAY
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Capture_Items_Have_Display_Names()
        {
            Assert.AreEqual("Veil Shard", CaptureItemConfig.GetDisplayName(CaptureItem.VEIL_SHARD));
            Assert.AreEqual("Veil Heart", CaptureItemConfig.GetDisplayName(CaptureItem.VEIL_HEART));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Capture_Items_Have_Descriptions()
        {
            Assert.IsNotEmpty(CaptureItemConfig.GetDescription(CaptureItem.VEIL_CORE));
        }

        // ====================================================================
        // CAPTURE FORMULA - BASIC
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Capture_Chance_Is_Within_Bounds()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            var result = CaptureFormulaCalculator.Calculate(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);

            Assert.GreaterOrEqual(result.finalChance, CaptureFormulaConfig.MIN_CHANCE);
            Assert.LessOrEqual(result.finalChance, CaptureFormulaConfig.MAX_CHANCE);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Null_Monster_Returns_Zero_Chance()
        {
            var result = CaptureFormulaCalculator.Calculate(null, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);
            Assert.AreEqual(0f, result.finalChance);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void None_Item_Returns_Zero_Chance()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            var result = CaptureFormulaCalculator.Calculate(monster, CaptureItem.NONE, 10, QTEResult.GOOD);
            Assert.AreEqual(0f, result.finalChance);
        }

        // ====================================================================
        // CAPTURE FORMULA - QTE MODIFIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Better_QTE_Gives_Higher_Chance()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float miss = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);
            float okay = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.OKAY);
            float good = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);
            float perfect = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.PERFECT);

            Assert.Greater(okay, miss, "Okay > Miss");
            Assert.Greater(good, okay, "Good > Okay");
            Assert.Greater(perfect, good, "Perfect > Good");
        }

        // ====================================================================
        // CAPTURE FORMULA - CORRUPTION MODIFIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Ascended_Corruption_Better_Than_Abyssal()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            monster.currentCorruption = 5f; // ASCENDED
            float ascended = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);

            monster.currentCorruption = 90f; // ABYSSAL
            float abyssal = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);

            Assert.Greater(ascended, abyssal);
        }

        // ====================================================================
        // CAPTURE FORMULA - LEVEL MODIFIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Higher_Level_Player_Has_Better_Chance()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float higher = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 15, QTEResult.MISS);
            float same = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.MISS);
            float lower = CaptureFormulaCalculator.CalculateQuick(monster, CaptureItem.VEIL_CRYSTAL, 5, QTEResult.MISS);

            Assert.Greater(higher, same, "Higher level = better chance");
            Assert.Greater(same, lower, "Lower level = worse chance");
        }

        // ====================================================================
        // CAPTURE FORMULA - ITEM EFFECTIVENESS CHECK
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void IsItemEffective_Core_On_Legendary()
        {
            Assert.IsTrue(CaptureFormulaCalculator.IsItemEffective(CaptureItem.VEIL_CORE, MonsterRarity.LEGENDARY));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void IsItemEffective_Shard_Not_On_Legendary()
        {
            Assert.IsFalse(CaptureFormulaCalculator.IsItemEffective(CaptureItem.VEIL_SHARD, MonsterRarity.LEGENDARY));
        }

        // ====================================================================
        // CAPTURE FORMULA - CHANCE RANGE
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void GetChanceRange_Min_Less_Than_Max()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.25f,
                currentCorruption = 35f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            var (min, max) = CaptureFormulaCalculator.GetChanceRange(monster, CaptureItem.VEIL_CRYSTAL, 10);
            Assert.LessOrEqual(min, max);
        }

        // ====================================================================
        // CAPTURE FORMULA - INTEGRATION (BEST / WORST CASE)
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Best_Case_Capture_Over_90_Percent()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.15f,
                currentCorruption = 10f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            var result = CaptureFormulaCalculator.Calculate(monster, CaptureItem.VEIL_HEART, 15, QTEResult.PERFECT);
            Assert.Greater(result.finalChance, 0.90f, "Best case should have >90% chance");
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Worst_Case_Capture_Under_10_Percent()
        {
            var monster = new BoundMonsterData
            {
                boundAtHPPercent = 0.45f,
                currentCorruption = 90f,
                rarity = MonsterRarity.LEGENDARY,
                monsterLevel = 20
            };

            var result = CaptureFormulaCalculator.Calculate(monster, CaptureItem.VEIL_CORE, 10, QTEResult.MISS);
            Assert.Less(result.finalChance, 0.10f, "Worst case should have <10% chance");
        }

        // ====================================================================
        // BERSERK CHANCE
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Low_Corruption_Has_Low_Berserk_Chance()
        {
            var monster = new BoundMonsterData
            {
                currentCorruption = 10f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float chance = CaptureFormulaCalculator.GetBerserkChance(monster, 10);
            Assert.Less(chance, 0.5f);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void High_Corruption_Has_High_Berserk_Chance()
        {
            var monster = new BoundMonsterData
            {
                currentCorruption = 80f,
                rarity = MonsterRarity.COMMON,
                monsterLevel = 10
            };

            float chance = CaptureFormulaCalculator.GetBerserkChance(monster, 10);
            Assert.Greater(chance, 0.5f);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Epic_Rarity_Increases_Berserk_Chance()
        {
            var monster = new BoundMonsterData
            {
                currentCorruption = 35f,
                rarity = MonsterRarity.EPIC,
                monsterLevel = 10
            };

            float chance = CaptureFormulaCalculator.GetBerserkChance(monster, 10);
            Assert.Greater(chance, 0.5f);
        }

        // ====================================================================
        // BIND THRESHOLD CONFIG - CONSTANTS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Bind_Threshold_Constants_Match_Design()
        {
            Assert.AreEqual(0.25f, BindThresholdConfig.BASE_THRESHOLD);
            Assert.AreEqual(0.05f, BindThresholdConfig.MIN_THRESHOLD);
            Assert.AreEqual(0.50f, BindThresholdConfig.MAX_THRESHOLD);
        }

        // ====================================================================
        // CAPTURE FORMULA CONFIG - CONSTANTS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Capture")]
        public void Capture_Formula_Bounds_Match_Design()
        {
            Assert.AreEqual(0.01f, CaptureFormulaConfig.MIN_CHANCE);
            Assert.AreEqual(0.99f, CaptureFormulaConfig.MAX_CHANCE);
            Assert.AreEqual(0.50f, CaptureFormulaConfig.BASE_CHANCE);
        }
    }
}

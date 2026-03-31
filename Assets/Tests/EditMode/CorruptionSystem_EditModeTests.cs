using NUnit.Framework;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Tests.EditMode
{
    public class CorruptionSystem_EditModeTests
    {
        // ====================================================================
        // CORRUPTION STATE BOUNDARIES
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_0_Is_Ascended()
        {
            Assert.AreEqual(CorruptionState.ASCENDED, CorruptionSystem.GetCorruptionState(0f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_10_Is_Ascended()
        {
            Assert.AreEqual(CorruptionState.ASCENDED, CorruptionSystem.GetCorruptionState(10f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_11_Is_Purified()
        {
            Assert.AreEqual(CorruptionState.PURIFIED, CorruptionSystem.GetCorruptionState(10.01f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_25_Is_Purified()
        {
            Assert.AreEqual(CorruptionState.PURIFIED, CorruptionSystem.GetCorruptionState(25f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_26_Is_Unstable()
        {
            Assert.AreEqual(CorruptionState.UNSTABLE, CorruptionSystem.GetCorruptionState(25.01f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_50_Is_Unstable()
        {
            Assert.AreEqual(CorruptionState.UNSTABLE, CorruptionSystem.GetCorruptionState(50f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_51_Is_Corrupted()
        {
            Assert.AreEqual(CorruptionState.CORRUPTED, CorruptionSystem.GetCorruptionState(50.01f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_75_Is_Corrupted()
        {
            Assert.AreEqual(CorruptionState.CORRUPTED, CorruptionSystem.GetCorruptionState(75f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_76_Is_Abyssal()
        {
            Assert.AreEqual(CorruptionState.ABYSSAL, CorruptionSystem.GetCorruptionState(75.01f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_79_Is_Abyssal()
        {
            Assert.AreEqual(CorruptionState.ABYSSAL, CorruptionSystem.GetCorruptionState(79f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_80_Is_Untamed()
        {
            Assert.AreEqual(CorruptionState.UNTAMED, CorruptionSystem.GetCorruptionState(80f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_100_Is_Untamed()
        {
            Assert.AreEqual(CorruptionState.UNTAMED, CorruptionSystem.GetCorruptionState(100f));
        }

        // ====================================================================
        // EDGE CASES - CLAMPING
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Negative_Corruption_Clamps_To_Ascended()
        {
            Assert.AreEqual(CorruptionState.ASCENDED, CorruptionSystem.GetCorruptionState(-5f));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Over_100_Corruption_Clamps_To_Untamed()
        {
            Assert.AreEqual(CorruptionState.UNTAMED, CorruptionSystem.GetCorruptionState(150f));
        }

        // ====================================================================
        // STAT MULTIPLIERS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Ascended_Gives_25_Percent_Bonus()
        {
            Assert.AreEqual(1.25f, CorruptionSystem.GetStatMultiplier(CorruptionState.ASCENDED));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Purified_Gives_10_Percent_Bonus()
        {
            Assert.AreEqual(1.10f, CorruptionSystem.GetStatMultiplier(CorruptionState.PURIFIED));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Unstable_Is_Neutral()
        {
            Assert.AreEqual(1.0f, CorruptionSystem.GetStatMultiplier(CorruptionState.UNSTABLE));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corrupted_Gives_10_Percent_Penalty()
        {
            Assert.AreEqual(0.9f, CorruptionSystem.GetStatMultiplier(CorruptionState.CORRUPTED));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Abyssal_Gives_20_Percent_Penalty()
        {
            Assert.AreEqual(0.8f, CorruptionSystem.GetStatMultiplier(CorruptionState.ABYSSAL));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Untamed_Gives_20_Percent_Penalty()
        {
            Assert.AreEqual(0.8f, CorruptionSystem.GetStatMultiplier(CorruptionState.UNTAMED));
        }

        // ====================================================================
        // APPLY CORRUPTION MODIFIER
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void ApplyCorruptionModifier_Ascended_Increases_Stat()
        {
            int result = CorruptionSystem.ApplyCorruptionModifier(100, 5f);
            Assert.AreEqual(125, result);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void ApplyCorruptionModifier_Unstable_No_Change()
        {
            int result = CorruptionSystem.ApplyCorruptionModifier(100, 35f);
            Assert.AreEqual(100, result);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void ApplyCorruptionModifier_Abyssal_Decreases_Stat()
        {
            int result = CorruptionSystem.ApplyCorruptionModifier(100, 78f);
            Assert.AreEqual(80, result);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void ApplyCorruptionModifier_Untamed_Decreases_Stat()
        {
            int result = CorruptionSystem.ApplyCorruptionModifier(100, 90f);
            Assert.AreEqual(80, result);
        }

        // ====================================================================
        // STAT MULTIPLIER VIA PERCENT
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void GetStatMultiplier_By_Percent_Matches_By_State()
        {
            Assert.AreEqual(
                CorruptionSystem.GetStatMultiplier(CorruptionState.ASCENDED),
                CorruptionSystem.GetStatMultiplier(5f)
            );
            Assert.AreEqual(
                CorruptionSystem.GetStatMultiplier(CorruptionState.UNTAMED),
                CorruptionSystem.GetStatMultiplier(85f)
            );
        }

        // ====================================================================
        // PURIFICATION AND CORRUPTION GAIN
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Purification_Amount_Is_Positive()
        {
            float amount = CorruptionSystem.CalculatePurificationAmount(50f, 10f, 5f);
            Assert.Greater(amount, 0f);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Purification_At_High_Corruption_Is_Easier()
        {
            float highCorr = CorruptionSystem.CalculatePurificationAmount(80f, 10f, 5f);
            float lowCorr = CorruptionSystem.CalculatePurificationAmount(20f, 10f, 5f);
            Assert.Greater(highCorr, lowCorr);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_Gain_Is_Positive()
        {
            float amount = CorruptionSystem.CalculateCorruptionGain(50f, 10f, 5f);
            Assert.Greater(amount, 0f);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Corruption_Gain_Saturates_At_High_Corruption()
        {
            float highCorr = CorruptionSystem.CalculateCorruptionGain(80f, 10f, 5f);
            float lowCorr = CorruptionSystem.CalculateCorruptionGain(20f, 10f, 5f);
            Assert.Less(highCorr, lowCorr);
        }

        // ====================================================================
        // DISPLAY NAMES
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void GetCorruptionStateName_Returns_Correct_Names()
        {
            Assert.AreEqual("Ascended", CorruptionSystem.GetCorruptionStateName(CorruptionState.ASCENDED));
            Assert.AreEqual("Purified", CorruptionSystem.GetCorruptionStateName(CorruptionState.PURIFIED));
            Assert.AreEqual("Unstable", CorruptionSystem.GetCorruptionStateName(CorruptionState.UNSTABLE));
            Assert.AreEqual("Corrupted", CorruptionSystem.GetCorruptionStateName(CorruptionState.CORRUPTED));
            Assert.AreEqual("Abyssal", CorruptionSystem.GetCorruptionStateName(CorruptionState.ABYSSAL));
            Assert.AreEqual("Untamed", CorruptionSystem.GetCorruptionStateName(CorruptionState.UNTAMED));
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void GetStatBonusText_Returns_Correct_Strings()
        {
            Assert.AreEqual("+25%", CorruptionSystem.GetStatBonusText(CorruptionState.ASCENDED));
            Assert.AreEqual("-20%", CorruptionSystem.GetStatBonusText(CorruptionState.ABYSSAL));
            Assert.AreEqual("-20%", CorruptionSystem.GetStatBonusText(CorruptionState.UNTAMED));
        }

        // ====================================================================
        // CONSTANTS MATCH DESIGN DOC
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Thresholds_Match_Design_Values()
        {
            Assert.AreEqual(10f, CorruptionSystem.ASCENDED_THRESHOLD);
            Assert.AreEqual(25f, CorruptionSystem.PURIFIED_THRESHOLD);
            Assert.AreEqual(50f, CorruptionSystem.UNSTABLE_THRESHOLD);
            Assert.AreEqual(75f, CorruptionSystem.CORRUPTED_THRESHOLD);
            Assert.AreEqual(79f, CorruptionSystem.ABYSSAL_THRESHOLD);
            Assert.AreEqual(80f, CorruptionSystem.UNTAMED_THRESHOLD);
        }

        [Test]
        [Category("Suite.Core")]
        [Category("System.Corruption")]
        public void Multipliers_Match_Design_Values()
        {
            Assert.AreEqual(1.25f, CorruptionSystem.ASCENDED_BONUS);
            Assert.AreEqual(1.10f, CorruptionSystem.PURIFIED_BONUS);
            Assert.AreEqual(1.00f, CorruptionSystem.UNSTABLE_BONUS);
            Assert.AreEqual(0.90f, CorruptionSystem.CORRUPTED_BONUS);
            Assert.AreEqual(0.80f, CorruptionSystem.ABYSSAL_BONUS);
            Assert.AreEqual(0.80f, CorruptionSystem.UNTAMED_BONUS);
        }
    }
}

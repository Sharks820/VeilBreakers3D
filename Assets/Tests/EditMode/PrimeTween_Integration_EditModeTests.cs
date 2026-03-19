using NUnit.Framework;
using PrimeTween;

namespace VeilBreakers.Tests.EditMode
{
    public class PrimeTween_Integration_EditModeTests
    {
        // ====================================================================
        // PRIMETWEEN ASSEMBLY RESOLUTION
        // ====================================================================

        [Test]
        [Category("Suite.Integration")]
        [Category("System.Animation")]
        public void PrimeTween_AssemblyResolves()
        {
            // Verify the PrimeTween type is accessible (this test compiling IS the verification)
            Assert.IsNotNull(typeof(Tween));
            Assert.IsNotNull(typeof(Sequence));
        }

        [Test]
        [Category("Suite.Integration")]
        [Category("System.Animation")]
        public void PrimeTween_TweenCustom_TypeExists()
        {
            // Verify Tween.Custom method exists via reflection
            var method = typeof(Tween).GetMethod("Custom",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method, "Tween.Custom static method should be accessible");
        }
    }
}

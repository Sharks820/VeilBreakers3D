using NUnit.Framework;
using UnityEngine;
using VeilBreakers.UI.CharacterSelect;

namespace VeilBreakers.Tests.EditMode
{
    public class HeroThemeConfig_EditModeTests
    {
        // ====================================================================
        // INSTANCE CREATION
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Theme")]
        public void HeroThemeConfig_CanCreateInstance()
        {
            var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
            Assert.IsNotNull(config);
            Object.DestroyImmediate(config);
        }

        // ====================================================================
        // COLOR FIELDS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Theme")]
        public void HeroThemeConfig_HasRequiredColorFields()
        {
            var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
            Assert.IsNotNull(config.GetType().GetField("primaryColor"));
            Assert.IsNotNull(config.GetType().GetField("glowColor"));
            Assert.IsNotNull(config.GetType().GetField("darkColor"));
            Assert.IsNotNull(config.GetType().GetField("dissolveEdgeColor"));
            Object.DestroyImmediate(config);
        }

        // ====================================================================
        // MUSIC FIELDS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Theme")]
        public void HeroThemeConfig_HasRequiredMusicFields()
        {
            var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
            Assert.IsNotNull(config.GetType().GetField("musicIntensity"));
            Assert.IsNotNull(config.GetType().GetField("musicWarmth"));
            Assert.IsNotNull(config.GetType().GetField("musicTension"));
            Assert.IsNotNull(config.GetType().GetField("musicSynth"));
            Assert.IsNotNull(config.GetType().GetField("musicPerc"));
            Assert.IsNotNull(config.GetType().GetField("musicPad"));
            Assert.IsNotNull(config.GetType().GetField("musicFilter"));
            Object.DestroyImmediate(config);
        }

        // ====================================================================
        // OVERLAY FIELDS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Theme")]
        public void HeroThemeConfig_HasOverlayFields()
        {
            var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
            Assert.IsNotNull(config.GetType().GetField("scanlineOpacity"));
            Assert.IsNotNull(config.GetType().GetField("vignetteIntensity"));
            Assert.IsNotNull(config.GetType().GetField("veilGlowOpacity"));
            Object.DestroyImmediate(config);
        }

        // ====================================================================
        // DISSOLVE FIELDS
        // ====================================================================

        [Test]
        [Category("Suite.Core")]
        [Category("System.Theme")]
        public void HeroThemeConfig_HasDissolveFields()
        {
            var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
            Assert.IsNotNull(config.GetType().GetField("dissolveNoiseScale"));
            Assert.IsNotNull(config.GetType().GetField("dissolveDuration"));
            Assert.IsNotNull(config.GetType().GetField("glitchResolveSpeed"));
            Object.DestroyImmediate(config);
        }
    }
}

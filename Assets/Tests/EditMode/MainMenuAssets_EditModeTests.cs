using NUnit.Framework;
using UnityEngine;

namespace VeilBreakers.Tests.EditMode
{
    public class MainMenuAssets_EditModeTests
    {
        [Test]
        [Category("Suite.Smoke")]
        [Category("Suite.Integrity")]
        [Category("Phase.PreProd")]
        public void MainMenu_RequiredResourcesExist()
        {
            // These are required at runtime (non-editor) for title screen overlay VFX.
            Assert.NotNull(Resources.Load<Texture2D>("Art/UI/MainMenu/ember_particles"), "Missing Resources: ember_particles");
            Assert.NotNull(Resources.Load<Texture2D>("Art/UI/MainMenu/ash_particles"), "Missing Resources: ash_particles");
            Assert.NotNull(Resources.Load<Texture2D>("Art/UI/MainMenu/vignette_overlay"), "Missing Resources: vignette_overlay");
            Assert.NotNull(Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers_glow"), "Missing Resources: logo_veilbreakers_glow");
            Assert.NotNull(Resources.Load<Texture2D>("Art/UI/MainMenu/logo_veilbreakers"), "Missing Resources: logo_veilbreakers");
        }

        [Test]
        [Category("Suite.Smoke")]
        [Category("Suite.Integrity")]
        [Category("Phase.PreProd")]
        public void MainMenu_RequiredShadersExist()
        {
            // If any of these are missing, title screen VFX will silently disable layers.
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/BackGlow"), "Missing shader: VeilBreakers/VFX/BackGlow");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/LightRays"), "Missing shader: VeilBreakers/VFX/LightRays");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/HeatShimmer"), "Missing shader: VeilBreakers/VFX/HeatShimmer");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/EnergyPulse"), "Missing shader: VeilBreakers/VFX/EnergyPulse");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/ScrollingSmoke"), "Missing shader: VeilBreakers/VFX/ScrollingSmoke");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/FloatingParticles"), "Missing shader: VeilBreakers/VFX/FloatingParticles");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/LogoShimmer"), "Missing shader: VeilBreakers/VFX/LogoShimmer");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/VeilWisps"), "Missing shader: VeilBreakers/VFX/VeilWisps");
            Assert.NotNull(Shader.Find("VeilBreakers/VFX/Vignette"), "Missing shader: VeilBreakers/VFX/Vignette");
        }
    }
}

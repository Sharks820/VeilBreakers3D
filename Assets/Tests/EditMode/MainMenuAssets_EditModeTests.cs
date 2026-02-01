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
            // NOTE: VFX shaders removed in v4.45 (clean slate). Main menu now uses
            // CSS-based UI Toolkit effects instead of custom shaders.
            // When VFX shaders are recreated for URP, add assertions here.

            // Verify Unity's built-in UI shaders are available (required for UI Toolkit)
            Assert.NotNull(Shader.Find("UI/Default"), "Missing shader: UI/Default");
            Assert.NotNull(Shader.Find("Hidden/Internal-Colored"), "Missing shader: Hidden/Internal-Colored");
        }
    }
}

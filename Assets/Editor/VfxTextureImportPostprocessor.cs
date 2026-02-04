using UnityEditor;
using UnityEngine;

namespace VeilBreakers.Editor
{
    public class VfxTextureImportPostprocessor : AssetPostprocessor
    {
        private const string kParticleTexturesDir = "Assets/Resources/VFX/ParticleTextures/";
        private const string kLegacyMainMenuResourcesDir = "Assets/Resources/Art/UI/MainMenu/";
        private const string kLightningResourcesDir = "Assets/Resources/UI/Lightning/";

        private void OnPreprocessTexture()
        {
            if (assetPath == null) return;

            string normalized = assetPath.Replace('\\', '/');

            bool isParticleTexture = normalized.StartsWith(kParticleTexturesDir);
            bool isLegacyMainMenuTexture = normalized.StartsWith(kLegacyMainMenuResourcesDir) &&
                                           (normalized.EndsWith("ash_particles.png") ||
                                            normalized.EndsWith("ember_particles.png") ||
                                            normalized.EndsWith("vignette_overlay.png") ||
                                            normalized.EndsWith("mainmenu_background_portal.png") ||
                                            normalized.EndsWith("mainmenu_buttons_sheet.png"));
            bool isMainMenuButtonSheet = normalized.EndsWith("mainmenu_buttons_sheet.png");
            bool isLightningTexture = normalized.StartsWith(kLightningResourcesDir);

            if (!isParticleTexture && !isLegacyMainMenuTexture && !isLightningTexture) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = !isParticleTexture;
            importer.mipmapEnabled = !(isLightningTexture || isLegacyMainMenuTexture);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 0;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 50;
            importer.isReadable = isLightningTexture || isMainMenuButtonSheet;

            if (isParticleTexture)
            {
                importer.maxTextureSize = 1024;
            }
        }
    }
}

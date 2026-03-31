using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VeilBreakers.UI.CharacterSelect;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// One-shot editor utility to create per-hero VolumeProfile assets with distinct
    /// post-processing overrides, then assign them to the corresponding HeroThemeConfig
    /// ScriptableObjects.
    ///
    /// Run via menu: Tools > Generate Hero Volume Profiles
    /// Safe to re-run: overwrites existing profile assets.
    /// Can be deleted after running successfully.
    /// </summary>
    public static class GenerateHeroVolumeProfiles
    {
        private const string kOutputFolder = "Assets/Resources/CharacterSelect/HeroThemes";
        private const string kThemeSearchFolder = "Assets/Resources/CharacterSelect/HeroThemes";

        // =====================================================================
        // Menu Item
        // =====================================================================

        [MenuItem("Tools/Generate Hero Volume Profiles", false, 200)]
        public static void Generate()
        {
            ErrorLogger.Log("[GenerateHeroVolumeProfiles] Starting VolumeProfile generation...");

            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(kOutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/CharacterSelect", "HeroThemes");
            }

            CreateVexProfile();
            CreateSeraphinaProfile();
            CreateOrionProfile();
            CreateNyxProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ErrorLogger.Log("[GenerateHeroVolumeProfiles] All 4 VolumeProfile assets created and assigned.");
        }

        // =====================================================================
        // Per-Hero Profile Generators
        // =====================================================================

        private static void CreateVexProfile()
        {
            // Warm, fiery: orange bloom, dark brown vignette, warm white color filter
            var profile = CreateProfile("VolProfile_Vex");

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1.0f, 0.7f, 0.5f, 1.0f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            vignette.rounded.Override(true);
            vignette.color.Override(new Color(0.25f, 0.1f, 0.05f, 1.0f));

            var dof = profile.Add<DepthOfField>(true);
            dof.gaussianStart.Override(10f);
            dof.gaussianEnd.Override(30f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.Override(15f);
            colorAdj.contrast.Override(10f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.95f, 0.9f, 1.0f));

            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);

            SaveAndAssign(profile, "VolProfile_Vex", "vex");
        }

        private static void CreateSeraphinaProfile()
        {
            // Ethereal, mystic: lavender bloom, deep purple vignette, cool white filter
            var profile = CreateProfile("VolProfile_Seraphina");

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.0f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(0.8f, 0.6f, 1.0f, 1.0f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.20f);
            vignette.smoothness.Override(0.4f);
            vignette.rounded.Override(true);
            vignette.color.Override(new Color(0.14f, 0.08f, 0.2f, 1.0f));

            var dof = profile.Add<DepthOfField>(true);
            dof.gaussianStart.Override(8f);
            dof.gaussianEnd.Override(25f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.Override(5f);
            colorAdj.contrast.Override(5f);
            colorAdj.colorFilter.Override(new Color(0.95f, 0.9f, 1.0f, 1.0f));

            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);

            SaveAndAssign(profile, "VolProfile_Seraphina", "seraphina");
        }

        private static void CreateOrionProfile()
        {
            // Tight, focused: cool blue bloom, dark blue vignette, blue-white filter
            var profile = CreateProfile("VolProfile_Orion");

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(0.6f, 0.5f, 1.0f, 1.0f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.40f);
            vignette.smoothness.Override(0.4f);
            vignette.rounded.Override(true);
            vignette.color.Override(new Color(0.08f, 0.06f, 0.2f, 1.0f));

            var dof = profile.Add<DepthOfField>(true);
            dof.gaussianStart.Override(6f);
            dof.gaussianEnd.Override(20f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.Override(12f);
            colorAdj.contrast.Override(15f);
            colorAdj.colorFilter.Override(new Color(0.92f, 0.92f, 1.0f, 1.0f));

            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);

            SaveAndAssign(profile, "VolProfile_Orion", "orion");
        }

        private static void CreateNyxProfile()
        {
            // Unstable, chaotic: blood red bloom, deep crimson vignette, warm red tint, high contrast
            var profile = CreateProfile("VolProfile_Nyx");

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1.0f, 0.3f, 0.35f, 1.0f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);
            vignette.rounded.Override(true);
            vignette.color.Override(new Color(0.22f, 0.04f, 0.04f, 1.0f));

            var dof = profile.Add<DepthOfField>(true);
            dof.gaussianStart.Override(5f);
            dof.gaussianEnd.Override(18f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.Override(20f);
            colorAdj.contrast.Override(15f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.9f, 0.9f, 1.0f));

            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);

            SaveAndAssign(profile, "VolProfile_Nyx", "nyx");
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static VolumeProfile CreateProfile(string name)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name;
            return profile;
        }

        private static void SaveAndAssign(VolumeProfile profile, string fileName, string heroId)
        {
            var assetPath = $"{kOutputFolder}/{fileName}.asset";

            // Create or overwrite the asset
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(profile, assetPath);
            ErrorLogger.Log($"[GenerateHeroVolumeProfiles] Created {assetPath}");

            // Find the matching HeroThemeConfig and assign the volumeProfile
            var guids = AssetDatabase.FindAssets($"t:{nameof(HeroThemeConfig)}", new[] { kThemeSearchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var theme = AssetDatabase.LoadAssetAtPath<HeroThemeConfig>(path);
                if (theme != null && theme.heroId == heroId)
                {
                    Undo.RecordObject(theme, $"Assign {fileName} to {theme.name}");
                    theme.volumeProfile = profile;
                    EditorUtility.SetDirty(theme);
                    ErrorLogger.Log($"[GenerateHeroVolumeProfiles] Assigned {fileName} to {theme.name}");
                    break;
                }
            }
        }
    }
}

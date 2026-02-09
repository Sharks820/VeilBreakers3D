using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// AssetPostprocessor that auto-configures Mixamo animation FBX files for Vex.
    /// Sets up humanoid avatar from Vex's source model and configures loop settings
    /// based on clip names.
    /// </summary>
    public sealed class MixamoAnimationImporter : AssetPostprocessor
    {
        private const string kVexAnimationPath = "Assets/Art/Animations/Characters/Vex/";
        private static readonly string[] kVexAvatarSourceModelPaths =
        {
            "Assets/Resources/Art/3D_Models/Characters/Vex.fbx",
            "Assets/Resources/Art/3D_Models/Characters/Vex_for_mixamo.fbx",
            "Assets/Resources/Art/3D_Models/Characters/Vex_mesh_only.fbx"
        };
        private const string kLogPrefix = "[MixamoAnimationImporter]";

        // Clip name keywords that should loop
        private static readonly string[] _loopingKeywords = { "idle", "walk", "run" };

        /// <summary>
        /// Configure model import settings for Mixamo FBX files targeting Vex.
        /// Sets humanoid animation type and copies avatar from the Vex source model.
        /// </summary>
        private void OnPreprocessModel()
        {
            if (!IsVexAnimationFbx())
                return;

            var importer = assetImporter as ModelImporter;
            if (importer == null)
                return;

            // Configure as humanoid animation using Vex's avatar
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;

            // Load the source avatar from Vex's main model
            Avatar sourceAvatar = LoadVexAvatar();
            if (sourceAvatar != null)
            {
                importer.sourceAvatar = sourceAvatar;
                Debug.Log($"{kLogPrefix} Configured '{assetPath}' to use Vex avatar.");
            }
            else
            {
                Debug.LogWarning(
                    $"{kLogPrefix} Could not load Vex avatar from configured source models. " +
                    "Avatar setup will fall back to creating from this model.");
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }

            // Minimal geometry settings for animation-only FBX
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.High;
            importer.isReadable = false;
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
        }

        /// <summary>
        /// Configure animation clip settings. Sets loop time based on clip name keywords.
        /// Clips containing "idle", "walk", or "run" are set to loop.
        /// </summary>
        private void OnPreprocessAnimation()
        {
            if (!IsVexAnimationFbx())
                return;

            var importer = assetImporter as ModelImporter;
            if (importer == null)
                return;

            ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
            {
                Debug.Log($"{kLogPrefix} No animation clips found in '{assetPath}'.");
                return;
            }

            foreach (ModelImporterClipAnimation clip in clipAnimations)
            {
                bool shouldLoop = ShouldClipLoop(clip.name);
                clip.loopTime = shouldLoop;

                Debug.Log(
                    $"{kLogPrefix} Clip '{clip.name}' in '{assetPath}' - " +
                    $"loopTime={shouldLoop}");
            }

            importer.clipAnimations = clipAnimations;
        }

        /// <summary>
        /// Determines whether a clip should loop based on its name.
        /// Matches against looping keywords (case insensitive).
        /// </summary>
        private static bool ShouldClipLoop(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return false;

            string lowerName = clipName.ToLowerInvariant();
            return _loopingKeywords.Any(keyword => lowerName.Contains(keyword));
        }

        /// <summary>
        /// Checks if the current asset is an FBX file under the Vex animation folder.
        /// </summary>
        private bool IsVexAnimationFbx()
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            string normalized = assetPath.Replace('\\', '/');

            return normalized.StartsWith(kVexAnimationPath, System.StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads Vex's humanoid avatar from the source FBX model.
        /// </summary>
        private static Avatar LoadVexAvatar()
        {
            for (int i = 0; i < kVexAvatarSourceModelPaths.Length; i++)
            {
                string sourcePath = kVexAvatarSourceModelPaths[i];
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
                if (allAssets == null || allAssets.Length == 0)
                {
                    continue;
                }

                foreach (Object asset in allAssets)
                {
                    if (asset is Avatar avatar)
                        return avatar;
                }
            }

            Debug.LogWarning($"{kLogPrefix} No Avatar found in any configured source model path.");
            return null;
        }

        /// <summary>
        /// Menu item to force reimport all FBX files in the Vex animation folder.
        /// Useful after changing import settings or when animations need refreshing.
        /// </summary>
        [MenuItem("VeilBreakers/Animation/Reimport Vex Animations")]
        private static void ReimportVexAnimations()
        {
            string fullPath = Path.Combine(Application.dataPath,
                kVexAnimationPath.Replace("Assets/", ""));

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning(
                    $"{kLogPrefix} Vex animation folder not found at '{fullPath}'. " +
                    "Nothing to reimport.");
                return;
            }

            string[] fbxFiles = Directory.GetFiles(fullPath, "*.fbx", SearchOption.AllDirectories);
            if (fbxFiles.Length == 0)
            {
                Debug.LogWarning($"{kLogPrefix} No FBX files found in '{kVexAnimationPath}'.");
                return;
            }

            int reimportCount = 0;
            foreach (string fbxPath in fbxFiles)
            {
                // Convert absolute path to Unity asset path
                string relativePath = "Assets" + fbxPath
                    .Replace(Application.dataPath, "")
                    .Replace('\\', '/');

                AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                reimportCount++;
            }

            Debug.Log($"{kLogPrefix} Reimported {reimportCount} Vex animation FBX file(s).");
            AssetDatabase.Refresh();
        }
    }
}

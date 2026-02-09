using UnityEngine;
using UnityEditor;
using System.IO;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Enhanced import postprocessor for Blender FBX files.
    /// Automatically applies optimal settings for models exported from Blender.
    /// </summary>
    public sealed class BlenderFBXImportPostprocessor : AssetPostprocessor
    {
        // Base paths for different model types
        private const string kCharactersPath = "Assets/Resources/Art/3D_Models/Characters/";
        private const string kPropsPath = "Assets/Resources/Art/3D_Models/Props/";
        private const string kEnvironmentPath = "Assets/Resources/Art/3D_Models/Environment/";
        private const string kAnimationsPath = "Assets/Resources/Art/3D_Models/Animations/";
        private const string kVfxPath = "Assets/Resources/Art/3D_Models/VFX/";
        
        // Run after CharacterModelImportPostprocessor (default order 0) to override its settings
        public override int GetPostprocessOrder() => 100;

        private void OnPreprocessModel()
        {
            if (assetPath == null) return;
            
            string normalized = assetPath.Replace('\\', '/');
            
            // Check if this is a model file
            if (!normalized.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) &&
                !normalized.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase) &&
                !normalized.EndsWith(".blend", System.StringComparison.OrdinalIgnoreCase) &&
                !normalized.EndsWith(".gltf", System.StringComparison.OrdinalIgnoreCase) &&
                !normalized.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            // Apply settings based on folder type
            if (normalized.StartsWith(kCharactersPath, System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigureCharacterImport();
            }
            else if (normalized.StartsWith(kPropsPath, System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigurePropImport();
            }
            else if (normalized.StartsWith(kEnvironmentPath, System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigureEnvironmentImport();
            }
            else if (normalized.StartsWith(kAnimationsPath, System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigureAnimationImport();
            }
            else if (normalized.StartsWith(kVfxPath, System.StringComparison.OrdinalIgnoreCase))
            {
                ConfigureVfxImport();
            }
            else
            {
                // Default settings for other models
                ConfigureDefaultImport();
            }
        }
        
        /// <summary>
        /// Optimized settings for character models exported from Blender
        /// </summary>
        private void ConfigureCharacterImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Geometry
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            
            // Don't import scene objects from Blender
            importer.importCameras = false;
            importer.importLights = false;
            
            // Animation and Rigging
            importer.importAnimation = true;
            importer.importBlendShapes = true;  // ARKit blend shapes
            importer.animationType = ModelImporterAnimationType.Human;  // For humanoid characters
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            
            // Material handling
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            
            // FBX specific: Preserve Blender's coordinate system
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            // Collision
            importer.addCollider = false;
            
            Debug.Log($"[DCC Bridge] Applied character import settings to: {assetPath}");
        }
        
        /// <summary>
        /// Optimized settings for static props
        /// </summary>
        private void ConfigurePropImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Geometry - higher compression for static props
            importer.meshCompression = ModelImporterMeshCompression.High;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            
            // No animation for static props
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.None;
            
            // Material handling
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            // Props might need colliders
            importer.addCollider = false;  // Add manually in prefab
            
            Debug.Log($"[DCC Bridge] Applied prop import settings to: {assetPath}");
        }
        
        /// <summary>
        /// Optimized settings for environment pieces
        /// </summary>
        private void ConfigureEnvironmentImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Geometry
            importer.meshCompression = ModelImporterMeshCompression.Low;  // Keep quality for large env pieces
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            
            // No animation
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.None;
            
            // Material handling
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            // Environment pieces need colliders
            importer.addCollider = true;
            
            Debug.Log($"[DCC Bridge] Applied environment import settings to: {assetPath}");
        }
        
        /// <summary>
        /// Optimized settings for animation files
        /// </summary>
        private void ConfigureAnimationImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Animation focused
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.animationType = ModelImporterAnimationType.Human;
            
            // Minimal geometry
            importer.meshCompression = ModelImporterMeshCompression.High;
            importer.isReadable = false;
            importer.importCameras = false;
            importer.importLights = false;
            
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            importer.addCollider = false;
            
            Debug.Log($"[DCC Bridge] Applied animation import settings to: {assetPath}");
        }
        
        /// <summary>
        /// Optimized settings for VFX meshes
        /// </summary>
        private void ConfigureVfxImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Geometry - readable for mesh manipulation
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = true;  // Needed for mesh effects
            importer.optimizeMeshPolygons = false;  // Keep original topology
            importer.optimizeMeshVertices = false;
            importer.weldVertices = false;
            
            // Animation
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            
            // Don't import extras
            importer.importCameras = false;
            importer.importLights = false;
            
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            importer.addCollider = false;
            
            Debug.Log($"[DCC Bridge] Applied VFX import settings to: {assetPath}");
        }
        
        /// <summary>
        /// Default import settings
        /// </summary>
        private void ConfigureDefaultImport()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            
            // Balanced settings
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            
            importer.importCameras = false;
            importer.importLights = false;
            
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            
            importer.addCollider = false;
        }
        
        /// <summary>
        /// Post-process the model after import to set up materials and components
        /// </summary>
        private void OnPostprocessModel(GameObject go)
        {
            // Additional post-processing can be done here
            // e.g., set up materials, add components, etc.
            
            string normalized = assetPath.Replace('\\', '/');
            
            if (normalized.StartsWith(kCharactersPath, System.StringComparison.OrdinalIgnoreCase))
            {
                SetupCharacterGameObject(go);
            }
        }
        
        private void SetupCharacterGameObject(GameObject go)
        {
            // Ensure the model has the proper tags/layers
            // This can be extended to add components, set up materials, etc.
            
            var animator = go.GetComponent<Animator>();
            if (animator != null)
            {
                // Ensure animator is properly configured
                animator.applyRootMotion = true;
            }
        }
        
        /// <summary>
        /// Called when assets are imported, deleted, or moved
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool needsRefresh = false;
            
            foreach (string assetPath in importedAssets)
            {
                if (IsBlenderExport(assetPath))
                {
                    Debug.Log($"[DCC Bridge] Detected Blender export: {assetPath}");
                    needsRefresh = true;
                }
            }
            
            // NOTE: Do NOT call AssetDatabase.Refresh() here - it causes
            // infinite reimport loops when called inside OnPostprocessAllAssets
        }
        
        private static bool IsBlenderExport(string assetPath)
        {
            // Check if file was recently modified (within last minute)
            // This is a simple heuristic to detect fresh exports
            string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            fullPath = Path.GetFullPath(fullPath);
            
            if (File.Exists(fullPath))
            {
                var lastWrite = File.GetLastWriteTime(fullPath);
                var timeSinceWrite = System.DateTime.Now - lastWrite;
                return timeSinceWrite.TotalMinutes < 5;
            }
            
            return false;
        }
    }
}

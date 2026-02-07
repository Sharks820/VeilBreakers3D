using UnityEditor;

namespace VeilBreakers.Editor
{
    public sealed class CharacterModelImportPostprocessor : AssetPostprocessor
    {
        private const string kCharacterModelDir = "Assets/Resources/Art/3D_Models/Characters/";

        private void OnPreprocessModel()
        {
            if (assetPath == null) return;

            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(kCharacterModelDir, System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (ModelImporter)assetImporter;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.addCollider = false;
        }
    }
}

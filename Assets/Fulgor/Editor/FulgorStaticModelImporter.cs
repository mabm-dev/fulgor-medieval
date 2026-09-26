using UnityEditor;

namespace Fulgor.Editor
{
    /// <summary>Import policy for static validation and terrain FBX assets only.</summary>
    public sealed class FulgorStaticModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                !(assetPath.StartsWith("Assets/Fulgor/Art/Validation/", System.StringComparison.Ordinal) ||
                  assetPath.StartsWith("Assets/Fulgor/Art/Terrain/", System.StringComparison.Ordinal)))
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
        }
    }
}

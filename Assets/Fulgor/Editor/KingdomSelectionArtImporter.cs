using UnityEditor;
using UnityEngine;
namespace Fulgor.Editor
{
    public sealed class KingdomSelectionArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Fulgor/Resources/KingdomSelection/"))return;
            var importer=(TextureImporter)assetImporter;importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;
        }
    }
}

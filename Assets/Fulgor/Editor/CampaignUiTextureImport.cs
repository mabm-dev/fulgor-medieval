using System;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>
    /// Importación de las imágenes de la interfaz (portada y menús, HUD, ciudad en el mapa, selección de reino): sin mipmaps, porque se ven a su
    /// tamaño y con mipmaps salían más blandas; hasta 4096, porque los paneles miden 2172 de ancho y se reducían a 2048; y compresión de calidad
    /// alta (BC7 en PC), porque con la normal (BC1) los degradados oscuros de los paneles se veían a manchas.
    /// Uso: -executeMethod Fulgor.Editor.CampaignUiTextureImport.Apply
    /// </summary>
    public static class CampaignUiTextureImport
    {
        static readonly string[] Folders =
        {
            "Assets/Fulgor/Resources/LegacyUI", "Assets/Fulgor/Resources/CityMap", "Assets/Fulgor/Resources/CampaignHud",
            "Assets/Fulgor/Resources/KingdomSelection",
        };

        [MenuItem("Fulgor/Interfaz/Ajustar la importación de las imágenes")]
        public static void Apply()
        {
            try
            {
                int changed = 0, checkedCount = 0;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", Folders))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                    checkedCount++;
                    var standalone = importer.GetPlatformTextureSettings("Standalone");
                    bool ready = !importer.mipmapEnabled && importer.maxTextureSize >= 4096 && importer.textureCompression == TextureImporterCompression.CompressedHQ
                                 && !importer.crunchedCompression && !standalone.overridden;
                    if (ready) continue;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 4096;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.compressionQuality = 100;
                    importer.crunchedCompression = false;
                    importer.filterMode = FilterMode.Bilinear;
                    if (standalone.overridden) { standalone.overridden = false; importer.SetPlatformTextureSettings(standalone); }
                    importer.SaveAndReimport();
                    changed++;
                    Debug.Log("Imagen de interfaz ajustada: " + path);
                }
                Debug.Log($"INTERFAZ_IMAGENES: {changed} de {checkedCount} ajustadas");
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}

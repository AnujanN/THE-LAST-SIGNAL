using UnityEditor;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Import settings for downloaded surface textures (Assets/Art/Textures/PolyHaven/...): "_nor" files are normal maps,
    /// everything is capped at 1024 px and mip-mapped (Android compresses to ASTC by default).
    /// </summary>
    public class TextureImportRules : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/Art/Textures/PolyHaven/")) return; // generated textures set their own import settings
            var importer = (TextureImporter)assetImporter;
            bool normal = path.Contains("_nor");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.mipmapEnabled = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}

using UnityEditor;

namespace RealmCommander.Editor
{
    public sealed class GeneratedArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;

            if (assetPath.Contains("/Icons/") || assetPath.Contains("/World/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            }
            else if (assetPath.Contains("/Terrain/"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            }
        }
    }
}

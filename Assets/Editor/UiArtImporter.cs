using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Imports the drawn UI art in Assets/Resources/UI as crisp, uncompressed sprites, with nine-slice borders on the
    /// panels so they stretch to any size and keep their round corners. The art comes from Tools/make_ui_art.py.
    /// </summary>
    public class UiArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            Vector4 border = Vector4.zero;
            switch (name)
            {
                case "panel":
                case "panel_edge": border = new Vector4(32, 32, 32, 32); break;
                case "pill": border = new Vector4(31, 31, 31, 31); break;
                case "shadow": border = new Vector4(64, 64, 64, 64); break;
                case "banner": border = new Vector4(120, 0, 120, 0); break;
            }
            importer.spriteBorder = border;
        }
    }
}

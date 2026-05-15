using UnityEditor;
using UnityEngine;

// Постпроцессор: все PNG-плейсхолдеры в Art/Generated/ импортируются как Sprite
// c PPU=128, чтобы у 128×128 спрайта мишени размер был ровно 1 unit.
// Срабатывает автоматически при первом импорте Unity'ем сразу после AssetForge
// записал PNG в проект.
public class GeneratedTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/_Project/Art/Generated/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}

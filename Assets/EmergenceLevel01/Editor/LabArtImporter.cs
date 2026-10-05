using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public sealed class LabArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/EmergenceLevel01/Art/") || !assetPath.EndsWith(".png")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = assetPath.Contains("/OriginalBody/head_") ? 128 : assetPath.Contains("/RobotModular/") ? 48 : (assetPath.Contains("/Tiles64/") ? 64 : 16);
            t.filterMode = FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.npotScale = TextureImporterNPOTScale.None;
            t.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            t.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = assetPath.Contains("/Characters/") ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
            t.SetTextureSettings(settings);
        }
    }
}

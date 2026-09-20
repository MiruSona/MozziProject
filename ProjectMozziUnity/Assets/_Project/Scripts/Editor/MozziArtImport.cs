using Mozzi.Game.View;
using UnityEditor;
using UnityEngine;

namespace Mozzi.EditorTools
{
    // Resources/Art/ 아래 그림은 전부 같은 임포트 설정이어야 한다.
    // 손으로 맞추면 한 장씩 빠지니 넣을 때 자동으로 준다.
    public sealed class MozziArtImport : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Resources/Art/";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ArtRoot) == false)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = LayoutConst.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;

            // pivot 과 mesh 종류는 importer 바로 위에 없어 설정 묶음으로 돌려 준다.
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}

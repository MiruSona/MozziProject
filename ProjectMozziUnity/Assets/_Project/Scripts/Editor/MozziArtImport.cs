using Mozzi.Game.View;
using UnityEditor;
using UnityEngine;

namespace Mozzi.EditorTools
{
    // Resources/Art/ 아래 그림은 전부 같은 임포트 설정이어야 한다.
    // 손으로 맞추면 한 장씩 빠지니 넣을 때 자동으로 준다.
    // 피벗만 그림 갈래마다 다르다 — 바닥에 서는 메인 화면 그림은 아래 가운데, 나머지는 가운데 (PivotFor).
    public sealed class MozziArtImport : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Resources/Art/";

        // 아래 가운데 피벗을 받는 자리. 메인 화면 그림(하늘 · 바닥 · 배경) 폴더 전부와,
        // 어느 폴더에 있든 이름이 _main 으로 끝나는 그림(새 건물 메인 화면용 등)이다.
        private const string BottomPivotFolder = ArtRoot + "Main/";
        private const string BottomPivotNameSuffix = "_main";

        // 농장 장식 조각(decor_00~). 32 칸 바닥에 세워 놓으니 아래 가운데다.
        private const string BottomPivotNamePrefix = "decor_";

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
            settings.spriteAlignment = (int)PivotFor(assetPath);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteBorder = BorderFor(assetPath);
            importer.SetTextureSettings(settings);
        }

        // 9-slice 틀 그림의 테두리 px. Unity 순서는 (왼, 아래, 오른, 위) 다 — README 는 (왼 · 위 · 오른 · 아래) 로 적혀 있으니 옮길 때 조심.
        // 정본 : Docs/Reference/Art/정식/가판대/README.md 의 ui_button · ui_bubble 줄. 표에 없으면 테두리 없음.
        public static Vector4 BorderFor(string path)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == "ui_button")
            {
                return new Vector4(10f, 10f, 10f, 10f);
            }

            if (name == "ui_bubble")
            {
                return new Vector4(14f, 20f, 14f, 12f);
            }

            // 정본 : 정식/합성소/README.md 의 synthesis_plate 줄 (왼 12 · 위 8 · 오른 12 · 아래 8).
            if (name == "synthesis_plate")
            {
                return new Vector4(12f, 8f, 12f, 8f);
            }

            return Vector4.zero;
        }

        // 이 그림의 피벗. 규칙에 안 걸리면 예전 그대로 가운데라 기존 그림의 meta 는 안 바뀐다.
        public static SpriteAlignment PivotFor(string path)
        {
            if (path.StartsWith(BottomPivotFolder))
            {
                return SpriteAlignment.BottomCenter;
            }

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith(BottomPivotNameSuffix))
            {
                return SpriteAlignment.BottomCenter;
            }

            if (name.StartsWith(BottomPivotNamePrefix))
            {
                return SpriteAlignment.BottomCenter;
            }

            return SpriteAlignment.Center;
        }
    }
}

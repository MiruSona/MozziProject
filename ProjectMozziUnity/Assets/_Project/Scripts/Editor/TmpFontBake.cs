using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Mozzi.Editor
{
    // ONE Mobile POP ttf 로 TMP 폰트 자산 두 장을 굽는다. 주 자산은 Static, 폴백은 빈 Dynamic 이다.
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.TmpFontBake.Bake();` 한 줄. 5초를 넘겨도 에디터 안에서 끝까지 돈다.
    public static class TmpFontBake
    {
        private const string FontPath = "Assets/_Project/Fonts/ONEMobilePOP.ttf";
        private const string CharsPath = "Assets/_Project/Fonts/ONEMobilePOP-chars.txt";
        private const string MainPath = "Assets/_Project/Fonts/ONEMobilePOP SDF.asset";
        private const string FallbackPath = "Assets/_Project/Fonts/ONEMobilePOP SDF Dynamic.asset";
        private const int Padding = 4;
        private const int FallbackAtlasSize = 1024;
        private const int MaxSamplingSize = 36;
        private const int MinSamplingSize = 24;
        private const int SamplingStep = 2;

        private static readonly int[] AtlasSizes = new int[] { 2048, 4096 };

        // 굽는 도중 다른 스크립트 변경으로 도메인이 다시 실리면 반쯤 된 자산이 남는다. 그래서 잠근다.
        public static void Bake()
        {
            EditorApplication.LockReloadAssemblies();
            try
            {
                BakeLocked();
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void BakeLocked()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[TmpFontBake] 폰트가 없다 : " + FontPath);
                return;
            }

            if (File.Exists(MainPath) || File.Exists(FallbackPath))
            {
                Debug.LogError("[TmpFontBake] 자산이 이미 있다. 다시 구우려면 먼저 지운다 : " + MainPath);
                return;
            }

            string wanted = File.ReadAllText(CharsPath, Encoding.UTF8);
            string notInFont;
            string bakeChars = SplitByFont(font, wanted, out notInFont);

            TMP_FontAsset main = BakeSmallest(font, bakeChars);
            if (main == null)
            {
                Debug.LogError("[TmpFontBake] 4096 아틀라스에도 글자가 다 안 들어간다");
                return;
            }

            main.atlasPopulationMode = AtlasPopulationMode.Static;
            SaveWithSubAssets(main, MainPath);

            TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(font, (int)main.faceInfo.pointSize, Padding,
                GlyphRenderMode.SDFAA, FallbackAtlasSize, FallbackAtlasSize, AtlasPopulationMode.Dynamic, true);
            SaveWithSubAssets(fallback, FallbackPath);
            fallback.ClearFontAssetData(true);

            if (main.fallbackFontAssetTable == null)
            {
                main.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            main.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(main);
            EditorUtility.SetDirty(fallback);

            SetDefaultFont(main);
            AssetDatabase.SaveAssets();

            Debug.Log("[TmpFontBake] 끝 — 아틀라스 " + main.atlasWidth + "x" + main.atlasHeight
                + " · 샘플링 " + main.faceInfo.pointSize + " · 글자 " + main.characterTable.Count
                + " / 바란 글자 " + wanted.Length + " · 폰트에 없는 글자 [" + notInFont + "]");
        }

        // 폰트 파일에 글리프가 있는 글자만 돌려주고, 없는 글자는 notInFont 로 뺀다
        private static string SplitByFont(Font font, string wanted, out string notInFont)
        {
            StringBuilder has = new StringBuilder();
            StringBuilder none = new StringBuilder();
            FontEngine.InitializeFontEngine();
            FontEngine.LoadFontFace(font, MaxSamplingSize);
            for (int i = 0; i < wanted.Length; i++)
            {
                uint glyphIndex;
                bool found = FontEngine.TryGetGlyphIndex(wanted[i], out glyphIndex);
                if (found)
                {
                    has.Append(wanted[i]);
                    continue;
                }

                none.Append(wanted[i]);
            }

            notInFont = none.ToString();
            return has.ToString();
        }

        // 작은 아틀라스부터, 그 안에서는 큰 샘플링 크기부터 넣어 보고 처음으로 다 들어간 것을 쓴다
        private static TMP_FontAsset BakeSmallest(Font font, string chars)
        {
            for (int a = 0; a < AtlasSizes.Length; a++)
            {
                for (int size = MaxSamplingSize; size >= MinSamplingSize; size -= SamplingStep)
                {
                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, size, Padding,
                        GlyphRenderMode.SDFAA, AtlasSizes[a], AtlasSizes[a], AtlasPopulationMode.Dynamic, false);
                    string missing;
                    bool ok = asset.TryAddCharacters(chars, out missing);
                    if (ok)
                    {
                        return asset;
                    }

                    DestroyTemp(asset);
                }
            }

            return null;
        }

        private static void DestroyTemp(TMP_FontAsset asset)
        {
            Object.DestroyImmediate(asset.material);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                Object.DestroyImmediate(asset.atlasTextures[i]);
            }

            Object.DestroyImmediate(asset);
        }

        // 아틀라스 텍스처와 머티리얼을 서브에셋으로 같이 넣어야 파일에 남는다
        private static void SaveWithSubAssets(TMP_FontAsset asset, string path)
        {
            string assetName = Path.GetFileNameWithoutExtension(path);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);

            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }

            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
        }

        private static void SetDefaultFont(TMP_FontAsset main)
        {
            SerializedObject settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = main;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(TMP_Settings.instance);
        }
    }
}

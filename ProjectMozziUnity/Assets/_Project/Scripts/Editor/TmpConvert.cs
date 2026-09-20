using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 프리팹 안의 legacy Text · TextMesh 를 TextMeshPro 로 바꾸는 일회성 변환기.
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.TmpConvert.ConvertAll();` 한 줄.
    public static class TmpConvert
    {
        private const string PrefabRoot = "Assets/_Project/Resources/Prefabs";
        private const string MainFontPath = "Assets/_Project/Fonts/ONEMobilePOP SDF.asset";

        // TextMesh 는 characterSize × fontSize × 0.1 이 한 글자 높이(월드)고, TMP 3D 는 fontSize × 0.1 이다
        private const float WorldRectWidth = 4f;
        private const float WorldRectHeight = 1f;

        public static void ConvertAll()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            if (font == null)
            {
                Debug.LogError("[TmpConvert] 폰트 자산이 없다 : " + MainFontPath);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { PrefabRoot });
            int totalUi = 0;
            int totalMesh = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                int uiCount;
                int meshCount;
                ConvertPrefab(path, font, out uiCount, out meshCount);
                totalUi += uiCount;
                totalMesh += meshCount;
                Debug.Log("[TmpConvert] " + path + " — Text→TMP " + uiCount + " · TextMesh→TMP " + meshCount);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TmpConvert] 끝 — 프리팹 " + guids.Length + " · Text→TMP " + totalUi + " · TextMesh→TMP " + totalMesh);
        }

        private static void ConvertPrefab(string path, TMP_FontAsset font, out int uiCount, out int meshCount)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            TextMesh[] meshes = root.GetComponentsInChildren<TextMesh>(true);
            uiCount = texts.Length;
            meshCount = meshes.Length;

            for (int i = 0; i < texts.Length; i++)
            {
                ConvertUiText(root, texts[i], font);
            }

            for (int i = 0; i < meshes.Length; i++)
            {
                ConvertTextMesh(root, meshes[i], font);
            }

            if (uiCount + meshCount > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConvertUiText(GameObject root, Text old, TMP_FontAsset font)
        {
            GameObject go = old.gameObject;
            string value = old.text;
            float fontSize = old.fontSize;
            Color color = old.color;
            TextAnchor anchor = old.alignment;
            bool raycastTarget = old.raycastTarget;
            bool richText = old.supportRichText;
            bool wrap = old.horizontalOverflow == HorizontalWrapMode.Wrap;
            bool truncate = old.verticalOverflow == VerticalWrapMode.Truncate;
            bool bestFit = old.resizeTextForBestFit;
            float bestFitMin = old.resizeTextMinSize;
            float bestFitMax = old.resizeTextMaxSize;
            FontStyle style = old.fontStyle;
            bool enabled = old.enabled;

            List<Object> owners = new List<Object>();
            List<string> props = new List<string>();
            CollectReferences(root, old, owners, props);

            Object.DestroyImmediate(old);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = value;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = ToAlignment(anchor);
            tmp.raycastTarget = raycastTarget;
            tmp.richText = richText;
            tmp.fontStyle = ToFontStyle(style);
            tmp.enabled = enabled;

            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (wrap)
            {
                tmp.textWrappingMode = TextWrappingModes.Normal;
            }

            tmp.overflowMode = TextOverflowModes.Overflow;
            if (truncate)
            {
                tmp.overflowMode = TextOverflowModes.Truncate;
            }

            tmp.enableAutoSizing = bestFit;
            if (bestFit)
            {
                tmp.fontSizeMin = bestFitMin;
                tmp.fontSizeMax = bestFitMax;
            }

            Relink(owners, props, tmp);
        }

        private static void ConvertTextMesh(GameObject root, TextMesh old, TMP_FontAsset font)
        {
            GameObject go = old.gameObject;
            string value = old.text;
            float fontSize = old.characterSize * old.fontSize;
            Color color = old.color;
            TextAnchor anchor = old.anchor;
            bool richText = old.richText;
            FontStyle style = old.fontStyle;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            int sortingLayerId = renderer.sortingLayerID;
            int sortingOrder = renderer.sortingOrder;

            List<Object> owners = new List<Object>();
            List<string> props = new List<string>();
            CollectReferences(root, old, owners, props);

            Object.DestroyImmediate(old);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = value;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = ToAlignment(anchor);
            tmp.richText = richText;
            tmp.fontStyle = ToFontStyle(style);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            // 기준점이 옛 anchor 자리에 그대로 오도록 pivot 을 같은 칸으로 잡는다
            RectTransform rect = tmp.rectTransform;
            rect.pivot = ToPivot(anchor);
            rect.sizeDelta = new Vector2(WorldRectWidth, WorldRectHeight);

            MeshRenderer newRenderer = go.GetComponent<MeshRenderer>();
            newRenderer.sortingLayerID = sortingLayerId;
            newRenderer.sortingOrder = sortingOrder;

            Relink(owners, props, tmp);
        }

        // Button.targetGraphic 처럼 옛 컴포넌트를 가리키던 직렬화 참조를 모은다
        private static void CollectReferences(GameObject root, Component target, List<Object> owners, List<string> props)
        {
            Component[] all = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i] == target)
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(all[i]);
                SerializedProperty it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (it.objectReferenceValue == target)
                    {
                        owners.Add(all[i]);
                        props.Add(it.propertyPath);
                    }
                }
            }
        }

        private static void Relink(List<Object> owners, List<string> props, Component replacement)
        {
            for (int i = 0; i < owners.Count; i++)
            {
                SerializedObject so = new SerializedObject(owners[i]);
                so.FindProperty(props[i]).objectReferenceValue = replacement;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[TmpConvert] 참조 다시 이음 : " + owners[i].name + "." + props[i]);
            }
        }

        private static TextAlignmentOptions ToAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                default: return TextAlignmentOptions.BottomRight;
            }
        }

        private static Vector2 ToPivot(TextAnchor anchor)
        {
            int index = (int)anchor;
            float x = (index % 3) * 0.5f;
            float y = 1f - (index / 3) * 0.5f;
            return new Vector2(x, y);
        }

        private static FontStyles ToFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }
    }
}

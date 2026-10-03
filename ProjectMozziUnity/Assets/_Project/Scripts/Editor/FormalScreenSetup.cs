using Mozzi.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 가판대 · 합성소 프리팹을 「정식 화면」 배치로 놓는 손질기. 자리 값은 LayoutConstUi 의 정식 화면 구역이 정본이다.
    // 몇 번 돌려도 같은 결과다 (있으면 자리만 다시 맞추고 없으면 만든다).
    // 떡 그림(Icon) · 손님 겹 · 진열 칸 같은 자식은 프리팹에 이미 있다고 보고 자리만 놓는다.
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 배치」 또는 unity command eval 로 FormalScreenSetup.Apply().
    public static class FormalScreenSetup
    {
        private const string StallScreenPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";
        private const string SynthesisScreenPath = "Assets/_Project/Resources/Prefabs/UI/SynthesisScreen.prefab";

        private const int QueueCellCount = 5;
        private const int OldShelfCellCount = 24;
        private const int SlotCount = 4;

        // 아래 판 안 자리 (판 왼쪽 위 또는 왼쪽 아래 기준 px). 기준 높이에서 판은 200 이다.
        private const float MakeRowWidth = 86f;
        private const float MakeRowHeight = 22f;
        private const float MakeRowPitchX = 87f;
        private const float MakeRowPitchY = 23f;
        private const int MakeRowColumns = 4;
        private const float TabWidth = 84f;
        private const float TabHeight = 24f;
        private const float TabPitch = 87f;

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
        private static readonly Color PanelCream = new Color(1f, 0.965f, 0.9f, 1f);
        private static readonly Color TextDark = new Color(0.29f, 0.18f, 0.13f, 1f);
        // 합성소 배경 위끝색(SynthesisFillTop)을 반쯤 어둡게 한 갈색. 불투명이라 뒤 그림이 안 비친다.
        private static readonly Color TopBarColor = new Color32(0x1C, 0x12, 0x10, 0xFF);
        private static readonly Color StripColor = new Color(0f, 0f, 0f, 0.08f);
        private static readonly Color BoardColor = new Color32(0x22, 0x20, 0x24, 0xFF);
        private static readonly Color BoardText = new Color(0.98f, 0.96f, 0.88f, 1f);
        private static readonly Color GlassCountColor = new Color(0.16f, 0.2f, 0.31f, 1f);
        private static readonly Color SlotBoxColor = new Color(0.93f, 0.86f, 0.74f, 1f);

        // 말풍선 꼴. 몸은 네모 셋을 겹쳐 모서리를 2px 깎고, 꼬리는 네모 둘을 계단으로 쌓는다.
        // 테두리 조각(Edge)을 먼저 깔고 그 위에 1px 씩 들어간 바탕 조각(Fill)을 덮는다.
        private const float BubbleTailLength = 4f;
        private static readonly Vector2[] BubbleBodyInsets =
        {
            new Vector2(2f, 0f), new Vector2(1f, 1f), new Vector2(0f, 2f),
        };

        // 배경 그림 위아래 끝 색. 800 을 넘는 세로와 안전 영역 밖을 이 색으로 채운다.
        private static readonly Color StallFillTop = new Color32(0xFD, 0xFB, 0xE6, 0xFF);
        private static readonly Color StallFillBottom = new Color32(0xEB, 0xE3, 0xB9, 0xFF);
        private static readonly Color SynthesisFillTop = new Color32(0x39, 0x23, 0x20, 0xFF);
        private static readonly Color SynthesisFillBottom = new Color32(0x9C, 0x68, 0x54, 0xFF);

        private static readonly string[] ToggleIds =
        {
            "redbean", "bean", "mugwort", "sesame", "pinenut", "jujube",
        };

        private static readonly string[] StripButtons =
        {
            UiNames.StallOpenButton, UiNames.StallClerkButton, UiNames.StallRecipeCodexButton,
        };

        [MenuItem("Mozzi/정식 화면 배치")]
        public static void Apply()
        {
            ApplyStall();
            ApplySynthesis();
            AssetDatabase.SaveAssets();
            Debug.Log("[FormalScreenSetup] 끝");
        }

        private static void ApplyStall()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StallScreenPath);
            Transform t = root.transform;
            TMP_Text sample = Find(t, UiNames.TitleText).GetComponent<TMP_Text>();

            RectTransform scene = MakeSceneFrame(t, StallFillTop, StallFillBottom);
            MakeTopBar(t, LayoutConstUi.StallTopBarHeight);
            RectTransform panel = MakeBottomPanel(t, LayoutConstUi.StallPanelHeight);

            MakeMenuBoard(scene, sample);
            PlaceGlassCells(t, scene);
            PlaceQueueCells(t, scene);
            PlaceStallPanel(t, panel, sample);

            panel.SetAsLastSibling();
            Find(t, UiNames.PoundPanel).SetAsLastSibling();
            Find(t, UiNames.StallMadePanel).SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root, StallScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[FormalScreenSetup] 가판대");
        }

        private static void ApplySynthesis()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SynthesisScreenPath);
            Transform t = root.transform;
            TMP_Text sample = Find(t, UiNames.TitleText).GetComponent<TMP_Text>();

            RectTransform scene = MakeSceneFrame(t, SynthesisFillTop, SynthesisFillBottom);
            MakeTopBar(t, LayoutConstUi.SynthesisTopBarHeight);
            RectTransform panel = MakeBottomPanel(t, LayoutConstUi.SynthesisPanelHeight);

            MakeMachine(scene, sample);
            PlaceSynthesisPanel(t, panel);

            panel.SetAsLastSibling();
            Find(t, UiNames.SynthesisPickPanel).SetAsLastSibling();
            Find(t, UiNames.SynthesisResultPanel).SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root, SynthesisScreenPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[FormalScreenSetup] 합성소");
        }

        // 위아래 끝색 채움 둘 + 장면 칸 + 배경 그림 자리. 맨 앞 형제 셋으로 둔다.
        // 장면 칸은 SceneAnchorY(위끝)에 붙어, 화면이 길면 남는 세로가 아래쪽에 생긴다.
        private static RectTransform MakeSceneFrame(Transform root, Color fillTop, Color fillBottom)
        {
            RectTransform top = Ensure(root, UiNames.SceneFillTop);
            top.anchorMin = new Vector2(0f, 0.5f);
            top.anchorMax = new Vector2(1f, 1f);
            top.offsetMin = Vector2.zero;
            top.offsetMax = new Vector2(0f, LayoutConstUi.BarBleedLength);
            SetImage(top, fillTop, false);
            top.SetSiblingIndex(0);

            RectTransform bottom = Ensure(root, UiNames.SceneFillBottom);
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0.5f);
            bottom.offsetMin = new Vector2(0f, -LayoutConstUi.BarBleedLength);
            bottom.offsetMax = Vector2.zero;
            SetImage(bottom, fillBottom, false);
            bottom.SetSiblingIndex(1);

            RectTransform scene = Ensure(root, UiNames.Scene);
            scene.anchorMin = new Vector2(0.5f, LayoutConstUi.SceneAnchorY);
            scene.anchorMax = new Vector2(0.5f, LayoutConstUi.SceneAnchorY);
            scene.pivot = new Vector2(0.5f, LayoutConstUi.SceneAnchorY);
            scene.anchoredPosition = Vector2.zero;
            // 가판대 · 합성소 프리팹은 아직 Legacy360 껍데기 안이라 옛 기준 크기다.
            scene.sizeDelta = new Vector2(LayoutConstUi.LegacyReferenceWidth, LayoutConstUi.LegacyReferenceHeight);
            scene.SetSiblingIndex(2);

            // 스프라이트는 화면 코드가 SpriteKeys 로 끼운다. 여기서는 자리만 잡는다.
            RectTransform bg = Ensure(scene, UiNames.SceneBg);
            bg.anchorMin = new Vector2(0.5f, 0.5f);
            bg.anchorMax = new Vector2(0.5f, 0.5f);
            bg.pivot = new Vector2(0.5f, 0.5f);
            bg.anchoredPosition = Vector2.zero;
            bg.sizeDelta = new Vector2(LayoutConstUi.SceneBgWidth, LayoutConstUi.SceneBgHeight);
            SetImage(bg, Color.white, true);
            bg.SetSiblingIndex(0);
            return scene;
        }

        // 머리 글자 뒤의 어두운 띠. 안전 영역 위쪽 밖까지 잇는다.
        private static void MakeTopBar(Transform root, float height)
        {
            RectTransform bar = Ensure(root, UiNames.TopBar);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -height);
            bar.offsetMax = new Vector2(0f, LayoutConstUi.BarBleedLength);
            SetImage(bar, TopBarColor, false);
            bar.SetSiblingIndex(3);
        }

        // 내용 칸 바닥에 붙은 고정 높이 판. 뒤판은 안전 영역 아래쪽 밖까지 잇는다.
        private static RectTransform MakeBottomPanel(Transform root, float height)
        {
            RectTransform panel = Ensure(root, UiNames.BottomPanel);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = new Vector2(0f, height);

            RectTransform back = Ensure(panel, UiNames.BottomPanelBack);
            back.anchorMin = Vector2.zero;
            back.anchorMax = Vector2.one;
            back.offsetMin = new Vector2(0f, -LayoutConstUi.BarBleedLength);
            back.offsetMax = Vector2.zero;
            SetImage(back, PanelCream, true);
            back.SetSiblingIndex(0);
            return panel;
        }

        private static void MakeMenuBoard(RectTransform scene, TMP_Text sample)
        {
            RectTransform board = Ensure(scene, UiNames.StallMenuBoard);
            TopLeft(board, LayoutConstUi.StallMenuBoardLeft, LayoutConstUi.StallMenuBoardTop,
                LayoutConstUi.StallMenuBoardWidth, LayoutConstUi.StallMenuBoardHeight);
            SetImage(board, BoardColor, false);

            TMP_Text title = EnsureText(board, UiNames.StallMenuTitleText, sample);
            TopLeft(title.rectTransform, 0f, 2f, LayoutConstUi.StallMenuBoardWidth, LayoutConstUi.StallMenuTitleHeight);
            StyleText(title, 10f, BoardText, TextAlignmentOptions.Center);
            title.text = UiText.StallMenuTitle;

            float bodyTop = LayoutConstUi.StallMenuTitleHeight + 2f;
            TMP_Text body = EnsureText(board, UiNames.StallMenuBodyText, sample);
            TopLeft(body.rectTransform, 0f, bodyTop, LayoutConstUi.StallMenuBoardWidth, LayoutConstUi.StallMenuBoardHeight - bodyTop);
            StyleText(body, 9f, BoardText, TextAlignmentOptions.Top);
            body.text = UiText.StallMenuDefault;
        }

        // 묶음 칸 여섯을 유리 진열장 안으로 옮긴다. 개수 글자가 위, 떡 그림이 아래. 나머지 옛 칸은 지운다.
        private static void PlaceGlassCells(Transform root, RectTransform scene)
        {
            for (int i = 0; i < OldShelfCellCount; i++)
            {
                Transform cell = Find(root, UiNames.StallShelfCellPrefix + i);
                if (cell == null)
                {
                    continue;
                }

                if (i >= UiNames.StallShelfCellCount)
                {
                    Object.DestroyImmediate(cell.gameObject);
                    continue;
                }

                cell.SetParent(scene, false);
                TopLeft(cell as RectTransform,
                    LayoutConstUi.StallGlassCellLeft + i * LayoutConstUi.StallGlassCellPitch, LayoutConstUi.StallGlassCellTop,
                    LayoutConstUi.StallGlassCellWidth, LayoutConstUi.StallGlassCellHeight);
                SetImage(cell as RectTransform, Clear, false);

                TopLeft(cell.Find(UiNames.CakeIcon) as RectTransform,
                    LayoutConstUi.StallGlassIconLeft, LayoutConstUi.StallGlassIconTop,
                    LayoutConstUi.CakeIconShelfSize, LayoutConstUi.CakeIconShelfSize);

                TMP_Text count = cell.Find(UiNames.StallShelfCountText).GetComponent<TMP_Text>();
                TopLeft(count.rectTransform, 0f, 0f, LayoutConstUi.StallGlassCellWidth, LayoutConstUi.StallGlassCountHeight);
                StyleText(count, LayoutConstUi.ShelfCountFontSize, GlassCountColor, TextAlignmentOptions.Center);
                cell.gameObject.SetActive(false);
            }
        }

        // 손님 칸 다섯을 바닥 줄 자리로. 칸 위 가운데에 말풍선(떡 그림 또는 글자), 아래 64 는 손님 겹이다.
        private static void PlaceQueueCells(Transform root, RectTransform scene)
        {
            string[] layers =
            {
                UiNames.CustomerBody, UiNames.CustomerHair, UiNames.CustomerShirt, UiNames.CustomerFace,
            };

            for (int i = 0; i < QueueCellCount; i++)
            {
                Transform cell = Find(root, UiNames.StallQueueCellPrefix + i);
                cell.SetParent(scene, false);
                TopLeft(cell as RectTransform,
                    LayoutConstUi.StallQueueLeft + i * LayoutConstUi.StallQueuePitch, LayoutConstUi.StallQueueTop,
                    LayoutConstUi.StallQueueCellWidth, LayoutConstUi.StallQueueCellHeight);
                SetImage(cell as RectTransform, Clear, true);

                for (int k = 0; k < layers.Length; k++)
                {
                    TopLeft(Find(cell, layers[k]) as RectTransform,
                        LayoutConstUi.StallCustomerLeft, LayoutConstUi.StallCustomerTop,
                        LayoutConstUi.CustomerCanvasWidth, LayoutConstUi.CustomerCanvasHeight);
                }

                RectTransform bubble = Ensure(cell, UiNames.StallOrderBubble);
                bubble.anchorMin = new Vector2(0.5f, 1f);
                bubble.anchorMax = new Vector2(0.5f, 1f);
                bubble.pivot = new Vector2(0.5f, 1f);
                bubble.anchoredPosition = Vector2.zero;
                bubble.sizeDelta = new Vector2(LayoutConstUi.StallBubbleWidthIcon, LayoutConstUi.StallBubbleHeight);
                Image oldPlate = bubble.GetComponent<Image>();
                if (oldPlate != null)
                {
                    Object.DestroyImmediate(oldPlate);
                }

                MakeBubbleShape(bubble);

                Transform label = Find(cell, UiNames.ButtonLabel);
                label.SetParent(bubble, false);
                RectTransform labelRect = label as RectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(3f, 2f);
                labelRect.offsetMax = new Vector2(-3f, -2f);
                TMP_Text labelText = label.GetComponent<TMP_Text>();
                StyleText(labelText, 8f, TextDark, TextAlignmentOptions.Center);
                labelText.margin = Vector4.zero;
                labelText.textWrappingMode = TextWrappingModes.Normal;
                labelText.enableAutoSizing = true;
                labelText.fontSizeMin = 6f;
                labelText.fontSizeMax = 8f;

                Transform icon = Find(cell, UiNames.CakeIcon);
                icon.SetParent(bubble, false);
                Middle(icon as RectTransform, 0f, 0f, LayoutConstUi.StallBubbleIconSize);
                icon.SetAsLastSibling();
                label.SetAsLastSibling();
            }
        }

        // 말풍선 조각들. 몸 조각은 부모에 맞춰 늘어나므로 코드가 폭만 바꿔도 꼴이 유지된다.
        private static void MakeBubbleShape(RectTransform bubble)
        {
            int order = 0;
            for (int i = 0; i < BubbleBodyInsets.Length; i++)
            {
                RectTransform edge = Ensure(bubble, UiNames.StallBubbleEdgePrefix + i);
                Inset(edge, BubbleBodyInsets[i].x, BubbleBodyInsets[i].y);
                SetImage(edge, TextDark, false);
                edge.SetSiblingIndex(order++);
            }

            // 꼬리 테두리 : 8×3 (몸 바닥 한 줄 위부터) → 4×2. 몸 아래로 BubbleTailLength 만큼 나온다.
            RectTransform tailEdgeA = Ensure(bubble, UiNames.StallBubbleEdgePrefix + 3);
            Tail(tailEdgeA, 8f, 1f, 3f);
            SetImage(tailEdgeA, TextDark, false);
            tailEdgeA.SetSiblingIndex(order++);
            RectTransform tailEdgeB = Ensure(bubble, UiNames.StallBubbleEdgePrefix + 4);
            Tail(tailEdgeB, 4f, -2f, BubbleTailLength - 2f);
            SetImage(tailEdgeB, TextDark, false);
            tailEdgeB.SetSiblingIndex(order++);

            for (int i = 0; i < BubbleBodyInsets.Length; i++)
            {
                RectTransform fill = Ensure(bubble, UiNames.StallBubbleFillPrefix + i);
                Inset(fill, BubbleBodyInsets[i].x + 1f, BubbleBodyInsets[i].y + 1f);
                SetImage(fill, Color.white, false);
                fill.SetSiblingIndex(order++);
            }

            // 꼬리 바탕 : 몸 바닥 테두리 줄을 끊고 이어진다.
            RectTransform tailFillA = Ensure(bubble, UiNames.StallBubbleFillPrefix + 3);
            Tail(tailFillA, 6f, 2f, 3f);
            SetImage(tailFillA, Color.white, false);
            tailFillA.SetSiblingIndex(order++);
            RectTransform tailFillB = Ensure(bubble, UiNames.StallBubbleFillPrefix + 4);
            Tail(tailFillB, 2f, -1f, 2f);
            SetImage(tailFillB, Color.white, false);
            tailFillB.SetSiblingIndex(order);
        }

        // 부모를 꽉 채우되 좌우 x · 위아래 y 만큼 들어간 자리.
        private static void Inset(RectTransform rect, float x, float y)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }

        // 부모 바닥 가운데 기준. top 은 조각 위끝이 부모 바닥에서 위로 얼마인가 (음수면 바닥 아래).
        private static void Tail(RectTransform rect, float width, float top, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 위쪽 묶음(진열 목록 · 만들기)은 판 위 끝에, 아래쪽 묶음(실험 · 버튼)은 판 바닥에 붙인다.
        private static void PlaceStallPanel(Transform root, RectTransform panel, TMP_Text sample)
        {
            TMP_Text shelf = MoveText(root, panel, UiNames.StallShelfText);
            shelf.gameObject.SetActive(true);
            TopLeft(shelf.rectTransform, 8f, 2f, 344f, 12f);
            StyleText(shelf, 9f, TextDark, TextAlignmentOptions.Left);
            shelf.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text makeTitle = MoveText(root, panel, "MakeTitleText");
            TopLeft(makeTitle.rectTransform, 8f, 15f, 200f, 12f);
            StyleText(makeTitle, 9f, TextDark, TextAlignmentOptions.Left);

            for (int i = 0; i < UiNames.StallMakeRowCount; i++)
            {
                Transform row = Find(root, UiNames.StallMakeRowPrefix + i);
                row.SetParent(panel, false);
                TopLeft(row as RectTransform,
                    6f + (i % MakeRowColumns) * MakeRowPitchX, 28f + (i / MakeRowColumns) * MakeRowPitchY,
                    MakeRowWidth, MakeRowHeight);

                TMP_Text label = Find(row, UiNames.ButtonLabel).GetComponent<TMP_Text>();
                label.enableAutoSizing = true;
                label.fontSizeMin = 6f;
                label.fontSizeMax = 9f;

                RectTransform icon = Find(row, UiNames.CakeIcon) as RectTransform;
                icon.anchoredPosition = new Vector2(LayoutConstUi.CakeIconMakeRowLeft, 0f);
                icon.sizeDelta = new Vector2(LayoutConstUi.CakeIconMakeRowSize, LayoutConstUi.CakeIconMakeRowSize);
            }

            // 찾은 레시피가 없을 때 만들기 줄 자리에 뜨는 안내. 켜고 끄기는 화면 코드가 한다.
            TMP_Text makeEmpty = EnsureText(panel, UiNames.StallMakeEmptyText, sample);
            TopLeft(makeEmpty.rectTransform, 8f, 28f, 344f, MakeRowPitchY * 2f);
            StyleText(makeEmpty, 9f, TextDark, TextAlignmentOptions.Center);
            makeEmpty.text = UiText.StallMakeEmpty;
            makeEmpty.gameObject.SetActive(false);

            RectTransform strip = Find(root, "ButtonStrip") as RectTransform;
            strip.SetParent(panel, false);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.offsetMin = Vector2.zero;
            strip.offsetMax = new Vector2(0f, 32f);
            SetImage(strip, StripColor, false);
            for (int i = 0; i < StripButtons.Length; i++)
            {
                BottomLeft(Find(strip, StripButtons[i]) as RectTransform, 8f + i * 118f, 3f, 108f, 26f);
            }

            TMP_Text expInfo = MoveText(root, panel, UiNames.StallExpInfoText);
            BottomLeft(expInfo.rectTransform, 8f, 34f, 250f, 26f);
            StyleText(expInfo, 9f, TextDark, TextAlignmentOptions.Left);

            Transform expMake = Find(root, UiNames.StallExpMakeButton);
            expMake.SetParent(panel, false);
            BottomLeft(expMake as RectTransform, 262f, 34f, 90f, 26f);

            for (int i = 0; i < ToggleIds.Length; i++)
            {
                Transform toggle = Find(root, UiNames.StallExpTogglePrefix + ToggleIds[i]);
                toggle.SetParent(panel, false);
                BottomLeft(toggle as RectTransform, 8f + i * 58f, 62f, 54f, 26f);
            }

            TMP_Text expTitle = MoveText(root, panel, "ExpTitleText");
            BottomLeft(expTitle.rectTransform, 8f, 89f, 200f, 12f);
            StyleText(expTitle, 9f, TextDark, TextAlignmentOptions.Left);
        }

        private static void MakeMachine(RectTransform scene, TMP_Text sample)
        {
            RectTransform machine = Ensure(scene, UiNames.SynthesisMachine);
            TopLeft(machine, LayoutConstUi.SynthesisMachineLeft, LayoutConstUi.SynthesisMachineTop,
                LayoutConstUi.SynthesisMachineWidth, LayoutConstUi.SynthesisMachineHeight);
            SetImage(machine, Color.white, false);

            MakeMozziIcon(scene, UiNames.SynthesisInletA, LayoutConstUi.SynthesisInletAX, LayoutConstUi.SynthesisInletY);
            MakeMozziIcon(scene, UiNames.SynthesisInletB, LayoutConstUi.SynthesisInletBX, LayoutConstUi.SynthesisInletY);

            MakeInletButton(scene, UiNames.SynthesisInletAButton, LayoutConstUi.SynthesisInletAX, sample);
            MakeInletButton(scene, UiNames.SynthesisInletBButton, LayoutConstUi.SynthesisInletBX, sample);

            RectTransform window = Ensure(scene, UiNames.SynthesisWindowFx);
            Centered(window, LayoutConstUi.SynthesisWindowCenterX, LayoutConstUi.SynthesisWindowCenterY,
                LayoutConstUi.SynthesisWindowSize, LayoutConstUi.SynthesisWindowSize);

            RectTransform yinRoot = Ensure(window, UiNames.SynthesisYinRoot);
            Middle(yinRoot, 0f, 0f, LayoutConstUi.SynthesisWindowSize);
            RectTransform halfA = Ensure(yinRoot, UiNames.SynthesisYinHalfA);
            Middle(halfA, 0f, 0f, LayoutConstUi.SynthesisWindowSize);
            SetImage(halfA, Color.white, false);
            RectTransform halfB = Ensure(yinRoot, UiNames.SynthesisYinHalfB);
            Middle(halfB, 0f, 0f, LayoutConstUi.SynthesisWindowSize);
            halfB.localEulerAngles = new Vector3(0f, 0f, 180f);
            SetImage(halfB, Color.white, false);
            yinRoot.gameObject.SetActive(false);

            RectTransform flash = Ensure(window, UiNames.SynthesisMergeFlash);
            Middle(flash, 0f, 0f, LayoutConstUi.SynthesisFlashSize);
            SetImage(flash, Color.white, false);
            flash.gameObject.SetActive(false);

            for (int i = 0; i < UiNames.SynthesisSparkleCount; i++)
            {
                RectTransform sparkle = Ensure(window, UiNames.SynthesisSparklePrefix + i);
                Middle(sparkle, 0f, 0f, LayoutConstUi.SynthesisSparkleSize);
                SetImage(sparkle, Color.white, false);
                sparkle.gameObject.SetActive(false);
            }

            RectTransform outlet = MakeMozziIcon(scene, UiNames.SynthesisOutletResult,
                LayoutConstUi.SynthesisOutletX, LayoutConstUi.SynthesisOutletY);
            TMP_Text mark = EnsureText(outlet, UiNames.SynthesisOutletMark, sample);
            Middle(mark.rectTransform, 0f, -2f, 40f);
            StyleText(mark, 22f, BoardText, TextAlignmentOptions.Center);
            mark.text = "?";
            mark.transform.SetAsLastSibling();
        }

        // 투입구 위의 보이지 않는 누름 칸과 빈 입구 표시 「+」. onClick 과 켜고 끄기는 화면 코드가 한다.
        private static void MakeInletButton(RectTransform scene, string name, float centerX, TMP_Text sample)
        {
            RectTransform rect = Ensure(scene, name);
            Centered(rect, centerX, LayoutConstUi.SynthesisInletY,
                LayoutConstUi.SynthesisInletTouchSize, LayoutConstUi.SynthesisInletTouchSize);
            SetImage(rect, Clear, true);

            Button button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }

            button.transition = Selectable.Transition.None;

            TMP_Text plus = EnsureText(rect, UiNames.SynthesisInletPlus, sample);
            Middle(plus.rectTransform, 0f, -LayoutConstUi.SynthesisInletPlusDrop, LayoutConstUi.SynthesisInletTouchSize);
            StyleText(plus, 22f, BoardText, TextAlignmentOptions.Center);
            plus.text = "+";
        }

        // 찹쌀이 겹 다섯 (몸 · 볼 둘 · 표정 · 악세사리). 그림은 코드가 끼우므로 꺼 둔다.
        private static RectTransform MakeMozziIcon(RectTransform scene, string name, float centerX, float centerY)
        {
            RectTransform holder = Ensure(scene, name);
            Centered(holder, centerX, centerY, LayoutConstUi.MozziIconSize, LayoutConstUi.MozziIconSize);

            MakeLayer(holder, UiNames.MozziIconBody, 0f, 0f, LayoutConstUi.MozziIconSize);
            MakeLayer(holder, UiNames.MozziIconCheekL, -LayoutConstUi.MozziIconCheekX, -LayoutConstUi.MozziIconCheekY, LayoutConstUi.MozziIconCheekSize);
            MakeLayer(holder, UiNames.MozziIconCheekR, LayoutConstUi.MozziIconCheekX, -LayoutConstUi.MozziIconCheekY, LayoutConstUi.MozziIconCheekSize);
            MakeLayer(holder, UiNames.MozziIconFace, 0f, 0f, LayoutConstUi.MozziIconSize);
            MakeLayer(holder, UiNames.MozziIconAcc, 0f, 0f, LayoutConstUi.MozziIconSize);
            holder.gameObject.SetActive(false);
            return holder;
        }

        private static void MakeLayer(RectTransform holder, string name, float x, float y, float size)
        {
            RectTransform layer = Ensure(holder, name);
            Middle(layer, x, y, size);
            SetImage(layer, Color.white, false);
        }

        private static void PlaceSynthesisPanel(Transform root, RectTransform panel)
        {
            TMP_Text shortcut = MoveText(root, panel, UiNames.SynthesisShortcutText);
            TopLeft(shortcut.rectTransform, 8f, 4f, 240f, 22f);
            shortcut.color = TextDark;

            Transform buy = Find(root, UiNames.SynthesisShortcutBuyButton);
            buy.SetParent(panel, false);
            TopLeft(buy as RectTransform, 250f, 3f, 102f, 24f);

            for (int i = 0; i < SlotCount; i++)
            {
                string tabName = UiNames.SynthesisSlotTabPrefix + i;
                Transform tab = Find(root, tabName);
                if (tab == null)
                {
                    // 버튼 모양(Image + Button + Label)은 단축 버튼을 베껴 쓴다.
                    tab = Object.Instantiate(buy.gameObject, panel).transform;
                    tab.name = tabName;
                }

                tab.SetParent(panel, false);
                TopLeft(tab as RectTransform, 6f + i * TabPitch, 30f, TabWidth, TabHeight);
                TMP_Text label = Find(tab, UiNames.ButtonLabel).GetComponent<TMP_Text>();
                label.fontSize = 10f;
                label.text = (i + 1).ToString();

                Transform slot = Find(root, UiNames.SynthesisSlotPrefix + i);
                slot.SetParent(panel, false);
                TopLeft(slot as RectTransform, 6f, 58f, 348f, 112f);

                // 칸 상자는 크림색 판에 맞춘 연한 베이지, 버튼 밖 글자는 어두운 갈색. 버튼은 그대로 둔다.
                SetImage(slot as RectTransform, SlotBoxColor, true);
                Find(slot, UiNames.SynthesisPreviewText).GetComponent<TMP_Text>().color = TextDark;
                Find(slot, UiNames.SynthesisRemainText).GetComponent<TMP_Text>().color = TextDark;
            }
        }

        private static TMP_Text MoveText(Transform root, Transform parent, string name)
        {
            Transform child = Find(root, name);
            child.SetParent(parent, false);
            return child.GetComponent<TMP_Text>();
        }

        private static void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
        }

        // 바로 밑 자식을 찾고 없으면 만든다.
        private static RectTransform Ensure(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            return child as RectTransform;
        }

        private static TMP_Text EnsureText(Transform parent, string name, TMP_Text sample)
        {
            RectTransform rect = Ensure(parent, name);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = sample.font;
            text.fontSharedMaterial = sample.fontSharedMaterial;
            return text;
        }

        private static void SetImage(RectTransform rect, Color color, bool raycast)
        {
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = raycast;
        }

        // 부모 왼쪽 위에서 (x, y) 만큼 들어간 자리. y 는 아래로 양수다.
        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 부모 왼쪽 아래에서 (x, y) 만큼 들어간 자리. y 는 위로 양수다.
        private static void BottomLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 부모 왼쪽 위 기준 그림 좌표 (x, y) 에 중심을 둔다.
        private static void Centered(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 부모 가운데에서 (x, y) 만큼 벗어난 정사각 자리.
        private static void Middle(RectTransform rect, float x, float y, float size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(size, size);
        }

        private static Transform Find(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform deeper = Find(child, childName);
                if (deeper != null)
                {
                    return deeper;
                }
            }

            return null;
        }
    }
}

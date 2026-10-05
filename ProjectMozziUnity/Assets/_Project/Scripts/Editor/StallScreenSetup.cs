using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 가판대 화면 프리팹을 빈 뿌리에서 새로 짓는 손질기. 자리는 StallLayout(그림 README 세계 좌표)이 정본이다.
    // 프리팹을 손으로 고치면 다음에 돌릴 때 덮이니 고칠 것은 여기서 고친다. 그림은 FormalArtCopy.CopyStall 을 먼저 돌린다.
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 540 배치/가판대」 또는 eval 로 StallScreenSetup.Apply().
    public static class StallScreenSetup
    {
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/UI/StallScreen.prefab";

        // 배경 맨 윗줄 색 (천장 나무). 1200 보다 긴 화면의 위와 넓적한 화면 옆을 이 색으로 채운다.
        private static readonly Color FillTop = new Color32(0xAD, 0x75, 0x5B, 0xFF);
        private static readonly Color ChalkText = new Color(0.96f, 0.95f, 0.88f, 1f);
        private static readonly Color OpenTint = new Color(1f, 0.86f, 0.88f, 1f);
        private static readonly Color Outline = new Color32(0x3A, 0x22, 0x18, 0xFF);

        // 손님 겹 캔버스 안 그림이 있는 칸 밖 여백 (왼 · 아래 · 오른 · 위). 누름 칸을 몸에 맞춰 옆 손님과 덜 겹치게 한다.
        private static readonly Vector4 CustomerTapPadding = new Vector4(18f, 0f, 12f, 24f);

        [MenuItem("Mozzi/정식 화면 540 배치/가판대")]
        public static void Apply()
        {
            GameObject root = FormalUiBuild.NewScreenRoot("StallScreen", FillTop);
            try
            {
                Build(root);
            }
            catch
            {
                // 짓다 실패하면 반쯤 지은 뿌리가 열린 씬에 남는다. 지우고 다시 던진다.
                Object.DestroyImmediate(root);
                throw;
            }

            FormalUiBuild.SavePrefab(root, PrefabPath);
            Debug.Log("[StallScreenSetup] 저장 " + PrefabPath);
        }

        // 겹 순서 (뒤 → 앞) : 배경 → 칠판 장식 · 칠판 글자 → 주인 → 계산대 → 진열장 · 떡 · 소품 → 손님 → 말풍선 → 윗줄 · 버튼 줄.
        private static void Build(GameObject root)
        {
            root.AddComponent<StallScreen>();
            Transform t = root.transform;

            FormalUiBuild.NewFillTop(t, FillTop);
            RectTransform stage = FormalUiBuild.NewStage(t);
            // 계산대 왼끝이 Stage 밖(x −17)이라 넓적한 화면에서 옆 바탕 위로 삐져나오지 않게 자른다.
            stage.gameObject.AddComponent<RectMask2D>();
            Image bg = FormalUiBuild.PlaceSprite(stage, UiNames.SceneBg, SpriteKeys.ScreenStallBg, 0f, 0f, new Vector2(0f, 1f));
            bg.raycastTarget = true;

            MakeBoard(stage);
            FormalUiBuild.PlaceSprite(stage, "Owner", SpriteKeys.ScreenStallOwner, StallLayout.OwnerX, StallLayout.OwnerY, new Vector2(0f, 1f));
            FormalUiBuild.PlaceSprite(stage, "Counter", SpriteKeys.ScreenStallCounter, StallLayout.CounterX, StallLayout.CounterY, new Vector2(0f, 1f));
            FormalUiBuild.PlaceSprite(stage, "DisplayCase", SpriteKeys.ScreenStallDisplayCase, StallLayout.CaseX, StallLayout.ItemLineY, new Vector2(0.5f, 0f));
            MakeShelfCells(stage);

            Vector2 bottomCenter = new Vector2(0.5f, 0f);
            FormalUiBuild.PlaceSprite(stage, "Vase", SpriteKeys.ScreenStallVase, StallLayout.VaseX, StallLayout.ItemLineY, bottomCenter);
            FormalUiBuild.PlaceSprite(stage, "Register", SpriteKeys.ScreenStallRegister, StallLayout.RegisterX, StallLayout.ItemLineY, bottomCenter);
            for (int i = 0; i < StallLayout.QueueShown; i++)
            {
                MakeCustomer(stage, i);
            }

            for (int i = 0; i < StallLayout.QueueShown; i++)
            {
                MakeBubble(stage, i);
            }

            MakeQueueMore(stage);

            FormalUiBuild.MakeTopRow(t);
            MakeStatusPlate(t);
            MakeBottomRow(t);
        }

        // 칠판 머리 장식 + 그 위 「오늘의 떡」, 녹색 면 = 누름 칸 + 진열 요약 글자.
        private static void MakeBoard(RectTransform stage)
        {
            Image topper = FormalUiBuild.PlaceSprite(stage, "BoardTopper", SpriteKeys.ScreenStallBoardTopper,
                StallLayout.BoardTopperX, StallLayout.BoardTopperY, new Vector2(0.5f, 0.5f));
            TextMeshProUGUI title = FormalUiBuild.MakeText(topper.transform, UiNames.StallMenuTitleText, 13f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.Stretch(title.rectTransform, 0f);
            title.text = UiText.StallMenuTitle;

            RectTransform board = FormalUiBuild.NewNode(stage, UiNames.StallMenuBoard);
            FormalUiBuild.TopLeft(board, StallLayout.BoardLeft, StallLayout.BoardTop,
                StallLayout.BoardRight - StallLayout.BoardLeft, StallLayout.BoardBottom - StallLayout.BoardTop);
            FormalUiBuild.SetImage(board, FormalUiBuild.Clear, true);
            board.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            TextMeshProUGUI body = FormalUiBuild.MakeText(board, UiNames.StallMenuBodyText, 18f, ChalkText, TextAlignmentOptions.Top);
            FormalUiBuild.Stretch(body.rectTransform, 8f);
            body.enableAutoSizing = true;
            body.fontSizeMin = 12f;
            body.fontSizeMax = 18f;
            body.lineSpacing = -8f;
            body.text = "진열 0/0";
        }

        // 진열장 접시 여섯 위의 떡 칸. 칸 아래 가운데 = 접시 위 떡 바닥. 칸마다 떡 그림 셋을 겹쳐 두고, 몇 개를 어디에 보일지는 화면이 정한다. 처음엔 꺼 둔다.
        private static void MakeShelfCells(RectTransform stage)
        {
            int columns = StallLayout.PlateCenterX.Length;
            Vector2 cakeSize = new Vector2(StallLayout.ShelfCakeWidth, StallLayout.ShelfCakeHeight);
            for (int i = 0; i < UiNames.StallShelfCellCount; i++)
            {
                float x = StallLayout.CaseLeft + StallLayout.PlateCenterX[i % columns];
                float y = StallLayout.CaseTop + StallLayout.PlateCakeBottom[i / columns];
                RectTransform cell = FormalUiBuild.NewNode(stage, UiNames.StallShelfCellPrefix + i);
                FormalUiBuild.PivotAt(cell, x, y, new Vector2(0.5f, 0f), cakeSize.x, cakeSize.y);

                for (int k = 0; k < UiNames.StallShelfCakeCount; k++)
                {
                    RectTransform cake = FormalUiBuild.NewNode(cell, UiNames.StallShelfCakePrefix + k);
                    cake.anchorMin = new Vector2(0.5f, 0f);
                    cake.anchorMax = new Vector2(0.5f, 0f);
                    cake.pivot = new Vector2(0.5f, 0f);
                    cake.anchoredPosition = Vector2.zero;
                    cake.sizeDelta = cakeSize;
                    FormalUiBuild.SetImage(cake, Color.white, false);
                }

                cell.gameObject.SetActive(false);
            }
        }

        // 손님 뿌리 = 겹 캔버스(120×240) 크기 · 피벗 발끝. 왼쪽을 보는 자리는 뿌리째 좌우로 뒤집는다 (발끝은 그대로).
        // 겹은 아래 → 위 body · shirt · face · hair. 그림은 StallScreen 이 끼운다.
        private static void MakeCustomer(RectTransform stage, int index)
        {
            Sprite body = FormalUiBuild.LoadSprite(CustomerArtKeys.Body);
            Vector2 size = new Vector2(120f, 240f);
            if (body != null)
            {
                size = body.rect.size;
            }

            RectTransform customer = FormalUiBuild.NewNode(stage, UiNames.StallCustomerPrefix + index);
            FormalUiBuild.PivotAt(customer, StallLayout.QueueFootX[index], StallLayout.QueueFootY,
                new Vector2(StallLayout.CustomerPivotX, 0f), size.x, size.y);
            if (StallLayout.QueueFacesLeft[index])
            {
                customer.localScale = new Vector3(-1f, 1f, 1f);
            }

            Image tap = FormalUiBuild.SetImage(customer, FormalUiBuild.Clear, true);
            tap.raycastPadding = CustomerTapPadding;
            customer.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            string[] layers = { UiNames.CustomerBody, UiNames.CustomerShirt, UiNames.CustomerFace, UiNames.CustomerHair };
            for (int k = 0; k < layers.Length; k++)
            {
                RectTransform layer = FormalUiBuild.NewNode(customer, layers[k]);
                FormalUiBuild.Stretch(layer, 0f);
                FormalUiBuild.SetImage(layer, Color.white, false);
                layer.gameObject.SetActive(false);
            }
        }

        // 말풍선 뿌리. 자리 · 크기 · 판 고르기는 OrderBubbleView 가 손님마다 정한다. 처음엔 꺼 둔다.
        private static void MakeBubble(RectTransform stage, int index)
        {
            RectTransform bubble = FormalUiBuild.NewNode(stage, UiNames.StallBubblePrefix + index);
            FormalUiBuild.TopLeft(bubble, StallLayout.QueueFootX[index], StallLayout.QueueFootY - StallLayout.HeadHeight - StallLayout.BubbleHeight,
                StallLayout.BubbleIconWidth, StallLayout.BubbleHeight);

            RectTransform frame = FormalUiBuild.NewNode(bubble, UiNames.StallBubbleFrame);
            FormalUiBuild.Stretch(frame, 0f);
            FormalUiBuild.MakeSliced(frame, SpriteKeys.UiBubbleFrame, Color.white, false);

            RectTransform small = FormalUiBuild.NewNode(bubble, UiNames.StallBubbleFrameSmall);
            FormalUiBuild.Stretch(small, 0f);
            FormalUiBuild.SetImage(small, Color.white, false).sprite = FormalUiBuild.LoadSprite(SpriteKeys.UiBubbleSmall);
            small.gameObject.SetActive(false);

            RectTransform icon = FormalUiBuild.NewNode(bubble, UiNames.CakeIcon);
            icon.anchorMin = new Vector2(0.5f, 0f);
            icon.anchorMax = new Vector2(0.5f, 0f);
            icon.pivot = new Vector2(0.5f, 0f);
            icon.anchoredPosition = new Vector2(0f, StallLayout.BubbleIconBottom);
            icon.sizeDelta = new Vector2(StallLayout.BubbleIconSize, StallLayout.BubbleIconSize);
            FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

            TextMeshProUGUI label = FormalUiBuild.MakeText(bubble, UiNames.ButtonLabel, 14f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.Stretch(label.rectTransform, 0f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 14f;
            label.lineSpacing = -10f;
            label.text = "";

            bubble.gameObject.SetActive(false);
        }

        // 넷째 손님부터 세는 글자. 마루 위라 밝은 글자에 짙은 테두리. 테두리는 켜진 동안 넣어야 한다 (꺼진 TMP 는 재질이 없다).
        private static void MakeQueueMore(RectTransform stage)
        {
            TextMeshProUGUI more = FormalUiBuild.MakeText(stage, UiNames.StallQueueMoreText, 20f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            FormalUiBuild.PivotAt(more.rectTransform, StallLayout.QueueMoreX, StallLayout.QueueMoreY, new Vector2(0.5f, 0.5f),
                StallLayout.QueueMoreWidth, StallLayout.QueueMoreHeight);
            more.outlineWidth = 0.25f;
            more.outlineColor = Outline;
            more.text = "뒤에 0명";
            more.gameObject.SetActive(false);
        }

        // 골드 판 오른쪽의 손님 · 영업 상태 판.
        private static void MakeStatusPlate(Transform root)
        {
            Transform row = root.Find(UiNames.TopRow);
            RectTransform plate = FormalUiBuild.NewNode(row, UiNames.StallStatusPlate);
            FormalUiBuild.TopLeft(plate, StallLayout.StatusPlateLeft, FormalLayout.TopPlateTop, StallLayout.StatusPlateWidth, FormalLayout.TopPlateHeight);
            FormalUiBuild.SetImage(plate, FormalUiBuild.PlateDark, false);

            TextMeshProUGUI status = FormalUiBuild.MakeText(plate, UiNames.StallStatusText, 20f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            FormalUiBuild.Stretch(status.rectTransform, 4f);
            status.enableAutoSizing = true;
            status.fontSizeMin = 13f;
            status.fontSizeMax = 20f;
            status.text = "손님 0/0 · 닫힘";
        }

        // 다섯 칸. 맨 왼쪽 「열기 / 다음날로」 를 크게 · 분홍으로 둔다. 글자는 StallScreen 이 채운다 (열기 · 점원).
        private static void MakeBottomRow(Transform root)
        {
            RectTransform row = FormalUiBuild.MakeBottomRow(root);
            string[] names =
            {
                UiNames.StallOpenButton, UiNames.StallKitchenButton, UiNames.StallClerkButton, UiNames.StallRecipeCodexButton, UiNames.CloseButton,
            };
            string[] labels = { "열기", "주방", "점원", "레시피 도감", "닫기" };
            float[] widths =
            {
                StallLayout.OpenButtonWidth, StallLayout.KitchenButtonWidth, StallLayout.ClerkButtonWidth,
                StallLayout.CodexButtonWidth, StallLayout.CloseButtonWidth,
            };

            float[] lefts = FormalUiBuild.RowLefts(widths);
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform button = FormalUiBuild.MakeButton(row, names[i], labels[i], FormalLayout.RowFontSize);
                FormalUiBuild.BottomLeft(button, lefts[i], 0f, widths[i], FormalLayout.RowHeight);
            }

            row.Find(UiNames.StallOpenButton).GetComponent<Image>().color = OpenTint;
        }
    }
}

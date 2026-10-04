using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 주방 화면 프리팹을 빈 뿌리에서 새로 짓는 손질기. 자리는 KitchenLayout(그림 README 세계 좌표)이 정본이다.
    // 프리팹을 손으로 고치면 다음에 돌릴 때 덮이니 고칠 것은 여기서 고친다. 그림은 FormalArtCopy.CopyKitchen 을 먼저 돌린다.
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 540 배치/주방」 또는 eval 로 KitchenScreenSetup.Apply().
    public static class KitchenScreenSetup
    {
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/UI/KitchenScreen.prefab";

        // 배경 맨 윗줄 색. 1200 보다 긴 화면의 위와 넓적한 화면 옆을 이 색으로 채운다.
        private static readonly Color FillTop = new Color32(0x9C, 0x6E, 0x51, 0xFF);
        private static readonly Color MenuCream = new Color(1f, 0.965f, 0.9f, 1f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);

        // 쟁반 칸 순서 = 부재료 표 순서.
        private static readonly string[] IngredientIds =
        {
            "redbean", "bean", "mugwort", "sesame", "pinenut", "jujube",
        };

        [MenuItem("Mozzi/정식 화면 540 배치/주방")]
        public static void Apply()
        {
            GameObject root = FormalUiBuild.NewScreenRoot("KitchenScreen", FillTop);
            root.AddComponent<KitchenScreen>();
            Transform t = root.transform;

            FormalUiBuild.NewFillTop(t, FillTop);
            RectTransform stage = FormalUiBuild.NewStage(t);
            Image bg = FormalUiBuild.PlaceSprite(stage, UiNames.SceneBg, SpriteKeys.ScreenKitchenBg, 0f, 0f, new Vector2(0f, 1f));
            bg.raycastTarget = true;

            MakeDoor(stage);
            MakeTray(stage);
            MakeMortar(stage);
            MakeResult(stage, t);

            FormalUiBuild.MakeTopRow(t);
            MakeHint(t);
            MakeBottomRow(t);
            MakeMenuPanel(t);

            FormalUiBuild.SavePrefab(root, PrefabPath);
            Debug.Log("[KitchenScreenSetup] 저장 " + PrefabPath);
        }

        // 보이지 않는 누름 칸. 누르면 가판대로 간다.
        private static void MakeDoor(RectTransform stage)
        {
            RectTransform door = FormalUiBuild.NewNode(stage, UiNames.KitchenDoorButton);
            FormalUiBuild.TopLeft(door, KitchenLayout.DoorLeft, KitchenLayout.DoorTop,
                KitchenLayout.DoorRight - KitchenLayout.DoorLeft, KitchenLayout.DoorBottom - KitchenLayout.DoorTop);
            FormalUiBuild.SetImage(door, FormalUiBuild.Clear, true);
            Button button = door.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        // 쟁반 그림 + 칸 여섯. 칸은 투명 버튼이고 고르면 코드가 색을 넣는다. 아이콘은 칸 아래끝에 바닥을 맞춘다.
        private static void MakeTray(RectTransform stage)
        {
            FormalUiBuild.PlaceSprite(stage, "Tray", SpriteKeys.ScreenKitchenTray, KitchenLayout.TrayX, KitchenLayout.TrayY, new Vector2(0f, 1f));

            for (int i = 0; i < IngredientIds.Length; i++)
            {
                float left = KitchenLayout.TrayCellLeft[i];
                float width = KitchenLayout.TrayCellRight[i] - left + 1f;
                float height = KitchenLayout.TrayCellBottom - KitchenLayout.TrayCellTop + 1f;

                RectTransform cell = FormalUiBuild.NewNode(stage, UiNames.KitchenTrayCellPrefix + IngredientIds[i]);
                FormalUiBuild.TopLeft(cell, left, KitchenLayout.TrayCellTop, width, height);
                FormalUiBuild.SetImage(cell, FormalUiBuild.Clear, true);
                Button button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;

                RectTransform icon = FormalUiBuild.NewNode(cell, UiNames.IngredientIcon);
                icon.anchorMin = new Vector2(0.5f, 0f);
                icon.anchorMax = new Vector2(0.5f, 0f);
                icon.pivot = new Vector2(0.5f, 0f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(KitchenLayout.TrayIconSize, KitchenLayout.TrayIconSize);
                Image iconImage = FormalUiBuild.SetImage(icon, Color.white, false);
                iconImage.preserveAspect = true;

                TextMeshProUGUI count = FormalUiBuild.MakeText(cell, UiNames.KitchenTrayCountText, 15f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
                count.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                count.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                count.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                count.rectTransform.anchoredPosition = new Vector2(0f, -(KitchenLayout.TrayCountY - KitchenLayout.TrayCellBottom));
                count.rectTransform.sizeDelta = new Vector2(width + 8f, KitchenLayout.TrayCountHeight);
                count.text = "0";
            }
        }

        // 절구 + 떡메 조각 (프레임 0). 누르면 떡메 한 번. 아래에 고른 재료 칸 셋.
        private static void MakeMortar(RectTransform stage)
        {
            Sprite frame = FormalUiBuild.LoadSprite(SpriteKeys.KitchenPound(0));
            float width = 212f;
            float height = 241f;
            if (frame != null)
            {
                width = frame.rect.width;
                height = frame.rect.height;
            }

            Image mortar = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenMortar, SpriteKeys.KitchenPound(0),
                KitchenLayout.MortarPivotX, KitchenLayout.MortarPivotY, new Vector2(KitchenLayout.MortarSpritePivotX / width, 0f));
            mortar.raycastTarget = true;
            Button button = mortar.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            float slot = KitchenLayout.MortarSlotSize;
            float total = slot * UiNames.KitchenMortarSlotCount + KitchenLayout.MortarSlotGap * (UiNames.KitchenMortarSlotCount - 1);
            float left = KitchenLayout.MortarPivotX - total * 0.5f;
            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                RectTransform rect = FormalUiBuild.NewNode(stage, UiNames.KitchenMortarSlotPrefix + i);
                FormalUiBuild.TopLeft(rect, left + i * (slot + KitchenLayout.MortarSlotGap), KitchenLayout.MortarSlotTop, slot, slot);
                Image image = FormalUiBuild.SetImage(rect, Color.white, false);
                image.preserveAspect = true;
                rect.gameObject.SetActive(false);
            }

            Debug.Log("[KitchenScreenSetup] 절구 조각 " + width + "×" + height);
        }

        // 접시 + 결과 떡 · NEW · 이름. 흰 번쩍은 화면 전체를 덮되 누름은 막지 않는다.
        private static void MakeResult(RectTransform stage, Transform root)
        {
            FormalUiBuild.PlaceSprite(stage, "Plate", SpriteKeys.ScreenKitchenPlate, KitchenLayout.PlateX, KitchenLayout.PlateY, new Vector2(0.5f, 0f));

            RectTransform result = FormalUiBuild.NewNode(stage, UiNames.KitchenResult);
            FormalUiBuild.Stretch(result, 0f);

            RectTransform icon = FormalUiBuild.NewNode(result, UiNames.CakeIcon);
            FormalUiBuild.PivotAt(icon, KitchenLayout.PlateX, KitchenLayout.ResultIconBottom, new Vector2(0.5f, 0f),
                KitchenLayout.ResultIconSize, KitchenLayout.ResultIconSize);
            FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

            TextMeshProUGUI isNew = FormalUiBuild.MakeText(result, UiNames.KitchenResultNewText, 30f, new Color(0.95f, 0.35f, 0.4f, 1f), TextAlignmentOptions.Center);
            FormalUiBuild.PivotAt(isNew.rectTransform, KitchenLayout.PlateX, KitchenLayout.ResultNewY, new Vector2(0.5f, 0.5f), KitchenLayout.ResultTextWidth, 40f);
            isNew.fontStyle = FontStyles.Bold;
            isNew.text = "NEW!";

            TextMeshProUGUI name = FormalUiBuild.MakeText(result, UiNames.KitchenResultNameText, 22f, FormalUiBuild.TextDark, TextAlignmentOptions.Top);
            FormalUiBuild.PivotAt(name.rectTransform, KitchenLayout.PlateX, KitchenLayout.ResultNameTop, new Vector2(0.5f, 1f), KitchenLayout.ResultTextWidth, 60f);
            name.textWrappingMode = TextWrappingModes.Normal;
            name.text = "";
            result.gameObject.SetActive(false);

            RectTransform flash = FormalUiBuild.NewNode(root, UiNames.KitchenResultFlash);
            FormalUiBuild.Stretch(flash, -LayoutConstUi.BarBleedLength);
            FormalUiBuild.SetImage(flash, FormalUiBuild.Clear, false);
        }

        private static void MakeHint(Transform root)
        {
            TextMeshProUGUI hint = FormalUiBuild.MakeText(root, UiNames.KitchenHintText, 22f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            RectTransform rect = hint.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, FormalLayout.HintBottom);
            rect.sizeDelta = new Vector2(FormalLayout.StageWidth - 24f, FormalLayout.HintHeight);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 14f;
            hint.fontSizeMax = 22f;
            hint.outlineWidth = 0.2f;
            hint.outlineColor = new Color32(0x3A, 0x22, 0x18, 0xFF);
            hint.text = "";
        }

        private static void MakeBottomRow(Transform root)
        {
            RectTransform row = FormalUiBuild.MakeBottomRow(root);
            string[] names = { UiNames.KitchenPoundButton, UiNames.KitchenMenuButton, UiNames.StallRecipeCodexButton, UiNames.CloseButton };
            string[] labels = { "떡메 치기", "차림표", "레시피 도감", "닫기" };
            float[] widths =
            {
                KitchenLayout.PoundButtonWidth, KitchenLayout.MenuButtonWidth, KitchenLayout.CodexButtonWidth, KitchenLayout.CloseButtonWidth,
            };

            float[] lefts = FormalUiBuild.RowLefts(widths);
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform button = FormalUiBuild.MakeButton(row, names[i], labels[i], FormalLayout.RowFontSize);
                FormalUiBuild.BottomLeft(button, lefts[i], 0f, widths[i], FormalLayout.RowHeight);
            }
        }

        // 차림표 판 : 어두운 가림막(누르면 닫힘) + 크림색 상자 + 줄 열둘(2열). 처음엔 꺼 둔다.
        private static void MakeMenuPanel(Transform root)
        {
            RectTransform panel = FormalUiBuild.NewNode(root, UiNames.KitchenMenuPanel);
            FormalUiBuild.Stretch(panel, 0f);

            RectTransform blocker = FormalUiBuild.NewNode(panel, UiNames.KitchenMenuBlocker);
            FormalUiBuild.Stretch(blocker, -LayoutConstUi.BarBleedLength);
            FormalUiBuild.SetImage(blocker, Dim, true);
            blocker.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            int rows = UiNames.KitchenMenuRowCount / KitchenLayout.MenuColumns;
            float boxWidth = KitchenLayout.MenuBoxPad * 2f + KitchenLayout.MenuRowWidth * KitchenLayout.MenuColumns + KitchenLayout.MenuRowGap;
            float boxHeight = KitchenLayout.MenuBoxPad * 2f + KitchenLayout.MenuTitleHeight + rows * (KitchenLayout.MenuRowHeight + KitchenLayout.MenuRowGap);

            RectTransform box = FormalUiBuild.NewNode(panel, "Box");
            box.anchorMin = new Vector2(0.5f, 0f);
            box.anchorMax = new Vector2(0.5f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, FormalLayout.HintBottom + FormalLayout.HintHeight);
            box.sizeDelta = new Vector2(boxWidth, boxHeight);
            FormalUiBuild.MakeSliced(box, SpriteKeys.UiButtonFrame, MenuCream, true);

            TextMeshProUGUI title = FormalUiBuild.MakeText(box, UiNames.KitchenMenuTitleText, 24f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.TopLeft(title.rectTransform, KitchenLayout.MenuBoxPad, KitchenLayout.MenuBoxPad, boxWidth - KitchenLayout.MenuBoxPad * 2f, KitchenLayout.MenuTitleHeight);
            title.text = "차림표 : 누르면 바로 만든다";

            TextMeshProUGUI empty = FormalUiBuild.MakeText(box, UiNames.KitchenMenuEmptyText, 20f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.TopLeft(empty.rectTransform, KitchenLayout.MenuBoxPad, KitchenLayout.MenuBoxPad + KitchenLayout.MenuTitleHeight,
                boxWidth - KitchenLayout.MenuBoxPad * 2f, KitchenLayout.MenuRowHeight * 2f);
            empty.textWrappingMode = TextWrappingModes.Normal;
            empty.text = UiText.StallMakeEmpty;

            for (int i = 0; i < UiNames.KitchenMenuRowCount; i++)
            {
                int column = i % KitchenLayout.MenuColumns;
                int line = i / KitchenLayout.MenuColumns;
                RectTransform row = FormalUiBuild.MakeButton(box, UiNames.KitchenMenuRowPrefix + i, "", 20f);
                FormalUiBuild.TopLeft(row,
                    KitchenLayout.MenuBoxPad + column * (KitchenLayout.MenuRowWidth + KitchenLayout.MenuRowGap),
                    KitchenLayout.MenuBoxPad + KitchenLayout.MenuTitleHeight + line * (KitchenLayout.MenuRowHeight + KitchenLayout.MenuRowGap),
                    KitchenLayout.MenuRowWidth, KitchenLayout.MenuRowHeight);

                RectTransform label = row.Find(UiNames.ButtonLabel) as RectTransform;
                label.offsetMin = new Vector2(52f, 6f);

                RectTransform icon = FormalUiBuild.NewNode(row, UiNames.CakeIcon);
                icon.anchorMin = new Vector2(0f, 0.5f);
                icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = new Vector2(12f, 0f);
                icon.sizeDelta = new Vector2(32f, 32f);
                FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;
            }

            panel.gameObject.SetActive(false);
        }
    }
}

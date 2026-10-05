using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 주방 화면 프리팹을 빈 뿌리에서 새로 짓는 손질기. 자리는 KitchenLayout 이 정본이다.
    // 프리팹을 손으로 고치면 다음에 돌릴 때 덮이니 고칠 것은 여기서 고친다. 그림은 FormalArtCopy.CopyKitchen 을 먼저 돌린다.
    // 반복 07 : 서랍장 틀 · 칸 · 그릇 · 찜기 · 빚기판을 정식 그림(주방/v3)으로 바꾸고 김 · 빚기 연출 자리를 더했다. 색 곱하기는 KitchenScreen 이 단계마다 넣는다.
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 540 배치/주방」 또는 eval 로 KitchenScreenSetup.Apply().
    public static class KitchenScreenSetup
    {
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/UI/KitchenScreen.prefab";

        // 배경 맨 윗줄 색. 1200 보다 긴 화면의 위와 넓적한 화면 옆을 이 색으로 채운다.
        private static readonly Color FillTop = new Color32(0x9C, 0x6E, 0x51, 0xFF);

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
            MakeDrawers(stage);
            MakeBowl(stage);
            MakeSteamer(stage);
            MakeMortar(stage);
            MakeBoard(stage);
            MakeResult(stage, t);

            FormalUiBuild.MakeTopRow(t);
            MakeHint(t);
            MakeBottomRow(t);
            FormalUiBuild.MakeRecipeMenuPanel(t);

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

        // 서랍장 : 틀 그림 + 칸 열둘(4열 × 3줄). 칸은 서랍 앞판 그림 버튼이고 앞면에 재료 아이콘 · 남은 수. 채우기 · 색은 코드가 한다.
        private static void MakeDrawers(RectTransform stage)
        {
            int rows = UiNames.KitchenDrawerCellCount / KitchenLayout.DrawerColumns;
            float width = KitchenLayout.DrawerColumns * KitchenLayout.DrawerCellWidth + (KitchenLayout.DrawerColumns - 1) * KitchenLayout.DrawerGapX;
            float height = rows * KitchenLayout.DrawerCellHeight + (rows - 1) * KitchenLayout.DrawerGapY;
            float pad = KitchenLayout.DrawerFramePad;

            // 틀 그림(238×284)은 칸 판을 Pad 만큼 둘러싼 크기로 나왔다. 다르면 알린다.
            Image frame = FormalUiBuild.PlaceSprite(stage, "DrawerFrame", SpriteKeys.ScreenKitchenDrawerFrame,
                KitchenLayout.DrawerLeft - pad, KitchenLayout.DrawerTop - pad, new Vector2(0f, 1f));
            WarnSize(frame, width + pad * 2f, height + pad * 2f);

            for (int i = 0; i < UiNames.KitchenDrawerCellCount; i++)
            {
                int column = i % KitchenLayout.DrawerColumns;
                int line = i / KitchenLayout.DrawerColumns;
                float left = KitchenLayout.DrawerLeft + column * (KitchenLayout.DrawerCellWidth + KitchenLayout.DrawerGapX);
                float top = KitchenLayout.DrawerTop + line * (KitchenLayout.DrawerCellHeight + KitchenLayout.DrawerGapY);

                RectTransform cell = FormalUiBuild.NewNode(stage, UiNames.KitchenDrawerCellPrefix + i);
                FormalUiBuild.TopLeft(cell, left, top, KitchenLayout.DrawerCellWidth, KitchenLayout.DrawerCellHeight);
                FormalUiBuild.SetImage(cell, Color.white, true).sprite = FormalUiBuild.LoadSprite(SpriteKeys.ScreenKitchenDrawerCell);
                Button button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;

                RectTransform icon = FormalUiBuild.NewNode(cell, UiNames.IngredientIcon);
                FormalUiBuild.PivotAt(icon, KitchenLayout.DrawerCellWidth * 0.5f, KitchenLayout.DrawerIconY, new Vector2(0.5f, 0.5f),
                    KitchenLayout.DrawerIconSize, KitchenLayout.DrawerIconSize);
                FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

                TextMeshProUGUI count = FormalUiBuild.MakeText(cell, UiNames.KitchenTrayCountText, 16f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
                FormalUiBuild.PivotAt(count.rectTransform, KitchenLayout.DrawerCellWidth * 0.5f, KitchenLayout.DrawerCountY, new Vector2(0.5f, 0.5f),
                    KitchenLayout.DrawerCellWidth, KitchenLayout.DrawerCountHeight);
                count.text = "0";
            }
        }

        // 고른 재료 그릇 : 그릇 그림 위에 칸 셋 (처음엔 꺼 둔다).
        private static void MakeBowl(RectTransform stage)
        {
            Image bowl = FormalUiBuild.PlaceSprite(stage, "Bowl", SpriteKeys.ScreenKitchenBowl, KitchenLayout.BowlX, KitchenLayout.BowlY, new Vector2(0.5f, 0.5f));
            WarnSize(bowl, KitchenLayout.BowlWidth, KitchenLayout.BowlHeight);

            float slot = KitchenLayout.MortarSlotSize;
            float total = slot * UiNames.KitchenMortarSlotCount + KitchenLayout.MortarSlotGap * (UiNames.KitchenMortarSlotCount - 1);
            float left = KitchenLayout.BowlX - total * 0.5f;
            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                RectTransform rect = FormalUiBuild.NewNode(stage, UiNames.KitchenMortarSlotPrefix + i);
                FormalUiBuild.TopLeft(rect, left + i * (slot + KitchenLayout.MortarSlotGap), KitchenLayout.BowlY - slot * 0.5f, slot, slot);
                Image image = FormalUiBuild.SetImage(rect, Color.white, false);
                image.preserveAspect = true;
                rect.gameObject.SetActive(false);
            }
        }

        // 찜기 + 그 위 김 (흰 한 색 4장 · 쉴 때는 꺼짐).
        private static void MakeSteamer(RectTransform stage)
        {
            MakeTool(stage, UiNames.KitchenSteamer, SpriteKeys.ScreenKitchenSteamer, KitchenLayout.SteamerLeft, KitchenLayout.SteamerTop,
                KitchenLayout.SteamerRight, KitchenLayout.WorkBottom);

            Image steam = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenSteam, SpriteKeys.KitchenSteam(0),
                KitchenLayout.SteamLeft, KitchenLayout.SteamTop, new Vector2(0f, 1f));
            steam.color = new Color(1f, 1f, 1f, KitchenLayout.SteamAlpha);
            steam.gameObject.SetActive(false);
        }

        private static void MakeBoard(RectTransform stage)
        {
            MakeTool(stage, UiNames.KitchenBoard, SpriteKeys.ScreenKitchenBoard, KitchenLayout.BoardLeft, KitchenLayout.BoardTop,
                KitchenLayout.BoardRight, KitchenLayout.WorkBottom);
        }

        // 도구 그림 버튼 : 자리 상자 가운데에 그림을 제 크기로. 통통(scale)이 가운데에서 커지게 피벗은 가운데다.
        private static void MakeTool(RectTransform stage, string name, string spriteKey, float left, float top, float right, float bottom)
        {
            float width = right - left;
            float height = bottom - top;
            Image tool = FormalUiBuild.PlaceSprite(stage, name, spriteKey, left + width * 0.5f, top + height * 0.5f, new Vector2(0.5f, 0.5f));
            WarnSize(tool, width, height);
            tool.raycastTarget = true;
            Button button = tool.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        // 그림 크기가 KitchenLayout 자리와 다르면 알린다 (그림은 제 크기로 놓으니 자리가 어긋난다).
        private static void WarnSize(Image image, float width, float height)
        {
            Vector2 size = image.rectTransform.sizeDelta;
            if (Mathf.Approximately(size.x, width) == false || Mathf.Approximately(size.y, height) == false)
            {
                Debug.LogWarning("[KitchenScreenSetup] " + image.name + " 그림 " + size + " 이 자리 " + width + "×" + height + " 와 다르다");
            }
        }

        // 절구 + 떡메 조각 (프레임 0). 누르면 떡메 한 번.
        private static void MakeMortar(RectTransform stage)
        {
            Sprite frame = FormalUiBuild.LoadSprite(SpriteKeys.KitchenPound(0, 0));
            float width = 212f;
            float height = 241f;
            if (frame != null)
            {
                width = frame.rect.width;
                height = frame.rect.height;
            }

            Image mortar = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenMortar, SpriteKeys.KitchenPound(0, 0),
                KitchenLayout.MortarPivotX, KitchenLayout.MortarPivotY, new Vector2(KitchenLayout.MortarSpritePivotX / width, 0f));
            mortar.raycastTarget = true;
            Button button = mortar.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Debug.Log("[KitchenScreenSetup] 절구 조각 " + width + "×" + height);
        }

        // 접시(빚기판 위) + 빚는 떡(꺼 둠) + 결과 떡 · NEW · 이름. 흰 번쩍은 화면 전체를 덮되 누름은 막지 않는다.
        private static void MakeResult(RectTransform stage, Transform root)
        {
            FormalUiBuild.PlaceSprite(stage, UiNames.KitchenPlate, SpriteKeys.ScreenKitchenPlate, KitchenLayout.PlateX, KitchenLayout.PlateY, new Vector2(0.5f, 0f));

            Vector2 shapePivot = new Vector2(0.5f, (KitchenLayout.ShapeSpriteHeight - KitchenLayout.ShapeSpriteBaseY) / KitchenLayout.ShapeSpriteHeight);
            Image shape = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenShape, SpriteKeys.KitchenShape(0), KitchenLayout.PlateX, KitchenLayout.PlateY, shapePivot);
            shape.gameObject.SetActive(false);

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
            FormalUiBuild.PivotAt(name.rectTransform, KitchenLayout.PlateX, KitchenLayout.ResultNameTop, new Vector2(0.5f, 1f), KitchenLayout.ResultTextWidth, 56f);
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

        // 「떡메 치기」 버튼은 없다 (반복 06) — 도구를 직접 누른다.
        private static void MakeBottomRow(Transform root)
        {
            RectTransform row = FormalUiBuild.MakeBottomRow(root);
            string[] names = { UiNames.KitchenMenuButton, UiNames.StallRecipeCodexButton, UiNames.CloseButton };
            string[] labels = { "차림표", "레시피 도감", "닫기" };
            float[] widths = { KitchenLayout.MenuButtonWidth, KitchenLayout.CodexButtonWidth, KitchenLayout.CloseButtonWidth };

            float[] lefts = FormalUiBuild.RowLefts(widths);
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform button = FormalUiBuild.MakeButton(row, names[i], labels[i], FormalLayout.RowFontSize);
                FormalUiBuild.BottomLeft(button, lefts[i], 0f, widths[i], FormalLayout.RowHeight);
            }
        }
    }
}

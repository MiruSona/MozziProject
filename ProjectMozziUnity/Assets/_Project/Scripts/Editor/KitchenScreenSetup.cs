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
    // 반복 08 : 입체 서랍장(칸 덧그림 없음) · 맷돌 · 찜기 몸통+뚜껑 · 두 줄 작업대 · 결과는 서랍장 왼쪽 바닥의 남색 판(접시 없음).
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 540 배치/주방」 또는 eval 로 KitchenScreenSetup.Apply().
    public static class KitchenScreenSetup
    {
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/UI/KitchenScreen.prefab";

        // 배경 맨 윗줄 색. 1200 보다 긴 화면의 위와 넓적한 화면 옆을 이 색으로 채운다.
        private static readonly Color FillTop = new Color32(0x9C, 0x6E, 0x51, 0xFF);

        // 결과 판 — 코드가 찾지 않는 모양 노드라 UiNames 에 안 둔다. 색은 찹쌀이 팝업 상자(0.16, 0.16, 0.22)를 반투명으로.
        private const string ResultPanelName = "ResultPanel";
        private const string BuiltinRoundSprite = "UI/Skin/UISprite.psd";
        private static readonly Color ResultPanelColor = new Color(0.16f, 0.16f, 0.22f, 0.85f);

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
            // 뒷줄(맷돌 · 찜기)을 먼저, 앞줄(절구 · 빚기판)을 뒤에 그린다.
            MakeMill(stage);
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

        // 서랍장 : 입체 서랍장 그림 한 장(틀) + 칸 열둘(4열 × 3줄, 반복 08). 칸 앞판은 그림에 들어 있어 칸 Image 는 투명 누름 칸이고
        // 그 위에 재료 아이콘 · 남은 수만 얹는다. 고른 칸 · 닫힌 칸의 반투명 덧칠은 코드가 한다.
        private static void MakeDrawers(RectTransform stage)
        {
            Image frame = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenDrawerFrame, SpriteKeys.ScreenKitchenDrawer3d,
                KitchenLayout.DrawerLeft, KitchenLayout.DrawerTop, new Vector2(0f, 1f));
            WarnSize(frame, KitchenLayout.DrawerWidth, KitchenLayout.DrawerHeight);
            // 틀(칸 밖)을 누르면 재료 팔기 팝업. 칸이 뒤 형제라 위에 있어 칸 누름은 그대로 재료 고르기다. 배선은 KitchenScreen 이 한다.
            frame.raycastTarget = true;
            frame.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            for (int i = 0; i < UiNames.KitchenDrawerCellCount; i++)
            {
                int column = i % KitchenLayout.DrawerColumns;
                int line = i / KitchenLayout.DrawerColumns;
                float left = KitchenLayout.DrawerLeft + KitchenLayout.DrawerCellX[column];
                float top = KitchenLayout.DrawerTop + KitchenLayout.DrawerCellY[line];

                RectTransform cell = FormalUiBuild.NewNode(stage, UiNames.KitchenDrawerCellPrefix + i);
                FormalUiBuild.TopLeft(cell, left, top, KitchenLayout.DrawerCellWidth, KitchenLayout.DrawerCellHeight);
                FormalUiBuild.SetImage(cell, FormalUiBuild.Clear, true);
                Button button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;

                RectTransform icon = FormalUiBuild.NewNode(cell, UiNames.IngredientIcon);
                FormalUiBuild.PivotAt(icon, KitchenLayout.DrawerIconX, KitchenLayout.DrawerIconY, new Vector2(0.5f, 0.5f),
                    KitchenLayout.DrawerIconSize, KitchenLayout.DrawerIconSize);
                FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

                TextMeshProUGUI count = FormalUiBuild.MakeText(cell, UiNames.KitchenTrayCountText, 16f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
                FormalUiBuild.PivotAt(count.rectTransform, KitchenLayout.DrawerIconX, KitchenLayout.DrawerCountY, new Vector2(0.5f, 0.5f),
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

        // 맷돌 (프레임 0 쉼). 누르면 한 바퀴 돈다.
        private static void MakeMill(RectTransform stage)
        {
            MakeTool(stage, UiNames.KitchenMill, SpriteKeys.KitchenMill(0), KitchenLayout.MillLeft, KitchenLayout.MillTop,
                KitchenLayout.MillRight, KitchenLayout.BackBottom);
        }

        // 찜기 몸통 + 뚜껑(같은 자리에 겹침) + 그 위 김 (흰 한 색 4장 · 쉴 때는 꺼짐). 찜기는 누르지 않아 버튼이 없다.
        private static void MakeSteamer(RectTransform stage)
        {
            float width = KitchenLayout.SteamerRight - KitchenLayout.SteamerLeft;
            float height = KitchenLayout.BackBottom - KitchenLayout.SteamerTop;
            Image body = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenSteamer, SpriteKeys.ScreenKitchenSteamerBody,
                KitchenLayout.SteamerLeft, KitchenLayout.SteamerTop, new Vector2(0f, 1f));
            WarnSize(body, width, height);
            Image lid = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenSteamerLid, SpriteKeys.ScreenKitchenSteamerLid,
                KitchenLayout.SteamerLeft, KitchenLayout.SteamerTop, new Vector2(0f, 1f));
            WarnSize(lid, width, height);

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

        // 빚기판 위 빚는 떡(꺼 둠) + 서랍장 왼쪽 바닥의 결과 판(떡 · 이름/값 · NEW, 반복 08 — 접시 없음). 흰 번쩍은 화면 전체를 덮되 누름은 막지 않는다.
        private static void MakeResult(RectTransform stage, Transform root)
        {
            Vector2 shapePivot = new Vector2(0.5f, (KitchenLayout.ShapeSpriteHeight - KitchenLayout.ShapeSpriteBaseY) / KitchenLayout.ShapeSpriteHeight);
            Image shape = FormalUiBuild.PlaceSprite(stage, UiNames.KitchenShape, SpriteKeys.KitchenShape(0), KitchenLayout.BoardX, KitchenLayout.BoardSurfaceY, shapePivot);
            shape.gameObject.SetActive(false);

            RectTransform result = FormalUiBuild.NewNode(stage, UiNames.KitchenResult);
            FormalUiBuild.Stretch(result, 0f);

            // 판 : 찹쌀이 팝업 상자와 같은 남색을 반투명으로, 모서리는 Unity 기본 둥근 UISprite 로 둥글린다. 누름은 막지 않는다.
            RectTransform panel = FormalUiBuild.NewNode(result, ResultPanelName);
            FormalUiBuild.PivotAt(panel, KitchenLayout.ResultPanelLeft, KitchenLayout.ResultPanelTop, new Vector2(0f, 1f),
                KitchenLayout.ResultPanelWidth, KitchenLayout.ResultPanelHeight);
            Image panelImage = FormalUiBuild.SetImage(panel, ResultPanelColor, false);
            panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinRoundSprite);
            panelImage.type = Image.Type.Sliced;

            RectTransform icon = FormalUiBuild.NewNode(result, UiNames.CakeIcon);
            FormalUiBuild.PivotAt(icon, KitchenLayout.ResultIconLeft, KitchenLayout.ResultPanelMidY, new Vector2(0f, 0.5f),
                KitchenLayout.ResultIconSize, KitchenLayout.ResultIconSize);
            FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

            // 이름 · 값 두 줄 (KitchenScreen 이 「이름\n값G」 로 채운다).
            TextMeshProUGUI name = FormalUiBuild.MakeText(result, UiNames.KitchenResultNameText, 22f, FormalUiBuild.TextLight, TextAlignmentOptions.Left);
            FormalUiBuild.PivotAt(name.rectTransform, KitchenLayout.ResultTextLeft, KitchenLayout.ResultPanelMidY, new Vector2(0f, 0.5f),
                KitchenLayout.ResultTextRight - KitchenLayout.ResultTextLeft, KitchenLayout.ResultTextHeight);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.text = "";

            TextMeshProUGUI isNew = FormalUiBuild.MakeText(result, UiNames.KitchenResultNewText, 22f, new Color(1f, 0.45f, 0.5f, 1f), TextAlignmentOptions.Right);
            FormalUiBuild.PivotAt(isNew.rectTransform, KitchenLayout.ResultTextRight, KitchenLayout.ResultNewY, new Vector2(1f, 0.5f),
                KitchenLayout.ResultNewWidth, KitchenLayout.ResultNewHeight);
            isNew.fontStyle = FontStyles.Bold;
            isNew.text = "NEW!";
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

using Mozzi.Game.Ui;
using Mozzi.Resource;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Editor
{
    // 합성소 화면 프리팹을 빈 뿌리에서 새로 짓는 손질기. 자리는 SynthesisRoomLayout(그림 README 세계 좌표)이 정본이다.
    // 프리팹을 손으로 고치면 다음에 돌릴 때 덮이니 고칠 것은 여기서 고친다. 그림은 FormalArtCopy.CopySynthesis 를 먼저 돌린다.
    // 부르는 법 : 메뉴 「Mozzi/정식 화면 540 배치/합성소」 또는 eval 로 SynthesisScreenSetup.Apply().
    public static class SynthesisScreenSetup
    {
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/UI/SynthesisScreen.prefab";

        // 배경 맨 윗줄 색 (천장 들보). 1200 보다 긴 화면의 위와 넓적한 화면 옆을 이 색으로 채운다.
        private static readonly Color FillTop = new Color32(0x95, 0x64, 0x51, 0xFF);
        private static readonly Color PanelCream = new Color(1f, 0.965f, 0.9f, 1f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color ResultDim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color NewColor = new Color(0.95f, 0.35f, 0.4f, 1f);
        private static readonly Color Outline = new Color32(0x3A, 0x22, 0x18, 0xFF);

        [MenuItem("Mozzi/정식 화면 540 배치/합성소")]
        public static void Apply()
        {
            GameObject root = FormalUiBuild.NewScreenRoot("SynthesisScreen", FillTop);
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
            Debug.Log("[SynthesisScreenSetup] 저장 " + PrefabPath);
        }

        private static void Build(GameObject root)
        {
            root.AddComponent<SynthesisScreen>();
            Transform t = root.transform;

            FormalUiBuild.NewFillTop(t, FillTop);
            RectTransform stage = FormalUiBuild.NewStage(t);
            Image bg = FormalUiBuild.PlaceSprite(stage, UiNames.SceneBg, SpriteKeys.ScreenSynthesisBg, 0f, 0f, new Vector2(0f, 1f));
            bg.raycastTarget = true;

            // 겹 순서 = 칸 번호 순서 (위 줄 기계 · 표 → 아래 줄 기계 · 표). 표는 기계 뿌리 안 맨 뒤라 제 기계 위에 온다.
            for (int i = 0; i < SynthesisRoomLayout.SlotPivotX.Length; i++)
            {
                MakeMachine(stage, i);
            }

            FormalUiBuild.MakeTopRow(t, SynthesisRoomLayout.GoldPlateLeft, SynthesisRoomLayout.GoldPlateTop,
                SynthesisRoomLayout.GoldPlateWidth, SynthesisRoomLayout.GoldPlateHeight);
            MakeShortcutText(t);
            MakeBottomRow(t);
            MakePickPanel(t);
            MakeResultPanel(t);
        }

        // 기계 뿌리 = 조각 캔버스(208×220) 크기, 피벗(받침 발 가운데)을 칸 자리에 둔다. 안의 자리는 조각 좌표(왼쪽 위 원점).
        private static void MakeMachine(RectTransform stage, int index)
        {
            float w = SynthesisRoomLayout.PieceWidth;
            float h = SynthesisRoomLayout.PieceHeight;
            RectTransform machine = FormalUiBuild.NewNode(stage, UiNames.SynthesisMachinePrefix + index);
            Vector2 pivot = new Vector2(SynthesisRoomLayout.PiecePivotX / w, 1f - SynthesisRoomLayout.PiecePivotY / h);
            FormalUiBuild.PivotAt(machine, SynthesisRoomLayout.SlotPivotX[index], SynthesisRoomLayout.SlotPivotY[index], pivot, w, h);

            RectTransform art = FormalUiBuild.NewNode(machine, UiNames.SynthesisMachineArt);
            FormalUiBuild.Stretch(art, 0f);
            FormalUiBuild.SetImage(art, Color.white, false).sprite = FormalUiBuild.LoadSprite(SpriteKeys.ScreenSynthesisMachine);

            MakeWindow(machine);
            MakeInlet(machine, UiNames.SynthesisInletClipA, UiNames.SynthesisInletA, SynthesisRoomLayout.InletAX);
            MakeInlet(machine, UiNames.SynthesisInletClipB, UiNames.SynthesisInletB, SynthesisRoomLayout.InletBX);
            MakeOutlet(machine);

            MakeInletButton(machine, UiNames.SynthesisInletAButton, SynthesisRoomLayout.InletAX);
            MakeInletButton(machine, UiNames.SynthesisInletBButton, SynthesisRoomLayout.InletBX);
            MakeDrawerButton(machine);
            MakeBuyStand(machine);
            MakePlate(machine);
        }

        // 깔때기에 앉는 찹쌀이와 그 가림 칸. 칸은 찹쌀이 겹 자리의 위끝부터 그릇 앞 가장자리(InletClipBottom)까지라
        // 평소 모습은 그대로고, 합성을 시작해 아래로 내려갈 때만 가장자리 아래가 잘려 그릇 속으로 들어가 보인다.
        private static void MakeInlet(RectTransform machine, string clipName, string iconName, float x)
        {
            float size = LayoutConstUi.MozziIconSize;
            RectTransform clip = FormalUiBuild.NewNode(machine, clipName);
            FormalUiBuild.TopLeft(clip, x - size * 0.5f, SynthesisRoomLayout.InletIconY - size * 0.5f, size, SynthesisRoomLayout.InletClipHeight);
            clip.gameObject.AddComponent<RectMask2D>();
            MakeMozziIcon(clip, iconName, size * 0.5f, size * 0.5f);
        }

        // 둥근 창 유리 자리의 가림판(반죽 소용돌이 세 겹이 그 안에서 돈다) · 번쩍 · 반짝이. 처음엔 꺼 두고 SynthesisMachineView 가 켠다.
        // 가림판은 유리 모양 흰 원을 Mask 로만 쓰고 그리지 않는다. 번쩍 · 반짝이는 창 밖으로 퍼지므로 가림판 바깥에 둔다.
        private static void MakeWindow(RectTransform machine)
        {
            float d = SynthesisRoomLayout.WindowRadius * 2f;
            RectTransform window = FormalUiBuild.NewNode(machine, UiNames.SynthesisWindowFx);
            FormalUiBuild.PivotAt(window, SynthesisRoomLayout.WindowX, SynthesisRoomLayout.WindowY, new Vector2(0.5f, 0.5f), d, d);

            float maskSize = SynthesisRoomLayout.MaskSize;
            RectTransform mask = Middle(window, UiNames.SynthesisWindowMask, maskSize);
            FormalUiBuild.SetImage(mask, Color.white, false).sprite = FormalUiBuild.LoadSprite(SpriteKeys.FxWindowMask);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            // 겹 순서 = 형제 순서 (첫째 반죽 → 둘째 반죽 → 외곽선과 반짝이). 그림과 색은 코드가 끼운다.
            FormalUiBuild.SetImage(Middle(mask, UiNames.SynthesisSwirlA, maskSize), Color.white, false);
            FormalUiBuild.SetImage(Middle(mask, UiNames.SynthesisSwirlB, maskSize), Color.white, false);
            FormalUiBuild.SetImage(Middle(mask, UiNames.SynthesisSwirlTop, maskSize), Color.white, false);
            mask.gameObject.SetActive(false);

            RectTransform flash = Middle(window, UiNames.SynthesisMergeFlash, SynthesisRoomLayout.FlashSize);
            FormalUiBuild.SetImage(flash, Color.white, false);
            flash.gameObject.SetActive(false);

            for (int i = 0; i < UiNames.SynthesisSparkleCount; i++)
            {
                RectTransform sparkle = Middle(window, UiNames.SynthesisSparklePrefix + i, SynthesisRoomLayout.SparkleSize);
                FormalUiBuild.SetImage(sparkle, Color.white, false);
                sparkle.gameObject.SetActive(false);
            }
        }

        // 서랍 속 결과 그림자(찹쌀이 겹)와 「?」. 다 됨일 때만 켜진다.
        // 맥박 연출이 OutletResult 의 scale 을 덮어쓰므로 배율은 바깥 묶음(OutletHolder)에 준다.
        private static void MakeOutlet(RectTransform machine)
        {
            float size = LayoutConstUi.MozziIconSize;
            RectTransform holder = FormalUiBuild.NewNode(machine, "OutletHolder");
            FormalUiBuild.PivotAt(holder, SynthesisRoomLayout.OutletX, SynthesisRoomLayout.OutletY, new Vector2(0.5f, 0.5f), size, size);
            holder.localScale = new Vector3(SynthesisRoomLayout.OutletScale, SynthesisRoomLayout.OutletScale, 1f);

            RectTransform outlet = MakeMozziIcon(holder, UiNames.SynthesisOutletResult, size * 0.5f, size * 0.5f);
            TextMeshProUGUI mark = FormalUiBuild.MakeText(outlet, UiNames.SynthesisOutletMark,
                SynthesisRoomLayout.OutletMarkFontSize, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            mark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(SynthesisRoomLayout.OutletMarkSize, SynthesisRoomLayout.OutletMarkSize);
            mark.fontStyle = FontStyles.Bold;

            // 꺼진 TMP 는 재질이 아직 없어 외곽선을 넣으면 NRE 가 난다. 잠깐 켜고 넣은 뒤 다시 끈다.
            outlet.gameObject.SetActive(true);
            mark.outlineWidth = 0.25f;
            mark.outlineColor = Outline;
            outlet.gameObject.SetActive(false);
            mark.text = "?";
        }

        // 찹쌀이 겹 다섯 (몸 · 볼 둘 · 표정 · 악세사리). 그림은 코드가 끼우므로 꺼 둔다. (x, y) 는 부모 왼쪽 위 기준 가운데.
        private static RectTransform MakeMozziIcon(RectTransform parent, string name, float x, float y)
        {
            float size = LayoutConstUi.MozziIconSize;
            RectTransform holder = FormalUiBuild.NewNode(parent, name);
            FormalUiBuild.PivotAt(holder, x, y, new Vector2(0.5f, 0.5f), size, size);

            Layer(holder, UiNames.MozziIconBody, 0f, 0f, size);
            Layer(holder, UiNames.MozziIconCheekL, -LayoutConstUi.MozziIconCheekX, -LayoutConstUi.MozziIconCheekY, LayoutConstUi.MozziIconCheekSize);
            Layer(holder, UiNames.MozziIconCheekR, LayoutConstUi.MozziIconCheekX, -LayoutConstUi.MozziIconCheekY, LayoutConstUi.MozziIconCheekSize);
            Layer(holder, UiNames.MozziIconFace, 0f, 0f, size);
            Layer(holder, UiNames.MozziIconAcc, 0f, 0f, size);
            holder.gameObject.SetActive(false);
            return holder;
        }

        private static void Layer(RectTransform holder, string name, float x, float y, float size)
        {
            RectTransform layer = Middle(holder, name, size);
            layer.anchoredPosition = new Vector2(x, y);
            FormalUiBuild.SetImage(layer, Color.white, false);
        }

        // 깔때기 위 보이지 않는 누름 칸 + 빈 깔때기 「+」 말풍선. 켜고 끄기 · 리스너는 화면 코드가 한다.
        private static void MakeInletButton(RectTransform machine, string name, float x)
        {
            RectTransform rect = FormalUiBuild.NewNode(machine, name);
            FormalUiBuild.PivotAt(rect, x, SynthesisRoomLayout.InletY, new Vector2(0.5f, 0.5f),
                SynthesisRoomLayout.InletTouchWidth, SynthesisRoomLayout.InletTouchHeight);
            FormalUiBuild.SetImage(rect, FormalUiBuild.Clear, true);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            // 누름 칸 가운데 = 깔때기 입구. 말풍선은 그보다 (InletY − PlusBubbleY) 위.
            RectTransform plus = FormalUiBuild.NewNode(rect, UiNames.SynthesisInletPlus);
            plus.anchorMin = new Vector2(0.5f, 0.5f);
            plus.anchorMax = new Vector2(0.5f, 0.5f);
            plus.pivot = new Vector2(0.5f, 0.5f);
            plus.anchoredPosition = new Vector2(0f, SynthesisRoomLayout.InletY - SynthesisRoomLayout.PlusBubbleY);
            plus.sizeDelta = new Vector2(SynthesisRoomLayout.PlusBubbleWidth, SynthesisRoomLayout.PlusBubbleHeight);
            FormalUiBuild.MakeSliced(plus, SpriteKeys.UiBubbleFrame, Color.white, false);

            TextMeshProUGUI mark = FormalUiBuild.MakeText(plus, UiNames.ButtonLabel, 22f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.Stretch(mark.rectTransform, 0f);
            mark.rectTransform.offsetMin = new Vector2(0f, 6f);
            mark.fontStyle = FontStyles.Bold;
            mark.text = "+";
        }

        // 서랍 누름 칸 (받기). 다 됨 그림은 서랍이 발 아래로 나오므로 아래끝을 캔버스 끝까지 둔다.
        private static void MakeDrawerButton(RectTransform machine)
        {
            float pad = SynthesisRoomLayout.DrawerTouchPad;
            RectTransform rect = FormalUiBuild.NewNode(machine, UiNames.SynthesisDrawerButton);
            FormalUiBuild.TopLeft(rect, SynthesisRoomLayout.DrawerLeft - pad, SynthesisRoomLayout.DrawerTop - pad,
                SynthesisRoomLayout.DrawerRight - SynthesisRoomLayout.DrawerLeft + pad * 2f,
                SynthesisRoomLayout.DrawerTouchBottom - SynthesisRoomLayout.DrawerTop + pad);
            FormalUiBuild.SetImage(rect, FormalUiBuild.Clear, true);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            rect.gameObject.SetActive(false);
        }

        // 빈 받침 누름 칸 (칸 사기) = 조각 전체. 간판 크림색 면에 「칸 사기 300G」.
        private static void MakeBuyStand(RectTransform machine)
        {
            RectTransform rect = FormalUiBuild.NewNode(machine, UiNames.SynthesisBuyStand);
            FormalUiBuild.Stretch(rect, 0f);
            FormalUiBuild.SetImage(rect, FormalUiBuild.Clear, true);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            TextMeshProUGUI text = FormalUiBuild.MakeText(rect, UiNames.SynthesisBuyText, 22f, FormalUiBuild.TextDark, TextAlignmentOptions.Center);
            FormalUiBuild.TopLeft(text.rectTransform, SynthesisRoomLayout.SignLeft, SynthesisRoomLayout.SignTop,
                SynthesisRoomLayout.SignRight - SynthesisRoomLayout.SignLeft, SynthesisRoomLayout.SignBottom - SynthesisRoomLayout.SignTop);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 22f;
            text.lineSpacing = -10f;
            text.text = "칸 사기\n300G";
            rect.gameObject.SetActive(false);
        }

        // 나무 표 (9-slice). 피벗 아래 PlateTop ~ PlateBottom, 가운데 맞춤. 누르면 시작 · 다 되면 받기.
        private static void MakePlate(RectTransform machine)
        {
            float top = SynthesisRoomLayout.PiecePivotY + SynthesisRoomLayout.PlateTop;
            float height = SynthesisRoomLayout.PlateBottom - SynthesisRoomLayout.PlateTop;
            RectTransform rect = FormalUiBuild.NewNode(machine, UiNames.SynthesisPlate);
            FormalUiBuild.PivotAt(rect, SynthesisRoomLayout.PiecePivotX, top, new Vector2(0.5f, 1f), SynthesisRoomLayout.PlateWidth, height);
            FormalUiBuild.MakeSliced(rect, SpriteKeys.ScreenSynthesisPlate, Color.white, true);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            TextMeshProUGUI text = FormalUiBuild.MakeText(rect, UiNames.SynthesisPlateText, 15f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            FormalUiBuild.Stretch(text.rectTransform, 0f);
            text.rectTransform.offsetMin = new Vector2(12f, 2f);
            text.rectTransform.offsetMax = new Vector2(-12f, -2f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 15f;
            text.outlineWidth = 0.2f;
            text.outlineColor = Outline;
            text.text = "";
        }

        // 버튼 줄 바로 위 「영구 단축 0/5 · 대기 100% → 90%」.
        private static void MakeShortcutText(Transform root)
        {
            TextMeshProUGUI text = FormalUiBuild.MakeText(root, UiNames.SynthesisShortcutText, 20f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, FormalLayout.HintBottom);
            rect.sizeDelta = new Vector2(FormalLayout.StageWidth - 24f, FormalLayout.HintHeight);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 20f;
            text.outlineWidth = 0.2f;
            text.outlineColor = Outline;
            text.text = "";
        }

        private static void MakeBottomRow(Transform root)
        {
            string[] names = { UiNames.SynthesisShortcutBuyButton, UiNames.SynthesisMozziCodexButton, UiNames.CloseButton };
            string[] labels = { "단축 사기", "찹쌀이 도감", "닫기" };
            float[] widths =
            {
                SynthesisRoomLayout.ShortcutButtonWidth, SynthesisRoomLayout.CodexButtonWidth, SynthesisRoomLayout.CloseButtonWidth,
            };

            FormalUiBuild.MakeBottomRow(root, names, labels, widths);
        }

        // 찹쌀이 고르기 판 : 어두운 가림막(누르면 닫힘) + 크림색 상자(버튼 줄 위) + 제목 · 닫기 + 줄 24(2열).
        private static void MakePickPanel(Transform root)
        {
            RectTransform panel = FormalUiBuild.NewNode(root, UiNames.SynthesisPickPanel);
            FormalUiBuild.Stretch(panel, 0f);

            RectTransform blocker = FormalUiBuild.NewNode(panel, UiNames.SynthesisPickBlocker);
            FormalUiBuild.Stretch(blocker, -LayoutConstUi.BarBleedLength);
            FormalUiBuild.SetImage(blocker, Dim, true);
            blocker.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float pad = SynthesisRoomLayout.PickBoxPad;
            int columns = SynthesisRoomLayout.PickColumns;
            int lines = UiNames.SynthesisPickRowCount / columns;
            float rowW = SynthesisRoomLayout.PickRowWidth;
            float rowH = SynthesisRoomLayout.PickRowHeight;
            float gap = SynthesisRoomLayout.PickRowGap;
            float boxWidth = pad * 2f + rowW * columns + gap * (columns - 1);
            float boxHeight = pad * 2f + SynthesisRoomLayout.PickTitleHeight + lines * (rowH + gap);

            RectTransform box = FormalUiBuild.NewNode(panel, "Box");
            box.anchorMin = new Vector2(0.5f, 0f);
            box.anchorMax = new Vector2(0.5f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, FormalLayout.HintBottom + FormalLayout.HintHeight);
            box.sizeDelta = new Vector2(boxWidth, boxHeight);
            FormalUiBuild.MakeSliced(box, SpriteKeys.UiButtonFrame, PanelCream, true);

            float closeW = SynthesisRoomLayout.PickCloseWidth;
            TextMeshProUGUI title = FormalUiBuild.MakeText(box, UiNames.SynthesisPickTitleText, 20f, FormalUiBuild.TextDark, TextAlignmentOptions.Left);
            FormalUiBuild.TopLeft(title.rectTransform, pad + 4f, pad, boxWidth - pad * 2f - closeW - 8f, SynthesisRoomLayout.PickTitleHeight - 6f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 20f;
            title.text = "찹쌀이 고르기";

            RectTransform close = FormalUiBuild.MakeButton(box, UiNames.SynthesisPickCloseButton, "닫기", 18f);
            FormalUiBuild.TopLeft(close, boxWidth - pad - closeW, pad, closeW, SynthesisRoomLayout.PickTitleHeight - 6f);

            for (int i = 0; i < UiNames.SynthesisPickRowCount; i++)
            {
                int column = i % columns;
                int line = i / columns;
                RectTransform row = FormalUiBuild.MakeButton(box, UiNames.SynthesisPickRowPrefix + i, "", 17f);
                FormalUiBuild.TopLeft(row, pad + column * (rowW + gap), pad + SynthesisRoomLayout.PickTitleHeight + line * (rowH + gap), rowW, rowH);
            }

            panel.gameObject.SetActive(false);
        }

        // 결과 연출 : 화면 전체 어두운 판(누르면 닫힘) + 가운데 찹쌀이 · 이름 · NEW.
        // 커지는 연출이 ResultBox 의 scale 을 덮어쓰므로 ×2 배율은 그 안 찹쌀이 겹 묶음에 준다.
        private static void MakeResultPanel(Transform root)
        {
            RectTransform panel = FormalUiBuild.NewNode(root, UiNames.SynthesisResultPanel);
            FormalUiBuild.Stretch(panel, -LayoutConstUi.BarBleedLength);
            FormalUiBuild.SetImage(panel, ResultDim, true);
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float size = SynthesisRoomLayout.ResultBoxSize;
            RectTransform box = Middle(panel, UiNames.SynthesisResultBox, size);
            RectTransform mozzi = MakeMozziIcon(box, UiNames.SynthesisResultMozzi, size * 0.5f, size * 0.5f);
            mozzi.localScale = new Vector3(SynthesisRoomLayout.ResultMozziScale, SynthesisRoomLayout.ResultMozziScale, 1f);

            float textOffset = SynthesisRoomLayout.ResultTextOffset;
            TextMeshProUGUI isNew = FormalUiBuild.MakeText(panel, UiNames.SynthesisResultNewText, 30f, NewColor, TextAlignmentOptions.Center);
            isNew.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            isNew.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            isNew.rectTransform.anchoredPosition = new Vector2(0f, textOffset);
            isNew.rectTransform.sizeDelta = new Vector2(240f, 44f);
            isNew.fontStyle = FontStyles.Bold;
            isNew.text = "NEW!";

            TextMeshProUGUI name = FormalUiBuild.MakeText(panel, UiNames.SynthesisResultNameText, 28f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            name.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            name.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            name.rectTransform.anchoredPosition = new Vector2(0f, -textOffset);
            name.rectTransform.sizeDelta = new Vector2(400f, 44f);
            name.text = "";

            panel.gameObject.SetActive(false);
        }

        // 부모 가운데 정사각 자리.
        private static RectTransform Middle(RectTransform parent, string name, float size)
        {
            RectTransform rect = FormalUiBuild.NewNode(parent, name);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }
    }
}

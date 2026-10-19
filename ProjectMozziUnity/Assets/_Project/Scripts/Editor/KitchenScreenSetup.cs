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
        private const string HintBgName = "HintBg";
        private const string BuiltinRoundSprite = "UI/Skin/UISprite.psd";
        private static readonly Color ResultPanelColor = new Color(0.16f, 0.16f, 0.22f, 0.85f);

        private static readonly Vector2 TopLeftPivot = new Vector2(0f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        // 빚기 반죽 캔버스 · 결과 떡 자리(견줌판 점선 원 236~304) 크기. 떡 그림 크기는 KitchenScreen 이 CakeScale 배로 맞춘다.
        private const float Shape9Width = 128f;
        private const float Steamer2Width = 180f;
        private const float Steamer2Height = 199f;
        private const float Shape9Height = 96f;
        private const float CakeSpotSize = 68f;

        [MenuItem("Mozzi/정식 화면 540 배치/주방")]
        public static void Apply()
        {
            GameObject root = FormalUiBuild.NewScreenRoot("KitchenScreen", FillTop);
            root.AddComponent<KitchenScreen>();
            Transform t = root.transform;

            FormalUiBuild.NewFillTop(t, FillTop);
            RectTransform stage = FormalUiBuild.NewStage(t);
            RectTransform strip = MakeStrip(stage);
            MakePrep(MakePan(strip, KitchenLayout.PanPrep));
            MakePound(MakePan(strip, KitchenLayout.PanPound));
            MakeFinish(MakePan(strip, KitchenLayout.PanFinish));
            MakeResult(stage, t);

            FormalUiBuild.MakeTopRow(t);
            MakeHint(stage);
            MakeBottomRow(t);
            FormalUiBuild.MakeRecipeMenuPanel(t);

            FormalUiBuild.SavePrefab(root, PrefabPath);
            Debug.Log("[KitchenScreenSetup] 저장 " + PrefabPath);
        }

        // 반복 09 : 창(540×1050, RectMask2D) 안에 세 칸 띠(1620×1050)를 깐다. 띠를 미는 일은 KitchenPanSlider 가 한다.
        private static RectTransform MakeStrip(RectTransform stage)
        {
            RectTransform view = FormalUiBuild.NewNode(stage, UiNames.KitchenView);
            FormalUiBuild.TopLeft(view, 0f, 0f, KitchenLayout.PanWidth, KitchenLayout.StripHeight);
            view.gameObject.AddComponent<RectMask2D>();

            RectTransform strip = FormalUiBuild.NewNode(view, UiNames.KitchenStrip);
            FormalUiBuild.TopLeft(strip, 0f, 0f, KitchenLayout.PanWidth * KitchenLayout.PanCount, KitchenLayout.StripHeight);
            FormalUiBuild.PlaceSprite(strip, UiNames.KitchenCeil, SpriteKeys.ScreenKitchenCeil, 0f, 0f, TopLeftPivot);
            return strip;
        }

        // 칸 하나 + 그 칸의 벽·조리대 그림. 칸 안 자리는 모두 칸 왼쪽 위 기준이다.
        private static RectTransform MakePan(RectTransform strip, int pan)
        {
            RectTransform node = FormalUiBuild.NewNode(strip, UiNames.KitchenPanPrefix + pan);
            FormalUiBuild.TopLeft(node, KitchenLayout.PanWidth * pan, 0f, KitchenLayout.PanWidth, KitchenLayout.StripHeight);
            Image bg = FormalUiBuild.PlaceSprite(node, UiNames.KitchenPanBg, SpriteKeys.KitchenBg(pan), 0f, KitchenLayout.BgTop, TopLeftPivot);
            bg.raycastTarget = true;
            return node;
        }

        // 칸1 준비 : 선반 판 3줄 · 병 18개(안에 재료 아이콘 · 남은 수) · ◀ ▶ · 쪽 점 · 쟁반 · 믹서기.
        private static void MakePrep(RectTransform pan)
        {
            for (int row = 0; row < KitchenLayout.ShelfRows; row++)
            {
                FormalUiBuild.PlaceSprite(pan, UiNames.KitchenShelfBoardPrefix + row, SpriteKeys.ScreenKitchenShelf,
                    KitchenLayout.ShelfLeft, KitchenLayout.ShelfBoardY[row], TopLeftPivot);
            }

            for (int i = 0; i < UiNames.KitchenJarCount; i++)
            {
                float left = KitchenLayout.JarLeft0 + KitchenLayout.JarDx * (i % KitchenLayout.ShelfColumns);
                float top = KitchenLayout.JarTopY[i / KitchenLayout.ShelfColumns];
                Image jar = FormalUiBuild.PlaceSprite(pan, UiNames.KitchenJarPrefix + i, SpriteKeys.ScreenKitchenJar, left, top, TopLeftPivot);
                jar.raycastTarget = true;
                jar.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

                RectTransform icon = FormalUiBuild.NewNode(jar.rectTransform, UiNames.IngredientIcon);
                FormalUiBuild.TopLeft(icon, KitchenLayout.JarIconLeft, KitchenLayout.JarIconTop,
                    KitchenLayout.JarIconRight - KitchenLayout.JarIconLeft, KitchenLayout.JarIconBottom - KitchenLayout.JarIconTop);
                FormalUiBuild.SetImage(icon, Color.white, false).preserveAspect = true;

                // 개수 글자는 선반 판(짙은 나무) 위에 걸리므로 흰 글자 + 짙은 외곽선으로 읽히게 한다 (안내 글과 같은 꼴).
                TextMeshProUGUI count = FormalUiBuild.MakeText(jar.rectTransform, UiNames.KitchenTrayCountText, 16f, Color.white, TextAlignmentOptions.Center);
                FormalUiBuild.PivotAt(count.rectTransform, KitchenLayout.JarWidth * 0.5f, KitchenLayout.JarCountY, new Vector2(0.5f, 0.5f),
                    KitchenLayout.JarWidth, KitchenLayout.JarCountHeight);
                count.outlineWidth = 0.25f;
                count.outlineColor = new Color32(0x3A, 0x22, 0x18, 0xFF);
                count.raycastTarget = false;
                count.text = "0";
            }

            MakeArrow(pan, UiNames.KitchenArrowLeft, KitchenLayout.ArrowLeftX, 1f);
            MakeArrow(pan, UiNames.KitchenArrowRight, KitchenLayout.ArrowRightX, -1f);
            for (int i = 0; i < KitchenLayout.PageDotMax; i++)
            {
                FormalUiBuild.PlaceSprite(pan, UiNames.KitchenPageDotPrefix + i, SpriteKeys.ScreenKitchenDotOff,
                    KitchenLayout.PageDotLeft + KitchenLayout.PageDotGap * i, KitchenLayout.PageDotY, TopLeftPivot);
            }

            Image tray = FormalUiBuild.PlaceSprite(pan, UiNames.KitchenTray, SpriteKeys.ScreenKitchenTray, KitchenLayout.TrayLeft, KitchenLayout.TrayTop, TopLeftPivot);
            // 쟁반 위 고른 재료 칸 셋 (빈 Image, 코드가 그림을 넣고 켠다).
            for (int i = 0; i < UiNames.KitchenMortarSlotCount; i++)
            {
                GameObject slot = new GameObject(UiNames.KitchenMortarSlotPrefix + i, typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)slot.transform;
                rect.SetParent(tray.rectTransform, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(KitchenLayout.TraySlotSize, KitchenLayout.TraySlotSize);
                rect.anchoredPosition = new Vector2(KitchenLayout.TraySlotFirstX + KitchenLayout.TraySlotGap * i, -KitchenLayout.TraySlotY);
                slot.GetComponent<Image>().raycastTarget = false;
                slot.SetActive(false);
            }

            // 믹서기는 눌리는 기구다 — PlaceSprite 기본값(레이캐스트 꺼짐)이면 진짜 터치가 PanBg 로 빠진다.
            // 믹서기 blender10 : 몸통(누름 받음) + 자식 내용물 겹 (섞인 겹 하나 · 층 겹 셋). 자식이라 몸통 위, 곧 유리 통 안에 그려진다.
            // 몸통과 겹은 캔버스(105×176)가 같아 겹을 몸통에 꽉 채운다. 색은 코드(KitchenBlenderView)가 입힌다.
            Image blender = FormalUiBuild.PlaceSprite(pan, UiNames.KitchenBlender, SpriteKeys.ScreenKitchenBlenderBody, KitchenLayout.BlenderX, KitchenLayout.BlenderBottom, BottomCenter);
            blender.raycastTarget = true;
            AddBlenderLayer(blender.transform, UiNames.KitchenBlenderFill, SpriteKeys.KitchenBlenderFill(0));
            for (int layer = 0; layer < UiNames.KitchenBlenderLayerMax; layer++)
            {
                AddBlenderLayer(blender.transform, UiNames.KitchenBlenderLayerPrefix + layer, null);
            }
        }

        private static void AddBlenderLayer(Transform body, string name, string spriteKey)
        {
            RectTransform rect = FormalUiBuild.NewNode(body, name);
            FormalUiBuild.Stretch(rect, 0f);
            Image image = FormalUiBuild.SetImage(rect, Color.white, false);
            image.sprite = spriteKey == null ? null : FormalUiBuild.LoadSprite(spriteKey);
            image.enabled = image.sprite != null;
        }

        // 화살표 : 그림(27×25)보다 큰 투명 누름 칸(ArrowHitSize) 가운데에 그림을 얹는다. 오른쪽은 같은 그림을 좌우로 뒤집는다.
        private static void MakeArrow(RectTransform pan, string name, float left, float flipX)
        {
            Sprite sprite = Resources.Load<Sprite>(SpriteKeys.Root + SpriteKeys.ScreenKitchenArrow);
            Vector2 size = sprite != null ? sprite.rect.size : new Vector2(27f, 25f);
            RectTransform hit = FormalUiBuild.NewNode(pan, name);
            FormalUiBuild.PivotAt(hit, left + size.x * 0.5f, KitchenLayout.ArrowY + size.y * 0.5f, new Vector2(0.5f, 0.5f),
                KitchenLayout.ArrowHitSize, KitchenLayout.ArrowHitSize);
            FormalUiBuild.SetImage(hit, FormalUiBuild.Clear, true);
            hit.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            float half = KitchenLayout.ArrowHitSize * 0.5f;
            Image arrow = FormalUiBuild.PlaceSprite(hit, "Arrow", SpriteKeys.ScreenKitchenArrow, half, half, new Vector2(0.5f, 0.5f));
            arrow.rectTransform.localScale = new Vector3(flipX, 1f, 1f);
        }

        // 칸2 치기 : 장식 둘 + 절구 치기 겹 다섯(440×560 캔버스 아래 가운데. 장은 KitchenPound15View 가 갈아 끼운다).
        private static void MakePound(RectTransform pan)
        {
            FormalUiBuild.PlaceSprite(pan, UiNames.KitchenDecoRail, SpriteKeys.ScreenKitchenDecoRail, KitchenLayout.RailLeft, KitchenLayout.RailTop, TopLeftPivot);
            FormalUiBuild.PlaceSprite(pan, UiNames.KitchenDecoPlantShelf, SpriteKeys.ScreenKitchenDecoPlantShelf,
                KitchenLayout.PlantShelfLeft, KitchenLayout.PlantShelfTop, TopLeftPivot);
            // 떡메 pound15 : 440×560 투명 판(Pound) 밑에 겹 다섯이 꽉 차게 겹친다. 누름은 몸통(MortarBack) 하나만 받는다.
            RectTransform pound = FormalUiBuild.NewNode(pan, UiNames.KitchenPound);
            FormalUiBuild.PivotAt(pound, KitchenLayout.Pound9X, KitchenLayout.Pound9Bottom, BottomCenter,
                KitchenLayout.Pound15Width, KitchenLayout.Pound15Height);
            FormalUiBuild.SetImage(pound, FormalUiBuild.Clear, false);
            MakePoundLayer(pound, UiNames.KitchenPoundMortarBack, SpriteKeys.ScreenKitchenPound15MortarBack, true);
            MakePoundLayer(pound, UiNames.KitchenPoundDough, SpriteKeys.KitchenPound15Stage(SpriteKeys.KitchenPound15LayerDough, 0, 0), false);
            MakePoundLayer(pound, UiNames.KitchenPoundMallet, SpriteKeys.KitchenPound15Stage(SpriteKeys.KitchenPound15LayerMallet, 0, 0), false);
            MakePoundLayer(pound, UiNames.KitchenPoundMortarFront, SpriteKeys.ScreenKitchenPound15MortarFront, false);
            MakePoundLayer(pound, UiNames.KitchenPoundDoughSpill, SpriteKeys.KitchenPound15Stage(SpriteKeys.KitchenPound15LayerSpill, 0, 0), false);
        }

        private static void MakePoundLayer(RectTransform pound, string name, string spriteKey, bool raycast)
        {
            RectTransform rect = FormalUiBuild.NewNode(pound, name);
            FormalUiBuild.Stretch(rect, 0f);
            Image image = FormalUiBuild.SetImage(rect, Color.white, raycast);
            image.sprite = FormalUiBuild.LoadSprite(spriteKey);
        }

        // 칸3 마무리 : 시계 · 빚기 판 · 빚기 반죽(꺼 둠) · 찜기(닫힘 한 장 / 열림 몸통 → 김 → 뚜껑) · 결과 떡 자리(꺼 둠).
        // 찜기 셋은 캔버스(180×199)가 같고 바닥이 보이는 바닥이라 같은 자리에 겹친다.
        private static void MakeFinish(RectTransform pan)
        {
            FormalUiBuild.PlaceSprite(pan, UiNames.KitchenDecoClock, SpriteKeys.ScreenKitchenDecoClock, KitchenLayout.ClockLeft, KitchenLayout.ClockTop, TopLeftPivot);
            FormalUiBuild.PlaceSprite(pan, UiNames.KitchenBoard9, SpriteKeys.ScreenKitchenBoard9, KitchenLayout.Board9X, KitchenLayout.Board9Bottom, BottomCenter).raycastTarget = true;

            RectTransform dough = FormalUiBuild.NewNode(pan, UiNames.KitchenShapeDough);
            FormalUiBuild.PivotAt(dough, KitchenLayout.Shape9X, KitchenLayout.Shape9Bottom, BottomCenter, Shape9Width, Shape9Height);
            FormalUiBuild.SetImage(dough, Color.white, false);
            dough.gameObject.SetActive(false);

            MakeSteamer2(pan);
            FormalUiBuild.PlaceSprite(pan, UiNames.KitchenSteam9, SpriteKeys.KitchenSteam9(0), KitchenLayout.Steam9X, KitchenLayout.Steam9Bottom, BottomCenter)
                .gameObject.SetActive(false);

            RectTransform cake = FormalUiBuild.NewNode(pan, UiNames.KitchenCakeSpot);
            FormalUiBuild.PivotAt(cake, KitchenLayout.CakeSpotX, KitchenLayout.CakeSpotY, new Vector2(0.5f, 0.5f), CakeSpotSize, CakeSpotSize);
            FormalUiBuild.SetImage(cake, Color.white, false).preserveAspect = true;
            cake.gameObject.SetActive(false);
        }

        // 1단 유리 찜기 (반복 09 steamer2) : 부모 180×199 (아래 가운데 = Steamer9X · Steamer9Bottom) 밑에 겹 7개를 꽉 채워 쌓는다.
        // 누름은 받침(SteamerBase, 꽉 찬 사각형이라 찜기 전체)만 받는다. 김 서림은 아래부터 차오르게 Filled · Vertical · Bottom.
        private static void MakeSteamer2(RectTransform pan)
        {
            RectTransform steamer = FormalUiBuild.NewNode(pan, UiNames.KitchenSteamer2);
            FormalUiBuild.PivotAt(steamer, KitchenLayout.Steamer9X, KitchenLayout.Steamer9Bottom, BottomCenter, Steamer2Width, Steamer2Height);
            string[] names =
            {
                UiNames.KitchenSteamerBase, UiNames.KitchenSteamerGlassBack, UiNames.KitchenSteamerDough, UiNames.KitchenSteamerGlassFront,
                UiNames.KitchenSteamerFog, UiNames.KitchenSteamerLidClosed, UiNames.KitchenSteamerLidOpen,
            };
            string[] keys =
            {
                SpriteKeys.ScreenKitchenSteamer2Base, SpriteKeys.ScreenKitchenSteamer2GlassBack, SpriteKeys.ScreenKitchenSteamer2Dough,
                SpriteKeys.ScreenKitchenSteamer2GlassFront, SpriteKeys.ScreenKitchenSteamer2Fog, SpriteKeys.ScreenKitchenSteamer2LidClosed,
                SpriteKeys.ScreenKitchenSteamer2LidOpen,
            };
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform layer = FormalUiBuild.NewNode(steamer, names[i]);
                layer.anchorMin = Vector2.zero;
                layer.anchorMax = Vector2.one;
                layer.offsetMin = Vector2.zero;
                layer.offsetMax = Vector2.zero;
                Image image = FormalUiBuild.SetImage(layer, Color.white, names[i] == UiNames.KitchenSteamerBase);
                image.sprite = FormalUiBuild.LoadSprite(keys[i]);
            }

            Image fog = steamer.Find(UiNames.KitchenSteamerFog).GetComponent<Image>();
            fog.type = Image.Type.Filled;
            fog.fillMethod = Image.FillMethod.Vertical;
            fog.fillOrigin = (int)Image.OriginVertical.Bottom;
            fog.fillAmount = 0f;
            steamer.Find(UiNames.KitchenSteamerDough).gameObject.SetActive(false);
            steamer.Find(UiNames.KitchenSteamerLidClosed).gameObject.SetActive(false);
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

        // 안내 글 : 칸이 밀려도 그 자리 (Stage 에 바로 둔다).
        private static void MakeHint(RectTransform stage)
        {
            // 바탕 : 아랫장 그림 위에서도 글이 읽히게 결과 판과 같은 남색 반투명 둥근 판을 글 뒤에 깐다. 누름은 막지 않는다.
            RectTransform hintBg = FormalUiBuild.NewNode(stage, HintBgName);
            FormalUiBuild.TopLeft(hintBg, KitchenLayout.HintLeft, KitchenLayout.HintTop, KitchenLayout.HintWidth, KitchenLayout.HintHeight);
            Image hintBgImage = FormalUiBuild.SetImage(hintBg, ResultPanelColor, false);
            hintBgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinRoundSprite);
            hintBgImage.type = Image.Type.Sliced;

            TextMeshProUGUI hint = FormalUiBuild.MakeText(stage, UiNames.KitchenHintText, 22f, FormalUiBuild.TextLight, TextAlignmentOptions.Center);
            FormalUiBuild.TopLeft(hint.rectTransform, KitchenLayout.HintLeft, KitchenLayout.HintTop, KitchenLayout.HintWidth, KitchenLayout.HintHeight);
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
            // 「재료 팔기」는 반복 09 3판에 안내 글 줄에서 이 줄로 옮겼다 (배선은 KitchenScreen 이 이름으로 찾는다).
            string[] names = { UiNames.KitchenMenuButton, UiNames.StallRecipeCodexButton, UiNames.KitchenSellAllButton, UiNames.CloseButton };
            string[] labels = { "차림표", "레시피 도감", "재료 팔기", "닫기" };
            float[] widths = { KitchenLayout.MenuButtonWidth, KitchenLayout.CodexButtonWidth, KitchenLayout.SellAllButtonWidth, KitchenLayout.CloseButtonWidth };

            FormalUiBuild.MakeBottomRow(root, names, labels, widths);
        }
    }
}

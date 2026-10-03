namespace Mozzi.Game.Ui
{
    // 코드가 찾는 화면 프리팹의 자식 이름. 프리팹 쪽 이름은 여기가 정본이다.
    // 화면마다 접두어로 나눈다 (중첩 클래스는 안 쓴다).
    public static class UiNames
    {
        public const string CanvasRoot = "UiCanvas";

        // 코드가 Canvas 밑에 만드는 내용 칸 (안전 영역 안 360 폭). 화면 프리팹은 이 밑에 붙는다.
        public const string ContentRoot = "ContentRoot";
        public const string DebugRoot = "DebugRoot";

        // 농장 화면의 띠 뒤판 · 띠를 화면 끝까지 잇는 덧띠. FarmScreen 이 코드로 만든다.
        public const string FarmBarBack = "BarBack";
        public const string FarmBarBleed = "BarBleed";

        // 덮개 화면 공통 (머리)
        public const string TitleText = "TitleText";
        public const string GoldText = "GoldText";
        public const string CloseButton = "CloseButton";

        // 덮개 뿌리 뒤에 까는 바탕. OverlayScreen 이 코드로 만들어 내용 칸 옆 · 안전 영역 밖까지 덮는다.
        public const string OverlayBackdrop = "Backdrop";

        // 정식 화면(가판대 · 합성소)의 뼈대. Scene 은 내용 칸 가운데에 붙는 360×640 그림 좌표 칸이다.
        // 배경 그림(SceneBg)은 코드가 SpriteKeys 로 끼운다. BottomPanel 은 그림 좌표 y 440 부터 바닥까지다.
        public const string Scene = "Scene";
        public const string SceneBg = "SceneBg";
        public const string SceneFillTop = "SceneFillTop";
        public const string SceneFillBottom = "SceneFillBottom";
        public const string TopBar = "TopBar";
        public const string BottomPanel = "BottomPanel";
        public const string BottomPanelBack = "PanelBack";

        // UI 에 그리는 찹쌀이 한 마리의 겹 (Image). 자리 뿌리(예 InletA) 밑에서 찾는다.
        public const string MozziIconBody = "MozziBody";
        public const string MozziIconCheekL = "MozziCheekL";
        public const string MozziIconCheekR = "MozziCheekR";
        public const string MozziIconFace = "MozziFace";
        public const string MozziIconAcc = "MozziAcc";

        // 버튼 안 글자 자식
        public const string ButtonLabel = "Label";

        // 농장 화면 (Prefabs/UI/FarmScreen)
        public const string FarmResourceBar = "ResourceBar";
        public const string FarmTitleText = "TitleText";
        public const string FarmGoldText = "GoldText";
        public const string FarmDayText = "DayText";
        public const string FarmFenceText = "FenceText";
        public const string FarmIngredientRow = "IngredientRow";
        public const string FarmBottomBar = "BottomBar";
        public const string FarmCodexButton = "CodexButton";
        public const string FarmRecipeCodexButton = "RecipeCodexButton";
        public const string FarmStorageButton = "StorageButton";

        // 부재료 칸은 "Ingredient_" + 부재료 id (예 Ingredient_redbean).
        public const string FarmIngredientPrefix = "Ingredient_";

        // 부재료 칸·실험 토글 안의 그림 자식 (Image). 그림이 없으면 꺼 둔다.
        public const string IngredientIcon = "Icon";

        // 떡 그림 자식. 도감 칸 · 발견 연출 · 만들기 줄 · 손님 칸이 같은 이름을 쓴다.
        // 한 칸 안에 재료 그림과 떡 그림을 같이 넣지 않는다 — 이름이 같아 FindDeep 가 먼저 만난 쪽을 집는다.
        public const string CakeIcon = "Icon";

        // 밤 덮개(농장 자리만) · 아래 띠의 「다음날로」. 낮에는 둘 다 꺼 둔다.
        public const string FarmNightCover = "NightCover";
        public const string FarmNightText = "NightText";
        public const string FarmNextDayButton = "NextDayButton";

        // 합성소 화면 (Prefabs/UI/SynthesisScreen). 칸은 "Slot_" + 번호(0~3), 칸 안 이름은 칸 뿌리에서 찾는다.
        public const string SynthesisShortcutText = "ShortcutText";
        public const string SynthesisShortcutBuyButton = "ShortcutBuyButton";
        public const string SynthesisSlotPrefix = "Slot_";
        public const string SynthesisLockedGroup = "LockedGroup";
        public const string SynthesisBuySlotButton = "BuySlotButton";
        public const string SynthesisEmptyGroup = "EmptyGroup";
        public const string SynthesisMaterialAButton = "MaterialAButton";
        public const string SynthesisMaterialBButton = "MaterialBButton";
        public const string SynthesisPreviewText = "PreviewText";
        public const string SynthesisStartButton = "StartButton";
        public const string SynthesisRunningGroup = "RunningGroup";
        public const string SynthesisRemainText = "RemainText";
        public const string SynthesisDoneGroup = "DoneGroup";
        public const string SynthesisTakeButton = "TakeButton";

        // 칸 고르기 탭은 "SlotTab_" + 번호(0~3). 누르면 기계와 아래 판이 그 칸을 보여 준다.
        public const string SynthesisSlotTabPrefix = "SlotTab_";

        // 기계 (Scene 안). 투입구 둘은 찹쌀이 겹 자리, 창 연출은 WindowFx 밑, 배출구 앞 결과는 OutletResult.
        public const string SynthesisMachine = "Machine";
        public const string SynthesisInletA = "InletA";
        public const string SynthesisInletB = "InletB";
        public const string SynthesisWindowFx = "WindowFx";
        public const string SynthesisYinRoot = "YinRoot";
        public const string SynthesisYinHalfA = "YinHalfA";
        public const string SynthesisYinHalfB = "YinHalfB";
        public const string SynthesisMergeFlash = "MergeFlash";
        public const string SynthesisSparklePrefix = "Sparkle_";
        public const int SynthesisSparkleCount = 5;
        public const string SynthesisOutletResult = "OutletResult";
        public const string SynthesisOutletMark = "OutletMark";

        // 찹쌀이 고르기 목록. 줄은 "PickRow_" + 번호.
        public const string SynthesisPickPanel = "PickPanel";
        public const string SynthesisPickTitleText = "PickTitleText";
        public const string SynthesisPickCloseButton = "PickCloseButton";
        public const string SynthesisPickRowPrefix = "PickRow_";
        public const int SynthesisPickRowCount = 24;

        // 결과 연출
        public const string SynthesisResultPanel = "ResultPanel";
        public const string SynthesisResultBox = "ResultBox";
        public const string SynthesisResultNameText = "ResultNameText";
        public const string SynthesisResultNewText = "ResultNewText";

        // 찹쌀이 도감 (Prefabs/UI/MozziCodexScreen). 칸은 "Cell_" + 계열 + "_" + 단계 (+ "_shiny").
        public const string CodexCellPrefix = "Cell_";
        public const string CodexCellShinySuffix = "_shiny";
        public const string CodexCellColorBox = "ColorBox";
        public const string CodexCellNameText = "NameText";
        public const string CodexCellIngredientText = "IngredientText";
        public const string CodexCellPairButton = "PairButton";
        public const string CodexCountText = "CountText";

        // 보관함 (Prefabs/UI/StorageScreen). 줄은 "Row_" + 번호.
        public const string StorageRowPrefix = "Row_";
        public const int StorageRowCount = 24;
        public const string StorageRowNameText = "NameText";
        public const string StorageRowCountText = "CountText";
        public const string StoragePlaceButton = "PlaceButton";
        public const string StorageSellButton = "SellButton";
        public const string StorageSellAllButton = "SellAllButton";
        public const string StorageEmptyText = "EmptyText";
        public const string StorageFenceText = "FenceText";
        public const string StorageFenceButton = "FenceButton";

        // 가판대 (Prefabs/UI/StallScreen). 줄 칸은 "QueueCell_" + 번호, 만들기 줄은 "MakeRow_" + 번호,
        // 실험 토글은 "ExpToggle_" + 부재료 id.
        public const string StallStatusText = "StatusText";
        public const string StallShelfText = "ShelfText";

        // 진열대 묶음 칸은 "ShelfCell_" + 번호(0~5). 유리 진열장 안에 앞에서부터 여섯 칸만 그림으로 보인다.
        // 칸 안 떡 그림은 CakeIcon, 개수 글자는 CountText. 전체 목록은 아래 판의 ShelfText 한 줄이다.
        public const string StallShelfCellPrefix = "ShelfCell_";
        public const int StallShelfCellCount = 6;
        public const string StallShelfCountText = "CountText";
        public const string StallQueueCellPrefix = "QueueCell_";
        public const string StallMakeRowPrefix = "MakeRow_";
        public const int StallMakeRowCount = 10;
        public const string StallExpTogglePrefix = "ExpToggle_";
        public const string StallExpInfoText = "ExpInfoText";
        public const string StallExpMakeButton = "ExpMakeButton";
        public const string StallOpenButton = "OpenButton";
        public const string StallClerkButton = "ClerkButton";
        public const string StallRecipeCodexButton = "RecipeCodexButton";

        // 손님 머리 위 주문 말풍선 (Image). 주문 글자(Label)와 떡 그림(Icon)이 이 밑에 있다. 손님이 없으면 끈다.
        public const string StallOrderBubble = "OrderBubble";
        // 말풍선 흰 바탕 조각들 (몸 셋 + 꼬리 둘). 손님 종류에 따라 코드가 색을 넣는다.
        public const string StallBubbleFillPrefix = "BubbleFill_";
        public const int StallBubbleFillCount = 5;
        public const string StallBubbleEdgePrefix = "BubbleEdge_";
        public const int StallBubbleEdgeCount = 5;

        // 칠판 위에 덧댄 판과 한글 메뉴 글자.
        public const string StallMenuBoard = "MenuBoard";
        public const string StallMenuTitleText = "MenuTitleText";
        public const string StallMenuBodyText = "MenuBodyText";

        // 손님 줄 칸 안의 겹 그림 넷 (Image). 그림이 없으면 꺼 둔다.
        // 이름에 Customer 접두어를 반드시 붙인다 — FindDeep 가 화면 전체에서 먼저 만난 자식을 집기 때문이다.
        public const string CustomerBody = "CustomerBody";
        public const string CustomerHair = "CustomerHair";
        public const string CustomerShirt = "CustomerShirt";
        public const string CustomerFace = "CustomerFace";

        // 떡메 3탭 덮개 (StallScreen 안). 덮개 자체가 큰 버튼이다.
        public const string PoundPanel = "PoundPanel";
        public const string PoundHintText = "PoundHintText";
        public const string PoundCountText = "PoundCountText";
        public const string PoundDoughBox = "PoundDoughBox";

        // 만든 결과 · 발견 연출 (StallScreen 안). 판 자체가 닫기 버튼이다.
        public const string StallMadePanel = "MadePanel";
        public const string StallMadeFlash = "MadeFlash";
        public const string StallMadeNewText = "MadeNewText";
        public const string StallMadeNameText = "MadeNameText";
        public const string StallMadePriceText = "MadePriceText";

        // 돌아왔어요 팝업 (Prefabs/UI/OfflinePopup). 뿌리 자체가 닫기 버튼이다.
        public const string OfflineTitleText = "OfflineTitleText";
        public const string OfflineTimeText = "OfflineTimeText";
        public const string OfflineBodyText = "OfflineBodyText";

        // 찹쌀이 팝업 (Prefabs/UI/MozziPopup). 뿌리는 닫기 덮개, 상자 안에 이름 · 「보관함으로」 · 「닫기」.
        public const string MozziPopupBox = "MozziPopupBox";
        public const string MozziPopupTitleText = "MozziPopupTitleText";
        public const string MozziPopupNameText = "MozziPopupNameText";
        public const string MozziPopupStoreButton = "MozziPopupStoreButton";
        public const string MozziPopupCloseButton = "MozziPopupCloseButton";

        // 디버그 패널 (Prefabs/UI/DebugPanel · 에디터 전용). 「D」 로 접었다 편다.
        public const string DebugToggleButton = "DebugToggleButton";
        public const string DebugBody = "DebugBody";
        public const string DebugSpeed1Button = "DebugSpeed1Button";
        public const string DebugSpeed5Button = "DebugSpeed5Button";
        public const string DebugSpeed10Button = "DebugSpeed10Button";
        public const string DebugFillButton = "DebugFillButton";
        public const string DebugGoldButton = "DebugGoldButton";
        public const string DebugSkipButton = "DebugSkipButton";
        public const string DebugDeleteSaveButton = "DebugDeleteSaveButton";

        // 레시피 도감 (Prefabs/UI/RecipeCodexScreen). 칸은 "RecipeCell_" + 레시피 id.
        public const string RecipeCodexCellPrefix = "RecipeCell_";
        public const string RecipeCodexCellNameText = "NameText";
        public const string RecipeCodexCellIngredientText = "IngredientText";
        public const string RecipeCodexCellPriceText = "PriceText";
        public const string RecipeCodexCountText = "CountText";
    }
}

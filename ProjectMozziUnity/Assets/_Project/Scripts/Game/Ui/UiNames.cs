namespace Mozzi.Game.Ui
{
    // 코드가 찾는 화면 프리팹의 자식 이름. 프리팹 쪽 이름은 여기가 정본이다.
    // 화면마다 접두어로 나눈다 (중첩 클래스는 안 쓴다).
    public static class UiNames
    {
        public const string CanvasRoot = "UiCanvas";

        // 코드가 Canvas 밑에 만드는 내용 칸 (안전 영역 안 540 폭). 화면 프리팹은 이 밑에 붙는다.
        public const string ContentRoot = "ContentRoot";
        public const string DebugRoot = "DebugRoot";

        // 옛 360 기준 프리팹을 ×1.5 로 감싸는 껍데기 (UiManager 가 만든다). 내용 칸 밑 · 캔버스 밑(팝업) 둘이다.
        public const string Legacy360Root = "Legacy360";
        public const string Legacy360PopupRoot = "Legacy360Popup";

        // 농장 화면의 띠 뒤판 · 띠를 화면 끝까지 잇는 덧띠. FarmScreen 이 코드로 만든다.
        public const string FarmBarBack = "BarBack";
        public const string FarmBarBleed = "BarBleed";

        // 덮개 화면 공통 (머리)
        public const string TitleText = "TitleText";
        public const string GoldText = "GoldText";
        public const string CloseButton = "CloseButton";

        // 덮개 뿌리 뒤에 까는 바탕. OverlayScreen 이 코드로 만들어 내용 칸 옆 · 안전 영역 밖까지 덮는다.
        public const string OverlayBackdrop = "Backdrop";

        // 전용 화면의 배경 그림(Stage 맨 뒤)과 1200 보다 긴 화면의 위를 채우는 띠.
        public const string SceneBg = "SceneBg";
        public const string SceneFillTop = "SceneFillTop";

        // 전용 화면(540 기준) 공통 뼈대. Stage 는 내용 칸 아래끝에 붙는 540×1200 세계 좌표 칸이다 (FormalLayout).
        // 윗줄 판(TopRow)과 아래 버튼 줄(BottomRow)은 Stage 가 아니라 내용 칸 위끝 · 아래끝에 붙는다.
        public const string Stage = "Stage";
        public const string TopRow = "TopRow";
        public const string GoldPlate = "GoldPlate";
        public const string BottomRow = "BottomRow";

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

        // 옛 아래 띠. FarmScreenSetup 이 걷어 낼 때 찾는 이름이다 (코드는 더 안 쓴다).
        public const string FarmBottomBar = "BottomBar";

        // 오른쪽 아래 메뉴 아이콘 셋 (도감 · 강화 · ☰).
        public const string FarmCodexButton = "CodexButton";
        public const string FarmUpgradeButton = "UpgradeButton";
        public const string FarmMenuButton = "MenuButton";

        // 아이콘을 누르면 뜨는 작은 판 둘. 판 뒤 가림막(Blocker)을 누르면 닫힌다.
        // 강화 판 : 농장 강화 목록 (지금은 담장 한 줄). ☰ 판 : 레시피 도감 (설정 화면이 생기면 줄을 더한다).
        public const string FarmUpgradePanel = "UpgradePanel";
        public const string FarmMenuPanel = "MenuPanel";
        public const string FarmPanelBlocker = "Blocker";
        public const string FarmPanelBox = "Box";
        public const string FarmPanelTitle = "PanelTitle";
        public const string FarmUpgradeFenceText = "UpgradeFenceText";
        public const string FarmUpgradeFenceButton = "UpgradeFenceButton";
        public const string FarmRecipeCodexButton = "RecipeCodexButton";

        // 부재료 칸은 "Ingredient_" + 부재료 id (예 Ingredient_redbean).
        public const string FarmIngredientPrefix = "Ingredient_";

        // 부재료 칸·실험 토글 안의 그림 자식 (Image). 그림이 없으면 꺼 둔다.
        public const string IngredientIcon = "Icon";

        // 떡 그림 자식. 도감 칸 · 발견 연출 · 만들기 줄 · 손님 칸이 같은 이름을 쓴다.
        // 한 칸 안에 재료 그림과 떡 그림을 같이 넣지 않는다 — 이름이 같아 FindDeep 가 먼저 만난 쪽을 집는다.
        public const string CakeIcon = "Icon";

        // 밤 덮개(자원바 아래 전부) · 화면 가운데 「다음날로」. 낮에는 둘 다 꺼 둔다.
        public const string FarmNightCover = "NightCover";
        public const string FarmNightText = "NightText";
        public const string FarmNextDayButton = "NextDayButton";

        // 합성소 화면 (Prefabs/UI/SynthesisScreen). 기계 넷은 Stage 밑 "Machine_" + 칸 번호(0~3)이고,
        // 그 안 이름은 넷이 같으니 반드시 각 기계 뿌리 밑에서 찾는다.
        public const string SynthesisShortcutText = "ShortcutText";
        public const string SynthesisShortcutBuyButton = "ShortcutBuyButton";
        public const string SynthesisMozziCodexButton = "MozziCodexButton";
        public const string SynthesisMachinePrefix = "Machine_";

        // 기계 하나 안. MachineArt 는 상태별 그림(쉼 · 도는 · 다 됨 · 살 칸 · 잠김)을 갈아 끼우는 Image 다.
        // Plate = 나무 표(누르면 시작 · 다 되면 받기), DrawerButton = 서랍(받기), BuyStand = 빈 받침(칸 사기).
        public const string SynthesisMachineArt = "MachineArt";
        public const string SynthesisPlate = "Plate";
        public const string SynthesisPlateText = "PlateText";
        public const string SynthesisDrawerButton = "DrawerButton";
        public const string SynthesisBuyStand = "BuyStand";
        public const string SynthesisBuyText = "BuyText";

        // 깔때기 찹쌀이 둘은 찹쌀이 겹 자리, 창 연출은 WindowFx 밑, 서랍 속 결과 그림자는 OutletResult.
        public const string SynthesisInletA = "InletA";
        public const string SynthesisInletB = "InletB";
        // 깔때기 위의 보이지 않는 누름 칸. 빈 입구에는 그 안의 Plus 말풍선이 켜진다.
        public const string SynthesisInletAButton = "InletAButton";
        public const string SynthesisInletBButton = "InletBButton";
        public const string SynthesisInletPlus = "Plus";
        public const string SynthesisWindowFx = "WindowFx";
        public const string SynthesisYinRoot = "YinRoot";
        public const string SynthesisYinHalfA = "YinHalfA";
        public const string SynthesisYinHalfB = "YinHalfB";
        public const string SynthesisMergeFlash = "MergeFlash";
        public const string SynthesisSparklePrefix = "Sparkle_";
        public const int SynthesisSparkleCount = 5;
        public const string SynthesisOutletResult = "OutletResult";
        public const string SynthesisOutletMark = "OutletMark";

        // 찹쌀이 고르기 목록. 줄은 "PickRow_" + 번호, 판 뒤 PickBlocker 를 누르면 닫힌다.
        public const string SynthesisPickPanel = "PickPanel";
        public const string SynthesisPickBlocker = "PickBlocker";
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

        // 주방 (Prefabs/UI/KitchenScreen). 쟁반 칸은 "TrayCell_" + 부재료 id, 칸 안에 Icon · CountText.
        public const string KitchenTrayCellPrefix = "TrayCell_";
        public const string KitchenTrayCountText = "CountText";
        // 절구 + 떡메 (누르면 떡메 한 번). 고른 재료 그림은 "MortarSlot_" + 0~2.
        public const string KitchenMortar = "Mortar";
        public const string KitchenMortarSlotPrefix = "MortarSlot_";
        public const int KitchenMortarSlotCount = 3;
        // 접시 위 결과 묶음. 떡 그림은 CakeIcon.
        public const string KitchenResult = "Result";
        public const string KitchenResultNameText = "ResultNameText";
        public const string KitchenResultNewText = "ResultNewText";
        public const string KitchenResultFlash = "ResultFlash";
        public const string KitchenHintText = "HintText";
        public const string KitchenDoorButton = "DoorButton";
        public const string KitchenPoundButton = "PoundButton";
        public const string KitchenMenuButton = "MenuButton";
        // 차림표 판 : 찾은 떡을 한 번에 만드는 목록. 줄은 "MenuRow_" + 번호, 판 뒤 Blocker 를 누르면 닫힌다.
        public const string KitchenMenuPanel = "MenuPanel";
        public const string KitchenMenuBlocker = "Blocker";
        public const string KitchenMenuTitleText = "MenuTitle";
        public const string KitchenMenuRowPrefix = "MenuRow_";
        public const int KitchenMenuRowCount = 12;
        public const string KitchenMenuEmptyText = "MenuEmptyText";

        // 가판대 (Prefabs/UI/StallScreen). 윗줄 판의 손님 · 영업 상태 글자와 아래 버튼 줄 버튼들.
        public const string StallStatusPlate = "StatusPlate";
        public const string StallStatusText = "StatusText";
        public const string StallOpenButton = "OpenButton";
        public const string StallClerkButton = "ClerkButton";
        public const string StallRecipeCodexButton = "RecipeCodexButton";
        public const string StallKitchenButton = "KitchenButton";

        // 진열장 안 묶음 칸은 "ShelfCell_" + 번호(0~5, 윗단 왼→오 · 아랫단 왼→오). 칸 안 떡 그림은 CakeIcon, 개수 글자는 CountText.
        public const string StallShelfCellPrefix = "ShelfCell_";
        public const int StallShelfCellCount = 6;
        public const string StallShelfCountText = "CountText";

        // 앞 손님 셋은 Stage 밑 "Customer_" + 번호(뿌리 = 누르는 버튼), 주문 말풍선은 "Bubble_" + 번호.
        // 말풍선 안 : 늘어나는 판 Frame · 작은 판 FrameSmall · 떡 그림 CakeIcon · 글자 ButtonLabel. 넷째부터는 QueueMoreText 글자.
        public const string StallCustomerPrefix = "Customer_";
        public const string StallBubblePrefix = "Bubble_";
        public const string StallBubbleFrame = "Frame";
        public const string StallBubbleFrameSmall = "FrameSmall";
        public const string StallQueueMoreText = "QueueMoreText";

        // 칠판 녹색 면 누름 칸(MenuBoard)과 그 위 진열 요약 글자, 칠판 머리 장식 위 「오늘의 떡」 글자.
        public const string StallMenuBoard = "MenuBoard";
        public const string StallMenuTitleText = "MenuTitleText";
        public const string StallMenuBodyText = "MenuBodyText";

        // 손님 뿌리 안의 겹 그림 넷 (Image). 그림이 없으면 꺼 둔다.
        // 이름에 Customer 접두어를 반드시 붙인다 — FindDeep 가 화면 전체에서 먼저 만난 자식을 집기 때문이다.
        public const string CustomerBody = "CustomerBody";
        public const string CustomerHair = "CustomerHair";
        public const string CustomerShirt = "CustomerShirt";
        public const string CustomerFace = "CustomerFace";

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

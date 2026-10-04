namespace Mozzi.Resource
{
    // 스프라이트 키 앞자리는 코드 상수로만 둔다. 표의 spriteKey("Mozzi/grass1") 앞에 붙어
    // Resources/Art/Mozzi/grass1 이 된다.
    public static class SpriteKeys
    {
        public const string Root = "Art/";

        // 메인 화면 건물 셋 (아래 가운데 피벗). 데이터 표가 없어 키를 코드 상수로 둔다.
        public const string BuildingSynthesis = "Building/synthesis_main";
        public const string BuildingStall = "Building/stall_main";
        public const string BuildingStorage = "Building/storage_main";

        // 메인 화면 오른쪽 아래 메뉴 아이콘 셋 (56×56). FarmScreenSetup 이 프리팹에 넣는다.
        public const string UiIconCodex = "UI/icon_codex";
        public const string UiIconUpgrade = "UI/icon_upgrade";
        public const string UiIconMenu = "UI/icon_menu";

        // 정식 화면 배경 · 합성 기계(쉼) · 합성 연출 조각. 표가 없어 코드 상수로 둔다.
        public const string ScreenStallBg = "Screen/stall_bg";
        public const string ScreenSynthesisBg = "Screen/synthesis_bg";
        public const string ScreenSynthesisMachine = "Screen/synthesis_machine";
        public const string FxYinYangHalf = "Fx/fx_yinyang_half";
        public const string FxMergeFlash = "Fx/fx_merge_flash";
        public const string FxSparkle = "Fx/fx_sparkle";

        // 주방 화면 (540×1200 배경 · 쟁반 · 접시 · 절구+떡메 7장). FormalArtCopy 가 정식 out 에서 들인다.
        public const string ScreenKitchenBg = "Screen/kitchen_bg";
        public const string ScreenKitchenTray = "Screen/kitchen_tray";
        public const string ScreenKitchenPlate = "Screen/kitchen_plate";
        public const string ScreenKitchenPoundPrefix = "Screen/kitchen_pound_";
        public const int KitchenPoundFrameCount = 7;

        // 가판대 화면 부품 (계산대 · 주인 · 진열장 · 소품 셋 · 칠판 머리 장식). FormalArtCopy.CopyStall 이 정식 out 에서 들인다.
        public const string ScreenStallCounter = "Screen/stall_counter";
        public const string ScreenStallOwner = "Screen/stall_owner";
        public const string ScreenStallDisplayCase = "Screen/display_case";
        public const string ScreenStallVase = "Screen/stall_prop_vase";
        public const string ScreenStallRegister = "Screen/stall_prop_register";
        public const string ScreenStallMenuStand = "Screen/stall_prop_menu";
        public const string ScreenStallBoardTopper = "Screen/stall_board_topper";

        // 9-slice 틀 (테두리는 MozziArtImport.BorderFor 가 넣는다). 꼬리 가운데 말풍선은 늘리지 않고 제 크기(54×29)로만 쓴다.
        public const string UiButtonFrame = "UI/ui_button";
        public const string UiBubbleFrame = "UI/ui_bubble";
        public const string UiBubbleSmall = "UI/ui_bubble_c";

        public static string KitchenPound(int frame)
        {
            return ScreenKitchenPoundPrefix + frame;
        }

        // 합성소 화면 (540×1200 배경 · 기계 조각 208×220 상태별 · 나무 표 9-slice). FormalArtCopy 가 정식 out 에서 들인다.
        public const string ScreenSynthesisMachineRunPrefix = "Screen/synthesis_machine_run_";
        public const int SynthesisMachineRunFrameCount = 8;
        public const string ScreenSynthesisMachineDone = "Screen/synthesis_machine_done";
        public const string ScreenSynthesisSlotBuy = "Screen/synthesis_slot_buy";
        public const string ScreenSynthesisSlotLocked = "Screen/synthesis_slot_locked";
        public const string ScreenSynthesisPlate = "Screen/synthesis_plate";

        public static string SynthesisMachineRun(int frame)
        {
            return ScreenSynthesisMachineRunPrefix + frame;
        }

        // 평범한 떡. Recipe 표에 줄이 없어 건물과 같이 코드 상수로 둔다.
        public const string CakePlain = "Cake/plain_cake";

        // 메인 화면 하늘 (아래 가운데 피벗). 없으면 SkyView 가 단색 네모로 대신한다.
        public const string MainSky = "Main/sky";

        // 메인 화면 길 (아래 가운데 피벗) · 농장 잔디 바탕 · 통나무 울타리 마디 셋. SpriteFarmGroundPainter 가 깐다.
        public const string MainPath = "Main/path_main";
        public const string FarmGrass = "Farm/grass_base";
        public const string FarmFenceH = "Farm/fence_h";
        public const string FarmFenceV = "Farm/fence_v";
        public const string FarmFencePost = "Farm/fence_post";
    }
}

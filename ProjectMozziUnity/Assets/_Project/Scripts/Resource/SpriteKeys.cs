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

        // 농장 건물 위 알림 표식 (16×16). 합성 다 됨 · 가판대 손님 있음.
        public const string UiBadgeAlert = "UI/ui_badge_alert";

        // 정식 화면 배경 · 합성 기계(쉼) · 합성 연출 조각. 표가 없어 코드 상수로 둔다.
        public const string ScreenStallBg = "Screen/stall_bg";
        public const string ScreenSynthesisBg = "Screen/synthesis_bg";
        public const string ScreenSynthesisMachine = "Screen/synthesis_machine";
        // 합성 창 유리 모양(60×60 흰 원). 창 안에서 도는 반죽 소용돌이를 이 모양으로 자르는 Mask 그림이다.
        public const string FxWindowMask = "Fx/fx_window_mask";
        // 창 안 반죽 소용돌이 세 겹 (60×60 낱장, 뒤에 프레임 번호). a · b 는 회색 반죽이라 종 색을 곱하고, top 은 외곽선 + 반짝이라 그대로 얹는다.
        public const string FxSwirlAPrefix = "Fx/fx_swirl_a_";
        public const string FxSwirlBPrefix = "Fx/fx_swirl_b_";
        public const string FxSwirlTopPrefix = "Fx/fx_swirl_top_";
        public const string FxMergeFlash = "Fx/fx_merge_flash";
        public const string FxSparkle = "Fx/fx_sparkle";

        // 주방 화면 (540×1200 배경 · 입체 서랍장 · 그릇 · 맷돌 5장 · 찜기 몸통+뚜껑 · 빚기판 · 절구+떡메 단계 셋 × 7장 · 빚기 3장 · 김 4장).
        // FormalArtCopy 가 정식 주방/v3/out (반복 07) · 반복08/out (반복 08) 에서 들인다.
        public const string ScreenKitchenBg = "Screen/kitchen_bg";
        public const string ScreenKitchenDrawer3d = "Screen/kitchen_drawer_3d";
        public const string ScreenKitchenBowl = "Screen/kitchen_bowl";
        public const string ScreenKitchenSteamerBody = "Screen/kitchen_steamer_body";
        public const string ScreenKitchenSteamerLid = "Screen/kitchen_steamer_lid";
        public const string ScreenKitchenBoard = "Screen/kitchen_board";
        public const string ScreenKitchenMillPrefix = "Screen/kitchen_mill_";
        public const int KitchenMillFrameCount = 5;
        public const int KitchenPoundFrameCount = 7;
        public const string ScreenKitchenShapePrefix = "Screen/kitchen_shape_";
        public const int KitchenShapeFrameCount = 3;
        public const string FxKitchenSteamPrefix = "Fx/kitchen_steam_";
        public const int KitchenSteamFrameCount = 4;

        // 떡메 단계 (반죽 모습) : 0 간 가루 · 1 거친 반죽 · 2 매끈한 떡. 단계마다 7장 묶음이 따로 있다 (반복 08 새 그림, 5번이 때림).
        private static readonly string[] KitchenPoundStagePrefixes =
        {
            "Screen/kitchen_pound_", "Screen/kitchen_pound_rough_", "Screen/kitchen_pound_smooth_",
        };

        public static int KitchenPoundStageCount
        {
            get { return KitchenPoundStagePrefixes.Length; }
        }

        // 가판대 화면 부품 (계산대 · 주인 · 진열장 · 소품 둘 · 칠판 머리 장식). FormalArtCopy.CopyStall 이 정식 out 에서 들인다.
        public const string ScreenStallCounter = "Screen/stall_counter";
        public const string ScreenStallOwner = "Screen/stall_owner";
        public const string ScreenStallDisplayCase = "Screen/display_case";
        public const string ScreenStallVase = "Screen/stall_prop_vase";
        public const string ScreenStallRegister = "Screen/stall_prop_register";
        public const string ScreenStallBoardTopper = "Screen/stall_board_topper";

        // 9-slice 틀 (테두리는 MozziArtImport.BorderFor 가 넣는다). 꼬리 가운데 말풍선은 늘리지 않고 제 크기(54×29)로만 쓴다.
        public const string UiButtonFrame = "UI/ui_button";
        public const string UiBubbleFrame = "UI/ui_bubble";
        public const string UiBubbleSmall = "UI/ui_bubble_c";

        public static string KitchenPound(int stage, int frame)
        {
            return KitchenPoundStagePrefixes[stage] + frame;
        }

        public static string KitchenMill(int frame)
        {
            return ScreenKitchenMillPrefix + frame;
        }

        public static string KitchenShape(int frame)
        {
            return ScreenKitchenShapePrefix + frame;
        }

        public static string KitchenSteam(int frame)
        {
            return FxKitchenSteamPrefix + frame;
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

        // 진열장에 쌓는 한 개짜리 떡 그림. 떡 키("Cake/redbean_cake")에서 규칙으로 뽑는다 -> "CakeOne/redbean_cake_one".
        public const string CakeFolder = "Cake/";
        public const string CakeOneFolder = "CakeOne/";
        public const string CakeOneSuffix = "_one";

        public static string CakeOne(string cakeKey)
        {
            return CakeOneFolder + cakeKey.Substring(CakeFolder.Length) + CakeOneSuffix;
        }

        // 메인 화면 하늘 (아래 가운데 피벗). 없으면 SkyView 가 단색 네모로 대신한다.
        public const string MainSky = "Main/sky";

        // 메인 화면 길 (아래 가운데 피벗) · 농장 잔디 바탕 · 통나무 울타리 마디 셋. SpriteFarmGroundPainter 가 깐다.
        public const string MainPath = "Main/path_main";
        public const string FarmGrass = "Farm/grass_base";
        public const string FarmFenceH = "Farm/fence_h";
        public const string FarmFenceV = "Farm/fence_v";
        public const string FarmFencePost = "Farm/fence_post";

        // 농장 장치 (뒤에 프레임 번호, 0 = 쉼). 장 수는 FarmDeviceLayout 에 있다. FormalArtCopy 가 반복08/out 에서 들인다.
        public const string FarmCollectorPrefix = "Farm/farm_collector_";
        public const string FarmFeederPrefix = "Farm/farm_feeder_";

        // 가판대 장식 다섯 (그림만, 누름 없음). StallScreenSetup 이 프리팹에 넣는다.
        public const string ScreenStallDecorFrame = "Screen/stall_decor_frame";
        public const string ScreenStallDecorShelf = "Screen/stall_decor_shelf";
        public const string ScreenStallDecorWreath = "Screen/stall_decor_wreath";
        public const string ScreenStallDecorPlant = "Screen/stall_decor_plant";
        public const string ScreenStallDecorSign = "Screen/stall_decor_sign";
    }
}

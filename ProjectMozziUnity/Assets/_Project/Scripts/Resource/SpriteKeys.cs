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
        public const string ScreenKitchenBoard = "Screen/kitchen_board";
        public const string ScreenKitchenMillPrefix = "Screen/kitchen_mill_";
        public const int KitchenMillFrameCount = 5;
        public const string ScreenKitchenShapePrefix = "Screen/kitchen_shape_";
        public const int KitchenShapeFrameCount = 3;
        public const string FxKitchenSteamPrefix = "Fx/kitchen_steam_";
        public const int KitchenSteamFrameCount = 4;

        // 반복 09 주방 (옆으로 세 칸 · 한 칸 540×1050). FormalArtCopy.CopyIter09Kitchen 이 정식/반복09 에서 들인다.
        // 천장 1620×250 한 장 · 칸 배경 셋 · 선반 · 병 · 쪽 넘김 화살표 · 쪽 점 · 쟁반 · 장식 셋 · 1단 유리 찜기 겹 7(steamer2).
        public const string ScreenKitchenCeil = "Screen/kitchen_ceil";
        public const string ScreenKitchenBgPrefix = "Screen/kitchen_bg_";
        public const int KitchenBgPanCount = 3;
        public const string ScreenKitchenShelf = "Screen/kitchen_shelf";
        public const string ScreenKitchenJar = "Screen/kitchen_jar";
        public const string ScreenKitchenArrow = "Screen/kitchen_arrow";
        public const string ScreenKitchenDotOn = "Screen/kitchen_dot_on";
        public const string ScreenKitchenDotOff = "Screen/kitchen_dot_off";
        public const string ScreenKitchenTray = "Screen/kitchen_tray";
        public const string ScreenKitchenDecoRail = "Screen/kitchen_deco_rail";
        public const string ScreenKitchenDecoPlantShelf = "Screen/kitchen_deco_plantshelf";
        public const string ScreenKitchenDecoClock = "Screen/kitchen_deco_clock";
        public const string ScreenKitchenBoard9 = "Screen/kitchen_board9";
        public const string ScreenKitchenSteamer2Base = "Screen/kitchen_steamer2_base";
        public const string ScreenKitchenSteamer2GlassBack = "Screen/kitchen_steamer2_glass_back";
        public const string ScreenKitchenSteamer2Dough = "Screen/kitchen_steamer2_dough";
        public const string ScreenKitchenSteamer2GlassFront = "Screen/kitchen_steamer2_glass_front";
        public const string ScreenKitchenSteamer2Fog = "Screen/kitchen_steamer2_fog";
        public const string ScreenKitchenSteamer2LidClosed = "Screen/kitchen_steamer2_lid_closed";
        public const string ScreenKitchenSteamer2LidOpen = "Screen/kitchen_steamer2_lid_open";

        public const string FxKitchenSteam9Prefix = "Fx/kitchen_steam9_";
        public const int KitchenSteam9FrameCount = 3;
        // 빚기 반죽 6장 (캔버스 128×96). 그림은 다른 갈래가 그리는 중 — 들이기 전에는 프리팹 자리만 있다.
        public const string ScreenKitchenShape9Prefix = "Screen/kitchen_shape9_";
        public const int KitchenShape9FrameCount = 6;

        // 떡메 pound15 : 440×560 겹. 단계(s1~s3) 8장 · 넘어감(t12 · t23) 4장. 겹 이름은 mallet · dough · dough_spill.
        public const string ScreenKitchenPound15MortarBack = "Screen/kitchen_pound15_mortar_back";
        public const string ScreenKitchenPound15MortarFront = "Screen/kitchen_pound15_mortar_front";
        public const string KitchenPound15LayerMallet = "mallet";
        public const string KitchenPound15LayerDough = "dough";
        public const string KitchenPound15LayerSpill = "dough_spill";
        public const int KitchenPound15StageCount = 3;
        public const int KitchenPound15StageFrameCount = 8;
        public const int KitchenPound15TurnFrameCount = 4;

        // stage 0~2 → s1~s3.
        public static string KitchenPound15Stage(string layer, int stage, int frame)
        {
            return "Screen/kitchen_pound15_" + layer + "_s" + (stage + 1) + "_" + frame;
        }

        // turn 0 → t12 (1→2단계), 1 → t23.
        public static string KitchenPound15Turn(string layer, int turn, int frame)
        {
            return "Screen/kitchen_pound15_" + layer + "_t" + (turn + 1) + (turn + 2) + "_" + frame;
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

        // pan 은 0~2 (칸 번호). 파일 이름은 bg_1..3 이라 1 을 더한다.
        public static string KitchenBg(int pan)
        {
            return ScreenKitchenBgPrefix + (pan + 1);
        }

        // 반복 09 믹서기 blender10 : 몸통 한 장 + 내용물 회색 겹 9장 (코드가 재료 색을 곱한다).
        // 층 마스크는 장 0~5 에만 있다 (n = 층 수 2·3, L0 = 맨 아래 층).
        public const string ScreenKitchenBlenderBody = "Screen/kitchen_blender10_body";
        public const string ScreenKitchenBlenderFillPrefix = "Screen/kitchen_blender10_fill_";
        public const int KitchenBlenderFillCount = 9;
        public const int KitchenBlenderLayerFrameCount = 6;

        public static string KitchenBlenderFill(int frame)
        {
            return ScreenKitchenBlenderFillPrefix + frame;
        }

        public static string KitchenBlenderLayer(int frame, int layerCount, int layer)
        {
            return ScreenKitchenBlenderFillPrefix + frame + "_n" + layerCount + "_L" + layer;
        }

        public static string KitchenSteam9(int frame)
        {
            return FxKitchenSteam9Prefix + frame;
        }

        public static string KitchenShape9(int frame)
        {
            return ScreenKitchenShape9Prefix + frame;
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

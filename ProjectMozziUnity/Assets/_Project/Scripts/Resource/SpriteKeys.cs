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

        // 정식 화면 배경 (360×800) · 합성 기계 · 합성 연출 조각. 표가 없어 코드 상수로 둔다.
        public const string ScreenStallBg = "Screen/stall_bg";
        public const string ScreenSynthesisBg = "Screen/synthesis_bg";
        public const string ScreenSynthesisMachine = "Screen/synthesis_machine";
        public const string FxYinYangHalf = "Fx/fx_yinyang_half";
        public const string FxMergeFlash = "Fx/fx_merge_flash";
        public const string FxSparkle = "Fx/fx_sparkle";

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

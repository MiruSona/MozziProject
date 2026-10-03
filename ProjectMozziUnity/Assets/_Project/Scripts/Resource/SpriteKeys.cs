namespace Mozzi.Resource
{
    // 스프라이트 키 앞자리는 코드 상수로만 둔다. 표의 spriteKey("Mozzi/grass1") 앞에 붙어
    // Resources/Art/Mozzi/grass1 이 된다.
    public static class SpriteKeys
    {
        public const string Root = "Art/";

        // 농장 바닥·담장 타일. 오른쪽 담장·모서리는 왼쪽 그림을 뒤집어 쓴다.
        public const string FarmGroundPrefix = "Farm/ground_grass_";
        public const string FarmFenceTop = "Farm/fence_top";
        public const string FarmFenceBottom = "Farm/fence_bottom";
        public const string FarmFenceSide = "Farm/fence_side";
        public const string FarmFenceCornerTop = "Farm/fence_corner_top";
        public const string FarmFenceCornerBottom = "Farm/fence_corner_bottom";

        // 건물 2채. 데이터 표가 없어 키를 코드 상수로 둔다.
        public const string BuildingSynthesis = "Building/synthesis";
        public const string BuildingStall = "Building/stall";

        // 정식 화면 배경 (360×800) · 합성 기계 · 합성 연출 조각. 표가 없어 코드 상수로 둔다.
        public const string ScreenStallBg = "Screen/stall_bg";
        public const string ScreenSynthesisBg = "Screen/synthesis_bg";
        public const string ScreenSynthesisMachine = "Screen/synthesis_machine";
        public const string FxYinYangHalf = "Fx/fx_yinyang_half";
        public const string FxMergeFlash = "Fx/fx_merge_flash";
        public const string FxSparkle = "Fx/fx_sparkle";

        // 평범한 떡. Recipe 표에 줄이 없어 건물과 같이 코드 상수로 둔다.
        public const string CakePlain = "Cake/plain_cake";
    }
}

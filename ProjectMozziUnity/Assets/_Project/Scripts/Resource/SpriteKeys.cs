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
    }
}

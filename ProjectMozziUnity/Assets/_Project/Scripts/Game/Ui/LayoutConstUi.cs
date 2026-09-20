namespace Mozzi.Game.Ui
{
    // 자원바 · 띠의 px 자리 (세로조각1판설계 3-2). 기준 360×640. 프리팹을 만들 때와 Canvas 설정에 쓴다.
    public static class LayoutConstUi
    {
        public const float ReferenceWidth = 360f;
        public const float ReferenceHeight = 640f;

        public const float ResourceBarHeight = 56f;
        public const float BuildingStripHeight = 80f;
        public const float BottomBarHeight = 80f;

        // 부재료 그림 칸 (px). 아이콘을 켜면 글씨를 그만큼 오른쪽으로 민다.
        public const float IngredientIconSize = 18f;
        public const float IngredientIconLeft = 2f;
        public const float IngredientTextIndent = 20f;

        public const float ToggleIconSize = 20f;
        public const float ToggleIconLeft = 4f;
        public const float ToggleTextIndent = 24f;
    }
}

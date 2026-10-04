namespace Mozzi.Game.Ui
{
    // 전용 화면(가판대 · 주방 · 합성소) 공통 자리. 화면마다 Stage(540×1200)를 내용 칸 아래끝 가운데에 붙이고,
    // Stage 안은 그림 README 의 세계 좌표(왼쪽 위 원점 · y 아래로)를 그대로 쓴다. 9:16 이면 위 240 줄이 화면 밖으로 나간다.
    public static class FormalLayout
    {
        public const float StageWidth = 540f;
        public const float StageHeight = 1200f;

        // 9:16(540×960) 화면에서 Stage 위쪽이 잘리는 높이. README 의 「9:16 화면 좌표」 = 세계 y − 이 값.
        public const float CropTop916 = 240f;

        // 아래 버튼 줄. 내용 칸 아래끝에서 위로 RowBottom 띄우고 높이 RowHeight, 버튼 사이 RowGap.
        public const float RowBottom = 24f;
        public const float RowHeight = 80f;
        public const float RowGap = 8f;
        public const float RowFontSize = 22f;

        // 윗줄 골드 판 (내용 칸 왼쪽 위 기준).
        public const float TopPlateLeft = 12f;
        public const float TopPlateTop = 12f;
        public const float TopPlateWidth = 170f;
        public const float TopPlateHeight = 44f;

        // 버튼 줄 바로 위 안내 글줄 (내용 칸 아래끝 기준).
        public const float HintBottom = RowBottom + RowHeight + 8f;
        public const float HintHeight = 36f;

        // Sliced Image 의 pixelsPerUnitMultiplier. 그림 PPU 32 와 캔버스 referencePixelsPerUnit 100 이 달라
        // 이 값을 안 넣으면 9-slice 테두리가 3.125배로 두꺼워진다.
        public const float SliceMultiplier = 100f / 32f;
    }
}

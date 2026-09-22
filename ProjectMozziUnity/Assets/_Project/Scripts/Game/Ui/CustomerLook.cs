namespace Mozzi.Game.Ui
{
    // 손님 한 명의 겉꼴 — 머리 모양 · 머리 색 · 옷 색. 씨앗 하나에서 나오므로 세이브에는 씨앗만 싣는다.
    public readonly struct CustomerLook
    {
        // 1 ~ CustomerArtKeys.HairShapeCount.
        public readonly int HairShape;

        // CustomerArtKeys.HairColors · ShirtColors 의 자리 번호.
        public readonly int HairColor;
        public readonly int ShirtColor;

        public CustomerLook(int hairShape, int hairColor, int shirtColor)
        {
            HairShape = hairShape;
            HairColor = hairColor;
            ShirtColor = shirtColor;
        }

        // 같은 씨앗은 늘 같은 겉꼴. 음수는 부호 비트를 떼어 접는다 (int.MinValue 도 안전하다).
        public static CustomerLook FromSeed(int seed)
        {
            int value = seed & int.MaxValue;

            int hairShape = value % CustomerArtKeys.HairShapeCount + 1;
            value /= CustomerArtKeys.HairShapeCount;

            int hairColor = value % CustomerArtKeys.HairColors.Length;
            value /= CustomerArtKeys.HairColors.Length;

            int shirtColor = value % CustomerArtKeys.ShirtColors.Length;
            return new CustomerLook(hairShape, hairColor, shirtColor);
        }
    }
}

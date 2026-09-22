namespace Mozzi.Game.Ui
{
    // 손님 겹 그림의 스프라이트 키. 앞자리 "Art/"(SpriteKeys.Root) 는 스프라이트 원천이 붙여 준다.
    // 몸 1 · 머리 2모양×4색 · 옷 4색 · 표정 3 = 16장을 Resources/Art/Customer/ 에 미리 구워 둔다.
    public static class CustomerArtKeys
    {
        public const string Folder = "Customer/";
        public const string Body = Folder + "body";

        public const int HairShapeCount = 2;
        public const int FaceCount = 3;

        public static readonly string[] HairColors = { "black", "brown", "orange", "purple" };
        public static readonly string[] ShirtColors = { "red", "green", "blue", "yellow" };

        // shape 는 1 ~ HairShapeCount, colorIndex 는 HairColors 의 자리. 파일은 hair_02_brown 처럼 두 자리다.
        public static string Hair(int shape, int colorIndex)
        {
            return Folder + "hair_" + shape.ToString("00") + "_" + HairColors[colorIndex];
        }

        public static string Shirt(int colorIndex)
        {
            return Folder + "shirt_" + ShirtColors[colorIndex];
        }

        public static string Face(CustomerFace face)
        {
            return Folder + "face_" + ((int)face).ToString("00");
        }
    }
}

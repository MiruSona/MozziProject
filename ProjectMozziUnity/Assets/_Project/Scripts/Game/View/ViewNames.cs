namespace Mozzi.Game.View
{
    // 코드가 찾는 월드 오브젝트의 자식 이름과 애니 상태 이름. 프리팹·컨트롤러 쪽 이름은 여기가 정본이다.
    // 프리팹마다 접두어로 나눈다 (UiNames 와 같은 짜임).
    public static class ViewNames
    {
        // 찹쌀이 프리팹 (Prefabs/Mozzi). Shadow 만 뿌리 자식이고 나머지는 Visual 아래다.
        public const string MozziVisual = "Visual";
        public const string MozziShadow = "Shadow";
        public const string MozziBorder = "Border";
        public const string MozziBody = "Body";
        public const string MozziCheekAnchorL = "CheekAnchorL";
        public const string MozziCheekAnchorR = "CheekAnchorR";
        public const string MozziCheekOffsetL = "CheekOffsetL";
        public const string MozziCheekOffsetR = "CheekOffsetR";
        public const string MozziCheek = "Cheek";
        public const string MozziFaceAnchor = "FaceAnchor";
        public const string MozziFaceOffset = "FaceOffset";
        public const string MozziFace = "Face";
        public const string MozziAccAnchor = "AccAnchor";
        public const string MozziAccOffset = "AccOffset";
        public const string MozziAcc = "Acc";

        // Mozzi.controller 의 상태 이름. 파라미터·전이는 없고 코드가 Play 로 직접 고른다.
        public const string AnimIdle = "Idle";
        public const string AnimHop = "Hop";
    }
}

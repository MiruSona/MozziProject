using UnityEngine;

namespace Mozzi.Game.View
{
    // 찹쌀이 레이어 그림의 스프라이트 키. 앞자리 "Art/" 는 스프라이트 원천이 붙여 준다.
    // 몸(표의 spriteKey)과 악세사리(표의 accessoryId)는 종마다 고르고, 볼·표정은 전 종 공용이다.
    public static class MozziArtKeys
    {
        public const int BodyHopCount = 4;
        public const int FaceCount = 10;

        // 평소 표정. 탭하면 다른 표정이 잠깐 나온다.
        public const int BaseFaceIndex = 1;

        // 들려 있는 동안 고정하는 놀란 표정 (동그란 눈 둘 + 동그란 입, face_05).
        public const int DragFaceIndex = 5;

        // 악세사리 id. 어느 종이 무엇을 쓰나는 표(MozziSpecies.AccessoryId)가 정한다. none 은 투명 그림이다.
        public const string CrownId = "crown";
        public const string NoneId = "none";
        public const string Cheek = Folder + "cheek";

        private const string Folder = "Mozzi/";

        // baseKey 는 표의 spriteKey (예 "Mozzi/body") 다. 뒤에 _hop0 ~ _hop3 이 붙는다.
        public static string BodyHop(string baseKey, int index)
        {
            return baseKey + "_hop" + index;
        }

        // index 는 1 ~ FaceCount. 파일은 face_01 처럼 두 자리다.
        public static string Face(int index)
        {
            return Folder + "face_" + index.ToString("00");
        }

        public static string Accessory(string id)
        {
            return Folder + "acc_" + id;
        }

        // 지금 표정(except)을 뺀 나머지에서 하나 고른다. 같은 표정이 다시 나오면 바뀐 티가 안 난다.
        public static int RandomFaceIndex(int except)
        {
            if (FaceCount <= 1)
            {
                return BaseFaceIndex;
            }

            int pick = Random.Range(1, FaceCount);
            if (pick >= except)
            {
                pick++;
            }

            return pick;
        }
    }
}

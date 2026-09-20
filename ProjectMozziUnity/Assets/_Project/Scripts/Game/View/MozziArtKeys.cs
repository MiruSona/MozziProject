using UnityEngine;

namespace Mozzi.Game.View
{
    // 찹쌀이 레이어 그림의 스프라이트 키. 앞자리 "Art/" 는 스프라이트 원천이 붙여 준다.
    // 몸만 종마다 다르고 (표의 spriteKey 가 기준 키) 볼·표정·악세사리는 전 종 공용이다.
    public static class MozziArtKeys
    {
        public const int BodyHopCount = 4;
        public const int FaceCount = 10;

        // 확인용 — 표정 규칙이 기획에서 정해지면 끈다. 꺼지면 모두 face_01 이다.
        public const bool DebugRandomFace = true;

        public const string CrownId = "crown";
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

        // 같은 마리는 늘 같은 표정이 나오게 id 로 고른다.
        public static int FaceIndexOf(int mozziId)
        {
            if (DebugRandomFace == false)
            {
                return 1;
            }

            return Mathf.Abs(mozziId) % FaceCount + 1;
        }
    }
}

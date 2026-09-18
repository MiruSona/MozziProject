using System;

namespace Mozzi.Save
{
    // 세이브 파일의 한 칸. key 는 ISavable.SaveKey, json 은 그 CaptureJson 결과다.
    [Serializable]
    public sealed class SaveEntry
    {
        public string key;
        public string json;
    }
}

using System;
using System.Collections.Generic;

namespace Mozzi.Save
{
    // save.json 전체. savedAtUtcTicks 로 안 켠 시간을 센다.
    // version 은 파일 꼴의 판 번호다. 꼴을 바꾸면 CurrentVersion 을 올리고 SaveMigrator 에 단계를 하나 더한다.
    [Serializable]
    public sealed class SaveFile
    {
        public const int CurrentVersion = 2;

        // version 글자가 아예 없는 옛 파일은 이 값으로 읽힌다 (JsonUtility 는 없는 필드를 초기값으로 둔다).
        public const int OldestVersion = 1;

        public int version = OldestVersion;
        public long savedAtUtcTicks;
        public List<SaveEntry> entries = new List<SaveEntry>();
    }
}

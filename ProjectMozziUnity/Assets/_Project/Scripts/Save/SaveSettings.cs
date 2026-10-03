using System;

namespace Mozzi.Save
{
    // 세이브 파일의 설정 구역. 게임 진행(entries)과 따로 둔다. 볼륨은 0 ~ 1 이다.
    [Serializable]
    public sealed class SaveSettings
    {
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
    }
}

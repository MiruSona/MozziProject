namespace Mozzi.Save
{
    // Manager 가 자기 ISavable 을 등록하고, 화면이 즉시 저장을 청하는 창구.
    public interface ISaveHub
    {
        // 세이브가 있었으면 그때의 UTC ticks, 새 게임이면 0. ClockManager 가 안 켠 초를 셀 때 쓴다.
        long LoadedAtUtcTicks { get; }

        bool HasSaveFile { get; }

        // 설정 구역 (볼륨). 받은 객체의 값을 고치면 다음 저장에 실린다.
        SaveSettings Settings { get; }

        void Register(ISavable savable);

        void SaveNow();

        // 디버그용. 세이브 파일을 지우고 씬을 다시 연다. 그 뒤로는 저장하지 않는다 (지운 파일이 되살아나지 않게).
        void DeleteAndRestart();
    }
}

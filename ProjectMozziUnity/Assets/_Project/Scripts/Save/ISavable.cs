namespace Mozzi.Save
{
    // 세이브에 한 칸 갖는 것. SaveKey 로 칸을 찾고 JSON 문자열로 넣고 꺼낸다.
    // Dispose 에서 상태를 비우지 않는다 — SaveManager 가 뒤에서 Capture 한다 (구독 해제 · Object 반납만).
    public interface ISavable
    {
        string SaveKey { get; }

        string CaptureJson();

        void RestoreJson(string json);
    }
}

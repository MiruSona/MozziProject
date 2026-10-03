namespace Mozzi.Game.Ui
{
    // 합성 기계 그림이 지금 보여 주는 것.
    public enum SynthesisMachineMode
    {
        None,
        // 빈 칸 · 잠긴 칸. 고른 재료만 투입구에 얹힌다.
        Idle,
        // 시작 연출 (빨려 들어감 → 음양 합쳐짐 → 번쩍).
        Merging,
        // 대기 중. 창 안에서 음양이 천천히 돈다.
        Running,
        // 다 됨. 배출구 앞에 결과 그림자가 놓인다.
        Done,
    }
}

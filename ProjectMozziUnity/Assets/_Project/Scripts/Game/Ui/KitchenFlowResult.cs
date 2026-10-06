namespace Mozzi.Game.Ui
{
    // 도구 한 번 누름(또는 찌기 시간 재기)의 판정. Steamed · Make · QuickMake 면 화면이 그때 StallManager.Make 를 부른다.
    public enum KitchenFlowResult
    {
        // 순서가 아닌 도구 · 너무 빠른 탭 · 찌는 중 누름. 아무 일도 없다.
        Ignored,
        // 맷돌 한 번을 셌다 (아직 남았다).
        Ground,
        // 마지막 맷돌 → 치기.
        GrindDone,
        // 떡메 한 번을 셌다 (아직 남았다).
        Pounded,
        // 마지막 떡메 → 빚기.
        PoundDone,
        // 빚기판을 눌러 빚었다 → 찌기 시작 (기다리기).
        Shaped,
        // 찌기 시간이 다 됐다 → 만든다 (뚜껑이 열린다).
        Steamed,
        // 두 번 빠르게 눌러 남은 과정을 건너뛰었다 → 바로 만든다.
        Make,
        // 해 본 조합 · 찾은 조합이라 첫 누름에서 과정을 건너뛰었다 → 바로 만든다.
        QuickMake
    }
}

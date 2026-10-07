namespace Mozzi.Game.Ui
{
    // 도구 한 번 누름(또는 찌기 시간 재기)의 판정. Steamed · QuickMake 면 화면이 그때 StallManager.Make 를 부른다.
    public enum KitchenFlowResult
    {
        // 순서가 아닌 도구 · 너무 빠른 탭 · 찌는 중 누름. 아무 일도 없다.
        Ignored,
        // 맷돌 한 번을 셌다 (마지막 타면 「한 번 더」 를 기다린다).
        Ground,
        // 「한 번 더」 누름 → 치기.
        GrindDone,
        // 떡메 한 번을 셌다 (마지막 타면 「한 번 더」 를 기다린다).
        Pounded,
        // 「한 번 더」 누름 → 빚기.
        PoundDone,
        // 빚기판 한 번을 셌다 (마지막이면 「한 번 더」 를 기다린다).
        ShapeTapped,
        // 「한 번 더」 누름 → 찌기 시작 (기다리기).
        Shaped,
        // 찌기 시간이 다 됐다 → 만든다 (뚜껑이 열린다).
        Steamed,
        // 해 본 조합 · 찾은 조합이라 첫 누름에서 과정을 건너뛰었다 → 바로 만든다.
        QuickMake
    }
}

namespace Mozzi.Game.Ui
{
    // 도구 한 번 누름의 판정. Make · QuickMake 면 화면이 그때 StallManager.Make 를 부른다.
    public enum KitchenFlowResult
    {
        // 순서가 아닌 도구 · 너무 빠른 탭. 아무 일도 없다.
        Ignored,
        // 찜기를 눌러 찌기가 끝났다 → 치기.
        Steamed,
        // 떡메 한 번을 셌다 (아직 남았다).
        Pounded,
        // 마지막 떡메 → 빚기.
        PoundDone,
        // 빚기를 마쳤거나 두 번 빠르게 눌러 남은 과정을 건너뛰었다 → 만든다.
        Make,
        // 해 본 조합 · 찾은 조합이라 과정을 건너뛰었다 → 절구 애니 한 번 뒤 만든다.
        QuickMake
    }
}

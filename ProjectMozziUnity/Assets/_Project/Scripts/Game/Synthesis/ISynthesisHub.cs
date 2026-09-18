namespace Mozzi.Game.Synthesis
{
    // 합성소 창구. 칸 1~4 · 영구 단축 0~5 · 시작 · 받기 · 미리보기.
    public interface ISynthesisHub
    {
        int SlotCount { get; }

        int MaxSlotCount { get; }

        int ShortcutLevel { get; }

        int MaxShortcutLevel { get; }

        // 지금 단축의 대기 배율 (0단계 1.0).
        float ShortcutRate { get; }

        // 다 샀으면 -1.
        int NextSlotPrice { get; }

        int NextShortcutPrice { get; }

        float NextShortcutRate { get; }

        // 이번 켜기(또는 마지막 Skip)의 오프라인 진행으로 끝난 칸 수. 돌아왔어요 팝업이 읽는다.
        int OfflineDoneCount { get; }

        SynthesisSlot GetSlot(int slotIndex);

        // 디버그용. 안 켠 것처럼 seconds 만큼 한 번에 진행하고, 끝난 칸은 SynthesisDoneEvent 를 낸다.
        void Skip(double seconds);

        // 재료 둘의 비용·확률·후보. 종 id 가 표에 없으면 던진다.
        SynthesisPreview Preview(string speciesA, string speciesB);

        // 두 마리를 보관함에서 빼고 골드를 낸다. 칸이 비어 있지 않거나 모자라면 false 와 아무 변화 없음.
        bool Start(int slotIndex, string speciesA, string speciesB);

        // Done 인 칸의 결과를 보관함에 넣고 도감을 연다. 결과 종 id 를 주고 아니면 null.
        string Take(int slotIndex);

        bool BuySlot();

        bool BuyShortcut();
    }
}

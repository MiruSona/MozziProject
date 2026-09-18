namespace Mozzi.Game.Synthesis
{
    // 합성 칸 하나의 상태. 세이브에 int 로 들어간다.
    public enum SynthesisSlotStatus
    {
        Locked = 0,
        Empty = 1,
        Running = 2,
        Done = 3
    }
}

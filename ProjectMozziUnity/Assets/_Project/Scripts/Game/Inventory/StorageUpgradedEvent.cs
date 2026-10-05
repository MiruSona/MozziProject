namespace Mozzi.Game.Inventory
{
    // 창고 넓히기 한 단계를 샀다. 모든 재료의 상한이 같이 바뀌므로 자원바를 통째로 되읽는다.
    public readonly struct StorageUpgradedEvent
    {
        public readonly int Level;

        public StorageUpgradedEvent(int level)
        {
            Level = level;
        }
    }
}

using System;

namespace Mozzi.Save
{
    // 「id → 개수」 한 쌍. JsonUtility 는 Dictionary 를 못 담으니 List<CountEntry> 로 저장한다.
    [Serializable]
    public sealed class CountEntry
    {
        public string id;
        public int count;

        public CountEntry()
        {
        }

        public CountEntry(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }
}

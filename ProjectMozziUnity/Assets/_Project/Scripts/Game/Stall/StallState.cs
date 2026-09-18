using System;
using System.Collections.Generic;

namespace Mozzi.Game.Stall
{
    // StallManager 의 세이브 칸. 시계 둘은 초 단위 남은 시간이다.
    [Serializable]
    public sealed class StallState
    {
        public List<ShelfItem> shelf = new List<ShelfItem>();
        public List<StallCustomer> queue = new List<StallCustomer>();
        public bool isOpen;
        public bool hasClerk;
        public int arrivedToday;
        public int servedToday;
        public double nextCustomerSec;
        public double clerkSec;
    }
}

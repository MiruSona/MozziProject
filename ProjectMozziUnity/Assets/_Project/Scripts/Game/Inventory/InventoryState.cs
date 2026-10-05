using System;
using System.Collections.Generic;
using Mozzi.Save;

namespace Mozzi.Game.Inventory
{
    // InventoryManager 의 세이브 칸. id 는 Ingredient 표의 id 다.
    [Serializable]
    public sealed class InventoryState
    {
        public List<CountEntry> counts = new List<CountEntry>();
        // 창고 넓히기 단계. 0 이면 Ingredient 표 cap 그대로이고 Growth 표 storage 행만큼 늘어난다.
        public int storageLevel = 0;
    }
}

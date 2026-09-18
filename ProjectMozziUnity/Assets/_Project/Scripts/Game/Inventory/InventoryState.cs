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
    }
}

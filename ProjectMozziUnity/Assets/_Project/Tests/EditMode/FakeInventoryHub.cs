using System.Collections.Generic;
using Mozzi.Game.Inventory;

namespace Mozzi.Tests
{
    // 시험용 창고. 모든 재료가 같은 상한 Room 을 쓴다.
    public sealed class FakeInventoryHub : IInventoryHub
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public int Room = 100;

        public IReadOnlyList<string> IngredientIds
        {
            get { return new List<string>(_counts.Keys); }
        }

        public int StorageLevel
        {
            get { return 0; }
        }

        public int MaxStorageLevel
        {
            get { return 0; }
        }

        public int NextStoragePrice
        {
            get { return -1; }
        }

        public int StorageBonus
        {
            get { return 0; }
        }

        public bool BuyStorage()
        {
            return false;
        }

        public int Count(string ingredientId)
        {
            int count;
            _counts.TryGetValue(ingredientId, out count);
            return count;
        }

        public int Cap(string ingredientId)
        {
            return Room;
        }

        public bool IsFull(string ingredientId)
        {
            return Count(ingredientId) >= Room;
        }

        public int Add(string ingredientId, int amount)
        {
            int moved = System.Math.Min(amount, Room - Count(ingredientId));
            if (moved <= 0)
            {
                return 0;
            }

            _counts[ingredientId] = Count(ingredientId) + moved;
            return moved;
        }

        public bool Has(string ingredientId, int amount)
        {
            return Count(ingredientId) >= amount;
        }

        // 시험용이라 개당 1 골드로 센다.
        public int SellAll()
        {
            int gold = SellAllPrice();
            _counts.Clear();
            return gold;
        }

        public int SellAllPrice()
        {
            int gold = 0;
            foreach (KeyValuePair<string, int> pair in _counts)
            {
                gold += pair.Value;
            }

            return gold;
        }

        public bool Take(string ingredientId, int amount)
        {
            if (Has(ingredientId, amount) == false)
            {
                return false;
            }

            _counts[ingredientId] = Count(ingredientId) - amount;
            return true;
        }
    }
}

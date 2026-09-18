using System;

namespace Mozzi.Game.Stall
{
    // 진열대의 떡 하나. recipeId 가 비면 평범한 떡이다. 평범한 떡은 조합마다 값이 달라 하나씩 값을 든다.
    [Serializable]
    public sealed class ShelfItem
    {
        public string recipeId = "";
        public int price;

        public ShelfItem()
        {
        }

        public ShelfItem(string recipeId, int price)
        {
            this.recipeId = recipeId;
            this.price = price;
        }

        public bool IsPlain
        {
            get { return string.IsNullOrEmpty(recipeId); }
        }
    }
}

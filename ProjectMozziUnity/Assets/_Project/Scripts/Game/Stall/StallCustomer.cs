using System;

namespace Mozzi.Game.Stall
{
    // 줄에 선 손님 한 명. orderRecipeId 가 비면 아무 떡이나 산다. isHint 면 못 찾은 레시피를 주문한 힌트 손님이다.
    [Serializable]
    public sealed class StallCustomer
    {
        public string orderRecipeId = "";
        public bool isHint;

        public StallCustomer()
        {
        }

        public StallCustomer(string orderRecipeId, bool isHint)
        {
            this.orderRecipeId = orderRecipeId;
            this.isHint = isHint;
        }

        public bool HasOrder
        {
            get { return string.IsNullOrEmpty(orderRecipeId) == false; }
        }
    }
}

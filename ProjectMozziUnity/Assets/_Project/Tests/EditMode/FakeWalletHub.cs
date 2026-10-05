using Mozzi.Game.Wallet;

namespace Mozzi.Tests
{
    // 시험용 지갑. Money 를 넣어 두면 그만큼 낼 수 있다 (기본 0 — 줍기 시험은 안 쓴다).
    public sealed class FakeWalletHub : IWalletHub
    {
        public int Money;

        public int Gold
        {
            get { return Money; }
        }

        public bool CanPay(int amount)
        {
            return Money >= amount;
        }

        public bool Pay(int amount)
        {
            if (Money < amount)
            {
                return false;
            }

            Money -= amount;
            return true;
        }

        public void Earn(int amount)
        {
            Money += amount;
        }
    }
}

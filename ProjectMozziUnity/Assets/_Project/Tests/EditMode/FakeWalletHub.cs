using Mozzi.Game.Wallet;

namespace Mozzi.Tests
{
    // 시험용 지갑. 줍기 시험에는 안 쓰여 늘 0 이다.
    public sealed class FakeWalletHub : IWalletHub
    {
        public int Gold
        {
            get { return 0; }
        }

        public bool CanPay(int amount)
        {
            return false;
        }

        public bool Pay(int amount)
        {
            return false;
        }

        public void Earn(int amount)
        {
        }
    }
}

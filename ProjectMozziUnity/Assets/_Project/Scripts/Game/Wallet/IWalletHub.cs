namespace Mozzi.Game.Wallet
{
    // 골드 창구. Pay 는 모자라면 false 를 주고 아무것도 안 뺀다.
    public interface IWalletHub
    {
        int Gold { get; }

        bool CanPay(int amount);

        bool Pay(int amount);

        void Earn(int amount);
    }
}

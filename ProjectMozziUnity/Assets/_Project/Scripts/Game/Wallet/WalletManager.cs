using System;
using Mozzi.Core;
using Mozzi.Events;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Wallet
{
    // 골드 하나만 맡는다. 시작 골드 0.
    public sealed class WalletManager : IManager, ISavable, IWalletHub
    {
        private const string Key = "Wallet";

        private WalletState _state = new WalletState();
        private ISaveHub _save;

        public string SaveKey
        {
            get { return Key; }
        }

        public int Gold
        {
            get { return _state.gold; }
        }

        public void Init()
        {
        }

        public void PostInit()
        {
            _save = InGameHub.Get<ISaveHub>();
            _save.Register(this);
            Debug.Log("[Wallet] 골드 " + _state.gold);
        }

        public void Dispose()
        {
        }

        public bool CanPay(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return _state.gold >= amount;
        }

        public bool Pay(int amount)
        {
            if (CanPay(amount) == false)
            {
                return false;
            }

            _state.gold -= amount;
            EventBus.Publish(new GoldChangedEvent(_state.gold, -amount));
            return true;
        }

        public void Earn(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount == 0)
            {
                return;
            }

            _state.gold += amount;
            EventBus.Publish(new GoldChangedEvent(_state.gold, amount));
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(_state);
        }

        public void RestoreJson(string json)
        {
            WalletState loaded = JsonUtility.FromJson<WalletState>(json);
            if (loaded == null)
            {
                return;
            }

            _state = loaded;
        }
    }
}

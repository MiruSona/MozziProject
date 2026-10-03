using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Game.Clock;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Codex;
using Mozzi.Game.Stall;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Ui;
using Mozzi.Game.Vfx;
using Mozzi.Game.View;
using Mozzi.Game.Wallet;
using Mozzi.Save;

namespace Mozzi.Game
{
    // Farm 씬의 Master. Manager 를 만들고 InGameHub 에 등록·해제한다.
    public sealed class InGameMaster : MasterBase
    {
        private StaticDataManager _staticData;
        private SaveManager _save;
        private ClockManager _clock;
        private WalletManager _wallet;
        private InventoryManager _inventory;
        private FarmManager _farm;
        private StorageManager _storage;
        private CodexManager _codex;
        private SynthesisManager _synthesis;
        private StallManager _stall;
        private FarmViewManager _farmView;
        private VfxManager _vfx;
        private UiManager _ui;

        // 넣는 순서가 곧 Init·Process 순서다 (세로조각1판설계 4-3 표).
        // FarmViewManager 는 규칙 Manager 들 뒤 · UiManager 앞이다 (월드 쪽 화면).
        // VfxManager 는 UiManager 바로 앞 — 화면이 PostInit 에서 꺼내 쓰고, 화면보다 먼저 한 프레임을 굴린다.
        protected override void CreateManagers(List<IManager> into)
        {
            _staticData = new StaticDataManager();
            _save = new SaveManager();
            _clock = new ClockManager();
            _wallet = new WalletManager();
            _inventory = new InventoryManager();
            _farm = new FarmManager();
            _storage = new StorageManager();
            _codex = new CodexManager();
            _synthesis = new SynthesisManager();
            _stall = new StallManager();
            _farmView = new FarmViewManager();
            _vfx = new VfxManager();
            _ui = new UiManager();

            into.Add(_staticData);
            into.Add(_save);
            into.Add(_clock);
            into.Add(_wallet);
            into.Add(_inventory);
            into.Add(_farm);
            into.Add(_storage);
            into.Add(_codex);
            into.Add(_synthesis);
            into.Add(_stall);
            into.Add(_farmView);
            into.Add(_vfx);
            into.Add(_ui);
        }

        // Manager 가 I~Hub 를 구현하면 InGameHub.Register<I~Hub>(manager, this) 를 여기 적는다.
        protected override void RegisterHubs()
        {
            InGameHub.Register<IStaticDataHub>(_staticData, this);
            InGameHub.Register<ISaveHub>(_save, this);
            InGameHub.Register<IClockHub>(_clock, this);
            InGameHub.Register<IWalletHub>(_wallet, this);
            InGameHub.Register<IInventoryHub>(_inventory, this);
            InGameHub.Register<IFarmHub>(_farm, this);
            InGameHub.Register<IStorageHub>(_storage, this);
            InGameHub.Register<ICodexHub>(_codex, this);
            InGameHub.Register<ISynthesisHub>(_synthesis, this);
            InGameHub.Register<IStallHub>(_stall, this);
            InGameHub.Register<IFarmViewHub>(_farmView, this);
            InGameHub.Register<IVfxHub>(_vfx, this);
            InGameHub.Register<IUiHub>(_ui, this);
        }

        protected override void UnregisterHubs()
        {
            InGameHub.UnregisterAllBy(this);
        }
    }
}

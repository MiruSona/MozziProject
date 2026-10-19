using Mozzi.Data;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Storage;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using NUnit.Framework;

namespace Mozzi.Tests
{
    // 농장에서 모자란 마리를 끌어와 합성 시작하기. 세이브 없이 켜서 농장에는 시작 종 2마리가 있다 (Farm 표 startCount).
    // 농장 · 보관함 · 도감 · 합성소는 진짜를 쓰고 표는 구운 것을 그대로 쓴다.
    public sealed class SynthesisStartFromFarmTests
    {
        private FakeWalletHub _wallet;
        private FarmManager _farm;
        private StorageManager _storage;
        private SynthesisManager _synthesis;
        private string _species;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();
            _wallet = new FakeWalletHub();

            StaticDataManager data = new StaticDataManager();
            data.Init();
            _farm = new FarmManager();
            _storage = new StorageManager();
            CodexManager codex = new CodexManager();
            _synthesis = new SynthesisManager();

            InGameHub.Register<IStaticDataHub>(data, this);
            InGameHub.Register<ISaveHub>(new FakeSaveHub(), this);
            InGameHub.Register<IClockHub>(new FakeClockHub(), this);
            InGameHub.Register<IWalletHub>(_wallet, this);
            InGameHub.Register<IInventoryHub>(new FakeInventoryHub(), this);
            InGameHub.Register<IFarmHub>(_farm, this);
            InGameHub.Register<IStorageHub>(_storage, this);
            InGameHub.Register<ICodexHub>(codex, this);

            _farm.Init();
            _farm.PostInit();
            _storage.Init();
            _storage.PostInit();
            codex.Init();
            codex.PostInit();
            _synthesis.Init();
            _synthesis.PostInit();

            _species = data.Farms.Get(DataKeys.DefaultRowId).StartSpeciesId;
            _storage.Add(_species, 1);
        }

        [TearDown]
        public void TearDown()
        {
            InGameHub.Clear();
        }

        // 같은 종 둘 : 보관함 1 + 농장 1 을 써서 칸이 돈다.
        [Test]
        public void SameSpecies_PullsOneFromFarm()
        {
            int farmBefore = _farm.CountOf(_species);
            _wallet.Money = _synthesis.Preview(_species, _species).Gold;

            Assert.That(_synthesis.StartPullingFromFarm(0, _species, _species), Is.True);
            Assert.That(_synthesis.GetSlot(0).status, Is.EqualTo(SynthesisSlotStatus.Running));
            Assert.That(_storage.Count(_species), Is.EqualTo(0));
            Assert.That(_farm.CountOf(_species), Is.EqualTo(farmBefore - 1));
            Assert.That(_wallet.Money, Is.EqualTo(0));
        }

        [Test]
        public void NotEnoughGold_LeavesMozzisWhereTheyWere()
        {
            int farmBefore = _farm.CountOf(_species);
            _wallet.Money = _synthesis.Preview(_species, _species).Gold - 1;

            Assert.That(_synthesis.StartPullingFromFarm(0, _species, _species), Is.False);
            Assert.That(_synthesis.GetSlot(0).status, Is.EqualTo(SynthesisSlotStatus.Empty));
            Assert.That(_storage.Count(_species), Is.EqualTo(1));
            Assert.That(_farm.CountOf(_species), Is.EqualTo(farmBefore));
        }
    }
}

using Mozzi.Data;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 진열대 비우기 검사. 진열된 떡을 정가 × clearRate(Stall 표 0.3)에 다 팔고 진열대를 비운다.
    // 표는 구운 것을 그대로 쓰고 진열대는 세이브 복원으로 채운다. 도감은 비우기에서 안 쓰여 그대로 끼운다.
    public sealed class StallClearShelfTests
    {
        private FakeWalletHub _wallet;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();
            _wallet = new FakeWalletHub();
        }

        [TearDown]
        public void TearDown()
        {
            InGameHub.Clear();
        }

        [Test]
        public void ClearShelf_SellsAtRateAndEmpties()
        {
            StallState state = new StallState();
            state.shelf.Add(new ShelfItem("redbean_cake", 15));
            state.shelf.Add(new ShelfItem("redbean_cake", 15));
            state.shelf.Add(new ShelfItem("", 100));
            StallManager stall = Build(state);

            // 15 × 0.3 = 4.5 → 5 (반올림 올림) 둘 + 100 × 0.3 = 30.
            Assert.That(stall.ClearShelfPrice(), Is.EqualTo(40));
            Assert.That(stall.ClearShelf(), Is.EqualTo(40));
            Assert.That(_wallet.Money, Is.EqualTo(40));
            Assert.That(stall.Shelf.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearShelf_Empty_PaysNothing()
        {
            StallManager stall = Build(new StallState());

            Assert.That(stall.ClearShelf(), Is.EqualTo(0));
            Assert.That(_wallet.Money, Is.EqualTo(0));
        }

        private StallManager Build(StallState state)
        {
            StaticDataManager data = new StaticDataManager();
            data.Init();
            FakeSaveHub save = new FakeSaveHub();
            save.Json = JsonUtility.ToJson(state);

            InGameHub.Register<IStaticDataHub>(data, this);
            InGameHub.Register<ISaveHub>(save, this);
            InGameHub.Register<IClockHub>(new FakeClockHub(), this);
            InGameHub.Register<IWalletHub>(_wallet, this);
            InGameHub.Register<IInventoryHub>(new FakeInventoryHub(), this);
            InGameHub.Register<ICodexHub>(new CodexManager(), this);

            StallManager stall = new StallManager();
            stall.Init();
            stall.PostInit();
            return stall;
        }
    }
}

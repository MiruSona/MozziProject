using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Events;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.View;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 농장 장치(수집기 · 먹이 공급기) 배선 검사. 수집기가 걷을 때만 CollectorSweptEvent 가 나는지(오프라인은 안 남),
    // 장치가 산 뒤에만 보이는지, 장치 자리에서 빠지는 울타리 칸이 맞는지 본다. 표는 구운 것 그대로 (수집기 30초 · 1단계 5개).
    public sealed class FarmDeviceTests
    {
        private const string Ingredient = "redbean";

        private StaticDataManager _data;
        private FakeInventoryHub _inventory;
        private readonly List<CollectorSweptEvent> _swept = new List<CollectorSweptEvent>();
        private EventSubscription _sub;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();
            _data = new StaticDataManager();
            _data.Init();
            _inventory = new FakeInventoryHub();
            _swept.Clear();
            _sub = EventBus.Subscribe<CollectorSweptEvent>(this, evt => _swept.Add(evt));
        }

        [TearDown]
        public void TearDown()
        {
            _sub.Dispose();
            InGameHub.Clear();
        }

        [Test]
        public void Collector_Process_PublishesSweptOnPeriod()
        {
            FarmManager farm = Build(1, 12);

            farm.Process(29f);
            Assert.That(_swept.Count, Is.EqualTo(0));

            farm.Process(1.5f);
            Assert.That(_swept.Count, Is.EqualTo(1));
            Assert.That(_swept[0].IngredientId, Is.EqualTo(Ingredient));
            Assert.That(_swept[0].Moved, Is.EqualTo(5));
            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(7));
        }

        [Test]
        public void Collector_Offline_DoesNotPublishSwept()
        {
            FarmManager farm = Build(1, 12);

            farm.Skip(31);

            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(7));
            Assert.That(_swept.Count, Is.EqualTo(0));
        }

        [Test]
        public void Collector_NotBought_NoSwept()
        {
            FarmManager farm = Build(0, 12);

            farm.Process(31f);

            Assert.That(_swept.Count, Is.EqualTo(0));
            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(12));
        }

        [Test]
        public void Device_ShownOnlyAfterBuying()
        {
            Assert.That(FarmDeviceLayout.IsBought(0), Is.False);
            Assert.That(FarmDeviceLayout.IsBought(1), Is.True);
            Assert.That(FarmDeviceLayout.IsBought(5), Is.True);
        }

        [Test]
        public void FenceRows_CollectorTakesThreeFeederFour()
        {
            int from;
            int to;
            Assert.That(FarmDeviceLayout.FenceRowsCovered(FarmDeviceLayout.CollectorBottomPx, FarmDeviceLayout.CollectorHeightPx, out from, out to), Is.True);
            Assert.That(from, Is.EqualTo(11));
            Assert.That(to, Is.EqualTo(13));

            Assert.That(FarmDeviceLayout.FenceRowsCovered(FarmDeviceLayout.FeederBottomPx, FarmDeviceLayout.FeederHeightPx, out from, out to), Is.True);
            Assert.That(from, Is.EqualTo(10));
            Assert.That(to, Is.EqualTo(13));
        }

        [Test]
        public void Devices_StayInsideFenceLines()
        {
            float bottomRailTop = LayoutConst.FarmBottomPx - LayoutConst.FarmCellPx;
            Assert.That(FarmDeviceLayout.CollectorBottomPx, Is.LessThan(bottomRailTop));
            Assert.That(FarmDeviceLayout.FeederBottomPx, Is.LessThan(bottomRailTop));
            Assert.That(FarmDeviceLayout.CollectorBottomPx - FarmDeviceLayout.CollectorHeightPx, Is.GreaterThan(LayoutConst.FarmTopPx));
            Assert.That(FarmDeviceLayout.FeederBottomPx - FarmDeviceLayout.FeederHeightPx, Is.GreaterThan(LayoutConst.FarmTopPx));
        }

        [Test]
        public void FenceRows_NoOverlap_ReturnsFalse()
        {
            int from;
            int to;
            Assert.That(FarmDeviceLayout.FenceRowsCovered(LayoutConst.FarmTopPx, 10f, out from, out to), Is.False);
        }

        // 마리 없이 redbean 더미 pile 만 있는 세이브. 수집기 단계 collectorLevel.
        private FarmManager Build(int collectorLevel, int pile)
        {
            FarmState state = new FarmState();
            state.nextMozziId = 1;
            state.collectorLevel = collectorLevel;
            state.piles.Add(new CountEntry(Ingredient, pile));

            FakeSaveHub save = new FakeSaveHub();
            save.Json = JsonUtility.ToJson(state);
            InGameHub.Register<IStaticDataHub>(_data, this);
            InGameHub.Register<ISaveHub>(save, this);
            InGameHub.Register<IClockHub>(new FakeClockHub(), this);
            InGameHub.Register<IWalletHub>(new FakeWalletHub(), this);
            InGameHub.Register<IInventoryHub>(_inventory, this);

            FarmManager farm = new FarmManager();
            farm.Init();
            farm.PostInit();
            return farm;
        }
    }
}

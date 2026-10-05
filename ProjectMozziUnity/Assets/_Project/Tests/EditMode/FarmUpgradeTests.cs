using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 바닥 한도 · 농장 강화(바닥 넓히기 · 수집기 · 먹이 공급기) · 창고 넓히기 검사.
    // 표는 구운 것을 그대로 쓴다 (바닥 한도 50 · 수집기 1단계 60초 · 5단계 10초 · 공급기 5단계 +50% · 창고 2단계 +100).
    // 시간은 Skip(오프라인 진행)으로 흘린다. grass1 은 redbean 을 떨군다.
    public sealed class FarmUpgradeTests
    {
        private const string Species = "grass1";
        private const string Ingredient = "redbean";
        private const string OtherIngredient = "bean";

        private StaticDataManager _data;
        private FakeInventoryHub _inventory;
        private FakeWalletHub _wallet;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();
            _data = new StaticDataManager();
            _data.Init();
            _inventory = new FakeInventoryHub();
            _wallet = new FakeWalletHub();
        }

        [TearDown]
        public void TearDown()
        {
            InGameHub.Clear();
        }

        [Test]
        public void FloorFull_StopsProduction()
        {
            FarmManager farm = Build(State(2, 50));

            farm.Skip(600);

            Assert.That(farm.FloorMax, Is.EqualTo(50));
            Assert.That(farm.FloorTotal, Is.EqualTo(50));
        }

        [Test]
        public void PickUpAfterFull_ProductionResumesUpToCap()
        {
            FarmManager farm = Build(State(2, 50));

            farm.PickUp(Ingredient, 10);
            Assert.That(farm.FloorTotal, Is.EqualTo(40));

            farm.Skip(600);
            Assert.That(farm.FloorTotal, Is.EqualTo(50));
        }

        [Test]
        public void Offline_StopsAtFloorCap()
        {
            FarmManager farm = Build(State(2, 0));

            farm.Skip(24 * 3600);

            Assert.That(farm.FloorTotal, Is.EqualTo(50));
            Assert.That(farm.MozziCount, Is.GreaterThan(2));
        }

        [Test]
        public void FloorUpgrade_RaisesCap()
        {
            FarmState state = State(2, 0);
            state.floorLevel = 3;
            FarmManager farm = Build(state);

            farm.Skip(24 * 3600);

            Assert.That(farm.FloorMax, Is.EqualTo(150));
            Assert.That(farm.FloorTotal, Is.EqualTo(150));
        }

        [Test]
        public void Collector_PicksUpOnPeriod()
        {
            FarmState state = State(0, 10);
            state.collectorLevel = 1;
            FarmManager farm = Build(state);

            farm.Skip(59);
            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(10));

            farm.Skip(1.5);
            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(0));
            Assert.That(_inventory.Count(Ingredient), Is.EqualTo(10));
        }

        [Test]
        public void Collector_FullStorageLeavesRestOnFloor()
        {
            FarmState state = State(0, 10);
            state.collectorLevel = 1;
            _inventory.Room = 4;
            FarmManager farm = Build(state);

            farm.Skip(61);

            Assert.That(_inventory.Count(Ingredient), Is.EqualTo(4));
            Assert.That(farm.PileOf(Ingredient), Is.EqualTo(6));
        }

        [Test]
        public void Collector_RunsOffline_StorageGetsMoreThanFloorCap()
        {
            FarmState state = State(2, 0);
            state.collectorLevel = 5;
            FarmManager farm = Build(state);

            farm.Skip(3600);

            Assert.That(farm.CollectorPeriodSec, Is.EqualTo(10).Within(1e-9));
            Assert.That(_inventory.Count(Ingredient), Is.EqualTo(_inventory.Room));
            Assert.That(farm.FloorTotal, Is.LessThanOrEqualTo(farm.FloorMax));
        }

        [Test]
        public void Feeder_RaisesProductionSpeed()
        {
            FarmManager plain = Build(State(1, 0));
            plain.Skip(150);
            int plainDrops = plain.PileOf(Ingredient);

            InGameHub.Clear();
            FarmState state = State(1, 0);
            state.feederLevel = 5;
            FarmManager fed = Build(state);
            fed.Skip(150);

            Assert.That(fed.FeederBonus, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(plainDrops, Is.EqualTo(10));
            Assert.That(fed.PileOf(Ingredient), Is.EqualTo(15));
        }

        [Test]
        public void ProducePeriod_DividesBySpeed()
        {
            Assert.That(FarmUpgradeMath.ProducePeriod(15, 2, 0), Is.EqualTo(30).Within(1e-9));
            Assert.That(FarmUpgradeMath.ProducePeriod(15, 1, 0.5), Is.EqualTo(10).Within(1e-9));
        }

        [Test]
        public void ClampDrop_CutsAtRoom()
        {
            Assert.That(FarmUpgradeMath.ClampDrop(5, 48, 50), Is.EqualTo(2));
            Assert.That(FarmUpgradeMath.ClampDrop(5, 50, 50), Is.EqualTo(0));
            Assert.That(FarmUpgradeMath.ClampDrop(3, 10, 50), Is.EqualTo(3));
        }

        [Test]
        public void OldSave_PilesOverCap_TrimmedOnLoad()
        {
            FarmState state = State(0, 1500000);
            state.piles.Add(new CountEntry(OtherIngredient, 500000));
            FarmManager farm = Build(state);

            Assert.That(farm.FloorTotal, Is.EqualTo(50));
            Assert.That(farm.PileOf(Ingredient), Is.GreaterThan(farm.PileOf(OtherIngredient)));
            Assert.That(farm.PileOf(OtherIngredient), Is.GreaterThan(0));
        }

        [Test]
        public void TrimToMax_KeepsRatioAndNeverGrows()
        {
            List<int> counts = new List<int> { 30, 30, 40 };
            FarmUpgradeMath.TrimToMax(counts, 50);
            Assert.That(counts, Is.EqualTo(new List<int> { 15, 15, 20 }));

            List<int> odd = new List<int> { 7, 0, 3 };
            FarmUpgradeMath.TrimToMax(odd, 5);
            Assert.That(odd[0] + odd[1] + odd[2], Is.EqualTo(5));
            Assert.That(odd[1], Is.EqualTo(0));
            Assert.That(odd[0], Is.LessThanOrEqualTo(7));

            List<int> under = new List<int> { 3, 4 };
            FarmUpgradeMath.TrimToMax(under, 50);
            Assert.That(under, Is.EqualTo(new List<int> { 3, 4 }));
        }

        [Test]
        public void BuyUpgrade_PaysAndRaises_StopsAtMax()
        {
            _wallet.Money = 100000;
            FarmManager farm = Build(State(0, 0));

            Assert.That(farm.BuyUpgrade(GrowthUtil.KindFloor), Is.True);
            Assert.That(farm.FloorMax, Is.EqualTo(75));
            Assert.That(_wallet.Money, Is.EqualTo(100000 - 300));

            Assert.That(farm.BuyUpgrade(GrowthUtil.KindFloor), Is.True);
            Assert.That(farm.BuyUpgrade(GrowthUtil.KindFloor), Is.True);
            Assert.That(farm.NextUpgradePrice(GrowthUtil.KindFloor), Is.EqualTo(-1));
            Assert.That(farm.BuyUpgrade(GrowthUtil.KindFloor), Is.False);
            Assert.That(farm.BuyUpgrade(GrowthUtil.KindSlot), Is.False);
        }

        [Test]
        public void SaveRoundTrip_KeepsUpgradeLevels()
        {
            FarmState state = State(0, 0);
            state.floorLevel = 2;
            state.collectorLevel = 3;
            state.feederLevel = 99;
            FarmManager farm = Build(state);

            FarmState saved = JsonUtility.FromJson<FarmState>(farm.CaptureJson());

            Assert.That(saved.floorLevel, Is.EqualTo(2));
            Assert.That(saved.collectorLevel, Is.EqualTo(3));
            Assert.That(saved.feederLevel, Is.EqualTo(5));
        }

        [Test]
        public void Storage_LevelAddsToEveryCap()
        {
            InventoryState state = new InventoryState();
            state.storageLevel = 2;
            InventoryManager inventory = BuildInventory(JsonUtility.ToJson(state));

            for (int i = 0; i < inventory.IngredientIds.Count; i++)
            {
                Assert.That(inventory.Cap(inventory.IngredientIds[i]), Is.EqualTo(200));
            }
        }

        [Test]
        public void Storage_BuyRaisesCapAndStopsAtMax()
        {
            _wallet.Money = 100000;
            InventoryManager inventory = BuildInventory(null);
            Assert.That(inventory.Cap(Ingredient), Is.EqualTo(100));

            Assert.That(inventory.BuyStorage(), Is.True);
            Assert.That(inventory.Cap(Ingredient), Is.EqualTo(150));
            Assert.That(inventory.BuyStorage(), Is.True);
            Assert.That(inventory.BuyStorage(), Is.True);
            Assert.That(inventory.Cap(Ingredient), Is.EqualTo(300));
            Assert.That(inventory.BuyStorage(), Is.False);
            Assert.That(_wallet.Money, Is.EqualTo(100000 - 300 - 1000 - 3000));
        }

        // mozzis 마리의 grass1 과 redbean 더미 pile 을 가진 세이브.
        private static FarmState State(int mozzis, int pile)
        {
            FarmState state = new FarmState();
            for (int i = 0; i < mozzis; i++)
            {
                FarmMozziState m = new FarmMozziState();
                m.id = i + 1;
                m.speciesId = Species;
                state.mozzis.Add(m);
            }

            state.nextMozziId = mozzis + 1;
            state.piles.Add(new CountEntry(Ingredient, pile));
            return state;
        }

        private FarmManager Build(FarmState state)
        {
            FakeSaveHub save = new FakeSaveHub();
            save.Json = JsonUtility.ToJson(state);
            RegisterHubs(save);
            InGameHub.Register<IInventoryHub>(_inventory, this);

            FarmManager farm = new FarmManager();
            farm.Init();
            farm.PostInit();
            return farm;
        }

        private InventoryManager BuildInventory(string json)
        {
            FakeSaveHub save = new FakeSaveHub();
            save.Json = json;
            RegisterHubs(save);

            InventoryManager inventory = new InventoryManager();
            inventory.Init();
            inventory.PostInit();
            return inventory;
        }

        private void RegisterHubs(FakeSaveHub save)
        {
            InGameHub.Register<IStaticDataHub>(_data, this);
            InGameHub.Register<ISaveHub>(save, this);
            InGameHub.Register<IClockHub>(new FakeClockHub(), this);
            InGameHub.Register<IWalletHub>(_wallet, this);
        }
    }
}

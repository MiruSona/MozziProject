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
    // FarmManager.PickUp(재료, 양) 검사. 바닥 낱개 · 꾸러미 하나만큼만 창고로 옮기고, 더미 · 창고 상한에서 잘리는지 본다.
    // 표는 구운 것을 그대로 쓰고 나머지 창구는 가짜로 끼운다. 더미 수는 세이브 복원으로 넣는다.
    public sealed class FarmPickUpTests
    {
        private const int StartPile = 10;

        private FakeInventoryHub _inventory;
        private FarmManager _farm;
        private string _ingredientId;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();

            StaticDataManager data = new StaticDataManager();
            data.Init();
            _ingredientId = data.Ingredients.All[0].Id;

            FarmState state = new FarmState();
            state.piles.Add(new CountEntry(_ingredientId, StartPile));
            FakeSaveHub save = new FakeSaveHub();
            save.Json = JsonUtility.ToJson(state);

            _inventory = new FakeInventoryHub();
            InGameHub.Register<IStaticDataHub>(data, this);
            InGameHub.Register<ISaveHub>(save, this);
            InGameHub.Register<IClockHub>(new FakeClockHub(), this);
            InGameHub.Register<IInventoryHub>(_inventory, this);
            InGameHub.Register<IWalletHub>(new FakeWalletHub(), this);

            _farm = new FarmManager();
            _farm.Init();
            _farm.PostInit();
        }

        [TearDown]
        public void TearDown()
        {
            InGameHub.Clear();
        }

        [Test]
        public void PickUpAmount_MovesOnlyThatMany()
        {
            int moved = _farm.PickUp(_ingredientId, 3);

            Assert.That(moved, Is.EqualTo(3));
            Assert.That(_farm.PileOf(_ingredientId), Is.EqualTo(StartPile - 3));
            Assert.That(_inventory.Count(_ingredientId), Is.EqualTo(3));
        }

        [Test]
        public void PickUpAmount_MoreThanPile_MovesPile()
        {
            int moved = _farm.PickUp(_ingredientId, StartPile + 5);

            Assert.That(moved, Is.EqualTo(StartPile));
            Assert.That(_farm.PileOf(_ingredientId), Is.EqualTo(0));
        }

        [Test]
        public void PickUpAmount_InventoryAlmostFull_LeavesRestOnPile()
        {
            _inventory.Room = 2;

            int moved = _farm.PickUp(_ingredientId, 5);

            Assert.That(moved, Is.EqualTo(2));
            Assert.That(_farm.PileOf(_ingredientId), Is.EqualTo(StartPile - 2));
        }

        [Test]
        public void PickUpAmount_ZeroOrNegative_MovesNothing()
        {
            Assert.That(_farm.PickUp(_ingredientId, 0), Is.EqualTo(0));
            Assert.That(_farm.PickUp(_ingredientId, -3), Is.EqualTo(0));
            Assert.That(_farm.PileOf(_ingredientId), Is.EqualTo(StartPile));
        }

        [Test]
        public void PickUpAll_StillTakesWholePile()
        {
            int moved = _farm.PickUp(_ingredientId);

            Assert.That(moved, Is.EqualTo(StartPile));
            Assert.That(_farm.PileOf(_ingredientId), Is.EqualTo(0));
        }
    }
}

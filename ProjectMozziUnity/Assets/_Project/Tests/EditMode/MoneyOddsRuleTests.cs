using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 돈 · 확률 규칙 검사. 표는 구운 것을 그대로 쓰고 가판대는 세이브 복원으로 채운다.
    public sealed class MoneyOddsRuleTests
    {
        private const int PlainPrice = 10;

        private StaticDataManager _data;
        private FakeWalletHub _wallet;

        [SetUp]
        public void SetUp()
        {
            InGameHub.Clear();
            _data = new StaticDataManager();
            _data.Init();
            _wallet = new FakeWalletHub();
        }

        [TearDown]
        public void TearDown()
        {
            InGameHub.Clear();
        }

        // 15 × 1.5 = 22.5 는 올려서 23 (Mathf.RoundToInt 면 22 가 된다).
        [Test]
        public void OrderPrice_RoundsHalfUp()
        {
            StallRow stall = _data.Stalls.Get(DataKeys.DefaultRowId);

            Assert.That(stall.OrderMult, Is.EqualTo(1.5f));
            Assert.That(PriceUtil.OrderPrice(15, stall), Is.EqualTo(23));
        }

        [Test]
        public void FindRecipe_IgnoresOrder()
        {
            RecipeRow forward = RecipeMatchUtil.FindRecipe(_data.Recipes, new List<string> { "redbean", "bean" });
            RecipeRow backward = RecipeMatchUtil.FindRecipe(_data.Recipes, new List<string> { "bean", "redbean" });

            Assert.That(forward, Is.Not.Null);
            Assert.That(backward, Is.SameAs(forward));
        }

        // 경계값은 그 칸 다음 갈래로 간다 (roll < 경계 만 앞 갈래).
        [Test]
        public void PickBranch_Boundaries()
        {
            SynthesisRuleRow rule = _data.SynthesisRules.Get("tier1");

            Assert.That(SynthesisRollUtil.PickBranch(rule, true, rule.PFixed), Is.EqualTo(SynthesisBranch.OtherFamily));
            Assert.That(SynthesisRollUtil.PickBranch(rule, true, rule.PFixed + rule.POtherFamily),
                Is.EqualTo(SynthesisBranch.Shiny));
            Assert.That(SynthesisRollUtil.PickBranch(rule, false, rule.POtherFamilyTop), Is.EqualTo(SynthesisBranch.Shiny));
        }

        // 점원이 있으면 하루 손님 N 명을 다 팔고 닫는다. 주문 없는 손님이라 정가 그대로다.
        [Test]
        public void Skip_WithClerk_SellsDayAndCloses()
        {
            StallManager stall = Build(OpenStall(true));
            int perDay = stall.CustomersPerDay;

            stall.Skip(100000);

            Assert.That(stall.OfflineReport.ServedCount, Is.EqualTo(perDay));
            Assert.That(stall.OfflineReport.ClosedForNight, Is.True);
            Assert.That(stall.OfflineReport.GoldEarned, Is.EqualTo(perDay * PlainPrice));
            Assert.That(_wallet.Money, Is.EqualTo(perDay * PlainPrice));
            Assert.That(stall.IsOpen, Is.False);
        }

        [Test]
        public void Skip_NoClerk_QueueFillsAndNothingSold()
        {
            StallState state = OpenStall(false);
            int shelfCount = state.shelf.Count;
            StallManager stall = Build(state);

            stall.Skip(100000);

            Assert.That(stall.Queue.Count, Is.EqualTo(stall.QueueMax));
            Assert.That(stall.OfflineReport.ServedCount, Is.EqualTo(0));
            Assert.That(stall.Shelf.Count, Is.EqualTo(shelfCount));
            Assert.That(_wallet.Money, Is.EqualTo(0));
        }

        // 열린 가판대 · 진열대에 평범한 떡 30개 (하루 손님 수보다 넉넉히).
        private static StallState OpenStall(bool hasClerk)
        {
            StallState state = new StallState();
            state.isOpen = true;
            state.hasClerk = hasClerk;
            for (int i = 0; i < 30; i++)
            {
                state.shelf.Add(new ShelfItem("", PlainPrice));
            }

            return state;
        }

        private StallManager Build(StallState state)
        {
            FakeSaveHub save = new FakeSaveHub();
            save.Json = JsonUtility.ToJson(state);

            InGameHub.Register<IStaticDataHub>(_data, this);
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

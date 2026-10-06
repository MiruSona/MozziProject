using System.Collections.Generic;
using Mozzi.Game.View;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 바닥 재료 셈 검사. 화면 합계 · 떨군 낱개가 합쳐질 더미 · 30 넘을 때 뭉칠 짝 · 모델이 줄었을 때 깎는 순서 · 훑기 거리.
    public sealed class FloorItemMathTests
    {
        [Test]
        public void ShownOf_SumsOnlyThatIngredient()
        {
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 1, 0f, 0f),
                Entry("rice", 5, 1f, 0f),
                Entry("bean", 3, 2f, 0f),
            };

            Assert.That(FloorItemMath.ShownOf(items, "bean"), Is.EqualTo(4));
            Assert.That(FloorItemMath.ShownOf(items, "rice"), Is.EqualTo(5));
            Assert.That(FloorItemMath.ShownOf(items, "none"), Is.EqualTo(0));
        }

        [Test]
        public void FindDropTarget_NearestSameIngredientUnderMaxInsideRadius()
        {
            // 0 은 꽉 찼고(5) 1 은 재료가 다르다. 2 와 3 가운데 가까운 3 을 고른다. 4 는 반지름 밖이다.
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 5, 0.1f, 0f),
                Entry("rice", 1, 0.1f, 0f),
                Entry("bean", 2, 0.8f, 0f),
                Entry("bean", 4, 0.3f, 0f),
                Entry("bean", 1, 5f, 0f),
            };

            Assert.That(FloorItemMath.FindDropTarget(items, "bean", Vector2.zero, 1f, 5), Is.EqualTo(3));
            Assert.That(FloorItemMath.FindDropTarget(items, "bean", Vector2.zero, 0.2f, 5), Is.EqualTo(-1));
            Assert.That(FloorItemMath.FindDropTarget(items, "mugwort", Vector2.zero, 9f, 5), Is.EqualTo(-1));
        }

        [Test]
        public void FindMergePair_PicksNearestSameIngredient()
        {
            // rice 둘이 가장 가깝지만 bean 은 재료가 달라 같이 안 뭉친다. bean 끼리는 0 과 3 이 가장 가깝다.
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 1, 0f, 0f),
                Entry("rice", 1, 0.1f, 0f),
                Entry("bean", 1, 9f, 0f),
                Entry("bean", 1, 0.5f, 0f),
            };

            int keep;
            int drop;
            Assert.That(FloorItemMath.FindMergePair(items, 99, out keep, out drop), Is.True);
            Assert.That(keep, Is.EqualTo(0));
            Assert.That(drop, Is.EqualTo(3));
        }

        [Test]
        public void FindMergePair_KeepsLargerAmount()
        {
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 1, 0f, 0f),
                Entry("bean", 7, 1f, 0f),
            };

            int keep;
            int drop;
            FloorItemMath.FindMergePair(items, 99, out keep, out drop);

            Assert.That(keep, Is.EqualTo(1));
            Assert.That(drop, Is.EqualTo(0));
        }

        [Test]
        public void FindMergePair_AllDifferent_ReturnsFalse()
        {
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 1, 0f, 0f),
                Entry("rice", 1, 0f, 0f),
            };

            int keep;
            int drop;
            Assert.That(FloorItemMath.FindMergePair(items, 99, out keep, out drop), Is.False);
        }

        [Test]
        public void PlanRemove_CutsNewestFirstAndLeavesOthers()
        {
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 4, 0f, 0f),
                Entry("rice", 2, 0f, 0f),
                Entry("bean", 1, 0f, 0f),
                Entry("bean", 2, 0f, 0f),
            };
            List<int> amounts = new List<int>();

            FloorItemMath.PlanRemove(items, "bean", 4, amounts);

            Assert.That(amounts, Is.EqualTo(new List<int> { 3, 2, 0, 0 }));
        }

        [Test]
        public void PlanRemove_MoreThanShown_EmptiesThatIngredientOnly()
        {
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 2, 0f, 0f),
                Entry("rice", 2, 0f, 0f),
            };
            List<int> amounts = new List<int>();

            FloorItemMath.PlanRemove(items, "bean", 10, amounts);

            Assert.That(amounts, Is.EqualTo(new List<int> { 0, 2 }));
        }

        [Test]
        public void DistanceToSegment_MiddleEndAndPoint()
        {
            Vector2 a = new Vector2(0f, 0f);
            Vector2 b = new Vector2(10f, 0f);

            Assert.That(FloorItemMath.DistanceToSegment(a, b, new Vector2(5f, 3f)), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(FloorItemMath.DistanceToSegment(a, b, new Vector2(13f, 4f)), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(FloorItemMath.DistanceToSegment(a, a, new Vector2(3f, 4f)), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void ChoosePressTarget_FullItemGivesWayToMozziAndBuilding()
        {
            // 창고가 찬 재료는 겹친 찹쌀이 · 건물에게 누름을 넘긴다.
            Assert.That(FloorItemMath.ChoosePressTarget(false, true, true, false), Is.EqualTo(FarmPressTarget.Mozzi));
            Assert.That(FloorItemMath.ChoosePressTarget(false, true, false, true), Is.EqualTo(FarmPressTarget.Building));
            // 겹친 것이 없으면 그 재료가 받는다 (못 줍는다는 흔들림).
            Assert.That(FloorItemMath.ChoosePressTarget(false, true, false, false), Is.EqualTo(FarmPressTarget.FloorItem));
        }

        [Test]
        public void FindMergePair_SkipsPairsOverPileMax()
        {
            // 0 과 1 이 가장 가깝지만 합이 6 이라 꾸러미 한도 5 를 넘는다. 합 5 인 짝 중 가장 가까운 1 과 2 가 뭉친다.
            List<FloorItemEntry> items = new List<FloorItemEntry>
            {
                Entry("bean", 3, 0f, 0f),
                Entry("bean", 3, 0.1f, 0f),
                Entry("bean", 2, 1f, 0f),
                Entry("bean", 5, 0.05f, 0f),
            };

            int keep;
            int drop;
            Assert.That(FloorItemMath.FindMergePair(items, 5, out keep, out drop), Is.True);
            Assert.That(keep, Is.EqualTo(1));
            Assert.That(drop, Is.EqualTo(2));

            List<FloorItemEntry> full = new List<FloorItemEntry>
            {
                Entry("bean", 5, 0f, 0f),
                Entry("bean", 1, 0.1f, 0f),
            };
            Assert.That(FloorItemMath.FindMergePair(full, 5, out keep, out drop), Is.False);
        }

        [Test]
        public void SplitIntoPiles_NeverOverPileMax()
        {
            List<int> parts = new List<int>();

            FloorItemMath.SplitIntoPiles(12, 5, parts);
            Assert.That(parts, Is.EqualTo(new List<int> { 5, 5, 2 }));

            FloorItemMath.SplitIntoPiles(5, 5, parts);
            Assert.That(parts, Is.EqualTo(new List<int> { 5 }));

            FloorItemMath.SplitIntoPiles(0, 5, parts);
            Assert.That(parts, Is.Empty);
        }

        [Test]
        public void ChoosePressTarget_RoomItemFirstThenMozziBuildingGround()
        {
            Assert.That(FloorItemMath.ChoosePressTarget(true, true, true, true), Is.EqualTo(FarmPressTarget.FloorItem));
            Assert.That(FloorItemMath.ChoosePressTarget(false, false, true, true), Is.EqualTo(FarmPressTarget.Mozzi));
            Assert.That(FloorItemMath.ChoosePressTarget(false, false, false, true), Is.EqualTo(FarmPressTarget.Building));
            Assert.That(FloorItemMath.ChoosePressTarget(false, false, false, false), Is.EqualTo(FarmPressTarget.Ground));
        }

        private static FloorItemEntry Entry(string id, int amount, float x, float y)
        {
            return new FloorItemEntry(id, amount, new Vector2(x, y));
        }
    }
}

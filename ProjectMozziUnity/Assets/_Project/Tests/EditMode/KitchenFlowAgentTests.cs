using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Ui;
using NUnit.Framework;

namespace Mozzi.Tests
{
    // 주방 네 단계 흐름 검사. 떡메 3탭 · 탭 사이 250ms · 연타 175ms (stall 표 기본값과 같다).
    public sealed class KitchenFlowAgentTests
    {
        private static readonly List<string> Picked = new List<string> { "redbean", "bean" };

        private KitchenFlowAgent _flow;

        [SetUp]
        public void SetUp()
        {
            StallRow row = new StallRow { PoundTaps = 3, PoundGapMs = 250, DoubleTapMs = 175 };
            _flow = new KitchenFlowAgent(row);
        }

        [Test]
        public void FullFlow_SteamPoundShape_ThenMakeAndBackToPick()
        {
            Assert.That(Press(KitchenTool.Steamer, 0f), Is.EqualTo(KitchenFlowResult.Steamed));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pound));

            Assert.That(Press(KitchenTool.Mortar, 1f), Is.EqualTo(KitchenFlowResult.Pounded));
            Assert.That(Press(KitchenTool.Mortar, 2f), Is.EqualTo(KitchenFlowResult.Pounded));
            Assert.That(Press(KitchenTool.Mortar, 3f), Is.EqualTo(KitchenFlowResult.PoundDone));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Shape));

            Assert.That(Press(KitchenTool.Board, 4f), Is.EqualTo(KitchenFlowResult.Make));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void FirstMortarTap_CountsAsOne()
        {
            Press(KitchenTool.Steamer, 0f);
            Press(KitchenTool.Mortar, 1f);

            Assert.That(_flow.PoundTaps, Is.EqualTo(1));
        }

        [Test]
        public void WrongTool_DoesNothing()
        {
            Assert.That(Press(KitchenTool.Mortar, 0f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Board, 1f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));

            Press(KitchenTool.Steamer, 2f);
            Assert.That(Press(KitchenTool.Steamer, 3f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Board, 4f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pound));
            Assert.That(_flow.PoundTaps, Is.EqualTo(0));
        }

        [Test]
        public void Steamer_WithoutIngredients_DoesNotStart()
        {
            KitchenFlowResult result = _flow.Press(KitchenTool.Steamer, 0f, new List<string>(), false, false);

            Assert.That(result, Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.NextTool(false), Is.EqualTo(KitchenTool.None));
            Assert.That(_flow.IsRunning, Is.False);
        }

        [Test]
        public void NextTool_FollowsStep()
        {
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Steamer));
            Press(KitchenTool.Steamer, 0f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Mortar));
            Press(KitchenTool.Mortar, 1f);
            Press(KitchenTool.Mortar, 2f);
            Press(KitchenTool.Mortar, 3f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Board));
        }

        [Test]
        public void DoubleTapOnSteamer_SkipsToMake()
        {
            Press(KitchenTool.Steamer, 0f);

            Assert.That(Press(KitchenTool.Steamer, 0.1f), Is.EqualTo(KitchenFlowResult.Make));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void DoubleTapWhilePounding_SkipsToMake()
        {
            Press(KitchenTool.Steamer, 0f);
            Press(KitchenTool.Mortar, 1f);

            Assert.That(Press(KitchenTool.Mortar, 1.1f), Is.EqualTo(KitchenFlowResult.Make));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
        }

        [Test]
        public void TapInsideGap_IsIgnoredAndNotCounted()
        {
            Press(KitchenTool.Steamer, 0f);
            Press(KitchenTool.Mortar, 1f);

            Assert.That(Press(KitchenTool.Mortar, 1.2f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.PoundTaps, Is.EqualTo(1));
            Assert.That(Press(KitchenTool.Mortar, 1.5f), Is.EqualTo(KitchenFlowResult.Pounded));
            Assert.That(_flow.PoundTaps, Is.EqualTo(2));
        }

        [Test]
        public void KnownCombo_SkipsProcessOnSteamer()
        {
            KitchenFlowResult result = _flow.Press(KitchenTool.Steamer, 0f, Picked, true, true);

            Assert.That(result, Is.EqualTo(KitchenFlowResult.QuickMake));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void Reset_ReturnsToPick()
        {
            Press(KitchenTool.Steamer, 0f);
            Press(KitchenTool.Mortar, 1f);

            _flow.Reset();

            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IsRunning, Is.False);
            Assert.That(Press(KitchenTool.Mortar, 2f), Is.EqualTo(KitchenFlowResult.Ignored));
        }

        private KitchenFlowResult Press(KitchenTool tool, float now)
        {
            return _flow.Press(tool, now, Picked, true, false);
        }
    }
}

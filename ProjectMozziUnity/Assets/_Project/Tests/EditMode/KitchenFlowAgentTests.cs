using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Ui;
using NUnit.Framework;

namespace Mozzi.Tests
{
    // 주방 다섯 단계 흐름 검사 (반복 08). 맷돌 · 떡메 3탭 · 탭 사이 250ms · 연타 175ms (stall 표 기본값과 같다) · 찌기 2.5초.
    public sealed class KitchenFlowAgentTests
    {
        private const float SteamSec = 2.5f;
        private static readonly List<string> Picked = new List<string> { "redbean", "bean" };

        private KitchenFlowAgent _flow;

        [SetUp]
        public void SetUp()
        {
            StallRow row = new StallRow { PoundTaps = 3, PoundGapMs = 250, DoubleTapMs = 175 };
            _flow = new KitchenFlowAgent(row, SteamSec);
        }

        [Test]
        public void FullFlow_GrindPoundShapeSteam_ThenMakeAndBackToPick()
        {
            Assert.That(Press(KitchenTool.Mill, 0f), Is.EqualTo(KitchenFlowResult.Ground));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Grind));
            Assert.That(Press(KitchenTool.Mill, 1f), Is.EqualTo(KitchenFlowResult.Ground));
            Assert.That(Press(KitchenTool.Mill, 2f), Is.EqualTo(KitchenFlowResult.GrindDone));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pound));

            Assert.That(Press(KitchenTool.Mortar, 3f), Is.EqualTo(KitchenFlowResult.Pounded));
            Assert.That(Press(KitchenTool.Mortar, 4f), Is.EqualTo(KitchenFlowResult.Pounded));
            Assert.That(Press(KitchenTool.Mortar, 5f), Is.EqualTo(KitchenFlowResult.PoundDone));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Shape));

            Assert.That(Press(KitchenTool.Board, 6f), Is.EqualTo(KitchenFlowResult.Shaped));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Steam));
            Assert.That(_flow.IsRunning, Is.True);

            Assert.That(_flow.Tick(6f + SteamSec), Is.EqualTo(KitchenFlowResult.Steamed));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void FirstMillPress_StartsFlowAndCountsAsOne()
        {
            Press(KitchenTool.Mill, 0f);

            Assert.That(_flow.IsRunning, Is.True);
            Assert.That(_flow.GrindTaps, Is.EqualTo(1));
            Assert.That(_flow.PoundTaps, Is.EqualTo(0));
        }

        [Test]
        public void FirstMortarTap_CountsAsOne()
        {
            GrindAll(0f);
            Press(KitchenTool.Mortar, 3f);

            Assert.That(_flow.PoundTaps, Is.EqualTo(1));
        }

        [Test]
        public void WrongTool_DoesNothing()
        {
            Assert.That(Press(KitchenTool.Mortar, 0f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Board, 1f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));

            Press(KitchenTool.Mill, 2f);
            Assert.That(Press(KitchenTool.Mortar, 3f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Board, 4f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Grind));
            Assert.That(_flow.GrindTaps, Is.EqualTo(1));
        }

        [Test]
        public void Mill_WithoutIngredients_DoesNotStart()
        {
            KitchenFlowResult result = _flow.Press(KitchenTool.Mill, 0f, new List<string>(), false, false);

            Assert.That(result, Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.NextTool(false), Is.EqualTo(KitchenTool.None));
            Assert.That(_flow.IsRunning, Is.False);
        }

        [Test]
        public void NextTool_FollowsStep()
        {
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Mill));
            Press(KitchenTool.Mill, 0f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Mill));
            Press(KitchenTool.Mill, 1f);
            Press(KitchenTool.Mill, 2f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Mortar));
            Press(KitchenTool.Mortar, 3f);
            Press(KitchenTool.Mortar, 4f);
            Press(KitchenTool.Mortar, 5f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.Board));
            Press(KitchenTool.Board, 6f);
            Assert.That(_flow.NextTool(true), Is.EqualTo(KitchenTool.None));
        }

        [Test]
        public void DoubleTapOnMill_SkipsToMake()
        {
            Press(KitchenTool.Mill, 0f);

            Assert.That(Press(KitchenTool.Mill, 0.1f), Is.EqualTo(KitchenFlowResult.Make));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void DoubleTapWhilePounding_SkipsToMake()
        {
            GrindAll(0f);
            Press(KitchenTool.Mortar, 3f);

            Assert.That(Press(KitchenTool.Mortar, 3.1f), Is.EqualTo(KitchenFlowResult.Make));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
        }

        [Test]
        public void TapInsideGap_IsIgnoredAndNotCounted()
        {
            Press(KitchenTool.Mill, 0f);

            Assert.That(Press(KitchenTool.Mill, 0.2f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.GrindTaps, Is.EqualTo(1));
            Assert.That(Press(KitchenTool.Mill, 0.5f), Is.EqualTo(KitchenFlowResult.Ground));
            Assert.That(_flow.GrindTaps, Is.EqualTo(2));
        }

        [Test]
        public void KnownCombo_SkipsProcessOnFirstMillPress()
        {
            KitchenFlowResult result = _flow.Press(KitchenTool.Mill, 0f, Picked, true, true);

            Assert.That(result, Is.EqualTo(KitchenFlowResult.QuickMake));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IngredientIds, Is.EqualTo(Picked));
        }

        [Test]
        public void Steam_FinishesOnlyAfterSteamSec()
        {
            RunToSteam();

            Assert.That(_flow.Tick(6f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Tick(6f + SteamSec - 0.01f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Steam));
            Assert.That(_flow.Tick(6f + SteamSec), Is.EqualTo(KitchenFlowResult.Steamed));
            // 다 찐 뒤로는 다시 Steamed 가 안 나온다 (Make 는 한 번만).
            Assert.That(_flow.Tick(20f), Is.EqualTo(KitchenFlowResult.Ignored));
        }

        [Test]
        public void Tick_OutsideSteam_IsIgnored()
        {
            Assert.That(_flow.Tick(100f), Is.EqualTo(KitchenFlowResult.Ignored));
            Press(KitchenTool.Mill, 0f);
            Assert.That(_flow.Tick(100f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Grind));
        }

        // 찌는 중에는 연타 건너뛰기도 없다 — 바로 뒤에 눌러도 · 어느 도구를 눌러도 Ignored.
        [Test]
        public void PressWhileSteaming_IsIgnored_EvenDoubleTap()
        {
            RunToSteam();

            Assert.That(Press(KitchenTool.Board, 6.05f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Mill, 6.1f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(Press(KitchenTool.Mortar, 7f), Is.EqualTo(KitchenFlowResult.Ignored));
            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Steam));
        }

        // 찌는 중 화면을 닫으면(Reset) 흐름이 처음으로 — 나중에 시간이 지나도 Steamed(만들기)가 안 나온다.
        [Test]
        public void ResetWhileSteaming_BackToPick_AndNeverSteams()
        {
            RunToSteam();

            _flow.Reset();

            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IsRunning, Is.False);
            Assert.That(_flow.Tick(6f + SteamSec + 1f), Is.EqualTo(KitchenFlowResult.Ignored));
        }

        [Test]
        public void Reset_ReturnsToPick()
        {
            GrindAll(0f);
            Press(KitchenTool.Mortar, 3f);

            _flow.Reset();

            Assert.That(_flow.Step, Is.EqualTo(KitchenStep.Pick));
            Assert.That(_flow.IsRunning, Is.False);
            Assert.That(Press(KitchenTool.Mortar, 4f), Is.EqualTo(KitchenFlowResult.Ignored));
        }

        private void GrindAll(float start)
        {
            Press(KitchenTool.Mill, start);
            Press(KitchenTool.Mill, start + 1f);
            Press(KitchenTool.Mill, start + 2f);
        }

        // 갈기 0~2 · 치기 3~5 · 빚기 6 → 찌기 시작 (6초).
        private void RunToSteam()
        {
            GrindAll(0f);
            Press(KitchenTool.Mortar, 3f);
            Press(KitchenTool.Mortar, 4f);
            Press(KitchenTool.Mortar, 5f);
            Press(KitchenTool.Board, 6f);
        }

        private KitchenFlowResult Press(KitchenTool tool, float now)
        {
            return _flow.Press(tool, now, Picked, true, false);
        }
    }
}

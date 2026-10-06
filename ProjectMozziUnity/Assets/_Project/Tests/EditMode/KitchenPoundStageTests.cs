using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Ui;
using NUnit.Framework;

namespace Mozzi.Tests
{
    // 떡메 반죽 단계 (반복 07 · 08) : 친 수 → 단계 · 한 번 치기 안의 장 → 묶음. 화면은 이 값으로 그림 묶음을 바꿔 끼운다.
    public sealed class KitchenPoundStageTests
    {
        [Test]
        public void StageAfter_AdvancesPerTap_AndStopsAtLast()
        {
            Assert.That(KitchenLayout.PoundStageAfter(0), Is.EqualTo(0));
            Assert.That(KitchenLayout.PoundStageAfter(1), Is.EqualTo(1));
            Assert.That(KitchenLayout.PoundStageAfter(2), Is.EqualTo(2));
            Assert.That(KitchenLayout.PoundStageAfter(3), Is.EqualTo(KitchenLayout.PoundStageLast));
            Assert.That(KitchenLayout.PoundStageAfter(-1), Is.EqualTo(0));
        }

        // 반복 08 새 프레임은 5번 장에서 떡메가 반죽에 닿는다.
        [Test]
        public void FrameStage_SwitchesAtHitFrame5()
        {
            Assert.That(KitchenLayout.PoundHitFrame, Is.EqualTo(5));
            for (int frame = 0; frame < KitchenLayout.PoundFrameSec.Length; frame++)
            {
                int expected = frame < 5 ? 0 : 1;
                Assert.That(KitchenLayout.FrameStage(frame, KitchenLayout.PoundHitFrame, 0, 1), Is.EqualTo(expected), "frame " + frame);
            }
        }

        // 화면은 탭을 받은 뒤 흐름의 PoundTaps 로 단계를 고른다 — 셋째 탭(PoundDone) 뒤에도 3 이어야 매끈한 떡이 된다.
        [Test]
        public void FlowTaps_AfterEachTap_GivePowderRoughSmooth()
        {
            StallRow row = new StallRow { PoundTaps = 3, PoundGapMs = 250, DoubleTapMs = 175 };
            KitchenFlowAgent flow = new KitchenFlowAgent(row, 2.5f);
            List<string> picked = new List<string> { "redbean" };
            for (int i = 0; i < 3; i++)
            {
                flow.Press(KitchenTool.Mill, i, picked, true, false);
            }

            int[] from = new int[3];
            int[] to = new int[3];
            for (int i = 0; i < 3; i++)
            {
                flow.Press(KitchenTool.Mortar, 3f + i, picked, true, false);
                from[i] = KitchenLayout.PoundStageAfter(flow.PoundTaps - 1);
                to[i] = KitchenLayout.PoundStageAfter(flow.PoundTaps);
            }

            Assert.That(from, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(to, Is.EqualTo(new[] { 1, 2, 2 }));
            Assert.That(flow.Step, Is.EqualTo(KitchenStep.Shape));
        }
    }
}

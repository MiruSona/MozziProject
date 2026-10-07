using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.Ui;
using NUnit.Framework;

namespace Mozzi.Tests
{
    // 떡메 치기 : 탭마다 PoundTaps 가 하나씩 늘고, 셋째 탭 뒤 「한 번 더」 에 빚기로 넘어간다. 묻힘 장 판정도 본다.
    public sealed class KitchenPoundStageTests
    {
        [Test]
        public void FlowTaps_CountEachTap_AndFourthGoesToShape()
        {
            StallRow row = new StallRow { PoundTaps = 3, PoundGapMs = 250, DoubleTapMs = 175 };
            KitchenFlowAgent flow = new KitchenFlowAgent(row, 2.5f);
            List<string> picked = new List<string> { "redbean" };
            flow.Press(KitchenTool.Mill, 0f, picked, true, false);
            flow.Press(KitchenTool.Mill, 1f, picked, true, false);
            flow.Press(KitchenTool.Mill, 2f, picked, true, false);
            flow.Press(KitchenTool.Mill, 2.5f, picked, true, false);
            Assert.That(flow.Step, Is.EqualTo(KitchenStep.Pound));

            int[] taps = new int[3];
            for (int i = 0; i < 3; i++)
            {
                flow.Press(KitchenTool.Mortar, 3f + i, picked, true, false);
                taps[i] = flow.PoundTaps;
            }

            Assert.That(taps, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(flow.Step, Is.EqualTo(KitchenStep.Pound));
            Assert.That(flow.IsWaitingNext, Is.True);

            flow.Press(KitchenTool.Mortar, 5.1f, picked, true, false);
            Assert.That(flow.Step, Is.EqualTo(KitchenStep.Shape));
        }

        // 단계 8장 중 0 · 5 · 7장과 넘어감 4장은 반죽이 떡메 위로 온다.
        [Test]
        public void DoughOnTop_StageFrames057_AndAllTurnFrames()
        {
            for (int frame = 0; frame < KitchenLayout.Pound15StageSec.Length; frame++)
            {
                bool expected = frame == 0 || frame == 5 || frame == 7;
                Assert.That(KitchenLayout.Pound15DoughOnTop(false, frame), Is.EqualTo(expected), "stage frame " + frame);
            }

            for (int frame = 0; frame < KitchenLayout.Pound15TurnSec.Length; frame++)
            {
                Assert.That(KitchenLayout.Pound15DoughOnTop(true, frame), Is.True, "turn frame " + frame);
            }
        }
    }
}

using LitMotion;
using Mozzi.Core;
using UnityEngine;

namespace Mozzi.Game.Vfx
{
    // LitMotion 을 게임의 시계로 굴린다. 디스패처 둘을 Process 에서 직접 Update 한다.
    // 건 쪽이 MotionHandle 을 쥐고 화면을 닫을 때 Cancel 한다. 씬이 내려가면 남은 것은 여기서 모두 끊는다.
    public sealed class VfxManager : IManager, IProcess, IVfxHub
    {
        private ManualMotionDispatcher _ui;
        private ManualMotionDispatcher _game;

        public IMotionScheduler Ui
        {
            get { return _ui.Scheduler; }
        }

        public IMotionScheduler Game
        {
            get { return _game.Scheduler; }
        }

        public void Init()
        {
            _ui = new ManualMotionDispatcher();
            _game = new ManualMotionDispatcher();
        }

        public void PostInit()
        {
        }

        // deltaTime 은 GameRunner 가 TimeScale 을 곱해 준 게임 시간이다. 화면 연출은 실제 시간으로 따로 굴린다.
        public void Process(float deltaTime)
        {
            _game.Update(deltaTime);
            _ui.Update(Time.unscaledDeltaTime);
        }

        public void Dispose()
        {
            _ui.Reset();
            _game.Reset();
        }
    }
}

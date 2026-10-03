using LitMotion;

namespace Mozzi.Game.Vfx
{
    // 연출(트윈)을 거는 창구. LMotion.Create(…).WithScheduler(여기 것) 으로 건다.
    // 화면 연출은 Ui, 게임 진행과 묶인 연출만 Game 을 쓴다.
    public interface IVfxHub
    {
        // 실제 시간으로 돈다. 게임 배속 · 일시정지와 상관없이 같은 빠르기다.
        IMotionScheduler Ui { get; }

        // 게임 시계로 돈다. 배속을 따르고 TimeScale 0 이면 멈춘다.
        IMotionScheduler Game { get; }
    }
}

using LitMotion;
using UnityEngine;

namespace Mozzi.Game.Ui
{
    // 주방 세 칸 띠(KitchenStrip)를 옆으로 민다. 칸 n 이면 띠 x = -PanWidth × n.
    // 이벤트 · Manager 를 모른다 — 언제 밀지는 KitchenScreen 이 정한다.
    public sealed class KitchenPanSlider
    {
        private readonly RectTransform _strip;
        private readonly IMotionScheduler _scheduler;
        private MotionHandle _handle;
        // 빈 MotionHandle(기본값)에 IsActive 를 묻지 않으려고 한 번이라도 걸었는지 따로 쥔다 (CustomerWalkView 와 같은 꼴).
        private bool _hasHandle;

        public int Current { get; private set; }

        // 미는 중이면 true. 화면이 이 동안 누름을 안 받는다.
        public bool IsSliding
        {
            get { return _hasHandle && _handle.IsActive(); }
        }

        // scheduler 는 InGameHub.Get<IVfxHub>().Ui 를 넘긴다.
        public KitchenPanSlider(RectTransform strip, IMotionScheduler scheduler)
        {
            _strip = strip;
            _scheduler = scheduler;
        }

        // 바로 그 칸으로 (화면 열 때).
        public void SnapTo(int pan)
        {
            Cancel();
            Current = Clamp(pan);
            SetX(TargetX(Current));
        }

        // PanSlideSec 동안 민다. 가는 중에 다시 부르면 앞 트윈을 끊고 지금 자리에서 새로 간다.
        public void SlideTo(int pan)
        {
            Cancel();
            Current = Clamp(pan);
            float from = _strip.anchoredPosition.x;
            _handle = LMotion.Create(from, TargetX(Current), KitchenLayout.PanSlideSec)
                .WithEase(Ease.OutCubic)
                .WithScheduler(_scheduler)
                .Bind(SetX);
            _hasHandle = true;
        }

        public void Cancel()
        {
            if (_hasHandle && _handle.IsActive())
            {
                _handle.Cancel();
            }
        }

        private void SetX(float x)
        {
            Vector2 position = _strip.anchoredPosition;
            position.x = x;
            _strip.anchoredPosition = position;
        }

        private static float TargetX(int pan)
        {
            return -KitchenLayout.PanWidth * pan;
        }

        private static int Clamp(int pan)
        {
            return Mathf.Clamp(pan, 0, KitchenLayout.PanCount - 1);
        }
    }
}

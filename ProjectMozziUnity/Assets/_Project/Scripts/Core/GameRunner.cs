using System;
using Mozzi.Events;
using UnityEngine;

namespace Mozzi.Core
{
    // 앱에서 Update 를 가진 유일한 자리다.
    // Service 가 먼저, Master 가 나중이다.
    public sealed class GameRunner : MonoBehaviour
    {
        private readonly ProcessList<IService> _services = new ProcessList<IService>();
        private readonly ProcessList<MasterBase> _masters = new ProcessList<MasterBase>();

        // 멈춤과 포커스 잃음이 잇달아 와도 한 번만 알린다.
        private bool _inBackground;
        // 뒤로 간 순간의 UTC 틱(0 이면 아직 뒤로 간 적이 없다)과 그때까지 Update 로 돈 초.
        // PC 는 포커스를 잃어도 계속 돌기도 하니, 실제 흐른 초에서 그동안 돈 초를 빼야 두 번 안 센다.
        private long _backgroundAtTicks;
        private double _ranSec;
        private double _backgroundAtRanSec;

        public void AddService(IService service)
        {
            _services.Add(service);
        }

        public void AddMaster(MasterBase master)
        {
            _masters.Add(master);
        }

        public void RemoveMaster(MasterBase master)
        {
            _masters.Remove(master);
        }

        private void Update()
        {
            _ranSec += Time.deltaTime;
            float dt = Time.deltaTime * GameMaster.TimeScale;
            _services.ProcessAll(dt);
            _masters.ProcessAll(dt);
        }

        // 씬 바꾸기는 그 프레임의 Process 가 다 끝난 뒤에 한다.
        private void LateUpdate()
        {
            GameMaster.FlushSceneRequest();
        }

        // 안드로이드는 홈으로 나가면 quitting 없이 죽을 수 있다. 뒤로 가는 순간을 AppBackgroundEvent 로 알린다.
        private void OnApplicationPause(bool paused)
        {
            SetBackground(paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            SetBackground(focused == false);
        }

        private void SetBackground(bool background)
        {
            if (background == _inBackground)
            {
                return;
            }

            _inBackground = background;
            if (background)
            {
                _backgroundAtTicks = DateTime.UtcNow.Ticks;
                _backgroundAtRanSec = _ranSec;
                EventBus.Publish(new AppBackgroundEvent());
                return;
            }

            // 멈췄다 돌아오면 Unity 는 deltaTime 을 잘라 버리므로 못 돈 초를 따로 알린다. 1초 미만은 무시한다.
            if (_backgroundAtTicks == 0)
            {
                return;
            }

            double wall = (DateTime.UtcNow.Ticks - _backgroundAtTicks) / (double)TimeSpan.TicksPerSecond;
            double seconds = Math.Max(0, wall - (_ranSec - _backgroundAtRanSec));
            _backgroundAtTicks = 0;
            if (seconds >= 1)
            {
                EventBus.Publish(new AppForegroundEvent(seconds));
            }
        }
    }
}

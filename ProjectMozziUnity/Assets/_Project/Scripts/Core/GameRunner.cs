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

        public void AddService(IService service)
        {
            _services.Add(service);
        }

        public void RemoveService(IService service)
        {
            _services.Remove(service);
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
            float dt = Time.deltaTime * GameMaster.TimeScale;
            _services.ProcessAll(dt);
            _masters.ProcessAll(dt);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime * GameMaster.TimeScale;
            _services.FixedProcessAll(dt);
            _masters.FixedProcessAll(dt);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime * GameMaster.TimeScale;
            _services.LateProcessAll(dt);
            _masters.LateProcessAll(dt);
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
                EventBus.Publish(new AppBackgroundEvent());
            }
        }
    }
}

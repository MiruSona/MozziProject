using UnityEngine;

namespace Mozzi.Core
{
    // 앱에서 Update 를 가진 유일한 자리다.
    // Service 가 먼저, Master 가 나중이다.
    public sealed class GameRunner : MonoBehaviour
    {
        private readonly ProcessList<IService> _services = new ProcessList<IService>();
        private readonly ProcessList<MasterBase> _masters = new ProcessList<MasterBase>();

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
    }
}

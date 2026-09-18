namespace Mozzi.Core
{
    // 대개 Init·PostInit 이 필요 없다. 필요한 것만 override 한다.
    public abstract class ServiceBase : IService
    {
        public virtual void Init()
        {
        }

        public virtual void PostInit()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}

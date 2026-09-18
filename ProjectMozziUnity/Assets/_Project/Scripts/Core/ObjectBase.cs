using Mozzi.Resource;
using UnityEngine;

namespace Mozzi.Core
{
    // Manager 가 Rent 로 만들고 Return 으로 거둔다.
    // 스스로 Destroy 하지 않는다.
    public abstract class ObjectBase : MonoBehaviour, IPooledObject
    {
        public bool IsDead { get; private set; }

        public virtual void OnRent()
        {
            IsDead = false;
        }

        public virtual void OnReturn()
        {
            IsDead = true;
        }

        public void MarkDead()
        {
            IsDead = true;
        }
    }
}

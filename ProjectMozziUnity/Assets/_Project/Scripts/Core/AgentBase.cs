using System;

namespace Mozzi.Core
{
    // Manager·Object 의 무거운 책임을 떼어 낸 조각.
    // 상태가 있고 owner 를 받는 것만 Agent 다.
    public abstract class AgentBase<TOwner> where TOwner : class
    {
        protected TOwner Owner { get; }

        protected AgentBase(TOwner owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            Owner = owner;
        }
    }
}

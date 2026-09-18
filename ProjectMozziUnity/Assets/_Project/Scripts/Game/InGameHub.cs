using Mozzi.Hubs;
using UnityEngine;

namespace Mozzi.Game
{
    // Farm 씬 하나의 Hub. Manager 가 I~Hub 로 등록하고 화면이 꺼내 쓴다.
    public static class InGameHub
    {
        private static readonly HubTable _table = new HubTable("InGameHub");

        // Domain Reload 를 꺼도 플레이마다 표를 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _table.Clear();
        }

        public static T Get<T>() where T : class
        {
            return _table.Get<T>();
        }

        public static bool TryGet<T>(out T hub) where T : class
        {
            return _table.TryGet<T>(out hub);
        }

        public static void Register<T>(T hub, object owner) where T : class
        {
            _table.Register<T>(hub, owner);
        }

        public static void UnregisterAllBy(object owner)
        {
            _table.UnregisterAllBy(owner);
        }

        public static void Clear()
        {
            _table.Clear();
        }
    }
}

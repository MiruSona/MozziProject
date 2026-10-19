namespace Mozzi.Hubs
{
    // Service 가 등록되는 유일한 Hub 다.
    public static class GlobalHub
    {
        private static readonly HubTable _table = new HubTable("GlobalHub");

        public static T Get<T>() where T : class
        {
            return _table.Get<T>();
        }

        public static void Register<T>(T hub, object owner) where T : class
        {
            _table.Register<T>(hub, owner);
        }

        public static void Clear()
        {
            _table.Clear();
        }
    }
}

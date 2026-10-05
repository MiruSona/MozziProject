using Mozzi.Save;

namespace Mozzi.Tests
{
    // 시험용 세이브. Register 때 Json 이 있으면 바로 RestoreJson 으로 넣어 준다 (실제 SaveManager 처럼).
    public sealed class FakeSaveHub : ISaveHub
    {
        public string Json;

        public long LoadedAtUtcTicks
        {
            get { return 0; }
        }

        public bool HasSaveFile
        {
            get { return Json != null; }
        }

        public SaveSettings Settings
        {
            get { return null; }
        }

        public void Register(ISavable savable)
        {
            if (Json != null)
            {
                savable.RestoreJson(Json);
            }
        }

        public void SaveNow()
        {
        }

        public void DeleteAndRestart()
        {
        }
    }
}

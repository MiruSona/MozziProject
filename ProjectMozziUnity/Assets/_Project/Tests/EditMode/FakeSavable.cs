using Mozzi.Save;

namespace Mozzi.Tests
{
    // 시험용 칸. 받은 json 을 그대로 돌려준다.
    public sealed class FakeSavable : ISavable
    {
        private readonly string _key;
        private readonly string _json;

        public FakeSavable(string key, string json)
        {
            _key = key;
            _json = json;
        }

        public string SaveKey
        {
            get { return _key; }
        }

        public string CaptureJson()
        {
            return _json;
        }

        public void RestoreJson(string json)
        {
        }
    }
}

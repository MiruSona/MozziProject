#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Mozzi.Save
{
    // WebGL 에서 파일을 쓰거나 지운 뒤 IndexedDB 로 밀어 넣는다 (Plugins/WebGL/MozziFs.jslib).
    // 에디터와 다른 플랫폼에서는 아무것도 안 한다.
    public static class WebGlFsUtil
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MozziSyncFs();
#endif

        public static void Sync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MozziSyncFs();
#endif
        }
    }
}

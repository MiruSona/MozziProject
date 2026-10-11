#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;

namespace VideoCaptureTool
{
    // Play 중 매 프레임을 png 로 떨어뜨리는 연속 캡처 도우미. 게임 코드를 하나도 참조하지 않아 옛 판에도 이 파일만 복사해 쓴다.
    // 방식 : Time.captureFramerate 로 게임 시간을 프레임마다 1/fps 초씩 고정해 돌리고,
    //        PlayerLoop 의 PostLateUpdate 끝에 끼운 한 걸음에서 ScreenCapture.CaptureScreenshot 을 부른다 (그 프레임 끝 화면이 찍힌다).
    //        MonoBehaviour 가 없어 Editor 어셈블리에 둬도 되고 씬을 건드리지 않는다.
    // 부르는 법 (unity command eval · 큰따옴표가 사라지지 않게 PowerShell 말고 Bash 에서 부른다) :
    //   VideoCaptureTool.VideoCapture.SetGameViewSize(540, 1200);
    //   VideoCaptureTool.VideoCapture.TouchSaveTime("save.json", "savedAtUtcTicks");  // 방치 보상 팝업 막기 (세이브 백업 뒤에)
    //   VideoCaptureTool.VideoCapture.Arm("Captures/v17", 30, 600, 45, "VideoCaptureTool.DemoDriverV17");  // Play 를 켜고 끝나면 Play 를 끈다
    //   VideoCaptureTool.VideoCapture.Stop();   // 중간에 멈출 때
    // 출력 폴더는 Unity 프로젝트 뿌리 기준 상대 경로. frame_00001.png 부터 5자리. 같은 폴더의 옛 frame_*.png 는 지운다.
    // 데모 드라이버 : 이름으로 찾는 정적 메서드 `IEnumerator Play()` 를 가진 타입. 없으면 화면만 찍는다.
    //   드라이버는 데움 프레임부터 매 프레임 MoveNext 한 번씩 돈다 (yield return null = 한 프레임 쉼).
    [InitializeOnLoad]
    public static class VideoCapture
    {
        private const string LogTag = "[VideoCapture] ";
        private const string KeyArmed = "VideoCapture.Armed";
        private const string KeyOut = "VideoCapture.Out";
        private const string KeyFps = "VideoCapture.Fps";
        private const string KeyFrames = "VideoCapture.Frames";
        private const string KeyWarmup = "VideoCapture.Warmup";
        private const string KeyDriver = "VideoCapture.Driver";
        private const string LogFileName = "capture_log.txt";

        private static bool _running;
        private static string _outDir;
        private static int _fps;
        private static int _frames;
        private static int _warmup;
        private static int _tick;
        private static int _saved;
        private static bool _exitWhenDone;
        private static IEnumerator _driver;
        private static readonly Stack<IEnumerator> _driverStack = new Stack<IEnumerator>();
        private static Stopwatch _clock;
        private static double _firstShotSec;

        static VideoCapture()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static bool IsRunning
        {
            get { return _running; }
        }

        // 지금 상태 한 줄 (eval 로 읽는다).
        public static string Status()
        {
            string s = "running=" + _running + " tick=" + _tick + " saved=" + _saved + "/" + _frames
                + " screen=" + Screen.width + "x" + Screen.height + " playing=" + EditorApplication.isPlaying
                + " armed=" + SessionState.GetBool(KeyArmed, false);
            Debug.Log(LogTag + s);
            return s;
        }

        // Play 를 켜고, 들어가자마자 데움 프레임 뒤 frames 장을 찍고, 다 찍으면 Play 를 끈다.
        public static void Arm(string outDirRelative, int fps, int frames, int warmupFrames, string driverTypeName)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError(LogTag + "이미 Play 중이다. Play 를 끄고 Arm 하거나, Play 중이면 Begin 을 부른다.");
                return;
            }

            SessionState.SetBool(KeyArmed, true);
            SessionState.SetString(KeyOut, outDirRelative);
            SessionState.SetInt(KeyFps, fps);
            SessionState.SetInt(KeyFrames, frames);
            SessionState.SetInt(KeyWarmup, warmupFrames);
            SessionState.SetString(KeyDriver, driverTypeName ?? "");
            Debug.Log(LogTag + "Arm · Play 를 켠다 · " + outDirRelative + " · " + frames + "장 @" + fps);
            EditorApplication.isPlaying = true;
        }

        // 이미 Play 중일 때 바로 시작한다. 다 찍어도 Play 는 그대로 둔다.
        public static void Begin(string outDirRelative, int fps, int frames, int warmupFrames, string driverTypeName)
        {
            if (EditorApplication.isPlaying == false)
            {
                Debug.LogError(LogTag + "Play 중이 아니다. Arm 을 쓴다.");
                return;
            }

            StartCapture(outDirRelative, fps, frames, warmupFrames, driverTypeName, false);
        }

        public static void Stop()
        {
            if (_running == false)
            {
                return;
            }

            Finish("Stop 호출");
        }

        // 세이브의 「마지막 저장 시각」 숫자 칸을 지금(UTC ticks)으로 바꿔 「돌아왔어요」 같은 방치 보상 팝업이 안 뜨게 한다.
        // Arm 바로 앞에 같은 eval 로 부른다. 파일 · 칸 이름은 판마다 다를 수 있어 인자로 받는다 (v17 : save.json · savedAtUtcTicks).
        // 칸을 못 찾으면 아무것도 안 바꾸고 false. 세이브 백업은 부르는 쪽이 먼저 한다.
        public static bool TouchSaveTime(string fileName, string fieldName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path) == false)
            {
                Debug.LogWarning(LogTag + "세이브 없음 : " + path);
                return false;
            }

            string text = File.ReadAllText(path);
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(
                "(\"" + System.Text.RegularExpressions.Regex.Escape(fieldName) + "\"\\s*:\\s*)-?\\d+");
            if (re.IsMatch(text) == false)
            {
                Debug.LogWarning(LogTag + "칸 없음 : " + fieldName + " in " + path);
                return false;
            }

            long now = DateTime.UtcNow.Ticks;
            File.WriteAllText(path, re.Replace(text, "${1}" + now, 1));
            Debug.Log(LogTag + "세이브 시각을 지금으로 : " + fieldName + "=" + now);
            return true;
        }

        // Game 뷰를 고정 해상도로 맞춘다. 빌드 대상 묶음(currentGroupType)에 넣어야 한다 — Standalone 묶음은 Android 대상에서 엉뚱한 크기가 된다.
        public static string SetGameViewSize(int width, int height)
        {
            Assembly asm = typeof(EditorWindow).Assembly;
            Type sizesType = asm.GetType("UnityEditor.GameViewSizes");
            Type singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleType.GetProperty("instance").GetValue(null, null);
            object groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes, null);
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { groupType });
            Type groupT = group.GetType();
            Type sizeT = asm.GetType("UnityEditor.GameViewSize");
            Type sizeKindT = asm.GetType("UnityEditor.GameViewSizeType");

            int count = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                object size = groupT.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                int w = (int)sizeT.GetProperty("width").GetValue(size, null);
                int h = (int)sizeT.GetProperty("height").GetValue(size, null);
                string kind = sizeT.GetProperty("sizeType").GetValue(size, null).ToString();
                if (w == width && h == height && kind == "FixedResolution")
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                object fixedKind = Enum.Parse(sizeKindT, "FixedResolution");
                ConstructorInfo ctor = sizeT.GetConstructor(new[] { sizeKindT, typeof(int), typeof(int), typeof(string) });
                object newSize = ctor.Invoke(new object[] { fixedKind, width, height, width + "x" + height });
                groupT.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                index = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            Type gameViewT = asm.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewT, false, null, false);
            PropertyInfo selected = gameViewT.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selected.SetValue(gameView, index, null);
            MethodInfo callback = gameViewT.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (callback != null)
            {
                callback.Invoke(gameView, new object[] { index, null });
            }

            gameView.Repaint();
            string s = "Game 뷰 " + width + "x" + height + " · 묶음 " + groupType + " · 자리 " + index;
            Debug.Log(LogTag + s);
            return s;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyArmed, false))
            {
                SessionState.SetBool(KeyArmed, false);
                StartCapture(SessionState.GetString(KeyOut, "Captures/out"), SessionState.GetInt(KeyFps, 30),
                    SessionState.GetInt(KeyFrames, 300), SessionState.GetInt(KeyWarmup, 30), SessionState.GetString(KeyDriver, ""), true);
                return;
            }

            if (change == PlayModeStateChange.ExitingPlayMode && _running)
            {
                Finish("Play 꺼짐");
            }
        }

        private static void StartCapture(string outDirRelative, int fps, int frames, int warmupFrames, string driverTypeName, bool exitWhenDone)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            _outDir = Path.GetFullPath(Path.Combine(projectRoot, outDirRelative));
            Directory.CreateDirectory(_outDir);
            foreach (string old in Directory.GetFiles(_outDir, "frame_*.png"))
            {
                File.Delete(old);
            }

            _fps = Mathf.Max(1, fps);
            _frames = Mathf.Max(1, frames);
            _warmup = Mathf.Max(0, warmupFrames);
            _tick = 0;
            _saved = 0;
            _exitWhenDone = exitWhenDone;
            _driver = CreateDriver(driverTypeName);
            _driverStack.Clear();
            if (_driver != null)
            {
                _driverStack.Push(_driver);
            }
            _clock = Stopwatch.StartNew();
            _firstShotSec = -1;
            Time.captureFramerate = _fps;
            InstallLoop();
            _running = true;
            Debug.Log(LogTag + "시작 · " + _outDir + " · " + _frames + "장 @" + _fps + " · 데움 " + _warmup
                + " · 드라이버 " + (_driver != null ? driverTypeName : "없음") + " · screen " + Screen.width + "x" + Screen.height);
        }

        private static IEnumerator CreateDriver(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(typeName, false);
                if (t == null)
                {
                    continue;
                }

                MethodInfo play = t.GetMethod("Play", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (play != null)
                {
                    return play.Invoke(null, null) as IEnumerator;
                }
            }

            Debug.LogWarning(LogTag + "드라이버 타입을 못 찾음 : " + typeName);
            return null;
        }

        // 한 프레임 걸음. 드라이버를 한 칸 돌리고, 데움이 끝났으면 이 프레임 끝 화면을 예약해 찍는다.
        private static void Tick()
        {
            if (_running == false || Application.isPlaying == false)
            {
                return;
            }

            if (_driver != null)
            {
                try
                {
                    StepDriver();
                }
                catch (Exception e)
                {
                    Debug.LogError(LogTag + "드라이버 예외로 드라이버만 멈춤 (캡처는 계속) : " + e);
                    _driver = null;
                }
            }

            _tick++;
            if (_tick <= _warmup)
            {
                return;
            }

            if (_saved >= _frames)
            {
                Finish("다 찍음");
                return;
            }

            if (_firstShotSec < 0)
            {
                _firstShotSec = _clock.Elapsed.TotalSeconds;
            }

            _saved++;
            string path = Path.Combine(_outDir, "frame_" + _saved.ToString("00000") + ".png");
            ScreenCapture.CaptureScreenshot(path);
        }

        // 드라이버 한 칸. yield return 으로 넘긴 IEnumerator 는 안으로 들어가 돈다 (코루틴처럼). 그 밖의 값은 한 프레임 쉼.
        private static void StepDriver()
        {
            while (_driverStack.Count > 0)
            {
                IEnumerator top = _driverStack.Peek();
                if (top.MoveNext() == false)
                {
                    _driverStack.Pop();
                    continue;
                }

                IEnumerator inner = top.Current as IEnumerator;
                if (inner != null)
                {
                    _driverStack.Push(inner);
                    continue;
                }

                return;
            }

            _driver = null;
        }

        private static void Finish(string why)
        {
            _running = false;
            Time.captureFramerate = 0;
            RemoveLoop();
            double total = _clock != null ? _clock.Elapsed.TotalSeconds : 0;
            double shoot = _firstShotSec >= 0 ? total - _firstShotSec : 0;
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · " + why + " · " + _saved + "장 @" + _fps
                + " · 찍는 데 실제 " + shoot.ToString("0.0") + "초 (실제 " + (shoot > 0 ? (_saved / shoot).ToString("0.0") : "-") + " fps)"
                + " · 데움 포함 " + total.ToString("0.0") + "초 · screen " + Screen.width + "x" + Screen.height;
            Debug.Log(LogTag + "끝 · " + line);
            if (_outDir != null)
            {
                File.AppendAllText(Path.Combine(_outDir, LogFileName), line + Environment.NewLine);
            }

            _driver = null;
            _driverStack.Clear();
            if (_exitWhenDone && EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
            }
        }

        private static void InstallLoop()
        {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            RemoveFrom(ref root);
            PlayerLoopSystem[] top = root.subSystemList;
            for (int i = 0; i < top.Length; i++)
            {
                if (top[i].type != typeof(PostLateUpdate))
                {
                    continue;
                }

                List<PlayerLoopSystem> subs = new List<PlayerLoopSystem>(top[i].subSystemList);
                subs.Add(new PlayerLoopSystem { type = typeof(VideoCapture), updateDelegate = Tick });
                top[i].subSystemList = subs.ToArray();
            }

            root.subSystemList = top;
            PlayerLoop.SetPlayerLoop(root);
        }

        private static void RemoveLoop()
        {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveFrom(ref root))
            {
                PlayerLoop.SetPlayerLoop(root);
            }
        }

        private static bool RemoveFrom(ref PlayerLoopSystem root)
        {
            bool removed = false;
            PlayerLoopSystem[] top = root.subSystemList;
            if (top == null)
            {
                return false;
            }

            for (int i = 0; i < top.Length; i++)
            {
                if (top[i].subSystemList == null)
                {
                    continue;
                }

                List<PlayerLoopSystem> subs = new List<PlayerLoopSystem>(top[i].subSystemList);
                if (subs.RemoveAll(s => s.type == typeof(VideoCapture)) > 0)
                {
                    top[i].subSystemList = subs.ToArray();
                    removed = true;
                }
            }

            root.subSystemList = top;
            return removed;
        }
    }
}
#endif

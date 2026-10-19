using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mozzi.Editor
{
    // 원격 테스트용 윈도우 64비트 빌드. 저장소 뿌리 Build/windows 에 exe 폴더와 그 zip 을 낸다 (gitignore).
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.WindowsBuild.SwitchPlatform();` 을 먼저 한 번,
    // 전환이 끝난 뒤 `Mozzi.Editor.WindowsBuild.Build();` 한 줄. 판을 올릴 때는 BuildVersion.cs 만 고친다.
    public static class WindowsBuild
    {
        private const string DebugDefine = "MOZZI_DEBUG";
        private const string ScenePath = "Assets/_Project/Scenes/Farm.unity";
        private const string OutputFolderRelative = "../../Build/windows";
        private const string BuildFolderFormat = "ProjectMozzi-windows-v{0}";
        private const string ExeName = "ProjectMozzi.exe";
        private const int WindowWidth = 540;
        private const int WindowHeight = 960;

        // Unity 가 exe 옆에 만드는 배포 금지 폴더의 이름 끝. zip 에서 뺀다.
        // Burst 디버그 정보와 IL2CPP 백업 폴더다. 게임 실행에는 필요 없고 크기만 크다.
        private static readonly string[] DoNotShipSuffixes = new string[]
        {
            "_BurstDebugInformation_DoNotShip",
            "_BackUpThisFolder_ButDontShipItWithYourGame",
        };

        // 플랫폼을 윈도우 Standalone 으로 바꾼다. 전체 재임포트와 스크립트 다시 컴파일이 따라온다.
        public static void SwitchPlatform()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            {
                Debug.Log("[WindowsBuild] 전환 이미 윈도우다");
                return;
            }

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            Debug.Log("[WindowsBuild] 전환 " + switched + " · 지금 " + EditorUserBuildSettings.activeBuildTarget);
        }

        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[WindowsBuild] Play 모드라 빌드하지 않는다. Play 를 끄고 다시 부른다.");
                return;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError("[WindowsBuild] 이미 빌드가 도는 중이라 빌드하지 않는다.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                Debug.LogError("[WindowsBuild] 플랫폼이 윈도우가 아니다. SwitchPlatform() 을 먼저 부른다.");
                return;
            }

            // 디버그 패널 정의만 빌드용이라 finally 에서 되돌린다. 나머지 설정은 윈도우 판의 정해진 값이라 남긴다.
            string savedDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            string outputFolder = Path.GetFullPath(Path.Combine(Application.dataPath, OutputFolderRelative));
            string buildFolderName = string.Format(BuildFolderFormat, BuildVersion.Version);
            string buildFolder = Path.Combine(outputFolder, buildFolderName);
            string exePath = Path.Combine(buildFolder, ExeName);
            string zipPath = Path.Combine(outputFolder, buildFolderName + ".zip");

            try
            {
                ApplySettings();
                PrepareBuildFolder(buildFolder);

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new string[] { ScenePath };
                options.target = BuildTarget.StandaloneWindows64;
                options.targetGroup = BuildTargetGroup.Standalone;
                options.locationPathName = exePath;
                options.options = BuildOptions.None;

                Debug.Log("[WindowsBuild] 시작 → " + exePath);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                LogResult(report);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    CreateZip(buildFolder, zipPath);
                    LogFileSize(zipPath);
                }
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, savedDefines);
                AssetDatabase.SaveAssets();
                Debug.Log("[WindowsBuild] 설정 원복 · 정의 " + savedDefines);
            }
        }

        private static void ApplySettings()
        {
            AddDefine(NamedBuildTarget.Standalone, DebugDefine);

            // productName 은 건드리지 않는다. 세이브 경로(persistentDataPath)가 이 이름을 따라가기 때문이다.
            PlayerSettings.bundleVersion = BuildVersion.Version;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            // 폰 화면 비율의 세로 창으로 띄우고, 창 크기는 바꿀 수 있게 둔다. 창이 뒤로 가도 시간은 흐른다.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = WindowWidth;
            PlayerSettings.defaultScreenHeight = WindowHeight;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            EditorUserBuildSettings.development = false;

            Debug.Log("[WindowsBuild] 설정 · 판 " + PlayerSettings.bundleVersion
                + " · " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)
                + " · 창 " + PlayerSettings.defaultScreenWidth + "x" + PlayerSettings.defaultScreenHeight
                + " " + PlayerSettings.fullScreenMode
                + " · 정의 " + PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone));
        }

        // 지난 빌드의 찌꺼기가 zip 에 섞이지 않게 같은 판 폴더를 비우고 새로 만든다.
        private static void PrepareBuildFolder(string buildFolder)
        {
            if (Directory.Exists(buildFolder))
            {
                Directory.Delete(buildFolder, true);
                Debug.Log("[WindowsBuild] 지난 폴더 비움 → " + buildFolder);
            }

            Directory.CreateDirectory(buildFolder);
        }

        // exe 폴더를 zip 으로 묶는다. zip 을 풀면 폴더 하나가 나오도록 폴더 이름을 앞에 붙인다.
        // ZipFile.CreateFromDirectory 는 하위 폴더를 거를 수 없어서 파일을 하나씩 넣는다.
        private static void CreateZip(string buildFolder, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            string rootName = Path.GetFileName(buildFolder);
            int fileCount = 0;
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                string[] files = Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    // 에디터 코드는 .NET Framework 4.8 API 라 Path.GetRelativePath 가 없다. 앞부분을 잘라 낸다.
                    string relative = files[i].Substring(buildFolder.Length).TrimStart('\\', '/');
                    if (IsDoNotShip(relative))
                    {
                        continue;
                    }

                    string entryName = rootName + "/" + relative.Replace('\\', '/');
                    archive.CreateEntryFromFile(files[i], entryName, System.IO.Compression.CompressionLevel.Optimal);
                    fileCount++;
                }
            }

            Debug.Log("[WindowsBuild] zip 묶음 · 파일 " + fileCount + "개 → " + zipPath);
        }

        // 상대 경로의 첫 폴더가 배포 금지 폴더인지 본다.
        private static bool IsDoNotShip(string relativePath)
        {
            string firstPart = relativePath.Split(new char[] { '\\', '/' }, 2)[0];
            for (int i = 0; i < DoNotShipSuffixes.Length; i++)
            {
                if (firstPart.EndsWith(DoNotShipSuffixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // 기존 정의는 남기고 없을 때만 더한다.
        private static void AddDefine(NamedBuildTarget target, string define)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            List<string> defines = new List<string>(current.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            if (defines.Contains(define))
            {
                return;
            }

            defines.Add(define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }

        private static void LogResult(BuildReport report)
        {
            BuildSummary summary = report.summary;
            string line = "[WindowsBuild] 결과 " + summary.result
                + " · 크기 " + (summary.totalSize / (1024.0 * 1024.0)).ToString("F1") + " MB"
                + " · 걸린 시간 " + summary.totalTime.TotalSeconds.ToString("F0") + "초"
                + " · 오류 " + summary.totalErrors + " · 경고 " + summary.totalWarnings;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(line);
                return;
            }

            Debug.LogError(line);
            LogErrors(report);
        }

        // 리포트의 totalSize 는 실제 파일 크기와 다를 수 있다. 떨어진 zip 을 직접 잰다.
        private static void LogFileSize(string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                Debug.LogWarning("[WindowsBuild] zip 이 없다 → " + outputPath);
                return;
            }

            long length = new FileInfo(outputPath).Length;
            Debug.Log("[WindowsBuild] 파일 " + Path.GetFileName(outputPath) + " · " + length + " 바이트 · "
                + (length / (1024.0 * 1024.0)).ToString("F2") + " MB");
        }

        private static void LogErrors(BuildReport report)
        {
            BuildStep[] steps = report.steps;
            for (int i = 0; i < steps.Length; i++)
            {
                BuildStepMessage[] messages = steps[i].messages;
                for (int j = 0; j < messages.Length; j++)
                {
                    if (messages[j].type != LogType.Error && messages[j].type != LogType.Exception)
                    {
                        continue;
                    }

                    Debug.LogError("[WindowsBuild] " + steps[i].name + " : " + messages[j].content);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mozzi.Editor
{
    // 원격 테스트용 안드로이드 APK 빌드. 저장소 뿌리 Build/itch 에 낸다 (gitignore).
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.AndroidBuild.SwitchPlatform();` 을 먼저 한 번,
    // 전환이 끝난 뒤 `Mozzi.Editor.AndroidBuild.Build();` 한 줄. 판을 올릴 때는 Version 만 고친다.
    public static class AndroidBuild
    {
        private const int Version = 14;
        private const string PackageName = "com.mirusona.projectmozzi";
        private const string DebugDefine = "MOZZI_DEBUG";
        private const string ScenePath = "Assets/_Project/Scenes/Farm.unity";
        private const string OutputFolderRelative = "../../Build/itch";
        private const string ApkNameFormat = "ProjectMozzi-android-v{0}.apk";

        // 플랫폼을 안드로이드로 바꾼다. 전체 재임포트와 스크립트 다시 컴파일이 따라온다.
        public static void SwitchPlatform()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[AndroidBuild] 전환 이미 안드로이드다");
                return;
            }

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("[AndroidBuild] 전환 " + switched + " · 지금 " + EditorUserBuildSettings.activeBuildTarget);
        }

        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[AndroidBuild] Play 모드라 빌드하지 않는다. Play 를 끄고 다시 부른다.");
                return;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError("[AndroidBuild] 이미 빌드가 도는 중이라 빌드하지 않는다.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("[AndroidBuild] 플랫폼이 안드로이드가 아니다. SwitchPlatform() 을 먼저 부른다.");
                return;
            }

            // 디버그 패널 정의만 빌드용이라 finally 에서 되돌린다. 나머지 설정은 안드로이드 판의 정해진 값이라 남긴다.
            string savedDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            string outputFolder = Path.GetFullPath(Path.Combine(Application.dataPath, OutputFolderRelative));
            string outputPath = Path.Combine(outputFolder, string.Format(ApkNameFormat, Version));

            try
            {
                ApplySettings();
                Directory.CreateDirectory(outputFolder);

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new string[] { ScenePath };
                options.target = BuildTarget.Android;
                options.targetGroup = BuildTargetGroup.Android;
                options.locationPathName = outputPath;
                options.options = BuildOptions.None;

                Debug.Log("[AndroidBuild] 시작 → " + outputPath);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                LogResult(report);
                LogFileSize(outputPath);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, savedDefines);
                AssetDatabase.SaveAssets();
                Debug.Log("[AndroidBuild] 설정 원복 · 정의 " + savedDefines);
            }
        }

        private static void ApplySettings()
        {
            AddDefine(NamedBuildTarget.Android, DebugDefine);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.bundleVersion = Version.ToString();
            PlayerSettings.Android.bundleVersionCode = Version;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // 커스텀 키스토어가 없으면 Unity 가 디버그 키로 서명한다.
            PlayerSettings.Android.useCustomKeystore = false;
            // aab 가 아니라 바로 깔 수 있는 apk 로 낸다. 개발 빌드는 끈다.
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;

            Debug.Log("[AndroidBuild] 설정 · 패키지 " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                + " · 판 " + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + ")"
                + " · " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)
                + " · " + PlayerSettings.Android.targetArchitectures
                + " · 최소 API " + PlayerSettings.Android.minSdkVersion
                + " · 정의 " + PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android));
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
            string line = "[AndroidBuild] 결과 " + summary.result
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

        // 리포트의 totalSize 는 실제 파일 크기와 다를 수 있다. 떨어진 apk 를 직접 잰다.
        private static void LogFileSize(string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                Debug.LogWarning("[AndroidBuild] apk 가 없다 → " + outputPath);
                return;
            }

            long length = new FileInfo(outputPath).Length;
            Debug.Log("[AndroidBuild] 파일 " + Path.GetFileName(outputPath) + " · " + length + " 바이트 · "
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

                    Debug.LogError("[AndroidBuild] " + steps[i].name + " : " + messages[j].content);
                }
            }
        }
    }
}

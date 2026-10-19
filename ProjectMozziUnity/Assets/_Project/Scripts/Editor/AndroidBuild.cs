using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mozzi.Editor
{
    // 원격 테스트용 안드로이드 APK 빌드. 저장소 뿌리 Build/android 에 낸다 (gitignore).
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.AndroidBuild.SwitchPlatform();` 을 먼저 한 번,
    // 전환이 끝난 뒤 `Mozzi.Editor.AndroidBuild.Build();` 한 줄. 판을 올릴 때는 BuildVersion.cs 만 고친다.
    public static class AndroidBuild
    {
        private const string Tag = "[AndroidBuild] ";
        private const string PackageName = "com.mirusona.projectmozzi";
        private const string OutputFolderRelative = "../../Build/android";
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
            if (BuildUtil.CanBuild(Tag, BuildTarget.Android, "안드로이드가") == false)
            {
                return;
            }

            // 디버그 패널 정의만 빌드용이라 finally 에서 되돌린다. 나머지 설정은 안드로이드 판의 정해진 값이라 남긴다.
            string savedDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            string outputFolder = Path.GetFullPath(Path.Combine(Application.dataPath, OutputFolderRelative));
            string outputPath = Path.Combine(outputFolder, string.Format(ApkNameFormat, BuildVersion.Version));

            try
            {
                ApplySettings();
                Directory.CreateDirectory(outputFolder);

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new string[] { BuildUtil.ScenePath };
                options.target = BuildTarget.Android;
                options.targetGroup = BuildTargetGroup.Android;
                options.locationPathName = outputPath;
                options.options = BuildOptions.None;

                Debug.Log("[AndroidBuild] 시작 → " + outputPath);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildUtil.LogResult(Tag, report);
                BuildUtil.LogFileSize(Tag, outputPath);
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
            BuildUtil.AddDefine(NamedBuildTarget.Android, BuildUtil.DebugDefine);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.bundleVersion = BuildVersion.Version;
            PlayerSettings.Android.bundleVersionCode = BuildVersion.VersionCode;
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
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mozzi.Editor
{
    // 원격 테스트용 WebGL 빌드. 저장소 뿌리 Build/WebGL 에 낸다 (gitignore).
    // 부르는 법 : unity command eval_file 에 `Mozzi.Editor.WebGlBuild.Build();` 한 줄.
    public static class WebGlBuild
    {
        private const string DebugDefine = "MOZZI_DEBUG";
        private const string ScenePath = "Assets/_Project/Scenes/Farm.unity";
        private const string OutputRelative = "../../Build/WebGL";
        private const string MinimalTemplate = "APPLICATION:Minimal";
        private const string DefaultTemplate = "APPLICATION:Default";
        private const string MinimalTemplateFolder = "PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Minimal";
        private const int ScreenWidth = 405;
        private const int ScreenHeight = 720;

        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[WebGlBuild] Play 모드라 빌드하지 않는다. Play 를 끄고 다시 부른다.");
                return;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError("[WebGlBuild] 이미 빌드가 도는 중이라 빌드하지 않는다.");
                return;
            }

            // 빌드용으로만 바꾸는 값이다. finally 에서 되돌려 ProjectSettings 를 더럽히지 않는다.
            string savedDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            string savedTemplate = PlayerSettings.WebGL.template;
            int savedWidth = PlayerSettings.defaultWebScreenWidth;
            int savedHeight = PlayerSettings.defaultWebScreenHeight;
            bool savedRunInBackground = PlayerSettings.runInBackground;

            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, OutputRelative));

            try
            {
                ApplySettings();

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new string[] { ScenePath };
                options.target = BuildTarget.WebGL;
                options.locationPathName = outputPath;
                options.options = BuildOptions.None;

                Debug.Log("[WebGlBuild] 시작 → " + outputPath);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                LogResult(report);
                LogFileSizes(outputPath);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, savedDefines);
                PlayerSettings.WebGL.template = savedTemplate;
                PlayerSettings.defaultWebScreenWidth = savedWidth;
                PlayerSettings.defaultWebScreenHeight = savedHeight;
                PlayerSettings.runInBackground = savedRunInBackground;
                AssetDatabase.SaveAssets();
                Debug.Log("[WebGlBuild] 설정 원복 · 정의 " + savedDefines
                    + " · 템플릿 " + savedTemplate
                    + " · 화면 " + savedWidth + "x" + savedHeight);
            }
        }

        private static void ApplySettings()
        {
            AddDefine(NamedBuildTarget.WebGL, DebugDefine);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            // 같은 판에서 이 한 줄만 바꿔 재니 wasm 이 8.78MB(DiskSize) 대 9.18MB(RuntimeSpeed) 다.
            // 받는 양이 급해 DiskSize 를 쓴다. 런타임 속도가 문제되면 여기만 바꾼다.
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
            // 폰에서 받다가 끊기는 일이 있어 브라우저 캐시(IndexedDB)에 다시 쌓는 부담을 뺀다.
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.template = PickTemplate();
            PlayerSettings.defaultWebScreenWidth = ScreenWidth;
            PlayerSettings.defaultWebScreenHeight = ScreenHeight;
            PlayerSettings.runInBackground = true;

            Debug.Log("[WebGlBuild] 설정 · 정의 " + PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL)
                + " · 템플릿 " + PlayerSettings.WebGL.template
                + " · 화면 " + ScreenWidth + "x" + ScreenHeight);
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

        // 에디터 설치 폴더에 Minimal 템플릿이 있으면 그것, 없으면 Default.
        private static string PickTemplate()
        {
            string folder = Path.Combine(EditorApplication.applicationContentsPath, MinimalTemplateFolder);
            if (Directory.Exists(folder))
            {
                return MinimalTemplate;
            }

            return DefaultTemplate;
        }

        private static void LogResult(BuildReport report)
        {
            BuildSummary summary = report.summary;
            string line = "[WebGlBuild] 결과 " + summary.result
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

        // 리포트의 totalSize 는 실제 받는 양과 다르다. 떨어진 파일을 직접 잰다.
        private static void LogFileSizes(string outputPath)
        {
            string buildFolder = Path.Combine(outputPath, "Build");
            if (!Directory.Exists(buildFolder))
            {
                Debug.LogWarning("[WebGlBuild] 크기를 잴 폴더가 없다 → " + buildFolder);
                return;
            }

            string[] files = Directory.GetFiles(buildFolder);
            Array.Sort(files);
            long total = 0;
            for (int i = 0; i < files.Length; i++)
            {
                long length = new FileInfo(files[i]).Length;
                total += length;
                Debug.Log("[WebGlBuild] 파일 " + Path.GetFileName(files[i]) + " · " + length + " 바이트");
            }

            Debug.Log("[WebGlBuild] 받는 양 합계 " + total + " 바이트 · "
                + (total / (1024.0 * 1024.0)).ToString("F2") + " MB");
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

                    Debug.LogError("[WebGlBuild] " + steps[i].name + " : " + messages[j].content);
                }
            }
        }
    }
}

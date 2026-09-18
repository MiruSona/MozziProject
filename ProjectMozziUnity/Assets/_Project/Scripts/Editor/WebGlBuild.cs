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
            ApplySettings();

            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, OutputRelative));
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new string[] { ScenePath };
            options.target = BuildTarget.WebGL;
            options.locationPathName = outputPath;
            options.options = BuildOptions.None;

            Debug.Log("[WebGlBuild] 시작 → " + outputPath);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            LogResult(report);
        }

        private static void ApplySettings()
        {
            AddDefine(NamedBuildTarget.WebGL, DebugDefine);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
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

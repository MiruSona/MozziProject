using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mozzi.Editor
{
    // 안드로이드 · 윈도우 · WebGL 빌드가 같이 쓰는 막기 검사 · 정의 더하기 · 결과 로그.
    // tag 는 로그 앞머리다 (예 : "[AndroidBuild] ").
    public static class BuildUtil
    {
        // 프로토 동안은 Release 판에도 디버그 패널을 넣는다.
        public const string DebugDefine = "MOZZI_DEBUG";
        public const string ScenePath = "Assets/_Project/Scenes/Farm.unity";

        // Play 중이거나 다른 빌드가 도는 중이면 false.
        public static bool CanBuild(string tag)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError(tag + "Play 모드라 빌드하지 않는다. Play 를 끄고 다시 부른다.");
                return false;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError(tag + "이미 빌드가 도는 중이라 빌드하지 않는다.");
                return false;
            }

            return true;
        }

        // CanBuild 에 더해 지금 플랫폼이 target 인지 본다. 아니면 SwitchPlatform() 을 먼저 불러야 한다.
        public static bool CanBuild(string tag, BuildTarget target, string platformName)
        {
            if (CanBuild(tag) == false)
            {
                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.LogError(tag + "플랫폼이 " + platformName + " 아니다. SwitchPlatform() 을 먼저 부른다.");
                return false;
            }

            return true;
        }

        // 기존 정의는 남기고 없을 때만 더한다.
        public static void AddDefine(NamedBuildTarget target, string define)
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

        public static void LogResult(string tag, BuildReport report)
        {
            BuildSummary summary = report.summary;
            string line = tag + "결과 " + summary.result
                + " · 크기 " + (summary.totalSize / (1024.0 * 1024.0)).ToString("F1") + " MB"
                + " · 걸린 시간 " + summary.totalTime.TotalSeconds.ToString("F0") + "초"
                + " · 오류 " + summary.totalErrors + " · 경고 " + summary.totalWarnings;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(line);
                return;
            }

            Debug.LogError(line);
            LogErrors(tag, report);
        }

        // 리포트의 totalSize 는 실제 파일 크기와 다를 수 있다. 떨어진 파일을 직접 잰다.
        public static void LogFileSize(string tag, string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                Debug.LogWarning(tag + "파일이 없다 → " + outputPath);
                return;
            }

            long length = new FileInfo(outputPath).Length;
            Debug.Log(tag + "파일 " + Path.GetFileName(outputPath) + " · " + length + " 바이트 · "
                + (length / (1024.0 * 1024.0)).ToString("F2") + " MB");
        }

        private static void LogErrors(string tag, BuildReport report)
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

                    Debug.LogError(tag + steps[i].name + " : " + messages[j].content);
                }
            }
        }
    }
}

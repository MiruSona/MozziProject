using System.Diagnostics;
using System.IO;
using System.Text;
using Mozzi.Data;
using UnityEditor;
using UnityEngine;

namespace Mozzi.EditorTools
{
    // GameData/*.json 을 datatool 로 검사하고 gamedata.bytes · 생성 코드를 다시 만든다.
    // 자리와 명령은 Docs/Guide/개발환경.md 를 본다.
    public static class GameDataMenu
    {
        private const string DataToolRelPath = "Tools/DataTool/bin/datatool.exe";
        private const string DataFolderName = "GameData";

        [MenuItem("Mozzi/Data/Validate+Export+Gen")]
        public static void ValidateExportGen()
        {
            // export · gen 도 안에서 validate 를 먼저 돌리지만, 무엇이 걸렸는지 먼저 보려고 따로 부른다.
            if (Run("validate") == false) { return; }
            if (Run("export") == false) { return; }
            if (Run("gen") == false) { return; }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[GameData] 검사 · 굽기 · 코드 생성 끝");
        }

        [MenuItem("Mozzi/Data/Validate")]
        public static void ValidateOnly()
        {
            Run("validate");
        }

        // 구운 파일을 실제 로더로 읽어 표마다 행 수를 찍는다. Play 를 안 켜고 확인하는 용도다.
        [MenuItem("Mozzi/Data/구운 표 읽어 보기")]
        public static void LogBakedCounts()
        {
            UnityEngine.Debug.Log("[GameData] " + BakedCounts());
        }

        public static string BakedCounts()
        {
            GameDataTables tables = StaticDataManager.LoadBaked();
            return "MozziSpecies " + tables.MozziSpeciesRows.Count
                + " · Ingredient " + tables.IngredientRows.Count
                + " · Recipe " + tables.RecipeRows.Count
                + " · SynthesisRule " + tables.SynthesisRuleRows.Count
                + " · Growth " + tables.GrowthRows.Count
                + " · Stall " + tables.StallRows.Count
                + " · Farm " + tables.FarmRows.Count
                + " (구운 시각 " + tables.BuiltAt + ")";
        }

        // datatool 한 번 돌리고 출력을 콘솔에 옮긴다. 종료 코드가 0 이 아니면 false.
        private static bool Run(string command)
        {
            string unityRoot = Path.GetDirectoryName(Application.dataPath);
            string repoRoot = Path.GetDirectoryName(unityRoot);
            string exe = Path.Combine(repoRoot, DataToolRelPath);
            if (File.Exists(exe) == false)
            {
                UnityEngine.Debug.LogError("[GameData] datatool 이 없다 : " + exe + " — Tools/setup.ps1 로 만든다");
                return false;
            }

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = exe;
            info.Arguments = command + " --data \"" + Path.Combine(unityRoot, DataFolderName) + "\"";
            info.WorkingDirectory = unityRoot;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.StandardOutputEncoding = Encoding.UTF8;
            info.StandardErrorEncoding = Encoding.UTF8;

            using (Process process = Process.Start(info))
            {
                // stderr 는 비동기로 받아야 두 관이 서로 막히지 않는다.
                StringBuilder errors = new StringBuilder();
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { errors.AppendLine(e.Data); };
                process.BeginErrorReadLine();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string text = "[GameData] datatool " + command + " (종료 " + process.ExitCode + ")\n" + output + errors;
                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError(text);
                    return false;
                }

                UnityEngine.Debug.Log(text);
                return true;
            }
        }
    }
}

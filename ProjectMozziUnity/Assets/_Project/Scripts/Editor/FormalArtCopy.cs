using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mozzi.Editor
{
    // 정식 그림(Docs/Reference/Art/정식/*/out/)을 Resources/Art/ 로 대응표대로 복사한다. 그림이 바뀌면 메뉴를 다시 돌리면 된다.
    // 화면별로 따로 돈다 — 가판대 · 합성소는 그 화면을 다시 짓는 판에서 들인다 (옛 배치가 새 그림 크기에 깨지지 않게).
    // 부르는 법 : 메뉴 「Mozzi/정식 그림 들이기/…」 또는 eval 로 FormalArtCopy.CopyKitchen() 처럼.
    public static class FormalArtCopy
    {
        private const string ArtRoot = "Assets/_Project/Resources/Art/";

        // { 정식 폴더 안 out 경로, Resources/Art 아래 경로 } (확장자 .png 생략).
        private static readonly string[,] KitchenTable =
        {
            { "주방/out/kitchen_bg", "Screen/kitchen_bg" },
            { "주방/out/kitchen_tray", "Screen/kitchen_tray" },
            { "주방/out/kitchen_plate", "Screen/kitchen_plate" },
            { "주방/out/kitchen_pound_0", "Screen/kitchen_pound_0" },
            { "주방/out/kitchen_pound_1", "Screen/kitchen_pound_1" },
            { "주방/out/kitchen_pound_2", "Screen/kitchen_pound_2" },
            { "주방/out/kitchen_pound_3", "Screen/kitchen_pound_3" },
            { "주방/out/kitchen_pound_4", "Screen/kitchen_pound_4" },
            { "주방/out/kitchen_pound_5", "Screen/kitchen_pound_5" },
            { "주방/out/kitchen_pound_6", "Screen/kitchen_pound_6" },
        };

        private static readonly string[,] UiTable =
        {
            { "가판대/out/ui_button", "UI/ui_button" },
            { "가판대/out/ui_bubble", "UI/ui_bubble" },
        };

        // 가판대 그림. 옛 stall_bg(360×800) · 옛 손님(48×64 정면)은 가판대만 썼으므로 같은 이름에 덮는다.
        // 손님은 3차 옆모습 겹을 옛 파일 이름으로 바꿔 넣어 CustomerArtKeys 를 안 고친다. 색은 구운 판(코드는 색을 곱하지 않는다).
        private static readonly string[,] StallTable =
        {
            { "가판대/out/stall_bg_b", "Screen/stall_bg" },
            { "가판대/out/stall_counter3_brown", "Screen/stall_counter" },
            { "가판대/out/owner_v2", "Screen/stall_owner" },
            { "가판대/out/display_case2", "Screen/display_case" },
            { "가판대/out/prop_vase", "Screen/stall_prop_vase" },
            { "가판대/out/prop_register", "Screen/stall_prop_register" },
            { "가판대/out/prop_menu", "Screen/stall_prop_menu" },
            { "가판대/out/board_topper", "Screen/stall_board_topper" },
            { "가판대/out/ui_bubble_c", "UI/ui_bubble_c" },
            { "가판대/out/cust3_side_body", "Customer/body" },
            { "가판대/out/cust3_side_face_01", "Customer/face_01" },
            { "가판대/out/cust3_side_face_02", "Customer/face_02" },
            { "가판대/out/cust3_side_face_03", "Customer/face_03" },
            { "가판대/out/cust3_side_hair_01_black", "Customer/hair_01_black" },
            { "가판대/out/cust3_side_hair_01_brown", "Customer/hair_01_brown" },
            { "가판대/out/cust3_side_hair_01_orange", "Customer/hair_01_orange" },
            { "가판대/out/cust3_side_hair_01_purple", "Customer/hair_01_purple" },
            { "가판대/out/cust3_side_hair_02_black", "Customer/hair_02_black" },
            { "가판대/out/cust3_side_hair_02_brown", "Customer/hair_02_brown" },
            { "가판대/out/cust3_side_hair_02_orange", "Customer/hair_02_orange" },
            { "가판대/out/cust3_side_hair_02_purple", "Customer/hair_02_purple" },
            { "가판대/out/cust3_side_shirt_blue", "Customer/shirt_blue" },
            { "가판대/out/cust3_side_shirt_green", "Customer/shirt_green" },
            { "가판대/out/cust3_side_shirt_red", "Customer/shirt_red" },
            { "가판대/out/cust3_side_shirt_yellow", "Customer/shirt_yellow" },
        };

        private static readonly string[,] SynthesisTable =
        {
            { "합성소/out/synthesis_bg", "Screen/synthesis_bg" },
            { "합성소/out/synthesis_machine", "Screen/synthesis_machine" },
            { "합성소/out/synthesis_machine_run_0", "Screen/synthesis_machine_run_0" },
            { "합성소/out/synthesis_machine_run_1", "Screen/synthesis_machine_run_1" },
            { "합성소/out/synthesis_machine_run_2", "Screen/synthesis_machine_run_2" },
            { "합성소/out/synthesis_machine_run_3", "Screen/synthesis_machine_run_3" },
            { "합성소/out/synthesis_machine_run_4", "Screen/synthesis_machine_run_4" },
            { "합성소/out/synthesis_machine_run_5", "Screen/synthesis_machine_run_5" },
            { "합성소/out/synthesis_machine_run_6", "Screen/synthesis_machine_run_6" },
            { "합성소/out/synthesis_machine_run_7", "Screen/synthesis_machine_run_7" },
            { "합성소/out/synthesis_machine_done", "Screen/synthesis_machine_done" },
            { "합성소/out/synthesis_slot_buy", "Screen/synthesis_slot_buy" },
            { "합성소/out/synthesis_slot_locked", "Screen/synthesis_slot_locked" },
            { "합성소/out/synthesis_plate", "Screen/synthesis_plate" },
        };

        [MenuItem("Mozzi/정식 그림 들이기/주방 + UI 틀")]
        public static void CopyKitchen()
        {
            Copy(KitchenTable);
            Copy(UiTable);
            AssetDatabase.Refresh();
        }

        [MenuItem("Mozzi/정식 그림 들이기/가판대")]
        public static void CopyStall()
        {
            Copy(StallTable);
            Copy(UiTable);
            AssetDatabase.Refresh();
        }

        [MenuItem("Mozzi/정식 그림 들이기/합성소 + UI 틀")]
        public static void CopySynthesis()
        {
            Copy(SynthesisTable);
            Copy(UiTable);
            AssetDatabase.Refresh();
        }

        // 원본이 없으면 그 줄만 건너뛰고 알린다. 같은 내용이면 덮지 않아 다시 임포트가 안 일어난다.
        private static void Copy(string[,] table)
        {
            string formalRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Reference/Art/정식"));
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            int copied = 0;
            for (int i = 0; i < table.GetLength(0); i++)
            {
                string from = Path.Combine(formalRoot, table[i, 0] + ".png");
                string to = Path.Combine(projectRoot, ArtRoot + table[i, 1] + ".png");
                if (File.Exists(from) == false)
                {
                    Debug.LogError("[FormalArtCopy] 원본이 없다 : " + from);
                    continue;
                }

                if (File.Exists(to) && SameBytes(from, to))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Copy(from, to, true);
                copied++;
            }

            Debug.Log("[FormalArtCopy] " + copied + "장 복사 (표 " + table.GetLength(0) + "줄)");
        }

        private static bool SameBytes(string a, string b)
        {
            byte[] left = File.ReadAllBytes(a);
            byte[] right = File.ReadAllBytes(b);
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

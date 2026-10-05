using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mozzi.Editor
{
    // 정식 그림(Docs/Reference/Art/정식/*/out/)을 Resources/Art/ 로 대응표대로 복사한다. 그림이 바뀌면 메뉴를 다시 돌리면 된다.
    // UI 틀 표에는 버튼 · 말풍선 틀과 농장 건물 알림 표식이 들어 있어 어느 메뉴로 돌려도 같이 들어온다.
    // 화면별로 따로 돈다 — 가판대 · 합성소는 그 화면을 다시 짓는 판에서 들인다 (옛 배치가 새 그림 크기에 깨지지 않게).
    // 부르는 법 : 메뉴 「Mozzi/정식 그림 들이기/…」 또는 eval 로 FormalArtCopy.CopyKitchen() 처럼.
    public static class FormalArtCopy
    {
        private const string ArtRoot = "Assets/_Project/Resources/Art/";

        // { 정식 폴더 안 out 경로, Resources/Art 아래 경로 } (확장자 .png 생략).
        // 반복 07 : 정본은 주방/v3/out (README 「코드에 넘길 것」). 접시만 v3 에 없어 옛 주방/out 것을 쓴다.
        private static readonly string[,] KitchenTable =
        {
            { "주방/v3/out/kitchen_bg", "Screen/kitchen_bg" },
            { "주방/out/kitchen_plate", "Screen/kitchen_plate" },
            { "주방/v3/out/kitchen_drawer_frame", "Screen/kitchen_drawer_frame" },
            { "주방/v3/out/kitchen_drawer_cell", "Screen/kitchen_drawer_cell" },
            { "주방/v3/out/kitchen_bowl", "Screen/kitchen_bowl" },
            { "주방/v3/out/kitchen_steamer", "Screen/kitchen_steamer" },
            { "주방/v3/out/kitchen_board", "Screen/kitchen_board" },
            { "주방/v3/out/kitchen_pound_0", "Screen/kitchen_pound_0" },
            { "주방/v3/out/kitchen_pound_1", "Screen/kitchen_pound_1" },
            { "주방/v3/out/kitchen_pound_2", "Screen/kitchen_pound_2" },
            { "주방/v3/out/kitchen_pound_3", "Screen/kitchen_pound_3" },
            { "주방/v3/out/kitchen_pound_4", "Screen/kitchen_pound_4" },
            { "주방/v3/out/kitchen_pound_5", "Screen/kitchen_pound_5" },
            { "주방/v3/out/kitchen_pound_6", "Screen/kitchen_pound_6" },
            { "주방/v3/out/kitchen_pound_rough_0", "Screen/kitchen_pound_rough_0" },
            { "주방/v3/out/kitchen_pound_rough_1", "Screen/kitchen_pound_rough_1" },
            { "주방/v3/out/kitchen_pound_rough_2", "Screen/kitchen_pound_rough_2" },
            { "주방/v3/out/kitchen_pound_rough_3", "Screen/kitchen_pound_rough_3" },
            { "주방/v3/out/kitchen_pound_rough_4", "Screen/kitchen_pound_rough_4" },
            { "주방/v3/out/kitchen_pound_rough_5", "Screen/kitchen_pound_rough_5" },
            { "주방/v3/out/kitchen_pound_rough_6", "Screen/kitchen_pound_rough_6" },
            { "주방/v3/out/kitchen_pound_smooth_0", "Screen/kitchen_pound_smooth_0" },
            { "주방/v3/out/kitchen_pound_smooth_1", "Screen/kitchen_pound_smooth_1" },
            { "주방/v3/out/kitchen_pound_smooth_2", "Screen/kitchen_pound_smooth_2" },
            { "주방/v3/out/kitchen_pound_smooth_3", "Screen/kitchen_pound_smooth_3" },
            { "주방/v3/out/kitchen_pound_smooth_4", "Screen/kitchen_pound_smooth_4" },
            { "주방/v3/out/kitchen_pound_smooth_5", "Screen/kitchen_pound_smooth_5" },
            { "주방/v3/out/kitchen_pound_smooth_6", "Screen/kitchen_pound_smooth_6" },
            { "주방/v3/out/kitchen_shape_0", "Screen/kitchen_shape_0" },
            { "주방/v3/out/kitchen_shape_1", "Screen/kitchen_shape_1" },
            { "주방/v3/out/kitchen_shape_2", "Screen/kitchen_shape_2" },
            { "주방/v3/out/kitchen_steam_0", "Fx/kitchen_steam_0" },
            { "주방/v3/out/kitchen_steam_1", "Fx/kitchen_steam_1" },
            { "주방/v3/out/kitchen_steam_2", "Fx/kitchen_steam_2" },
            { "주방/v3/out/kitchen_steam_3", "Fx/kitchen_steam_3" },
        };

        private static readonly string[,] UiTable =
        {
            { "가판대/화풍통일/out/UI/ui_button", "UI/ui_button" },
            { "가판대/화풍통일/out/UI/ui_bubble", "UI/ui_bubble" },
            { "가판대/화풍통일/out/UI/ui_badge_alert", "UI/ui_badge_alert" },
        };

        // 가판대 그림. 선 색을 손질한 판(화풍통일/out)은 Resources 와 같은 폴더 · 같은 이름이고, 안 손댄 그림은 옛 out 에서 온다.
        // 손님 겹 색은 구운 판이다 (코드는 색을 곱하지 않는다).
        private static readonly string[,] StallTable =
        {
            { "가판대/out/stall_bg_b", "Screen/stall_bg" },
            { "가판대/화풍통일/out/Screen/stall_counter", "Screen/stall_counter" },
            { "가판대/화풍통일/out/Screen/stall_owner", "Screen/stall_owner" },
            { "가판대/화풍통일/out/Screen/display_case", "Screen/display_case" },
            { "가판대/화풍통일/out/Screen/stall_prop_vase", "Screen/stall_prop_vase" },
            { "가판대/out/prop_register", "Screen/stall_prop_register" },
            { "가판대/out/board_topper", "Screen/stall_board_topper" },
            { "가판대/화풍통일/out/UI/ui_bubble_c", "UI/ui_bubble_c" },
            { "가판대/화풍통일/out/Customer/body", "Customer/body" },
            { "가판대/화풍통일/out/Customer/face_01", "Customer/face_01" },
            { "가판대/화풍통일/out/Customer/face_02", "Customer/face_02" },
            { "가판대/화풍통일/out/Customer/face_03", "Customer/face_03" },
            { "가판대/화풍통일/out/Customer/hair_01_black", "Customer/hair_01_black" },
            { "가판대/화풍통일/out/Customer/hair_01_brown", "Customer/hair_01_brown" },
            { "가판대/화풍통일/out/Customer/hair_01_orange", "Customer/hair_01_orange" },
            { "가판대/화풍통일/out/Customer/hair_01_purple", "Customer/hair_01_purple" },
            { "가판대/화풍통일/out/Customer/hair_02_black", "Customer/hair_02_black" },
            { "가판대/화풍통일/out/Customer/hair_02_brown", "Customer/hair_02_brown" },
            { "가판대/화풍통일/out/Customer/hair_02_orange", "Customer/hair_02_orange" },
            { "가판대/화풍통일/out/Customer/hair_02_purple", "Customer/hair_02_purple" },
            { "가판대/화풍통일/out/Customer/shirt_blue", "Customer/shirt_blue" },
            { "가판대/화풍통일/out/Customer/shirt_green", "Customer/shirt_green" },
            { "가판대/화풍통일/out/Customer/shirt_red", "Customer/shirt_red" },
            { "가판대/화풍통일/out/Customer/shirt_yellow", "Customer/shirt_yellow" },
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
            { "합성소/화풍통일/out/synthesis_plate", "Screen/synthesis_plate" },
            { "합성소/화풍통일/out/Fx/fx_window_mask", "Fx/fx_window_mask" },
            // 창 안 반죽 소용돌이 세 겹 × 8장 (낱장). 섞임 단계 그림(fx_mix_*)은 안 쓰기로 해서 들이지 않는다.
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_0", "Fx/fx_swirl_a_0" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_1", "Fx/fx_swirl_a_1" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_2", "Fx/fx_swirl_a_2" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_3", "Fx/fx_swirl_a_3" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_4", "Fx/fx_swirl_a_4" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_5", "Fx/fx_swirl_a_5" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_6", "Fx/fx_swirl_a_6" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_a_7", "Fx/fx_swirl_a_7" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_0", "Fx/fx_swirl_b_0" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_1", "Fx/fx_swirl_b_1" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_2", "Fx/fx_swirl_b_2" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_3", "Fx/fx_swirl_b_3" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_4", "Fx/fx_swirl_b_4" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_5", "Fx/fx_swirl_b_5" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_6", "Fx/fx_swirl_b_6" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_b_7", "Fx/fx_swirl_b_7" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_0", "Fx/fx_swirl_top_0" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_1", "Fx/fx_swirl_top_1" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_2", "Fx/fx_swirl_top_2" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_3", "Fx/fx_swirl_top_3" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_4", "Fx/fx_swirl_top_4" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_5", "Fx/fx_swirl_top_5" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_6", "Fx/fx_swirl_top_6" },
            { "합성소/반죽소용돌이/out/Fx/frames/fx_swirl_top_7", "Fx/fx_swirl_top_7" },
        };

        // 농장 그림 가운데 손질한 판이 있는 것만 (찹쌀이 몸 넷 · 풀 · 꽃 장식 일곱).
        private static readonly string[,] FarmTable =
        {
            { "농장/화풍통일/out/body_hop0", "Mozzi/body_hop0" },
            { "농장/화풍통일/out/body_hop1", "Mozzi/body_hop1" },
            { "농장/화풍통일/out/body_hop2", "Mozzi/body_hop2" },
            { "농장/화풍통일/out/body_hop3", "Mozzi/body_hop3" },
            { "농장/화풍통일/out/decor_00", "Farm/decor_00" },
            { "농장/화풍통일/out/decor_01", "Farm/decor_01" },
            { "농장/화풍통일/out/decor_02", "Farm/decor_02" },
            { "농장/화풍통일/out/decor_03", "Farm/decor_03" },
            { "농장/화풍통일/out/decor_04", "Farm/decor_04" },
            { "농장/화풍통일/out/decor_05", "Farm/decor_05" },
            { "농장/화풍통일/out/decor_06", "Farm/decor_06" },
        };

        // 떡 11 · 재료 6 아이콘 (외곽선 넣고 캔버스 바닥에 붙인 판). 캔버스는 32 · 28 그대로라 쓰는 쪽 크기는 안 바뀐다.
        // 끝 11줄은 진열장에 쌓는 한 개짜리 떡 (22×16).
        private static readonly string[,] IconTable =
        {
            { "아이콘/out/plain_cake", "Cake/plain_cake" },
            { "아이콘/out/mugwort_cake", "Cake/mugwort_cake" },
            { "아이콘/out/sesame_cake", "Cake/sesame_cake" },
            { "아이콘/out/pinenut_cake", "Cake/pinenut_cake" },
            { "아이콘/out/jujube_cake", "Cake/jujube_cake" },
            { "아이콘/out/bean_cake", "Cake/bean_cake" },
            { "아이콘/out/redbean_cake", "Cake/redbean_cake" },
            { "아이콘/out/mugwort_sesame_cake", "Cake/mugwort_sesame_cake" },
            { "아이콘/out/pinenut_jujube_cake", "Cake/pinenut_jujube_cake" },
            { "아이콘/out/redbean_bean_cake", "Cake/redbean_bean_cake" },
            { "아이콘/out/redbean_mugwort_sesame_cake", "Cake/redbean_mugwort_sesame_cake" },
            { "아이콘/out/mugwort", "Ingredient/mugwort" },
            { "아이콘/out/sesame", "Ingredient/sesame" },
            { "아이콘/out/pinenut", "Ingredient/pinenut" },
            { "아이콘/out/jujube", "Ingredient/jujube" },
            { "아이콘/out/bean", "Ingredient/bean" },
            { "아이콘/out/redbean", "Ingredient/redbean" },
            { "아이콘/out/CakeOne/plain_cake_one", "CakeOne/plain_cake_one" },
            { "아이콘/out/CakeOne/mugwort_cake_one", "CakeOne/mugwort_cake_one" },
            { "아이콘/out/CakeOne/sesame_cake_one", "CakeOne/sesame_cake_one" },
            { "아이콘/out/CakeOne/pinenut_cake_one", "CakeOne/pinenut_cake_one" },
            { "아이콘/out/CakeOne/jujube_cake_one", "CakeOne/jujube_cake_one" },
            { "아이콘/out/CakeOne/bean_cake_one", "CakeOne/bean_cake_one" },
            { "아이콘/out/CakeOne/redbean_cake_one", "CakeOne/redbean_cake_one" },
            { "아이콘/out/CakeOne/mugwort_sesame_cake_one", "CakeOne/mugwort_sesame_cake_one" },
            { "아이콘/out/CakeOne/pinenut_jujube_cake_one", "CakeOne/pinenut_jujube_cake_one" },
            { "아이콘/out/CakeOne/redbean_bean_cake_one", "CakeOne/redbean_bean_cake_one" },
            { "아이콘/out/CakeOne/redbean_mugwort_sesame_cake_one", "CakeOne/redbean_mugwort_sesame_cake_one" },
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

        [MenuItem("Mozzi/정식 그림 들이기/농장 (찹쌀이 몸 · 장식) + UI 틀")]
        public static void CopyFarm()
        {
            Copy(FarmTable);
            Copy(UiTable);
            AssetDatabase.Refresh();
        }

        [MenuItem("Mozzi/정식 그림 들이기/떡 · 재료 아이콘")]
        public static void CopyIcons()
        {
            Copy(IconTable);
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

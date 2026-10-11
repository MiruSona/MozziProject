#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Game;
using Mozzi.Game.Clock;
using Mozzi.Game.Codex;
using Mozzi.Game.Farm;
using Mozzi.Game.Inventory;
using Mozzi.Game.Stall;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Ui;
using Mozzi.Game.Wallet;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace VideoCaptureTool
{
    // v17(현재 main) 전용 데모 드라이버. VideoCapture 가 이름으로 찾아 매 프레임 한 칸씩 돌린다 (30fps 기준 프레임 수).
    // 흐름 : 농장(재료 줍기) → 가판대(손님 걸어 들어옴) → 주방(재료 두 병 → 믹서기 → 떡메 → 빚기 → 찜기) → 가판대(손님 판매) → 합성소(시작 → 받기) → 농장.
    // 게임 코드를 많이 알아 옛 판에는 못 쓴다 — 옛 worktree 에는 VideoCapture.cs 만 복사한다.
    // 누름은 화면의 Button.onClick 을 그대로 부르고, 화면 안 상태(_flow · _pickA 등)는 리플렉션으로 읽는다.
    public static class DemoDriverV17
    {
        private const string Tag = "[DemoV17] ";
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static IEnumerator Play()
        {
            // ── 데움 (안 찍힘) : 낮으로 · 덮개 닫기 · 골드 · 시험 조합 재료 채우기
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            IUiHub ui = InGameHub.Get<IUiHub>();
            IFarmHub farm = InGameHub.Get<IFarmHub>();
            IStallHub stall = InGameHub.Get<IStallHub>();
            IInventoryHub inventory = InGameHub.Get<IInventoryHub>();
            ICodexHub codex = InGameHub.Get<ICodexHub>();
            IWalletHub wallet = InGameHub.Get<IWalletHub>();
            IClockHub clock = InGameHub.Get<IClockHub>();
            ISynthesisHub synthesis = InGameHub.Get<ISynthesisHub>();
            IStaticDataHub data = InGameHub.Get<IStaticDataHub>();

            ui.CloseAll();
            // 「돌아왔어요」 팝업이 떠 있으면 닫는다 (세이브 시각을 Play 직전에 지금으로 바꿔 안 뜨게 하는 게 먼저 — 이건 안전장치).
            foreach (OfflinePopup popup in UnityEngine.Object.FindObjectsByType<OfflinePopup>(FindObjectsSortMode.None))
            {
                popup.Hide();
            }

            if (clock.IsNight)
            {
                clock.AdvanceDay();
            }

            if (wallet.Gold < 3000)
            {
                wallet.Earn(3000 - wallet.Gold);
            }

            int[] combo = PickUntriedCombo(inventory, codex);
            Debug.Log(Tag + "시험 조합 병 " + combo[0] + " + " + combo[1] + " · 골드 " + wallet.Gold + " · 찹쌀이 " + farm.MozziCount
                + " · 진열 " + stall.Shelf.Count + " · 줄 " + stall.Queue.Count + " · 오늘 판 " + stall.ServedToday);

            // ── 농장 약 2초 (데움 45 프레임을 빼면 찍히는 것은 ~1초 + 줍기 뒤 1초) : 바닥 더미를 줍는다 (자원바 숫자 튐)
            yield return Wait(75);
            List<string> piles = new List<string>(farm.Piles.Keys);
            for (int i = 0; i < piles.Count; i++)
            {
                farm.PickUp(piles[i]);
            }

            yield return Wait(30);

            // ── 가판대 2초 : 열고 배속을 올려 손님이 걸어 들어오게
            ui.OpenStall();
            if (stall.IsOpen == false)
            {
                stall.Open();
            }

            GameMaster.TimeScale = 8f;
            yield return Wait(60);
            GameMaster.TimeScale = 1f;

            // ── 주방 : 병 둘 → 믹서기 → 떡메 → 빚기 → 찜기
            ui.OpenKitchen();
            yield return Wait(20);
            KitchenScreen kitchen = UnityEngine.Object.FindFirstObjectByType<KitchenScreen>();
            if (kitchen != null)
            {
                Click(kitchen.transform, UiNames.KitchenJarPrefix + combo[0]);
                yield return Wait(10);
                Click(kitchen.transform, UiNames.KitchenJarPrefix + combo[1]);
                yield return Wait(12);

                object flow = kitchen.GetType().GetField("_flow", Any).GetValue(kitchen);
                MethodInfo nextTool = flow.GetType().GetMethod("NextTool");
                string[] toolNames = { UiNames.KitchenBlender, UiNames.KitchenPound, UiNames.KitchenBoard9 };
                KitchenTool[] tools = { KitchenTool.Mill, KitchenTool.Mortar, KitchenTool.Board };
                // 기구 움직임이 도는 동안은 누름이 무시되므로(IsToolBusy) 움직임이 끝나기를 기다렸다 누른다.
                MethodInfo isBusy = kitchen.GetType().GetMethod("IsToolBusy", Any);
                for (int t = 0; t < tools.Length; t++)
                {
                    for (int guard = 0; guard < 400; guard++)
                    {
                        if ((bool)isBusy.Invoke(kitchen, null))
                        {
                            yield return null;
                            continue;
                        }

                        KitchenTool next = (KitchenTool)nextTool.Invoke(flow, new object[] { true });
                        if (next != tools[t])
                        {
                            break;
                        }

                        Click(kitchen.transform, toolNames[t]);
                        yield return Wait(3);
                    }

                    Debug.Log(Tag + "기구 " + tools[t] + " 끝 · 다음 " + nextTool.Invoke(flow, new object[] { true }));
                }

                // 찜기 (실제 4초) — 결과가 뜰 때까지 기다리고 1초 더 보인다.
                FieldInfo backToPrep = kitchen.GetType().GetField("_backToPrep", Any);
                for (int i = 0; i < 300 && (bool)backToPrep.GetValue(kitchen) == false; i++)
                {
                    yield return null;
                }

                yield return Wait(30);
            }
            else
            {
                Debug.LogWarning(Tag + "KitchenScreen 을 못 찾음");
            }

            // ── 가판대로 돌아와 주문 떡을 만들어 판다
            ui.CloseTop();
            yield return Wait(15);
            for (int served = 0; served < 3 && stall.Queue.Count > 0; served++)
            {
                StallCustomer front = stall.Queue[0];
                if (front.HasOrder)
                {
                    string[] ids = data.Recipes.Get(front.orderRecipeId).IngredientIds;
                    for (int k = 0; k < ids.Length; k++)
                    {
                        inventory.Add(ids[k], 1);
                    }

                    stall.Make(ids);
                }
                else if (stall.Shelf.Count == 0)
                {
                    stall.Make(new[] { inventory.IngredientIds[combo[0]] });
                }

                RefreshOpen();
                yield return Wait(10);
                bool ok = stall.Serve(0);
                RefreshOpen();
                Debug.Log(Tag + "판매 " + ok + " · 줄 " + stall.Queue.Count);
                yield return Wait(18);
            }

            yield return Wait(10);

            // ── 합성소 : 깔때기 둘 → 시작(섞는 연출) → 시간 건너뛰기 → 받기(결과 연출)
            ui.OpenSynthesis();
            yield return Wait(15);
            SynthesisScreen synth = UnityEngine.Object.FindFirstObjectByType<SynthesisScreen>();
            int slot = FindEmptySlot(synthesis);
            if (synth != null && slot >= 0 && farm.MozziCount >= 2)
            {
                string a = farm.Mozzis[0].SpeciesId;
                string b = farm.Mozzis[1].SpeciesId;
                ((List<string>)synth.GetType().GetField("_pickA", Any).GetValue(synth))[slot] = a;
                synth.Refresh();
                yield return Wait(8);
                ((List<string>)synth.GetType().GetField("_pickB", Any).GetValue(synth))[slot] = b;
                synth.Refresh();
                yield return Wait(12);
                synth.GetType().GetMethod("OnStart", Any).Invoke(synth, new object[] { slot });
                Debug.Log(Tag + "합성 시작 칸 " + slot + " · " + a + " + " + b + " · " + synthesis.GetSlot(slot).status);
                yield return Wait(40);
                synthesis.Skip(synthesis.GetSlot(slot).remainingSec + 1);
                yield return Wait(15);
                synth.GetType().GetMethod("OnTake", Any).Invoke(synth, new object[] { slot });
                yield return Wait(45);
            }
            else
            {
                Debug.LogWarning(Tag + "합성 건너뜀 · 화면 " + (synth != null) + " · 빈 칸 " + slot + " · 찹쌀이 " + farm.MozziCount);
            }

            // ── 농장으로
            ui.CloseAll();
            for (;;)
            {
                yield return null;
            }
        }

        private static IEnumerator Wait(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        // 해 본 적 없는 두 재료 조합 (선반 첫 쪽 18병 안). 둘 다 창고에 3개씩 채운다.
        private static int[] PickUntriedCombo(IInventoryHub inventory, ICodexHub codex)
        {
            IReadOnlyList<string> ids = inventory.IngredientIds;
            int n = Mathf.Min(ids.Count, UiNames.KitchenJarCount);
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    string key = RecipeMatchUtil.ComboKey(new[] { ids[i], ids[j] });
                    if (codex.HasTriedCombo(key) == false)
                    {
                        inventory.Add(ids[i], 3);
                        inventory.Add(ids[j], 3);
                        return new[] { i, j };
                    }
                }
            }

            inventory.Add(ids[0], 3);
            inventory.Add(ids[1], 3);
            return new[] { 0, 1 };
        }

        private static int FindEmptySlot(ISynthesisHub synthesis)
        {
            for (int i = 0; i < synthesis.SlotCount; i++)
            {
                if (synthesis.GetSlot(i).status == SynthesisSlotStatus.Empty)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void RefreshOpen()
        {
            StallScreen stallScreen = UnityEngine.Object.FindFirstObjectByType<StallScreen>();
            if (stallScreen != null)
            {
                stallScreen.Refresh();
            }
        }

        private static void Click(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            Button button = t != null ? t.GetComponent<Button>() : null;
            if (button == null)
            {
                Debug.LogWarning(Tag + "버튼 없음 : " + name);
                return;
            }

            button.onClick.Invoke();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif

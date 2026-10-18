#if UNITY_EDITOR
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
    // 소개 영상용 자동 플레이 (v17). 농장 → 주방 → 가판대 → 합성소 → 농장+도감을 실시간(배속 없이)으로 돈다.
    // Arm 의 데움 프레임은 Warmup 과 같게 넘긴다 — 장면 시작 로그의 frame= 이 png 번호와 맞는다.
    public static class DemoDriverIntro
    {
        public const int Warmup = 60;

        private const string Tag = "[DemoIntro] ";
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static int _startFrame;

        public static IEnumerator Play()
        {
            _startFrame = Time.frameCount;

            // ── 데움 (안 찍힘) : 낮으로 · 덮개 닫기 · 골드 · 바닥 더미 · 시험 조합 재료
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

            if (clock.IsNight)
            {
                clock.AdvanceDay();
            }

            if (wallet.Gold < 3000)
            {
                wallet.Earn(3000 - wallet.Gold);
            }

            if (farm.FloorTotal == 0)
            {
                farm.Skip(120);
            }

            ui.CloseAll();
            HidePopups();
            // 에디터 전용 디버그 패널(「D」)은 소개 영상에 안 나오게 끈다.
            GameObject debugRoot = GameObject.Find(UiNames.DebugRoot);
            if (debugRoot != null)
            {
                debugRoot.SetActive(false);
            }

            Debug.Log(Tag + "디버그 패널 끔 " + (debugRoot != null));
            int[] combo = PickUntriedCombo(inventory, codex);
            Debug.Log(Tag + "준비 · 조합 병 " + combo[0] + " + " + combo[1] + " · 골드 " + wallet.Gold + " · 찹쌀이 " + farm.MozziCount
                + " · 바닥 " + farm.FloorTotal + " · 진열 " + stall.Shelf.Count + " · 줄 " + stall.Queue.Count);

            while (Shot() < 1)
            {
                yield return null;
            }

            // ── 장면 1 농장 약 8초 : 돌아다니는 찹쌀이 4.5초 → 더미 줍기(자원바 튐) → 3.5초 머묾
            HidePopups();
            LogScene(1, "농장");
            yield return Wait(135);
            List<string> piles = new List<string>(farm.Piles.Keys);
            for (int i = 0; i < piles.Count; i++)
            {
                farm.PickUp(piles[i]);
            }

            Debug.Log(Tag + "더미 " + piles.Count + "종 주움 frame=" + Shot());
            yield return Wait(105);

            // ── 장면 2 주방 약 18초 : 병 둘 → 믹서기 → 떡메 → 빚기 → 찜기 (기구 사이 1초 숨)
            ui.OpenKitchen();
            LogScene(2, "주방");
            yield return Wait(30);
            KitchenScreen kitchen = Object.FindFirstObjectByType<KitchenScreen>();
            if (kitchen != null)
            {
                Click(kitchen.transform, UiNames.KitchenJarPrefix + combo[0]);
                yield return Wait(20);
                Click(kitchen.transform, UiNames.KitchenJarPrefix + combo[1]);
                yield return Wait(30);
                yield return RunKitchenTools(kitchen);

                // 찜기 (실제 4초) — 결과가 뜰 때까지 기다리고 1.5초 더 보인다.
                FieldInfo backToPrep = kitchen.GetType().GetField("_backToPrep", Any);
                for (int i = 0; i < 300 && (bool)backToPrep.GetValue(kitchen) == false; i++)
                {
                    yield return null;
                }

                Debug.Log(Tag + "찜기 끝 frame=" + Shot());
                yield return Wait(45);
            }
            else
            {
                Debug.LogWarning(Tag + "KitchenScreen 을 못 찾음");
            }

            // ── 장면 3 가판대 약 10초 : 줄을 비우고 손님 둘이 걸어 들어오게 → 주문 떡 내주기 → 골드 오름
            ui.CloseAll();
            object stallState = stall.GetType().GetField("_state", Any).GetValue(stall);
            ResetQueue(stallState);
            if (stall.IsOpen == false)
            {
                stall.Open();
            }

            SetStallTimer(stallState, "nextCustomerSec", 0.3);
            ui.OpenStall();
            LogScene(3, "가판대");
            for (int i = 0; i < 90; i++)
            {
                SetStallTimer(stallState, "clerkSec", 999);
                if (i == 30)
                {
                    SetStallTimer(stallState, "nextCustomerSec", 0.4);
                }

                yield return null;
            }

            Debug.Log(Tag + "손님 줄 " + stall.Queue.Count + " frame=" + Shot());
            for (int served = 0; served < 2 && stall.Queue.Count > 0; served++)
            {
                SetStallTimer(stallState, "clerkSec", 999);
                SetStallTimer(stallState, "nextCustomerSec", 999);
                MakeOrder(stall, inventory, data, combo);
                RefreshStall();
                yield return Wait(20);
                int before = wallet.Gold;
                bool ok = stall.Serve(0);
                RefreshStall();
                Debug.Log(Tag + "판매 " + ok + " · 골드 " + before + " → " + wallet.Gold + " frame=" + Shot());
                yield return Wait(60);
            }

            yield return Wait(30);

            // ── 장면 4 합성소 약 12초 : 깔때기 둘 → 시작(섞는 연출) → 시간 건너뛰기 → 받기(결과 연출)
            ui.CloseAll();
            ui.OpenSynthesis();
            LogScene(4, "합성소");
            yield return Wait(30);
            SynthesisScreen synth = Object.FindFirstObjectByType<SynthesisScreen>();
            int slot = FindEmptySlot(synthesis);
            if (synth != null && slot >= 0 && farm.MozziCount >= 2)
            {
                string a = farm.Mozzis[0].SpeciesId;
                string b = farm.Mozzis[1].SpeciesId;
                ((List<string>)synth.GetType().GetField("_pickA", Any).GetValue(synth))[slot] = a;
                synth.Refresh();
                yield return Wait(25);
                ((List<string>)synth.GetType().GetField("_pickB", Any).GetValue(synth))[slot] = b;
                synth.Refresh();
                yield return Wait(30);
                synth.GetType().GetMethod("OnStart", Any).Invoke(synth, new object[] { slot });
                Debug.Log(Tag + "합성 시작 칸 " + slot + " · " + a + " + " + b + " · " + synthesis.GetSlot(slot).status + " frame=" + Shot());
                yield return Wait(150);
                synthesis.Skip(synthesis.GetSlot(slot).remainingSec + 1);
                yield return Wait(30);
                synth.GetType().GetMethod("OnTake", Any).Invoke(synth, new object[] { slot });
                Debug.Log(Tag + "합성 받기 frame=" + Shot());
                yield return Wait(90);
            }
            else
            {
                Debug.LogWarning(Tag + "합성 건너뜀 · 화면 " + (synth != null) + " · 빈 칸 " + slot + " · 찹쌀이 " + farm.MozziCount);
                yield return Wait(300);
            }

            // ── 장면 5 농장 + 도감 약 6초 : 농장 2초 → 찹쌀이 도감 3초 → 닫기
            ui.CloseAll();
            LogScene(5, "농장+도감");
            yield return Wait(60);
            ui.OpenMozziCodex();
            Debug.Log(Tag + "도감 열기 frame=" + Shot());
            yield return Wait(90);
            ui.CloseTop();
            Debug.Log(Tag + "도감 닫기 frame=" + Shot());
            yield return Wait(30);
            Debug.Log(Tag + "끝 frame=" + Shot());
            for (;;)
            {
                yield return null;
            }
        }

        // 지금 프레임 끝에 찍힐 png 번호 (데움 중이면 0 이하).
        private static int Shot()
        {
            return Time.frameCount - _startFrame + 1 - Warmup;
        }

        private static void LogScene(int index, string name)
        {
            Debug.Log(Tag + "장면 " + index + " 시작 frame=" + Shot() + " · " + name);
        }

        private static IEnumerator Wait(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        // 믹서기 → 떡메 → 빚기. 기구 움직임이 도는 동안은 누름이 무시되므로(IsToolBusy) 끝나기를 기다렸다 누른다.
        private static IEnumerator RunKitchenTools(KitchenScreen kitchen)
        {
            object flow = kitchen.GetType().GetField("_flow", Any).GetValue(kitchen);
            MethodInfo nextTool = flow.GetType().GetMethod("NextTool");
            MethodInfo isBusy = kitchen.GetType().GetMethod("IsToolBusy", Any);
            string[] toolNames = { UiNames.KitchenBlender, UiNames.KitchenPound, UiNames.KitchenBoard9 };
            KitchenTool[] tools = { KitchenTool.Mill, KitchenTool.Mortar, KitchenTool.Board };
            for (int t = 0; t < tools.Length; t++)
            {
                while ((bool)isBusy.Invoke(kitchen, null))
                {
                    yield return null;
                }

                yield return Wait(30);
                Debug.Log(Tag + "기구 " + tools[t] + " 시작 frame=" + Shot());
                for (int guard = 0; guard < 600; guard++)
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
            }
        }

        // 세이브에 남은 줄을 비우고 오늘 온 손님 수를 0 으로 — 손님이 화면 밖에서 걸어 들어오게 한다.
        private static void ResetQueue(object stallState)
        {
            ((List<StallCustomer>)stallState.GetType().GetField("queue").GetValue(stallState)).Clear();
            stallState.GetType().GetField("arrivedToday").SetValue(stallState, 0);
        }

        private static void SetStallTimer(object stallState, string field, double seconds)
        {
            stallState.GetType().GetField(field).SetValue(stallState, seconds);
        }

        // 맨 앞 손님의 주문 떡을 만든다 (재료는 채워 준다). 아무거나 손님이면 조합 첫 병으로 평범한 떡.
        private static void MakeOrder(IStallHub stall, IInventoryHub inventory, IStaticDataHub data, int[] combo)
        {
            StallCustomer front = stall.Queue[0];
            if (front.HasOrder == false)
            {
                string plain = inventory.IngredientIds[combo[0]];
                inventory.Add(plain, 1);
                stall.Make(new[] { plain });
                return;
            }

            string[] ids = data.Recipes.Get(front.orderRecipeId).IngredientIds;
            for (int k = 0; k < ids.Length; k++)
            {
                inventory.Add(ids[k], 1);
            }

            stall.Make(ids);
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

        // 「돌아왔어요」 팝업 안전장치 (TouchSaveTime 으로 안 뜨게 하는 게 먼저).
        private static void HidePopups()
        {
            foreach (OfflinePopup popup in Object.FindObjectsByType<OfflinePopup>(FindObjectsSortMode.None))
            {
                popup.Hide();
            }
        }

        private static void RefreshStall()
        {
            StallScreen stallScreen = Object.FindFirstObjectByType<StallScreen>();
            if (stallScreen != null)
            {
                stallScreen.Refresh();
            }
        }

        private static void Click(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            Button button = null;
            if (t != null)
            {
                button = t.GetComponent<Button>();
            }

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

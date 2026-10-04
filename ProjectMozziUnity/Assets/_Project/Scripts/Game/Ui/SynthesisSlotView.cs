using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 합성소 칸 하나(기계 뿌리 Machine_i)가 쥔 위젯 묶음. 넷이 같은 자식 이름을 쓰므로 전부 기계 뿌리 밑에서 찾는다.
    // 상태별로 무엇을 켜고 끌지는 화면(SynthesisScreen)이 정한다.
    public sealed class SynthesisSlotView
    {
        public readonly GameObject Root;
        public readonly Image MachineArt;
        public readonly Button Plate;
        public readonly TMP_Text PlateText;
        public readonly Button DrawerButton;
        public readonly Button BuyStand;
        public readonly TMP_Text BuyText;
        public readonly Button InletAButton;
        public readonly Button InletBButton;
        public readonly GameObject PlusA;
        public readonly GameObject PlusB;
        public readonly SynthesisMachineView Machine;

        public SynthesisSlotView(Transform machineRoot, string prefabName, SynthesisMachineView machine,
            UnityAction onInletA, UnityAction onInletB, UnityAction onPlate, UnityAction onDrawer, UnityAction onBuy)
        {
            Root = machineRoot.gameObject;
            Machine = machine;
            MachineArt = UiBindUtil.Find<Image>(machineRoot, UiNames.SynthesisMachineArt, prefabName);

            Plate = UiBindUtil.BindButton(machineRoot, UiNames.SynthesisPlate, prefabName, onPlate);
            PlateText = UiBindUtil.Find<TMP_Text>(Plate.transform, UiNames.SynthesisPlateText, prefabName);
            DrawerButton = UiBindUtil.BindButton(machineRoot, UiNames.SynthesisDrawerButton, prefabName, onDrawer);
            BuyStand = UiBindUtil.BindButton(machineRoot, UiNames.SynthesisBuyStand, prefabName, onBuy);
            BuyText = UiBindUtil.Find<TMP_Text>(BuyStand.transform, UiNames.SynthesisBuyText, prefabName);

            InletAButton = UiBindUtil.BindButton(machineRoot, UiNames.SynthesisInletAButton, prefabName, onInletA);
            InletBButton = UiBindUtil.BindButton(machineRoot, UiNames.SynthesisInletBButton, prefabName, onInletB);
            PlusA = UiBindUtil.FindObject(InletAButton.transform, UiNames.SynthesisInletPlus, prefabName);
            PlusB = UiBindUtil.FindObject(InletBButton.transform, UiNames.SynthesisInletPlus, prefabName);
        }

        // 상태마다 누를 수 있는 것만 켠다. 표 · 서랍 · 받침 · 깔때기 둘.
        public void ShowControls(bool plate, bool drawer, bool buy, bool inlets)
        {
            UiBindUtil.SetActive(Plate.gameObject, plate);
            UiBindUtil.SetActive(DrawerButton.gameObject, drawer);
            UiBindUtil.SetActive(BuyStand.gameObject, buy);
            UiBindUtil.SetActive(InletAButton.gameObject, inlets);
            UiBindUtil.SetActive(InletBButton.gameObject, inlets);
        }
    }
}

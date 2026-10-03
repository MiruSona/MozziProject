using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 합성소 화면의 칸 하나가 쥔 위젯 묶음. 상태(잠김 · 빔 · 돌고 있음 · 다 됨)마다 그룹 하나를 켠다.
    public sealed class SynthesisSlotView
    {
        // 칸 뿌리. 화면은 보고 있는 칸 하나만 켠다.
        public readonly GameObject Root;
        public readonly GameObject LockedGroup;
        public readonly GameObject EmptyGroup;
        public readonly GameObject RunningGroup;
        public readonly GameObject DoneGroup;

        public readonly Button BuySlotButton;
        public readonly Button MaterialAButton;
        public readonly Button MaterialBButton;
        public readonly Button StartButton;
        public readonly Button TakeButton;
        public readonly TMP_Text PreviewText;
        public readonly TMP_Text RemainText;

        public SynthesisSlotView(Transform slotRoot, string prefabName,
            UnityAction onBuySlot, UnityAction onMaterialA, UnityAction onMaterialB, UnityAction onStart, UnityAction onTake)
        {
            Root = slotRoot.gameObject;
            LockedGroup = UiBindUtil.FindObject(slotRoot, UiNames.SynthesisLockedGroup, prefabName);
            EmptyGroup = UiBindUtil.FindObject(slotRoot, UiNames.SynthesisEmptyGroup, prefabName);
            RunningGroup = UiBindUtil.FindObject(slotRoot, UiNames.SynthesisRunningGroup, prefabName);
            DoneGroup = UiBindUtil.FindObject(slotRoot, UiNames.SynthesisDoneGroup, prefabName);

            BuySlotButton = UiBindUtil.BindButton(slotRoot, UiNames.SynthesisBuySlotButton, prefabName, onBuySlot);
            MaterialAButton = UiBindUtil.BindButton(slotRoot, UiNames.SynthesisMaterialAButton, prefabName, onMaterialA);
            MaterialBButton = UiBindUtil.BindButton(slotRoot, UiNames.SynthesisMaterialBButton, prefabName, onMaterialB);
            StartButton = UiBindUtil.BindButton(slotRoot, UiNames.SynthesisStartButton, prefabName, onStart);
            TakeButton = UiBindUtil.BindButton(slotRoot, UiNames.SynthesisTakeButton, prefabName, onTake);
            PreviewText = UiBindUtil.Find<TMP_Text>(slotRoot, UiNames.SynthesisPreviewText, prefabName);
            RemainText = UiBindUtil.Find<TMP_Text>(slotRoot, UiNames.SynthesisRemainText, prefabName);
        }

        public void ShowGroup(GameObject group)
        {
            UiBindUtil.SetActive(LockedGroup, group == LockedGroup);
            UiBindUtil.SetActive(EmptyGroup, group == EmptyGroup);
            UiBindUtil.SetActive(RunningGroup, group == RunningGroup);
            UiBindUtil.SetActive(DoneGroup, group == DoneGroup);
        }
    }
}

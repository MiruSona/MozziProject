using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 찹쌀이 팝업(Prefabs/UI/MozziPopup)과 같은 모양 · 같은 자리로 뜨는 「할 일 하나」 팝업. 제목 · 설명 · 할 일 버튼 · 닫기.
    // 진열대 비우기 · 재료 전체 팔기가 같이 쓴다. 프리팹은 찹쌀이 팝업 것을 하나 더 빌려 자식 이름(UiNames.MozziPopup*)으로 찾는다.
    public sealed class ActionPopupView
    {
        private const string PrefabName = "MozziPopup";

        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private readonly TMP_Text _body;
        private readonly Button _action;
        private UnityAction _onAction;

        // onPicked 는 할 일 버튼을 누른 뒤 부른다 (UiManager 가 열린 화면을 다시 그린다).
        public ActionPopupView(Transform root, UnityAction onPicked)
        {
            _root = root.gameObject;
            _title = UiBindUtil.Find<TMP_Text>(root, UiNames.MozziPopupTitleText, PrefabName);
            _body = UiBindUtil.Find<TMP_Text>(root, UiNames.MozziPopupNameText, PrefabName);

            // 뿌리는 어두운 덮개라 상자 밖을 눌러도 닫힌다.
            Button cover = root.GetComponent<Button>();
            cover.onClick.RemoveAllListeners();
            cover.onClick.AddListener(Hide);

            UiBindUtil.BindButton(root, UiNames.MozziPopupCloseButton, PrefabName, Hide);
            _action = UiBindUtil.BindButton(root, UiNames.MozziPopupStoreButton, PrefabName, delegate { OnAction(onPicked); });
        }

        public bool IsOpen
        {
            get { return _root.activeSelf; }
        }

        // actionOn 이 false 면 버튼을 끈다 (팔 것이 없을 때).
        public void Show(string title, string body, string actionLabel, bool actionOn, UnityAction onAction)
        {
            _onAction = onAction;
            UiBindUtil.SetText(_title, title);
            UiBindUtil.SetText(_body, body);
            UiBindUtil.SetLabel(_action, actionLabel);
            _action.interactable = actionOn;
            UiBindUtil.SetActive(_root, true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _onAction = null;
            UiBindUtil.SetActive(_root, false);
        }

        // 팝업 자체가 확인이라 한 번 누르면 바로 한다.
        private void OnAction(UnityAction onPicked)
        {
            UnityAction action = _onAction;
            Hide();
            if (action == null)
            {
                return;
            }

            action();
            onPicked();
        }
    }
}

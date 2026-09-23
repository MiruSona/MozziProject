using Mozzi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 농장 찹쌀이를 탭했을 때 뜨는 팝업. 종 이름 · 「보관함으로」 · 「닫기」.
    // 뿌리는 어두운 덮개이자 닫기 버튼이라 상자 밖을 탭해도 닫힌다. 옮기기는 UiManager 가 IStorageHub 로 한다.
    public sealed class MozziPopup : ObjectBase
    {
        private const string PrefabName = "MozziPopup";

        private TMP_Text _title;
        private TMP_Text _name;
        private int _mozziId;
        private bool _bound;

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public int MozziId
        {
            get { return _mozziId; }
        }

        public void Bind(UnityAction<int> onStore, UnityAction onClose)
        {
            if (_bound)
            {
                return;
            }

            _title = UiBindUtil.Find<TMP_Text>(transform, UiNames.MozziPopupTitleText, PrefabName);
            _name = UiBindUtil.Find<TMP_Text>(transform, UiNames.MozziPopupNameText, PrefabName);

            Button cover = GetComponent<Button>();
            cover.onClick.RemoveAllListeners();
            cover.onClick.AddListener(onClose);

            UiBindUtil.BindButton(transform, UiNames.MozziPopupCloseButton, PrefabName, onClose);
            UiBindUtil.BindButton(transform, UiNames.MozziPopupStoreButton, PrefabName, delegate { onStore(_mozziId); });

            UiBindUtil.SetText(_title, "찹쌀이");
            _bound = true;
        }

        // 풀로 돌아갈 때 리스너를 떼고 다시 Bind 되게 한다.
        public override void OnReturn()
        {
            base.OnReturn();
            UiBindUtil.UnbindAllButtons(transform);
            _title = null;
            _name = null;
            _bound = false;
        }

        public void Show(int mozziId, string speciesName)
        {
            _mozziId = mozziId;
            UiBindUtil.SetText(_name, speciesName);
            UiBindUtil.SetActive(gameObject, true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            UiBindUtil.SetActive(gameObject, false);
        }
    }
}

using Mozzi.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 「돌아왔어요」 팝업. 뿌리 자체가 큰 버튼이라 아무 데나 탭하면 닫힌다.
    // 글은 UiManager 가 OfflineTextUtil 로 만들어 넣는다. 열고 닫기만 한다.
    public sealed class OfflinePopup : ObjectBase
    {
        private const string PrefabName = "OfflinePopup";

        private Text _title;
        private Text _time;
        private Text _body;
        private bool _bound;

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public void Bind(UnityAction onClose)
        {
            if (_bound)
            {
                return;
            }

            _title = UiBindUtil.Find<Text>(transform, UiNames.OfflineTitleText, PrefabName);
            _time = UiBindUtil.Find<Text>(transform, UiNames.OfflineTimeText, PrefabName);
            _body = UiBindUtil.Find<Text>(transform, UiNames.OfflineBodyText, PrefabName);

            Button cover = GetComponent<Button>();
            cover.onClick.RemoveAllListeners();
            cover.onClick.AddListener(onClose);

            UiBindUtil.SetText(_title, "돌아왔어요");
            _bound = true;
        }

        // 풀로 돌아갈 때 닫기 리스너를 떼고 다시 Bind 되게 한다.
        public override void OnReturn()
        {
            base.OnReturn();
            UiBindUtil.UnbindAllButtons(transform);
            _title = null;
            _time = null;
            _body = null;
            _bound = false;
        }

        public void Show(string timeLine, string body)
        {
            UiBindUtil.SetText(_time, timeLine);
            UiBindUtil.SetText(_body, body);
            UiBindUtil.SetActive(gameObject, true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            UiBindUtil.SetActive(gameObject, false);
        }
    }
}

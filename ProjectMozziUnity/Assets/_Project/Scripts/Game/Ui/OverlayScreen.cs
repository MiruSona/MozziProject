using Mozzi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 농장 위에 덮이는 전체 화면 패널의 공통 뼈대. 머리(제목 · 골드 · 닫기)를 여기서 묶는다.
    // Bind 는 UiManager 가 PostInit 에서 한 번 부르고, Refresh 는 열 때와 이벤트 때 부른다.
    public abstract class OverlayScreen : ObjectBase
    {
        private TMP_Text _title;
        private TMP_Text _gold;
        private bool _bound;

        protected abstract string PrefabName { get; }

        protected abstract string Title { get; }

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        public void Bind(IUiHub ui)
        {
            if (_bound)
            {
                return;
            }

            _title = UiBindUtil.Find<TMP_Text>(transform, UiNames.TitleText, PrefabName);
            _gold = UiBindUtil.Find<TMP_Text>(transform, UiNames.GoldText, PrefabName);
            UiBindUtil.BindButton(transform, UiNames.CloseButton, PrefabName, ui.CloseAll);
            UiBindUtil.SetText(_title, Title);
            EnsureBackdrop();

            BindChildren();
            _bound = true;
        }

        public void SetGold(int gold)
        {
            UiBindUtil.SetText(_gold, gold + " G");
        }

        // 풀로 돌아갈 때 배선을 다 푼다. 풀 루트는 씬을 넘어 살아남으므로 다음 Bind 가 새 Manager 를 잡게 한다.
        public override void OnReturn()
        {
            base.OnReturn();
            UiBindUtil.UnbindAllButtons(transform);
            UnbindChildren();
            _title = null;
            _gold = null;
            _bound = false;
        }

        // 뿌리 뒤에 같은 색 바탕을 한 장 더 깐다. 내용 칸 옆(넓적한 화면)과 안전 영역 밖까지 덮어 농장이 안 비친다.
        // 한 번만 만든다 — 풀에서 다시 빌려 와도 이름으로 찾아 그대로 쓴다.
        private void EnsureBackdrop()
        {
            if (transform.Find(UiNames.OverlayBackdrop) != null)
            {
                return;
            }

            GameObject go = new GameObject(UiNames.OverlayBackdrop, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-LayoutConstUi.BarBleedLength, -LayoutConstUi.BarBleedLength);
            rect.offsetMax = new Vector2(LayoutConstUi.BarBleedLength, LayoutConstUi.BarBleedLength);

            Image image = go.AddComponent<Image>();
            image.color = Color.black;
            Image rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                image.color = rootImage.color;
            }
        }

        // 화면마다 자식을 찾아 버튼을 거는 자리.
        protected abstract void BindChildren();

        // 화면마다 쥐고 있던 목록 · 허브를 비우는 자리. 버튼 리스너는 위에서 이미 다 뗐다.
        protected abstract void UnbindChildren();

        public abstract void Refresh();

        public virtual void Process(float deltaTime)
        {
        }

        // 닫힐 때 고르던 것 · 연출을 비우는 자리.
        public virtual void OnClosed()
        {
        }
    }
}

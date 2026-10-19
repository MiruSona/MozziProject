using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 병 하나에 그릴 재료 한 줄. Locked 면 잠긴 칸(아이콘 없이 잠금 글자).
    public struct KitchenJarItem
    {
        public Sprite Icon;
        public string CountText;
        public bool Empty;
        public bool Selected;
        public bool Interactable;
        public bool Locked;
    }

    // 칸1 재료 선반 : 병 18개(한 쪽) · ◀ ▶ · 쪽 점. 재료 목록을 받아 지금 쪽만 그린다.
    // 병을 누르면 onJar(재료 번호 = 쪽 × 18 + 병 번호). 화살표는 안에서 쪽만 넘긴다. Manager 를 모른다.
    public sealed class KitchenShelfView
    {
        private static readonly Color JarOn = new Color(1f, 0.85f, 0.55f, 1f);
        private static readonly Color JarLocked = new Color(0.55f, 0.55f, 0.6f, 1f);
        private static readonly Color IconEmpty = new Color(1f, 1f, 1f, 0.35f);

        private readonly Button[] _jars;
        private readonly Image[] _icons;
        private readonly TMP_Text[] _counts;
        private readonly GameObject _arrowLeft;
        private readonly GameObject _arrowRight;
        private readonly Image[] _dots;
        private readonly Sprite _dotOn;
        private readonly Sprite _dotOff;
        private readonly Action<int> _onJar;
        private readonly List<KitchenJarItem> _items = new List<KitchenJarItem>();
        private int _page;

        public KitchenShelfView(Button[] jars, Image[] icons, TMP_Text[] counts, Button arrowLeft, Button arrowRight,
            Image[] dots, Sprite dotOn, Sprite dotOff, Action<int> onJar)
        {
            _jars = jars;
            _icons = icons;
            _counts = counts;
            _arrowLeft = arrowLeft.gameObject;
            _arrowRight = arrowRight.gameObject;
            _dots = dots;
            _dotOn = dotOn;
            _dotOff = dotOff;
            _onJar = onJar;

            for (int i = 0; i < _jars.Length; i++)
            {
                int slot = i;
                _jars[i].onClick.AddListener(() => OnJar(slot));
            }

            arrowLeft.onClick.AddListener(() => SetPage(_page - 1));
            arrowRight.onClick.AddListener(() => SetPage(_page + 1));
        }

        public int PageCount
        {
            get { return Mathf.Max(1, (_items.Count + _jars.Length - 1) / _jars.Length); }
        }

        public void SetItems(IReadOnlyList<KitchenJarItem> items)
        {
            _items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                _items.Add(items[i]);
            }

            SetPage(_page);
        }

        // 쪽은 끝에서 돌아간다 (마지막 쪽 ▶ = 첫 쪽).
        public void SetPage(int page)
        {
            int count = PageCount;
            _page = ((page % count) + count) % count;
            Draw();
        }

        private void Draw()
        {
            int start = _page * _jars.Length;
            for (int i = 0; i < _jars.Length; i++)
            {
                int index = start + i;
                bool has = index < _items.Count;
                _jars[i].gameObject.SetActive(has);
                if (has == false)
                {
                    continue;
                }

                KitchenJarItem item = _items[index];
                _jars[i].image.color = item.Locked ? JarLocked : (item.Selected ? JarOn : Color.white);
                _jars[i].interactable = item.Interactable && item.Locked == false;
                _icons[i].enabled = item.Icon != null && item.Locked == false;
                _icons[i].sprite = item.Icon;
                _icons[i].color = item.Empty ? IconEmpty : Color.white;
                UiBindUtil.SetText(_counts[i], item.CountText);
            }

            bool many = PageCount > 1;
            _arrowLeft.SetActive(many);
            _arrowRight.SetActive(many);
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].gameObject.SetActive(many && i < PageCount);
                _dots[i].sprite = i == _page ? _dotOn : _dotOff;
            }
        }

        private void OnJar(int slot)
        {
            int index = _page * _jars.Length + slot;
            if (index < _items.Count && _onJar != null)
            {
                _onJar(index);
            }
        }
    }
}

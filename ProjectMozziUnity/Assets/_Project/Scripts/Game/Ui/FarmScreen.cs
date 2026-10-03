using System.Collections.Generic;
using Mozzi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 농장 화면(자원바 · 아래 띠 · 밤 덮개). 프리팹은 모양만 갖고 여기서 UiNames 로 자식을 찾아 값을 넣는다.
    // 값이 바뀔 때 숫자가 커졌다 돌아오는 튐 1종과, 아침에 덮개가 걷히는 밝아짐 1종을 돈다.
    // 위아래 띠는 안전 영역 안에 있고, 그 바깥(카메라 구멍 쪽)은 같은 색 덧띠를 코드로 이어 붙인다.
    public sealed class FarmScreen : ObjectBase
    {
        private const string PrefabName = "FarmScreen";
        private const float BumpDuration = 0.25f;
        private const float BumpScale = 1.5f;
        private const float DawnDuration = 0.5f;

        private readonly Dictionary<string, TMP_Text> _ingredientTexts = new Dictionary<string, TMP_Text>();
        private readonly Dictionary<string, Image> _ingredientIcons = new Dictionary<string, Image>();
        private readonly Dictionary<TMP_Text, float> _bumps = new Dictionary<TMP_Text, float>();
        private readonly List<TMP_Text> _bumpDone = new List<TMP_Text>();

        private TMP_Text _title;
        private TMP_Text _gold;
        private TMP_Text _day;
        private TMP_Text _fence;
        private Image _nightCover;
        private TMP_Text _nightText;
        private GameObject _nextDayButton;
        private RectTransform _storageButtonRect;
        private float _nightAlpha;
        private float _dawnTime;
        private bool _dawning;
        private bool _bound;

        // 아래 띠의 도감 둘 · 보관함 버튼은 IUiHub 로 열고, 「다음날로」는 넘겨받은 onNextDay 를 부른다.
        public void Bind(IReadOnlyList<string> ingredientIds, IUiHub ui, UnityAction onNextDay)
        {
            if (_bound)
            {
                return;
            }

            _title = UiBindUtil.Find<TMP_Text>(transform, UiNames.FarmTitleText, PrefabName);
            _gold = UiBindUtil.Find<TMP_Text>(transform, UiNames.FarmGoldText, PrefabName);
            _day = UiBindUtil.Find<TMP_Text>(transform, UiNames.FarmDayText, PrefabName);
            _fence = UiBindUtil.Find<TMP_Text>(transform, UiNames.FarmFenceText, PrefabName);

            for (int i = 0; i < ingredientIds.Count; i++)
            {
                string childName = UiNames.FarmIngredientPrefix + ingredientIds[i];
                TMP_Text text = UiBindUtil.Find<TMP_Text>(transform, childName, PrefabName);
                _ingredientTexts[ingredientIds[i]] = text;

                Image icon;
                if (UiBindUtil.TryFind<Image>(text.transform, UiNames.IngredientIcon, out icon))
                {
                    _ingredientIcons[ingredientIds[i]] = icon;
                    SetIngredientIcon(ingredientIds[i], null);
                }
            }

            UiBindUtil.BindButton(transform, UiNames.FarmCodexButton, PrefabName, ui.OpenMozziCodex);
            _storageButtonRect = UiBindUtil.BindButton(transform, UiNames.FarmStorageButton, PrefabName, ui.OpenStorage).transform as RectTransform;
            UiBindUtil.BindButton(transform, UiNames.FarmRecipeCodexButton, PrefabName, ui.OpenRecipeCodex);
            _nextDayButton = UiBindUtil.BindButton(transform, UiNames.FarmNextDayButton, PrefabName, onNextDay).gameObject;

            _nightCover = UiBindUtil.Find<Image>(transform, UiNames.FarmNightCover, PrefabName);
            _nightAlpha = _nightCover.color.a;
            _nightText = UiBindUtil.Find<TMP_Text>(_nightCover.transform, UiNames.FarmNightText, PrefabName);
            UiBindUtil.SetText(_nightText, "밤이 됐어요\n아래 「다음날로」를 눌러요");

            UiBindUtil.SetText(_title, "농장");

            ExtendBar(UiNames.FarmResourceBar, true);
            ExtendBar(UiNames.FarmBottomBar, false);
            _bound = true;
        }

        // 띠를 화면 끝까지 잇는다. 뒤판은 카메라 배경색으로 띠 밑을 막아 월드(풀밭)가 비치지 않게 하고,
        // 덧띠는 띠와 같은 색으로 안전 영역 밖을 채운다. 풀에서 다시 나와도 한 번만 만든다.
        private void ExtendBar(string barName, bool upward)
        {
            Image bar = UiBindUtil.Find<Image>(transform, barName, PrefabName);
            if (bar.transform.Find(UiNames.FarmBarBleed) != null)
            {
                return;
            }

            Color backColor = Color.black;
            if (Camera.main != null)
            {
                backColor = Camera.main.backgroundColor;
                backColor.a = 1f;
            }

            RectTransform back = AddBarPiece(bar.transform.parent, UiNames.FarmBarBack, backColor);
            back.SetSiblingIndex(0);
            RectTransform barRect = bar.rectTransform;
            back.anchorMin = barRect.anchorMin;
            back.anchorMax = barRect.anchorMax;
            back.pivot = barRect.pivot;
            back.anchoredPosition = barRect.anchoredPosition;
            back.sizeDelta = barRect.sizeDelta;
            // 넓적한 화면에서 내용 칸 옆으로 비어져 나온 풀밭 끝도 가린다 (바탕색이라 티가 안 난다).
            back.offsetMin = new Vector2(-LayoutConstUi.BarBleedLength, back.offsetMin.y);
            back.offsetMax = new Vector2(LayoutConstUi.BarBleedLength, back.offsetMax.y);

            RectTransform bleed = AddBarPiece(bar.transform, UiNames.FarmBarBleed, bar.color);
            bleed.SetSiblingIndex(0);
            bleed.sizeDelta = new Vector2(0f, LayoutConstUi.BarBleedLength);
            bleed.anchoredPosition = Vector2.zero;

            if (upward)
            {
                bleed.anchorMin = new Vector2(0f, 1f);
                bleed.anchorMax = new Vector2(1f, 1f);
                bleed.pivot = new Vector2(0.5f, 0f);
                back.offsetMax = new Vector2(back.offsetMax.x, back.offsetMax.y + LayoutConstUi.BarBleedLength);
                return;
            }

            bleed.anchorMin = new Vector2(0f, 0f);
            bleed.anchorMax = new Vector2(1f, 0f);
            bleed.pivot = new Vector2(0.5f, 1f);
            back.offsetMin = new Vector2(back.offsetMin.x, back.offsetMin.y - LayoutConstUi.BarBleedLength);
        }

        private static RectTransform AddBarPiece(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        // 풀로 돌아갈 때 배선을 푼다. 씬을 다시 열면 같은 인스턴스가 새 UiManager 로 다시 Bind 된다.
        public override void OnReturn()
        {
            base.OnReturn();
            UiBindUtil.UnbindAllButtons(transform);
            _ingredientTexts.Clear();
            _ingredientIcons.Clear();
            _bumps.Clear();
            _bumpDone.Clear();
            _dawning = false;
            _nextDayButton = null;
            _storageButtonRect = null;
            _bound = false;
        }

        // 보관함 버튼 위인가. Overlay 캔버스라 카메라 없이 잰다.
        public bool IsOverStorageButton(Vector2 screen)
        {
            if (_storageButtonRect == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_storageButtonRect, screen);
        }

        public void SetGold(int gold, bool bump)
        {
            UiBindUtil.SetText(_gold, gold + " G");
            if (bump)
            {
                StartBump(_gold);
            }
        }

        public void SetDay(int day, bool isNight)
        {
            string suffix = "낮";
            if (isNight)
            {
                suffix = "밤";
            }

            UiBindUtil.SetText(_day, day + "일째 " + suffix);
        }

        // 밤이면 덮개 + 「다음날로」 를 바로 켠다. 낮이면 덮개를 걷되 animate 면 0.5초 밝아진 뒤 끈다.
        public void SetNight(bool isNight, bool animate)
        {
            UiBindUtil.SetActive(_nextDayButton, isNight);

            if (isNight)
            {
                _dawning = false;
                SetCoverAlpha(_nightAlpha);
                UiBindUtil.SetActive(_nightCover.gameObject, true);
                return;
            }

            if (animate && _nightCover.gameObject.activeSelf)
            {
                _dawning = true;
                _dawnTime = 0f;
                return;
            }

            _dawning = false;
            UiBindUtil.SetActive(_nightCover.gameObject, false);
        }

        public void SetFence(int count, int max)
        {
            UiBindUtil.SetText(_fence, count + "/" + max);
        }

        // 그림이 없으면 아이콘을 끄고 글씨를 왼쪽 끝으로 되돌린다. 칸 폭이 58px 이라 둘 다는 못 넣는다.
        public void SetIngredientIcon(string ingredientId, Sprite sprite)
        {
            Image icon;
            if (_ingredientIcons.TryGetValue(ingredientId, out icon) == false)
            {
                return;
            }

            icon.sprite = sprite;
            UiBindUtil.SetActive(icon.gameObject, sprite != null);

            TMP_Text text;
            if (_ingredientTexts.TryGetValue(ingredientId, out text) == false)
            {
                return;
            }

            float indent = 0f;
            if (sprite != null)
            {
                indent = LayoutConstUi.IngredientTextIndent;
            }

            text.margin = new Vector4(indent, 0f, 0f, 0f);
        }

        // 아이콘이 켜져 있으면 이름은 그림이 대신하므로 숫자만 쓴다.
        public void SetIngredient(string ingredientId, string label, int count, int cap, bool bump)
        {
            TMP_Text text;
            if (_ingredientTexts.TryGetValue(ingredientId, out text) == false)
            {
                return;
            }

            string head = label + " ";
            if (HasIcon(ingredientId))
            {
                head = "";
            }

            UiBindUtil.SetText(text, head + count + "/" + cap);
            if (bump)
            {
                StartBump(text);
            }
        }

        public void Process(float deltaTime)
        {
            StepDawn(deltaTime);
            StepBumps(deltaTime);
        }

        private void StepDawn(float deltaTime)
        {
            if (_dawning == false)
            {
                return;
            }

            _dawnTime += deltaTime;
            float k = Mathf.Clamp01(_dawnTime / DawnDuration);
            SetCoverAlpha(Mathf.Lerp(_nightAlpha, 0f, k));
            if (k >= 1f)
            {
                _dawning = false;
                UiBindUtil.SetActive(_nightCover.gameObject, false);
            }
        }

        private void StepBumps(float deltaTime)
        {
            if (_bumps.Count == 0)
            {
                return;
            }

            _bumpDone.Clear();
            List<TMP_Text> keys = new List<TMP_Text>(_bumps.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                TMP_Text text = keys[i];
                float t = _bumps[text] + deltaTime;
                _bumps[text] = t;

                float k = Mathf.Clamp01(t / BumpDuration);
                float scale = Mathf.Lerp(BumpScale, 1f, k);
                text.rectTransform.localScale = new Vector3(scale, scale, 1f);
                if (k >= 1f)
                {
                    _bumpDone.Add(text);
                }
            }

            for (int i = 0; i < _bumpDone.Count; i++)
            {
                _bumps.Remove(_bumpDone[i]);
            }
        }

        private void SetCoverAlpha(float alpha)
        {
            Color c = _nightCover.color;
            c.a = alpha;
            _nightCover.color = c;

            // 안내 글자도 같이 옅어진다.
            Color tc = _nightText.color;
            tc.a = alpha / _nightAlpha;
            _nightText.color = tc;
        }

        private bool HasIcon(string ingredientId)
        {
            Image icon;
            if (_ingredientIcons.TryGetValue(ingredientId, out icon) == false)
            {
                return false;
            }

            return icon.gameObject.activeSelf;
        }

        private void StartBump(TMP_Text text)
        {
            _bumps[text] = 0f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 믹서기 blender10 내용물 겹을 돌리고 재료 색을 입힌다. 몸통은 누르는 Image 한 장 그대로, 겹은 그 자식이라 몸통 위(통 안)에 그려진다.
    // 색 방식 = 층 : 고른 재료가 아래부터 층으로 쌓였다가 장이 넘어갈수록 섞인 색(재료 색 평균)으로 간다.
    //   섞임 t = clamp((장 - 1) / 5, 0, 1). 재료 1개 · 6장째부터는 섞인 겹 한 장에 평균 색, 그 밖에는 층 마스크마다 lerp(층 색, 평균, t).
    // 누름마다 정해진 장 묶음을 70ms 로 돌고 묶음 끝 장에서 멈춘다 (KitchenLayout.BlenderTapFrames).
    public sealed class KitchenBlenderView
    {
        // 재료 id → 내용물 색 (그림 갈래 build_blender9.py 와 같은 값). 표에 없는 재료는 쌀가루 크림.
        private static readonly Dictionary<string, Color32> IngredientColors = new Dictionary<string, Color32>
        {
            { "redbean", new Color32(0xc2, 0x83, 0x83, 0xff) },
            { "bean", new Color32(0xe6, 0xc8, 0x68, 0xff) },
            { "mugwort", new Color32(0x80, 0xaa, 0x68, 0xff) },
            { "sesame", new Color32(0xd6, 0xb0, 0x6a, 0xff) },
            { "pinenut", new Color32(0xf4, 0xea, 0xcc, 0xff) },
            { "jujube", new Color32(0xc9, 0x83, 0x71, 0xff) },
        };

        private static readonly Color32 RiceCream = new Color32(0xff, 0xf2, 0xd7, 0xff);

        private readonly Image _fill;
        private readonly Image[] _layers;
        private readonly Sprite[] _fills;
        // [장, 층 수(2·3), 층] — 장 0~5 만.
        private readonly Sprite[,,] _layerSprites;
        private readonly List<Color> _colors = new List<Color>();
        private Color _mixed = RiceCream;
        private int[] _sequence;
        private int _index;
        private float _elapsed;
        private int _frame;

        public KitchenBlenderView(Image fill, Image[] layers, Sprite[] fills, Sprite[,,] layerSprites)
        {
            _fill = fill;
            _layers = layers;
            _fills = fills;
            _layerSprites = layerSprites;
            _colors.Add(RiceCream);
        }

        public bool IsPlaying
        {
            get { return _sequence != null; }
        }

        // 고른 재료 순서 = 쌓이는 순서 (첫 재료가 맨 아래). 없으면 쌀가루 크림 한 색.
        public void SetIngredients(IList<string> ids)
        {
            _colors.Clear();
            for (int i = 0; i < ids.Count && i < UiNames.KitchenBlenderLayerMax; i++)
            {
                _colors.Add(ColorOf(ids[i]));
            }

            if (_colors.Count == 0)
            {
                _colors.Add(RiceCream);
            }

            _mixed = MixedColor(ids);
            Draw(_frame);
        }

        // 재료 하나의 내용물 색. 표에 없으면 쌀가루 크림.
        public static Color ColorOf(string id)
        {
            Color32 color;
            return IngredientColors.TryGetValue(id, out color) ? color : RiceCream;
        }

        // 고른 재료 색 평균 (앞에서 층 수만큼만). 없으면 쌀가루 크림. 칸3 반죽도 이 색을 옅게 쓴다.
        // 절구 반죽은 겹이 갈라진 그림이 오면 같은 색을 쓴다.
        public static Color MixedColor(IList<string> ids)
        {
            int count = Mathf.Min(ids.Count, UiNames.KitchenBlenderLayerMax);
            if (count == 0)
            {
                return RiceCream;
            }

            Color sum = Color.clear;
            for (int i = 0; i < count; i++)
            {
                sum += ColorOf(ids[i]);
            }

            return sum / count;
        }

        // 누름 tap 번째(1~3) 묶음을 돈다.
        public void Play(int tap)
        {
            int[][] table = KitchenLayout.BlenderTapFrames;
            _sequence = table[Mathf.Clamp(tap, 1, table.Length) - 1];
            _index = 0;
            _elapsed = 0f;
            Draw(_sequence[0]);
        }

        // 쉬는 장 0 으로 되돌린다 (화면 다시 열기 · 결과 뒤 칸1).
        public void Stop()
        {
            _sequence = null;
            Draw(0);
        }

        public void Process(float deltaTime)
        {
            if (_sequence == null)
            {
                return;
            }

            _elapsed += deltaTime;
            while (_sequence != null && _elapsed >= KitchenLayout.BlenderFrameStepSec)
            {
                _elapsed -= KitchenLayout.BlenderFrameStepSec;
                _index++;
                if (_index >= _sequence.Length)
                {
                    // 묶음 끝 장에 멈춰 둔다.
                    _sequence = null;
                    return;
                }

                Draw(_sequence[_index]);
            }
        }

        private void Draw(int frame)
        {
            _frame = frame;
            float t = Mathf.Clamp01((frame - 1) / 5f);
            int n = _colors.Count;
            bool layered = n >= 2 && t < 1f && frame < _layerSprites.GetLength(0);
            _fill.enabled = layered == false && _fills[frame] != null;
            _fill.sprite = _fills[frame];
            _fill.color = _mixed;
            for (int i = 0; i < _layers.Length; i++)
            {
                Sprite sprite = layered && i < n ? _layerSprites[frame, n - 2, i] : null;
                _layers[i].enabled = sprite != null;
                _layers[i].sprite = sprite;
                if (sprite != null)
                {
                    _layers[i].color = Color.Lerp(_colors[i], _mixed, t);
                }
            }
        }
    }
}

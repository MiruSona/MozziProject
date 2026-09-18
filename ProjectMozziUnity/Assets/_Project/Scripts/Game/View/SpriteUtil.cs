using UnityEngine;

namespace Mozzi.Game.View
{
    // 그림이 없는 동안 쓰는 흰 네모 스프라이트. 크기 1×1 유닛이라 scale 이 곧 크기다.
    public static class SpriteUtil
    {
        private static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    _white = CreateWhite();
                }

                return _white;
            }
        }

        // 아트 규칙대로 Point 필터. 색은 SpriteRenderer.color 로 입힌다.
        private static Sprite CreateWhite()
        {
            int size = (int)LayoutConst.PixelsPerUnit;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Rect rect = new Rect(0, 0, size, size);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), LayoutConst.PixelsPerUnit);
        }

        public static Color ParseHex(string hex, Color fallback)
        {
            Color color;
            if (string.IsNullOrEmpty(hex) || ColorUtility.TryParseHtmlString(hex, out color) == false)
            {
                return fallback;
            }

            return color;
        }
    }
}

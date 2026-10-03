using UnityEngine;

namespace Mozzi.Game.Ui
{
    // 화면 크기 맞춤의 한 자리. 기준 360×640 「내용 칸」이 안전 영역 안에 통째로 들어가게 배율을 잡는다.
    // 길쭉한 폰은 가로를 맞춰 세로가 남고, 넓적한 화면은 세로를 맞춰 가로가 남는다 (칸은 가운데).
    // 캔버스(UiManager)와 카메라(FarmViewManager)가 같은 값을 써야 UI 띠와 월드 자리가 안 어긋난다.
    public static class ScreenFitUtil
    {
#if UNITY_EDITOR
        // 에디터에서 카메라 구멍·둥근 모서리를 흉내 낼 때 쓴다 (px). x 왼쪽 · y 아래 · z 오른쪽 · w 위.
        public static Vector4 EditorSafeInsetPx;
#endif

        // 카메라 구멍 · 둥근 모서리를 뺀 자리 (화면 px, 왼쪽 아래가 원점).
        public static Rect SafeArea()
        {
            Rect safe = Screen.safeArea;
#if UNITY_EDITOR
            safe.xMin += EditorSafeInsetPx.x;
            safe.yMin += EditorSafeInsetPx.y;
            safe.xMax -= EditorSafeInsetPx.z;
            safe.yMax -= EditorSafeInsetPx.w;
#endif
            return safe;
        }

        // UI px 하나가 화면 px 몇 개인가. 캔버스 scaleFactor 로 그대로 쓴다.
        public static float CanvasScale()
        {
            Rect safe = SafeArea();
            float byWidth = safe.width / LayoutConstUi.ReferenceWidth;
            float byHeight = safe.height / LayoutConstUi.ReferenceHeight;
            return Mathf.Max(0.01f, Mathf.Min(byWidth, byHeight));
        }

        // 내용 칸 (화면 px). 폭은 기준 360 그대로 안전 영역 가운데, 높이는 안전 영역 전부다.
        public static Rect ContentScreenRect()
        {
            Rect safe = SafeArea();
            float width = LayoutConstUi.ReferenceWidth * CanvasScale();
            float x = safe.x + (safe.width - width) * 0.5f;
            return new Rect(x, safe.y, width, safe.height);
        }
    }
}

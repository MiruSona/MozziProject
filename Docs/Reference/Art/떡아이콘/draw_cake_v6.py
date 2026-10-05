"""떡 아이콘 9판 — 외곽선 1px(selout)을 넣고 바닥에 붙인다 (화풍 통일 A 갈래).

8판(draw_cake_v5.py)과 다른 점
  · 알마다 1px 외곽선을 두른다. 색은 제 피 색을 짙게 한 따뜻한 갈색 둘(그늘 쪽 · 밝은 쪽). 순검정은 안 쓴다.
  · 실제 그림을 캔버스 바닥에서 1줄 띄워 붙이고 가로 가운데에 둔다 (8판은 아래 9줄이 비어 떠 보였다).
  · 외곽선 꼴을 --form 으로 고른다.
      1 = 바깥에 1px 더함 (몸 30x18 그대로 -> 그림 32x20, 가로를 꽉 채운다)
      2 = 몸을 1px 줄이고 안쪽 둘레에 선 (그림 30x18 그대로, 속이 1px 좁아진다)

모양 · 색 · 재료 자리 규칙은 8판 그대로라 draw_cake_v5 의 함수를 불러 쓴다.

쓰기 : python draw_cake_v6.py --form 1 --out <폴더>
한 개짜리 (진열장에 쌓는 그림 · 앞 알만) : python draw_cake_v6.py --form 1 --one --out <폴더>
"""
import argparse
import os

from PIL import Image

import draw_cake_v5 as v5

N = v5.N
IDS = v5.IDS
DIRS4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
BOTTOM_GAP = 1      # 캔버스 바닥에서 띄우는 줄 수

# 피 색마다 외곽선 둘 : (그늘 쪽 · 밝은 쪽). 그늘색 칸 옆은 앞의 것, 밑색 칸 옆은 뒤의 것을 쓴다.
# 흰 떡의 그늘 쪽은 공용 팔레트 line 램프(#3A2E25), 대추는 #4A2622 그대로다.
OUTLINE = {
    None: ("#3A2E25", "#75614F"),
    "mugwort": ("#243A1C", "#3B5C27"),
    "jujube": ("#4A2622", "#6E2E1E"),
    "bean": ("#4C3A1A", "#7E6126"),
}


def ring_out(px):
    """px 바깥 4방향으로 맞닿은 칸 (꼴 1 의 선 자리)."""
    return {(x + dx, y + dy) for (x, y) in px for dx, dy in DIRS4} - px


def ring_in(px):
    """px 안에서 바깥과 4방향으로 맞닿은 칸 (꼴 2 의 선 자리)."""
    return {p for p in px if any((p[0] + dx, p[1] + dy) not in px for dx, dy in DIRS4)}


def split(px, form):
    """알 실루엣 -> (몸, 선). 꼴 1 은 몸 그대로 + 바깥 고리, 꼴 2 는 가장자리 고리를 선으로 내준다."""
    if form == 1:
        return px, ring_out(px)
    line = ring_in(px)
    return px - line, line


def put(buf, px, color):
    c = v5.rgb(color)
    for q in px:
        buf[q] = c


def put_ring(buf, ring, body, dark_hex, mid_hex):
    """선 칸마다 옆 몸 칸 색을 본다. 그늘색이 하나라도 닿으면 그늘 쪽 선, 아니면 밝은 쪽 선."""
    dark, mid = v5.rgb(dark_hex), v5.rgb(mid_hex)
    for (x, y) in ring:
        near = [buf[(x + dx, y + dy)] for dx, dy in DIRS4 if (x + dx, y + dy) in body]
        buf[(x, y)] = dark if any(c in put_ring.shade for c in near) else mid


def make(cid, form):
    s = v5.recipe(cid)
    sb, ss, sf = v5.skin_of(s)
    ol_key = "bean" if s["topping"] == "bean" else s["skin"]
    ol_dark, ol_mid = OUTLINE[ol_key]
    put_ring.shade = {v5.rgb(ss)}

    back_shape, front_shape = v5.shapes()
    back_all, back_line = split(back_shape, form)
    wide = s["skin"] in v5.DARK_SKIN
    buf = {}

    # --- 뒤 알 : 그늘색으로 한 톤 낮춘 겉면 (8판 그대로) ---
    put(buf, back_all, ss)
    put(buf, {q for q in v5.ellipse(v5.BACK_C[0] - 2.6, v5.BACK_C[1] - 4.0, 2.2, 1.6) if q in back_all}, sb)
    put_ring(buf, back_line, back_all, ol_dark, ol_mid)
    if s["topping"] == "sesame":
        reg = v5.erode(back_all) - front_shape
        put(buf, v5.sesame_px(v5.spread_dots(reg, 7, 5, 1), wide, reg), v5.SESAME_DOT)
    if s["topping"] == "bean":
        put(buf, v5.spread_dots(v5.erode(back_all) - front_shape, 5, 5, 3), v5.BEAN_POWDER)

    paint_front(buf, s, front_shape, form)
    return settle(buf, N)


DENSE_SESAME = 14      # 한 개짜리 깨떡의 깨 점 수 (점끼리 2px 이상 뗀다)


def paint_front(buf, s, front_shape, form, dense=False):
    """앞 알(자른 면이 정면) 한 덩이 : 선 -> 피 테두리 -> 떡살 -> 고명 -> 소. 두 알 그림과 한 개짜리 그림이 같이 쓴다.
    dense 면 깨를 테두리뿐 아니라 떡살에도 촘촘히 뿌린다 (한 개짜리는 뒤 알의 깨가 없어 흰 떡과 안 갈린다)."""
    sb, ss, sf = v5.skin_of(s)
    ol_key = "bean" if s["topping"] == "bean" else s["skin"]
    ol_dark, ol_mid = OUTLINE[ol_key]
    put_ring.shade = {v5.rgb(ss)}
    front, front_line = split(front_shape, form)
    wide = s["skin"] in v5.DARK_SKIN
    put(buf, front, sb)
    rim = front - v5.erode(front)
    put(buf, {q for q in v5.shade_edge(front, (1, 2)) if q in rim}, ss)
    put_ring(buf, front_line, front, ol_dark, ol_mid)      # 뒤 알 위에도 그려 두 알을 가른다
    flesh = v5.erode(front)
    put(buf, flesh, sf)
    if s["topping"] == "sesame" and not dense:
        put(buf, v5.sesame_px(v5.spread_dots(rim, 5, 4, 7), wide, rim), v5.SESAME_DOT)
    if s["topping"] == "bean":
        put(buf, v5.spread_dots(rim, 4, 4, 5), v5.BEAN_POWDER)

    k = s["filling"]
    core = set()
    if k:
        fb, fa = v5.FILL[k]
        fx, fy = v5.BACK_C[0] + v5.OFF[0], v5.BACK_C[1] + v5.OFF[1]
        core = {q for q in v5.blob(fx, fy + 1.0, v5.RX * 0.68, v5.RY * 0.52) if q in flesh}
        put(buf, core, fb)
        if k == "redbean":
            top = {q for q in core if (q[0], q[1] - 1) not in core}
            put(buf, top, fa)
            put(buf, v5.spread_dots(v5.erode(core, v5.DISK1) - top, 5, 3, 2), fa)
        else:
            cx0, cy0 = fx, fy + 1.0
            for dx, dy, lean in ((-3.8, 0.8, -1), (0.0, -1.6, 1), (3.6, 0.2, -1)):
                put(buf, v5.pinenut_grain(round(cx0 + dx), round(cy0 + dy), lean) & core, v5.PINENUT_MEAT)
                put(buf, {(round(cx0 + dx), round(cy0 + dy))} & core, fa)

    if s["topping"] == "sesame" and dense:
        reg = front - v5.dilate1(core)
        put(buf, v5.sesame_px(v5.spread_dots(reg, DENSE_SESAME, 3, 7), wide, reg), v5.SESAME_DOT)


def make_one(cid, form):
    """진열장에 쌓는 한 개짜리 — 앞 알(속이 보이는 반쪽)만 그려 딱 맞는 캔버스(바닥 1줄 띄움)에 담는다.
    가로가 홀수면 오른쪽에 빈 1줄을 더한다 (가운데 피벗으로 찍을 때 반 픽셀에 걸리지 않게)."""
    buf = {}
    paint_front(buf, v5.recipe(cid), v5.shapes()[1], form, dense=True)
    xs = [q[0] for q in buf]
    ys = [q[1] for q in buf]
    w = max(xs) - min(xs) + 1
    im = Image.new("RGBA", (w + w % 2, max(ys) - min(ys) + 1 + BOTTOM_GAP), (0, 0, 0, 0))
    p = im.load()
    for (x, y), c in buf.items():
        p[x - min(xs), y - min(ys)] = c
    return im


def settle(buf, n):
    """그린 칸을 캔버스 바닥 1줄 위 · 가로 가운데로 옮겨 그림으로 만든다."""
    xs = [q[0] for q in buf]
    ys = [q[1] for q in buf]
    w = max(xs) - min(xs) + 1
    if w > n or max(ys) - min(ys) + 1 > n - BOTTOM_GAP:
        raise ValueError("그림이 캔버스보다 크다 : %dx%d" % (w, max(ys) - min(ys) + 1))
    dx = (n - w) // 2 - min(xs)
    dy = (n - 1 - BOTTOM_GAP) - max(ys)
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    p = im.load()
    for (x, y), c in buf.items():
        p[x + dx, y + dy] = c
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--form", type=int, choices=(1, 2), required=True, help="외곽선 꼴 : 1 바깥에 더함 · 2 안쪽 둘레")
    ap.add_argument("--out", required=True, help="PNG 를 쓸 폴더")
    ap.add_argument("--one", action="store_true", help="한 개짜리(<id>_one.png)를 굽는다 — 진열장에 쌓는 그림")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    for cid in IDS:
        im = make_one(cid, a.form) if a.one else make(cid, a.form)
        im.save(os.path.join(a.out, cid + ("_one" if a.one else "") + ".png"))
        print("form", a.form, cid, im.getbbox(), "colors", len([c for c in im.getcolors(999) if c[1][3]]))


if __name__ == "__main__":
    main()

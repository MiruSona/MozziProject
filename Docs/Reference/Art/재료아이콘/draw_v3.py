"""재료 아이콘 3판 — 외곽선 1px(selout)을 넣고 바닥에 붙인다 (화풍 통일 A 갈래).

2판(draw_v2.py)의 setD_2tone(그늘 1 + 하이라이트 점)과 다른 점
  · 낱알 · 잎마다 1px 외곽선. 색은 제 색을 짙게 한 따뜻한 색 둘(그늘 쪽 · 밝은 쪽). 순검정은 안 쓴다.
  · 실제 그림을 캔버스 바닥에서 1줄 띄워 붙이고 가로 가운데에 둔다.
  · 외곽선 꼴을 --form 으로 고른다.
      1 = 바깥에 1px 더함 (몸 그대로, 그림이 가로 · 세로 2px 커진다)
      2 = 몸을 1px 줄이고 안쪽 둘레에 선 (그림 크기 그대로, 속이 1px 좁아진다)
  · 꼴 1 은 28 캔버스(바닥 1줄 빼고 27줄)에 들어가게 쑥 줄기를 2줄, 잣을 1줄 줄였다.

모양 · 색은 2판 그대로라 draw_v2 의 함수를 불러 쓴다.

쓰기 : python draw_v3.py --form 1 --out <폴더>
"""
import argparse
import os

from PIL import Image

import draw_v2 as v2

N = v2.N
NAMES = v2.NAMES
DIRS4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
BOTTOM_GAP = 1

# 꼴 1 깨 낱알 자리 (가운데 x · y · 기운 각). 낱알 사이가 선 포함 1칸 이상 뜨게 잡았다.
SESAME_APART = ((6, 5, 200), (20, 4.5, -40), (13, 12, 100), (4.5, 18, 60), (22, 17, 150), (13, 22.5, -10))

# 재료마다 외곽선 둘 : (그늘 쪽 · 밝은 쪽)
OUTLINE = {
    "redbean": ("#3E1616", "#5C1E1E"),
    "bean": ("#4C3A1A", "#7E6126"),
    "mugwort": ("#1F3318", "#33521F"),
    "sesame": ("#4A3A1C", "#7A5E2C"),
    "pinenut": ("#3A2E25", "#75614F"),
    "jujube": ("#3A1810", "#5E2416"),
}


def ring_out(px):
    return {(x + dx, y + dy) for (x, y) in px for dx, dy in DIRS4} - px


def ring_in(px):
    return {p for p in px if any((p[0] + dx, p[1] + dy) not in px for dx, dy in DIRS4)}


def fit_parts(name, form):
    """2판의 면 · 무늬를 받는다. 꼴 1 은 키 큰 둘(쑥 · 잣)을 캔버스에 들어가게 줄인다."""
    if form == 1 and name == "pinenut":
        return pinenut_short()
    if form == 1 and name == "sesame":
        return sesame_apart()
    parts, marks = dict(zip(NAMES, v2.MAKERS))[name]()
    if name == "mugwort":
        # 잎 갈래 사이의 막힌 구멍 둘은 선을 두르면 눈처럼 보여 얼굴이 된다 -> 잎으로 메운다.
        parts[0].px = parts[0].px | holes(parts[0].px)
        if form == 1:
            for part in parts:                   # 줄기 아래 2줄을 뺀다 (27줄 -> 25줄)
                part.px = {q for q in part.px if q[1] <= 25}
    return parts, marks


def holes(px):
    """px 에 둘러싸여 바깥과 4방향으로 안 이어진 빈 칸."""
    seen, todo = set(), [(-1, -1)]
    while todo:
        q = todo.pop()
        if q in seen or q in px or not (-1 <= q[0] <= N and -1 <= q[1] <= N):
            continue
        seen.add(q)
        todo += [(q[0] + dx, q[1] + dy) for dx, dy in DIRS4]
    return {(x, y) for x in range(N) for y in range(N)} - px - seen


def pinenut_short():
    """잣 두 톨 — 2판과 같되 길이 23 -> 22 (꼴 1 에서 세로 27줄 안에 들게)."""
    B, S, H, T = "#F4EACC", "#D6C49A", "#FFFFFF", "#A9743E"
    parts, marks = [], []
    for cx, cy, deg in ((8, 12.5, -100), (20, 15.5, -78)):
        t = v2.teardrop(cx, cy, 22, 10.5, deg, 0.5)
        parts.append(v2.Part(t, B, S, H, v2.dot(int(cx - 3), int(cy - 1), 2, 3), (2, 2)))
        a = v2.math.radians(deg)
        tipx, tipy = cx + 11.0 * v2.math.cos(a), cy + 11.0 * v2.math.sin(a)
        marks.append(({q for q in t if (q[0] + 0.5 - tipx) ** 2 + (q[1] + 0.5 - tipy) ** 2 <= 4.2 ** 2}, T, "both"))
    return parts, marks


def sesame_apart():
    """깨 여섯 톨 — 2판과 같은 색 · 꼴이되 낱알을 조금 줄이고(9.5x6 -> 8x5) 벌려, 바깥 선끼리 안 붙게 한다 (꼴 1)."""
    B, S, H = "#D6B06A", "#B08A48", "#F0D8A0"
    parts = []
    for cx, cy, deg in SESAME_APART:
        t = v2.teardrop(cx, cy, 8, 5, deg, 0.6)
        parts.append(v2.Part(t, B, S, H, v2.dot(int(cx - 1), int(cy - 1), 1, 1), (1, 1)))
    return parts, []


def make(name, form):
    parts, marks = fit_parts(name, form)
    dark, mid = (v2.rgb(c) for c in OUTLINE[name])
    buf, line_px = {}, set()
    for part in parts:
        if form == 1:
            body, line = part.px, ring_out(part.px)
        else:
            line = ring_in(part.px)
            body = part.px - line
        shade_px = set()
        for (x, y) in body:
            c = part.base
            if part.shade and (x + part.off[0], y + part.off[1]) not in body:
                c = part.shade
                shade_px.add((x, y))
            buf[(x, y)] = v2.rgb(c)
        if part.hl and part.hl_at:
            for q in part.hl_at:
                if q in body:
                    buf[q] = v2.rgb(part.hl)
        line_px -= body
        for (x, y) in line:
            # 그늘색 없는 면(줄기)은 통째로 그늘 쪽 선. 나머지는 옆 몸 칸에 그늘이 닿으면 그늘 쪽 선.
            near = [(x + dx, y + dy) for dx, dy in DIRS4 if (x + dx, y + dy) in body]
            buf[(x, y)] = dark if (not part.shade or any(q in shade_px for q in near)) else mid
            line_px.add((x, y))
    for px, col, _ in marks:        # 무늬는 몸 칸에만 (선은 안 덮는다)
        for q in px:
            if q in buf and q not in line_px:
                buf[q] = v2.rgb(col)
    return settle(buf, N)


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
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    for name in NAMES:
        im = make(name, a.form)
        im.save(os.path.join(a.out, name + ".png"))
        print("form", a.form, name, im.getbbox(), "colors", len([c for c in im.getcolors(999) if c[1][3]]))


if __name__ == "__main__":
    main()

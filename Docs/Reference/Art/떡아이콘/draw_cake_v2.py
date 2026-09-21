"""떡 아이콘 3판 — 「반 자른 찹쌀떡」 구도 두 벌.

앞 판(draw_cake.py 의 A·B벌)은 통짜 한 알이라 겉·속·고물 중 하나밖에 못 보여 줬다.
반으로 자르면 **피(겉) · 소(속) · 고물(겉에 묻힌 것)** 세 자리를 한 그림에 담을 수 있어
재료 두셋이 섞인 떡도 한 문법으로 그릴 수 있다. (조사 : 레퍼런스조사.md)

  setC_halfmoon : 반쪽 하나만 크게. 단면이 앞을 본다.
  setD_split    : 뒤에 온전한 한 알, 앞에 반쪽이 기대어 선 3/4 구도. 단면 소재의 정석.

규칙은 앞 판과 같다 — 24x24 · 투명 · 알파 0/255 · 외곽선 없음 · 빛 왼쪽 위 · 장당 3~6색.
도우미(타원 · 물방울 · 풀밭 바탕)는 앞 판 스크립트에서 그대로 가져온다.
"""
import os
from PIL import Image, ImageDraw

from draw_cake import N, rgb, mix, ellipse, dot, grass_bg, font

D = os.path.dirname(os.path.abspath(__file__))

# ---------- 재료가 앉는 자리 ----------
# 자리는 셋뿐이다 : 피(skin) · 소(filling) · 고물(topping).
# 한 재료는 어느 떡에서나 같은 자리에 같은 색으로 나온다. 그래야 팥떡의 팥과
# 팥콩떡의 팥이 같은 재료로 읽힌다.
#   팥   = 소   (팥소. 단면의 주인공)
#   콩   = 고물 (인절미처럼 겉이 온통 노래진다 — 피 색 자체를 바꾼다)
#   쑥   = 피   (쑥떡은 피가 초록이다. 소로 넣으면 쑥떡으로 안 읽힌다)
#   깨   = 고물 (겉에 성긴 점. 콩고물과 같은 자리지만 「전면 도포 vs 점점이」로 갈린다)
#   잣   = 소   (크림색 소에 잣 알이 박힌다)
#   대추 = 피   (대추를 이겨 넣은 붉은 피. 잣과 한 떡에 들어가도 자리가 안 겹친다)
SLOT = {"redbean": "filling", "bean": "topping", "mugwort": "skin",
        "sesame": "topping", "pinenut": "filling", "jujube": "skin"}

# 피 색 (밑색 / 그늘 / 떡살). 떡살은 단면 안쪽의 밝은 살이고, 색을 아끼려고
# 피 하이라이트로도 같이 쓴다 — 그래서 세 재료 떡도 6색을 안 넘는다.
SKIN = {
    None: ("#F2E9DC", "#D8C9B2", "#FFFBF2"),        # 흰 떡. 순백을 피한다 — 밝은 UI 에서 묻혔다
    "mugwort": ("#6E9B4A", "#4C7433", "#C9DCB4"),   # 쑥 피
    "jujube": ("#BF6448", "#8E3B26", "#E8C4B2"),    # 대추 피
    "bean": ("#E6C868", "#C2A048", "#FFFBF2"),      # 콩고물이 덮은 겉. 속살은 흰색 그대로다
}

FILL = {
    "redbean": ("#7A2A2A", "#A84848"),   # 팥소 + 알갱이 점
    "pinenut": ("#EFE2BE", "#A9743E"),   # 크림 소 + 잣 꼭지(잣 알 자체는 떡살 색을 다시 쓴다)
}
SESAME_DOT = "#8A6A34"  # 겉에 찍는 깨. 재료 아이콘 깨의 그늘색이라 같은 재료로 읽힌다


def scatter(px, every):
    """깨를 흩뿌린다. x·y 를 그냥 더해 고르면 점이 대각선 줄로 서서 바느질 자국처럼 보인다.
    자리마다 뒤섞은 값으로 골라야 뿌린 것처럼 보인다."""
    return {q for q in px if ((q[0] * 73856093) ^ (q[1] * 19349663)) % every == 0}

IDS = ["plain_cake", "redbean_cake", "bean_cake", "mugwort_cake", "sesame_cake",
       "pinenut_cake", "jujube_cake", "redbean_bean_cake", "mugwort_sesame_cake",
       "pinenut_jujube_cake", "redbean_mugwort_sesame_cake"]

# Recipe.json 의 id 그대로다. plain_cake 만 그 표에 없는 「아무것도 안 넣은 떡」이다.
PARTS_OF = {i: [] if i == "plain_cake" else i[:-5].split("_") for i in IDS}


def recipe(cid):
    """떡 id 를 세 자리로 나눈다. 없는 자리는 None."""
    slots = {"skin": None, "filling": None, "topping": None}
    for key in PARTS_OF[cid]:
        slots[SLOT[key]] = key
    return slots


# ---------- 모양 도우미 ----------
DISK2 = [(dx, dy) for dx in range(-2, 3) for dy in range(-2, 3) if dx * dx + dy * dy <= 4]


def erode(px):
    """2px 안쪽. 바깥과의 차가 피 띠가 된다. 어떤 실루엣에도 같은 두께로 먹는다."""
    return {p for p in px if all((p[0] + d[0], p[1] + d[1]) in px for d in DISK2)}


def half(cx, cy, rx, ry, floor):
    """아래가 평평한 반달 — 「잘린 떡」이라는 신호다. 바닥 모서리는 한 칸씩 깎아 둥글린다."""
    s = {(x, y) for (x, y) in ellipse(cx, cy, rx, ry) if y <= floor}
    s -= {(x, y) for (x, y) in s if y >= floor - 1 and abs(x + 0.5 - cx) > rx - 1.6}
    return s


def blob(cx, cy, rx, ry):
    """통짜 떡 한 알 — 위는 동글고 아래가 살짝 퍼진다 (앞 판 몸 실루엣의 작은 판)."""
    return ellipse(cx, cy, rx, ry) | ellipse(cx, cy + ry * 0.42, rx * 1.07, ry * 0.55)


def draw(im, px, color):
    p = im.load()
    c = rgb(color)
    for (x, y) in px:
        if 0 <= x < N and 0 <= y < N:
            p[x, y] = c


def shade_edge(px, off=(2, 2)):
    """오른쪽 아래로 밀었을 때 면 밖으로 나가는 칸 = 그늘. 앞 판과 같은 셈법이다."""
    return {q for q in px if (q[0] + off[0], q[1] + off[1]) not in px}


# ---------- 한 덩어리 그리기 ----------
def piece_cut(im, shape, slots, fill_at, seeds, nuts):
    """잘린 반쪽 하나 : 피 띠 + 떡살 + 소 + 고물."""
    sb, ss, sf = SKIN[slots["topping"] if slots["topping"] == "bean" else slots["skin"]]
    if slots["topping"] == "bean" and slots["skin"]:
        # 콩고물 + 색 있는 피가 겹치면 겉은 콩색, 속살만 피 색 계열로 남긴다.
        sf = SKIN[slots["skin"]][2]
    inner = erode(shape)
    draw(im, shape, sb)
    draw(im, shade_edge(shape), ss)
    draw(im, inner, sf)
    key = slots["filling"]
    if key:
        fb, fa = FILL[key]
        core = {q for q in ellipse(*fill_at) if q in inner}
        draw(im, core, fb)
        if key == "redbean":
            draw(im, {q for q in seeds if q in core}, fa)  # 팥알 : 밝은 점 두엇
        else:
            for (nx, ny) in nuts:  # 잣 알 : 떡살 색 낱알 + 갈색 꼭지
                draw(im, {q for q in dot(nx, ny, 2, 2) if q in core}, sf)
                draw(im, {q for q in dot(nx, ny, 1, 1) if q in core}, fa)
    if slots["topping"] == "sesame":
        draw(im, scatter(shape - inner, 4), SESAME_DOT)  # 띠가 얇아 성기게 뿌리면 안 보인다
    return inner


def piece_whole(im, shape, slots, hl_at):
    """안 자른 한 알 : 피와 고물만 보인다."""
    sb, ss, sf = SKIN[slots["topping"] if slots["topping"] == "bean" else slots["skin"]]
    draw(im, shape, sb)
    draw(im, shade_edge(shape), ss)
    draw(im, {q for q in hl_at if q in shape}, sf)  # 왼쪽 위 하이라이트 점
    if slots["topping"] == "sesame":
        # 겉면 전체에 찍되 성기게. 촘촘하면 딸기처럼 보여 떡이 아니게 된다.
        draw(im, scatter(erode(shape), 7), SESAME_DOT)


# ---------- 두 구도 ----------
# 1판은 반달이 납작해 만두처럼 보였다. 키를 13줄로 올리고 폭을 줄여 떡답게 봉긋하게 만든다.
C_SHAPE = half(12, 18.5, 9.2, 12.5, 18)
C_SEEDS = dot(9, 11, 1, 1) | dot(13, 12, 1, 1) | dot(11, 14, 1, 1)

D_BACK = blob(15.5, 10.0, 6.6, 5.8)
D_FRONT = half(8.5, 17.5, 7.4, 9.4, 17)
D_SEEDS = dot(7, 12, 1, 1) | dot(10, 14, 1, 1)


def make(cid, layout):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    s = recipe(cid)
    if layout == "C":
        piece_cut(im, C_SHAPE, s, (12, 12.8, 4.8, 4.0), C_SEEDS, ((8, 11), (13, 13)))
    else:
        # 하이라이트는 2x2 정사각이면 붙인 딱지처럼 보인다. 한 칸 덜어 모서리를 죽인다.
        piece_whole(im, D_BACK - D_FRONT, s, dot(12, 5, 2, 2) - {(13, 5)})
        piece_cut(im, D_FRONT, s, (8.5, 13.6, 3.8, 3.0), D_SEEDS, ((6, 12), (10, 13)))
    return im


# ---------- 비교 그림 ----------
def compare(sets):
    S, COL, PAD = 7, 24 * 7 + 8, 92
    W = PAD + len(IDS) * COL
    head, small = 52, 262
    ph = head + 2 * (24 * S) + small
    panels = [("on grass tile", None, (255, 255, 255)),
              ("dark UI  #2B2B33", (0x2B, 0x2B, 0x33, 255), (235, 235, 240)),
              ("light UI  #E8E0D0", (0xE8, 0xE0, 0xD0, 255), (40, 35, 30))]
    out = Image.new("RGBA", (W, ph * 3), (20, 20, 22, 255))
    f, fs, ft = font(20), font(14), font(13)
    for pi, (title, fill, ink) in enumerate(panels):
        panel = grass_bg(W, ph, S) if fill is None else Image.new("RGBA", (W, ph), fill)
        d = ImageDraw.Draw(panel)
        d.text((6, 6), title, font=f, fill=ink)
        for c, cid in enumerate(IDS):
            # 이름이 칸보다 길면 잘라 옆 칸을 안 침범하게 한다 (redbean_mugwort_sesame).
            nm = cid.replace("_cake", "").replace("redbean", "rb").replace("mugwort", "mg") \
                    .replace("sesame", "se").replace("pinenut", "pn").replace("jujube", "ju")
            d.text((PAD + c * COL + 2, 30), nm, font=ft, fill=ink)
        for r, (label, icons) in enumerate(sets):
            d.text((4, head + r * 24 * S + 24 * S // 2), label, font=fs, fill=ink)
            for c, ic in enumerate(icons):
                panel.alpha_composite(ic.resize((24 * S, 24 * S), Image.NEAREST),
                                      (PAD + c * COL, head + r * 24 * S))
        # 실제 크기 줄 : 게임에서 24 / 48 / 96px 로 쓴다. 작을 때 읽히는지가 이 줄의 목적이다.
        y = head + 2 * 24 * S + 6
        for k, (label, icons) in enumerate(sets):
            d.text((4, y + 40), label + " x4", font=fs, fill=ink)
            for c, ic in enumerate(icons):
                panel.alpha_composite(ic.resize((96, 96), Image.NEAREST), (PAD + c * COL + 24, y))
            y += 100
        d.text((4, y + 14), "x2 / x1", font=fs, fill=ink)
        for c in range(len(IDS)):
            x = PAD + c * COL + 6
            for k in (0, 1):
                panel.alpha_composite(sets[k][1][c].resize((48, 48), Image.NEAREST), (x + k * 80, y))
                panel.alpha_composite(sets[k][1][c], (x + k * 80 + 52, y + 12))
        out.alpha_composite(panel, (0, pi * ph))
    out.save(os.path.join(D, "compare_cake_v2.png"))


def main():
    rows = []
    for folder, layout in (("setC_halfmoon", "C"), ("setD_split", "D")):
        od = os.path.join(D, folder)
        os.makedirs(od, exist_ok=True)
        icons = []
        for cid in IDS:
            im = make(cid, layout)
            im.save(os.path.join(od, cid + ".png"))
            icons.append(im)
            cols = [c for c in im.getcolors(999) if c[1][3]]
            print(folder, cid, im.size, "colors", len(cols))
        rows.append((folder.split("_")[1], icons))
    compare(rows)


if __name__ == "__main__":
    main()

"""떡 아이콘 시안 — PIL 로 직접 짠다. setA_dyed(몸이 재료 색) · setB_topped(흰 몸 + 고물).

재료 아이콘 2판의 확정 화풍(`../재료아이콘/draw_v2.py` 의 setD_2tone)을 그대로 본뜬다.
  밑색 + 그늘 한 단계(오른쪽 아래 테두리) + 왼쪽 위 하이라이트 점 하나.
24x24 · 투명 · 알파 0/255 · 외곽선 없음 · 안티앨리어싱 없음 · 빛은 왼쪽 위.

두 벌은 실루엣이 같다. 같은 떡인지 한눈에 보이게 하려는 것이고,
벌끼리 차이가 「색이냐 고물이냐」 하나로만 남아야 고를 때 헷갈리지 않는다.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ART = r"C:\Mirusona\Project\ClaudeProject\ProjectMozzi2\ProjectMozziUnity\Assets\_Project\Resources\Art"
N = 24

# 흰 떡 몸 색 — 찹쌀이 몸(body_hop0.png)에서 실제로 뽑은 값이다. 눈대중으로 정하지 않는다.
WHITE_B, WHITE_S, WHITE_H = "#FEFDFC", "#E0DFDD", "#FFFFFF"

# 재료 색 (밑색 / 그늘 / 하이라이트) — 재료 아이콘과 같은 팔레트를 쓴다.
ING = {
    "redbean": ("#9A3232", "#742424", "#C45A58"),
    "bean": ("#E6C868", "#C2A048", "#F8E8A8"),
    "mugwort": ("#5F9440", "#47752F", "#86B860"),
    "sesame": ("#D6B06A", "#B08A48", "#F0D8A0"),
    "pinenut": ("#F4EACC", "#D6C49A", "#FFFFFF"),
    "jujube": ("#B0482E", "#863220", "#DC7A58"),
}

# A벌에서 몸을 물들일 때 흰색으로 얼마나 섞나 (0 = 재료 색 그대로).
# 팥·쑥·대추는 원색 그대로면 24px 에서 검게 뭉쳐 떡이 아니라 돌처럼 보여 파스텔로 누인다.
# 반대로 잣은 흰 떡과 구분이 안 되니 오히려 진하게 (음수 = 그늘색 쪽으로 섞는다).
# 팥과 대추는 둘 다 붉은 계열이라 같이 누이면 서로 구분이 안 된다. 팥은 더 진한 자줏빛,
# 대추는 밝은 주홍빛으로 벌린다. 깨는 콩·잣과 가려지게 덜 누인다.
PASTEL = {
    "redbean": 0.28,
    "bean": 0.12,
    "mugwort": 0.38,
    "sesame": 0.06,
    "pinenut": -0.55,
    "jujube": 0.40,
}

IDS = ["plain_cake", "redbean_cake", "bean_cake", "mugwort_cake",
       "sesame_cake", "pinenut_cake", "jujube_cake"]
KEYS = [None, "redbean", "bean", "mugwort", "sesame", "pinenut", "jujube"]


# ---------- 색 ----------
def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def hexs(c):
    return "#%02X%02X%02X" % c[:3]


def mix(h1, h2, t):
    a, b = rgb(h1), rgb(h2)
    return hexs(tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)))


def dye(key):
    """A벌 몸 색 세 개를 만든다. t>0 이면 흰색 쪽, t<0 이면 그늘색 쪽으로 섞는다."""
    b, s, h = ING[key]
    t = PASTEL[key]
    if t >= 0:
        return mix(b, "#FFFFFF", t), mix(s, "#FFFFFF", t * 0.55), mix(h, "#FFFFFF", t)
    t = -t
    return mix(b, s, t), mix(s, s, 0), mix(h, b, t * 0.6)


# ---------- 모양 ----------
def local(x, y, cx, cy, deg):
    a = math.radians(deg)
    dx, dy = x + 0.5 - cx, y + 0.5 - cy
    return dx * math.cos(a) + dy * math.sin(a), -dx * math.sin(a) + dy * math.cos(a)


def ellipse(cx, cy, rx, ry, deg=0):
    out = set()
    for y in range(N):
        for x in range(N):
            u, v = local(x, y, cx, cy, deg)
            if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def teardrop(cx, cy, length, width, deg, taper=0.55):
    """deg 방향(+u)으로 뾰족해지는 물방울."""
    out = set()
    hl = length / 2
    for y in range(N):
        for x in range(N):
            u, v = local(x, y, cx, cy, deg)
            s = u / hl
            if -1 <= s <= 1:
                w = (width / 2) * math.sqrt(1 - s * s) * (1 - taper * (s + 1) / 2) / (1 - taper * 0.35)
                if abs(v) <= w:
                    out.add((x, y))
    return out


def line_px(x0, y0, x1, y1):
    out = set()
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        out.add((int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))))
    return out


def dot(x, y, w=2, h=2):
    return {(x + i, y + j) for i in range(w) for j in range(h)}


# 떡 몸 : 위는 동글고 아래는 살짝 퍼진 모찌 한 알.
# 위 원과 아래 납작한 타원을 겹쳐서 「눌러 앉은」 실루엣을 만든다.
BODY = ellipse(12, 12.0, 8.6, 7.6) | ellipse(12, 15.5, 9.8, 4.6)
BODY = {(x, y) for (x, y) in BODY if 0 <= x < N and 0 <= y < N}


class Part:
    """한 덩어리의 면. off 만큼 밀었을 때 면 밖으로 나가는 픽셀이 그늘이 된다."""

    def __init__(self, px, base, shade=None, hl=None, hl_at=None, off=(2, 2)):
        self.px, self.base, self.shade = px, base, shade
        self.hl, self.hl_at, self.off = hl, hl_at, off


def paint(parts):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p = im.load()
    for part in parts:
        for (x, y) in part.px:
            if 0 <= x < N and 0 <= y < N:
                c = part.base
                if part.shade and (x + part.off[0], y + part.off[1]) not in part.px:
                    c = part.shade
                p[x, y] = rgb(c)
        if part.hl and part.hl_at:
            for (x, y) in part.hl_at:
                if (x, y) in part.px:
                    p[x, y] = rgb(part.hl)
    return im


def body_part(base, shade, hl):
    return Part(BODY, base, shade, hl, dot(7, 6, 2, 2), (2, 2))


# ---------- 고물 (B벌) ----------
def clip(px):
    """고물이 떡 밖으로 삐져나가면 실루엣이 깨진다. 몸 안으로만 자른다."""
    return {q for q in px if q in BODY}


def top_marks(key):
    """흰 몸 위에 얹는 재료. 24px 에서 읽히도록 덩어리는 크게, 점은 성기게 둔다."""
    b, s, h = ING[key]
    out = []
    if key == "redbean":
        # 팥 세 알 — 알끼리 붙으면 덩어리 하나로 보인다. 서로 2px 넘게 떼고 알을 작게 잡는다.
        for cx, cy in ((8.0, 8.5), (15.0, 9.0), (11.5, 13.0)):
            out.append(Part(clip(ellipse(cx, cy, 2.6, 2.0)), b, s, h,
                            clip(dot(int(cx - 2), int(cy - 2), 1, 1)), (1, 1)))
    elif key == "bean":
        # 콩고물 — 작은 점을 흩뿌린다. 덩어리로 보이면 팥과 헷갈린다.
        px = set()
        for x, y in ((6, 9), (9, 7), (12, 9), (15, 7), (17, 10), (8, 12), (13, 13), (16, 14), (10, 15)):
            px |= dot(x, y, 2, 2)
        out.append(Part(clip(px), b, s, None, None, (1, 1)))
    elif key == "mugwort":
        # 쑥잎 — 큰 잎 하나 + 작은 조각 하나. 비슷한 잎 둘을 V 로 놓으면 체크 표시로 읽혀 버린다.
        big = clip(teardrop(10.5, 9.5, 11.0, 5.4, -118, 0.55))
        # 잎에는 하이라이트를 안 준다 — 주면 한 장에 색이 7개가 되어 장당 3~6색 규칙을 넘는다.
        out.append(Part(big, b, s, None, None, (1, 1)))
        out.append(Part(clip(line_px(9.5, 5.5, 12.5, 13.0)), "#3F6A2A"))  # 잎맥 : 잎인 줄 알게 하는 표시
        out.append(Part(clip(teardrop(15.5, 12.5, 6.5, 3.6, -35, 0.55)), b, s, None, None, (1, 1)))
    elif key == "sesame":
        # 깨 — 1~2px 낱알을 점점이. 색이 몸과 가까워 그늘색을 밑색으로 쓴다.
        px = set()
        for x, y in ((7, 8), (10, 6), (13, 8), (16, 9), (8, 12), (12, 11), (15, 13), (10, 15), (14, 16)):
            px |= dot(x, y, 2, 1)
        out.append(Part(clip(px), s, "#8A6A34", None, None, (1, 1)))
    elif key == "pinenut":
        # 잣 두 알 — 흰 몸 위의 크림색은 거의 안 보인다. 밑색을 그늘색으로 내리고
        # 갈색 꼭지를 크게 찍어 「잣」이라고 읽히게 한다. 두 알은 떼어 놓는다.
        for cx, cy, deg in ((8.5, 9.5, -108), (15.0, 11.5, -68)):
            t = clip(teardrop(cx, cy, 9.0, 4.4, deg, 0.5))
            out.append(Part(t, s, "#B39A63", b, clip(dot(int(cx - 1), int(cy - 1), 1, 2)), (1, 1)))
            a = math.radians(deg)
            tipx, tipy = cx + 4.2 * math.cos(a), cy + 4.2 * math.sin(a)
            out.append(Part({q for q in t if (q[0] + .5 - tipx) ** 2 + (q[1] + .5 - tipy) ** 2 <= 2.1 ** 2},
                            "#A9743E"))
    elif key == "jujube":
        # 대추 조각 둘 — 말린 대추 쪽을 눕혀 얹은 모양.
        for cx, cy, deg in ((9.0, 9.0, -25), (14.5, 11.5, 20)):
            out.append(Part(clip(ellipse(cx, cy, 4.4, 2.4, deg)), b, s, h,
                            clip(dot(int(cx - 3), int(cy - 1), 2, 1)), (1, 2)))
    return out


# ---------- 미리보기 ----------
def grass_bg(w, h, scale):
    g = Image.open(os.path.join(ART, "Farm", "ground_grass_0.png")).convert("RGBA")
    g = g.resize((32 * scale, 32 * scale), Image.NEAREST)
    bg = Image.new("RGBA", (w, h))
    for i in range(w // g.width + 1):
        for j in range(h // g.height + 1):
            bg.alpha_composite(g, (i * g.width, j * g.height))
    return bg.crop((0, 0, w, h))


def font(sz):
    try:
        return ImageFont.load_default(size=sz)
    except TypeError:  # 옛 Pillow 는 크기를 못 받는다
        return ImageFont.load_default()


def compare(sets):
    S, COL, PAD = 8, 24 * 8, 86
    W = PAD + 7 * COL
    head, small = 52, 80  # 머리글 두 줄 : 바탕 이름 / 칸 이름. 한 줄에 겹쳐 쓰면 글자가 물린다.
    ph = head + 2 * COL + small + 10
    panels = [
        ("on grass tile", None, (255, 255, 255)),
        ("dark UI  #2B2B33", (0x2B, 0x2B, 0x33, 255), (235, 235, 240)),
        ("light UI  #E8E0D0", (0xE8, 0xE0, 0xD0, 255), (40, 35, 30)),
    ]
    out = Image.new("RGBA", (W, ph * 3), (20, 20, 22, 255))
    f, fs = font(20), font(15)
    for pi, (title, fill, ink) in enumerate(panels):
        panel = grass_bg(W, ph, S) if fill is None else Image.new("RGBA", (W, ph), fill)
        d = ImageDraw.Draw(panel)
        d.text((6, 6), title, font=f, fill=ink)
        for c, cid in enumerate(IDS):
            d.text((PAD + c * COL + 4, 30), cid.replace("_cake", ""), font=fs, fill=ink)
        for r, (label, icons) in enumerate(sets):
            d.text((4, head + r * COL + COL // 2), label, font=fs, fill=ink)
            for c, ic in enumerate(icons):
                panel.alpha_composite(ic.resize((24 * S, 24 * S), Image.NEAREST),
                                      (PAD + c * COL, head + r * COL))
        # 실제 크기 줄 : 작을 때도 재료가 읽히는지 본다.
        # 실제 크기 줄 : A x1 · A x2 · B x1 · B x2 를 한 줄에 나란히. 작아도 재료가 읽히는지 본다.
        y = head + 2 * COL + 10
        d.text((4, y + 20), "A/B  x1 x2", font=fs, fill=ink)
        for c in range(7):
            x = PAD + c * COL + 20
            for k in (0, 1):
                ic = sets[k][1][c]
                panel.alpha_composite(ic, (x + k * 88, y + 18))
                panel.alpha_composite(ic.resize((48, 48), Image.NEAREST), (x + k * 88 + 30, y + 6))
        out.alpha_composite(panel, (0, pi * ph))
    out.save(os.path.join(D, "compare_cake.png"))


def main():
    rows = []
    for folder, mode in (("setA_dyed", "A"), ("setB_topped", "B")):
        od = os.path.join(D, folder)
        os.makedirs(od, exist_ok=True)
        icons = []
        for cid, key in zip(IDS, KEYS):
            if key is None or mode == "B":
                parts = [body_part(WHITE_B, WHITE_S, WHITE_H)]
                if key and mode == "B":
                    parts += top_marks(key)
            else:
                parts = [body_part(*dye(key))]
            im = paint(parts)
            im.save(os.path.join(od, cid + ".png"))
            icons.append(im)
            cols = [c for c in im.getcolors(999) if c[1][3]]
            print(folder, cid, im.size, "colors", len(cols))
        rows.append((folder.split("_")[1], icons))
    compare(rows)


if __name__ == "__main__":
    main()

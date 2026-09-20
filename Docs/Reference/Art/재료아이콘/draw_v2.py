"""재료 아이콘 2판 — PIL 로 직접 짠다. setC_flat(명암 없음) · setD_2tone(그늘 1 + 하이라이트 점 1).
두 벌은 실루엣이 같다. 빛은 왼쪽 위. 28x28 · 투명 · 알파 0/255 · 외곽선 없음."""
import math
import os
from PIL import Image

D = os.path.dirname(os.path.abspath(__file__))
ART = r"C:\Mirusona\Project\ClaudeProject\ProjectMozzi2\ProjectMozziUnity\Assets\_Project\Resources\Art"
N = 28
NAMES = ["redbean", "bean", "mugwort", "sesame", "pinenut", "jujube"]


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


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


def line(x0, y0, x1, y1):
    out = set()
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        out.add((int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))))
    return out


class Part:
    def __init__(self, px, base, shade=None, hl=None, hl_at=None, off=(2, 2)):
        self.px, self.base, self.shade, self.hl, self.hl_at, self.off = px, base, shade, hl, hl_at, off


def paint(parts, marks, tone):
    """parts : 면. marks : (픽셀, 색, 어느 벌에 그리나 'both'|'D'). tone : 'C' 또는 'D'."""
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p = im.load()
    for part in parts:
        for (x, y) in part.px:
            if 0 <= x < N and 0 <= y < N:
                c = part.base
                if tone == "D" and part.shade and (x + part.off[0], y + part.off[1]) not in part.px:
                    c = part.shade
                p[x, y] = rgb(c)
        if tone == "D" and part.hl and part.hl_at:
            for (x, y) in part.hl_at:
                if (x, y) in part.px:
                    p[x, y] = rgb(part.hl)
    for px, col, where in marks:
        if where == "both" or where == tone:
            for (x, y) in px:
                if 0 <= x < N and 0 <= y < N and p[x, y][3]:
                    p[x, y] = rgb(col)
    return im


def dot(x, y, w=2, h=2):
    return {(x + i, y + j) for i in range(w) for j in range(h)}


# ---------- 여섯 종 ----------
def redbean():
    B, S, H, M = "#9A3232", "#742424", "#C45A58", "#F2E4D4"
    parts, marks = [], []
    for cx, cy, deg in ((7.5, 8, -25), (20.5, 10.5, 30), (12, 21, 5)):
        e = ellipse(cx, cy, 6.6, 5, deg)
        parts.append(Part(e, B, S, H, dot(int(cx - 4), int(cy - 3)), (2, 2)))
        a = math.radians(deg)  # 팥 배꼽 : 흰 짧은 줄
        ux, uy = math.cos(a), math.sin(a)
        mx, my = cx + 1.5 * -uy * -1, cy + 1.5 * ux * -1
        marks.append((line(cx - 2 * ux + 2 * uy - 0.5, cy - 2 * uy - 2 * ux - 0.5,
                           cx + 2 * ux + 2 * uy - 0.5, cy + 2 * uy - 2 * ux - 0.5), M, "both"))
    return parts, marks


def bean():
    B, S, H, M = "#E6C868", "#C2A048", "#F8E8A8", "#8A6430"
    parts, marks = [], []
    for cx, cy in ((14, 8), (7.5, 19), (20.5, 19)):
        e = ellipse(cx, cy, 6.5, 6.5)
        parts.append(Part(e, B, S, H, dot(int(cx - 4), int(cy - 4)), (2, 2)))
        marks.append((dot(int(cx + 1), int(cy), 2, 1), M, "both"))  # 콩 눈
    return parts, marks


def mugwort():
    B, S, H, V = "#5F9440", "#47752F", "#86B860", "#3F6A2A"
    leaf = set()
    # 깊게 갈라진 깃꼴 잎 : 가운데 긴 갈래 + 위로 뻗는 옆 갈래 두 쌍 + 잎자루
    leaf |= teardrop(14, 8.5, 16, 7.5, -90, 0.5)
    for sgn in (-1, 1):
        leaf |= teardrop(14 + sgn * 3.5, 6.5, 7, 3.6, -90 - sgn * 50, 0.5)      # 가운데 갈래의 톱니
        leaf |= teardrop(14 + sgn * 6, 12.5, 15, 6.4, -90 - sgn * 50, 0.5)     # 위 옆 갈래
        leaf |= teardrop(14 + sgn * 10, 8.5, 6, 3.4, -90 - sgn * 15, 0.5)      # 옆 갈래의 톱니
        leaf |= teardrop(14 + sgn * 6.5, 19.5, 13, 5.6, -90 - sgn * 80, 0.5)   # 아래 옆 갈래
    leaf |= {(x, y) for x in (12, 13, 14, 15) for y in range(12, 23)}
    stem = {(13, y) for y in range(23, 28)} | {(14, y) for y in range(23, 28)}
    parts = [Part(leaf, B, S, H, dot(12, 4, 1, 3), (1, 1)), Part(stem, V)]
    marks = [(line(13, 8, 13, 23), V, "both")]
    for sgn in (-1, 1):
        marks.append((line(13.5 + sgn * 0.5, 16, 13.5 + sgn * 8, 10), V, "both"))
        marks.append((line(13.5 + sgn * 0.5, 21, 13.5 + sgn * 9, 19), V, "both"))
    return parts, marks


def sesame():
    B, S, H = "#D6B06A", "#B08A48", "#F0D8A0"
    parts = []
    for cx, cy, deg in ((7, 6.5, 200), (19.5, 5.5, -40), (13.5, 14, 100), (5.5, 20, 60), (21.5, 19, 150), (13, 24, -10)):
        t = teardrop(cx, cy, 9.5, 6, deg, 0.6)
        parts.append(Part(t, B, S, H, dot(int(cx - 1), int(cy - 2), 1, 1), (1, 1)))
    return parts, []


def pinenut():
    B, S, H, T = "#F4EACC", "#D6C49A", "#FFFFFF", "#A9743E"
    parts, marks = [], []
    for cx, cy, deg in ((8, 12.5, -100), (20, 15.5, -78)):
        t = teardrop(cx, cy, 23, 10.5, deg, 0.5)
        parts.append(Part(t, B, S, H, dot(int(cx - 3), int(cy - 1), 2, 3), (2, 2)))
        a = math.radians(deg)
        tipx, tipy = cx + 11.5 * math.cos(a), cy + 11.5 * math.sin(a)
        marks.append(({q for q in t if (q[0] + 0.5 - tipx) ** 2 + (q[1] + 0.5 - tipy) ** 2 <= 4.2 ** 2}, T, "both"))
    return parts, marks


def jujube():
    B, S, H, W, ST = "#B0482E", "#863220", "#DC7A58", "#8E3622", "#6A4A28"
    body = ellipse(14, 15.5, 12.5, 8.2, -40)
    parts = [Part(line(22, 5, 25, 1) | line(23, 5, 26, 1), ST), Part(body, B, S, H, dot(7, 14, 2, 3), (2, 3))]
    marks = [(line(12, 12, 16, 9), W, "both"), (line(12, 20, 17, 16), W, "both"), (dot(21, 6, 2, 2), W, "both")]
    return parts, marks


MAKERS = [redbean, bean, mugwort, sesame, pinenut, jujube]


# ---------- 미리보기 ----------
def grass_bg(w_tiles, h_tiles):
    g = Image.open(os.path.join(ART, "Farm", "ground_grass_0.png")).convert("RGBA")
    bg = Image.new("RGBA", (32 * w_tiles, 32 * h_tiles))
    for i in range(w_tiles):
        for j in range(h_tiles):
            bg.alpha_composite(g, (i * 32, j * 32))
    return bg


def previews(od, icons):
    S = 8
    row = Image.new("RGBA", (6 * 32 * S, 32 * S), (60, 60, 60, 255))
    bg = grass_bg(7, 2)
    for i, ic in enumerate(icons):
        row.alpha_composite(ic.resize((N * S, N * S), Image.NEAREST), (i * 32 * S + 2 * S, 2 * S))
        bg.alpha_composite(ic, (8 + i * 36, 2))
        bg.alpha_composite(ic.resize((18, 18), Image.NEAREST), (13 + i * 36, 40))
    row.save(os.path.join(od, "preview_x8.png"))
    bg.resize((bg.width * 4, bg.height * 4), Image.NEAREST).save(os.path.join(od, "preview_on_grass.png"))


def main():
    sets = {}
    for tone, folder in (("C", "setC_flat"), ("D", "setD_2tone")):
        od = os.path.join(D, folder)
        os.makedirs(od, exist_ok=True)
        icons = []
        for name, mk in zip(NAMES, MAKERS):
            parts, marks = mk()
            im = paint(parts, marks, tone)
            im.save(os.path.join(od, name + ".png"))
            cols = [c for c in im.getcolors(999) if c[1][3]]
            print(folder, name, im.size, im.getbbox(), "colors", len(cols))
            icons.append(im)
        previews(od, icons)
        sets[tone] = icons
    # 비교 한 장 : 풀밭 위 윗줄 C · 아랫줄 D + 찹쌀이 몸 + 담장
    bg = grass_bg(9, 2)
    for r, tone in enumerate("CD"):
        for i, ic in enumerate(sets[tone]):
            bg.alpha_composite(ic, (4 + i * 32, 4 + r * 32))
    body = Image.open(os.path.join(ART, "Mozzi", "body_hop0.png")).convert("RGBA")
    bg.alpha_composite(body, (6 * 32 + 16, -2))
    fence = Image.open(os.path.join(ART, "Farm", "fence_top.png")).convert("RGBA")
    bg.alpha_composite(fence, (6 * 32, 32))
    bg.alpha_composite(fence, (8 * 32, 32))
    bg.resize((bg.width * 8, bg.height * 8), Image.NEAREST).save(os.path.join(D, "compare_v2.png"))


if __name__ == "__main__":
    main()

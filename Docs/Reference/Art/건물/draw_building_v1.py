"""건물 2채(합성소·가판대) 1판 — PIL 로 직접 짠다. C벌(그늘 없음) · D벌(그늘 1단계 + 하이라이트).
두 벌은 실루엣이 같다. 빛은 왼쪽 위. 140x64 · 투명 · 알파 0/255(+그림자 56) · 외곽선 없음 · 낮은 탑다운.
재료아이콘/draw_v2.py 와 같은 방식(Part/paint/마크)을 건물 크기에 맞게 옮겨 적었다."""
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))  # 저장소 뿌리
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 140, 64
CX = W // 2  # 70 — 좌우 대칭의 기준

# 글자가 얹히는 자리(가운데)와 알림 점 자리(오른쪽 위). 여기엔 알맹이를 두지 않는다.
SAFE = (35, 20, 105, 44)      # x0, y0, x1, y1 (끝 제외)
BADGE = (W - 14, 0, W, 14)

# ---------- 색 ----------
# 나무는 담장(fence_bottom.png)과 같은 벌이라야 한 세트로 보인다
WOOD_HI = "#D0A576"
WOOD_1 = "#B78B60"
WOOD_2 = "#966D48"
WOOD_3 = "#755438"
WOOD_4 = "#583E2A"
NAIL = "#606068"

TILE_A = "#4A5068"   # 기와 가로 줄 두 톤
TILE_B = "#393651"
TILE_HI = "#5C6486"  # 용마루
TILE_DK = "#2E2B42"  # 기와 이음선 겸 그늘

IRON = "#4A4753"     # 무쇠 솥
IRON_S = "#332F3C"
IRON_HI = "#6B677A"
SOUP = "#AE74DB"     # 은은한 보랏빛 국물 — 음침하지 않게 밝은 쪽
SOUP_HI = "#C99BE8"
STEAM = "#FEFDFC"
STEAM_S = "#E0DFDD"

PINK = "#F8BBC0"     # 찹쌀이 분홍
PINK_S = "#E0959C"
WHITE = "#FEFDFC"
WHITE_S = "#DCD9D6"

MOCHI_W = "#FEFDFC"
MOCHI_G = "#7FA05A"
MOCHI_R = "#8E4A4A"
MOCHI_S = {MOCHI_W: "#DCD9D6", MOCHI_G: "#688444", MOCHI_R: "#733838"}
PLATE = "#D8CEBE"
PLATE_S = "#B3A691"
SHOP_DK = "#6B4E36"   # 가게 안쪽 어둠 — 흰 글자가 읽히면서 검지 않은 선
SHOP_DK_S = "#583E2A"

SHADOW_A = 56        # 오른쪽 아래 2px 옅은 그림자


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 도우미 ----------
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def ellipse(cx, cy, rx, ry):
    out = set()
    for y in range(max(0, int(cy - ry) - 1), min(H, int(cy + ry) + 2)):
        for x in range(max(0, int(cx - rx) - 1), min(W, int(cx + rx) + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def line(x0, y0, x1, y1):
    out = set()
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        out.add((int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))))
    return out


def poly(points, w=2):
    """점들을 이어 굵기 w 의 띠로 만든다 (김·밧줄 같은 가는 것에 쓴다)."""
    out = set()
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        for (x, y) in line(x0, y0, x1, y1):
            out |= {(x + i, y + j) for i in range(w) for j in range(w)}
    return out


def dot(x, y, w=2, h=2):
    return {(x + i, y + j) for i in range(w) for j in range(h)}


def trapezoid(cx, y0, y1, hw0, hw1):
    """위가 좁고 아래가 넓은 사다리꼴. 지붕·차양의 바탕이다."""
    px, cols = set(), {}
    for y in range(y0, y1):
        t = (y - y0) / max(1, (y1 - 1 - y0))
        hw = hw0 + (hw1 - hw0) * t
        for x in range(int(round(cx - hw)), int(round(cx + hw)) + 1):
            px.add((x, y))
            cols.setdefault(x, []).append(y)
    return px, cols


def outside_safe(px):
    """글자 자리를 피한다 — 김처럼 가운데를 가리면 안 되는 것에 쓴다."""
    x0, y0, x1, y1 = SAFE
    return {(x, y) for (x, y) in px if not (x0 <= x < x1 and y0 <= y < y1)}


class Part:
    def __init__(self, px, base, shade=None, hl=None, off=(2, 2), hoff=(1, 1)):
        self.px, self.base, self.shade, self.hl, self.off, self.hoff = px, base, shade, hl, off, hoff


def paint(parts, marks, tone):
    """tone 'C' = 바탕색 + 무늬만. 'D' = 오른쪽 아래 가장자리 그늘 + 왼쪽 위 하이라이트."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    p = im.load()
    for part in parts:
        for (x, y) in part.px:
            if 0 <= x < W and 0 <= y < H:
                c = part.base
                if tone == "D" and part.shade and (x + part.off[0], y + part.off[1]) not in part.px:
                    c = part.shade
                p[x, y] = rgb(c)
        if tone == "D" and part.hl:
            for (x, y) in part.px:
                if 0 <= x < W and 0 <= y < H and (x - part.hoff[0], y - part.hoff[1]) not in part.px:
                    p[x, y] = rgb(part.hl)
    for px, col, where in marks:
        if where in ("both", tone):
            for (x, y) in px:
                if 0 <= x < W and 0 <= y < H and p[x, y][3]:
                    p[x, y] = rgb(col)
    # 바닥에 붙어 보이게 오른쪽 아래 2px 옅은 그림자 — 두 벌 모두 같게 둬야 실루엣이 같다
    sil = {(x, y) for part in parts for (x, y) in part.px if 0 <= x < W and 0 <= y < H}
    for (x, y) in sil:
        for dx, dy in ((2, 2),):
            q = (x + dx, y + dy)
            if 0 <= q[0] < W and 0 <= q[1] < H and q not in sil and p[q][3] == 0:
                p[q] = (0, 0, 0, SHADOW_A)
    return im


# ---------- 나무 부품 ----------
def draw_pillar(parts, marks, x0, y0, y1, w=10):
    """굵은 기둥 하나. 나뭇결 세로줄 · 옹이 · 못까지 넣어야 담장과 같은 벌로 보인다."""
    body = rect(x0, y0 + 3, x0 + w, y1)
    cap = rect(x0, y0, x0 + w, y0 + 3)          # 기둥 윗면(탑다운이라 보인다)
    parts.append(Part(body, WOOD_2, WOOD_3, WOOD_HI))
    parts.append(Part(cap, WOOD_1, WOOD_2, WOOD_HI))
    for i in (2, 6):
        marks.append(({(x0 + i, y) for y in range(y0 + 5, y1 - 2)}, WOOD_3, "both"))
    marks.append((dot(x0 + 4, y0 + 12, 2, 2), WOOD_4, "both"))      # 옹이
    marks.append((dot(x0 + 3, y1 - 9, 2, 2), NAIL, "both"))         # 못
    marks.append((dot(x0 + 7, y1 - 16, 2, 2), NAIL, "both"))


def draw_plank_wall(parts, marks, x0, y0, x1, y1, plank=5, avoid=frozenset()):
    """가로 판자 벽. 판자 한 장이 Part 하나라 D벌에서 장마다 그늘 줄이 생긴다.
    avoid = 이음선을 긋지 않을 자리(글자 판). 마크는 Part 뒤에 칠해지므로 여기서 빼 둬야 평평하게 남는다."""
    y = y0
    while y < y1:
        h = min(plank, y1 - y)
        parts.append(Part(rect(x0, y, x1, y + h), WOOD_2, WOOD_3, None))
        marks.append(({(x, y + h - 1) for x in range(x0, x1)} - avoid, WOOD_4, "C"))
        y += h
    marks.append((dot(x0 + 5, y0 + 7, 2, 2) - avoid, WOOD_4, "both"))
    marks.append((dot(x1 - 9, y1 - 8, 2, 2) - avoid, WOOD_4, "both"))


def draw_tile_roof(parts, marks, cx, y0, y1, hw0, hw1, band=4):
    """기와 지붕. 가로 줄 두 톤 + 세로 이음선.
    줄 하나가 아니라 **줄 묶음(band)**이 Part 다 — 1px Part 로 하면 D벌에서 전부 그늘이 돼 새까매진다."""
    px, _ = trapezoid(cx, y0, y1, hw0, hw1)
    rows = {}
    for (x, y) in px:
        rows.setdefault(y, set()).add(x)
    ys = sorted(rows)
    parts.append(Part({(x, y) for y in ys[:2] for x in rows[y]}, TILE_HI, TILE_A, None))  # 용마루
    i = 0
    for top in range(ys[0] + 2, ys[-1] + 1, band):
        grp = [y for y in ys if top <= y < top + band]
        if not grp:
            continue
        bpx = {(x, y) for y in grp for x in rows[y]}
        parts.append(Part(bpx, TILE_A if i % 2 == 0 else TILE_B, TILE_DK, None))
        off = 0 if i % 2 == 0 else 5
        marks.append(({(x, y) for y in grp[:-1] for x in rows[y] if (x + off) % 10 == 0},
                      TILE_DK, "both"))
        marks.append(({(x, grp[-1]) for x in rows[grp[-1]]}, TILE_DK, "C"))  # C벌은 줄만으로 기와를 읽힌다
        i += 1
    return px


# ---------- 합성소 ----------
def synthesis():
    parts, marks = [], []
    # 기둥이 먼저 — 벽과 지붕이 그 위를 덮어야 뒤에 선 것처럼 보인다
    draw_pillar(parts, marks, 10, 19, 60)
    draw_pillar(parts, marks, 121, 19, 60)
    panel = rect(32, 22, 108, 43)
    draw_plank_wall(parts, marks, 24, 21, 116, 57, avoid=panel)
    # 글자가 얹힐 가운데 판 — 평평하고 조금 어둡게 둬야 흰 글자가 읽힌다
    parts.append(Part(panel, WOOD_3, WOOD_4, None))
    draw_tile_roof(parts, marks, CX, 3, 22, 48, 62)

    # 둥근 투입구 둘 — 「둘 넣어 하나」를 그림만으로 읽히게 한다. 솥보다 바깥으로 벌려 둔다
    for cx in (38, 102):
        parts.append(Part(ellipse(cx, 50, 7.0, 6.5), WOOD_1, WOOD_2, WOOD_HI))
        parts.append(Part(ellipse(cx, 50.5, 4.6, 4.2), "#3A2A1C", None, None))
        marks.append((line(cx - 3, 47, cx + 1, 46), WOOD_3, "both"))  # 구멍 안쪽 턱

    # 가마솥 — 밝은 무쇠 3색이라 마녀 솥처럼 음침하지 않다
    body = {(x, y) for (x, y) in ellipse(CX, 51, 15, 10) if y >= 46}
    parts.append(Part(body, IRON, IRON_S, IRON_HI))
    # 솥 전은 몸통과 같은 무쇠색이다 — 밝은 색으로 두면 벽을 가로지르는 흰 막대처럼 보인다
    parts.append(Part(ellipse(CX, 45.5, 15, 3.4), IRON, IRON_S, IRON_HI))
    parts.append(Part(ellipse(CX, 45.5, 11.5, 2.2), SOUP, None, SOUP_HI))     # 국물
    marks.append((dot(CX - 7, 45, 3, 1) | dot(CX + 3, 46, 2, 1), SOUP_HI, "both"))
    marks.append(({(x, 57) for x in range(CX - 10, CX + 11)}, IRON_S, "C"))   # C벌 띠(솥 배)
    for lx in (CX - 12, CX + 8):
        parts.append(Part(rect(lx, 59, lx + 4, 62), IRON_S, None, None))      # 솥 다리

    # 김 — 둥근 뭉게 세 덩이씩. 가는 줄로 그리면 벽에 난 금처럼 보여서 뭉텅이로 바꿨다
    for sgn in (-1, 1):
        # 덩이끼리 겹쳐 한 가닥으로 이어야 김으로 읽힌다. 떨어뜨려 놓으면 구슬처럼 보인다
        puffs = [(CX + sgn * 40, 43, 4.2, 3.2), (CX + sgn * 44, 38, 3.6, 3.0),
                 (CX + sgn * 40, 33, 3.0, 2.6), (CX + sgn * 44, 28, 2.2, 2.0)]
        s = set()
        for (px_, py_, rx, ry) in puffs:
            s |= ellipse(px_, py_, rx, ry)
        parts.append(Part(outside_safe(s), STEAM, STEAM_S, None))
    return parts, marks


# ---------- 가판대 ----------
def draw_awning(parts, marks, cx, y0, y1, hw0, hw1):
    """줄무늬 천 차양. 아래 가장자리를 20px 주기로 파 천처럼 보이게 한다."""
    px, cols = trapezoid(cx, y0, y1, hw0, hw1)
    cloth = set()
    for x, ys in cols.items():
        notch = 2 - round(2 * abs((x % 20) - 10) / 10)  # 깊으면 밑의 어둠이 들쭉날쭉 드러난다
        ymax = max(ys) - notch
        cloth |= {(x, y) for y in ys if y <= ymax}
    for i in range(0, 20):
        xs = range(-60 + i * 10, -60 + i * 10 + 10)
        band = {(x, y) for (x, y) in cloth if x in xs}
        if not band:
            continue
        pink = ((i + 60 // 10) % 2 == 0)
        parts.append(Part(band, PINK if pink else WHITE, PINK_S if pink else WHITE_S, None))
    return cloth


def draw_counter(parts, marks, x0, x1, ytop, yfront, ybot):
    """상판 : 윗면(밝게)과 앞면(어둡게) 두 장. 낮은 탑다운이라 둘 다 보인다."""
    parts.append(Part(rect(x0, ytop, x1, yfront), WOOD_1, WOOD_2, WOOD_HI))
    parts.append(Part(rect(x0 + 2, yfront, x1 - 2, ybot), WOOD_2, WOOD_3, None))
    for x in range(x0 + 12, x1 - 6, 22):
        marks.append(({(x, y) for y in range(yfront + 1, ybot - 1)}, WOOD_3, "both"))
    marks.append(({(x, yfront) for x in range(x0 + 2, x1 - 2)}, WOOD_3, "both"))
    for x in range(x0 + 8, x1 - 6, 22):
        marks.append((dot(x, yfront + 3, 2, 2), NAIL, "both"))


def draw_mochi(parts, marks, cx, color):
    """접시 + 떡 한 덩이. 떡은 찹쌀이와 같은 흰 계열이라 튀지 않는다."""
    parts.append(Part(ellipse(cx, 46, 9.0, 2.8), PLATE, PLATE_S, None))
    dome = {(x, y) for (x, y) in ellipse(cx, 43.5, 6.5, 5.5) if y <= 45}
    parts.append(Part(dome, color, MOCHI_S.get(color), "#FFFFFF" if color != MOCHI_W else None))
    marks.append((dot(cx - 4, 41, 2, 1), "#FFFFFF", "both"))  # 떡 윤기 점
    # 흰 떡은 접시와 색이 붙어 버린다 — 닿는 줄 하나로 떼어 놓는다
    marks.append(({(x, 45) for x in range(cx - 6, cx + 7)}, PLATE_S, "both"))


def stall():
    parts, marks = [], []
    draw_pillar(parts, marks, 10, 19, 60)
    draw_pillar(parts, marks, 121, 19, 60)
    # 가게 안쪽 어둠 — 여기가 글자 자리다. 평평하게 둔다
    parts.append(Part(rect(20, 18, 121, 46), SHOP_DK, SHOP_DK_S, None))
    draw_awning(parts, marks, CX, 2, 22, 50, 62)
    draw_counter(parts, marks, 8, 133, 44, 50, 58)
    for cx, col in ((24, MOCHI_W), (40, MOCHI_G), (100, MOCHI_R), (116, MOCHI_W)):
        draw_mochi(parts, marks, cx, col)
    # 왼쪽 기둥에 붙인 작은 나무 간판. 끈으로 매달면 기둥과 겹쳐 창틀처럼 보여서 기둥에 박은 꼴로 바꿨다
    parts.append(Part(rect(3, 25, 25, 37), WOOD_1, WOOD_3, WOOD_HI))
    parts.append(Part(rect(6, 28, 22, 34), WOOD_2, WOOD_3, None))
    marks.append((rect(9, 30, 19, 31), WOOD_4, "both"))
    marks.append((dot(4, 26) | dot(22, 34), NAIL, "both"))
    return parts, marks


# ---------- 검산 ----------
def report(name, im):
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    alphas = sorted({c[1][3] for c in im.getcolors(99999)})
    p = im.load()
    x0, y0, x1, y1 = SAFE
    safe_cols = {p[x, y] for x in range(x0, x1) for y in range(y0, y1)}
    bx0, by0, bx1, by1 = BADGE
    badge = sum(1 for x in range(bx0, bx1) for y in range(by0, by1) if p[x, y][3])
    print(f"{name}: size={im.size} alphas={alphas} colors={len(cols)} "
          f"safebox_colors={len(safe_cols)} badgebox_opaque={badge}")
    return {(x, y) for x in range(W) for y in range(H) if p[x, y][3] == 255}


# ---------- 비교 그림 ----------
def tile_img(path):
    return Image.open(path).convert("RGBA")


def scene(bld_syn, bld_stall, label, with_text):
    """실제 화면 배치대로 : 어두운 띠 위 건물 둘 + 담장 윗줄 + 풀밭 두 줄 + 찹쌀이."""
    SW, BAND = 360, 88
    grass = tile_img(os.path.join(ART, "Farm", "ground_grass_0.png"))
    fence = tile_img(os.path.join(ART, "Farm", "fence_top.png"))
    body = tile_img(os.path.join(ART, "Mozzi", "body_hop0.png"))
    sh = BAND + 88
    im = Image.new("RGBA", (SW, sh), (0x2B, 0x2B, 0x33, 255))
    for i in range(SW // 32 + 1):
        for j in range(2):
            im.alpha_composite(grass, (i * 32, BAND + 24 + j * 32))
    im.alpha_composite(bld_syn, (20, 8))
    im.alpha_composite(bld_stall, (200, 8))
    for i in range(SW // 32 + 1):
        im.alpha_composite(fence, (i * 32, BAND - 8))
    im.alpha_composite(body, (150, BAND + 8))
    if with_text:
        d = ImageDraw.Draw(im)
        try:
            f = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 16)
        except OSError:
            f = None
        for x0, t in ((20, "합성소"), (200, "가판대")):
            cx, cy = x0 + W // 2, 8 + H // 2
            if f:
                d.text((cx, cy), t, font=f, fill=(255, 255, 255, 255), anchor="mm")
            else:
                d.rectangle([cx - 30, cy - 9, cx + 30, cy + 9], fill=(255, 255, 255, 255))
            d.ellipse([x0 + W - 15, 8 + 1, x0 + W - 2, 8 + 14], fill=(0xF2, 0x33, 0x33, 255))
    d = ImageDraw.Draw(im)
    try:
        lf = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 12)
        d.text((4, 2), label, font=lf, fill=(255, 255, 255, 255))
    except OSError:
        pass
    return im


def main():
    os.makedirs(D, exist_ok=True)
    sils, imgs = {}, {}
    for tone in ("C", "D"):
        for name, mk in (("synthesis", synthesis), ("stall", stall)):
            parts, marks = mk()
            im = paint(parts, marks, tone)
            im.save(os.path.join(D, f"{name}_{tone}.png"))
            sils[(name, tone)] = report(f"{name}_{tone}", im)
            imgs[(name, tone)] = im
    for name in ("synthesis", "stall"):
        same = sils[(name, "C")] == sils[(name, "D")]
        print(f"{name}: C·D 불투명(알파255) 실루엣 같은가 = {same} "
              f"(C {len(sils[(name,'C')])}px / D {len(sils[(name,'D')])}px)")

    scenes = []
    for tone in ("C", "D"):
        for wt in (False, True):
            scenes.append(scene(imgs[("synthesis", tone)], imgs[("stall", tone)],
                                tone + ("+글자" if wt else ""), wt))
    sw, shh = scenes[0].size
    cmp_im = Image.new("RGBA", (sw, shh * len(scenes)), (0x1A, 0x1A, 0x20, 255))
    for i, s in enumerate(scenes):
        cmp_im.alpha_composite(s, (0, i * shh))
    cmp_im.resize((sw * 3, shh * len(scenes) * 3), Image.NEAREST).save(
        os.path.join(D, "compare_building_v1.png"))

    S = 6
    close = Image.new("RGBA", ((W * S + 12) * 2, (H * S + 12) * 2), (0x2B, 0x2B, 0x33, 255))
    for i, key in enumerate((("synthesis", "C"), ("stall", "C"), ("synthesis", "D"), ("stall", "D"))):
        big = imgs[key].resize((W * S, H * S), Image.NEAREST)
        close.alpha_composite(big, (6 + (i % 2) * (W * S + 12), 6 + (i // 2) * (H * S + 12)))
    close.save(os.path.join(D, "closeup_building_v1.png"))
    print("compare/closeup saved")


if __name__ == "__main__":
    main()

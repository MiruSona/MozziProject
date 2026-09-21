"""건물 2판 — 합성소 = 귀여운 합성 기계, 가판대 = 현대 카페풍 떡 가게.
1판(전통 기와·가마솥·천막 좌판)은 사용자가 물렸다. 여기서는 **모양 안 두 개씩**(A·B)을 내고 톤은 전부 D다.
 A = 글자가 정중앙 70x24 에 얹힌다는 가정 / B = 글자가 맨 아래 18px 띠로 내려간다는 가정.
규격 : 140x64 · 투명 · 알파 0/56/255 · 외곽선 없음 · 빛 왼쪽 위 · 오른쪽 아래 2px 그림자.
참고(읽기 자료) : Kenney industrial(굵은 파이프·플랜지), OGA vending(유리 안에 내용물),
 Kenney modern city(유리창 대각선 반사줄), OGA coffeeshop(간판띠→어닝→창→문 순서)."""
import math
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 140, 64
CX = W // 2
SAFE_A = (35, 20, 105, 44)    # 글자가 정중앙에 얹히는 안
SAFE_B = (35, 46, 105, 62)    # 글자가 아래 띠로 내려간 안
BADGE = (W - 14, 0, W, 14)
SHADOW_A = 56

# ---------- 색 ----------
# 기계 : 파스텔 연보라 + 놋쇠. 공장처럼 칙칙하면 안 되니 회색 대신 보라 계열을 몸통으로 쓴다
BODY, BODY_S, BODY_HI = "#B9A6DE", "#9A85C2", "#D6C9EE"
DEEP, DEEP_S = "#6E5A99", "#54417A"          # 명판·받침 (흰 글자가 읽히는 중간 어두움)
BRASS, BRASS_S, BRASS_HI = "#E8B863", "#BE8F3E", "#F7DC9C"
GLASS, GLASS_S, GLASS_HI = "#CFE9F5", "#A9C9DA", "#FFFFFF"
SLOT = "#4A3F63"                              # 배출구 어둠
MOCHI, MOCHI_S = "#FEFDFC", "#DCD9D6"

# 카페 : 크림 벽 + 찹쌀이 분홍 포인트. 나무는 문틀·카운터에만 쓴다
WALL, WALL_S, WALL_HI = "#F4EEE4", "#DCD3C4", "#FFFFFF"
PINK, PINK_S = "#F8BBC0", "#DE9AA1"
HOT = "#FA6270"
WIN, WIN_S, WIN_HI = "#5A6B78", "#46545F", "#93A9B6"   # 창유리 청회색 한 톤 + 반사
SHOW, SHOW_S = "#C6DCE6", "#A5C0CD"                     # 쇼케이스 유리 — 흰 떡이 묻히지 않게 한 단계 낮췄다
WOOD_1, WOOD_2, WOOD_3, WOOD_4 = "#B78B60", "#966D48", "#755438", "#583E2A"
WOOD_HI, NAIL = "#D0A576", "#606068"
LEAF, LEAF_S = "#7FA05A", "#688444"
POT, POT_S = "#C98A6A", "#A76C50"
MOCHI_G, MOCHI_R = "#7FA05A", "#8E4A4A"
MOCHI_SH = {MOCHI: MOCHI_S, MOCHI_G: "#688444", MOCHI_R: "#733838"}


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 도우미 (1판에서 옮겨 적고 rrect·ring·spiral 을 더했다) ----------
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def rrect(x0, y0, x1, y1, r=3):
    """모서리를 둥글린 네모. 기계·벽을 장난감처럼 보이게 하는 가장 싼 방법이다."""
    out = set()
    for y in range(y0, y1):
        for x in range(x0, x1):
            dx = max(x0 + r - 1 - x, 0, x - (x1 - r))
            dy = max(y0 + r - 1 - y, 0, y - (y1 - r))
            if dx * dx + dy * dy <= r * r:
                out.add((x, y))
    return out


def ellipse(cx, cy, rx, ry):
    out = set()
    for y in range(max(0, int(cy - ry) - 1), min(H, int(cy + ry) + 2)):
        for x in range(max(0, int(cx - rx) - 1), min(W, int(cx + rx) + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def ring(cx, cy, r_out, r_in):
    return ellipse(cx, cy, r_out, r_out) - ellipse(cx, cy, r_in, r_in)


def line(x0, y0, x1, y1):
    out = set()
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        out.add((int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))))
    return out


def poly(points, w=2):
    out = set()
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        for (x, y) in line(x0, y0, x1, y1):
            out |= {(x + i - w // 2, y + j - w // 2) for i in range(w) for j in range(w)}
    return out


def spiral(cx, cy, r0, r1, t0, t1, w=2, steps=40):
    """회전 창 안의 소용돌이. 「돌아간다」를 한 눈에 읽히게 한다."""
    pts = []
    for i in range(steps + 1):
        t = t0 + (t1 - t0) * i / steps
        r = r0 + (r1 - r0) * i / steps
        pts.append((cx + r * math.cos(t), cy + r * math.sin(t)))
    return poly(pts, w)


def dot(x, y, w=2, h=2):
    return {(x + i, y + j) for i in range(w) for j in range(h)}


def trapezoid(cx, y0, y1, hw0, hw1):
    px = set()
    for y in range(y0, y1):
        t = (y - y0) / max(1, (y1 - 1 - y0))
        hw = hw0 + (hw1 - hw0) * t
        for x in range(int(round(cx - hw)), int(round(cx + hw)) + 1):
            px.add((x, y))
    return px


def outside(px, safe):
    x0, y0, x1, y1 = safe
    return {(x, y) for (x, y) in px if not (x0 <= x < x1 and y0 <= y < y1)}


class Part:
    def __init__(self, px, base, shade=None, hl=None, off=(2, 2), hoff=(1, 1)):
        self.px, self.base, self.shade, self.hl, self.off, self.hoff = px, base, shade, hl, off, hoff


def paint(parts, marks):
    """전부 D톤 : 오른쪽 아래 가장자리 그늘 + 왼쪽 위 하이라이트 + 바닥 그림자."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    p = im.load()
    for part in parts:
        for (x, y) in part.px:
            if 0 <= x < W and 0 <= y < H:
                c = part.base
                if part.shade and (x + part.off[0], y + part.off[1]) not in part.px:
                    c = part.shade
                p[x, y] = rgb(c)
        if part.hl:
            for (x, y) in part.px:
                if 0 <= x < W and 0 <= y < H and (x - part.hoff[0], y - part.hoff[1]) not in part.px:
                    p[x, y] = rgb(part.hl)
    for px, col in marks:
        for (x, y) in px:
            if 0 <= x < W and 0 <= y < H and p[x, y][3]:
                p[x, y] = rgb(col)
    sil = {(x, y) for part in parts for (x, y) in part.px if 0 <= x < W and 0 <= y < H}
    for (x, y) in sil:
        q = (x + 2, y + 2)
        if 0 <= q[0] < W and 0 <= q[1] < H and q not in sil and p[q][3] == 0:
            p[q] = (0, 0, 0, SHADOW_A)
    return im


# ---------- 기계 부품 ----------
def capsule(parts, marks, cx, top=2, bot=20, rw=11):
    """유리 캡슐 + 안에 흰 떡. 자판기 참고에서 가져온 「유리 안에 내용물이 보인다」."""
    parts.append(Part(rrect(cx - rw, top, cx + rw, bot, 9), GLASS, GLASS_S, GLASS_HI))
    parts.append(Part(ellipse(cx, (top + bot) / 2 + 1, rw - 4, (bot - top) / 2 - 4), MOCHI, MOCHI_S, None))
    marks.append((line(cx - rw + 3, top + 8, cx - rw + 3, top + 4) |
                  line(cx - rw + 4, top + 3, cx - rw + 6, top + 2), GLASS_HI))  # 유리 반짝임


def collar(parts, cx, y0, y1, hw):
    parts.append(Part(rect(cx - hw, y0, cx + hw, y1), BRASS, BRASS_S, BRASS_HI))


def pipe_v(parts, x0, x1, y0, y1, flanges=()):
    parts.append(Part(rect(x0, y0, x1, y1), BODY_HI, BODY_S, None))
    for fy in flanges:
        parts.append(Part(rect(x0 - 2, fy, x1 + 2, fy + 3), BRASS, BRASS_S, BRASS_HI))


def crank(parts, cx, cy):
    """손잡이 크랭크 — 「돌린다」를 읽히게 하는 가장 작은 부품."""
    parts.append(Part(ring(cx, cy, 6.5, 3.0), BRASS, BRASS_S, BRASS_HI))
    parts.append(Part(poly([(cx, cy), (cx - 5, cy - 7)], 3), BRASS, BRASS_S, None))
    parts.append(Part(ellipse(cx - 5, cy - 8, 3, 3), BRASS_HI, BRASS_S, None))


def gauge(parts, marks, cx, cy):
    parts.append(Part(ellipse(cx, cy, 6.5, 6.5), BRASS, BRASS_S, BRASS_HI))
    parts.append(Part(ellipse(cx, cy, 4.5, 4.5), WALL, WALL_S, None))
    marks.append((line(cx, cy, cx + 3, cy - 3), DEEP_S))


def rivets(marks, xs, y):
    for x in xs:
        marks.append((dot(x, y, 2, 2), BRASS_HI))


# ---------- 합성소 A : 글자 정중앙 ----------
def synthesis_A():
    parts, marks = [], []
    parts.append(Part(rrect(10, 54, 130, 62, 3), DEEP, DEEP_S, None))            # 받침
    parts.append(Part(rrect(14, 8, 126, 56, 6), BODY, BODY_S, BODY_HI))          # 몸통
    parts.append(Part(rect(22, 8, 118, 15), BRASS, BRASS_S, BRASS_HI))           # 윗 띠 (기계 뚜껑)
    rivets(marks, (26, 32, 106, 112), 10)
    # 명판이 가운데를 다 먹으니 투입구는 **명판 양옆 세로 유리 통**으로 세운다. 통마다 떡이 보인다
    for cx in (24, 116):
        parts.append(Part(rrect(cx - 7, 16, cx + 7, 45, 5), GLASS, GLASS_S, GLASS_HI))
        for cy in (23, 35):
            parts.append(Part(ellipse(cx, cy, 5, 4.4), MOCHI, MOCHI_S, None))
        marks.append((line(cx - 5, 26, cx - 5, 20), GLASS_HI))
        parts.append(Part(rect(cx - 8, 44, cx + 8, 48), BRASS, BRASS_S, BRASS_HI))  # 통 아래 이음쇠
    parts.append(Part(rect(20, 47, 120, 53), BODY_HI, BODY_S, None))              # 모으는 가로 파이프
    parts.append(Part(rect(33, 17, 107, 46), DEEP, DEEP_S, None))                 # 명판(글자 자리) — 평평
    marks.append((rect(33, 17, 107, 18) | rect(33, 45, 107, 46) |
                  rect(33, 17, 34, 46) | rect(106, 17, 107, 46), BRASS))          # 명판 놋쇠 테
    parts.append(Part(rrect(48, 44, 92, 58, 5), BODY, BODY_S, BODY_HI))           # 가운데 통
    parts.append(Part(ellipse(CX, 49, 12, 4.5), GLASS, GLASS_S, GLASS_HI))        # 통 안이 보이는 창
    parts.append(Part(ellipse(CX, 49.5, 5, 2.4), MOCHI, MOCHI_S, None))
    parts.append(Part(rect(60, 53, 80, 58), SLOT, None, None))                    # 배출구
    parts.append(Part(ellipse(CX, 58, 7, 3.6), MOCHI, MOCHI_S, None))             # 나온 떡 한 덩이
    crank(parts, 8, 49)
    gauge(parts, marks, 132, 49)
    return parts, marks, SAFE_A


# ---------- 합성소 B : 글자 아래 띠 ----------
def synthesis_B():
    parts, marks = [], []
    parts.append(Part(rrect(14, 6, 126, 46, 6), BODY, BODY_S, BODY_HI))
    for sgn in (-1, 1):                                                           # 깔때기 둘 + 비스듬한 파이프
        cx = CX + sgn * 40
        # 파이프는 놋쇠로 — 몸통과 같은 보라 계열로 두면 흐름이 안 보인다
        parts.append(Part(poly([(cx, 20), (CX + sgn * 22, 31)], 8), BRASS, BRASS_S, BRASS_HI))
        fun = trapezoid(cx, 3, 23, 13, 5)
        parts.append(Part(fun, BODY_HI, BODY_S, "#E7DFF7"))
        parts.append(Part(rect(cx - 14, 2, cx + 14, 6), BRASS, BRASS_S, BRASS_HI))
        # 떡은 깔때기 안에서만 보여야 한다 — 안 자르면 깔때기 밖으로 흘러넘친 흰 덩이가 된다
        parts.append(Part(ellipse(cx, 9, 8, 5) & fun, MOCHI, MOCHI_S, None))
    parts.append(Part(ring(CX, 26, 19, 16), BRASS, BRASS_S, BRASS_HI))            # 회전 창 테
    parts.append(Part(ellipse(CX, 26, 16, 16), GLASS, GLASS_S, None))
    parts.append(Part(spiral(CX, 26, 4, 14, 0.4, 4.2, 3), WALL, GLASS_S, None))   # 소용돌이
    parts.append(Part(ellipse(CX, 26, 6, 5), MOCHI, MOCHI_S, None))
    marks.append((line(59, 17, 63, 14) | line(57, 20, 60, 18), GLASS_HI))         # 유리 반짝임
    parts.append(Part(rect(61, 40, 79, 46), SLOT, None, None))                    # 배출구
    parts.append(Part(ellipse(CX, 43, 6.5, 2.6), MOCHI, MOCHI_S, None))
    crank(parts, 16, 36)      # 몸통 가장자리에 걸치게 둔다 — 떨어뜨리면 떠 보인다
    gauge(parts, marks, 116, 36)
    rivets(marks, (22, 28, 108, 114), 12)
    parts.append(Part(rect(6, 46, 134, 64), DEEP, DEEP_S, None))                  # 아래 명판 띠(글자 자리)
    marks.append((rect(6, 46, 134, 47), BRASS))
    return parts, marks, SAFE_B


# ---------- 카페 부품 ----------
def mochi_row(parts, marks, ys, xs, cols):
    for cx, col in zip(xs, cols):
        dome = {(x, y) for (x, y) in ellipse(cx, ys, 5.5, 4.6) if y <= ys + 3}
        parts.append(Part(dome, col, MOCHI_SH[col], None))
        marks.append((dot(cx - 3, ys - 2, 2, 1), WALL_HI))
        parts.append(Part(ellipse(cx, ys + 4, 6.5, 1.6), WALL_S, None, None))     # 접시


def plant(parts, marks, cx, ybase):
    parts.append(Part(trapezoid(cx, ybase - 8, ybase, 6, 4), POT, POT_S, "#E0A585"))
    for dx, dy, rx, ry in ((-4, -13, 4.5, 3.4), (4, -14, 4.5, 3.4), (0, -18, 4.0, 3.6)):
        parts.append(Part(ellipse(cx + dx, ybase + dy, rx, ry), LEAF, LEAF_S, "#9ABC74"))


def pendant(parts, cx, ytop, ybot):
    parts.append(Part(rect(cx, ytop, cx + 1, ybot), BRASS_S, None, None))
    parts.append(Part(trapezoid(cx, ybot, ybot + 5, 1.5, 4.5), BRASS, BRASS_S, BRASS_HI))
    parts.append(Part(ellipse(cx, ybot + 6, 2.5, 2), "#FFF3C9", None, None))


def door(parts, marks, x0, x1, y0, y1):
    parts.append(Part(rect(x0, y0, x1, y1), WOOD_2, WOOD_3, WOOD_1))
    parts.append(Part(rect(x0 + 3, y0 + 3, x1 - 3, y1), WIN, WIN_S, None))
    marks.append((line(x0 + 5, y1 - 4, x1 - 6, y0 + 8), WIN_HI))
    marks.append((dot(x1 - 6, (y0 + y1) // 2, 2, 3), BRASS))


# ---------- 가판대 A : 글자 정중앙(큰 유리창 면) ----------
def stall_A():
    parts, marks = [], []
    parts.append(Part(rrect(14, 11, 126, 60, 3), WALL, WALL_S, WALL_HI))          # 벽
    parts.append(Part(rect(8, 2, 132, 13), PINK, PINK_S, "#FFD6D9"))              # 평평한 어닝
    parts.append(Part(rect(8, 11, 132, 14), WALL, WALL_S, None))                  # 어닝 아래 크림 띠
    parts.append(Part(rect(20, 14, 114, 48), WOOD_2, WOOD_3, WOOD_1))             # 창틀(나무 포인트)
    glass = rect(22, 16, 112, 46)
    parts.append(Part(glass, WIN, WIN_S, None))                                   # 큰 유리창 — 여기가 글자 자리
    refl = (poly([(26, 45), (36, 20)], 3) | poly([(32, 45), (38, 32)], 2) |
            poly([(104, 45), (111, 26)], 3))
    marks.append((outside(refl & glass, SAFE_A), WIN_HI))                         # 대각선 반사줄
    parts.append(Part(rect(18, 48, 116, 58), WALL, WALL_S, WALL_HI))              # 쇼케이스 틀
    parts.append(Part(rect(20, 49, 114, 57), SHOW, SHOW_S, None))
    mochi_row(parts, marks, 51, (32, 52, 82, 102), (MOCHI, MOCHI_G, MOCHI_R, MOCHI))
    parts.append(Part(rect(12, 57, 128, 62), WOOD_1, WOOD_2, WOOD_HI))            # 카운터 상판
    for cx in (28, 106):
        pendant(parts, cx, 16, 21)
    plant(parts, marks, 7, 60)
    return parts, marks, SAFE_A


# ---------- 가판대 B : 글자 아래 띠, 쇼케이스를 크게 ----------
def stall_B():
    parts, marks = [], []
    parts.append(Part(rrect(14, 12, 126, 46, 3), WALL, WALL_S, WALL_HI))
    parts.append(Part(rect(10, 2, 130, 14), PINK, PINK_S, "#FFD6D9"))             # 간판 띠
    for cx in (58, 70, 82):                                                       # 글씨 대신 떡 아이콘
        parts.append(Part(ellipse(cx, 8, 4, 3.4), WALL, WALL_S, None))
    parts.append(Part(rect(24, 16, 102, 45), WOOD_2, WOOD_3, WOOD_1))             # 쇼케이스 틀
    parts.append(Part(rect(26, 18, 100, 43), SHOW, SHOW_S, None))
    mochi_row(parts, marks, 25, (36, 52, 68, 84), (MOCHI, MOCHI_G, MOCHI_R, MOCHI))
    parts.append(Part(rect(26, 30, 100, 32), WOOD_3, None, None))                 # 선반
    mochi_row(parts, marks, 37, (44, 60, 76, 92), (MOCHI_R, MOCHI, MOCHI_G, MOCHI))
    marks.append((poly([(28, 42), (36, 20)], 2) | poly([(94, 42), (99, 30)], 2), WALL_HI))
    door(parts, marks, 104, 124, 16, 45)
    plant(parts, marks, 8, 45)
    parts.append(Part(rect(6, 45, 134, 64), WOOD_2, WOOD_3, WOOD_1))              # 카운터 앞판(글자 자리)
    marks.append((rect(6, 45, 134, 46), WOOD_1))
    return parts, marks, SAFE_B


# ---------- 검산 ----------
def report(name, im, safe):
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    alphas = sorted({c[1][3] for c in im.getcolors(99999)})
    p = im.load()
    x0, y0, x1, y1 = safe
    scols = {p[x, y] for x in range(x0, x1) for y in range(y0, y1)}
    bx0, by0, bx1, by1 = BADGE
    badge = sum(1 for x in range(bx0, bx1) for y in range(by0, by1) if p[x, y][3])
    print(f"{name}: size={im.size} alphas={alphas} colors={len(cols)} "
          f"safebox_colors={len(scols)} badgebox_opaque={badge}")


# ---------- 비교 그림 ----------
def scene(b1, b2, label, text_at):
    """1판과 같은 배치 : 어두운 띠 + 건물 둘 + 담장 윗줄 + 풀밭 + 찹쌀이.
    text_at : None · 'center' · 'bottom'"""
    SW, BAND = 360, 88
    grass = Image.open(os.path.join(ART, "Farm", "ground_grass_0.png")).convert("RGBA")
    fence = Image.open(os.path.join(ART, "Farm", "fence_top.png")).convert("RGBA")
    body = Image.open(os.path.join(ART, "Mozzi", "body_hop0.png")).convert("RGBA")
    im = Image.new("RGBA", (SW, BAND + 88), (0x2B, 0x2B, 0x33, 255))
    for i in range(SW // 32 + 1):
        for j in range(2):
            im.alpha_composite(grass, (i * 32, BAND + 24 + j * 32))
    im.alpha_composite(b1, (20, 8))
    im.alpha_composite(b2, (200, 8))
    for i in range(SW // 32 + 1):
        im.alpha_composite(fence, (i * 32, BAND - 8))
    im.alpha_composite(body, (150, BAND + 8))
    d = ImageDraw.Draw(im)
    try:
        f = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 16)
        lf = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 12)
    except OSError:
        f = lf = None
    if text_at:
        cy = 8 + (32 if text_at == "center" else 54)
        for x0, t in ((20, "합성소"), (200, "가판대")):
            if f:
                d.text((x0 + CX, cy), t, font=f, fill=(255, 255, 255, 255), anchor="mm")
            else:
                d.rectangle([x0 + CX - 30, cy - 9, x0 + CX + 30, cy + 9], fill=(255, 255, 255, 255))
            d.ellipse([x0 + W - 15, 9, x0 + W - 2, 22], fill=(0xF2, 0x33, 0x33, 255))
    if lf:
        d.text((4, 2), label, font=lf, fill=(255, 255, 255, 255))
    return im


def main():
    made = {}
    for name, mk in (("synthesis_v2_A", synthesis_A), ("stall_v2_A", stall_A),
                     ("synthesis_v2_B", synthesis_B), ("stall_v2_B", stall_B)):
        parts, marks, safe = mk()
        im = paint(parts, marks)
        im.save(os.path.join(D, name + ".png"))
        report(name, im, safe)
        made[name] = im

    scenes = [
        scene(made["synthesis_v2_A"], made["stall_v2_A"], "A", None),
        scene(made["synthesis_v2_A"], made["stall_v2_A"], "A+글자", "center"),
        scene(made["synthesis_v2_B"], made["stall_v2_B"], "B", None),
        scene(made["synthesis_v2_B"], made["stall_v2_B"], "B+글자", "bottom"),
    ]
    sw, sh = scenes[0].size
    cmp_im = Image.new("RGBA", (sw, sh * 4), (0x1A, 0x1A, 0x20, 255))
    for i, s in enumerate(scenes):
        cmp_im.alpha_composite(s, (0, i * sh))
    cmp_im.resize((sw * 3, sh * 4 * 3), Image.NEAREST).save(os.path.join(D, "compare_building_v2.png"))

    S = 6
    close = Image.new("RGBA", ((W * S + 12) * 2, (H * S + 12) * 2), (0x2B, 0x2B, 0x33, 255))
    for i, k in enumerate(("synthesis_v2_A", "stall_v2_A", "synthesis_v2_B", "stall_v2_B")):
        close.alpha_composite(made[k].resize((W * S, H * S), Image.NEAREST),
                              (6 + (i % 2) * (W * S + 12), 6 + (i // 2) * (H * S + 12)))
    close.save(os.path.join(D, "closeup_building_v2.png"))
    print("compare/closeup v2 saved")


if __name__ == "__main__":
    main()

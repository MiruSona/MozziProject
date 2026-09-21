"""건물 3판 — 모양은 2판 B안(글자를 아래 18px 띠로) 확정. 합성소 **색만 두 벌**을 고른다.
 P = 찹쌀이 캐릭터표 색(분홍 몸통 + 모자색 받침 + 볼 다이아 포인트)
 K = 카페 색(크림 몸통 + 분홍 포인트)
손질 : 놋쇠 파이프를 굵기 일정한 관 + 이음쇠로 다시, 쇼케이스 반사줄을 떡 없는 구석으로,
 카페 간판 띠를 오른쪽 위 알림 점 자리에서 끊었다.
규격 : 140x64 · 투명 · 알파 0/56/255 · 외곽선 없음 · 빛 왼쪽 위 · 오른쪽 아래 2px 그림자 · 전부 D톤."""
import math
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 140, 64
CX = W // 2
SAFE = (35, 46, 105, 62)      # 글자는 아래 띠에 얹힌다
BADGE = (W - 14, 0, W, 14)
SHADOW_A = 56

# ---------- 색 ----------
# 찹쌀이 원작 색표 (Docs/Design/00-컨셉.md 6절)
CHAR_PINK, CHAR_EYE, CHAR_DIA, CHAR_HAT = "#F8BBC0", "#AE74DB", "#FA6270", "#393651"

BRASS, BRASS_S, BRASS_HI = "#E8B863", "#BE8F3E", "#F7DC9C"
GLASS, GLASS_S, GLASS_HI = "#CFE9F5", "#A9C9DA", "#FFFFFF"
MOCHI, MOCHI_S = "#FEFDFC", "#DCD9D6"
HAT_S = "#2A2840"             # 모자색 한 단계 아래 (받침 띠 그늘)

# 합성소 색 두 벌 : 몸통 / 몸통그늘 / 몸통하이라이트 / 뚜껑·테 포인트 / 포인트그늘 / 작은 강조
SETS = {
    "P": dict(body=CHAR_PINK, body_s="#D9949B", body_hi="#FFD8DB",
              trim=BRASS, trim_s=BRASS_S, trim_hi=BRASS_HI, spot=CHAR_DIA),
    "K": dict(body="#F4EEE4", body_s="#DCD3C4", body_hi="#FFFFFF",
              trim=CHAR_PINK, trim_s="#D9949B", trim_hi="#FFD8DB", spot=CHAR_DIA),
}

# 카페
WALL, WALL_S, WALL_HI = "#F4EEE4", "#DCD3C4", "#FFFFFF"
PINK, PINK_S = "#F8BBC0", "#DE9AA1"
WIN, WIN_S, WIN_HI = "#5A6B78", "#46545F", "#93A9B6"
SHOW, SHOW_S = "#C6DCE6", "#A5C0CD"
WOOD_1, WOOD_2, WOOD_3 = "#B78B60", "#966D48", "#755438"
LEAF, LEAF_S = "#7FA05A", "#688444"
POT, POT_S = "#C98A6A", "#A76C50"
MOCHI_G, MOCHI_R = "#7FA05A", "#8E4A4A"
MOCHI_SH = {MOCHI: MOCHI_S, MOCHI_G: "#688444", MOCHI_R: "#733838"}


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 도우미 (2판에서 옮겨 적었다) ----------
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def rrect(x0, y0, x1, y1, r=3):
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


class Part:
    def __init__(self, px, base, shade=None, hl=None, off=(2, 2), hoff=(1, 1)):
        self.px, self.base, self.shade, self.hl, self.off, self.hoff = px, base, shade, hl, off, hoff


def paint(parts, marks):
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


# ---------- 기계 ----------
def elbow_pipe(parts, sgn, cx, ydrum=30):
    """깔때기 아래에서 수직으로 내려와 한 번 꺾여 회전 창 옆구리로 들어가는 관.
    굵기를 6px 로 고정하고 off/hoff 를 1 로 둬 **윗면 1줄 밝게 · 아랫면 1줄 어둡게** — 이래야 관으로 보인다.
    2판처럼 대각선 띠로 그리면 날개·리본처럼 보였다."""
    xin, xout = (48, 92)
    vert = rect(cx - 3, 20, cx + 3, ydrum + 3)
    horz = (rect(cx - 3, ydrum - 3, xin, ydrum + 3) if sgn < 0
            else rect(xout, ydrum - 3, cx + 3, ydrum + 3))
    parts.append(Part(vert | horz, BRASS, BRASS_S, BRASS_HI, off=(1, 1), hoff=(1, 1)))
    # 이음쇠는 **조금 어두운 놋쇠**다. 남색으로 두면 관에 낀 검은 벽돌처럼 보인다
    parts.append(Part(rect(cx - 5, 24, cx + 5, 27), BRASS_S, None, BRASS))           # 세로관 이음쇠
    fx = xin if sgn < 0 else xout - 3
    parts.append(Part(rect(fx, ydrum - 5, fx + 3, ydrum + 5), BRASS_S, None, BRASS))  # 끝 이음쇠


def synthesis(setname):
    s = SETS[setname]
    parts, marks = [], []
    parts.append(Part(rrect(14, 6, 126, 46, 6), s["body"], s["body_s"], s["body_hi"]))
    for sgn in (-1, 1):
        cx = CX + sgn * 40
        elbow_pipe(parts, sgn, cx)
        fun = trapezoid(cx, 3, 22, 13, 5)
        # 깔때기 안쪽은 몸통보다 **어둡게** — 밝게 두면 흰 떡이 안 보이고 등불처럼 보인다
        parts.append(Part(fun, s["body_s"], s["body_s"], s["body"]))
        parts.append(Part(rect(cx - 14, 2, cx + 14, 6), s["trim"], s["trim_s"], s["trim_hi"]))
        parts.append(Part(ellipse(cx, 9, 8, 5) & fun, MOCHI, MOCHI_S, None))          # 깔때기 안 떡
    parts.append(Part(ring(CX, 26, 19, 16), s["trim"], s["trim_s"], s["trim_hi"]))    # 회전 창 테
    parts.append(Part(ellipse(CX, 26, 16, 16), GLASS, GLASS_S, None))
    parts.append(Part(spiral(CX, 26, 4, 14, 0.4, 4.2, 3), CHAR_EYE, "#8C5CB4", None))  # 소용돌이 = 찹쌀이 눈색
    parts.append(Part(ellipse(CX, 26, 6, 5), MOCHI, MOCHI_S, None))
    marks.append((line(59, 17, 63, 14) | line(57, 20, 60, 18), GLASS_HI))             # 유리 반짝임
    parts.append(Part(rect(61, 40, 79, 46), CHAR_HAT, None, None))                    # 배출구
    parts.append(Part(ellipse(CX, 43, 6.5, 2.6), MOCHI, MOCHI_S, None))
    # 크랭크 (돌린다) · 계기 — 둘 다 놋쇠로 고정한다. 크림 몸통에 분홍으로 두면 눈에 안 띈다
    parts.append(Part(ring(16, 36, 6.5, 3.0), BRASS, BRASS_S, BRASS_HI))
    parts.append(Part(poly([(16, 36), (11, 29)], 3), BRASS, BRASS_S, None))
    parts.append(Part(ellipse(11, 28, 3, 3), BRASS_HI, BRASS_S, None))
    parts.append(Part(ellipse(116, 36, 6.5, 6.5), BRASS, BRASS_S, BRASS_HI))
    parts.append(Part(ellipse(116, 36, 4.5, 4.5), WALL, WALL_S, None))
    marks.append((line(116, 36, 119, 33), CHAR_HAT))
    marks.append((dot(30, 12, 2, 2) | dot(108, 12, 2, 2), s["spot"]))                 # 작은 포인트 단추
    parts.append(Part(rect(6, 46, 134, 64), CHAR_HAT, HAT_S, None))                   # 글자 띠 (모자색)
    marks.append((rect(6, 46, 134, 47), s["trim"]))
    return parts, marks


# ---------- 카페 ----------
def mochi_row(parts, marks, ys, xs, cols):
    for cx, col in zip(xs, cols):
        dome = {(x, y) for (x, y) in ellipse(cx, ys, 5.5, 4.6) if y <= ys + 3}
        parts.append(Part(dome, col, MOCHI_SH[col], None))
        marks.append((dot(cx - 3, ys - 2, 2, 1), WALL_HI))
        parts.append(Part(ellipse(cx, ys + 4, 6.5, 1.6), WALL_S, None, None))


def plant(parts, cx, ybase):
    parts.append(Part(trapezoid(cx, ybase - 8, ybase, 6, 4), POT, POT_S, "#E0A585"))
    for dx, dy, rx, ry in ((-4, -13, 4.5, 3.4), (4, -14, 4.5, 3.4), (0, -18, 4.0, 3.6)):
        parts.append(Part(ellipse(cx + dx, ybase + dy, rx, ry), LEAF, LEAF_S, "#9ABC74"))


def stall():
    parts, marks = [], []
    parts.append(Part(rrect(14, 12, 126, 46, 3), WALL, WALL_S, WALL_HI))
    # 간판 띠는 x124 에서 끊는다 — 오른쪽 위 알림 점(빨강)이 분홍 위에 얹히면 안 보인다
    parts.append(Part(rect(16, 2, 124, 14), PINK, PINK_S, "#FFD6D9"))
    for cx in (58, 70, 82):
        parts.append(Part(ellipse(cx, 8, 4, 3.4), WALL, WALL_S, None))                # 글씨 대신 떡 아이콘
    parts.append(Part(rect(24, 16, 102, 45), WOOD_2, WOOD_3, WOOD_1))                 # 쇼케이스 틀
    parts.append(Part(rect(26, 18, 100, 43), SHOW, SHOW_S, None))
    mochi_row(parts, marks, 25, (36, 52, 68, 84), (MOCHI, MOCHI_G, MOCHI_R, MOCHI))
    parts.append(Part(rect(26, 30, 100, 32), WOOD_3, None, None))                     # 선반
    mochi_row(parts, marks, 37, (44, 60, 76, 92), (MOCHI_R, MOCHI, MOCHI_G, MOCHI))
    # 반사줄은 떡이 없는 구석에만 짧게 — 길게 그으면 떡을 가로지른다
    marks.append((poly([(29, 41), (32, 34)], 2) | poly([(97, 25), (99, 20)], 2), WALL_HI))
    parts.append(Part(rect(104, 16, 124, 45), WOOD_2, WOOD_3, WOOD_1))                # 문틀
    parts.append(Part(rect(107, 19, 121, 45), WIN, WIN_S, None))
    marks.append((line(110, 41, 118, 26), WIN_HI))
    marks.append((dot(116, 31, 2, 3), BRASS))
    plant(parts, 8, 45)
    parts.append(Part(rect(6, 45, 134, 64), WOOD_2, WOOD_3, WOOD_1))                  # 카운터 앞판(글자 자리)
    marks.append((rect(6, 45, 134, 46), WOOD_1))
    return parts, marks


# ---------- 검산 ----------
def report(name, im):
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    alphas = sorted({c[1][3] for c in im.getcolors(99999)})
    p = im.load()
    x0, y0, x1, y1 = SAFE
    scols = {p[x, y] for x in range(x0, x1) for y in range(y0, y1)}
    bx0, by0, bx1, by1 = BADGE
    solid = sum(1 for x in range(bx0, bx1) for y in range(by0, by1) if p[x, y][3] == 255)
    print(f"{name}: size={im.size} alphas={alphas} colors={len(cols)} "
          f"safebox_colors={len(scols)} badgebox_solid={solid}")


# ---------- 비교 그림 ----------
def scene(b1, b2, label, with_text):
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
    if with_text:
        for x0, t in ((20, "합성소"), (200, "가판대")):
            if f:
                d.text((x0 + CX, 8 + 54), t, font=f, fill=(255, 255, 255, 255), anchor="mm")
            else:
                d.rectangle([x0 + CX - 30, 53, x0 + CX + 30, 71], fill=(255, 255, 255, 255))
            d.ellipse([x0 + W - 15, 9, x0 + W - 2, 22], fill=(0xF2, 0x33, 0x33, 255))
    if lf:
        d.text((4, 2), label, font=lf, fill=(255, 255, 255, 255))
    return im


def main():
    made = {}
    for name, (parts, marks) in (("synthesis_v3_P", synthesis("P")),
                                 ("synthesis_v3_K", synthesis("K")),
                                 ("stall_v3", stall())):
        im = paint(parts, marks)
        im.save(os.path.join(D, name + ".png"))
        report(name, im)
        made[name] = im

    scenes = []
    for key, lab in (("synthesis_v3_P", "P 캐릭터색"), ("synthesis_v3_K", "K 카페색")):
        scenes.append(scene(made[key], made["stall_v3"], lab, False))
        scenes.append(scene(made[key], made["stall_v3"], lab.split()[0] + "+글자", True))
    sw, sh = scenes[0].size
    cmp_im = Image.new("RGBA", (sw, sh * 4), (0x1A, 0x1A, 0x20, 255))
    for i, s in enumerate(scenes):
        cmp_im.alpha_composite(s, (0, i * sh))
    cmp_im.resize((sw * 3, sh * 4 * 3), Image.NEAREST).save(os.path.join(D, "compare_building_v3.png"))

    S = 6
    close = Image.new("RGBA", ((W * S + 12) * 3, H * S + 12), (0x2B, 0x2B, 0x33, 255))
    for i, k in enumerate(("synthesis_v3_P", "synthesis_v3_K", "stall_v3")):
        close.alpha_composite(made[k].resize((W * S, H * S), Image.NEAREST),
                              (6 + i * (W * S + 12), 6))
    close.save(os.path.join(D, "closeup_building_v3.png"))
    print("compare/closeup v3 saved")


if __name__ == "__main__":
    main()

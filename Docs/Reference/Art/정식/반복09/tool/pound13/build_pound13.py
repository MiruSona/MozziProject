# 떡메 치기 시안 pound13 — 세 겹(절구 / 반죽 / 떡메) 나누기. 겹 순서: 절구 < 떡메 < 반죽
# 실행: python build_pound13.py   (이 폴더에서. ../pound12/raw/mallet_A.png 를 원본으로 쓴다)
import os
import random
import sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
sys.path.insert(0, TOOL)
import build_tool as bt  # noqa: E402

ROOT = os.path.abspath(os.path.join(TOOL, "..", "..", "..", "..", "..", ".."))
OLD = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen", "kitchen_pound_{}.png")
BG = os.path.join(TOOL, "..", "bg", "bg_2.png")
SRC_A = os.path.join(TOOL, "pound12", "raw", "mallet_A.png")
SCREEN_X, SCREEN_Y, BG_TOP = 270, 815, 250
W, H = 440, 560          # 캔버스. 바닥 가운데(220,560) = 절구 바닥 가운데 = 화면 (270,815)
K = 2                    # 픽셀 배율 (절구 그림의 2칸 도트)
HW, HH = W // K, H // K  # 반죽을 그리는 반 해상도
ADD = 25                 # A 자루에 더하는 길이(원본 칸). 40 → 65
OUTL = (120, 96, 72, 255)
C_L, C_M, C_D = (255, 251, 240, 255), (255, 242, 215, 255), (232, 210, 172, 255)
TIMES = [110, 80, 90, 140, 45, 160, 90, 140]
T_TIMES = [130, 130, 130, 160]

mortar = bt.tight(bt.MORTAR)
OX, OY = (W - mortar.width) // 2, H - mortar.height
CX = W // 2


# ---------- 떡메 ----------
def long_mallet():
    im = Image.open(SRC_A).convert("RGBA")
    im = im.crop(im.getbbox())
    cut = 40                       # 자루 가운데 세로줄을 ADD 번 되풀이해 늘인다 (나뭇결이 가로라 안 끊긴다)
    out = Image.new("RGBA", (im.width + ADD, im.height))
    out.alpha_composite(im.crop((0, 0, cut, im.height)), (0, 0))
    col = im.crop((cut, 0, cut + 1, im.height))
    for i in range(ADD):
        out.alpha_composite(col, (cut + i, 0))
    out.alpha_composite(im.crop((cut, 0, im.width, im.height)), (cut + ADD, 0))
    return out


MAL = long_mallet()
HCX, HCY = 11, MAL.height / 2      # 머리 가운데 (원본 칸)
HALF = MAL.height * K / 2          # 머리 가운데 → 머리 아랫면 (캔버스 px)


def place(ang, hx, bottom):
    """머리 아랫면 가운데를 (hx,bottom) 에 두고 ang° 돌린다(양수 = 자루가 오른쪽 위). 원래 크기에서 돌린 뒤 K 배."""
    R = max(MAL.size) + 4
    pad = Image.new("RGBA", (2 * R, 2 * R))
    pad.alpha_composite(MAL, (round(R - HCX), round(R - HCY)))
    rot = pad.rotate(ang, resample=Image.NEAREST).resize((2 * R * K, 2 * R * K), Image.NEAREST)
    layer = Image.new("RGBA", (W, H))
    layer.alpha_composite(rot, (round(hx - R * K), round(bottom - HALF - R * K)))
    return layer


def streak(layer, hx, top):
    d = ImageDraw.Draw(layer)
    for dx, ln in ((-16, 50), (0, 70), (16, 50)):
        d.line((hx + dx, top - 8 - ln, hx + dx, top - 8), fill=(255, 255, 255, 140), width=4)
    return layer


# ---------- 반죽 (반 해상도 타원 묶음 → 2배) ----------
def render(ells, tex, amt):
    m = Image.new("L", (HW, HH))
    dm = ImageDraw.Draw(m)
    for cx, cy, rx, ry in ells:
        dm.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=255)
    im = Image.new("RGBA", (HW, HH))
    d = ImageDraw.Draw(im)
    for cx, cy, rx, ry in ells:
        d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=C_D)
    for cx, cy, rx, ry in ells:
        d.ellipse((cx - rx + 1, cy - ry, cx + rx - 1, cy + ry - 2.5), fill=C_M)
    big = max(ells, key=lambda e: e[2] * e[3])
    for cx, cy, rx, ry in ells:
        if rx * ry >= 0.5 * big[2] * big[3]:
            d.ellipse((cx - rx * 0.6, cy - ry * 0.8, cx - rx * 0.05, cy - ry * 0.35), fill=C_L)
    px, mp = im.load(), m.load()
    rnd = random.Random(7)
    if tex == "grain" and amt > 0:
        for _ in range(int(220 * amt)):
            x, y = rnd.randrange(HW), rnd.randrange(HH)
            if mp[x, y] and all(mp[x + a, y + b] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = C_D if rnd.random() < 0.6 else C_L
    if tex == "lump" and amt > 0:
        for cx, cy, rx, ry in ells:
            if rx > 8 and ry > 5:
                d.arc((cx - rx * 0.7, cy - ry * 0.2, cx + rx * 0.5, cy + ry * 0.7), 200, 330, fill=C_D)
    for y in range(1, HH - 1):          # 외곽선 1칸
        for x in range(1, HW - 1):
            if mp[x, y] and not all(mp[x + a, y + b] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = OUTL
    for y in range(HH):
        for x in range(HW):
            if not mp[x, y]:
                px[x, y] = (0, 0, 0, 0)
    return im.resize((W, H), Image.NEAREST)


S1_REST = [(88, 206, 14, 7), (108, 203, 15, 8), (128, 206, 14, 7), (98, 212, 16, 6), (122, 212, 16, 6)]
S2_REST = [(96, 205, 18, 10), (110, 200, 16, 9), (122, 204, 18, 11), (96, 205, 18, 10), (122, 204, 18, 11)]
S3_REST = [(110, 202, 32, 14)] * 5
DOUGH = {   # 장 0..4 는 쉼, 5 친 장, 6 튕김, 7 가라앉음
    1: {5: [(110, 214, 46, 9), (110, 206, 20, 10), (76, 198, 11, 15), (144, 198, 11, 15), (68, 184, 4, 7), (152, 184, 4, 7),
            (88, 188, 4, 6), (132, 188, 4, 6), (68, 222, 6, 12), (152, 224, 5, 10), (118, 222, 5, 9),
            (60, 166, 3, 3), (160, 162, 3, 3), (100, 170, 2, 2), (124, 168, 2, 2)],
        6: [(110, 212, 44, 9), (110, 207, 19, 9), (78, 202, 12, 12), (142, 202, 12, 12), (68, 228, 6, 15), (152, 230, 5, 13),
            (118, 226, 5, 11), (58, 182, 3, 3), (162, 178, 3, 3)],
        7: S1_REST + [(110, 210, 46, 8), (66, 222, 5, 10), (154, 222, 4, 9)]},
    2: {5: [(110, 210, 40, 8), (110, 208, 18, 6), (74, 197, 9, 10), (146, 197, 9, 10), (68, 214, 4, 8), (152, 214, 4, 8),
            (78, 180, 2, 2), (142, 178, 2, 2)],
        6: [(110, 208, 36, 9), (110, 205, 18, 7), (74, 201, 9, 8), (146, 201, 9, 8), (68, 217, 4, 9), (152, 217, 4, 9)],
        7: S2_REST + [(110, 207, 30, 8)]},
    3: {5: [(110, 206.5, 38, 9.5)], 6: [(110, 200.5, 30, 15.5)], 7: [(110, 202.5, 33, 13.5)]},
}
REST = {1: S1_REST, 2: S2_REST, 3: S3_REST}
TEX = {1: "grain", 2: "lump", 3: None}
HIT_BOTTOM = {1: 440, 2: 418, 3: 394}         # 친 장 머리 아랫면 (캔버스 y)
REST_BOTTOM = {1: 398, 2: 386, 3: 376}        # 쉼 장 머리 아랫면 (반죽 윗면 + 묻힘 8/4/0)
POSE = [(0, 0), (70, 18), (200, 42), (235, 48), (110, 22), None, (28, 6), (6, 2)]   # (들어 올린 높이, 각도)


def dough(stage, i):
    ells = DOUGH[stage].get(i, REST[stage])
    return render(ells, TEX[stage], 1.0)


def mallet(stage, i):
    if POSE[i] is None:
        return place(-3, CX, HIT_BOTTOM[stage])
    up, ang = POSE[i]
    lay = place(ang, CX + up * 0.12, REST_BOTTOM[stage] - up)
    return streak(lay, CX, REST_BOTTOM[stage] - up - 2 * HALF) if i == 4 else lay


def lerp(a, b, t):
    return [tuple(p + (q - p) * t for p, q in zip(e, f)) for e, f in zip(a, b)]


def trans(a, b, i):
    t = (0.2, 0.45, 0.7, 0.92)[i]
    ells = lerp(REST[a], REST[b], t)
    if i == 2:   # 뭉치며 한 번 출렁
        ells = [(cx, cy - 1, rx * 0.96, ry * 1.08) for cx, cy, rx, ry in ells]
    tex = ("grain", 1 - t) if (a == 1 and t < 0.5) else ("lump", 1 - t if a == 2 else t)
    lay = render(ells, *tex)
    mal = place(0, CX, REST_BOTTOM[a] + (REST_BOTTOM[b] - REST_BOTTOM[a]) * t)
    return lay, mal


# ---------- 합성·출력 ----------
def comp(d, m):
    f = Image.new("RGBA", (W, H))
    f.alpha_composite(mortar, (OX, OY))
    f.alpha_composite(m)
    f.alpha_composite(d)
    return f


def on_bg(f):
    bg = Image.open(BG).convert("RGBA")
    x0, y0 = SCREEN_X - W // 2, SCREEN_Y - H - BG_TOP
    bg.alpha_composite(f, (x0, y0))
    return bg.crop((x0, y0 - 20, x0 + W, y0 + H + 20))


def old_frame(i):
    im = Image.open(OLD.format(i)).convert("RGBA")
    im = im.resize((round(im.width * 1.55), round(im.height * 1.55)), Image.NEAREST)
    f = Image.new("RGBA", (W, H))
    f.alpha_composite(im, (W // 2 - im.width // 2 - 20, H - im.getbbox()[3]))
    return f


def save_gif(name, frames, times):
    rgb = [fr.convert("RGB") for fr in frames]
    rgb[0].save(os.path.join(HERE, name), save_all=True, append_images=rgb[1:], duration=times, loop=0, disposal=1)


def main():
    os.makedirs(os.path.join(HERE, "layers"), exist_ok=True)
    L = lambda n: os.path.join(HERE, "layers", n)
    mo = Image.new("RGBA", (W, H)); mo.alpha_composite(mortar, (OX, OY)); mo.save(L("mortar.png"))
    MAL.save(os.path.join(HERE, "raw", "mallet_A_long.png"))
    st = {}
    for s in (1, 2, 3):
        st[s] = []
        for i in range(8):
            d, m = dough(s, i), mallet(s, i)
            d.save(L(f"dough_s{s}_{i}.png")); m.save(L(f"mallet_s{s}_{i}.png"))
            st[s].append(comp(d, m))
    tr = {}
    for a, b in ((1, 2), (2, 3)):
        tr[a] = []
        for i in range(4):
            d, m = trans(a, b, i)
            d.save(L(f"dough_t{a}{b}_{i}.png")); m.save(L(f"mallet_t{a}{b}_{i}.png"))
            tr[a].append(comp(d, m))
    bgs = lambda fs: [on_bg(f) for f in fs]
    for s in (1, 2, 3):
        save_gif(f"pound13_s{s}.gif", bgs(st[s] * 2), TIMES * 2)
    seq = st[1] + st[1] + tr[1] + st[2] + st[2] + tr[2] + st[3] + st[3]
    tms = TIMES * 2 + T_TIMES + TIMES * 2 + T_TIMES + TIMES * 2
    tms[-1] = 600
    save_gif("pound13_이어서.gif", bgs(seq), tms)
    olds = [old_frame(i) for i in range(7)]
    side = []
    for k in range(8):
        a, b = on_bg(olds[k % 7]), on_bg(st[1][k])
        c = Image.new("RGB", (2 * W + 10, a.height), (20, 20, 20)); c.paste(a.convert("RGB"), (0, 0)); c.paste(b.convert("RGB"), (W + 10, 0))
        side.append(c)
    save_gif("pound13_나란히.gif", side, TIMES)
    # 견줌판
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 20)
    ph = H + 40
    board = Image.new("RGB", (5 * W, 2 * (ph + 34)), (36, 32, 30))
    chk = Image.new("RGBA", (W, H), (90, 90, 96, 255))
    def solo(lay):
        f = chk.copy(); f.alpha_composite(lay); return f
    oldm = Image.open(OLD.format(3)).convert("RGBA").crop((40, 25, 200, 108))
    oldm = oldm.resize((round(oldm.width * 1.55), round(oldm.height * 1.55)), Image.NEAREST)
    a0 = Image.open(SRC_A).convert("RGBA"); a0 = a0.crop(a0.getbbox()); a0 = a0.resize((a0.width * K, a0.height * K), Image.NEAREST)
    a1 = MAL.resize((MAL.width * K, MAL.height * K), Image.NEAREST)
    cmp_ = chk.copy()
    for yy, im_, t in ((10, oldm, "옛 것(1.55배)"), (180, a1, "새 A(늘임)"), (330, a0, "원래 A")):
        cmp_.alpha_composite(im_, (10, yy + 26)); ImageDraw.Draw(cmp_).text((12, yy), t, fill=(255, 255, 255), font=font)
    top = [(solo(mo), "절구 겹"), (solo(dough(1, 5)), "반죽 겹 1단계 친 장"), (solo(mallet(1, 5)), "떡메 겹 1단계 친 장"),
           (on_bg(st[1][3]), "합성 · 든 장"), (cmp_, "떡메 길이 견줌")]
    bot = [(on_bg(st[1][5]), "1단계 친 장"), (on_bg(st[2][5]), "2단계 친 장"), (on_bg(st[3][5]), "3단계 친 장"),
           (on_bg(tr[1][1]), "넘어감 t12 (1번 장)"), (on_bg(tr[2][1]), "넘어감 t23 (1번 장)")]
    for r, row in enumerate((top, bot)):
        for c, (im_, t) in enumerate(row):
            x, y = c * W, r * (ph + 34) + 30
            board.paste(im_.convert("RGB"), (x, y))
            ImageDraw.Draw(board).text((x + 8, y - 26), t, fill=(240, 240, 240), font=font)
    board.save(os.path.join(HERE, "pound13_견줌판.png"))


if __name__ == "__main__":
    main()

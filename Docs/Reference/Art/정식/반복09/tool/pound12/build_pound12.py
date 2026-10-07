# 떡메 치기 시안 pound12 — 세 단계(넘침 크게 → 조금 → 없음), 높이 들었다 내리치기
# 실행: python build_pound12.py   (이 폴더에서. raw/mallet_A.png · mallet_B.png 가 있어야 한다)
import os
import sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
sys.path.insert(0, TOOL)
import build_tool as bt  # noqa: E402

ROOT = os.path.abspath(os.path.join(TOOL, "..", "..", "..", "..", "..", ".."))
OLD = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen", "kitchen_pound_{}.png")
BG = os.path.join(TOOL, "..", "bg", "bg_2.png")
SCREEN_X, SCREEN_Y, BG_TOP = 270, 815, 250
W, H = 440, 560                     # 캔버스. 바닥 가운데(220,560) = 화면 (270,815)
PICK = "B"                          # 움직임에 쓴 떡메
K = 2                               # 떡메·반죽 픽셀 배율 (절구 그림의 2칸 도트에 맞춤)
OUTL, CREAM, CREAM_D, CREAM_L = (110, 84, 58, 255), (251, 235, 195, 255), (232, 208, 160, 255), (255, 248, 228, 255)

mortar = bt.tight(bt.MORTAR)
_, (IX0, IY0, IX1, IY1) = bt.interior(mortar)
OX, OY = (W - mortar.width) // 2, H - mortar.height
CX, RIM, FLOOR = OX + mortar.width // 2, OY + IY0, OY + IY1 + 6
FRONT = OY + (IY0 + IY1) // 2
INSIDE = bt.interior(mortar)[0]


# ---------- 떡메 ----------
def draw_c():
    """후보 C: 옛 떡메 꼴(선 원통 머리 + 굵고 짧은 자루)을 PIL 로 (원래 크기 64x48)."""
    im = Image.new("RGBA", (64, 48)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((26, 12, 60, 21), radius=4, fill=(58, 36, 20, 255))
    d.rounded_rectangle((27, 13, 59, 20), radius=3, fill=(188, 126, 70, 255))
    d.line((28, 14, 58, 14), fill=(228, 172, 108, 255))
    d.rounded_rectangle((4, 4, 30, 44), radius=5, fill=(58, 36, 20, 255))
    d.rounded_rectangle((5, 5, 29, 43), radius=4, fill=(188, 126, 70, 255))
    d.rectangle((7, 6, 11, 42), fill=(228, 172, 108, 255)); d.rectangle((24, 6, 28, 42), fill=(128, 78, 40, 255))
    for y in (10, 38):
        d.line((5, y, 29, y), fill=(58, 36, 20, 255))
    return im


def mallet(tag):
    im = draw_c() if tag == "C" else Image.open(os.path.join(HERE, "raw", f"mallet_{tag}.png")).convert("RGBA")
    im = im.crop(im.getbbox())
    a = im.getchannel("A")
    cols = [sum(1 for y in range(im.height) if a.getpixel((x, y)) > 128) for x in range(im.width)]
    head = [x for x, c in enumerate(cols) if c > 0.6 * max(cols)]
    ys = [y for y in range(im.height) if any(a.getpixel((x, y)) > 128 for x in head)]
    return im, ((head[0] + head[-1] + 1) / 2, (ys[0] + ys[-1] + 1) / 2), ys[-1] + 1 - ys[0]


def place(m, ang, hx, hy):
    """머리 가운데를 (hx,hy) 에 두고 ang° 돌린다(양수 = 자루가 오른쪽 위로). 원래 크기에서 돌린 뒤 K 배 — 계단이 도트 단위로 고르다."""
    im, (cx, cy), _ = m
    R = int(max(im.size)) + 4
    pad = Image.new("RGBA", (2 * R, 2 * R)); pad.alpha_composite(im, (round(R - cx), round(R - cy)))
    rot = pad.rotate(ang, resample=Image.NEAREST).resize((4 * R, 4 * R) if K == 2 else (2 * R * K, 2 * R * K), Image.NEAREST)
    layer = Image.new("RGBA", (W, H)); layer.alpha_composite(rot, (round(hx - R * K), round(hy - R * K)))
    return layer


# ---------- 반죽 ----------
def blob(shapes, grains=False):
    """반 크기로 도형을 칠하고 외곽선 · 아래 그늘 · 위 빛을 넣은 뒤 K 배. shapes = [(종류, 좌표)…] 캔버스 좌표."""
    w, h = W // K, H // K
    m = Image.new("L", (w, h)); d = ImageDraw.Draw(m)
    for kind, box in shapes:
        b = [round(v / K) for v in box]
        (d.ellipse if kind == "e" else d.polygon)(b if kind == "e" else list(zip(b[::2], b[1::2])), fill=255)
    out = Image.new("RGBA", (w, h)); p, mp = out.load(), m.load()
    edge = m.filter(ImageFilter.MaxFilter(3)).load()
    for y in range(h):
        for x in range(w):
            if mp[x, y]:
                below = y + 3 < h and not mp[x, y + 3]
                above = y - 2 >= 0 and not mp[x, y - 2]
                c = CREAM_D if below else CREAM_L if above else CREAM
                if grains and (x * 7 + y * 13) % 11 == 0:
                    c = CREAM_D
                p[x, y] = c
            elif edge[x, y]:
                p[x, y] = OUTL
    return out.resize((W, H), Image.NEAREST)


def dough_img(stage, react):
    """(안쪽 반죽 층, 앞에 덮는 넘침 층, 반죽 윗면 y)"""
    base = bt.tight(f"dough_s{stage}.png")
    sx, sy = 1.0, 1.0
    if stage == 3 and react == 1:
        sx, sy = 1.2, 0.68                           # 눌려 납작
    elif stage == 3 and react == 2:
        sx, sy = 0.94, 1.1                           # 되돌아 솟음
    elif stage == 2 and react == 1:
        sx, sy = 1.15, 0.8
    d = base.resize((round(base.width * sx), round(base.height * sy)), Image.NEAREST)
    inner = Image.new("RGBA", (W, H)); inner.alpha_composite(d, (CX - d.width // 2, FLOOR - d.height))
    top = FLOOR - d.height + 6
    over = None
    L, R = OX + IX0, OX + IX1
    if stage == 1 and react == 1:                    # 크게 넘침: 왕관처럼 솟고 절구 밖으로 흘러내림
        over = blob([("e", (L - 26, RIM - 14, R + 26, RIM + 16)),            # 테두리를 덮는 띠
                     ("e", (L - 10, RIM - 40, L + 46, RIM + 4)), ("e", (R - 46, RIM - 42, R + 10, RIM + 4)),
                     ("e", (CX - 46, RIM - 30, CX - 6, RIM)), ("e", (CX + 6, RIM - 30, CX + 46, RIM)),
                     ("e", (L + 8, RIM - 58, L + 34, RIM - 30)), ("e", (R - 34, RIM - 62, R - 8, RIM - 32)),
                     ("e", (L - 24, RIM, L - 2, RIM + 44)), ("e", (L + 28, RIM + 4, L + 46, RIM + 30)),
                     ("e", (R - 4, RIM, R + 18, RIM + 50)), ("e", (CX + 14, RIM + 4, CX + 32, RIM + 28)),
                     ("e", (L - 44, RIM - 70, L - 28, RIM - 54)), ("e", (R + 24, RIM - 80, R + 40, RIM - 64)),
                     ("e", (CX - 76, RIM - 100, CX - 64, RIM - 88)), ("e", (CX + 66, RIM - 104, CX + 78, RIM - 92))], grains=True)
        top = RIM - 22
    elif stage == 1 and react == 2:                  # 넘친 게 흘러내리는 중
        over = blob([("e", (L - 18, RIM - 12, R + 18, RIM + 12)),
                     ("e", (L - 20, RIM, L - 2, RIM + 52)), ("e", (R - 4, RIM, R + 14, RIM + 58)),
                     ("e", (CX + 16, RIM + 4, CX + 32, RIM + 34)),
                     ("e", (L - 52, RIM - 30, L - 40, RIM - 18)), ("e", (R + 36, RIM - 26, R + 48, RIM - 14))], grains=True)
        top = RIM - 8
    elif stage == 1 and react == 3:                  # 쉼: 가장자리에 조금 남음
        over = blob([("e", (L - 12, RIM - 6, L + 14, RIM + 10)), ("e", (R - 10, RIM - 6, R + 12, RIM + 12)),
                     ("e", (R - 6, RIM, R + 8, RIM + 22))], grains=True)
    elif stage == 2 and react == 1:                  # 조금 넘침: 가장자리로만 솟음
        over = blob([("e", (L - 6, RIM - 16, L + 40, RIM + 6)), ("e", (R - 40, RIM - 14, R + 6, RIM + 6)),
                     ("e", (L - 20, RIM - 44, L - 10, RIM - 34)), ("e", (R + 10, RIM - 40, R + 20, RIM - 30))])
    return inner, over, top


def streak(layer, hx, hy):
    d = ImageDraw.Draw(layer)
    for dx, ln in ((-30, 46), (-8, 70), (16, 54)):
        d.line((hx + dx, hy - 60 - ln, hx + dx, hy - 60), fill=(255, 255, 255, 150), width=4)


# (머리 가운데 x 어긋남, 높이 기준, 높이 값, 각도, 반응, ms)
# 높이 기준 "rim" = 절구 입구 윗줄에서, "top" = 반죽 윗면에서(머리 바닥이 그만큼 박힘)
SINK = {1: 56, 2: 24, 3: 4}
REST = {1: 26, 2: 12, 3: 4}


def poses(stage):
    return [(-10, "top", REST[stage], 30, 0 if stage > 1 else 0, 110),
            (-18, "rim", -110, 38, 0, 80),
            (-30, "rim", -190, 48, 0, 90),
            (-32, "rim", -198, 50, 0, 140),
            (-12, "rim", -100, 18, 0, 45),
            (0, "top", SINK[stage], 0, 1, 160),
            (0, "top", SINK[stage] - 18, 4, 2, 90),
            (-10, "top", REST[stage], 30, 3, 140)]


def build_stage(stage, m):
    frames, times = [], []
    hh = m[2] * K
    for i, (dx, ref, v, ang, react, ms) in enumerate(poses(stage)):
        inner, over, top = dough_img(stage, react)
        if ref == "top":
            hy = top + v - hh / 2
            if ang:
                hy += 8
        else:
            hy = RIM + v - hh / 2
        hx = CX + dx
        f = Image.new("RGBA", (W, H)); f.alpha_composite(mortar, (OX, OY))
        mid = Image.new("RGBA", (W, H)); mid.alpha_composite(inner)
        if i == 4:
            streak(mid, hx, hy)
        mid.alpha_composite(place(m, ang, hx, hy))
        mp, op = mid.load(), f.load()
        for y in range(FRONT, H):                    # 절구 앞 테두리가 안쪽 것을 가린다
            for x in range(W):
                if mp[x, y][3] and op[x, y][3] and (x - OX, y - OY) not in INSIDE:
                    mp[x, y] = (0, 0, 0, 0)
        f.alpha_composite(mid)
        if over:
            f.alpha_composite(over)
        f.save(os.path.join(HERE, f"s{stage}_{i}.png"))
        frames.append(f); times.append(ms)
    return frames, times


def on_bg(f):
    bg = Image.open(BG).convert("RGBA")
    x0, y0 = SCREEN_X - W // 2, SCREEN_Y - H - BG_TOP
    bg.alpha_composite(f, (x0, y0))
    return bg.crop((x0, y0 - 20, x0 + W, y0 + H + 20))


def old_frame(i):
    im = Image.open(OLD.format(i)).convert("RGBA")
    im = im.resize((round(im.width * 1.55), round(im.height * 1.55)), Image.NEAREST)
    f = Image.new("RGBA", (W, H)); f.alpha_composite(im, (W // 2 - im.width // 2 - 20, H - im.getbbox()[3]))
    return f


def save_gif(name, frames, times):
    rgb = [fr.convert("RGB") for fr in frames]
    rgb[0].save(os.path.join(HERE, name), save_all=True, append_images=rgb[1:], duration=times, loop=0, disposal=1)


def main():
    ms = {t: mallet(t) for t in "ABC"}
    m = ms[PICK]
    allf, allt, stages = [], [], {}
    for s in (1, 2, 3):
        fr, tm = build_stage(s, m)
        stages[s] = fr
        bgf = [on_bg(x) for x in fr]
        save_gif(f"pound12_s{s}.gif", bgf + bgf, tm + tm[:-1] + [600])
        allf += bgf + [bgf[-1]]; allt += tm + [500]
    save_gif("pound12_이어서.gif", allf, allt)
    old = [on_bg(old_frame(i)) for i in range(7)]
    side = []
    for i, f in enumerate(stages[1]):
        o, n = old[min(6, i * 7 // 8)], on_bg(f)
        sh = Image.new("RGBA", (W * 2, n.height)); sh.alpha_composite(o, (0, 0)); sh.alpha_composite(n, (W, 0)); side.append(sh)
    save_gif("pound12_나란히.gif", side, poses(1) and [p[5] for p in poses(1)])
    # 견줌판
    ph = H + 40
    board = Image.new("RGBA", (W * 3, ph * 2 + 60), (40, 40, 48, 255))
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 20)
    except OSError:
        font = ImageFont.load_default()
    d = ImageDraw.Draw(board)
    cells = [(old[2], "옛 것 — 든 장"), (on_bg(stages[1][3]), "새 것 — 든 장"), (None, "떡메 후보 A · B · C")]
    cells += [(on_bg(stages[s][5]), f"{s}번째 친 장") for s in (1, 2, 3)]
    for k, (im, label) in enumerate(cells):
        x, y = (k % 3) * W, (k // 3) * (ph + 30) + 30
        if im is None:
            cand = Image.new("RGBA", (W, ph), (200, 190, 175, 255))
            yy = 20
            for t in "ABC":
                mi = ms[t][0]; mi = mi.resize((mi.width * K, mi.height * K), Image.NEAREST)
                cand.alpha_composite(mi, (40, yy))
                ImageDraw.Draw(cand).text((W - 120, yy + 30), t + (" (씀)" if t == PICK else ""), fill=(40, 30, 20), font=font)
                yy += mi.height + 30
            im = cand
        board.alpha_composite(im, (x, y))
        d.text((x + 8, y - 28), label, fill=(240, 240, 240), font=font)
    board.save(os.path.join(HERE, "pound12_견줌판.png"))
    print("head h:", {t: ms[t][2] * K for t in "ABC"}, "canvas", W, H, "rim", RIM, "floor", FLOOR)


if __name__ == "__main__":
    main()

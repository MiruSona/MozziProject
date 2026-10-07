# 떡메 치기 시안 P · Q (반복 09, pound11). 옛 kitchen_pound_0..6 의 떡메 자세를 그대로 따라 W1 절구 위에 놓는다.
# 실행: python build_pound11.py   (이 폴더에서. Q 는 raw/Q_mallet.png 가 있어야 한다)
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
sys.path.insert(0, TOOL)
import build_tool as bt  # noqa: E402

ROOT = os.path.abspath(os.path.join(TOOL, "..", "..", "..", "..", "..", ".."))
OLD = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen", "kitchen_pound_{}.png")
BG = os.path.join(TOOL, "..", "bg", "bg_2.png")
SCREEN_X, SCREEN_Y, BG_TOP = 270, 815, 250
W, H = 540, 385            # 화면 폭과 같게 넓혔다 — 자루가 화면 오른쪽 끝까지 이어지게
OX, OY = 65 + 103, 209     # 절구 자리 (지금 334 폭 프레임의 자리를 540 폭 가운데로 옮김)
SINK = 10                  # 치는 장에서 머리가 납작 반죽 윗면보다 박히는 깊이
HEAD_W = 46                # 떡메 머리 폭 (옛 장 단위, 옛 것은 약 37)

# 옛 프레임(212x241)에서 눈으로 잰 떡메 자세: (머리 가운데 x, y, 자루 각도°, 옛 장 번호)
# 각도 0 = 머리가 똑바로 서고 자루가 수평(치는 순간), 양수 = 자루가 오른쪽 위로 들림
OLD_POSE = [(68, 95, 35, 0), (75, 88, 33, 1), (72, 78, 30, 2), (69, 68, 0, 3),
            (69, 58, 0, 4), (70, 92, 33, 5), (68, 95, 35, 6)]
OUT, LINE = (58, 36, 20, 255), (90, 56, 30, 255)
LIGHT, MID, DARK = (228, 172, 108, 255), (188, 126, 70, 255), (128, 78, 40, 255)
CREAM, CREAM_D = (250, 244, 228, 255), (222, 210, 186, 255)


def old_mortar_box():
    """옛 0번 장 아랫부분(절구)의 좌우 폭과 입구 윗줄."""
    im = Image.open(OLD.format(0)).convert("RGBA")
    a = im.getchannel("A").load()
    rows = [(y, [x for x in range(im.width) if a[x, y]]) for y in range(120, im.height)]
    x0 = min(r[1][0] for r in rows if r[1]); x1 = max(r[1][-1] for r in rows if r[1])
    return x0, x1


def draw_p_mallet(s):
    """P 떡메: 옛 떡메 꼴(선 원통 머리 + 머리 위 1/3 에서 수평 자루)을 크게 · 외곽선 정돈 · 명암 3단으로 다시 그림."""
    hw, hh, th, hl = round(HEAD_W * s), round(72 * s), max(8, round(8 * s)), 600
    im = Image.new("RGBA", (hw + hl + 4, hh + 4))
    d = ImageDraw.Draw(im)
    ay = 2 + hh * 35 // 100                      # 자루가 붙는 높이
    # 자루: 굵기 일정, 위 밝게 · 가운데 중간 · 아래 어둡게
    d.rectangle((hw, ay - th // 2, hw + hl, ay + th // 2), fill=OUT)
    d.rectangle((hw, ay - th // 2 + 2, hw + hl, ay - 1), fill=LIGHT)
    d.rectangle((hw, ay, hw + hl, ay + th // 2 - 2), fill=MID)
    d.line((hw, ay + th // 2 - 2, hw + hl, ay + th // 2 - 2), fill=DARK)
    # 머리: 둥근 모서리 원통, 왼쪽 밝게 · 가운데 중간 · 오른쪽 어둡게
    d.rounded_rectangle((2, 2, 2 + hw, 2 + hh), radius=max(3, hw // 8), fill=OUT)
    d.rounded_rectangle((4, 4, hw, hh), radius=max(2, hw // 9), fill=MID)
    d.rectangle((6, 6, 4 + hw // 4, hh - 2), fill=LIGHT)
    d.rectangle((hw - hw // 4, 6, hw - 2, hh - 2), fill=DARK)
    for k in (0.2, 0.8):                         # 위아래 테 두 줄
        y = 2 + round(hh * k)
        d.line((4, y, hw, y), fill=OUT); d.line((4, y + 1, hw, y + 1), fill=DARK)
    for gx in (hw * 0.38, hw * 0.55):            # 나뭇결 두 줄
        gx = round(gx)
        d.line((gx, 2 + round(hh * 0.28), gx, 2 + round(hh * 0.72)), fill=LINE)
    return im, (2 + hw // 2, 2 + hh // 2)


def load_q_mallet(s):
    """Q 떡메: PixelLab 원본(raw/Q_mallet.png)을 머리가 선 꼴로 돌리고 자루를 이어 붙인다."""
    m = Image.open(os.path.join(HERE, "raw", "Q_mallet.png")).convert("RGBA")
    m = m.crop(m.getbbox())
    with open(os.path.join(HERE, "raw", "Q_mallet.txt"), encoding="utf-8") as f:
        hx0, hx1, ay, rep_x = (int(v) for v in f.read().split())   # 머리 x 범위, 자루 높이, 자루 늘일 때 베낄 열
    k = (HEAD_W * s) / (hx1 - hx0)
    m = m.resize((round(m.width * k), round(m.height * k)), Image.NEAREST)
    hx0, hx1, ay, rep_x = (round(v * k) for v in (hx0, hx1, ay, rep_x))
    col = m.crop((rep_x, 0, rep_x + 1, m.height))
    ext = Image.new("RGBA", (m.width + 600, m.height))
    ext.alpha_composite(m.crop((0, 0, rep_x, m.height)))
    for x in range(rep_x, ext.width):
        ext.alpha_composite(col, (x, 0))
    return ext, ((hx0 + hx1) // 2, m.height // 2)


def place(mallet, center, angle, pos):
    cx, cy = center
    R = mallet.width + mallet.height
    pad = Image.new("RGBA", (2 * R, 2 * R))
    pad.alpha_composite(mallet, (R - cx, R - cy))
    rot = pad.rotate(angle, resample=Image.NEAREST)
    layer = Image.new("RGBA", (W, H))
    layer.alpha_composite(rot.crop((R - pos[0], R - pos[1], R - pos[0] + W, R - pos[1] + H)))
    return layer


def specks(layer, cx, top, spread, up):
    d = ImageDraw.Draw(layer)
    for sx, sy, r in ((-1, 0, 5), (-1, -1, 3), (1, 0, 5), (1, -1, 3), (-0.6, -1.6, 2), (0.7, -1.5, 2)):
        x, y = cx + sx * spread, top - up + sy * 12
        d.ellipse((x - r - 1, y - r - 1, x + r + 1, y + r + 1), fill=CREAM_D)
        d.ellipse((x - r, y - r, x + r - 1, y + r - 1), fill=CREAM)


def build(tag, mallet, center, poses, s, ox0, oy_rim):
    mortar = bt.tight(bt.MORTAR)
    dough, flat = bt.tight("dough_s2.png"), bt.tight("dough_s2_flat.png")
    inside, (ix0, iy0, ix1, iy1) = bt.interior(mortar)
    cx, floor = mortar.width // 2, iy1 + 6
    front_y = OY + (iy0 + iy1) // 2
    rim_new = OY + iy0
    out = []
    for i, (hx, hy, ang, react) in enumerate(poses):
        # 반죽 반응: 0 쉼 · 1 눌림(납작+옆으로 퍼짐+튐) · 2 되돌아옴(살짝 위로 솟음)
        if react == 1:
            d = flat.resize((round(flat.width * 1.12), flat.height), Image.NEAREST)
        elif react == 2:
            d = dough.resize((round(dough.width * 0.95), round(dough.height * 1.08)), Image.NEAREST)
        else:
            d = dough
        frame = Image.new("RGBA", (W, H))
        frame.alpha_composite(mortar, (OX, OY))
        op = frame.copy().load()
        upper = Image.new("RGBA", (W, H))
        upper.alpha_composite(d, (OX + cx - d.width // 2, OY + floor - d.height))
        px = OX + cx + round((hx - ox0) * s)
        py = rim_new + round((hy - oy_rim) * s)
        if react == 1:   # 치는 장: 머리 바닥을 납작 반죽 윗면보다 SINK 칸 박히게 맞춘다 (옛 장도 머리가 반죽에 묻혔다)
            fa = d.getchannel("A")
            ftop = OY + floor - d.height + next(y for y in range(d.height) if fa.getpixel((d.width // 2, y)) > 200)
            py = ftop + SINK - (mallet.height - center[1])
            if hy < 68:  # 튕긴 장(옛 4번)은 조금 덜 박힌다
                py -= round((68 - hy) * s * 0.5)
        upper.alpha_composite(place(mallet, center, ang, (px, py)))
        if react == 1:
            specks(upper, OX + cx, rim_new, mortar.width * 0.42, 6)
        up = upper.load()
        for y in range(front_y, H):
            for x in range(W):
                if up[x, y][3] and op[x, y][3] and (x - OX, y - OY) not in inside:
                    up[x, y] = (0, 0, 0, 0)
        frame.alpha_composite(upper)
        frame.save(os.path.join(HERE, f"{tag}_{i}.png"))
        out.append(frame)
    return out


def on_bg(f):
    bg = Image.open(BG).convert("RGBA")
    bg.alpha_composite(f, (SCREEN_X - W // 2, SCREEN_Y - H - BG_TOP))
    return bg


def old_on_screen(i, s):
    """옛 장을 절구 폭이 W1 과 같게 키워 같은 자리에 놓은 540 폭 프레임."""
    im = Image.open(OLD.format(i)).convert("RGBA")
    big = im.resize((round(im.width * s), round(im.height * s)), Image.NEAREST)
    f = Image.new("RGBA", (W, H))
    mortar = bt.tight(bt.MORTAR)
    x0, _ = old_mortar_box()
    f.alpha_composite(big, (OX + mortar.width // 2 - round((x0 + old_mortar_box()[1]) / 2 * s), OY + mortar.height - big.height))
    return f


def gif(name, seqs, times, scale=1):
    frames, dur = [], []
    for rep in range(2):
        for k, ms in enumerate(times):
            ims = [on_bg(seq[k % len(seq)]) for seq in seqs]
            sheet = Image.new("RGBA", (sum(im.width for im in ims), ims[0].height))
            for j, im in enumerate(ims):
                sheet.alpha_composite(im, (j * ims[0].width, 0))
            if scale != 1:
                sheet = sheet.resize((sheet.width * scale // 2, sheet.height * scale // 2), Image.NEAREST)
            frames.append(sheet.convert("RGB"))
            dur.append(ms)
    dur[-1] += 500
    frames[0].save(os.path.join(HERE, name), save_all=True, append_images=frames[1:], duration=dur, loop=0, disposal=1)


def main():
    x0, x1 = old_mortar_box()
    mortar = bt.tight(bt.MORTAR)
    s = mortar.width / (x1 - x0 + 1)
    ox0, oy_rim = (x0 + x1) / 2, 100             # 옛 절구 가운데 x · 입구 윗줄 y(눈으로 잼)
    print(f"배율 {s:.2f}")
    react = {0: 0, 1: 0, 2: 0, 3: 1, 4: 1, 5: 2, 6: 0}
    p_pose = [(x, y, a, react[k]) for x, y, a, k in OLD_POSE]
    pm, pc = draw_p_mallet(s)
    P = build("P", pm, pc, p_pose, s, ox0, oy_rim)
    P_T = [90, 90, 120, 120, 70, 90, 90]
    seqs, times = [P], P_T
    if os.path.exists(os.path.join(HERE, "raw", "Q_mallet.png")):
        q_pose = p_pose[:2] + [(74, 56, 40, 0), (70, 62, 14, 0)] + p_pose[3:]   # 더 높이 든 장 + 내려치는 중간 장
        qm, qc = load_q_mallet(s)
        Q = build("Q", qm, qc, q_pose, s, ox0, oy_rim)
        Q_T = [90, 90, 120, 40, 120, 70, 90, 90]
        gif("pound11_Q.gif", [Q], Q_T)
        seqs.append(Q)
    gif("pound11_P.gif", [P], P_T)
    old = [old_on_screen(i, s) for i in range(7)]
    # 나란히: Q 의 8장 박자에 맞춰 옛 것·P 는 중간 장 자리에서 앞 장을 한 번 더 보인다
    if len(seqs) == 2:
        pick = [0, 1, 2, 2, 3, 4, 5, 6]
        gif("pound11_나란히.gif", [[old[k] for k in pick], [P[k] for k in pick], seqs[1]], Q_T)
    cols = [("old (<=08)", old[0], old[3]), ("P", P[0], P[3])]
    if len(seqs) == 2:
        cols.append(("Q", seqs[1][0], seqs[1][4]))
    crop = (0, 0, W, 540)                         # 배경 위 절구 둘레만
    cw, ch = W, 540
    sheet = Image.new("RGBA", (cw * len(cols), ch * 2 + 24), (96, 96, 96, 255))
    dr = ImageDraw.Draw(sheet)
    for c, (nm, a, b) in enumerate(cols):
        dr.text((c * cw + 8, 6), f"{nm}  (top: rest / bottom: hit)", fill=(255, 255, 255, 255))
        for r, im in enumerate((a, b)):
            full = on_bg(im)
            sheet.alpha_composite(full.crop((0, full.height - ch, W, full.height)), (c * cw, 24 + r * ch))
    sheet.save(os.path.join(HERE, "pound11_견줌판.png"))


if __name__ == "__main__":
    main()

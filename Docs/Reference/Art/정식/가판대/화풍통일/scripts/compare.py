"""비교판을 compare/ 에 만든다. (b1 → b2 → b3 → b4 → b5 → b6 다음에 돌린다)

화면 캡처 위 덧대기 : 고친 그림에서 「바뀐 칸」만, 캡처의 그 자리가 옛 그림 색 그대로일 때만 바꾼다
(위에 다른 그림이 덮은 칸은 안 건드린다). 유리는 캡처 칸이 배경 그림과 같을 때만 깐다.
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

from common import CMP, OUT, OUT_ALT, RES, SHOTS, load, on_bg

FONT = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 22)
INK = (0x35, 0x2E, 0x27)
PAPER = (0xFB, 0xEC, 0xD0)


def new(rel, base=OUT):
    return Image.open(os.path.join(base, rel)).convert("RGBA")


def label(im, text, xy):
    d = ImageDraw.Draw(im)
    d.text(xy, text, font=FONT, fill=INK)


def person(get, hair, shirt, face):
    im = Image.new("RGBA", (120, 240), (0, 0, 0, 0))
    for rel in ("Customer/body.png", "Customer/shirt_%s.png" % shirt, "Customer/face_0%d.png" % face,
                "Customer/hair_%s.png" % hair):
        im.alpha_composite(get(rel))
    return im


def sheet(cells, cols, pad=12, top=34, bg=PAPER):
    """cells = [(그림, 이름표)] → 격자 한 장."""
    cw = max(c[0].width for c in cells)
    ch = max(c[0].height for c in cells)
    rows = (len(cells) + cols - 1) // cols
    out = Image.new("RGB", (pad + cols * (cw + pad), pad + rows * (ch + top + pad)), bg)
    for i, (im, text) in enumerate(cells):
        x = pad + (i % cols) * (cw + pad)
        y = pad + (i // cols) * (ch + top + pad)
        label(out, text, (x + 4, y))
        out.paste(im.convert("RGB"), (x, y + top))
    return out


# ---------- b1 : 선 색
def b1():
    looks = [("01_orange", "green", 2), ("02_brown", "yellow", 1), ("02_black", "red", 3)]
    cells = []
    for name, get in (("지금", load), ("고친 뒤", new)):
        for i, (hair, shirt, face) in enumerate(looks):
            im = person(get, hair, shirt, face).crop((12, 22, 112, 240))
            cells.append((on_bg(im, 3), "%s · 손님 %d" % (name, i + 1)))
        cells.append((on_bg(get("Screen/stall_owner.png"), 3), "%s · 주인" % name))
    cells.append((on_bg(new("Screen/stall_owner.png", OUT_ALT), 3), "고를 판 · 주인 안쪽 선도"))
    sheet(cells, 4).save(os.path.join(CMP, "b1_line_before_after.png"))


# ---------- b2 : 귀 · 볼
def b2():
    cells = []
    box = (12, 24, 112, 114)
    for face in (1, 2, 3):
        for name, get in (("지금", load), ("고친 뒤", new)):
            im = person(get, "02_brown", "blue", face).crop(box)
            cells.append((on_bg(im, 6), "%s · 표정 %d · 곱슬" % (name, face)))
    for name, get in (("지금", load), ("고친 뒤", new)):
        cells.append((on_bg(get("Customer/face_01.png").crop(box), 6), "%s · 머리통만" % name))
    for name, get in (("지금", load), ("고친 뒤", new)):
        cells.append((on_bg(person(get, "01_brown", "blue", 1).crop(box), 6), "%s · 단발 (볼)" % name))
    sheet(cells, 2).save(os.path.join(CMP, "b2_ear_cheek_x6.png"))


# ---------- 캡처 위 덧대기
def find(shot, sprite, n=60, seed=1):
    """sprite 의 불투명 칸 몇 개를 뽑아 캡처에서 가장 많이 맞는 자리를 찾는다 → (ox, oy, 맞은 비율)."""
    a = np.asarray(sprite)
    ys, xs = np.nonzero(a[:, :, 3])
    pick = np.random.default_rng(seed).choice(len(ys), size=min(n, len(ys)), replace=False)
    H, W = shot.shape[:2]
    h, w = a.shape[:2]
    score = np.zeros((H + h, W + w), dtype=np.int32)   # 칸 (oy + h, ox + w) — 화면 밖으로 삐져나온 자리도 받는다
    for i in pick:
        y, x = int(ys[i]), int(xs[i])
        hit = np.all(shot == a[y, x, :3], axis=2)
        score[h - y:h - y + H, w - x:w - x + W] += hit
    oy, ox = np.unravel_index(np.argmax(score), score.shape)
    return int(ox) - w, int(oy) - h, float(score[oy, ox]) / len(pick)


def patch(shot, old, newim, ox, oy):
    """바뀐 칸만, 캡처가 옛 색일 때만 바꾼다. 바꾼 칸 수를 돌려준다."""
    a, b = np.asarray(old), np.asarray(newim)
    H, W = shot.shape[:2]
    n = 0
    for y, x in zip(*np.nonzero(np.any(a != b, axis=2) & (a[:, :, 3] > 0))):
        sy, sx = y + oy, x + ox
        if 0 <= sy < H and 0 <= sx < W and tuple(shot[sy, sx]) == tuple(a[y, x, :3]):
            shot[sy, sx] = b[y, x, :3]
            n += 1
    return n


def stall(glass):
    shot = np.array(Image.open(os.path.join(SHOTS, "b_stall_a.png")).convert("RGB"))
    base = shot.copy()
    log = []
    for rel in ("Screen/stall_counter.png", "Screen/stall_prop_vase.png", "Screen/stall_owner.png"):
        old = load(rel)
        ox, oy, s = find(base, old)
        log.append((rel, ox, oy, round(s, 2), patch(shot, old, new(rel), ox, oy)))
    # 손님 — 윗옷으로 자리를 찾는다. 겹은 캔버스가 같아 자리도 같다
    for shirt in ("blue", "green", "red", "yellow"):
        for flip in (False, True):                     # 오른쪽 줄 손님은 좌우로 뒤집혀 선다
            turn = ImageOps.mirror if flip else (lambda im: im)
            ox, oy, s = find(base, turn(load("Customer/shirt_%s.png" % shirt)))
            if s < 0.9:
                continue
            n = 0
            for rel in ["Customer/body.png"] + ["Customer/face_0%d.png" % i for i in (1, 2, 3)] +                        ["Customer/hair_0%d_black.png" % i for i in (1, 2)]:
                n += patch(shot, turn(load(rel)), turn(new(rel)), ox, oy)
            log.append(("손님 " + shirt + (" (뒤집힘)" if flip else ""), ox, oy, round(s, 2), n))
    if glass:
        old = load("Screen/display_case.png")
        ox, oy, s = find(base, old)
        g = np.asarray(new("Screen/display_case.png", OUT_ALT))
        a = np.asarray(old)
        bg = np.asarray(load("Screen/stall_bg.png").convert("RGB"))
        off = bg.shape[0] - shot.shape[0]              # 배경은 아래 맞춤
        n = 0
        for y, x in zip(*np.nonzero((a[:, :, 3] == 0) & (g[:, :, 3] > 0))):
            sy, sx = y + oy, x + ox
            if tuple(shot[sy, sx]) == tuple(bg[sy + off, sx]):
                shot[sy, sx] = g[y, x, :3]
                n += 1
        log.append(("유리", ox, oy, round(s, 2), n))
    return Image.fromarray(base), Image.fromarray(shot), log


def b5_and_whole():
    before, after, log = stall(False)
    _, after_glass, log2 = stall(True)
    for row in log + log2[-1:]:
        print("  덧대기 (무엇, x, y, 맞은 비율, 바꾼 칸)", row)
    box = (0, 540, 200, 700)
    cells = [(im.crop(box).resize((800, 640), Image.NEAREST), t) for im, t in
             ((before, "유리 없음 (지금)"), (after_glass, "유리 있음"))]
    sheet(cells, 1).save(os.path.join(CMP, "b5_case_glass.png"))
    cells = [(before, "지금"), (after, "고친 뒤 · 유리 없음"), (after_glass, "고친 뒤 · 유리 있음")]
    sheet(cells, 3).save(os.path.join(CMP, "stall_whole.png"))
    box = (0, 540, 540, 1080)
    cells = [(im.crop(box).resize((1080, 1080), Image.NEAREST), t) for im, t in
             ((before, "지금 ×2"), (after_glass, "고친 뒤 ×2 · 유리 있음"))]
    sheet(cells, 1).save(os.path.join(CMP, "stall_whole_x2.png"))


# ---------- b6 : 알림 표식
def b6():
    shot = Image.open(os.path.join(SHOTS, "a_main.png")).convert("RGBA")
    a = np.array(shot.convert("RGB"))
    stall_im = Image.open(os.path.join(RES, "Building", "stall_main.png")).convert("RGBA")
    ox, oy, s = find(a, stall_im)
    rows = np.arange(a.shape[0])[:, None]
    red = np.argwhere((a[:, :, 0] > 200) & (a[:, :, 1] < 70) & (a[:, :, 2] < 70) & (rows > 440) & (rows < 560))
    y0, x0 = (int(v) for v in red.min(axis=0))
    y1, x1 = (int(v) for v in red.max(axis=0))
    print("  빨간 네모", (x0, y0, x1, y1), "· 가판대 건물 자리", (ox, oy), "맞은 비율", round(s, 2))
    after = shot.copy()
    roof = stall_im.crop((x0 - ox, y0 - oy, x1 + 1 - ox, y1 + 1 - oy))     # 네모 자리를 지붕 그림으로 되돌린다
    after.alpha_composite(roof, (x0, y0))
    after.alpha_composite(new("UI/ui_badge_alert.png"), ((x0 + x1 + 1) // 2 - 8, (y0 + y1 + 1) // 2 - 8))
    box = (170, 450, 450, 690)
    cells = [(im.crop(box).resize((840, 720), Image.NEAREST), t)
             for im, t in ((shot, "지금 — 빨간 네모 ×3"), (after, "새 표식 16×16 ×3"))]
    sheet(cells, 1).save(os.path.join(CMP, "b6_badge_on_roof.png"))
    cells = [(im.crop((0, 440, 540, 700)), t) for im, t in ((shot, "지금 ×1"), (after, "새 표식 ×1"))]
    sheet(cells, 1).save(os.path.join(CMP, "b6_badge_on_roof_x1.png"))


if __name__ == "__main__":
    os.makedirs(CMP, exist_ok=True)
    b1()
    b2()
    b5_and_whole()
    b6()
    print("비교판 :", sorted(os.listdir(CMP)))

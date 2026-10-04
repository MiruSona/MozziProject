# 정식 그림 — 합성소 화면 2차 (2026-10-04 · 반복 04)
# raw/(PixelLab 원본) → out/ 의 배경 · 기계(쉼 · 도는 중 8장 · 다 됨) · 칸 조각 둘 · 나무 표 와
# 판정용 미리보기 · 동작 gif 를 만든다.
# PIL 은 자르기 · 배치 · 줄 이어 붙이기 · 글자 · 단색 상자 · 프레임 잇기만 한다. 새 그림을 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/합성소/build_synthesis.py
# 1차(v1) 값은 README 의 「1차 기록」 과 out/synthesis_bg_v1.png 에 남아 있다.
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw'); OUT = os.path.join(HERE, 'out'); os.makedirs(OUT, exist_ok=True)
SIAN = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '시안')
UI = os.path.join(HERE, '..', '가판대', 'out')          # 읽기만 한다
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
PREVIEW = os.path.join(SIAN, '2026-10-04-합성소화면-정식-v2-미리보기.png')
GIF = os.path.join(SIAN, '2026-10-04-합성기계-동작.gif')

W, H = 540, 1200            # 세계 = 9:20 화면. 9:16 은 아래 960 줄 (y 240~1200)
Y916 = H - 960
FLOOR_Y = 643               # 마루 시작 (1차와 같다 — 칸 좌표를 그대로 쓰려고)

# ---- 배경 잇는 자리 (raw 줄 번호) ----
TOP = 'top_20404'           # 기계실 천장 (구리관 · 철망 등 둘)
FLOOR = 'floor_20203'       # 마루는 1차 그대로
FLOORS = [(0, 399), (26, 184)]
WALLS = {                   # 벽 후보 둘 : (raw, 벽 줄 구간, 천장 줄 구간들)
    'a': ('wall_20401', (25, 434), [(0, 212), (338, 360)]),     # 배관 · 계기판 둘 · 스위치 · 톱니 · 주전자 탱크
    'b': ('wall_20402', (43, 440), [(0, 246)]),                         # 둥근 창 + 배관 · 큰 계기판 · 스위치 판 · 톱니
}
WIN_PATCH = ('wall_20402_window_20403', (188, 100))             # 벽 b 의 흰 창 속을 inpaint 로 다시 그린 조각 → 이 자리에 붙인다
PICK = 'a'                  # out/synthesis_bg.png 로 내보낼 후보

# ---- 기계 · 칸 조각 공통 캔버스 ----
BOX = (8, 9, 216, 229)      # raw 224×240 에서 자를 칸 → 208×220. 손잡이가 도는 자리 · 열린 서랍까지 들어간다
FEET = 209                  # raw 에서 받침 발 아래끝 (이 줄 바로 위까지가 그림)
PIVOT = (104, FEET - BOX[1])            # 조각 안 피벗 (발 가운데) = (104, 200) · 아래에서 20 줄 위
SLOTS = [(135, 670), (405, 670), (135, 950), (405, 950)]       # 칸 넷 : 피벗이 놓일 세계 좌표 (1차와 같다)
WIN_C, WIN_R = (105, 126), 30            # raw 에서 창 속 원 (가운데 · 반지름) — 도는 그림의 창 속을 쉬는 그림 것으로 덮는다
RUN = 'run_20801'                       # animate_image 9장 (f0 = 입력 그대로)
RUN_FRAMES = [0, 1, 2, 3, 4, 5, 6, 7]   # 고리 순서 (f8 은 f0 과 거의 같아 뺐다)
RUN_MS = 90

# 조각 안 자리 (조각 왼쪽 위 원점 · 208×220) — 쉬는 그림 기준
FUNNEL_L, FUNNEL_R = (56, 20), (142, 20)
WINDOW = (WIN_C[0] - BOX[0], WIN_C[1] - BOX[1], 31)
DRAWER = (52, 164, 120, 194)

BTN_H = 80
CREAM, INK, BGC = (250, 240, 220, 255), (60, 40, 32, 255), (58, 50, 48, 255)


def raw(n): return Image.open(os.path.join(RAW, n + '.png')).convert('RGBA')
def font(s): return ImageFont.truetype(FONT, s)
def box(d, x0, y0, x1, y1, fill, line=INK, w=3): d.rectangle((x0, y0, x1, y1), fill=fill, outline=line, width=w)
def txt(d, xy, s, size=15, fill=CREAM):
    sw = 0 if fill == INK else 3
    d.text(xy, s, font=font(size), fill=fill, anchor='mm', align='center', stroke_width=sw, stroke_fill=INK)


def nine(src, w, h, l, t, r, b):
    """9-slice 늘리기 (모서리는 그대로, 변 · 가운데는 픽셀 되풀이). 미리보기 · 나무 표용."""
    sw, sh = src.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0:
                out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


# ---------------- 배경 ----------------
def build_bg(key):
    name, (w0, w1), tops = WALLS[key]
    wall = raw(name)
    if key == 'b':
        p, xy = WIN_PATCH
        wall.paste(raw(p), xy)
    top, floor = np.array(raw(TOP)), np.array(raw(FLOOR))
    parts = [top[a:b] for a, b in tops] + [np.array(wall)[w0:w1]] + [floor[a:b] for a, b in FLOORS]
    col = np.concatenate(parts, axis=0)
    assert col.shape[0] == H, col.shape
    wall_y0 = sum(b - a for a, b in tops)
    assert wall_y0 + w1 - w0 == FLOOR_Y, (key, wall_y0 + w1 - w0)
    return Image.fromarray(col), wall_y0


# ---------------- 기계 ----------------
def feet_row(im):
    """받침 발이 있는 칸(x 40~190)에서 가장 아래 불투명 줄 + 1."""
    a = np.array(im)[:, 40:190, 3]
    rows = np.where(a.max(axis=1) > 0)[0]
    return rows.max() + 1


def shift(im, dx, dy):
    out = Image.new('RGBA', im.size, (0, 0, 0, 0)); out.paste(im, (dx, dy)); return out


def build_run():
    """도는 중 프레임 : ① 발 줄을 쉬는 그림에 맞춰 위아래로 옮긴다 ② 창 속(원)을 쉬는 그림 것으로 덮는다.
    animate_image 가 창 속에 김 덩이를 그려 넣어서다 — 창 속은 코드가 음양을 얹는 자리라 비워 둔다."""
    base = raw('machine_closed_20502')
    ba = np.array(base)
    yy, xx = np.mgrid[0:240, 0:224]
    frames, report = [], []
    for i in RUN_FRAMES:
        f = Image.open(os.path.join(RAW, RUN, 'f%d.png' % i)).convert('RGBA')
        f = shift(f, 0, FEET - feet_row(f))
        fa = np.array(f).astype(int)
        # 몸이 덜컹이며 창이 위아래로 몇 줄 움직인다 — 창 테 둘레(반지름 28~33)가 쉬는 그림과 가장 맞는 dy 를 찾는다
        best = None
        for dy in range(-5, 6):
            ring = ((xx - WIN_C[0]) ** 2 + (yy - WIN_C[1] - dy) ** 2)
            m = (ring >= 31 ** 2) & (ring <= 35 ** 2)
            src = np.roll(ba, dy, axis=0).astype(int)
            err = np.abs(fa[m] - src[m]).sum()
            if best is None or err < best[0]: best = (err, dy)
        dy = best[1]
        circle = ((xx - WIN_C[0]) ** 2 + (yy - WIN_C[1] - dy) ** 2) <= WIN_R ** 2
        fa[circle] = np.roll(ba, dy, axis=0)[circle]
        frames.append(Image.fromarray(fa.astype('uint8')).crop(BOX)); report.append((i, dy))
    return frames, report


def build_parts():
    idle = raw('machine_closed_20502').crop(BOX)
    done = raw('machine_done_20802').crop(BOX)
    out = {}
    for key, n in (('sign', 'stand_sign_20601'), ('lock', 'covered_20602')):
        im = raw(n); bb = im.getbbox()
        cx = (bb[0] + bb[2]) // 2
        im = shift(im, 112 - cx, FEET - bb[3])          # 가로 가운데 · 받침 아래끝을 기계 발 줄에 맞춘다
        out[key] = im.crop(BOX)
    plank = raw('plank_20901').crop((22, 7, 138, 33))  # 116×26
    return idle, done, out['sign'], out['lock'], plank


# ---------------- 미리보기 ----------------
def place(im, part, slot):
    im.alpha_composite(part, (slot[0] - PIVOT[0], slot[1] - PIVOT[1]))


def scene(bg, P, states, top_y, run_idx=0):
    """states : 칸마다 run · idle · done · buy · lock. 9:16 한 화면에 넷이 다 보인다 (칸 줄 둘이 모두 아래 960 줄 안)."""
    idle, done, sign, lock, plank, run = P['idle'], P['done'], P['sign'], P['lock'], P['plank'], P['run']
    btn, bub = Image.open(os.path.join(UI, 'ui_button.png')).convert('RGBA'), Image.open(os.path.join(UI, 'ui_bubble.png')).convert('RGBA')
    im = bg.copy()
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for slot, st in zip(SLOTS, states):
        part = {'run': run[run_idx], 'idle': idle, 'done': done, 'buy': sign, 'lock': lock}[st]
        place(im, part, slot)
        x, y = slot[0] - PIVOT[0], slot[1] - PIVOT[1]
        if st in ('run', 'idle'):
            for fx, fy in (FUNNEL_L, FUNNEL_R):
                if st == 'run':
                    box(d, x + fx - 22, y + fy - 30, x + fx + 22, y + fy + 8, (255, 255, 255, 230), w=2); txt(d, (x + fx, y + fy - 11), '찹쌀이', 12, INK)
                else:
                    b = nine(bub, 40, 38, 14, 12, 14, 20); im.alpha_composite(b, (x + fx - 20, y + fy - 52)); d.text((x + fx, y + fy - 40), '+', font=font(20), fill=INK, anchor='mm')
        if st == 'run':
            wx, wy, r = WINDOW
            d.ellipse((x + wx - 16, y + wy - 16, x + wx + 16, y + wy + 16), fill=(240, 240, 240, 200), outline=INK, width=2); txt(d, (x + wx, y + wy), '음양', 10, INK)
        if st == 'done':
            txt(d, (x + (DRAWER[0] + DRAWER[2]) // 2, y + DRAWER[1] + 8), '?', 22, INK)
        if st == 'buy':
            txt(d, (slot[0] + 1, y + 72), '칸 사기\n300G', 18, INK)
        if st == 'lock':
            pass
        if st in ('run', 'idle', 'done'):
            pw = 150; pl = nine(plank, pw, 26, 12, 8, 12, 8)
            px, py = slot[0] - pw // 2, slot[1] + 4
            im.alpha_composite(pl, (px, py))
            s = {'run': '1:42', 'idle': '시작 20G · 풀 75%', 'done': '다 됨! 받기'}[st]
            txt(d, (slot[0], py + 13), s, 13)
    box(d, 12, top_y + 12, 170, top_y + 60, (250, 240, 220, 220)); d.text((91, top_y + 36), '898 G', font=font(24), fill=INK, anchor='mm')
    by = H - 104
    txt(d, (W // 2, by - 22), '영구 단축 0/5 · 대기 100% → 90%', 16)
    for x0, x1, n in [(30, 250, '단축 사기 200G'), (270, 390, '찹쌀이 도감'), (410, 510, '닫기')]:
        im.alpha_composite(nine(btn, x1 - x0, BTN_H, 10, 10, 10, 10), (x0, by))
        d.text(((x0 + x1) // 2, by + BTN_H // 2), n, font=font(20), fill=INK, anchor='mm')
    return Image.alpha_composite(im, lay)


def board(cols, title, notes, extra=None):
    pad, head = 24, 96
    ch = max(c.height for _, c in cols); note_h = 30 * len(notes) + 20
    eh = extra.height + 40 if extra else 0
    out = Image.new('RGBA', (pad + len(cols) * (W + pad), head + ch + 40 + eh + note_h), BGC); d = ImageDraw.Draw(out)
    d.text((pad, 20), title, font=font(26), fill=CREAM)
    for i, (cap, c) in enumerate(cols):
        x = pad + i * (W + pad); d.text((x, head - 8), cap, font=font(18), fill=(230, 200, 170, 255), anchor='lb')
        out.paste(c, (x, head + ch - c.height))
    y = head + ch + 30
    if extra:
        out.alpha_composite(extra, (pad, y)); y += extra.height + 30
    for n in notes: d.text((pad, y), n, font=font(18), fill=CREAM); y += 30
    return out


def strip(frames, labels, scale=2, bg=(90, 120, 110, 255)):
    fw, fh = frames[0].width * scale, frames[0].height * scale
    out = Image.new('RGBA', (len(frames) * (fw + 8) + 8, fh + 40), bg); d = ImageDraw.Draw(out)
    for i, (f, lab) in enumerate(zip(frames, labels)):
        x = 8 + i * (fw + 8)
        out.alpha_composite(f.resize((fw, fh), Image.NEAREST), (x, 8))
        d.line((x, 8 + PIVOT[1] * scale, x + fw, 8 + PIVOT[1] * scale), fill=(255, 80, 80, 160))
        d.text((x + fw // 2, fh + 24), lab, font=font(16), fill=CREAM, anchor='mm')
    return out


if __name__ == '__main__':
    bgs = {k: build_bg(k) for k in WALLS}
    for k, (bg, wy) in bgs.items():
        bg.convert('RGB').save(os.path.join(OUT, 'synthesis_bg_%s.png' % k)); print('bg', k, 'wall_y0', wy)
    bgs[PICK][0].convert('RGB').save(os.path.join(OUT, 'synthesis_bg.png'))

    run, rep = build_run(); print('run dy', rep)
    idle, done, sign, lock, plank = build_parts()
    idle.save(os.path.join(OUT, 'synthesis_machine.png'))
    done.save(os.path.join(OUT, 'synthesis_machine_done.png'))
    sign.save(os.path.join(OUT, 'synthesis_slot_buy.png'))
    lock.save(os.path.join(OUT, 'synthesis_slot_locked.png'))
    plank.save(os.path.join(OUT, 'synthesis_plate.png'))
    for i, f in enumerate(run): f.save(os.path.join(OUT, 'synthesis_machine_run_%d.png' % i))
    sheet = Image.new('RGBA', (idle.width * len(run), idle.height), (0, 0, 0, 0))
    for i, f in enumerate(run): sheet.alpha_composite(f, (i * idle.width, 0))
    sheet.save(os.path.join(OUT, 'synthesis_machine_run_strip.png'))
    print('part', idle.size, 'pivot', PIVOT)

    # gif : 도는 고리 3바퀴 → 다 됨 한 번 (×2)
    gf = []
    for _ in range(3):
        for f in run: gf.append((f, RUN_MS))
    gf.append((done, 900)); gf.append((idle, 500))
    imgs = []
    for f, _ in gf:
        c = Image.new('RGBA', f.size, (176, 112, 92, 255)); c.alpha_composite(f); imgs.append(c.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('P', palette=Image.ADAPTIVE, colors=128))
    imgs[0].save(GIF, save_all=True, append_images=imgs[1:], duration=[ms for _, ms in gf], loop=0, disposal=2)

    P = dict(idle=idle, done=done, sign=sign, lock=lock, plank=plank, run=run)
    st = ['run', 'idle', 'buy', 'lock']
    bgA, bgB = bgs['a'][0], bgs['b'][0]
    s916 = scene(bgA, P, st, Y916).crop((0, Y916, W, H))
    s920 = scene(bgA, P, st, 0)
    s920b = scene(bgB, P, ['run', 'done', 'run', 'idle'], 0)
    r1 = strip([idle] + run[:6], ['쉼'] + ['도는 %d' % i for i in range(6)], scale=1)
    r2 = strip(run[6:] + [done, sign, lock], ['도는 %d' % i for i in range(6, len(run))] + ['다 됨', '살 칸', '잠김'], scale=1)
    extra = Image.new('RGBA', (max(r1.width, r2.width), r1.height + r2.height + 8), BGC)
    extra.alpha_composite(r1, (0, 0)); extra.alpha_composite(r2, (0, r1.height + 8))
    board([('9:16 (540×960) · 벽 a · 도는 중 · 쉼 · 살 수 있음 · 잠김', s916), ('9:20 (540×1200) · 벽 a', s920), ('9:20 · 벽 b (다른 후보) · 도는 중 · 다 됨', s920b)],
          '합성소 화면 정식 2차 · 기계실 · 2026-10-04 · 도트 1px · 모두 ×1', [
          '벽은 기계실로 다시 뽑았다 : 구리 배관 · 밸브 · 둥근 계기판 · 스위치 판 · 톱니와 벨트 · 김 나는 주전자 탱크. 천장도 배관 + 철망 등으로. 마루는 1차 그대로.',
          '벽 a(왼쪽 · 가운데) = 창 없이 배관 · 계기판 둘 · 주전자 탱크. 벽 b(오른쪽) = 둥근 창 + 큰 계기판 · 스위치 판. 기본은 a 로 내보냈다.',
          '아래 줄 : 기계 조각 208×220 하나의 쉼 · 도는 8장 · 다 됨 · 살 칸 · 잠김. 빨간 줄 = 피벗(발) 줄. 모든 조각이 같은 캔버스 · 같은 피벗이다.',
          '흰 상자 = 찹쌀이 자리 · 회색 원 「음양」 = 코드가 창 안에 얹는 회전 · 「?」 = 다 됨 그림자. 나무 표 · 버튼 틀 · 말풍선은 정식 조각 9-slice.',
          '버튼은 아래 줄 (단축 사기 · 찹쌀이 도감 · 닫기). 9:20 에서만 보이는 위 240 줄 = 천장 · 들보 · 구리관 · 철망 등 둘.'], extra).convert('RGB').save(PREVIEW)
    print('ok')

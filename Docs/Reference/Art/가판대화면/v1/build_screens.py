# 가판대 화면 · 합성소 화면 시안 v1 합성 스크립트 — 2026-10-03 반복 03
# PIL 은 자르기·배경 지우기·배치·9조각 이어 붙이기·확대·글자 얹기·곱하기 색 미리보기만 한다. 그림을 그리거나 다시 칠하지 않는다.
# (예외 : 하늘 단색 바탕, 시안 바깥 여백과 설명 글)
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v1/build_screens.py
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
REF = os.path.join(ROOT, 'Docs', 'Reference', 'Art')
RAW_S = os.path.join(HERE, 'raw'); PARTS_S = os.path.join(HERE, 'parts')
RAW_M = os.path.join(REF, '합성소화면', 'v1', 'raw'); PARTS_M = os.path.join(REF, '합성소화면', 'v1', 'parts')
MAIN1 = os.path.join(REF, '메인화면', 'v1', 'parts'); MAIN2 = os.path.join(REF, '메인화면', 'v2', 'parts')
CUST = os.path.join(REF, '손님', 'v2-새화풍', 'layers')
ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
OUT = os.path.join(REF, '시안')
# 임시 그림 (저장소 밖 · 미확정) : 가판대 주인 v9 1번, 지금 게임 화면 캡처
OWNER = r'C:\Users\USER\AppData\Local\Temp\claude\mozzi-owner-02\v9\pick1.png'
CAPS = r'C:\Users\USER\AppData\Local\Temp\claude\C--Mirusona-Project-ClaudeProject-ProjectMozzi2\653c476f-f547-4ae3-a69f-180cce86dc67\scratchpad\captures'
os.makedirs(PARTS_S, exist_ok=True); os.makedirs(PARTS_M, exist_ok=True)

W = 540; H_TALL = 1200
SKY = (212, 238, 250, 255); INK = (74, 44, 36); CREAM = (255, 246, 226); GRAY = (150, 130, 120); BG = (58, 50, 48, 255)


def op(path): return Image.open(path).convert('RGBA')
def raw_s(n): return op(os.path.join(RAW_S, n + '.png'))
def raw_m(n): return op(os.path.join(RAW_M, n + '.png'))
def m1(n): return op(os.path.join(MAIN1, n + '.png'))
def m2(n): return op(os.path.join(MAIN2, n + '.png'))
def art(rel): return op(os.path.join(ART, rel + '.png'))
def cust(n): return op(os.path.join(CUST, n + '.png'))
def trim(im): return im.crop(im.getbbox())
def font(size): return ImageFont.truetype(FONT, size)


def key_flood(im, tol=40):
    a = np.array(im).astype(int); h, w = a.shape[:2]
    ref = a[0, 0, :3]; near = (np.abs(a[..., :3] - ref).sum(-1) <= tol)
    seen = np.zeros((h, w), bool); stack = [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)]
    while stack:
        y, x = stack.pop()
        if y < 0 or x < 0 or y >= h or x >= w or seen[y, x] or not near[y, x]: continue
        seen[y, x] = True; stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    a[seen] = 0
    return Image.fromarray(a.astype('uint8'))


def nine(im, w, h, b):
    out = Image.new('RGBA', (w, h)); sw, sh = im.size
    xs = [(0, b, 0), (b, sw - b, b), (sw - b, sw, w - b)]; ys = [(0, b, 0), (b, sh - b, b), (sh - b, sh, h - b)]
    for iy, (y0, y1, dy) in enumerate(ys):
        for ix, (x0, x1, dx) in enumerate(xs):
            piece = im.crop((x0, y0, x1, y1))
            tw = w - 2 * b if ix == 1 else b; th = h - 2 * b if iy == 1 else b
            if tw <= 0 or th <= 0: continue
            cell = Image.new('RGBA', (tw, th))
            for ty in range(0, th, piece.height):
                for tx in range(0, tw, piece.width): cell.paste(piece, (tx, ty))
            out.paste(cell, (dx, dy))
    return out


# ---------- 부품 (자르기·배경 지우기만) ----------
S = {}; M = {}
S['shop'] = trim(raw_s('shop_6101'))
S['shop_alt'] = trim(raw_s('shop_6102'))
S['counter'] = trim(raw_s('counter_6401'))
S['chalkboard'] = trim(raw_s('chalkboard_6402'))
S['tree_lamp'] = trim(raw_s('tree_lamp_6403'))
S['ui_btn_wide'] = trim(key_flood(raw_s('btn_wide_6301')))
S['ui_bubble'] = trim(key_flood(raw_s('bubble_big_6303').crop((4, 4, 52, 44))))   # 원본 가장자리에 흰 띠가 있어 4px 안쪽을 자른다
S['ui_bubble_small'] = trim(key_flood(raw_s('bubble_6302')))
M['machine'] = trim(raw_m('machine_6201'))
M['machine_alt'] = trim(raw_m('machine_6202'))
pr = trim(raw_m('props_6501'))
M['props_sacks'] = trim(pr.crop((0, 0, 97, pr.height))); M['props_mortar'] = trim(pr.crop((97, 0, pr.width, pr.height)))
for k, v in S.items(): v.save(os.path.join(PARTS_S, k + '.png'))
for k, v in M.items(): v.save(os.path.join(PARTS_M, k + '.png'))
P = dict(S); P.update(M)
for k in ['forest', 'clouds', 'path', 'ui_capsule', 'ui_ribbon', 'ui_tab', 'icon_coin']: P[k] = m1(k)
for k in ['grass', 'ui_panel_light', 'mozzi_body_edit_a']: P[k] = m2(k)
P['owner'] = trim(op(OWNER))        # 임시 · 미확정 (128×160 원본 크기 그대로)


def mozzi(color_hex, face, crown):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5))
    a = np.array(P['mozzi_body_edit_a']).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    im = Image.fromarray(a.astype('uint8'))
    im.alpha_composite(art('Mozzi/face_%02d' % face))
    ck = art('Mozzi/cheek'); im.alpha_composite(ck, (14, 34)); im.alpha_composite(ck, (39, 34))
    if crown: im.alpha_composite(art('Mozzi/acc_crown'))
    return im


def customer(hair, shirt, bottom, face):
    im = cust('body')
    for n in ('bottom_' + bottom, 'shirt_' + shirt, 'face_%02d' % face, 'hair_' + hair): im.alpha_composite(cust(n))
    return im


def paste(dst, im, x, y):
    x, y = int(x), int(y)
    if y + im.height <= 0 or y >= dst.height or x + im.width <= 0 or x >= dst.width: return
    y0 = max(0, -y); x0 = max(0, -x)
    im = im.crop((x0, y0, max(x0, min(im.width, dst.width - x)), max(y0, min(im.height, dst.height - y))))
    if im.width > 0 and im.height > 0: dst.alpha_composite(im, (max(x, 0), max(y, 0)))


def paste_base(dst, im, cx, base_y): paste(dst, im, cx - im.width // 2, base_y - im.height)


def backdrop(horizon, path_top, ground_bottom, path_rows=1):
    im = Image.new('RGBA', (W, H_TALL), SKY)
    f = P['forest']; band = Image.new('RGBA', (800, f.height)); band.paste(f, (0, 0)); band.paste(ImageOps.mirror(f), (400, 0))
    c = P['clouds']
    paste(im, c, 150, horizon - f.height - c.height + 6); paste(im, ImageOps.mirror(c), -160, horizon - f.height - 2 * c.height - 30)
    paste(im, c, 250, horizon - f.height - 3 * c.height - 90); paste(im, ImageOps.mirror(c), -90, horizon - f.height - 4 * c.height - 150)
    paste(im, band.crop((130, 0, 130 + W, f.height)), 0, horizon - f.height)
    g = P['grass']
    for y in range(horizon, ground_bottom, 32):
        for x in range(-2, W, 32): paste(im, g, x, y)
    p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
    for r in range(path_rows): paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top + r * 60)
    return im


def btn(im, texts, x, y, w, h, label, size=22, col=INK, icon=None):
    paste(im, nine(P['ui_btn_wide'], w, h, 12), x, y)
    tx = x + w // 2
    if icon is not None:
        paste(im, icon, x + 8, y + (h - icon.height) // 2); tx = x + (w + icon.width) // 2 + 4
    texts.append((label, tx, y + h // 2 - 1, size, col, 'mm'))


def topbar(im, texts, title, gold):
    cap = P['ui_capsule']; coin = P['icon_coin']
    paste(im, cap, 8, 8); paste(im, coin, 20, 8 + (cap.height - coin.height) // 2); texts.append((gold, 8 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    rb = P['ui_ribbon']; paste(im, rb, (W - rb.width) // 2, 10); texts.append((title, W // 2, 10 + 10, 24, CREAM, 'mm'))
    btn(im, texts, W - 8 - 76, 6, 76, 32, '닫기', 22)


# ---------- 가판대 ----------
def stall_world():
    py = H_TALL - 330; path_top = py - 136; horizon = path_top - 84
    im = backdrop(horizon, path_top, H_TALL, 2); labels = []
    base = path_top + 14
    paste_base(im, P['tree_lamp'], 52, base)
    paste_base(im, P['shop'], 214, base)
    paste_base(im, P['chalkboard'], 366, base + 2)
    paste_base(im, P['owner'], 462, base + 44)             # 주인 : 진열대 뒤 (임시 그림 · 원본 크기)
    cn = P['counter']; paste_base(im, cn, 462, base + 62)
    ck = art('Cake/redbean_cake'); cx0 = 462 - cn.width // 2
    paste(im, ck, cx0 + 8, base + 62 - cn.height - 2); paste(im, ck, cx0 + 42, base + 62 - cn.height - 2)
    labels.append(('×30', 462, base + 62 - 14, 20, CREAM, 'mm', INK))
    paste(im, nine(P['ui_btn_wide'], 150, 30, 12), 304, base - 104); labels.append(('오늘의 떡 : 팥떡', 379, base - 90, 20, INK, 'mm'))
    bub = P['ui_bubble']; feet = path_top + 104
    line = [('02_brown', '01_blue', '01_navy', 1, 364), ('04_orange', '02_yellow', '02_beige', 2, 288), ('05_black', '03_green', '01_gray', 3, 212),
            ('01_gray', '01_red', '02_navy', 1, 136), ('06_brown', '02_green', '01_beige', 2, 60)]
    for (hair, shirt, bottom, face, cx) in line:
        paste(im, customer(hair, shirt, bottom, face), cx - 32, feet - 76)
        by = feet - 72 - bub.height
        paste(im, bub, cx - bub.width // 2, by)
        c2 = trim(ck); paste(im, c2, cx - c2.width // 2, by + (bub.height - 8 - c2.height) // 2)
    return im, labels, py


def stall_ui(im, H, py, state):
    t = []
    topbar(im, t, '가판대', '798')
    paste(im, nine(P['ui_tab'], 306, 35, 12), 8, 44); t.append(('오늘 손님 3/10 · 열림 · 진열 30/30', 20, 44 + 17, 22, INK, 'lm'))
    ph = H - py - 8; px, pw = 8, W - 16
    paste(im, nine(P['ui_panel_light'], pw, ph, 10), px, py)
    x = px + 16; y = py + 14
    t.append(('진열 30/30 · 팥떡×30', x, y + 10, 22, INK, 'lm')); y += 30
    t.append(('만들기 (찾은 레시피)', x, y + 10, 20, GRAY, 'lm')); y += 24
    btn(im, t, x, y, 150, 40, '팥떡 15G', 22, icon=trim(art('Cake/redbean_cake'))); y += 50
    t.append(('실험 (재료 1~3개)', x, y + 10, 20, GRAY, 'lm')); y += 24
    for i, (ing, n) in enumerate([('redbean', '24'), ('bean', '0'), ('mugwort', '0'), ('sesame', '0'), ('pinenut', '0'), ('jujube', '0')]):
        btn(im, t, x + i * 83, y, 78, 40, n, 22, icon=art('Ingredient/' + ing))
    y += 50
    t.append(('재료를 1~3개 고르세요', x, y + 20, 20, INK, 'lm'))
    btn(im, t, px + pw - 16 - 130, y, 130, 40, '만들기', 24, col=GRAY)
    by = py + ph - 60; bw = (pw - 32 - 16) // 3
    for i, lab in enumerate(['열림', '점원 200G', '레시피 도감']): btn(im, t, x + i * (bw + 8), by, bw, 44, lab, 24)
    return t


# ---------- 합성소 ----------
def synth_world(state):
    py = H_TALL - 216; path_top = py - 84; horizon = path_top - 84
    im = backdrop(horizon, path_top, H_TALL); labels = []
    base = path_top + 44; mc = P['machine']; mx = 270 - mc.width // 2; my = base - mc.height
    paste_base(im, P['props_sacks'], 78, base - 6); paste_base(im, P['props_mortar'], 468, base - 4)
    paste(im, mc, mx, my)
    fl = (mx + 60, my + 41); fr = (mx + 162, my + 41)            # 입구 둘의 가운데 (기계 그림 안 좌표)
    def plate(cx, text, col=INK):
        paste(im, nine(P['ui_btn_wide'], 104, 30, 12), cx - 52, my - 40); labels.append((text, cx, my - 26, 20, col, 'mm'))
    paste(im, mozzi('#FFFFFF', 1, False), fl[0] - 32, fl[1] - 50); plate(fl[0] - 10, '흰찹쌀이')
    if state == 'both':
        paste(im, mozzi('#4FA84A', 2, True), fr[0] - 32, fr[1] - 50); plate(fr[0] + 10, '풀찹쌀이')
    else:
        bb = P['ui_bubble']; paste(im, bb, fr[0] - bb.width // 2, fr[1] - 16 - bb.height); labels.append(('+', fr[0], fr[1] - 16 - bb.height // 2 - 5, 52, INK, 'mm')); plate(fr[0] + 10, '눌러서 고르기', GRAY)
    return im, labels, py


def synth_ui(im, H, py, state):
    t = []
    topbar(im, t, '합성소', '898')
    ph = H - py - 8; px, pw = 8, W - 16
    paste(im, nine(P['ui_panel_light'], pw, ph, 10), px, py)
    x = px + 16; y = py + 14
    t.append(('단축 0/5 · 대기 100% → 90%', x, y + 17, 22, INK, 'lm')); btn(im, t, px + pw - 16 - 170, y, 170, 36, '단축 사기 200G', 22); y += 46
    tw = (pw - 32 - 18) // 4
    for i, lab in enumerate(['1 · 비었음', '2 · 2:26', '3 · 칸 사기', '4 · 잠김']):
        if i == 0: paste(im, nine(P['ui_tab'], tw, 40, 12), x, y - 2); t.append((lab, x + tw // 2, y + 17, 22, INK, 'mm'))
        else: btn(im, t, x + i * (tw + 6), y, tw, 36, lab, 20, col=INK if i == 1 else GRAY)
    y += 50
    if state == 'both':
        t.append(('풀찹쌀이 75%', x, y + 12, 20, INK, 'lm'))
        t.append(('다른 계열(물/불) 20% · 변종 ??? 5%', x, y + 38, 20, INK, 'lm'))
        t.append(('대기 2:30', x, y + 64, 20, INK, 'lm'))
        btn(im, t, px + pw - 16 - 140, y + 16, 140, 46, '시작 20G', 24)
    else:
        t.append(('재료 2마리를 고르세요', x, y + 12, 20, INK, 'lm'))
        t.append(('기계 입구를 눌러 찹쌀이를 넣는다', x, y + 38, 20, GRAY, 'lm'))
        btn(im, t, px + pw - 16 - 140, y + 16, 140, 46, '시작', 24, col=GRAY)
    return t


def render(kind, H, state=None):
    world, labels, py = stall_world() if kind == 'stall' else synth_world(state)
    off = H_TALL - H; im = world.crop((0, off, W, H_TALL))
    texts = stall_ui(im, H, py - off, state) if kind == 'stall' else synth_ui(im, H, py - off, state)
    for l in labels: texts.append((l[0], l[1], l[2] - off) + tuple(l[3:]))
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for t in texts:
        s, x, y, size, col, anchor = t[:6]; stroke = t[6] if len(t) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big


def board(title, cols, notes):
    """cols : (머리글, 그림) 목록. 그림은 모두 폭 1080."""
    Mg = 40; top = 150; hmax = max(c[1].height for c in cols)
    cw = Mg * (len(cols) + 1) + 1080 * len(cols); c = Image.new('RGBA', (cw, top + hmax + 40 + 38 * len(notes) + 20), BG); d = ImageDraw.Draw(c)
    d.text((Mg, 24), title, font=font(40), fill=(255, 240, 220))
    for i, (head, im) in enumerate(cols):
        x = Mg + i * (1080 + Mg); d.text((x, 100), head, font=font(26), fill=(230, 200, 170)); c.paste(im.convert('RGB'), (x, top))
    y = top + hmax + 30
    for line in notes: d.text((Mg, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


def cap(name):
    p = os.path.join(CAPS, name)
    if not os.path.exists(p): return None
    im = op(p); return im.resize((1080, int(im.height * 1080 / im.width)), Image.NEAREST)


if __name__ == '__main__':
    cols = []
    c = cap('it03_stall.png')
    if c: cols.append(('지금 게임 화면 (v13 캡처 · 9:20)', c))
    cols += [('새 시안 9:16 · 540×960 (×2)', render('stall', 960)), ('새 시안 9:20 · 540×1200 (×2) · 남는 세로는 하늘', render('stall', 1200))]
    board('가판대 화면 시안 v1 : 가게 바깥 정면 + 아래로 붙이기', cols, [
        '아래로 붙였다 : 종이 패널 → 길(손님 줄) → 가게. 화면이 길어지면 하늘만 늘어난다.',
        '지금 있는 것을 다 옮겼다 : 골드 · 닫기 · 오늘 손님/열림/진열 · 오늘의 떡 칠판 · 진열 떡(×30) · 손님 줄과 주문 말풍선',
        '· 진열 줄 · 찾은 레시피 버튼 · 실험 재료 6칸 · 안내 글 · 만들기 · 열림 · 점원 · 레시피 도감. 새 기능은 없다.',
        '주인 그림은 임시(v9 1번 · 미확정)다. 128×160 원본 크기 그대로라 손님(64×80)의 두 배다 : 절반으로 줄이면 얼굴이 뭉개진다.',
        '벽시계 · 빈 액자(지금 화면의 꾸밈)는 뺐다. 떡메 치기 화면 · 밤 상태(「다음날로」)는 안 그렸다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v1.png'))
    cols = []
    c = cap('it03_synth_plus.png')
    if c: cols.append(('지금 게임 화면 (v13 캡처 · 9:20)', c))
    cols += [('새 시안 9:16 · 왼쪽 입구에 찹쌀이, 오른쪽 입구는 빈 「+」', render('synth', 960, 'one')), ('새 시안 9:20 · 둘 다 넣은 상태 (시작할 수 있음)', render('synth', 1200, 'both'))]
    board('합성소 화면 시안 v1 : 바깥에 선 떡 기계 + 입구 둘을 눌러 고르기', cols, [
        '아래로 붙였다 : 종이 패널 → 기계. 화면이 길어지면 하늘만 늘어난다.',
        '「고르기」 버튼 둘을 없애고 기계 입구(깔때기) 둘을 직접 누른다 : 입구 위 이름표가 고른 종을 알려 준다. (누르는 자리만 바뀜 · 새 기능 아님)',
        '합성 칸 1~4 는 지금처럼 탭 한 줄이다 : 기계는 하나고, 탭을 누르면 그 칸의 입구 · 남은 시간으로 바뀐다. (한 안)',
        '지금 있는 것 : 골드 · 닫기 · 단축 줄과 단축 사기 · 칸 탭(비었음 · 남은 시간 · 칸 사기 · 잠김) · 확률 미리보기 · 시작. 숫자와 확률 글은 자리표시다.',
        '안 그린 것 : 도는 중(가운데 창의 음양) · 다 됨(받기) · 결과 덮개(NEW!) · 고르는 목록(보관함 · 농장 마릿수). 결과는 아래 나무 받침으로 나온다.']).save(os.path.join(OUT, '2026-10-03-합성소화면-v1.png'))
    print({k: v.size for k, v in P.items()})

# 가판대 화면 · 합성소 화면 시안 v2 합성 스크립트 — 2026-10-03 반복 03
# v2 의 뜻 : 「그 장소에 들어온 전용 화면」. 그림이 화면을 채우고, 기능은 화면 속 물건으로 옮긴다 (아래 종이 패널 없음).
# PIL 은 자르기·배경 지우기·배치·뒤집기·9조각 이어 붙이기·확대·글자·곱하기 색 미리보기만 한다. 그림을 그리거나 다시 칠하지 않는다.
# (예외 : 하늘 단색 바탕, 시안 바깥 여백과 설명 글)
# v1 스크립트(../v1/build_screens.py)의 함수와 부품을 불러 쓴다. 합성소 부품은 ../../합성소화면/v2/ 에 있다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v2/build_screens_v2.py
import os, math, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v1', os.path.join(HERE, '..', 'v1', 'build_screens.py'))
v1 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v1)
REF, OUT, W, H_TALL, SKY, INK, CREAM, GRAY, BG = v1.REF, v1.OUT, v1.W, v1.H_TALL, v1.SKY, v1.INK, v1.CREAM, v1.GRAY, v1.BG
op, trim, font, nine, paste, paste_base, art, customer, key_flood = v1.op, v1.trim, v1.font, v1.nine, v1.paste, v1.paste_base, v1.art, v1.customer, v1.key_flood
RAW_S = os.path.join(HERE, 'raw'); PARTS_S = os.path.join(HERE, 'parts')
RAW_M = os.path.join(REF, '합성소화면', 'v2', 'raw'); PARTS_M = os.path.join(REF, '합성소화면', 'v2', 'parts')
MAIN3 = os.path.join(REF, '메인화면', 'v3', 'parts'); MAIN4 = os.path.join(REF, '메인화면', 'v4', 'raw')
os.makedirs(PARTS_S, exist_ok=True); os.makedirs(PARTS_M, exist_ok=True)
# 주인 : 10차 A (확정 그림 · 128×160). 손님 크기 판(v11)이 있으면 그것을 쓴다.
OWNER_DIR = r'C:\Users\USER\AppData\Local\Temp\claude\mozzi-owner-02'
OWNER = os.path.join(OWNER_DIR, 'v11', 'pick1.png'); OWNER_IS_SMALL = os.path.exists(OWNER)
if not OWNER_IS_SMALL: OWNER = os.path.join(OWNER_DIR, 'v10', 'pick1.png')
GREEN = (170, 235, 150)

P = dict(v1.P); S = {}; M = {}
S['shop_wide'] = trim(op(os.path.join(RAW_S, 'shop_wide_9101.png')))                       # 508×428
S['basket'] = trim(op(os.path.join(RAW_S, 'basket_9201.png')))
S['mortar_big'] = trim(op(os.path.join(RAW_S, 'mortar_9202.png')))
S['icon_apron'] = trim(op(os.path.join(RAW_S, 'icon_apron_9203.png')))
S['ground_sand'] = op(os.path.join(RAW_S, 'ground_sand_9301.png')).crop((0, 56, 400, 240))    # 위쪽에 딸려 온 가게 · 나무를 잘라 낸다
M['ground_stone'] = op(os.path.join(RAW_M, 'ground_stone_9302.png')).crop((64, 76, 392, 230))  # 위 · 왼쪽 담을 잘라 낸다
M['pedestal'] = trim(op(os.path.join(RAW_M, 'pedestal_9401.png')))
M['noticeboard'] = trim(op(os.path.join(RAW_M, 'noticeboard_9403.png')))
mc = P['machine']                                                                             # v1 기계 230×227 그대로
cov = op(os.path.join(RAW_M, 'machine_covered_9402.png')); src = op(os.path.join(v1.RAW_M, 'machine_6201.png'))
M['machine_covered'] = cov.crop(src.getbbox())                                               # 원본 기계와 같은 자리로 자른다 (230×227)


def funnel_front(xc, yc=38, rx=32, ry=6, box=(18, 26, 82, 60)):
    """깔때기 입구의 앞 테두리 조각 : 입구 타원의 아래 호보다 아래에 있는 기계 픽셀만 남긴다 (자르기). 찹쌀이 위에 덮는다."""
    x0 = xc - (box[2] - box[0]) // 2; c = np.array(mc.crop((x0, box[1], x0 + box[2] - box[0], box[1] + box[3] - box[1]))); h, w = c.shape[:2]
    for x in range(w):
        dx = (x0 + x - xc) / rx
        edge = yc + ry * math.sqrt(1 - dx * dx) if abs(dx) < 1 else 0
        for y in range(h):
            if box[1] + y < edge: c[y, x, 3] = 0
    return Image.fromarray(c), x0, box[1]


FUN = {'L': 59, 'R': 163}                                # 입구 가운데 x (기계 그림 안)
for k, xc in FUN.items(): M['funnel_front_' + k] = funnel_front(xc)[0]
for k, v in S.items(): v.save(os.path.join(PARTS_S, k + '.png'))
for k, v in M.items(): v.save(os.path.join(PARTS_M, k + '.png'))
P.update(S); P.update(M)
P['owner'] = trim(op(OWNER)); P['field'] = op(os.path.join(MAIN4, 'grass_field_8101.png'))
P['body'] = op(os.path.join(MAIN3, 'mozzi_body_hop0_line110.png'))
for k in ['ui_btn', 'icon_book']: P[k] = v1.m1(k)
P['props_sacks'] = op(os.path.join(v1.PARTS_M, 'props_sacks.png')); P['props_mortar'] = op(os.path.join(v1.PARTS_M, 'props_mortar.png'))


def mozzi(color_hex, face, crown):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5)); a = np.array(P['body']).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    im = Image.fromarray(a.astype('uint8')); im.alpha_composite(art('Mozzi/face_%02d' % face))
    ck = art('Mozzi/cheek'); im.alpha_composite(ck, (14, 34)); im.alpha_composite(ck, (39, 34))       # 볼 : 샛노랑 그대로 (확정)
    if crown: im.alpha_composite(art('Mozzi/acc_crown'))
    return im


def tint(im, color_hex):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5)); a = np.array(im).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    return Image.fromarray(a.astype('uint8'))


def tw(text, size): return int(font(size).getlength(text) / 2) + 1


def tag(im, t, cx, cy, text, size=20, col=CREAM, icon=None, minw=0):
    """작은 나무 숫자표 · 팻말 (9조각). 가운데 (cx, cy)."""
    iw = (icon.width + 4) if icon is not None else 0; w = max(minw, tw(text, size) + 22 + iw); h = max(26, (icon.height + 6) if icon is not None else 26)
    x = int(cx - w // 2); y = int(cy - h // 2); paste(im, nine(P['ui_tab'], w, h, 12), x, y)
    if icon is not None: paste(im, icon, x + 9, y + (h - icon.height) // 2)
    t.append((text, x + (w + iw) // 2 + (2 if icon is not None else 0), y + h // 2 - 1, size, col, 'mm', INK))


def label(t, cx, cy, text, size=20, col=CREAM, anchor='mm'): t.append((text, cx, cy, size, col, anchor, INK))


def tile(tex, w, h):
    """땅 무늬 한 장을 좌우 · 위아래로 뒤집어 가며 이어 붙여 w×h 를 채운다."""
    out = Image.new('RGBA', (w, h)); row = 0
    for y in range(0, h, tex.height):
        col = 0
        for x in range(0, w, tex.width):
            p = tex if col % 2 == 0 else ImageOps.mirror(tex)
            out.paste(p if row % 2 == 0 else ImageOps.flip(p), (x, y)); col += 1
        row += 1
    return out


def backdrop(horizon, ground_top, ground):
    im = Image.new('RGBA', (W, H_TALL), SKY)
    f = P['forest']; band = Image.new('RGBA', (800, f.height)); band.paste(f, (0, 0)); band.paste(ImageOps.mirror(f), (400, 0)); c = P['clouds']
    paste(im, c, 150, horizon - f.height - c.height + 6); paste(im, ImageOps.mirror(c), -160, horizon - f.height - 2 * c.height - 30)
    paste(im, c, 250, horizon - f.height - 3 * c.height - 90)
    paste(im, band.crop((130, 0, 130 + W, f.height)), 0, horizon - f.height)
    g = Image.new('RGBA', (768, 256)); [g.paste(P['field'], (x, 0)) for x in (0, 256, 512)]
    paste(im, g.crop((0, 0, W, max(1, ground_top - horizon))), 0, horizon)
    paste(im, tile(ground, W, H_TALL - ground_top), 0, ground_top)
    return im


def topbar(im, t, title, gold, sub=None):
    cap = P['ui_capsule']; coin = P['icon_coin']
    paste(im, cap, 8, 8); paste(im, coin, 20, 8 + (cap.height - coin.height) // 2); t.append((gold, 8 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    rb = P['ui_ribbon']; paste(im, rb, (W - rb.width) // 2, 10); t.append((title, W // 2, 10 + 10, 24, CREAM, 'mm'))
    paste(im, nine(P['ui_btn_wide'], 76, 32, 12), W - 8 - 76, 6); t.append(('닫기', W - 8 - 38, 6 + 15, 22, INK, 'mm'))
    if sub: tag(im, t, 8 + tw(sub, 20) // 2 + 11, 44 + 15, sub, 20)


def icon_btn(im, t, x, y, icon, caption):
    paste(im, P['ui_btn'], x, y); paste(im, icon, x + (48 - icon.width) // 2, y + (40 - icon.height) // 2)
    label(t, x + 24, y + 54, caption, 18)


# ---------- 가판대 ----------
D = H_TALL - 960                                         # 960 화면 기준 좌표에 더해 1200 세계 좌표로 옮긴다
SHOP_X, SHOP_Y = 16, 130 + D


def sp(x, y): return SHOP_X - 2 + x, SHOP_Y + y          # 가게 원본(512×448) 안 좌표 → 세계 좌표


def stall_world():
    im = backdrop(460 + D, 530 + D, P["ground_sand"]); t = []
    paste(im, P['shop_wide'], SHOP_X, SHOP_Y)
    # 진열장 : 접시 여섯에 떡을 얹는다 (한 접시 = 묶음 5개)
    ck = art('Cake/redbean_cake')
    for i, (px, py) in enumerate([(92, 262), (141, 262), (191, 262), (92, 302), (141, 302), (191, 302)]):
        x, y = sp(px, py); paste(im, ck, x - 16, y - 24); label(t, x + 15, y + 6, '×5', 16)
    x, y = sp(141, 345); tag(im, t, x, y, '진열 30/30', 18)
    # 칠판 : 오늘의 떡
    x, y = sp(294, 250); label(t, x, y, '오늘의 떡', 18, CREAM); paste(im, ck, x - 16, y + 6); label(t, x, y + 54, '팥떡', 20, CREAM)
    # 문패 : 열림 / 닫힘
    x, y = sp(412, 293); label(t, x, y, '열림', 18, CREAM)
    # 계산대 앞 : 찾은 레시피 팻말 (누르면 바로 만든다)
    x, y = sp(138, 382); tag(im, t, x, y, '팥떡 만들기 15G', 18, icon=trim(ck))
    # 주인 (문 앞)
    ow = P['owner']; ox, oy = sp(418, 0)[0], 586 + D; paste_base(im, ow, ox, oy)
    label(t, ox + 2, oy + 12, '주인 10차 A (자리표시)' if not OWNER_IS_SMALL else '주인 v11 (손님 크기 판)', 16)
    if not OWNER_IS_SMALL: label(t, ox + 2, oy + 28, '손님 크기 판으로 바뀔 자리', 16)
    # 손님 줄 + 주문 말풍선
    bub = P['ui_bubble']; feet = 712 + D
    line = [('02_brown', '01_blue', '01_navy', 1, 62, 'redbean_cake'), ('04_orange', '02_yellow', '02_beige', 2, 132, 'redbean_cake'), ('05_black', '03_green', '01_gray', 3, 202, 'bean_cake'),
            ('01_gray', '01_red', '02_navy', 1, 272, 'redbean_cake'), ('06_brown', '02_green', '01_beige', 2, 342, None)]
    for (hair, shirt, bottom, face, cx, cake) in line:
        paste(im, customer(hair, shirt, bottom, face), cx - 32, feet - 76); by = feet - 74 - bub.height; paste(im, bub, cx - bub.width // 2, by)
        if cake: c2 = trim(art('Cake/' + cake)); paste(im, c2, cx - c2.width // 2, by + (bub.height - 8 - c2.height) // 2)
        else: t.append(('아무거나', cx, by + (bub.height - 8) // 2, 16, INK, 'mm'))
    # 재료 바구니 여섯 (누르면 실험 재료로 담는다) + 떡메 · 절구 (만들기)
    bk = P['basket']; base = 836 + D
    label(t, 14, base - 62, '실험 : 바구니에서 재료 1~3개를 고른다', 18, CREAM, 'lm')
    for i, (ing, n, picked) in enumerate([('redbean', '24', True), ('bean', '0', False), ('mugwort', '0', False), ('sesame', '0', False), ('pinenut', '0', False), ('jujube', '0', False)]):
        cx = 44 + i * 62; b = base - (8 if picked else 0); paste_base(im, bk, cx, b); ic = art('Ingredient/' + ing); paste(im, ic, cx - 14, b - bk.height - 6)
        label(t, cx, base + 12, n + (' 담음' if picked else ''), 18, (255, 236, 150) if picked else CREAM)
    mo = P['mortar_big']; mx = 462; paste_base(im, mo, mx, base + 6)
    ic = art('Ingredient/redbean'); paste(im, ic, mx - 14, base + 6 - mo.height - 30)
    tag(im, t, mx, base + 22, '만들기', 20)
    return im, t


def stall_ui(im, H):
    t = []; topbar(im, t, '가판대', '798', '오늘 손님 3/10')
    icon_btn(im, t, W - 8 - 48, H - 76, P['icon_book'], '레시피 도감'); icon_btn(im, t, W - 8 - 48 - 78, H - 76, P['icon_apron'], '점원 200G')
    return t


# ---------- 합성소 ----------
ROWS = (470 + D, 794 + D); COLS = (138, 402)


def slot_xy(i): return COLS[i % 2], ROWS[i // 2]


def put_mozzi(im, mx, my, side, color, face, crown):
    """찹쌀이를 입구 안에 넣는다 : 찹쌀이를 놓고 그 위에 입구 앞 테두리 조각을 덮는다."""
    xc = FUN[side]; m = mozzi(color, face, crown); bb = P['body'].getbbox()
    paste(im, m, mx + xc - (bb[0] + bb[2]) // 2, my + 52 - bb[3])
    fr, fx, fy = funnel_front(xc); paste(im, fr, mx + fx, my + fy)


def plus(im, t, mx, my, side):
    bb = P['ui_bubble']; xc = FUN[side]; paste(im, bb, mx + xc - bb.width // 2, my + 30 - bb.height); t.append(('+', mx + xc, my + 30 - bb.height // 2 - 5, 48, INK, 'mm'))


def machine(im, t, i, state):
    cx, base = slot_xy(i); mx = cx - mc.width // 2; my = base - mc.height
    label(t, mx + 14, my + 96, str(i + 1), 26)
    if state == 'buy':
        pd = P['pedestal']; paste_base(im, pd, cx, base - 6); tag(im, t, cx, base - 6 - pd.height - 18, '칸 사기 300G', 20); label(t, cx, base + 10, '눌러서 기계를 들인다', 16); return
    if state == 'locked':
        paste(im, P['machine_covered'], mx, my); label(t, cx, base + 10, '잠김 (앞 칸을 먼저 산다)', 16); return
    paste(im, mc, mx, my)
    if state == 'empty':
        plus(im, t, mx, my, 'L'); plus(im, t, mx, my, 'R')
    elif state == 'one':
        put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); plus(im, t, mx, my, 'R')
    elif state == 'ready':
        put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); put_mozzi(im, mx, my, 'R', '#4FA84A', 2, True)
        tag(im, t, mx + 196, my + 180, '시작 20G', 20); tag(im, t, mx + 34, my + 128, '2:30', 18)
        tag(im, t, cx, base + 12, '풀 75% · 다른 계열 20% · 변종 5%', 16)
    elif state == 'run':
        yy = art('Fx/fx_yinyang_half'); a = tint(yy, '#FFFFFF'); b = tint(yy.rotate(180), '#3F9BE0'); paste(im, a, mx + 109 - 24, my + 127 - 24); paste(im, b, mx + 109 - 24, my + 127 - 24)
        tag(im, t, mx + 34, my + 128, '1:42', 18, GREEN)
    elif state == 'done':
        sh = tint(P['body'], '#5A4A44'); paste(im, sh, mx + 107 - 32, my + 222 - 56); label(t, mx + 107, my + 198, '?', 30)
        sk = art('Fx/fx_sparkle'); paste(im, sk, mx + 82, my + 172); paste(im, sk, mx + 128, my + 180)
        tag(im, t, mx + 107, base + 12, '다 됨! 받기', 20, GREEN)


def synth_world(states):
    im = backdrop(232 + D, 232 + D, P['ground_stone']); t = []
    paste_base(im, P['props_sacks'], 70, 930 + D); paste_base(im, P['props_mortar'], 506, 842 + D)
    for i in (0, 1): machine(im, t, i, states[i])
    for i in (2, 3): machine(im, t, i, states[i])
    nb = P['noticeboard']; paste_base(im, nb, 190, 948 + D)                # 알림판 : 영구 단축
    t.append(('단축 0/5', 190, 948 + D - 52, 14, INK, 'mm')); t.append(('대기 100%', 190, 948 + D - 36, 14, INK, 'mm'))
    tag(im, t, 330, 948 + D - 58, '대기 100% → 90%', 18); tag(im, t, 330, 948 + D - 26, '단축 사기 200G', 20)
    return im, t


def synth_ui(im, H):
    t = []; topbar(im, t, '합성소', '898'); return t


def render(kind, H, states=None):
    world, labels = stall_world() if kind == 'stall' else synth_world(states)
    off = H_TALL - H; im = world.crop((0, off, W, H_TALL)); texts = stall_ui(im, H) if kind == 'stall' else synth_ui(im, H)
    for l in labels: texts.append((l[0], l[1], l[2] - off) + tuple(l[3:]))
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for tx in texts:
        s, x, y, size, col, anchor = tx[:6]; stroke = tx[6] if len(tx) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big


def board(title, cols, notes):
    """cols : (머리글, 그림) 목록. 폭은 그림마다 다를 수 있다."""
    Mg = 40; top = 150; hmax = max(c[1].height for c in cols)
    cw = Mg + sum(c[1].width + Mg for c in cols); c = Image.new('RGBA', (cw, top + hmax + 40 + 38 * len(notes) + 20), BG); d = ImageDraw.Draw(c)
    d.text((Mg, 24), title, font=font(40), fill=(255, 240, 220)); x = Mg
    for (head, im) in cols:
        d.text((x, 100), head, font=font(24), fill=(230, 200, 170)); c.paste(im.convert('RGB'), (x, top)); x += im.width + Mg
    y = top + hmax + 30
    for line in notes: d.text((Mg, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


def small(im): return im.resize((im.width // 2, im.height // 2), Image.NEAREST)


if __name__ == '__main__':
    board('가판대 화면 시안 v2 : 가게가 화면 폭을 채운 전용 화면 · 기능을 화면 속 물건으로', [
        ('v1 (견줄 것 · 9:16 · ×1)', small(v1.render('stall', 960))), ('v2 9:16 · 540×960 (×2)', render('stall', 960)), ('v2 9:20 · 540×1200 (×2) · 남는 세로는 하늘', render('stall', 1200))], [
        '가게 : 508×428 한 장 (create_image_pro 512×448). 화면 폭 540 을 거의 다 채운다. 가게 · 손님 · 찹쌀이 모두 ×1 이라 도트 굵기가 같다.',
        '아래 종이 패널을 없앴다. 진열 떡 = 진열장 접시 · 오늘의 떡 = 벽 칠판 · 열림/닫힘 = 문패 · 찾은 레시피 만들기 = 계산대 앞 팻말',
        '· 실험 재료 = 가게 앞 바구니 여섯 · 만들기 = 떡메와 절구 · 점원 / 레시피 도감 = 오른쪽 아래 아이콘 버튼 · 손님 주문 = 말풍선.',
        ('주인은 손님 크기 판 v11(64×80 · 다른 갈래가 만든 시안)을 세웠다.' if OWNER_IS_SMALL else '주인은 10차 A(128×160)를 줄이지 않고 세운 자리표시다. 손님 크기 판이 나오면 그것으로 바뀐다.'),
        '안 그린 것 : 떡메 치기 연출 · 밤(「다음날로」) · 닫힘 상태 · 레시피가 여럿일 때 팻말 줄. 숫자는 자리표시다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v2.png'))
    one = ['one', 'buy', 'locked', 'locked']; four = ['ready', 'run', 'done', 'empty']
    board('합성소 화면 시안 v2 : 칸 수만큼 기계가 선다 · 기계를 직접 누른다', [
        ('v1 (견줄 것 · 9:16 · ×1)', small(v1.render('synth', 960, 'one'))), ('v2 9:16 · 기계 1대일 때', render('synth', 960, one)), ('v2 9:16 · 기계 4대일 때', render('synth', 960, four)),
        ('v2 9:20 · 기계 4대 · 남는 세로는 하늘', render('synth', 1200, four))], [
        '자리 넷은 늘 2×2 로 고정이다. 산 칸에는 기계, 다음에 살 칸에는 「칸 사기」 팻말이 선 빈 받침, 아직 못 사는 칸에는 천 덮개 + 자물쇠.',
        '기계 230×227 은 v1 그림 그대로(×1)다. 입구 하나가 약 70×30px 이라 손가락으로 눌린다 (기준 48px 이상).',
        '4대 그림의 상태 : 1 = 둘 다 넣음(시작 20G · 확률 팻말) · 2 = 도는 중(창에 음양 · 남은 시간) · 3 = 다 됨(받침에 「?」 · 받기) · 4 = 비었음(「+」 둘).',
        '찹쌀이는 입구 「안」에 들어가 보인다 : 찹쌀이를 놓고 입구 앞 테두리 조각을 위에 덮었다. 아래 종이 패널은 없고 단축은 알림판 한 줄이다.',
        '안 그린 것 : 결과 덮개(NEW!) · 고르는 목록(보관함 · 농장 마릿수) · 합쳐지는 연출. 숫자 · 확률은 자리표시다.']).save(os.path.join(OUT, '2026-10-03-합성소화면-v2.png'))
    print({k: v.size for k, v in list(S.items()) + list(M.items())}, 'owner small' if OWNER_IS_SMALL else 'owner v10')

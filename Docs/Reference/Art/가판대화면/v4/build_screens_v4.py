# 가판대 화면 v4 · 주방 화면 v1 · 합성소 화면 v4 시안 합성 스크립트 — 2026-10-03 반복 03
# v3 에서 바꾼 것 (사용자 확정) :
#   가판대 : 주인을 계산대 안쪽(카메라 쪽)에 세운다 · 작업대(재료 칸 · 절구)를 빼고 「주방」 아이콘만 둔다
#            · 줄은 큰 손님으로 겹치지 않게 한 줄 · 즉시 만들기는 벽 차림표를 눌러 뜨는 목록에서 한다
#   주방   : 새 레시피를 찾는 별도 화면. 재료 여섯 · 큰 절구 · 떡메
#   합성소 : 기계를 메인 화면의 기계(후보 2)와 같은 생김새로 바꾸고 틈을 넓혔다
# 화면은 270×480(9:16) · 270×600(9:20) 으로 짜서 ×2 한다. PIL 은 자르기·배치·정수 배 확대·글자·단색 채우기만 한다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v4/build_screens_v4.py
import os, math, importlib.util
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v3s', os.path.join(HERE, '..', 'v3', 'build_screens_v3.py'))
v3 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v3)
v2 = v3.v2; v1 = v2.v1 if hasattr(v2, 'v1') else None
REF, OUT, W, INK, CREAM, YELLOW, GREEN = v3.REF, v3.OUT, v3.W, v3.INK, v3.CREAM, v3.YELLOW, v3.GREEN
op, trim, font, paste, paste_base, art, customer, tag, tint, mozzi = v3.op, v3.trim, v3.font, v3.paste, v3.paste_base, v3.art, v3.customer, v3.tag, v3.tint, v3.mozzi
Scene, finish, BGW, BGH, P = v3.Scene, v3.finish, v3.BGW, v3.BGH, dict(v3.P)
nine = v2.nine
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts')
KIT = os.path.join(REF, '주방화면', 'v1'); MILL = os.path.join(REF, '합성소화면', 'v4')
for d in (PARTS, os.path.join(KIT, 'parts'), os.path.join(MILL, 'parts')): os.makedirs(d, exist_ok=True)

N = {}
N['customer_big_b'] = trim(op(os.path.join(RAW, 'customer_big_15201.png')))           # 71×151 검은 머리 · 초록 겉옷
N['customer_big_c'] = trim(op(os.path.join(RAW, 'customer_big_15202.png')))           # 86×146 주황 머리 · 노란 옷
N['icon_mortar'] = trim(op(os.path.join(RAW, 'icon_mortar_15401.png')))               # 20×25
N['interior_hall'] = op(os.path.join(RAW, 'interior_hall_15101.png'))                 # 안 고른 안(카메라를 손님 쪽으로)의 배경
for k, v in N.items(): v.save(os.path.join(PARTS, k + '.png'))
K = {}
kraw = op(os.path.join(KIT, 'raw', 'interior_kitchen_16101.png')); ka = np.array(kraw)
FLOOR = tuple(ka[300, 10]); CEIL = tuple(ka[36, 100])
white = ka[:, :, :3].min(axis=2) > 245; ka[white & (np.arange(480)[:, None] > 200)] = FLOOR   # 가장자리에 온 흰 띠를 마루 색 한 가지로 메운다
kit = Image.new('RGBA', (BGW, 480), CEIL); kit.paste(Image.fromarray(ka).crop((0, 31, 272, 451)), (0, 60))   # 위 60 줄은 천장 띠 색으로 채움 (윗줄 UI 자리)
K['interior_kitchen'] = kit
for k, v in K.items(): v.save(os.path.join(KIT, 'parts', k + '.png'))
M = {}
M['machine2'] = op(os.path.join(MILL, 'raw', 'machine_small2_15301.png'))             # 144×144 캔버스 (그림 95×103, 바닥 y 125)
cov = os.path.join(MILL, 'raw', 'machine_small2_covered_15302.png')
M['machine2_covered'] = op(cov) if os.path.exists(cov) else v3.M['machine_covered']
for k, v in M.items(): v.save(os.path.join(MILL, 'parts', k + '.png'))
P.update(N); P.update(K); P.update(M)

CAKES = [('plain_cake', '찹쌀떡', 1), ('redbean_cake', '팥떡', 1), ('bean_cake', '콩떡', 1), ('mugwort_cake', '쑥떡', 1), ('sesame_cake', '깨떡', 0), ('pinenut_cake', '잣떡', 0),
         ('jujube_cake', '대추떡', 0), ('redbean_bean_cake', '팥콩떡', 1), ('mugwort_sesame_cake', '쑥깨떡', 0), ('pinenut_jujube_cake', '잣대추떡', 0), ('redbean_mugwort_sesame_cake', '팥쑥깨떡', 0)]
INGS = [('redbean', '팥', 24), ('bean', '콩', 12), ('mugwort', '쑥', 8), ('sesame', '깨', 3), ('pinenut', '잣', 0), ('jujube', '대추', 0)]


def cake(k): return trim(art('Cake/' + k))


# ---------- 가판대 v4 ----------
COUNTER_Y, FEET = v3.COUNTER_Y, v3.FEET


def stall_world():
    bg = P['interior_shop']; sc = Scene(bg); im = sc.im
    # 줄 : 큰 손님이 문 쪽(오른쪽)에서 계산대 가운데로 한 줄. 겹치지 않는다. 세 번째부터는 문밖이라 안 보인다
    paste_base(im, P['customer_big_b'], 236, FEET)
    cb = P['customer_big']; cx = 160; paste_base(im, cb, cx, FEET); head = FEET - cb.height
    bub = P['ui_bubble']; ck = cake('redbean_cake'); paste(im, bub, cx - bub.width // 2, head - bub.height - 2); paste(im, ck, cx - ck.width // 2, head - bub.height - 2 + (bub.height - 6 - ck.height) // 2)
    im.paste(bg.crop((0, COUNTER_Y, BGW, BGH)), (0, COUNTER_Y))                                    # 계산대를 다시 덮는다
    # 진열장 : 찾은 떡이 접시에 놓인다. 수만 보여 준다 (누르는 기능 없음)
    dc = P['display_case']; dx, dy = 4, 500 - dc.height; paste(im, dc, dx, dy)
    for (px, py), (k, n) in zip([(25, 27), (57, 27), (89, 27), (25, 52), (57, 52), (89, 52)], [('plain_cake', 8), ('redbean_cake', 5), ('bean_cake', 6), ('mugwort_cake', 4), ('redbean_bean_cake', 7), (None, 0)]):
        if k: c = cake(k); paste(im, c, dx + px - c.width // 2, dy + py + 4 - c.height); sc.text(dx + px + 11, dy + py + 5, '×%d' % n, 14)
    sc.tag(dx + dc.width // 2, dy + 5, '진열 30/30', 18)
    # 벽 칠판 = 차림표. 누르면 찾은 떡 목록이 뜬다. 오늘의 떡도 여기 적힌다
    sc.text(154, 236, '차림표', 22)
    for i, k in enumerate(['plain_cake', 'redbean_cake', 'bean_cake']): c = cake(k); paste(im, c, 124 + i * 30 - c.width // 2, 262 - c.height // 2)
    sc.text(154, 285, '오늘의 떡 : 팥떡', 16, YELLOW)
    sc.tag(154, 306, '눌러서 바로 만들기', 16)
    sc.text(252, 196, '열림', 18)
    sc.tag(236, 330, '문으로 드나든다', 16)
    # 주인 : 계산대 안쪽(카메라 쪽). 계산대보다 앞이라 계산대 위에 그린다
    paste_base(im, P['owner_big'], 216, 606)
    return sc


def stall_ui(im, t, H):
    v2.topbar(im, t, '가판대', '798', '오늘 손님 3/10')
    y = H - 84
    v2.icon_btn(im, t, 16, y, P['icon_mortar'], '주방'); v2.icon_btn(im, t, 96, y, P['icon_book'], '레시피 도감'); v2.icon_btn(im, t, 186, y, P['icon_apron'], '점원 200G')


def stall_menu_ui(im, t, H):
    """차림표를 누른 상태 : 밝은 종이 목록. 찾은 떡은 그림 · 이름 · 값 · 「만들기」, 못 찾은 떡은 「?」 줄. 11종을 2열로."""
    t.clear(); stall_ui(im, t, H)                                   # 목록 뒤에 깔린 화면 속 글자는 지운다
    pw, ph = 516, 470; px, py = (W - pw) // 2, 150; paste(im, nine(P['ui_panel_light'], pw, ph, 10), px, py)
    rb = P['ui_ribbon']; paste(im, rb, (W - rb.width) // 2, py - 10); t.append(('차림표', W // 2, py, 24, CREAM, 'mm'))
    for i, (k, name, found) in enumerate(CAKES):
        x = px + 14 + (i % 2) * 248; y = py + 26 + (i // 2) * 72
        if found:
            c = art('Cake/' + k); paste(im, c, x, y + 12); t.append((name, x + 40, y + 20, 22, INK, 'lm')); t.append(('15G', x + 40, y + 46, 20, (150, 96, 40), 'lm'))
            paste(im, nine(P['ui_btn_wide'], 72, 30, 12), x + 160, y + 18); t.append(('만들기', x + 196, y + 32, 20, INK, 'mm'))
        else:
            t.append(('?', x + 16, y + 30, 34, (150, 120, 100), 'mm')); t.append(('아직 못 찾은 떡', x + 40, y + 30, 20, (150, 120, 100), 'lm'))
    paste(im, nine(P['ui_btn_wide'], 76, 32, 12), px + pw - 90, py + ph - 44); t.append(('닫기', px + pw - 52, py + ph - 29, 22, INK, 'mm'))


def stall_alt_world():
    """안 고른 안 : 카메라를 손님 쪽(가게 홀)으로 돌린 구도. 주인이 계산대 뒤에서 정면을 본다."""
    bg = P['interior_hall']; im = Image.new('RGBA', (BGW, BGH), (0, 0, 0, 255)); im.paste(bg.crop((0, 0, BGW, 580)), (0, 28)); base = im.copy()
    paste_base(im, P['owner_big'], 186, 420); im.paste(base.crop((0, 322 + 28, BGW, BGH)), (0, 322 + 28))
    paste_base(im, P['customer_big'], 96, 572 + 28)
    sc = Scene(im); return sc


# ---------- 주방 v1 ----------
def kitchen_world():
    sc = Scene(Image.new('RGBA', (BGW, BGH), (0, 0, 0, 255))); im = sc.im; OFF = BGH - 480; im.paste(P['interior_kitchen'], (0, OFF))
    Y = lambda y: y + OFF
    tray = op(os.path.join(v3.RAW_S, 'tray_14202.png')).crop((29, 45, 227, 97)); tx, ty = 68, Y(214); paste(im, tray, tx, ty)   # 재료 칸 여섯 한 줄 (198×52)
    for x, (k, name, n) in zip((21, 52, 83, 114, 145, 175), INGS):
        ic = art('Ingredient/' + k); paste(im, ic, tx + x - 14, ty + 19 - 16) if n else paste(im, tint(ic, '#8A7A70'), tx + x - 14, ty + 19 - 16)
        sc.text(tx + x, ty + 60, '%s %d' % (name, n), 16, YELLOW if n else CREAM)
    sc.tag(tx + 99, ty - 8, '재료를 1~3개 골라 절구에 넣는다', 16)
    mo = P['mortar']; paste_base(im, mo, 112, Y(452)); paste(im, art('Ingredient/redbean'), 92, Y(350)); paste(im, art('Ingredient/bean'), 112, Y(346))
    sc.tag(112, Y(462), '떡메 치기', 20)
    c = cake('redbean_bean_cake'); sk = art('Fx/fx_sparkle'); paste(im, c, 218 - c.width // 2, Y(400)); paste(im, sk, 196, Y(388)); paste(im, sk, 228, Y(410))
    sc.tag(218, Y(382), 'NEW!', 22, GREEN); sc.text(218, Y(432), '팥콩떡', 20); sc.text(218, Y(446), '새 레시피를 찾았다', 14)
    sc.tag(44, Y(196), '가게로', 20)
    return sc


def kitchen_ui(im, t, H): v2.topbar(im, t, '주방', '798')


# ---------- 합성소 v4 ----------
COLS = (72, 198); ROWS = (400, 552); FUN = {'L': 45, 'R': 91}; MB = 125      # 자리 2×2 · 깔때기 입구 가운데 x · 기계 바닥 y (캔버스 안)


def funnel_front(xc):
    x0, y0, x1, y1 = xc - 22, 22, xc + 22, 54; c = np.array(P['machine2'].crop((x0, y0, x1, y1)))
    for x in range(c.shape[1]):
        dx = (x0 + x - xc + .5) / 18.0; edge = 30 + 3 * math.sqrt(1 - dx * dx) if abs(dx) < 1 else 0
        for y in range(c.shape[0]):
            if y0 + y < edge: c[y, x, 3] = 0
    return Image.fromarray(c), x0, y0


for s_, xc_ in FUN.items(): funnel_front(xc_)[0].save(os.path.join(MILL, 'parts', 'funnel2_front_%s.png' % s_))


def put_mozzi(im, mx, my, side, color, face, crown):
    xc = FUN[side]; m = mozzi(color, face, crown); bb = P['body'].getbbox()
    paste(im, m, mx + xc - (bb[0] + bb[2]) // 2, my + 44 - bb[3]); fr, fx, fy = funnel_front(xc); paste(im, fr, mx + fx, my + fy)


def plus(sc, mx, my, side):
    bb = P['ui_bubble_small']; xc = FUN[side]; paste(sc.im, bb, mx + xc - bb.width // 2, my + 22 - bb.height); sc.text(mx + xc, my + 22 - bb.height + 8, '+', 30, INK, False)


def machine(sc, i, state):
    im = sc.im; cx, base = COLS[i % 2], ROWS[i // 2]; mx, my = cx - 72, base - MB
    if state == 'buy':
        ss = P['stand_sign']; paste(im, ss, cx - 64, base - 133); sc.text(cx + 1, base - 133 + 40, '칸 사기', 32); sc.text(cx + 1, base - 133 + 59, '300G', 34, YELLOW); return
    if state == 'locked': paste(im, P['machine2_covered'], mx, my); return
    paste(im, P['machine2'], mx, my)
    if state == 'empty': plus(sc, mx, my, 'L'); plus(sc, mx, my, 'R')
    elif state == 'one': put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); plus(sc, mx, my, 'R')
    elif state == 'ready':
        put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); put_mozzi(im, mx, my, 'R', '#4FA84A', 2, True); sc.tag(cx, base + 10, '시작 20G · 풀 75%', 18)
    elif state == 'run':
        yy = art('Fx/fx_yinyang_half'); a = tint(yy, '#FFFFFF'); a.alpha_composite(tint(yy.rotate(180), '#3F9BE0'))
        r = 15; a = a.crop((24 - r, 24 - r, 24 + r, 24 + r)); mk = np.array(a)
        for y in range(2 * r):
            for x in range(2 * r):
                if (x - r + .5) ** 2 + (y - r + .5) ** 2 > r * r: mk[y, x, 3] = 0
        paste(im, Image.fromarray(mk), mx + 72 - r, my + 78 - r); sc.tag(cx, base + 10, '1:42', 22, GREEN)
    elif state == 'done':
        c = cake('plain_cake'); sk = art('Fx/fx_sparkle'); paste(im, sk, mx + 44, my + 96); paste(im, sk, mx + 84, my + 100)
        sc.text(cx, my + 112, '?', 30); sc.tag(cx, base + 10, '다 됨! 받기', 20, GREEN)


def synth_world(states):
    sc = Scene(P['interior_mill'])
    for i in range(4): machine(sc, i, states[i])
    return sc


def synth_ui(im, t, H):
    v2.topbar(im, t, '합성소', '898')
    tag(im, t, 150, H - 20, '단축 0/5 · 대기 100% → 90%', 18); tag(im, t, 430, H - 20, '단축 사기 200G', 20)


if __name__ == '__main__':
    P['body'] = v2.P['body']; board, small = v2.board, v2.small
    four = ['ready', 'run', 'done', 'empty']; one = ['one', 'buy', 'locked', 'locked']
    board('가판대 화면 시안 v4 : 주인은 계산대 안쪽 · 줄은 큰 손님 한 줄 · 만들기는 차림표와 주방으로', [
        ('v3 (견줄 것)', small(finish(v3.stall_world(), 960, v3.stall_ui))), ('v4 9:16', finish(stall_world(), 960, stall_ui)), ('v4 9:20 · 남는 세로는 천장', finish(stall_world(), 1200, stall_ui)),
        ('곁그림 : 벽 차림표를 누른 상태 (확정 · UI 느낌은 감수)', finish(stall_world(), 960, stall_menu_ui)),
        ('안 고른 안 : 카메라를 손님 쪽으로 돌림', small(finish(stall_alt_world(), 960, lambda im, t, H: None)))], [
        '주인(10차 A · 큰 그림)은 계산대 안쪽, 카메라 쪽에 선다. 뒷모습은 안 만든다 (사용자 확정). 계산대보다 앞이라 계산대 위에 겹친다.',
        '줄은 큰 손님이 문 쪽에서 계산대 쪽으로 겹치지 않고 선다. 한 화면에 둘까지 보이고 나머지는 문밖이다. 문으로 들어오고 나가는 움직임을 넣는다.',
        '떡 만들기 : (1) 찾은 떡을 바로 만들기 = 벽 차림표(칠판)를 누르면 목록이 뜬다 (2) 새 레시피 찾기 = 왼쪽 아래 「주방」 아이콘으로 주방 화면에 간다.',
        '차림표와 「오늘의 떡」 은 칠판 한 장에 합쳤다. 벽에 판이 둘이면 어느 것을 누르는지 헷갈리고, 오늘의 떡도 차림표의 한 줄이기 때문이다.',
        '진열장 접시는 진열 수만 보여 준다. 작업대(재료 칸 · 절구)는 주방 화면으로 옮겼다. 떡 이름 · 값 · 숫자는 자리표시다. 전부 시안이다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v4.png'))
    board('주방 화면 시안 v1 : 새 레시피를 찾는 곳 (지금의 「실험」 을 화면으로 옮김)', [('9:16 · 270×480 을 ×2', finish(kitchen_world(), 960, kitchen_ui))], [
        '가판대 화면의 「주방」 아이콘으로 들어온다. 왼쪽 문(분홍 발)을 누르면 가게로 돌아간다.',
        '작업대 위 재료 칸 여섯에서 1~3개를 골라 절구에 넣고 떡메로 친다.',
        '처음 만든 떡이면 「NEW!」 가 뜬다. 새 기능은 없다.',
        '재료가 0 인 칸은 어둡게 보인다. 숫자는 자리표시다.',
        '안 그린 것 : 떡메 치는 연출 · 실패했을 때 · 힌트(「? + ? + 팥」).']).save(os.path.join(OUT, '2026-10-03-주방화면-v1.png'))
    board('합성소 화면 시안 v4 : 기계를 메인 화면의 기계(후보 2)와 같은 생김새로', [
        ('v3 (견줄 것)', small(finish(v3.synth_world(four), 960, v3.synth_ui))), ('v4 9:16 · 기계 1대일 때', finish(synth_world(one), 960, synth_ui)),
        ('v4 9:16 · 기계 4대일 때', finish(synth_world(four), 960, synth_ui)), ('v4 9:20 · 기계 4대', finish(synth_world(four), 1200, synth_ui))], [
        '기계는 메인 화면에 선 기계(후보 2)를 화풍 그림으로 주고 작게 다시 뽑았다 (95×103). 같은 건물로 읽힌다.',
        'v3 흠 고친 것 : 기계 사이 틈이 넓어졌다 (좌우 약 60px · 위아래 약 90px) · 「+」 말풍선이 윗줄 기계에 안 붙는다.',
        '못 고친 것 : 찹쌀이 선(회색 110)이 옅다. 선 굵기는 확정 값이라 그대로 뒀다. 찹쌀이가 깔때기 입구보다 조금 넓다.',
        '「다 됨」 은 서랍 위에 「?」 와 반짝임만 띄웠다. 방 · 2×2 자리 · 간판 · 덮개 · 단축 줄은 v3 과 같다. 전부 시안이다.']).save(os.path.join(OUT, '2026-10-03-합성소화면-v4.png'))
    print('ok')

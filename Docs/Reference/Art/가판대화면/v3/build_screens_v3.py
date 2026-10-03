# 가판대 화면 · 합성소 화면 시안 v3 합성 스크립트 — 2026-10-03 반복 03
# v3 의 뜻 : 「실내」 전용 화면. 계산대 너머로 손님이 크게 서고(가판대), 방 안에 기계가 칸 수만큼 선다(합성소).
# 화면은 270×480(9:16) · 270×600(9:20) 으로 짜서 ×2 한다 — 배경 · 사람 · 소품의 도트 굵기를 하나로 맞추려는 것이다.
# PIL 은 자르기·배치·정수 배 확대·글자·색 곱하기 미리보기만 한다. 그림을 그리거나 다시 칠하지 않는다.
# 윗줄 · 나무 표(9조각) · 아이콘 버튼은 v2 스크립트의 것을 540 기준으로 얹는다 (UI 는 ×1 굵기다).
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v3/build_screens_v3.py
import os, math, importlib.util
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v2s', os.path.join(HERE, '..', 'v2', 'build_screens_v2.py'))
v2 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v2)
REF, OUT, W, INK, CREAM = v2.REF, v2.OUT, v2.W, v2.INK, v2.CREAM
op, trim, font, paste, paste_base, art, customer, tag, label, tint, mozzi = v2.op, v2.trim, v2.font, v2.paste, v2.paste_base, v2.art, v2.customer, v2.tag, v2.label, v2.tint, v2.mozzi
RAW_S = os.path.join(HERE, 'raw'); PARTS_S = os.path.join(HERE, 'parts')
RAW_M = os.path.join(REF, '합성소화면', 'v3', 'raw'); PARTS_M = os.path.join(REF, '합성소화면', 'v3', 'parts')
os.makedirs(PARTS_S, exist_ok=True); os.makedirs(PARTS_M, exist_ok=True)
OWNER = r'C:\Users\USER\AppData\Local\Temp\claude\mozzi-owner-02\v10\pick1.png'      # 주인 큰 그림 (10차 A · 128×160)
YELLOW = (255, 236, 150); GREEN = v2.GREEN
BGW, BGH = 272, 608                                                                    # 배경 원본 크기

S = {}; M = {}
S['interior_shop'] = op(os.path.join(RAW_S, 'interior_shop_12101.png'))
S['customer_big'] = trim(op(os.path.join(RAW_S, 'customer_big_14101.png')))            # 78×146
S['display_case'] = trim(op(os.path.join(RAW_S, 'display_case_14201.png')))            # 117×82
tray = op(os.path.join(RAW_S, 'tray_14202.png'))
S['tray_left'] = tray.crop((29, 45, 128, 97)); S['tray_right'] = tray.crop((128, 45, 227, 97))   # 여섯 칸 한 줄을 가운데 칸막이에서 잘라 두 줄로 쌓는다
S['mortar'] = trim(op(os.path.join(RAW_S, 'mortar_14203.png')))                        # 111×118
S['owner_big'] = trim(op(OWNER))
M['interior_mill'] = op(os.path.join(RAW_M, 'interior_mill_12201.png'))
M['machine'] = op(os.path.join(RAW_M, 'machine_small_13301.png'))                      # 128×144 캔버스 그대로 (그림 124×121)
M['machine_covered'] = op(os.path.join(RAW_M, 'machine_small_covered_13401.png'))
M['stand_sign'] = op(os.path.join(RAW_M, 'stand_sign_13402.png'))
FUN = {'L': 33, 'R': 94}; RIM_Y = 18                                                   # 깔때기 입구 가운데 x · 앞 테두리 높이 (기계 캔버스 안)


def funnel_front(xc):
    """입구 앞 테두리 조각 : 입구 타원의 아래 호보다 아래 픽셀만 남긴다 (자르기). 찹쌀이 위에 덮는다."""
    x0, y0, x1, y1 = xc - 31, 8, xc + 31, 50; c = np.array(M['machine'].crop((x0, y0, x1, y1)))
    for x in range(c.shape[1]):
        dx = (x0 + x - xc) / 27.0; edge = RIM_Y + 3 * math.sqrt(1 - dx * dx) if abs(dx) < 1 else 0
        for y in range(c.shape[0]):
            if y0 + y < edge: c[y, x, 3] = 0
    return Image.fromarray(c), x0, y0


for k, xc in FUN.items(): M['funnel_front_' + k] = funnel_front(xc)[0]
for k, v in S.items(): v.save(os.path.join(PARTS_S, k + '.png'))
for k, v in M.items(): v.save(os.path.join(PARTS_M, k + '.png'))
P = dict(v2.P); P.update(S); P.update(M)


class Scene:
    """배경 원본 좌표(272×608)로 그림을 놓고, 글자·나무 표는 540 기준 좌표로 바꿔 모아 둔다."""

    def __init__(self, bg): self.im = bg.copy(); self.ops = []          # ops : 540 기준 그림 위에서 할 일 (H 를 받아 실행)

    def B(self, x, y, H): return int((x - 1) * 2), int((y - (BGH - H // 2)) * 2)

    def text(self, x, y, s, size=20, col=CREAM, stroke=True):
        self.ops.append(lambda im, t, H: t.append((s,) + self.B(x, y, H) + (size, col, 'mm') + ((INK,) if stroke else ())))

    def tag(self, x, y, s, size=20, col=CREAM, icon=None):
        self.ops.append(lambda im, t, H: tag(im, t, *self.B(x, y, H), s, size, col, icon))


def finish(sc, H, ui):
    off = BGH - H // 2; im = sc.im.crop((1, off, 271, BGH)).resize((W, H), Image.NEAREST); t = []
    for f in sc.ops: f(im, t, H)
    ui(im, t, H)
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for tx in t:
        s, x, y, size, col, anchor = tx[:6]; stroke = tx[6] if len(tx) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big


# ---------- 가판대 (실내) ----------
COUNTER_Y = 457                                           # 계산대 윗면이 시작하는 줄. 이 아래는 사람 위에 다시 덮는다
FEET = 492                                                # 계산대 뒤에 선 사람의 발 높이 (계산대에 가려 안 보인다)


def stall_world(front='big'):
    bg = P['interior_shop']; sc = Scene(bg); im = sc.im; ck = art('Cake/redbean_cake'); ckt = trim(ck)
    # 줄 선 손님 : 뒤쪽 벽 앞에 작게 (멀리 있다)
    for (hair, shirt, bottom, face, cx) in [('04_orange', '02_yellow', '02_beige', 2, 24), ('05_black', '03_green', '01_gray', 3, 52), ('01_gray', '01_red', '02_navy', 1, 80)]:
        paste(im, customer(hair, shirt, bottom, face), cx - 32, 410 - 76)
    # 맨 앞 손님 : 계산대 바로 뒤
    cx = 146
    if front == 'big': paste_base(im, P['customer_big'], cx, FEET); head = FEET - P['customer_big'].height
    elif front == 'x2':
        c2 = customer('02_brown', '01_blue', '01_navy', 1); c2 = c2.resize((c2.width * 2, c2.height * 2), Image.NEAREST); paste(im, c2, cx - 64, FEET - 152); head = FEET - 150
    else: paste(im, customer('02_brown', '01_blue', '01_navy', 1), cx - 32, 456 - 76); head = 456 - 74
    bub = P['ui_bubble']; paste(im, bub, cx - bub.width // 2, head - bub.height - 2); paste(im, ckt, cx - ckt.width // 2, head - bub.height - 2 + (bub.height - 6 - ckt.height) // 2)
    # 주인 : 계산대 오른쪽 끝 뒤
    paste_base(im, P['owner_big'], 224, FEET)
    # 계산대를 다시 덮는다 (배경에서 그 줄 아래를 잘라 얹음)
    im.paste(bg.crop((0, COUNTER_Y, BGW, BGH)), (0, COUNTER_Y))
    # 진열장 : 계산대 왼쪽 위. 접시 여섯에 떡
    dc = P['display_case']; dx, dy = 4, 500 - dc.height; paste(im, dc, dx, dy)
    for (px, py) in [(25, 27), (57, 27), (89, 27), (25, 52), (57, 52), (89, 52)]:
        paste(im, ckt, dx + px - ckt.width // 2, dy + py + 4 - ckt.height); sc.text(dx + px + 11, dy + py + 5, '×5', 14)
    sc.tag(dx + dc.width // 2, dy + 5, '진열 30/30', 18)
    # 칠판 : 오늘의 떡 / 문패 : 열림
    sc.text(154, 239, '오늘의 떡', 20); paste(im, ckt, 154 - ckt.width // 2, 252); sc.text(154, 286, '팥떡', 22)
    sc.text(252, 196, '열림', 18)
    # 계산대 위 차림표 팻말 : 찾은 레시피로 바로 만들기
    sc.tag(196, 478, '팥떡 만들기 15G', 18, icon=ckt)
    # 작업대(계산대 안쪽) : 재료 칸 여섯 + 떡메와 절구
    for (part, ty, xs, ings) in [('tray_left', 502, (21, 52, 83), ('redbean', 'bean', 'mugwort')), ('tray_right', 554, (15, 46, 76), ('sesame', 'pinenut', 'jujube'))]:
        paste(im, P[part], 6, ty)
        for x, ing in zip(xs, ings):
            paste(im, art('Ingredient/' + ing), 6 + x - 14, ty + 19 - 16)
            sc.text(6 + x + 8, ty + 36, '24' if ing == 'redbean' else '0', 16, YELLOW if ing == 'redbean' else CREAM)
    sc.text(133, 550, '재료', 20); sc.text(133, 563, '1~3개', 20); sc.text(133, 576, '고르기', 20)
    mo = P['mortar']; paste_base(im, mo, 214, 606); paste(im, art('Ingredient/redbean'), 196, 512)
    sc.tag(214, 597, '만들기', 20)
    return sc


def stall_ui(im, t, H):
    v2.topbar(im, t, '가판대', '798', '오늘 손님 3/10')
    v2.icon_btn(im, t, 10, 74, P['icon_book'], '레시피 도감'); v2.icon_btn(im, t, 10, 148, P['icon_apron'], '점원 200G')


# ---------- 합성소 (실내) ----------
COLS = (70, 202); ROWS = (404, 562)                       # 기계 자리 2×2 (가운데 x · 바닥 y)


def put_mozzi(im, mx, my, side, color, face, crown):
    xc = FUN[side]; m = mozzi(color, face, crown); bb = P['body'].getbbox()
    paste(im, m, mx + xc - (bb[0] + bb[2]) // 2, my + 33 - bb[3])                     # 몸 아래가 입구 테두리보다 15px 아래로 들어간다
    fr, fx, fy = funnel_front(xc); paste(im, fr, mx + fx, my + fy)


def plus(sc, mx, my, side):
    bb = P['ui_bubble']; xc = FUN[side]; paste(sc.im, bb, mx + xc - bb.width // 2, my + 10 - bb.height); sc.text(mx + xc, my + 10 - bb.height + 11, '+', 44, INK, False)


def machine(sc, i, state):
    im = sc.im; cx, base = COLS[i % 2], ROWS[i // 2]; mx, my = cx - 64, base - 132
    if state == 'buy':
        paste(im, P['stand_sign'], mx, my - 1); sc.text(cx + 1, my + 39, '칸 사기', 32); sc.text(cx + 1, my + 58, '300G', 34, YELLOW); return
    if state == 'locked':
        paste(im, P['machine_covered'], mx, my); return
    paste(im, P['machine'], mx, my)
    if state == 'empty': plus(sc, mx, my, 'L'); plus(sc, mx, my, 'R')
    elif state == 'one': put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); plus(sc, mx, my, 'R')
    elif state == 'ready':
        put_mozzi(im, mx, my, 'L', '#FFFFFF', 1, False); put_mozzi(im, mx, my, 'R', '#4FA84A', 2, True)
        sc.tag(cx, base - 9, '시작 20G · 풀 75%', 18)
    elif state == 'run':
        yy = art('Fx/fx_yinyang_half'); a = tint(yy, '#FFFFFF'); a.alpha_composite(tint(yy.rotate(180), '#3F9BE0'))
        r = 17; a = a.crop((24 - r, 24 - r, 24 + r, 24 + r)); mk = np.array(a)                # 창 안쪽 원 크기로 자른다
        for y in range(2 * r):
            for x in range(2 * r):
                if (x - r + .5) ** 2 + (y - r + .5) ** 2 > r * r: mk[y, x, 3] = 0
        paste(im, Image.fromarray(mk), mx + 60 - r, my + 80 - r); sc.tag(cx, base - 9, '1:42', 22, GREEN)
    elif state == 'done':
        sh = tint(P['body'], '#5A4A44'); bb = P['body'].getbbox(); paste(im, sh, mx + 60 - (bb[0] + bb[2]) // 2, my + 128 - bb[3]); sc.text(cx - 4, my + 112, '?', 30)
        sk = art('Fx/fx_sparkle'); paste(im, sk, mx + 30, my + 92); paste(im, sk, mx + 84, my + 100)
        sc.tag(cx, base + 9, '다 됨! 받기', 20, GREEN)


def synth_world(states):
    sc = Scene(P['interior_mill'])
    for i in range(4): machine(sc, i, states[i])
    return sc


def synth_ui(im, t, H):
    v2.topbar(im, t, '합성소', '898')
    tag(im, t, 150, H - 22, '단축 0/5 · 대기 100% → 90%', 18); tag(im, t, 430, H - 22, '단축 사기 200G', 20)


def board(title, cols, notes): return v2.board(title, cols, notes)


if __name__ == '__main__':
    P['body'] = v2.P['body']
    four = ['ready', 'run', 'done', 'empty']; one = ['one', 'buy', 'locked', 'locked']
    board('가판대 화면 시안 v3 : 가게 안 · 계산대 너머로 손님이 크게 선다', [
        ('v2 (견줄 것 · 바깥 · ×1)', v2.small(v2.render('stall', 960))), ('v3 9:16 · 270×480 을 ×2 (그림은 ×4)', finish(stall_world(), 960, stall_ui)),
        ('v3 9:20 · 270×600 을 ×2 · 남는 세로는 천장', finish(stall_world(), 1200, stall_ui))], [
        '카메라는 계산대 안쪽(일하는 쪽)에서 손님을 마주 본다. 맨 앞 손님 한 명이 크게 서고(새로 뽑은 78×146) 줄 선 손님은 뒤쪽 벽 앞에 작게 선다.',
        '주인은 큰 그림 10차 A(128×160)를 그대로 썼다. 계산대 오른쪽 끝에 서서 손님을 맞는다. 배경 · 사람 · 소품 모두 도트 한 칸이 화면 2px 이다.',
        '기능 → 물건 : 진열 떡 = 계산대 위 진열장 · 오늘의 떡 = 벽 칠판 · 열림/닫힘 = 문패 · 주문 = 말풍선 · 찾은 레시피 = 계산대 위 차림표',
        '· 실험 재료 = 작업대 재료 칸 여섯 · 만들기 = 떡메와 절구 · 레시피 도감 / 점원 = 왼쪽 위 아이콘 버튼. 종이 패널은 없다.',
        '9:20 에서는 천장(들보 · 등)이 더 보인다. 안 그린 것 : 떡메 치기 연출 · 밤 · 닫힘 · 레시피 여럿 · 손님이 바뀌는 연출. 숫자는 자리표시다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v3.png'))
    board('가판대 v3 손님 크기 견주기 (9:16) : 어느 안으로 맨 앞 손님을 세우나', [
        ('가 : 지금 손님 그림 그대로 (64×80)', finish(stall_world('small'), 960, stall_ui)), ('나 : 지금 손님 그림을 ×2 (도트가 두 배로 굵다)', finish(stall_world('x2'), 960, stall_ui)),
        ('다 : 큰 손님을 새로 뽑음 (78×146) · 고름', finish(stall_world('big'), 960, stall_ui))], [
        '가 : 주인(큰 그림)의 절반 크기라 아이처럼 보이고, 뒤에 줄 선 손님과 크기가 같아 맨 앞이라는 것이 안 읽힌다.',
        '나 : 크기는 맞지만 도트 한 칸이 배경 · 주인의 두 배라 혼자 흐릿하고 거칠다.',
        '다 : 주인과 같은 굵기 · 같은 등신이라 한 화면에 어울린다. 대신 손님 그림(머리 · 옷 · 표정 겹)을 큰 크기로 다시 뽑아야 한다.',
        '줄 선 손님은 세 안 모두 지금 그림(64×80)을 뒤쪽에 작게 세웠다. 멀리 있어서 작은 것으로 읽힌다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v3-손님크기.png'))
    board('합성소 화면 시안 v3 : 방앗간 안 · 단순한 기계가 칸 수만큼 선다', [
        ('v2 (견줄 것 · 바깥 · ×1)', v2.small(v2.render('synth', 960, four))), ('v3 9:16 · 기계 1대일 때', finish(synth_world(one), 960, synth_ui)),
        ('v3 9:16 · 기계 4대일 때', finish(synth_world(four), 960, synth_ui)), ('v3 9:20 · 기계 4대 · 남는 세로는 벽과 천장', finish(synth_world(four), 1200, synth_ui))], [
        '자리 넷은 2×2 고정이다. 산 칸 = 기계 · 다음에 살 칸 = 「칸 사기」 간판이 선 빈 받침(기계와 같은 폭) · 못 사는 칸 = 천 덮개 + 자물쇠(김 없음).',
        '기계는 단순하게 새로 뽑았다 (124×121 · 깔때기 둘 · 둥근 몸통 · 창 하나 · 손잡이 하나). 화면에서는 ×2 라 입구 하나가 약 110px 폭이다.',
        '4대 그림 : 1 = 둘 다 넣음 · 2 = 도는 중(창에 음양 · 남은 시간) · 3 = 다 됨(받침에 「?」) · 4 = 비었음(「+」 둘). 기계마다 표는 하나만 달았다.',
        '찹쌀이는 입구 테두리보다 15px 아래까지 들어간다 (앞 테두리 조각을 위에 덮음). 단축은 맨 아래 나무 표 한 줄이다.',
        '안 그린 것 : 결과 덮개(NEW!) · 고르는 목록 · 합쳐지는 연출 · 2대 · 3대일 때. 숫자 · 확률은 자리표시다.']).save(os.path.join(OUT, '2026-10-03-합성소화면-v3.png'))
    print({k: v.size for k, v in list(S.items()) + list(M.items())})

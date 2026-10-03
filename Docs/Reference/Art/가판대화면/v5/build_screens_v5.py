# 가판대 화면 v5 「안쪽 버전」 시안 합성 스크립트 — 2026-10-03 반복 03
# v4 와 다른 것 : 카메라가 손님 쪽(가게 홀)에 있다. 주인이 계산대 뒤(안쪽)에 서서 이쪽을 보고, 손님은 계산대 앞 마루에 선다.
# 들어가는 기능은 v4 와 같다 (주문 말풍선 · 진열장 · 벽 차림표 · 목록 팝업 · 주방 / 도감 / 점원 아이콘 · 윗줄).
# 화면은 270×480(9:16) · 270×600(9:20) 으로 짜서 ×2 한다. PIL 은 자르기·배치·정수 배 확대·글자·단색 채우기·어두운 막만 한다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v5/build_screens_v5.py
import os, importlib.util
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v4s', os.path.join(HERE, '..', 'v4', 'build_screens_v4.py'))
v4 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v4)
v3, v2 = v4.v3, v4.v2
OUT, W, INK, CREAM, YELLOW = v4.OUT, v4.W, v4.INK, v4.CREAM, v4.YELLOW
op, font, paste, paste_base, cake, nine, P = v4.op, v4.font, v4.paste, v4.paste_base, v4.cake, v4.nine, v4.P
Scene, finish, BGW, BGH = v4.Scene, v4.finish, v4.BGW, v4.BGH
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)

# 배경 : create_image_pro 272×608 · 17001. 위아래에 흰 띠가 와서 내용(y 31~579)만 잘라 바닥에 붙이고, 위 60 줄은 천장 색 한 가지로 채운다
raw = op(os.path.join(RAW, 'interior_hall2_17001.png')); ra = np.array(raw)
rows = np.where((ra[:, :, :3].min(axis=2) < 250).any(axis=1))[0]; Y0, Y1 = int(rows[0]), int(rows[-1]) + 1
CEIL = tuple(int(v) for v in ra[Y0 + 2, 5]); OFF = BGH - Y1                    # 내용 좌표 → 세계 좌표는 y + OFF
hall = Image.new('RGBA', (BGW, BGH), CEIL); hall.paste(raw.crop((0, Y0, BGW, Y1)), (0, Y0 + OFF))
hall.save(os.path.join(PARTS, 'interior_hall2.png')); P['interior_hall2'] = hall

COUNTER_Y = 325 + OFF        # 계산대 윗면이 시작하는 줄. 이 아래(계산대 폭 안)는 주인 위에 다시 덮는다
COUNTER_X = 201              # 계산대 오른쪽 끝. 그 오른쪽은 문이다
FLOOR_FEET = 560             # 마루에 선 손님의 발 높이


def stall_world():
    bg = P['interior_hall2']; sc = Scene(bg); im = sc.im
    # 주인 : 계산대 뒤(안쪽)에서 이쪽을 본다. 허리 아래는 계산대에 가려진다
    paste_base(im, P['owner_big'], 154, COUNTER_Y + 64)
    im.paste(bg.crop((0, COUNTER_Y, COUNTER_X, BGH)), (0, COUNTER_Y))
    # 진열장 : 계산대 위 왼쪽. 수만 보여 준다
    dc = P['display_case']; dx, dy = 3, COUNTER_Y + 18 - dc.height; paste(im, dc, dx, dy)
    for (px, py), (k, n) in zip([(25, 27), (57, 27), (89, 27), (25, 52), (57, 52), (89, 52)], [('plain_cake', 8), ('redbean_cake', 5), ('bean_cake', 6), ('mugwort_cake', 4), ('redbean_bean_cake', 7), (None, 0)]):
        if k: c = cake(k); paste(im, c, dx + px - c.width // 2, dy + py + 4 - c.height); sc.text(dx + px + 11, dy + py + 5, '×%d' % n, 14)
    sc.tag(dx + dc.width // 2, dy + 5, '진열 30/30', 18)
    # 벽 칠판 = 차림표 (「오늘의 떡」 과 한 장). 누르면 목록이 뜬다
    bx = 120; by = 150 + OFF
    sc.text(bx, by + 18, '차림표', 22)
    for i, k in enumerate(['plain_cake', 'redbean_cake', 'bean_cake']): c = cake(k); paste(im, c, bx - 34 + i * 34 - c.width // 2, by + 44 - c.height // 2)
    sc.text(bx, by + 68, '오늘의 떡 : 팥떡', 16, YELLOW)
    sc.tag(bx - 34, by + 87, '눌러서 바로 만들기', 16)
    sc.text(247, 172 + OFF, '열림', 18)
    # 손님 : 계산대 앞 마루에 한 줄. 맨 앞이 계산대 가운데, 둘째는 문 쪽. 겹치지 않고 문도 안 가린다 (문은 머리 위에 있다)
    cb = P['customer_big']; cx = 100; paste_base(im, cb, cx, FLOOR_FEET); head = FLOOR_FEET - cb.height
    paste_base(im, P['customer_big_b'], 218, FLOOR_FEET)
    bub = P['ui_bubble']; ck = cake('redbean_cake'); paste(im, bub, cx - bub.width // 2, head - bub.height - 2); paste(im, ck, cx - ck.width // 2, head - bub.height - 2 + (bub.height - 6 - ck.height) // 2)
    sc.tag(238, 300 + OFF, '문으로 드나든다', 16)
    sc.tag(200, FLOOR_FEET + 16, '← 줄 (들어온 차례로)', 16)
    return sc


def stall_ui(im, t, H):
    v2.topbar(im, t, '가판대', '798', '오늘 손님 3/10')
    y = H - 84
    v2.icon_btn(im, t, 16, y, P['icon_mortar'], '주방'); v2.icon_btn(im, t, 96, y, P['icon_book'], '레시피 도감'); v2.icon_btn(im, t, 186, y, P['icon_apron'], '점원 200G')


def draw_texts(big, t):
    d = ImageDraw.Draw(big)
    for tx in t:
        s, x, y, size, col, anchor = tx[:6]; stroke = tx[6] if len(tx) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)


def stall_menu(H):
    """차림표를 누른 상태 : 뒤 화면 전체(글자 포함)에 어두운 막을 깔고 그 위에 종이 목록을 올린다."""
    big = finish(stall_world(), H, stall_ui).convert('RGBA')
    big = Image.alpha_composite(big, Image.new('RGBA', big.size, (30, 20, 16, 140)))                 # 어두운 막 (한 가지 색)
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0)); t = []
    pw, ph = 516, 470; px, py = (W - pw) // 2, 190; paste(layer, nine(P['ui_panel_light'], pw, ph, 10), px, py)
    rb = P['ui_ribbon']; paste(layer, rb, (W - rb.width) // 2, py - 10); t.append(('차림표', W // 2, py, 24, CREAM, 'mm'))
    for i, (k, name, found) in enumerate(v4.CAKES):
        x = px + 14 + (i % 2) * 248; y = py + 26 + (i // 2) * 72
        if found:
            c = v4.art('Cake/' + k); paste(layer, c, x, y + 12); t.append((name, x + 40, y + 20, 22, INK, 'lm')); t.append(('15G', x + 40, y + 46, 20, (150, 96, 40), 'lm'))
            paste(layer, nine(P['ui_btn_wide'], 72, 30, 12), x + 160, y + 18); t.append(('만들기', x + 196, y + 32, 20, INK, 'mm'))
        else:
            t.append(('?', x + 16, y + 30, 34, (150, 120, 100), 'mm')); t.append(('아직 못 찾은 떡', x + 40, y + 30, 20, (150, 120, 100), 'lm'))
    paste(layer, nine(P['ui_btn_wide'], 76, 32, 12), px + pw - 90, py + ph - 44); t.append(('닫기', px + pw - 52, py + ph - 29, 22, INK, 'mm'))
    big.alpha_composite(layer.resize((W * 2, H * 2), Image.NEAREST)); draw_texts(big, t)
    return big


if __name__ == '__main__':
    board, small = v2.board, v2.small
    board('가판대 화면 시안 v5 「안쪽 버전」 : 주인이 계산대 뒤에서 이쪽을 본다 · 손님은 계산대 앞 마루에 선다', [
        ('v4 (견줄 것 · 주인이 카메라 쪽)', small(finish(v4.stall_world(), 960, v4.stall_ui))), ('v5 9:16 · 270×480 을 ×2', finish(stall_world(), 960, stall_ui)),
        ('v5 9:20 · 남는 세로는 천장', finish(stall_world(), 1200, stall_ui)), ('곁그림 : 벽 차림표를 누른 상태 (뒤에 어두운 막)', stall_menu(960))], [
        '카메라가 손님 쪽(가게 홀)에 있다. 주인(10차 A · 큰 그림)은 계산대 뒤에서 정면을 본다. 허리 아래는 계산대에 가려진다. 뒷모습 그림은 없다.',
        '손님은 새 그림 없이 앞 보는 그림 그대로 계산대 앞 마루에 세웠다. 그래서 손님이 주인에게 등을 돌리고 이쪽을 보는 꼴이다 (어색한지 봐 주세요).',
        '줄은 큰 손님이 겹치지 않고 선다. 맨 앞이 계산대 앞, 둘째가 문 쪽이다. 문은 뒤 벽 오른쪽이고 손님 머리 위라 안 가려진다. 문으로 들어오고 나간다.',
        '벽 칠판 = 차림표 (「오늘의 떡」 과 한 장). 누르면 목록이 뜬다. 진열장은 계산대 위 왼쪽. 「주방」「레시피 도감」「점원」 은 왼쪽 아래 작은 아이콘.',
        'v4 흠 고친 것 : 둘째 손님이 문을 가리던 것 · 팝업 뒤에 어두운 막이 없던 것. 배경은 새로 뽑았다 (create_image_pro · 17001). 떡 이름 · 값 · 숫자는 자리표시다. 전부 시안이다.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v5-안쪽.png'))
    print('ok', 'content rows', Y0, Y1, 'OFF', OFF)

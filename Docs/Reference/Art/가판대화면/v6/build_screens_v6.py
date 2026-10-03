# 가판대 화면 v6 시안 합성 스크립트 — 손님 옆모습 견주기 · 2026-10-03 반복 03
# v5 「안쪽 버전」 을 그대로 두고 손님 그림만 바꿔 본다. 견줄 판은 셋이다.
#   front  : v5 그대로. 손님이 이쪽(카메라)을 본다 → 주인을 등진다
#   side   : 옆모습. 맨 앞 손님은 오른쪽(주인 쪽)을, 둘째 손님은 왼쪽(계산대 · 주인 쪽)을 본다
#   back34 : 3/4 뒷모습. 몸은 계산대 쪽으로 돌고 얼굴 옆선만 조금 보인다
# 옆모습 · 3/4 뒷모습은 v3 · v4 큰 손님을 edit_image_pro_flash 글 편집으로 돌려 뽑았다 (raw/ · README 참고).
# 화면은 270×480(9:16) 으로 짜서 ×2 한다 (v5 와 같은 도트 굵기). PIL 은 자르기·배치·정수 배 확대·글자·단색 채우기만 한다.
# v5 스크립트를 불러 쓰지 않고 복사해 고쳤다 (v5 폴더에 아무것도 쓰지 않으려고). 배경 원본은 v5/raw 를 읽기만 한다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/가판대화면/v6/build_screens_v6.py
import os, importlib.util
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v4s', os.path.join(HERE, '..', 'v4', 'build_screens_v4.py'))
v4 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v4)
v3, v2 = v4.v3, v4.v2
OUT, YELLOW = v4.OUT, v4.YELLOW
op, trim, paste, paste_base, cake, P = v4.op, v4.trim, v4.paste, v4.paste_base, v4.cake, v4.P
Scene, finish, BGW, BGH = v4.Scene, v4.finish, v4.BGW, v4.BGH
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)
RAW5 = os.path.join(HERE, '..', 'v5', 'raw')

# 배경 : v5 와 같다 (create_image_pro 272×608 · 17001). 흰 띠를 자르고 위 60 줄은 천장 색 한 가지로 채운다
raw = op(os.path.join(RAW5, 'interior_hall2_17001.png')); ra = np.array(raw)
rows = np.where((ra[:, :, :3].min(axis=2) < 250).any(axis=1))[0]; Y0, Y1 = int(rows[0]), int(rows[-1]) + 1
CEIL = tuple(int(v) for v in ra[Y0 + 2, 5]); OFF = BGH - Y1
hall = Image.new('RGBA', (BGW, BGH), CEIL); hall.paste(raw.crop((0, Y0, BGW, Y1)), (0, Y0 + OFF))
P['interior_hall2'] = hall

# 새 손님 그림 : 128×160 캔버스에서 그림만 잘라 쓴다 (배경은 처음부터 투명으로 받았다)
N = {}
N['customer_big_side'] = trim(op(os.path.join(RAW, 'customer_big_side_18101.png')))          # 파란 후드 · 오른쪽을 본다
N['customer_big_back34'] = trim(op(os.path.join(RAW, 'customer_big_back34_18102.png')))      # 파란 후드 · 3/4 뒷모습 (오른쪽 위)
N['customer_big_b_side'] = trim(op(os.path.join(RAW, 'customer_big_b_side_18201.png')))      # 초록 점퍼 · 왼쪽을 본다
N['customer_big_b_back34'] = trim(op(os.path.join(RAW, 'customer_big_b_back34_18202.png')))  # 초록 점퍼 · 3/4 뒷모습 (왼쪽 위)
for k, v in N.items(): v.save(os.path.join(PARTS, k + '.png'))
P.update(N)

COUNTER_Y = 325 + OFF        # 계산대 윗면이 시작하는 줄
COUNTER_X = 201              # 계산대 오른쪽 끝. 그 오른쪽은 문이다
FLOOR_FEET = 560             # 마루에 선 손님의 발 높이
POSE = {                     # 판 : (맨 앞 손님, 둘째 손님, 말풍선 가로 어긋남)
    'front': ('customer_big', 'customer_big_b', 0),
    'side': ('customer_big_side', 'customer_big_b_side', 22),      # 말풍선을 얼굴 앞(오른쪽 = 주인 쪽)으로 민다
    'back34': ('customer_big_back34', 'customer_big_b_back34', 16),
}


def stall_world(pose):
    bg = P['interior_hall2']; sc = Scene(bg); im = sc.im
    paste_base(im, P['owner_big'], 154, COUNTER_Y + 64)                         # 주인 : 계산대 뒤에서 이쪽을 본다
    im.paste(bg.crop((0, COUNTER_Y, COUNTER_X, BGH)), (0, COUNTER_Y))            # 허리 아래를 계산대로 다시 덮는다
    dc = P['display_case']; dx, dy = 3, COUNTER_Y + 18 - dc.height; paste(im, dc, dx, dy)
    for (px, py), (k, n) in zip([(25, 27), (57, 27), (89, 27), (25, 52), (57, 52), (89, 52)], [('plain_cake', 8), ('redbean_cake', 5), ('bean_cake', 6), ('mugwort_cake', 4), ('redbean_bean_cake', 7), (None, 0)]):
        if k: c = cake(k); paste(im, c, dx + px - c.width // 2, dy + py + 4 - c.height); sc.text(dx + px + 11, dy + py + 5, '×%d' % n, 14)
    sc.tag(dx + dc.width // 2, dy + 5, '진열 30/30', 18)
    bx = 120; by = 150 + OFF
    sc.text(bx, by + 18, '차림표', 22)
    for i, k in enumerate(['plain_cake', 'redbean_cake', 'bean_cake']): c = cake(k); paste(im, c, bx - 34 + i * 34 - c.width // 2, by + 44 - c.height // 2)
    sc.text(bx, by + 68, '오늘의 떡 : 팥떡', 16, YELLOW)
    sc.tag(bx - 34, by + 87, '눌러서 바로 만들기', 16)
    sc.text(247, 172 + OFF, '열림', 18)
    a, b, shift = POSE[pose]
    cb = P[a]; cx = 100; paste_base(im, cb, cx, FLOOR_FEET); head = FLOOR_FEET - cb.height
    paste_base(im, P[b], 218, FLOOR_FEET)
    bub = P['ui_bubble']; ck = cake('redbean_cake'); bxc = cx + shift
    paste(im, bub, bxc - bub.width // 2, head - bub.height - 2); paste(im, ck, bxc - ck.width // 2, head - bub.height - 2 + (bub.height - 6 - ck.height) // 2)
    sc.tag(238, 300 + OFF, '문으로 드나든다', 16)
    sc.tag(200, FLOOR_FEET + 16, '← 줄 (들어온 차례로)', 16)
    return sc


def stall_ui(im, t, H):
    v2.topbar(im, t, '가판대', '798', '오늘 손님 3/10')
    y = H - 84
    v2.icon_btn(im, t, 16, y, P['icon_mortar'], '주방'); v2.icon_btn(im, t, 96, y, P['icon_book'], '레시피 도감'); v2.icon_btn(im, t, 186, y, P['icon_apron'], '점원 200G')


if __name__ == '__main__':
    v2.board('가판대 화면 v6 : 손님이 어디를 보나 견주기 (v5 안쪽 버전 · 9:16 · 같은 배율 ×2)', [
        ('지금 (앞 보는 손님 · v5 그대로)', finish(stall_world('front'), 960, stall_ui)),
        ('옆모습 손님 (주인 쪽을 본다)', finish(stall_world('side'), 960, stall_ui)),
        ('덤 : 3/4 뒷모습 손님', finish(stall_world('back34'), 960, stall_ui))], [
        '손님 그림만 바꿨다. 배경 · 주인 · 진열장 · 차림표 · 아이콘 자리는 셋 다 같다. 옆모습 · 3/4 뒷모습은 지금 큰 손님 그림을 PixelLab 글 편집으로 돌려 뽑았다.',
        '옆모습 : 맨 앞 손님은 오른쪽(주인)을, 둘째 손님은 왼쪽(계산대)을 본다. 주문 말풍선은 얼굴 앞쪽(오른쪽)으로 조금 옮겼다. 표정은 옆얼굴만 보인다.',
        '3/4 뒷모습 : 계산대로 다가선 느낌이 가장 세지만 얼굴이 거의 안 보여 기다림 · 시무룩 표정을 쓰기 어렵다. 셋 중 하나를 골라 주세요.']).save(os.path.join(OUT, '2026-10-03-가판대화면-v6-손님옆모습.png'))
    print('ok', {k: v.size for k, v in N.items()})

# 정식 그림 — 가판대 화면(가게 안쪽) 배경 + 계산대 조각 (2026-10-04 · 반복 04 첫 일감)
# raw/(PixelLab 원본) → out/(게임에 넣을 png) 과 판정용 미리보기 한 장을 만든다.
# PIL 은 자르기 · 배치 · 줄 되풀이(같은 줄을 이어 붙이기) · 정수 배 확대 · 글자 · 단색 상자만 한다. 새 그림을 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/build_stall.py
# 다른 폴더(가판대화면/v1~v6 · Unity Resources)는 읽기만 한다 — 그 폴더 스크립트를 import 하지 않는다 (import 하면 그쪽 parts 를 다시 쓴다).
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw'); OUT = os.path.join(HERE, 'out'); os.makedirs(OUT, exist_ok=True)
REF = os.path.join(ROOT, 'Docs', 'Reference', 'Art')
MOCK = os.path.join(REF, '가판대화면')
ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
PREVIEW = os.path.join(REF, '시안', '2026-10-04-가판대실내-정식-미리보기.png')

W, H = 540, 1200            # 세계 = 9:20 화면. 9:16 은 아래 960 줄 (y 240~1200)
Y916 = H - 960

# ---- 배경을 잇는 자리 (세계 y) ----
# 후보 A : 세 조각을 그대로 쌓는다. top 0~359 (천장 · 등 · 위 벽 · 몰딩 343~359) → 벽 → 마루.
# 후보 B : top 에서 등 아래 맨벽 127 줄(216~342)을 빼서 벽을 127 줄 올리고, 마루를 127 줄 늘린다
#          (마루 원본 190~316 줄을 한 번 더 — 둘 다 널 이음 줄에서 자른다).
WALL_CUT = 17               # wall_19102 맨 위 몰딩(0~16 줄)은 버린다 — top 의 몰딩이 그 자리를 맡는다
VARIANT = {                 # 이름 : (top 에서 쓰는 줄 구간들, 마루에서 쓰는 줄 구간들, 계산대 윗면 y)
    'a': ([(0, 360)], [(0, 420)], 690),
    'b': ([(0, 216), (343, 360)], [(0, 317), (190, 420)], 600),
}
# ---- 계산대 조각 ----
CT_X1 = 360                 # 계산대 오른쪽 끝 (문 왼쪽 틀이 x 365 쯤)
CT_EXTRA = 60               # 앞판 세로 널을 이만큼 줄 되풀이로 늘린다 (원본 85 줄 → 145 줄)
CUST1_X, CUST2_X, FEET = 170, 420, 1070
BTN_H = 80                  # 버튼 줄 : 화면 아래끝에서 104 위 ~ 24 위

CREAM, INK, BGC = (250, 240, 220, 255), (60, 40, 32, 255), (58, 50, 48, 255)


def rgba(p): return Image.open(p).convert('RGBA')
def raw(n): return rgba(os.path.join(RAW, n + '.png'))
def trim(im): return im.crop(im.getbbox())
def font(s): return ImageFont.truetype(FONT, s)
def x2(im): return im.resize((im.width * 2, im.height * 2), Image.NEAREST)
def paste(dst, im, x, y): dst.alpha_composite(im, (int(x), int(y)))
def paste_base(dst, im, cx, base): paste(dst, im, cx - im.width // 2, base - im.height)


def build_bg(v):
    top = np.array(raw('top_19302'))
    # 몰딩 바로 위 오른쪽에 선반 물건 같은 얼룩(병·화분 흔적)이 왔다 → 같은 줄의 왼쪽 맨벽(x 160~350)을 옮겨 덮는다 (B 는 이 줄을 아예 뺀다)
    top[296:343, 350:540] = top[296:343, 160:350]
    wall = np.array(raw('wall_19102'))[WALL_CUT:]; floor = np.array(raw('floor_19201'))
    tops, floors, _ = VARIANT[v]
    col = np.concatenate([top[a:b] for a, b in tops] + [wall] + [floor[a:b] for a, b in floors], axis=0)[:H]
    assert col.shape[0] == H, col.shape
    floor_y = sum(b - a for a, b in tops) + wall.shape[0]
    return Image.fromarray(col), floor_y


def build_counter():
    c = np.array(raw('counter_19401'))
    c[99] = c[100]; c[99, :, 3] = 255                       # 배경 지우기가 윗판 한 줄(99)을 투명하게 뚫었다 → 아랫줄을 옮겨 메운다
    body = c[89:174]                                       # 그림이 있는 줄만 (85 줄)
    rep = np.repeat(body[140 - 89:141 - 89], CT_EXTRA, axis=0)   # 앞판 세로 널은 줄마다 같다 → 한 줄을 되풀이해 키를 키운다
    tall = np.concatenate([body[:140 - 89], rep, body[140 - 89:]], axis=0)
    im = Image.fromarray(tall)
    return im.crop((400 - CT_X1, 0, 400, im.height))           # 오른쪽 끝을 문 앞에서 멈추게, 왼쪽을 잘라 폭 360


def cake(k): return trim(rgba(os.path.join(ART, 'Cake', k + '.png')))


def world(bg, counter, ct_top, with_parts):
    im = bg.copy()
    if with_parts:
        paste_base(im, x2(rgba(os.path.join(MOCK, 'v3', 'parts', 'owner_big.png'))), 250, ct_top + 130)   # 허리 아래는 계산대에 가려진다
    paste(im, counter, 0, ct_top)
    if not with_parts: return im
    dc = x2(rgba(os.path.join(MOCK, 'v3', 'parts', 'display_case.png'))); dx, dy = 4, ct_top + 20 - dc.height; paste(im, dc, dx, dy)
    for (px, py), k in zip([(25, 27), (57, 27), (89, 27), (25, 52), (57, 52)], ['plain_cake', 'redbean_cake', 'bean_cake', 'mugwort_cake', 'redbean_bean_cake']):
        c = x2(cake(k)); paste(im, c, dx + px * 2 - c.width // 2, dy + (py + 4) * 2 - c.height)
    c1 = x2(rgba(os.path.join(MOCK, 'v6', 'parts', 'customer_big_side.png'))); paste_base(im, c1, CUST1_X, FEET)
    c2 = x2(rgba(os.path.join(MOCK, 'v6', 'parts', 'customer_big_b_side.png'))); paste_base(im, c2, CUST2_X, FEET)
    bub = x2(rgba(os.path.join(MOCK, 'v1', 'parts', 'ui_bubble.png'))); bx = CUST1_X + 44 - bub.width // 2; by = FEET - c1.height - bub.height + 8
    paste(im, bub, bx, by); ck = x2(cake('redbean_cake')); paste(im, ck, bx + (bub.width - ck.width) // 2, by + (bub.height - 12 - ck.height) // 2)
    return im


def box(d, x0, y0, x1, y1, fill, line=INK): d.rectangle((x0, y0, x1, y1), fill=fill, outline=line, width=3)


def ui(im, top_y=0):
    """UI 자리 표시 : 단색 상자 + 글자. 윗줄은 보이는 화면 맨 위, 버튼 줄은 화면 맨 아래 (화면 높이 기준)."""
    BTN_Y = im.height - 104; FEET_S = FEET - (H - im.height)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    box(d, 12, top_y + 12, 170, top_y + 60, (250, 240, 220, 220)); d.text((91, top_y + 36), '798 G', font=font(24), fill=INK, anchor='mm')
    box(d, 182, top_y + 12, 380, top_y + 60, (250, 240, 220, 220)); d.text((281, top_y + 36), '오늘 손님 3/10', font=font(22), fill=INK, anchor='mm')
    names = ['주방', '레시피 도감', '점원 200G', '닫기']; bw, gap = 120, (W - 4 * 120) // 5
    for i, n in enumerate(names):
        x0 = gap + i * (bw + gap); fill = (236, 150, 150, 235) if n == '닫기' else (250, 240, 220, 235)
        box(d, x0, BTN_Y, x0 + bw, BTN_Y + BTN_H, fill); d.text((x0 + bw // 2, BTN_Y + BTN_H // 2), n, font=font(22), fill=INK, anchor='mm')
    d.text((W - 10, BTN_Y - 10), '버튼 줄 자리 (단색 상자)', font=font(16), fill=CREAM, anchor='rb', stroke_width=3, stroke_fill=INK)
    d.text((10, FEET_S + 6), '주인 · 손님 · 진열장 · 말풍선 = 시안 부품 ×2 (자리 보기용)', font=font(15), fill=CREAM, anchor='lt', stroke_width=3, stroke_fill=INK)
    return Image.alpha_composite(im, lay)


def board(cols, title, notes):
    pad, head = 24, 96; cw = W
    ch = max(c.height for _, c in cols)
    note_h = 30 * len(notes) + 20
    out = Image.new('RGBA', (pad + len(cols) * (cw + pad), head + ch + 40 + note_h), BGC); d = ImageDraw.Draw(out)
    d.text((pad, 20), title, font=font(26), fill=CREAM)
    for i, (cap, c) in enumerate(cols):
        x = pad + i * (cw + pad); d.text((x, head - 8), cap, font=font(18), fill=(230, 200, 170, 255), anchor='lb')
        out.paste(c, (x, head + ch - c.height))
    y = head + ch + 30
    for n in notes: d.text((pad, y), n, font=font(18), fill=CREAM); y += 30
    return out


if __name__ == '__main__':
    counter = build_counter(); counter.save(os.path.join(OUT, 'stall_counter.png'))
    cols = []
    for v in ('a', 'b'):
        bg, floor_y = build_bg(v); ct = VARIANT[v][2]
        bg.save(os.path.join(OUT, 'stall_bg_%s.png' % v))
        full = world(bg, counter, ct, True)
        cols += [('후보 %s · 9:16 (540×960)' % v.upper(), ui(full.crop((0, Y916, W, H)))), ('후보 %s · 9:20 (540×1200)' % v.upper(), ui(full))]
        print(v, 'floor_y', floor_y, 'counter', ct, ct + counter.height)
    cols.append(('후보 B 배경 + 계산대만 (9:20)', world(bg, counter, VARIANT['b'][2], False)))
    board(cols, '가판대 화면(가게 안쪽) 정식 배경 미리보기 · 2026-10-04 · 도트 1px · 모두 ×1 · 후보 A (세 조각 그대로) / 후보 B (위 맨벽 127 줄 빼고 벽을 올림)', [
          '배경은 PixelLab 세 조각(천장 · 벽 · 마루)을 몰딩 · 걸레받이 줄에서 이어 붙였다. 계산대는 따로 뗀 조각이라 주인을 계산대 뒤에 세울 수 있다.',
          '주인 · 옆모습 손님 둘 · 진열장 · 말풍선은 지난 시안 부품을 ×2 로 얹은 것이다. 자리 보기용이고 정식 그림이 아니다 (다음에 1px 로 다시 뽑는다).',
          '윗줄(골드 · 오늘 손님)과 아래 버튼 줄(주방 · 레시피 도감 · 점원 · 닫기)은 단색 상자 자리 표시다. 칠판은 비어 있다. 차림표 글자 · 떡은 코드가 얹는다.',
          'A : 칠판이 낮아 진열장 · 주인에 많이 가린다. B : 등 아래 맨벽을 빼서 벽을 127 줄 올리고 마루 널 127 줄을 한 번 더 이었다 (칠판이 덜 가린다).',
          '9:20 에서만 보이는 위 240 줄 = 나무 천장 · 들보 · 종이 등 셋. 9:16 에서는 등이 안 보인다.']).convert('RGB').save(PREVIEW)
    print('ok', counter.size)

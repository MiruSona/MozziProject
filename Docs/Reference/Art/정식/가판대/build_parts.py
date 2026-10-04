"""가판대 화면 정식 부품 (반복 04 · 2026-10-04) — raw/ 의 PixelLab 결과를 out/ 부품으로 다듬고 미리보기를 만든다.

PIL 로는 새 그림을 칠하지 않는다. 여기서 하는 일은 배경 지우기 · 자르기 · 줄 빼기 · 겹 나누기(차이 뽑기) ·
단색 바꾸기(팔레트 교체) · 자리에 얹기 · 미리보기 단색 상자 · 글자뿐이다.
실행 : Tools/ArtTool/.venv/Scripts/python.exe build_parts.py
"""
import os
import numpy as np
from PIL import Image, ImageDraw

from build_stall import (RAW, OUT, REF, W, H, Y916, CREAM, INK, BGC,
                         rgba, raw, trim, font, paste, paste_base, box, build_bg, board)

PREVIEW = os.path.join(REF, '시안', '2026-10-04-가판대화면-정식부품-미리보기.png')

# ---- 자리 (세계 540×1200 = 9:20 화면, 9:16 은 y 240~1200) ----
CT_X, CT_TOP = 10, 670        # 계산대 왼쪽 끝 x, 윗판 맨 윗줄 y (문 아래끝 640 · 마루 시작 656 보다 아래)
CT_TOP_ALT = 700              # 다른 안 : 계산대를 30 더 내린다
CT_CUT = (78, 102)            # 계산대 앞판 가운데 민 줄 24 줄을 뺀다 (152 → 128 줄)
SLAB = 26                     # 계산대 윗면 안쪽 깊이 — 진열장 발이 이 줄에 닿는다 (계산대 맨 윗줄에서)
OWNER_CX = 300                # 주인 가운데 x
OWNER_WAIST = 170             # 주인 그림(손질 뒤)에서 허리 줄(코르셋 아래끝) — 이 줄을 계산대 윗면에 맞춘다
CASE_X = 18                   # 진열장 왼쪽 x
FEET = 1060                   # 손님 발끝 y (9:16 화면 y 820) · 버튼 줄 위끝 1096 과 36 줄 띈다
CUST_A_X, CUST_B_X = 150, 410 # 손님 둘 가운데 x. A 는 오른쪽(주인)을 본다, B 는 좌우 뒤집어 왼쪽을 본다
BTN_H = 80

FACE_BOX = (80, 60, 110, 96)  # 손님 얼굴 칸 (x0, y0, x1, y1) — 눈 · 볼 · 입
SHIRT_Y = (96, 168)           # 손님 윗옷이 있는 줄 (청바지는 168 아래)

HAIR_COLORS = {               # 머리 색 : 어두운 → 밝은 4 단
    'black':  [(34, 30, 38), (52, 46, 56), (74, 66, 78), (104, 94, 108)],
    'brown':  [(74, 42, 30), (112, 66, 46), (146, 92, 66), (178, 124, 94)],
    'orange': [(122, 52, 22), (176, 84, 34), (214, 122, 52), (240, 166, 92)],
    'purple': [(64, 40, 86), (98, 62, 128), (132, 92, 164), (170, 132, 196)],
}
SHIRT_COLORS = {              # 윗옷 색 : 어두운 → 밝은 4 단
    'blue':   None,           # 원래 색 그대로
    'red':    [(120, 40, 46), (168, 62, 64), (206, 92, 88), (232, 138, 128)],
    'green':  [(44, 92, 58), (66, 128, 80), (96, 162, 104), (140, 196, 140)],
    'yellow': [(150, 110, 36), (198, 154, 52), (232, 196, 82), (248, 226, 140)],
}


# ---- 손질 도구 ----
def knock_bg(im, tol=40, seeds=None):
    """네 귀퉁이(또는 seeds)에서 이어진 바탕색을 투명으로 (tol = 색 차이 허용)."""
    a = np.array(im.convert('RGBA')).astype(int); h, w = a.shape[:2]
    seeds = seeds or [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    seen = np.zeros((h, w), bool); stack = []
    for x, y in seeds:
        stack.append((x, y, a[y, x, :3].copy()))
    while stack:
        x, y, ref = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y, x]: continue
        if a[y, x, 3] == 0 or np.abs(a[y, x, :3] - ref).sum() <= tol:
            seen[y, x] = True
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): stack.append((x + dx, y + dy, ref))
    a[seen, 3] = 0
    return Image.fromarray(a.astype('uint8'))


def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def ramp_swap(im, ramp, keep_ink=45, mask=None):
    """겹의 색을 밝기 순서대로 새 4 단 색에 맞춘다 (팔레트 교체). 아주 어두운 테두리(밝기 keep_ink 아래)는 그대로."""
    a = np.array(im).copy(); m = a[..., 3] > 0
    if mask is not None: m &= mask
    cols = sorted({tuple(c) for c in a[m][:, :3]}, key=lum)
    cols = [c for c in cols if lum(c) >= keep_ink]
    if not cols: return Image.fromarray(a)
    lo, hi = lum(cols[0]), lum(cols[-1])
    lut = {}
    for c in cols:
        t = 0 if hi == lo else (lum(c) - lo) / (hi - lo)
        lut[c] = ramp[min(len(ramp) - 1, int(t * len(ramp)))]
    ys, xs = np.nonzero(m)
    for y, x in zip(ys, xs):
        c = tuple(a[y, x, :3])
        if c in lut: a[y, x, :3] = lut[c]
    return Image.fromarray(a)


def diff_layer(top, base, keep_out=None, hair_only=False):
    """top 그림에서 base 와 다른 점만 남긴 겹. keep_out 칸(x0,y0,x1,y1) 안은 비운다."""
    t = np.array(top); b = np.array(base)
    d = (np.abs(t.astype(int) - b.astype(int)).sum(-1) > 0) & (t[..., 3] > 0)
    if keep_out:
        x0, y0, x1, y1 = keep_out; d[y0:y1, x0:x1] = False
    if hair_only:   # 다시 그리며 바뀐 살 · 파란 윗옷 점은 머리 겹에 넣지 않는다 (색 바꾸기에 같이 물들고 윗옷 색을 덮는다)
        ti = t.astype(int); r, g, bl = ti[..., 0], ti[..., 1], ti[..., 2]
        skin = (r > 200) & (g > 170) & (bl > 140)   # 옅은 살색만 (갈색 머리 밝은 띠는 남긴다)
        blue = bl > r + 15
        d &= ~skin & ~blue
    out = np.zeros_like(t); out[d] = t[d]
    hole = ((b[..., 3] > 0) & (t[..., 3] == 0)).sum()   # base 에만 있고 top 에는 없는 점 — 겹으로는 못 지운다
    return Image.fromarray(out), int(hole)


# ---- 부품 ----
def build_counter():
    c = np.array(knock_bg(raw('counter_20102'), tol=60))
    a, b = CT_CUT
    c = np.concatenate([c[:a], c[b:]], axis=0)
    return trim(Image.fromarray(c))


def build_owner():
    return trim(raw('owner_20201'))


def build_case():
    return trim(raw('case_20301'))


def build_customer():
    base = raw('cust_side_20401'); body = raw('cust_buzz_20402')
    hair2 = raw('cust_hair2_20403'); happy = raw('cust_happy_20404'); sad = raw('cust_sad_20405')
    x0, y0, x1, y1 = FACE_BOX
    # 머리 겹 = 몸(짧은 머리)과 다른 점. 얼굴 칸은 표정 겹 몫이라 비운다.
    hair_01, hole1 = diff_layer(base, body, FACE_BOX, hair_only=True)
    hair_02, hole2 = diff_layer(hair2, body, FACE_BOX, hair_only=True)
    # 표정 겹 = 얼굴 칸만 오린 것 (보통은 몸 그림의 얼굴 칸 그대로)
    faces = {}
    for k, im in (('01', body), ('02', happy), ('03', sad)):
        f = Image.new('RGBA', body.size, (0, 0, 0, 0)); f.paste(im.crop(FACE_BOX), (x0, y0)); faces[k] = f
    # 윗옷 겹 = 몸에서 파란 윗옷 점만 떼어 낸다 (몸 겹에서는 지운다)
    b = np.array(body).astype(int); yy = np.arange(b.shape[0])[:, None]
    blue = (b[..., 3] > 0) & (b[..., 2] > b[..., 0] + 20) & (b[..., 2] >= b[..., 1]) & (yy >= SHIRT_Y[0]) & (yy < SHIRT_Y[1])
    shirt = np.zeros_like(b); shirt[blue] = b[blue]
    body_np = b.copy(); body_np[blue] = 0
    shirt_im = Image.fromarray(shirt.astype('uint8'))
    layers = {'body': Image.fromarray(body_np.astype('uint8')), 'hair_01': hair_01, 'hair_02': hair_02,
              'face_01': faces['01'], 'face_02': faces['02'], 'face_03': faces['03']}
    for k, ramp in SHIRT_COLORS.items():
        layers['shirt_' + k] = shirt_im if ramp is None else ramp_swap(shirt_im, ramp, keep_ink=0)
    for k, ramp in HAIR_COLORS.items():
        layers['hair_01_' + k] = ramp_swap(hair_01, ramp)
        layers['hair_02_' + k] = ramp_swap(hair_02, ramp)
    print('customer holes (몸에만 있고 머리 그림엔 없는 점)', hole1, hole2, 'shirt px', int(blue.sum()))
    return layers


def compose_customer(L, hair='01', hair_color='brown', shirt='blue', face='01'):
    im = L['body'].copy()
    for k in ('shirt_' + shirt, 'face_' + face, 'hair_%s_%s' % (hair, hair_color)):
        im.alpha_composite(L[k])
    return im


def build_bubble():
    return trim(knock_bg(raw('bubble_20501'), tol=90))


def build_button():
    return trim(knock_bg(raw('button_20601'), tol=90))


BUBBLE_9 = (14, 12, 14, 20)   # 9-slice 테두리 (왼 · 위 · 오른 · 아래) — 아래에 꼬리가 있어 두껍다
BUTTON_9 = (10, 10, 10, 10)


def nine_slice(im, w, h, border):
    """9-slice 늘이기 (미리보기용 — 코드의 Image.Type Sliced 와 같은 일을 한다). 가운데·변은 늘이지 않고 되풀이로 채운다."""
    l, t, r, b = border; sw, sh = im.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    def tile(src, x, y, tw, th):
        if src.width == 0 or src.height == 0 or tw <= 0 or th <= 0: return
        for yy in range(0, th, src.height):
            for xx in range(0, tw, src.width):
                out.alpha_composite(src.crop((0, 0, min(src.width, tw - xx), min(src.height, th - yy))), (x + xx, y + yy))
    cx = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    cy = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in cx:
        for sy0, sy1, dy0, dy1 in cy:
            tile(im.crop((sx0, sy0, sx1, sy1)), dx0, dy0, dx1 - dx0, dy1 - dy0)
    return out


# ---- 미리보기 ----
def world(bg, P, ct_top):
    im = bg.copy()
    owner, counter, case = P['owner_big'], P['stall_counter2'], P['display_case']
    paste(im, owner, OWNER_CX - owner.width // 2, ct_top - OWNER_WAIST)            # 1 주인 (계산대 뒤)
    paste(im, counter, CT_X, ct_top)                                                # 2 계산대
    paste(im, case, CASE_X, ct_top + SLAB - case.height)                            # 3 진열장 (윗면에 얹힘)
    L = P['cust']
    a = compose_customer(L, '01', 'brown', 'blue', '02')
    bimg = compose_customer(L, '02', 'black', 'red', '01').transpose(Image.FLIP_LEFT_RIGHT)
    ab = a.getbbox(); paste(im, a, CUST_A_X - (ab[0] + ab[2]) // 2, FEET - ab[3])  # 4 손님 A (발끝 기준)
    bb = bimg.getbbox(); paste(im, bimg, CUST_B_X - (bb[0] + bb[2]) // 2, FEET - bb[3])
    bub = nine_slice(P['ui_bubble'], 96, 72, BUBBLE_9)
    paste(im, bub, CUST_A_X + 30, FEET - (ab[3] - ab[1]) - 40)                      # 5 말풍선 (손님 얼굴 앞 · 꼬리가 얼굴 쪽)
    return im


def ui(im, top_y, btn):
    BTN_Y = im.height - 104
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    box(d, 12, top_y + 12, 170, top_y + 60, (250, 240, 220, 220)); d.text((91, top_y + 36), '798 G', font=font(24), fill=INK, anchor='mm')
    box(d, 182, top_y + 12, 380, top_y + 60, (250, 240, 220, 220)); d.text((281, top_y + 36), '오늘 손님 3/10', font=font(22), fill=INK, anchor='mm')
    names = ['주방', '레시피 도감', '점원', '닫기']; bw, gap = 120, (W - 4 * 120) // 5
    for i, n in enumerate(names):
        x0 = gap + i * (bw + gap)
        lay.alpha_composite(nine_slice(btn, bw, BTN_H, BUTTON_9), (x0, BTN_Y))
        d.text((x0 + bw // 2, BTN_Y + BTN_H // 2), n, font=font(22), fill=INK, anchor='mm')
    return Image.alpha_composite(im, lay)


def face_strip(L):
    """표정 견주기 : 보통 · 기쁨 · 시무룩 을 ×1 과 ×3 으로, 머리 둘 × 색 넷 · 윗옷 넷 줄."""
    cols = []
    for f, n in (('01', '보통'), ('02', '기쁨'), ('03', '시무룩')):
        cols.append((n, compose_customer(L, '01', 'brown', 'blue', f)))
    combos = [('02', 'black', 'red'), ('01', 'orange', 'green'), ('02', 'purple', 'yellow'), ('01', 'black', 'blue')]
    out = Image.new('RGBA', (W * 3 + 24, 560), BGC); d = ImageDraw.Draw(out)
    d.text((12, 8), '표정 견주기 · 위 : ×1 (실제 크기) · 가운데 : 얼굴 ×3 · 아래 : 머리 2 × 색 4 · 윗옷 4 (팔레트 교체)', font=font(18), fill=CREAM)
    x = 12
    for n, im in cols:
        t = trim(im); out.alpha_composite(t, (x, 40)); d.text((x, 40 + t.height + 4), n, font=font(16), fill=CREAM)
        fc = im.crop((56, 30, 116, 110)).resize((180, 240), Image.NEAREST); out.alpha_composite(fc, (x + t.width + 8, 40))
        x += t.width + 200
    x = 12
    for h, hc, sc in combos:
        for f in ('01', '02', '03'):
            t = trim(compose_customer(L, h, hc, sc, f)); out.alpha_composite(t, (x, 310)); x += t.width + 6
        x += 14
    return out


if __name__ == '__main__':
    P = {'stall_counter2': build_counter(), 'owner_big': build_owner(), 'display_case': build_case(),
         'ui_bubble': build_bubble(), 'ui_button': build_button()}
    for k, im in P.items():
        im.save(os.path.join(OUT, k + '.png')); print(k, im.size)
    L = build_customer(); P['cust'] = L
    for k, im in L.items(): im.save(os.path.join(OUT, 'cust_side_%s.png' % k))
    print('cust layers', len(L), L['body'].size)

    bg, floor_y = build_bg('b')
    main = world(bg, P, CT_TOP); alt = world(bg, P, CT_TOP_ALT)
    cols = [('본안 · 9:16 (540×960)', ui(main.crop((0, Y916, W, H)), 0, P['ui_button'])),
            ('본안 · 9:20 (540×1200)', ui(main, 0, P['ui_button'])),
            ('다른 안 · 계산대 30 더 내림 · 9:16', ui(alt.crop((0, Y916, W, H)), 0, P['ui_button']))]
    top = board(cols, '가판대 화면 정식 부품 미리보기 · 2026-10-04 · 도트 1px · 모두 ×1 (배경 B)', [
        '진열장은 계산대 왼쪽, 주인은 그 오른쪽 빈자리에 나란히 선다. 계산대 윗면을 문 아래끝보다 낮게(y 670) 내려 주인 허리 위가 다 보인다.',
        '주인 머리 꼭대기가 칠판 틀 아래끝보다 낮아 칠판을 가리지 않는다. 진열장 지붕도 칠판 아래에 머문다.',
        '다른 안 : 계산대를 30 더 내리면(y 700) 주인 치마 윗부분까지 보이고 계산대가 손님 머리와 가까워진다. 주인은 그대로 계산대에 허리를 맞춘다.',
        '윗줄 · 버튼 줄의 글자와 칸 자리는 자리 표시다. 버튼 틀은 정식 9-slice 부품(ui_button)을 늘인 것이다.'])
    strip = face_strip(L)
    full = Image.new('RGBA', (top.width, top.height + strip.height + 20), BGC)
    full.paste(top, (0, 0)); full.alpha_composite(strip, (24, top.height + 10))
    full.convert('RGB').save(PREVIEW); print('preview', full.size, PREVIEW)

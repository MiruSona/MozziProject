"""가판대 화면 정식 부품 2차 (반복 04 · 2026-10-04) — 사용자 피드백 1~4 를 반영한 판.

raw/ 의 PixelLab 결과를 out/(정본) · out_alt/(견줄 것)으로 다듬고 v2 미리보기 한 장을 만든다.
PIL 로는 새 그림을 칠하지 않는다. 하는 일 : 배경 지우기 · 자르기 · 배치 · 정수배 확대 · 글자 ·
두 그림의 차이 떼기 · 단색 일대일 바꾸기(팔레트) · 미리보기 단색 상자.
실행 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/build_v2.py
"""
import os
import numpy as np
from collections import Counter
from PIL import Image, ImageDraw

from build_stall import (RAW, OUT, REF, W, H, Y916, CREAM, INK, BGC, raw, trim, font, paste, board, build_bg)
from build_parts import knock_bg, nine_slice, BUTTON_9, BUBBLE_9

HERE = os.path.dirname(os.path.abspath(__file__))
ALT = os.path.join(HERE, 'out_alt'); os.makedirs(ALT, exist_ok=True)
PREVIEW = os.path.join(REF, '시안', '2026-10-04-가판대화면-정식부품-v2-미리보기.png')

# ---- 자리 (세계 540×1200 = 9:20 화면, 9:16 은 y 240~1200) ----
CT_RIGHT = 380          # 계산대 오른쪽 끝(마구리) x. 왼쪽은 화면 밖에서 들어온다 (문 왼쪽 틀 364 · 문 아래끝 640 보다 아래라 문을 안 가린다)
CT_TOP = 660            # 계산대 맨 윗줄(상판 뒤끝) y
TOP_DEPTH = 35          # 상판 윗면 깊이 (계산대 맨 윗줄에서 앞 모서리 lip 까지)
ON_TOP = 22             # 물건 발이 닿는 줄 = 계산대 맨 윗줄 + 22 (상판 한가운데쯤 — 앞에 13 줄이 남는다)
CASE_X = 8              # 진열장 왼쪽 x
OWNER_CX = 268          # 주인 가운데 x
OWNER_WAIST = 146       # 주인 그림(손질 뒤)에서 허리 줄 — 계산대 맨 윗줄 + 4 에 맞춘다
FEET = 1060             # 손님 발끝 y
CUST_A_X, CUST_B_X = 150, 410
BTN_H = 80

# ---- 손님 겹 ----
FACE_BOX = (80, 54, 112, 96)   # 표정을 다시 그린 칸 (x0, y0, x1, y1)
SHIRT_Y = (92, 175)

# 6 단 램프 (outline · deep · shade · base · light · shine) — 손님/v2-새화풍 의 값과 같다. purple 만 새로 정했다.
def hx(s): return tuple(int(s[i:i + 2], 16) for i in (1, 3, 5))
def ramp(*c): return [hx(x) for x in c]
HAIR = {
    'black':  ramp('#0F0E11', '#201D23', '#2D2A32', '#3A3640', '#625D6A', '#9994A0'),
    'brown':  ramp('#21140C', '#452B19', '#623E26', '#7E5234', '#9A6F52', '#BEA18C'),
    'orange': ramp('#341E0E', '#6E3F1D', '#9C5C2E', '#C87A42', '#D49262', '#E3BC9F'),
    'purple': ramp('#1E1028', '#3A2350', '#563476', '#7248A0', '#9370BF', '#BDA5DC'),
}
SHIRT = {
    'blue':   ramp('#101F31', '#224269', '#356094', '#4A7EBE', '#6995CC', '#A1BDDE'),
    'red':    ramp('#35100D', '#71211B', '#A0352C', '#CD4A40', '#D86A61', '#E6A49F'),
    'green':  ramp('#172915', '#31572D', '#487B42', '#5F9E58', '#7AB374', '#A9CEA5'),
    'yellow': ramp('#3B3014', '#7D652B', '#B19142', '#E3BC5C', '#E9C97A', '#F1DEB1'),
}
CUTS = (0.62, 0.88, 1.15, 1.70)   # base 밝기 대비 비율로 deep | shade | base | light | shine 을 가른다


def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def arr(im): return np.array(im.convert('RGBA')).astype(int)


def img(a): return Image.fromarray(np.clip(a, 0, 255).astype('uint8'))


def royal(a):   # 겹을 가르려고 「royal blue」로 시킨 색 — 채도가 높은 파랑
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (b - np.maximum(r, g) > 45) & (a[..., 3] > 0)


def dark(a): return (a[..., 3] > 0) & (0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2] < 60)


def near(m, k=2):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def steps(a, m, outline_mask):
    """m 안의 색을 6 단 번호(0~5)로. outline_mask 는 0 단. 나머지는 가장 많은 색을 base(3) 로 보고 밝기 비율로 자른다."""
    idx = np.full(a.shape[:2], -1)
    idx[m & outline_mask] = 0
    rest = m & ~outline_mask
    if rest.any():
        base = Counter(map(tuple, a[rest][:, :3])).most_common(1)[0][0]
        bl = max(1.0, lum(base))
        for y, x in zip(*np.nonzero(rest)):
            t = lum(a[y, x]) / bl
            idx[y, x] = 1 + sum(t >= c for c in CUTS)
    return idx


def bake(idx, rmp):
    out = np.zeros(idx.shape + (4,), int)
    for k in range(6):
        out[idx == k, :3] = rmp[k]; out[idx == k, 3] = 255
    return img(out)


def merge_near(a, mask, tol=10):
    """거의 같은 색(차이 tol 이하)을 그 무리에서 가장 많은 색 하나로 모은다 — 단색 일대일 바꾸기. 「깨져 보이는」 잔색을 줄인다."""
    a = a.copy(); cnt = Counter(map(tuple, a[mask][:, :3]))
    keep = []
    lut = {}
    for c, _ in cnt.most_common():
        for k in keep:
            if sum(abs(c[i] - k[i]) for i in range(3)) <= tol:
                lut[c] = k; break
        else:
            keep.append(c); lut[c] = c
    for y, x in zip(*np.nonzero(mask)):
        a[y, x, :3] = lut[tuple(a[y, x, :3])]
    return a, len(cnt), len(keep)


# ---- A · 계산대 ----
PINK_LUT = {  # 짙은 갈색 앞판 → 분홍 앞판 (앞판 줄 87~149 만. 굽도리 · 외곽선 · 상판은 그대로)
    (118, 67, 50): (226, 182, 176), (109, 62, 48): (219, 172, 166), (96, 55, 44): (212, 165, 159),
    (88, 46, 38): (199, 145, 141), (79, 42, 36): (175, 121, 119), (70, 37, 34): (149, 92, 97),
    (62, 33, 33): (120, 70, 78), (53, 29, 32): (120, 70, 78), (167, 98, 73): (232, 196, 190),
}
TRIM_LUT = {(207, 136, 147): (225, 206, 185), (255, 190, 192): (244, 238, 227)}   # 분홍 판에서는 분홍 띠를 크림 띠로


def build_counters():
    c = arr(knock_bg(raw('counter_21103'), tol=50))
    c = c[42:164, :397]                                     # 그림이 있는 줄 · 칸만 (122 × 397)
    brown = c.copy(); pink = c.copy()
    for y in range(c.shape[0]):
        for x in range(c.shape[1]):
            if c[y, x, 3] == 0: continue
            t = tuple(c[y, x, :3]); yy = y + 42
            if 87 <= yy < 150 and t in PINK_LUT: pink[y, x, :3] = PINK_LUT[t]
            elif 78 <= yy < 87 and t in TRIM_LUT: pink[y, x, :3] = TRIM_LUT[t]
    return img(brown), img(pink)


# ---- C · 주인 ----
def build_owner(): return trim(raw('owner_21201'))


# ---- B · 진열장 ----
def build_case(): return trim(raw('case_21302'))


# ---- D · 손님 겹 ----
def body_and_shirt():
    b = arr(raw('cust_bald_21401')); yy = np.arange(240)[:, None]
    sh = (b[..., 3] > 0) & (b[..., 2] > b[..., 0] + 20) & (b[..., 2] > b[..., 1] + 15) & ~royal(b) & (yy >= SHIRT_Y[0]) & (yy < SHIRT_Y[1])
    sidx = steps(b, sh, np.zeros_like(sh))
    body = b.copy(); body[sh] = 0
    body, n0, n1 = merge_near(body, body[..., 3] > 0, tol=10)
    print('body colors', n0, '->', n1, 'shirt px', int(sh.sum()))
    return b, img(body), sidx


def hair_layer_m2(name, base):
    """방법 ② : 완성 그림 − 민머리 기준 그림 = 차이. 그중 royal blue 와 그 곁의 짙은 선만 머리로 남기고 6 단으로 묶는다."""
    h = arr(raw(name)); d = (np.abs(h - base).sum(-1) > 0) & (h[..., 3] > 0)
    x0, y0, x1, y1 = FACE_BOX; d[y0:y1, x0:x1] = False
    rb = royal(h) & d
    ol = dark(h) & d & near(rb, 2)
    keep = rb | ol
    dropped = int((d & ~keep).sum())
    return steps(h, keep, ol), dropped


def hair_layer_m1(name):
    """방법 ① : PixelLab 이 「바뀐 칸만」 으로 돌려준 겹을 그대로 쓴다. 파랑만 6 단으로 묶고 나머지(살 · 옷 점)는 그 색 그대로 둔다."""
    h = arr(raw(name)); m = h[..., 3] > 0
    rb = royal(h) & m; ol = dark(h) & m & near(rb, 2)
    idx = steps(h, rb | ol, ol)
    other = h.copy(); other[rb | ol] = 0
    return idx, other, int((m & ~(rb | ol)).sum())


def face_layer(name, base, only=False):
    f = arr(raw(name)); x0, y0, x1, y1 = FACE_BOX
    if only:
        m = f[..., 3] > 0                       # ① : PixelLab 이 돌려준 칸 전체 (마스크 네모 그대로)
    else:
        m = (np.abs(f - base).sum(-1) > 0) & (f[..., 3] > 0)   # ② : 바뀐 점만
        box = np.zeros_like(m); box[y0:y1, x0:x1] = True; m &= box
    out = np.zeros_like(f); out[m] = f[m]
    return out, m


def build_customers():
    base, body, sidx = body_and_shirt()
    pal = set(map(tuple, arr(body)[arr(body)[..., 3] > 0][:, :3]))
    L2, L1 = {'body': body}, {'body': body}
    for k, r in SHIRT.items():
        L2['shirt_' + k] = L1['shirt_' + k] = bake(sidx, r)
    # 표정 : face_01 = 기준 그림의 얼굴 그대로 (빈 겹)
    empty = img(np.zeros((240, 120, 4), int))
    L2['face_01'] = L1['face_01'] = empty
    for k, n in (('02', 'happy'), ('03', 'sad')):
        f2, m2 = face_layer('cust_%s_216%s' % (n, '01' if k == '02' else '02'), base)
        f2, a0, a1 = merge_near(f2, m2, tol=12)
        L2['face_' + k] = img(f2); print('face', k, 'px', int(m2.sum()), 'colors', a0, '->', a1)
        f1, m1 = face_layer('cust_%s_only_216%s' % (n, '01' if k == '02' else '02'), base, only=True)
        L1['face_' + k] = img(f1)
    for hk, n2, n1 in (('01', 'cust_hairB_21501', 'cust_hairB_only_21501'), ('02', 'cust_hairS_21502', 'cust_hairS_only_21502')):
        idx2, drop = hair_layer_m2(n2, base)
        idx1, other, rest = hair_layer_m1(n1)
        print('hair', hk, '② px', int((idx2 >= 0).sum()), 'dropped', drop, '| ① px', int((idx1 >= 0).sum()), 'non-hair kept', rest)
        for ck, r in HAIR.items():
            L2['hair_%s_%s' % (hk, ck)] = bake(idx2, r)
            h1 = arr(bake(idx1, r)); o = other.copy(); o[h1[..., 3] > 0] = h1[h1[..., 3] > 0]
            L1['hair_%s_%s' % (hk, ck)] = img(o)
    return L2, L1


def compose(L, hair='01', hc='brown', shirt='blue', face='01'):
    im = L['body'].copy()
    for k in ('shirt_' + shirt, 'face_' + face, 'hair_%s_%s' % (hair, hc)):
        im.alpha_composite(L[k])
    return im


# ---- E · 소품 ----
def build_props():
    return {'prop_register': trim(raw('prop_register_21701')), 'prop_vase': trim(raw('prop_vase_21702')),
            'prop_menu': trim(raw('prop_menu_21703')), 'board_topper': trim(raw('board_topper_21704')),
            'ui_bubble_c': trim(raw('bubble_21705'))}


# ---- 미리보기 ----
def world(bg, P, counter, owner, L, props=True, cust_scale_note=None):
    im = bg.copy()
    top = P['board_topper']; paste(im, top, 213 - top.width // 2, 337 - top.height // 2)        # 칠판 머리 장식
    paste(im, owner, OWNER_CX - owner.width // 2, CT_TOP + 4 - OWNER_WAIST)                      # 1 주인 (계산대 뒤)
    paste(im, counter, CT_RIGHT - counter.width, CT_TOP)                                          # 2 계산대
    on = CT_TOP + ON_TOP
    case = P['display_case2']; paste(im, case, CASE_X, on - case.height)                           # 3 진열장 (상판 안쪽에 올라앉음)
    if props:
        v = P['prop_vase']; paste(im, v, CASE_X + case.width + 4, on - v.height)
        r = P['prop_register']; paste(im, r, CT_RIGHT - r.width - 6, on - r.height)
    a = compose(L, '01', 'brown', 'blue', '02')
    b = compose(L, '02', 'black', 'red', '01').transpose(Image.FLIP_LEFT_RIGHT)
    ab = a.getbbox(); paste(im, a, CUST_A_X - (ab[0] + ab[2]) // 2, FEET - ab[3])
    bb = b.getbbox(); paste(im, b, CUST_B_X - (bb[0] + bb[2]) // 2, FEET - bb[3])
    bub = P['ui_bubble_c']; hx_ = CUST_A_X + 20
    paste(im, bub, hx_ - bub.width // 2 + 30, FEET - (ab[3] - ab[1]) - bub.height - 2)            # 4 말풍선 (꼬리 가운데 판)
    return im


def ui(im, btn):
    BTN_Y = im.height - 104
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for x0, x1, t in ((12, 170, '798 G'), (182, 380, '오늘 손님 3/10')):
        d.rectangle((x0, 12, x1, 60), fill=(250, 240, 220, 220), outline=INK, width=3)
        d.text(((x0 + x1) // 2, 36), t, font=font(22), fill=INK, anchor='mm')
    names = ['주방', '레시피 도감', '점원', '닫기']; bw, gap = 120, (W - 4 * 120) // 5
    for i, n in enumerate(names):
        x0 = gap + i * (bw + gap)
        lay.alpha_composite(nine_slice(btn, bw, BTN_H, BUTTON_9), (x0, BTN_Y))
        d.text((x0 + bw // 2, BTN_Y + BTN_H // 2), n, font=font(22), fill=INK, anchor='mm')
    return Image.alpha_composite(im, lay)


def board_text(im, y_off):
    d = ImageDraw.Draw(im)
    d.text((213, 337 - y_off), '오늘의 떡', font=font(14), fill=INK, anchor='mm')   # 코드가 TMP 로 얹을 글자 (자리 보기)
    return im


def label(im, t, y=4):
    d = ImageDraw.Draw(im); d.text((6, y), t, font=font(16), fill=CREAM, stroke_width=3, stroke_fill=INK)
    return im


def x3(im): return im.resize((im.width * 3, im.height * 3), Image.NEAREST)


def head(im): return im.crop((10, 14, 116, 112))


def strip_methods(L2, L1, v1):
    """손님 두 방법 견주기 + 1차(v1) 같은 꼴. ×1 와 머리 ×3."""
    rows = [('1차 (지난 판)', v1), ('방법 ② 차이 떼기 + 6 단 팔레트 (추천 · out/)', L2), ('방법 ① PixelLab 이 준 겹 그대로 (out_alt/)', L1)]
    combos = [('01', 'brown', 'blue'), ('02', 'black', 'red'), ('01', 'orange', 'green'), ('02', 'purple', 'yellow')]
    out = Image.new('RGBA', (1800, 40 + len(rows) * 330), BGC); d = ImageDraw.Draw(out)
    d.text((12, 8), '손님 겹 견주기 · 왼쪽 : 머리 ×3 (보통 · 기쁨 · 시무룩) · 오른쪽 : ×1 조합 (머리 2 × 색 · 윗옷 색 × 표정 3)', font=font(18), fill=CREAM)
    for i, (t, L) in enumerate(rows):
        y = 40 + i * 330; d.text((12, y), t, font=font(18), fill=(230, 200, 170, 255))
        x = 12
        for f in ('01', '02', '03'):
            im = v1compose(L, '01', 'brown', 'blue', f) if L is v1 else compose(L, '01', 'brown', 'blue', f)
            out.alpha_composite(x3(head(im)), (x, y + 26)); x += 330
        for h_, hc, sc in combos:
            for f in ('01', '02', '03'):
                im = v1compose(L, h_, hc, sc, f) if L is v1 else compose(L, h_, hc, sc, f)
                t_ = trim(im); out.alpha_composite(t_, (x, y + 300 - t_.height)); x += t_.width + 2
            x += 8
    return out


def v1compose(L, hair, hc, shirt, face):
    im = L['body'].copy()
    for k in ('shirt_' + shirt, 'face_' + face, 'hair_%s_%s' % (hair, hc)):
        im.alpha_composite(L[k])
    return im


def load_v1():
    L = {}
    for f in os.listdir(OUT):
        if f.startswith('cust_side_') and f.endswith('.png'): L[f[10:-4]] = Image.open(os.path.join(OUT, f)).convert('RGBA')
    return L


def strip_counters(bg, P, owner, L):
    """계산대 후보 견주기 (9:16 · 손님 · UI 없이)."""
    cols = []
    for k, t in (('stall_counter3_brown', '후보 1 · 짙은 갈색 앞판 (추천)'), ('stall_counter3_pink', '후보 2 · 분홍 앞판'), ('stall_counter2', '1차 계산대 (지난 판)')):
        im = bg.copy()
        paste(im, owner, OWNER_CX - owner.width // 2, CT_TOP + 4 - OWNER_WAIST)
        c = P[k]
        if k == 'stall_counter2':
            paste(im, c, 10, 670); case = P['display_case']; paste(im, case, 18, 696 - case.height)
        else:
            paste(im, c, CT_RIGHT - c.width, CT_TOP); case = P['display_case2']; paste(im, case, CASE_X, CT_TOP + ON_TOP - case.height)
        cols.append((t, im.crop((0, 300, W, 900))))
    return cols


def strip_size(bg, P, counter, L):
    """주인 크기 견주기 : 1차 주인(큰) vs 2차 주인(줄임)."""
    cols = []
    for k, t in (('owner_v2', '주인 줄임 (2차 · 추천)'), ('owner_big', '주인 그대로 (1차 크기)')):
        im = bg.copy(); o = P[k]
        waist = OWNER_WAIST if k == 'owner_v2' else 170
        paste(im, o, OWNER_CX - o.width // 2, CT_TOP + 4 - waist)
        paste(im, counter, CT_RIGHT - counter.width, CT_TOP)
        case = P['display_case2']; paste(im, case, CASE_X, CT_TOP + ON_TOP - case.height)
        a = compose(L, '01', 'brown', 'blue', '01'); ab = a.getbbox(); paste(im, a, CUST_A_X - (ab[0] + ab[2]) // 2, FEET - ab[3])
        cols.append((t, im.crop((0, 440, W, 1080))))
    return cols


def strip_props(P):
    out = Image.new('RGBA', (1100, 200), BGC); d = ImageDraw.Draw(out)
    d.text((12, 8), '소품 · 칠판 머리 장식 · 꼬리 가운데 말풍선 (×1 와 ×3)', font=font(18), fill=CREAM)
    x = 12
    for k in ('prop_register', 'prop_vase', 'prop_menu', 'board_topper', 'ui_bubble_c'):
        im = P[k]; out.alpha_composite(im, (x, 40)); out.alpha_composite(x3(im) if im.width < 70 else im.resize((im.width * 2, im.height * 2), Image.NEAREST), (x, 40 + im.height + 8))
        x += max(im.width * (3 if im.width < 70 else 2), 60) + 24
    return out


def stack(*ims):
    w = max(i.width for i in ims); h = sum(i.height for i in ims) + 20 * len(ims)
    out = Image.new('RGBA', (w, h), BGC); y = 0
    for i in ims: out.alpha_composite(i, (0, y)); y += i.height + 20
    return out


if __name__ == '__main__':
    brown, pink = build_counters()
    P = {'stall_counter3_brown': brown, 'stall_counter3_pink': pink, 'owner_v2': build_owner(), 'display_case2': build_case()}
    P.update(build_props())
    for k, im in P.items(): im.save(os.path.join(OUT, k + '.png')); print(k, im.size)
    for k in ('stall_counter2', 'display_case', 'owner_big', 'ui_button'):
        P[k] = Image.open(os.path.join(OUT, k + '.png')).convert('RGBA')
    L2, L1 = build_customers()
    for k, im in L2.items(): im.save(os.path.join(OUT, 'cust2_side_%s.png' % k))
    for k, im in L1.items(): im.save(os.path.join(ALT, 'cust2m1_side_%s.png' % k))
    print('layers', len(L2), len(L1))

    bg, _ = build_bg('b')
    main = world(bg, P, brown, P['owner_v2'], L2)
    main = board_text(main, 0)
    alt = world(bg, P, pink, P['owner_v2'], L2)
    alt = board_text(alt, 0)
    cols = [('본안 · 9:16 (540×960) · 갈색 계산대', ui(main.crop((0, Y916, W, H)), P['ui_button'])),
            ('본안 · 9:20 (540×1200)', ui(main, P['ui_button'])),
            ('분홍 계산대 · 9:16', ui(alt.crop((0, Y916, W, H)), P['ui_button']))]
    top = board(cols, '가판대 화면 정식 부품 2차 미리보기 · 2026-10-04 · 도트 1px · 모두 ×1 (배경 B)', [
        '1 계산대 : 화면 왼쪽 밖에서 들어와 문 앞(x 380)에서 둥근 마구리로 끝난다. 상판 윗면을 35 줄로 깊게, 앞판은 마루와 다른 짙은 갈색(또는 분홍).',
        '2 진열장 : 상판 안쪽(계산대 맨 윗줄 + 22)에 발을 디뎌 앞에 상판이 13 줄 남는다, 떨어질 것 같지 않게.',
        '3 주인 : 1.65 배 → 1.4 배 크기로 새로 뽑았다 (147×195 → 126×175). 머리 폭이 손님 단발 머리와 거의 같다.',
        '4 손님 : 민머리 기준 그림 + 머리 · 윗옷 · 표정 겹. 머리 · 윗옷은 6 단 팔레트로 색을 바꾼다. 글자(오늘의 떡 · 버튼)는 코드 몫 자리 보기.'])
    cc = board(strip_counters(bg, P, P['owner_v2'], L2), '계산대 후보 견주기 (세계 y 300~900 만)', ['후보 1 은 상판 크림 · 앞판 짙은 갈색 · 분홍 띠, 마루(주황 나무)와 가장 또렷이 갈린다.'])
    ss = board(strip_size(bg, P, brown, L2), '주인 크기 견주기 (세계 y 440~1080)', [
        '손님 키움 안은 따로 그리지 않았다. 1px 도트라 정수배가 아닌 확대가 안 되고, 키우면 발끝이 버튼 줄(y 1096)에 더 붙는다.'])
    v1 = load_v1()
    ms = strip_methods(L2, L1, v1)
    ps = strip_props(P)
    full = stack(top, cc, ss, ms, ps)
    full.convert('RGB').save(PREVIEW); print('preview', full.size, PREVIEW)

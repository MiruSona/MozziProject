"""가판대 화면 옆모습 손님 3차 (반복 04 · 2026-10-04) — 파츠를 처음부터 따로 그려 받은 판.

PixelLab 이 몸 · 윗옷 · 머리(표정) · 머리카락을 각각 따로 그린 120×240 겹(raw/cust3_*)을
out/cust3_side_* 로 옮기고 3차 미리보기 한 장을 만든다.
PIL 로는 새 그림을 칠하지 않는다. 하는 일 : 겹을 같은 만큼 옮기기(배치) · 떨어진 부스러기 지우기 ·
회색 단을 색 램프로 일대일 바꾸기(팔레트) · 정수배 확대 · 글자.
예외 (3d · 사용자 허락) : 머리통 겹 셋의 턱 밑 가장자리와 입은 fix_face_v4.py 가 점을 직접 찍어 고친다.
실행 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/build_v3.py
"""
import os
import numpy as np
from collections import Counter
from PIL import Image, ImageDraw

from build_stall import RAW, OUT, REF, font
from build_v2 import HAIR, SHIRT, lum, arr, img

PREVIEW = os.path.join(REF, '시안', '2026-10-04-가판대손님-3차-미리보기.png')
PREVIEW_C = os.path.join(REF, '시안', '2026-10-04-가판대손님-3차c-턱입-미리보기.png')
PREVIEW_B = os.path.join(REF, '시안', '2026-10-04-가판대손님-3차b-기쁨표정-미리보기.png')
PREVIEW_D = os.path.join(REF, '시안', '2026-10-04-가판대손님-3차d-손수정-미리보기.png')
CW, CH = 120, 240           # 모든 손님 겹의 캔버스
PIVOT = (66, 240)           # 발끝 = (0.55, 0). 2차와 같다

# 겹 이름 → raw 파일 (모두 같은 120×240 캔버스 · 같은 자리에서 그려 받음)
SRC = {
    'body':    'cust3_body_22101',        # 대머리 · 얼굴 없음 · 흰 반팔 · 청바지 · 운동화
    'shirt':   'cust3_hoodie_iso_22202',  # 후드 (밝은 회색) — 몸에 입힌 뒤 후드만 남기게 다시 그림
    # 3d : 머리통은 v2 판(3차 · 3차b 통과 판)으로 되돌리고, 턱 밑 · 입은 fix_face_v4.py 가 손으로 고친다.
    #      3c 판(cust3_headN_22904 · headH_22911 · headS_22921)은 퇴짜 — raw/cust3_face_0?_v3c.png 에 남김.
    'face_01': 'cust3_head1_iso_22811',   # 머리통 + 보통 = raw/cust3_face_01_v2.png
    'face_02': 'cust3_head2b_iso_22833',  # 머리통 + 기쁨 (3차b) = raw/cust3_face_02_v2.png
    'face_03': 'cust3_head3_iso_22813',   # 머리통 + 시무룩 = raw/cust3_face_03_v2.png
    'hair_01': 'cust3_hairB_iso_22302',   # 단발 (밝은 회색)
    'hair_02': 'cust3_hairC_iso_22503',   # 뭉게 곱슬 (밝은 회색)
}
ORDER = ['body', 'shirt', 'face', 'hair']   # 아래 → 위

DARK = 70          # 이 밝기 아래는 외곽선(0 단)
# 밝은 회색 겹을 6 단으로 : 가장 많은 회색(바탕) 밝기와의 차이로 가른다
D_DEEP, D_SHADE, D_LIGHT, D_SHINE = -70, -22, 7, 15


def load(n):
    return arr(Image.open(os.path.join(RAW, n + '.png')))


def drop_crumbs(a, min_px=6):
    """떨어진 작은 덩이(min_px 점 미만, 8 방향 이웃)를 지운다 — 칠하지 않고 지우기만 한다."""
    m = a[..., 3] > 0
    seen = np.zeros_like(m); removed = 0
    H, W = m.shape
    for y0, x0 in zip(*np.nonzero(m)):
        if seen[y0, x0]: continue
        st = [(y0, x0)]; seen[y0, x0] = True; comp = []
        while st:
            y, x = st.pop(); comp.append((y, x))
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < H and 0 <= xx < W and m[yy, xx] and not seen[yy, xx]:
                        seen[yy, xx] = True; st.append((yy, xx))
        if len(comp) < min_px:
            for y, x in comp: a[y, x] = 0
            removed += len(comp)
    return a, removed


def shift(a, dx, dy):
    out = np.zeros_like(a)
    H, W = a.shape[:2]
    ys, xs = np.nonzero(a[..., 3])
    ny, nx = ys + dy, xs + dx
    ok = (ny >= 0) & (ny < H) & (nx >= 0) & (nx < W)
    out[ny[ok], nx[ok]] = a[ys[ok], xs[ok]]
    return out, int((~ok).sum())


def grey_steps(a):
    """밝은 회색 겹의 점을 6 단 번호로 (0 외곽선 · 1 짙음 · 2 그늘 · 3 바탕 · 4 밝음 · 5 빛)."""
    m = a[..., 3] > 0
    L = 0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2]
    idx = np.full(m.shape, -1)
    out = m & (L < DARK)
    rest = m & ~out
    base = Counter(map(tuple, a[rest][:, :3])).most_common(1)[0][0]
    d = L - lum(base)
    idx[out] = 0
    idx[rest & (d < D_DEEP)] = 1
    idx[rest & (d >= D_DEEP) & (d < D_SHADE)] = 2
    idx[rest & (d >= D_SHADE) & (d <= D_LIGHT)] = 3
    idx[rest & (d > D_LIGHT) & (d <= D_SHINE)] = 4
    idx[rest & (d > D_SHINE)] = 5
    return idx


def bake(idx, rmp):
    out = np.zeros(idx.shape + (4,), int)
    for k in range(6):
        out[idx == k, :3] = rmp[k]; out[idx == k, 3] = 255
    return out


def build():
    layers, notes = {}, []
    for k, n in SRC.items():
        a = load(n)
        a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)      # 알파는 0 또는 255 만
        a[a[..., 3] == 0] = 0
        a, rm = drop_crumbs(a)
        layers[k] = a; notes.append(f'{k}: 부스러기 {rm} 점 지움')
    # 발끝 가운데를 피벗 x 66 · 맨 아래 줄에 맞춘다 (모든 겹을 같은 만큼 옮김)
    b = layers['body']; ys, xs = np.nonzero(b[..., 3])
    foot = xs[ys >= ys.max() - 3]
    dx = PIVOT[0] - int(round((foot.min() + foot.max()) / 2)); dy = CH - 1 - ys.max()
    for k in layers:
        layers[k], lost = shift(layers[k], dx, dy)
        if lost: notes.append(f'{k}: 옮기며 캔버스 밖으로 {lost} 점')
    notes.append(f'옮김 dx={dx} dy={dy}')
    return layers, notes


def save_all(layers):
    files = []
    def put(name, a):
        p = os.path.join(OUT, name + '.png'); img(a).save(p); files.append(p)
    put('cust3_side_body', layers['body'])
    for f in ('01', '02', '03'): put(f'cust3_side_face_{f}', layers[f'face_{f}'])
    put('cust3_side_shirt_base', layers['shirt'])       # 회색 — 코드가 색을 곱해 입힐 때
    si = grey_steps(layers['shirt'])
    for c, r in SHIRT.items(): put(f'cust3_side_shirt_{c}', bake(si, r))
    for h in ('01', '02'):
        a = layers[f'hair_{h}']; put(f'cust3_side_hair_{h}_base', a)
        hi = grey_steps(a)
        for c, r in HAIR.items(): put(f'cust3_side_hair_{h}_{c}', bake(hi, r))
    return files


def L(name): return Image.open(os.path.join(OUT, name + '.png')).convert('RGBA')


def cust(face='01', hair='01', hc='brown', sc='blue'):
    c = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
    for n in ('cust3_side_body', f'cust3_side_shirt_{sc}', f'cust3_side_face_{face}', f'cust3_side_hair_{hair}_{hc}'):
        c.alpha_composite(L(n))
    return c


def up(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)


def preview():
    BG = (40, 40, 48, 255); CRM = (245, 235, 215, 255)
    combos = [('01', '01', 'brown', 'blue'), ('02', '02', 'black', 'red'), ('03', '01', 'orange', 'green'),
              ('01', '02', 'purple', 'yellow'), ('02', '01', 'black', 'red'), ('03', '02', 'brown', 'blue')]
    W = 1780
    sheet = Image.new('RGBA', (W, 2780), BG); d = ImageDraw.Draw(sheet)
    d.text((20, 14), '가판대 손님 3차 - 파츠를 따로 그려 받음 (몸 · 윗옷 · 머리통+표정 · 머리카락)', font=font(26), fill=CRM)
    # 1) ×4 : 겹 낱장 (회색 원본)
    y = 60; d.text((20, y), '① 겹 낱장 ×2 (body · shirt_base · face_01~03 · hair_01_base · hair_02_base)', font=font(18), fill=CRM); y += 28
    x = 20
    for n in ('cust3_side_body', 'cust3_side_shirt_base', 'cust3_side_face_01', 'cust3_side_face_02', 'cust3_side_face_03',
              'cust3_side_hair_01_base', 'cust3_side_hair_02_base'):
        tile = Image.new('RGBA', (CW * 2, CH * 2), (60, 60, 70, 255)); tile.alpha_composite(up(L(n), 2))
        sheet.alpha_composite(tile, (x, y)); x += CW * 2 + 6
    y += CH * 2 + 20
    # 2) ×4 조합 셋 (머리 위쪽 절반만)
    d.text((20, y), '② ×4 조합 (머리 2 × 표정 3 · 옷색 셋) - 위 130 줄', font=font(18), fill=CRM); y += 28
    x = 20
    for f, h, hc, sc in combos[:3]:
        sheet.alpha_composite(up(cust(f, h, hc, sc).crop((0, 20, 120, 150)), 4), (x, y)); x += 480 + 40
    y += 520 + 10
    x = 20
    for f, h, hc, sc in combos[3:]:
        sheet.alpha_composite(up(cust(f, h, hc, sc).crop((0, 20, 120, 150)), 4), (x, y)); x += 480 + 40
    y += 520 + 20
    # 3) ×1 어두운 바탕 줄 + 가판대 배경 위 (주인 · 계산대와 나란히)
    d.text((20, y), '③ ×1 어두운 바탕 (6 조합)          ④ ×1 가판대 배경 B 위 - 주인 v2 · 계산대 3 · 진열장 2 와 나란히 (세계 y 480~1100 을 잘라 봄)', font=font(18), fill=CRM); y += 28
    x = 20
    for f, h, hc, sc in combos:
        sheet.alpha_composite(cust(f, h, hc, sc), (x, y)); x += 125
    # 가판대 장면 (build_v2 의 2차 자리 값)
    bg = Image.open(os.path.join(OUT, 'stall_bg_b.png')).convert('RGBA')
    sc_ = bg.copy()
    sc_.alpha_composite(Image.open(os.path.join(OUT, 'owner_v2.png')).convert('RGBA'), (206, 518))
    sc_.alpha_composite(Image.open(os.path.join(OUT, 'stall_counter3_brown.png')).convert('RGBA').crop((17, 0, 397, 122)), (0, 660))
    sc_.alpha_composite(Image.open(os.path.join(OUT, 'display_case2.png')).convert('RGBA'), (8, 559))
    a = cust('02', '01', 'brown', 'blue'); sc_.alpha_composite(a, (150 - PIVOT[0], 1060 - CH))
    bb = cust('03', '02', 'black', 'red').transpose(Image.FLIP_LEFT_RIGHT); sc_.alpha_composite(bb, (410 - (CW - PIVOT[0]), 1060 - CH))
    cc = cust('01', '02', 'orange', 'green').transpose(Image.FLIP_LEFT_RIGHT); sc_.alpha_composite(cc, (520 - (CW - PIVOT[0]), 1060 - CH))
    crop = sc_.crop((0, 480, 540, 1100))
    sheet.alpha_composite(crop, (W - 540 - 20, y))
    y2 = y + CH + 20
    d.text((20, y2), '⑤ ×3 가판대 위 손님 A (기쁨) · B (flipX · 시무룩) - 온몸 · 손목 · 옷단 · 목 이음 보기', font=font(18), fill=CRM)
    sheet.alpha_composite(up(sc_.crop((95, 815, 215, 1065)), 3), (20, y2 + 28))
    sheet.alpha_composite(up(sc_.crop((355, 815, 475, 1065)), 3), (20 + 380, y2 + 28))
    sheet.convert('RGB').save(PREVIEW)
    return PREVIEW


def check():
    """통과 기준 ① · ② 를 잰다."""
    rep = []
    names = sorted(f[:-4] for f in os.listdir(OUT) if f.startswith('cust3_side_'))
    for n in names:
        a = np.array(L(n)); al = a[..., 3]
        semi = int(((al > 0) & (al < 255)).sum())
        cols = len(set(map(tuple, a[al > 0][:, :3])))
        # 2×2 덩이 검사 : 짝수 격자 2×2 칸이 모두 같은 색인 비율 (2 배로 키운 그림이면 1.0 에 가깝다)
        q = a[:CH // 2 * 2, :CW // 2 * 2].astype(int)
        blk = (q[0::2, 0::2] == q[1::2, 0::2]).all(-1) & (q[0::2, 0::2] == q[0::2, 1::2]).all(-1) & (q[0::2, 0::2] == q[1::2, 1::2]).all(-1)
        filled = q[0::2, 0::2, 3] > 0
        ratio = float(blk[filled].mean()) if filled.any() else 0
        rep.append(f'{n}: 반투명 {semi} · 색 {cols} · 2x2 같은색 비율 {ratio:.2f}')
    # 겹 틈 : 몸 · 얼굴 · 머리 둘레 바깥으로 튀는 점(다른 겹이 어디에도 안 닿는 섬) 은 drop_crumbs 로 0. 몸 머리 줄 덮임
    body = np.array(L('cust3_side_body'))[..., 3] > 0
    for f in ('01', '02', '03'):
        fa = np.array(L(f'cust3_side_face_{f}'))[..., 3] > 0
        ys = np.nonzero(fa)[0]
        head_rows = body.copy(); head_rows[ys.max() - 6:] = False   # 턱 아래 목은 몸이 맡는다
        rep.append(f'face_{f}: 몸의 머리 점 중 얼굴 겹이 안 덮는 점 {int((head_rows & ~fa).sum())} · 몸 밖으로 나온 점 {int((fa & ~body).sum())}')
    return rep


def preview_b():
    """3b : 기쁨 표정 다시 뽑기 판정용 — 옛 · 새 ×6, 머리 둘 ×4 조합, ×1 실제 크기."""
    BG = (40, 40, 48, 255); CRM = (245, 235, 215, 255)
    v1, _ = shift(np.array(Image.open(os.path.join(RAW, 'cust3_face_02_v1.png')).convert('RGBA')).astype(int), 0, 0)
    alt, _ = shift(load('cust3_head2b_iso_22832'), 8, 1)
    faces = [('보통 face_01', L('cust3_side_face_01')), ('옛 기쁨 v1', img(v1)), ('새 기쁨 (고름 · 22833)', L('cust3_side_face_02')),
             ('버린 후보 22832', img(alt)), ('시무룩 face_03', L('cust3_side_face_03'))]
    W = 1780; sheet = Image.new('RGBA', (W, 1500), BG); d = ImageDraw.Draw(sheet)
    d.text((20, 14), '가판대 손님 3차b - 기쁨 표정 다시 뽑기 (가는 1px 감은 눈 · 옆얼굴선 안쪽 작은 미소)', font=font(26), fill=CRM)
    y = 60; d.text((20, y), '① 얼굴 ×6 (머리통 겹 x 66~111 · y 60~120)', font=font(18), fill=CRM); y += 28
    x = 20
    for cap, f in faces:
        t = Image.new('RGBA', (45, 60), (60, 60, 70, 255)); t.alpha_composite(f.crop((66, 60, 111, 120)))
        sheet.alpha_composite(up(t, 6), (x, y)); d.text((x, y + 366), cap, font=font(16), fill=CRM); x += 270 + 80
    y += 400
    d.text((20, y), '② ×4 머리 둘을 씌운 조합 (옛 기쁨 · 새 기쁨)', font=font(18), fill=CRM); y += 28
    x = 20
    for f, h, hc, sc in (('v1', '01', 'brown', 'blue'), ('02', '01', 'brown', 'blue'), ('v1', '02', 'black', 'red'), ('02', '02', 'black', 'red')):
        c = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
        for n in ('cust3_side_body', f'cust3_side_shirt_{sc}'):
            c.alpha_composite(L(n))
        c.alpha_composite(img(v1) if f == 'v1' else L('cust3_side_face_02'))
        c.alpha_composite(L(f'cust3_side_hair_{h}_{hc}'))
        sheet.alpha_composite(up(c.crop((20, 20, 120, 120)), 4), (x, y)); d.text((x, y + 404), '옛' if f == 'v1' else '새', font=font(18), fill=CRM)
        x += 400 + 30
    y += 440
    d.text((20, y), '③ ×1 실제 크기 (보통 · 새 기쁨 · 시무룩 × 머리 둘) · 가판대 배경 위', font=font(18), fill=CRM); y += 28
    src = Image.open(os.path.join(OUT, 'stall_bg_b.png')).convert('RGBA').crop((0, 820, 540, 1070))
    bg = Image.new('RGBA', (760, 250)); bg.alpha_composite(src, (0, 0)); bg.alpha_composite(src.crop((0, 0, 220, 250)), (540, 0))
    x = 10
    for h, hc, sc in (('01', 'brown', 'blue'), ('02', 'black', 'red')):
        for f in ('01', '02', '03'):
            bg.alpha_composite(cust(f, h, hc, sc), (x, 0)); x += 124
    sheet.alpha_composite(bg, (20, y))
    sheet.convert('RGB').save(PREVIEW_B)
    return PREVIEW_B


def preview_c():
    """3c : 머리통 셋의 턱 밑 · 입 다시 뽑기 — 옛(v2) · 새 ×8, 머리 둘 × 표정 셋 ×4, ×1 마루 위."""
    BG = (40, 40, 48, 255); CRM = (245, 235, 215, 255)
    old = {f: Image.open(os.path.join(RAW, f'cust3_face_{f}_v2.png')).convert('RGBA') for f in ('01', '02', '03')}
    W = 1780; sheet = Image.new('RGBA', (W, 2600), BG); d = ImageDraw.Draw(sheet)
    d.text((20, 14), '가판대 손님 3차c - 머리통 셋의 턱 밑 가장자리 · 입 다시 뽑기', font=font(26), fill=CRM)
    y = 60; d.text((20, y), '① 얼굴 ×8 (x 66~111 · y 60~122 - 3차b 네모 자리와 같은 구도). 윗줄 옛(v2) · 아랫줄 새', font=font(18), fill=CRM); y += 28
    for row, getf in (('옛', lambda f: old[f]), ('새', lambda f: L(f'cust3_side_face_{f}'))):
        x = 20
        for f, cap in (('01', '보통'), ('02', '기쁨'), ('03', '시무룩')):
            t = Image.new('RGBA', (45, 62), (60, 60, 70, 255)); t.alpha_composite(getf(f).crop((66, 60, 111, 122)))
            sheet.alpha_composite(up(t, 8), (x, y)); d.text((x, y + 500), f'{row} {cap} face_{f}', font=font(16), fill=CRM); x += 360 + 40
        y += 530
    d.text((20, y), '② ×4 머리 둘 × 표정 셋 (옷 · 몸 위에 겹친 모습)', font=font(18), fill=CRM); y += 28
    x = 20
    for h, hc, sc in (('01', 'brown', 'blue'), ('02', 'black', 'red')):
        for f in ('01', '02', '03'):
            sheet.alpha_composite(up(cust(f, h, hc, sc).crop((30, 30, 115, 125)), 4), (x, y)); x += 340 + 8
        x = 20; y += 388
    y += 10
    d.text((20, y), '③ ×1 가판대 마루 위 (보통 · 기쁨 · 시무룩 × 머리 둘)', font=font(18), fill=CRM); y += 28
    src = Image.open(os.path.join(OUT, 'stall_bg_b.png')).convert('RGBA').crop((0, 820, 540, 1070))
    bg = Image.new('RGBA', (760, 250)); bg.alpha_composite(src, (0, 0)); bg.alpha_composite(src.crop((0, 0, 220, 250)), (540, 0))
    x = 10
    for h, hc, sc in (('01', 'brown', 'blue'), ('02', 'black', 'red')):
        for f in ('01', '02', '03'):
            bg.alpha_composite(cust(f, h, hc, sc), (x, 0)); x += 124
    sheet.alpha_composite(bg, (20, y))
    sheet = sheet.crop((0, 0, W, y + 270))
    sheet.convert('RGB').save(PREVIEW_C)
    return PREVIEW_C


def preview_d():
    """3d : 턱 밑 · 입 손수정 — 고치기 전(v2) · 후 ×8, 머리 둘 × 표정 셋 ×4, ×1 마루 위."""
    BG = (40, 40, 48, 255); CRM = (245, 235, 215, 255)
    old = {f: Image.open(os.path.join(RAW, f'cust3_face_{f}_v2.png')).convert('RGBA') for f in ('01', '02', '03')}
    box = (34, 84, 104, 112)                     # 귀 아래 ~ 턱 끝, 입 높이 ~ 목 잘린 줄 아래까지
    bw, bh = (box[2] - box[0]) * 8, (box[3] - box[1]) * 8
    W = 1780; sheet = Image.new('RGBA', (W, 3000), BG); d = ImageDraw.Draw(sheet)
    d.text((20, 14), '가판대 손님 3차d - 머리통 셋의 턱 밑 · 입 손수정 (바탕 v2)', font=font(26), fill=CRM)
    y = 60; d.text((20, y), f'① 얼굴 ×8 (x {box[0]}~{box[2] - 1} · y {box[1]}~{box[3] - 1}). 왼쪽 고치기 전(v2) · 오른쪽 고친 뒤', font=font(18), fill=CRM); y += 28
    for f, cap in (('01', '보통'), ('02', '기쁨'), ('03', '시무룩')):
        x = 20
        for tag, im in (('전', old[f]), ('후', L(f'cust3_side_face_{f}'))):
            t = Image.new('RGBA', (box[2] - box[0], box[3] - box[1]), (60, 60, 70, 255)); t.alpha_composite(im.crop(box))
            sheet.alpha_composite(up(t, 8), (x, y)); d.text((x, y + bh + 2), f'{tag} {cap} face_{f}', font=font(16), fill=CRM)
            x += bw + 40
        y += bh + 28
    d.text((20, y), '② ×4 머리 둘 × 표정 셋 (옷 · 몸 위에 겹친 모습)', font=font(18), fill=CRM); y += 28
    for h, hc, sc in (('01', 'brown', 'blue'), ('02', 'black', 'red')):
        x = 20
        for f in ('01', '02', '03'):
            sheet.alpha_composite(up(cust(f, h, hc, sc).crop((30, 30, 115, 125)), 4), (x, y)); x += 340 + 8
        y += 388
    y += 10
    d.text((20, y), '③ ×1 가판대 마루 위 (보통 · 기쁨 · 시무룩 × 머리 둘)', font=font(18), fill=CRM); y += 28
    src = Image.open(os.path.join(OUT, 'stall_bg_b.png')).convert('RGBA').crop((0, 820, 540, 1070))
    bg = Image.new('RGBA', (760, 250)); bg.alpha_composite(src, (0, 0)); bg.alpha_composite(src.crop((0, 0, 220, 250)), (540, 0))
    x = 10
    for h, hc, sc in (('01', 'brown', 'blue'), ('02', 'black', 'red')):
        for f in ('01', '02', '03'):
            bg.alpha_composite(cust(f, h, hc, sc), (x, 0)); x += 124
    sheet.alpha_composite(bg, (20, y))
    sheet = sheet.crop((0, 0, W, y + 270))
    sheet.convert('RGB').save(PREVIEW_D)
    return PREVIEW_D

if __name__ == '__main__':
    layers, notes = build()
    files = save_all(layers)
    print('\n'.join(notes))
    print(len(files), 'files')
    import fix_face_v4          # 3d : 머리통 셋의 턱 밑 · 입 손수정 (사용자 허락 · 범위 그 둘뿐)
    fix_face_v4.main()
    print('\n'.join(check()))
    # 3차 · 3차b 미리보기는 사용자가 빨간 네모를 그려 놓은 판, 3차c 는 퇴짜 판 기록이라 다시 쓰지 않는다 (2026-10-04).
    # print(preview()); print(preview_b()); print(preview_c())
    print(preview_d())

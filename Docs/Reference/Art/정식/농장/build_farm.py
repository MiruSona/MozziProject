# v15 정식 그림 — 농장 갈래 (2026-10-03 반복 03)
# raw/(PixelLab 원본) 에서 out/(게임에 넣을 png) 을 만들고, 판정용 그림 둘을 Docs/Reference/Art/시안/ 에 굽는다.
# PIL 은 자르기 · 배치 · 정수배 확대 · 글자 · 배경 지우기 · 단색 바꾸기만 한다. 새 그림을 칠하지 않는다.
#   예외 하나 : 크기 견주기 그림의 축소(64→40 · 64→32 · ×1.143)는 「게임이 하는 축소 흉내」라 최근접으로 줄인다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/build_farm.py
import os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw'); OUTP = os.path.join(HERE, 'out'); os.makedirs(OUTP, exist_ok=True)
REF = os.path.join(ROOT, 'Docs', 'Reference', 'Art')
SIAN = os.path.join(REF, '시안')
GAME = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art', 'Mozzi')   # 읽기만
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
BG = (58, 50, 48, 255); TITLE = (255, 240, 220); SUB = (230, 200, 170); TXT = (240, 225, 205)
LINE = (110, 110, 110)


def op(p): return Image.open(p).convert('RGBA')
def raw(n): return op(os.path.join(RAW, n + '.png'))
def game(n): return op(os.path.join(GAME, n + '.png'))
def font(s): return ImageFont.truetype(FONT, s)
def save(im, n): im.save(os.path.join(OUTP, n + '.png'))


# ---------- 1. 잔디 바탕 ----------
# grass_base_9104 를 반 칸(128,128) 밀고(workbench copy_region) 가운데 십자 16px 를 inpaint_image_pro_flash(9111)로 고친 것.
GRASS = raw('grass_base_9104_shift_inpaint_9111').convert('RGBA')
FIELD_GA = op(os.path.join(REF, '메인화면', 'v4', 'parts', 'grass_field_256.png'))


def tile_ground(t, w, h):
    im = Image.new('RGBA', (w, h))
    for y in range(0, h, t.height):
        for x in range(0, w, t.width): im.paste(t, (x, y))
    return im


# ---------- 2. 장식 조각 ----------
def groups(im, gap=3):
    """투명 바탕 묶음 그림에서 조각을 떼어 낸다. gap px 안에 붙은 조각(돌 + 잔돌 등)은 한 조각으로 본다."""
    a = np.array(im)[..., 3] > 0; h, w = a.shape
    d = a.copy()
    for _ in range(gap):                                           # 가까운 조각을 잇기 위한 가림판 넓히기 (그림에는 안 씀)
        p = np.pad(d, 1); d = p[1:-1, 1:-1] | p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
    seen = np.zeros_like(d); out = []
    for y in range(h):
        for x in range(w):
            if d[y, x] and not seen[y, x]:
                st = [(y, x)]; seen[y, x] = True; pts = []
                while st:
                    cy, cx = st.pop(); pts.append((cy, cx))
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < h and 0 <= nx < w and d[ny, nx] and not seen[ny, nx]: seen[ny, nx] = True; st.append((ny, nx))
                ys, xs = zip(*pts); m = np.zeros_like(a); m[list(ys), list(xs)] = True; m &= a
                if m.sum() < 12: continue
                yy, xx = np.where(m); x0, y0, x1, y1 = xx.min(), yy.min(), xx.max() + 1, yy.max() + 1
                piece = np.array(im)[y0:y1, x0:x1].copy(); piece[~m[y0:y1, x0:x1]] = 0
                out.append((y0 // 28, x0, Image.fromarray(piece)))
    return [g[2] for g in sorted(out, key=lambda g: (g[0], g[1]))]


def v4raw(n): return op(os.path.join(REF, '메인화면', 'v4', 'raw', n + '.png'))   # 「가」 벌 PixelLab 원본 (읽기만)


DEC_ALL = groups(v4raw('decals_8401'))          # 원본 순서 : 풀포기 4 · 흰꽃 2 · 분홍꽃 1 · 토끼풀 1 · 토끼풀 덤불 1 · 돌 3 · 버섯 1
# 뺀 것 : 토끼풀(7) · 토끼풀 덤불(8) — 풀색 위에서 진한 초록 얼룩으로만 보인다 (README).
KEEP = [(0, '풀포기'), (1, '풀포기'), (2, '풀포기'), (3, '풀포기'), (4, '흰꽃'), (5, '흰꽃'), (6, '분홍꽃'), (9, '돌'), (10, '돌'), (11, '돌'), (12, '버섯')]


def pad32(p):
    """32×32 투명 칸의 가운데(가로) · 아래(세로, 바닥 1px 띄움)에 놓는다."""
    c = Image.new('RGBA', (32, 32)); c.alpha_composite(p, ((32 - p.width) // 2, 31 - p.height)); return c


DECOR = []
for i, (k, kind) in enumerate(KEEP):
    p = DEC_ALL[k]; assert p.width <= 32 and p.height <= 31, (k, p.size)
    DECOR.append((kind, pad32(p)))

# ---------- 3. 울타리 ----------
# 「가」 벌 통나무 울타리 한 바퀴(fence_ring_8201)에서 잘라 32 격자에 맞춘다. 좌표는 원본 그림 안.
RING = v4raw('fence_ring_8201')
FENCE_H = RING.crop((104, 29, 136, 65))                         # 가로대 17px(104~120) + 기둥 15px(121~135) = 32×36
FENCE_POST = Image.new('RGBA', (32, 36)); FENCE_POST.alpha_composite(RING.crop((27, 29, 44, 65)), (15, 0))   # 모서리 기둥 17px 을 마디 기둥 자리(오른쪽)에
v = RING.crop((12, 102, 44, 139))                               # 세로 한 마디(37줄) : 덮개 102 ~ 다음 덮개 앞 138
FENCE_V = Image.new('RGBA', (32, 32)); FENCE_V.paste(v.crop((0, 0, 32, 9)), (0, 0)); FENCE_V.paste(v.crop((0, 14, 32, 37)), (0, 9))   # 똑같은 줄(111~115) 5줄을 빼 32 로


def fence(n, m):
    """가로 n 칸 · 세로 m 칸 울타리. (뒤 겹, 앞 겹) 을 돌려준다. 칸 0 · n-1 이 모서리 기둥 칸이다. 기둥 그림(36 높이)은 칸 위로 4px 솟는다."""
    W, H = n * 32, m * 32 + 4
    back = Image.new('RGBA', (W, H)); front = Image.new('RGBA', (W, H))
    for j in range(1, m - 1):                                    # 양옆 세로 (뒤 겹 : 실제 게임은 y 정렬)
        back.alpha_composite(FENCE_V, (0, 4 + j * 32)); back.alpha_composite(FENCE_V, ((n - 1) * 32, 4 + j * 32))
    for (dst, row) in ((back, 0), (front, m - 1)):
        y = row * 32
        dst.alpha_composite(FENCE_POST, (0, y))
        for i in range(1, n): dst.alpha_composite(FENCE_H, (i * 32, y))
        dst.alpha_composite(FENCE_POST, ((n - 1) * 32, y))       # 마지막 마디 기둥 위에 모서리 기둥을 덮는다
    return back, front


# ---------- 4. 찹쌀이 몸 ----------
BODY_SRC = [raw('body_hop0_edit_9401')] + [op(os.path.join(REF, '메인화면', 'v3', 'parts', 'mozzi_body_hop%d.png' % i)) for i in (1, 2, 3)]


def edge_mask(al):
    p = np.pad(al, 1); inner = p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]; return al & ~inner


def line110(im):
    """맨 바깥 1px(4 이웃 중 하나라도 투명) 픽셀을 회색 110 한 값으로 바꾼다. 윤곽(알파)은 그대로."""
    a = np.array(im); e = edge_mask(a[..., 3] > 0); a[e, :3] = LINE; return Image.fromarray(a)


BODIES = [line110(b) for b in BODY_SRC]
CHEEK = game('cheek')


def mozzi(body, hexc, face, crown=False):
    c = tuple(int(hexc[i:i + 2], 16) for i in (1, 3, 5)); a = np.array(body).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    im = Image.fromarray(a.astype('uint8')); im.alpha_composite(game('face_%02d' % face))
    im.alpha_composite(CHEEK, (14, 34)); im.alpha_composite(CHEEK, (39, 34))
    if crown: im.alpha_composite(game('acc_crown'))
    return im


def check_bodies():
    rows = []
    for i, (s, b) in enumerate(zip(BODY_SRC, BODIES)):
        g = np.array(game('body_hop%d' % i)); n = np.array(b); src = np.array(s)
        diff_game = int(((g[..., 3] > 0) != (n[..., 3] > 0)).sum()); diff_src = int(((src[..., 3] > 0) != (n[..., 3] > 0)).sum())
        e = edge_mask(n[..., 3] > 0); vals = sorted(set(map(tuple, n[e, :3].tolist())))
        rows.append((i, diff_game, diff_src, int(e.sum()), vals))
    return rows


# ---------- 판정 그림 공용 ----------
FARM_W, FARM_H = 540, 480
FN, FM = 16, 14                                                 # 울타리 16 × 14 칸 = 512 × 452
FX, FY = (FARM_W - FN * 32) // 2, 14


def farm(ground, decor_seed=None, mozzis=(), scale_fn=None):
    im = ground.copy()
    if decor_seed is not None:                                  # 장식 : 32 칸마다 22% 확률로 하나 (울타리 안쪽 칸만)
        rnd = random.Random(decor_seed)
        weights = {'풀포기': 0.55, '흰꽃': 0.15, '분홍꽃': 0.08, '돌': 0.17, '버섯': 0.05}
        kinds = list(weights); w = [weights[k] for k in kinds]
        for cy in range(1, FM - 1):
            for cx in range(1, FN - 1):
                if rnd.random() < 0.22:
                    k = rnd.choices(kinds, w)[0]; p = rnd.choice([d for (kk, d) in DECOR if kk == k])
                    if rnd.random() < 0.5: p = ImageOps.mirror(p)
                    im.alpha_composite(p, (FX + cx * 32, FY + 4 + cy * 32))
    back, front = fence(FN, FM); im.alpha_composite(back, (FX, FY))
    for (body, hexc, face, crown, x, y) in sorted(mozzis, key=lambda m: m[5]):
        m = mozzi(body, hexc, face, crown)
        if scale_fn: m = scale_fn(m)
        im.alpha_composite(m, (x - m.width // 2, y - m.height))   # (x, y) = 발 밑 가운데
    im.alpha_composite(front, (FX, FY))
    return im


SPECIES = ['#FFFFFF', '#4FA84A', '#7FC8F8', '#F5A05A', '#FFFFFF', '#3F9BE0', '#E58FB0', '#2E7D32', '#FFFFFF', '#C9A0E8', '#F2D24B', '#4FA84A']


def herd(n, seed):
    rnd = random.Random(seed); out = []
    cols, rows = 4, 3
    for i in range(n):
        cx = FX + 64 + (i % cols) * 120 + rnd.randint(-18, 18); cy = FY + 130 + (i // cols) * 120 + rnd.randint(-14, 14)
        out.append((BODIES[rnd.choice([0, 0, 0, 1, 2, 3])], SPECIES[i % len(SPECIES)], rnd.choice([1, 2, 3, 5, 7]), rnd.random() < 0.3, cx, cy))
    return out


def header(c, title, lines, y0=24):
    d = ImageDraw.Draw(c); d.text((40, y0), title, font=font(40), fill=TITLE); y = y0 + 64
    for ln in lines: d.text((40, y), ln, font=font(24), fill=TXT); y += 36
    return y


# ---------- 판정 그림 1 : 잔디 바탕과 장식 ----------
def board_grass():
    S = 2; pw, ph = FARM_W * S, FARM_H * S; M = 40
    mz = [(BODIES[0], '#FFFFFF', 1, False, 150, 200), (BODIES[0], '#4FA84A', 2, True, 330, 170), (BODIES[2], '#7FC8F8', 1, True, 250, 330), (BODIES[1], '#F5A05A', 5, False, 420, 380)]
    left = farm(tile_ground(FIELD_GA, FARM_W, FARM_H), None, mz)
    right = farm(tile_ground(GRASS, FARM_W, FARM_H), 20261003, mz)
    c = Image.new('RGBA', (M * 3 + pw * 2, 260 + ph + 60 + 1100), BG)
    top = header(c, '농장 잔디 : 「가」 풀밭 한 장  vs  새 바탕 + 장식 조각 (×2) · 2026-10-03',
                 ['오른쪽 = 무늬 약한 바탕 256×256 한 장을 되풀이 + 장식 조각(풀포기 · 꽃 · 돌 · 버섯)을 32 칸에 22% 로 흩음. 울타리는 둘 다 같은 새 32 마디.',
                  '전부 PixelLab 그림이고 PIL 은 자르기 · 배치 · 확대만 했다. 농장 540×480, 울타리 16×14 칸.'])
    d = ImageDraw.Draw(c); y = top + 10
    d.text((M, y), '왼쪽 : 「가」 grass_field_256 (얼룩 · 꽃 · 풀포기가 바탕에 그려짐)', font=font(26), fill=SUB)
    d.text((M * 2 + pw, y), '오른쪽 : 새 바탕 grass_base + 장식 decor_00~10', font=font(26), fill=SUB)
    y += 44
    c.alpha_composite(left.resize((pw, ph), Image.NEAREST), (M, y)); c.alpha_composite(right.resize((pw, ph), Image.NEAREST), (M * 2 + pw, y))
    y += ph + 40
    # 아래 줄 : 바탕 2×2 이음매 확인 (×2) · 장식 조각 표 (×4) · 울타리 마디 (×4)
    d.text((M, y), '새 바탕을 2×2 로 깐 것 (×2) : 가운데 십자가 256 이음매', font=font(24), fill=SUB)
    g2 = tile_ground(GRASS, 512, 512).resize((1024, 1024), Image.NEAREST)
    c.alpha_composite(g2, (M, y + 40))
    x2 = M + 1024 + 60
    d.text((x2, y), '장식 조각 decor_00~10 (×4 · 32 칸 테두리 표시)', font=font(24), fill=SUB)
    for i, (kind, p) in enumerate(DECOR):
        cx = x2 + (i % 6) * 150; cy = y + 50 + (i // 6) * 190
        cell = tile_ground(GRASS, 32, 32); cell.alpha_composite(p)
        big = cell.resize((128, 128), Image.NEAREST); bd = ImageDraw.Draw(big); bd.rectangle((0, 0, 127, 127), outline=(255, 255, 255, 90))
        c.alpha_composite(big, (cx, cy)); d.text((cx, cy + 134), 'decor_%02d %s' % (i, kind), font=font(18), fill=TXT)
    yf = y + 50 + 2 * 190 + 20
    d.text((x2, yf), '울타리 마디 (×4) : fence_post · fence_h · fence_v · 3×3 짜임', font=font(24), fill=SUB)
    xx = x2
    for p in (FENCE_POST, FENCE_H, FENCE_V):
        cell = tile_ground(GRASS, 32, 36); cell.alpha_composite(p)
        c.alpha_composite(cell.resize((128, 144), Image.NEAREST), (xx, yf + 44)); xx += 150
    b, f = fence(4, 4); g = tile_ground(GRASS, 128, 132); g.alpha_composite(b); g.alpha_composite(f)
    c.alpha_composite(g.resize((384, 396), Image.NEAREST), (xx + 10, yf + 44))
    yl = yf + 44 + 396 + 30
    for ln in ['뺀 장식 : 토끼풀 · 토끼풀 덤불 : 풀색 위에서 진한 초록 얼룩으로만 보인다.',
               '바탕 이음매 : 오른쪽 끝 ↔ 왼쪽 끝은 원래 그림 안쪽이라 이어진다 (반 칸 밀기).',
               '  밀고 나서 생긴 가운데 십자(16px 폭)는 PixelLab inpaint 로 다시 그렸다 : 조금 밝다 (밝기 +2).',
               '울타리 : 가로 마디 = 가로대 17 + 기둥 15 = 32. 세로 마디 = 원본 37줄에서 같은 줄 5줄을 빼 32.']:
        d.text((x2, yl), ln, font=font(20), fill=TXT); yl += 30
    return c.crop((0, 0, c.width, max(y + 40 + 1024, yl) + 40)).convert('RGB')


# ---------- 판정 그림 2 : 찹쌀이 크기 견주기 ----------
def nearest(k):
    def f(m):
        n = max(1, round(64 * k)); return m.resize((n, n), Image.NEAREST)
    return f


def board_size():
    S = 2; pw, ph = FARM_W * S, FARM_H * S; M = 40
    ground = tile_ground(GRASS, FARM_W, FARM_H)
    cases = [('① 64px ×1 (시안 크기)', 1.0), ('② 지금 게임 1단계 실제 : 몸 높이 40 = 캔버스 73 (×1.143)', 40 / 35), ('③ 캔버스 64 → 40 (×0.625)', 40 / 64), ('④ 캔버스 64 → 32 (×0.5)', 0.5)]
    c = Image.new('RGBA', (M + (pw + M) * len(cases), 2400), BG)
    top = header(c, '찹쌀이 크기 견주기 : 새 몸(선 110) · 새 잔디 · 새 울타리 · 각 칸 12마리 (×2) · 2026-10-03',
                 ['③ ④ 와 ② 는 게임이 하는 최근접 축소·확대 흉내다 (그림을 새로 그린 것 아님). 아래 줄은 한 마리를 ×6 으로 키운 것 : 선(회색 110 1px)이 깨지는지 본다.',
                  '② 를 더한 까닭 : 지금 게임(LayoutConst)은 「40px」 이 캔버스가 아니라 몸 높이(35px)라 실제로는 64 캔버스를 73px 로 키워 찍는다.'])
    d = ImageDraw.Draw(c); y = top + 10
    for i, (label, k) in enumerate(cases):
        x = M + i * (pw + M); d.text((x, y), label, font=font(24), fill=SUB)
        f = farm(ground, 20261003, herd(12, 7), nearest(k))
        c.alpha_composite(f.resize((pw, ph), Image.NEAREST), (x, y + 40))
        one = mozzi(BODIES[0], '#4FA84A', 2, True); n = max(1, round(64 * k)); one = one.resize((n, n), Image.NEAREST)
        cell = tile_ground(GRASS, 80, 80); cell.alpha_composite(one, ((80 - n) // 2, (80 - n) // 2))
        two = mozzi(BODIES[0], '#FFFFFF', 1).resize((n, n), Image.NEAREST); cell2 = tile_ground(GRASS, 80, 80); cell2.alpha_composite(two, ((80 - n) // 2, (80 - n) // 2))
        c.alpha_composite(cell.resize((480, 480), Image.NEAREST), (x, y + 40 + ph + 30)); c.alpha_composite(cell2.resize((480, 480), Image.NEAREST), (x + 520, y + 40 + ph + 30))
        d.text((x, y + 40 + ph + 30 + 490), '%dpx 를 ×6 (초록 · 흰)' % n, font=font(22), fill=TXT)
    return c.crop((0, 0, c.width, y + 40 + ph + 30 + 540)).convert('RGB')


if __name__ == '__main__':
    save(GRASS, 'grass_base')
    for i, (kind, p) in enumerate(DECOR): save(p, 'decor_%02d' % i)
    save(FENCE_H, 'fence_h'); save(FENCE_V, 'fence_v'); save(FENCE_POST, 'fence_post')
    for i, b in enumerate(BODIES): save(b, 'body_hop%d' % i)
    print('decor groups', len(DEC_ALL), [p.size for p in DEC_ALL])
    for r in check_bodies(): print('hop%d 윤곽(게임과 다른 px) %d · 윤곽(원본과 다른 px) %d · 선 %dpx · 선 색 %s' % r)
    board_grass().save(os.path.join(SIAN, '2026-10-03-잔디-바탕과장식.png'))
    board_size().save(os.path.join(SIAN, '2026-10-03-찹쌀이-크기견주기.png'))

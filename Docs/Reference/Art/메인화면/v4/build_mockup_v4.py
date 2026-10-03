# 메인 화면 「나」 v4 목업 + 농장 새 화풍 후보 비교판 — 2026-10-03 반복 03
# v3 에서 바꾼 것 : 잔디 · 울타리(건물 화풍으로 새로 뽑음) · 볼 샛노랑 · 건물 사이 틈과 보관함 자리.
# PIL 은 자르기·배치·이어 붙이기·확대·글자만 한다. 그림을 그리거나 다시 칠하지 않는다.
#   울타리 : 「네모 울타리 한 바퀴」 그림 한 장에서 모서리·가로 단위·세로 단위를 잘라 필요한 길이만큼 이어 붙인다.
#   잔디 : (가) 256 풀밭 한 장을 통째로 되풀이  (나) create_tiles_pro 32 타일 섞기  (다) 풀밭을 32 로 64장 잘라 섞기  (라) v3 바탕 타일 + 새 꾸밈 조각 얹기
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v4/build_mockup_v4.py
import os, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v3', os.path.join(HERE, '..', 'v3', 'build_mockup_v3.py'))
v3 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v3)
v2 = v3.v2
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)
W, H_TALL, INK, CREAM, OUT, font, art, paste, paste_base, paste_clip = v2.W, v2.H_TALL, v2.INK, v2.CREAM, v2.OUT, v2.font, v2.art, v2.paste, v2.paste_base, v2.paste_clip
P = dict(v2.P); N3 = v3.N


def raw(n): return Image.open(os.path.join(RAW, n + '.png')).convert('RGBA')


# ---------- 울타리 : 한 바퀴 그림에서 조각을 잘라 길이에 맞춰 잇는다 ----------
class Ring:
    """spec : 그림 안 좌표. top/bottom = (y0, y1) 띠, xl/xu/xr = 왼 모서리 · 가로 단위 · 오른 모서리의 x 범위,
    side = (y0, y1) 세로 단위, tail = 세로 단위 뒤 아래 띠 앞까지 남는 줄, lx/rx = 왼·오른 세로 줄의 x 범위."""

    def __init__(self, im, top, bottom, xl, xu, xr, side, tail, lx, rx):
        self.im = im; self.top = top; self.bottom = bottom; self.xl = xl; self.xu = xu; self.xr = xr; self.side = side; self.tail = tail; self.lx = lx; self.rx = rx

    def size(self, n, m):
        w = (self.xl[1] - self.xl[0]) + n * (self.xu[1] - self.xu[0]) + (self.xr[1] - self.xr[0])
        h = (self.top[1] - self.top[0]) + m * (self.side[1] - self.side[0]) + (self.tail[1] - self.tail[0]) + (self.bottom[1] - self.bottom[0])
        return w, h

    def band(self, rows, n):
        w, _ = self.size(n, 0); h = rows[1] - rows[0]; b = Image.new('RGBA', (w, h)); x = 0
        for (x0, x1) in [self.xl] + [self.xu] * n + [self.xr]:
            b.paste(self.im.crop((x0, rows[0], x1, rows[1])), (x, 0)); x += x1 - x0
        return b

    def back(self, n, m):
        """위 띠 + 양 옆 줄 (찹쌀이보다 뒤에 그린다)."""
        w, h = self.size(n, m); out = Image.new('RGBA', (w, h)); out.alpha_composite(self.band(self.top, n), (0, 0))
        y = self.top[1] - self.top[0]; lw = self.lx[1] - self.lx[0]; rw = self.rx[1] - self.rx[0]
        for rows in [self.side] * m + [self.tail]:
            out.alpha_composite(self.im.crop((self.lx[0], rows[0], self.lx[1], rows[1])), (0, y))
            out.alpha_composite(self.im.crop((self.rx[0], rows[0], self.rx[1], rows[1])), (w - rw, y)); y += rows[1] - rows[0]
        return out

    def front(self, n, m):
        """아래 띠 (찹쌀이보다 앞에 그린다). 돌려주는 y 는 back 그림 안에서의 자리."""
        _, h = self.size(n, m); return self.band(self.bottom, n), h - (self.bottom[1] - self.bottom[0])


LOG = Ring(raw('fence_ring_8201'), top=(29, 65), bottom=(183, 220), xl=(26, 59), xu=(59, 105), xr=(197, 230), side=(65, 102), tail=(176, 183), lx=(26, 46), rx=(210, 230))
PLANK = Ring(raw('fence_ring_plank_8202'), top=(48, 93), bottom=(147, 197), xl=(42, 64), xu=(64, 191), xr=(191, 212), side=(93, 147), tail=(147, 147), lx=(42, 60), rx=(194, 212))

# ---------- 잔디 ----------
FIELD = raw('grass_field_8101')
FIELD_TILES = [FIELD.crop((x, y, x + 32, y + 32)) for y in range(0, 256, 32) for x in range(0, 256, 32)]
TP = [raw('tilespro_8301_%d' % i) for i in range(16)]


def components(im):
    """투명 바탕 묶음 그림에서 떨어진 조각들을 왼쪽 위부터 떼어 낸다 (자르기)."""
    a = np.array(im)[..., 3] > 0; h, w = a.shape; seen = np.zeros_like(a); out = []
    for y in range(h):
        for x in range(w):
            if a[y, x] and not seen[y, x]:
                st = [(y, x)]; seen[y, x] = True; ys = [y]; xs = [x]
                while st:
                    cy, cx = st.pop()
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h and 0 <= nx < w and a[ny, nx] and not seen[ny, nx]: seen[ny, nx] = True; st.append((ny, nx)); ys.append(ny); xs.append(nx)
                if len(ys) > 12: out.append((min(ys) // 28, min(xs), im.crop((min(xs), min(ys), max(xs) + 1, max(ys) + 1))))
    return [c[2] for c in sorted(out, key=lambda c: (c[0], c[1]))]


DECALS = components(raw('decals_8401'))            # 줄 1 풀포기 4 · 줄 2 꽃 3 + 토끼풀 · 줄 3 토끼풀 + 자갈 3 + 버섯
for i, d in enumerate(DECALS): d.save(os.path.join(PARTS, 'decal_%02d.png' % i))


def decal_tile(base, d):
    t = base.copy(); t.alpha_composite(d, ((32 - d.width) // 2, (32 - d.height) // 2)); return t


def ground(kind, w, h, seed=20261003):
    """kind 별로 w×h 땅을 깐다."""
    im = Image.new('RGBA', (w, h)); rnd = random.Random(seed)
    if kind == 'v3':
        for y in range(0, h, 32):
            for x in range(0, w, 32):
                r = rnd.random(); im.paste(N3['grass_0'] if r < 0.82 else (N3['grass_2'] if r < 0.92 else N3['grass_1']), (x, y))
    elif kind == 'field':
        for y in range(0, h, 256):
            for x in range(0, w, 256): im.paste(FIELD, (x, y))
    elif kind == 'tilespro':
        for y in range(0, h, 32):
            for x in range(0, w, 32):
                r = rnd.random(); im.paste(TP[rnd.choice([0, 1, 2])] if r < 0.82 else (TP[rnd.choice([4, 5, 7])] if r < 0.92 else TP[rnd.choice([8, 9, 10])]), (x, y))
    elif kind == 'cut':
        for y in range(0, h, 32):
            for x in range(0, w, 32): im.paste(rnd.choice(FIELD_TILES), (x, y))
    elif kind == 'decal':
        g = N3['grass_0']; fl = [decal_tile(g, DECALS[i]) for i in (4, 5, 6)]; pe = [decal_tile(g, DECALS[i]) for i in (9, 10, 11)]; tu = [decal_tile(g, DECALS[i]) for i in (0, 1, 2, 3)]
        for y in range(0, h, 32):
            for x in range(0, w, 32):
                r = rnd.random(); im.paste(g if r < 0.62 else (rnd.choice(tu) if r < 0.82 else (rnd.choice(pe) if r < 0.92 else rnd.choice(fl))), (x, y))
    return im


def seam(kind):
    """이음매 재기 : 32 격자 선을 사이에 둔 이웃 픽셀의 색 차 평균 ÷ 격자 선이 아닌 곳의 이웃 픽셀 색 차 평균. 1 에 가까우면 이음매가 안 보인다."""
    a = np.array(ground(kind, 512, 512).convert('RGB')).astype(int); d = np.abs(a[:, 1:] - a[:, :-1]).sum(-1).mean(0)
    on = [d[x] for x in range(len(d)) if (x + 1) % 32 == 0]; off = [d[x] for x in range(len(d)) if (x + 1) % 32 != 0]
    return round(float(np.mean(on) / np.mean(off)), 2)


CHEEK = art('Mozzi/cheek')                           # 확정 : 샛노랑 #FBD701 그대로
BODY = N3['mozzi_body_hop0_line110']                 # 확정 : 선 회색 110


def mozzi(color, face, crown): return v3.mozzi(BODY, color, face, crown, CHEEK)


# ---------- 후보 비교판 ----------
def panel(kind, ring, n, m):
    pw, ph = 288, 330; im = ground(kind, pw, ph)
    paste(im, N3['shop_big'].crop((40, 110, 240, 200)), 8, 0); paste(im, N3['machine_big'].crop((150, 140, 230, 227)), 208, 3)
    if ring is None:                                      # v3 울타리 (32 칸 조각)
        cols = list(range(16, pw - 32, 32)); rows = list(range(112, ph - 32, 32))
        for y in rows:
            for x in cols:
                l, r_ = x == cols[0], x == cols[-1]
                if y == rows[0]: t = N3['fence_corner_top'] if l else (ImageOps.mirror(N3['fence_corner_top']) if r_ else N3['fence_top'])
                elif y == rows[-1]: t = N3['fence_corner_bottom'] if l else (ImageOps.mirror(N3['fence_corner_bottom']) if r_ else N3['fence_bottom'])
                elif l: t = N3['fence_side']
                elif r_: t = ImageOps.mirror(N3['fence_side'])
                else: continue
                paste(im, t, x, y)
        fx, fy, fr = 16, 112, None
    else:
        w, h = ring.size(n, m); fx = (pw - w) // 2; fy = 104; paste(im, ring.back(n, m), fx, fy); fr = ring.front(n, m)
    paste(im, mozzi('#FFFFFF', 1, False), 70, 170); paste(im, mozzi('#4FA84A', 2, True), 160, 200); paste(im, P['pile_redbean'], 130, 180)
    if fr: paste(im, fr[0], fx, fy + fr[1])
    return im


def candidates():
    items = [('지금 (v3) : 풀 3종 + 가는 울타리', 'v3', None, 0, 0), ('가 : 256 풀밭 한 장을 되풀이 + 통나무 울타리', 'field', LOG, 4, 3), ('나 : 타일 도구(tiles_pro) 32 타일 + 판자 울타리', 'tilespro', PLANK, 1, 2),
             ('다 : 풀밭을 32 로 잘라 섞기 + 통나무 울타리', 'cut', LOG, 4, 3), ('라 : v3 바탕 타일 + 새 꾸밈 조각 + 통나무 울타리', 'decal', LOG, 4, 3)]
    S = 3; M = 30; cw = 288 * S; c = Image.new('RGBA', (M + (cw + M) * len(items), 150 + 330 * S + 300), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 20), '농장 잔디 · 울타리 새 화풍 후보 (×3) : 건물 조각 · 찹쌀이 둘과 나란히', font=font(40), fill=(255, 240, 220))
    for i, (label, kind, ring, n, m) in enumerate(items):
        x = M + i * (cw + M); d.text((x, 96), label, font=font(24), fill=(230, 200, 170))
        c.paste(panel(kind, ring, n, m).resize((cw, 330 * S), Image.NEAREST), (x, 140))
        d.text((x, 150 + 330 * S), '32 격자 선의 색 차 ÷ 다른 곳의 색 차 = %.2f' % seam(kind), font=font(22), fill=(240, 225, 205))
    y = 150 + 330 * S + 44
    for line in ['전부 PixelLab 그림이다 (PIL 은 자르기 · 배치 · 이어 붙이기만). 풀밭과 울타리는 가게 그림(가판대 v1)을 화풍 그림으로 넣어 뽑았다.',
                 '가 : 풀 얼룩 · 풀포기 · 꽃 · 자갈이 손그림처럼 섞여 건물 화풍과 가장 가깝다. 256 마다 같은 그림이 되풀이되고, 게임의 「타일 3종 섞기」와 방식이 다르다.',
                 '나 : 타일마다 가장자리가 어두워 격자가 보인다. 판자 울타리는 모서리 기둥만 있고 굵어서 찹쌀이를 가린다.',
                 '다 : 가 의 풀밭을 32 로 64장 잘라 아무렇게나 섞은 것. 얼룩이 타일 선에서 끊기는지 보는 시험이다.',
                 '라 : 바탕은 v3 타일 그대로(민무늬)고 풀포기 · 꽃 · 자갈만 새 조각이다. 타일 3종 방식과 맞지만 바탕이 여전히 밋밋하다.',
                 '통나무 울타리 : 기둥 간격이 가로 46 · 세로 37 이라 32 격자에는 안 맞는다 (화풍 먼저). 가로 · 세로 · 모서리가 한 그림에서 나와 짜임이 같다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.crop((0, 0, c.width, y + 20)).convert('RGB')


# ---------- 메인 화면 v4 ----------
GRASS_KIND = 'field'; FN, FM = 10, 11                 # 고른 벌 : 가 (풀밭 통째 + 통나무 울타리 가로 10칸 · 세로 11칸)


def scene():
    H = H_TALL; im = Image.new('RGBA', (W, H), v2.SKY); info = {}
    farm_top = H - 480; path_top = farm_top - 60; horizon = path_top - 112
    v2.sky_and_forest(im, horizon)
    g = ground(GRASS_KIND, 768, 768); paste(im, g.crop((0, 0, W, H - horizon)), 0, horizon)
    p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
    paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top)
    fw, fh = LOG.size(FN, FM); fx = (W - fw) // 2; fy = H - fh
    paste(im, LOG.back(FN, FM), fx, fy)
    base = path_top + 8
    paste_base(im, N3['machine_big'], 120, base); paste_base(im, N3['shop_big'], 365, base)        # 기계 5~235 · 틈 10 · 가게 245~485
    paste_base(im, P['chest'], 499, path_top + 54)                                                 # 상자 : 가게 오른쪽 앞, 길 위
    info['labels'] = [('합성소', 120, base + 22), ('가판대', 350, base + 22), ('보관함', 499, path_top + 54 - 62)]
    for (hair, shirt, bottom, face, cx) in [('02_brown', '01_blue', '01_navy', 1, 262), ('04_orange', '02_yellow', '02_beige', 2, 418), ('05_black', '03_green', '01_gray', 3, 40)]:
        paste(im, v2.customer(hair, shirt, bottom, face), cx - 32, path_top + 50 - 76)
    mz = [('#FFFFFF', 1, False, 80, 120), ('#4FA84A', 2, True, 200, 100), ('#7FC8F8', 1, True, 330, 160), ('#FFFFFF', 5, False, 120, 260),
          ('#3F9BE0', 2, True, 190, 310), ('#2E7D32', 1, True, 80, 390), ('#F5A05A', 1, True, 420, 250)]
    for (k, x, y) in [('pile_redbean', 150, 190), ('pile_sesame', 270, 90), ('pile_bean', 220, 380)]: paste(im, P[k], x, farm_top + y)
    for (c, f, crown, x, y) in sorted(mz, key=lambda m: m[4]): paste(im, mozzi(c, f, crown), x - 32, farm_top + y - 32)
    fr, fo = LOG.front(FN, FM); paste(im, fr, fx, fy + fo)
    return im, info


def render(H):
    world, info = scene(); im = world.crop((0, H_TALL - H, W, H_TALL)); off = H_TALL - H
    texts = v2.ui(im, H)
    for (label, cx, y) in info['labels']: texts.append((label, cx, y - off + 9, 22, CREAM, 'mm', INK))
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for t in texts:
        s, x, y, size, col, anchor = t[:6]; stroke = t[6] if len(t) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big


def board():
    a = render(960); b = render(1200); M = 40; top = 150
    c = Image.new('RGBA', (M * 3 + a.width * 2, top + b.height + 40), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 시안 「나」 v4 : 잔디 · 울타리를 건물 화풍으로 새로 · 볼 샛노랑', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170)); d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2)', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top)); y = top + a.height + 30
    for line in ['v3 에서 바꾼 것 : 잔디(후보 가 · 256 풀밭 한 장을 되풀이) · 울타리(통나무 한 바퀴 그림을 잘라 이음)',
                 '· 볼 = 샛노랑 #FBD701 (확정) · 찹쌀이 선 회색 110 · 크기 ×1 (확정).',
                 '건물은 그대로다. 기계와 가게 사이에 10px 틈을 두고, 보관함 상자는 가게 오른쪽 앞 길 위로 내렸다.',
                 '메뉴 · 길 · 숲 · 구름 · 손님 · 재료 더미 · 패널은 v3 그대로다.',
                 '울타리 기둥 간격은 가로 46 · 세로 37 이라 32 격자와 안 맞는다.',
                 '풀밭은 256 마다 되풀이된다 (게임에 넣을 때 다시 정한다).']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


if __name__ == '__main__':
    LOG.back(4, 3).save(os.path.join(PARTS, 'fence_log_sample_back.png')); LOG.front(4, 3)[0].save(os.path.join(PARTS, 'fence_log_bottom_sample.png'))
    for k, box in [('fence_log_corner_tl', (26, 29, 59, 65)), ('fence_log_top_unit', (59, 29, 105, 65)), ('fence_log_corner_tr', (197, 29, 230, 65)), ('fence_log_side_left_unit', (26, 65, 46, 102)),
                   ('fence_log_side_right_unit', (210, 65, 230, 102)), ('fence_log_corner_bl', (26, 183, 59, 220)), ('fence_log_bottom_unit', (59, 183, 105, 220)), ('fence_log_corner_br', (197, 183, 230, 220))]:
        LOG.im.crop(box).save(os.path.join(PARTS, k + '.png'))
    FIELD.save(os.path.join(PARTS, 'grass_field_256.png'))
    print({k: seam(k) for k in ['v3', 'field', 'tilespro', 'cut', 'decal']}, len(DECALS))
    candidates().save(os.path.join(OUT, '2026-10-03-농장-새화풍-후보.png'))
    board().save(os.path.join(OUT, '2026-10-03-메인화면-나-v4.png'))

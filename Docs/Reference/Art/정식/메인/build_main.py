# 정식 그림 v15 — 메인 화면 건물 · 하늘 · 메뉴 아이콘 (2026-10-03)
# raw/(PixelLab 원본) → out/(게임에 넣을 png) 과 판정용 미리보기 한 장을 만든다.
# PIL 은 자르기 · 배치 · 정수 배 확대 · 글자 · 배경 지우기 · 단색 바꾸기만 한다. 새 그림을 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/메인/build_main.py
# 다른 폴더(메인화면/v1~v7 · Unity Resources)는 읽기만 한다 — 그 폴더 스크립트를 import 하지 않는다 (import 하면 그쪽 parts 를 다시 쓴다).
import os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw'); OUT = os.path.join(HERE, 'out'); os.makedirs(OUT, exist_ok=True)
REF = os.path.join(ROOT, 'Docs', 'Reference', 'Art')
MAIN = os.path.join(REF, '메인화면')
ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
PREVIEW = os.path.join(REF, '시안', '2026-10-03-메인화면-정식-미리보기.png')
V7_MOCK = os.path.join(REF, '시안', '2026-10-03-메인화면-나-v7.png')

W = 540
SKY = (212, 238, 250, 255)            # 하늘 원본(sky_15101)의 하늘색 = v7 하늘색. sky.png 맨 윗줄 색 = 카메라 배경색
OUTLINE = (53, 46, 39, 255)            # 가게(후보 3) 외곽선 색. 기계 · 보관함 외곽선을 이 색 하나로 맞춘다
BG = (58, 50, 48, 255)


def rgba(p): return Image.open(p).convert('RGBA')
def raw(n): return rgba(os.path.join(RAW, n + '.png'))
def trim(im): return im.crop(im.getbbox())
def font(s): return ImageFont.truetype(FONT, s)


def recolor(im, test, to):
    """단색 바꾸기 : test(r,g,b) 가 참인 불투명 픽셀을 to 한 색으로 바꾼다."""
    a = np.array(im); rgb = a[..., :3].astype(int); m = (a[..., 3] > 0) & test(rgb[..., 0], rgb[..., 1], rgb[..., 2])
    a[m] = to; return Image.fromarray(a), int(m.sum())


def drop_bg_from_corners(im, tol=40):
    """배경 지우기 : 네 모서리 색과 가까운 픽셀을 모서리에서 이어진 곳만 투명으로 (flood fill)."""
    a = np.array(im); h, w = a.shape[:2]; bg = a[0, 0, :3].astype(int)
    near = np.abs(a[..., :3].astype(int) - bg).sum(-1) <= tol; seen = np.zeros((h, w), bool)
    st = [(y, x) for (y, x) in [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)] if near[y, x]]
    for p in st: seen[p] = True
    while st:
        y, x = st.pop()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and near[ny, nx] and not seen[ny, nx]: seen[ny, nx] = True; st.append((ny, nx))
    a[seen] = (0, 0, 0, 0); return Image.fromarray(a)


# ---------- out : 건물 셋 ----------
STALL = trim(raw('shop_simple_13203'))                                          # 250×203 · 외곽선 이미 OUTLINE
SYNTH, n_syn = recolor(trim(raw('machine_simple_13102')),
                       lambda r, g, b: ((r == 29) & (g == 27) & (b == 24)) | ((r == 44) & (g == 39) & (b == 34)), OUTLINE)
STORAGE, n_sto = recolor(trim(raw('chest_17113')), lambda r, g, b: (r + g + b) <= 45, OUTLINE)

# ---------- out : 하늘 540×720 ----------
SKY_H = 720


def build_sky():
    g = raw('sky_15101')                                                       # 576×384 · 하늘색 한 가지 + 구름 + 먼 숲
    band = g.crop((18, 0, 18 + W, g.height))
    sky = Image.new('RGBA', (W, SKY_H), SKY); top = SKY_H - band.height; sky.paste(band, (0, top))
    # 위로 늘린 자리 : 같은 원본의 구름을 잘라 옮겨 놓는다 (하늘이 한 색이라 이음새가 없다)
    clouds = {'c': (100, 140, 226, 196), 'a': (352, 120, 436, 156), 'b': (316, 162, 386, 198), 'd': (420, 182, 548, 236)}
    for key, x, y, mirror in [('c', 300, 236, True), ('a', 40, 300, False), ('d', 330, 410, True), ('b', 120, 120, True), ('a', 380, 40, True)]:
        x0, y0, x1, y1 = clouds[key]; piece = g.crop((x0, y0, x1, y1))
        if mirror: piece = ImageOps.mirror(piece)
        sky.paste(piece, (x, y))
    return sky


SKYIMG = build_sky()

# ---------- out : 메뉴 아이콘 ----------
ICON_CODEX = raw('icon_codex_15201')
ICON_MENU = raw('icon_menu_15203')
ICON_UPGRADE = drop_bg_from_corners(raw('icon_upgrade_15204'))

# ---------- out : 길 (v1 부품 path 340×60 을 좌우 뒤집어 이어 540 폭으로) ----------
PATH_SRC = rgba(os.path.join(MAIN, 'v1', 'parts', 'path.png'))
_strip = Image.new('RGBA', (680, 60)); _strip.paste(PATH_SRC, (0, 0)); _strip.paste(ImageOps.mirror(PATH_SRC), (340, 0))
PATH = _strip.crop((60, 0, 60 + W, 60))

OUTS = {'stall_main': STALL, 'synthesis_main': SYNTH, 'storage_main': STORAGE, 'sky': SKYIMG,
        'icon_codex': ICON_CODEX, 'icon_upgrade': ICON_UPGRADE, 'icon_menu': ICON_MENU, 'path_main': PATH}

# ---------- 배치 (540×960 기준 · 왼쪽 위 0,0 · 아래로 붙임) ----------
H = 960
FARM_TOP = H - 480; PATH_TOP = FARM_TOP - 60; HORIZON = PATH_TOP - 112; FEET = PATH_TOP + 8
PLACE = {   # 이름 : (가운데 x, 발끝 y)  — 피벗 = 아래 가운데
    'synthesis_main': (88, FEET), 'stall_main': (306, FEET), 'storage_main': (484, PATH_TOP + 50)}
ICON_X = W - 2 - 56                                                            # 캔버스 56 · 그림 48 (둘레 4px 빈칸)
ICONS = [('icon_menu', ICON_X, H - 24 - 56), ('icon_upgrade', ICON_X, H - 24 - 56 * 2), ('icon_codex', ICON_X, H - 24 - 56 * 3)]


# ---------- 미리보기용 농장 (v4 「가」 벌 부품 · 읽기만) ----------
V4RAW = os.path.join(MAIN, 'v4', 'raw')
FIELD = rgba(os.path.join(V4RAW, 'grass_field_8101.png'))
RING = rgba(os.path.join(V4RAW, 'fence_ring_8201.png'))
TOP, BOT, XL, XU, XR, SIDE, TAIL, LX, RX = (29, 65), (183, 220), (26, 59), (59, 105), (197, 230), (65, 102), (176, 183), (26, 46), (210, 230)


def ring_size(n, m):
    return (XL[1] - XL[0]) + n * (XU[1] - XU[0]) + (XR[1] - XR[0]), (TOP[1] - TOP[0]) + m * (SIDE[1] - SIDE[0]) + (TAIL[1] - TAIL[0]) + (BOT[1] - BOT[0])


def ring_band(rows, n):
    w, _ = ring_size(n, 0); b = Image.new('RGBA', (w, rows[1] - rows[0])); x = 0
    for (x0, x1) in [XL] + [XU] * n + [XR]: b.paste(RING.crop((x0, rows[0], x1, rows[1])), (x, 0)); x += x1 - x0
    return b


def ring_back(n, m):
    w, h = ring_size(n, m); out = Image.new('RGBA', (w, h)); out.alpha_composite(ring_band(TOP, n), (0, 0)); y = TOP[1] - TOP[0]
    for rows in [SIDE] * m + [TAIL]:
        out.alpha_composite(RING.crop((LX[0], rows[0], LX[1], rows[1])), (0, y))
        out.alpha_composite(RING.crop((RX[0], rows[0], RX[1], rows[1])), (w - (RX[1] - RX[0]), y)); y += rows[1] - rows[0]
    return out


BODY = rgba(os.path.join(MAIN, 'v3', 'parts', 'mozzi_body_hop0_line110.png'))


def mozzi(color_hex, face, crown):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5)); a = np.array(BODY).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    im = Image.fromarray(a.astype('uint8')); im.alpha_composite(rgba(os.path.join(ART, 'Mozzi', 'face_%02d.png' % face)))
    ck = rgba(os.path.join(ART, 'Mozzi', 'cheek.png')); im.alpha_composite(ck, (14, 34)); im.alpha_composite(ck, (39, 34))
    if crown: im.alpha_composite(rgba(os.path.join(ART, 'Mozzi', 'acc_crown.png')))
    return im


def put(dst, im, x, y):
    x, y = int(x), int(y); box = (max(0, -x), max(0, -y), min(im.width, dst.width - x), min(im.height, dst.height - y))
    if box[2] > box[0] and box[3] > box[1]: dst.alpha_composite(im.crop(box), (x + box[0], y + box[1]))


def scene(h):
    """540×h 화면. 아래로 붙인다 — 960 위의 모든 y 에 (h-960) 을 더한다."""
    off = h - H; im = Image.new('RGBA', (W, h), SKY)
    put(im, SKYIMG, 0, HORIZON + off - SKY_H)
    for y in range(HORIZON + off, h, 256):
        for x in range(0, W, 256): put(im, FIELD, x, y)
    put(im, PATH, 0, PATH_TOP + off)
    FN, FM = 10, 11; fw, fh = ring_size(FN, FM); fx = (W - fw) // 2; fy = h - fh
    put(im, ring_back(FN, FM), fx, fy)
    for name, (cx, by) in PLACE.items():
        o = OUTS[name]; put(im, o, cx - o.width // 2, by + off - o.height)
    mz = [('#FFFFFF', 1, False, 80, 120), ('#4FA84A', 2, True, 200, 100), ('#7FC8F8', 1, True, 330, 160), ('#FFFFFF', 5, False, 120, 260),
          ('#3F9BE0', 2, True, 190, 310), ('#2E7D32', 1, True, 80, 390), ('#F5A05A', 1, True, 420, 250)]
    for (c, f, cr, x, y) in sorted(mz, key=lambda m: m[4]): put(im, mozzi(c, f, cr), x - 32, FARM_TOP + off + y - 32)
    put(im, ring_band(BOT, FN), fx, fy + fh - (BOT[1] - BOT[0]))
    for name, x, y in ICONS: put(im, OUTS[name], x, y + off)
    return im


def board():
    a = scene(960).resize((W * 2, 960 * 2), Image.NEAREST); b = scene(1200).resize((W * 2, 1200 * 2), Image.NEAREST)
    v7 = rgba(V7_MOCK); v7 = v7.crop((40, 150, 40 + 1080, 150 + 1920))           # v7 시안의 9:16 칸 (이미 ×2)
    M = 40; top = 150; c = Image.new('RGBA', (M * 4 + W * 2 * 3, top + b.height + 300), BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 정식 그림 미리보기 (v15) : 건물 셋 · 하늘 · 메뉴 아이콘', font=font(44), fill=(255, 240, 220))
    xs = [M, M * 2 + W * 2, M * 3 + W * 4]
    for x, t in zip(xs, ['v7 시안 (견줄 것) · 9:16 · ×2', '정식 · 9:16 · 540×960 · ×2', '정식 · 9:20 · 540×1200 · ×2']):
        d.text((x, 100), t, font=font(28), fill=(230, 200, 170))
    c.paste(v7, (xs[0], top)); c.paste(a, (xs[1], top)); c.paste(b, (xs[2], top))
    y = top + b.height + 30
    for line in ['정식 out 그림 : 가게(stall_main) · 합성 기계(synthesis_main) · 보관함(storage_main · 열린 궤짝) · 하늘(sky) · 길(path_main) · 메뉴 아이콘 셋. 글자는 그림에 없다.',
                 '농장 자리는 v4 「가」 벌(잔디 · 통나무 울타리)과 찹쌀이로 채운 자리표시다. 재화 캡슐 · 건물 이름 · 펼친 메뉴 목록은 안 얹었다 (코드 몫).',
                 '9:20 은 같은 그림을 아래로 붙이고 위 240 줄을 하늘이 받는다. 하늘 그림 위쪽은 카메라 배경색(#D4EEFA)과 같은 색이다.']:
        d.text((M, y), line, font=font(26), fill=(240, 225, 205)); y += 42
    return c.crop((0, 0, c.width, y + 20)).convert('RGB')


def report():
    print('외곽선 바꾼 픽셀 : 기계 %d · 보관함 %d' % (n_syn, n_sto))
    for k, im in OUTS.items():
        print('%-15s %dx%d  bbox %s  top-row-colors %d' % (k, im.width, im.height, im.getchannel('A').getbbox(), len(set(map(tuple, np.array(im)[0].tolist())))))
    for name, (cx, by) in PLACE.items():
        o = OUTS[name]; print('%-15s 가운데x %d 발끝y %d  사각형 x%d~%d y%d~%d' % (name, cx, by, cx - o.width // 2, cx - o.width // 2 + o.width, by - o.height, by))
    for name, x, y in ICONS: print('%-15s 왼쪽위 (%d,%d)' % (name, x, y))
    print('지평선(하늘 아래끝) y %d · 길 y %d~%d · 농장 위끝 %d' % (HORIZON, PATH_TOP, PATH_TOP + 60, FARM_TOP))


if __name__ == '__main__':
    for k, im in OUTS.items(): im.save(os.path.join(OUT, k + '.png'))
    board().save(PREVIEW)
    report()

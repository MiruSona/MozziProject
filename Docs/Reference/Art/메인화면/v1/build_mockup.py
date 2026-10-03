# 메인 화면 목업(가·나) 합성 스크립트 — 2026-10-03 반복 03
# PIL 은 자르기·배경 지우기·배치·9조각 이어 붙이기·확대·글자 얹기만 한다. 그림을 그리거나 다시 칠하지 않는다.
# (예외 : 하늘 단색 바탕, 시안 바깥 여백과 설명 글)
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v1/build_mockup.py
import os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw')
PARTS = os.path.join(HERE, 'parts')
ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
OUT = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '시안')
os.makedirs(PARTS, exist_ok=True)

W = 540            # 기준 폭
H_TALL = 1200      # 긴 폰. 960 판은 이 그림의 아래 960 줄이다 (아래로 붙이고 위는 하늘)
SKY = (212, 238, 250, 255)   # 구름 그림의 바탕색 그대로
INK = (74, 44, 36)           # 글자 색 (짙은 갈색)
CREAM = (255, 246, 226)


def raw(name): return Image.open(os.path.join(RAW, name + '.png')).convert('RGBA')
def art(rel): return Image.open(os.path.join(ART, rel + '.png')).convert('RGBA')
def trim(im): return im.crop(im.getbbox())


def key_exact(im, rgb):
    a = np.array(im); m = (a[..., :3] == np.array(rgb)).all(-1); a[m] = 0
    return Image.fromarray(a)


def key_flood(im, tol=40):
    """네 모서리에서 번져 가며 바탕색(모서리 색과 가까운 색)을 지운다."""
    a = np.array(im).astype(int); h, w = a.shape[:2]
    ref = a[0, 0, :3]; near = (np.abs(a[..., :3] - ref).sum(-1) <= tol)
    seen = np.zeros((h, w), bool); stack = [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)]
    while stack:
        y, x = stack.pop()
        if y < 0 or x < 0 or y >= h or x >= w or seen[y, x] or not near[y, x]: continue
        seen[y, x] = True; stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    a[seen] = 0
    return Image.fromarray(a.astype('uint8'))


def nine(im, w, h, b):
    """9조각 : 모서리는 그대로, 변과 속은 타일처럼 이어 붙인다 (늘려 뭉개지 않는다)."""
    out = Image.new('RGBA', (w, h)); sw, sh = im.size
    xs = [(0, b, 0), (b, sw - b, b), (sw - b, sw, w - b)]; ys = [(0, b, 0), (b, sh - b, b), (sh - b, sh, h - b)]
    for iy, (y0, y1, dy) in enumerate(ys):
        for ix, (x0, x1, dx) in enumerate(xs):
            piece = im.crop((x0, y0, x1, y1))
            tw = w - 2 * b if ix == 1 else b; th = h - 2 * b if iy == 1 else b
            if tw <= 0 or th <= 0: continue
            cell = Image.new('RGBA', (tw, th))
            for ty in range(0, th, piece.height):
                for tx in range(0, tw, piece.width):
                    cell.paste(piece, (tx, ty))
            out.paste(cell, (dx, dy))
    return out


# ---------- 부품 다듬기 (자르기·배경 지우기만) ----------
P = {}
P['cafe_34'] = trim(raw('cafe34_3101'))
P['synth_34'] = trim(raw('synth34_3202'))
P['cafe_front'] = trim(raw('cafefront_3302'))
P['synth_front'] = trim(raw('synthfront_3401'))
P['chest'] = trim(raw('chest_3502'))
kit = raw('uikit_3801')
P['ui_panel'] = kit.crop((60, 214, 318, 336))
P['ui_tab'] = kit.crop((147, 169, 238, 204))
P['ui_btn'] = trim(key_flood(raw('btn_blank_4002')))
P['ui_btn_menu'] = trim(raw('btn_menu_4101'))
P['ui_capsule'] = trim(raw('capsule_4401'))
P['ui_ribbon'] = trim(raw('ribbon_4301'))
P['icon_book'] = trim(raw('icon_book_4201'))
P['icon_basket'] = trim(raw('icon_basket_4202'))
P['icon_scroll'] = trim(raw('icon_scroll_4203'))
P['icon_coin'] = trim(raw('icon_coin_4204'))
forest = key_exact(raw('forest_3601'), (219, 250, 252))
P['forest'] = forest.crop((0, forest.getbbox()[1], 400, 96))
cl = raw('clouds_3701'); ca = np.array(cl)
haze = ((ca[..., :3] == np.array((225, 232, 245))).all(-1)).mean(1)
cut = int(np.argmax(haze > 0.5)) if (haze > 0.5).any() else 200
P['clouds'] = trim(key_exact(cl.crop((0, 0, 400, cut - 14)), (212, 238, 250)))
P['path'] = raw('path_3901').crop((30, 2, 370, 62))
for k, v in P.items(): v.save(os.path.join(PARTS, k + '.png'))

# ---------- 지금 게임 그림 (읽기만) ----------
GRASS = [art('Farm/ground_grass_%d' % i) for i in range(3)]
F_TOP, F_BOT, F_SIDE = art('Farm/fence_top'), art('Farm/fence_bottom'), art('Farm/fence_side')
F_CT, F_CB = art('Farm/fence_corner_top'), art('Farm/fence_corner_bottom')


def mozzi(color_hex, face, crown):
    body = art('Mozzi/body_hop0'); c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5))
    a = np.array(body).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0      # 게임과 같은 곱하기 색
    im = Image.fromarray(a.astype('uint8'))
    im.alpha_composite(art('Mozzi/face_%02d' % face))
    ck = art('Mozzi/cheek'); im.alpha_composite(ck, (14, 34)); im.alpha_composite(ck, (39, 34))
    if crown: im.alpha_composite(art('Mozzi/acc_crown'))
    return im


def customer(hair, shirt, face):
    im = art('Customer/body')
    for rel in ('Customer/shirt_' + shirt, 'Customer/face_%02d' % face, 'Customer/hair_' + hair):
        im.alpha_composite(art(rel))
    return im


def paste_clip(dst, im, x, y, top=0):
    """화면 밖으로 나가는 부분을 잘라 내고 얹는다."""
    x, y = int(x), int(y)
    if y + im.height <= top or y >= dst.height or x + im.width <= 0 or x >= dst.width: return
    if y < top: im = im.crop((0, top - y, im.width, im.height)); y = top
    x0 = max(0, -x); im = im.crop((x0, 0, max(x0, min(im.width, dst.width - x)), min(im.height, dst.height - y))); x = max(x, 0)
    if im.width > 0 and im.height > 0: dst.alpha_composite(im, (x, y))


def paste(dst, im, x, y): paste_clip(dst, im, x, y)
def paste_base(dst, im, cx, base_y): paste(dst, im, cx - im.width // 2, base_y - im.height)


def ground(dst, top, bottom, seed):
    rnd = random.Random(seed)
    for y in range(bottom - 32, top - 32, -32):
        for x in range(-2, W, 32):
            r = rnd.random(); t = GRASS[0] if r < 0.82 else (GRASS[2] if r < 0.92 else GRASS[1])
            paste_clip(dst, t, x, y, top)


def fence(dst, top, bottom):
    cols = list(range(-2, W, 32)); last = cols[-1]
    rows = list(range(top, bottom, 32))
    for y in rows:
        for x in cols:
            edge_l, edge_r = x == cols[0], x == last
            if y == rows[0]: t = F_CT if edge_l else (ImageOps.mirror(F_CT) if edge_r else F_TOP)
            elif y == rows[-1]: t = F_CB if edge_l else (ImageOps.mirror(F_CB) if edge_r else F_BOT)
            elif edge_l: t = F_SIDE
            elif edge_r: t = ImageOps.mirror(F_SIDE)
            else: continue
            paste_clip(dst, t, x, y)


def sky_and_forest(dst, horizon):
    f = P['forest']; band = Image.new('RGBA', (800, f.height)); band.paste(f, (0, 0)); band.paste(ImageOps.mirror(f), (400, 0))
    c = P['clouds']
    paste_clip(dst, c, 140, horizon - f.height - c.height + 10)
    paste_clip(dst, ImageOps.mirror(c), -150, horizon - f.height - 2 * c.height - 40)
    paste_clip(dst, c, 260, horizon - f.height - 3 * c.height - 110)
    paste(dst, band.crop((130, 0, 130 + W, f.height)), 0, horizon - f.height)


# ---------- 장면 ----------
def scene(kind):
    H = H_TALL; im = Image.new('RGBA', (W, H), SKY); info = {}
    if kind == 'ga':
        farm_top = H - 544; horizon = farm_top - 172
        sky_and_forest(im, horizon); ground(im, horizon, H, 20260920); fence(im, farm_top, H)
        base = farm_top - 20
        paste_base(im, P['synth_34'], 100, base); paste_base(im, P['cafe_34'], 282, base - 6); paste_base(im, P['chest'], 432, base - 10)
        info['labels'] = [('합성소', 100, base + 2), ('가판대', 282, base + 2), ('보관함', 432, base + 2)]
        mz = [('#FFFFFF', 1, False, 60, 150), ('#4FA84A', 2, True, 190, 110), ('#7FC8F8', 1, True, 330, 170), ('#FFFFFF', 5, False, 250, 280),
              ('#3F9BE0', 2, True, 90, 350), ('#2E7D32', 1, True, 380, 360), ('#FFFFFF', 2, False, 200, 430)]
        drops = [('redbean', 140, 210), ('sesame', 300, 120), ('bean', 400, 290), ('redbean', 60, 470)]
    else:
        farm_top = H - 480; path_top = farm_top - 60; horizon = path_top - 112
        sky_and_forest(im, horizon); ground(im, horizon, H, 20260921)
        p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
        paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top); fence(im, farm_top, H)
        base = path_top + 8
        paste_base(im, P['synth_front'], 92, base); paste_base(im, P['cafe_front'], 296, base); paste_base(im, P['chest'], 470, base + 4)
        info['labels'] = [('합성소', 92, base + 34), ('가판대', 296, base + 34), ('보관함', 470, base + 34)]
        for (hair, shirt, face, cx) in [('01_brown', 'blue', 1, 210), ('02_orange', 'yellow', 2, 372), ('01_purple', 'green', 1, 30)]:
            paste(im, customer(hair, shirt, face), cx - 24, path_top + 48 - 59)
        mz = [('#FFFFFF', 1, False, 60, 110), ('#4FA84A', 2, True, 200, 90), ('#7FC8F8', 1, True, 330, 150), ('#FFFFFF', 5, False, 120, 250),
              ('#3F9BE0', 2, True, 300, 300), ('#2E7D32', 1, True, 60, 380)]
        drops = [('redbean', 150, 180), ('sesame', 280, 80), ('bean', 220, 370)]
    for (ing, x, y) in drops: paste(im, art('Ingredient/' + ing), x, farm_top + y)
    for (c, f, crown, x, y) in sorted(mz, key=lambda m: m[4]): paste(im, mozzi(c, f, crown), x - 32, farm_top + y - 32)
    info['farm_top'] = farm_top; info['horizon'] = horizon
    return im, info


def ui(im, kind, H):
    """화면(540×H) 위에 UI 조각을 놓고, 글자 자리 목록을 돌려준다 (글자는 ×2 로 키운 뒤 얹는다)."""
    texts = []
    cap = P['ui_capsule']; coin = P['icon_coin']
    paste(im, cap, 8, 8); paste(im, coin, 20, 8 + (cap.height - coin.height) // 2); texts.append(('1,250', 8 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    x2 = 8 + cap.width + 6; ing = art('Ingredient/redbean')
    paste(im, cap, x2, 8); paste(im, ing, x2 + 12, 8); texts.append(('34', x2 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    bm = P['ui_btn_menu']; bx = W - 6 - bm.width; by = H - 28 - bm.height
    paste(im, bm, bx, by)
    if kind == 'ga':
        b = P['ui_btn']; x = W - 6 - b.width; y = by
        for (icon, label) in [('icon_scroll', '임무'), ('icon_basket', '상점'), ('icon_book', '도감')]:
            y -= b.height + 6; paste(im, b, x, y); ic = P[icon]
            paste(im, ic, x + (b.width - ic.width) // 2, y + 4); texts.append((label, x + b.width // 2, y + b.height - 10, 18, CREAM, 'mm', INK))
        paste(im, nine(P['ui_tab'], 300, 35, 12), 8, 44)
        paste(im, art('Ingredient/sesame'), 16, 44 + 3); texts.append(('풀찹쌀이가 참깨를 물어왔어요!', 50, 44 + 17, 24, INK, 'lm'))
    else:
        pw, ph = 172, 236; px = bx - 6 - pw; py = H - 28 - ph
        paste(im, nine(P['ui_panel'], pw, ph, 26), px, py)
        rb = P['ui_ribbon']; paste(im, rb, px + (pw - rb.width) // 2, py - 6); texts.append(('메뉴', px + pw // 2, py - 6 + 10, 24, CREAM, 'mm'))
        y = py + 34
        for (icon, label) in [('icon_book', '도감'), ('icon_basket', '상점'), ('icon_scroll', '임무'), (None, '설정')]:
            if icon: ic = P[icon]; paste(im, ic, px + 30 + (28 - ic.width) // 2, y + (40 - ic.height) // 2)
            texts.append((label, px + 76, y + 20, 28, INK, 'lm')); y += 44
    return texts


def font(size): return ImageFont.truetype(FONT, size)


def render(kind, H):
    world, info = scene(kind); im = world.crop((0, H_TALL - H, W, H_TALL)); off = H_TALL - H
    texts = ui(im, kind, H)
    for (label, cx, y) in info['labels']: texts.append((label, cx, y - off + 9, 22, CREAM, 'mm', INK))
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for t in texts:
        s, x, y, size, col, anchor = t[:6]; stroke = t[6] if len(t) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big, info


def board(kind, title, notes):
    a, info = render(kind, 960); b, _ = render(kind, 1200)
    M = 40; top = 150; cw = M * 3 + a.width * 2; chh = top + b.height + 40
    c = Image.new('RGBA', (cw, chh), (58, 50, 48, 255)); d = ImageDraw.Draw(c)
    d.text((M, 24), title, font=font(44), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170))
    d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2) · 아래로 붙이고 남는 세로는 하늘이 받는다', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top))
    y = top + a.height + 30
    for line in notes: d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB'), info


def parts_sheet():
    names = [('cafe_34', '가판대 3/4 (가)'), ('synth_34', '합성소 3/4 (가)'), ('cafe_front', '가판대 정면 (나)'), ('synth_front', '합성소 정면 (나)'), ('chest', '창고 상자'),
             ('forest', '먼 숲 띠'), ('clouds', '구름'), ('path', '길 (나)'), ('ui_panel', '종이 패널 (9조각 원본)'), ('ui_tab', '작은 판'), ('ui_capsule', '재화 캡슐'),
             ('ui_btn', '메뉴 버튼(빈)'), ('ui_btn_menu', '☰ 버튼'), ('ui_ribbon', '리본 제목 띠'), ('icon_book', '도감'), ('icon_basket', '상점'), ('icon_scroll', '임무'), ('icon_coin', '골드')]
    S = 2; cw = 1900; c = Image.new('RGBA', (cw, 2400), (119, 193, 132, 255)); d = ImageDraw.Draw(c)
    d.text((30, 20), '메인 화면 부품표 v1 · 2026-10-03 (×2 · 바탕은 풀색 #77C184)', font=font(40), fill=(40, 60, 45))
    x, y, rowh = 30, 100, 0
    for key, label in names:
        im = P[key]; big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        text = '%s  %d×%d' % (label.replace('☰', '햄버거'), im.width, im.height); tw = int(d.textlength(text, font=font(22)))
        cellw = max(big.width, tw) + 40
        if x + cellw > cw: x = 30; y += rowh + 70; rowh = 0
        c.alpha_composite(big, (x, y)); d.text((x, y + big.height + 8), text, font=font(22), fill=(40, 60, 45))
        x += cellw; rowh = max(rowh, big.height)
    return c.crop((0, 0, cw, y + rowh + 70)).convert('RGB')


if __name__ == '__main__':
    ga, i1 = board('ga', '메인 화면 시안 「가」 : 한 화면 고정', [
        '위에서부터 : 하늘(늘어남) → 먼 숲 띠 → 건물 줄(합성소 · 가판대 · 창고 상자, 3/4 시점)',
        '→ 농장 17칸×17줄. 오른쪽 : 주요 메뉴 3개(도감 · 상점 · 임무는 자리표시 이름) + 맨 아래 햄버거.',
        '왼쪽 위 : 재화 캡슐 둘 + 종이 알림 패널 본보기.'])
    na, i2 = board('na', '메인 화면 시안 「나」 : 건물 크게 + 길', [
        '위에서부터 : 하늘(늘어남) → 먼 숲 띠 → 큰 건물 줄(가판대는 정면 가게)',
        '→ 손님이 지나는 길 60 → 농장 17칸×15줄. 오른쪽 : 햄버거 하나만.',
        '눌러 펼친 목록(종이 패널 + 리본 제목)을 본보기로 열어 두었다.'])
    ga.save(os.path.join(OUT, '2026-10-03-메인화면-가.png')); na.save(os.path.join(OUT, '2026-10-03-메인화면-나.png'))
    parts_sheet().save(os.path.join(OUT, '2026-10-03-메인화면-부품표.png'))
    print('ga', i1['farm_top'], i1['horizon'], 'na', i2['farm_top'], i2['horizon'], {k: v.size for k, v in P.items()})

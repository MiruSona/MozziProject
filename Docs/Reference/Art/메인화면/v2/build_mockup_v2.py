# 메인 화면 「나」 v2 목업 + 찹쌀이 새 화풍 비교판 합성 스크립트 — 2026-10-03 반복 03
# PIL 은 자르기·배경 지우기·배치·9조각 이어 붙이기·확대·글자 얹기·곱하기 색 미리보기·기계적 외곽선만 한다.
# 그림을 그리거나 다시 칠하지 않는다. (예외 : 하늘 단색 바탕, 시안 바깥 여백과 설명 글)
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v2/build_mockup_v2.py
import os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw')
PARTS = os.path.join(HERE, 'parts')
V1 = os.path.join(HERE, '..', 'v1', 'parts')
CUST = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '손님', 'v2-새화풍', 'layers')
ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
OUT = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '시안')
os.makedirs(PARTS, exist_ok=True)

W = 540
H_TALL = 1200
SKY = (212, 238, 250, 255)
INK = (74, 44, 36)
CREAM = (255, 246, 226)


def raw(name): return Image.open(os.path.join(RAW, name + '.png')).convert('RGBA')
def v1(name): return Image.open(os.path.join(V1, name + '.png')).convert('RGBA')
def art(rel): return Image.open(os.path.join(ART, rel + '.png')).convert('RGBA')
def cust(name): return Image.open(os.path.join(CUST, name + '.png')).convert('RGBA')
def trim(im): return im.crop(im.getbbox())
def font(size): return ImageFont.truetype(FONT, size)


def nine(im, w, h, b):
    """9조각 : 모서리는 그대로, 변과 속은 타일처럼 이어 붙인다."""
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


def mech_outline(body, gray=150):
    """기계적 외곽선 : 몸 실루엣의 맨 바깥 1px(이웃 넷 중 하나가 투명)을 회색으로 바꾼다. 크기는 그대로다."""
    a = np.array(body); al = a[..., 3] > 0
    pad = np.pad(al, 1); inner = pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
    edge = al & ~inner
    a[edge, 0] = gray; a[edge, 1] = gray; a[edge, 2] = gray; a[edge, 3] = 255
    return Image.fromarray(a)


# ---------- 부품 (자르기·배경 지우기만) ----------
P = {}
P['mozzi_body_edit_a'] = raw('mozzi_edit_5001')         # 길 (가) : 지금 몸을 글로 고침
P['mozzi_body_edit_b'] = raw('mozzi_edit_5002')
P['mozzi_body_new'] = raw('mozzi_new_5101')             # 길 (나) : 새로 뽑음
P['mozzi_body_mech'] = mech_outline(art('Mozzi/body_hop0'))   # 길 (다) : 기계적 외곽선
P['grass'] = raw('grass32_5604')              # 고른 것 : 거의 민무늬 + 풀포기 둘
P['grass_busy'] = raw('grass32_5601')         # 버린 후보 : 무늬가 촘촘해 시끄럽다
P['grass_flower'] = raw('grass64_5603')       # 후보 : 꽃 든 64px (2×2 칸)
P['grass_alt'] = raw('grass64_5602')
P['fence_h'] = raw('fence_h_5201').crop((2, 0, 150, 32))    # 기둥 간격 37px × 4 = 되풀이 단위
fv = raw('fence_v_5202'); P['fence_v'] = fv.crop((fv.getbbox()[0], 16, fv.getbbox()[2], 144))
pl = raw('piles_5401')
P['pile_redbean'] = trim(pl.crop((0, 0, 30, 32))); P['pile_sesame'] = trim(pl.crop((32, 0, 64, 32))); P['pile_bean'] = trim(pl.crop((66, 0, 96, 32)))
P['ui_panel_light'] = raw('panel_5302').crop((15, 17, 142, 89))
P['icon_upgrade'] = trim(raw('icon_upgrade_5501'))
P['icon_gear'] = trim(raw('icon_gear_5502'))
for k, v in P.items(): v.save(os.path.join(PARTS, k + '.png'))
for k in ['cafe_front', 'synth_front', 'chest', 'forest', 'clouds', 'path', 'ui_btn', 'ui_btn_menu', 'ui_capsule', 'ui_ribbon', 'icon_book', 'icon_scroll', 'icon_coin']:
    P[k] = v1(k)      # v1 부품을 그대로 쓴다 (다시 저장하지 않는다)


def mozzi(body, color_hex, face, crown):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5))
    a = np.array(body).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0      # 게임과 같은 곱하기 색
    im = Image.fromarray(a.astype('uint8'))
    im.alpha_composite(art('Mozzi/face_%02d' % face))
    ck = art('Mozzi/cheek'); im.alpha_composite(ck, (14, 34)); im.alpha_composite(ck, (39, 34))
    if crown: im.alpha_composite(art('Mozzi/acc_crown'))
    return im


def customer(hair, shirt, bottom, face):
    im = cust('body')
    for n in ('bottom_' + bottom, 'shirt_' + shirt, 'face_%02d' % face, 'hair_' + hair): im.alpha_composite(cust(n))
    return im


def paste_clip(dst, im, x, y, top=0):
    x, y = int(x), int(y)
    if y + im.height <= top or y >= dst.height or x + im.width <= 0 or x >= dst.width: return
    if y < top: im = im.crop((0, top - y, im.width, im.height)); y = top
    x0 = max(0, -x); im = im.crop((x0, 0, max(x0, min(im.width, dst.width - x)), min(im.height, dst.height - y))); x = max(x, 0)
    if im.width > 0 and im.height > 0: dst.alpha_composite(im, (x, y))


def paste(dst, im, x, y): paste_clip(dst, im, x, y)
def paste_base(dst, im, cx, base_y): paste(dst, im, cx - im.width // 2, base_y - im.height)


def ground(dst, top, bottom):
    g = P['grass']
    for y in range(bottom - 32, top - 32, -32):
        for x in range(-2, W, 32): paste_clip(dst, g, x, y, top)


def fence(dst, top, bottom):
    h = P['fence_h']; v = P['fence_v']
    for y in range(top + 20, bottom, v.height):
        paste_clip(dst, v, 2, y); paste_clip(dst, v, W - 2 - v.width, y)
    for x in range(-4, W, h.width): paste_clip(dst, h, x, top); paste_clip(dst, h, x, bottom - 30)


def sky_and_forest(dst, horizon):
    f = P['forest']; band = Image.new('RGBA', (800, f.height)); band.paste(f, (0, 0)); band.paste(ImageOps.mirror(f), (400, 0))
    c = P['clouds']
    paste_clip(dst, c, 140, horizon - f.height - c.height + 10)
    paste_clip(dst, ImageOps.mirror(c), -150, horizon - f.height - 2 * c.height - 40)
    paste_clip(dst, c, 260, horizon - f.height - 3 * c.height - 110)
    paste(dst, band.crop((130, 0, 130 + W, f.height)), 0, horizon - f.height)


def scene():
    H = H_TALL; im = Image.new('RGBA', (W, H), SKY); info = {}
    farm_top = H - 480; path_top = farm_top - 60; horizon = path_top - 112
    sky_and_forest(im, horizon); ground(im, horizon, H)
    p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
    paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top); fence(im, farm_top, H)
    base = path_top + 8
    paste_base(im, P['synth_front'], 92, base); paste_base(im, P['cafe_front'], 296, base); paste_base(im, P['chest'], 470, base + 4)
    info['labels'] = [('합성소', 92, base + 34), ('가판대', 296, base + 34), ('보관함', 470, base + 34)]
    for (hair, shirt, bottom, face, cx) in [('02_brown', '01_blue', '01_navy', 1, 212), ('04_orange', '02_yellow', '02_beige', 2, 384), ('05_black', '03_green', '01_gray', 3, 26)]:
        paste(im, customer(hair, shirt, bottom, face), cx - 32, path_top + 52 - 76)      # 발 = y 76
    body = P['mozzi_body_edit_a']
    mz = [('#FFFFFF', 1, False, 70, 120), ('#4FA84A', 2, True, 200, 100), ('#7FC8F8', 1, True, 330, 160), ('#FFFFFF', 5, False, 120, 260),
          ('#3F9BE0', 2, True, 190, 310), ('#2E7D32', 1, True, 70, 390), ('#F5A05A', 1, True, 420, 250)]
    drops = [('pile_redbean', 150, 190), ('pile_sesame', 270, 90), ('pile_bean', 220, 380)]
    for (k, x, y) in drops: paste(im, P[k], x, farm_top + y)
    for (c, f, crown, x, y) in sorted(mz, key=lambda m: m[4]): paste(im, mozzi(body, c, f, crown), x - 32, farm_top + y - 32)
    info['farm_top'] = farm_top; info['horizon'] = horizon
    return im, info


def ui(im, H):
    texts = []
    cap = P['ui_capsule']; coin = P['icon_coin']
    paste(im, cap, 8, 8); paste(im, coin, 20, 8 + (cap.height - coin.height) // 2); texts.append(('1,250', 8 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    x2 = 8 + cap.width + 6; ing = P['pile_redbean']
    paste(im, cap, x2, 8); paste(im, ing, x2 + 10, 8 + (cap.height - ing.height) // 2); texts.append(('34', x2 + 66, 8 + cap.height // 2, 26, INK, 'mm'))
    # 오른쪽 아래 : 늘 보이는 「도감」「강화」 + 햄버거 (사용자 확정)
    bm = P['ui_btn_menu']; bx = W - 6 - bm.width; by = H - 28 - bm.height
    paste(im, bm, bx, by)
    b = P['ui_btn']; x = W - 6 - b.width; y = by
    for (icon, label) in [('icon_upgrade', '강화'), ('icon_book', '도감')]:
        y -= b.height + 6; paste(im, b, x, y); ic = P[icon]
        paste(im, ic, x + (b.width - ic.width) // 2, y + 4); texts.append((label, x + b.width // 2, y + b.height - 10, 18, CREAM, 'mm', INK))
    # 햄버거를 눌러 펼친 목록 (밝은 종이 패널)
    pw, ph = 196, 132; px = bx - 6 - pw; py = H - 28 - ph
    paste(im, nine(P['ui_panel_light'], pw, ph, 10), px, py)
    rb = P['ui_ribbon']; paste(im, rb, px + (pw - rb.width) // 2, py - 12); texts.append(('메뉴', px + pw // 2, py - 12 + 10, 24, CREAM, 'mm'))
    yy = py + 24
    for (icon, label) in [('icon_scroll', '레시피 도감'), ('icon_gear', '설정')]:
        ic = P[icon]; paste(im, ic, px + 18 + (28 - ic.width) // 2, yy + (44 - ic.height) // 2)
        texts.append((label, px + 56, yy + 22, 28, INK, 'lm')); yy += 46
    return texts


def render(H):
    world, info = scene(); im = world.crop((0, H_TALL - H, W, H_TALL)); off = H_TALL - H
    texts = ui(im, H)
    for (label, cx, y) in info['labels']: texts.append((label, cx, y - off + 9, 22, CREAM, 'mm', INK))
    big = im.resize((W * 2, H * 2), Image.NEAREST); d = ImageDraw.Draw(big)
    for t in texts:
        s, x, y, size, col, anchor = t[:6]; stroke = t[6] if len(t) > 6 else None
        d.text((x * 2, y * 2), s, font=font(size), fill=col, anchor=anchor, stroke_width=3 if stroke else 0, stroke_fill=stroke)
    return big, info


BG = (58, 50, 48, 255)


def board():
    a, info = render(960); b, _ = render(1200)
    M = 40; top = 150; cw = M * 3 + a.width * 2; chh = top + b.height + 40
    c = Image.new('RGBA', (cw, chh), BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 시안 「나」 v2 : 찹쌀이·풀·울타리·재료·패널을 새 화풍으로', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170))
    d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2) · 아래로 붙이고 남는 세로는 하늘이 받는다', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top))
    y = top + a.height + 30
    for line in ['찹쌀이 : 지금 몸을 글로 고친 판(회색 외곽선 + 3단 명암)에 종 색을 곱했다.',
                 '풀 · 울타리 · 재료 더미 · 종이 패널 : 새로 뽑았다. 손님 : 새 화풍 64×80 겹.',
                 '오른쪽 아래 메뉴는 「확정」이다 : 늘 보이는 도감 · 강화 + 햄버거 세로 줄. 건물은 눌러서 연다.',
                 '햄버거를 누르면 「레시피 도감」「설정」이 펼쳐진다 : 본보기로 열어 두었다.',
                 '건물 · 숲 · 구름 · 길 · 캡슐 · 버튼 · 글꼴은 v1 그대로다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB'), a


def compare(a2):
    old = Image.open(os.path.join(OUT, '2026-10-03-메인화면-나.png')).convert('RGBA').crop((40, 150, 40 + 1080, 150 + 1920))
    M = 40; top = 120; c = Image.new('RGBA', (M * 3 + 2160, top + 1920 + 40), BG); d = ImageDraw.Draw(c)
    d.text((M, 30), 'v1 「나」 (지금 찹쌀이 · 지금 풀 · 어두운 패널)', font=font(34), fill=(255, 240, 220))
    d.text((M * 2 + 1080, 30), 'v2 「나」 (새 화풍 찹쌀이 · 풀 · 울타리 · 밝은 패널 · 메뉴 확정안)', font=font(34), fill=(255, 240, 220))
    c.paste(old, (M, top)); c.paste(a2, (M * 2 + 1080, top))
    return c.convert('RGB')


def mozzi_sheet():
    species = [('흰', '#FFFFFF', False), ('풀', '#4FA84A', True), ('물', '#7FC8F8', True), ('불', '#F5A05A', True), ('상어', '#1F5FA8', True), ('화산', '#A82A1A', True)]
    rows = [('지금 (게임에 든 몸)', art('Mozzi/body_hop0')),
            ('(가)-1 지금 몸을 글로 고침 : PixelLab', P['mozzi_body_edit_a']),
            ('(가)-2 지금 몸을 글로 고침 : PixelLab (다른 씨앗)', P['mozzi_body_edit_b']),
            ('(나) 새로 뽑음 : PixelLab (모양이 달라져 얼굴이 안 맞는다)', P['mozzi_body_new']),
            ('(다) 기계적 외곽선 : 맨 바깥 1px 을 회색 150 으로 (명암 없음)', P['mozzi_body_mech'])]
    S = 3; cell = 64 * S; gap = 16; left = 40; top = 150; rowh = cell + 60
    cw = left * 2 + 7 * (cell + gap); chh = top + len(rows) * rowh + 150
    c = Image.new('RGBA', (cw, chh), (158, 206, 150, 255)); d = ImageDraw.Draw(c); ink = (40, 60, 45)
    d.text((left, 20), '찹쌀이 새 화풍 시안 · 2026-10-03 (×3 · 바탕은 새 풀색)', font=font(40), fill=ink)
    d.text((left, 78), '맨 왼쪽 칸은 색을 곱하기 전의 몸(흰 겹)이다. 나머지는 몸 × 종 색 + 볼 + 얼굴 + 왕관.', font=font(24), fill=ink)
    d.text((left + cell + gap, 118), '   '.join('%s %s' % (n, h) for n, h, _ in species), font=font(22), fill=ink)
    for r, (label, body) in enumerate(rows):
        y = top + r * rowh
        d.text((left, y), label, font=font(24), fill=ink)
        c.alpha_composite(body.resize((cell, cell), Image.NEAREST), (left, y + 30))
        for i, (n, hexc, crown) in enumerate(species):
            m = mozzi(body, hexc, 1 if i % 2 == 0 else 2, crown).resize((cell, cell), Image.NEAREST)
            c.alpha_composite(m, (left + (i + 1) * (cell + gap), y + 30))
    y = top + len(rows) * rowh + 10
    for line in ['겹 구조(흰 몸 × 종 색)를 그대로 두고 외곽선을 넣을 수 있다 : 회색 선이 곱해져 「그 색의 어두운 선」이 된다.',
                 '흰 찹쌀이만 선이 회색으로 남는다. 볼 · 얼굴 · 왕관은 (가)·(다) 몸에 그대로 맞는다 (몸 윤곽이 같다).',
                 '(가)는 뛰는 그림 4장(body_hop0~3) 가운데 hop0 한 장만 만들었다.']:
        d.text((left, y), line, font=font(22), fill=ink); y += 34
    return c.convert('RGB')


if __name__ == '__main__':
    b, a2 = board()
    b.save(os.path.join(OUT, '2026-10-03-메인화면-나-v2.png'))
    compare(a2).save(os.path.join(OUT, '2026-10-03-메인화면-나-v1v2비교.png'))
    mozzi_sheet().save(os.path.join(OUT, '2026-10-03-찹쌀이-새화풍.png'))
    print({k: v.size for k, v in P.items()})

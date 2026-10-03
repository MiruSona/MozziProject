# 메인 화면 「나」 v3 목업 + 변형 비교판 + 찹쌀이 뛰는 그림 확인표 — 2026-10-03 반복 03
# PIL 은 자르기·배치·이어 붙이기·확대·글자·곱하기 색 미리보기와, 아래 「기계적 손질」 넷만 한다 (그림에 방법을 적는다).
#   ① 볼 색 바꾸기 미리보기 (단색 치환)  ② 선 회색 150 → 110 (어두운 회색 픽셀만 같은 비율로 내림)
#   ③ 울타리를 32 칸에 맞추기 (가로대 줄을 잘라 이어 붙임)  ④ 풀 변형 타일의 바탕색을 기본 타일 바탕색으로 (단색 치환)
# 공용 함수와 v1·v2 부품은 ../v2/build_mockup_v2.py 를 불러 쓴다 (불러올 때 v2 parts 를 같은 내용으로 다시 쓴다).
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v3/build_mockup_v3.py
import os, sys, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v2', os.path.join(HERE, '..', 'v2', 'build_mockup_v2.py'))
v2 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v2)
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)
REF = os.path.join(v2.ROOT, 'Docs', 'Reference', 'Art')
W, H_TALL, INK, CREAM, OUT, font, art, nine, paste, paste_base, paste_clip = v2.W, v2.H_TALL, v2.INK, v2.CREAM, v2.OUT, v2.font, v2.art, v2.nine, v2.paste, v2.paste_base, v2.paste_clip
P = dict(v2.P)


def raw(n): return Image.open(os.path.join(RAW, n + '.png')).convert('RGBA')
def op(p): return Image.open(p).convert('RGBA')


# ---------- 기계적 손질 ----------
def line_dark(body, to=110, frm=150, cut=180):
    """② 몸 그림에서 회색값이 cut 보다 어두운 픽셀(외곽선)만 to/frm 배로 내린다. 명암(밝은 회색)은 그대로."""
    a = np.array(body).astype(float); m = (a[..., 3] > 0) & (a[..., :3].max(-1) < cut)
    a[m, :3] = a[m, :3] * to / frm
    return Image.fromarray(a.astype('uint8'))


CHEEK_MAIN = (251, 215, 1)


def cheek_tint(target):
    """① 볼 그림의 노랑을 target 색으로 바꾼다. 가장자리 옅은 픽셀은 흰색 쪽으로 같은 비율만큼 옅게 둔다."""
    a = np.array(art('Mozzi/cheek')).astype(float); s = np.clip((255 - a[..., 2]) / (255 - CHEEK_MAIN[2]), 0, 1)
    for i in range(3): a[..., i] = 255 + (target[i] - 255) * s
    return Image.fromarray(a.astype('uint8'))


def fence32(strip, horizontal):
    """③ 기둥 간격 26px 로 온 울타리에서 [기둥 + 가로대 26px] 을 자르고 가로대 6px 을 한 번 더 이어 32px 타일로 만든다."""
    if horizontal:
        t = Image.new('RGBA', (32, 32)); t.paste(strip.crop((13, 0, 39, 32)), (0, 0)); t.paste(strip.crop((24, 0, 30, 32)), (26, 0))
    else:
        t = Image.new('RGBA', (32, 32)); t.paste(strip.crop((0, 8, 32, 34)), (0, 0)); t.paste(strip.crop((0, 22, 32, 28)), (0, 26))
    return t


def base_match(tile, ref):
    """④ 변형 타일에서 가장 많은 색(바탕)을 기본 타일의 바탕색으로 바꾼다."""
    def top(im): return max(im.getcolors(4096), key=lambda c: c[0])[1]
    a = np.array(tile); b0 = np.array(top(tile)); b1 = np.array(top(ref))
    m = (np.abs(a[..., :3].astype(int) - b0[:3]).sum(-1) <= 14); a[m, :3] = b1[:3]
    return Image.fromarray(a)


# ---------- 부품 ----------
N = {}
N['mozzi_body_hop0'] = P['mozzi_body_edit_a']
for i in (1, 2, 3): N['mozzi_body_hop%d' % i] = raw('mozzi_hop%d_edit_5001' % i)
for i in range(4): N['mozzi_body_hop%d_line110' % i] = line_dark(N['mozzi_body_hop%d' % i])
N['grass_0'] = P['grass']
N['grass_1'] = base_match(raw('grass_flower_7201'), P['grass']); N['grass_2'] = base_match(raw('grass_pebble_7202'), P['grass'])
fh = raw('fence_h_7101'); fv = raw('fence_v_7102')
N['fence_top'] = fence32(fh, True); N['fence_bottom'] = N['fence_top']; N['fence_side'] = fence32(fv, False)
ct = N['fence_side'].copy(); ct.alpha_composite(N['fence_top'], (6, -6)); N['fence_corner_top'] = ct
cb = N['fence_side'].copy(); cb.alpha_composite(N['fence_bottom'], (6, 2)); N['fence_corner_bottom'] = cb
CHEEKS = [('지금 : 샛노랑 #FBD701', art('Mozzi/cheek')), ('낮은 채도 1 : 살구 분홍 #F2A99A', cheek_tint((242, 169, 154))), ('낮은 채도 2 : 바랜 노랑 #F0D488', cheek_tint((240, 212, 136)))]
N['cheek_low1'] = CHEEKS[1][1]; N['cheek_low2'] = CHEEKS[2][1]
N['shop_big'] = op(os.path.join(REF, '가판대화면', 'v1', 'parts', 'shop.png')); N['machine_big'] = op(os.path.join(REF, '합성소화면', 'v1', 'parts', 'machine.png'))
for k, v in N.items(): v.save(os.path.join(PARTS, k + '.png'))


def mozzi(body, color_hex, face, crown, cheek):
    c = tuple(int(color_hex[i:i + 2], 16) for i in (1, 3, 5)); a = np.array(body).astype(float)
    for i in range(3): a[..., i] = a[..., i] * c[i] / 255.0
    im = Image.fromarray(a.astype('uint8')); im.alpha_composite(art('Mozzi/face_%02d' % face))
    im.alpha_composite(cheek, (14, 34)); im.alpha_composite(cheek, (39, 34))
    if crown: im.alpha_composite(art('Mozzi/acc_crown'))
    return im


# ---------- 메인 화면 v3 ----------
def scene():
    H = H_TALL; im = Image.new('RGBA', (W, H), v2.SKY); info = {}
    farm_top = H - 480; path_top = farm_top - 60; horizon = path_top - 112
    v2.sky_and_forest(im, horizon)
    rnd = random.Random(20261003)
    for y in range(H - 32, horizon - 32, -32):
        for x in range(-2, W, 32):
            r = rnd.random(); paste_clip(im, N['grass_0'] if r < 0.82 else (N['grass_2'] if r < 0.92 else N['grass_1']), x, y, horizon)
    p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
    paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top)
    cols = list(range(-2, W, 32)); rows = list(range(farm_top, H, 32))        # 울타리 : 32 칸 다섯 조각
    for y in rows:
        for x in cols:
            l, r_ = x == cols[0], x == cols[-1]
            if y == rows[0]: t = N['fence_corner_top'] if l else (ImageOps.mirror(N['fence_corner_top']) if r_ else N['fence_top'])
            elif y == rows[-1]: t = N['fence_corner_bottom'] if l else (ImageOps.mirror(N['fence_corner_bottom']) if r_ else N['fence_bottom'])
            elif l: t = N['fence_side']
            elif r_: t = ImageOps.mirror(N['fence_side'])
            else: continue
            paste_clip(im, t, x, y)
    base = path_top + 8
    paste_base(im, N['machine_big'], 118, base); paste_base(im, N['shop_big'], 352, base); paste_base(im, P['chest'], 500, base + 30)
    info['labels'] = [('합성소', 118, base + 34), ('가판대', 352, base + 34), ('보관함', 500, base + 34)]
    for (hair, shirt, bottom, face, cx) in [('02_brown', '01_blue', '01_navy', 1, 250), ('04_orange', '02_yellow', '02_beige', 2, 430), ('05_black', '03_green', '01_gray', 3, 26)]:
        paste(im, v2.customer(hair, shirt, bottom, face), cx - 32, path_top + 52 - 76)
    mz = [('#FFFFFF', 1, False, 70, 120), ('#4FA84A', 2, True, 200, 100), ('#7FC8F8', 1, True, 330, 160), ('#FFFFFF', 5, False, 120, 260),
          ('#3F9BE0', 2, True, 190, 310), ('#2E7D32', 1, True, 70, 390), ('#F5A05A', 1, True, 420, 250)]
    for (k, x, y) in [('pile_redbean', 150, 190), ('pile_sesame', 270, 90), ('pile_bean', 220, 380)]: paste(im, P[k], x, farm_top + y)
    for (c, f, crown, x, y) in sorted(mz, key=lambda m: m[4]): paste(im, mozzi(N['mozzi_body_hop0_line110'], c, f, crown, N['cheek_low1']), x - 32, farm_top + y - 32)
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
    d.text((M, 24), '메인 화면 시안 「나」 v3 (임시 값) : 큰 건물 · 32 칸 울타리 · 풀 3종', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170)); d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2)', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top)); y = top + a.height + 30
    for line in ['「임시」로 고른 값 (사용자 확정 전) : 볼 = 낮은 채도 1(살구 분홍) · 찹쌀이 선 회색 110 · 크기 ×1.',
                 '건물 : 가판대 · 합성소 화면 시안에서 뽑은 큰 가게(240×200)와 떡 기계(230×227)를 그대로 놓았다.',
                 '울타리 : 새로 뽑은 가로 · 세로 띠를 32 칸 조각 다섯으로 잘라 맞췄다 (기계적 손질 ③).',
                 '풀 : 3종을 게임과 같은 무게(0.82 / 0.10 / 0.08)로 섞었다. 변형 2종은 바탕색을 맞췄다 (④).',
                 '메뉴(확정) · 길 · 숲 · 구름 · 손님 · 재료 더미 · 패널은 v2 그대로다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


GREEN = (158, 206, 150, 255); DINK = (40, 60, 45)


def variants():
    S = 4; cell = 64 * S; c = Image.new('RGBA', (2000, 2450), GREEN); d = ImageDraw.Draw(c)
    d.text((40, 20), '메인 화면 v3 : 고를 것 셋 (변형 비교) · 2026-10-03', font=font(40), fill=DINK)
    y = 100; d.text((40, y), '(a) 볼 색 : 볼 그림의 노랑을 한 색으로 바꾼 미리보기 (PIL 단색 치환 · 새로 그린 것 아님). ×4', font=font(26), fill=DINK); y += 44
    sp = [('#FFFFFF', False), ('#4FA84A', True), ('#7FC8F8', True), ('#A82A1A', True)]; body = N['mozzi_body_hop0_line110']
    for (label, ck) in CHEEKS:
        d.text((40, y), label, font=font(24), fill=DINK)
        for i, (hx, cr) in enumerate(sp): c.alpha_composite(mozzi(body, hx, 1 if i % 2 == 0 else 2, cr, ck).resize((cell, cell), Image.NEAREST), (40 + i * (cell + 20), y + 20))
        y += cell - 10
    y += 40; d.text((40, y), '(b) 찹쌀이 선 회색 : 150(시안 v2 그대로) / 110(어두운 회색 픽셀만 같은 비율로 내림 · PIL). ×4', font=font(26), fill=DINK); y += 44
    for (label, bd) in [('선 150', N['mozzi_body_hop0']), ('선 110', N['mozzi_body_hop0_line110'])]:
        d.text((40, y), label, font=font(24), fill=DINK)
        c.alpha_composite(bd.resize((cell, cell), Image.NEAREST), (40, y + 20))
        for i, (hx, cr) in enumerate(sp): c.alpha_composite(mozzi(bd, hx, 1, cr, N['cheek_low1']).resize((cell, cell), Image.NEAREST), (40 + (i + 1) * (cell + 20), y + 20))
        y += cell - 10
    y += 40; d.text((40, y), '(c) 찹쌀이 크기 : ×1(64px) / ×1.25(80px · 최근접 확대라 도트가 깨진다). 옆은 손님 64×80. ×3', font=font(26), fill=DINK); y += 50
    cu = v2.customer('02_brown', '01_blue', '01_navy', 1); x = 40
    for (label, k) in [('×1', 1.0), ('×1.25', 1.25)]:
        m = mozzi(body, '#4FA84A', 2, True, N['cheek_low1']); n = int(64 * k); m = m.resize((n, n), Image.NEAREST)
        g = Image.new('RGBA', (170, 100), GREEN); g.alpha_composite(m, (0, 100 - n + int(13 * k))); g.alpha_composite(cu, (96, 100 - 80 + 4))
        c.alpha_composite(g.resize((510, 300), Image.NEAREST), (x, y + 30)); d.text((x, y), label, font=font(24), fill=DINK); x += 560
    z = mozzi(body, '#4FA84A', 2, True, N['cheek_low1']).resize((80, 80), Image.NEAREST).crop((10, 14, 70, 70)).resize((480, 448), Image.NEAREST)
    c.alpha_composite(z, (x, y + 30)); d.text((x, y), '×1.25 를 크게 본 것 : 선이 1px 과 2px 로 들쭉날쭉하다', font=font(24), fill=DINK)
    y += 500
    for line in ['(c) 대안 : 찹쌀이를 키우려면 확대가 아니라 80px 로 새로 뽑아야 한다 (몸 · 볼 · 얼굴 10 · 왕관 · 뛰는 그림 4장 전부).',
                 '       손님을 0.75 배로 줄이는 것도 같은 까닭으로 도트가 깨진다. 지금 비율(찹쌀이 42px 폭 · 손님 38px 폭)로도 화면에서는 어색하지 않다.']:
        d.text((40, y), line, font=font(24), fill=DINK); y += 36
    return c.crop((0, 0, 2000, y + 30)).convert('RGB')


def hop_sheet():
    S = 3; cell = 64 * S; c = Image.new('RGBA', (40 + 4 * (cell + 16) + 760, 150 + 4 * (cell + 50) + 60), GREEN); d = ImageDraw.Draw(c)
    d.text((40, 20), '찹쌀이 새 화풍 : 뛰는 그림 4장 확인표 (×3)', font=font(40), fill=DINK)
    d.text((40, 80), 'hop0 ~ hop3 을 모두 「지금 몸을 PixelLab 글 고침」 한 방법 · 같은 글 · 같은 씨앗(5001)으로 만들었다.', font=font(24), fill=DINK)
    rows = [('지금 (게임에 든 몸)', [art('Mozzi/body_hop%d' % i) for i in range(4)], None), ('새 몸 (선 150 안팎)', [N['mozzi_body_hop%d' % i] for i in range(4)], None),
            ('새 몸 선 110 × 풀색 + 볼 · 얼굴 · 왕관 (hop0 자리 그대로)', [N['mozzi_body_hop%d_line110' % i] for i in range(4)], '#4FA84A'),
            ('새 몸 선 110 × 흰색 + 볼 · 얼굴', [N['mozzi_body_hop%d_line110' % i] for i in range(4)], '#FFFFFF')]
    y = 130; stats = []
    for (label, ims, hx) in rows:
        d.text((40, y), label, font=font(24), fill=DINK)
        for i, im in enumerate(ims):
            m = mozzi(im, hx, 1, hx != '#FFFFFF', N['cheek_low1']) if hx else im
            c.alpha_composite(m.resize((cell, cell), Image.NEAREST), (40 + i * (cell + 16), y + 30))
        y += cell + 50
    x = 40 + 4 * (cell + 16) + 20; yy = 160
    d.text((x, yy), '확인 (스크립트가 잰 값)', font=font(26), fill=DINK); yy += 44
    for i in range(4):
        o = np.array(art('Mozzi/body_hop%d' % i)); n = np.array(N['mozzi_body_hop%d' % i])
        diff = int(((o[..., 3] > 0) != (n[..., 3] > 0)).sum()); al = n[..., 3] > 0
        pad = np.pad(al, 1); inner = pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]; edge = al & ~inner
        g = n[..., :3].max(-1)[edge]; dark = int((g < 180).sum()); stats.append((i, diff, int(edge.sum()), dark, int(g.min()), int(np.median(g))))
        d.text((x, yy), 'hop%d : 윤곽 다른 픽셀 %d · 가장자리 %dpx 중 어두운 선 %dpx' % (i, diff, int(edge.sum()), dark), font=font(22), fill=DINK); yy += 32
        d.text((x, yy), '          가장자리 회색 최소 %d · 가운데값 %d' % (int(g.min()), int(np.median(g))), font=font(22), fill=DINK); yy += 40
    for line in ['윤곽은 네 장 모두 지금 그림과 똑같다 (다른 픽셀 0).', '선은 모두 맨 바깥 1px 이다.', '선 회색은 장마다 다르다 (가운데값 137 ~ 164) :', '넣기 전에 한 값으로 맞춰야 깜빡이지 않는다.', '얼굴 · 볼 · 왕관은 hop0 자리에 고정해 얹었다.', '게임이 장마다 얼굴 자리를 옮기는지는 안 봤다.']:
        d.text((x, yy), line, font=font(22), fill=DINK); yy += 32
    return c.convert('RGB'), stats


if __name__ == '__main__':
    board().save(os.path.join(OUT, '2026-10-03-메인화면-나-v3.png'))
    variants().save(os.path.join(OUT, '2026-10-03-메인화면-나-v3-변형.png'))
    hs, stats = hop_sheet(); hs.save(os.path.join(OUT, '2026-10-03-찹쌀이-새화풍-hop.png'))
    print(stats)

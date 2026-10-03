# 잔디 바탕 밝게 — 후보 b1~ (2026-10-04)
# 지금 grass_base.png 는 그대로 두고 out/grass_base_b*.png 후보와 비교 그림 하나를 굽는다.
# PIL 은 자르기 · 배치 · 정수배 확대 · 글자 · 단색 바꾸기(팔레트 색 일대일)만 한다. 새 그림을 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/build_grass_b.py
import os, sys, colorsys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw'); OUTP = os.path.join(HERE, 'out')


def op(p): return Image.open(p).convert('RGBA')


BASE = op(os.path.join(OUTP, 'grass_base.png'))      # 지금 바탕 (읽기만)


def palette_swap(im, dh, dl, ks_light, ks_dark):
    """팔레트 색마다 HLS 로 색상 dh 도 · 밝기 +dl · 채도 ×ks 로 옮긴다 (밝은 바탕 색은 ks_light, 어두운 풀잎 획은 ks_dark).
    색 하나가 색 하나로만 간다 (일대일). 무늬(어느 픽셀이 어느 색인지)는 그대로다."""
    a = np.array(im); rgb = a[..., :3]
    cols = np.unique(rgb.reshape(-1, 3), axis=0); out = a.copy(); mp = {}
    for c in cols:
        h, l, s = colorsys.rgb_to_hls(*(c / 255.0)); k = ks_light if l > 0.5 else ks_dark
        n = tuple(int(round(v * 255)) for v in colorsys.hls_to_rgb(((h * 360 + dh) % 360) / 360, min(1, l + dl), min(1, s * k)))
        mp[tuple(int(x) for x in c)] = n; out[(rgb == c).all(-1), :3] = n
    assert len(set(mp.values())) == len(mp), '두 색이 한 색으로 합쳐졌다'
    return Image.fromarray(out), mp


# b1 : 지금 바탕을 색만 옮김 — 색상 -10 · 밝기 +0.08 · 바탕 채도 ×2.4 · 획 채도 ×1.6
B1_ARGS = (-10, 0.08, 2.4, 1.6)


def seam_score(t):
    """이음매 값 = (오른끝↔왼끝 · 아래끝↔위끝 이웃 차이 평균) / (안쪽 이웃 차이 평균). 1 근처면 줄이 안 보인다."""
    a = np.array(t.convert('RGB')).astype(float)
    inner = (np.abs(np.diff(a, axis=1)).mean() + np.abs(np.diff(a, axis=0)).mean()) / 2
    edge = (np.abs(a[:, -1] - a[:, 0]).mean() + np.abs(a[-1] - a[0]).mean()) / 2
    return edge / inner


def dark_lift(im, dl, ks):
    """어두운 색(밝기 0.5 아래 = 풀잎 획)만 밝기 +dl · 채도 ×ks 로 옮긴다. 바탕 색은 그대로. 일대일 단색 바꾸기."""
    a = np.array(im); rgb = a[..., :3]; out = a.copy(); seen = set()
    for c in np.unique(rgb.reshape(-1, 3), axis=0):
        h, l, s = colorsys.rgb_to_hls(*(c / 255.0))
        n = tuple(int(c_) for c_ in c) if l >= 0.5 else tuple(int(round(v * 255)) for v in colorsys.hls_to_rgb(h, min(1, l + dl), min(1, s * ks)))
        assert n not in seen, '두 색이 한 색으로 합쳐졌다'; seen.add(n); out[(rgb == c).all(-1), :3] = n
    return Image.fromarray(out)


# b2 : PixelLab 새로 뽑음 (create_image_pro_flash 9201 · 화풍 그림 = b1 왼쪽 위 64×64)
#      → workbench copy_region 넷으로 반 칸(128,128) 밀기 → inpaint_image_pro_flash 가운데 십자 16px (9211)
#      → 풀잎 획이 지금 바탕보다 진해서 어두운 색만 밝기 +0.12 · 채도 ×0.85
B2_RAW = 'grass_base_b_9201_shift_inpaint_9211'
B2_ARGS = (0.12, 0.85)


# b3 : b1 보다 한 걸음 더 — 색상 -14 · 밝기 +0.10 · 바탕 채도 ×2.8 · 획 채도 ×1.8
B3_ARGS = (-14, 0.10, 2.8, 1.8)

# ---------- 미리보기 : 메인 화면 540×960 에 후보마다 깔아 본다 ----------
SIAN = os.path.join(HERE, '..', '..', '시안')
MAIN_OUT = os.path.join(HERE, '..', '메인', 'out')
W, H = 540, 960
FARM_TOP = H - 480; PATH_TOP = FARM_TOP - 60; HORIZON = PATH_TOP - 112; FEET = PATH_TOP + 8     # 정식/메인/build_main.py 와 같은 값
PLACE = {'synthesis_main': (88, FEET), 'stall_main': (306, FEET), 'storage_main': (484, PATH_TOP + 50)}
ICON_X = W - 2 - 56
ICONS = [('icon_menu', ICON_X, H - 24 - 56), ('icon_upgrade', ICON_X, H - 24 - 56 * 2), ('icon_codex', ICON_X, H - 24 - 56 * 3)]


def main_scene(grass, bf):
    """정식 부품(하늘 · 길 · 건물 셋 · 아이콘)과 정식/농장 부품(장식 · 울타리 · 찹쌀이)으로 540×960 한 장."""
    m = lambda n: op(os.path.join(MAIN_OUT, n + '.png'))
    im = Image.new('RGBA', (W, H), (212, 238, 250, 255))
    sky = m('sky'); im.alpha_composite(sky, (0, HORIZON - sky.height))
    im.alpha_composite(bf.tile_ground(grass, W, H - HORIZON), (0, HORIZON))      # 건물 뒤 풀밭도 같은 바탕
    im.alpha_composite(m('path_main'), (0, PATH_TOP))
    for name, (cx, by) in PLACE.items():
        o = m(name); im.alpha_composite(o, (cx - o.width // 2, by - o.height))
    farm = bf.farm(bf.tile_ground(grass, bf.FARM_W, bf.FARM_H), 20261003, bf.herd(12, 7))
    im.alpha_composite(farm, (0, FARM_TOP))
    for name, x, y in ICONS: im.alpha_composite(m(name), (x, y))
    return im


def board(cands):
    import build_farm as bf
    from PIL import ImageDraw
    S = 2; M = 40; pw = W * S
    c = Image.new('RGBA', (M + (pw + M) * len(cands), 3600), bf.BG)
    y = bf.header(c, '농장 잔디 바탕 밝게 : 지금 vs 후보 (×2) · 2026-10-04',
                  ['전부 같은 화면 · 같은 장식 · 같은 찹쌀이. 바탕 grass_base 만 바꿔 깔았다. 아래 줄은 바탕을 2×2 로 깐 것(×2) : 가운데 십자가 256 이음매다.',
                   'b1 · b3 = 지금 바탕의 팔레트 색만 일대일로 옮김 (무늬 그대로, 크레딧 0). b2 = PixelLab 으로 새로 뽑고 반 칸 밀어 십자를 inpaint (18 생성).'])
    d = ImageDraw.Draw(c); y += 10
    for i, (label, sub, g) in enumerate(cands):
        x = M + i * (pw + M)
        d.text((x, y), label, font=bf.font(30), fill=bf.SUB); d.text((x, y + 42), sub, font=bf.font(20), fill=bf.TXT)
        c.alpha_composite(main_scene(g, bf).resize((W * S, H * S), Image.NEAREST), (x, y + 80))
        yy = y + 80 + H * S + 30
        c.alpha_composite(bf.tile_ground(g, 512, 512).resize((1024, 1024), Image.NEAREST), (x + (pw - 1024) // 2, yy))
        a = np.array(g)[..., :3].reshape(-1, 3); cols, cnt = np.unique(a, axis=0, return_counts=True); top = tuple(int(v) for v in cols[cnt.argmax()])
        d.text((x, yy + 1034), '바탕 대표색 #%02X%02X%02X · 평균 밝기 %.0f · 이음매 값 %.2f' % (top + (a.mean(), seam_score(g))), font=bf.font(22), fill=bf.TXT)
    return c.crop((0, 0, c.width, y + 80 + H * S + 30 + 1034 + 50)).convert('RGB')


if __name__ == '__main__':
    sys.path.insert(0, HERE)
    b1, mp = palette_swap(BASE, *B1_ARGS)
    b1.save(os.path.join(OUTP, 'grass_base_b1.png'))
    print('b1 바탕 대표색', mp[(152, 167, 125)], '획 대표색', mp[(85, 109, 81)])
    b2 = dark_lift(op(os.path.join(RAW, B2_RAW + '.png')), *B2_ARGS); b2.save(os.path.join(OUTP, 'grass_base_b2.png'))
    b3, mp3 = palette_swap(BASE, *B3_ARGS); b3.save(os.path.join(OUTP, 'grass_base_b3.png'))
    print('b3 바탕 대표색', mp3[(152, 167, 125)], '획 대표색', mp3[(85, 109, 81)])
    cands = [('지금 grass_base', '회색기 도는 녹색 (PixelLab 9104 + 십자 9111)', BASE),
             ('b1 · 색만 옮김 (중간)', '색상 -10 · 밝기 +0.08 · 채도 ×2.4 / 획 ×1.6', b1),
             ('b2 · PixelLab 새로', '9201 + 십자 inpaint 9211 · 획만 밝기 +0.12', b2),
             ('b3 · 색만 옮김 (더 밝고 노랗게)', '색상 -14 · 밝기 +0.10 · 채도 ×2.8 / 획 ×1.8', b3)]
    board(cands).save(os.path.join(SIAN, '2026-10-04-잔디바탕-밝게.png'))
    for n in ['grass_base'] + ['grass_base_b%d' % i for i in range(1, 4)]:
        p = os.path.join(OUTP, n + '.png')
        if os.path.exists(p): print(n, '이음매 값 %.2f' % seam_score(op(p)))

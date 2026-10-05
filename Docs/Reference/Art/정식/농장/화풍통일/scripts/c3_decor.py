# C-3 — 풀 장식 4 (decor_00~03) · 꽃 장식 3 (decor_04~06) 의 풀색을 잔디 바탕(b1) 쪽으로 옮긴다.
# 풀색 12가지를 색마다 일대일로 옮긴다 (색상 · 밝기 · 채도). 무늬 · 실루엣은 그대로다.
# 꽃잎 · 꽃술 · 돌 색은 표에 없어 안 바뀐다 (꽃은 잎 · 줄기만 바뀐다).
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/c3_decor.py
import colorsys
import os
from PIL import Image
from common import OUT, SIAN, res, hex_rgb, rgb_hex, colors_of, remap, same_alpha, tile, zoom, label

# 풀 장식이 쓰는 풀색 12 (어두움 → 밝음). 꽃 장식의 잎 · 줄기도 같은 색이다.
GREENS = ['#2E3E2D', '#364935', '#3F533D', '#4A5D49', '#566A54', '#5F755D',
          '#688065', '#708A6D', '#799274', '#849D7C', '#95AD88', '#AABE96']
# 옮기는 값. 설계 출발점은 잔디 b1 과 같은 「색상 -10 · 밝기 +0.08 · 채도 ×2.4」 였다.
# 눈으로 보고 맞춘 값 : 밝은 잎은 바탕(#C0CF7D)보다 밝아지면 덩이가 풀밭에 녹아 밝은 쪽 밝기는 오히려 조금 내렸다 (DL_LIGHT).
DH = -14          # 색상 (도). 풀잎 획(#698F58, 약 101도)과 바탕(71도) 사이로
KS_DARK = 1.9     # 어두운 색 (둘레 · 그늘) 채도 배율
KS_LIGHT = 2.4    # 밝은 색 (잎 면) 채도 배율
DL_DARK = 0.03    # 어두운 색 밝기 더하기
DL_LIGHT = -0.03  # 밝은 색 밝기 더하기
NAMES = ['decor_%02d.png' % i for i in range(7)]


def shift(c):
    h, l, s = colorsys.rgb_to_hls(*(v / 255.0 for v in c))
    t = min(1.0, max(0.0, (l - 0.2) / 0.45))                  # 0 = 가장 어두운 색, 1 = 가장 밝은 색
    ks = KS_DARK + (KS_LIGHT - KS_DARK) * t
    dl = DL_DARK + (DL_LIGHT - DL_DARK) * t
    dh = DH * (0.5 + 0.5 * t)                                 # 밝은 잎일수록 노란 쪽으로 더 (바탕과 같은 쪽)
    r, g, b = colorsys.hls_to_rgb(((h * 360 + dh) % 360) / 360, min(1, l + dl), min(1, s * ks))
    return tuple(int(round(v * 255)) for v in (r, g, b))


def table():
    t = {hex_rgb(g): shift(hex_rgb(g)) for g in GREENS}
    assert len(set(t.values())) == len(t), '두 색이 한 색으로 합쳐졌다'
    return t


def board(pairs, grass):
    """잔디 위 지금 / 고친 뒤 — 1배와 ×3."""
    n = len(pairs)
    sw, sh = n * 40 + 8, 44
    k = 3
    paper = (253, 252, 245, 255)
    W = 16 + sw * k + 16
    H = 40 + (sh + 26) * 2 + (sh * k + 26) * 2 + 16
    im = Image.new('RGBA', (W, H), paper)
    label(im, (16, 10), 'C-3 풀 장식 4 · 꽃 장식 3 — 잔디 바탕 위 (왼쪽부터 decor_00 ~ 06)', 17)
    y = 40
    for scale in (1, k):
        for title, idx in (('지금', 0), ('고친 뒤', 1)):
            strip = tile(grass, sw, sh)
            for i, pair in enumerate(pairs):
                strip.alpha_composite(pair[idx], (8 + i * 40, 8))
            label(im, (16, y), '%s · %s' % (title, '1배' if scale == 1 else '×%d' % scale), 14)
            im.alpha_composite(zoom(strip, scale), (16, y + 22))
            y += sh * scale + 26
    return im.convert('RGB')


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(SIAN, exist_ok=True)
    t = table()
    for old, new in t.items():
        print('%s → %s' % (rgb_hex(old), rgb_hex(new)))
    pairs = []
    for name in NAMES:
        src = res('Farm/' + name)
        dst = remap(src, t)
        assert same_alpha(src, dst), name + ' : 알파가 달라졌다'
        assert len(colors_of(dst)) == len(colors_of(src)), name + ' : 색 수가 달라졌다'
        dst.save(os.path.join(OUT, name))
        pairs.append((src, dst))
        print('%s : %d색 · 바뀐 색 %d가지' % (name, len(colors_of(dst)), sum(1 for c in colors_of(src) if c in t)))
    board(pairs, res('Farm/grass_base.png')).save(os.path.join(SIAN, 'c3_deco_on_grass.png'))


if __name__ == '__main__':
    main()

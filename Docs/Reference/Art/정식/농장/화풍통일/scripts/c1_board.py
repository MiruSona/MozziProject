# C-1 시안 판 — 찹쌀이 몸 선 색 셋(지금 회색 · 따뜻한 후보 둘)을 농장 잔디 위에 건물 · 울타리와 함께 놓는다.
# 맨 아래 줄은 「지금 칠」 대 「명암 1단 더한 칠」 견줌이다 (보이기만 — 고칠지는 따로 정한다).
# 먼저 c2_merge_body.py 와 c1_line.py (후보 둘) 를 돌려 둔다. 이 스크립트가 없으면 스스로 굽는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/c1_board.py
import os
import numpy as np
from PIL import Image
from common import OUT, SIAN, LINE_NOW, BODIES, op, res, hex_rgb, rgb_hex, tint, tile, zoom, label, remap, same_alpha
import c1_line

CANDS = [('지금 — 회색 #6E6E6E', LINE_NOW),
         ('후보 1 — 따뜻한 회갈색 #6E6259', hex_rgb('#6E6259')),
         ('후보 2 — 짙은 회갈색 #5E5048', hex_rgb('#5E5048'))]
# 종 색 (GameData/mozzi_species.json 의 color_hex — 게임은 몸 그림에 이 색을 곱한다. 선도 같이 곱해진다)
SPECIES = [('흰찹쌀이 #FFFFFF', hex_rgb('#FFFFFF'), False),
           ('풀찹쌀이 #4FA84A', hex_rgb('#4FA84A'), True),
           ('화산찹쌀이 #A82A1A', hex_rgb('#A82A1A'), True)]
PAPER = (253, 252, 245, 255)
PW, PH = 420, 420                 # 한 판(1배) 크기
PATH_TOP = 150                    # 길 윗줄. 건물 발은 그 8줄 아래 (정식/메인 과 같은 값)
FARM_TOP = PATH_TOP + 60
CROP = (0, PATH_TOP - 24, 300, PH)   # 확대해 볼 자리 (건물 발 · 울타리 · 찹쌀이)
K = 3
# 그늘 색 (mozzi_white 램프) — 「명암 1단 더한 칠」 에만 쓴다
SHADE1 = (0xDE, 0xDE, 0xDE)
SHADE2 = (0xCC, 0xCC, 0xCC)


def bodies_of(line):
    """선 색을 바꾼 몸 4장."""
    return [c1_line.reline(op(os.path.join(OUT, n)), line)[0] for n in BODIES]


def mozzi(body, color, crown, cheek, face, acc):
    """게임과 같은 겹 순서 : 몸(× 종 색) → 볼 둘 → 표정 → 악세. 볼 가운데는 몸 가운데에서 좌우 12.5 · 아래 7.5."""
    im = tint(body, color)
    # hop0 의 볼 자리는 코드 값 그대로 (x14 · x39 · y34). 뜀 장면(hop1~3)은 게임에서 애니메이션 클립이 볼 · 표정을 옮기는데,
    # 여기서는 몸 bbox 에 같은 비율로 붙여 어림한다 (왼쪽 +3 · 오른쪽 -3 · 바닥에서 16 위).
    l, t, r, b = body.getbbox()
    im.alpha_composite(cheek, (l + 3, b - 17))
    im.alpha_composite(cheek.transpose(Image.FLIP_LEFT_RIGHT), (r - 3 - 11, b - 17))
    lift = (t - 16) // 2
    im.alpha_composite(face, (0, max(lift, 0)), (0, max(-lift, 0)))
    if crown:
        im.alpha_composite(acc)
    return im


def shaded(body):
    """명암 1단 더한 칠 — 빛은 왼쪽 위. 오른쪽 · 아래 가장자리에서 5칸 안은 옅은 그늘, 2칸 안은 한 단 짙은 그늘.
    이미 더 어두운 칸은 그대로 둔다. 선 · 실루엣은 안 바뀐다."""
    a = np.array(body)
    h, w = a.shape[:2]
    line = c1_line.find_line(body)
    fill = (a[..., 3] > 0) & ~(a[..., :3] == np.array(line, dtype=np.uint8)).all(-1)
    out = a.copy()

    def reach(y, x):
        for k in range(1, 7):
            for dy, dx in ((k, k), (k, 0), (0, k)):
                yy, xx = y + dy, x + dx
                if yy >= h or xx >= w or not fill[yy, xx]:
                    return k
        return 99

    for y, x in zip(*np.nonzero(fill)):
        k = reach(y, x)
        want = SHADE2 if k <= 2 else SHADE1 if k <= 5 else None
        if want is not None and int(a[y, x, 0]) > want[0]:
            out[y, x, :3] = want
    res_im = Image.fromarray(out)
    assert same_alpha(body, res_im)
    return res_im


def scene(bodies, parts):
    """한 판 : 풀밭 + 길 + 합성소 · 가판대 + 울타리 + 찹쌀이 12 (종 색 3 줄 × 몸 4장)."""
    im = tile(parts['grass'], PW, PH)
    im.alpha_composite(parts['path'].crop((0, 0, PW, 60)), (0, PATH_TOP))
    feet = PATH_TOP + 8
    for name, cx in (('syn', 76), ('stall', 286)):
        o = parts[name]
        im.alpha_composite(o, (cx - o.width // 2, feet - o.height))
    for x in range(0, PW, 32):
        im.alpha_composite(parts['fence'], (x, FARM_TOP - 6))
    for i, (dx, dy) in enumerate(((330, 62), (370, 120), (318, 170))):     # 풀 장식 조금 (지금 그림)
        im.alpha_composite(parts['decor'][i], (dx, FARM_TOP + dy))
    for row, (_, color, crown) in enumerate(SPECIES):
        for col, body in enumerate(bodies):
            m = mozzi(body, color, crown, parts['cheek'], parts['face'], parts['acc'])
            im.alpha_composite(m, (6 + col * 72 + (row % 2) * 10, FARM_TOP + 22 + row * 52))
    return im


def main():
    os.makedirs(SIAN, exist_ok=True)
    parts = {'grass': res('Farm/grass_base.png'), 'path': res('Main/path_main.png'), 'syn': res('Building/synthesis_main.png'),
             'stall': res('Building/stall_main.png'), 'fence': res('Farm/fence_h.png'),
             'decor': [res('Farm/decor_%02d.png' % i) for i in (0, 6, 2)],
             'cheek': res('Mozzi/cheek.png'), 'face': res('Mozzi/face_01.png'), 'acc': res('Mozzi/acc_crown.png')}
    cw, ch = (CROP[2] - CROP[0]) * K, (CROP[3] - CROP[1]) * K
    head, gap = 30, 16
    row_h = max(PH, ch) + head + gap
    # 아래 견줌 줄 : 지금 칠 / 명암 더한 칠 — 종 색 셋 · hop0, 1배와 ×4
    flat = op(os.path.join(OUT, BODIES[0]))
    deep = shaded(flat)
    for i, n in enumerate(BODIES):                       # 견줌용 몸 4장도 파일로 남긴다 (시안 — 게임에 안 넣는다)
        os.makedirs(os.path.join(SIAN, 'shade_1단더함'), exist_ok=True)
        shaded(op(os.path.join(OUT, n))).save(os.path.join(SIAN, 'shade_1단더함', n))
    sk = 4
    strip_w, strip_h = 3 * 70 + 10, 64
    shade_h = head + (strip_h * sk + 26 + gap) * 2
    W = 16 + PW + 20 + cw + 16
    H = 56 + row_h * len(CANDS) + 40 + shade_h + 16
    board = Image.new('RGBA', (W, H), PAPER)
    label(board, (16, 10), 'C-1 찹쌀이 몸 선 색 시안 — 농장 잔디 위 · 건물 · 울타리와 함께 (왼쪽 1배 · 오른쪽 ×3)', 18)
    label(board, (16, 34), '줄 = 종 색 (위부터 흰 · 풀 · 화산) · 칸 = 몸 4장 (hop0~3). 게임은 몸 그림에 종 색을 곱해 선도 같이 물든다. 볼 #FBD701 은 그대로.', 13)
    y = 56
    for name, line in CANDS:
        bodies = bodies_of(line)
        if line != LINE_NOW:                             # 후보별 몸 PNG
            d = os.path.join(SIAN, 'line_' + rgb_hex(line)[1:])
            os.makedirs(d, exist_ok=True)
            for n, b in zip(BODIES, bodies):
                b.save(os.path.join(d, n))
        sc = scene(bodies, parts)
        label(board, (16, y + 4), name, 16)
        board.alpha_composite(sc, (16, y + head))
        board.alpha_composite(zoom(sc.crop(CROP), K), (16 + PW + 20, y + head))
        y += row_h
    label(board, (16, y + 4), '견줌 (보이기만) — 「지금 칠」 대 「명암 1단 더한 칠」 · 선은 지금 회색 · hop0 · ×%d' % sk, 16)
    y += 40
    x = 16
    for title, body in (('지금 칠 (흰 면 + 옅은 그늘 한 줄)', flat), ('명암 1단 더한 칠 (그늘 #DEDEDE 넓게 + #CCCCCC 가장자리)', deep)):
        strip = tile(parts['grass'], strip_w, strip_h)
        for i, (_, color, crown) in enumerate(SPECIES):
            strip.alpha_composite(mozzi(body, color, crown, parts['cheek'], parts['face'], parts['acc']), (5 + i * 70, 4))
        label(board, (x, y), title, 14)
        board.alpha_composite(zoom(strip, sk), (x, y + 24))
        y += strip_h * sk + 26 + gap
    board.convert('RGB').save(os.path.join(SIAN, 'c1_mozzi_line.png'))
    print('쓴 것 :', os.path.join(SIAN, 'c1_mozzi_line.png'), board.size)
    # 가까이 볼 조각 : 흰찹쌀이 hop0 을 후보 셋으로 ×8, 건물 선 옆에
    k = 8
    close = Image.new('RGBA', (3 * 64 * k + 135 * 2 + 40, 64 * k + 30), PAPER)
    for i, (name, line) in enumerate(CANDS):
        cell = tile(parts['grass'], 64, 64)
        cell.alpha_composite(mozzi(bodies_of(line)[0], (255, 255, 255), False, parts['cheek'], parts['face'], parts['acc']))
        close.alpha_composite(zoom(cell, k), (i * 64 * k, 30))
        label(close, (i * 64 * k + 6, 6), name, 15)
    syn = tile(parts['grass'], 135, 150)
    syn.alpha_composite(parts['syn'], (0, 3))
    close.alpha_composite(zoom(syn, 2), (3 * 64 * k + 20, 30))
    label(close, (3 * 64 * k + 20, 6), '합성소 (선 = 짙은 갈색) ×2', 15)
    close.convert('RGB').save(os.path.join(SIAN, 'c1_mozzi_line_close_x8.png'))


if __name__ == '__main__':
    main()

# C-4 — 합성 결과 판(synthesis_plate) 나무 면의 잡색을 합친다. 실루엣(알파)은 안 바뀐다.
# 가장 가까운 두 색을 칸 수 많은 쪽으로 합치기를 되풀이한다. 서로 DELTA 안인 색이 없고 색 수가 LIMIT 아래가 될 때까지.
# 못 · 둘레선처럼 뚝 떨어진 색은 가까운 짝이 없어 그대로 남는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/합성소/화풍통일/scripts/c4_plate.py
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = os.path.dirname(HERE)
ROOT = os.path.abspath(os.path.join(WORK, '..', '..', '..', '..', '..', '..'))
SRC = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art', 'Screen', 'synthesis_plate.png')   # 읽기만
NAME = 'synthesis_plate.png'
DELTA = 5        # 채널 차이가 모두 이 값 아래면 「비슷한 색」 (ArtTool near_colors 는 4)
LIMIT = 28       # 색 수 목표 (mozzi_prop 의 128px 권장은 32)


def dist(a, b):
    return max(abs(x - y) for x, y in zip(a, b))


def merge_table(counts):
    """색 → 대표 색 표. 가까운 짝부터 합치고, 합친 색은 칸 수로 무게를 둔 평균이 아니라 「많은 쪽 색 그대로」 둔다."""
    alive = dict(counts)
    to = {c: c for c in counts}
    while True:
        cols = list(alive)
        pairs = [(dist(a, b), a, b) for i, a in enumerate(cols) for b in cols[i + 1:]]
        if not pairs:
            break
        d, a, b = min(pairs, key=lambda p: (p[0], alive[p[1]] + alive[p[2]]))
        if d > DELTA and len(alive) <= LIMIT:
            break
        if d > DELTA * 3:                    # 뚝 떨어진 색(못 · 선)은 색 수가 넘쳐도 안 합친다
            break
        keep, drop = (a, b) if alive[a] >= alive[b] else (b, a)
        alive[keep] += alive.pop(drop)
        for k, v in to.items():
            if v == drop:
                to[k] = keep
    return to


def despeckle(out, solid):
    """합친 뒤 나무 면에 홀로 남은 1~2칸 점을 옆 색으로 메운다.
    줄(가로)로 1~2칸뿐이고 위아래에 같은 색이 없는 칸만 본다. 밝은 못 색과 그림 가장자리(옆이 투명)는 안 건드린다."""
    h, w = solid.shape
    fixed = 0
    rgb = out[..., :3]
    same = lambda y, x, c: 0 <= y < h and 0 <= x < w and solid[y, x] and tuple(rgb[y, x]) == c
    for y in range(1, h - 1):
        x = 1
        while x < w - 1:
            c = tuple(int(v) for v in rgb[y, x])
            end = x
            while end + 1 < w and same(y, end + 1, c):
                end += 1
            run = end - x + 1
            inside = all(solid[y + dy, xx] for xx in range(x - 1, min(end + 2, w)) for dy in (-1, 0, 1))
            alone = not any(same(y + dy, xx, c) for xx in range(x, end + 1) for dy in (-1, 1))
            if run <= 2 and inside and alone and max(c) < 0xC0 and not same(y, x - 1, c):
                left = tuple(int(v) for v in rgb[y, x - 1])
                right = tuple(int(v) for v in rgb[y, min(end + 1, w - 1)])
                if left == right and max(left) < 0xC0 and dist(left, c) <= DELTA * 6:
                    rgb[y, x:end + 1] = left
                    fixed += run
            x = end + 1
    return fixed


def main():
    src = Image.open(SRC).convert('RGBA')
    a = np.array(src)
    solid = a[..., 3] > 0
    cols, cnt = np.unique(a[solid][:, :3], axis=0, return_counts=True)
    counts = {tuple(int(v) for v in c): int(n) for c, n in zip(cols, cnt)}
    table = merge_table(counts)
    out = a.copy()
    for old, new in table.items():
        if old != new:
            out[(a[..., :3] == np.array(old, dtype=np.uint8)).all(-1) & solid, :3] = new
    fixed = despeckle(out, solid)
    print('홀로 남은 점 메움 : %d칸' % fixed)
    assert np.array_equal(out[..., 3], a[..., 3]), '알파가 달라졌다'
    os.makedirs(os.path.join(WORK, 'out'), exist_ok=True)
    Image.fromarray(out).save(os.path.join(WORK, 'out', NAME))
    cols2, cnt2 = np.unique(out[solid][:, :3], axis=0, return_counts=True)
    left = [tuple(int(v) for v in c) for c, _ in sorted(zip(cols2, cnt2), key=lambda p: -p[1])]
    print('%s : %d색 → %d색' % (NAME, len(counts), len(left)))
    print(' '.join('#%02X%02X%02X' % c for c in left))
    # 눈으로 볼 비교판 : 위 지금 · 아래 고친 뒤, ×6
    k = 6
    board = Image.new('RGBA', (src.width * k, src.height * k * 2 + 8), (243, 215, 175, 255))
    board.alpha_composite(src.resize((src.width * k, src.height * k), Image.NEAREST), (0, 0))
    board.alpha_composite(Image.fromarray(out).resize((src.width * k, src.height * k), Image.NEAREST), (0, src.height * k + 8))
    os.makedirs(os.path.join(WORK, '시안'), exist_ok=True)
    board.save(os.path.join(WORK, '시안', 'c4_plate_before_after_x6.png'))


if __name__ == '__main__':
    main()

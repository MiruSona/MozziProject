# C-2 (색 합치기만) — 찹쌀이 몸 4장의 칠 색을 한 벌로 모은다. 선 색은 지금 값(#6E6E6E) 그대로.
# hop0 · hop2 가 이미 쓰는 칠 색을 「한 벌」로 삼고, hop1(9색) · hop3(32색)의 칠 색을 가장 가까운 색으로 보낸다.
# hop0 · hop2 는 한 칸도 안 바뀐다 (이미 한도 안). 보낼 곳은 hop2 의 다섯 색 + hop0 의 짙은 두 색 = 7 (선까지 8).
# 실루엣 · 선 자리는 안 바뀐다 (칸 색만 바꾼다).
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/c2_merge_body.py
import os
from PIL import Image
from common import OUT, SIAN, LINE_NOW, BODIES, res, colors_of, remap, same_alpha, rgb_hex, zoom, label

# hop0 · hop2 의 칠 색 (밝음 → 어두움). 선은 따로다.
FILL = [(0xFD, 0xFD, 0xFD),
        (0xDF, 0xDF, 0xDF), (0xD6, 0xD5, 0xD6), (0xD4, 0xD4, 0xD4), (0xCC, 0xCB, 0xCB),
        (0xC8, 0xC7, 0xC8), (0xC4, 0xC4, 0xC4), (0xB8, 0xB7, 0xB7), (0xA4, 0xA4, 0xA4)]
LIMIT = 8                                                     # mozzi_char 의 max_colors


# hop1 · hop3 이 갈 수 있는 색. hop0 에만 있는 #D6D5D6 · #C8C7C8 은 바로 옆 #D4D4D4 · #C4C4C4 와 겹쳐 뺐다 (9색이면 한도 8 을 넘는다).
TARGET = [c for c in FILL if c not in ((0xD6, 0xD5, 0xD6), (0xC8, 0xC7, 0xC8))]


def nearest(c):
    return min(TARGET, key=lambda f: sum((a - b) ** 2 for a, b in zip(c, f)))


def main():
    os.makedirs(OUT, exist_ok=True)
    for name in BODIES:
        src = res('Mozzi/' + name)
        before = colors_of(src)
        table = {c: nearest(c) for c in before if c != LINE_NOW and c not in FILL}
        dst = remap(src, table)
        after = colors_of(dst)
        assert same_alpha(src, dst), name + ' : 알파가 달라졌다'
        assert after.get(LINE_NOW) == before.get(LINE_NOW), name + ' : 선 칸 수가 달라졌다'
        assert len(after) <= LIMIT, '%s : %d색 — 한도 %d 를 넘는다' % (name, len(after), LIMIT)
        dst.save(os.path.join(OUT, name))
        print('%s : %d색 → %d색  %s' % (name, len(before), len(after),
                                      ' '.join('%s:%d' % (rgb_hex(c), n) for c, n in sorted(after.items(), key=lambda kv: -kv[1]))))
    # 눈으로 볼 비교판 : 위 지금 · 아래 합친 뒤, 몸 4장 ×6 (풀색 바탕)
    k = 6
    board = Image.new('RGBA', (64 * 4 * k, (64 * k + 24) * 2), (192, 207, 125, 255))
    for row, (title, get) in enumerate((('지금 (Resources)', lambda n: res('Mozzi/' + n)), ('색 합친 뒤 (out)', lambda n: Image.open(os.path.join(OUT, n)).convert('RGBA')))):
        y = row * (64 * k + 24)
        for i, name in enumerate(BODIES):
            im = get(name)
            board.alpha_composite(zoom(im, k), (i * 64 * k, y + 24))
            label(board, (i * 64 * k + 6, y + 4), '%s · %s · %d색' % (title, name[:-4], len(colors_of(im))), 14)
    os.makedirs(SIAN, exist_ok=True)
    board.convert('RGB').save(os.path.join(SIAN, 'c2_body_before_after_x6.png'))


if __name__ == '__main__':
    main()

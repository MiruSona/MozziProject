"""B-4 말풍선 꼬리 가운데 · 버튼 틀 — 크림 면 · 테 잡색 합치기 (ui_bubble_c 56색 → 32 아래).

가장 가까운 두 색을 찾아 적게 쓰인 쪽을 많이 쓰인 쪽으로 합치기를 되풀이한다.
가장 가까운 쌍도 MAX_DIST 보다 멀면 멈춘다 (모양을 만드는 색까지 합치지 않게).
`ui_bubble.png` (29색) 은 이미 32 아래라 그대로 복사한다.
"""
from collections import Counter

from common import diff_pixels, load, save

TARGET = {"UI/ui_bubble_c.png": 24, "UI/ui_button.png": 20}
MAX_DIST = 30


def dist(a, b):
    return ((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2 + (a[2] - b[2]) ** 2) ** 0.5


def merge(im, target):
    px = im.load()
    cnt = Counter(px[x, y][:3] for y in range(im.height) for x in range(im.width) if px[x, y][3])
    table = {}
    while len(cnt) > target:
        cols = list(cnt)
        d, a, b = min((dist(a, b), a, b) for i, a in enumerate(cols) for b in cols[i + 1:])
        if d > MAX_DIST:
            break
        lo, hi = (a, b) if cnt[a] < cnt[b] else (b, a)
        cnt[hi] += cnt.pop(lo)
        table[lo] = hi
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if not c[3]:
                continue
            k = c[:3]
            while k in table:
                k = table[k]
            px[x, y] = k + (255,)
    return im, len(cnt)


def run():
    for rel, target in TARGET.items():
        src = load(rel)
        out, n = merge(src.copy(), target)
        save(out, rel)
        print(rel, "색", len(src.getcolors(9999)), "→", n, "· 바꾼 칸", len(diff_pixels(src, out)))
    save(load("UI/ui_bubble.png"), "UI/ui_bubble.png")


if __name__ == "__main__":
    run()

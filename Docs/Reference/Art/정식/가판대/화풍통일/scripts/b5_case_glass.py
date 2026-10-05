"""B-5 진열장 — 유리 칸에 옅은 유리 색 한 겹. 넣을지는 사용자가 비교판을 보고 고른다.

- out/Screen/display_case.png     : 유리 없음 (지금 그림 그대로)
- out_alt/Screen/display_case.png : 유리 있음
유리 칸 = 틀에 갇힌 투명 칸 (그림 가장자리에서 투명으로 못 가는 칸). 반투명은 안 쓴다 — 불투명 3색.
"""
from common import OUT_ALT, load, save

GLASS = (0xD3, 0xE4, 0xE6)   # 바탕 — 흰 떡이 묻히지 않게 조금 푸르고 어둡게
SHADE = (0xBC, 0xD3, 0xD8)   # 선반 아래 그늘 2줄
SHINE = (0xEA, 0xF4, 0xF4)   # 빗금 반짝임


def enclosed(im):
    w, h = im.size
    px = im.load()
    seen = set()
    todo = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while todo:
        p = todo.pop()
        if p in seen or not (0 <= p[0] < w and 0 <= p[1] < h) or px[p][3]:
            continue
        seen.add(p)
        todo += [(p[0] + 1, p[1]), (p[0] - 1, p[1]), (p[0], p[1] + 1), (p[0], p[1] - 1)]
    return {(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0 and (x, y) not in seen}


def run():
    im = load("Screen/display_case.png")
    save(im, "Screen/display_case.png")
    hole = enclosed(im)
    glass = im.copy()
    px = glass.load()
    for (x, y) in hole:
        c = GLASS
        if (x, y - 1) not in hole or (x, y - 2) not in hole:
            c = SHADE                                  # 위가 틀 · 선반이면 그늘
        elif (x + y) % 46 in (0, 1, 2, 6):             # 왼쪽 위 빛 — 오른쪽 위에서 왼쪽 아래로 내려오는 빗금 두 줄
            c = SHINE
        px[x, y] = c + (255,)
    save(glass, "Screen/display_case.png", OUT_ALT)
    xs = [p[0] for p in hole]
    ys = [p[1] for p in hole]
    print("유리 칸", len(hole), "칸 · bbox", (min(xs), min(ys), max(xs), max(ys)) if hole else None,
          "· 색 수", len(im.getcolors(9999)), "→", len(glass.getcolors(9999)))


if __name__ == "__main__":
    run()

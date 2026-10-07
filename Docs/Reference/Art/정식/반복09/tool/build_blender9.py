"""믹서기 내용물 겹 나누기 (blender9).

blender_idle / blender_run_0..2 를 몸통 겹(body)과 내용물 겹(fill)으로 나누고,
재료 아이콘에서 물들임 색 표를 뽑고, 견줌판을 만든다.
새 꼴은 그리지 않는다 — 가려내기 · 색 바꾸기 · 배치만 한다.

실행 : python build_blender9.py   (이 폴더에서)
"""
import json
import os
import statistics
from collections import deque

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, *[".."] * 6))
ICON_DIR = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Ingredient")
DATA = os.path.join(ROOT, "ProjectMozziUnity", "GameData", "ingredient.json")

SRC = ["blender_idle", "blender_run_0", "blender_run_1", "blender_run_2"]
# 유리 통 안쪽 세로 범위 (뚜껑 아래 ~ 받침 위). 원본 좌표.
Y_TOP, Y_BOT = 33, 100
MIN_ROW = 8  # 위쪽에서 이보다 좁은 줄은 칼날 축 · 반사 줄기라 뺀다
SEED = (45, 80)  # 내용물 한가운데


def lum(r, g, b):
    return 0.299 * r + 0.587 * g + 0.114 * b


def is_dark(p):  # 외곽선
    return lum(*p[:3]) < 80


def is_blue(p):  # 빈 유리 · 유리 테두리 (푸른 기)
    return p[2] > p[0] + 2


def is_metal(p):  # 칼날 축 · 쇠 (무채색 회색)
    return abs(p[0] - p[2]) <= 6 and lum(*p[:3]) < 190


def is_white(p):  # 유리 반사 하이라이트
    return min(p[:3]) >= 245


def content_mask(im):
    """씨앗에서 4방향으로 번지며 '어둡지 않고 · 푸르지 않고 · 하얗지 않은' 칸을 모은다."""
    w, h = im.size
    px = im.load()

    def ok(x, y):
        p = px[x, y]
        return p[3] == 255 and Y_TOP <= y < Y_BOT and not is_dark(p) and not is_blue(p) and not is_metal(p)

    # 하얀 하이라이트도 번짐 길로는 쓴다 (내용물 속 반사가 길을 끊지 않게), 마스크에서는 뺀다.
    seen, q = set(), deque([SEED])
    seen.add(SEED)
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and ok(nx, ny):
                seen.add((nx, ny))
                q.append((nx, ny))
    m = {xy for xy in seen if not is_white(px[xy])}
    # 위에서부터 폭이 MIN_ROW 보다 좁은 줄은 내용물이 아니라 칼날 축이 번진 것이라 잘라 낸다.
    for y in range(Y_TOP, Y_BOT):
        row = [xy for xy in m if xy[1] == y]
        if len(row) >= MIN_ROW:
            break
        m.difference_update(row)
    return m


def empty_glass_color(im, mask):
    """통 안 빈 곳(푸른 칸) 가운데 가장 흔한 색 — 몸통 겹에서 내용물 자리를 메운다."""
    px = im.load()
    xs = [x for x, _ in mask]
    cnt = {}
    for y in range(Y_TOP - 20, Y_BOT):
        for x in range(min(xs), max(xs) + 1):
            p = px[x, y]
            if p[3] == 255 and (x, y) not in mask and is_blue(p) and lum(*p[:3]) > 170:
                cnt[p] = cnt.get(p, 0) + 1
    return max(cnt, key=cnt.get)


def split(name):
    im = Image.open(os.path.join(HERE, name + ".png")).convert("RGBA")
    mask = content_mask(im)
    px = im.load()
    fillc = empty_glass_color(im, mask)
    body = im.copy()
    bpx = body.load()
    fill = Image.new("RGBA", im.size, (0, 0, 0, 0))
    fpx = fill.load()
    lmax = max(lum(*px[xy][:3]) for xy in mask)
    for xy in mask:
        bpx[xy] = fillc
        g = round(min(255, lum(*px[xy][:3]) / lmax * 255))
        fpx[xy] = (g, g, g, 255)
    return im, body, fill, mask, fillc


def tint(body, fill, rgb):
    """fill 의 흰·회색에 색을 곱해(multiply) 몸통 위에 얹는다. 코드에서는 SpriteRenderer.color / Image.color 와 같다."""
    f = fill.copy()
    fp = f.load()
    for y in range(f.size[1]):
        for x in range(f.size[0]):
            r, g, b, a = fp[x, y]
            if a:
                fp[x, y] = (r * rgb[0] // 255, g * rgb[1] // 255, b * rgb[2] // 255, a)
    out = body.copy()
    out.alpha_composite(f)
    return out


def icon_color(path):
    """투명 · 외곽선(어두운 색) · 흰 하이라이트를 뺀 픽셀의 채널별 가운데값."""
    im = Image.open(path).convert("RGBA")
    ps = [p for p in im.getdata() if p[3] >= 128 and lum(*p[:3]) >= 60 and not (min(p[:3]) >= 225)]
    return tuple(int(statistics.median(c[i] for c in ps)) for i in range(3))


def dye(rgb, min_lum=150):
    """너무 어두우면 흰색 쪽으로 섞어 밝기를 min_lum 까지 올린다 (곱했을 때 시커멓지 않게)."""
    L = lum(*rgb)
    if L >= min_lum:
        return rgb
    t = (min_lum - L) / (255 - L)
    return tuple(round(c + (255 - c) * t) for c in rgb)


def mix(*cols):
    return tuple(round(sum(c[i] for c in cols) / len(cols)) for i in range(3))


def hexc(c):
    return "#%02x%02x%02x" % tuple(c)


def main():
    layers = []
    for i, n in enumerate(SRC):
        im, body, fill, mask, fillc = split(n)
        body.save(os.path.join(HERE, f"blender9_body_{i}.png"))
        fill.save(os.path.join(HERE, f"blender9_fill_{i}.png"))
        # 다시 합치기 1 : 내용물 자리에 원본 색을 그대로 얹는다 → 원본과 같아야 한다.
        back = body.copy()
        bp, op = back.load(), im.load()
        for xy in mask:
            bp[xy] = op[xy]
        diff_exact = sum(1 for a, b in zip(back.getdata(), im.getdata()) if a != b)
        outside = sum(1 for (x, y) in mask if not (Y_TOP <= y < Y_BOT))
        xs = [x for x, _ in mask]
        ys = [y for _, y in mask]
        print(f"{n}: 내용물 {len(mask)}칸 상자 x{min(xs)}..{max(xs)} y{min(ys)}..{max(ys)} "
              f"메움색 {hexc(fillc[:3])} 다시합친차이 {diff_exact} 통밖 {outside}")
        layers.append((im, body, fill, mask, back))

    # 원래 쌀가루 색 = 원본 내용물 / 회색 의 평균 비 → 물들임 없음(기본색) 판
    im0, body0, fill0, mask0, _ = layers[0]
    o, f = im0.load(), fill0.load()
    base = tuple(min(255, round(statistics.median(o[xy][i] * 255 / max(1, f[xy][0]) for xy in mask0))) for i in range(3))
    rice = tint(body0, fill0, base)
    d = [max(abs(a[i] - b[i]) for i in range(3)) for a, b in zip(rice.getdata(), im0.getdata())]
    print(f"기본(쌀가루) 물들임색 {hexc(base)} : 원본과 다른 칸 {sum(1 for v in d if v > 0)}, "
          f"채널 차이 가운데값 {statistics.median([v for v in d if v > 0] or [0])} 최대 {max(d)}")

    items = json.load(open(DATA, encoding="utf-8"))
    table = []
    for it in items:
        c = icon_color(os.path.join(ICON_DIR, it["id"] + ".png"))
        table.append((it["id"], it["name"], c, dye(c)))
        print(f"| `{it['id']}` | {it['name']} | {hexc(c)} | {hexc(dye(c))} |")
    by = {t[0]: t[3] for t in table}
    mixes = [("redbean", "mugwort"), ("bean", "sesame")]
    for m in mixes:
        print(f"섞기 {'+'.join(m)} = {hexc(mix(*(by[k] for k in m)))}")

    # 견줌판 : 4배 최근접, 6칸 × 2줄, 회색 바탕
    panels = [im0, layers[0][4], rice] + [tint(body0, fill0, t[3]) for t in table]
    panels += [tint(body0, fill0, mix(*(by[k] for k in m))) for m in mixes]
    panels.append(tint(layers[2][1], layers[2][2], by["mugwort"]))
    S, G, COLS = 4, 12, 6
    pw, ph = 105 * S, 176 * S
    rows = (len(panels) + COLS - 1) // COLS
    board = Image.new("RGBA", (COLS * (pw + G) + G, rows * (ph + G) + G), (128, 128, 128, 255))
    for k, p in enumerate(panels):
        board.alpha_composite(p.resize((pw, ph), Image.NEAREST), (G + (k % COLS) * (pw + G), G + (k // COLS) * (ph + G)))
    board.save(os.path.join(HERE, "blender9_색견줌.png"))
    print("견줌판", board.size, "칸", len(panels))


if __name__ == "__main__":
    main()

"""믹서기 「갈기」 애니 blender10 — 겹 나누기 · 물들임 견줌판.

raw/anim_0..8.png (PixelLab animate_image_pixminimax 결과, 통 부분을 2배로 키운 156x164) 에서
통 안 내용물만 가려 회색 겹으로 만들고, 몸통 한 장(../blender9_body_0.png) 위에 얹는다.
새 꼴은 그리지 않는다 — 줄이기(2배 최근접) · 가려내기 · 색 곱하기 · 배치만 한다.

실행 : python build_blender10.py   (이 폴더에서)
"""
import os
import statistics

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
BODY = Image.open(os.path.join(TOOL, "blender9_body_0.png")).convert("RGBA")
W, H = BODY.size  # 105x176
CROP = (8, 24)  # raw 는 몸통 (8,24)-(86,106) 을 2배로 키운 것
X0, X1, Y0, Y1 = 15, 79, 33, 100  # 통 안 영역 (이 밖으로는 내용물을 두지 않는다)
N = 9
BASE = (0xFF, 0xF2, 0xD7)  # 기본 쌀가루
COL = {"팥": (0xC2, 0x83, 0x83), "콩": (0xE6, 0xC8, 0x68), "쑥": (0x80, 0xAA, 0x68)}


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def is_content(p):  # 살구·베이지 : 붉은 기가 푸른 기보다 뚜렷이 큼 (유리·반사·쇠는 빠진다)
    return p[3] > 0 and p[0] - p[2] >= 18 and lum(p) >= 60


def load_raw(i):
    im = Image.open(os.path.join(HERE, "raw", f"anim_{i}.png")).convert("RGBA")
    return im.resize((im.width // 2, im.height // 2), Image.NEAREST)


def extract():
    lums, leaks = [], 0
    for i in range(N):
        r = load_raw(i).load()
        m = {}
        for y in range(82):
            for x in range(78):
                p = r[x, y]
                if not is_content(p):
                    continue
                bx, by = x + CROP[0], y + CROP[1]
                if X0 <= bx < X1 and Y0 <= by < Y1:
                    m[(bx, by)] = lum(p)
                else:
                    leaks += 1
        lums.append(m)
    top = max(max(m.values()) for m in lums)  # 모든 장 같은 기준 → 장마다 밝기가 튀지 않는다
    fills = []
    for m in lums:
        f = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        px = f.load()
        for (x, y), v in m.items():
            g = min(255, round(v / top * 255))
            px[x, y] = (g, g, g, 255)
        fills.append(f)
    return fills, leaks


def tint(fill, color_at):
    """color_at(x, y) → (r,g,b). 회색 겹에 색을 곱한다."""
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    s, d = fill.load(), out.load()
    for y in range(H):
        for x in range(W):
            p = s[x, y]
            if p[3]:
                c = color_at(x, y)
                d[x, y] = tuple(round(c[k] * p[0] / 255) for k in range(3)) + (255,)
    return out


def compose(fill, color_at):
    im = BODY.copy()
    im.alpha_composite(tint(fill, color_at))
    return im


def avg(cols):
    return tuple(round(sum(c[k] for c in cols) / len(cols)) for k in range(3))


def layer_index(fill, n):
    """열마다 내용물 윗면~바닥을 n 등분 → 층 경계가 윗면 꼴을 따라 울퉁불퉁해진다. 0 = 맨 아래 층."""
    px = fill.load()
    idx = {}
    for x in range(X0, X1):
        ys = [y for y in range(Y0, Y1) if px[x, y][3]]
        if not ys:
            continue
        t, b = min(ys), max(ys) + 1
        for y in ys:
            idx[(x, y)] = min(n - 1, int((b - y) * n / (b - t)))
    return idx


def mix_ratio(i):
    """장 번호 → 층색에서 평균색으로 옮겨 간 비율 (0 = 층 그대로, 1 = 평균)."""
    return max(0.0, min(1.0, (i - 1) / 5))


def layered(fill, i, cols):
    idx, a, t = layer_index(fill, len(cols)), avg(cols), mix_ratio(i)

    def c(x, y):
        lc = cols[idx.get((x, y), 0)]
        return tuple(round(lc[k] * (1 - t) + a[k] * t) for k in range(3))

    return c


def up(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def row(ims, gap=4, bg=(40, 40, 40, 255)):
    w = sum(i.width for i in ims) + gap * (len(ims) - 1)
    out = Image.new("RGBA", (w, max(i.height for i in ims)), bg)
    x = 0
    for i in ims:
        out.alpha_composite(i, (x, 0))
        x += i.width + gap
    return out


def save_gif(frames, path, ms=70):
    fr = [f.convert("RGB") for f in frames]
    fr[0].save(path, save_all=True, append_images=fr[1:], duration=ms, loop=1, disposal=1)  # loop=1 → 두 바퀴


def main():
    fills, leaks = extract()
    for i, f in enumerate(fills):
        f.save(os.path.join(HERE, f"blender10_fill_{i}.png"))
        compose(f, lambda x, y: BASE).save(os.path.join(HERE, f"blender10_{i}.png"))
    # 층 방식 마스크 겹 (mix_ratio < 1 인 장만 필요) — 2층 · 3층
    for i, f in enumerate(fills):
        if mix_ratio(i) >= 1:
            continue
        for n in (2, 3):
            idx = layer_index(f, n)
            for L in range(n):
                m = Image.new("RGBA", (W, H), (0, 0, 0, 0))
                mp, fp = m.load(), f.load()
                for (x, y), k in idx.items():
                    if k == L:
                        mp[x, y] = fp[x, y]
                m.save(os.path.join(HERE, f"blender10_fill_{i}_n{n}_L{L}.png"))
    # 기본 색 vs 쑥 GIF
    ssuk = COL["쑥"]
    save_gif([up(row([compose(f, lambda x, y: BASE), compose(f, lambda x, y: ssuk)]), 4) for f in fills],
             os.path.join(HERE, "blender10.gif"))
    # 색 방식 견줌 : 팥+쑥 / 팥+콩+쑥 × 평균 / 층
    combos = [[COL["팥"], COL["쑥"]], [COL["팥"], COL["콩"], COL["쑥"]]]

    def four(i, f):
        ims = []
        for cols in combos:
            a = avg(cols)
            ims.append(compose(f, lambda x, y, a=a: a))
            ims.append(compose(f, layered(f, i, cols)))
        return row(ims)

    save_gif([up(four(i, f), 3) for i, f in enumerate(fills)], os.path.join(HERE, "blender10_색방식.gif"))
    still = [four(i, fills[i]) for i in (0, 2, 4, 8)]
    sh = Image.new("RGBA", (still[0].width, sum(s.height + 4 for s in still)), (40, 40, 40, 255))
    for k, s in enumerate(still):
        sh.alpha_composite(s, (0, k * (s.height + 4)))
    up(sh, 2).save(os.path.join(HERE, "blender10_색방식.png"))
    # 한눈판 : 옛 4장 위 · 새 9장 아래
    old = [Image.open(os.path.join(TOOL, n + ".png")).convert("RGBA")
           for n in ("blender_idle", "blender_run_0", "blender_run_1", "blender_run_2")]
    new = [compose(f, lambda x, y: BASE) for f in fills]
    r1, r2 = row(old), row(new)
    sh = Image.new("RGBA", (max(r1.width, r2.width), r1.height + r2.height + 8), (40, 40, 40, 255))
    sh.alpha_composite(r1, (0, 0))
    sh.alpha_composite(r2, (0, r1.height + 8))
    up(sh, 4).save(os.path.join(HERE, "blender10_한눈판.png"))

    # ── 끝 확인 수치 ──
    bp = BODY.load()
    for i, f in enumerate(fills):
        c, fp = new[i].load(), f.load()
        diff = sum(1 for y in range(H) for x in range(W) if not fp[x, y][3] and c[x, y] != bp[x, y])
        g = [fp[x, y][0] for y in range(H) for x in range(W) if fp[x, y][3]]
        print(f"장{i}: 몸통 차이 {diff}px · 내용물 {len(g)}px · 밝기 표준편차 {statistics.pstdev(g):.1f} · 최소 {min(g)}")
    print("통 영역 밖으로 잘린 내용물 칸(2배 줄인 뒤):", leaks)
    # 옛 그림 대비 : 통 안 · 푸르지 않은 칸의 밝기 표준편차 (흰 소용돌이 · 그늘 포함)
    for n, im in zip(("idle", "run0", "run1", "run2"), old):
        p = im.load()
        g = [lum(p[x, y]) for y in range(Y0, Y1) for x in range(X0, X1)
             if p[x, y][3] and not p[x, y][2] > p[x, y][0] + 2 and lum(p[x, y]) >= 80]
        print(f"옛 {n}: 밝기 표준편차 {statistics.pstdev(g):.1f} · 흰칸 {sum(1 for v in g if v >= 245)}")
    # 새 장 : 같은 잣대 (합친 그림에서)
    for i, im in enumerate(new):
        p = im.load()
        g = [lum(p[x, y]) for y in range(Y0, Y1) for x in range(X0, X1)
             if fills[i].load()[x, y][3]]
        print(f"새 {i}: 밝기 표준편차 {statistics.pstdev(g):.1f}")
    # 흰 덩어리 : 날 그림의 통 안 흰 칸 중 내용물 겹에 못 들어간 것 (몸통 반사 자리 제외)
    for i in range(N):
        r = load_raw(i).load()
        lost = 0
        for y in range(82):
            for x in range(78):
                bx, by = x + CROP[0], y + CROP[1]
                if X0 <= bx < X1 and Y0 <= by < Y1 and min(r[x, y][:3]) >= 235 and r[x, y][3] \
                        and min(bp[bx, by][:3]) < 235:
                    lost += 1
        print(f"장{i}: 겹에서 빠진 흰 칸 {lost}")


if __name__ == "__main__":
    main()

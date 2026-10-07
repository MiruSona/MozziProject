# 반복 09 주방 기구 정식 그림 가공 스크립트.
# raw/ 의 PixelLab 원본을 자르고 겹쳐서 tool/ 에 내보낸다. 새로 칠하지 않는다.
# 실행: python build_tool.py  (이 폴더에서)
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")


def load(name):
    return Image.open(os.path.join(RAW, name)).convert("RGBA")


def crop_common(images):
    """여러 장을 같은 상자로 잘라 프레임끼리 자리가 안 흔들리게 한다."""
    boxes = [im.getbbox() for im in images]
    box = (min(b[0] for b in boxes), min(b[1] for b in boxes),
           max(b[2] for b in boxes), max(b[3] for b in boxes))
    return [im.crop(box) for im in images]


def save(im, name, out):
    im.save(os.path.join(HERE, name))
    out.append((name, im))


def split3(sheet):
    """한 줄에 셋 놓인 반죽 판을 세 장으로 나눈다."""
    # 셋이 고르게 놓이지 않으므로, 빈 세로줄로 덩어리를 나누고 큰 셋을 왼쪽부터 고른다
    alpha = sheet.getchannel("A")
    filled = [alpha.crop((x, 0, x + 1, sheet.height)).getbbox() is not None for x in range(sheet.width)]
    runs, start = [], None
    for x, f in enumerate(filled + [False]):
        if f and start is None:
            start = x
        elif not f and start is not None:
            runs.append((start, x))
            start = None
    runs = sorted(sorted(runs, key=lambda r: r[1] - r[0])[-3:])
    parts = [sheet.crop((a, 0, b, sheet.height)) for a, b in runs]
    return [p.crop(p.getbbox()) for p in parts]


def interior_box(mortar):
    """절구 입구 안쪽(어두운 회색) 자리를 찾는다. 위쪽 35% 만 본다 (아래 몸통 그늘이 섞이지 않게)."""
    px = mortar.load()
    xs, ys = [], []
    for y in range(int(mortar.height * 0.35)):
        for x in range(mortar.width):
            if px[x, y][3] > 200 and _is_interior(px[x, y]):
                xs.append(x)
                ys.append(y)
    return min(xs), min(ys), max(xs), max(ys)


def _is_interior(p):
    r, g, b, a = p
    return max(r, g, b) - min(r, g, b) < 18 and 80 < (r + g + b) / 3 < 158


def tight(name):
    im = load(name)
    return im.crop(im.getbbox())


def head_anchor(mallet):
    """떡메 머리 아래 가운데 (아래 12줄의 불투명 칸 x 평균, 맨 아래 y)."""
    px = mallet.load()
    xs = [x for y in range(mallet.height - 12, mallet.height) for x in range(mallet.width) if px[x, y][3] > 0]
    return sum(xs) // len(xs), mallet.height


def build_pound(out):
    # 2026-10-06 큰 판: 절구 폭 249 · 떡메 두 자세(든 장 45도 mallet_up, 친 장 20도 mallet_hit)
    mortar = tight("mortar_huge_c.png")
    up, hit = tight("mallet_up.png"), tight("mallet_hit.png")
    doughs = [tight(f"dough_s{i}.png") for i in (1, 2, 3)]
    flats = [tight(f"dough_s{i}_flat.png") for i in (1, 2, 3)]
    ix0, iy0, ix1, iy1 = interior_box(mortar)
    # 0 들기 → 1 더 들기 → 2 내리기 → 3 침 → 4 튐 → 5 올리기 → 6 들기
    poses = [up, up, hit, hit, hit, up, up]
    lift = [-70, -85, -38, 1, -8, -42, -70]   # 떡메 머리 바닥 y (반죽 윗면 기준)
    # 1차: 절구 왼쪽 위를 (0,0) 으로 두고 모든 조각 자리를 잰다
    cx = (ix0 + ix1) // 2
    floor = iy0 + (iy1 - iy0) * 3 // 4   # 반죽 바닥 줄 — 입구 3/4 깊이 (반죽이 테두리 위로 보이게)
    plans, x0, y0, x1 = [], 0, 0, mortar.width
    for s in range(3):
        for f in range(7):
            d = flats[s] if f in (3, 4) else doughs[s]
            m = poses[f]
            ax, ay = head_anchor(m)
            mx, my = cx - ax, floor - d.height + lift[f] - ay   # 친 장은 납작 반죽 윗면에 닿는다
            plans.append((s, f, d, m, mx, my))
            x0, y0, x1 = min(x0, mx), min(y0, my), max(x1, mx + m.width)
    # 절구 아래 가운데가 프레임 아래 가운데가 되도록 좌우를 같게 넓힌다
    half = max(mortar.width // 2 - x0, x1 - mortar.width // 2)
    W, H = half * 2, mortar.height - y0
    ox, oy = W // 2 - mortar.width // 2, -y0
    front_y = oy + (iy0 + iy1) // 2
    for s, f, d, m, mx, my in plans:
        frame = Image.new("RGBA", (W, H))
        frame.alpha_composite(mortar, (ox, oy))
        mortar_only = frame.copy()
        op = mortar_only.load()
        layer = Image.new("RGBA", (W, H))
        layer.alpha_composite(d, (ox + cx - d.width // 2, oy + floor - d.height))
        lp = layer.load()
        # 앞 테두리 가림: 입구 아래 절반에서 절구 몸통과 겹치는 반죽 칸은 지운다
        for y in range(max(front_y, 0), H):
            for x in range(W):
                if lp[x, y][3] and op[x, y][3] and not _is_interior(op[x, y]):
                    lp[x, y] = (0, 0, 0, 0)
        frame.alpha_composite(layer)
        frame.alpha_composite(m, (ox + mx, oy + my))
        save(frame, f"pound_s{s + 1}_{f}.png", out)


def main():
    out = []
    bl = crop_common([load(n) for n in ("blender_big2.png", "blender_big_run_a.png",
                                        "blender_big_run_b.png", "blender_big_run_c.png")])
    save(bl[0], "blender_idle.png", out)
    for i in range(3):
        save(bl[i + 1], f"blender_run_{i}.png", out)
    build_pound(out)
    b = load("board_big.png")
    save(b.crop(b.getbbox()), "shape_board.png", out)
    st = crop_common([load("steamer_closed.png"), load("steamer_open.png")])
    save(st[0], "steamer_closed.png", out)
    save(st[1], "steamer_open.png", out)
    for i, im in enumerate(crop_common([load("steam.png"), load("steam_b.png"), load("steam_c.png")])):
        save(im, f"steam_{i}.png", out)
    # 치기 3단계 이어 붙인 gif (밝은 회색 바탕)
    frames = []
    for name, im in out:
        if name.startswith("pound_"):
            bg = Image.new("RGBA", im.size, (200, 200, 200, 255))
            bg.alpha_composite(im)
            frames.append(bg.convert("P"))
    frames[0].save(os.path.join(HERE, "pound.gif"), save_all=True, append_images=frames[1:],
                   duration=110, loop=0)
    # 한눈판: 이름과 함께 줄지어 놓는다
    cols, cw, ch = 7, 360, 400
    rows = (len(out) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cw, rows * ch), (200, 200, 200, 255))
    dr = ImageDraw.Draw(sheet)
    for i, (name, im) in enumerate(out):
        x, y = (i % cols) * cw, (i // cols) * ch
        sheet.alpha_composite(im, (x + (cw - im.width) // 2, y + 375 - im.height))
        dr.text((x + 4, y + 382), f"{name} {im.width}x{im.height}", fill=(40, 30, 20, 255))
    sheet.save(os.path.join(HERE, "한눈판.png"))
    for name, im in out:
        print(name, im.size)


if __name__ == "__main__":
    main()

# 반복 09 칸2 — 절구만 바꾼 안(A 흰 대리석 · B 꿀색 나무 · B2 연한 나무 덤) 견줌.
# raw/ 의 PixelLab 원본(A1 · B1 · B3, B2 는 안 쓴 원통꼴 판)을 자르고, ../build_tool.py 의 치기 겹 합성을 본떠 든 장 · 친 장을 만든다.
# 그 프레임을 ../../견줌/build_view.py 의 칸2 화면에 끼워 ../../견줌/절구_견줌판.png 한 장을 낸다.
# PIL 은 자르기 · 겹 합성 · 배치 · 글자만. 확대 · 축소 · 새로 칠하기 없음. 모두 1배.
# 실행: python build_alt.py  (아무 폴더에서나)
import importlib.util
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
VIEW = os.path.join(os.path.dirname(TOOL), "견줌")


def _mod(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


bt = _mod("build_tool", os.path.join(TOOL, "build_tool.py"))     # tight · head_anchor 만 빌린다 (파일은 안 고친다)
bv = _mod("build_view", os.path.join(VIEW, "build_view.py"))     # room · rail_spot · plant_spot · ui · POUND 를 빌린다

# 안별 원본과 입구 안쪽을 가르는 밝기 띠 (lo < 밝기 < hi 인 칸만 안쪽으로 본다 — 바깥 외곽선 · 밝은 테는 빠진다)
ALTS = {
    "A": ("A1.png", 130, 195),    # 흰 대리석: 테 ~240, 안쪽 회색 149~175
    "B": ("B1.png", 55, 125),     # 나무(꿀색): 테 ~165, 안쪽 63~94
    "B2": ("B3.png", 80, 150),    # 나무(연한 물푸레색, 덤): 테 163~208, 안쪽 100~136
    "W1": ("W1.png", 45, 110),    # 밝은 호두(B1 을 edit_image_pro_flash 로 색만 바꿈): 테 ~133, 안쪽 74~78, 외곽선 21~39
    "W2": ("W2.png", 38, 80),     # 짙은 호두(같은 편집): 테 ~99, 안쪽 50~59, 외곽선 17~29
}
# build_tool 의 7프레임 중 든 장(0)과 친 장(3)만 뽑는다
FRAMES = {"up": ("dough_s1.png", "mallet_up.png", -70), "hit": ("dough_s2_flat.png", "mallet_hit.png", 1)}


def lum(p):
    return (p[0] + p[1] + p[2]) / 3


def interior(mortar, lo, hi):
    """가운데 세로줄에서 처음 만나는 안쪽 칸부터 이어진 안쪽 칸을 채운다. (칸 집합, 상자)"""
    px = mortar.load()
    cx = mortar.width // 2
    ok = lambda p: p[3] > 200 and lo < lum(p) < hi
    rim = next(y for y in range(mortar.height) if px[cx, y][3] > 200 and lum(px[cx, y]) >= hi)   # 밝은 뒤 테를 먼저 지난다
    sy = next(y for y in range(rim, mortar.height) if ok(px[cx, y]))
    seen, todo = {(cx, sy)}, [(cx, sy)]
    while todo:
        x, y = todo.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n not in seen and 0 <= n[0] < mortar.width and 0 <= n[1] < mortar.height and ok(px[n]):
                seen.add(n)
                todo.append(n)
    xs, ys = [p[0] for p in seen], [p[1] for p in seen]
    return seen, (min(xs), min(ys), max(xs), max(ys))


def tight_raw(name):
    im = Image.open(os.path.join(HERE, "raw", name)).convert("RGBA")
    return im.crop(im.getbbox())


def build(key):
    src, lo, hi = ALTS[key]
    mortar = tight_raw(src)
    mortar.save(os.path.join(HERE, "mortar_%s.png" % key))
    inside, (ix0, iy0, ix1, iy1) = interior(mortar, lo, hi)
    print("%s 절구 %dx%d · 입구 안쪽 x %d~%d (폭 %d) y %d~%d · 칸 %d" %
          (key, mortar.width, mortar.height, ix0, ix1, ix1 - ix0 + 1, iy0, iy1, len(inside)))
    cx = mortar.width // 2
    floor = iy1 + 6                                   # build_tool 과 같은 깊이
    plans, x0, y0, x1 = [], 0, 0, mortar.width
    for tag, (dn, mn, lift) in FRAMES.items():
        d, m = bt.tight(dn), bt.tight(mn)
        ax, ay = bt.head_anchor(m)
        dp = d.load()
        top = next(y for y in range(d.height) if dp[d.width // 2, y][3] > 200)
        mx, my = cx - ax, floor - d.height + top + lift - ay
        plans.append((tag, d, m, mx, my))
        x0, y0, x1 = min(x0, mx), min(y0, my), max(x1, mx + m.width)
    half = max(mortar.width // 2 - x0, x1 - mortar.width // 2)
    W, H = half * 2, mortar.height - y0
    ox, oy = W // 2 - mortar.width // 2, -y0
    front_y = oy + (iy0 + iy1) // 2
    out = {}
    for tag, d, m, mx, my in plans:
        frame = Image.new("RGBA", (W, H))
        frame.alpha_composite(mortar, (ox, oy))
        op = frame.load()
        layer = Image.new("RGBA", (W, H))
        layer.alpha_composite(d, (ox + cx - d.width // 2, oy + floor - d.height))
        lp = layer.load()
        # 앞 테두리 가림: 입구 아래 절반에서 절구 몸통(안쪽이 아닌 칸)과 겹치는 반죽 칸은 지운다
        for y in range(max(front_y, 0), H):
            for x in range(W):
                if lp[x, y][3] and op[x, y][3] and (x - ox, y - oy) not in inside:
                    lp[x, y] = (0, 0, 0, 0)
        frame.alpha_composite(layer)
        frame.alpha_composite(m, (ox + mx, oy + my))
        frame.save(os.path.join(HERE, "%s_%s.png" % (key, tag)))
        out[tag] = frame
    return out


def screen(frame, rail_xy, plant, hint):
    c = bv.room(2)
    c.alpha_composite(bv.img(bv.BG, "deco_rail.png"), rail_xy)
    if plant and plant[0] == 2:
        c.alpha_composite(bv.img(bv.BG, "deco_plantshelf.png"), plant[1])
    c.alpha_composite(frame, (bv.POUND[0] - frame.width // 2, bv.POUND[1] - frame.height))
    return bv.ui(c, hint)


def main():
    rail_xy, _ = bv.rail_spot()                      # 지금 판과 같은 레일 · 화분 자리 (세 안을 같은 조건으로 견준다)
    plant = bv.plant_spot(rail_xy)
    # 2판(10-07): B 꼴이 골라져 색만 견준다 — B 꿀색 · W1 밝은 호두 · W2 짙은 호두 · B 덤 연한 나무.
    # 1판(지금 회색 · A 흰 대리석 · B · B 덤)은 ../../견줌/절구_견줌판_v1.png 에 남아 있다.
    cols = {"B안 (꿀색 나무)": build("B")}
    cols["W1 (밝은 호두)"] = build("W1")
    cols["W2 (짙은 호두)"] = build("W2")
    cols["B안 덤 (연한 나무)"] = build("B2")
    m, lab, gap = 20, 50, 20
    sw = m * 2 + 540 * len(cols) + gap * (len(cols) - 1)
    sh = m + (lab + 1200) * 2 + gap + m
    s = Image.new("RGBA", (sw, sh), (48, 48, 52, 255))
    d = ImageDraw.Draw(s)
    for r, (tag, word) in enumerate((("up", "든 장"), ("hit", "친 장"))):
        y = m + r * (lab + 1200 + gap)
        for i, (name, fr) in enumerate(cols.items()):
            x = m + i * (540 + gap)
            hint = bv.HINT["칸2_치기_든" if tag == "up" else "칸2_치기_친"]
            s.paste(screen(fr[tag], rail_xy, plant, hint), (x, y + lab))
            d.text((x + 270, y + lab // 2), "%s · %s" % (name, word), font=bv.v3.font(26),
                   fill=(255, 255, 255, 255), anchor="mm")
    s.save(os.path.join(VIEW, "절구_견줌판.png"))
    print("견줌판", s.size)


if __name__ == "__main__":
    sys.exit(main())

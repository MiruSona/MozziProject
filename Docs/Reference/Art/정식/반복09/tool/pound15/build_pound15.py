# 떡메 치기 시안 pound15 — pound14 의 떡메·절구는 그대로, 반죽만 PixelLab 핵심 꼴 6장으로 바꿈
# 겹 순서: mortar_back < dough < mallet < mortar_front < dough_spill (묻힘 장 0·5·7·넘어감은 dough 가 mallet 위)
# 실행: python build_pound15.py   (raw/key_*.png 여섯 장이 있어야 한다)
import math
import os
import shutil
import sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
P14 = os.path.join(TOOL, "pound14")
sys.dont_write_bytecode = True
sys.path.insert(0, P14)
sys.path.insert(0, os.path.join(TOOL, "pound13"))
sys.path.insert(0, TOOL)
import build_pound14 as p14  # noqa: E402

p13 = p14.p13
W, H, CX = p14.W, p14.H, p14.CX
TIMES, T_TIMES = p14.TIMES, p14.T_TIMES
HOLE_CX, HOLE_CY, HOLE_RX = p14.HOLE_CX, p14.HOLE_CY, p14.HOLE_RX

# ---------- 핵심 꼴 (PixelLab 원본, 흰색으로 맞춤) ----------
# 화면 크기 (폭, 높이) — 입구 폭 162px 보다 넓게 해 테두리에 걸치게 한다
# 절구 바깥 테두리 폭은 y413 에서 x120~319 (200px). 쉼은 그 안, 맞음은 1단계만 테두리 밖으로 넘친다
SIZE = {(1, "rest"): (188, 46), (1, "hit"): (232, 34),
        (2, "rest"): (180, 62), (2, "hit"): (204, 48),
        (3, "rest"): (176, 76), (3, "hit"): (196, 58)}
# 반죽 밑 y (캔버스 px). pound14 는 444. 구멍 아래 끝 435 보다 아래일수록 앞 테두리가 더 가린다
# 쉼 꼴 윗면 = 밑 - 높이 ≈ pound14 떡메 쉼 자리(REST_BOTTOM 406/394/384) — 머리가 반죽 위에 얹힌다
BOTTOM = {1: 446, 2: 452, 3: 456}
HIT_DROP = 4          # 맞는 장은 눌려 밑이 조금 더 내려간다


def whiten(im):
    """색을 빼고 밝은 쪽이 흰색(255)이 되게 늘린다. 그늘은 회색 톤만 남는다."""
    im = im.convert("RGBA")
    a = im.getchannel("A")
    l = im.convert("L")
    vals = sorted(v for v, al in zip(l.getdata(), a.getdata()) if al > 0)
    top = vals[int(len(vals) * 0.95)] if vals else 255
    l = l.point(lambda v: min(255, round(v * 250 / max(top, 1))))
    out = Image.merge("RGBA", (l, l, l, a))
    return out.crop(out.getbbox())


KEY = {(s, k): whiten(Image.open(os.path.join(HERE, "raw", f"key_s{s}_{k}.png")))
       for s in (1, 2, 3) for k in ("rest", "hit")}

# 장별 눌림 정도 f (0 = 쉼 꼴, 1 = 맞음 꼴, 음수 = 끈적하게 위로 늘어남)
F = [0.0, -0.06, -0.1, -0.1, 0.0, 1.0, 0.45, 0.1]


def lerp(a, b, t):
    return a + (b - a) * t


def place_key(img, w, h, bottom):
    lay = Image.new("RGBA", (W, H))
    im = img.resize((max(1, round(w)), max(1, round(h))), Image.NEAREST)
    lay.alpha_composite(im, (round(CX - im.width / 2), round(bottom - im.height)))
    return lay


def body(stage, f, extra_h=0):
    """extra_h : 밑은 그대로 두고 윗면만 올리는 높이(px) — 떡메 머리 밑면에 반죽 윗면을 맞춘다."""
    wr, hr = SIZE[(stage, "rest")]
    wh, hh = SIZE[(stage, "hit")]
    key = KEY[(stage, "hit" if f >= 0.5 else "rest")]
    return place_key(key, lerp(wr, wh, f), lerp(hr, hh, f) + extra_h, BOTTOM[stage] + HIT_DROP * max(f, 0))


# ---------- 넘침 (spill) ----------
# 넘침은 맞는 장 5·6 의 1·2단계에만 있다. 쉼(0)·복귀(7)·넘어감·3단계는 빈 투명.
# 값 = lobe_spill 의 (테두리 덩어리 rx, ry, 벽 방울 rx, ry). 1단계는 크게 + 벽 타고 흐름, 2단계는 작게
SPILL = {(1, 5): (32, 17, 11, 16), (1, 6): (27, 14, 9, 12), (2, 5): (16, 9, 0, 0), (2, 6): (13, 7, 0, 0)}
SPILL_TONES = (255, 240, 226)
OUTLINE = 205                   # 튀는 넘침 조각의 윤곽 톤 (크림 벽 위 대비)   # spill 은 밝은 세 톤만 (어두운 음영은 밝은 쪽으로)
MORTAR_MASK = None              # mortar_front ∪ mortar_back 알파 — 이 밖(허공) 픽셀은 버린다
# 떡메 머리 밑면과 반죽 윗면의 목표 차 (음수 = 머리가 반죽에 묻힌 px). 쉼 장은 살짝 얹히게 −3
REST_SINK = -5
HIT_SINK = {1: -12, 2: -10, 3: -6}


def tone(px):
    r, g, b, a = px
    l = (r + g + b) // 3
    v = SPILL_TONES[0] if l >= 238 else SPILL_TONES[1] if l >= 212 else SPILL_TONES[2]
    return (v, v, v, 255 if a else 0)


def drop_specks(im, min_px=24):
    """작은 떨어진 조각(min_px 미만 연결 덩어리)을 지운다 — 허공에 뜬 가는 줄 막기."""
    px = im.load()
    seen = set()
    for y0 in range(H):
        for x0 in range(W):
            if not px[x0, y0][3] or (x0, y0) in seen:
                continue
            comp, st = [], [(x0, y0)]
            seen.add((x0, y0))
            while st:
                x, y = st.pop(); comp.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in seen and px[nx, ny][3]:
                        seen.add((nx, ny)); st.append((nx, ny))
            if len(comp) < min_px:
                for x, y in comp:
                    px[x, y] = (0, 0, 0, 0)


def lobe_spill(p, malpx, hx, mask):
    """테두리에 걸쳐 흘러내리는 넘침 덩어리를 양옆에 그린다.
    p = (rx, ry, drip_rx, drip_ry) : 테두리 위 덩어리 반지름 · 바깥 벽을 타고 내려가는 방울 반지름 (0 이면 없음).
    절구 윤곽 밖(허공)과 자루 자리는 버린다. 톤은 위 한두 줄 255, 몸 240, 아래 두 줄 226."""
    rx, ry, drx, dry = p
    on = lambda x, y, cx, cy, ax, ay: ax > 0 and ((x - cx) / ax) ** 2 + ((y - cy) / ay) ** 2 <= 1
    sp = Image.new("RGBA", (W, H))
    ps = sp.load()
    for side in (-1, 1):
        cx = HOLE_CX + side * (HOLE_RX + 4)        # 테두리 한가운데쯤 (옛 꼴 — 지금은 splash_spill 을 쓴다)
        dx = HOLE_CX + side * (HOLE_RX + 16)       # 방울은 바깥 벽 쪽
        for y in range(HOLE_CY - ry - 2, H):
            for x in range(W):
                # 안쪽은 자르지 않아 둥근 끝이 반죽과 이어진다. 바깥은 윤곽에서 3px 안쪽까지만 (끝이 윤곽을 따라 둥글게)
                ins = all(0 <= x + ox < W and 0 <= y + oy < H and mask[x + ox, y + oy]
                          for ox, oy in ((3, 0), (-3, 0), (0, 3), (2, 2), (-2, 2)))
                if not ins:
                    continue
                if malpx[x, y][3] and x > hx + 44:
                    continue
                if on(x, y, cx, HOLE_CY + 5, rx, ry) or on(x, y, dx, HOLE_CY + 5 + ry, drx, dry):
                    ps[x, y] = (240, 240, 240, 255)
    for x in range(W):  # 열마다 위 2줄 밝게 · 아래 2줄 살짝 그늘
        ys = [y for y in range(H) if ps[x, y][3]]
        if not ys:
            continue
        for y in ys[:2]:
            ps[x, y] = (255, 255, 255, 255)
        for y in ys[-2:]:
            ps[x, y] = (226, 226, 226, 255)
    return sp


# ---------- 튀는 넘침 (splash) ----------
# 키 = (단계, 장) 또는 ("t12", 장). 1단계 크기 1.0, 2단계 0.6 · 방울 절반. 3단계·쉼 장은 없음.
# 5장 터짐 → 6장 내려옴 → 7장 벽 타고 흘러내림 → t12 0·1장 잔방울.
SPLASH_KEYS = {(1, 5), (1, 6), (1, 7), (2, 5), (2, 6), (2, 7), ("t12", 0), ("t12", 1)}
RIM_Y = 412                       # 테두리 윗면 y
RIM_END = HOLE_RX + 4             # 가운데에서 테두리 끝(덩어리 뿌리)까지 x
WALL = 97                         # 가운데에서 바깥 벽까지 x
# 방울 (테두리 끝에서 바깥쪽 dx, 테두리 위 높이 dy, 지름) — 5장 / 6장(내려오며 작아짐). 2단계는 앞 절반만 쓴다
DROPS5 = [(16, 30, 10), (34, 48, 8), (52, 26, 6), (28, 60, 5), (46, 40, 7)]   # 지름 8 이상은 위로 뾰족한 물방울
DROPS6 = [(22, 22, 8), (40, 36, 6), (58, 14, 5), (34, 46, 4), (52, 26, 5)]
HEAD_DROPS5 = [(30, 40, 6), (42, 26, 5), (20, 55, 5)]   # 머리 양옆 위 (머리 가운데에서 dx, 테두리 위 dy, 지름)
HEAD_DROPS6 = [(36, 30, 5), (48, 16, 4), (26, 44, 4)]


def _blob(pts, cx, cy, rx, ry):
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if rx > 0 and ry > 0 and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 and 0 <= x < W and 0 <= y < H:
                pts.add((x, y))


def _shade(ps, pts):
    """조각 하나: 윤곽 1px 205, 안쪽은 열마다 위 1~2줄 255, 몸 240, 아래 1줄 226."""
    edge = {(x, y) for x, y in pts if any(n not in pts for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))}
    for x, y in edge:
        ps[x, y] = (OUTLINE, OUTLINE, OUTLINE, 255)
    pts = pts - edge
    cols = {}
    for x, y in pts:
        cols.setdefault(x, []).append(y)
    for x, ys in cols.items():
        ys.sort()
        for k, y in enumerate(ys):
            v = SPILL_TONES[0] if k < (2 if len(ys) > 4 else 1) else SPILL_TONES[2] if k == len(ys) - 1 and len(ys) > 2 else SPILL_TONES[1]
            ps[x, y] = (v, v, v, 255)


def splash_spill(key, malpx, hx):
    """넘침 겹을 그린다. 떡메와 겹치는 자루 쪽(x > 머리+44) 픽셀은 버린다(자루 뒤로 숨는 꼴)."""
    st, f = key
    k = 1.0 if st == 1 else 0.6
    n_drop = 5 if st == 1 else 3
    pieces = []
    for s in (-1, 1):
        x0 = HOLE_CX + s * RIM_END
        if f == 5:      # 터짐: 바깥·위로 뻗고 끝이 휘어 오르는 물결 덩어리
            p = set()
            for j in range(21):
                t = j / 20
                r = (13 * (1 - t) + 5 + 1.5 * math.sin(t * 9)) * k
                _blob(p, x0 + s * (-6 + 56 * t - 8 * t ** 4) * k, RIM_Y + 2 - (6 * t + 24 * t ** 3) * k, r, r * 0.8)
            pieces.append(p)
            drops = [(dx, dy, d) for dx, dy, d in DROPS5[:n_drop]]
            hdrops = HEAD_DROPS5[: (3 if st == 1 else 1)]
            for side_r in (24, 29):   # 머리 밑동 둘레 작은 튐 조각
                q = set(); _blob(q, hx + s * side_r * (1 if k == 1 else 0.95), RIM_Y - 6 - (side_r - 24), 3.5 * k + 0.5, 2.5 * k + 0.5); pieces.append(q)
        elif f == 6:    # 내려옴: 덩어리는 테두리 위로 처져 걸치고, 방울은 낮아지며 작아짐
            p = set()
            for j in range(13):
                t = j / 12
                r = (11 * (1 - t) + 6) * k
                _blob(p, x0 + s * (-4 + 40 * t) * k, RIM_Y + 3 + 10 * t * t * k, r * 1.15, r * 0.6)
            _blob(p, x0 + s * 35 * k, RIM_Y + 14 * k + 3, 6.5 * k + 0.5, 10 * k)   # 테두리 끝에 처져 매달린 방울
            pieces.append(p)
            drops = [(dx, dy, d) for dx, dy, d in DROPS6[:n_drop]]
            hdrops = HEAD_DROPS6[: (3 if st == 1 else 1)]
        elif f == 7:    # 흘러내림: 테두리에 얇은 자국 + 바깥 벽을 타고 가늘고 긴 방울
            p = set()
            _blob(p, x0 + s * 4, RIM_Y + 1, 9 * k, 2.5)
            wx = HOLE_CX + s * WALL
            _blob(p, wx, RIM_Y + 4 + 16 * k, 3.5 * k + 0.5, 16 * k)        # 길이 32 · 폭 7 (1단계)
            _blob(p, wx, RIM_Y + 4 + 32 * k, 4.5 * k + 0.5, 5 * k + 0.5)
            pieces.append(p)
            drops = [(-10, 1, 2), (14, 1, 2)] if st == 1 else [(10, 1, 2)]
            hdrops = []
        else:           # t12 잔방울: 벽 아래로 떨어지는 작은 방울
            wx = HOLE_CX + s * WALL
            p = set()
            if f == 0:
                _blob(p, wx, RIM_Y + 32, 2.5, 3.5)
            else:
                _blob(p, wx + s, RIM_Y + 38, 1.5, 2)
            pieces.append(p)
            drops, hdrops = [], []
        for dx, dy, d in drops:
            q = set(); cx, cy, r = x0 + s * dx * k, RIM_Y - dy * k, d * k / 2
            _blob(q, cx, cy, r, r)
            if d >= 8:   # 큰 방울은 위로 뾰족한 물방울 꼴
                _blob(q, cx, cy - r * 1.1, r * 0.45, r * 0.8)
            pieces.append(q)
        for dx, dy, d in hdrops:
            q = set(); _blob(q, hx + s * dx, RIM_Y - dy - 20 * k, d * k / 2 + 0.5, d * k / 2 + 0.5); pieces.append(q)
    sp = Image.new("RGBA", (W, H))
    ps = sp.load()
    for q in pieces:
        q = {(x, y) for x, y in q if not (malpx[x, y][3] and x > hx + 44)}
        _shade(ps, q)
    return sp


def split(full, mal, over, spill_src=None):
    """반죽 → (main, spill).
    main : 절구 윤곽 밖(허공) 픽셀을 지운 반죽. 입구 밖 부분은 앞 테두리가 가린다.
    spill : spill_src(넘침 꼴)의 입구 양옆 밖(|x-가운데| > 77) · y>413 픽셀 중 절구 윤곽 안만, 밝은 톤으로.
    자루 덮기 막기: 머리 가운데 x + 44 보다 오른쪽에서 떡메와 겹친 반죽 픽셀은 spill 에서 빼고,
    반죽이 떡메 위인 장(over)에서는 main 에서도 지운다."""
    ang, px, py = mal
    hx, _, _ = p14.head_of(ang, px, py)
    malpx = MALLET_LAYER.load()
    mask = MORTAR_MASK.load()
    main = full.copy()
    pm = main.load()
    for y in range(HOLE_CY + 1, H):
        for x in range(W):
            if pm[x, y][3] and not mask[x, y]:
                pm[x, y] = (0, 0, 0, 0)
    spill = None
    if spill_src is not None:
        spill = splash_spill(spill_src, malpx, hx)
        ps = spill.load()
        for y in range(H):
            for x in range(W):
                if ps[x, y][3]:
                    pm[x, y] = (0, 0, 0, 0)
    if over:
        for y in range(H):
            for x in range(W):
                if pm[x, y][3] and malpx[x, y][3] and x > hx + 44:
                    pm[x, y] = (0, 0, 0, 0)
    return main, spill


def head_gap(dough, mallet, hx):
    """머리 가운데 ±28 열에서 (반죽 윗면 y − 떡메 밑면 y) 의 최솟값. 양수 = 머리가 떠 있다."""
    dp, mp = dough.load(), mallet.load()
    best = None
    for x in range(int(hx) - 28, int(hx) + 29):
        mb = [y for y in range(H) if mp[x, y][3]]
        dt = [y for y in range(H) if dp[x, y][3]]
        if mb and dt:
            g = dt[0] - max(mb)
            best = g if best is None else min(best, g)
    return best


def fit_extra(stage, f, mallet, pose, target):
    """반죽 윗면이 머리 밑면보다 target 만큼(음수 = 묻힘) 되게 extra_h 를 찾는다. 이미 묻혀 있으면 0."""
    hx, _, _ = p14.head_of(*pose)
    g = head_gap(body(stage, f), mallet, hx)
    return max(0, g - target) if g is not None else 0


MALLET_LAYER = None


def frame(d_full, m_layer, mal_pose, over, spill_src=None):
    global MALLET_LAYER
    MALLET_LAYER = m_layer
    d, sp = split(d_full, mal_pose, over, spill_src)
    return d, sp


def main():
    os.makedirs(os.path.join(HERE, "layers"), exist_ok=True)
    L = lambda n: os.path.join(HERE, "layers", n)
    for n in os.listdir(os.path.join(P14, "layers")):
        if n.startswith(("mallet_", "mortar_")):
            shutil.copy(os.path.join(P14, "layers", n), L(n))
    global MORTAR_MASK
    mf = Image.open(L("mortar_front.png")).convert("RGBA").getchannel("A")
    mb = Image.open(L("mortar_back.png")).convert("RGBA").getchannel("A")
    MORTAR_MASK = Image.composite(mf, mb, mf)  # 둘 중 하나라도 있으면 0 아님
    for n in os.listdir(os.path.join(HERE, "layers")):
        if n.startswith("dough_spill_"):
            Image.new("RGBA", (W, H)).save(L(n))  # 먼저 빈 투명으로 덮고, 넘침 장만 아래에서 다시 쓴다
    st, tr, extra = {}, {}, {}
    mal = lambda s, i: Image.open(L(f"mallet_s{s}_{i}.png")).convert("RGBA")
    for s in (1, 2, 3):
        # 쉼 장(0) 기준으로 반죽 윗면을 머리에 맞추고, 들고 있는 장(1~4·6)도 같은 높이를 쓴다
        e0 = fit_extra(s, F[0], mal(s, 0), p14.pose(s, 0), REST_SINK)
        e7 = fit_extra(s, F[7], mal(s, 7), p14.pose(s, 7), REST_SINK)
        e5 = fit_extra(s, F[5], mal(s, 5), p14.pose(s, 5), HIT_SINK[s])
        extra[s] = [e0, e0, e0, e0, e0, e5, e0, max(e0, e7)]
        st[s] = []
        for i in range(8):
            m = mal(s, i)
            src = (s, i) if (s, i) in SPLASH_KEYS else None
            d, sp = frame(body(s, F[i], extra[s][i]), m, p14.pose(s, i), i in p14.DOUGH_OVER, src)
            d.save(L(f"dough_s{s}_{i}.png"))
            if sp is not None:
                sp.save(L(f"dough_spill_s{s}_{i}.png"))
            st[s].append(p14.comp(d, sp, m, i in p14.DOUGH_OVER))
    for a, b in ((1, 2), (2, 3)):
        tr[a] = []
        for i in range(4):
            t = (0.2, 0.45, 0.7, 0.92)[i]
            wa, ha = SIZE[(a, "rest")]; wb, hb = SIZE[(b, "rest")]
            key = KEY[(a if t < 0.5 else b, "rest")]
            full = place_key(key, lerp(wa, wb, t), lerp(ha, hb, t) + lerp(extra[a][0], extra[b][0], t),
                             lerp(BOTTOM[a], BOTTOM[b], t))
            m = Image.open(L(f"mallet_t{a}{b}_{i}.png")).convert("RGBA")
            bottom = p14.REST_BOTTOM[a] + (p14.REST_BOTTOM[b] - p14.REST_BOTTOM[a]) * t
            tk = (f"t{a}{b}", i)
            d, sp = frame(full, m, (4, *p14.pivot_for(4, bottom)), True, tk if tk in SPLASH_KEYS else None)
            d.save(L(f"dough_t{a}{b}_{i}.png"))
            if sp is not None:
                sp.save(L(f"dough_spill_t{a}{b}_{i}.png"))
            tr[a].append(p14.comp(d, sp, m, True))
    bg = lambda fs: [p13.on_bg(f).convert("RGB") for f in fs]
    save = lambda n, fr, t: fr[0].save(os.path.join(HERE, n), save_all=True, append_images=fr[1:], duration=t, loop=0, disposal=1)
    seq = st[1] * 2 + tr[1] + st[2] * 2 + tr[2] + st[3] * 2
    tms = TIMES * 2 + T_TIMES + TIMES * 2 + T_TIMES + TIMES * 2
    tms[-1] = 600
    save("pound15_이어서.gif", bg(seq), tms)
    # 나란히: 왼쪽 = PREV_LAYERS(환경 변수, 고치기 전 layers 복사본) 또는 pound14, 오른쪽 = 지금 pound15
    prev = os.environ.get("PREV_LAYERS") or os.path.join(P14, "layers")

    def old(s, i):
        P = lambda n: os.path.join(prev, n)
        g = lambda n: Image.open(P(n)).convert("RGBA") if os.path.exists(P(n)) else None
        return p14.comp(g(f"dough_s{s}_{i}.png"), g(f"dough_spill_s{s}_{i}.png"), g(f"mallet_s{s}_{i}.png"), i in p14.DOUGH_OVER)
    side = []
    for s in (1, 2, 3):
        for i in range(8):
            a, b = p13.on_bg(old(s, i)).convert("RGB"), p13.on_bg(st[s][i]).convert("RGB")
            c = Image.new("RGB", (2 * a.width + 10, a.height), (20, 20, 20)); c.paste(a, (0, 0)); c.paste(b, (a.width + 10, 0))
            side.append(c)
    save("나란히.gif", side, TIMES * 3)
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
    cw, ch = 330, 400
    board = Image.new("RGB", (8 * cw * 2, 3 * (ch * 2 + 34)), (36, 32, 30))
    for r, s in enumerate((1, 2, 3)):
        for i in range(8):
            f = p13.on_bg(st[s][i]).crop((110, 70, 440, 470)).resize((cw * 2, ch * 2), Image.NEAREST)
            x, y = i * cw * 2, r * (ch * 2 + 34) + 30
            board.paste(f.convert("RGB"), (x, y))
            ImageDraw.Draw(board).text((x + 6, y - 28), f"{s}단계 {i}장 f={F[i]}", fill=(240, 240, 240), font=font)
    board.save(os.path.join(HERE, "견줌판.png"))
    # Resources 복사 — 빠진 spill 은 빈 440×560 으로 채운다
    res = os.path.join(TOOL, *[".."] * 6, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen")
    res = os.path.normpath(res)
    names = ["mortar_back", "mortar_front"]
    for tag in [f"s{s}_{i}" for s in (1, 2, 3) for i in range(8)] + [f"t{a}{b}_{i}" for a, b in ((1, 2), (2, 3)) for i in range(4)]:
        names += [f"mallet_{tag}", f"dough_{tag}", f"dough_spill_{tag}"]
    for n in names:
        p = L(n + ".png")
        if not os.path.exists(p):
            Image.new("RGBA", (W, H)).save(p)
        im = Image.open(p)
        assert im.size == (W, H), (n, im.size)
        shutil.copy(p, os.path.join(res, f"kitchen_pound15_{n}.png"))
    print("resources", res, len(names))


if __name__ == "__main__":
    main()

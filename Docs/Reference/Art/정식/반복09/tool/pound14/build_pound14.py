# 떡메 치기 시안 pound14 — 떡메 호 운동 + 절구 두 겹(뒷벽 / 앞 테두리)
# 겹 순서: mortar_back < dough < mallet < mortar_front < dough_spill (묻힘 장은 dough 가 mallet 위)
# 실행: python build_pound14.py   (pound13 의 반죽 모양·박자를 그대로 가져다 쓴다)
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
sys.dont_write_bytecode = True     # pound13 폴더에 캐시를 안 남긴다
sys.path.insert(0, os.path.join(TOOL, "pound13"))
sys.path.insert(0, TOOL)
import build_pound13 as p13  # noqa: E402

W, H, K, HW, HH = p13.W, p13.H, p13.K, p13.HW, p13.HH
CX = p13.CX
TIMES, T_TIMES = p13.TIMES, p13.T_TIMES
MAL = Image.open(os.path.join(HERE, "raw", "mallet_A_long.png")).convert("RGBA")

# ---------- 절구 입구 타원 (캔버스 px) ----------
HOLE_CX, HOLE_CY, HOLE_RX, HOLE_RY = 220, 413, 81, 22   # 안쪽 검은 구멍. 위 391 · 아래 435


def lower_arc(x):
    u = (x - HOLE_CX) / HOLE_RX
    return HOLE_CY + HOLE_RY * math.sqrt(1 - u * u) if abs(u) < 1 else HOLE_CY


def split_mortar():
    full = Image.new("RGBA", (W, H))
    full.alpha_composite(p13.mortar, (p13.OX, p13.OY))
    back, front = full.copy(), full.copy()
    pb, pf = back.load(), front.load()
    for y in range(H):
        for x in range(W):
            if y > lower_arc(x):
                pb[x, y] = (0, 0, 0, 0)
            else:
                pf[x, y] = (0, 0, 0, 0)
    return full, back, front


# ---------- 반죽: pound13 모양을 DY 만큼 아래로 (구멍 안에 앉힘) ----------
DY = 8                       # 캔버스 px. 반죽 밑 436→444 (구멍 아래 끝 435 보다 아래 = 앞 테두리가 가림)
DYH = DY / K


def shift(ells):
    return [(cx, cy + DYH, rx, ry) for cx, cy, rx, ry in ells]


def inside_hole(cx, cy):      # 반 해상도 타원 가운데가 입구 안인가
    x, y = cx * K, cy * K
    return ((x - HOLE_CX) / HOLE_RX) ** 2 + ((y - HOLE_CY) / (HOLE_RY + 6)) ** 2 <= 1


def render_split(ells, tex, amt):
    """반죽 한 장 → (main, spill). spill = 가운데가 입구 밖인 타원(넘친 방울) 픽셀만. 앞 테두리 위에 그린다."""
    ells = shift(ells)
    full = p13.render(ells, tex, amt)
    out = [e for e in ells if not inside_hole(e[0], e[1]) and e[1] * K > HOLE_CY]
    if not out:
        return full, None
    m = Image.new("L", (HW, HH))
    d = ImageDraw.Draw(m)
    for cx, cy, rx, ry in out:
        d.ellipse((cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1), fill=255)
    m = m.resize((W, H), Image.NEAREST)
    spill = Image.new("RGBA", (W, H))
    spill.paste(full, (0, 0), m)
    main = full.copy()
    main.paste((0, 0, 0, 0), (0, 0, W, H), m)
    return main, spill


def dough(stage, i):
    return render_split(p13.DOUGH[stage].get(i, p13.REST[stage]), p13.TEX[stage], 1.0)


# ---------- 떡메: 손 자리(축) 둘레로 회전 + 축 이동 ----------
GRIP = (MAL.width - 3, MAL.height / 2)          # 원본 칸에서 손 자리 (자루 끝)
HEAD = (p13.HCX, p13.HCY)                       # 원본 칸에서 머리 가운데
ARM = (GRIP[0] - HEAD[0]) * K                   # 머리 가운데 → 축 (캔버스 px) = 146
HALF = p13.HALF                                 # 머리 가운데 → 머리 아랫면 = 49
HIT_BOTTOM = {s: v + DY for s, v in p13.HIT_BOTTOM.items()}    # 448 / 426 / 402
REST_BOTTOM = {s: v + DY for s, v in p13.REST_BOTTOM.items()}  # 406 / 394 / 384
HIT_ANG = 2
# 장별 (자루 각도°, 머리 가운데를 쉼 자리에서 오른쪽으로 민 px, 위로 올린 px).
# 각도 = 자루가 수평에서 기운 정도. 양수 = 손이 머리보다 위, 음수 = 머리가 손보다 위(어깨 뒤로 넘김)
POSE = [(4, 0, 0),        # 0 쉼 (머리 반죽에 얹힘)
        (-16, 22, 60),    # 1 들기 — 느리게 호 위로
        (-32, 50, 140),   # 2 들기
        (-42, 64, 180),   # 3 꼭대기 멈칫
        (-18, 26, 70),    # 4 내리치기 (중간 장, 흐림 없음)
        None,             # 5 맞음 — 머리가 반죽에 묻힘 (각도 HIT_ANG)
        (-8, 6, 20),      # 6 튐
        (2, 0, 2)]        # 7 복귀
DOUGH_OVER = {0, 5, 7}    # 머리가 반죽에 묻히는 장: 반죽이 떡메 위


def pivot_for(ang, bottom):
    """머리 아랫면 가운데가 (CX,bottom) 이고 자루가 ang° 일 때 축 좌표."""
    a = math.radians(ang)
    hx, hy = CX + HALF * math.sin(a), bottom - HALF * math.cos(a)   # 머리 가운데
    return hx + ARM * math.cos(a), hy - ARM * math.sin(a)


def place(ang, px, py):
    """축(손 자리)을 (px,py) 에 두고 ang° 돌린다. 원래 크기에서 돌린 뒤 K 배 (도트 유지)."""
    R = MAL.width + 4
    pad = Image.new("RGBA", (2 * R, 2 * R))
    pad.alpha_composite(MAL, (round(R - GRIP[0]), round(R - GRIP[1])))
    rot = pad.rotate(ang, resample=Image.NEAREST).resize((2 * R * K, 2 * R * K), Image.NEAREST)
    layer = Image.new("RGBA", (W, H))
    layer.alpha_composite(rot, (round(px - R * K), round(py - R * K)))
    return layer


def head_of(ang, px, py):
    a = math.radians(ang)
    hx, hy = px - ARM * math.cos(a), py + ARM * math.sin(a)
    return hx, hy, hy + HALF * math.cos(a)     # 머리 가운데 x,y · 머리 아랫면 y (대략)


def pose(stage, i):
    if POSE[i] is None:
        ang, (px, py) = HIT_ANG, pivot_for(HIT_ANG, HIT_BOTTOM[stage])
    else:
        ang, dx, up = POSE[i]
        a = math.radians(ang)
        hx, hy = CX + dx, REST_BOTTOM[stage] - HALF - up
        px, py = hx + ARM * math.cos(a), hy - ARM * math.sin(a)
    return ang, px, py


def mallet(stage, i):
    ang, px, py = pose(stage, i)
    hx, hy, _ = head_of(ang, px, py)
    assert 50 <= hx <= W - 30 and 55 <= hy <= H - 55, (stage, i, hx, hy)   # 머리는 늘 캔버스 안
    return place(ang, px, py)


def trans(a, b, i):
    t = (0.2, 0.45, 0.7, 0.92)[i]
    ells = p13.lerp(p13.REST[a], p13.REST[b], t)
    if i == 2:
        ells = [(cx, cy - 1, rx * 0.96, ry * 1.08) for cx, cy, rx, ry in ells]
    tex = ("grain", 1 - t) if (a == 1 and t < 0.5) else ("lump", 1 - t if a == 2 else t)
    d, sp = render_split(ells, *tex)
    bottom = REST_BOTTOM[a] + (REST_BOTTOM[b] - REST_BOTTOM[a]) * t
    return d, sp, place(4, *pivot_for(4, bottom))


# ---------- 합성·출력 ----------
FULL, BACK, FRONT = split_mortar()


def comp(d, sp, m, dough_over):
    f = BACK.copy()
    for lay in ((m, d) if dough_over else (d, m)):
        f.alpha_composite(lay)
    f.alpha_composite(FRONT)
    if sp is not None:
        f.alpha_composite(sp)
    return f


def old_frame(name):
    L = lambda n: os.path.join(TOOL, "pound13", "layers", n)
    f = Image.new("RGBA", (W, H))
    for n in ("mortar.png", "mallet_" + name, "dough_" + name):
        f.alpha_composite(Image.open(L(n)).convert("RGBA"))
    return f


def main():
    os.makedirs(os.path.join(HERE, "layers"), exist_ok=True)
    L = lambda n: os.path.join(HERE, "layers", n)
    BACK.save(L("mortar_back.png")); FRONT.save(L("mortar_front.png"))
    st, rows = {}, []
    for s in (1, 2, 3):
        st[s] = []
        for i in range(8):
            (d, sp), m = dough(s, i), mallet(s, i)
            d.save(L(f"dough_s{s}_{i}.png")); m.save(L(f"mallet_s{s}_{i}.png"))
            if sp is not None:
                sp.save(L(f"dough_spill_s{s}_{i}.png"))
            st[s].append(comp(d, sp, m, i in DOUGH_OVER))
            ang, px, py = pose(s, i)
            hx, hy, hb = head_of(ang, px, py)
            rows.append(f"| {s} | {i} | {ang} | ({px:.0f},{py:.0f}) | ({hx:.0f},{hy:.0f}) | {hb:.0f} | {'O' if sp else ''} |")
    print("\n".join(rows))
    tr = {}
    for a, b in ((1, 2), (2, 3)):
        tr[a] = []
        for i in range(4):
            d, sp, m = trans(a, b, i)
            d.save(L(f"dough_t{a}{b}_{i}.png")); m.save(L(f"mallet_t{a}{b}_{i}.png"))
            if sp is not None:
                sp.save(L(f"dough_spill_t{a}{b}_{i}.png"))
            tr[a].append(comp(d, sp, m, True))
    bgs = lambda fs: [p13.on_bg(f) for f in fs]
    seq = st[1] * 2 + tr[1] + st[2] * 2 + tr[2] + st[3] * 2
    tms = TIMES * 2 + T_TIMES + TIMES * 2 + T_TIMES + TIMES * 2
    tms[-1] = 600
    save = lambda n, fr, t: fr[0].save(os.path.join(HERE, n), save_all=True, append_images=fr[1:], duration=t, loop=0, disposal=1)
    save("pound14_이어서.gif", [f.convert("RGB") for f in bgs(seq)], tms)
    # 나란히: 왼쪽 pound13, 오른쪽 pound14 (같은 순서)
    names13 = [f"s{s}_{i}.png" for s in (1, 2, 3) for i in range(8)]
    side = []
    for k, f14 in enumerate(st[1] + st[2] + st[3]):
        a, b = p13.on_bg(old_frame(names13[k])), p13.on_bg(f14)
        c = Image.new("RGB", (2 * W + 10, a.height), (20, 20, 20)); c.paste(a.convert("RGB"), (0, 0)); c.paste(b.convert("RGB"), (W + 10, 0))
        side.append(c)
    save("나란히.gif", side, TIMES * 3)
    # 견줌판: 단계별 8장 가로줄 (반 크기 장을 2배로 = 캔버스 원래 크기 가운데 잘라)
    font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 22)
    cw, ch = 330, 400                      # 장 하나에서 잘라 쓸 칸 (캔버스 x110~440 · y50~450), 2배로 붙인다
    board = Image.new("RGB", (8 * cw * 2, 3 * (ch * 2 + 34)), (36, 32, 30))
    for r, s in enumerate((1, 2, 3)):
        for i in range(8):
            f = p13.on_bg(st[s][i]).crop((110, 20 + 50, 440, 20 + 450)).resize((cw * 2, ch * 2), Image.NEAREST)
            x, y = i * cw * 2, r * (ch * 2 + 34) + 30
            board.paste(f.convert("RGB"), (x, y))
            ang = HIT_ANG if POSE[i] is None else POSE[i][0]
            ImageDraw.Draw(board).text((x + 6, y - 28), f"{s}단계 {i}장 {ang}°", fill=(240, 240, 240), font=font)
    board.save(os.path.join(HERE, "견줌판.png"))


if __name__ == "__main__":
    main()

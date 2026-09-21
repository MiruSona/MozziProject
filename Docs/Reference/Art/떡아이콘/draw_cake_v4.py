"""떡 아이콘 5판 — 32x32 확정본 한 벌 (setG_32).

구도는 사용자 제안 그대로다 : 왼쪽 위에 온전한 떡 한 알, 그 오른쪽 아래를 살짝 덮으며
기운 반쪽이 기대어 있다. 반쪽은 3판 halfmoon 의 봉긋한 정면 반달(가로:세로 2:1)을
시계 방향으로 조금 기울인 것이다 — 4판처럼 140도씩 돌리면 멜론 한 쪽이 된다.

4판 불합격 사유와 그 대책:
  · 멜론처럼 길쭉했다        -> 반달을 정확히 반원(rx=ry, 위 절반)으로 잡고 기울기를 30~45도로 제한
  · 두 덩이가 한 덩어리로 붙었다 -> 앞 반쪽 둘레를 따라 뒤 알에 1px 짙은 그늘 띠를 넣어 앞뒤를 가른다
  · 깨 점이 눈처럼 모여 얼굴로 보였다 -> 점을 서로 4px 넘게 떼고, 가까운 거리에서 같은 줄에 두 점을 안 놓는다

규칙 : 32x32 · RGBA · 알파 0/255 · 외곽선 없음 · 빛 왼쪽 위 · 장당 3~7색.
재료 자리 규칙(팥·잣=소 · 쑥·대추=피 · 콩·깨=고물)과 11종 id 는 3판 그대로다.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ART = r"C:\Mirusona\Project\ClaudeProject\ProjectMozzi2\ProjectMozziUnity\Assets\_Project\Resources\Art"
N = 32

# 반쪽을 시계 방향으로 기울이는 각. 5판에서는 30 으로 정했으나, 6판에서 반쪽이 통통해지자
# 30 은 다시 옆으로 누워 보였다. 20 이 「알에 기대어 선 통통한 반쪽」으로 가장 잘 읽힌다.
TILT = 20

SKIN = {
    None: ("#F2E9DC", "#C9BCA6", "#FFFBF2"),
    "mugwort": ("#6E9B4A", "#4C7433", "#C9DCB4"),
    "jujube": ("#BF6448", "#8E3B26", "#E8C4B2"),
    "bean": ("#E6C868", "#BE9840", "#FFFBF2"),
}
FILL = {"redbean": ("#7A2A2A", "#A84848"), "pinenut": ("#E8D49A", "#A9743E")}
# 볶은 깨 갈색. 5판의 거의 검은색(#4A3520)은 너무 튀었다. 초록 피 위에서 묻히는 만큼은
# 색을 다시 검게 하지 말고 점을 가로 2x1 로 키워 푼다.
SESAME_DOT = "#8A5A2C"
DARK_SKIN = ("mugwort", "jujube")  # 이 피 위에서는 깨를 2x1 로 찍는다
BEAN_POWDER = "#F7E6A8"  # 콩고물 겉의 밝은 가루 점

SLOT = {"redbean": "filling", "bean": "topping", "mugwort": "skin",
        "sesame": "topping", "pinenut": "filling", "jujube": "skin"}
IDS = ["plain_cake", "redbean_cake", "bean_cake", "mugwort_cake", "sesame_cake",
       "pinenut_cake", "jujube_cake", "redbean_bean_cake", "mugwort_sesame_cake",
       "pinenut_jujube_cake", "redbean_mugwort_sesame_cake"]
PARTS_OF = {i: [] if i == "plain_cake" else i[:-5].split("_") for i in IDS}


def recipe(cid):
    s = {"skin": None, "filling": None, "topping": None}
    for k in PARTS_OF[cid]:
        s[SLOT[k]] = k
    return s


# ---------- 모양 ----------
def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def local(x, y, cx, cy, deg):
    a = math.radians(deg)
    dx, dy = x + 0.5 - cx, y + 0.5 - cy
    return dx * math.cos(a) + dy * math.sin(a), -dx * math.sin(a) + dy * math.cos(a)


def ellipse(cx, cy, rx, ry, deg=0):
    out = set()
    for y in range(N):
        for x in range(N):
            u, v = local(x, y, cx, cy, deg)
            if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def blob(cx, cy, rx, ry):
    """온전한 떡 한 알 — 위는 동글고 아래가 살짝 퍼진다 (1판 몸 실루엣)."""
    return ellipse(cx, cy, rx, ry) | ellipse(cx, cy + ry * 0.40, rx * 1.05, ry * 0.58)


def half_ell(cx, cy, rx, ry, deg, round_tip=2.2):
    """기울인 반달. rx 는 자른 면의 반너비, ry 는 등의 높이다.
    5판은 2:1(18x9)이라 얇고 길쭉해 「누운 버섯의 자루」로 보였다. 6판은 3:2 로 통통하게 잡는다.
    자른 면의 양 끝은 둥글린다 — 뾰족하면 떡이 아니라 갈퀴·멜론 조각이 된다."""
    out = set()
    a = math.radians(deg)
    tips = [(cx + rx * math.cos(a), cy + rx * math.sin(a)),
            (cx - rx * math.cos(a), cy - rx * math.sin(a))]
    for y in range(N):
        for x in range(N):
            u, v = local(x, y, cx, cy, deg)
            if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0 and v <= 0:
                if all((x + 0.5 - tx) ** 2 + (y + 0.5 - ty) ** 2 > round_tip ** 2
                       for tx, ty in tips):
                    out.add((x, y))
    return out


DISK2 = [(dx, dy) for dx in range(-2, 3) for dy in range(-2, 3) if dx * dx + dy * dy <= 4]
DISK1 = [(dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]


def erode(px, disk=DISK2):
    return {p for p in px if all((p[0] + d[0], p[1] + d[1]) in px for d in disk)}


def dilate1(px):
    return {(p[0] + d[0], p[1] + d[1]) for p in px for d in DISK1}


def shade_edge(px, off=(2, 3)):
    return {q for q in px if (q[0] + off[0], q[1] + off[1]) not in px}


def spread_dots(px, count, gap=4, seed=0):
    """점을 고르게 흩는다. 서로 gap 이상 떨어뜨리고, 가까운 거리에서 같은 줄에 두 점을
    안 놓는다 — 가로로 나란한 두 점이 눈으로 보여 떡이 얼굴이 되기 때문이다."""
    cand = sorted(px, key=lambda q: (((q[0] + seed) * 73856093) ^ (q[1] * 19349663)) & 0xFFFF)
    out = []
    for q in cand:
        if len(out) >= count:
            break
        if any(max(abs(q[0] - p[0]), abs(q[1] - p[1])) < gap for p in out):
            continue
        if any(q[1] == p[1] and abs(q[0] - p[0]) < 9 for p in out):
            continue
        out.append(q)
    return set(out)


def sesame_px(pts, wide, region):
    """깨 낱알. 어두운 피 위에서는 가로 2x1 로 키워야 보인다. 떡 밖으로는 안 나간다."""
    if not wide:
        return pts
    return {q for p in pts for q in ((p[0], p[1]), (p[0] + 1, p[1]))} & region


def draw(im, px, color):
    p = im.load()
    c = rgb(color)
    for (x, y) in px:
        if 0 <= x < N and 0 <= y < N:
            p[x, y] = c


# ---------- 자리 ----------
# 1판은 반쪽을 알 안쪽에 너무 깊이 넣어 초승달 조각만 보였다. 반쪽은 대부분 알 밖에
# 두고 알의 오른쪽 아래만 4px 쯤 덮게 민다.
# 알을 왼쪽에, 반쪽을 오른쪽 아래에 두되 바닥 높이를 비슷하게 맞춘다. 알을 너무 위로
# 올리면 둘이 대각선으로 이어져 긴 바나나 하나로 보인다 (4판 tilt 의 실패).
BACK = blob(10.0, 12.6, 8.8, 7.0)                      # 폭 약 18px
FRONT_C = (21.0, 20.6)                             # 반달의 평평한 면 한가운데
# 5판은 반쪽이 알보다 4px 아래로 내려가 둘이 대각선으로 누웠다(누운 버섯).
# 6판은 바닥을 거의 맞춰(알 바닥 19, 반쪽 바닥 21) 반쪽이 알에 기대어 선 모양으로 올린다.
FRONT_RX, FRONT_RY = 8.0, 11.2                         # 찍히는 반쪽은 14x12. 피 2px 를 빼면 소가 꼭 10x8 이다


def front_shape(deg):
    return half_ell(FRONT_C[0], FRONT_C[1], FRONT_RX, FRONT_RY, deg)


def skin_of(s):
    key = "bean" if s["topping"] == "bean" else s["skin"]
    sb, ss, sf = SKIN[key]
    if s["topping"] == "bean" and s["skin"]:
        sf = SKIN[s["skin"]][2]
    return sb, ss, sf


def make(cid, deg=None):
    deg = TILT if deg is None else deg
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    s = recipe(cid)
    sb, ss, sf = skin_of(s)
    front = front_shape(deg)
    back = BACK - front

    # 뒤 알
    draw(im, back, sb)
    draw(im, shade_edge(back), ss)
    draw(im, {q for q in (ellipse(7.2, 7.8, 2.0, 1.6)) if q in back}, sf)   # 왼쪽 위 하이라이트
    wide = s["skin"] in DARK_SKIN
    if s["topping"] == "sesame":
        draw(im, sesame_px(spread_dots(erode(back), 8, 5, 1), wide, erode(back)), SESAME_DOT)
    if s["topping"] == "bean":
        draw(im, spread_dots(erode(back), 6, 5, 3), BEAN_POWDER)
    # 앞 반쪽이 닿는 자리에 1px 짙은 띠 — 두 덩이가 한 덩어리로 안 보이게 가른다.
    draw(im, {q for q in dilate1(front) if q in back}, ss)

    # 앞 반쪽 : 피 띠 2px + 떡살 + 소
    draw(im, front, sb)
    draw(im, shade_edge(front), ss)
    flesh = erode(front)
    draw(im, flesh, sf)
    # 고물은 반쪽의 「등」(둥근 겉면)에만 얹는다. 자른 면 쪽 테두리에 찍으면
    # 떡에 묻은 것이 아니라 떨어진 부스러기처럼 보인다.
    back_band = {q for q in (front - flesh) if local(q[0], q[1], *FRONT_C, deg)[1] <= -3.0}
    if s["topping"] == "sesame":
        draw(im, sesame_px(spread_dots(back_band, 3, 4, 7), wide, back_band), SESAME_DOT)
    if s["topping"] == "bean":
        draw(im, spread_dots(back_band, 3, 4, 5), BEAN_POWDER)
    k = s["filling"]
    if k:
        fb, fa = FILL[k]
        a = math.radians(deg)  # 소는 반달의 평평한 면과 나란히, 등 쪽으로 조금 올려 놓는다
        core = {q for q in ellipse(FRONT_C[0] + 4.2 * math.sin(a), FRONT_C[1] - 4.2 * math.cos(a),
                                   5.2, 4.8, deg) if q in flesh}
        draw(im, core, fb)
        if k == "redbean":
            draw(im, spread_dots(erode(core, DISK1), 4, 3, 2), fa)      # 팥알
        else:
            for q in spread_dots(erode(core, DISK1), 3, 4, 4):          # 잣알 + 갈색 꼭지
                draw(im, {(q[0] + i, q[1] + j) for i in (0, 1) for j in (0, 1)} & core, sf)
                draw(im, {q} & core, fa)
    return im


# ---------- 비교 그림 ----------
def grass_bg(w, h, scale):
    g = Image.open(os.path.join(ART, "Farm", "ground_grass_0.png")).convert("RGBA")
    g = g.resize((32 * scale, 32 * scale), Image.NEAREST)
    bg = Image.new("RGBA", (w, h))
    for i in range(w // g.width + 1):
        for j in range(h // g.height + 1):
            bg.alpha_composite(g, (i * g.width, j * g.height))
    return bg.crop((0, 0, w, h))


def font(sz):
    try:
        return ImageFont.load_default(size=sz)
    except TypeError:
        return ImageFont.load_default()


def load_v5():
    """5판 그림을 참고 줄로 같이 싣는다 — 무엇이 바뀌었는지 바로 보이게."""
    out = []
    for cid in IDS:
        p = os.path.join(D, "setG_32_v5backup", cid + ".png")
        out.append(Image.open(p).convert("RGBA") if os.path.exists(p)
                   else Image.new("RGBA", (N, N)))
    return out


def compare(icons, ref=None):
    S, COL, PAD = 6, 32 * 6 + 8, 96
    W = PAD + len(IDS) * COL
    head, big = 52, 32 * 6
    ph = head + big * (2 if ref else 1) + 110 + 60
    panels = [("on grass tile", None, (255, 255, 255)),
              ("dark UI  #2B2B33", (0x2B, 0x2B, 0x33, 255), (235, 235, 240)),
              ("light UI  #E8E0D0", (0xE8, 0xE0, 0xD0, 255), (40, 35, 30))]
    out = Image.new("RGBA", (W, ph * 3), (20, 20, 22, 255))
    f, fs, ft = font(20), font(14), font(13)
    for pi, (title, fill, ink) in enumerate(panels):
        panel = grass_bg(W, ph, S) if fill is None else Image.new("RGBA", (W, ph), fill)
        d = ImageDraw.Draw(panel)
        d.text((6, 6), title + "   ·   32x32   ·   tilt %d" % TILT, font=f, fill=ink)
        for c, cid in enumerate(IDS):
            nm = cid.replace("_cake", "").replace("redbean", "rb").replace("mugwort", "mg") \
                    .replace("sesame", "se").replace("pinenut", "pn").replace("jujube", "ju")
            d.text((PAD + c * COL + 2, 30), nm, font=ft, fill=ink)
        y0 = head
        if ref:
            d.text((4, y0 + big // 2), "5판", font=fs, fill=ink)
            for c, ic in enumerate(ref):
                panel.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST),
                                      (PAD + c * COL, y0))
            y0 += big
        d.text((4, y0 + big // 2), "6판 x6", font=fs, fill=ink)
        for c, ic in enumerate(icons):
            panel.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST), (PAD + c * COL, y0))
        # 실제 쓰임 크기 : x3(96) · x2(64) · x1(32) · 0.75배(24 — 만들기 줄에서 이렇게 보인다)
        y = y0 + big + 8
        d.text((4, y + 40), "x3 / x2", font=fs, fill=ink)
        for c, ic in enumerate(icons):
            x = PAD + c * COL
            panel.alpha_composite(ic.resize((96, 96), Image.NEAREST), (x, y))
            panel.alpha_composite(ic.resize((64, 64), Image.NEAREST), (x + 104, y + 16))
        y += 110
        d.text((4, y + 16), "x1 / 0.75", font=fs, fill=ink)
        for c, ic in enumerate(icons):
            x = PAD + c * COL
            panel.alpha_composite(ic, (x + 20, y))
            panel.alpha_composite(ic.resize((24, 24), Image.NEAREST), (x + 64, y + 4))
        out.alpha_composite(panel, (0, pi * ph))
    out.save(os.path.join(D, "compare_cake_v5.png"))


def probe(degs=(30, 45), cids=("redbean_cake", "sesame_cake", "redbean_mugwort_sesame_cake"),
          path=None):
    """각도 후보를 나란히 뽑아 눈으로 고르는 그림."""
    S = 8
    w = len(degs) * len(cids) * (32 * S + 8) + 16
    im = Image.new("RGBA", (w, 32 * S + 40), (0x2B, 0x2B, 0x33, 255))
    d = ImageDraw.Draw(im)
    i = 0
    for g in degs:
        for cid in cids:
            im.alpha_composite(make(cid, g).resize((32 * S, 32 * S), Image.NEAREST),
                               (8 + i * (32 * S + 8), 32))
            d.text((8 + i * (32 * S + 8), 8), "deg %d  %s" % (g, cid[:10]), font=font(15),
                   fill=(255, 255, 255))
            i += 1
    im.save(path or os.path.join(D, "probe_tilt.png"))


def main():
    od = os.path.join(D, "setG_32")
    os.makedirs(od, exist_ok=True)
    icons = []
    for cid in IDS:
        im = make(cid)
        im.save(os.path.join(od, cid + ".png"))
        icons.append(im)
        print("setG_32", cid, im.size, "colors", len([c for c in im.getcolors(999) if c[1][3]]))
    compare(icons, load_v5())


if __name__ == "__main__":
    main()

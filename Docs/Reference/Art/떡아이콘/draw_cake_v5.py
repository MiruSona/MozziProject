"""떡 아이콘 7판 — 같은 크기 두 알, 앞 알은 단면이 정면을 본다 (setG_32).

6판까지는 앞의 반쪽을 작게·비스듬히 뒀는데, 사용자가 구도를 단순하게 바꿨다.
  · 앞 떡은 뒤 떡과 **같은 실루엣·같은 크기**, 기울기 0.
  · 앞 떡은 세로로 반 갈라 **자른 면이 보는 사람을 향한다** — 앞면 전체가 단면이다.
    바깥 피 테두리 2px -> 안쪽 떡살 -> 가운데 소 덩어리.
  · 단면은 평평한 면이니 떡살에는 명암을 안 넣는다. 피 테두리만 아래·오른쪽이 그늘이다.

규칙 : 32x32 · RGBA · 알파 0/255 · 외곽선 없음 · 빛 왼쪽 위 · 장당 3~7색.
재료 자리 규칙(팥·잣=소 · 쑥·대추=피 · 콩·깨=고물)과 11종 id 는 3판 그대로다.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ART = r"C:\Mirusona\Project\ClaudeProject\ProjectMozzi2\ProjectMozziUnity\Assets\_Project\Resources\Art"
N = 32

SKIN = {
    None: ("#F2E9DC", "#C9BCA6", "#FFFBF2"),
    "mugwort": ("#6E9B4A", "#4C7433", "#C9DCB4"),
    "jujube": ("#BF6448", "#8E3B26", "#E8C4B2"),
    "bean": ("#E6C868", "#BE9840", "#FFFBF2"),
}
# 잣 소는 7판에서 흰 떡살과 색이 가까워 흐렸다. 8판에서 조금 더 노릇하게 내렸다
# (#E8D49A -> #DEC27E). 더 내리면 밤이 된다.
FILL = {"redbean": ("#7A2A2A", "#A84848"), "pinenut": ("#DEC27E", "#A9743E")}
PINENUT_MEAT = "#F4EACC"   # 잣알 — 소 밑색보다 밝은 크림
SESAME_DOT = "#8A5A2C"             # 볶은깨 갈색 (6판에서 정한 값)
DARK_SKIN = ("mugwort", "jujube")  # 이 피 위에서는 깨를 가로 2x1 로 찍는다
BEAN_POWDER = "#F7E6A8"

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


def ellipse(cx, cy, rx, ry):
    return {(x, y) for y in range(N) for x in range(N)
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0}


def blob(cx, cy, rx, ry):
    """온전한 떡 한 알 — 위는 동글고 아래가 살짝 퍼진다 (6판까지의 알 실루엣)."""
    return ellipse(cx, cy, rx, ry) | ellipse(cx, cy + ry * 0.40, rx * 1.05, ry * 0.58)


DISK2 = [(dx, dy) for dx in range(-2, 3) for dy in range(-2, 3) if dx * dx + dy * dy <= 4]
DISK1 = [(dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]


def erode(px, disk=DISK2):
    return {p for p in px if all((p[0] + d[0], p[1] + d[1]) in px for d in disk)}


def dilate1(px):
    return {(p[0] + d[0], p[1] + d[1]) for p in px for d in DISK1}


def shade_edge(px, off=(2, 3)):
    return {q for q in px if (q[0] + off[0], q[1] + off[1]) not in px}


def spread_dots(px, count, gap=4, seed=0):
    """점을 고르게 흩는다. 서로 gap 이상 떼고, 가까운 거리에서 같은 줄에 두 점을 안 놓는다
    — 가로로 나란한 두 점이 눈으로 보여 떡이 얼굴이 되기 때문이다."""
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
    if not wide:
        return pts
    return {q for p in pts for q in ((p[0], p[1]), (p[0] + 1, p[1]))} & region


def pinenut_grain(x, y, lean):
    """잣알 한 톨 — 2x3 물방울. (x, y) 가 꼭지(갈색 1px)가 붙는 위쪽 끝이고,
    lean 이 -1 이면 왼쪽으로, +1 이면 오른쪽으로 기운다."""
    return {(x, y), (x + lean, y + 1), (x, y + 1), (x + lean, y + 2)}


def draw(im, px, color):
    p = im.load()
    c = rgb(color)
    for (x, y) in px:
        if 0 <= x < N and 0 <= y < N:
            p[x, y] = c


# ---------- 자리 ----------
# 알 하나가 19x14. 두 알을 가로로 11px, 세로로 4px 어긋나게 둔다 (겹침이 알 폭의 약 40%).
RX, RY = 9.0, 7.0
BACK_C = (9.5, 11.6)
OFF = (11, 4)
BACK_DARK = True        # 뒤 알을 한 톤 어둡게 칠해 같은 색 떡도 두 덩이로 읽히게 한다


def shapes(off=None):
    ox, oy = OFF if off is None else off
    back = blob(BACK_C[0], BACK_C[1], RX, RY)
    front = blob(BACK_C[0] + ox, BACK_C[1] + oy, RX, RY)
    return back, front


def skin_of(s):
    key = "bean" if s["topping"] == "bean" else s["skin"]
    sb, ss, sf = SKIN[key]
    if s["topping"] == "bean" and s["skin"]:
        sf = SKIN[s["skin"]][2]
    return sb, ss, sf


def make(cid, off=None, back_dark=None):
    back_dark = BACK_DARK if back_dark is None else back_dark
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    s = recipe(cid)
    sb, ss, sf = skin_of(s)
    back_all, front = shapes(off)
    back = back_all - front
    wide = s["skin"] in DARK_SKIN

    # --- 뒤 알 : 둥근 겉면이라 명암이 있다 ---
    # back_dark 면 밑칠을 그늘색으로 깔아 뒤 알 전체를 한 톤 낮춘다. 색이 늘지 않는다.
    b_base, b_hi = (ss, sb) if back_dark else (sb, sf)
    draw(im, back, b_base)
    if not back_dark:
        draw(im, shade_edge(back), ss)
    draw(im, {q for q in ellipse(BACK_C[0] - 2.6, BACK_C[1] - 4.0, 2.2, 1.6) if q in back}, b_hi)
    if s["topping"] == "sesame":
        reg = erode(back_all) - front
        draw(im, sesame_px(spread_dots(reg, 7, 5, 1), wide, reg), SESAME_DOT)
    if s["topping"] == "bean":
        draw(im, spread_dots(erode(back_all) - front, 5, 5, 3), BEAN_POWDER)

    # --- 앞 알 : 자른 면이 정면. 피 테두리 2px -> 떡살 -> 소 ---
    draw(im, front, sb)
    rim = front - erode(front)
    draw(im, {q for q in shade_edge(front, (1, 2)) if q in rim}, ss)   # 아래·오른쪽 피 그늘
    flesh = erode(front)
    draw(im, flesh, sf)            # 평평한 단면이라 명암 없이 고르게 칠한다
    if s["topping"] == "sesame":   # 고물은 피 바깥쪽에 묻는다
        draw(im, sesame_px(spread_dots(rim, 5, 4, 7), wide, rim), SESAME_DOT)
    if s["topping"] == "bean":
        draw(im, spread_dots(rim, 4, 4, 5), BEAN_POWDER)

    k = s["filling"]
    if k:
        fb, fa = FILL[k]
        fx, fy = BACK_C[0] + (OFF if off is None else off)[0], \
                 BACK_C[1] + (OFF if off is None else off)[1]
        # 소는 가운데보다 1px 아래, 가로로 넓게 — 한가운데 동그라미면 눈알·도넛으로 보인다.
        core = {q for q in blob(fx, fy + 1.0, RX * 0.68, RY * 0.52) if q in flesh}
        draw(im, core, fb)
        if k == "redbean":
            top = {q for q in core if (q[0], q[1] - 1) not in core}    # 소 윗쪽 1px 하이라이트
            draw(im, top, fa)
            inner = erode(core, DISK1) - top
            draw(im, spread_dots(inner, 5, 3, 2), fa)                  # 밝은 팥알
        else:
            # 7판의 잣떡은 소 위 아치 선 + 그 아래 점이 웃는 얼굴(식빵)로 보였다.
            # 8판은 아치를 없애고(소 윤곽은 밑색과 떡살의 색 차이로만 갈린다) 잣알을
            # 물방울 모양 낱알 셋으로 바꿨다. 셋 다 높이·기울기가 달라 눈·입이 안 된다.
            cx0, cy0 = fx, fy + 1.0
            # 왼쪽·오른쪽이 같은 높이고 가운데가 아래면 눈·입이 된다. 가운데를 가장 위로
            # 올려 층계처럼 흩는다.
            for dx, dy, lean in ((-3.8, 0.8, -1), (0.0, -1.6, 1), (3.6, 0.2, -1)):
                draw(im, pinenut_grain(round(cx0 + dx), round(cy0 + dy), lean) & core,
                     PINENUT_MEAT)
                draw(im, {(round(cx0 + dx), round(cy0 + dy))} & core, fa)   # 갈색 꼭지 1px
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


def load_ref(folder="setG_32_v6backup"):
    out = []
    for cid in IDS:
        p = os.path.join(D, folder, cid + ".png")
        out.append(Image.open(p).convert("RGBA") if os.path.exists(p)
                   else Image.new("RGBA", (N, N)))
    return out


def compare(icons, ref=None, path="compare_cake_v6.png"):
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
        d.text((6, 6), title + "   ·   32x32   ·   off %s" % (OFF,), font=f, fill=ink)
        for c, cid in enumerate(IDS):
            nm = cid.replace("_cake", "").replace("redbean", "rb").replace("mugwort", "mg") \
                    .replace("sesame", "se").replace("pinenut", "pn").replace("jujube", "ju")
            d.text((PAD + c * COL + 2, 30), nm, font=ft, fill=ink)
        y0 = head
        if ref:
            d.text((4, y0 + big // 2), "6pan", font=fs, fill=ink)
            for c, ic in enumerate(ref):
                panel.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST),
                                      (PAD + c * COL, y0))
            y0 += big
        d.text((4, y0 + big // 2), "7pan x6", font=fs, fill=ink)
        for c, ic in enumerate(icons):
            panel.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST), (PAD + c * COL, y0))
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
    out.save(os.path.join(D, path))


def probe(cases, cids=("plain_cake", "redbean_cake", "mugwort_sesame_cake"), path=None):
    """어긋남·분리 방식 후보를 나란히 뽑아 눈으로 고르는 그림."""
    S = 8
    w = len(cases) * len(cids) * (32 * S + 8) + 16
    im = Image.new("RGBA", (w, 32 * S + 40), (0x2B, 0x2B, 0x33, 255))
    d = ImageDraw.Draw(im)
    i = 0
    for off, dark in cases:
        for cid in cids:
            im.alpha_composite(make(cid, off, dark).resize((32 * S, 32 * S), Image.NEAREST),
                               (8 + i * (32 * S + 8), 32))
            d.text((8 + i * (32 * S + 8), 8), "%s %s %s" % (off, "dark" if dark else "flat",
                                                            cid[:8]), font=font(15),
                   fill=(255, 255, 255))
            i += 1
    im.save(path or os.path.join(D, "probe_off.png"))


def compare_pinenut(path="compare_cake_v7.png"):
    """잣떡 두 장만 앞(7판)/뒤(8판)로 견주는 그림."""
    cids = ["pinenut_cake", "pinenut_jujube_cake"]
    old = [Image.open(os.path.join(D, "setG_32_v7backup", c + ".png")).convert("RGBA")
           for c in cids]
    new = [make(c) for c in cids]
    S, COL, PAD = 8, 32 * 8 + 24, 80
    W = PAD + len(cids) * 2 * COL
    ph = 40 + 32 * S + 70
    panels = [("on grass tile", None, (255, 255, 255)),
              ("dark UI  #2B2B33", (0x2B, 0x2B, 0x33, 255), (235, 235, 240)),
              ("light UI  #E8E0D0", (0xE8, 0xE0, 0xD0, 255), (40, 35, 30))]
    out = Image.new("RGBA", (W, ph * 3), (20, 20, 22, 255))
    f, fs = font(20), font(15)
    for pi, (title, fill, ink) in enumerate(panels):
        panel = grass_bg(W, ph, S) if fill is None else Image.new("RGBA", (W, ph), fill)
        d = ImageDraw.Draw(panel)
        d.text((6, 6), title + "   ·   pinenut filling fix (7pan -> 8pan)", font=f, fill=ink)
        for i, c in enumerate(cids):
            for j, (tag, ic) in enumerate((("7pan", old[i]), ("8pan", new[i]))):
                x = PAD + (i * 2 + j) * COL
                d.text((x, 30), "%s %s" % (c.replace("_cake", ""), tag), font=fs, fill=ink)
                panel.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST), (x, 48))
                y = 48 + 32 * S + 6
                panel.alpha_composite(ic.resize((64, 64), Image.NEAREST), (x, y))
                panel.alpha_composite(ic, (x + 72, y + 16))
        out.alpha_composite(panel, (0, pi * ph))
    out.save(os.path.join(D, path))


def final_sheet(icons, path="final_cake_sheet.png"):
    """확정본 11장을 어두운 UI 바탕에 한 줄로."""
    S, COL, PAD = 4, 32 * 4 + 14, 12
    W = PAD * 2 + len(IDS) * COL
    im = Image.new("RGBA", (W, 32 * S + 64), (0x2B, 0x2B, 0x33, 255))
    d = ImageDraw.Draw(im)
    d.text((PAD, 8), "mochi icons - final 11 - 32x32 - draw_cake_v5.py", font=font(17),
           fill=(235, 235, 240))
    for c, (cid, ic) in enumerate(zip(IDS, icons)):
        x = PAD + c * COL
        im.alpha_composite(ic.resize((32 * S, 32 * S), Image.NEAREST), (x, 34))
        d.text((x, 34 + 32 * S + 4), cid.replace("_cake", ""), font=font(12),
               fill=(200, 200, 210))
    im.save(os.path.join(D, path))


def main():
    od = os.path.join(D, "setG_32")
    os.makedirs(od, exist_ok=True)
    icons = []
    for cid in IDS:
        im = make(cid)
        im.save(os.path.join(od, cid + ".png"))
        icons.append(im)
        print("setG_32", cid, im.size, "colors", len([c for c in im.getcolors(999) if c[1][3]]))
    compare(icons, load_ref())
    compare_pinenut()
    final_sheet(icons)


if __name__ == "__main__":
    main()

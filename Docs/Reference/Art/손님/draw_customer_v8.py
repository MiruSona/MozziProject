"""가판대 손님 시안 8판 — **동물만**. v7 의 벌 B 를 고르고 벌 A(사람)는 접었다.

사용자 판정 (2026-09-22) :
  「좋아 동물이 맘에들어. 동물쪽을 디테일좀 더 살리고 입은 점 하나만 할꺼면 빼던가
   다른 모양으로 해봐줄래? 그리고 눈도 다른표정도 해봐줘. 다른동물도 해봐줘 고양이나 고슴도치나..」

v7 에서 바꾼 것 네 가지 :
  1. **종마다 그 동물임을 알게 하는 요소를 1~2개 더했다** — 꼬리 · 주둥이 · 수염 · 앞니 · 가시 · 배 투톤.
  2. **점 하나 입을 버렸다.** 종마다 ω · ‿ · 코+Y자 중에서 어울리는 것을 쓴다. (a)없음 안도 같이 그려 비교한다.
  3. **눈 후보 셋**(점눈 2×2 · 세로 타원 2×3 · 찹쌀이식 2×4)을 비교 그림으로 뽑는다.
  4. **표정 여섯** — 기본 · 웃음 · 시무룩 · 놀람 · 반함 · 화남.
  5. 옆에 붙인 앞발이 「팔짱」으로 보여 **몸 앞 가운데로 작게 모았다.**

지킨 것 : 색 10~14가지 · 둥근 모서리 · 한 단 명암 · 왼쪽 위 하이라이트 점 · 먹색(#12110F) 외곽선.
뼈대(머리·몸 덩어리)는 여섯 종이 **같은 한 장**을 쓴다. 귀·꼬리·무늬·색만 바꾼다.

산출은 이 파일 옆 v8/ 에 떨어진다. v7 이하 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import colorsys
import os
import sys

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v8")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 48, 64
CX = 24.0                 # 좌우 대칭축은 픽셀 23 과 24 의 경계다 (mirror : x → 47-x)
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

OUTLINE = "#12110F"       # 찹쌀이 눈과 같은 먹색. 순검정이 아니다
INK = "#12110F"
CHEEK = "#F49CA8"
SPARK = "#FFFFFF"
SHADOW_A = 56


# ======================================================================
# 색 도우미
# ======================================================================
def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def darker(hexcol, vm=0.78, sm=1.15):
    """그늘 한 단 — 명도를 내리고 채도를 조금 올린다. 회색을 섞으면 탁해진다."""
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    rr, gg, bb = colorsys.hsv_to_rgb(h, min(1.0, s * sm), v * vm)
    return "#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255))


def lighter(hexcol, vm=1.12, sm=0.80):
    """하이라이트 한 점 — 떡·재료 아이콘 화풍에 맞춰 왼쪽 위에 찍는다."""
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, s_, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    rr, gg, bb = colorsys.hsv_to_rgb(h, max(0.0, s_ * sm), min(1.0, v * vm))
    return "#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255))


# ======================================================================
# 픽셀 집합 도우미
# ======================================================================
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def ellipse(cx, cy, rx, ry, y0=None, y1=None):
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def superellipse(cx, cy, rx, ry, n, y0=None, y1=None):
    """n 이 커질수록 모서리가 둥근 네모가 된다."""
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if abs((x + 0.5 - cx) / rx) ** n + abs((y + 0.5 - cy) / ry) ** n <= 1.0:
                out.add((x, y))
    return out


def tri_up(cx, ybase, w, h):
    """**위로 뾰족한** 삼각 — 고양이 귀·고슴도치 가시에 쓴다.
    v7 의 tri() 는 아래로 뾰족해 귀가 잘려 보였다."""
    out = set()
    for i in range(h):
        half = w / 2.0 * (h - i) / float(h)
        y = ybase - i
        out |= {(x, y) for x in range(int(round(cx - half)), int(round(cx + half)))}
    return out


def blob_path(pts, r):
    """점 목록을 굵기 r 로 이은 곡선 — 꼬리에 쓴다."""
    out = set()
    for (cx, cy, rr) in pts:
        out |= ellipse(cx, cy, rr if rr else r, rr if rr else r)
    return out


def _curve(pts, r, step=0.5):
    """점 사이를 직선으로 잘게 쪼개 동그라미를 끊김 없이 잇는다."""
    out = []
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(1, int(((x1 - x0) ** 2 + (y1 - y0) ** 2) ** 0.5 / step))
        for i in range(n + 1):
            t = i / float(n)
            out.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r))
    return out


def mirror(px):
    return px | {(W - 1 - x, y) for (x, y) in px}


def edge_of(px):
    """실루엣 **안쪽** 한 겹. 바깥으로 넓히지 않아 캔버스를 안 넘는다."""
    return {(x, y) for (x, y) in px
            if not all(n in px for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def touching(a, b):
    return {(x, y) for (x, y) in a
            if any(n in b for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def spikes_up(px, xs, h=4):
    """덩어리 윗변에서 **위로** 솟은 가시들 — 고슴도치."""
    tops = {}
    for (x, y) in px:
        tops[x] = min(tops.get(x, 999), y)
    out = set()
    for x in xs:
        if x not in tops:
            continue
        t = tops[x]
        for i, w in enumerate((3, 2, 1)):
            out |= {(x + k, t - 1 - i) for k in range(-(w // 2), -(w // 2) + w)}
        if h > 3:
            out.add((x, t - 4))
    return out


def spikes_side(px, ys, depth=3):
    """덩어리 왼쪽 변에서 **바깥으로** 뻗은 가시들. mirror 로 오른쪽도 같이 낸다."""
    lefts = {}
    for (x, y) in px:
        lefts[y] = min(lefts.get(y, 999), x)
    out = set()
    for y in ys:
        if y not in lefts:
            continue
        L = lefts[y]
        for i, hh in enumerate((3, 2, 1)):
            out |= {(L - 1 - i, y + k) for k in range(-(hh // 2), -(hh // 2) + hh)}
    return out


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, px, col):
    p, c = im.load(), rgb(col)
    for (x, y) in px:
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = c


def paint(im, regions):
    """regions : [(픽셀집합, 속색, 그늘색 또는 None)]. 그늘은 오른쪽·아래 대각선 한 겹."""
    for px, base, shade in regions:
        put(im, px, base)
        if shade:
            put(im, {(x, y) for (x, y) in px if (x + 1, y + 1) not in px}, shade)


def put_shadow(im, rx=9.0):
    p = im.load()
    for (x, y) in ellipse(CX, GROUND_Y - 2, rx, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)


# ======================================================================
# 뼈대 — 여섯 종이 같이 쓴다
# ======================================================================
SK = dict(
    head=dict(cy=34.0, rx=13.0, ry=11.0, n=2.5, y0=23, y1=45),
    body=dict(cy=54.0, rx=12.0, ry=11.0, n=3.0, y0=43, y1=64),
    belly=(CX, 55.5, 8.0, 8.0),        # 배 투톤 타원
    muzzle=(CX, 38.5, 6.5, 4.0),       # 주둥이 (곰·강아지·고슴도치)
    paw=(20.5, 57.0, 3.0, 3.2),        # **몸 앞 가운데**로 모은 앞발 — v7 은 옆에 있어 팔짱처럼 보였다
    foot=(18.0, 62.0, 4.5, 2.5),
    face=dict(eye_x=17, eye_y=31, brow_y=29, mouth_y=38, cheek_y=36),
)


# ---------- 귀 ----------
def ears_of(kind):
    """(바깥 귀, 귀속) 한 쌍. 귀가 종을 가르는 가장 큰 표시다."""
    if kind == "rabbit":       # 긴 귀 — 머리보다 한참 위로
        return (mirror(ellipse(16.0, 21.0, 3.5, 9.0)),
                mirror(ellipse(16.0, 21.5, 1.5, 6.0)))
    if kind == "bear":         # 작고 둥근 귀
        return (mirror(ellipse(14.0, 26.5, 4.5, 4.5)),
                mirror(ellipse(14.0, 26.5, 2.0, 2.0)))
    if kind == "cat":          # 세모 귀 — 위로 뾰족
        return (mirror(tri_up(14.5, 26, 10, 9)),
                mirror(tri_up(14.5, 25, 5, 5)))
    if kind == "hedgehog":     # 가시에 묻히는 아주 작은 귀
        return (mirror(ellipse(15.0, 29.0, 3.0, 3.0)),
                mirror(ellipse(15.0, 29.0, 1.5, 1.5)))
    if kind == "dog":          # 늘어진 귀 — 옆으로 길게 처진다
        return (mirror(ellipse(11.5, 35.0, 4.5, 9.0)),
                mirror(ellipse(11.5, 35.0, 2.0, 5.5)))
    if kind == "squirrel":     # 작고 도톰한 귀
        return (mirror(ellipse(15.0, 25.5, 3.5, 4.5)),
                mirror(ellipse(15.0, 25.5, 1.5, 2.0)))
    return (set(), set())


# ---------- 꼬리 ----------
def tail_of(kind):
    """오른쪽 뒤로 나오는 꼬리. 실루엣의 일부라 종을 멀리서도 가른다."""
    if kind == "rabbit":       # 동그란 솜뭉치
        return ellipse(37.0, 57.0, 4.0, 4.0)
    if kind == "cat":          # 위로 휘는 가는 꼬리. **점을 1.5px 간격으로 촘촘히** —
                               # 3px 간격이면 지름 4px 원들이 떨어져 토막 꼬리가 된다 (1판 버그)
        return blob_path(_curve([(35.0, 59.0), (38.0, 56.5), (40.0, 53.0), (41.5, 49.0),
                                 (42.0, 45.0), (40.5, 42.0)], 2.2), 2.2)
    if kind == "dog":          # 짧게 위로 든 꼬리
        return blob_path(_curve([(35.0, 56.0), (37.5, 53.0), (39.0, 49.5), (39.0, 46.5)],
                                2.4), 2.4)
    if kind == "squirrel":     # 커다란 북슬 꼬리 — 다람쥐의 핵심
        return superellipse(38.5, 44.0, 7.0, 12.0, 2.2)
    return set()               # 곰 · 고슴도치는 꼬리 없음


SPECIES = [
    dict(key="rabbit", name="토끼", ear="rabbit", tail="rabbit",
         belly=True, muzzle=False, mouth="teeth", tail_light=True,
         note="긴 귀 + 앞니 + 솜꼬리"),
    dict(key="bear", name="곰", ear="bear", tail=None,
         belly=False, muzzle=True, mouth="nose", note="둥근 귀 + 밝은 주둥이 + 코"),
    dict(key="cat", name="고양이", ear="cat", tail="cat",
         belly=True, muzzle=False, mouth="omega", whisker=True, note="세모 귀 + 수염 + 꼬리"),
    dict(key="hedgehog", name="고슴도치", ear="hedgehog", tail=None,
         belly=True, muzzle=True, mouth="nose", quill=True, note="등 가시 + 밝은 얼굴·배"),
    dict(key="dog", name="강아지", ear="dog", tail="dog",
         belly=True, muzzle=True, mouth="nose", note="늘어진 귀 + 주둥이 + 든 꼬리"),
    dict(key="squirrel", name="다람쥐", ear="squirrel", tail="squirrel",
         belly=True, muzzle=False, mouth="omega", note="큰 북슬 꼬리 + 밝은 배"),
]

# 종마다 색 두 벌. inner 는 귀속·꼬리속, light 는 배·주둥이다
COLORS = {
    "rabbit": [dict(name="흰", body="#F2EDE4", light="#FFFFFA", inner="#F4B8C4"),
               dict(name="회갈", body="#CDBBA8", light="#EEE3D4", inner="#F4B8C4")],
    "bear": [dict(name="갈색", body="#C89A6B", light="#E8C9A6", inner="#E8C9A6"),
             dict(name="진갈", body="#9E7551", light="#D3AF89", inner="#D3AF89")],
    "cat": [dict(name="회색", body="#B9C0CA", light="#E2E6EC", inner="#F4B8C4"),
            dict(name="노랑", body="#E8C98A", light="#F7E8C4", inner="#F4B8C4")],
    "hedgehog": [dict(name="연갈", body="#9C8062", light="#F0DCC4", inner="#F4B8C4"),
                 dict(name="회보라", body="#8E84A0", light="#E4DCEC", inner="#F4B8C4")],
    "dog": [dict(name="크림", body="#E0BC8C", light="#F6E4C8", inner="#F4B8C4"),
            dict(name="갈흰", body="#B98A5E", light="#EAD9BE", inner="#F4B8C4")],
    "squirrel": [dict(name="주황갈", body="#D08C4C", light="#F2D6A8", inner="#F4B8C4"),
                 dict(name="회다람", body="#B0A290", light="#E8E0D2", inner="#F4B8C4")],
}


def parts(sp):
    h, b = SK["head"], SK["body"]
    head = superellipse(CX, h["cy"], h["rx"], h["ry"], h["n"], h["y0"], h["y1"])
    body = superellipse(CX, b["cy"], b["rx"], b["ry"], b["n"], b["y0"], b["y1"])
    blob = head | body
    ear, inner = ears_of(sp["ear"])
    ear = ear - blob
    inner = inner - blob
    tail = tail_of(sp["tail"]) - blob if sp["tail"] else set()
    quill, quill_side = set(), set()
    if sp.get("quill"):
        quill = spikes_up(head, [x for x in range(15, 34) if (x - 15) % 3 == 0], h=4)
        quill |= mirror(spikes_side(head, [26, 30, 34, 38]))
        quill |= mirror(spikes_side(body, [46, 50, 54]))
        quill -= blob
    belly = (ellipse(*SK["belly"]) & body) if sp["belly"] else set()
    muzzle = (ellipse(*SK["muzzle"]) & head) if sp["muzzle"] else set()
    paws = mirror(ellipse(*SK["paw"])) & body
    feet = mirror(ellipse(*SK["foot"])) & body
    belly -= paws | feet
    return dict(head=head, body=body, blob=blob, ears=ear, inner=inner, tail=tail,
                quill=quill, belly=belly, muzzle=muzzle, paws=paws, feet=feet)


# ======================================================================
# 얼굴 — 눈 후보 3가지 × 표정 6가지
# ======================================================================
EYE_KINDS = ["dot", "tall", "mozzi"]
EYE_LABEL = {"dot": "점눈 2×2", "tall": "세로 타원 2×3", "mozzi": "찹쌀이식 2×4"}
FACE_LABEL = {1: "기본", 2: "웃음", 3: "시무룩", 4: "놀람", 5: "반함", 6: "화남"}


def eye_shape(kind, ex):
    if kind == "dot":
        return rect(ex, 32, ex + 2, 34)
    if kind == "tall":
        return rect(ex, 31, ex + 2, 34)
    return rect(ex, 31, ex + 2, 35)


def heart(x0, y0):
    """5×4 작은 하트 — 반함 표정의 볼."""
    out = set()
    for x in (x0, x0 + 1, x0 + 3, x0 + 4):
        out.add((x, y0))
    out |= {(x0 + i, y0 + 1) for i in range(5)}
    out |= {(x0 + i, y0 + 2) for i in range(1, 4)}
    out.add((x0 + 2, y0 + 3))
    return out


def mouth_shape(style, my, kind):
    """입 — **점 하나는 안 쓴다.** 종마다 어울리는 모양을 고른다.
    omega : ω 두 산 · teeth : ω + 앞니 · nose : 코 + Y 자 입선"""
    ink, teeth = set(), set()
    if kind in (2, 5):                      # 웃음·반함 — ‿ 크게
        ink = {(21, my), (22, my + 1), (23, my + 2), (24, my + 2),
               (25, my + 1), (26, my)}
    elif kind == 3:                         # 시무룩 — ⌒
        ink = {(22, my + 2), (23, my + 1), (24, my + 1), (25, my + 2)}
    elif kind == 4:                         # 놀람 — o
        ink = ellipse(24.0, my + 1.5, 2.0, 2.2)
        ink -= ellipse(24.0, my + 1.5, 1.0, 1.2)
    elif kind == 6:                         # 화남 — 작은 ⌒
        ink = {(22, my + 1), (23, my), (24, my), (25, my + 1)}
    elif style == "omega":                  # 기본 ω
        ink = {(21, my + 1), (22, my + 1), (23, my), (24, my),
               (25, my + 1), (26, my + 1)}
    elif style == "teeth":                  # 기본 ω + 앞니
        ink = {(22, my), (23, my), (24, my), (25, my)}
        teeth = rect(23, my + 1, 25, my + 4)
    else:                                   # nose — Y 자 입선
        ink = {(24, my + 1), (23, my + 2), (22, my + 3), (21, my + 3),
               (24, my + 2), (25, my + 3), (26, my + 3)}
    if style == "teeth" and kind not in (1,):
        teeth = set()
    return ink, teeth


def face_layer(sp, col, kind=1, eye="mozzi", mouth=True):
    f = SK["face"]
    im = blank()
    ex, my = f["eye_x"], f["mouth_y"]

    # 볼 — 반함일 때만 하트, 나머지는 네모 볼터치
    if kind == 5:
        put(im, mirror(heart(13, f["cheek_y"] - 2)), CHEEK)
    else:
        put(im, mirror(rect(13, f["cheek_y"], 16, f["cheek_y"] + 2)), CHEEK)

    # 수염 — 고양이만. 볼 위에 2px 두 줄씩
    if sp.get("whisker"):
        put(im, mirror(rect(10, 37, 14, 38)) | mirror(rect(10, 40, 14, 41)),
            darker(col, 0.62, 1.1))

    eyes, extra = set(), set()
    if kind == 1:
        eyes = mirror(eye_shape(eye, ex))
    elif kind == 2:                                  # 웃음 — ∧ 감은 눈
        eyes = mirror({(ex - 1, 33), (ex, 32), (ex + 1, 32), (ex + 2, 33)})
    elif kind == 3:                                  # 시무룩 — 안쪽 끝이 올라간다
        eyes = mirror(rect(ex, 32, ex + 2, 35)) | mirror({(ex + 2, 31)})
    elif kind == 4:                                  # 놀람 — 동그랗게 커진 눈
        eyes = mirror(ellipse(18.0, 32.5, 2.4, 2.6))
    elif kind == 5:                                  # 반함 — 큰 눈 + 반짝
        eyes = mirror(ellipse(18.0, 32.5, 2.4, 2.8))
        extra = mirror({(17, 31), (17, 32)})
    else:                                            # 화남 — 눈 + 내린 눈썹
        eyes = mirror(rect(ex, 32, ex + 2, 35))
        extra = set()
        put(im, mirror({(ex - 1, 29), (ex, 29), (ex + 1, 30), (ex + 2, 30)}), INK)
    put(im, eyes, INK)
    if kind == 5:
        put(im, extra, SPARK)
    elif kind in (1, 4) and eye == "mozzi":
        put(im, mirror({(ex, 31)}), SPARK)           # 찹쌀이식 눈에는 반짝 한 점

    if sp["mouth"] == "nose" or sp.get("quill"):
        put(im, rect(23, my - 1, 25, my + 1), INK)   # 코 — 주둥이가 있는 종
    if mouth:
        ink, teeth = mouth_shape(sp["mouth"], my, kind)
        put(im, ink, INK)
        if teeth:
            put(im, teeth, "#FFF8F0")
            put(im, {(24, y) for (_, y) in teeth}, INK)    # 앞니 사이 금
            put(im, edge_of(teeth) - {(x, y) for (x, y) in teeth if y == my + 1}, INK)
    return im


# ======================================================================
# 한 장 그리기
# ======================================================================
def build(sp, col, kind=1, eye="mozzi", mouth=True, shadow=True):
    g = parts(sp)
    base, light, inner = col["body"], col["light"], col["inner"]
    quillc = darker(base, 0.80, 1.10)
    im = blank()
    if shadow:
        put_shadow(im, 9.5)

    regions = [
        (g["quill"], quillc, None),
        (g["tail"], light if sp.get("tail_light") else base,
         darker(light if sp.get("tail_light") else base)),
        (g["ears"], base, darker(base)),
        (g["blob"], base, darker(base)),
        (g["belly"], light, darker(light)),
        (g["muzzle"], light, darker(light)),
        (g["inner"], inner, None),
        (g["paws"], base, darker(base)),
        (g["feet"], darker(base), None),
    ]
    paint(im, [r for r in regions if r[0]])
    allpx = set()
    for px, _, _ in regions:
        allpx |= px

    # 이음선 — **앞발·발·배·주둥이 자신을 뺀** 몸에서 긋는다.
    # 안 빼면 그 덩어리가 통째로 검게 칠해진다 (v7 1판에서 앞발이 그랬다).
    plain = g["body"] - g["paws"] - g["feet"] - g["belly"]
    seam = touching(plain, g["paws"]) | touching(plain, g["feet"])
    seam |= touching(g["head"] - g["muzzle"], g["muzzle"])
    # 배 경계 — 흰 토끼는 몸색과 배색이 가까워 선이 없으면 안 보인다
    put(im, touching(g["body"] - g["belly"] - g["paws"] - g["feet"], g["belly"]),
        darker(base, 0.86, 1.05))
    put(im, seam, OUTLINE)
    # 다람쥐 꼬리는 몸과 같은 색이라 이음선을 그어야 갈린다
    if sp["key"] == "squirrel":
        put(im, touching(g["tail"], g["blob"]), darker(base, 0.70, 1.15))
        put(im, superellipse(38.5, 44.0, 3.5, 8.0, 2.2) & g["tail"], light)

    put(im, hilite_ring(g["head"]) - edge_of(g["head"]), lighter(base))
    put(im, edge_of(allpx), OUTLINE)
    im.alpha_composite(face_layer(sp, base, kind, eye, mouth))
    return im


def hilite_ring(px, n=3):
    return {(x, y) for (x, y) in px if (x - n, y - n) not in px}


# ======================================================================
# 미리보기
# ======================================================================
BG_DARK = (0x2B, 0x2B, 0x33, 255)
BG_GRASS = (0x8C, 0xB5, 0x6A, 255)
BG_CREAM = (0xF4, 0xEE, 0xE4, 255)
CELL_BG = (140, 115, 89, 255)


def font(sz):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", sz)
    except OSError:
        return None


def nearest(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def grid(items, rows, title, scale=4, cols=None):
    """items : [(이름, 그림)]. rows : [(줄이름, 바탕색)] — 같은 줄을 바탕만 바꿔 되풀이한다."""
    S, GAP, PAD, TOP, ROWGAP = scale, 10, 10, 24, 8
    cw, ch = W * S, H * S
    n = cols or len(items)
    im = Image.new("RGBA", (PAD * 2 + cw * n + GAP * (n - 1),
                            TOP + PAD + (ch + ROWGAP + 16) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(13), font(12)
    for i, (name, _) in enumerate(items):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 12), name, font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, bg) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP + 16)
        for i, (_, src) in enumerate(items):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=bg)
            im.alpha_composite(nearest(src, S), (x, y))
        if lf:
            d.text((PAD, y + ch + 3), f"{title} · {rname}", font=lf, fill=(255, 255, 255, 255))
    return im


THREE = [("어두운 UI 바탕", BG_DARK), ("풀밭 바탕", BG_GRASS), ("크림 바탕", BG_CREAM)]


def preview_species():
    items = [(sp["name"], build(sp, COLORS[sp["key"]][0])) for sp in SPECIES]
    return grid(items, THREE, "여섯 종")


def preview_colors():
    items = [(f"{sp['name']}·{c['name']}", build(sp, c))
             for sp in SPECIES for c in COLORS[sp["key"]]]
    S, GAP, PAD, TOP = 3, 8, 10, 22
    cw, ch = W * S, H * S
    per = 6
    rowsn = (len(items) + per - 1) // per
    im = Image.new("RGBA", (PAD * 2 + cw * per + GAP * (per - 1),
                            TOP + PAD + (ch + 20) * rowsn), BG_GRASS)
    d, f = ImageDraw.Draw(im), font(11)
    for i, (name, src) in enumerate(items):
        r, c = divmod(i, per)
        x, y = PAD + c * (cw + GAP), TOP + r * (ch + 20)
        im.alpha_composite(nearest(src, S), (x, y))
        if f:
            d.text((x + cw // 2, y + ch + 8), name, font=f, fill=(20, 20, 20, 255), anchor="mm")
    return im


def preview_mouth():
    """입 (a) 없음 · (b) 종마다 고른 모양 — 같은 종을 나란히 놓고 고른다."""
    items = []
    for sp in SPECIES:
        col = COLORS[sp["key"]][0]
        items.append((sp["name"] + " (a)없음", build(sp, col, mouth=False)))
        items.append((sp["name"] + " (b)모양", build(sp, col, mouth=True)))
    return grid(items, [("풀밭 바탕", BG_GRASS)], "입 안 비교", scale=3)


def preview_eyes():
    items = []
    for kind in EYE_KINDS:
        for sp in (SPECIES[0], SPECIES[2], SPECIES[3]):
            items.append((f"{sp['name']}·{EYE_LABEL[kind]}", build(sp, COLORS[sp["key"]][0],
                                                                  eye=kind)))
    return grid(items, [("풀밭 바탕", BG_GRASS)], "눈 후보 셋", scale=3)


def preview_faces(sp=None):
    sp = sp or SPECIES[2]
    col = COLORS[sp["key"]][0]
    items = [(FACE_LABEL[k], build(sp, col, kind=k)) for k in range(1, 7)]
    return grid(items, [("풀밭 바탕", BG_GRASS)], f"표정 여섯 ({sp['name']})", scale=4)


def mozzi_image():
    m = os.path.join(ART, "Mozzi")
    im = Image.open(os.path.join(m, "body_hop0.png")).convert("RGBA")
    im.alpha_composite(Image.open(os.path.join(m, "face_01.png")).convert("RGBA"))
    ch = Image.open(os.path.join(m, "cheek.png")).convert("RGBA")
    im.alpha_composite(ch, (14, 34))
    im.alpha_composite(ch.transpose(Image.FLIP_LEFT_RIGHT), (39, 34))
    return im


def cell_image(src):
    im = Image.new("RGBA", (CELL_W, CELL_H), CELL_BG)
    im.alpha_composite(src, ((CELL_W - W) // 2, CELL_H - H))
    return im


def _scale_row(scale):
    PAD, GAP = 8, 10
    items = [("찹쌀이", mozzi_image())]
    for sp in (SPECIES[0], SPECIES[1], SPECIES[3], SPECIES[5]):
        items.append((sp["name"], build(sp, COLORS[sp["key"]][0])))
    items.append(("가판대", Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")))
    items.append(("칸 68×80", cell_image(build(SPECIES[2], COLORS["cat"][0]))))
    bots = [im.getbbox()[3] for _, im in items]
    tops = [im.getbbox()[1] for _, im in items]
    tall = max(b - t for b, t in zip(bots, tops))
    cw = [im.width for _, im in items]
    row = Image.new("RGBA", (PAD * 2 + sum(cw) + GAP * (len(items) - 1), tall + PAD), BG_DARK)
    for i, (_, src) in enumerate(items):
        row.alpha_composite(src, (PAD + sum(cw[:i]) + GAP * i, tall - bots[i]))
    ImageDraw.Draw(row).rectangle([0, tall, row.width, row.height], fill=BG_GRASS)
    out = nearest(row, scale)
    d, f = ImageDraw.Draw(out), font(13)
    if f and scale >= 2:
        for i, (name, src) in enumerate(items):
            x = (PAD + sum(cw[:i]) + GAP * i + src.width // 2) * scale
            d.text((x, 9), f"{name} {bots[i] - tops[i]}px", font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    return out


def preview_scale():
    one, two = _scale_row(1), _scale_row(2)
    GAP = 12
    im = Image.new("RGBA", (max(one.width, two.width), one.height + GAP + two.height), BG_DARK)
    im.alpha_composite(one, (0, 0))
    im.alpha_composite(two, (0, one.height + GAP))
    return im


def _line_row(scale):
    """줄 선 손님 넷 — 가판대 앞에 조금씩 겹쳐 세운다. 겹쳐도 갈리는지 본다."""
    PAD, STEP = 6, 34
    picks = [(SPECIES[0], 0), (SPECIES[2], 0), (SPECIES[3], 1), (SPECIES[4], 0)]
    stall = Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")
    w = PAD * 2 + stall.width + 6 + STEP * (len(picks) - 1) + W
    h = max(stall.getbbox()[3], H) + 10
    row = Image.new("RGBA", (w, h), BG_DARK)
    ground = h - 6
    ImageDraw.Draw(row).rectangle([0, ground, w, h], fill=BG_GRASS)
    row.alpha_composite(stall, (PAD, ground - stall.getbbox()[3]))
    x0 = PAD + stall.width + 6
    for i, (sp, ci) in enumerate(picks):
        im = build(sp, COLORS[sp["key"]][ci])
        row.alpha_composite(im, (x0 + i * STEP, ground - GROUND_Y))
    return nearest(row, scale)


def preview_line():
    one, two = _line_row(1), _line_row(2)
    GAP = 10
    im = Image.new("RGBA", (max(one.width, two.width), one.height + GAP + two.height), BG_DARK)
    im.alpha_composite(one, (0, 0))
    im.alpha_composite(two, (0, one.height + GAP))
    return im


# ======================================================================
# 검산
# ======================================================================
def measure(im, label):
    bb = im.getbbox()
    cols = [c for c in im.getcolors(99999) if c[1][3] == 255]
    print(f"  {label:16s} bbox={bb} 키 {bb[3] - bb[1]} 폭 {bb[2] - bb[0]} "
          f"색 {len(cols)}가지 알파 {sorted({c[1][3] for c in im.getcolors(99999)})}")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")   # 콘솔이 cp949 라 우리말 로그가 깨진다
    except Exception:
        pass
    os.makedirs(OUT, exist_ok=True)

    print("낱장 — 종 6 × 색 2")
    for sp in SPECIES:
        for ci, col in enumerate(COLORS[sp["key"]], start=1):
            build(sp, col).save(os.path.join(OUT, f"{sp['key']}_{ci}_{col['name']}.png"))
    print("표정 낱장 — 고양이")
    cat = SPECIES[2]
    for k in range(1, 7):
        build(cat, COLORS["cat"][0], kind=k).save(os.path.join(OUT, f"face_0{k}.png"))

    print("실측")
    for sp in SPECIES:
        measure(build(sp, COLORS[sp["key"]][0], shadow=False), sp["name"])

    print("미리보기")
    for name, fn in (("preview_species", preview_species), ("preview_colors", preview_colors),
                     ("preview_mouth", preview_mouth), ("preview_eyes", preview_eyes),
                     ("preview_faces", preview_faces), ("preview_scale", preview_scale),
                     ("preview_line", preview_line)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

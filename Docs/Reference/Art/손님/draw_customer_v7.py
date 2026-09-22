"""가판대 손님 시안 7판 — **두 벌**. 흰 통짜 사람 꼴을 버렸다.

정본 : `Docs/Research/2026-09-22-손님그림레퍼런스조사4-새방향.md` 의 **안 가**(벌 A)와 **안 다**(벌 B).
사용자 판정 (2026-09-22) : v6 는 「너무 기괴한데」로 물렀다.
  「카페 운영 게임류에서 손님을 참고해줘. 주로 2디나 픽셀류. 카이로소프트게임도 참고하면 좋을 것 같아」
  「굳이 흰색 통짜 사람모양 고집안할께 레퍼런스들 조사한거 바탕으로 자유롭게 그려봐줘」

**베끼지 않았다.** 레퍼런스에서 가져온 것은 **수치와 화풍 규칙**뿐이다.

v1~v6 이 막힌 자리와 이번에 푸는 법 (조사4 2절) :
  · 「맨몸이라 기괴하다」 → **살색을 얼굴에만 쓰고 나머지는 옷으로 덮는다.**
  · 「외곽선이 속 빈 껍질을 만든다」 → 옷 색이 진하고 외곽선이 **거의 검정**이라 속이 차 보인다.
  · 「가는 팔이 고무관이 된다」  → **팔을 몸에 붙이고** 가는 팔에는 명암을 안 넣는다.
  · 「얼굴이 크다 / 머리가 작아 어른 같다」 → **얼굴은 14px 로 작게, 머리 덩어리는 머리카락으로 24px 로.**

벌 A — 카이로식 SD 사람 (주력). 벌 B — 동물 손님 (안전판).
산출은 이 파일 옆 v7/a · v7/b 에 떨어진다. v6 이하 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import colorsys
import os
import sys

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v7")
OUT_A, OUT_B = os.path.join(OUT, "a"), os.path.join(OUT, "b")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 48, 64
CX = 24.0                 # 좌우 대칭축은 픽셀 23 과 24 의 경계다 (mirror : x → 47-x)
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

# ---------- 공용 색 ----------
OUTLINE = "#12110F"       # **거의 검정 1px** — 다만 순검정이 아니라 **찹쌀이 눈과 같은 먹색**이다.
                          # 레퍼런스는 #212421 쯤을 쓰지만 우리는 이미 쓰는 먹색으로 맞춰 한 식구로 보이게 했다.
INK = "#12110F"           # 눈 — 찹쌀이 face 와 같은 색
SKIN = "#FFDDB0"          # 살색은 **얼굴과 손에만** 쓴다
MOUTH = "#C4566A"
CHEEK = "#F49CA8"
SHADOW_A = 56             # 바닥 그림자 알파 — 건물 그림(stall)과 같은 값


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def darker(hexcol, vm=0.78, sm=1.15):
    """그늘 한 단 — HSV 로 **명도를 내리고 채도를 조금 올린다.** 회색을 섞으면 탁해진다.
    재질(살·옷·머리카락)마다 제 색에서 뽑으므로 「재질마다 2단」이 된다 (조사4 3절)."""
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    rr, gg, bb = colorsys.hsv_to_rgb(h, min(1.0, s * sm), v * vm)
    return "#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255))


def lighter(hexcol, vm=1.12, sm=0.80):
    """하이라이트 한 점 — 떡·재료 아이콘 화풍(「하이라이트 점」)에 맞추려고 왼쪽 위에 찍는다.
    레퍼런스에는 없는 자리다. 우리 그림과 한 식구로 보이게 하는 값싼 수단이다."""
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, s_, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    rr, gg, bb = colorsys.hsv_to_rgb(h, max(0.0, s_ * sm), min(1.0, v * vm))
    return "#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255))


def hilite(px, n=2):
    """덩어리 안에서 **왼쪽 위** 대각선 한 겹 — 빛이 왼쪽 위라 여기가 가장 밝다."""
    return {(x, y) for (x, y) in px if (x - n, y - n) not in px}


# ---------- 팔레트 ----------
HAIR_COLORS = [("검정갈", "#4A3327"), ("갈색", "#8A5A3B"),
               ("금발", "#E0B85E"), ("붉은", "#B4573C")]
SHIRT_COLORS = [("분홍", "#E8798F"), ("하늘", "#5FA8D3"), ("연두", "#7FB069"),
                ("노랑", "#E8C15F"), ("보라", "#9B7EC8")]
PANTS = "#5E6B80"     # 신발(#3A3330)과 갈리게 v7 1판보다 한 단 밝다
SHOES = "#3A3330"

# 손님 넷 — **몸 뼈대는 한 장을 같이 쓰고 머리 모양·머리색·옷 색만 다르다** (조사4 3절 읽은 것 5)
CUSTOMERS_A = [
    dict(name="단발·갈색·분홍", hair="bob", hair_col="#8A5A3B", shirt="#E8798F", cheek=True),
    dict(name="짧은머리·검정갈·하늘", hair="short", hair_col="#4A3327", shirt="#5FA8D3"),
    dict(name="모자·금발·노랑", hair="cap", hair_col="#E0B85E", shirt="#E8C15F", cap="#7FB069"),
    dict(name="단발·붉은·보라", hair="bob", hair_col="#B4573C", shirt="#9B7EC8", cheek=True),
]

# 동물 넷 — 귀 모양과 몸 색만 다르고 **몸 뼈대는 같다**
CUSTOMERS_B = [
    dict(name="토끼·흰", ear="rabbit", body="#F2EDE4", inner="#F4B8C4"),
    dict(name="곰·갈색", ear="bear", body="#C89A6B", inner="#E8C9A6"),
    dict(name="고양이·회색", ear="cat", body="#B9C0CA", inner="#F4B8C4"),
    dict(name="고양이·노랑", ear="cat", body="#E8C98A", inner="#F4B8C4"),
]


# ---------- 픽셀 집합 도우미 ----------
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
    """n 이 커질수록 모서리가 둥근 **네모**가 된다. 몸통을 네모 덩어리로 두는 데 쓴다 —
    조사4 3절 : 허리를 좁히면 마네킹이 된다."""
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if abs((x + 0.5 - cx) / rx) ** n + abs((y + 0.5 - cy) / ry) ** n <= 1.0:
                out.add((x, y))
    return out


def tri(x0, y0, w, h, flip=False):
    """삼각 귀 — 밑변 w, 높이 h. flip 이면 바깥쪽으로 기운다."""
    out = set()
    for i in range(h):
        half = max(1, int(round(w * (h - i) / (2.0 * h))))
        c = x0 + w / 2.0 + (i * 0.0 if not flip else 0.0)
        out |= {(x, y0 + i) for x in range(int(c - half), int(c + half))}
    return out


def mirror(px):
    return px | {(W - 1 - x, y) for (x, y) in px}


def edge_of(px):
    """실루엣 **안쪽** 한 겹. 바깥으로 넓히지 않아 캔버스를 안 넘는다."""
    return {(x, y) for (x, y) in px
            if not all(n in px for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def touching(a, b):
    """a 안에서 b 와 맞닿은 칸 — 덩어리 사이 이음선(seam)을 여기에 긋는다."""
    return {(x, y) for (x, y) in a
            if any(n in b for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, px, col):
    p, c = im.load(), rgb(col)
    for (x, y) in px:
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = c


def paint(im, regions):
    """regions : [(픽셀집합, 속색, 그늘색 또는 None)]. 그늘은 오른쪽·아래 대각선 한 겹이다.
    **가는 팔에는 그늘을 안 준다** (None) — 굵기의 3분의 1이 그늘이면 고무관이 된다 (조사4 2절)."""
    for px, base, shade in regions:
        put(im, px, base)
        if shade:
            put(im, {(x, y) for (x, y) in px if (x + 1, y + 1) not in px}, shade)


# ======================================================================
# 벌 A — 카이로식 SD 사람
# ======================================================================
# 세로 자리 : 머리 y20~39 (20) · 몸통 y39~56 (17) · 다리 y56~64 (8) = 키 44
A = dict(
    head=dict(cy=30.0, ry=10.0, n=2.6, top=20, bot=40),
    head_rx=dict(bob=12.0, short=10.5, cap=10.5),        # 머리 모양이 머리 덩어리 폭을 정한다
    face=dict(cy=33.0, rx=7.0, ry=6.0, n=2.6),           # 얼굴(살색) 14×12 — 나머지는 머리카락이 덮는다
    torso=dict(cy=47.5, rx=13.0, ry=8.5, n=3.5, y0=39, y1=56),   # 폭 26 · 허리를 안 좁힌다
    arm_x=15,                                            # x11~14 가 팔 (폭 4)
    hand=(12, 51, 15, 54),
    leg=(13, 56, 22, 64),                                # 폭 9 · 틈 4 · 아래 3px 는 신발
    shoe_y=61,
    face_px=dict(eye_x=19, eye_y=29, eye_w=2, eye_h=4, mouth_y=35, cheek_y=33),
)


def a_parts(cust):
    """벌 A 의 덩어리들. 머리 모양만 다르고 **몸 뼈대는 넷이 같다.**"""
    style = cust["hair"]
    head = superellipse(CX, A["head"]["cy"], A["head_rx"][style], A["head"]["ry"],
                        A["head"]["n"], A["head"]["top"], A["head"]["bot"])
    f = A["face"]
    face = superellipse(CX, f["cy"], f["rx"], f["ry"], f["n"])
    face &= head
    t = A["torso"]
    torso = superellipse(CX, t["cy"], t["rx"], t["ry"], t["n"], t["y0"], t["y1"])
    arms = {(x, y) for (x, y) in torso if x < A["arm_x"] or x >= W - A["arm_x"]}
    torso -= arms
    hands = mirror(rect(*A["hand"]))
    legs = mirror(rect(*A["leg"]))
    legs -= {(A["leg"][0], GROUND_Y - 1), (W - 1 - A["leg"][0], GROUND_Y - 1)}
    shoes = {(x, y) for (x, y) in legs if y >= A["shoe_y"]}
    legs -= shoes

    cap = set()
    hair = head - face
    if style == "cap":
        # 모자 : 챙 + 위통. 머리 꼭대기를 덮어 머리카락은 앞머리만 남는다
        cap = mirror(rect(11, 21, 24, 24)) | superellipse(CX, 19.0, 8.5, 5.0, 2.6, 15, 22)
        hair -= cap
    return dict(head=head, face=face, hair=hair, cap=cap, torso=torso,
                arms=arms, hands=hands, legs=legs, shoes=shoes)


def a_face_layer(kind, cheek=False):
    """표정 — 눈 2×4 세로 막대 · 입 2×2 점. 볼은 넣는 손님만 (조사4 안 가)."""
    p = A["face_px"]
    im = blank()
    if cheek:
        put(im, mirror(rect(17, p["cheek_y"], 20, p["cheek_y"] + 2)), CHEEK)
    eyes, mouth = set(), set()
    my = p["mouth_y"]
    if kind == 1:                      # 기본
        eyes = mirror(rect(p["eye_x"], p["eye_y"], p["eye_x"] + p["eye_w"],
                           p["eye_y"] + p["eye_h"]))
        mouth = rect(23, my, 25, my + 2)
    elif kind == 2:                    # 웃음 — 눈은 ∧ 활, 입은 ∪ 활
        ex, ey = p["eye_x"] - 1, p["eye_y"] + 1
        eyes = mirror({(ex, ey + 1), (ex + 1, ey), (ex + 2, ey), (ex + 3, ey + 1)})
        mouth = {(22, my), (23, my + 1), (24, my + 1), (25, my)}
    else:                              # 시무룩 — 눈 안쪽 끝이 올라가고 입은 ∩ 활
        eyes = mirror(rect(p["eye_x"], p["eye_y"] + 1, p["eye_x"] + p["eye_w"],
                           p["eye_y"] + p["eye_h"]))
        eyes |= mirror({(p["eye_x"] + p["eye_w"], p["eye_y"])})
        mouth = {(22, my + 1), (23, my), (24, my), (25, my + 1)}
    put(im, eyes, INK)
    put(im, mouth, MOUTH)
    return im


def build_a(cust, face=1, shadow=True):
    g = a_parts(cust)
    im = blank()
    if shadow:
        put_shadow(im, 10.0)
    regions = [
        (g["legs"], PANTS, darker(PANTS)),
        (g["shoes"], SHOES, None),
        (g["torso"], cust["shirt"], darker(cust["shirt"])),
        (g["arms"], cust["shirt"], None),        # 가는 팔에는 그늘을 안 넣는다
        (g["hands"], SKIN, None),
        (g["hair"], cust["hair_col"], darker(cust["hair_col"])),
        (g["face"], SKIN, darker(SKIN, 0.90, 1.10)),
    ]
    if g["cap"]:
        regions.append((g["cap"], cust["cap"], darker(cust["cap"])))
    paint(im, regions)

    allpx = set()
    for px, _, _ in regions:
        allpx |= px
    # 이음선 : 팔과 몸통 사이 · 머리와 몸통 사이 · 몸통과 다리 사이
    seam = touching(g["torso"], g["arms"])
    seam |= touching(g["torso"], g["head"] | g["hair"] | g["face"])
    seam |= touching(g["torso"] | g["arms"], g["legs"])
    put(im, seam, OUTLINE)
    # 하이라이트 : 머리카락·옷의 왼쪽 위 한 겹 (떡·재료 아이콘과 같은 화풍)
    put(im, hilite(g["hair"], 3) - edge_of(g["hair"]), lighter(cust["hair_col"]))
    put(im, hilite(g["torso"], 3) - edge_of(g["torso"]), lighter(cust["shirt"]))
    put(im, edge_of(allpx), OUTLINE)          # 실루엣 안쪽 1px — 먹색
    im.alpha_composite(a_face_layer(face, cust.get("cheek", False)))
    return im


# ======================================================================
# 벌 B — 동물 손님
# ======================================================================
# 세로 자리 : 귀 y18~30 · 머리 y24~44 · 몸 y44~64 = 키 40 (귀 포함 46)
B = dict(
    # 머리와 몸이 **겹쳐야** 한 덩어리가 된다. 1판에서 y44 한 줄이 비어 머리가 떠 보였다.
    head=dict(cy=34.0, rx=13.0, ry=11.0, n=2.5, y0=23, y1=45),
    body=dict(cy=54.0, rx=12.0, ry=11.0, n=3.0, y0=43, y1=64),
    # 앞발은 **몸 밖으로 안 내민다.** 내밀면 3px 짜리 돌기가 통째로 외곽선이 돼 검은 손잡이처럼 보였다.
    # 몸 안쪽에 타원을 두고 이음선 1px 만 그어 「몸에 붙인 앞발」로 읽게 한다.
    paw=(15.0, 51.0, 4.0, 5.5),
    foot=(18.0, 62.0, 4.5, 2.5),     # 바닥에 붙은 어두운 발
    face_px=dict(eye_x=17, eye_y=32, eye_w=2, eye_h=4, mouth_y=38, cheek_y=36),
)


def b_ears(kind):
    if kind == "rabbit":
        return mirror(ellipse(16.0, 24.0, 3.5, 7.5))
    if kind == "bear":
        return mirror(ellipse(14.0, 27.0, 4.5, 4.5))
    return mirror(tri(11, 20, 9, 9))          # 고양이 — 삼각 귀


def b_inner_ears(kind):
    if kind == "rabbit":
        return mirror(ellipse(16.0, 24.5, 1.5, 5.0))
    if kind == "bear":
        return mirror(ellipse(14.0, 27.0, 2.0, 2.0))
    return mirror(tri(13, 23, 5, 5))


def b_parts(cust):
    h, b = B["head"], B["body"]
    head = superellipse(CX, h["cy"], h["rx"], h["ry"], h["n"], h["y0"], h["y1"])
    body = superellipse(CX, b["cy"], b["rx"], b["ry"], b["n"], b["y0"], b["y1"])
    ears = b_ears(cust["ear"]) - head
    inner = b_inner_ears(cust["ear"]) - head
    paws = mirror(ellipse(*B["paw"])) & body
    feet = mirror(ellipse(*B["foot"])) & body
    return dict(head=head, body=body, ears=ears, inner=inner, paws=paws, feet=feet)


def b_face_layer(kind, col):
    p = B["face_px"]
    im = blank()
    put(im, mirror(rect(15, p["cheek_y"], 18, p["cheek_y"] + 2)), CHEEK)
    eyes, mouth = set(), set()
    my = p["mouth_y"]
    if kind == 1:
        eyes = mirror(rect(p["eye_x"], p["eye_y"], p["eye_x"] + p["eye_w"],
                           p["eye_y"] + p["eye_h"]))
        mouth = {(23, my + 1), (24, my + 1)}
    elif kind == 2:
        ex, ey = p["eye_x"] - 1, p["eye_y"] + 1
        eyes = mirror({(ex, ey + 1), (ex + 1, ey), (ex + 2, ey), (ex + 3, ey + 1)})
        mouth = {(22, my + 1), (23, my + 2), (24, my + 2), (25, my + 1)}
    else:
        eyes = mirror(rect(p["eye_x"], p["eye_y"] + 1, p["eye_x"] + p["eye_w"],
                           p["eye_y"] + p["eye_h"]))
        eyes |= mirror({(p["eye_x"] + p["eye_w"], p["eye_y"])})
        mouth = {(22, my + 2), (23, my + 1), (24, my + 1), (25, my + 2)}
    put(im, eyes, INK)
    put(im, rect(23, my - 1, 25, my + 1), OUTLINE)     # 코
    put(im, mouth, OUTLINE)
    return im


def build_b(cust, face=1, shadow=True):
    g = b_parts(cust)
    col = cust["body"]
    im = blank()
    if shadow:
        put_shadow(im, 9.0)
    regions = [
        (g["ears"], col, darker(col)),
        (g["head"] | g["body"], col, darker(col)),
        (g["paws"], col, darker(col)),
        (g["inner"], cust["inner"], None),
        (g["feet"], darker(col), None),
    ]
    paint(im, regions)
    allpx = set()
    for px, _, _ in regions:
        allpx |= px
    # 이음선은 **앞발·발 자신을 뺀** 몸에서 긋는다. 안 빼면 앞발이 통째로 검게 칠해진다 (1판 버그).
    plain = g["body"] - g["paws"] - g["feet"]
    put(im, touching(plain, g["paws"]) | touching(plain, g["feet"]), OUTLINE)
    put(im, hilite(g["head"], 3) - edge_of(g["head"]), lighter(col))
    put(im, edge_of(allpx), OUTLINE)
    im.alpha_composite(b_face_layer(face, col))
    return im


# ---------- 바닥 그림자 ----------
def put_shadow(im, rx):
    p = im.load()
    for (x, y) in ellipse(CX, GROUND_Y - 2, rx, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)


# ---------- 미리보기 도우미 ----------
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


def preview_row(customers, builder, title):
    """손님 넷을 한 줄로 세우고 바탕 셋에 깐다 — **줄을 세워야 갈리는지 보인다** (조사4 8절 5)."""
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 24, 8
    rows = [("어두운 UI 바탕", BG_DARK), ("풀밭 바탕", BG_GRASS), ("크림 바탕", BG_CREAM)]
    cw, ch = W * S, H * S
    n = len(customers)
    im = Image.new("RGBA", (PAD * 2 + cw * n + GAP * (n - 1),
                            TOP + PAD + (ch + ROWGAP + 16) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(13), font(12)
    for i, c in enumerate(customers):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 12), c["name"], font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, bg) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP + 16)
        for i, c in enumerate(customers):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=bg)
            im.alpha_composite(nearest(builder(c, 1), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 3), f"{title} · {rname}", font=lf, fill=(255, 255, 255, 255))
    return im


def preview_faces():
    S, GAP, PAD, TOP, ROWGAP = 5, 12, 10, 24, 24
    names = ["기본", "웃음", "시무룩"]
    cw, ch = W * S, H * S
    rows = [("벌 A 카이로식 사람", lambda k: build_a(CUSTOMERS_A[0], k)),
            ("벌 B 동물 손님", lambda k: build_b(CUSTOMERS_B[0], k))]
    im = Image.new("RGBA", (PAD * 2 + cw * 3 + GAP * 2,
                            TOP + PAD + (ch + ROWGAP) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(14), font(12)
    for i, name in enumerate(names):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 12), name, font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, fn) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP)
        for i in range(3):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=BG_GRASS)
            im.alpha_composite(nearest(fn(i + 1), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 4), rname, font=lf, fill=(255, 255, 255, 255))
    return im


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
    items = [("찹쌀이", mozzi_image()),
             ("A 손님", build_a(CUSTOMERS_A[0])), ("A 손님", build_a(CUSTOMERS_A[2])),
             ("B 손님", build_b(CUSTOMERS_B[0])), ("B 손님", build_b(CUSTOMERS_B[1])),
             ("가판대", Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")),
             ("칸 68×80", cell_image(build_a(CUSTOMERS_A[1])))]
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


# ---------- 검산 ----------
def measure(im, label):
    p = im.load()

    def wide(y):
        xs = [x for x in range(W) if p[x, y][3]]
        return (max(xs) - min(xs) + 1) if xs else 0

    bb = im.getbbox()
    cols = [c for c in im.getcolors(99999) if c[1][3] == 255]
    print(f"  {label:18s} bbox={bb} 키 {bb[3] - bb[1]} 폭 {bb[2] - bb[0]} "
          f"색 {len(cols)}가지 알파 {sorted({c[1][3] for c in im.getcolors(99999)})}")
    print(f"     줄폭 y30 {wide(30)} y45 {wide(45)} y52 {wide(52)} y60 {wide(60)}")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")   # 콘솔이 cp949 라 우리말 로그가 깨진다
    except Exception:
        pass
    for d in (OUT, OUT_A, OUT_B):
        os.makedirs(d, exist_ok=True)

    print("벌 A — 겹 낱장")
    base = CUSTOMERS_A[0]
    g = a_parts(base)
    for name, px, col in (("layer_body", g["torso"] | g["arms"] | g["legs"] | g["shoes"], None),
                          ("layer_hair_bob", a_parts(CUSTOMERS_A[0])["hair"], "#8A5A3B"),
                          ("layer_hair_short", a_parts(CUSTOMERS_A[1])["hair"], "#4A3327"),
                          ("layer_hair_cap", a_parts(CUSTOMERS_A[2])["hair"]
                           | a_parts(CUSTOMERS_A[2])["cap"], "#E0B85E")):
        im = blank()
        if col:
            paint(im, [(px, col, darker(col))])
        else:
            paint(im, [(g["legs"], PANTS, darker(PANTS)), (g["shoes"], SHOES, None),
                       (g["torso"], base["shirt"], darker(base["shirt"])),
                       (g["arms"], base["shirt"], None), (g["hands"], SKIN, None)])
        put(im, edge_of(px), OUTLINE)
        im.save(os.path.join(OUT_A, name + ".png"))
        print(f"  {name:18s} bbox={im.getbbox()}")
    for k in (1, 2, 3):
        a_face_layer(k, True).save(os.path.join(OUT_A, f"face_0{k}.png"))
    for i, c in enumerate(CUSTOMERS_A, start=1):
        build_a(c).save(os.path.join(OUT_A, f"full_{i}_{c['name']}.png"))

    print("벌 B — 낱장")
    for k in (1, 2, 3):
        b_face_layer(k, "#F2EDE4").save(os.path.join(OUT_B, f"face_0{k}.png"))
    for i, c in enumerate(CUSTOMERS_B, start=1):
        build_b(c).save(os.path.join(OUT_B, f"full_{i}_{c['name']}.png"))

    print("실측")
    for c in CUSTOMERS_A:
        measure(build_a(c, shadow=False), "A " + c["name"])
    for c in CUSTOMERS_B:
        measure(build_b(c, shadow=False), "B " + c["name"])

    print("미리보기")
    preview_row(CUSTOMERS_A, build_a, "벌 A").save(os.path.join(OUT, "preview_a.png"))
    preview_row(CUSTOMERS_B, build_b, "벌 B").save(os.path.join(OUT, "preview_b.png"))
    preview_faces().save(os.path.join(OUT, "preview_faces.png"))
    preview_scale().save(os.path.join(OUT, "preview_scale.png"))
    for n in ("preview_a", "preview_b", "preview_faces", "preview_scale"):
        print("  " + n + ".png")


if __name__ == "__main__":
    main()

"""가판대 손님 시안 9판 — **왼쪽 3/4 방향**. 고슴도치를 빼고 다섯 종.

사용자 판정 (2026-09-22) :
  「손님이 왼쪽 바라본 방향이면 좋을것 같고, 완전 왼쪽을 바라보기보다
   음.. 손님이 오른쪽 입구에서 왼쪽으로 들어온다는 느낌으로?」
  「고슴도치는 일단 빼야겠고」 「손이 다 어색해」

v8 에서 바꾼 것 :
  1. **정면 → 왼쪽 3/4.** 화면 오른쪽 입구에서 왼쪽 가판대로 걸어 들어오는 손님이다.
     · 얼굴 요소를 얼굴 가운데(x24)보다 **왼쪽(x19.5)** 으로 몰았다.
     · **먼 눈(왼쪽)은 작고 실루엣 가장자리 쪽**, 가까운 눈(오른쪽)은 크다. 눈 둘 다 보인다 — 옆모습이 아니다.
     · 먼 귀(왼쪽)는 작고 한 줄 뒤(위)로, 가까운 귀(오른쪽)는 크다.
     · 머리를 몸보다 2px 왼쪽에 두어 **몸이 왼쪽으로 기운다.**
     · 꼬리는 오른쪽(뒤)으로 나온다 — 진행 방향의 반대다.
     시점은 `Docs/Design/06-아트규칙.md` 2번 「낮은 탑다운(3/4)」·빛은 왼쪽 위를 그대로 지킨다.
  2. **고슴도치 제외.** 토끼·곰·고양이·강아지·다람쥐 다섯 종 × 색 2벌.
     다람쥐는 **귀를 키우고 꼬리를 더 세워** 햄스터와 갈리게 했다.
  3. **손(앞발) 세 안.** v8 의 「몸 앞 가운데 모은 앞발 + 밝은 배」가 한 덩어리로 커서
     앞치마·기저귀처럼 보였다. (a)없음 · (b)몸 옆 작은 앞발 · (c)왼쪽 한 손만 — 셋을 그려 비교한다.
  4. 기본 표정의 ω 입이 뾰로통해 보여 **1px 내리고 작게** 했다. 표정 여섯은 그대로.

지킨 것 : 색 10~14가지 · 둥근 모서리 · 한 단 명암 · 왼쪽 위 하이라이트 · 먹색(#12110F) 외곽선.
뼈대(머리·몸 덩어리)는 다섯 종이 같은 한 장이다. 귀·꼬리·무늬·색만 바꾼다.

산출은 이 파일 옆 v9/ 에 떨어진다. v8 이하 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import colorsys
import os
import sys

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v9")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

W, H = 48, 64
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

HX = 22.0        # 머리 가로 복판 — 몸(24.5)보다 **2.5px 왼쪽**이라 몸이 왼쪽으로 기운다
BX = 24.5        # 몸 가로 복판
FX = 19.5        # 얼굴 복판(코 선) — 머리 복판보다 또 왼쪽이다. 3/4 회전의 핵심 값

OUTLINE = "#12110F"
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
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    rr, gg, bb = colorsys.hsv_to_rgb(h, min(1.0, s * sm), v * vm)
    return "#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255))


def lighter(hexcol, vm=1.12, sm=0.80):
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
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if abs((x + 0.5 - cx) / rx) ** n + abs((y + 0.5 - cy) / ry) ** n <= 1.0:
                out.add((x, y))
    return out


def tri_up(cx, ybase, w, h):
    """위로 뾰족한 삼각 — 고양이 귀."""
    out = set()
    for i in range(h):
        half = w / 2.0 * (h - i) / float(h)
        y = ybase - i
        out |= {(x, y) for x in range(int(round(cx - half)), int(round(cx + half)))}
    return out


def curve(pts, r, step=0.5):
    """점 사이를 잘게 쪼개 동그라미를 끊김 없이 잇는다 — 꼬리.
    v8 1판에서 3px 간격으로 찍었다가 꼬리가 토막으로 끊겼다."""
    out = set()
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(1, int(((x1 - x0) ** 2 + (y1 - y0) ** 2) ** 0.5 / step))
        for i in range(n + 1):
            t = i / float(n)
            out |= ellipse(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r, r)
    return out


def edge_of(px):
    return {(x, y) for (x, y) in px
            if not all(n in px for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def touching(a, b):
    return {(x, y) for (x, y) in a
            if any(n in b for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


def hilite_ring(px, n=3):
    return {(x, y) for (x, y) in px if (x - n, y - n) not in px}


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, px, col):
    p, c = im.load(), rgb(col)
    for (x, y) in px:
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = c


def paint(im, regions):
    for px, base, shade in regions:
        put(im, px, base)
        if shade:
            put(im, {(x, y) for (x, y) in px if (x + 1, y + 1) not in px}, shade)


def put_shadow(im, rx=9.5):
    p = im.load()
    for (x, y) in ellipse(BX - 1.0, GROUND_Y - 2, rx, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)


# ======================================================================
# 뼈대 — 다섯 종이 같이 쓴다
# ======================================================================
SK = dict(
    head=dict(cy=34.0, rx=13.0, ry=11.0, n=2.5, y0=23, y1=45),
    body=dict(cy=54.0, rx=12.0, ry=11.0, n=3.0, y0=43, y1=64),
    belly=(FX + 1.0, 55.5, 7.0, 7.5),      # 배 타원도 **왼쪽으로** 따라간다
    muzzle=(FX, 38.5, 6.0, 4.0),
    foot_far=(17.5, 62.0, 4.0, 2.5),       # 먼 발 — 조금 작다
    foot_near=(28.0, 62.0, 4.5, 2.5),
    paw_side_far=(12.0, 53.0, 3.2, 3.6),   # (b) 안 : 몸 **바깥선 옆**에 붙는 작은 앞발
    paw_one=(17.5, 56.0, 3.2, 3.4),        # (c) 안 : 왼쪽 한 손만 몸 앞에
)

FACE = dict(
    eye_far_x=13, eye_far_y=32, eye_far_w=2, eye_far_h=3,     # 먼 눈 — 작고 가장자리 쪽
    eye_near_x=22, eye_near_y=31, eye_near_w=2, eye_near_h=4,  # 가까운 눈 — 크다
    mouth_y=39,                                                # v8 보다 1px 내렸다
    cheek_far=(9, 37), cheek_near=(26, 36),
)


# ---------- 귀 : (먼 귀, 가까운 귀) 를 따로 만든다 ----------
def ears_of(kind):
    """3/4 라 좌우가 다르다. **왼쪽(먼) 귀는 작고 한 줄 뒤(위)**, 오른쪽(가까운) 귀는 크다."""
    if kind == "rabbit":
        out = ellipse(13.0, 21.5, 3.0, 8.0) | ellipse(30.5, 20.0, 3.6, 9.2)
        inn = ellipse(13.0, 22.0, 1.2, 5.2) | ellipse(30.5, 20.5, 1.6, 6.2)
        return out, inn
    if kind == "bear":
        out = ellipse(11.5, 27.0, 3.8, 3.8) | ellipse(31.0, 26.0, 4.7, 4.7)
        inn = ellipse(11.5, 27.0, 1.6, 1.6) | ellipse(31.0, 26.0, 2.1, 2.1)
        return out, inn
    if kind == "cat":
        out = tri_up(12.0, 26, 8, 8) | tri_up(31.5, 25, 10, 10)
        inn = tri_up(12.0, 25, 4, 4) | tri_up(31.5, 24, 5, 6)
        return out, inn
    if kind == "dog":
        out = ellipse(9.5, 35.0, 3.8, 8.0) | ellipse(33.5, 34.5, 4.6, 9.2)
        inn = ellipse(9.5, 35.0, 1.6, 4.6) | ellipse(33.5, 34.5, 2.1, 5.6)
        return out, inn
    if kind == "squirrel":
        # 다람쥐 귀 — v8 보다 **키우고 세웠다.** 안 그러면 햄스터로 읽힌다
        out = ellipse(12.5, 24.0, 3.4, 5.4) | ellipse(31.0, 23.0, 4.2, 6.4)
        inn = ellipse(12.5, 24.5, 1.4, 2.8) | ellipse(31.0, 23.5, 1.8, 3.4)
        return out, inn
    return set(), set()


# ---------- 꼬리 : 오른쪽(뒤) ----------
def tail_of(kind):
    if kind == "rabbit":
        return ellipse(37.5, 57.0, 4.0, 4.0)
    if kind == "cat":
        return curve([(35.5, 59.0), (38.5, 56.5), (40.5, 53.0), (42.0, 49.0),
                      (42.5, 45.0), (41.0, 42.0)], 2.2)
    if kind == "dog":
        return curve([(35.5, 56.0), (38.0, 53.0), (39.5, 49.5), (39.5, 46.5)], 2.4)
    if kind == "squirrel":
        # **더 세웠다** — 몸보다 높이 솟아 다람쥐로 읽히게
        return superellipse(39.5, 40.0, 6.5, 13.0, 2.2)
    return set()


SPECIES = [
    dict(key="rabbit", name="토끼", belly=True, muzzle=False, mouth="teeth",
         tail_light=True, note="긴 귀 + 앞니 + 솜꼬리"),
    dict(key="bear", name="곰", belly=False, muzzle=True, mouth="nose",
         note="둥근 귀 + 밝은 주둥이"),
    dict(key="cat", name="고양이", belly=True, muzzle=False, mouth="omega",
         whisker=True, note="세모 귀 + 수염 + 휜 꼬리"),
    dict(key="dog", name="강아지", belly=True, muzzle=True, mouth="nose",
         note="늘어진 귀 + 든 꼬리"),
    dict(key="squirrel", name="다람쥐", belly=True, muzzle=False, mouth="omega",
         note="큰 귀 + 높이 세운 북슬 꼬리"),
]

COLORS = {
    "rabbit": [dict(name="흰", body="#F2EDE4", light="#FFFFFA", inner="#F4B8C4"),
               dict(name="회갈", body="#CDBBA8", light="#EEE3D4", inner="#F4B8C4")],
    "bear": [dict(name="갈색", body="#C89A6B", light="#E8C9A6", inner="#E8C9A6"),
             dict(name="진갈", body="#9E7551", light="#D3AF89", inner="#D3AF89")],
    "cat": [dict(name="회색", body="#B9C0CA", light="#E2E6EC", inner="#F4B8C4"),
            dict(name="노랑", body="#E8C98A", light="#F7E8C4", inner="#F4B8C4")],
    "dog": [dict(name="크림", body="#E0BC8C", light="#F6E4C8", inner="#F4B8C4"),
            dict(name="갈흰", body="#B98A5E", light="#EAD9BE", inner="#F4B8C4")],
    "squirrel": [dict(name="주황갈", body="#D08C4C", light="#F2D6A8", inner="#F4B8C4"),
                 dict(name="회다람", body="#B0A290", light="#E8E0D2", inner="#F4B8C4")],
}

HAND_LABEL = {"a": "(a)손 없음", "b": "(b)옆 앞발", "c": "(c)왼손 하나"}


def parts(sp, hand="a"):
    h, b = SK["head"], SK["body"]
    head = superellipse(HX, h["cy"], h["rx"], h["ry"], h["n"], h["y0"], h["y1"])
    body = superellipse(BX, b["cy"], b["rx"], b["ry"], b["n"], b["y0"], b["y1"])
    blob = head | body
    ear, inner = ears_of(sp["key"])
    ear, inner = ear - blob, inner - blob
    tail = tail_of(sp["key"]) - blob
    belly = (ellipse(*SK["belly"]) & body) if sp["belly"] else set()
    muzzle = (ellipse(*SK["muzzle"]) & head) if sp["muzzle"] else set()
    feet = (ellipse(*SK["foot_far"]) | ellipse(*SK["foot_near"])) & body

    paws_in, paws_out = set(), set()
    if hand == "b":
        # 몸 **바깥선 옆**에 작은 앞발. 3/4 라 먼(왼쪽) 앞발만 몸 밖에 보이고
        # 가까운(오른쪽) 앞발은 몸에 가려 안쪽 이음선만 남는다
        paws_out = ellipse(*SK["paw_side_far"]) - blob - tail
        # 가까운(오른쪽) 앞발은 그리지 않는다 — 몸에 가린다.
        # 1판에서 몸 안에 타원을 두었더니 오른쪽 가장자리에 갈고리 같은 이음선이 생겼다.
    elif hand == "c":
        paws_in = ellipse(*SK["paw_one"]) & body

    belly -= feet | paws_in
    return dict(head=head, body=body, blob=blob, ears=ear, inner=inner, tail=tail,
                belly=belly, muzzle=muzzle, feet=feet,
                paws_in=paws_in, paws_out=paws_out)


# ======================================================================
# 얼굴 — 3/4 라 좌우가 다르다
# ======================================================================
FACE_LABEL = {1: "기본", 2: "웃음", 3: "시무룩", 4: "놀람", 5: "반함", 6: "화남"}


def heart(x0, y0):
    out = {(x, y0) for x in (x0, x0 + 1, x0 + 3, x0 + 4)}
    out |= {(x0 + i, y0 + 1) for i in range(5)}
    out |= {(x0 + i, y0 + 2) for i in range(1, 4)}
    out.add((x0 + 2, y0 + 3))
    return out


def mouth_shape(style, my, kind):
    """입 — 점 하나는 안 쓴다. **얼굴 복판이 FX(19.5) 라 입도 왼쪽에 있다.**
    기본 ω 는 v8 보다 1px 내리고 한 칸 좁혔다 — 뾰로통해 보였다."""
    L = int(FX) - 2                     # 입 왼쪽 끝
    ink, teeth = set(), set()
    if kind in (2, 5):                                  # 웃음·반함 — ‿
        ink = {(L, my), (L + 1, my + 1), (L + 2, my + 2), (L + 3, my + 2),
               (L + 4, my + 1), (L + 5, my)}
    elif kind == 3:                                     # 시무룩 — ⌒
        ink = {(L + 1, my + 2), (L + 2, my + 1), (L + 3, my + 1), (L + 4, my + 2)}
    elif kind == 4:                                     # 놀람 — o
        ink = ellipse(L + 2.5, my + 1.5, 2.0, 2.2) - ellipse(L + 2.5, my + 1.5, 1.0, 1.2)
    elif kind == 6:                                     # 화남 — 작은 ⌒
        ink = {(L + 1, my + 1), (L + 2, my), (L + 3, my), (L + 4, my + 1)}
    elif style == "omega":
        # 기본 ω — **지그재그 다섯 칸.** 1판에서 ⌒ 모양으로 썼더니 화남과 똑같아 뾰로통해 보였다
        ink = {(L + 1, my), (L + 2, my + 1), (L + 3, my), (L + 4, my + 1), (L + 5, my)}
    elif style == "teeth":                              # 기본 — 짧은 입 + 앞니
        ink = {(L + 1, my), (L + 2, my), (L + 3, my)}
        teeth = rect(L + 2, my + 1, L + 4, my + 4)
    else:                                               # nose — Y 자 입선
        ink = {(L + 2, my + 1), (L + 1, my + 2), (L, my + 3),
               (L + 2, my + 2), (L + 3, my + 3), (L + 4, my + 3)}
    return ink, teeth


def face_layer(sp, col, kind=1, mouth=True):
    f = FACE
    im = blank()
    fx, nx, my = f["eye_far_x"], f["eye_near_x"], f["mouth_y"]

    # 볼 — 먼 볼은 작고, 가까운 볼은 크다
    if kind == 5:
        put(im, heart(f["cheek_far"][0], f["cheek_far"][1] - 1)
            | heart(f["cheek_near"][0], f["cheek_near"][1] - 1), CHEEK)
    else:
        cf, cn = f["cheek_far"], f["cheek_near"]
        put(im, rect(cf[0], cf[1], cf[0] + 2, cf[1] + 2)
            | rect(cn[0], cn[1], cn[0] + 3, cn[1] + 2), CHEEK)

    if sp.get("whisker"):      # 수염 — 앞쪽(왼쪽)으로만 뻗는다. 3/4 라 뒤쪽은 안 보인다
        put(im, rect(8, 38, 12, 39) | rect(8, 41, 12, 42), darker(col, 0.62, 1.1))

    eyes, spark = set(), set()
    if kind == 1:
        eyes = rect(fx, f["eye_far_y"], fx + f["eye_far_w"], f["eye_far_y"] + f["eye_far_h"])
        eyes |= rect(nx, f["eye_near_y"], nx + f["eye_near_w"],
                     f["eye_near_y"] + f["eye_near_h"])
        spark = {(fx, f["eye_far_y"]), (nx, f["eye_near_y"])}
    elif kind == 2:                                   # 웃음 — ∧ 감은 눈
        eyes = {(fx - 1, 34), (fx, 33), (fx + 1, 33), (fx + 2, 34)}
        eyes |= {(nx - 1, 33), (nx, 32), (nx + 1, 32), (nx + 2, 33)}
    elif kind == 3:                                   # 시무룩
        eyes = rect(fx, 33, fx + 2, 35) | {(fx + 2, 32)}
        eyes |= rect(nx, 32, nx + 2, 35) | {(nx + 2, 31)}
    elif kind == 4:                                   # 놀람
        eyes = ellipse(fx + 0.5, 33.0, 2.0, 2.2) | ellipse(nx + 1.0, 32.5, 2.4, 2.6)
    elif kind == 5:                                   # 반함
        eyes = ellipse(fx + 0.5, 33.0, 2.0, 2.4) | ellipse(nx + 1.0, 32.5, 2.4, 2.8)
        spark = {(fx - 1, 32), (fx - 1, 33), (nx, 31), (nx, 32)}
    else:                                             # 화남 — 내린 눈썹
        eyes = rect(fx, 33, fx + 2, 35) | rect(nx, 32, nx + 2, 35)
        put(im, {(fx - 1, 30), (fx, 30), (fx + 1, 31),
                 (nx - 1, 29), (nx, 29), (nx + 1, 30), (nx + 2, 30)}, INK)
    put(im, eyes, INK)
    put(im, spark, SPARK)

    if sp["mouth"] == "nose":
        put(im, rect(int(FX) - 1, my - 2, int(FX) + 1, my), INK)     # 코
    if mouth:
        ink, teeth = mouth_shape(sp["mouth"], my, kind)
        put(im, ink, INK)
        if teeth and kind == 1:
            put(im, teeth, "#FFF8F0")
            put(im, edge_of(teeth) - {(x, y) for (x, y) in teeth if y == my + 1}, INK)
            put(im, {(int(FX), y) for (_, y) in teeth}, INK)          # 앞니 사이 금
    return im


# ======================================================================
# 한 장 그리기
# ======================================================================
def build(sp, col, kind=1, hand="a", mouth=True, shadow=True):
    g = parts(sp, hand)
    base, light, inner = col["body"], col["light"], col["inner"]
    im = blank()
    if shadow:
        put_shadow(im)

    regions = [
        (g["tail"], light if sp.get("tail_light") else base,
         darker(light if sp.get("tail_light") else base)),
        (g["ears"], base, darker(base)),
        (g["blob"], base, darker(base)),
        (g["paws_out"], darker(base, 0.90, 1.05), darker(base)),
        (g["belly"], light, darker(light)),
        (g["muzzle"], light, darker(light)),
        (g["inner"], inner, None),
        (g["paws_in"], base, darker(base)),
        (g["feet"], darker(base), None),
    ]
    paint(im, [r for r in regions if r[0]])
    allpx = set()
    for px, _, _ in regions:
        allpx |= px

    # 이음선은 **그 덩어리 자신을 뺀** 몸에서 긋는다.
    # 안 빼면 앞발·발이 통째로 검게 칠해진다 (v7 에서 겪은 버그).
    plain = g["body"] - g["feet"] - g["belly"] - g["paws_in"]
    seam = touching(plain, g["feet"]) | touching(plain, g["paws_in"])
    seam |= touching(g["head"] - g["muzzle"], g["muzzle"])
    put(im, seam, OUTLINE)
    put(im, touching(g["body"] - g["belly"] - g["feet"] - g["paws_in"], g["belly"]),
        darker(base, 0.86, 1.05))          # 배 경계 — 흰 토끼는 선이 없으면 안 보인다
    if sp["key"] == "squirrel":
        put(im, touching(g["tail"], g["blob"]), darker(base, 0.70, 1.15))
        put(im, superellipse(39.5, 40.0, 3.4, 8.5, 2.2) & g["tail"], light)

    put(im, hilite_ring(g["head"]) - edge_of(g["head"]), lighter(base))
    put(im, edge_of(allpx), OUTLINE)
    im.alpha_composite(face_layer(sp, base, kind, mouth))
    return im


# ======================================================================
# 미리보기
# ======================================================================
BG_DARK = (0x2B, 0x2B, 0x33, 255)
BG_GRASS = (0x8C, 0xB5, 0x6A, 255)
BG_CREAM = (0xF4, 0xEE, 0xE4, 255)
CELL_BG = (140, 115, 89, 255)
THREE = [("어두운 UI 바탕", BG_DARK), ("풀밭 바탕", BG_GRASS), ("크림 바탕", BG_CREAM)]


def font(sz):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", sz)
    except OSError:
        return None


def nearest(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def grid(items, rows, title, scale=4):
    S, GAP, PAD, TOP, ROWGAP = scale, 10, 10, 24, 8
    cw, ch = W * S, H * S
    n = len(items)
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


def preview_species():
    return grid([(sp["name"], build(sp, COLORS[sp["key"]][0])) for sp in SPECIES],
                THREE, "다섯 종 · 왼쪽 3/4")


def preview_colors():
    items = [(f"{sp['name']}·{c['name']}", build(sp, c))
             for sp in SPECIES for c in COLORS[sp["key"]]]
    S, GAP, PAD, TOP, per = 3, 8, 10, 22, 5
    cw, ch = W * S, H * S
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


def preview_hands():
    """손 세 안 × 다섯 종 — 줄마다 한 안이다."""
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 24, 22
    cw, ch = W * S, H * S
    n = len(SPECIES)
    im = Image.new("RGBA", (PAD * 2 + cw * n + GAP * (n - 1),
                            TOP + PAD + (ch + ROWGAP) * 3), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(13), font(13)
    for i, sp in enumerate(SPECIES):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 12), sp["name"], font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, hand in enumerate(("a", "b", "c")):
        y = TOP + r * (ch + ROWGAP)
        for i, sp in enumerate(SPECIES):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=BG_GRASS)
            im.alpha_composite(nearest(build(sp, COLORS[sp["key"]][0], hand=hand), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 4), HAND_LABEL[hand], font=lf, fill=(255, 255, 255, 255))
    return im


def preview_faces(sp=None):
    sp = sp or SPECIES[2]
    col = COLORS[sp["key"]][0]
    return grid([(FACE_LABEL[k], build(sp, col, kind=k)) for k in range(1, 7)],
                [("풀밭 바탕", BG_GRASS)], f"표정 여섯 ({sp['name']}) · 왼쪽 3/4")


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
    for sp in SPECIES:
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
    """**가판대는 왼쪽**, 손님은 그 오른쪽에 줄을 서서 왼쪽을 본다 — 오른쪽 입구에서 들어온 꼴."""
    PAD, STEP = 6, 42   # 칸 너비(68) 보다 좁게 붙여도 꼬리가 옆 손님을 안 덮게
    picks = [(SPECIES[0], 0), (SPECIES[2], 0), (SPECIES[3], 0), (SPECIES[4], 0)]
    stall = Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")
    w = PAD * 2 + stall.width + 4 + STEP * (len(picks) - 1) + W
    h = max(stall.getbbox()[3], H) + 10
    row = Image.new("RGBA", (w, h), BG_DARK)
    ground = h - 6
    ImageDraw.Draw(row).rectangle([0, ground, w, h], fill=BG_GRASS)
    row.alpha_composite(stall, (PAD, ground - stall.getbbox()[3]))
    x0 = PAD + stall.width + 4
    for i, (sp, ci) in enumerate(picks):
        row.alpha_composite(build(sp, COLORS[sp["key"]][ci]), (x0 + i * STEP, ground - GROUND_Y))
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
    print(f"  {label:14s} bbox={bb} 키 {bb[3] - bb[1]} 폭 {bb[2] - bb[0]} "
          f"색 {len(cols)}가지 알파 {sorted({c[1][3] for c in im.getcolors(99999)})}")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    os.makedirs(OUT, exist_ok=True)

    print("낱장 — 종 5 × 색 2")
    for sp in SPECIES:
        for ci, col in enumerate(COLORS[sp["key"]], start=1):
            build(sp, col).save(os.path.join(OUT, f"{sp['key']}_{ci}_{col['name']}.png"))
    print("표정 낱장 — 고양이")
    for k in range(1, 7):
        build(SPECIES[2], COLORS["cat"][0], kind=k).save(os.path.join(OUT, f"face_0{k}.png"))

    print("실측")
    for sp in SPECIES:
        measure(build(sp, COLORS[sp["key"]][0], shadow=False), sp["name"])

    print("미리보기")
    for name, fn in (("preview_species", preview_species), ("preview_colors", preview_colors),
                     ("preview_hands", preview_hands), ("preview_faces", preview_faces),
                     ("preview_scale", preview_scale), ("preview_line", preview_line)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

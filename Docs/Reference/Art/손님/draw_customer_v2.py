"""가판대 손님 시안 2판 — **흰 사람 덩어리 + 표정 + 볼터치** (v1 의 헤어·옷·살색은 물러났다).

정본 : Docs/Research/2026-09-21-손님그림레퍼런스조사2-흰사람.md
  - 안 A (권함) : 2등신 콩몸 — 키 56 · 머리 28x26 · 어깨 22 / 허리 17 / 엉덩이 20 · 팔 3x14 · 다리 5x14 · 목 없음
  - 안 B (비교) : 2.5등신 물방울몸 — 머리 22x22 · 18 / 15 / 18 · 팔 3x15 · 다리 5x18 · 목 2px
핵심 : **몸통을 네모로 쌓지 않는다.** 줄(y)마다 폭을 보간해 어깨→허리→엉덩이로 흐르게 한다 (v1 이 틀린 자리).
색   : 찹쌀이 몸(body_hop0)의 흰색 + 회색 4단을 그대로 베낀다. 외곽선 없음 · 빛 왼쪽 위 · 알파 0/255.

산출은 이 파일 옆 v2/ 폴더에 떨어진다. v1 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v2")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

# ---------- 캔버스 (v1 에서 살린 값) ----------
W, H = 48, 64
CX = 24.0               # 48 폭의 진짜 한가운데는 픽셀 23 과 24 의 경계다. 좌우 짝은 (23,24)(22,25)…
GROUND_Y = 64           # 발바닥이 닿는 바닥선
CELL_W, CELL_H = 68, 80

# ---------- 찹쌀이 몸에서 베낀 흰색 5단 ----------
# 0 이 몸통 속(거의 흰색), 4 가 가장 어두운 가장자리. 빛이 왼쪽 위라 오른쪽·아래에 쌓인다.
WHITE5 = ["#FEFDFC", "#E0DFDD", "#C7C6C4", "#B6B5B4", "#A6A5A2"]
INK = "#12110F"                       # 눈·입 — 찹쌀이 face 와 같은 먹색
CHEEK_PINK = ["#F6A7AE", "#FBC7CC"]   # 분홍판 (속 · 왼위 밝은 쪽)
CHEEK_YELLOW = ["#FBD701", "#FDE367"]  # 노랑판 — 찹쌀이 cheek.png 실측색
SHADOW_A = 56                          # 바닥 그림자 알파 — 건물 그림(stall)과 같은 값

# ---------- 비율안 ----------
# 8절 7번 : 안이 바뀌면 **이 표만** 고쳐 다시 돌린다.
SPEC_A = dict(
    name="A",
    head=dict(cx=CX, cy=21.0, rx=14.0, ry=13.0, n=2.5, top=8, bot=34),
    # (y, 반폭) — 사이는 코사인으로 보간한다. 반폭 11 = 폭 22
    # 어깨 반폭을 11(=22) 이 아니라 10(=20) 으로 둔다 — 팔 3px 두 개가 붙으면 바깥 실루엣이 26 이라
    # 머리 28 보다 확실히 좁아진다. 표의 22 를 그대로 쓰면 팔까지 28 이라 머리와 같아진다.
    torso=[(28, 4.5), (36, 10.0), (43, 8.0), (48, 10.0), (52, 7.0)],
    # gap=0 : 어깨에서는 몸통에 **붙인다**. 띄우면 팔이 아니라 막대 두 개로 보인다 (1판에서 봤다).
    # 허리가 들어가면서 아래쪽에 틈이 저절로 생긴다 — 그게 자연스러운 팔이다.
    arm=dict(w=3, gap=0, y0=36, y1=49),
    leg=dict(w=5, gap=2, y0=50, y1=GROUND_Y),
    face=dict(eye_x=18, eye_y=20, eye_w=2, eye_h=4, mouth_y=26),
    cheek=dict(x=13, y=21, r=2.5),
)
SPEC_B = dict(
    name="B",
    head=dict(cx=CX, cy=19.0, rx=11.0, ry=11.0, n=2.5, top=8, bot=30),
    torso=[(28, 3.5), (33, 9.0), (39, 7.5), (44, 9.0), (48, 6.0)],   # 목 2px (y30~32)
    arm=dict(w=3, gap=0, y0=33, y1=48),
    leg=dict(w=5, gap=2, y0=46, y1=GROUND_Y),
    face=dict(eye_x=19, eye_y=17, eye_w=2, eye_h=4, mouth_y=22),
    cheek=dict(x=15, y=18, r=2.0),
)


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 픽셀 집합 도우미 (v1 에서 가져왔다) ----------
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def ellipse(cx, cy, rx, ry):
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def superellipse(cx, cy, rx, ry, n, y0=None, y1=None):
    """|dx/rx|^n + |dy/ry|^n <= 1. n 이 2 면 타원, 커질수록 모서리가 둥근 네모가 된다.
    머리를 순 타원으로 두면 턱이 뾰족해 「말랑한 덩어리」로 안 읽혀서 n=2.5 를 쓴다."""
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if abs((x + 0.5 - cx) / rx) ** n + abs((y + 0.5 - cy) / ry) ** n <= 1.0:
                out.add((x, y))
    return out


def cosine_at(keys, y):
    """(y, 반폭) 표를 코사인으로 부드럽게 잇는다. 선형으로 이으면 꺾인 자리가 각으로 보인다."""
    import math
    if y <= keys[0][0]:
        return keys[0][1]
    if y >= keys[-1][0]:
        return keys[-1][1]
    for (y0, w0), (y1, w1) in zip(keys, keys[1:]):
        if y0 <= y <= y1:
            t = (y - y0) / (y1 - y0)
            t = (1 - math.cos(t * math.pi)) / 2
            return w0 + (w1 - w0) * t
    return keys[-1][1]


def band(keys):
    """폭 표로 몸통을 그린다 — **줄마다 폭이 바뀌는 덩어리**. 네모를 쌓는 것이 아니다."""
    out = set()
    for y in range(int(keys[0][0]), int(keys[-1][0]) + 1):
        hw = cosine_at(keys, y + 0.5)
        x0, x1 = int(round(CX - hw)), int(round(CX + hw))
        out |= {(x, y) for x in range(x0, x1)}
    return out


def capsule(x0, y0, w, h):
    """끝이 둥근 막대 — 팔다리용. 휴먼 폴 플랫의 「끝이 뭉툭한 팔다리」를 이걸로 낸다."""
    r = w / 2.0
    cx = x0 + r
    body = rect(x0, y0 + int(r), x0 + w, y0 + h - int(r))
    return body | ellipse(cx, y0 + r, r, r) | ellipse(cx, y0 + h - r, r, r)


def mirror(px):
    """왼쪽 덩어리를 오른쪽으로 뒤집어 붙인다 (x → 47-x)."""
    return px | {(W - 1 - x, y) for (x, y) in px}


# ---------- 명암 ----------
def shade_rings(px, rings):
    """가장자리에서 안쪽으로 색을 한 겹씩 밝게 쌓는다.
    (x+k, y+k) 가 덩어리 밖이면 k 번째 겹 = 오른쪽·아래 가장자리만 어두워진다 → 빛이 왼쪽 위.
    rings 는 바깥에서 안으로 쓸 WHITE5 번호다. 팔처럼 얇은 것은 겹을 2개만 준다."""
    out = {}
    for (x, y) in px:
        idx = 0
        for k, ring in enumerate(rings, start=1):
            if (x + k, y + k) not in px:
                idx = ring
                break
        out[(x, y)] = WHITE5[idx]
    return out


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, colored):
    p = im.load()
    for (x, y), col in colored.items():
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = rgb(col)


# ---------- 겹 1 : 몸 ----------
def layer_body(spec):
    """머리·몸통·팔·다리를 따로 칠한다. 한 덩어리로 묶으면 머리와 어깨 사이 경계가 안 생긴다."""
    im = blank()
    h = spec["head"]
    head = superellipse(h["cx"], h["cy"], h["rx"], h["ry"], h["n"], h["top"], h["bot"])
    torso = band(spec["torso"])

    a = spec["arm"]
    # 팔은 몸통 가장 넓은 줄(어깨)에서 gap 만큼 띄운 자리에 건다
    shoulder_hw = max(w for _, w in spec["torso"])
    ax1 = int(round(CX - shoulder_hw)) - a["gap"]        # 팔의 오른쪽 끝(몸통 쪽)
    arms = mirror(capsule(ax1 - a["w"], a["y0"], a["w"], a["y1"] - a["y0"]))

    g = spec["leg"]
    lx1 = int(CX - g["gap"] / 2)                          # 왼다리의 오른쪽 끝
    legs = mirror(rect(lx1 - g["w"], g["y0"], lx1, g["y1"]))
    legs -= {(lx1 - g["w"], g["y1"] - 1), (W - lx1 + g["w"] - 1, g["y1"] - 1)}   # 발 바깥 모서리 1px 둥글게

    # 다리·팔은 얇아서 겹을 2개만 준다 — 4개 주면 통째로 회색이 된다
    put(im, shade_rings(legs, [4, 2]))
    put(im, shade_rings(torso, [4, 3, 2, 1]))
    put(im, shade_rings(arms, [4, 2]))
    put(im, shade_rings(head, [4, 3, 2, 1]))
    return im


# ---------- 겹 2 : 표정 ----------
def _eyes_tall(f, dy=0):
    """기본 눈 — 세로로 긴 네모 두 개 (찹쌀이 face_01 화풍)."""
    return mirror(rect(f["eye_x"], f["eye_y"] + dy, f["eye_x"] + f["eye_w"],
                       f["eye_y"] + dy + f["eye_h"]))


def layer_face(spec, kind):
    f = spec["face"]
    im = blank()
    px = set()
    if kind == 1:                       # 기본 — 세로 긴 눈 + 짧은 입
        px |= _eyes_tall(f)
        px |= rect(int(CX) - 1, f["mouth_y"], int(CX) + 1, f["mouth_y"] + 1)
    elif kind == 2:                     # 웃음 — 눈은 ∧ 활, 입은 ∪ 활
        ex, ey = f["eye_x"] - 1, f["eye_y"] + 1
        px |= mirror({(ex, ey + 1), (ex + 1, ey), (ex + 2, ey), (ex + 3, ey + 1)})
        my = f["mouth_y"]
        px |= mirror({(int(CX) - 3, my - 1), (int(CX) - 2, my), (int(CX) - 1, my)})
    else:                               # 놀람 — 눈은 큰 네모, 입은 작은 ○
        px |= mirror(rect(f["eye_x"] - 1, f["eye_y"], f["eye_x"] + f["eye_w"],
                          f["eye_y"] + f["eye_h"] - 1))
        my = f["mouth_y"]
        px |= mirror(rect(int(CX) - 2, my - 1, int(CX), my + 2))
        px -= {(int(CX) - 1, my), (int(CX), my)}
    put(im, {p: INK for p in px})
    return im


# ---------- 겹 3 : 볼 ----------
def layer_cheek(spec, cols):
    c = spec["cheek"]
    im = blank()
    one = ellipse(c["x"] + c["r"], c["y"] + c["r"], c["r"], c["r"])
    px = mirror(one)
    put(im, {p: cols[0] for p in px})
    # 왼쪽 위 한 줄만 밝게 — 동그랗게 보인다
    top = min(y for _, y in px)
    put(im, {(x, y): cols[1] for (x, y) in px if y == top})
    return im


# ---------- 겹 4 : 바닥 그림자 ----------
def layer_shadow(spec):
    """발밑 납작한 타원. 흰 몸이 밝은 바닥에 묻히는 것을 막는 가장 싼 수단이다 (조사 4-(5)).
    알파 56 은 건물 그림자와 같은 값이라 화면에서 한 벌로 붙는다."""
    im = blank()
    p = im.load()
    for (x, y) in ellipse(CX, GROUND_Y - 2, 9.5, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)
    return im


# ---------- 겹치기 ----------
def build(spec, face=1, cheek=CHEEK_PINK, shadow=True, tint_col=None):
    im = blank()
    if shadow:
        im.alpha_composite(layer_shadow(spec))
    body = layer_body(spec)
    if tint_col:
        body = tint(body, tint_col)
    im.alpha_composite(body)
    im.alpha_composite(layer_face(spec, face))
    im.alpha_composite(layer_cheek(spec, cheek))
    return im


def tint(im, hexcol):
    """몸 픽셀에 RGB 를 곱한다 — 게임 코드가 할 일을 미리 흉내 낸 것."""
    r0, g0, b0, _ = rgb(hexcol)
    out = im.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if a:
                p[x, y] = (r * r0 // 255, g * g0 // 255, b * b0 // 255, a)
    return out


# ---------- 미리보기 도우미 (v1 에서 가져왔다) ----------
BG_DARK = (0x2B, 0x2B, 0x33, 255)
BG_GRASS = (0x8C, 0xB5, 0x6A, 255)
BG_CREAM = (0xF4, 0xEE, 0xE4, 255)
CELL_BG = (140, 115, 89, 255)          # Color(0.55,0.45,0.35) 쯤의 갈색


def checker(w, h, size=8):
    im = Image.new("RGBA", (w, h), (0x70, 0x70, 0x78, 255))
    d = ImageDraw.Draw(im)
    for y in range(0, h, size):
        for x in range(0, w, size):
            if (x // size + y // size) % 2:
                d.rectangle([x, y, x + size - 1, y + size - 1], fill=(0x5A, 0x5A, 0x62, 255))
    return im


def font(sz):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", sz)
    except OSError:
        return None


def nearest(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


# ---------- 미리보기 1 : 겹 낱장 ----------
def preview_layers():
    S, GAP, PAD, TOP = 6, 10, 10, 20
    items = [("몸 A", layer_body(SPEC_A)), ("face_01", layer_face(SPEC_A, 1)),
             ("cheek", layer_cheek(SPEC_A, CHEEK_PINK)), ("shadow", layer_shadow(SPEC_A)),
             ("합침", build(SPEC_A))]
    cw = W * S
    im = Image.new("RGBA", (PAD * 2 + cw * len(items) + GAP * (len(items) - 1),
                            TOP + PAD + H * S), BG_DARK)
    d, f = ImageDraw.Draw(im), font(13)
    for i, (name, src) in enumerate(items):
        x = PAD + i * (cw + GAP)
        im.alpha_composite(checker(cw, H * S), (x, TOP))
        im.alpha_composite(nearest(src, S), (x, TOP))
        if f:
            d.text((x + cw // 2, 10), name, font=f, fill=(255, 255, 255, 255), anchor="mm")
    return im


# ---------- 미리보기 2 : 안 A 대 안 B, 바탕 세 가지 ----------
def preview_ab():
    S, GAP, PAD, TOP, ROWGAP = 6, 10, 10, 20, 8
    cols = [("A 그림자X", SPEC_A, False), ("A 그림자O", SPEC_A, True),
            ("B 그림자X", SPEC_B, False), ("B 그림자O", SPEC_B, True)]
    rows = [("어두운 바탕", BG_DARK), ("풀색 바탕", BG_GRASS), ("크림 바탕", BG_CREAM)]
    cw, ch = W * S, H * S
    im = Image.new("RGBA", (PAD * 2 + cw * len(cols) + GAP * (len(cols) - 1),
                            TOP + PAD + (ch + ROWGAP + 14) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(13), font(12)
    for i, (name, _, _) in enumerate(cols):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 10), name, font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, bg) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP + 14)
        for i, (_, spec, shadow) in enumerate(cols):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=bg)
            im.alpha_composite(nearest(build(spec, shadow=shadow), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 2), rname, font=lf, fill=(255, 255, 255, 255))
    return im


# ---------- 미리보기 3 : 손님 다섯 ----------
# 곱하기라 원래 색보다 어두워진다 → 파스텔로 보이려면 원색보다 훨씬 옅은 값을 넣는다
TINTS = [("#FFFFFF", "순백"), ("#FBDDE0", "분홍"), ("#DCEBF3", "하늘"),
         ("#DEEBC9", "풀"), ("#F3E8D6", "크림")]
MIX = [(1, CHEEK_PINK), (2, CHEEK_PINK), (1, CHEEK_YELLOW), (2, CHEEK_YELLOW), (3, CHEEK_PINK)]


def preview_variants():
    S, GAP, PAD, TOP, ROWGAP = 6, 10, 10, 20, 26
    cw, ch = W * S, H * S
    im = Image.new("RGBA", (PAD * 2 + cw * 5 + GAP * 4, TOP + PAD + ch * 2 + ROWGAP), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(13), font(12)
    for i in range(5):
        col, cname = TINTS[i]
        face, cheek = MIX[i]
        x = PAD + i * (cw + GAP)
        im.alpha_composite(checker(cw, ch), (x, TOP))
        im.alpha_composite(nearest(build(SPEC_A, face, cheek, tint_col=col), S), (x, TOP))
        if f:
            cn = "분홍볼" if cheek is CHEEK_PINK else "노랑볼"
            d.text((x + cw // 2, 10), f"{cname}·표정{face}·{cn}", font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
        y2 = TOP + ch + ROWGAP
        im.alpha_composite(checker(cw, ch), (x, y2))
        im.alpha_composite(nearest(build(SPEC_A, (i % 3) + 1, CHEEK_PINK), S), (x, y2))
    if lf:
        d.text((PAD, TOP + ch + 6), "위 : 몸 색 곱하기 + 표정 + 볼색 섞음   "
                                    "아래 : 색 곱하기 없는 순백 (표정만 다름)",
               font=lf, fill=(255, 255, 255, 255))
    return im


# ---------- 미리보기 4 : 키 비교 ----------
def mozzi_image():
    """찹쌀이 원본 세 장을 그대로 겹친다. 볼 자리는 mem 20260920-688f3a26 : L x14 · R x39 · y34."""
    m = os.path.join(ART, "Mozzi")
    im = Image.open(os.path.join(m, "body_hop0.png")).convert("RGBA")
    im.alpha_composite(Image.open(os.path.join(m, "face_01.png")).convert("RGBA"))
    ch = Image.open(os.path.join(m, "cheek.png")).convert("RGBA")
    im.alpha_composite(ch, (14, 34))
    im.alpha_composite(ch.transpose(Image.FLIP_LEFT_RIGHT), (39, 34))
    return im


def preview_scale():
    S, PAD, GAP, BAND = 4, 8, 12, 18
    items = [("찹쌀이", mozzi_image()), ("손님 A", build(SPEC_A)),
             ("손님 B", build(SPEC_B)),
             ("가판대", Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA"))]
    bots = [im.getbbox()[3] for _, im in items]
    tops = [im.getbbox()[1] for _, im in items]
    tall = max(b - t for b, t in zip(bots, tops))
    cw = [im.width for _, im in items]
    im = Image.new("RGBA", (PAD * 2 + sum(cw) + GAP * 3, tall + BAND + PAD), BG_DARK)
    base = BAND + tall
    for i, (_, src) in enumerate(items):
        im.alpha_composite(src, (PAD + sum(cw[:i]) + GAP * i, base - bots[i]))
    ImageDraw.Draw(im).rectangle([0, base, im.width, im.height], fill=BG_GRASS)
    out = nearest(im, S)
    d, f = ImageDraw.Draw(out), font(14)
    if f:
        for i, (name, src) in enumerate(items):
            x = (PAD + sum(cw[:i]) + GAP * i + src.width // 2) * S
            d.text((x, 10), f"{name} {bots[i] - tops[i]}px", font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    return out


# ---------- 미리보기 5 : 손님 칸 ----------
# (가) 떡을 머리 위 가운데에 — 칸 80 은 빠듯하고 88 이면 넉넉하다
# 칸 80 은 머리 꼭대기(칸 y24)와 떡 밑(y22) 사이가 2px 뿐이다 — 붙어 보여 「떡을 인」 꼴이 된다.
CAKE_TOP_80 = (18, 0)
CAKE_TOP_88 = (18, 4)
# (나) 머리 오른쪽 위 말풍선 안에 떡 — 틀 사각형과 꼬리, 떡 자리
BUBBLE_RECT = (30, 0, 66, 26)
BUBBLE_TAIL = [(34, 24), (44, 24), (34, 33)]
CAKE_IN_BUBBLE = (32, -1)


def cell_top(cake, cell_h, cake_at, shadow=True):
    im = Image.new("RGBA", (CELL_W, cell_h), CELL_BG)
    im.alpha_composite(build(SPEC_A, shadow=shadow), ((CELL_W - W) // 2, cell_h - H))
    im.alpha_composite(cake, cake_at)
    return im


def cell_bubble(cake, cell_h=CELL_H):
    im = Image.new("RGBA", (CELL_W, cell_h), CELL_BG)
    im.alpha_composite(build(SPEC_A), ((CELL_W - W) // 2, cell_h - H))
    # 말풍선도 흰색이라 흰 손님 머리 위에서 경계가 사라진다.
    # 1px 밀어 회색 틀을 먼저 깔아 테두리처럼 보이게 한다 (외곽선이 아니라 그늘이다).
    bub = Image.new("RGBA", (CELL_W, cell_h), (0, 0, 0, 0))
    d = ImageDraw.Draw(bub)
    x0, y0, x1, y1 = BUBBLE_RECT
    for dx, dy, col in ((1, 1, (0xB6, 0xB5, 0xB4, 255)), (0, 0, (0xFE, 0xFD, 0xFC, 255))):
        d.rounded_rectangle([x0 + dx, y0 + dy, x1 - 1 + dx, y1 - 1 + dy], radius=4, fill=col)
        d.polygon([(px + dx, py + dy) for px, py in BUBBLE_TAIL], fill=col)
    im.alpha_composite(bub)
    im.alpha_composite(cake, CAKE_IN_BUBBLE)
    return im


def preview_cell():
    S, PAD, GAP = 4, 8, 4
    cake = Image.open(os.path.join(ART, "Cake", "redbean_cake.png")).convert("RGBA")
    tiles = [("가-1 칸 80", cell_top(cake, 80, CAKE_TOP_80)),
             ("가-2 칸 88", cell_top(cake, 88, CAKE_TOP_88)),
             ("나 말풍선", cell_bubble(cake))]
    row = Image.new("RGBA", (360, CELL_H), (0x1A, 0x1A, 0x20, 255))
    for i in range(5):
        row.alpha_composite(tiles[0][1], (i * (CELL_W + GAP), 0))
    top_h = max(t.height for _, t in tiles) * S
    im = Image.new("RGBA", (PAD * 2 + max(sum(t.width for _, t in tiles) + GAP * 2 * S, row.width) * S,
                            PAD * 3 + 16 + top_h + row.height * S), BG_DARK)
    d, f = ImageDraw.Draw(im), font(13)
    x = PAD
    for name, t in tiles:
        im.alpha_composite(nearest(t, S), (x, PAD + 16))
        if f:
            d.text((x + t.width * S // 2, PAD + 8), name, font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
        x += t.width * S + GAP * S
    im.alpha_composite(nearest(row, S), (PAD, PAD * 2 + 16 + top_h))
    return im


# ---------- 검산 ----------
def measure(spec):
    """실제로 찍힌 px 을 잰다 — 표 숫자와 그림이 어긋나는지 보려는 것이다."""
    im = layer_body(spec)
    p = im.load()

    def wide(y):
        xs = [x for x in range(W) if p[x, y][3]]
        return (max(xs) - min(xs) + 1) if xs else 0

    h, t, a, g = spec["head"], spec["torso"], spec["arm"], spec["leg"]
    hw = max(wide(y) for y in range(h["top"], h["bot"]))
    sh = max(wide(y) for y in range(int(t[1][0]), int(t[1][0]) + 3))
    print(f"  안 {spec['name']}: bbox={im.getbbox()} 머리 {hw}x{h['bot'] - h['top']} "
          f"어깨줄폭(팔포함) {sh} 팔 {a['w']}x{a['y1'] - a['y0']} 다리 {g['w']}x{g['y1'] - g['y0']}")
    for label, y in (("어깨", int(t[1][0]) + 1), ("허리", int(t[2][0])), ("엉덩이", int(t[3][0]))):
        xs = [x for x in range(W) if p[x, y][3]]
        # 몸통만 재려면 팔(바깥 덩어리)을 뺀다 — 가운데 이어진 구간만 센다
        mid = [x for x in xs if abs(x - CX) < 13]
        run, best = [], []
        for x in sorted(mid):
            if run and x != run[-1] + 1:
                best = max(best, run, key=len)
                run = []
            run.append(x)
        best = max(best, run, key=len) if run else best
        print(f"     y{y} {label} 몸통폭 {len(best)} (전체 {len(xs)})")
    alphas = sorted({c[1][3] for c in im.getcolors(99999)})
    print(f"     색 {len([c for c in im.getcolors(99999) if c[1][3]])}가지 알파 {alphas}")


def main():
    os.makedirs(OUT, exist_ok=True)
    saves = [("body_a", layer_body(SPEC_A)), ("body_b", layer_body(SPEC_B)),
             ("face_01", layer_face(SPEC_A, 1)), ("face_02", layer_face(SPEC_A, 2)),
             ("face_03", layer_face(SPEC_A, 3)),
             ("cheek", layer_cheek(SPEC_A, CHEEK_PINK)),
             ("cheek_yellow", layer_cheek(SPEC_A, CHEEK_YELLOW)),
             ("shadow", layer_shadow(SPEC_A))]
    print("겹 낱장")
    for name, im in saves:
        im.save(os.path.join(OUT, name + ".png"))
        print(f"  {name:13s} bbox={im.getbbox()}")
    print("실측")
    measure(SPEC_A)
    measure(SPEC_B)
    print("미리보기")
    for name, fn in (("preview_layers", preview_layers), ("preview_ab", preview_ab),
                     ("preview_variants", preview_variants), ("preview_scale", preview_scale),
                     ("preview_cell", preview_cell)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

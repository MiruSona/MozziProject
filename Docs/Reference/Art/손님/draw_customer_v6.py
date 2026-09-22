"""가판대 손님 시안 6판 — v5 의 한 몸 꼴에 **작은 머리 · 외곽선 1px · 약한 명암 한 단**.

정본 : v5 (`draw_customer_v5.py`). 실루엣 짜는 법(union)과 표정·색은 그대로 가져왔다.
사용자 판정 (2026-09-22 · v5 를 보고) : **「오 내가 원한게 맞는데」 — 꼴은 확정이다.**
  고칠 것은 셋 : 「얼굴이 너무 커」 「외각선이랑 약간의 명암은 들어가는게 좋겠다」

v5 → v6 로 바뀐 것 :
  ① **머리 26×26 → 22×22.** 몸통·팔·다리 짜임은 그대로 두고 키 56 을 지켰다.
     어깨·팔은 v5 그대로 두고 **몸통만 14 → 12 로 줄여** 전체 폭 26 을 지켰다.
     머리 22 에 어깨(팔 포함) 26 — 「어깨는 좁게」(mem 20260921-77bca9ea)에 맞춘 값이다.
  ② **외곽선 1px.** 검정이 아니라 **몸 색을 어둡게 만든 색**이다 (HSV 명도 ↓ · 채도 ↑).
     **실루엣 안쪽에 그린다** — 바깥으로 넓히면 발이 캔버스 아래(y64)를 넘고
     팔 틈·가랑이 틈을 외곽선이 먹는다. 안쪽에 그리면 틈이 그대로 남고 크기도 안 변한다.
  ③ **명암 한 단.** 외곽선 바로 안쪽 **오른쪽·아래 1px** 과 **턱 밑 두 줄**뿐이다.
     v3 처럼 가운데까지 차지 않는다 — 「약간」이다.

그밖에는 v5 그대로다 — 한 몸 실루엣 · 볼 없음 · 표정 3장 · 캔버스 48×64 ·
파스텔 5색 · 빛 왼쪽 위 · 알파 0/255.
산출은 이 파일 옆 v6/ 폴더에 떨어진다. v5 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import colorsys
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v6")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

# ---------- 캔버스 (v5 그대로) ----------
W, H = 48, 64
CX = 24.0
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

INK = "#12110F"         # 눈·입 — 찹쌀이 face 와 같은 먹색
SHADOW_A = 56           # 바닥 그림자 알파 — 건물 그림(stall)과 같은 값

# ---------- 몸 색 다섯 ----------
# 속 색만 적는다. 외곽선 색과 명암 색은 아래 ramp_from() 이 HSV 로 만든다 —
# **명도를 내리고 채도를 조금 올린다.** 회색을 섞으면 색이 탁해진다.
# 밝기 배율은 찹쌀이 흰 5단(#FEFDFC→#A6A5A2)에서 잰 비율이다 (v3 에서 쓰던 눈금 그대로).
BODY_COLORS = [
    ("크림흰", "#EFE9DC"),
    ("연분홍", "#F6D9DD"),
    ("연하늘", "#D6E6F2"),
    ("연노랑", "#F7EBC0"),
    ("연보라", "#E2DAF0"),
]
VS = (1.000, 0.882, 0.783, 0.717, 0.654)   # 명도 배율
SS = (1.00, 1.25, 1.50, 1.65, 1.80)        # 채도 배율
SHADE_STEP, LINE_STEP = 1, 4               # 명암은 1단 · 외곽선은 가장 어두운 4단


def ramp_from(hexcol):
    """속 색 하나에서 5단을 만든다. 0 이 속, 1 이 명암, 4 가 외곽선."""
    r, g, b = (int(hexcol.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
    h, sat, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    out = []
    for vm, sm in zip(VS, SS):
        rr, gg, bb = colorsys.hsv_to_rgb(h, min(1.0, sat * sm), v * vm)
        out.append("#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255)))
    return out


RAMPS = {col: ramp_from(col) for _, col in BODY_COLORS}
DEFAULT_COLOR = BODY_COLORS[0][1]

# ---------- 비율표 ----------
# 안이 바뀌면 **이 표만** 고쳐 다시 돌린다.
SPEC = dict(
    name="v6",
    # 머리 22×22 (v5 는 26×26). 아래를 어깨 덩어리에 겹쳐 **목이 없다**.
    head=dict(cx=CX, cy=19.0, rx=11.0, ry=11.0, n=2.15, top=8, bot=30),
    # 어깨 : 납작한 타원 덩어리 (v5 와 같은 rx 13). 머리가 줄어든 만큼 4줄 위로 올렸다.
    shoulder=dict(cy=34.0, rx=13.0, ry=5.5),
    # 몸통 : 반폭 6.5 고정 = 폭 12 일자. 아래만 살짝 둥글다.
    # v5 의 14 에서 2px 줄였다 — 팔 틈을 2px 로 넓히면서 전체 폭 26 을 지키려고 몸통에서 뺐다.
    torso=[(27, 5.0), (31, 6.5), (50, 6.5), (53, 6.0), (55, 5.5)],
    # 팔 : 폭 5 (v5 그대로) · 끝 둥근 막대. 위는 어깨에 파묻히고 아래로는 몸통과 **2px 틈**(x16·17).
    # v5 의 1px 틈은 외곽선이 양쪽에서 1px 씩 먹으면 어두운 줄 하나로 뭉쳐 팔이 안 읽힌다.
    arm=dict(w=5, x_out=11, y0=32, y1=50),
    # 다리 : 폭 5 · 틈 2 · 끝은 뭉툭한 발. 위쪽은 몸통에 파묻혀 y55 아래에서만 갈라진다.
    leg=dict(w=5, gap=2, y0=46, y1=GROUND_Y),
    face=dict(eye_x=19, eye_y=17, eye_w=2, eye_h=4, mouth_y=23),
)


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 픽셀 집합 도우미 (v3 에서 가져왔다) ----------
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
    """|dx/rx|^n + |dy/ry|^n <= 1. n 이 2 면 타원, 커질수록 모서리가 둥근 네모가 된다."""
    out = set()
    for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
        if (y0 is not None and y < y0) or (y1 is not None and y >= y1):
            continue
        for x in range(int(cx - rx) - 2, int(cx + rx) + 3):
            if abs((x + 0.5 - cx) / rx) ** n + abs((y + 0.5 - cy) / ry) ** n <= 1.0:
                out.add((x, y))
    return out


def cosine_at(keys, y):
    """(y, 반폭) 표를 코사인으로 부드럽게 잇는다."""
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
    """폭 표로 몸통을 그린다 — 줄마다 폭이 바뀌는 덩어리."""
    out = set()
    for y in range(int(keys[0][0]), int(keys[-1][0]) + 1):
        hw = cosine_at(keys, y + 0.5)
        x0, x1 = int(round(CX - hw)), int(round(CX + hw))
        out |= {(x, y) for x in range(x0, x1)}
    return out


def capsule(x0, y0, w, h):
    """끝이 둥근 막대 — 팔·다리용. 레퍼런스의 「끝이 뭉툭한 팔다리」를 이걸로 낸다."""
    r = w / 2.0
    body = rect(x0, y0 + int(r), x0 + w, y0 + h - int(r))
    return body | ellipse(x0 + r, y0 + r, r, r) | ellipse(x0 + r, y0 + h - r, r, r)


def leg_shape(x0, y0, w, y1):
    """다리 — 위는 둥글게 몸통에 녹고, 바닥은 **뭉툭한 발**이다.
    캡슐로 두면 끝이 뾰족해져 까치발로 선 것처럼 보인다 (1판에서 봤다).
    그래서 아래는 네모로 두고 바깥 아래 모서리 1px 만 깎는다."""
    r = w / 2.0
    out = rect(x0, y0 + int(r), x0 + w, y1) | ellipse(x0 + r, y0 + r, r, r)
    return out - {(x0, y1 - 1)}


def mirror(px):
    """왼쪽 덩어리를 오른쪽으로 뒤집어 붙인다 (x → 47-x)."""
    return px | {(W - 1 - x, y) for (x, y) in px}


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, px, col):
    p = im.load()
    c = rgb(col)
    for (x, y) in px:
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = c


def edge_of(px):
    """실루엣 **안쪽** 한 겹 — 상하좌우 넷 중 하나라도 바깥이면 가장자리다.
    바깥으로 넓히지 않으므로 캔버스를 안 넘고 팔 틈·가랑이 틈도 안 먹는다."""
    return {(x, y) for (x, y) in px
            if not all(n in px for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))}


# ---------- 겹 1 : 몸 (한 색 · 명암 없음) ----------
def body_pixels(spec=SPEC):
    """**한 덩어리로 합친다.** 머리·어깨·몸통·팔·다리를 union 해서 윤곽이 하나로 이어진다.
    겹치는 자리는 이음새가 아예 안 생기고, 안 겹치는 자리(팔 아래·가랑이)에만 틈이 남는다."""
    h, sh, a, g = spec["head"], spec["shoulder"], spec["arm"], spec["leg"]
    px = superellipse(h["cx"], h["cy"], h["rx"], h["ry"], h["n"], h["top"], h["bot"])
    px |= ellipse(CX, sh["cy"], sh["rx"], sh["ry"])       # 둥근 어깨 — 머리·몸통·팔을 잇는다
    px |= band(spec["torso"])
    px |= mirror(capsule(a["x_out"], a["y0"], a["w"], a["y1"] - a["y0"]))
    px |= mirror(leg_shape(int(CX) - g["gap"] // 2 - g["w"], g["y0"], g["w"], g["y1"]))
    return px


def layer_body(color=DEFAULT_COLOR, spec=SPEC):
    """속 색 → 명암 → 턱 밑 → 외곽선 차례로 얹는다. 외곽선이 맨 위라 실루엣이 안 깨진다."""
    ramp = RAMPS.get(color) or ramp_from(color)
    px = body_pixels(spec)
    line = edge_of(px)
    inner = px - line

    # 명암 : 외곽선 바로 안쪽에서 **오른쪽·아래 대각선 한 겹**만. 빛이 왼쪽 위라 여기만 어둡다.
    shade = {(x, y) for (x, y) in inner if (x + 1, y + 1) not in inner}

    # 턱 밑 : 머리 모양을 두 줄 내려 몸과 겹치는 자리 — 목이 없으니 이 한 줄이 머리와 몸을 가른다
    h = spec["head"]
    head = superellipse(h["cx"], h["cy"], h["rx"], h["ry"], h["n"], h["top"], h["bot"])
    chin = set()
    for dy in (1, 2):
        chin |= {(x, y + dy) for (x, y) in head}
    shade |= (chin & inner) - head

    im = blank()
    put(im, inner, ramp[0])
    put(im, shade, ramp[SHADE_STEP])
    put(im, line, ramp[LINE_STEP])
    return im


# ---------- 겹 2 : 표정 (기본 · 웃음 · 시무룩) ----------
def layer_face(kind, spec=SPEC):
    f = spec["face"]
    im = blank()
    px = set()
    my = f["mouth_y"]
    if kind == 1:                       # 기본 — 세로 긴 눈 + 짧은 입
        px |= mirror(rect(f["eye_x"], f["eye_y"], f["eye_x"] + f["eye_w"],
                          f["eye_y"] + f["eye_h"]))
        px |= rect(int(CX) - 1, my, int(CX) + 1, my + 1)
    elif kind == 2:                     # 웃음 — 눈은 ∧ 활, 입은 ∪ 활
        ex, ey = f["eye_x"] - 1, f["eye_y"] + 1
        px |= mirror({(ex, ey + 1), (ex + 1, ey), (ex + 2, ey), (ex + 3, ey + 1)})
        px |= mirror({(int(CX) - 3, my - 1), (int(CX) - 2, my), (int(CX) - 1, my)})
    else:                               # 시무룩 — 눈 안쪽 끝이 올라가고 입은 ∩ 활
        px |= mirror(rect(f["eye_x"], f["eye_y"] + 1, f["eye_x"] + f["eye_w"],
                          f["eye_y"] + 1 + f["eye_h"] - 1))
        # 눈 **안쪽** 끝 1px 을 올린다 — 바깥을 올리면 화난 얼굴이 된다
        px |= mirror({(f["eye_x"] + f["eye_w"], f["eye_y"])})
        px |= mirror({(int(CX) - 2, my), (int(CX) - 1, my - 1)})
    put(im, px, INK)
    return im


# ---------- 겹 3 : 바닥 그림자 ----------
def layer_shadow():
    """발밑 납작한 타원. 플랫 몸이 밝은 바닥에 묻히는 것을 막는 가장 싼 수단이다."""
    im = blank()
    p = im.load()
    for (x, y) in ellipse(CX, GROUND_Y - 2, 7.0, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)
    return im


# ---------- 겹치기 ----------
def build(face=1, color=DEFAULT_COLOR, shadow=True, spec=SPEC):
    im = blank()
    if shadow:
        im.alpha_composite(layer_shadow())
    im.alpha_composite(layer_body(color, spec))
    im.alpha_composite(layer_face(face, spec))
    return im


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


def load_v5():
    """v5 스크립트를 **그대로 불러와** 비교 칸을 그린다. 값을 베껴 오면 v5 와 어긋날 수 있다."""
    path = os.path.join(D, "draw_customer_v5.py")
    spec = importlib.util.spec_from_file_location("draw_customer_v5", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# ---------- 미리보기 1 : v5 대 v6 ----------
def preview_v5_v6():
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 22, 8
    v5 = load_v5()
    cols = [("v5 기본", lambda: v5.build(1)),
            ("v6 기본", lambda: build(1)),
            ("v6 웃음", lambda: build(2)),
            ("v6 시무룩", lambda: build(3))]
    rows = [("어두운 UI 바탕", BG_DARK), ("풀밭 바탕", BG_GRASS), ("크림 바탕", BG_CREAM)]
    cw, ch = W * S, H * S
    im = Image.new("RGBA", (PAD * 2 + cw * len(cols) + GAP * (len(cols) - 1),
                            TOP + PAD + (ch + ROWGAP + 16) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(14), font(12)
    for i, (name, _) in enumerate(cols):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 11), name, font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, bg) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP + 16)
        for i, (_, fn) in enumerate(cols):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=bg)
            im.alpha_composite(nearest(fn(), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 3), rname, font=lf, fill=(255, 255, 255, 255))
    return im


# ---------- 미리보기 2 : 다섯 색 · 그림자 있음/없음 ----------
def preview_colors():
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 22, 18
    cw, ch = W * S, H * S
    rows = [("풀밭 · 그림자 O", BG_GRASS, True), ("풀밭 · 그림자 X", BG_GRASS, False),
            ("어두운 UI · 그림자 O", BG_DARK, True), ("크림 바탕 · 그림자 O", BG_CREAM, True)]
    n = len(BODY_COLORS)
    im = Image.new("RGBA", (PAD * 2 + cw * n + GAP * (n - 1),
                            TOP + PAD + (ch + ROWGAP) * len(rows)), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(14), font(12)
    for i, (name, col) in enumerate(BODY_COLORS):
        if f:
            d.text((PAD + i * (cw + GAP) + cw // 2, 11), f"{name} {col}", font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    for r, (rname, bg, sh) in enumerate(rows):
        y = TOP + r * (ch + ROWGAP)
        for i, (name, col) in enumerate(BODY_COLORS):
            x = PAD + i * (cw + GAP)
            d.rectangle([x, y, x + cw - 1, y + ch - 1], fill=bg)
            im.alpha_composite(nearest(build((i % 3) + 1, col, shadow=sh), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 3), rname, font=lf, fill=(255, 255, 255, 255))
    return im


# ---------- 미리보기 3 : 표정 셋 ----------
def preview_faces():
    S, GAP, PAD, TOP = 6, 12, 10, 22
    names = ["기본", "웃음", "시무룩"]
    cw, ch = W * S, H * S
    im = Image.new("RGBA", (PAD * 2 + cw * 3 + GAP * 2, TOP + PAD + ch), BG_DARK)
    d, f = ImageDraw.Draw(im), font(14)
    for i, name in enumerate(names):
        x = PAD + i * (cw + GAP)
        d.rectangle([x, TOP, x + cw - 1, TOP + ch - 1], fill=BG_GRASS)
        im.alpha_composite(nearest(build(i + 1), S), (x, TOP))
        if f:
            d.text((x + cw // 2, 11), name, font=f, fill=(255, 255, 255, 255), anchor="mm")
    return im


# ---------- 미리보기 4 : 실제 크기 ----------
def mozzi_image():
    m = os.path.join(ART, "Mozzi")
    im = Image.open(os.path.join(m, "body_hop0.png")).convert("RGBA")
    im.alpha_composite(Image.open(os.path.join(m, "face_01.png")).convert("RGBA"))
    ch = Image.open(os.path.join(m, "cheek.png")).convert("RGBA")
    im.alpha_composite(ch, (14, 34))
    im.alpha_composite(ch.transpose(Image.FLIP_LEFT_RIGHT), (39, 34))
    return im


def cell_image(color=DEFAULT_COLOR, face=1):
    """손님 칸 68×80 안에 넣은 모습."""
    im = Image.new("RGBA", (CELL_W, CELL_H), CELL_BG)
    im.alpha_composite(build(face, color), ((CELL_W - W) // 2, CELL_H - H))
    return im


def _scale_row(scale):
    PAD, GAP = 8, 10
    items = [("찹쌀이", mozzi_image()), ("손님 v6", build(1)),
             ("손님 연분홍", build(2, BODY_COLORS[1][1])),
             ("가판대", Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")),
             ("칸 68×80", cell_image())]
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
    if f and scale >= 2:            # 1배 줄에는 글씨가 겹쳐서 안 붙인다
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
def measure():
    im = layer_body()
    p = im.load()

    def wide(y):
        xs = [x for x in range(W) if p[x, y][3]]
        return (max(xs) - min(xs) + 1) if xs else 0

    h, a, g = SPEC["head"], SPEC["arm"], SPEC["leg"]
    hw = max(wide(y) for y in range(h["top"], h["bot"]))
    print(f"  bbox={im.getbbox()} 머리 {hw}x{h['bot'] - h['top']} "
          f"(가로/세로 {hw / (h['bot'] - h['top']):.2f}) "
          f"팔 {a['w']}x{a['y1'] - a['y0']} 다리 {g['w']}x{g['y1'] - g['y0']}")

    def holes(y):
        """그 줄에서 몸 픽셀이 끊긴 칸 수 — 팔 틈·가랑이가 실제로 열렸는지 본다."""
        xs = [x for x in range(W) if p[x, y][3]]
        return (max(xs) - min(xs) + 1 - len(xs)) if xs else 0

    for y in (40, 44, 48, 52, 58, 62):
        print(f"     y{y} 전체폭 {wide(y)} 빈칸 {holes(y)}")
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    print(f"     몸 색 {len(cols)}가지 {[('#%02X%02X%02X' % c[1][:3]) for c in cols]} "
          f"알파 {sorted({c[1][3] for c in im.getcolors(99999)})}")


def main():
    os.makedirs(OUT, exist_ok=True)
    print("겹 낱장")
    saves = [("body", layer_body()), ("face_01", layer_face(1)),
             ("face_02", layer_face(2)), ("face_03", layer_face(3)),
             ("shadow", layer_shadow())]
    for name, im in saves:
        im.save(os.path.join(OUT, name + ".png"))
        print(f"  {name:10s} bbox={im.getbbox()}")
    for name, col in BODY_COLORS:
        build(1, col).save(os.path.join(OUT, f"full_{name}.png"))
    print("실측")
    measure()
    print("미리보기")
    for name, fn in (("preview_v5_v6", preview_v5_v6), ("preview_colors", preview_colors),
                     ("preview_faces", preview_faces), ("preview_scale", preview_scale)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

"""가판대 손님 시안 3판 — v2 의 2등신 사람 꼴을 두고 **마네킹처럼 보이던 네 자리**를 손질했다.

정본 : v2 (`draw_customer_v2.py`) 의 안 A. 뼈대·도우미 함수는 그대로 가져왔다.
사용자 결정 (2026-09-22 · mem decision 20260922-470f1ce1) :
  - 찹쌀이식 한 덩어리로 바꾸지 않는다. **2등신 사람 꼴 유지.**
  - **볼터치는 안 붙인다.**
  - **몸 색은 파스텔 다섯 가지**로 손님을 가른다.
  - 캔버스 48×64 · 전신 · 외곽선 없음 · 빛 왼쪽 위 · 알파 0/255 · 손님 칸 68×80 은 그대로.

v2 가 「흰 마네킹·로봇 인형」으로 읽힌 까닭 넷과 이번 손질 :
  ① 팔이 몸 옆에 따로 매달린 곧은 막대 두 개  → 어깨에서 곧장 내려오되 **몸통에 닿게** 하고,
     아래로 갈수록 바깥으로 벌리고 끝에 **손 덩어리**를 붙였다. 짧아졌다 (14 → 12).
  ② 다리가 틈을 벌린 곧은 기둥 둘             → 틈 0 · 길이 14 → 7 로 줄이고 **둥근 발**로 만들었다.
     줄인 만큼 몸통을 아래로 늘려 아랫도리가 둥글다.
  ③ 몸통 가운데 명암이 배 주름처럼 뭉침       → 명암 겹을 4 → 2 로 줄여 **오른쪽·아래 가장자리만** 얇게 둔다.
  ④ 머리가 세로로 긴 달걀이라 몸과 따로 논다  → 27×25 (가로/세로 1.08) 로 둥글게 하고
     머리 아래를 몸통에 **3px 겹쳐** 목 없이 붙였다.

산출은 이 파일 옆 v3/ 폴더에 떨어진다. v2 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import colorsys
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v3")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

# ---------- 캔버스 (v2 그대로) ----------
W, H = 48, 64
CX = 24.0               # 48 폭의 진짜 한가운데는 픽셀 23 과 24 의 경계다
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

INK = "#12110F"         # 눈·입 — 찹쌀이 face 와 같은 먹색
SHADOW_A = 56           # 바닥 그림자 알파 — 건물 그림(stall)과 같은 값

# ---------- 몸 색 파스텔 다섯 ----------
# 속 색만 적는다. 명암 4단은 아래 ramp_from() 이 HSV 로 만든다 (명도를 낮추고 채도를 살짝 올린다).
# 밝기 배율은 찹쌀이 흰색 5단(#FEFDFC→#A6A5A2)에서 잰 비율 그대로다 — 그래서 흰 손님은 찹쌀이와 같은 눈금이 된다.
# 흰 손님만 #FEFDFC 가 아니라 **크림 흰색**이다 — 찹쌀이와 나란히 섰을 때 갈리라고 일부러 띄웠다.
BODY_COLORS = [
    ("크림흰", "#F5F1E8"),
    ("연분홍", "#F6D9DD"),
    ("연하늘", "#D6E6F2"),
    ("연노랑", "#F7EBC0"),
    ("연보라", "#E2DAF0"),
]
VS = (1.000, 0.882, 0.783, 0.717, 0.654)   # 명도 배율 (찹쌀이 5단에서 실측)
SS = (1.00, 1.25, 1.50, 1.65, 1.80)        # 채도 배율 — 어두울수록 색이 살짝 진해진다

# ---------- 비율표 ----------
# 안이 바뀌면 **이 표만** 고쳐 다시 돌린다.
SPEC = dict(
    name="v3",
    # 가로/세로 ≒ 1.05. n=2.15 — 2.4 로 두면 둥근 네모(식빵)로 읽혀 조금 낮췄다.
    head=dict(cx=CX, cy=20.0, rx=13.5, ry=12.5, n=2.15, top=8, bot=33),
    # (y, 반폭) — 사이는 코사인으로 보간한다. 머리 아래 3px(y30~33)은 머리에 덮인다.
    # 허리를 v2 의 8.0 에서 8.5 로 덜 조였다 — 너무 조이면 팔과 몸 사이가 통째로 벌어져 막대로 보인다.
    torso=[(30, 5.0), (36, 10.0), (42, 8.5), (49, 10.5), (55, 9.0), (57, 7.0)],
    # 팔 : 안쪽 모서리를 어깨 바깥선에 **딱 붙인다**(x=14). lean 은 아래로 갈수록 바깥으로 벌어지는 px.
    arm=dict(w=4, y0=35, y1=47, lean=2, hand_r=2.5),
    # 다리 : 틈 0 · 폭 6 두 개 = 12. 가운데 한 줄만 어둡게 해 두 짝으로 읽히게 한다.
    leg=dict(w=6, y0=57, y1=GROUND_Y),
    face=dict(eye_x=18, eye_y=19, eye_w=2, eye_h=4, mouth_y=26),
)


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def ramp_from(hexcol):
    """속 색 하나에서 명암 5단을 만든다. 0 이 속, 4 가 가장 어두운 가장자리.
    HSV 로 **명도만 내리고 채도를 조금 올린다** — 회색을 섞으면 색이 탁해진다."""
    r, g, b, _ = rgb(hexcol)
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    out = []
    for vm, sm in zip(VS, SS):
        rr, gg, bb = colorsys.hsv_to_rgb(h, min(1.0, s * sm), v * vm)
        out.append("#%02X%02X%02X" % (round(rr * 255), round(gg * 255), round(bb * 255)))
    return out


RAMPS = {name: ramp_from(col) for name, col in BODY_COLORS}
DEFAULT_RAMP = RAMPS["크림흰"]


# ---------- 픽셀 집합 도우미 (v2 에서 가져왔다) ----------
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
    """끝이 둥근 막대."""
    r = w / 2.0
    cx = x0 + r
    body = rect(x0, y0 + int(r), x0 + w, y0 + h - int(r))
    return body | ellipse(cx, y0 + r, r, r) | ellipse(cx, y0 + h - r, r, r)


def mirror(px):
    """왼쪽 덩어리를 오른쪽으로 뒤집어 붙인다 (x → 47-x)."""
    return px | {(W - 1 - x, y) for (x, y) in px}


def left_arm(a, x_inner):
    """왼팔 하나. 어깨에서는 몸통에 닿고, 아래로 내려갈수록 lean px 만큼 바깥으로 벌어진다.
    끝에는 팔보다 1px 넓은 **손 덩어리**를 붙인다 — 「막대 두 개」로 안 읽히게 하는 값싼 수단이다."""
    w, y0, y1 = a["w"], a["y0"], a["y1"]
    n = y1 - y0
    out = set()
    for y in range(y0, y1):
        t = (y - y0) / float(max(1, n - 1))
        off = int(round(a["lean"] * t))          # 왼쪽이라 바깥은 -x
        x_hi = x_inner - off                     # 팔의 오른쪽 끝(몸통 쪽)
        out |= {(x, y) for x in range(x_hi - w, x_hi)}
    # 어깨 쪽 위 모서리 두 개를 깎아 둥글게
    out -= {(x_inner - w, y0), (x_inner - 1, y0)}
    # 손 : 팔 끝 가운데에 얹는 동그란 덩어리
    off = a["lean"]
    hx = (x_inner - off) - w / 2.0
    out |= ellipse(hx, y1 - 1.0, a["hand_r"], a["hand_r"])
    return out


# ---------- 명암 ----------
def shade_rings(px, rings, ramp):
    """가장자리에서 안쪽으로 색을 한 겹씩 쌓는다.
    (x+k, y+k) 가 덩어리 밖이면 k 번째 겹 → 오른쪽·아래 가장자리만 어두워진다 (빛이 왼쪽 위).
    **rings 가 짧을수록 가장자리만 얇게 어둡다.** v2 는 몸통에 4겹을 줘서 가운데가 배 주름처럼 뭉쳤다."""
    out = {}
    for (x, y) in px:
        idx = 0
        for k, ring in enumerate(rings, start=1):
            if (x + k, y + k) not in px:
                idx = ring
                break
        out[(x, y)] = ramp[idx]
    return out


def blank():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(im, colored):
    p = im.load()
    for (x, y), col in colored.items():
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = rgb(col)


# ---------- 겹 1 : 몸 ----------
def layer_body(spec=SPEC, ramp=DEFAULT_RAMP):
    """머리·몸통·팔·다리를 따로 칠한다. 한 덩어리로 묶으면 머리와 어깨 사이 경계가 안 생긴다."""
    im = blank()
    h = spec["head"]
    head = superellipse(h["cx"], h["cy"], h["rx"], h["ry"], h["n"], h["top"], h["bot"])
    torso = band(spec["torso"])

    a = spec["arm"]
    shoulder_hw = max(w for _, w in spec["torso"])
    x_inner = int(round(CX - shoulder_hw))      # 어깨 바깥선. 팔 안쪽 끝을 여기에 딱 붙인다
    arms = mirror(left_arm(a, x_inner))

    g = spec["leg"]
    legs = mirror(rect(int(CX) - g["w"], g["y0"], int(CX), g["y1"]))
    # 발 바깥 아래 모서리를 깎아 둥글게
    lx0, rx1 = int(CX) - g["w"], W - int(CX) + g["w"] - 1
    legs -= {(lx0, g["y1"] - 1), (rx1, g["y1"] - 1), (lx0, g["y0"]), (rx1, g["y0"])}

    # 얇은 부위(팔·다리)는 겹을 2개만 준다. 몸통도 2겹이다 — 가운데를 비워 뭉침을 없앴다
    put(im, shade_rings(legs, [4, 2], ramp))
    # 두 발 사이 틈이 0 이라 가운데 한 줄을 어둡게 해 두 짝으로 읽히게 한다 (외곽선이 아니라 그늘이다)
    put(im, {(int(CX), y): ramp[3] for y in range(g["y0"] + 1, g["y1"] - 1)})
    put(im, shade_rings(torso, [4, 2], ramp))
    put(im, shade_rings(arms, [4, 2], ramp))
    put(im, shade_rings(head, [4, 3, 2], ramp))
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
    else:                               # 시무룩 — 눈은 짧고 살짝 처지고, 입은 ∩ 활
        px |= mirror(rect(f["eye_x"], f["eye_y"] + 1, f["eye_x"] + f["eye_w"],
                          f["eye_y"] + 1 + f["eye_h"] - 1))
        # 눈 **안쪽** 끝 1px 을 올린다 — 바깥을 올리면 화난 얼굴이 된다
        px |= mirror({(f["eye_x"] + f["eye_w"], f["eye_y"])})
        px |= mirror({(int(CX) - 2, my), (int(CX) - 1, my - 1)})
    put(im, {p: INK for p in px})
    return im


# ---------- 겹 3 : 바닥 그림자 ----------
def layer_shadow():
    """발밑 납작한 타원. 흰 몸이 밝은 바닥에 묻히는 것을 막는 가장 싼 수단이다."""
    im = blank()
    p = im.load()
    for (x, y) in ellipse(CX, GROUND_Y - 2, 8.5, 2.5):
        if 0 <= x < W and 0 <= y < H:
            p[x, y] = (0, 0, 0, SHADOW_A)
    return im


# ---------- 겹치기 ----------
def build(face=1, ramp=DEFAULT_RAMP, shadow=True, spec=SPEC):
    im = blank()
    if shadow:
        im.alpha_composite(layer_shadow())
    im.alpha_composite(layer_body(spec, ramp))
    im.alpha_composite(layer_face(face, spec))
    return im


# ---------- 미리보기 도우미 (v2 에서 가져왔다) ----------
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


def load_v2():
    """v2 스크립트를 **그대로 불러와** 비교 칸을 그린다. 값을 베껴 오면 v2 와 어긋날 수 있다."""
    path = os.path.join(D, "draw_customer_v2.py")
    spec = importlib.util.spec_from_file_location("draw_customer_v2", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# ---------- 미리보기 1 : v2 대 v3 ----------
def preview_v2_v3():
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 22, 8
    v2 = load_v2()
    cols = [("v2 안 A", lambda sh: v2.build(v2.SPEC_A, shadow=sh)),
            ("v3 기본", lambda sh: build(1, shadow=sh)),
            ("v3 웃음", lambda sh: build(2, shadow=sh)),
            ("v3 시무룩", lambda sh: build(3, shadow=sh))]
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
            im.alpha_composite(nearest(fn(True), S), (x, y))
        if lf:
            d.text((PAD, y + ch + 3), rname, font=lf, fill=(255, 255, 255, 255))
    return im


# ---------- 미리보기 2 : 파스텔 다섯 ----------
def preview_colors():
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 22, 26
    cw, ch = W * S, H * S
    n = len(BODY_COLORS)
    im = Image.new("RGBA", (PAD * 2 + cw * n + GAP * (n - 1),
                            TOP + PAD + ch * 2 + ROWGAP), BG_DARK)
    d, f, lf = ImageDraw.Draw(im), font(14), font(12)
    for i, (name, col) in enumerate(BODY_COLORS):
        x = PAD + i * (cw + GAP)
        ramp = RAMPS[name]
        d.rectangle([x, TOP, x + cw - 1, TOP + ch - 1], fill=BG_GRASS)
        im.alpha_composite(nearest(build((i % 3) + 1, ramp), S), (x, TOP))
        y2 = TOP + ch + ROWGAP
        d.rectangle([x, y2, x + cw - 1, y2 + ch - 1], fill=BG_DARK)
        im.alpha_composite(nearest(build((i % 3) + 1, ramp), S), (x, y2))
        if f:
            d.text((x + cw // 2, 11), f"{name} {col}", font=f,
                   fill=(255, 255, 255, 255), anchor="mm")
    if lf:
        d.text((PAD, TOP + ch + 7), "위 : 풀밭 바탕   아래 : 어두운 UI 바탕",
               font=lf, fill=(255, 255, 255, 255))
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


def cell_image(ramp=DEFAULT_RAMP, face=1):
    """손님 칸 68×80 안에 넣은 모습."""
    im = Image.new("RGBA", (CELL_W, CELL_H), CELL_BG)
    im.alpha_composite(build(face, ramp), ((CELL_W - W) // 2, CELL_H - H))
    return im


def _scale_row(scale):
    PAD, GAP, BAND = 8, 10, 0
    items = [("찹쌀이", mozzi_image()), ("손님 v3", build(1)),
             ("손님 연분홍", build(2, RAMPS["연분홍"])),
             ("가판대", Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")),
             ("칸 68×80", cell_image())]
    bots = [im.getbbox()[3] for _, im in items]
    tops = [im.getbbox()[1] for _, im in items]
    tall = max(b - t for b, t in zip(bots, tops))
    cw = [im.width for _, im in items]
    row = Image.new("RGBA", (PAD * 2 + sum(cw) + GAP * (len(items) - 1), tall + BAND + PAD), BG_DARK)
    base = BAND + tall
    for i, (_, src) in enumerate(items):
        row.alpha_composite(src, (PAD + sum(cw[:i]) + GAP * i, base - bots[i]))
    ImageDraw.Draw(row).rectangle([0, base, row.width, row.height], fill=BG_GRASS)
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

    h, t, a, g = SPEC["head"], SPEC["torso"], SPEC["arm"], SPEC["leg"]
    hw = max(wide(y) for y in range(h["top"], h["bot"]))
    print(f"  bbox={im.getbbox()} 머리 {hw}x{h['bot'] - h['top']} "
          f"(가로/세로 {hw / (h['bot'] - h['top']):.2f}) "
          f"팔 {a['w']}x{a['y1'] - a['y0']} 다리 {g['w']}x{g['y1'] - g['y0']}")
    for label, y in (("어깨", 37), ("허리", 42), ("엉덩이", 49), ("발", 60)):
        print(f"     y{y} {label} 전체폭 {wide(y)}")
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    print(f"     색 {len(cols)}가지 알파 {sorted({c[1][3] for c in im.getcolors(99999)})}")


def main():
    os.makedirs(OUT, exist_ok=True)
    print("색표")
    for name, col in BODY_COLORS:
        print(f"  {name} {col} → {' '.join(RAMPS[name])}")
    print("겹 낱장")
    saves = [("body", layer_body()), ("face_01", layer_face(1)),
             ("face_02", layer_face(2)), ("face_03", layer_face(3)),
             ("shadow", layer_shadow())]
    for name, im in saves:
        im.save(os.path.join(OUT, name + ".png"))
        print(f"  {name:10s} bbox={im.getbbox()}")
    for name, col in BODY_COLORS:
        build(1, RAMPS[name]).save(os.path.join(OUT, f"full_{name}.png"))
    print("실측")
    measure()
    print("미리보기")
    for name, fn in (("preview_v2_v3", preview_v2_v3), ("preview_colors", preview_colors),
                     ("preview_faces", preview_faces), ("preview_scale", preview_scale)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

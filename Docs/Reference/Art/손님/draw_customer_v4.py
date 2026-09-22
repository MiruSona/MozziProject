"""가판대 손님 시안 4판 — **팔은 몸에 붙이고 · 일자형 몸매 · 명암 없음**.

정본 : v3 (`draw_customer_v3.py`). 뼈대·도우미 함수는 그대로 가져왔다.
사용자 판정 (2026-09-22 · v3 를 보고) : 「팔이 붙어있으면 좋겠고 좀더 일자형몸매 그리고 명암 없었으면 좋겠어」

v3 → v4 로 바뀐 것 셋 :
  ① 팔을 몸에 **붙인다.** 팔을 따로 그린 막대가 아니라 **몸통 띠에 더하는 혹**으로 만들어서
     틈이 생길 수가 없다 (`band(torso, arm)`). 명암이 없으니 경계는 **실루엣 굴곡**으로만 준다 —
       · 어깨(y34~37)에서 혹이 0 → 3 으로 부풀어 어깨선이 생긴다
       · y47 에서 혹이 3.4 로 가장 불룩하다 = 손
       · y47~49 에서 혹이 3.4 → 0 으로 빠지며 팔이 끝난다
  ② **일자형 몸매.** 어깨→허리→엉덩이 폭 보간을 없앴다 (y36~y52 반폭 9.0 고정 = 폭 18).
     v3 의 넓은 아랫도리(반폭 10.5)가 치마로 읽히던 것을 없앴다. 아래 모서리만 둥글다.
  ③ **명암 없음.** 몸은 색 **한 가지**뿐이다 (플랫). 회색 4단·가장자리 그늘을 전부 뺐다.
     바닥 그림자는 따로 겹이라 그대로 두고, 미리보기에서 있음/없음을 둘 다 보인다.

그밖에는 v3 그대로다 — 캔버스 48×64 · 머리 거의 원 · 볼 없음 · 표정 3장 · 외곽선 없음 · 알파 0/255.
산출은 이 파일 옆 v4/ 폴더에 떨어진다. v3 파일·그림은 안 건드린다.
ProjectMozziUnity/ 도 안 건드린다. 다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "v4")
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

# ---------- 캔버스 (v3 그대로) ----------
W, H = 48, 64
CX = 24.0
GROUND_Y = 64
CELL_W, CELL_H = 68, 80

INK = "#12110F"         # 눈·입 — 찹쌀이 face 와 같은 먹색
SHADOW_A = 56           # 바닥 그림자 알파 — 건물 그림(stall)과 같은 값

# ---------- 몸 색 다섯 (플랫 — 한 색뿐이다) ----------
# 명암이 없어져 명암표(ramp)가 사라졌다. 손님을 가르는 것은 이 색 하나뿐이다.
# 크림흰은 v3 의 #F5F1E8 보다 한 단 진하다 — 플랫이 되면서 크림 바탕에서 묻혀 내린 값이다.
BODY_COLORS = [
    ("크림흰", "#EFE9DC"),
    ("연분홍", "#F6D9DD"),
    ("연하늘", "#D6E6F2"),
    ("연노랑", "#F7EBC0"),
    ("연보라", "#E2DAF0"),
]
DEFAULT_COLOR = BODY_COLORS[0][1]

# ---------- 비율표 ----------
# 안이 바뀌면 **이 표만** 고쳐 다시 돌린다.
SPEC = dict(
    name="v4",
    head=dict(cx=CX, cy=20.0, rx=13.5, ry=12.5, n=2.15, top=8, bot=33),
    # (y, 반폭) — **y36~y52 가 9.0 고정이라 몸통은 일자다** (폭 18). 아래 모서리만 둥글다.
    torso=[(30, 5.0), (34, 7.5), (36, 9.0), (52, 9.0), (55, 8.5), (57, 7.0)],
    # 팔 : 몸통 반폭에 **더해지는 혹**이다 — 따로 그린 막대가 아니라 같은 띠에 얹는다.
    # 그래서 팔과 몸이 처음부터 한 실루엣이고 틈이 생길 수가 없다.
    # y47 의 3.4 가 손 — 팔보다 살짝 불룩해 「여기서 팔이 끝난다」를 알린다.
    arm=[(34, 0.0), (37, 3.0), (45, 3.0), (47, 3.4), (49, 0.0)],
    leg=dict(w=6, y0=57, y1=GROUND_Y),
    face=dict(eye_x=18, eye_y=19, eye_w=2, eye_h=4, mouth_y=26),
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


def band(keys, extra=None):
    """폭 표로 몸통을 그린다. extra 를 주면 그 표의 반폭을 **더한다** — 팔이 그렇게 붙는다."""
    out = set()
    for y in range(int(keys[0][0]), int(keys[-1][0]) + 1):
        hw = cosine_at(keys, y + 0.5)
        if extra is not None:
            hw += cosine_at(extra, y + 0.5)
        x0, x1 = int(round(CX - hw)), int(round(CX + hw))
        out |= {(x, y) for x in range(x0, x1)}
    return out


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


# ---------- 겹 1 : 몸 (한 색 · 명암 없음) ----------
def body_pixels(spec=SPEC):
    """머리·몸통·팔·다리를 **한 덩어리로 합친다.** 명암이 없으니 따로 칠할 까닭이 없다."""
    h = spec["head"]
    px = superellipse(h["cx"], h["cy"], h["rx"], h["ry"], h["n"], h["top"], h["bot"])
    px |= band(spec["torso"], spec["arm"])

    g = spec["leg"]
    legs = mirror(rect(int(CX) - g["w"], g["y0"], int(CX), g["y1"]))
    lx0, rx1 = int(CX) - g["w"], W - int(CX) + g["w"] - 1
    legs -= {(lx0, g["y1"] - 1), (rx1, g["y1"] - 1), (lx0, g["y0"]), (rx1, g["y0"])}
    px |= legs
    return px


def layer_body(color=DEFAULT_COLOR, spec=SPEC):
    im = blank()
    put(im, body_pixels(spec), color)
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
    for (x, y) in ellipse(CX, GROUND_Y - 2, 8.5, 2.5):
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


def load_v3():
    """v3 스크립트를 **그대로 불러와** 비교 칸을 그린다. 값을 베껴 오면 v3 와 어긋날 수 있다."""
    path = os.path.join(D, "draw_customer_v3.py")
    spec = importlib.util.spec_from_file_location("draw_customer_v3", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# ---------- 미리보기 1 : v3 대 v4 ----------
def preview_v3_v4():
    S, GAP, PAD, TOP, ROWGAP = 4, 10, 10, 22, 8
    v3 = load_v3()
    cols = [("v3 기본", lambda: v3.build(1)),
            ("v4 기본", lambda: build(1)),
            ("v4 웃음", lambda: build(2)),
            ("v4 시무룩", lambda: build(3))]
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
    items = [("찹쌀이", mozzi_image()), ("손님 v4", build(1)),
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
          f"팔혹 최대 {max(w for _, w in a)} 다리 {g['w']}x{g['y1'] - g['y0']}")
    for label, y in (("어깨위", 35), ("팔 있는 줄", 40), ("허리", 45),
                     ("팔 아래", 50), ("아랫도리", 55), ("발", 60)):
        print(f"     y{y} {label} 전체폭 {wide(y)}")
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
    for name, fn in (("preview_v3_v4", preview_v3_v4), ("preview_colors", preview_colors),
                     ("preview_faces", preview_faces), ("preview_scale", preview_scale)):
        fn().save(os.path.join(OUT, name + ".png"))
        print("  " + name + ".png")


if __name__ == "__main__":
    main()

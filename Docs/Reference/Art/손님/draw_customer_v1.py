"""가판대 손님(사람) 시안 1판 — 겹 다섯 장(몸·옷·표정·볼·헤어)과 미리보기 넷을 뽑는다.

규격 : 캔버스 48x64 · 인물 약 26x48 · 2.5등신(머리 19px) · 정면 서 있는 전신
       발은 캔버스 맨 아래 바닥선(y=64)에 닿고 가로 가운데 — 피벗 bottom_center 전제.
화풍 : 외곽선 없음 · 명암 한 단계 · 빛은 왼쪽 위 · 장당 3~6색 (Docs/Design/06-아트규칙.md 2절).
옷 : 흰색 + 옅은 회색 그늘로 **모양만** 그린다. 색은 코드가 곱한다 (찹쌀이 몸과 같은 수법).

이 판은 시안이라 ProjectMozziUnity/ 안에는 아무것도 안 넣는다. 결과는 이 폴더에 떨어진다.
다시 돌리면 같은 그림이 나온다 (무작위 없음).
"""
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

# ---------- 공용 좌표 상수 ----------
# 나머지 10장(헤어 4 · 모자 2 · 표정 2 · 옷 2)을 더할 때 이 값들만 보고 그리면 1px 도 안 어긋난다.
W, H = 48, 64
CX = 24                 # 가로 한가운데 (몸·머리 중심축)
GROUND_Y = 64           # 발바닥이 닿는 바닥선 (캔버스 맨 아래)

HEAD_CY = 25.0          # 머리 타원 중심 y
HEAD_RX, HEAD_RY = 9.0, 9.5   # 머리 반지름 → 머리 18x19, y16~35
HEAD_TOP_Y = 16         # 머리 꼭대기 (모자·헤어는 이 위로 2px 까지만 나간다)
HEAD_BOT_Y = 34         # 턱 끝

NECK_Y0, NECK_Y1 = 33, 37       # 목
SHOULDER_Y = 37                 # 어깨선 — 옷은 여기서부터 시작한다
TORSO_X0, TORSO_X1 = 16, 32     # 몸통 좌우 (폭 16)
TORSO_Y1 = 52                   # 몸통 아래 끝 (허리)
ARM_XL, ARM_XR = 15, 32         # 팔은 **1px 폭** — 2px 면 몸통에 붙어 팔로 안 읽힌다
ARM_Y0, ARM_Y1 = 39, 50
HAND_Y = 50                     # 손 (2x2 덩어리)
LEG_Y0, LEG_Y1 = 52, 60         # 다리
LEG_XL, LEG_XR = 20, 26         # 다리 두 개의 왼쪽 x (각 폭 2)
FOOT_Y0 = 60                    # 신발 윗줄 → y60~64

EYE_Y = 27                      # 눈 줄
MOUTH_Y = 31                    # 입 줄
CHEEK_Y = 29                    # 볼터치 줄
CHEEK_XL, CHEEK_XR = 16, 29     # 볼터치 두 개의 왼쪽 x

# 손님 칸(68x80) 안에서 주문 떡 32x32 를 얹는 자리 — 미리보기 4에서 쓴다
CELL_W, CELL_H = 68, 80
CAKE_IN_CELL = (30, 15)         # 떡 png 의 왼쪽 위 (그림 알맹이는 여기서 +0,+5 부터)

# ---------- 색 ----------
# 살빛 3색 + 머리 3색 + 신발 2색. 장당 3~6색 규칙은 겹마다 따로 센다.
SKIN, SKIN_S, SKIN_HI = "#F7D5B0", "#E0B48D", "#FFEBD3"
SHOE, SHOE_S = "#6B4A32", "#513826"
CLOTH, CLOTH_S = "#FEFDFC", "#DCD9D6"     # 찹쌀이 몸(body_hop0)과 같은 흰색 두 단계
INK = "#12110F"                            # 눈·입 — 찹쌀이 face_01 과 같은 먹색
BLUSH, BLUSH_HI = "#F09AA2", "#F8BBC0"     # 볼터치 (건물 분홍 계열)
HAIR1, HAIR1_S, HAIR1_HI = "#6B4A32", "#4E3524", "#8C6344"   # 헤어 1 : 짙은 밤색 단발


def rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ---------- 픽셀 집합 도우미 (건물 스크립트에서 옮겨 적었다) ----------
def rect(x0, y0, x1, y1):
    return {(x, y) for x in range(x0, x1) for y in range(y0, y1)}


def ellipse(cx, cy, rx, ry):
    out = set()
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                out.add((x, y))
    return out


def head_px():
    """머리 타원. 헤어·모자가 이 집합을 기준으로 얹힌다."""
    return ellipse(CX, HEAD_CY, HEAD_RX, HEAD_RY)


class Part:
    """픽셀 덩어리 하나 + 그 덩어리의 기본색·그늘·하이라이트.
    그늘은 (off) 만큼 옮겼을 때 덩어리 밖이면 칠한다 = 오른쪽·아래 가장자리.
    하이라이트는 반대로 왼쪽·위 가장자리. 빛이 왼쪽 위라는 규칙을 이렇게 푼다."""

    def __init__(self, px, base, shade=None, hl=None, off=(1, 1), hoff=(1, 1), hl_left=False):
        self.px, self.base, self.shade, self.hl = px, base, shade, hl
        self.off, self.hoff, self.hl_left = off, hoff, hl_left


def paint(parts, marks=()):
    """겹 한 장을 그린다. marks 는 이미 칠해진 곳 위에만 덧칠하는 점(눈·반짝임 따위)이다."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    p = im.load()
    for part in parts:
        for (x, y) in part.px:
            if 0 <= x < W and 0 <= y < H:
                c = part.base
                if part.shade and (x + part.off[0], y + part.off[1]) not in part.px:
                    c = part.shade
                p[x, y] = rgb(c)
        if part.hl:
            for (x, y) in part.px:
                if part.hl_left and x > CX:      # 빛이 왼쪽 위 — 오른쪽 위까지 밝히면 테두리처럼 보인다
                    continue
                if 0 <= x < W and 0 <= y < H and (x - part.hoff[0], y - part.hoff[1]) not in part.px:
                    p[x, y] = rgb(part.hl)
    for px, col in marks:
        for (x, y) in px:
            if 0 <= x < W and 0 <= y < H and p[x, y][3]:
                p[x, y] = rgb(col)
    return im


# ---------- 겹 1 : 몸 ----------
def layer_body():
    """살빛 한 장. 머리·목·몸통·팔·손·다리 + 신발.
    몸통까지 살로 채워 둔다 — 옷 겹이 얇아도 구멍이 안 뚫린다."""
    parts = []
    # 몸통은 팔(x15 · x32)에 딱 붙여야 한다 — 1px 이라도 비면 팔이 떠 보인다 (1판에서 밟았다)
    torso = rect(TORSO_X0, SHOULDER_Y, TORSO_X1, TORSO_Y1)
    neck = rect(CX - 2, NECK_Y0, CX + 2, NECK_Y1)
    legs = rect(LEG_XL, LEG_Y0, LEG_XL + 2, LEG_Y1) | rect(LEG_XR, LEG_Y0, LEG_XR + 2, LEG_Y1)
    arms = (rect(ARM_XL, ARM_Y0, ARM_XL + 1, ARM_Y1)
            | rect(ARM_XR, ARM_Y0, ARM_XR + 1, ARM_Y1))
    hands = (rect(ARM_XL - 1, HAND_Y, ARM_XL + 1, HAND_Y + 2)
             | rect(ARM_XR, HAND_Y, ARM_XR + 2, HAND_Y + 2))
    # 머리는 따로 칠한다 — 몸통과 한 덩어리로 묶으면 턱 밑 그늘이 안 생긴다
    parts.append(Part(torso | neck | legs | arms | hands, SKIN, SKIN_S, None))
    parts.append(Part(head_px(), SKIN, SKIN_S, SKIN_HI))
    # 신발은 발목보다 1px 씩 넓은 작은 덩어리
    shoes = (rect(LEG_XL - 1, FOOT_Y0, LEG_XL + 3, GROUND_Y)
             | rect(LEG_XR - 1, FOOT_Y0, LEG_XR + 3, GROUND_Y))
    parts.append(Part(shoes, SHOE, SHOE_S, None))
    # 팔과 몸통이 같은 살빛이라 붙여 놓으면 한 덩어리로 보인다.
    # 몸통 맨 바깥 줄을 한 단계 어둡게 그어 **이음매**를 만든다 — 이래야 1px 팔이 팔로 읽힌다.
    seam = (rect(TORSO_X0, ARM_Y0, TORSO_X0 + 1, ARM_Y1)
            | rect(TORSO_X1 - 1, ARM_Y0, TORSO_X1, ARM_Y1))
    return paint(parts, [(seam, SKIN_S)])


# ---------- 겹 2 : 옷 ----------
def layer_cloth_apron():
    """앞치마 한 벌 — 반팔 윗옷 + 허리 아래로 퍼지는 앞치마.
    **흰색 + 한 단계 옅은 회색으로 모양만** 그린다. 색은 코드가 픽셀마다 곱한다.
    어깨 아래 실루엣이 몸통(폭 14)보다 좌우 1px 씩 넓어져야 다른 옷과 구별된다."""
    parts = []
    top = rect(TORSO_X0, SHOULDER_Y, TORSO_X1, 47)            # 윗옷 (어깨~허리 위)
    sleeve = (rect(TORSO_X0 - 1, SHOULDER_Y + 1, TORSO_X0, 43)
              | rect(TORSO_X1, SHOULDER_Y + 1, TORSO_X1 + 1, 43))   # 반팔 소매
    # 앞치마 : 허리에서 아래로 갈수록 한 단씩 넓어지는 사다리꼴
    skirt = set()
    for i, y in enumerate(range(47, 56)):
        grow = i // 3                                          # 3줄마다 1px 씩 퍼진다
        skirt |= rect(TORSO_X0 - grow, y, TORSO_X1 + grow, y + 1)
    parts.append(Part(top | sleeve | skirt, CLOTH, CLOTH_S, None))
    # 앞치마 끈 두 줄 — 그늘색으로 그어 흰 옷 위에서도 모양이 읽힌다
    strap = (rect(19, SHOULDER_Y, 20, 46) | rect(28, SHOULDER_Y, 29, 46)
             | rect(TORSO_X0, 46, TORSO_X1, 47))
    return paint(parts, [(strap, CLOTH_S)])


# ---------- 겹 3 : 표정 ----------
def layer_face_basic():
    """기본 표정 — 눈 두 점(2x3)과 짧은 입. 나머지는 비운다."""
    eyes = rect(19, EYE_Y, 21, EYE_Y + 3) | rect(27, EYE_Y, 29, EYE_Y + 3)
    mouth = rect(CX - 1, MOUTH_Y, CX + 1, MOUTH_Y + 1)
    return paint([Part(eyes | mouth, INK)])


# ---------- 겹 4 : 볼 ----------
def layer_cheek():
    """볼터치 두 개를 한 장에 넣는다 (찹쌀이는 한 장을 코드가 뒤집지만,
    손님은 겹을 전부 같은 48x64 캔버스로 맞추기로 해서 좌우를 같이 그린다)."""
    px = set()
    for x0 in (CHEEK_XL, CHEEK_XR):
        px |= rect(x0, CHEEK_Y, x0 + 3, CHEEK_Y + 2)
    im = paint([Part(px, BLUSH)])
    p = im.load()
    for x0 in (CHEEK_XL, CHEEK_XR):          # 위 한 줄만 밝게 — 둥글게 보인다
        for x in range(x0, x0 + 3):
            p[x, CHEEK_Y] = rgb(BLUSH_HI)
    return im


# ---------- 겹 5 : 헤어 ----------
def layer_hair_bob():
    """헤어 1 — 단발. 머리 타원보다 1px 크게 덮고 얼굴 쪽만 도려낸다.
    옆머리는 턱까지 내려와 실루엣을 바꾼다."""
    cap = ellipse(CX, HEAD_CY - 0.5, HEAD_RX + 1, HEAD_RY + 1)
    face_hole = ellipse(CX, HEAD_CY + 4.0, HEAD_RX - 1.5, HEAD_RY - 2.0)
    side = (rect(CX - 10, 24, CX - 7, HEAD_BOT_Y) | rect(CX + 7, 24, CX + 10, HEAD_BOT_Y))
    px = ((cap - face_hole) | (side & ellipse(CX, HEAD_CY, HEAD_RX + 1.5, HEAD_RY + 1.5)))
    return paint([Part(px, HAIR1, HAIR1_S, HAIR1_HI, hl_left=True)])


LAYERS = [
    ("body", layer_body),
    ("cloth_01", layer_cloth_apron),
    ("face_01", layer_face_basic),
    ("cheek", layer_cheek),
    ("hair_01", layer_hair_bob),
]
# 겹치는 순서 (아래 → 위). 나중에 모자를 더하면 헤어 뒤에 붙인다.
STACK = ["body", "cloth_01", "face_01", "cheek", "hair_01"]


def compose(made, keys=None):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in (keys or STACK):
        im.alpha_composite(made[k])
    return im


# ---------- 미리보기 도우미 ----------
BG_DARK = (0x2B, 0x2B, 0x33, 255)


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


def tint(im, hexcol):
    """옷 겹 픽셀에 RGB 를 곱한다 — 게임 코드가 할 일을 미리 흉내 낸 것."""
    r0, g0, b0, _ = rgb(hexcol)
    out = im.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if a:
                p[x, y] = (r * r0 // 255, g * g0 // 255, b * b0 // 255, a)
    return out


# ---------- 미리보기 1 : 겹 낱장 ----------
def preview_layers(made):
    S, GAP, PAD, TOP = 6, 10, 10, 20
    names = STACK + ["합침"]
    cw = W * S
    im = Image.new("RGBA", (PAD * 2 + cw * len(names) + GAP * (len(names) - 1),
                            TOP + PAD + H * S), BG_DARK)
    f = font(13)
    d = ImageDraw.Draw(im)
    for i, name in enumerate(names):
        x = PAD + i * (cw + GAP)
        im.alpha_composite(checker(cw, H * S), (x, TOP))
        src = compose(made) if name == "합침" else made[name]
        im.alpha_composite(nearest(src, S), (x, TOP))
        if f:
            d.text((x + cw // 2, 10), name, font=f, fill=(255, 255, 255, 255), anchor="mm")
    return im


# ---------- 미리보기 2 : 옷 색 다섯 ----------
TINTS = [("#F8BBC0", "분홍"), ("#C6DCE6", "하늘"), ("#7FA05A", "잎"),
         ("#E8B863", "살구"), ("#8E86C8", "보라")]


def preview_tint(made):
    S, GAP, PAD, TOP = 6, 10, 10, 20
    cw = W * S
    im = Image.new("RGBA", (PAD * 2 + cw * len(TINTS) + GAP * (len(TINTS) - 1),
                            TOP + PAD + H * S), BG_DARK)
    f = font(13)
    d = ImageDraw.Draw(im)
    for i, (col, label) in enumerate(TINTS):
        x = PAD + i * (cw + GAP)
        im.alpha_composite(checker(cw, H * S), (x, TOP))
        one = dict(made)
        one["cloth_01"] = tint(made["cloth_01"], col)
        im.alpha_composite(nearest(compose(one), S), (x, TOP))
        if f:
            d.text((x + cw // 2, 10), label, font=f, fill=(255, 255, 255, 255), anchor="mm")
    return im


# ---------- 미리보기 3 : 찹쌀이·손님·건물 키 비교 ----------
def mozzi_image():
    """찹쌀이 원본 세 장을 그대로 겹친다 (몸 + 표정 + 볼 좌우).
    볼 자리는 mem 20260920-688f3a26 : L x14 · R x39 · y34."""
    m = os.path.join(ART, "Mozzi")
    im = Image.open(os.path.join(m, "body_hop0.png")).convert("RGBA")
    im.alpha_composite(Image.open(os.path.join(m, "face_01.png")).convert("RGBA"))
    cheek = Image.open(os.path.join(m, "cheek.png")).convert("RGBA")
    im.alpha_composite(cheek, (14, 34))
    im.alpha_composite(cheek.transpose(Image.FLIP_LEFT_RIGHT), (39, 34))
    return im


def preview_scale(made):
    S, PAD = 4, 8
    GRASS = (0x8C, 0xB5, 0x6A, 255)
    mz = mozzi_image()
    cust = compose(made)
    stall = Image.open(os.path.join(ART, "Building", "stall.png")).convert("RGBA")
    items = [("찹쌀이", mz), ("손님", cust), ("가판대", stall)]
    # 바닥선을 맞춘다 — 각 그림의 **불투명 아래 끝**을 같은 y 에 놓는다
    bots = [im.getbbox()[3] for _, im in items]
    tops = [im.getbbox()[1] for _, im in items]
    tall = max(b - t for b, t in zip(bots, tops))
    band, gap = 18, 12
    cw = [im.width for _, im in items]
    im = Image.new("RGBA", (PAD * 2 + sum(cw) + gap * 2, tall + band + PAD), BG_DARK)
    base_y = band + tall
    for i, (name, src) in enumerate(items):
        x = PAD + sum(cw[:i]) + gap * i
        im.alpha_composite(src, (x, base_y - bots[i]))
    d = ImageDraw.Draw(im)
    d.rectangle([0, base_y, im.width, im.height], fill=GRASS)
    out = nearest(im, S)
    f = font(14)
    d = ImageDraw.Draw(out)
    if f:
        for i, (name, src) in enumerate(items):
            x = (PAD + sum(cw[:i]) + gap * i + src.width // 2) * S
            hpx = bots[i] - tops[i]
            d.text((x, 10), f"{name} {hpx}px", font=f, fill=(255, 255, 255, 255), anchor="mm")
    return out


# ---------- 미리보기 4 : 손님 칸 ----------
CELL_BG = (140, 115, 89, 255)      # Color(0.55,0.45,0.35) 쯤의 갈색


def cell_image(made, cake):
    im = Image.new("RGBA", (CELL_W, CELL_H), CELL_BG)
    im.alpha_composite(compose(made), ((CELL_W - W) // 2, CELL_H - H))
    im.alpha_composite(cake, CAKE_IN_CELL)
    return im


def preview_cell(made):
    S, PAD, GAP = 4, 8, 4
    cake = Image.open(os.path.join(ART, "Cake", "redbean_cake.png")).convert("RGBA")
    one = cell_image(made, cake)
    row = Image.new("RGBA", (360, CELL_H), (0x1A, 0x1A, 0x20, 255))
    for i in range(5):
        row.alpha_composite(one, (i * (CELL_W + GAP), 0))
    # 칸 하나는 x4, 5칸 줄(360px 폭)도 x4 — 가로가 1440px 이라 캔버스를 거기 맞춘다
    im = Image.new("RGBA", (PAD * 2 + max(one.width, row.width) * S,
                            PAD * 3 + (one.height + row.height) * S), BG_DARK)
    im.alpha_composite(nearest(one, S), (PAD, PAD))
    im.alpha_composite(nearest(row, S), (PAD, PAD * 2 + one.height * S))
    return im


# ---------- 검산 ----------
def report(name, im):
    cols = [c for c in im.getcolors(99999) if c[1][3]]
    alphas = sorted({c[1][3] for c in im.getcolors(99999)})
    print(f"  {name:10s} size={im.size} bbox={im.getbbox()} colors={len(cols)} alphas={alphas}")


def main():
    made = {}
    print("겹 다섯 장")
    for name, fn in LAYERS:
        im = fn()
        im.save(os.path.join(D, name + ".png"))
        report(name, im)
        made[name] = im
    full = compose(made)
    print(f"  {'합친 손님':10s} bbox={full.getbbox()}")

    for name, fn in (("preview_layers", preview_layers), ("preview_tint", preview_tint),
                     ("preview_scale", preview_scale), ("preview_cell", preview_cell)):
        fn(made).save(os.path.join(D, name + ".png"))
        print("  " + name + ".png 저장")


if __name__ == "__main__":
    main()

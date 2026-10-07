# 반복 09 v5 — 「천장으로 읽히는」 나무 천장 변형 셋 (W1 짙은 보 · W2 원근 널 · W3 흰 널 + 밝은 보).
# 세 칸(S1 + G1 · P3 · T1)은 v3 build_mock.py 의 함수를 그대로 불러 짜고, 등은 v4 raw/lamp_cone 을 경로로 읽는다.
# 새 그림은 raw/ 의 PixelLab 부품뿐이다. 여기서는 자르기 · 되풀이 · 배치 · 정수배 확대 · 글자 · 단색 판만 한다.
import importlib.util
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
V3 = os.path.join(HERE, "..", "반복09-v3")
V4 = os.path.join(HERE, "..", "반복09-v4")
spec = importlib.util.spec_from_file_location("v3mock", os.path.join(V3, "build_mock.py"))
v3 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v3)

W, H, WALL_TOP = v3.W, v3.H, v3.WALL_TOP
INK = v3.INK
SCALE = 3                                            # 천장 · 등 · 받침 모두 3배 (v4 는 천장 2배 · 등 3배로 섞였다)

# 천장 : (바탕 조각, 오릴 네모(x0, y0, x1, y1), 등 x 자리)
# 조각은 둘레가 흰 여백이라 그림 부분만 오린다. 180 × 84 를 3배 하면 540 × 252 — 한 칸에 한 장이 꼭 맞는다.
# 오린 네모의 왼 · 오른 끝을 바깥 보 한가운데에 맞춰, 칸 경계 이음매가 보 속에 숨는다.
# 등 x 는 안쪽 보 둘이 위 끝(y=0)에 닿는 자리를 재서 적었다 (W2 는 보가 없어 앞 판 자리 그대로).
CEIL = {
    "W1": ("ceil_beam", (37, 32, 217, 116), (160, 362)),
    "W2": ("ceil_persp", (38, 36, 218, 120), (120, 270, 420)),
    "W3": ("ceil_white", (36, 40, 216, 124), (177, 363)),
}
NAME = {"C2": "C2  앞 판 (나무 널 + 검은 등)", "W1": "W1  짙은 보가 가로지르는 나무 천장",
        "W2": "W2  원근 널 천장 + 몰딩", "W3": "W3  흰 널 + 밝은 나무 보"}


def own(name):
    return Image.open(os.path.join(HERE, "raw", name + ".png")).convert("RGBA")


def ceiling(c, kind):
    bg, box, lamps = CEIL[kind]
    piece = own(bg).crop(box)
    row = v3.up(piece, SCALE)
    lamp = Image.open(os.path.join(V4, "raw", "lamp_cone.png")).convert("RGBA")
    lamp = v3.up(lamp.crop(lamp.getbbox()), SCALE)
    rose = own("rosette")
    rose = v3.up(rose.crop((6, 7, 26, 15)), SCALE)   # 받침 원판만 (아래 줄 토막은 등 줄이 대신한다)
    for px in range(0, c.width, W):
        c.paste(row.crop((0, row.height - WALL_TOP, W, row.height)), (px, 0))
        for lx in lamps:
            c.alpha_composite(lamp, (px + lx - lamp.width // 2, 6))
            c.alpha_composite(rose, (px + lx - rose.width // 2, 0))
    return lamps


def ui(c, x=0):
    """아래 버튼 줄은 캡처 그대로, 돈 표시는 불투명한 짙은 갈색 판 + 흰 글자."""
    cap = Image.open(v3.CAP).convert("RGBA")
    c.paste(cap.crop((0, v3.UI_TOP, W, H)), (x, v3.UI_TOP))
    d = ImageDraw.Draw(c)
    d.rectangle((x + 10, 10, x + 170, 54), fill=(58, 36, 24, 255), outline=(30, 18, 12, 255), width=2)
    d.text((x + 90, 32), "352 G", font=v3.font(26), fill=(255, 255, 255, 255), anchor="mm")


def long_strip(kind):
    c = v3.base(W * 3)
    ceiling(c, kind)
    v3.pane1(c, 0, "G1", "S1")
    v3.pane2(c, W, "P3")
    v3.pane3(c, W * 2, "T1")
    ui(c, W)
    out = c.convert("RGB")
    out.save(os.path.join(HERE, "긴띠_%s.png" % kind))
    return out


def pane2(kind):
    c = v3.base(W)
    print(kind, "등 x :", ceiling(c, kind))
    v3.pane2(c, 0, "P3")
    ui(c)
    out = c.convert("RGB")
    out.save(os.path.join(HERE, "칸2_%s.png" % kind))
    return out


def sheet(panes, strips):
    pad, gap, col = 30, 30, W * 2                    # 한 열 = 천장 2배 오림 폭
    width = pad * 2 + 4 * col + 3 * gap
    sw = (width - pad * 2 - gap * 2) // 3
    sh = sw * H // (W * 3)
    y_zoom = 90 + H + 70
    y_strip = y_zoom + 660 + 90
    s = Image.new("RGB", (width, y_strip + sh + 40), (250, 244, 232))
    d = ImageDraw.Draw(s)
    for i, (kind, im) in enumerate(panes):
        x = pad + i * (col + gap)
        d.text((x + col // 2, 46), NAME[kind], font=v3.font(40), fill=INK, anchor="mm")
        s.paste(im, (x + (col - W) // 2, 90))
        d.text((x + col // 2, y_zoom - 30), kind + "  천장만 2배", font=v3.font(30), fill=(150, 80, 40), anchor="mm")
        s.paste(v3.up(im.crop((0, 0, W, 330)), 2), (x, y_zoom))
    for i, (kind, im) in enumerate(strips):
        x = pad + i * (sw + gap)
        d.text((x + sw // 2, y_strip - 34), "긴 띠 " + kind + " (세 칸 · 축소)", font=v3.font(30), fill=(150, 80, 40), anchor="mm")
        s.paste(im.resize((sw, sh), Image.LANCZOS), (x, y_strip))
    s.save(os.path.join(HERE, "00_견줌판.png"))


if __name__ == "__main__":
    kinds = ("W1", "W2", "W3")
    strips = [(k, long_strip(k)) for k in kinds]
    panes = [("C2", Image.open(os.path.join(V4, "칸2_C2.png")).convert("RGB"))]
    panes += [(k, pane2(k)) for k in kinds]
    sheet(panes, strips)

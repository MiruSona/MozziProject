# 반복 09 v4 — 주방 천장 변형 시안 (C0 지금 · C1 평천장 · C2 나무 널 · C3 타일 + 덕트).
# 고른 조합(S1 + G1 · P3 · T1)의 세 칸은 v3 build_mock.py 의 함수를 그대로 불러 짠다.
# 새 그림은 raw/ 의 PixelLab 부품뿐이다. 여기서는 자르기 · 되풀이 · 배치 · 정수배 확대 · 글자만 한다.
import importlib.util
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
V3 = os.path.join(HERE, "..", "반복09-v3")
spec = importlib.util.spec_from_file_location("v3mock", os.path.join(V3, "build_mock.py"))
v3 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v3)                          # __main__ 이 아니라 v3 그림은 다시 안 쓴다

W, H, WALL_TOP = v3.W, v3.H, v3.WALL_TOP
INK = v3.INK
LAMP_X = (120, 270, 420)                             # 칸마다 같은 자리 — 칸 경계에서 등이 안 잘린다

# 천장 : (바탕 조각, 쓸 열 범위, 위에서부터 쌓을 줄 범위들, 등)
# PixelLab 조각은 둘레에 흰 여백이 있어 그림 부분만 오려 쓴다. 2배로 키우면 높이가 모자라
# 민무늬 줄(평천장) · 널 한 장(나무)을 한 번 더 쌓아 250px 를 채운다.
CEIL = {
    "C0": None,
    "C1": ("ceil_flat", (16, 240), ((16, 60), (50, 78), (60, 113)), "lamp_globe"),
    "C2": ("ceil_wood", (8, 248), ((18, 22), (8, 39), (24, 39), (39, 114)), "lamp_cone"),
    "C3": ("ceil_tile", (8, 247), ((11, 48),), "lamp_steel"),      # 크림 띠 + 덕트만. 아래는 벽 타일을 잇는다
}
LAMP_SCALE = 3
NAME = {"C0": "C0  지금 (서까래 + 한지 등)", "C1": "C1  크림 평천장 + 유리 등",
        "C2": "C2  나무 널 천장 + 검은 등", "C3": "C3  타일 벽 + 덕트 + 철제 등"}


def own(name):
    return Image.open(os.path.join(HERE, "raw", name + ".png")).convert("RGBA")


def ceiling(c, kind):
    """천장 바탕을 2배로 키워 거울 되풀이로 깔고(아래 끝 = 윗벽장 위 끝 y=250) 등을 얹는다."""
    if CEIL[kind] is None:
        return
    bg, (x0, x1), bands, lamp = CEIL[kind]
    src = own(bg)
    piece = Image.new("RGBA", (x1 - x0, sum(b - a for a, b in bands)))
    y = 0
    for a, b in bands:
        piece.paste(src.crop((x0, a, x1, b)), (0, y))
        y += b - a
    row = v3.strip(v3.up(piece, 2), c.width)
    if kind == "C3":                                 # 벽 타일(v2 wall 의 아래 48줄)을 천장 자리까지 올려 깐다
        wall = v3.strip(v3.raw("wall"), c.width)
        band = wall.crop((0, wall.height - 48, c.width, wall.height))
        for ty in range(WALL_TOP - 48, -48, -48):
            c.paste(band, (0, ty))
        c.paste(row, (0, 0))
    else:
        c.paste(row.crop((0, row.height - WALL_TOP, c.width, row.height)), (0, 0))
    im = own(lamp)
    im = v3.up(im.crop(im.getbbox()), LAMP_SCALE)
    for px in range(0, c.width, W):
        for lx in LAMP_X:
            c.alpha_composite(im, (px + lx - im.width // 2, 0))


def long_strip(kind):
    c = v3.base(W * 3)
    ceiling(c, kind)
    v3.pane1(c, 0, "G1", "S1")
    v3.pane2(c, W, "P3")
    v3.pane3(c, W * 2, "T1")
    v3.ui(c, W)
    out = c.convert("RGB")
    out.save(os.path.join(HERE, "긴띠_%s.png" % kind))
    return out


def pane2(kind):
    c = v3.base(W)
    ceiling(c, kind)
    v3.pane2(c, 0, "P3")
    v3.ui(c)
    out = c.convert("RGB")
    if kind != "C0":
        out.save(os.path.join(HERE, "칸2_%s.png" % kind))
    return out


def sheet(panes, strips):
    gap, pad = 30, 30
    width = pad * 2 + 4 * W + 3 * gap
    sw, sh = 740, 548
    s = Image.new("RGB", (width, 90 + H + 80 + sh + 60), (250, 244, 232))
    d = ImageDraw.Draw(s)
    x = pad
    for kind, im in panes:
        d.text((x + W // 2, 46), NAME[kind], font=v3.font(30), fill=INK, anchor="mm")
        s.paste(im, (x, 90))
        x += W + gap
    y = 90 + H + 20
    d.text((pad, y), "긴 띠 (세 칸 이은 것 · S1 + G1 · P3 · T1)", font=v3.font(28), fill=INK)
    x = pad
    step = sw + (width - pad * 2 - sw * 3) // 2   # 740 × 3 + 틈 15 × 2 = 2250
    for kind, im in strips:
        s.paste(im.resize((sw, sh), Image.LANCZOS), (x, y + 80))
        d.text((x + sw // 2, y + 58), kind, font=v3.font(26), fill=(150, 80, 40), anchor="mm")
        x += step
    s.save(os.path.join(HERE, "00_견줌판.png"))


if __name__ == "__main__":
    kinds = ("C0", "C1", "C2", "C3")
    strips = [(k, long_strip(k)) for k in kinds]
    panes = [(k, pane2(k)) for k in kinds]
    sheet(panes, strips[1:])

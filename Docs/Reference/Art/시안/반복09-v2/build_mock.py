# 반복 09 v2 — 주방 구조 시안 A · B · C 를 짠다.
# 새 그림은 raw/ 의 PixelLab 부품뿐이다. 여기서는 자르기 · 배치 · 정수배 확대 · 글자 · 반투명 표시만 한다.
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
BG = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen", "kitchen_bg.png")
CAP = os.path.join(ROOT, "Docs", "Reference", "Art", "정식", "반복08", "견줌", "주방캡처", "1_쉼.png")
W, H = 540, 1200
WALL_TOP = 250                    # 이 위(천장 · 등)는 지금 배경 그대로
UI_TOP = 1050                     # 안내 글 · 버튼 줄 — 지금 캡처에서 오려 붙인다
COUNTER_H = 244                   # 조리대 조각에서 쓰는 높이 (그 아래는 조각에 딸린 바닥이라 버린다)
BIG_TOP, SMALL_TOP = 600, 700     # 조리대 윗면이 시작하는 y — A · B 는 2배 조리대, C 는 1배


def font(size):
    return ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", size)


def raw(name, scale=1):
    """부품을 읽어 빈 둘레를 자르고 정수배로 키운다 (NEAREST — 도트가 안 뭉개진다)."""
    im = Image.open(os.path.join(HERE, "raw", name + ".png")).convert("RGBA")
    im = im.crop(im.getbbox())
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST) if scale > 1 else im


def strip(piece, width):
    """조각 · 뒤집은 조각을 번갈아 이어 width 를 채운다 (이음매가 맞는다)."""
    flip = piece.transpose(Image.FLIP_LEFT_RIGHT)
    row = Image.new("RGBA", (width, piece.height))
    for i in range(width // piece.width + 1):
        row.paste(flip if i % 2 else piece, (piece.width * i, 0))
    return row


def base(width, counter_top, scale):
    """천장 · 바닥(지금 배경) + 벽 + 조리대. width 가 540 보다 넓으면 옆으로 이어 붙인다."""
    bg = Image.open(BG).convert("RGBA")
    c = Image.new("RGBA", (width, H))
    for x in range(0, width, W):
        c.paste(bg, (x, 0))
    wall = strip(raw("wall"), width)
    c.paste(wall, (0, WALL_TOP))
    band = wall.crop((0, wall.height - 48, width, wall.height))   # 타일 띠를 되풀이해 조리대까지 채운다
    for y in range(WALL_TOP + wall.height, counter_top, 48):
        c.paste(band, (0, y))
    piece = Image.open(os.path.join(HERE, "raw", "counter.png")).convert("RGBA").crop((0, 0, 256, COUNTER_H))
    piece = piece.resize((piece.width * scale, piece.height * scale), Image.NEAREST)
    c.paste(strip(piece, width), (0, counter_top))
    return c


def put(c, name, cx, bottom, scale=1):
    """부품을 가운데 x · 아래 끝 y 에 올린다."""
    im = raw(name, scale)
    c.alpha_composite(im, (cx - im.width // 2, bottom - im.height))


def mark(c, box, text, fill=(40, 48, 90, 150), size=18):
    """반투명 표시 네모 + 글자 (결과 자리 · 작업면 표시)."""
    layer = Image.new("RGBA", c.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle(box, fill=fill, outline=(255, 255, 255, 255), width=2)
    d.text(((box[0] + box[2]) // 2, (box[1] + box[3]) // 2), text, font=font(size), fill=(255, 255, 255, 255),
           anchor="mm", align="center")
    c.alpha_composite(layer)


def ui(c, x=0):
    cap = Image.open(CAP).convert("RGBA")
    c.paste(cap.crop((0, UI_TOP, W, H)), (x, UI_TOP))
    c.paste(cap.crop((0, 0, 190, 60)), (x, 0))


def save(c, name):
    c.convert("RGB").save(os.path.join(HERE, name))
    return c.convert("RGB")


# ---- A : 고정 화면 + 가운데 큰 작업면 하나 ----
def plan_a(name, big, waiting, done=False):
    c = base(W, BIG_TOP, 2)
    put(c, "shelf", 175, 540, 2)                                  # 재료 통 선반 — 벽 왼쪽, 늘 보임
    put(c, "steamer_open" if done else "steamer", 440, 690, 2)    # 화구+찜기 — 조리대 오른쪽 뒤 붙박이
    if done:
        mark(c, (350, 340, 530, 440), "결과 자리\n떡 · 이름 · NEW", (200, 120, 40, 190), 16)
    else:
        mark(c, (350, 340, 530, 440), "결과 자리", size=16)
    mark(c, (110, 700, 390, 905), "", (255, 255, 255, 40))        # 작업면 (약 280×205 — 기구는 위로 솟는다)
    for part, cx, bottom in big:
        put(c, part, cx, bottom, 2)
    if waiting:
        put(c, waiting, 62, 900)                                  # 다음 단계 기구 — 구석에 1배로 대기
    ui(c)
    return save(c, name)


# ---- B : 옆으로 긴 조리대 세 칸 ----
def plan_b():
    c = base(W * 3, BIG_TOP, 2)
    put(c, "shelf", 270, 545, 2)                                  # 칸1 준비
    put(c, "tray", 140, 880, 2)
    put(c, "ingredients", 140, 850, 1)
    put(c, "mill", 390, 860, 2)
    put(c, "mortar", 540 + 270, 900, 3)                           # 칸2 치기 — 3배로 화면의 주인공
    put(c, "marble", 1080 + 140, 880, 2)                          # 칸3 마무리
    put(c, "steamer", 1080 + 420, 790, 2)
    mark(c, (1080 + 130, 350, 1080 + 410, 520), "결과 자리")
    panes = []
    for i in range(3):
        p = c.crop((W * i, 0, W * (i + 1), H))
        ui(p)
        panes.append(save(p, "B_칸%d.png" % (i + 1)))
    ui(c, W)                                                      # 긴 띠 — 지금 보이는 칸(칸2)에만 UI + 테두리
    d = ImageDraw.Draw(c)
    d.rectangle((W, 0, W * 2 - 1, H - 1), outline=(255, 60, 60, 255), width=8)
    for i, t in enumerate(("칸1 준비", "칸2 치기 (지금 보이는 칸)", "칸3 마무리")):
        d.text((W * i + 270, 80), t, font=font(34), fill=(255, 255, 255, 255), anchor="mm",
               stroke_width=4, stroke_fill=(60, 30, 20, 255))
    return panes, save(c, "B_긴띠.png")


# ---- C : 한 줄 고정 붙박이 ----
def plan_c():
    c = base(W, SMALL_TOP, 1)
    put(c, "shelf", 130, 560)
    mark(c, (300, 410, 520, 550), "결과 자리")
    put(c, "tray", 75, 850)
    put(c, "mill", 75, 800)
    put(c, "mortar", 205, 800)
    put(c, "marble", 330, 815)
    put(c, "steamer", 465, 800)
    ui(c)
    return save(c, "C_한줄.png")


def sheet(a, b, strip_im, c):
    """견줌판 — 세 줄. 그림은 절반 크기로 줄여 놓는다 (보기용)."""
    tw, th, lab, gap, rowh = 270, 600, 330, 24, 680
    s = Image.new("RGB", (lab + 4 * (tw + gap) + 150, rowh * 3 + 20), (250, 244, 232))
    d = ImageDraw.Draw(s)
    cur = Image.open(CAP).convert("RGB")
    rows = [
        ("A", "큰 작업면 하나", "화면은 안 움직인다.\n가운데 작업면에\n그 단계 기구만 크게.",
         list(zip(a, ("A1 고르기", "A2 갈기", "A3 치기", "A4 찌기 · 결과")))),
        ("B", "옆으로 세 칸", "조리대가 옆으로 길다.\n단계가 넘어가면\n화면이 옆으로 밀린다.",
         list(zip(b, ("칸1 준비", "칸2 치기", "칸3 마무리")))),
        ("C", "한 줄", "한 화면에 넷이 다 보인다.\n기구는 작다.",
         [(c, "C 한 줄"), (cur, "(견줄 것) 지금 화면")]),
    ]
    for r, (big, title, desc, items) in enumerate(rows):
        y = 20 + rowh * r
        d.text((30, y - 10), big, font=font(150), fill=(150, 80, 40))
        d.text((150, y + 60), title, font=font(22), fill=(60, 30, 20))
        d.text((30, y + 200), desc, font=font(22), fill=(60, 30, 20), spacing=8)
        for i, (im, cap) in enumerate(items):
            x = lab + i * (tw + gap)
            s.paste(im.resize((tw, th), Image.LANCZOS), (x, y + 40))
            d.text((x + tw // 2, y + 18), cap, font=font(24), fill=(60, 30, 20), anchor="mm")
            if big == "B" and i < 2:
                d.text((x + tw + gap // 2, y + 340), "→", font=font(40), fill=(220, 50, 50), anchor="mm")
        if big == "B":
            x = lab + 3 * (tw + gap)
            s.paste(strip_im.resize((405, 300), Image.LANCZOS), (x, y + 190))
            d.text((x + 202, y + 160), "세 칸을 이은 긴 띠 (빨간 테 = 보이는 칸)", font=font(18), fill=(60, 30, 20), anchor="mm")
    s.save(os.path.join(HERE, "00_견줌판.png"))


if __name__ == "__main__":
    a = [
        plan_a("A1_고르기.png", [("tray", 250, 885), ("ingredients", 250, 850)], "mill"),
        plan_a("A2_갈기.png", [("mill", 250, 890)], "mortar"),
        plan_a("A3_치기.png", [("mortar", 250, 890)], "marble"),
        plan_a("A4_찌기.png", [], None, done=True),
    ]
    b, long_strip = plan_b()
    sheet(a, b, long_strip, plan_c())

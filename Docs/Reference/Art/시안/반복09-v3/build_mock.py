# 반복 09 v3 — 주방 B 구조(옆으로 세 칸)의 기구 · 선반 변형 시안을 짠다.
# 새 그림은 raw/ 의 PixelLab 부품뿐이다 (v2 부품은 v2 raw/ 에서 읽는다).
# 여기서는 자르기 · 줄/칸 되풀이 · 배치 · 정수배 확대 · 글자 · 반투명 표시만 한다.
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
V2 = os.path.join(HERE, "..", "반복09-v2")
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
BG = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen", "kitchen_bg.png")
CAP = os.path.join(ROOT, "Docs", "Reference", "Art", "정식", "반복08", "견줌", "주방캡처", "1_쉼.png")
W, H = 540, 1200
WALL_TOP, UI_TOP, COUNTER_H, TOP = 250, 1050, 244, 600
INK = (60, 30, 20)


def font(size):
    return ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", size)


def load(name):
    """v3 raw/ 에 있으면 그것, 없으면 v2 raw/ 의 부품을 읽는다."""
    p = os.path.join(HERE, "raw", name + ".png")
    if not os.path.exists(p):
        p = os.path.join(V2, "raw", name + ".png")
    return Image.open(p).convert("RGBA")


def up(im, scale):
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST) if scale > 1 else im


def raw(name, scale=1):
    im = load(name)
    return up(im.crop(im.getbbox()), scale)


def strip(piece, width):
    flip = piece.transpose(Image.FLIP_LEFT_RIGHT)
    row = Image.new("RGBA", (width, piece.height))
    for i in range(width // piece.width + 1):
        row.paste(flip if i % 2 else piece, (piece.width * i, 0))
    return row


def base(width):
    bg = Image.open(BG).convert("RGBA")
    c = Image.new("RGBA", (width, H))
    for x in range(0, width, W):
        c.paste(bg, (x, 0))
    wall = strip(raw("wall"), width)
    c.paste(wall, (0, WALL_TOP))
    band = wall.crop((0, wall.height - 48, width, wall.height))
    for y in range(WALL_TOP + wall.height, TOP, 48):
        c.paste(band, (0, y))
    piece = up(load("counter").crop((0, 0, 256, COUNTER_H)), 2)
    c.paste(strip(piece, width), (0, TOP))
    return c


def put(c, im, cx, bottom):
    c.alpha_composite(im, (cx - im.width // 2, bottom - im.height))


def mark(c, box, text, fill=(40, 48, 90, 150), size=18):
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


# ---- 재료 선반 : v2 선반(6칸 × 2줄)을 줄 · 칸 단위로 잘라 되풀이한다 ----
def shelf(cols, rows):
    s = load("shelf")
    x0, x1 = 47, 208
    head, foot = s.crop((x0, 19, x1, 30)), s.crop((x0, 106, x1, 120))
    jar_a, jar_b, board = s.crop((x0, 30, x1, 64)), s.crop((x0, 72, x1, 106)), s.crop((x0, 64, x1, 72))
    parts = [head]
    for r in range(rows):
        parts.append(jar_b if r % 2 else jar_a)
        parts.append(board if r < rows - 1 else foot)
    im = Image.new("RGBA", (x1 - x0, sum(p.height for p in parts)))
    y = 0
    for p in parts:
        im.paste(p, (0, y))
        y += p.height
    if cols == 8:                                   # 왼쪽 4칸 + 오른쪽 4칸(3~6번째 병)을 병 사이 틈에서 잇는다
        wide = Image.new("RGBA", (105 + 106, im.height))
        wide.paste(im.crop((0, 0, 105, im.height)), (0, 0))
        wide.paste(im.crop((55, 0, 161, im.height)), (105, 0))
        im = wide
    return up(im, 2)


def tag(c, cx, cy, text):
    d = ImageDraw.Draw(c)
    d.rectangle((cx - 17, cy - 8, cx + 17, cy + 8), fill=(255, 255, 255, 255), outline=INK + (255,))
    d.text((cx, cy), text, font=font(12), fill=INK + (255,), anchor="mm")


def shelf_s1(c):
    im = shelf(6, 3)
    put(c, im, 270, 598)
    top = 598 - im.height
    d = ImageDraw.Draw(c)
    for cx, t in ((60, "◀"), (480, "▶")):
        d.ellipse((cx - 26, top + 105, cx + 26, top + 157), fill=(255, 250, 240, 255), outline=INK + (255,), width=3)
        d.text((cx, top + 130), t, font=font(26), fill=INK + (255,), anchor="mm")
    d.text((270, 616), "●  ○", font=font(18), fill=INK + (255,), anchor="mm")
    tag(c, 149, top + 96, "150")
    tag(c, 198, top + 96, "32")


def shelf_s2(c):
    im = shelf(8, 3)
    put(c, im, 270, 598)
    top = 598 - im.height
    tag(c, 98, top + 96, "150")
    tag(c, 147, top + 96, "32")


def shelf_s3(c):
    im = shelf(6, 2)
    put(c, im, 270, 590)
    top = 590 - im.height
    d = ImageDraw.Draw(c)
    tabs = (("잔디", (120, 180, 90)), ("물", (90, 150, 210)), ("용암", (220, 110, 70)), ("늪", (130, 110, 150)))
    for i, (t, col) in enumerate(tabs):
        x = 112 + i * 80
        on = i == 0
        d.rectangle((x, top - (44 if on else 36), x + 74, top + 2), fill=col + (255,) if on else (225, 215, 200, 255),
                    outline=INK + (255,), width=3)
        d.text((x + 37, top - (21 if on else 17)), t, font=font(20), fill=(255, 255, 255, 255) if on else INK + (255,),
               anchor="mm")
    tag(c, 149, top + 96, "150")
    tag(c, 198, top + 96, "32")


SHELF = {"S1": shelf_s1, "S2": shelf_s2, "S3": shelf_s3}
GRIND = {"G1": "blender", "G2": "grinder"}
POUND = {"P1": "mortar", "P2": "bowl", "P3": "marble_mortar"}
STEAM = {"T1": "esteamer", "T1_열림": "esteamer_open", "T2": "steamer", "T2_열림": "steamer_open"}


def pane1(c, x, g, s):
    sub = Image.new("RGBA", (W, H))
    SHELF[s](sub)
    c.alpha_composite(sub, (x, 0))
    put(c, raw("tray", 2), x + 140, 900)
    put(c, raw("ingredients", 1), x + 140, 870)
    put(c, raw(GRIND[g], 2), x + 395, 880)


def pane2(c, x, p):
    put(c, raw("plants", 2), x + 270, 440)          # 휑하던 벽 — 화분 선반 + 조리 도구 줄
    put(c, raw("rail", 2), x + 270, 590)
    put(c, raw(POUND[p], 3), x + 270, 920)


def pane3(c, x, t):
    put(c, raw("marble", 2), x + 140, 880)
    put(c, raw(STEAM[t], 2), x + 410, 800)
    if t.endswith("열림"):
        mark(c, (x + 130, 350, x + 410, 520), "결과 자리\n떡 · 이름 · NEW", (200, 120, 40, 190))
    else:
        mark(c, (x + 130, 350, x + 410, 520), "결과 자리")


def single(name, fn, *a):
    c = base(W)
    fn(c, 0, *a)
    ui(c)
    c.convert("RGB").save(os.path.join(HERE, name))
    return c.convert("RGB")


def long_strip():
    c = base(W * 3)
    pane1(c, 0, "G1", "S1")
    pane2(c, W, "P1")
    pane3(c, W * 2, "T1")
    ui(c, W)
    d = ImageDraw.Draw(c)
    d.rectangle((W, 0, W * 2 - 1, H - 1), outline=(255, 60, 60, 255), width=8)
    for i, t in enumerate(("칸1 준비", "칸2 치기 (지금 보이는 칸)", "칸3 마무리")):
        d.text((W * i + 270, 80), t, font=font(34), fill=(255, 255, 255, 255), anchor="mm",
               stroke_width=4, stroke_fill=INK + (255,))
    c.convert("RGB").save(os.path.join(HERE, "B_긴띠_권장.png"))
    return c.convert("RGB")


def sheet(p1, p2, p3, strip_im):
    """견줌판 — 네 줄 + 긴 띠. 낱장은 절반 크기, 갈기 기구는 오려서 2배."""
    lab, gap = 340, 30
    half = lambda im: im.resize((270, 600), Image.LANCZOS)
    zoom = lambda im: up(im.crop((255, 630, 535, 900)), 2)          # 갈기 기구 둘레 280×270 → 560×540
    rows = [
        ("①", "재료 선반", "재료가 24종 넘게\n늘 때 담는 방법.\nS1 넘기기 · S2 한 벽에\n24병 · S3 계열 탭",
         [(half(p1[0]), "S1  18병 + ◀▶ 넘김"), (half(p1[1]), "S2  한 벽에 24병"), (half(p1[2]), "S3  계열 탭 + 12병")]),
        ("②", "갈기 기구", "맷돌 대신 쓸\n현대 기구.\n(칸1 오른쪽을 크게\n오려 본 것)",
         [(zoom(p1[0]), "G1  믹서기"), (zoom(p1[1]), "G2  탁상 분쇄기")]),
        ("③", "치기 기구", "칸2 의 주인공.\n떡메로 눌러 친다.\n벽 장식도 새로 걸었다.",
         [(half(p2[0]), "P1  나무 절구 (v2 그대로)"), (half(p2[1]), "P2  스테인리스 볼"), (half(p2[2]), "P3  흰 돌 절구")]),
        ("④", "찜기", "2.5초 뒤 뚜껑이\n열리며 완성.",
         [(half(p3[0]), "T1  전기 찜기 (닫힘)"), (half(p3[1]), "T1  (열림 · 결과)"), (half(p3[2]), "T2  화구 + 대나무 (v2)")]),
    ]
    width = lab + 2 * (560 + gap) + 20
    heights = [max(im.height for im, _ in r[3]) + 110 for r in rows]
    s = Image.new("RGB", (width, sum(heights) + 720), (250, 244, 232))
    d = ImageDraw.Draw(s)
    y = 20
    for (num, title, desc, items), rh in zip(rows, heights):
        d.text((30, y), num, font=font(90), fill=(150, 80, 40))
        d.text((30, y + 120), title, font=font(40), fill=INK)
        d.text((30, y + 190), desc, font=font(22), fill=INK, spacing=8)
        x = lab
        for im, cap in items:
            d.text((x + im.width // 2, y + 26), cap, font=font(24), fill=INK, anchor="mm")
            s.paste(im, (x, y + 56))
            x += im.width + gap
        d.line((20, y + rh - 22, width - 20, y + rh - 22), fill=(210, 190, 170), width=2)
        y += rh
    d.text((30, y + 10), "긴 띠", font=font(40), fill=INK)
    d.text((30, y + 80), "권장 묶음으로 이은\n세 칸 (G1 + S1 · P1 · T1).\n빨간 테 = 보이는 칸", font=font(22), fill=INK, spacing=8)
    s.paste(strip_im.resize((810, 600), Image.LANCZOS), (lab, y + 56))
    s.save(os.path.join(HERE, "00_견줌판.png"))


if __name__ == "__main__":
    p1 = [single("칸1_G1_S1.png", pane1, "G1", "S1"), single("칸1_G2_S2.png", pane1, "G2", "S2"),
          single("칸1_G1_S3.png", pane1, "G1", "S3")]
    p2 = [single("칸2_%s.png" % p, pane2, p) for p in ("P1", "P2", "P3")]
    p3 = [single("칸3_T1.png", pane3, "T1"), single("칸3_T1_열림.png", pane3, "T1_열림"), single("칸3_T2.png", pane3, "T2")]
    sheet(p1, p2, p3, long_strip())

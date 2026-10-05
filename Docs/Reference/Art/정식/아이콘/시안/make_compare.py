"""아이콘 외곽선 시안 비교판 — 지금 / 꼴1 / 꼴2 를 나란히 놓는다.

읽는 것 : 지금 그림(Resources/Art/Cake · Ingredient) · 이 폴더의 꼴1/ · 꼴2/ · 진열장 · 말풍선 · 버튼 그림.
쓰는 것 : 이 폴더의 compare_*.png 넷. Resources 는 읽기만 한다.

쓰기 : python make_compare.py
"""
import os

from PIL import Image, ImageDraw, ImageFont

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, "..", "..", "..", "..", "..", ".."))
ART = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")
FONT = os.path.join(ROOT, "Tools", "ArtTool", "src", "arttool", "assets", "fonts", "Pretendard-Medium.otf")

CAKES = ["plain_cake", "redbean_cake", "bean_cake", "mugwort_cake", "sesame_cake", "pinenut_cake", "jujube_cake",
         "redbean_bean_cake", "mugwort_sesame_cake", "pinenut_jujube_cake", "redbean_mugwort_sesame_cake"]
INGS = ["redbean", "bean", "mugwort", "sesame", "pinenut", "jujube"]
VERSIONS = [("지금", None), ("꼴 1\n바깥에 더함", "꼴1"), ("꼴 2\n안쪽 둘레", "꼴2")]

LIGHT = (0xF4, 0xE8, 0xD9, 255)     # 접시 흰색 — 흰 떡이 묻히던 바탕
CREAM = (0xFB, 0xED, 0xD6, 255)     # 말풍선 안
DARK = (0x51, 0x39, 0x30, 255)      # 짙은 나무
WALL = (0xDF, 0x98, 0x98, 255)      # 가판대 분홍 벽
FLOOR = (0xDD, 0xA5, 0x6A, 255)     # 마루
BUTTON = (0xE2, 0xD0, 0xAF, 255)    # 차림표 버튼 면
PAGE = (0x2A, 0x24, 0x20, 255)
INK = (0xFD, 0xFC, 0xF5, 255)


def font(size):
    return ImageFont.truetype(FONT, size)


def load(version_dir, name):
    if version_dir is None:
        sub = "Cake" if name.endswith("_cake") else "Ingredient"
        return Image.open(os.path.join(ART, sub, name + ".png")).convert("RGBA")
    return Image.open(os.path.join(D, version_dir, name + ".png")).convert("RGBA")


def zoom(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def shrink(im, size):
    """게임이 32 그림을 24 자리에 찍을 때처럼 nearest 로 줄인다."""
    return im.resize((size, size), Image.NEAREST)


def label(d, xy, text, size=22, fill=INK):
    d.multiline_text(xy, text, font=font(size), fill=fill, spacing=4)


# ---------- 1. 크기별 ----------
def compare_sizes():
    picks = ["plain_cake", "redbean_cake", "pinenut_jujube_cake", "redbean_mugwort_sesame_cake"]
    Z, LAB, GAP = 4, 150, 8
    sections = [("24px 자리 (0.75배로 줄여 찍힘 · 만들기 줄)", 24, picks), ("32px 자리 (1배 · 진열장 · 말풍선)", 32, picks),
                ("64px 자리 (2배)", 64, picks[:2])]
    half = len(picks) * (32 * Z + GAP) + GAP
    W = LAB + half * 2
    rows = []
    for title, size, names in sections:
        cell = size * Z
        h = 44 + 3 * (cell + GAP)
        sec = Image.new("RGBA", (W, h), PAGE)
        d = ImageDraw.Draw(sec)
        label(d, (10, 8), title, 24)
        for r, (vname, vdir) in enumerate(VERSIONS):
            y = 44 + r * (cell + GAP)
            label(d, (10, y + cell // 2 - 24), vname, 20)
            for side, bg in enumerate((LIGHT, DARK)):
                x0 = LAB + side * half
                d.rectangle([x0, y - GAP // 2, x0 + half - 1, y + cell + GAP // 2 - 1], fill=bg)
                step = (half - GAP) // len(names)
                for i, name in enumerate(names):
                    ic = load(vdir, name)
                    shown = shrink(ic, 24) if size == 24 else (zoom(ic, 2) if size == 64 else ic)
                    sec.alpha_composite(zoom(shown, Z), (x0 + GAP + i * step, y))
        rows.append(sec)
    out = Image.new("RGBA", (W, sum(r.height for r in rows) + 40), PAGE)
    d = ImageDraw.Draw(out)
    label(d, (LAB + 10, 8), "왼쪽 : 접시 흰색 바탕", 20)
    label(d, (LAB + half + 10, 8), "오른쪽 : 짙은 나무 바탕   (모두 4배로 키워 보임)", 20)
    y = 40
    for r in rows:
        out.alpha_composite(r, (0, y))
        y += r.height
    out.save(os.path.join(D, "compare_sizes.png"))


# ---------- 2. 17장 전부 ----------
def compare_all():
    Z, LAB, GAP = 4, 150, 6
    blocks = [("떡 11 (32x32)", CAKES, 32), ("재료 6 (28x28)", INGS, 28)]
    W = LAB + len(CAKES) * (32 * Z + GAP) + GAP
    parts = []
    for title, names, size in blocks:
        cell = size * Z
        h = 40 + 6 * (cell + GAP)
        sec = Image.new("RGBA", (W, h), PAGE)
        d = ImageDraw.Draw(sec)
        label(d, (10, 6), title + "   ·   위 석 줄 접시 흰색 바탕 · 아래 석 줄 짙은 나무 바탕   ·   4배", 22)
        for b, bg in enumerate((LIGHT, DARK)):
            for r, (vname, vdir) in enumerate(VERSIONS):
                y = 40 + (b * 3 + r) * (cell + GAP)
                d.rectangle([LAB, y - GAP // 2, W - 1, y + cell + GAP // 2 - 1], fill=bg)
                label(d, (10, y + cell // 2 - 24), vname, 20)
                for i, name in enumerate(names):
                    sec.alpha_composite(zoom(load(vdir, name), Z), (LAB + GAP + i * (32 * Z + GAP), y))
        parts.append(sec)
    out = Image.new("RGBA", (W, sum(p.height for p in parts)), PAGE)
    y = 0
    for p in parts:
        out.alpha_composite(p, (0, y))
        y += p.height
    out.save(os.path.join(D, "compare_all.png"))


# ---------- 3. 진열장 접시 위 ----------
PLATE_X = (41, 88, 135)      # StallLayout.PlateCenterX
PLATE_BOTTOM = (45, 83)      # StallLayout.PlateCakeBottom — 떡 칸(32x32)의 아래 가운데가 오는 줄
ON_CASE = ["plain_cake", "redbean_cake", "sesame_cake", "pinenut_cake", "mugwort_cake", "redbean_bean_cake"]


def compare_on_case():
    case = Image.open(os.path.join(ART, "Screen", "display_case.png")).convert("RGBA")
    Z, LAB, PAD = 5, 150, 12
    ph = (case.height + PAD * 2) * Z
    W = LAB + (case.width + PAD * 2) * Z
    out = Image.new("RGBA", (W, ph * 3 + 36), PAGE)
    d = ImageDraw.Draw(out)
    label(d, (10, 6), "진열장 접시 위 (5배) — 떡 칸 자리는 셋 다 같다. 그림만 바꿨다", 22)
    for r, (vname, vdir) in enumerate(VERSIONS):
        scene = Image.new("RGBA", (case.width + PAD * 2, case.height + PAD * 2), WALL)
        ImageDraw.Draw(scene).rectangle([0, scene.height - 10, scene.width, scene.height], fill=FLOOR)
        scene.alpha_composite(case, (PAD, PAD))
        for i, name in enumerate(ON_CASE):
            cx, bottom = PLATE_X[i % 3], PLATE_BOTTOM[i // 3]
            scene.alpha_composite(load(vdir, name), (PAD + cx - 16, PAD + bottom - 32))
        y = 36 + r * ph
        out.alpha_composite(zoom(scene, Z), (LAB, y))
        label(d, (10, y + ph // 2 - 24), vname, 20)
    out.save(os.path.join(D, "compare_on_case.png"))


# ---------- 4. 말풍선 · 차림표 버튼 ----------
def nine_slice(src, w, h, left, bottom, right, top):
    """Unity spriteBorder(왼 · 아래 · 오른 · 위)로 9조각 늘이기 (가운데 · 변은 nearest 로 늘인다)."""
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sx = [0, left, src.width - right, src.width]
    sy = [0, top, src.height - bottom, src.height]
    dx = [0, left, w - right, w]
    dy = [0, top, h - bottom, h]
    for j in range(3):
        for i in range(3):
            piece = src.crop((sx[i], sy[j], sx[i + 1], sy[j + 1]))
            size = (dx[i + 1] - dx[i], dy[j + 1] - dy[j])
            if size[0] > 0 and size[1] > 0:
                out.alpha_composite(piece.resize(size, Image.NEAREST), (dx[i], dy[j]))
    return out


def content_box(im):
    return im.getbbox()


def compare_in_bubble():
    bubble_src = Image.open(os.path.join(ART, "UI", "ui_bubble.png")).convert("RGBA")
    button_src = Image.open(os.path.join(ART, "UI", "ui_button.png")).convert("RGBA")
    bubble = nine_slice(bubble_src, 56, 54, 14, 20, 14, 12)        # StallLayout.BubbleIconWidth · BubbleHeight
    tint = Image.new("RGBA", (100, 56), BUTTON)
    button = Image.composite(nine_slice(button_src, 100, 56, 10, 10, 10, 10), Image.new("RGBA", (100, 56)), tint)
    names = ["plain_cake", "redbean_cake", "pinenut_cake", "sesame_cake"]
    Z, LAB = 5, 150
    # 말풍선 아이콘 바닥 : 지금 16 · 꼴 1 은 20 · 꼴 2 는 21 (설계 9-1 의 값 — 떡 가운데가 풍선 안쪽 가운데에 온다)
    icon_bottom = {None: 16, "꼴1": 20, "꼴2": 21}
    bw = len(names) * 64 + 8
    bh = 66
    mw = len(names) * 108 + 8
    mh = 68
    W = LAB + max(bw * Z, mw * 3)
    row_h = bh * Z + mh * 3 + 16
    out = Image.new("RGBA", (W, 70 + 3 * row_h), PAGE)
    d = ImageDraw.Draw(out)
    label(d, (10, 6), "윗줄 : 말풍선 안 (5배 · 아이콘 높이는 꼴에 맞춰 고친 값)\n"
                     "아랫줄 : 차림표 버튼 위 (3배 · 왼쪽 둘 32px, 오른쪽 둘 24px)", 20)
    for r, (vname, vdir) in enumerate(VERSIONS):
        y = 70 + r * row_h
        label(d, (10, y + row_h // 2 - 24), vname, 20)
        scene = Image.new("RGBA", (bw, bh), FLOOR)
        for i, name in enumerate(names):
            x = 8 + i * 64
            scene.alpha_composite(bubble, (x, 6))
            # 풍선 아래 가운데에서 icon_bottom 만큼 위가 아이콘 칸(32)의 바닥
            scene.alpha_composite(load(vdir, name), (x + 28 - 16, 6 + 54 - icon_bottom[vdir] - 32))
        out.alpha_composite(zoom(scene, Z), (LAB, y))
        menu = Image.new("RGBA", (mw, mh), CREAM)
        for i, name in enumerate(names):
            x = 8 + i * 108
            menu.alpha_composite(button, (x, 6))
            size = 32 if i < 2 else 24
            ic = load(vdir, names[i % 2])
            ic = ic if size == 32 else shrink(ic, 24)
            if vdir is None:
                top = 6 + 28 - size // 2                       # 지금 : 칸 가운데 = 줄 가운데
            else:
                box = ic.getbbox()                             # 새 그림 : 떡 가운데 = 줄 가운데 (코드 갈래가 맞출 값)
                top = 6 + 28 - (box[1] + box[3]) // 2
            menu.alpha_composite(ic, (x + 12, top))
        out.alpha_composite(zoom(menu, 3), (LAB, y + bh * Z + 8))
    out.save(os.path.join(D, "compare_in_bubble.png"))


def main():
    compare_sizes()
    compare_all()
    compare_on_case()
    compare_in_bubble()
    print("ok")


if __name__ == "__main__":
    main()

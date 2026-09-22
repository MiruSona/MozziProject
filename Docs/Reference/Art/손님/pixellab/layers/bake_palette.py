# -*- coding: utf-8 -*-
"""
손님 겹 png 16장을 구워 낸다 (몸 1 · 머리 2모양×4색 8 · 윗옷 4색 4 · 표정 3).

split_layers.py 가 낸 겹 png 를 밑감으로 삼아 팔레트만 갈아 끼운다.
런타임에 색을 바꾸지 않고 미리 구워 두기로 했다 (결정 20260922-fc4a258a).

쓰는 법
    python bake_palette.py [출력폴더]
    (출력폴더를 안 적으면 ProjectMozziUnity/.../Resources/Art/Customer/ 에 쓴다)

밑감과 결과의 짝
    red_body_filled_pil.png -> body.png
    red_hair.png            -> hair_01_<색>.png   (짧은 머리)
    purple_hair.png         -> hair_02_<색>.png   (긴 머리)
    red_shirt.png           -> shirt_<색>.png
    red_face.png            -> face_01.png        (기본)
    face_웃음.png           -> face_02.png        (웃음)
    face_시무룩.png         -> face_03.png        (시무룩)

색 고르는 규칙
    2단 명암과 검은 외곽선을 그대로 둔다. 밑감의 색면 하나가 결과의 색면
    하나로 1:1 로 간다 — 밝기를 계산하지 않고 표에 적은 값을 그대로 쓴다.
    계산으로 뽑으면 원래 색과 같은 색을 골랐을 때 원본보다 살짝 어두워진다.
"""
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
DEFAULT_OUT = os.path.join(
    REPO, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Customer")

OUTLINE = (0x03, 0x02, 0x02)        # 모든 겹의 외곽선을 이 먹색으로 맞춘다

# 밑감(짧은 머리)의 색면 — main 은 넓은 면, lit 는 윗머리 광택
SRC_HAIR_01 = dict(main=(0x33, 0x2E, 0x2B), lit=(0x58, 0x53, 0x4F),
                   lit2=(0x92, 0x83, 0x6C), outline=(0x03, 0x02, 0x02))
# 밑감(긴 머리)의 색면 — main 은 넓은 면, shade 는 그늘, gray 는 흘러든 점 셋
SRC_HAIR_02 = dict(main=(0xD9, 0xC3, 0xAB), shade=(0xAC, 0x95, 0x7D),
                   gray=(0x5C, 0x5C, 0x5C), outline=(0x03, 0x03, 0x02))
SRC_SHIRT = dict(main=(0xCD, 0x3A, 0x31), shade=(0xA3, 0x2B, 0x23),
                 outline=(0x03, 0x02, 0x02))

# 머리색 네 벌 : 밝음 / 기본 / 어둠.
# 짧은 머리는 기본+밝음(광택), 긴 머리는 기본+어둠(그늘)을 쓴다.
HAIR_PALETTE = {
    "black":  dict(light=(0x58, 0x53, 0x4F), base=(0x33, 0x2E, 0x2B), dark=(0x22, 0x20, 0x1E)),
    "brown":  dict(light=(0x9E, 0x6C, 0x43), base=(0x7A, 0x4E, 0x2C), dark=(0x5A, 0x38, 0x20)),
    "orange": dict(light=(0xE0, 0x96, 0x5F), base=(0xC4, 0x6E, 0x3A), dark=(0x96, 0x52, 0x2A)),
    "purple": dict(light=(0xA3, 0x7D, 0xC6), base=(0x7E, 0x5A, 0xA6), dark=(0x5E, 0x3F, 0x80)),
}

# 윗옷색 네 벌 : 기본 / 그늘. 떡 그림(채도 50~70·명도 45~90)과 같은 자리에 둔다.
SHIRT_PALETTE = {
    "red":    dict(base=(0xCD, 0x3A, 0x31), shade=(0xA3, 0x2B, 0x23)),
    "green":  dict(base=(0x5A, 0x9E, 0x54), shade=(0x44, 0x79, 0x3F)),
    "blue":   dict(base=(0x3C, 0x74, 0xB5), shade=(0x2C, 0x58, 0x8A)),
    "yellow": dict(base=(0xE3, 0xBC, 0x5C), shade=(0xB0, 0x8F, 0x3F)),
}


def load(name):
    return Image.open(os.path.join(HERE, name)).convert("RGBA")


def remap(img, mapping):
    """색면을 표대로 1:1 로 갈아 끼운다. 표에 없는 색은 그대로 둔다."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            c = px[x, y]
            if c[3] and c[:3] in mapping:
                px[x, y] = mapping[c[:3]] + (c[3],)
    return out


def bake_hair_01(src, color):
    pal = HAIR_PALETTE[color]
    return remap(src, {
        SRC_HAIR_01["main"]: pal["base"],
        SRC_HAIR_01["lit"]: pal["light"],
        SRC_HAIR_01["lit2"]: pal["light"],
        SRC_HAIR_01["outline"]: OUTLINE,
    })


def drop_specks(img, min_size=8):
    """붙어 있지 않은 자잘한 점 덩어리를 지운다.

    긴 머리 겹에는 어깨 옆에 세 점짜리 조각이 떨어져 있다. 나누기에서
    옷 색면이 머리로 잘못 간 자리라 그대로 두면 허공에 점이 뜬다.
    """
    w, h = img.size
    px = img.load()
    seen = set()
    out = img.copy()
    op = out.load()
    for y in range(h):
        for x in range(w):
            if not px[x, y][3] or (x, y) in seen:
                continue
            stack_, comp = [(x, y)], []
            seen.add((x, y))
            while stack_:
                ax, ay = stack_.pop()
                comp.append((ax, ay))
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        nx, ny = ax + dx, ay + dy
                        if (0 <= nx < w and 0 <= ny < h
                                and (nx, ny) not in seen and px[nx, ny][3]):
                            seen.add((nx, ny))
                            stack_.append((nx, ny))
            if len(comp) < min_size:
                for p in comp:
                    op[p] = (0, 0, 0, 0)
    return out


def bake_hair_02(src, color):
    pal = HAIR_PALETTE[color]
    src = drop_specks(src)
    return remap(src, {
        SRC_HAIR_02["main"]: pal["base"],
        SRC_HAIR_02["shade"]: pal["dark"],
        SRC_HAIR_02["gray"]: pal["dark"],
        SRC_HAIR_02["outline"]: OUTLINE,
    })


def bake_shirt(src, color):
    pal = SHIRT_PALETTE[color]
    return remap(src, {
        SRC_SHIRT["main"]: pal["base"],
        SRC_SHIRT["shade"]: pal["shade"],
        SRC_SHIRT["outline"]: OUTLINE,
    })


def bake_all():
    """구운 겹 열여섯 장을 {파일이름: 그림} 으로 돌려준다."""
    body = remap(load("red_body_filled_pil.png"), {SRC_HAIR_01["outline"]: OUTLINE})
    hair01 = load("red_hair.png")
    hair02 = load("purple_hair.png")
    shirt = load("red_shirt.png")

    made = {"body.png": body}
    for color in HAIR_PALETTE:
        made["hair_01_%s.png" % color] = bake_hair_01(hair01, color)
        made["hair_02_%s.png" % color] = bake_hair_02(hair02, color)
    for color in SHIRT_PALETTE:
        made["shirt_%s.png" % color] = bake_shirt(shirt, color)
    made["face_01.png"] = remap(load("red_face.png"), {SRC_HAIR_01["outline"]: OUTLINE})
    made["face_02.png"] = remap(load("face_웃음.png"), {SRC_HAIR_01["outline"]: OUTLINE})
    made["face_03.png"] = remap(load("face_시무룩.png"), {SRC_HAIR_01["outline"]: OUTLINE})
    return made


# ------------------------------------------------------------------ 검사·미리보기

def stack(*imgs):
    out = Image.new("RGBA", imgs[0].size, (0, 0, 0, 0))
    for im in imgs:
        out.alpha_composite(im)
    return out


def diff_count(a, b):
    pa, pb = a.load(), b.load()
    return sum(1 for y in range(a.height) for x in range(a.width) if pa[x, y] != pb[x, y])


def alpha_diff_count(a, b):
    """색은 빼고 「어느 칸이 차 있나」만 견준다 — 좌표계가 같은지 보는 검사다."""
    pa, pb = a.load(), b.load()
    return sum(1 for y in range(a.height) for x in range(a.width)
               if bool(pa[x, y][3]) != bool(pb[x, y][3]))


def check(made):
    """되돌림 검사 — 구운 겹을 쌓으면 split_layers 의 조합 예와 같아야 한다."""
    rows = []
    base = stack(made["body.png"], made["hair_01_black.png"],
                 made["shirt_red.png"], made["face_01.png"])
    ref = load("mix_검은머리_빨간옷.png")
    rows.append(("검은머리·빨간옷·기본표정", "mix_검은머리_빨간옷.png",
                 diff_count(base, ref), alpha_diff_count(base, ref)))

    pairs = [
        ("hair_01_brown.png", "shirt_green.png", "face_01.png", "mix_갈색머리_초록옷.png"),
        ("hair_01_orange.png", "shirt_blue.png", "face_01.png", "mix_주황머리_푸른옷.png"),
        ("hair_02_purple.png", "shirt_red.png", "face_01.png", "mix_다른머리모양.png"),
        ("hair_01_black.png", "shirt_red.png", "face_02.png", "mix_표정_웃음.png"),
        ("hair_01_black.png", "shirt_red.png", "face_03.png", "mix_표정_시무룩.png"),
    ]
    for hair, shirt, face, refname in pairs:
        got = stack(made["body.png"], made[hair], made[shirt], made[face])
        r = load(refname)
        rows.append(("%s+%s+%s" % (hair, shirt, face), refname,
                     diff_count(got, r), alpha_diff_count(got, r)))
    return rows


COMBOS = [
    ("hair_01_black", "shirt_red", "face_01"),
    ("hair_01_brown", "shirt_green", "face_02"),
    ("hair_01_orange", "shirt_blue", "face_03"),
    ("hair_01_purple", "shirt_yellow", "face_01"),
    ("hair_02_black", "shirt_yellow", "face_02"),
    ("hair_02_brown", "shirt_blue", "face_03"),
    ("hair_02_orange", "shirt_green", "face_01"),
    ("hair_02_purple", "shirt_red", "face_02"),
]

ORDER = (["body.png"]
         + ["hair_01_%s.png" % c for c in ("black", "brown", "orange", "purple")]
         + ["hair_02_%s.png" % c for c in ("black", "brown", "orange", "purple")]
         + ["shirt_%s.png" % c for c in ("red", "green", "blue", "yellow")]
         + ["face_01.png", "face_02.png", "face_03.png"])


def preview(made, path):
    """겹 열여섯 장(2배)과 조합 예 여덟(4배)을 한 장에 늘어놓는다."""
    bg, grid, label = (0x20, 0x22, 0x26), (0x2E, 0x31, 0x36), (0xDD, 0xDD, 0xDD)
    cw, ch, pad = 96, 128, 6                 # 겹 칸 (2배)
    mw, mh = 192, 256                        # 조합 칸 (4배)
    cols, mcols = 8, 4
    top = 18
    layer_h = top + 2 * (ch + 14 + pad)
    mix_h = top + 2 * (mh + 14 + pad)
    w = cols * (cw + pad) + pad
    canvas = Image.new("RGB", (w, layer_h + mix_h + pad), bg)
    d = ImageDraw.Draw(canvas)
    d.text((pad, 4), "layers x2  (48x64 canvas)", fill=label)

    for i, name in enumerate(ORDER):
        cx = pad + (i % cols) * (cw + pad)
        cy = top + (i // cols) * (ch + 14 + pad)
        d.rectangle([cx, cy, cx + cw - 1, cy + ch - 1], fill=grid)
        canvas.paste(made[name].resize((cw, ch), Image.NEAREST), (cx, cy),
                     made[name].resize((cw, ch), Image.NEAREST))
        d.text((cx, cy + ch + 2), name[:-4], fill=label)

    my0 = layer_h + pad
    d.text((pad, my0 - 14), "mixes x4", fill=label)
    for i, (hair, shirt, face) in enumerate(COMBOS):
        img = stack(made["body.png"], made[hair + ".png"],
                    made[shirt + ".png"], made[face + ".png"]).resize((mw, mh), Image.NEAREST)
        cx = pad + (i % mcols) * (mw + pad)
        cy = my0 + (i // mcols) * (mh + 14 + pad)
        d.rectangle([cx, cy, cx + mw - 1, cy + mh - 1], fill=grid)
        canvas.paste(img, (cx, cy), img)
        d.text((cx, cy + mh + 2), "%s + %s + %s" % (hair, shirt[6:], face[5:]), fill=label)

    canvas.save(path)
    return canvas.size


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_OUT
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    made = bake_all()
    for name in ORDER:
        made[name].save(os.path.join(out_dir, name))
    print("겹 %d 장 저장 -> %s" % (len(ORDER), out_dir))

    print("\n[불투명 bbox]")
    for name in ORDER:
        print("  %-20s %s" % (name[:-4], made[name].getbbox()))

    print("\n[되돌림 검사] 겹을 쌓아 split_layers 의 조합 예와 견준다")
    for what, ref, dc, ac in check(made):
        print("  %-46s vs %-26s 색다름 %4d · 자리다름 %d" % (what, ref, dc, ac))

    size = preview(made, os.path.join(HERE, "preview_customer_set.png"))
    print("\n미리보기 %dx%d -> preview_customer_set.png" % size)


if __name__ == "__main__":
    main()

# -*- coding: utf-8 -*-
"""
손님 새 화풍 시안 석 장을 Docs/Reference/Art/시안/ 에 낸다.

    python make_previews.py

    2026-10-03-손님-새화풍-조합.png      주인 B안 · 지금 손님 · 새 손님 12명
    2026-10-03-손님-새화풍-파츠표.png    겹별 파츠 표
    2026-10-03-손님-새화풍-화면비교.png  가판대 배경 위 — 왼쪽 지금 손님, 오른쪽 새 손님 + 주인

겹은 bake_parts.py 가 굽는 것을 그대로 쓴다 (파일을 읽지 않고 함수를 부른다).
"""
import os

from PIL import Image, ImageDraw, ImageFont

import bake_parts as bp

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
MOCK = os.path.join(REPO, "Docs", "Reference", "Art", "시안")
OLD = os.path.join(REPO, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Customer")
STALL_BG = os.path.join(REPO, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art",
                        "Screen", "stall_bg.png")

BG = (0xEE, 0xE8, 0xDC)
CELL = (0xE2, 0xDA, 0xCA)
INK = (0x33, 0x2E, 0x3A)
FONT = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 13)
FONT_B = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 15)

# 조합 예 12명 : (머리, 머리색, 윗옷, 옷색, 아래옷, 아래색, 표정)
MIXES = [
    ("hair_01", "black", "shirt_01", "red", "bottom_01", "navy", "face_01"),
    ("hair_02", "brown", "shirt_03", "yellow", "bottom_02", "navy", "face_02"),
    ("hair_03", "black", "shirt_02", "blue", "bottom_02", "beige", "face_01"),
    ("hair_04", "orange", "shirt_01", "green", "bottom_02", "gray", "face_02"),
    ("hair_05", "brown", "shirt_03", "red", "bottom_01", "gray", "face_01"),
    ("hair_06", "black", "shirt_03", "blue", "bottom_01", "beige", "face_01"),
    ("hair_06", "gray", "shirt_03", "green", "bottom_01", "navy", "face_02"),
    ("hair_05", "gray", "shirt_01", "blue", "bottom_01", "beige", "face_03"),
    ("hair_01", "orange", "shirt_02", "yellow", "bottom_01", "gray", "face_02"),
    ("hair_02", "black", "shirt_01", "yellow", "bottom_02", "gray", "face_03"),
    ("hair_03", "brown", "shirt_03", "green", "bottom_01", "navy", "face_02"),
    ("hair_04", "black", "shirt_02", "red", "bottom_02", "navy", "face_01"),
]


def mix(made, m):
    hair, hc, shirt, sc, bottom, bc, face = m
    return bp.stack(made["body"], made["%s_%s" % (bottom, bc)], made["%s_%s" % (shirt, sc)],
                    made[face], made["%s_%s" % (hair, hc)])


def old_customer(hair="hair_01_brown", shirt="shirt_red", face="face_01"):
    out = Image.new("RGBA", (48, 64), (0, 0, 0, 0))
    for name in ("body", hair, shirt, face):
        out.alpha_composite(Image.open(os.path.join(OLD, name + ".png")).convert("RGBA"))
    return out


def up(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def sheet_mix(made, owner):
    k = 5
    cw, ch = 64 * k + 12, 80 * k + 40
    cols = 7
    w, h = cols * cw + 16, 2 * ch + 44
    c = Image.new("RGBA", (w, h), BG + (255,))
    d = ImageDraw.Draw(c)
    d.text((10, 8), "손님 새 화풍 — 파츠를 섞은 12명 (64×80 · 5배)  /  왼쪽 : 주인 1차 B안 · 지금 손님(48×64)",
           fill=INK, font=FONT_B)
    cells = [("주인 1차 B안", owner)] + [(None, m) for m in MIXES[:6]] \
        + [("지금 손님", old_customer())] + [(None, m) for m in MIXES[6:]]
    for i, (label, item) in enumerate(cells):
        x = 8 + (i % cols) * cw
        y = 36 + (i // cols) * ch
        if label is None:
            d.rectangle([x, y, x + cw - 8, y + ch - 8], fill=CELL)
            im = mix(made, item)
            label = "%s %s\n%s %s · %s %s · %s" % (
                item[0][5:], item[1], item[2][6:], item[3], item[4][7:], item[5], item[6][5:])
        else:
            d.rectangle([x, y, x + cw - 8, y + ch - 8], outline=INK)
            im = item
        big = up(im, k)
        # 발끝을 같은 줄에 맞춘다 (새 손님·주인은 y=76, 지금 손님은 y=59 가 바닥)
        foot = im.getbbox()[3]
        c.alpha_composite(big, (x + (cw - 8 - big.width) // 2, y + 80 * k + 2 - foot * k + (80 - 77) * k))
        d.multiline_text((x + 4, y + 80 * k + 2), label, fill=INK, font=FONT, spacing=0)
    c.convert("RGB").save(os.path.join(MOCK, "2026-10-03-손님-새화풍-조합.png"))


def sheet_parts(src, made):
    k = 3
    cw, ch = 64 * k + 8, 80 * k + 24
    rows = [
        ("몸 · 표정 (body · face_01 기본 / 02 웃음 / 03 시무룩) — 오른쪽 셋은 몸에 얹은 모습",
         [("body", made["body"]), ("face_01", made["face_01"]), ("face_02", made["face_02"]),
          ("face_03", made["face_03"])]
         + [("body+" + f, bp.stack(made["body"], made[f])) for f in ("face_01", "face_02", "face_03")]),
        ("머리 모양 6 (src/ 기준색 파랑 6단 — 여기서 색만 갈아 끼운다)",
         [(n, src[n]) for n in bp.HAIRS]),
        ("머리색 4 (black · brown · orange · gray)",
         [("hair_01_" + c, made["hair_01_" + c]) for c in bp.HAIR_COLORS]
         + [("hair_05_" + c, made["hair_05_" + c]) for c in ("brown", "gray")]
         + [("hair_03_" + c, made["hair_03_" + c]) for c in ("black",)]),
        ("윗옷 3 (후드 · 반팔 · 가디건) × 색 4 (red · green · blue · yellow)",
         [(n, src[n]) for n in bp.SHIRTS]
         + [("shirt_01_red", made["shirt_01_red"]), ("shirt_02_green", made["shirt_02_green"]),
            ("shirt_03_yellow", made["shirt_03_yellow"]), ("shirt_03_blue", made["shirt_03_blue"])]),
        ("아래옷 2 (긴 바지 · 치마) × 색 3 (navy · beige · gray)",
         [(n, src[n]) for n in bp.BOTTOMS]
         + [("bottom_01_" + c, made["bottom_01_" + c]) for c in bp.BOTTOM_COLORS]
         + [("bottom_02_" + c, made["bottom_02_" + c]) for c in ("navy", "beige")]),
    ]
    cols = 7
    w, h = cols * cw + 16, len(rows) * (ch + 26) + 80
    c = Image.new("RGBA", (w, h), BG + (255,))
    d = ImageDraw.Draw(c)
    d.text((10, 8), "손님 새 화풍 — 파츠표 (모든 겹 64×80 같은 캔버스 · 3배)   겹 순서 : body → bottom → shirt → face → hair",
           fill=INK, font=FONT_B)
    y = 36
    for title, items in rows:
        d.text((10, y), title, fill=INK, font=FONT)
        y += 20
        for i, (label, im) in enumerate(items):
            x = 8 + i * cw
            d.rectangle([x, y, x + cw - 6, y + 80 * k], fill=CELL)
            c.alpha_composite(up(im, k), (x + 1, y))
            d.text((x + 2, y + 80 * k + 2), label, fill=INK, font=FONT)
        y += ch + 6
    # 램프 견본
    d.text((10, y), "6단 램프 (outline · deep · shade · base · light · shine) : 기준색 → 머리 4 → 윗옷 4 → 아래옷 3",
           fill=INK, font=FONT)
    x = 10
    ramps = [bp.KEY] + list(bp.HAIR_COLORS.values()) + list(bp.SHIRT_COLORS.values()) \
        + list(bp.BOTTOM_COLORS.values())
    for r in ramps:
        for j, col in enumerate(r):
            d.rectangle([x + j * 12, y + 20, x + j * 12 + 11, y + 34], fill=col)
        x += 6 * 12 + 10
    c.convert("RGB").save(os.path.join(MOCK, "2026-10-03-손님-새화풍-파츠표.png"))


def sheet_screen(made, owner):
    k = 2
    bg = Image.open(STALL_BG).convert("RGBA").crop((0, 80, 360, 720))
    foot_y = 432
    centers = [2 + 34 + 72 * i for i in range(5)]

    left = bg.copy()
    olds = [("hair_01_black", "shirt_red", "face_01"), ("hair_02_brown", "shirt_blue", "face_02"),
            ("hair_01_orange", "shirt_yellow", "face_01"), ("hair_02_purple", "shirt_green", "face_03"),
            ("hair_01_brown", "shirt_red", "face_02")]
    for cx, o in zip(centers, olds):
        im = old_customer(*o)
        left.alpha_composite(im, (cx - 24, foot_y - im.getbbox()[3]))

    right = bg.copy()
    # 주인 : 진열장 오른쪽 옆, 카운터 뒤 (얹은 뒤 카운터 앞판을 다시 덮는다)
    right.alpha_composite(owner, (330 - 32, 332 - owner.getbbox()[3]))
    right.alpha_composite(bg.crop((0, 306, 360, 400)), (0, 306))
    for cx, m in zip(centers, [MIXES[0], MIXES[1], MIXES[5], MIXES[3], MIXES[7]]):
        im = mix(made, m)
        right.alpha_composite(im, (cx - 32, foot_y - im.getbbox()[3]))

    w, h = 360 * k * 2 + 36, 640 * k + 44
    c = Image.new("RGBA", (w, h), BG + (255,))
    d = ImageDraw.Draw(c)
    d.text((12, 8), "지금 손님 (48×64)", fill=INK, font=FONT_B)
    d.text((12 + 360 * k + 12, 8), "새 손님 (64×80) + 주인 1차 B안 — 발 y=432 그대로", fill=INK, font=FONT_B)
    c.alpha_composite(up(left, k), (12, 34))
    c.alpha_composite(up(right, k), (12 + 360 * k + 12, 34))
    c.convert("RGB").save(os.path.join(MOCK, "2026-10-03-손님-새화풍-화면비교.png"))


def main():
    base, src, made, report = bp.bake(save=False)
    owner = Image.open(os.path.join(HERE, "ref", "owner_B.png")).convert("RGBA")
    sheet_mix(made, owner)
    sheet_parts(src, made)
    sheet_screen(made, owner)
    print("시안 3장 ->", MOCK)


if __name__ == "__main__":
    main()

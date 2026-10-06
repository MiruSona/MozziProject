"""반복 08 그림 시안 견줌판을 만든다.

PixelLab 에서 받은 raw/ 그림을 지금 게임 화면(캡처 · 배경) 위에 올리고,
정수배로 키워 후보 번호 글자를 붙인다. 새로 칠하는 점은 없다
(자르기 · 배치 · 정수배 확대 · 글자 · 회색 띠 바탕만).

저장소 뿌리에서 : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/시안/반복08-v1/build_compare.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
OUT = os.path.join(HERE, "견줌")
os.makedirs(OUT, exist_ok=True)

FARM = os.path.join(REPO, "Docs/Reference/Art/점검/2026-10-05/반복07/unity/farm_after_kitchen.png")
STALL = os.path.join(REPO, "Docs/Reference/Art/점검/2026-10-05/unity_after/final/stall.png")
KV3 = os.path.join(REPO, "Docs/Reference/Art/정식/주방/v3/out")
KIT_FULL = os.path.join(REPO, "Docs/Reference/Art/정식/주방/v3/견줌/주방_전체_1x.png")

FONT = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 22)
FONT_S = ImageFont.truetype(r"C:\Windows\Fonts\malgun.ttf", 15)
BG = (40, 36, 34, 255)


def raw(group, name):
    fixed = os.path.join(HERE, group, "손질", name + ".png")   # 바탕 한 색만 걷은 판이 있으면 그것
    src = fixed if os.path.exists(fixed) else os.path.join(HERE, group, "raw", name + ".png")
    im = Image.open(src).convert("RGBA")
    return im.crop(im.getbbox())


def up(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def place(base, sprite, x, y_bottom):
    """sprite 의 아래 왼쪽을 (x, y_bottom) 에 맞춰 얹는다."""
    base.alpha_composite(sprite, (x, y_bottom - sprite.height))


def label_panel(im, text, sub=None):
    bar = 34 if sub is None else 54
    c = Image.new("RGBA", (im.width, im.height + bar), BG)
    d = ImageDraw.Draw(c)
    d.text((8, 3), text, font=FONT, fill=(255, 230, 120, 255))
    if sub:
        d.text((8, 32), sub, font=FONT_S, fill=(230, 230, 230, 255))
    c.alpha_composite(im, (0, bar))
    return c


def hstack(ims, gap=10):
    w = sum(i.width for i in ims) + gap * (len(ims) - 1)
    h = max(i.height for i in ims)
    c = Image.new("RGBA", (w, h), BG)
    x = 0
    for i in ims:
        c.alpha_composite(i, (x, 0))
        x += i.width + gap
    return c


def vstack(ims, gap=16, title=None):
    w = max(i.width for i in ims)
    th = 44 if title else 0
    h = sum(i.height for i in ims) + gap * (len(ims) - 1) + th
    c = Image.new("RGBA", (w, h), BG)
    if title:
        ImageDraw.Draw(c).text((8, 6), title, font=FONT, fill=(255, 255, 255, 255))
    y = th
    for i in ims:
        c.alpha_composite(i, (0, y))
        y += i.height + gap
    return c


def split_items(im):
    """세로로 빈 열을 경계로 낱개를 나눈다 (장식 묶음용)."""
    a = im.getchannel("A")
    cols = [any(a.getpixel((x, y)) for y in range(im.height)) for x in range(im.width)]
    items, start = [], None
    for x, on in enumerate(cols + [False]):
        if on and start is None:
            start = x
        if not on and start is not None:
            piece = im.crop((start, 0, x, im.height))
            items.append(piece.crop(piece.getbbox()))
            start = None
    return [i for i in items if i.width > 6]


saved = {}


def save(img, name):
    p = os.path.join(OUT, name)
    img.save(p)
    saved[name] = img
    print("저장", p, img.size)


# ---------- 1 · 2 농장 장치 ----------
farm = Image.open(FARM).convert("RGBA")
CROP = (250, 455, 540, 720)          # 울타리 오른쪽 위 구석 둘레
COLLECT_BR = (500, 585)              # 수집기 아래 오른쪽 끝 (울타리 안 오른쪽 위 구석)
FEED_BR = (420, 585)                 # 공급기는 그 왼쪽 옆 (짝 미리보기)


def farm_panel(sprites):
    f = farm.copy()
    for spr, (xr, yb) in sprites:
        place(f, spr, xr - spr.width, yb)
    return up(f.crop(CROP), 2)


for g, key, br in (("1_수집기", "1", COLLECT_BR), ("2_먹이공급기", "2", COLLECT_BR)):
    panels = [label_panel(up(farm.crop(CROP), 2), "지금")]
    for s in "ABC":
        panels.append(label_panel(farm_panel([(raw(g, f"{key}-{s}"), br)]), f"{key}-{s}"))
    save(vstack([hstack(panels)], title=f"{key}번 {'수집기' if key == '1' else '먹이 공급기'} — 농장 울타리 안 오른쪽 위 구석 (×2, 그림은 1px 그대로)"), f"{key}_{g.split('_')[1]}_견줌_x2.png")

# 짝 미리보기 : 권장 묶음
pair = [label_panel(farm_panel([(raw("1_수집기", f"1-{a}"), COLLECT_BR), (raw("2_먹이공급기", f"2-{b}"), FEED_BR)]), f"1-{a} + 2-{b}")
        for a, b in (("A", "A"), ("A", "C"), ("B", "B"), ("C", "A"))]
save(vstack([hstack(pair)], title="수집기 + 먹이 공급기 짝 미리보기 (×2) — 둘이 한 벌로 보이나"), "1-2_짝_x2.png")

# ---------- 3 · 4 주방 ----------
kbg = Image.open(os.path.join(KV3, "kitchen_bg.png")).convert("RGBA")
kfull = Image.open(KIT_FULL).convert("RGBA")
MILL_BL = (405, 885)   # 맷돌 임시 자리 (빈 마루 — 떡메 자루 오른쪽, 빚기판 위). 정식 자리는 코드 갈래가 정한다

mill_panels = [label_panel(kfull.crop((0, 640, 540, 1100)), "지금")]
for s in "ABC":
    k = kfull.copy()
    place(k, raw("3_맷돌", f"3-{s}"), *MILL_BL)
    mill_panels.append(label_panel(k.crop((0, 640, 540, 1100)), f"3-{s}"))
strip = hstack([label_panel(up(raw("3_맷돌", f"3-{s}"), 3), f"3-{s} (×3)") for s in "ABC"])
save(vstack([hstack(mill_panels), strip], title="3번 맷돌 — 주방 작업대 (×1 화면 + ×3 낱개). 자리는 임시"), "3_맷돌_견줌.png")

# 서랍장 : (가) 지금 자리에 1px 그대로 (나) ×2 로 키워 뒤 테이블을 가린 미리보기
frame = Image.open(os.path.join(KV3, "kitchen_drawer_frame.png")).convert("RGBA")
KCROP = (150, 380, 540, 800)
now = kbg.copy()
now.alpha_composite(frame, (292, 470))
cell = Image.open(os.path.join(KV3, "kitchen_drawer_cell.png")).convert("RGBA")
for r in range(3):
    for c in range(4):
        now.alpha_composite(cell, (302 + c * 56, 480 + r * 92))
drawer_rows = []
row1 = [label_panel(up(now.crop(KCROP), 2), "지금", "지금 서랍장 (뒤에 테이블)")]
row2 = [label_panel(up(now.crop(KCROP), 2), "지금", "")]
for s in "AB":
    d = raw("4_서랍장", f"4-{s}")
    k1 = kbg.copy()
    place(k1, d, 292, 754)                       # 지금 서랍장 아래 왼쪽 자리에 1px 그대로
    row1.append(label_panel(up(k1.crop(KCROP), 2), f"4-{s}", "1px 그대로 (지금 자리) — 작다"))
    k2 = kbg.copy()
    d2 = up(d, 2)
    place(k2, d2, 540 - d2.width - 4, 760)      # ×2 로 키워 테이블 자리까지 덮음
    row2.append(label_panel(up(k2.crop(KCROP), 2), f"4-{s}", "×2 크기 미리보기 · 테이블 가림"))
save(vstack([hstack(row1), hstack(row2)], title="4번 입체 서랍장 (×2 확대) — 윗줄: 1px 그대로 · 아랫줄: 그림을 ×2 로 키워 뒤 테이블을 가린 느낌"), "4_서랍장_견줌_x2.png")

# ---------- 5 가판대 장식 ----------
stall = Image.open(STALL).convert("RGBA")
spots = {
    # 묶음마다 [ (자리 이름, x 왼쪽, y 아래) ] — 낱개 순서대로. 자리는 임시
    "5-A": [(392, 785), (474, 785)],                      # (화분+바구니 의자 붙어 나옴) · 칠판 간판 → 오른쪽 빈 마루
    "5-B": [(14, 335), (175, 330), (400, 330)],            # 액자 → 왼쪽 벽 위, 선반 → 칠판 위, 등나무 접시 → 문 위
    "5-C": [(385, 785), (462, 785)],                      # 쌀자루 상자 · 곁탁자(떡 유리 덮개) → 오른쪽 빈 마루
}
stall_panels = [label_panel(up(stall.crop((0, 200, 540, 820)), 1), "지금")]
for s in "ABC":
    items = split_items(raw("5_가판대장식", f"5-{s}"))
    st = stall.copy()
    for spr, (x, yb) in zip(items, spots[f"5-{s}"]):
        place(st, spr, x, yb)
    stall_panels.append(label_panel(st.crop((0, 200, 540, 820)), f"5-{s}", f"낱개 {len(items)}개 · 자리는 임시"))
strip5 = hstack([label_panel(up(raw("5_가판대장식", f"5-{s}"), 2), f"5-{s} (×2)") for s in "ABC"])
save(vstack([hstack(stall_panels), strip5], title="5번 가판대 장식 — 가판대 화면 (×1) + 낱개 (×2)"), "5_가판대장식_견줌.png")

# ---------- 모음판 ----------
rows = []
for g, key, s_ in (("1_수집기", "1", 3), ("2_먹이공급기", "2", 3), ("3_맷돌", "3", 2), ("4_서랍장", "4", 2), ("5_가판대장식", "5", 2)):
    names = ["A", "B", "C"] if key != "4" else ["A", "B"]
    rows.append(hstack([label_panel(up(raw(g, f"{key}-{n}"), s_), f"{key}-{n}") for n in names], gap=24))
left = vstack(rows, title="반복 08 그림 시안 모음 — 번호로 골라 주세요 (1·2 는 ×3, 3·4·5 는 ×2)")
right = vstack([saved["1-2_짝_x2.png"].resize((saved["1-2_짝_x2.png"].width // 2, saved["1-2_짝_x2.png"].height // 2), Image.NEAREST),
                saved["4_서랍장_견줌_x2.png"].resize((saved["4_서랍장_견줌_x2.png"].width // 2, saved["4_서랍장_견줌_x2.png"].height // 2), Image.NEAREST)],
               title="화면 위 모습 (짝 · 서랍장, ×1)")
save(hstack([left, right], gap=30), "00_모음판.png")

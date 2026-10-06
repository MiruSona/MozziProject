# 반복 08 정식 그림 견줌판 — out/ 을 지금 화면 그림 위에 올린다. 자리 · 확대 · 글자만, 새로 칠하지 않는다.
# 돌리기 : 저장소 뿌리에서  Tools/ArtTool/.venv/Scripts/python.exe "Docs/Reference/Art/정식/반복08/build_compare.py"
from PIL import Image, ImageDraw, ImageFont
import os

V = os.path.dirname(os.path.abspath(__file__))
O = os.path.join(V, "out")
C = os.path.join(V, "견줌")
os.makedirs(C, exist_ok=True)
REPO = os.path.abspath(os.path.join(V, "..", "..", "..", "..", ".."))
RES = os.path.join(REPO, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")
KV3 = os.path.join(REPO, "Docs", "Reference", "Art", "정식", "주방", "v3")
FARM = os.path.join(REPO, "Docs/Reference/Art/점검/2026-10-05/반복07/unity/farm_after_kitchen.png")
STALL = os.path.join(REPO, "Docs/Reference/Art/점검/2026-10-05/unity_after/final/stall.png")
FONT = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 20)
FONT_S = ImageFont.truetype(r"C:\Windows\Fonts\malgun.ttf", 14)
BG = (40, 36, 34, 255)


def o(n):
    return Image.open(os.path.join(O, n)).convert("RGBA")


def up(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def label(im, t, sub=""):
    bar = 30 if not sub else 50
    c = Image.new("RGBA", (im.width, im.height + bar), BG)
    d = ImageDraw.Draw(c)
    d.text((6, 3), t, font=FONT, fill=(255, 230, 120, 255))
    if sub:
        d.text((6, 28), sub, font=FONT_S, fill=(230, 230, 230, 255))
    c.alpha_composite(im, (0, bar))
    return c


def hstack(ims, gap=10):
    c = Image.new("RGBA", (sum(i.width for i in ims) + gap * (len(ims) - 1), max(i.height for i in ims)), BG)
    x = 0
    for i in ims:
        c.alpha_composite(i, (x, 0))
        x += i.width + gap
    return c


def vstack(ims, gap=12):
    c = Image.new("RGBA", (max(i.width for i in ims), sum(i.height for i in ims) + gap * (len(ims) - 1)), BG)
    y = 0
    for i in ims:
        c.alpha_composite(i, (0, y))
        y += i.height + gap
    return c


def gif(frames, name, s, ms, bg=(206, 200, 214, 255)):
    out = []
    for f in frames:
        b = Image.new("RGBA", f.size, bg)
        b.alpha_composite(f)
        out.append(up(b, s).convert("P", palette=Image.ADAPTIVE))
    out[0].save(os.path.join(C, name), save_all=True, append_images=out[1:], duration=ms, loop=0, disposal=2)
    strip = hstack([up(Image.alpha_composite(Image.new("RGBA", f.size, bg), f), s) for f in frames], gap=4)
    strip.save(os.path.join(C, name.replace(".gif", "_띠.png")))


# ---------- 농장 ----------
farm = Image.open(FARM).convert("RGBA")
col0, fed0 = o("Farm/farm_collector_0.png"), o("Farm/farm_feeder_0.png")
# 오른쪽 울타리(통나무 x 501~517) 한가운데 위쪽 칸 둘(y 544~608)을 대신한다. 오른쪽 끝 538, 아래 608
COL_POS = (538 - col0.width, 608 - col0.height)
# 왼쪽 울타리(통나무 x 21~37) 같은 높이. 왼쪽 끝 2
FED_POS = (2, 608 - fed0.height)
f = farm.copy()
f.alpha_composite(col0, COL_POS)
f.alpha_composite(fed0, FED_POS)
whole = hstack([label(farm, "지금"), label(f, "반복 08", "수집기 = 오른쪽 울타리 · 공급기 = 왼쪽 울타리")])
whole.save(os.path.join(C, "농장_전체_1x.png"))
crop = (0, 440, 540, 700)
hstack([label(up(farm.crop(crop), 2), "지금 (×2)"), label(up(f.crop(crop), 2), "반복 08 (×2)", "울타리 칸은 아직 안 지웠다 — 코드가 그 칸을 빼면 기계만 남는다")]).save(os.path.join(C, "농장_울타리_2x.png"))
gif([o(f"Farm/farm_collector_{i}.png") for i in range(7)], "수집기_3x.gif", 3, 110, bg=(191, 207, 125, 255))
gif([o(f"Farm/farm_feeder_{i}.png") for i in range(4)], "공급기_3x.gif", 3, 140, bg=(191, 207, 125, 255))
# 장치 v2 : 옛 판(raw/옛, 75x61 · 64x80) 과 새 판을 같은 자리에 나란히
OLD = os.path.join(V, "raw", "옛")
oc = Image.open(os.path.join(OLD, "farm_collector_0.png")).convert("RGBA")
of = Image.open(os.path.join(OLD, "farm_feeder_0.png")).convert("RGBA")
f_old = farm.copy()
f_old.alpha_composite(oc, (538 - oc.width, 608 - oc.height))
f_old.alpha_composite(of, (2, 608 - of.height))
hstack([label(up(f_old.crop(crop), 2), "옛 판 (×2)", f"수집기 {oc.width}x{oc.height} · 공급기 {of.width}x{of.height}"),
        label(up(f.crop(crop), 2), "새 판 v2 (×2)", f"수집기 {col0.width}x{col0.height} · 공급기 {fed0.width}x{fed0.height} — 아래 y 608 · 울타리 칸은 아직 안 뺐다")]).save(os.path.join(C, "농장_장치_v2_2x.png"))

# ---------- 주방 ----------
ICONS = ["redbean", "pinenut", "mugwort", "sesame", "jujube", "bean"]
CELL_X = [13, 62, 111, 159]       # kitchen_drawer_3d 안 칸 왼쪽 x (폭 42)
CELL_Y = [22, 102, 178]           # 칸 위 y (높이 약 70)
DRAWER_POS = (300, 470)
MILL_POS = (405, 885)             # 맷돌 아래 왼쪽 (임시)


def kitchen(new, pound="kitchen_pound_0.png", lid_dy=0):
    if new:
        bg = o("Screen/kitchen_bg.png")
        bg.alpha_composite(o("Screen/kitchen_drawer_3d.png"), DRAWER_POS)
        for k in range(12):
            r, c = divmod(k, 4)
            if k < 6:
                ic = Image.open(os.path.join(RES, "Ingredient", ICONS[k] + ".png")).convert("RGBA")
                x, y = DRAWER_POS[0] + CELL_X[c], DRAWER_POS[1] + CELL_Y[r]
                bg.alpha_composite(ic, (x + 21 - ic.width // 2, y + 32 - ic.height // 2))
        bg.alpha_composite(Image.open(os.path.join(KV3, "out", "kitchen_bowl.png")).convert("RGBA"), (240, 778))
        bg.alpha_composite(o("Screen/kitchen_steamer_body.png"), (30, 900))
        bg.alpha_composite(o("Screen/kitchen_steamer_lid.png"), (30, 900 + lid_dy))
        m = o("Screen/kitchen_mill_0.png")
        bg.alpha_composite(m, (MILL_POS[0], MILL_POS[1] - m.height))
        bg.alpha_composite(Image.open(os.path.join(KV3, "out", "kitchen_board.png")).convert("RGBA"), (370, 960))
        bg.alpha_composite(o("Screen/" + pound), (270 - 79, 1040 - 241))
        return bg
    return Image.open(os.path.join(KV3, "견줌", "주방_전체_1x.png")).convert("RGBA")


old, new = kitchen(False), kitchen(True)
hstack([label(old, "지금 (반복 07)"), label(new, "반복 08", "입체 서랍장 · 테이블 지움 · 맷돌(임시 자리) · 찜기 몸통+뚜껑")]).save(os.path.join(C, "주방_전체_1x.png"))
kc = (180, 440, 540, 800)
hstack([label(up(old.crop(kc), 2), "지금 (×2)"), label(up(new.crop(kc), 2), "반복 08 (×2)", "서랍장 왼쪽 테이블 자리 · 칸 위 재료 아이콘")]).save(os.path.join(C, "주방_서랍장_2x.png"))
for key, nm in (("", "가루"), ("_rough", "거친반죽"), ("_smooth", "매끈")):
    gif([o(f"Screen/kitchen_pound{key}_{i}.png") for i in range(7)], f"떡메_{nm}_2x.gif", 2, 120)
gif([o(f"Screen/kitchen_mill_{i}.png") for i in range(5)], "맷돌_3x.gif", 3, 150)
# 찜기 뚜껑 열기 미리보기 : 뚜껑 그림을 위로 옮기기만 (코드 연출 제안)
lid_frames = []
for dy in (0, -6, -14, -24):
    c = Image.new("RGBA", (150, 170))
    c.alpha_composite(o("Screen/kitchen_steamer_body.png"), (0, 30))
    c.alpha_composite(o("Screen/kitchen_steamer_lid.png"), (0, 30 + dy))
    lid_frames.append(c)
gif(lid_frames, "찜기뚜껑_3x.gif", 3, 160)

# ---------- 가판대 ----------
stall = Image.open(STALL).convert("RGBA")
s = stall.copy()
for n, (x, yb) in (("stall_decor_frame", (14, 335)), ("stall_decor_shelf", (175, 330)), ("stall_decor_wreath", (400, 330)),
                   ("stall_decor_plant", (392, 785)), ("stall_decor_sign", (474, 785))):
    im = o(f"Screen/{n}.png")
    s.alpha_composite(im, (x, yb - im.height))
hstack([label(stall, "지금"), label(s, "반복 08", "액자(찹쌀이) · 선반 · 등나무 접시 · 화분 · 칠판 간판 — 자리는 임시")]).save(os.path.join(C, "가판대_전체_1x.png"))
hstack([label(up(stall.crop((0, 200, 540, 800)), 2), "지금 (×2)"), label(up(s.crop((0, 200, 540, 800)), 2), "반복 08 (×2)")]).save(os.path.join(C, "가판대_2x.png"))

# ---------- 모음판 ----------
top = hstack([label(f, "농장"), label(new, "주방"), label(s, "가판대")], gap=16)
top.save(os.path.join(C, "00_모음판.png"))
print("done")

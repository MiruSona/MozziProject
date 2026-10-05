# 반복 07 주방 정식 그림 견줌판 — out/ 그림을 KitchenLayout 자리에 얹어 본다 (게임 코드와 1:1 은 아니다. 눈으로 보는 판).
# 돌리기 : 저장소 뿌리에서  python "Docs/Reference/Art/정식/주방/v3/build_compare.py"
from PIL import Image, ImageDraw, ImageFont
import os

V = os.path.dirname(os.path.abspath(__file__))
O = os.path.join(V, "out")
C = os.path.join(V, "견줌")
RES = os.path.join(V, "..", "..", "..", "..", "..", "..", "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")
os.makedirs(C, exist_ok=True)


def L(p):
    return Image.open(p).convert("RGBA")


def o(n):
    return L(os.path.join(O, n))


def multiply(im, tint):
    r, g, b, a = im.split()
    r = r.point(lambda v: v * tint[0] // 255)
    g = g.point(lambda v: v * tint[1] // 255)
    b = b.point(lambda v: v * tint[2] // 255)
    return Image.merge("RGBA", (r, g, b, a))


def fit(im, size):
    s = max(1, size // max(im.size)) if max(im.size) <= size else 0
    if s:
        return im.resize((im.width * s, im.height * s), Image.NEAREST)
    k = size / max(im.size)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.NEAREST)


try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 14)
except OSError:
    FONT = ImageFont.load_default()

ING = ["redbean", "bean", "mugwort", "sesame", "jujube", "pinenut"]
TINTS = [(255, 214, 214), (255, 240, 200), (214, 240, 206), (232, 226, 246), (255, 222, 190), (236, 228, 214)]


PLATE_Y = 960 + 14  # 새 판의 윗면 안쪽 줄. 지금 코드 PlateY = BoardTop - 2 → BoardTop + 14 로 바꿀 것


def screen(pound_name="kitchen_pound_0.png", shape_name=None, steam_i=0):
    bg = o("kitchen_bg.png")
    # 서랍장
    bg.alpha_composite(o("kitchen_drawer_frame.png"), (292, 470))
    cell = o("kitchen_drawer_cell.png")
    d = ImageDraw.Draw(bg)
    for i in range(12):
        c, r = i % 4, i // 4
        x, y = int(302 + c * 56), int(480 + r * 92)
        k = i % 6
        bg.alpha_composite(multiply(cell, TINTS[k]) if i < 6 else cell, (x, y))
        if i < 6:
            ic = fit(L(os.path.join(RES, "Ingredient", ING[k] + ".png")), 28)
            bg.alpha_composite(ic, (x + 25 - ic.width // 2, y + 30 - ic.height // 2))
            t = str([12, 3, 0, 27, 8, 1][k])
            w = d.textlength(t, font=FONT)
            d.text((x + 25 - w / 2, y + 62 - 9), t, font=FONT, fill=(58, 36, 30))
    # 그릇 + 넣은 재료
    bg.alpha_composite(o("kitchen_bowl.png"), (240, 778))
    for j, k in enumerate([0, 2, 3]):
        ic = fit(L(os.path.join(RES, "Ingredient", ING[k] + ".png")), 28)
        sx = 240 + 9 + j * 32
        bg.alpha_composite(ic, (sx + 14 - ic.width // 2, 778 + 18 - ic.height // 2))
    # 찜기 + 김
    bg.alpha_composite(o("kitchen_steamer.png"), (30, 900))
    st = o(f"kitchen_steam_{steam_i}.png")
    st.putalpha(st.getchannel("A").point(lambda v: v * 200 // 255))
    bg.alpha_composite(st, (73, 842))
    # 빚기판 + 접시 + 빚는 떡
    bg.alpha_composite(o("kitchen_board.png"), (370, 960))
    plate = L(os.path.join(RES, "Screen", "kitchen_plate.png"))
    if shape_name is None:
        bg.alpha_composite(plate, (452 - plate.width // 2, PLATE_Y - plate.height))
    else:
        sh = o(shape_name)  # 64x48, 떡 바닥 줄이 y42 → 아래 가운데 피벗 (32, 42)
        bg.alpha_composite(sh, (452 - 32, PLATE_Y - 42))
    # 절구 (아래 가운데 피벗 x79)
    bg.alpha_composite(o(pound_name), (270 - 79, 1040 - 241))
    return bg


s1 = screen()
s1.save(os.path.join(C, "주방_전체_1x.png"))
s1.resize((1080, 2400), Image.NEAREST).save(os.path.join(C, "주방_전체_2x.png"))
# 2배 확대 부분 (서랍 · 그릇 · 작업대)
s1.crop((0, 440, 540, 1060)).resize((1080, 1240), Image.NEAREST).save(os.path.join(C, "주방_작업대_2x.png"))
s2 = screen("kitchen_pound_smooth_4.png", "kitchen_shape_1.png", 2)
s2.crop((0, 440, 540, 1060)).resize((1080, 1240), Image.NEAREST).save(os.path.join(C, "주방_작업대_치기빚기중_2x.png"))

# 옛 배경과 새 배경 나란히
old = L(os.path.join(V, "..", "out", "kitchen_bg.png"))
cmp = Image.new("RGBA", (1090, 1200), (255, 255, 255, 255))
cmp.alpha_composite(old, (0, 0)); cmp.alpha_composite(o("kitchen_bg.png"), (550, 0))
cmp.save(os.path.join(C, "배경_옛판_새판.png"))


def strip(names, scale, gap=4, bgc=(206, 200, 214, 255)):
    ims = [o(n) for n in names]
    W = sum(i.width for i in ims) + gap * (len(ims) + 1)
    H = max(i.height for i in ims) + gap * 2
    c = Image.new("RGBA", (W, H), bgc)
    x = gap
    for i in ims:
        c.alpha_composite(i, (x, gap)); x += i.width + gap
    return c.resize((W * scale, H * scale), Image.NEAREST)


SEC = [60, 50, 50, 40, 120, 60, 60]
for suf in ["", "_rough", "_smooth"]:
    names = [f"kitchen_pound{suf}_{i}.png" for i in range(7)]
    strip(names, 1).save(os.path.join(C, f"떡메{suf or '_rice'}_띠.png"))
    fr = []
    for i in range(7):
        f = Image.new("RGBA", (212, 241), (206, 200, 214, 255)); f.alpha_composite(o(names[i]))
        fr.append(f.resize((424, 482), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
    fr.append(fr[0])
    fr[0].save(os.path.join(C, f"떡메{suf or '_rice'}_2x.gif"), save_all=True, append_images=fr[1:],
               duration=SEC + [600], loop=0, disposal=2)

# 김 · 빚기
strip([f"kitchen_steam_{i}.png" for i in range(4)], 3, bgc=(120, 92, 80, 255)).save(os.path.join(C, "김_띠_3x.png"))
fr = []
for i in range(4):
    f = Image.new("RGBA", (64, 64), (120, 92, 80, 255)); f.alpha_composite(o(f"kitchen_steam_{i}.png"))
    fr.append(f.resize((192, 192), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
fr[0].save(os.path.join(C, "김_3x.gif"), save_all=True, append_images=fr[1:], duration=150, loop=0, disposal=2)
strip([f"kitchen_shape_{i}.png" for i in range(3)], 3).save(os.path.join(C, "빚기_띠_3x.png"))
fr = []
for i in range(3):
    f = Image.new("RGBA", (64, 48), (206, 200, 214, 255)); f.alpha_composite(o(f"kitchen_shape_{i}.png"))
    fr.append(f.resize((192, 144), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
fr[0].save(os.path.join(C, "빚기_3x.gif"), save_all=True, append_images=fr[1:], duration=[250, 250, 600], loop=0, disposal=2)

# 낱장 (2배) — 서랍 틀 · 칸 · 그릇 · 찜기 · 판
strip(["kitchen_drawer_cell.png", "kitchen_bowl.png", "kitchen_steamer.png", "kitchen_board.png"], 2).save(os.path.join(C, "소품_2x.png"))
strip(["kitchen_drawer_frame.png"], 2).save(os.path.join(C, "서랍장틀_2x.png"))
# 시안과 정식 견줌 (시안 서랍장 C · 찜기 B · 절구 B · 빚기판 A · 그릇 B)
D = os.path.join(V, "..", "..", "..", "시안", "주방기구-v1")
pairs = [("서랍장_C.png", "kitchen_drawer_frame.png"), ("찜기_B.png", "kitchen_steamer.png"),
         ("손질/절구떡메_B.png", "kitchen_pound_0.png"), ("빚기판_A.png", "kitchen_board.png"), ("그릇_B.png", "kitchen_bowl.png")]
ims = [(L(os.path.join(D, a)), o(b)) for a, b in pairs]
W = sum(max(a.width, b.width) + 8 for a, b in ims) + 8
H = max(a.height for a, b in ims) + max(b.height for a, b in ims) + 24
c = Image.new("RGBA", (W, H), (206, 200, 214, 255))
x = 8
top = max(a.height for a, b in ims) + 16
for a, b in ims:
    c.alpha_composite(a, (x, 8)); c.alpha_composite(b, (x, top)); x += max(a.width, b.width) + 8
c.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(C, "시안_정식_2x.png"))
print("done", sorted(os.listdir(C)))

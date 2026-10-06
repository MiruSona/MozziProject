# 반복 08 정식 그림 — raw/ 에서 out/ 을 짜 맞춘다.
# PIL 은 자르기 · 줄/칸 되풀이 · 배치 · 바탕 지우기 · 색 수 줄이기 · 차이 떼기(뚜껑)만 한다. 새로 그리지 않는다.
# 돌리기 : 저장소 뿌리에서  Tools/ArtTool/.venv/Scripts/python.exe "Docs/Reference/Art/정식/반복08/build_out.py"
from PIL import Image
import os, json

V = os.path.dirname(os.path.abspath(__file__))
R = os.path.join(V, "raw")
O = os.path.join(V, "out")
REPO = os.path.abspath(os.path.join(V, "..", "..", "..", "..", ".."))
KV3 = os.path.join(REPO, "Docs", "Reference", "Art", "정식", "주방", "v3")
SIAN = os.path.join(REPO, "Docs", "Reference", "Art", "시안", "반복08-v1")
for d in ("Farm", "Screen"):
    os.makedirs(os.path.join(O, d), exist_ok=True)
INFO = {}


def load(n, base=R):
    return Image.open(os.path.join(base, n)).convert("RGBA")


def save(im, n):
    im.save(os.path.join(O, n))
    cols = im.getcolors(1 << 16)
    INFO[n] = {"size": im.size, "colors": len(cols) - 1 if cols else None}
    print(f"{n:40s} {im.size} colors={len(cols) if cols else '>65k'}")


def key_teal(im):
    """청록 바탕(파랑이 빨강보다 20 넘게 큰 점)을 투명으로."""
    im = im.copy()
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if b - r > 20:
                px[x, y] = (0, 0, 0, 0)
    return im


def reduce(im, n):
    """색 수를 n 개 이하로 (알파는 0/255)."""
    cols = im.getcolors(1 << 16)
    if cols and len(cols) <= n + 1:
        return im
    a = im.getchannel("A")
    q = im.convert("RGB").quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    q.putalpha(a.point(lambda v: 255 if v > 127 else 0))
    return q


def reduce_set(frames, n):
    """여러 장을 한 팔레트로 줄인다 (장마다 색이 튀지 않게)."""
    W = sum(f.width for f in frames)
    H = max(f.height for f in frames)
    sheet = Image.new("RGBA", (W, H))
    x = 0
    for f in frames:
        sheet.alpha_composite(f, (x, 0))
        x += f.width
    sheet = reduce(sheet, n)
    out, x = [], 0
    for f in frames:
        out.append(sheet.crop((x, 0, x + f.width, f.height)))
        x += f.width
    return out


def union_crop(frames, pad=0):
    boxes = [f.getbbox() for f in frames]
    x0 = max(min(b[0] for b in boxes) - pad, 0)
    y0 = max(min(b[1] for b in boxes) - pad, 0)
    x1 = max(b[2] for b in boxes) + pad
    y1 = max(b[3] for b in boxes) + pad
    return [f.crop((x0, y0, x1, y1)) for f in frames]


def pick_cols(im, cols):
    out = Image.new("RGBA", (len(cols), im.height))
    for i, x in enumerate(cols):
        out.paste(im.crop((x, 0, x + 1, im.height)), (i, 0))
    return out


def pick_rows(im, rows):
    out = Image.new("RGBA", (im.width, len(rows)))
    for i, y in enumerate(rows):
        out.paste(im.crop((0, y, im.width, y + 1)), (0, i))
    return out


# 1. 수집기 7장 (0 = 쉼) · 먹이 공급기 4장 (0 = 쉼) — v2 (울타리 한 변 크기로 새로 뽑음)
#    공급기 원본 0~2장엔 알갱이 줄기가 있고 3장엔 없다 → 3 을 쉼으로, 0·1·2 를 내보내기로 쓴다
#    옛 판(75x61 · 64x80)은 raw/옛/ 으로 옮겼다
for name, src, order in (("farm_collector", "collector_v2anim", range(7)), ("farm_feeder", "feeder_v2anim", (3, 0, 1, 2))):
    fr = reduce_set(union_crop([load(f"{src}_f{i}.png") for i in order]), 48)
    for i, f in enumerate(fr):
        save(f, f"Farm/{name}_{i}.png")

# 2. 맷돌 5장 (0 = 쉼, 줄기 없음 · 1~4 돌기)
fr = reduce_set(union_crop([load(f"mill_anim_f{i}.png") for i in range(5)]), 48)
for i, f in enumerate(fr):
    save(f, f"Screen/kitchen_mill_{i}.png")

# 3. 떡메 치기 — 단계 셋 x 7장. 뽑힌 순서 그대로, 5번 장이 「때림」
STAGES = {"": "pound_flour2", "_rough": "pound_rough", "_smooth": "pound_smooth"}
for suffix, src in STAGES.items():
    frames = [key_teal(load(f"{src}_f{k}.png")).crop((3, -5, 215, 236)) for k in range(7)]  # v3 과 같은 212x241 · 밑바닥 240 줄
    for i, f in enumerate(reduce_set(frames, 48)):
        save(f, f"Screen/kitchen_pound{suffix}_{i}.png")

# 4. 입체 서랍장 238x284 — 칸 앞판 안의 같은 줄/칸을 되풀이해 늘린다 (칸 4x3 그대로)
dr = load("drawer.png")
dr = dr.crop(dr.getbbox())                       # 213x208
bx, by = 7, 19                                   # 원본 bbox 왼쪽 위
col_rep = {24 - bx: 6, 67 - bx: 6, 110 - bx: 6, 152 - bx: 6}     # 칸마다 왼쪽 판 줄 하나를 6번 더
row_rep = {70 - by: 25, 125 - by: 25, 180 - by: 25}               # 칸마다 손잡이 아래 판 줄 하나를 25번 더
cols = []
for x in range(dr.width):
    cols += [x] * (1 + col_rep.get(x, 0))
rows = []
for y in range(dr.height):
    rows += [y] * (1 + row_rep.get(y, 0))
drawer = pick_rows(pick_cols(dr, cols), rows)
canvas = Image.new("RGBA", (238, 284))
canvas.alpha_composite(drawer, ((238 - drawer.width) // 2, 284 - drawer.height))
save(reduce(canvas, 48), "Screen/kitchen_drawer_3d.png")
# 칸 자리 재기 (밝은 판색 덩어리)
px = canvas.load()
def is_face(p):
    return p[3] and p[0] > 190 and p[1] > 140
xs = [x for x in range(238) if is_face(px[x, 140])]
runs_x, s = [], None
for x in range(239):
    on = x < 238 and is_face(px[x, 140])
    if on and s is None:
        s = x
    if not on and s is not None:
        if x - s > 20:
            runs_x.append((s, x - s))
        s = None
cx = runs_x[0][0] + 6
runs_y, s = [], None
for y in range(285):
    on = y < 284 and is_face(px[cx, y])
    if on and s is None:
        s = y
    if not on and s is not None:
        if y - s > 20:
            runs_y.append((s, y - s))
        s = None
INFO["drawer_cells"] = {"cols(x,w)": runs_x, "rows(y,h)": runs_y}
print("drawer cells", runs_x, runs_y)

# 5. 찜기 몸통 + 뚜껑 — 뚜껑 연 판(몸통)과 지금 판(닫힘)의 차이 = 뚜껑. 둘 다 v3 과 같은 자리 맞춤
closed = Image.open(os.path.join(KV3, "raw", "steamer_30713.png")).convert("RGBA")
opened = load("steamer_open.png")
bb = closed.getbbox()
ox, oy = (150 - (bb[2] - bb[0])) // 2 - bb[0], 140 - (bb[3] - bb[1]) - bb[1]   # v3 place() 와 같은 옮김
lid = Image.new("RGBA", closed.size)
cp, op_, lp = closed.load(), opened.load(), lid.load()
LID_BOTTOM = 40   # 뚜껑은 이 줄 위에만 있다 (몸통 테 위)
for y in range(LID_BOTTOM):
    for x in range(closed.width):
        if cp[x, y][3] and cp[x, y] != op_[x, y]:
            lp[x, y] = cp[x, y]
for img, name in ((opened, "kitchen_steamer_body"), (lid, "kitchen_steamer_lid")):
    c = Image.new("RGBA", (150, 140))
    c.alpha_composite(img, (ox, oy)) if ox >= 0 and oy >= 0 else c.paste(img.crop((-min(ox, 0), -min(oy, 0), img.width, img.height)), (max(ox, 0), max(oy, 0)))
    save(reduce(c, 48), f"Screen/{name}.png")

# 6. 배경 — 서랍장 왼쪽에 보이던 테이블 조각을 inpaint 판으로 바꾼다 (마스크 안쪽만 붙임)
bg = Image.open(os.path.join(KV3, "out", "kitchen_bg.png")).convert("RGBA")
patch = load("table_L2.png")
bg.paste(patch.crop((8, 16, 96, 160)), (204 + 8, 520 + 16))
save(bg, "Screen/kitchen_bg.png")

# 7. 가판대 장식 — 시안 5-A 와 액자에 찹쌀이를 넣은 5-B 에서 낱개를 자른다
def split_items(im):
    a = im.getchannel("A")
    on = [any(a.getpixel((x, y)) for y in range(im.height)) for x in range(im.width)]
    out, s = [], None
    for x, v in enumerate(on + [False]):
        if v and s is None:
            s = x
        if not v and s is not None:
            p = im.crop((s, 0, x, im.height))
            out.append(p.crop(p.getbbox()))
            s = None
    return [i for i in out if i.width > 6]

a_items = split_items(load(os.path.join("5_가판대장식", "raw", "5-A.png"), SIAN))
b_items = split_items(load("decor_frame.png"))
for im, n in zip(a_items, ("stall_decor_plant", "stall_decor_sign")):
    save(reduce(im, 48), f"Screen/{n}.png")
for im, n in zip(b_items, ("stall_decor_frame", "stall_decor_shelf", "stall_decor_wreath")):
    save(reduce(im, 48), f"Screen/{n}.png")

json.dump(INFO, open(os.path.join(V, "out_info.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)

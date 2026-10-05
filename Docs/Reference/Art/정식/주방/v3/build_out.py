# 반복 07 주방 정식 그림 — raw/ 에서 out/ 을 짜 맞춘다.
# PIL 은 자르기 · 줄/칸 되풀이 · 줄/칸 빼기 · 배치 · 바탕 지우기 · 한 색 바꾸기만 한다. 새로 그리지 않는다.
# 돌리기 : 저장소 뿌리에서  python "Docs/Reference/Art/정식/주방/v3/build_out.py"
from PIL import Image
import os, shutil

V = os.path.dirname(os.path.abspath(__file__))
R = os.path.join(V, "raw")
O = os.path.join(V, "out")
os.makedirs(O, exist_ok=True)


def load(n):
    return Image.open(os.path.join(R, n)).convert("RGBA")


def save(im, n):
    im.save(os.path.join(O, n))
    cols = im.getcolors(1 << 16)
    print(f"{n:32s} {im.size} colors={len(cols) if cols else '>65k'}")


def key_out(im, tol=40):
    """가장자리 바탕색(왼쪽 위 점)과 가까운 점을 투명으로. 바탕 지우기."""
    im = im.copy()
    px = im.load()
    br, bg, bb, _ = px[0, 0]
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if abs(r - br) + abs(g - bg) + abs(b - bb) <= tol:
                px[x, y] = (0, 0, 0, 0)
    return im


def key_teal(im):
    """청록 바탕(파랑이 빨강보다 20 넘게 큰 점)을 투명으로. 바탕 테두리 줄까지 지운다."""
    im = im.copy()
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if b - r > 20:
                px[x, y] = (0, 0, 0, 0)
    return im


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


def reduce(im, n):
    """색 수를 n 개 이하로 (알파는 0/255 유지)."""
    cols = im.getcolors(1 << 16)
    if cols and len(cols) <= n + 1:
        return im
    a = im.getchannel("A")
    q = im.convert("RGB").quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    q.putalpha(a.point(lambda v: 255 if v > 127 else 0))
    return q


def place(im, size, anchor="bottom"):
    """투명 여백을 걷고 size 판에 가운데(아래 맞춤) 놓는다."""
    im = im.crop(im.getbbox())
    W, H = size
    assert im.width <= W and im.height <= H, (im.size, size)
    c = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (W - im.width) // 2
    y = H - im.height if anchor == "bottom" else (H - im.height) // 2
    c.alpha_composite(im, (x, y))
    return c


# 1. 서랍장 틀 238x284 — 기둥·띠의 같은 색 줄을 빼서 10px 로, 안판은 같은 구간을 되풀이해 늘린다
fr = load("drawer_frame_filled.png")
cols = list(range(32, 42)) + list(range(42, 122)) + list(range(60, 122)) + list(range(122, 198)) + list(range(198, 208))
rows = [10, 11, 12, 13, 17, 18, 19, 22, 23, 24] + list(range(25, 152)) + list(range(80, 152)) + list(range(152, 217)) \
    + [218, 219, 220, 226, 227, 228, 229, 231, 232, 233]
frame = pick_rows(pick_cols(fr, cols), rows)
assert frame.size == (238, 284), frame.size
save(reduce(frame, 48), "kitchen_drawer_frame.png")

# 2. 서랍 칸 앞판 50x80 — 시안 칸(38x63)의 같은 색 줄을 되풀이해 늘리고, 바탕 판색만 밝은 저채도로 바꾼다
cell = load("drawer_cell_30752.png").crop((7, 8, 45, 71))  # 38x63
cw, ch = cell.size
cc = list(range(0, 8)) + [8] * 6 + list(range(8, 30)) + [30] * 6 + list(range(30, cw))  # 손잡이 양옆 판 줄 되풀이 → 50
rr = list(range(0, 40)) + [40] * 17 + list(range(40, ch))          # 손잡이 아래 판 17번 되풀이 → 80
cell = pick_rows(pick_cols(cell, cc), rr)
assert cell.size == (50, 80), cell.size
# 한 색 바꾸기 : 감나무 판색(주황)만 밝은 저채도로. 외곽선 · 놋쇠 손잡이 색은 그대로 둔다.
KEEP = {"3E1E23", "422125", "3A1D21", "CBA96E", "EACE92", "AF8B58", "9E784D", "FCF9E7", "F9E9BB", "FBFAFA",
        "76503B", "56352B", "452C24"}
LIGHT = (242, 230, 214)
swap = {}
for _, c in cell.getcolors(1 << 16):
    h = "%02X%02X%02X" % c[:3]
    if c[3] == 0 or h in KEEP:
        continue
    swap[c] = tuple(round(c[i] * 0.42 + LIGHT[i] * 0.58) for i in range(3)) + (255,)
px = cell.load()
for y in range(cell.height):
    for x in range(cell.width):
        if px[x, y] in swap:
            px[x, y] = swap[px[x, y]]
print("cell swapped colors:", len(swap))
save(reduce(cell, 16), "kitchen_drawer_cell.png")

# 3. 그릇 110x36 — 시안 B 를 그대로 가운데 놓기
bowl = Image.open(os.path.join(V, "..", "..", "..", "시안", "주방기구-v1", "그릇_B.png")).convert("RGBA")
save(reduce(place(bowl, (110, 36)), 48), "kitchen_bowl.png")

# 4. 찜기 150x140
save(reduce(place(load("steamer_30713.png"), (150, 140)), 48), "kitchen_steamer.png")

# 5. 김 4장 64x64 흰 한 색
for i in range(4):
    s = key_out(load(f"steam_anim_f{i}.png"), tol=120)
    px = s.load()
    for y in range(s.height):
        for x in range(s.width):
            if px[x, y][3]:
                px[x, y] = (255, 255, 255, 255)
    save(s, f"kitchen_steam_{i}.png")

# 6~7. 떡메 치기 — 단계 셋 x 7장. 뽑은 순서 [0,2,3,4,5,6,1] 로 다시 늘어놓아 4번이 때리는 장면
ORDER = [0, 2, 3, 4, 5, 6, 1]
STAGES = {"": "pound_grain", "_rough": "pound_rice", "_smooth": "pound_smooth"}
for suffix, src in STAGES.items():
    if not os.path.exists(os.path.join(R, f"{src}_f0.png")):
        print("skip", src)
        continue
    for i, k in enumerate(ORDER):
        f = key_teal(load(f"{src}_f{k}.png"))
        f = f.crop((3, -5, 215, 236))  # 절구 밑바닥을 맨 아랫줄(240)에, 몸통 가운데를 x79 에 맞춘다 → 212x241
        save(reduce(f, 48), f"kitchen_pound{suffix}_{i}.png")

# 8. 빚기판 150x80
save(reduce(place(load("board_30721.png"), (150, 80)), 48), "kitchen_board.png")

# 9. 빚기 3장 64x48 — 덩이 → 누른 덩이 → 둥근 떡
for i, n in enumerate(["dough_lump_30771.png", "shape_mid.png", "shape_round.png"]):
    save(reduce(key_teal(load(n)), 16), f"kitchen_shape_{i}.png")

# 10. 배경 — 옹이 지운 판(inpaint 6판 합성) + 쟁반 자리 줄 되풀이
bg = os.path.join(V, "work", "bg_fixed.png")
if os.path.exists(bg):
    shutil.copy(bg, os.path.join(O, "kitchen_bg.png"))
    save(Image.open(os.path.join(O, "kitchen_bg.png")).convert("RGBA"), "kitchen_bg.png")

# 주방 기구 시안 v1 견줌판 만들기 (2026-10-05 · 반복 07).
# 하는 일 : 자르기 · 배치 · 정수배 확대 · 글자 · 선 상자만. 그림을 새로 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) :
#   Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/시안/주방기구-v1/build_compare.py
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
RES = ROOT / "ProjectMozziUnity/Assets/_Project/Resources/Art"
OUT = HERE / "견줌"
OUT.mkdir(exist_ok=True)

FONT = "C:/Windows/Fonts/malgunbd.ttf"


def font(size):
    return ImageFont.truetype(FONT, size)


def load(name):
    p = HERE / f"{name}.png"
    if not p.exists():
        p = HERE / "손질" / f"{name}.png"
    return Image.open(p).convert("RGBA")


def trimmed(img):
    return img.crop(img.getbbox())


def colors(img):
    return len({c for c in img.getdata() if c[3] > 0})


# KitchenLayout.cs 의 자리 (왼, 위, 오른, 아래). 정본은 그 파일이다.
SLOT = {
    "서랍장": (292, 470, 530, 754),
    "그릇": (185, 760, 295, 796),
    "찜기": (30, 900, 180, 1040),
    "절구떡메": (191, 799, 403, 1040),
    "빚기판": (370, 960, 520, 1040),
    "서랍칸": (0, 0, 50, 80),
}
ICONS = ["redbean", "bean", "mugwort", "sesame", "pinenut", "jujube"]
COUNTS = ["92", "97", "8", "4", "9", "9"]


def place_bottom_center(screen, img, cx, bottom):
    t = trimmed(img)
    screen.alpha_composite(t, (round(cx - t.width / 2), bottom - t.height))
    return t


# 서랍 칸 안쪽 (캔버스 px · 그림의 어두운 칸 경계선을 재서 얻은 값, ±1). 열 (왼, 오른) · 줄 (위, 아래)
GRID = {
    "서랍장_A": ([(27, 67), (75, 115), (122, 162), (169, 210)], [(46, 94), (102, 149), (156, 204)]),
    "서랍장_B": ([(21, 63), (71, 114), (122, 165), (173, 216)], [(26, 85), (92, 151), (158, 218)]),
    "서랍장_C": ([(28, 61), (69, 102), (109, 142), (150, 183)], [(26, 83), (92, 148), (157, 214)]),
}


def compose_screen(set_name, picks):
    bg = Image.open(RES / "Screen/kitchen_bg.png").convert("RGBA")
    s = bg.copy()
    # 서랍장 : 자리 가운데 · 바닥 맞춤
    l, t, r, b = SLOT["서랍장"]
    dimg = load(picks["서랍장"])
    dt = place_bottom_center(s, dimg, (l + r) / 2, b)
    bb = dimg.getbbox()
    ox = round((l + r) / 2 - dt.width / 2) - bb[0]
    oy = b - dt.height - bb[1]
    # 재료 아이콘 · 남은 수 : 시안 그림의 칸 가운데에 (게임 값 DrawerIconY 30 / CountY 62 를 칸 높이 80 에 대한 비율로)
    d = ImageDraw.Draw(s)
    f = font(13)
    cols, rows = GRID[picks["서랍장"]]
    i = 0
    for (y0, y1) in rows:
        for (x0, x1) in cols:
            cx = ox + (x0 + x1) / 2
            ch = y1 - y0
            iy = oy + y0 + ch * 30 / 80
            ty = oy + y0 + ch * 62 / 80
            if i < len(ICONS):
                ic = Image.open(RES / f"Ingredient/{ICONS[i]}.png").convert("RGBA")
                s.alpha_composite(ic, (round(cx - 14), round(iy - 14)))
                d.text((cx, ty), COUNTS[i], font=f, fill=(53, 46, 39, 255), anchor="mm")
            else:
                d.text((cx, ty), "?", font=f, fill=(53, 46, 39, 255), anchor="mm")
            i += 1
    # 그릇 · 찜기 · 빚기판
    l, t, r, b = SLOT["그릇"]
    place_bottom_center(s, load(picks["그릇"]), (l + r) / 2, b)
    l, t, r, b = SLOT["찜기"]
    place_bottom_center(s, load(picks["찜기"]), (l + r) / 2, b)
    l, t, r, b = SLOT["빚기판"]
    board = place_bottom_center(s, load(picks["빚기판"]), (l + r) / 2, b)
    # 절구 + 떡메 : 캔버스 왼끝을 191 에, 그림 아래끝을 1040 에
    m = load(picks["절구떡메"])
    bb = m.getbbox()
    s.alpha_composite(m, (191, 1040 - bb[3]))
    # 접시 : 빚기판 윗면에 얹는다 (판 그림 위끝 + 6). 지금 게임 값(PlateY 958)과 다를 수 있다 — README 참고
    plate = Image.open(RES / "Screen/kitchen_plate.png").convert("RGBA")
    board_top = b - board.height
    s.alpha_composite(plate, (452 - plate.width // 2, board_top + 6 - plate.height))
    # 이름표
    d = ImageDraw.Draw(s)
    d.rectangle((8, 1150, 532, 1192), fill=(53, 46, 39, 230))
    txt = f"{set_name} — 서랍장 {picks['서랍장'][-1]} · 찜기 {picks['찜기'][-1]} · 절구 {picks['절구떡메'][-1]} · 빚기판 {picks['빚기판'][-1]} · 그릇 {picks['그릇'][-1]}"
    d.text((270, 1171), txt, font=font(16), fill=(255, 244, 224, 255), anchor="mm")
    return s


SETS = {
    "A안묶음": {"서랍장": "서랍장_A", "찜기": "찜기_A", "절구떡메": "절구떡메_A", "빚기판": "빚기판_A", "그릇": "그릇_A"},
    "B안묶음": {"서랍장": "서랍장_B", "찜기": "찜기_B", "절구떡메": "절구떡메_B", "빚기판": "빚기판_B", "그릇": "그릇_B"},
    "C안묶음": {"서랍장": "서랍장_C", "찜기": "찜기_B", "절구떡메": "절구떡메_C", "빚기판": "빚기판_A", "그릇": "그릇_B"},
}

for name, picks in SETS.items():
    s = compose_screen(name, picks)
    s.save(OUT / f"화면_{name}.png")
    s.resize((s.width * 2, s.height * 2), Image.NEAREST).save(OUT / f"화면_{name}_x2.png")

# 기구별 견줌판 (×2). 줄마다 기구 하나, 칸마다 안 하나. 점선 같은 얇은 상자 = 게임 자리 크기.
ROWS = [
    ("서랍장 (자리 238×284)", "서랍장", ["서랍장_A", "서랍장_B", "서랍장_C"]),
    ("서랍 칸 앞면 (자리 50×80)\n서랍장에서 한 칸만 잘라 봄", "서랍칸", ["@cell:서랍장_A", "@cell:서랍장_B", "@cell:서랍장_C"]),
    ("찜기 (자리 150×140)", "찜기", ["찜기_A", "찜기_B"]),
    ("절구+떡메 쉼\n(자리 212×241)", "절구떡메", ["@old", "절구떡메_A", "절구떡메_B", "절구떡메_C"]),
    ("빚기판 (자리 150×80)", "빚기판", ["빚기판_A", "빚기판_B"]),
    ("그릇 (자리 110×36)", "그릇", ["그릇_A", "그릇_B"]),
]
SC = 2
PAD = 16
BGC = (92, 88, 96, 255)
cells = []
for title, key, names in ROWS:
    l, t, r, b = SLOT[key]
    sw, sh = r - l, b - t
    row = []
    for n in names:
        if n.startswith("@cell:"):
            src = n.split(":")[1]
            (cx0, cx1), (cy0, cy1) = GRID[src][0][0], GRID[src][1][0]
            img = load(src).crop((cx0 - 2, cy0 - 2, cx1 + 3, cy1 + 3))
            label = src + " 첫 칸"
        elif n == "@old":
            img = Image.open(RES / "Screen/kitchen_pound_0.png").convert("RGBA")
            label = "지금 게임 (옛 판)"
        else:
            img = load(n)
            label = n
        t_img = trimmed(img)
        info = f"{t_img.width}×{t_img.height} · {colors(img)}색"
        row.append((label, info, t_img, sw, sh))
    cells.append((title, row))

col_w = max(max(c[2].width, c[3]) for _, row in cells for c in row) * SC + PAD * 2
row_hs = [max(max(c[2].height, c[4]) for c in row) * SC + 70 for _, row in cells]
ncol = max(len(row) for _, row in cells)
W = 250 + col_w * ncol
H = sum(row_hs) + PAD * (len(cells) + 1) + 50
board = Image.new("RGBA", (W, H), BGC)
d = ImageDraw.Draw(board)
d.text((PAD, PAD), "주방 기구 시안 v1 — 기구별 견줌 (×2 · 얇은 노란 상자 = 게임 자리 크기, 바닥 가운데 맞춤)", font=font(22), fill=(255, 244, 224, 255))
y = 50 + PAD
for (title, row), rh in zip(cells, row_hs):
    d.multiline_text((PAD, y + 10), title, font=font(16), fill=(255, 244, 224, 255))
    for i, (label, info, img, sw, sh) in enumerate(row):
        x0 = 250 + i * col_w
        base = y + rh - 10
        cx = x0 + col_w // 2
        # 자리 상자
        d.rectangle((cx - sw * SC // 2, base - sh * SC, cx + sw * SC // 2, base), outline=(240, 200, 80, 255), width=1)
        big = img.resize((img.width * SC, img.height * SC), Image.NEAREST)
        board.alpha_composite(big, (cx - big.width // 2, base - big.height))
        d.text((x0 + 8, y + 6), label, font=font(17), fill=(255, 255, 255, 255))
        d.text((x0 + 8, y + 28), info, font=font(14), fill=(220, 220, 220, 255))
    y += rh + PAD
board.save(OUT / "기구별_견줌_x2.png")
print("done", W, H)

# 주방 구성 시안 3안 + 견줌판을 짜 맞춘다.
# PIL 은 자르기 · 배치 · 좌우 뒤집어 잇기 · 글자 · 반투명 표시 네모만 쓴다 (새로 칠하지 않는다).
# 실행 : python build_mock.py  (이 폴더에서)
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
RES = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Screen")
CAP = os.path.join(ROOT, "Docs", "Reference", "Art", "정식", "반복08", "견줌", "주방캡처", "1_쉼.png")
W, H = 540, 1200
WALL_TOP = 250          # 이 위(천장 · 등)는 지금 배경을 그대로 둔다
UI_TOP, UI_BOTTOM = 1050, 1200  # 안내 글 · 아래 버튼 줄 — 지금 캡처에서 그대로 오려 붙인다


def res(name):
    return Image.open(os.path.join(RES, name + ".png")).convert("RGBA")


def raw(name):
    """뽑은 원본. 조리대는 흰 여백을(어두운 외곽선이 처음·끝 나오는 줄까지), 벽은 양옆 4px 을 잘라 쓴다."""
    im = Image.open(os.path.join(HERE, "raw", name + ".png")).convert("RGBA")
    if name.endswith("wall"):
        return im.crop((4, 0, im.width - 4, im.height))
    px = im.load()
    solid = lambda p: p[3] > 200 and min(p[:3]) < 236
    rows = [y for y in range(im.height) if sum(solid(px[x, y]) for x in range(im.width)) > im.width * 0.6]
    top = rows[0]
    if name.startswith("3"):   # 흰 대리석 윗판은 여백과 색이 같아 따로 잰다 — 불투명한 줄부터
        top = min(y for y in range(im.height) if px[im.width // 2, y][3] > 200 and y >= 8)
    end = rows[0]
    while end + 1 in rows:      # 처음 끊기는 줄까지만 — 그 아래 흰 띠 · 그림자 줄은 버린다
        end += 1
    return im.crop((6, top, im.width - 6, end + 1))


def strip(piece, width):
    """조각 · 뒤집은 조각 · 조각 순서로 이어 가운데 width 만큼 자른다 (이음매가 맞는다)."""
    pw, ph = piece.size
    flip = piece.transpose(Image.FLIP_LEFT_RIGHT)
    row = Image.new("RGBA", (pw * 3, ph))
    for i, p in enumerate((piece, flip, piece)):
        row.paste(p, (pw * i, 0))
    x0 = (pw * 3 - width) // 2
    return row.crop((x0, 0, x0 + width, ph))


def put(canvas, name, x, bottom):
    """기구 그림을 왼쪽 x · 아래 끝 bottom 에 그대로(1배) 올린다."""
    im = res(name)
    canvas.alpha_composite(im, (x, bottom - im.height))


def wall(canvas, piece, until, band_rows=None):
    """벽 조각을 WALL_TOP 부터 깔고, 모자란 아래쪽은 조각의 맨 아래 48px 띠를 되풀이해 until 까지 채운다 (band_rows 로 다른 띠를 고를 수 있다)."""
    row = strip(piece, W)
    canvas.paste(row, (0, WALL_TOP))
    b0, b1 = band_rows or (row.height - 48, row.height)
    band = row.crop((0, b0, W, b1))
    y = WALL_TOP + row.height
    while y < until:
        canvas.paste(band, (0, y))
        y += b1 - b0


def counter(canvas, piece, top, x0=0, x1=W):
    row = strip(piece, W).crop((x0, 0, x1, piece.height))
    canvas.paste(row, (x0, top))


def result_box(canvas, box):
    """결과 자리 표시 — 반투명 남색 네모 + 흰 테두리 + 글자."""
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle(box, fill=(40, 48, 90, 150), outline=(255, 255, 255, 255), width=2)
    d.text(((box[0] + box[2]) // 2, (box[1] + box[3]) // 2), "결과 자리", font=font(18),
           fill=(255, 255, 255, 255), anchor="mm")
    canvas.alpha_composite(layer)


def font(size):
    for f in ("malgunbd.ttf", "malgun.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def base():
    c = res("kitchen_bg").copy()
    return c


def finish(c, name):
    cap = Image.open(CAP).convert("RGBA")
    c.paste(cap.crop((0, UI_TOP, W, UI_BOTTOM)), (0, UI_TOP))      # 안내 글 · 버튼 줄
    c.paste(cap.crop((0, 0, 190, 60)), (0, 0))                      # 돈 표시
    c.convert("RGB").save(os.path.join(HERE, name))
    return c


# ---- 안마다 자리 (왼쪽 x · 아래 끝 y) ----
def plan1():
    """현대 카페 · 조리대 한 줄. 서랍장은 왼쪽 뒤 벽, 조리대 위에 맷돌 → 절구 → 빚기판 → 찜기."""
    c = base()
    wall(c, raw("1_wall"), 850)
    put(c, "kitchen_drawer_3d", 12, 600)
    counter(c, raw("1_counter"), 818)
    put(c, "kitchen_mill_0", 2, 834)
    put(c, "kitchen_pound_0", 80, 838)
    put(c, "kitchen_steamer_body", 395, 834)
    put(c, "kitchen_steamer_lid", 395, 834)
    put(c, "kitchen_board", 268, 842)
    result_box(c, (286, 430, 524, 560))
    return finish(c, "구성_1안.png")


def plan2():
    """전통 섞기 · 조리대 한 줄. 서랍장은 오른쪽 뒤 벽(지금과 비슷), 결과 자리는 왼쪽 벽 액자."""
    c = base()
    wall(c, raw("2_wall"), 850, (180, 228))
    put(c, "kitchen_drawer_3d", 292, 600)
    counter(c, raw("2_counter"), 818)
    put(c, "kitchen_mill_0", 2, 834)
    put(c, "kitchen_pound_0", 80, 838)
    put(c, "kitchen_steamer_body", 395, 834)
    put(c, "kitchen_steamer_lid", 395, 834)
    put(c, "kitchen_board", 268, 842)
    result_box(c, (16, 430, 276, 560))
    return finish(c, "구성_2안.png")


def plan3():
    """밝은 타일 · 2단. 뒤 조리대(서랍장 옆, 맷돌) + 앞 섬 조리대(절구 → 빚기판 → 찜기)."""
    c = base()
    wall(c, raw("3_wall"), 700)
    cp = raw("3_counter")
    counter(c, cp.crop((0, 0, cp.width, 76)), 612, 250, W)   # 뒤 조리대 (오른쪽 절반, 위 76px 만)
    put(c, "kitchen_drawer_3d", 8, 700)
    put(c, "kitchen_mill_0", 280, 630)
    counter(c, cp, 890)
    put(c, "kitchen_pound_0", 10, 910)
    put(c, "kitchen_steamer_body", 388, 906)
    put(c, "kitchen_steamer_lid", 388, 906)
    put(c, "kitchen_board", 226, 912)
    result_box(c, (396, 520, 530, 626))
    return finish(c, "구성_3안.png")


def compare(imgs):
    cap = Image.open(CAP).convert("RGBA")
    labels = ["지금", "1", "2", "3"]
    pad, head = 16, 70
    sheet = Image.new("RGB", (pad + (W + pad) * 4, head + H + pad), (30, 30, 36))
    d = ImageDraw.Draw(sheet)
    for i, im in enumerate([cap] + imgs):
        x = pad + (W + pad) * i
        sheet.paste(im.convert("RGB"), (x, head))
        d.text((x + W // 2, head // 2), labels[i], font=font(44), fill=(255, 255, 255), anchor="mm")
    sheet.save(os.path.join(HERE, "00_견줌판.png"))


if __name__ == "__main__":
    compare([plan1(), plan2(), plan3()])
    print("ok")

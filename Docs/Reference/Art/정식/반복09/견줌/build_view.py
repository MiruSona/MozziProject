# 반복 09 주방 — 정식 배경 + 정식 기구를 화면 꼴(540×1200)로 조립한 견줌판.
# 새 그림은 없다. PIL 로는 자르기 · 배치 · 글자 · 점선 표시 · 반투명 UI 판만 한다. 모두 1배.
import importlib.util
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.dirname(HERE)                                   # 정식/반복09
BG = os.path.join(BASE, "bg")
TOOL = os.path.join(BASE, "tool")
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
RES = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")

spec = importlib.util.spec_from_file_location("v3mock", os.path.join(BASE, "..", "..", "시안", "반복09-v3", "build_mock.py"))
v3 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v3)                                    # font() · CAP · UI_TOP 만 빌려 쓴다
spec5 = importlib.util.spec_from_file_location("v5mock", os.path.join(BASE, "..", "..", "시안", "반복09-v5", "build_mock.py"))
v5 = importlib.util.module_from_spec(spec5)
spec5.loader.exec_module(v5)                                   # 사용자가 고른 시안 W2 천장(등 포함)을 UI 없이 다시 짠다

W, H, PANELS = 540, 1200, 3
BG_TOP = 250
INK = (60, 30, 20, 255)

# ── 자리 값 (칸 안 좌표) ──
SHELF_X, SHELF_ROW_Y = 40, [400, 487, 574]
JAR_X0, JAR_DX = 54, 72
JAR_SLOT = (8, 18, 40, 50)                                     # 병 안 재료 자리 (병 안 x0, y0, x1, y1)
DOT_Y = 586
TRAY = (60, 715)                                               # 큰 쟁반 왼쪽 위 (bg README 값)
BLENDER = (440, 800)                                           # 가운데 x, 보이는 바닥 y
POUND = (270, 815)                                             # 절구 아래 가운데 = 프레임 아래 가운데
RAIL = [145, 345]                                              # 칸2 걸이 레일 왼쪽 위 (가운데 x 270 · 윗변 345, 겹치면 왼쪽으로 민다)
PLANT_TRY = [(2, 30, 480), (2, 30, 560), (2, 2, 480), (2, 420, 480), (3, 30, 360)]   # 화분 선반 후보 (칸, x, y) — 앞에서부터 겹침 0 인 것
CLOCK = (440, 370)                                             # 칸3 벽시계 왼쪽 위
CAKE_SCALE = 2                                                 # 판 위 떡만 정수 2배(최근접) — 게임에서도 그렇게 놓는다
BOARD = (150, 822)
STEAMER = (400, 805)
STEAM_RIM, STEAM_X = 62, 94                                    # 몸통 캔버스 안 김 바닥 y · 가운데 x (몸통 윗테 y 54~66, 맨 위 줄 x 65~123)
CAKE_SPOT = (270, 480, 34)                                     # 결과 떡 자리 표시 (가운데 x, y, 반지름)
INGR = ["redbean", "mugwort", "sesame", "bean", "jujube", "pinenut"]

OLD_HINT = (1060, 1080, (220, 141, 82, 255))                    # 캡처 버튼 줄 위 옛 안내 글 줄 (y0, y1, 판 색)
HINT = {"칸1_준비": "재료를 골라 믹서기에 넣어요", "칸1_갈기": "쌀가루를 가는 중…",
        "칸2_치기_든": "떡메를 내려 반죽을 쳐요", "칸2_치기_친": "쿵! 한 번 더",
        "칸2_치기_매끈": "반죽이 매끈해졌어요", "칸3_빚기": "반죽을 빚어 찜기에 넣어요",
        "칸3_찌기": "찌는 중…"}


def img(*p):
    return Image.open(os.path.join(*p)).convert("RGBA")


def put_bottom(c, im, cx, by):
    """보이는 그림의 아래 가운데를 (cx, by) 에 맞춰 얹는다 (투명 여백 무시)."""
    b = im.getbbox()
    c.alpha_composite(im, (cx - (b[0] + b[2]) // 2, by - b[3]))
    return (cx - (b[0] + b[2]) // 2, by - b[3])


def alpha_box(im, x, y):
    b = im.getbbox()
    return (x + b[0], y + b[1], x + b[2], y + b[3])


def overlap_px(a_im, a_xy, b_im, b_xy):
    """두 그림의 불투명 픽셀이 겹치는 개수."""
    ax, ay = a_xy
    bx, by = b_xy
    x0, y0 = max(ax, bx), max(ay, by)
    x1, y1 = min(ax + a_im.width, bx + b_im.width), min(ay + a_im.height, by + b_im.height)
    if x0 >= x1 or y0 >= y1:
        return 0
    a = a_im.getchannel("A").crop((x0 - ax, y0 - ay, x1 - ax, y1 - ay)).point(lambda v: 255 if v else 0)
    b = b_im.getchannel("A").crop((x0 - bx, y0 - by, x1 - bx, y1 - by)).point(lambda v: 255 if v else 0)
    from PIL import ImageChops
    return ImageChops.multiply(a, b).histogram()[255]


_CEIL = None


def ceil_strip():
    """시안 W2 천장 1620×250 (등 · 받침 포함, 돈 표시 없음). v5 build_mock 의 ceiling() 을 그대로 부른다."""
    global _CEIL
    if _CEIL is None:
        _CEIL = Image.new("RGBA", (W * PANELS, BG_TOP), (0, 0, 0, 255))
        v5.ceiling(_CEIL, "W2")
    return _CEIL


def room(n):
    """칸 n(1~3)의 바탕 : 시안 W2 천장 조각 + bg_n."""
    c = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    c.paste(ceil_strip().crop(((n - 1) * W, 0, n * W, BG_TOP)), (0, 0))
    c.paste(img(BG, "bg_%d.png" % n), (0, BG_TOP))
    return c


def panel1(blender):
    c = room(1)
    jar = img(BG, "jar.png")
    icons = [img(RES, "Ingredient", k + ".png") for k in INGR]
    sw, sh = JAR_SLOT[2] - JAR_SLOT[0], JAR_SLOT[3] - JAR_SLOT[1]
    i = 0
    for y in SHELF_ROW_Y:
        for k in range(6):
            jx, jy = JAR_X0 + k * JAR_DX, y - jar.height + 3
            c.alpha_composite(jar, (jx, jy))
            if i < len(icons):                                 # 28×28 이라 32×32 자리에 1배로 들어간다
                ic = icons[i]
                c.alpha_composite(ic, (jx + JAR_SLOT[0] + (sw - ic.width) // 2, jy + JAR_SLOT[1] + (sh - ic.height) // 2))
            i += 1
        c.alpha_composite(img(BG, "shelf.png"), (SHELF_X, y))
    a = img(BG, "arrow.png")
    ay = 487 - a.height // 2 - 20
    c.alpha_composite(a, (4, ay))
    c.alpha_composite(a.transpose(Image.FLIP_LEFT_RIGHT), (W - 4 - a.width, ay))
    for k, name in enumerate(["dot_on", "dot_off", "dot_off"]):
        c.alpha_composite(img(BG, name + ".png"), (270 - 18 + k * 12, DOT_Y))
    c.alpha_composite(img(BG, "tray.png"), TRAY)
    put_bottom(c, img(TOOL, blender + ".png"), *BLENDER)
    return c


def rail_spot():
    """레일이 치기 21장 어느 것과도 겹치지 않는 자리. 겹치면 왼쪽으로, 그다음 위로 옮긴다."""
    rail = img(BG, "deco_rail.png")
    frames = [img(TOOL, "pound_s%d_%d.png" % (s, f)) for s in (1, 2, 3) for f in range(7)]
    fxy = (POUND[0] - frames[0].width // 2, POUND[1] - frames[0].height)
    x, y = RAIL
    def hits(x, y):
        return sum(overlap_px(rail, (x, y), fr, fxy) for fr in frames)
    while hits(x, y) and x > 8:
        x -= 4
    return (x, y), hits(x, y)


def plant_spot(rail_xy):
    """화분 선반이 레일 · 치기 21장(칸2) 또는 시계 · 결과 떡 자리(칸3)와 안 겹치는 첫 후보."""
    ps = img(BG, "deco_plantshelf.png")
    rail = img(BG, "deco_rail.png")
    frames = [img(TOOL, "pound_s%d_%d.png" % (s, f)) for s in (1, 2, 3) for f in range(7)]
    fxy = (POUND[0] - frames[0].width // 2, POUND[1] - frames[0].height)
    clock = img(BG, "deco_clock.png")
    for n, x, y in PLANT_TRY:
        if n == 2:
            hit = overlap_px(ps, (x, y), rail, rail_xy) + sum(overlap_px(ps, (x, y), fr, fxy) for fr in frames)
        else:
            hit = overlap_px(ps, (x, y), clock, CLOCK)
            cx, cy, r = CAKE_SPOT
            if x < cx + r and x + ps.width > cx - r and y < cy + r and y + ps.height > cy - r:
                hit += 1
        if hit == 0 and y + ps.height <= 700:
            return n, (x, y)
    return None


def panel2(frame, rail_xy, plant):
    c = room(2)
    c.alpha_composite(img(BG, "deco_rail.png"), rail_xy)
    if plant and plant[0] == 2:
        c.alpha_composite(img(BG, "deco_plantshelf.png"), plant[1])
    im = img(TOOL, frame + ".png")
    c.alpha_composite(im, (POUND[0] - im.width // 2, POUND[1] - im.height))
    return c


def dashed_circle(c, cx, cy, r):
    d = ImageDraw.Draw(c)
    for a in range(0, 360, 20):
        d.arc((cx - r, cy - r, cx + r, cy + r), a, a + 10, fill=(150, 100, 60, 230), width=2)


def panel3(open_, plant):
    c = room(3)
    c.alpha_composite(img(BG, "deco_clock.png"), CLOCK)
    if plant and plant[0] == 3:
        c.alpha_composite(img(BG, "deco_plantshelf.png"), plant[1])
    dashed_circle(c, *CAKE_SPOT)
    board = img(TOOL, "shape_board.png")
    bxy = put_bottom(c, board, *BOARD)
    bb = alpha_box(board, *bxy)
    cake = img(RES, "Cake", "plain_cake.png")
    cake = cake.resize((cake.width * CAKE_SCALE, cake.height * CAKE_SCALE), Image.NEAREST)
    put_bottom(c, cake, BOARD[0], (bb[1] + bb[3]) // 2 + cake.height // 2)   # 판 가운데쯤
    if not open_:
        put_bottom(c, img(TOOL, "steamer_closed.png"), *STEAMER)
        return c
    body = img(TOOL, "steamer_open_body.png")                  # 몸통 → 김 → 뚜껑 (김이 뚜껑 뒤에서 솟는다)
    sx, sy = put_bottom(c, body, *STEAMER)                     # 두 겹은 같은 캔버스라 같은 자리에 얹는다
    put_bottom(c, img(TOOL, "steam_1.png"), sx + STEAM_X, sy + STEAM_RIM)
    c.alpha_composite(img(TOOL, "steamer_open_lid.png"), (sx, sy))
    return c


def ui(c, hint):
    cap = Image.open(v3.CAP).convert("RGBA")
    c.paste(cap.crop((0, v3.UI_TOP, W, H)), (0, v3.UI_TOP))
    d = ImageDraw.Draw(c)
    d.rectangle((0, OLD_HINT[0], W - 1, OLD_HINT[1]), fill=OLD_HINT[2])   # 캡처에 남은 옛 안내 글 줄을 판 색으로 덮는다
    d.rectangle((10, 10, 170, 54), fill=(58, 36, 24, 255), outline=(30, 18, 12, 255), width=2)
    d.text((90, 32), "352 G", font=v3.font(26), fill=(255, 255, 255, 255), anchor="mm")
    ov = Image.new("RGBA", c.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(ov)
    od.rounded_rectangle((60, 992, 480, 1036), radius=12, fill=(40, 24, 16, 170))
    c.alpha_composite(ov)
    d.text((270, 1014), hint, font=v3.font(22), fill=(255, 255, 255, 255), anchor="mm")
    return c


def main():
    rail_xy, left = rail_spot()
    plant = plant_spot(rail_xy)
    raw = {
        "칸1_준비": panel1("blender_idle"),
        "칸1_갈기": panel1("blender_run_1"),
        "칸2_치기_든": panel2("pound_s1_0", rail_xy, plant),
        "칸2_치기_친": panel2("pound_s2_3", rail_xy, plant),
        "칸2_치기_매끈": panel2("pound_s3_6", rail_xy, plant),
        "칸3_빚기": panel3(False, plant),
        "칸3_찌기": panel3(True, plant),
    }
    strip = Image.new("RGBA", (W * PANELS, H), (0, 0, 0, 255))
    for i, k in enumerate(["칸1_준비", "칸2_치기_든", "칸3_빚기"]):
        strip.paste(raw[k], (i * W, 0))
    strip.save(os.path.join(HERE, "긴띠.png"))
    shots = {}
    for k, c in raw.items():
        shots[k] = ui(c.copy(), HINT[k])
        shots[k].save(os.path.join(HERE, k + ".png"))

    # 견줌판 : 윗줄 고른 시안 + 한 화면 넷(1배), 아랫줄 긴띠(1배, 가운데)
    shots["고른 시안(칸2)"] = Image.open(os.path.join(BASE, "..", "..", "시안", "반복09-v5", "칸2_W2.png")).convert("RGBA")
    pick = ["고른 시안(칸2)", "칸1_준비", "칸2_치기_든", "칸2_치기_친", "칸3_찌기"]
    m, gap, lab = 20, 20, 60
    sw = m * 2 + len(pick) * W + (len(pick) - 1) * gap
    sh = m + lab + H + gap + lab + H + m
    s = Image.new("RGBA", (sw, sh), (48, 48, 52, 255))
    d = ImageDraw.Draw(s)
    for i, k in enumerate(pick):
        x = m + i * (W + gap)
        d.text((x + W // 2, m + lab // 2), k, font=v3.font(30), fill=(255, 255, 255), anchor="mm")
        s.paste(shots[k], (x, m + lab))
    y = m + lab + H + gap
    d.text((sw // 2, y + lab // 2), "긴띠 (칸1_준비 · 칸2_치기_든 · 칸3_빚기, 1배, UI 없음)", font=v3.font(30),
           fill=(255, 255, 255), anchor="mm")
    s.paste(strip, ((sw - strip.width) // 2, y + lab))
    s.convert("RGB").save(os.path.join(HERE, "00_견줌판.png"))
    print("rail", rail_xy, "overlap", left, "plant", plant)
    check()


def box_at_bottom(name, cx, by):
    """put_bottom 으로 얹었을 때 보이는 그림의 화면 네모."""
    im = img(TOOL, name + ".png")
    b = im.getbbox()
    x, y = cx - (b[0] + b[2]) // 2, by - b[3]
    return (x + b[0], y + b[1], x + b[2], y + b[3])


def check():
    """겹침 · 높이를 숫자로 찍는다."""
    shelf_bottom = SHELF_ROW_Y[-1] + img(BG, "shelf.png").height
    for n in ["blender_idle", "blender_run_1"]:
        bx = box_at_bottom(n, *BLENDER)
        print(n, "box", bx, "선반 아래 끝", shelf_bottom, "쪽 점 y", DOT_Y, "틈", bx[1] - max(shelf_bottom, DOT_Y + 7))
    tops = {}
    for st in (1, 2, 3):
        for f in range(7):
            im = img(TOOL, "pound_s%d_%d" % (st, f) + ".png")
            tops["s%d_%d" % (st, f)] = POUND[1] - im.height + im.getbbox()[1]
    hi = min(tops, key=tops.get)
    print("떡메 맨 위 가장 높은 장", hi, tops[hi], "윗벽장 아래 끝 336", "s1_0", tops["s1_0"], "s3_6", tops["s3_6"])
    for n, xy in [("shape_board", BOARD), ("steamer_closed", STEAMER), ("steamer_open", STEAMER)]:
        print(n, "box", box_at_bottom(n, *xy))
    pb = img(TOOL, "pound_s1_0.png")
    print("절구 프레임 x", POUND[0] - pb.width // 2, POUND[0] + pb.width // 2, "바닥", POUND[1])
    tb = img(BG, "tray.png")
    bl = box_at_bottom("blender_idle", *BLENDER)
    print("쟁반", TRAY, "~", (TRAY[0] + tb.width, TRAY[1] + tb.height), "믹서기 왼끝", bl[0], "틈", bl[0] - (TRAY[0] + tb.width))
    rail_xy, left = rail_spot()
    rl = img(BG, "deco_rail.png")
    print("레일", rail_xy, "~", (rail_xy[0] + rl.width, rail_xy[1] + rl.height), "치기 21장 겹침 px", left)
    pl = plant_spot(rail_xy)
    print("화분 선반", pl, "(겹침 0 인 첫 후보)")
    ck = img(BG, "deco_clock.png")
    cx, cy, r = CAKE_SPOT
    print("시계", CLOCK, "~", (CLOCK[0] + ck.width, CLOCK[1] + ck.height), "결과 떡 원 x", cx - r, cx + r, "y", cy - r, cy + r,
          "가로 틈", CLOCK[0] - (cx + r))
    st = box_at_bottom("steamer_open", *STEAMER)
    print("찜기 열림 캔버스", st, "시계 아래 끝", CLOCK[1] + ck.height, "찜기 위 끝과 틈", st[1] - (CLOCK[1] + ck.height))
    cake = img(RES, "Cake", "plain_cake.png")
    bb = box_at_bottom("shape_board", *BOARD)
    print("떡", cake.size, "x%d →" % CAKE_SCALE, (cake.width * CAKE_SCALE, cake.height * CAKE_SCALE), "판", bb)


if __name__ == "__main__":
    main()

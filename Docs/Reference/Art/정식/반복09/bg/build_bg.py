"""반복 09 주방 정식 배경 조립. raw/ (PixelLab 원본) -> 이 폴더의 낱장 + 조립_긴띠.png.
PIL 은 자르기 · 잇기 · 좌우 뒤집기 · 투명 오리기 · 조립만 한다. 모두 1배.
다시 짜기 : python build_bg.py
"""
import os
from PIL import Image, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
W, PANELS = 540, 3
LONG = W * PANELS                      # 1620
CEIL_H = 250                           # 천장 : 화면 y 0~250
BG_TOP, BG_H = 250, 800                # 배경 : 화면 y 250~1050 (그 아래는 버튼 줄 UI)
WALL_H = 450                           # 벽 : 배경 안 y 0~450 (화면 250~700)
CLEAN_TILE_Y = 115                     # wall 원본에서 누런 얼룩이 가장 적은 23px 타일 줄의 시작 y (재서 고름)
CEIL_BRIGHT = 1.30                    # 새 천장(raw/ceil4)이 시안 W2 보다 짙어 한 톤만 밝게 (밝기만 곱한다)


def raw(name):
    return Image.open(os.path.join(RAW, name + ".png")).convert("RGBA")


def trim(im):
    return im.crop(im.split()[3].getbbox())


def mirror_strip(piece, width):
    """조각을 좌우 번갈아 뒤집어 이어 붙인다 -> 이음매에서 무늬가 끊기지 않는다."""
    out = Image.new("RGBA", (width, piece.height))
    flip = piece.transpose(Image.FLIP_LEFT_RIGHT)
    for i, x in enumerate(range(0, width, piece.width)):
        out.paste(flip if i % 2 else piece, (x, 0))
    return out


def period(im, y0, y1):
    """y0~y1 구간에서 줄 무늬(타일 · 서랍)의 세로 반복 간격을 찾는다."""
    g = im.convert("L")
    w = g.width
    rows = [[g.getpixel((x, y)) for x in range(0, w, 4)] for y in range(g.height)]
    best = min(range(16, (y1 - y0) // 2),
               key=lambda p: sum(abs(a - b) for y in range(y0, y1 - p)
                                 for a, b in zip(rows[y], rows[y + p])) / (y1 - p - y0))
    return best


def grow_down(im, need_h, y0, y1, p=None):
    """im 의 y0~y1 반복 띠를 아래로 되풀이해 높이 need_h 를 채운다. p 를 주면 그 간격을 쓴다."""
    p = p or period(im, y0, y1)
    band = im.crop((0, y1 - p, im.width, y1))
    out = Image.new("RGBA", (im.width, need_h))
    out.paste(im.crop((0, 0, im.width, y1)), (0, 0))
    for y in range(y1, need_h, p):
        out.paste(band, (0, y))
    return out, p


CAB_H = 92                             # raw/wall 위 0~92 : 윗벽장(외곽선 y 87) + 그 아래 그림자 4 줄
BRICK_BAND = (16, 247)                 # raw/wall3 (흰 벽돌 256×256) 에서 줄눈 y 16 ~ y 247 = 벽돌 12 줄(짝수라 엇갈림 위상이 맞다)


def brick_wall():
    """윗벽장(옛 raw/wall 위 92 줄) 아래에 흰 벽돌(raw/wall3) 12 줄 띠를 되풀이해 깐다."""
    wall = Image.new("RGBA", (256, WALL_H))
    wall.paste(raw("wall").crop((0, 0, 256, CAB_H)), (0, 0))
    band = raw("wall3").crop((0, BRICK_BAND[0], 256, BRICK_BAND[1]))
    for y in range(CAB_H, WALL_H, band.height):
        wall.paste(band, (0, y))
    return wall, band.height


def build_bg():
    wall, wp = brick_wall()
    counter, cp = build_cabinet(BG_H - WALL_H)
    bg = Image.new("RGBA", (LONG, BG_H))
    bg.paste(mirror_strip(wall, LONG), (0, 0))
    bg.paste(mirror_strip(counter, LONG), (0, WALL_H))
    bg.paste(mirror_strip(cream_top(), LONG), (0, WALL_H))        # 크림색 윗면을 따로 뒤집어 이음
    for i in range(PANELS):
        bg.crop((i * W, 0, (i + 1) * W, BG_H)).save(os.path.join(HERE, f"bg_{i + 1}.png"))
    return bg, wp, cp


CTR_TOP_H = 130                        # 조리대 윗면 : 화면 y 700~830
CREAM_Y0 = 8                           # raw/counter3 (크림색 윗면 256×148) 의 y 8~138. 맨 아래 줄(147)의 짙은 테는 버린다


def cream_top():
    """옛 v2 조리대처럼 옅은 크림색에 결이 아주 흐린 윗면 130 줄. (v3 의 대리석 counter2 는 안 쓴다)"""
    return raw("counter3").crop((0, CREAM_Y0, 256, CREAM_Y0 + CTR_TOP_H))


EDGE_CTR = (127, 137)                  # 앞 모서리 위쪽 : 옛 counter 원본의 밝은 턱 + 앞면 (10 줄)
EDGE_BASE = (14, 20)                   # 앞 모서리 아래쪽 : base 원본 윗줄 (6 줄). 합 16 줄 = 화면 830~846
BASE_HEAD = (20, 150)                  # base : 서랍 한 줄 + 문 윗부분(손잡이까지)
BASE_TAIL_Y = 182                      # base : 문 아랫부분 + 걸레받이. 그 사이 민무늬 줄(150~182)은 버린다


def build_cabinet(need_h):
    """조리대 윗면(counter2) + 앞 모서리 + 아랫장 한 벌(base: 서랍 한 줄 · 여닫이 문 · 걸레받이).
    아랫장은 문짝 가운데 민무늬 줄을 빼서 높이 need_h 에 맞춘다 (204 줄)."""
    top = Image.new("RGBA", (256, CTR_TOP_H))                     # 자리만 잡는다. 대리석은 build_bg 에서 marble() 로 따로 깐다
    ctr, base = raw("counter"), raw("base")
    parts = [top,
             ctr.crop((0, EDGE_CTR[0], 256, EDGE_CTR[1])),
             base.crop((0, EDGE_BASE[0], 256, EDGE_BASE[1])),
             base.crop((0, BASE_HEAD[0], 256, BASE_HEAD[1]))]
    used = sum(p.height for p in parts)
    tail = base.crop((0, base.height - (need_h - used), 256, base.height))
    assert tail.height <= base.height - BASE_HEAD[1], "아랫장이 모자란다"
    parts.append(tail)
    out = Image.new("RGBA", (256, need_h))
    y = 0
    for p in parts:
        out.paste(p, (0, y))
        y += p.height
    return out, tail.height


# 천장 : 시안 W2(어두운 원근 널) 를 글로 다시 뽑은 raw/ceil4 (256×252). 소실점이 오른쪽 아래인 「왼쪽 반」 판이다.
# 위아래 흰 여백을 잘라 쓰고(내용 y 16~234, 218 줄), 모자란 위 32 줄은 내용 윗부분을 위아래 뒤집어 채운다.
# 칸 하나 = 왼쪽 반(256 + 오른끝 14 열 한 번 더 = 270) + 그 좌우 뒤집기 -> 칸마다 소실점 하나, 칸 경계도 대칭이라 선이 이어진다.
CEIL_SRC = (16, 234)
CENTER_PAD = 14


def build_ceil():
    src = raw("ceil4")
    body = src.crop((0, CEIL_SRC[0], 256, CEIL_SRC[1]))
    top_n = CEIL_H - body.height
    half = Image.new("RGBA", (256, CEIL_H))
    half.paste(body.crop((0, 0, 256, top_n)).transpose(Image.FLIP_TOP_BOTTOM), (0, 0))
    half.paste(body, (0, top_n))
    left = Image.new("RGBA", (W // 2, CEIL_H))
    left.paste(half, (0, 0))
    left.paste(half.crop((256 - CENTER_PAD, 0, 256, CEIL_H)), (256, 0))
    panel = Image.new("RGBA", (W, CEIL_H))
    panel.paste(left, (0, 0))
    panel.paste(left.transpose(Image.FLIP_LEFT_RIGHT), (W // 2, 0))
    panel = ImageEnhance.Brightness(panel).enhance(CEIL_BRIGHT)
    ceil = Image.new("RGBA", (LONG, CEIL_H))
    for i in range(PANELS):
        ceil.paste(panel, (i * W, 0))
    ceil.save(os.path.join(HERE, "ceil.png"))
    return ceil


def build_parts():
    parts = {
        "lamp": trim(raw("lamp")),
        "jar": trim(raw("jar2")),
        "arrow": trim(raw("arrow2")),
        "tray": trim(raw("tray2")),                                # 큰 나무 쟁반 (v3 의 72×32 raw/tray 는 안 쓴다)
        "deco_rail": trim(raw("rail2")),                           # 큰 걸이 레일 (v3 의 100×56 raw/rail 은 안 쓴다)
        "deco_plant": trim(raw("plant")),                          # 옛 작은 화분 (지금 조립에는 안 씀)
        "deco_plantshelf": trim(raw("plantshelf2")),
        "deco_clock": trim(raw("clock2")),
    }
    s = trim(raw("shelf2"))                                        # 248 폭 -> 가운데를 잘라 이어 460 폭
    half = 230
    shelf = Image.new("RGBA", (half * 2, s.height))
    shelf.paste(s.crop((0, 0, half, s.height)), (0, 0))
    shelf.paste(s.crop((s.width - half, 0, s.width, s.height)), (half, 0))
    parts["shelf"] = shelf
    d = raw("dots")
    parts["dot_on"] = trim(d.crop((0, 0, 16, 16)))
    parts["dot_off"] = trim(d.crop((16, 0, 32, 16)))
    for k, im in parts.items():
        im.save(os.path.join(HERE, k + ".png"))
    return parts


# 화면 자리 (칸 안 좌표, 왼쪽 위). 칸 n 의 화면 x = (n-1)*540 + x
LAMP_X = [120, 270, 420]               # 등 가운데 x, 칸마다 셋
SHELF_X, SHELF_ROW_Y = 40, [400, 487, 574]    # 선반 판 윗변 y 셋 (화면 y)
JAR_X0, JAR_DX = 54, 72                # 병 6열
DOT_Y = 586
TRAY_XY = (60, 715)                    # 칸1 쟁반 왼쪽 위 (윗면 700~830 안, 믹서기 x 388 앞에서 끝)
PLANTSHELF_TOP = 360                   # 칸2 화분 선반 윗변 (가운데 x 270)
RAIL_TOP = 440                         # 칸2 걸이 레일 윗변 (가운데 x 270) — 떡메 든 장(맨 위 y 441)과 겹칠 수 있다
CLOCK_XY = (440, 370)                  # 칸3 벽 시계 왼쪽 위 (가운데 270,480 은 결과 떡 자리라 비운다)


def compose(bg, ceil, p):
    c = Image.new("RGBA", (LONG, BG_TOP + BG_H), (0, 0, 0, 255))
    c.paste(ceil, (0, 0))
    c.paste(bg, (0, BG_TOP))
    for i in range(PANELS):
        for x in LAMP_X:
            c.alpha_composite(p["lamp"], (i * W + x - p["lamp"].width // 2, 0))
    jar = p["jar"]
    for y in SHELF_ROW_Y:                                          # 칸1 재료 선반 6×3
        for k in range(6):
            c.alpha_composite(jar, (JAR_X0 + k * JAR_DX, y - jar.height + 3))
        c.alpha_composite(p["shelf"], (SHELF_X, y))
    a = p["arrow"]
    ay = 487 - a.height // 2 - 20
    c.alpha_composite(a, (4, ay))
    c.alpha_composite(a.transpose(Image.FLIP_LEFT_RIGHT), (W - 4 - a.width, ay))
    dx = 270 - (3 * 12) // 2
    for k, name in enumerate(["dot_on", "dot_off", "dot_off"]):
        c.alpha_composite(p[name], (dx + k * 12, DOT_Y))
    c.alpha_composite(p["tray"], TRAY_XY)                          # 칸1 조리대 위 쟁반 (믹서기 왼쪽 빈자리)
    ps, rl, ck = p["deco_plantshelf"], p["deco_rail"], p["deco_clock"]
    c.alpha_composite(ps, (W + 270 - ps.width // 2, PLANTSHELF_TOP))   # 칸2 벽 화분 선반
    c.alpha_composite(rl, (W + 270 - rl.width // 2, RAIL_TOP))         # 칸2 벽 걸이 레일
    c.alpha_composite(ck, (2 * W + CLOCK_XY[0], CLOCK_XY[1]))          # 칸3 벽 오른쪽 위 시계
    c.save(os.path.join(HERE, "조립_긴띠.png"))


if __name__ == "__main__":
    bg, wp, cp = build_bg()
    ceil = build_ceil()
    parts = build_parts()
    compose(bg, ceil, parts)
    print("tile period", wp, "cabinet tail rows", cp)
    for k, im in parts.items():
        print(k, im.size)

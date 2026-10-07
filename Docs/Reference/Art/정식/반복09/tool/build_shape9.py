"""빚기 반죽 shape9 (6장) 을 raw_shape9/ 원본에서 다시 만든다.

하는 일 : 보이는 상자 자르기 → 128×96 캔버스 아래 가운데에 맞추기 → 기준 팔레트로 가까운 색 맞추기
         → 판 위 한눈판(shape9_한눈판.png) · 움직임(shape9.gif).
손으로 그리거나 칠하지 않는다. 확대는 정수배 최근접만.
실행 : python build_shape9.py   (이 폴더에서)
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw_shape9")
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
CAKE = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art", "Cake", "plain_cake.png")

# 원본 파일 → 장 번호 (PixelLab pro_flash 88×64, 5번을 먼저 뽑고 0~4 는 5번을 편집해 만들었다)
# (s1·s4·s5 는 첫 판이 약해 다시 뽑은 b 판을 쓴다. 첫 판 s1.png·s4.png·s5.png 는 기록으로 남겨 둔다)
SRC = ["s0.png", "s1b.png", "s2.png", "s3.png", "s4b.png", "s5b.png"]
CW, CH = 128, 96                 # 캔버스 고정. 반죽의 보이는 아래 가운데 = (64, 96)
BOARD = (150, 822)               # 판 아래 가운데 (견줌/build_view.py 와 같은 값)
SPOT = (150, 811)                # shape9 캔버스 아래 가운데를 놓는 화면 자리 = build_view 가 떡 보이는 아래를 놓는 자리
CAKE_SCALE = 2
MS = [90, 90, 90, 90, 90, 400]   # 장당 시간 (ms)
GIF_SCALE = 4


def load(p):
    return Image.open(p).convert("RGBA")


def clean_alpha(im):
    """반투명 가장자리를 0/255 로 가르고, 이웃 없는 낱 픽셀을 지운다."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= 128 else (0, 0, 0, 0)
    lone = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0:
                continue
            n = sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3])
            if n == 0:
                lone.append((x, y))
    for x, y in lone:
        px[x, y] = (0, 0, 0, 0)
    return im


def palette_of(im):
    return sorted({p[:3] for p in im.getdata() if p[3]})


def snap(im, pal):
    """각 픽셀을 기준 팔레트(5번 장)의 가장 가까운 색으로 맞춘다."""
    px = im.load()
    cache = {}
    for y in range(im.height):
        for x in range(im.width):
            p = px[x, y]
            if not p[3]:
                continue
            c = cache.get(p[:3])
            if c is None:
                c = min(pal, key=lambda q: sum((a - b) ** 2 for a, b in zip(p[:3], q)))
                cache[p[:3]] = c
            px[x, y] = c + (255,)
    return im


def fit(im):
    b = im.getbbox()
    part = im.crop(b)
    if part.width > CW or part.height > CH:
        raise SystemExit("캔버스보다 큼 %s" % (part.size,))
    c = Image.new("RGBA", (CW, CH))
    c.alpha_composite(part, ((CW - part.width) // 2, CH - part.height))
    return c


def put_bottom(c, im, cx, by):
    b = im.getbbox()
    xy = (cx - (b[0] + b[2]) // 2, by - b[3])
    c.alpha_composite(im, xy)
    return xy


def on_board(frame):
    """판 둘레만 잘라 낸 작은 화면 (화면 좌표 x 40~260, y 690~830)."""
    ox, oy = 40, 690
    c = Image.new("RGBA", (220, 140), (246, 240, 230, 255))
    board = load(os.path.join(HERE, "shape_board.png"))
    put_bottom(c, board, BOARD[0] - ox, BOARD[1] - oy)
    if frame is not None:
        c.alpha_composite(frame, (SPOT[0] - ox - CW // 2, SPOT[1] - oy - CH))
    return c


def main():
    raws = [clean_alpha(load(os.path.join(RAW, s))) for s in SRC]
    pal = palette_of(raws[5])
    frames = [fit(snap(r, pal)) for r in raws]
    for i, f in enumerate(frames):
        f.save(os.path.join(HERE, "shape9_%d.png" % i))
        print("shape9_%d" % i, "보이는 상자", f.getbbox())
    print("기준 팔레트", len(pal), "색")

    cake = load(CAKE)
    cake = cake.resize((cake.width * CAKE_SCALE, cake.height * CAKE_SCALE), Image.NEAREST)
    cells = [on_board(f) for f in frames]
    real = on_board(None)
    put_bottom(real, cake, SPOT[0] - 40, SPOT[1] - 690)   # build_view 와 같은 규칙 : 떡 보이는 아래 가운데 = SPOT
    cells.append(real)
    sheet = Image.new("RGBA", (len(cells) * 224, 140), (255, 255, 255, 255))
    for i, c in enumerate(cells):
        sheet.alpha_composite(c, (i * 224, 0))
    sheet = sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST)
    sheet.save(os.path.join(HERE, "shape9_한눈판.png"))
    print("떡 x2 보이는 상자", cake.getbbox(), "/ 5번 장", frames[5].getbbox())

    gif = [c.convert("RGB").resize((c.width * GIF_SCALE, c.height * GIF_SCALE), Image.NEAREST)
           for c in [on_board(f) for f in frames] + [real]]
    gif[0].save(os.path.join(HERE, "shape9.gif"), save_all=True, append_images=gif[1:],
                duration=MS + [600], loop=0, disposal=1)


if __name__ == "__main__":
    main()

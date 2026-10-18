import os, sys
from PIL import Image, ImageDraw

ROOT = r"D:\Project\ClaudeProject\MozziProject"
SCR = os.path.join(ROOT, r"ProjectMozziUnity\Assets\_Project\Resources\Art\Screen")
FX = os.path.join(ROOT, r"ProjectMozziUnity\Assets\_Project\Resources\Art\Fx")
OUT = os.path.join(ROOT, r"Docs\Reference\Art\정식\반복09\tool\steamer2")
LAY = os.path.join(OUT, "layers")
os.makedirs(LAY, exist_ok=True)
os.makedirs(os.path.join(OUT, "raw"), exist_ok=True)

W, H = 180, 199
ld = lambda n, d=SCR: Image.open(os.path.join(d, n + ".png")).convert("RGBA")
ob = ld("kitchen_steamer_open_body")
ol = ld("kitchen_steamer_open_lid")
cl = ld("kitchen_steamer_closed")
dough_src = ld("kitchen_shape9_5")

def lum(p): return (p[0] * 3 + p[1] * 6 + p[2]) // 10
def rows(img, y0, y1): return img.crop((0, y0, W, y1))

# ---- 1단 몸통 다시 쌓기 (아래 맞춤, 캔버스 180x199 그대로) ----
# 원본: 윗단 유리 54~86 · 손잡이 테 86~104 · 아랫단 유리 104~141 · 받침 141~199
# 새것: 테 68~86 · 유리 늘림 86~104(원본 104~122 복사) · 유리 104~141 · 받침 141~199
body = Image.new("RGBA", (W, H))
body.paste(rows(ob, 104, 122), (0, 86))
body.paste(rows(ob, 104, 199), (0, 104))
rim = Image.new("RGBA", (W, H)); rim.paste(rows(ob, 86, 104), (0, 68))

base = Image.new("RGBA", (W, H)); base.paste(rows(ob, 141, 199), (0, 141))

G0, G1 = 86, 141  # 유리 칸 세로 범위
back = Image.new("RGBA", (W, H)); front = Image.new("RGBA", (W, H))
fog = Image.new("RGBA", (W, H))
interior = {}
for y in range(G0, G1):
    xs = [x for x in range(W) if body.getpixel((x, y))[3] > 0]
    if not xs: continue
    l, r = xs[0], xs[-1]
    interior[y] = (l + 3, r - 3)
    for x in range(l, r + 1):
        p = body.getpixel((x, y))
        if p[3] == 0: continue
        L = lum(p); edge = (x - l < 3) or (r - x < 3)
        # 뒷벽·받침판 : 반투명. 받침살(어두운 줄)은 진하게
        if not edge:
            a = 190 if L < 175 else 105
            back.putpixel((x, y), (p[0], p[1], p[2], a))
        # 앞면 : 외곽선 진하게, 옆 두께·반사 밝게, 나머지는 거의 비침
        if L < 90:
            front.putpixel((x, y), p)
        elif edge:
            front.putpixel((x, y), (min(255, p[0] + 15), min(255, p[1] + 15), min(255, p[2] + 15), 210))
        elif L > 236:
            front.putpixel((x, y), (255, 255, 255, 150))
        else:
            front.putpixel((x, y), (min(255, p[0] + 30), min(255, p[1] + 30), min(255, p[2] + 32), 45))
        # 김 서림 : 안쪽만, 흰 안개 + 성긴 물방울
        if not edge:
            dot = (((x * 73856093) ^ (y * 19349663)) % 61 == 0)
            fog.putpixel((x, y), (255, 255, 255, 255) if dot else (238, 242, 244, 180))
# 앞면 세로 반사 줄 2개
for y in range(G0 + 2, G1 - 4):
    if y in interior:
        l, r = interior[y]
        for x in (l + 8, l + 9, r - 14):
            front.putpixel((x, y), (255, 255, 255, 120))
front.alpha_composite(rim)

lid_closed = Image.new("RGBA", (W, H)); lid_closed.paste(rows(cl, 0, 64), (0, 22))
lid_open = Image.new("RGBA", (W, H)); lid_open.alpha_composite(ol, (0, 22))

# ---- 떡 : shape9_5 를 흰색으로 (밝기만 남김, 코드가 물들임) ----
bb = dough_src.getbbox(); d = dough_src.crop(bb)
mx = max(lum(p) for p in d.getdata() if p[3])
dw = Image.new("RGBA", d.size)
for y in range(d.height):
    for x in range(d.width):
        p = d.getpixel((x, y))
        if p[3]:
            v = min(255, lum(p) * 255 // mx); dw.putpixel((x, y), (v, v, v, p[3]))
DOUGH_BOTTOM = 133
DX = (W - d.width) // 2; DY = DOUGH_BOTTOM - d.height
dough = Image.new("RGBA", (W, H)); dough.alpha_composite(dw, (DX, DY))

layers = dict(base=base, glass_back=back, dough=dough, glass_front=front, fog=fog,
              lid_closed=lid_closed, lid_open=lid_open)
for n, im in layers.items(): im.save(os.path.join(LAY, f"steamer2_{n}.png"))
print("dough rect", DX, DY, d.size, "lid_closed bbox", lid_closed.getbbox(), "body bbox", base.getbbox(), front.getbbox())

# ---- 4초 연출 ----
steam = [ld(f"kitchen_steam9_{i}", FX) for i in range(3)]
print("steam", steam[0].size)
PAD = 70; BG = (236, 226, 210, 255)
RAW = (255, 255, 255); DONE = (255, 241, 206)

def tint(img, c, grow):
    t = Image.new("RGBA", img.size)
    for y in range(img.height):
        for x in range(img.width):
            p = img.getpixel((x, y))
            if p[3]: t.putpixel((x, y), (p[0] * c[0] // 255, p[1] * c[1] // 255, p[2] * c[2] // 255, p[3]))
    if grow:
        bbx = t.getbbox(); c2 = t.crop(bbx)
        c2 = c2.resize((c2.width + grow, c2.height + grow), Image.NEAREST)
        t = Image.new("RGBA", img.size); t.alpha_composite(c2, (bbx[0] - grow // 2, bbx[3] - c2.height))
    return t

def fog_at(a, fill):
    # 아래부터 fill(0~1) 만큼, 알파 a(0~1)
    f = fog.copy(); px = f.load(); top = G1 - int((G1 - G0) * fill)
    for y in range(H):
        for x in range(W):
            p = px[x, y]
            if p[3]: px[x, y] = (p[0], p[1], p[2], int(p[3] * a) if y >= top else 0)
    return f

def lerp(a, b, t): return a + (b - a) * max(0, min(1, t))
STEP = 100; frames = []; info = []
for ms in range(0, 4700, STEP):
    t = ms
    lid_is_open = t == 0 or t >= 4000
    fa = lerp(0, 0.7, (t - 300) / 1700) if t < 3600 else lerp(0.7, 0, (t - 3600) / 400)
    ff = lerp(0, 1, (t - 300) / 1700) if t < 3600 else 1
    k = (t - 500) / 3000
    col = tuple(int(lerp(RAW[i], DONE[i], k)) for i in range(3))
    grow = 2 if t >= 2500 else 0
    rattle = -1 if t in (1500, 2300, 3000) else 0
    cv = Image.new("RGBA", (W, H + PAD), BG)
    o = (0, PAD)
    cv.alpha_composite(base, o); cv.alpha_composite(back, o)
    cv.alpha_composite(tint(dough, col, grow), o)
    cv.alpha_composite(front, o)
    if fa > 0: cv.alpha_composite(fog_at(fa, ff), o)
    cv.alpha_composite(lid_open if lid_is_open else lid_closed, (0, PAD + (rattle if not lid_is_open else 0)))
    if 800 <= t < 4000 or 4000 <= t < 4400:
        s = steam[(t // 150) % 3]
        sb = PAD + 30 if t < 4000 else PAD + 70
        cv.alpha_composite(s, ((W - s.width) // 2 + 4, sb - s.height))
    frames.append(cv); info.append(t)

def x2(im): return im.resize((im.width * 2, im.height * 2), Image.NEAREST)
gif = [x2(f).convert("RGB").convert("P", palette=Image.ADAPTIVE, colors=255) for f in frames]
durs = [STEP] * len(gif); durs[-1] = 900
gif[0].save(os.path.join(OUT, "steamer2_익기.gif"), save_all=True, append_images=gif[1:], duration=durs, loop=0)

# ---- 견줌판 : 위 = 지금 찜기(열림·닫힘), 아래 = 새 1단 6장 ----
pick = [0, 1000, 2000, 3000, 3700, 4300]
sel = [frames[info.index(p)] for p in pick]
board = Image.new("RGBA", (W * 6, (H + PAD) * 2 + 10), (70, 70, 90, 255))
for i, im in enumerate([ob, cl]):
    c = Image.new("RGBA", (W, H + PAD), BG); c.alpha_composite(im, (0, PAD))
    if i == 0: c.alpha_composite(ol, (0, PAD))
    board.alpha_composite(c, (i * W, 0))
for i, im in enumerate(sel): board.alpha_composite(im, (i * W, H + PAD + 10))
x2(board).save(os.path.join(OUT, "견줌판.png"))
print("frames", len(frames), "board", board.size)

# 차비 님 원작 색 · 찹쌀이 원본 색을 재고, 지금 게임 그림에서 가장 가까운 색을 찾는다 (읽기만)
import os, glob, colorsys, json
from PIL import Image

R = r"D:\Project\ClaudeProject\ProjectMozzi2"
REF = os.path.join(R, r"Docs\Reference\Art")
S = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.join(S, "refs")

def hx(c): return "#%02X%02X%02X" % c[:3]
def hsv(c):
    h, s, v = colorsys.rgb_to_hsv(*(x / 255 for x in c[:3]))
    return round(h * 360), round(s, 2), round(v, 2)

# Lab 거리
def lab(c):
    def f(u):
        u /= 255
        return ((u + 0.055) / 1.055) ** 2.4 if u > 0.04045 else u / 12.92
    r, g, b = (f(x) for x in c[:3])
    X = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    Y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    Z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    g_ = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    fx, fy, fz = g_(X), g_(Y), g_(Z)
    return 116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)
def de(a, b):
    A, B = lab(a), lab(b)
    return round(sum((x - y) ** 2 for x, y in zip(A, B)) ** 0.5, 1)

def top_colors(path, n=12, alpha_only=True):
    im = Image.open(path).convert("RGBA")
    if im.width * im.height > 400 * 400:
        im = im.resize((min(400, im.width), int(im.height * min(400, im.width) / im.width)), Image.NEAREST)
    cnt = {}
    for p in im.getdata():
        if p[3] < 200: continue
        q = tuple((x // 6) * 6 for x in p[:3])
        cnt[q] = cnt.get(q, 0) + 1
    tot = sum(cnt.values())
    return [(hx(c), round(n_ / tot, 3), hsv(c)) for c, n_ in sorted(cnt.items(), key=lambda t: -t[1])[:n]]

out = {"refs": {}}
for p in sorted(glob.glob(os.path.join(REF, "차비님캐릭", "*"))) + [os.path.join(REF, "찹쌀이", n) for n in ("Front.png", "Base.png")]:
    try:
        out["refs"][os.path.relpath(p, REF)] = top_colors(p)
    except Exception as e:
        out["refs"][p] = str(e)

CHABI = {"머리 분홍": (0xF8, 0xBB, 0xC0), "눈 보라": (0xAE, 0x74, 0xDB), "볼 다이아": (0xFA, 0x62, 0x70), "모자 남색": (0x39, 0x36, 0x51)}
# 게임 그림 전체 색 모으기
gcol = {}
for f in glob.glob(os.path.join(GAME, "**", "*.png"), recursive=True):
    kind = os.path.relpath(f, GAME).split(os.sep)[0]
    im = Image.open(f).convert("RGBA")
    for c, k in (im.getcolors(1 << 20) or []):
        pass
    for n_, c in (im.getcolors(1 << 20) or []):
        if c[3] < 200: continue
        key = c[:3]
        e = gcol.setdefault(key, {"n": 0, "files": set()})
        e["n"] += n_
        e["files"].add(kind + "/" + os.path.basename(f))
out["chabi_vs_game"] = {}
for name, c in CHABI.items():
    near = sorted(gcol.items(), key=lambda t: de(c, t[0]))[:5]
    out["chabi_vs_game"][name] = {"color": hx(c), "hsv": hsv(c),
        "nearest": [(hx(k), de(c, k), hsv(k), v["n"], sorted(v["files"])[:3]) for k, v in near]}
out["outline_vs_hat"] = {"#352E27": hsv((0x35, 0x2E, 0x27)), "#393651": hsv((0x39, 0x36, 0x51)), "dE": de((0x35, 0x2E, 0x27), (0x39, 0x36, 0x51))}
# 찹쌀이 볼 지금 색과 원작 볼
out["cheek_now_vs"] = {k: de((0xFD, 0xE3, 0x67), v) for k, v in CHABI.items()}
json.dump(out, open(os.path.join(S, "chabi.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps(out, ensure_ascii=False, indent=1)[:9000])

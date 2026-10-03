# -*- coding: utf-8 -*-
"""
손님 새 화풍(64x80) 겹을 굽는다.

raw/ 의 PixelLab 그림(기본체 1장 + 기본체에 inpaint 로 머리·옷·표정만 입힌 판)을
기본체와 픽셀 단위로 견줘 「달라진 칸」만 겹으로 떼어 낸다. 그림은 PixelLab 픽셀 그대로이고
여기서 하는 일은 떼어 내기 · 파란 기준색을 6단 램프로 정리하기 · 색 갈아 끼우기뿐이다.

쓰는 법
    python bake_parts.py          # src/ (기준색 한 벌) 과 layers/ (색 구운 것) 를 다시 쓴다

겹 순서 (아래에서 위로)
    body -> bottom -> shirt -> face -> hair

밑감과 결과
    raw/base.png                 -> layers/body.png · face_01.png
    raw/face_02.png              -> face_02.png (웃음)
    raw/face_03_eyes.png + face_03_brow_mouth.png -> face_03.png (시무룩)
    raw/hair_0N.png              -> src/hair_0N.png   -> layers/hair_0N_<색>.png
    raw/top_0N.png               -> src/shirt_0N.png  -> layers/shirt_0N_<색>.png
    raw/bottom_0N.png            -> src/bottom_0N.png -> layers/bottom_0N_<색>.png
"""
import colorsys
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw")
SRC = os.path.join(HERE, "src")
OUT = os.path.join(HERE, "layers")
W, H = 64, 80

# 표정 겹이 덮는 얼굴 칸 (눈·입). 머리 inpaint 는 이 칸을 안 건드린다.
FACE_ZONE = (21, 23, 42, 34)          # x0, y0, x1, y1 (끝 포함)
SHOULDER_Y = 37                        # 이 줄부터는 윗옷 자리. 긴 머리는 여기서 「머리 픽셀」만 남긴다.

# 기준색 6단 (파랑). src/ 의 겹은 색 바꿀 자리가 정확히 이 여섯 색이다.
KEY = [
    (10, 12, 40),      # 0 outline 외곽선
    (26, 44, 104),     # 1 deep    깊은 그늘
    (40, 66, 140),     # 2 shade   그늘
    (54, 94, 186),     # 3 base    기본
    (110, 140, 225),   # 4 light   밝은 면
    (170, 195, 250),   # 5 shine   광택
]
# 가장 많은 파랑(=기본)의 밝기에 대한 비율로 단을 나눈다.
LEVEL_CUTS = [0.30, 0.62, 0.88, 1.15, 1.70]


def ramp(base):
    """기본색 하나에서 6단 램프를 만든다 (어두울수록 채도를 조금 올리고, 밝을수록 뺀다)."""
    r, g, b = [v / 255.0 for v in base]
    h, s, v = colorsys.rgb_to_hsv(r, g, b)

    def mk(s2, v2):
        rr, gg, bb = colorsys.hsv_to_rgb(h, max(0.0, min(1.0, s2)), max(0.0, min(1.0, v2)))
        return (int(round(rr * 255)), int(round(gg * 255)), int(round(bb * 255)))

    return [
        mk(s * 1.10, v * 0.26),
        mk(s * 1.10, v * 0.55),
        mk(s * 1.05, v * 0.78),
        base,
        mk(s * 0.80, v + (1 - v) * 0.22),
        mk(s * 0.45, v + (1 - v) * 0.50),
    ]


HAIR_COLORS = {
    "black":  ramp((0x3A, 0x36, 0x40)),
    "brown":  ramp((0x7E, 0x52, 0x34)),
    "orange": ramp((0xC8, 0x7A, 0x42)),
    "gray":   ramp((0x9C, 0x9A, 0xA2)),
}
SHIRT_COLORS = {
    "red":    ramp((0xCD, 0x4A, 0x40)),
    "green":  ramp((0x5F, 0x9E, 0x58)),
    "blue":   ramp((0x4A, 0x7E, 0xBE)),
    "yellow": ramp((0xE3, 0xBC, 0x5C)),
}
BOTTOM_COLORS = {
    "navy":  ramp((0x3E, 0x4A, 0x70)),
    "beige": ramp((0xC2, 0xA4, 0x7A)),
    "gray":  ramp((0x6E, 0x70, 0x7A)),
}

HAIRS = ["hair_01", "hair_02", "hair_03", "hair_04", "hair_05", "hair_06"]
LONG_HAIRS = {"hair_03", "hair_04"}    # 어깨 위로 내려오는 머리 (mask_hairlong 으로 뽑은 것)
SHIRTS = {"shirt_01": "top_01", "shirt_02": "top_02", "shirt_03": "top_03"}
BOTTOMS = ["bottom_01", "bottom_02"]


def load(name):
    return Image.open(os.path.join(RAW, name + ".png")).convert("RGBA")


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def is_blue(c):
    h, s, v = colorsys.rgb_to_hsv(c[0] / 255.0, c[1] / 255.0, c[2] / 255.0)
    return 0.55 <= h <= 0.72 and s > 0.25


def is_dark(c):
    return max(c[:3]) < 102


def keep_hair_only(x, y, c):
    """긴 머리 : 어깨 줄 아래에서는 파란(머리) 칸과 어두운 선만 남긴다. 다시 그려진 흰 옷·팔은 버린다."""
    return y < SHOULDER_Y or is_blue(c) or is_dark(c)


def diff_layer(base, img, keep=None):
    """기본체와 달라진 칸만 남긴다. keep(x, y, 색) 이 False 인 칸은 버린다."""
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bp, ip, op = base.load(), img.load(), out.load()
    erased = 0
    for y in range(H):
        for x in range(W):
            c = ip[x, y]
            if c == bp[x, y]:
                continue
            if c[3] == 0:
                erased += 1
                continue
            if keep is None or keep(x, y, c):
                op[x, y] = c
    return out, erased


def drop_specks(layer, min_size=4):
    """몇 칸짜리 외딴 점을 지운다 (긴 머리 끝에 떨어져 찍힌 점)."""
    px = layer.load()
    seen = set()
    dropped = 0
    for y in range(H):
        for x in range(W):
            if not px[x, y][3] or (x, y) in seen:
                continue
            todo, comp = [(x, y)], []
            seen.add((x, y))
            while todo:
                ax, ay = todo.pop()
                comp.append((ax, ay))
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        nx, ny = ax + dx, ay + dy
                        if (0 <= nx < W and 0 <= ny < H and (nx, ny) not in seen
                                and px[nx, ny][3]):
                            seen.add((nx, ny))
                            todo.append((nx, ny))
            if len(comp) < min_size:
                for q in comp:
                    px[q] = (0, 0, 0, 0)
                dropped += len(comp)
    return dropped


def drop_white_below_chin(layer):
    """머리 겹의 턱 아래 흰 칸(다시 그려진 흰 옷 조각)을 지운다. 다른 옷 위에서 흰 점으로 뜬다."""
    px = layer.load()
    for y in range(33, H):
        for x in range(W):
            c = px[x, y]
            if c[3] and min(c[:3]) > 225:
                px[x, y] = (0, 0, 0, 0)


def to_key(layer, dark_is_outline):
    """겹의 파란 칸을 KEY 6단으로 정리한다. dark_is_outline 이면 파랑이 아닌 어두운 선도 외곽선 단으로 본다."""
    px = layer.load()
    count = {}
    for y in range(H):
        for x in range(W):
            c = px[x, y]
            if c[3] and is_blue(c):
                count[c[:3]] = count.get(c[:3], 0) + 1
    base_l = lum(max(count.items(), key=lambda kv: kv[1])[0])
    used = [0] * 6
    for y in range(H):
        for x in range(W):
            c = px[x, y]
            if not c[3]:
                continue
            if is_blue(c):
                ratio = lum(c) / base_l
                level = sum(1 for cut in LEVEL_CUTS if ratio >= cut)
            elif dark_is_outline and is_dark(c):
                level = 0
            else:
                assert c[:3] not in KEY, "기준색과 겹치는 안 바꿀 색이 있다"
                continue
            px[x, y] = KEY[level] + (255,)
            used[level] += 1
    return layer, used


def recolor(layer, target):
    out = layer.copy()
    px = out.load()
    table = dict(zip(KEY, target))
    for y in range(H):
        for x in range(W):
            c = px[x, y]
            if c[3] and c[:3] in table:
                px[x, y] = table[c[:3]] + (255,)
    return out


def face_layer(base, imgs_and_zones):
    """얼굴 칸은 통째로(살색 포함), 칸 밖은 달라진 칸만 가져온다."""
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bp, op = base.load(), out.load()
    x0, y0, x1, y1 = FACE_ZONE
    for img, whole_zone in imgs_and_zones:
        ip = img.load()
        for y in range(H):
            for x in range(W):
                inside = x0 <= x <= x1 and y0 <= y <= y1
                if inside and whole_zone:
                    op[x, y] = ip[x, y]
                elif not inside and ip[x, y] != bp[x, y] and ip[x, y][3]:
                    op[x, y] = ip[x, y]
    return out


def stack(*imgs):
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for im in imgs:
        out.alpha_composite(im)
    return out


def trim_body(base, src):
    """몸에서 「어느 파츠를 입혀도 가려지거나 지워지는 칸」을 뺀다.

    PixelLab 이 머리·옷을 입히면서 기본체 윤곽을 몇 칸 깎은 자리(정수리·어깨선)다.
    그대로 두면 머리 위로 살색이 삐져나온다. 한 파츠라도 그 칸을 비워 두면 안 뺀다(leak 으로 센다).
    """
    groups = [[(n, n) for n in HAIRS], list(SHIRTS.items()), [(n, n) for n in BOTTOMS]]
    body = base.copy()
    bpx, body_px = base.load(), body.load()
    trimmed, leaks = 0, {}
    for group in groups:
        erased, covers = {}, {}
        for name, rawname in group:
            rp = load(rawname).load()
            lp = src[name].load()
            erased[name] = set((x, y) for y in range(H) for x in range(W)
                               if bpx[x, y][3] and not rp[x, y][3])
            covers[name] = lp
        union = set().union(*erased.values())
        for (x, y) in union:
            if all((x, y) in erased[n] or covers[n][x, y][3] for n in erased):
                body_px[x, y] = (0, 0, 0, 0)
                trimmed += 1
        for n in erased:
            leaks[n] = sum(1 for (x, y) in erased[n] if body_px[x, y][3])
    return body, trimmed, leaks


def bake(save=True):
    base = load("base")
    made = {}
    report = []
    made["face_02"] = face_layer(base, [(load("face_02"), True)])
    made["face_03"] = face_layer(
        base, [(load("face_03_eyes"), True), (load("face_03_brow_mouth"), False)])

    src = {}
    for name in HAIRS:
        keep = keep_hair_only if name in LONG_HAIRS else None
        layer, erased = diff_layer(base, load(name), keep)
        drop_white_below_chin(layer)
        if name in LONG_HAIRS:
            drop_specks(layer)
        layer, used = to_key(layer, dark_is_outline=True)
        src[name] = layer
        report.append((name, erased, used))
        for color, target in HAIR_COLORS.items():
            made["%s_%s" % (name, color)] = recolor(layer, target)
    for name, rawname in SHIRTS.items():
        layer, erased = diff_layer(base, load(rawname))
        layer, used = to_key(layer, dark_is_outline=True)
        src[name] = layer
        report.append((name, erased, used))
        for color, target in SHIRT_COLORS.items():
            made["%s_%s" % (name, color)] = recolor(layer, target)
    for name in BOTTOMS:
        layer, erased = diff_layer(base, load(name))
        layer, used = to_key(layer, dark_is_outline=False)
        src[name] = layer
        report.append((name, erased, used))
        for color, target in BOTTOM_COLORS.items():
            made["%s_%s" % (name, color)] = recolor(layer, target)

    body, trimmed, leaks = trim_body(base, src)
    made["body"] = body
    made["face_01"] = face_layer(base, [(base, True)])
    report.append(("body", trimmed, leaks))

    if save:
        for d in (SRC, OUT):
            if not os.path.isdir(d):
                os.makedirs(d)
        for name, im in src.items():
            im.save(os.path.join(SRC, name + ".png"))
        for name, im in made.items():
            im.save(os.path.join(OUT, name + ".png"))
    return base, src, made, report


def check(body, src, base):
    """몸+겹 을 PixelLab 원본과 「어느 칸이 차 있나」로 견준다."""
    rows = []
    base_px = base.load()
    pairs = [(n, n) for n in HAIRS] + list(SHIRTS.items()) + [(n, n) for n in BOTTOMS]
    for name, rawname in pairs:
        raw = load(rawname)
        got = stack(body, src[name])
        ga, ra = got.load(), raw.load()
        extra = sum(1 for y in range(H) for x in range(W) if ga[x, y][3] and not ra[x, y][3])
        # 다른 갈래(머리↔옷) 때문에 몸에서 뺀 칸은 이 원본에만 남아 있으니 세지 않는다.
        missing = sum(1 for y in range(H) for x in range(W)
                      if ra[x, y][3] and not ga[x, y][3] and ra[x, y] != base_px[x, y])
        rows.append((name, extra, missing))
    return rows


def main():
    base, src, made, report = bake()
    print("겹 %d 장 -> layers/ · 기준색 %d 장 -> src/" % (len(made), len(src)))
    print("\n[떼어 내기] 이름 · 기본체보다 줄어든 칸 · 단별 픽셀 수(outline..shine)")
    for name, erased, used in report:
        if name == "body":
            print("  몸에서 뺀 칸 %d · 그래도 삐져나오는 칸 %s" % (erased, used))
        else:
            print("  %-10s 줄어든 칸 %2d · %s" % (name, erased, used))
    print("\n[되돌림 검사] 몸+겹 을 PixelLab 원본과 견준다 — 남는 칸(몸이 삐져나옴) / 모자란 칸(구멍)")
    for name, extra, missing in check(made["body"], src, base):
        print("  %-10s 남는 칸 %2d · 모자란 칸 %2d" % (name, extra, missing))
    print("\n[불투명 bbox]")
    for name in sorted(src):
        print("  %-10s %s" % (name, src[name].getbbox()))
    print("  %-10s %s" % ("body", base.getbbox()))


if __name__ == "__main__":
    main()

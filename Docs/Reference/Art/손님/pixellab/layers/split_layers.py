# -*- coding: utf-8 -*-
"""
사람 손님 그림을 네 겹으로 나누고, 나눈 겹을 갈아 끼워 본다.

챹쌀이가 몸·볼·표정·악세 네 겹인 것처럼, 사람 손님도
몸 · 머리카락 · 옷 · 표정 네 겹으로 나눈다.
입력은 clean/ 판(낱색 11~15개)이고, 원본과 clean 판은 안 건드린다.

갈라내는 규칙 셋
  1. 색면으로 간다 — 살색·바지색은 몸, 윗옷색은 옷, 볼·입은 표정.
  2. 자리로 가른다 — 머리카락과 신발은 같은 어두운 색이라 y 높이로 가른다.
     눈도 외곱선과 같은 먹색이라 얼굴 네모 안에 들면 표정으로 본다.
  3. 외곱선(먹색)은 이웃 투표로 붙인다 — 그 선이 두른 색면의 겹에 간다.
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PL = os.path.dirname(HERE)
CLEAN = os.path.join(PL, "clean")

LAYERS = ["body", "hair", "shirt", "face"]
EYE_BOX = (12, 29, 27, 34)      # 이 네모 안의 먹색은 눈이다
HAIR_MAX_Y = 45                 # 이 줄까지가 머리카락, 아래 같은 색은 신발

# 손님마다 낱색이 달라 색표를 따로 적는다.
# hair_or_shoe 는 y 로 다시 가르고, pants_or_hair 도 마찬가지다.
SPECS = {
    "red": dict(
        src="proflash_human2_bob_ref-human_clean.png",
        outline=(0x03, 0x02, 0x02),
        skin=[(0xFE, 0xEA, 0xCD), (0xFB, 0xD6, 0xAB)],
        colors={
            (0x33, 0x2E, 0x2B): "hair_or_shoe",
            (0x58, 0x53, 0x4F): "hair",
            (0x92, 0x83, 0x6C): "hair",
            (0xFE, 0xEA, 0xCD): "body",
            (0xFB, 0xD6, 0xAB): "body",
            (0x95, 0x53, 0x24): "body",
            (0x6A, 0x36, 0x14): "body",
            (0xCD, 0x3A, 0x31): "shirt",
            (0xA3, 0x2B, 0x23): "shirt",
            (0xFE, 0x9D, 0xA2): "face",
            (0xF9, 0x76, 0x63): "face",
        },
        hair_main=(0x33, 0x2E, 0x2B), hair_lit=(0x58, 0x53, 0x4F),
        shirt_main=(0xCD, 0x3A, 0x31), shirt_shade=(0xA3, 0x2B, 0x23),
    ),
    "purple": dict(
        src="proflash_human4_blond_ref-human_clean.png",
        outline=(0x03, 0x03, 0x02),
        skin=[(0xFE, 0xEB, 0xD3)],
        colors={
            (0xD9, 0xC3, 0xAB): "hair",
            (0xAC, 0x95, 0x7D): "hair_or_pants",
            (0x5C, 0x5C, 0x5C): "hair_or_pants",
            (0xFE, 0xEB, 0xD3): "body",
            (0x34, 0x1E, 0x13): "body",
            (0x81, 0x7E, 0x81): "body",
            (0xAC, 0x9A, 0xD4): "shirt",
            (0x8D, 0x72, 0xC2): "shirt",
            (0x62, 0x3C, 0x9D): "shirt",
            (0xFE, 0x8B, 0x7D): "face",
            (0xC4, 0x6C, 0x53): "face",
        },
        hair_main=(0xD9, 0xC3, 0xAB), hair_lit=(0xAC, 0x95, 0x7D),
        shirt_main=(0xAC, 0x9A, 0xD4), shirt_shade=(0x8D, 0x72, 0xC2),
    ),
}


# ---------------------------------------------------------------- 나누기

def classify(im, spec):
    w, h = im.size
    px = im.load()
    owner, outline = {}, []
    ex1, ey1, ex2, ey2 = EYE_BOX
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if not a:
                continue
            c = (r, g, b)
            if c == spec["outline"]:
                owner[(x, y)] = "face" if (ex1 <= x <= ex2 and ey1 <= y <= ey2) else None
                if owner[(x, y)] is None:
                    del owner[(x, y)]
                    outline.append((x, y))
                continue
            lay = spec["colors"].get(c, "body")
            if lay in ("hair_or_shoe", "hair_or_pants"):
                lay = "hair" if y <= HAIR_MAX_Y else "body"
            owner[(x, y)] = lay
    return owner, outline


def vote_outline(owner, outline):
    """외곱선을 이웃이 가장 많은 겹에 붙인다 — 가까울수록 가중치가 크다."""
    left = list(outline)
    for rad in (1, 2, 3, 4, 6):
        nxt, decided = [], {}
        for (x, y) in left:
            tally = {}
            for dy in range(-rad, rad + 1):
                for dx in range(-rad, rad + 1):
                    o = owner.get((x + dx, y + dy))
                    if o:
                        tally[o] = tally.get(o, 0) + 1.0 / (abs(dx) + abs(dy) + 1)
            if tally:
                decided[(x, y)] = max(tally.items(), key=lambda t: t[1])[0]
            else:
                nxt.append((x, y))
        owner.update(decided)
        left = nxt
        if not left:
            break
    for p in left:
        owner[p] = "body"
    return owner


def split(name):
    spec = SPECS[name]
    im = Image.open(os.path.join(CLEAN, spec["src"])).convert("RGBA")
    w, h = im.size
    px = im.load()
    owner = vote_outline(*classify(im, spec))
    # 이마 위쪽의 먹색은 앞머리 그림자다 — 몸이 아니라 머리카락에 붙인다.
    # 이걸 몸에 두면 머리를 벗겼을 때 민머리 이마에 검은 점선이 남는다.
    for (x, y), lay in list(owner.items()):
        if lay == "body" and y < EYE_BOX[1] and px[x, y][:3] == spec["outline"]:
            owner[(x, y)] = "hair"
    out = {k: Image.new("RGBA", (w, h), (0, 0, 0, 0)) for k in LAYERS}
    dst = {k: out[k].load() for k in LAYERS}
    for (x, y), lay in owner.items():
        dst[lay][x, y] = px[x, y]
    # 되돌림 검사 — 네 겹을 다시 쌓으면 원본과 같아야 한다
    re = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for k in LAYERS:
        re.alpha_composite(out[k])
    diff = sum(1 for y in range(h) for x in range(w)
               if re.getpixel((x, y)) != im.getpixel((x, y)))
    for k, img in out.items():
        img.save(os.path.join(HERE, "%s_%s.png" % (name, k)))
    print("[%s] 겹 넷 저장 · 되돌림 다른 픽셀 = %d" % (name, diff))
    return out, spec


# ------------------------------------------------------- 가려졌던 자리 채우기

def fill_body_pil(body, shirt, spec):
    """(a) PIL 로 채운다 — 민머리 두상 + 민 몸통."""
    w, h = body.size
    skin, outline = spec["skin"][0], spec["outline"]
    bp = body.load()
    pts = [(x, y) for y in range(24, 39) for x in range(w)
           if bp[x, y][3] and bp[x, y][:3] in spec["skin"]]
    fx0 = min(p[0] for p in pts); fx1 = max(p[0] for p in pts)
    fy1 = max(p[1] for p in pts); fy0 = fy1 - 17

    filled = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(filled)
    d.rounded_rectangle([fx0 - 1, fy0 - 1, fx1 + 1, fy1 + 1], radius=8, fill=outline + (255,))
    d.rounded_rectangle([fx0, fy0, fx1, fy1], radius=7, fill=skin + (255,))

    sp, fp = shirt.load(), filled.load()
    for y in range(h):
        for x in range(w):
            if sp[x, y][3]:
                fp[x, y] = (outline + (255,)) if sp[x, y][:3] == outline else (skin + (255,))

    # 원래 몸 겹을 업는다. 단, 두상 안쪽에 남은 먹색 점(앞머리 경계석)은 버린다 —
    # 그대로 두면 민머리 이마에 검은 점선이 남는다.
    for y in range(h):
        for x in range(w):
            c = bp[x, y]
            if not c[3]:
                continue
            inside_skull = (fx0 < x < fx1 and fy0 < y < EYE_BOX[1])
            if inside_skull and c[:3] == outline:
                continue
            fp[x, y] = c
    return filled


def make_mask(hair, shirt):
    """(b) PixelLab inpaint 용 마스크 — 흰색이 다시 그릴 자리다."""
    w, h = hair.size
    m = Image.new("RGB", (w, h), (0, 0, 0))
    mp = m.load()
    for src in (hair, shirt):
        p = src.load()
        for y in range(h):
            for x in range(w):
                if p[x, y][3]:
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if 0 <= x + dx < w and 0 <= y + dy < h:
                                mp[x + dx, y + dy] = (255, 255, 255)
    return m


# ------------------------------------------------------------- 갈아 끼우기

def recolor(img, mapping):
    out = img.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            c = p[x, y]
            if c[3] and c[:3] in mapping:
                p[x, y] = mapping[c[:3]] + (255,)
    return out


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c)


def stack(*imgs):
    out = Image.new("RGBA", imgs[0].size, (0, 0, 0, 0))
    for i in imgs:
        if i is not None:
            out.alpha_composite(i)
    return out


def draw_face(base_face, kind, spec):
    """눈·입을 지우고 다른 표정을 손으로 찍는다. 볼은 그대로 둔다."""
    out = base_face.copy()
    p = out.load()
    ex1, ey1, ex2, ey2 = EYE_BOX
    ink = spec["outline"] + (255,)
    mouth = (0xF9, 0x76, 0x63, 255)
    # 눈·입만 지운다 (볼 분홍은 남긴다)
    for y in range(out.height):
        for x in range(out.width):
            c = p[x, y]
            if c[3] and (c[:3] == spec["outline"] or c[:3] == (0xF9, 0x76, 0x63)):
                p[x, y] = (0, 0, 0, 0)
    L, R = 13, 23          # 눈 왼·오른 시작 x (넣은 넣 장 모두 같다)
    if kind == "smile":            # ∧ ∧ 웃는 눈
        for bx in (L, R):
            p[bx, 32] = ink; p[bx + 1, 31] = ink
            p[bx + 2, 31] = ink; p[bx + 3, 32] = ink
        for mx in (18, 19):
            p[mx, 37] = mouth
        p[17, 36] = mouth; p[20, 36] = mouth
    elif kind == "sad":            # 시무룩한 눈 + ⌓ 입
        for bx in (L, R):
            p[bx, 31] = ink; p[bx + 1, 32] = ink
            p[bx + 2, 32] = ink; p[bx + 3, 31] = ink
            p[bx + 1, 33] = ink; p[bx + 2, 33] = ink
        for mx in (18, 19):
            p[mx, 36] = mouth
        p[17, 37] = mouth; p[20, 37] = mouth
    return out


def main():
    red, rspec = split("red")
    pur, pspec = split("purple")

    # 채우기 (a) PIL
    pil = fill_body_pil(red["body"], red["shirt"], rspec)
    pil.save(os.path.join(HERE, "red_body_filled_pil.png"))
    make_mask(red["hair"], red["shirt"]).save(os.path.join(HERE, "mask_hair_shirt.png"))
    print("(a) PIL 채운 몸 · (b) 용 마스크 저장")

    # 채우기 (b) 결과는 PixelLab 에서 받아 둔 파일을 그대로 쓴다
    inp_path = os.path.join(HERE, "red_body_filled_inpaint.png")
    raw = os.path.join(HERE, "red_body_filled_inpaint_raw.png")
    if os.path.exists(raw) and not os.path.exists(inp_path):
        Image.open(raw).convert("RGBA").save(inp_path)

    # 갈아 끼우는 밑바탕은 (a) PIL 채운 몸을 쓴다.
    # (b) inpaint 는 민 몸통을 흰 러닝셔츠로 그렸는데, 그 단이
    # 윗옷 밑단으로 빠져나와 허리에 흰 줄이 생긴다.
    # (a) 는 윗옷 실루에을 그대로 메우므로 절대 밖으로 안 빠져나온다.
    base = pil

    # --- 머리색 3 × 옷색 3
    HAIRS = {"검은머리": (0x33, 0x2E, 0x2B),
             "갈색머리": (0x7A, 0x4E, 0x2C),
             "주황머리": (0xC4, 0x6E, 0x3A)}
    SHIRTS = {"빨간옷": (0xCD, 0x3A, 0x31),
              "푸른옷": (0x3C, 0x74, 0xB5),
              "초록옷": (0x5A, 0x9E, 0x54)}
    mixes = []
    for hn, hc in HAIRS.items():
        # 원래 색과 같은 색이면 그대로 둔다 — 밝은 면까지 계산해 버리면
        # 원본보다 살짝 어두워져 되돌림 검사가 어긋난다.
        hmap = {} if hc == rspec["hair_main"] else {
            rspec["hair_main"]: hc, rspec["hair_lit"]: shade(hc, 1.35)}
        hair = recolor(red["hair"], hmap)
        for sn, sc in SHIRTS.items():
            smap = {} if sc == rspec["shirt_main"] else {
                rspec["shirt_main"]: sc, rspec["shirt_shade"]: shade(sc, 0.78)}
            shirt = recolor(red["shirt"], smap)
            img = stack(base, hair, shirt, red["face"])
            img.save(os.path.join(HERE, "mix_%s_%s.png" % (hn, sn)))
            mixes.append((hn + "·" + sn, img))

    # --- 머리 모양 바꾸기 : 보라옷 손님의 머리카락을 빨간옷 몸에
    other = stack(base, pur["hair"], red["shirt"], red["face"])
    other.save(os.path.join(HERE, "mix_다른머리모양.png"))

    # --- 표정 두 가지
    faces = []
    for kind, label in [("smile", "웃음"), ("sad", "시무룩")]:
        f = draw_face(red["face"], kind, rspec)
        f.save(os.path.join(HERE, "face_%s.png" % label))
        img = stack(base, red["hair"], red["shirt"], f)
        img.save(os.path.join(HERE, "mix_표정_%s.png" % label))
        faces.append((label, img))

    print("갈아 끼운 그림 %d 장 저장" % (len(mixes) + 1 + len(faces)))
    return red, pur, rspec, base, mixes, other, faces


if __name__ == "__main__":
    main()

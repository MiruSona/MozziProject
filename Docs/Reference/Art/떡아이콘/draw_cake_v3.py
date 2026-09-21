"""떡 아이콘 4판 — 「온전한 알 + 옆에 45도 돌린 반쪽」 구도 두 해석.

사용자가 3판을 보고 낸 구도다 : 뒤·왼쪽에 안 자른 떡 한 알, 그 오른쪽 앞에 살짝 겹쳐
45도 돌린 반쪽. 3판 split 과 좌우가 반대다.
「45도」가 24px 에서 두 가지로 읽혀 둘 다 그린다.

  setE_tilt : 반달을 화면 안에서 시계 방향으로 기울였다. 단면이 비스듬한 면으로 보이고
              떡이 뒤 알에 기대어 굴러 있는 느낌이다.
  setF_turn : 반쪽을 세로축으로 돌린 3/4 시점. 단면이 좁은 타원으로 앞-왼쪽을 보고
              그 오른쪽에 둥근 겉면이 같이 보인다. 자른 케이크 조각을 비스듬히 보는 느낌이다.

3판에서 고친 것 셋 (사용자 지적):
  1. 깨를 거의 검은 갈색으로 내리고 뒤 알의 넓은 겉면에 찍는다 — 쑥 피 위에서도 보이게.
  2. 흰 피 그늘을 한 톤 더 어둡게 하고 그늘 폭을 아래로 한 칸 넓힌다 — 밝은 UI 에서 실루엣이 서게.
  3. 잣 소를 더 노릇하게 — 흰 떡살과 갈리게.

규칙은 앞 판과 같다 : 24x24 · 투명 · 알파 0/255 · 외곽선 없음 · 빛 왼쪽 위 · 장당 3~6색.
"""
import os
from PIL import Image, ImageDraw

from draw_cake import N, rgb, ellipse, dot, local, grass_bg, font
from draw_cake_v2 import IDS, recipe, erode, blob, draw, scatter

D = os.path.dirname(os.path.abspath(__file__))

# 재료 자리 규칙은 3판 그대로다 (팥·잣=소 · 쑥·대추=피 · 콩·깨=고물).
# 색만 손본다.
SKIN = {
    None: ("#F2E9DC", "#C9BCA6", "#FFFBF2"),        # 흰 떡. 그늘을 3판(#D8C9B2)보다 어둡게
    "mugwort": ("#6E9B4A", "#4C7433", "#C9DCB4"),
    "jujube": ("#BF6448", "#8E3B26", "#E8C4B2"),
    "bean": ("#E6C868", "#BE9840", "#FFFBF2"),
}
FILL = {
    "redbean": ("#7A2A2A", "#A84848"),
    "pinenut": ("#E8D49A", "#A9743E"),   # 3판(#EFE2BE)보다 노릇하게 — 떡살과 갈리게
}
SESAME_DOT = "#4A3520"  # 거의 검은 갈색. 쑥 피(초록) 위에서도 읽혀야 한다

SHADE_OFF = (2, 3)  # 아래로 한 칸 더 — 밝은 UI 에서 바닥 쪽 실루엣이 선다


def shade_edge(px, off=SHADE_OFF):
    return {q for q in px if (q[0] + off[0], q[1] + off[1]) not in px}


def erode1(px):
    """1px 안쪽. 단면 둘레에 얇은 피를 남길 때 쓴다 (2px 는 작은 면을 다 먹는다)."""
    return {p for p in px if all((p[0] + dx, p[1] + dy) in px
                                 for dx in (-1, 0, 1) for dy in (-1, 0, 1))}


def half_rot(cx, cy, rx, ry, deg, cut=0.12):
    """돌아간 반달. 잘린 면은 local 좌표의 v 쪽이라, deg 를 주면 통째로 기울어진다."""
    out = set()
    for y in range(N):
        for x in range(N):
            u, v = local(x, y, cx, cy, deg)
            if (u / rx) ** 2 + (v / ry) ** 2 <= 1.0 and v <= ry * cut:
                out.add((x, y))
    return out


def ell(cx, cy, rx, ry, deg=0):
    return ellipse(cx, cy, rx, ry, deg)


# ---------- 칠하기 ----------
def paint_skin(im, shape, slots, hl_at=None, sesame=True):
    """겉면 : 피 + 그늘 (+ 고물). 안 자른 알과 반쪽의 겉 모두 이걸 쓴다."""
    key = "bean" if slots["topping"] == "bean" else slots["skin"]
    sb, ss, sf = SKIN[key]
    if slots["topping"] == "bean" and slots["skin"]:
        sf = SKIN[slots["skin"]][2]
    draw(im, shape, sb)
    draw(im, shade_edge(shape), ss)
    if hl_at:
        draw(im, {q for q in hl_at if q in shape}, sf)
    if sesame and slots["topping"] == "sesame":
        # 넓은 겉면에 성기게. 3판에서는 얇은 띠에만 찍혀 바느질 자국처럼 보였다.
        draw(im, scatter(erode(shape), 6), SESAME_DOT)
    return sf


def paint_cut(im, face, slots, fill_at, nuts):
    """잘린 면 : 얇은 피 테두리를 남기고 안쪽을 떡살 + 소로 채운다."""
    key = "bean" if slots["topping"] == "bean" else slots["skin"]
    sf = SKIN[slots["skin"]][2] if (slots["topping"] == "bean" and slots["skin"]) else SKIN[key][2]
    flesh = erode1(face)
    draw(im, flesh, sf)
    k = slots["filling"]
    if k:
        fb, fa = FILL[k]
        core = {q for q in ell(*fill_at) if q in flesh}
        draw(im, core, fb)
        if k == "redbean":
            draw(im, {q for q in (dot(nuts[0][0], nuts[0][1], 1, 1) |
                                  dot(nuts[1][0], nuts[1][1], 1, 1)) if q in core}, fa)
        else:
            for (nx, ny) in nuts:
                draw(im, {q for q in dot(nx, ny, 2, 2) if q in core}, sf)
                draw(im, {q for q in dot(nx, ny, 1, 1) if q in core}, fa)


# ---------- 두 해석 ----------
# E : 뒤 알은 왼쪽 위, 반쪽은 오른쪽 아래에서 45도 기울어 기대어 있다.
# 1판은 뒤 알과 반쪽이 이어져 하나의 긴 초승달로 보였다. 반쪽을 동글게 줄이고
# 뒤 알을 왼쪽 위로 당겨 두 덩어리로 갈라 놓는다.
# 기울기는 40도(시계)로 재 봤더니 뒤 알과 이어져 긴 초승달로 보였다. 여섯 각도를 견줘
# 140도가 가장 잘 읽혔다 — 잘린 면이 왼쪽 위(보는 쪽)를 향하고 덩어리가 동글게 뭉친다.
E_BACK = blob(7.4, 8.4, 5.6, 4.8)
# 자리를 셋 재 보고 13.4 로 정했다 — 뒤 알에 살짝만 닿는다. 더 당기면(12.8) 둘이 한 덩어리로 붙는다.
E_FRONT = half_rot(13.4, 14.8, 7.8, 7.4, 140)
E_FILL = (13.4, 14.6, 4.4, 3.4, 140)
E_NUTS = ((12, 14), (16, 17))

# F : 반쪽을 세로축으로 돌린 3/4. 겉면 덩어리 안에 좁은 단면 타원이 앞-왼쪽을 본다.
F_BACK = blob(8.0, 8.8, 5.8, 5.0)
F_FRONT = blob(15.2, 15.2, 7.8, 6.2)
F_FACE = {q for q in ell(12.8, 15.0, 4.6, 6.2, -20) if q in F_FRONT}
F_FILL = (12.8, 14.6, 2.8, 4.0, -20)
F_NUTS = ((11, 12), (13, 16))


def make(cid, layout):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    s = recipe(cid)
    if layout == "E":
        paint_skin(im, E_BACK - E_FRONT, s, dot(5, 5, 2, 2) - {(6, 5)})
        paint_skin(im, E_FRONT, s, None, sesame=False)  # 반쪽 겉은 좁다 — 깨는 뒤 알에만 찍는다
        paint_cut(im, E_FRONT, s, E_FILL, E_NUTS)
    else:
        paint_skin(im, F_BACK - F_FRONT, s, dot(5, 5, 2, 2) - {(6, 5)})
        # F 는 앞 조각의 겉면이 넓다 — 깨를 여기도 찍어야 깨떡으로 읽힌다.
        paint_skin(im, F_FRONT - F_FACE, s, None)
        draw(im, F_FACE, SKIN["bean" if s["topping"] == "bean" else s["skin"]][0])  # 단면 둘레 피
        paint_cut(im, F_FACE, s, F_FILL, F_NUTS)
    return im


# ---------- 비교 그림 ----------
def load_ref():
    """3판 setD_split 을 참고 줄로 같이 싣는다 — 새 구도와 바로 견주려고."""
    out = []
    for cid in IDS:
        p = os.path.join(D, "setD_split", cid + ".png")
        out.append(Image.open(p).convert("RGBA") if os.path.exists(p)
                   else Image.new("RGBA", (N, N)))
    return out


def compare(sets):
    S, COL, PAD = 7, 24 * 7 + 8, 96
    W = PAD + len(IDS) * COL
    head = 52
    big = len(sets) * 24 * S
    small = 100 * (len(sets) - 1) + 70
    ph = head + big + small
    panels = [("on grass tile", None, (255, 255, 255)),
              ("dark UI  #2B2B33", (0x2B, 0x2B, 0x33, 255), (235, 235, 240)),
              ("light UI  #E8E0D0", (0xE8, 0xE0, 0xD0, 255), (40, 35, 30))]
    out = Image.new("RGBA", (W, ph * 3), (20, 20, 22, 255))
    f, fs, ft = font(20), font(14), font(13)
    for pi, (title, fill, ink) in enumerate(panels):
        panel = grass_bg(W, ph, S) if fill is None else Image.new("RGBA", (W, ph), fill)
        d = ImageDraw.Draw(panel)
        d.text((6, 6), title, font=f, fill=ink)
        for c, cid in enumerate(IDS):
            nm = cid.replace("_cake", "").replace("redbean", "rb").replace("mugwort", "mg") \
                    .replace("sesame", "se").replace("pinenut", "pn").replace("jujube", "ju")
            d.text((PAD + c * COL + 2, 30), nm, font=ft, fill=ink)
        for r, (label, icons) in enumerate(sets):
            d.text((4, head + r * 24 * S + 24 * S // 2), label, font=fs, fill=ink)
            for c, ic in enumerate(icons):
                panel.alpha_composite(ic.resize((24 * S, 24 * S), Image.NEAREST),
                                      (PAD + c * COL, head + r * 24 * S))
        y = head + big + 6
        for label, icons in sets[1:]:  # 참고 줄(3판)은 실제 크기 줄에서 뺀다
            d.text((4, y + 40), label + " x4", font=fs, fill=ink)
            for c, ic in enumerate(icons):
                panel.alpha_composite(ic.resize((96, 96), Image.NEAREST), (PAD + c * COL + 24, y))
            y += 100
        d.text((4, y + 14), "x2 / x1", font=fs, fill=ink)
        for c in range(len(IDS)):
            x = PAD + c * COL + 6
            for k, (_, icons) in enumerate(sets[1:]):
                panel.alpha_composite(icons[c].resize((48, 48), Image.NEAREST), (x + k * 80, y))
                panel.alpha_composite(icons[c], (x + k * 80 + 52, y + 12))
        out.alpha_composite(panel, (0, pi * ph))
    out.save(os.path.join(D, "compare_cake_v3.png"))


def main():
    rows = [("3판 split", load_ref())]
    for folder, layout in (("setE_tilt", "E"), ("setF_turn", "F")):
        od = os.path.join(D, folder)
        os.makedirs(od, exist_ok=True)
        icons = []
        for cid in IDS:
            im = make(cid, layout)
            im.save(os.path.join(od, cid + ".png"))
            icons.append(im)
            print(folder, cid, im.size, "colors", len([c for c in im.getcolors(999) if c[1][3]]))
        rows.append((folder.split("_")[1], icons))
    compare(rows)


if __name__ == "__main__":
    main()

# 반복 09 떡메 치기 시안 A(떡메만) · B(찹쌀이 손이 잡은 떡메) 합성 스크립트.
# raw/A_f*.png · raw/B_f*.png (PixelLab 이 새로 그린 192x192 프레임) 를 절구 · s2 반죽과 겹쳐 334x385 프레임을 만든다.
# 떡메 프레임은 돌리거나 늘리지 않는다. 자르기 · 자리 맞추기 · 겹 합성만 한다.
# 실행: python build_pound10.py  (이 폴더에서)
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.dirname(HERE)
sys.path.insert(0, TOOL)
import build_tool as bt  # noqa: E402  (tight · interior 를 같이 쓴다)

W, H = 334, 385          # 지금 프레임과 같은 캔버스
OX, OY = 65, 209         # 절구 자리 (지금 프레임과 같다)
SMEAR_UP = 16           # 잔상 장에서 친 장 그림을 위로 옮기는 칸 수
SINK = 6                 # 닿는 장에서 떡메 머리가 납작 반죽 윗면보다 더 박히는 깊이
BG = os.path.join(TOOL, "..", "bg", "bg_2.png")
SCREEN_X, SCREEN_Y, BG_TOP = 270, 815, 250   # 프레임 아래 가운데의 화면 자리, 칸2 배경 윗변


def drop_specks(m, keep=40):
    """원본 바닥에 튄 작은 먼지 칸(떨어진 덩어리 keep 칸 미만)을 지운다. 떡메 그림은 손대지 않는다."""
    px = m.load()
    seen = set()
    for y0 in range(m.height):
        for x0 in range(m.width):
            if px[x0, y0][3] == 0 or (x0, y0) in seen:
                continue
            comp, todo = [(x0, y0)], [(x0, y0)]
            seen.add((x0, y0))
            while todo:
                x, y = todo.pop()
                for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if q not in seen and 0 <= q[0] < m.width and 0 <= q[1] < m.height and px[q][3]:
                        seen.add(q)
                        comp.append(q)
                        todo.append(q)
            if len(comp) < keep:
                for q in comp:
                    px[q] = (0, 0, 0, 0)
    return m


def head_bottom(m):
    """떡메 프레임에서 가장 아래 불투명 줄 y 와 그 아래 12줄의 x 평균."""
    box = m.getbbox()
    px = m.load()
    b = box[3]
    xs = [x for y in range(b - 12, b) for x in range(m.width) if px[x, y][3] > 0]
    return sum(xs) / len(xs), b


def build(tag, n):
    mortar = bt.tight(bt.MORTAR)
    dough, flat = bt.tight("dough_s2.png"), bt.tight("dough_s2_flat.png")
    inside, (ix0, iy0, ix1, iy1) = bt.interior(mortar)
    cx, floor = mortar.width // 2, iy1 + 6
    front_y = OY + (iy0 + iy1) // 2
    frames = [Image.open(os.path.join(HERE, "raw", f"{tag}_f{i}.png")).convert("RGBA") for i in range(n)]
    frames = [drop_specks(m) for m in frames]
    bottoms = [head_bottom(m) for m in frames]
    hit = max(range(n), key=lambda i: bottoms[i][1])
    hx, hb = bottoms[hit]
    # 잔상 장: 원본은 든 장에서 친 장으로 바로 건너뛴다. 친 장을 SMEAR_UP 만큼 위로 옮기고(돌리지 않음)
    # 바로 앞 장을 옅게 깔아 내려치는 줄기를 만든다. 이 장은 40ms 로 짧게 보인다.
    smear = Image.new("RGBA", (192, 192))
    ghost = frames[hit - 1].copy()
    ghost.putalpha(ghost.getchannel("A").point(lambda a: a * 35 // 100))
    smear.alpha_composite(ghost)
    smear.alpha_composite(frames[hit], (0, -SMEAR_UP))
    frames.insert(hit, smear)
    flat_at = {i for i, m in enumerate(frames) if i != hit and head_bottom(m)[1] >= hb - 2}
    hit += 1
    fp = flat.load()
    ftop = next(y for y in range(flat.height) if fp[flat.width // 2, y][3] > 200)
    flat_top = OY + floor - flat.height + ftop
    mx = round(OX + cx - hx)                 # 닿는 장의 머리 가운데 = 절구 가운데
    my = flat_top + SINK - hb                # 닿는 장의 머리 바닥 = 납작 반죽 윗면 + SINK
    print(f"{tag}: {len(frames)}장 (잔상 1 포함) · 닿는 장 {hit} · 떡메 캔버스 자리 ({mx},{my}) 오른쪽 끝 {mx + 192}")
    out = []
    for i, m in enumerate(frames):
        d = flat if i in flat_at else dough
        frame = Image.new("RGBA", (W, H))
        frame.alpha_composite(mortar, (OX, OY))
        op = frame.copy().load()
        upper = Image.new("RGBA", (W, H))
        upper.alpha_composite(d, (OX + cx - d.width // 2, OY + floor - d.height))
        upper.alpha_composite(m.crop((max(0, -mx), max(0, -my), 192, 192)), (max(0, mx), max(0, my)))
        up = upper.load()
        # 앞 테두리 가림: 입구 아래 절반에서 절구 몸통과 겹치는 반죽 · 떡메 칸은 지운다 (머리가 절구 안으로 들어가 보이게)
        for y in range(front_y, H):
            for x in range(W):
                if up[x, y][3] and op[x, y][3] and (x - OX, y - OY) not in inside:
                    up[x, y] = (0, 0, 0, 0)
        frame.alpha_composite(upper)
        frame.save(os.path.join(HERE, f"{tag}_{i}.png"))
        out.append(frame)
    # 절구 흔들림 검사: 절구 상자 안에서, 두 장 모두 떡메 · 반죽이 없는 칸의 차이 수
    box = (OX, OY, OX + mortar.width, OY + mortar.height)
    base = Image.new("RGBA", (W, H))
    base.alpha_composite(mortar, (OX, OY))
    bp = base.load()
    diffs = []
    for f in out:
        p = f.load()
        diffs.append(sum(1 for y in range(box[1], box[3]) for x in range(box[0], box[2])
                         if (x - OX, y - OY) not in inside and p[x, y] != bp[x, y]))
    print(f"{tag}: 입구 밖 절구 칸에서 절구만 그린 판과 다른 칸 수 {diffs}")
    return out, hit


def timings(n, hit):
    """들어 올림 느리게 90 · 내려침 빠르게 40 · 닿음 멈춤 120 · 튕김 70."""
    t = [90] * n
    t[hit - 2] = 120   # 가장 높이 든 장에서 잠깐 멈춤
    t[hit - 1] = 40    # 잔상
    t[hit] = 120
    t[(hit + 1) % n] = 70
    return t


def gif(tag, frames, hit):
    bg = Image.open(BG).convert("RGBA")
    px, py = SCREEN_X - W // 2, SCREEN_Y - H - BG_TOP
    t = timings(len(frames), hit)
    seq, dur = [], []
    for rep in range(2):
        for f, ms in zip(frames, t):
            im = bg.copy()
            im.alpha_composite(f, (px, py))
            seq.append(im.convert("RGB"))
            dur.append(ms)
    dur[-1] += 500   # 두 번 치고 잠깐 멈춤
    seq[0].save(os.path.join(HERE, f"pound10_{tag}.gif"), save_all=True, append_images=seq[1:],
                duration=dur, loop=0, disposal=1)
    print(f"{tag} 장별 ms {t}")


def board(cols):
    """옛 것 / A / B 의 든 장 · 친 장을 두 줄로 2배."""
    cw, ch = W * 2, H * 2
    sheet = Image.new("RGBA", (cw * len(cols), ch * 2 + 24), (96, 96, 96, 255))
    dr = ImageDraw.Draw(sheet)
    for c, (name, up, hit) in enumerate(cols):
        dr.text((c * cw + 8, 4), f"{name}  (top: up / bottom: hit)", fill=(255, 255, 255, 255))
        for r, im in enumerate((up, hit)):
            sheet.alpha_composite(im.resize((cw, ch), Image.NEAREST), (c * cw, 24 + r * ch))
    sheet.save(os.path.join(HERE, "pound10_견줌판.png"))


def main():
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 8
    a, ha = build("A", n)
    b, hb = build("B", n)
    gif("A", a, ha)
    gif("B", b, hb)
    old = lambda i: Image.open(os.path.join(TOOL, f"pound_s2_{i}.png")).convert("RGBA")
    board([("old", old(0), old(3)), ("A", a[0], a[ha]), ("B", b[0], b[hb])])


if __name__ == "__main__":
    main()

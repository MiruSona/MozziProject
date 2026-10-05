"""B-2 손님 머리통 겹 셋 — 귀 · 볼만 점 손수정. (b1_reline.py 다음에 돌린다 — out/ 의 머리통을 읽어 덮어쓴다)

허락 범위 : 머리통 겹 셋의 귀 · 볼뿐. 그래서 고친 칸이 아래 두 상자 밖에 하나라도 있으면 멈춘다.
- 귀 : 바깥 둘레 1px 을 머리통 선 색(#352E27)으로 닫는다. 안쪽 결은 붉은 갈색 그대로 두되 표정 셋이 같은 꼴이 되게 한 판을 찍는다.
- 볼 : 네모 덩이(11×5 한 색)를 둥근 2단 덩이(8×5 · 바깥 살 그늘색 · 안쪽 더 분홍)로.
"""
import os
import sys

from PIL import Image

from common import LINE, OUT, bbox_of, diff_pixels, save

EAR_AT = (46, 80)      # 판 왼쪽 위
EAR = [                # _ 안 건드림 · . 밝은 살 · b 살 그늘 · m 옅은 분홍 그늘 · p 분홍 · r 붉은 갈색 결 · L 머리통 선
    "____........__",
    "___.LLLLLLLb._",
    "__.LmbbbbmmrP_".replace("P", "p"),
    "__.Lmmmmbbbrp_",
    "_.Lm.......rp_",
    "_Lm..........._",
    "_Lm..rrrrb...._",
    "_Lm..rppprm..._",
    "_Lm.rrbbbbrm.._",
    "_Lm.rrbbbbrm.._",
    "_Lm..bbbbbrm.._",
    "_Lm..bbbbbm..._",
    "_Lm..bbbbbr..._",
    "_bLm..rrrpp..._",
    "_bbL..rpppb..._",
    "_bbLm.....bb.._",
    "_bbbL........._",
    "_bbbLLLLLr...._",
    "_bbbbbmmmmrm.._",
    "_bbbbbbbbbbb__",
]
EAR = [r[:14] for r in EAR]
CHEEK_AT = (75, 90)
CHEEK = [
    "...bbbbbb..",
    "..bbmmmmbb.",
    "..bmmmmmmb.",
    "..bbmmmmbb.",
    "...bbbbbb..",
]
# 표정마다 살색이 1~2 단계씩 다르다 (따로 뽑힌 그림) — 그 그림에 이미 있는 색을 쓴다
COLORS = {
    1: {".": "FBD8C9", "b": "F4B3A3", "m": "E99F90", "p": "CB8278", "r": "894F4B"},
    2: {".": "FCD9CB", "b": "F9B5A4", "m": "F4A394", "p": "D4867D", "r": "834647"},
    3: {".": "FBD8CA", "b": "F7B7A4", "m": "E78F7F", "p": "AF726B", "r": "794C4D"},
}
# 볼 자리에서 덮어써도 되는 색 (살 · 옛 볼). 눈물 · 눈은 여기 없으니 안 건드린다
CHEEK_OK = {
    1: {"FBD8C9", "F4B3A3"},
    2: {"FCD9CB", "F9B5A4"},
    3: {"FBD8CA", "FBDACD", "F7B7A4", "F8C6B6"},
}


def rgb(h):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def stamp(px, at, rows, cmap, only=None):
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == "_":
                continue
            x, y = at[0] + i, at[1] + j
            if px[x, y][3] == 0:
                continue
            if only is not None and "%02X%02X%02X" % px[x, y][:3] not in only:
                continue
            px[x, y] = LINE + (255,) if ch == "L" else rgb(cmap[ch])


def inside(p, at, rows):
    return at[0] <= p[0] < at[0] + len(rows[0]) and at[1] <= p[1] < at[1] + len(rows)


def run():
    print("겹 | 귀 고친 칸 (bbox) | 볼 고친 칸 (bbox) | 상자 밖")
    for n in (1, 2, 3):
        rel = "Customer/face_0%d.png" % n
        before = Image.open(os.path.join(OUT, rel)).convert("RGBA")
        after = before.copy()
        px = after.load()
        stamp(px, EAR_AT, EAR, COLORS[n])
        stamp(px, CHEEK_AT, CHEEK, COLORS[n], only=CHEEK_OK[n])
        pts = diff_pixels(before, after)
        ear = [p for p in pts if inside(p, EAR_AT, EAR)]
        cheek = [p for p in pts if inside(p, CHEEK_AT, CHEEK)]
        outside = [p for p in pts if p not in ear and p not in cheek]
        print("face_0%d | %d %s | %d %s | %d" % (n, len(ear), bbox_of(ear), len(cheek), bbox_of(cheek), len(outside)))
        if outside or after.size != before.size:
            sys.exit("허락 범위 밖을 고쳤다 : %s" % outside[:5])
        save(after, rel)


if __name__ == "__main__":
    run()

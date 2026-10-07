"""blender9_body_0 손질 — 유리 통 안 가운데 쇠봉(칼날 축)을 지운다.

피드백 : 가운데 쇠봉 때문에 어색하다. 칼날은 바닥에 있다고 보고 통 안은 비워 둔다.
손질 : 축과 그 그림자(x41..55, y33..57)를 지우고, 같은 행의 맨 유리 칸(x36..40 · x56..60)에서
       하나씩 골라 그대로 옮겨 붙인다 (평균 없음 · 유리 팔레트 색만).
원본은 raw_before_fix_body/blender9_body_0.png. 실행 : python fix_body9.py (이 폴더에서).
"""
import os
import random

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "raw_before_fix_body", "blender9_body_0.png")
DST = os.path.join(HERE, "blender9_body_0.png")
HOLE_X = range(41, 56)
HOLE_Y = range(33, 58)
SRC_X = list(range(36, 41)) + list(range(56, 61))
GLASS = {(0xcf, 0xda, 0xe2), (0xd3, 0xdd, 0xe4), (0xca, 0xd4, 0xdc), (0xdb, 0xe3, 0xe6), (0xbf, 0xcd, 0xd6)}


def main():
    im = Image.open(SRC).convert("RGBA")
    px = im.load()
    rnd = random.Random(9)
    n = 0
    for y in HOLE_Y:
        pool = [px[x, y] for x in SRC_X if px[x, y][:3] in GLASS]
        # 맨 유리색을 많이 두고 반사 알갱이는 원래 비율대로 섞인다
        for x in HOLE_X:
            px[x, y] = rnd.choice(pool)
            n += 1
    im.save(DST)
    print("바꾼 칸", n)


if __name__ == "__main__":
    main()

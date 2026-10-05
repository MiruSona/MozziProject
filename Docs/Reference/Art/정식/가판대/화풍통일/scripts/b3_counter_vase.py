"""B-3 계산대 · 꽃병 — 남색 검정 · 붉은 검정 선을 line 램프로. (메뉴 세움판은 안 고친다 — 화면에서 뺀다)

- 계산대 : `arttool reline --scope colors` 로 가장 어두운 남색 둘(#110E26 · #191028)을 그림 전체에서 `#211D16` 으로.
  (널 사이 선 · 둘레가 같은 색이다. 널 면이 어두운 갈색이라 기준 `#352E27` 보다 한 칸 어두운 색을 쓴다)
  남은 남보라 그늘 둘(#1D1425 · #261621)은 PIL 로 `#352E27`.
- 꽃병 : 붉은 검정 셋(#1F0702 · #150603 · #280A04)을 `#352E27` 로. 잎의 짙은 풀색 선은 selout 이라 그대로 둔다.
  reline 은 색을 골라 줄 수 없어(밝기 폭만) 잎 선까지 먹는다 → 색표로 바꾼다.
"""
import os
import shutil
import subprocess
import tempfile

from PIL import Image

from common import ARTTOOL, RES, diff_pixels, load, save

COUNTER_MAP = {(0x1D, 0x14, 0x25): (0x35, 0x2E, 0x27), (0x26, 0x16, 0x21): (0x35, 0x2E, 0x27)}
VASE_MAP = {(0x1F, 0x07, 0x02): (0x35, 0x2E, 0x27), (0x15, 0x06, 0x03): (0x35, 0x2E, 0x27),
            (0x28, 0x0A, 0x04): (0x35, 0x2E, 0x27)}


def remap(im, table):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3] and c[:3] in table:
                px[x, y] = table[c[:3]] + (255,)
    return im


def run():
    tmp = tempfile.mkdtemp(prefix="b3_")
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    subprocess.run([ARTTOOL, "reline", "--in", os.path.join(RES, "Screen", "stall_counter.png"), "--out", tmp,
                    "--color", "#211D16", "--pick", "dark", "--scope", "colors", "--tol", "4",
                    "--report", os.path.join(tmp, "r.json")], check=True, env=env, stdout=subprocess.DEVNULL)
    counter = remap(Image.open(os.path.join(tmp, "stall_counter.png")).convert("RGBA"), COUNTER_MAP)
    save(counter, "Screen/stall_counter.png")
    vase = remap(load("Screen/stall_prop_vase.png"), VASE_MAP)
    save(vase, "Screen/stall_prop_vase.png")
    shutil.rmtree(tmp, ignore_errors=True)
    for rel, im in (("Screen/stall_counter.png", counter), ("Screen/stall_prop_vase.png", vase)):
        print(rel, "바꾼 칸", len(diff_pixels(load(rel), im)), "색 수", len(im.getcolors(9999)))


if __name__ == "__main__":
    run()

"""B-1 손님 16장 · 주인 1장 — 둘레 검정 선을 짙은 갈색(line 램프)으로.

1) 검정 선인 일곱 장(몸 · 머리통 셋 · 검은 머리 둘 · 주인)만 `arttool reline` (고리 · 어두운 칸 · tol 14).
2) 뒷손질 (툴이 못 하는 것)
   - 두꺼운 선의 안쪽 칸 : 고리 칸과 붙어 있고 투명에서 2칸 안인 같은 검정 → 같이 바꾼다 (눈동자는 3칸 넘게 안쪽이라 안 바뀐다).
   - 어두운 면(바지 · 검은 머리 · 검은 옷) 옆 선은 `#352E27` 대신 `#211D16` — 선이 면보다 밝아지면 안 되므로.
   - 검은 머리는 선 색 `#0F0E11` 을 그림 전체에서 `#211D16` 으로 (안쪽 결 선도 같은 선이다).
3) 색 있는 머리 여섯 · 윗옷 넷은 이미 제 색의 짙은 선이라 그대로 복사한다.
"""
import glob
import os
import shutil
import subprocess
import sys
import tempfile

from common import ARTTOOL, LINE, LINE_DARK, OUT_ALT, RES, load, lum, save

BLACK = ["Customer/body.png", "Customer/face_01.png", "Customer/face_02.png", "Customer/face_03.png",
         "Customer/hair_01_black.png", "Customer/hair_02_black.png", "Screen/stall_owner.png"]
WHOLE_COLOR = {"Customer/hair_01_black.png", "Customer/hair_02_black.png"}
TOL = 14
TOL_OF = {"Customer/body.png": 26}   # 몸은 선이 여러 검정(#070504 ~ #1C1D21)이라 폭을 넓힌다
EYES = (27, 62, 88, 84)              # 주인 눈 상자 (x0, y0, x1, y1) — 안쪽 선 판에서도 안 건드린다
NEAR = 2          # 투명에서 이 칸 안쪽까지만 선으로 본다
DARK_FILL = 75    # 옆 면 밝기가 이 아래면 어두운 선 색


def near_alpha(px, w, h, x, y, r):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            nx, ny = x + dx, y + dy
            if nx < 0 or ny < 0 or nx >= w or ny >= h or px[nx, ny][3] == 0:
                return True
    return False


def owner_inner(orig, out, line, blackish):
    """고를 판 : 주인의 안쪽 가는 검정 선(머리 결 · 얼굴 둘레)도 갈색으로. 눈 상자와 검은 옷 · 모자 면은 그대로."""
    w, h = orig.size
    po = orig.load()
    alt = out.copy()
    pa = alt.load()
    n = 0
    for y in range(h):
        for x in range(w):
            if (x, y) in line or not po[x, y][3] or po[x, y][:3] not in blackish:
                continue
            if EYES[0] <= x <= EYES[2] and EYES[1] <= y <= EYES[3]:
                continue
            near = [po[x + dx, y + dy] for dy in (-1, 0, 1) for dx in (-1, 0, 1)
                    if 0 <= x + dx < w and 0 <= y + dy < h]
            light = [c for c in near if c[3] and c[:3] not in blackish and lum(c) > 110]
            thick = sum(1 for dy in range(-2, 3) for dx in range(-2, 3)
                        if 0 <= x + dx < w and 0 <= y + dy < h and po[x + dx, y + dy][3]
                        and lum(po[x + dx, y + dy]) < 60)
            if len(light) >= 2 and thick <= 12:
                pa[x, y] = LINE + (255,)
                n += 1
    save(alt, "Screen/stall_owner.png", OUT_ALT)
    print("주인 안쪽 선 판 (out_alt) : 더 바꾼 칸", n)


def run():
    tmp = tempfile.mkdtemp(prefix="b1_")
    src, dst = os.path.join(tmp, "in"), os.path.join(tmp, "out")
    os.makedirs(src)
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    for rel in BLACK:
        one = os.path.join(src, os.path.basename(rel))
        shutil.copy(os.path.join(RES, rel), one)
        subprocess.run([ARTTOOL, "reline", "--in", one, "--out", dst, "--color", "#352E27", "--pick", "dark",
                        "--scope", "ring", "--tol", str(TOL_OF.get(rel, TOL)),
                        "--report", os.path.join(tmp, "reline.json")],
                       check=True, env=env, stdout=subprocess.DEVNULL)
    summary = []
    for rel in BLACK:
        from PIL import Image
        orig = load(rel)
        rl = Image.open(os.path.join(dst, os.path.basename(rel))).convert("RGBA")
        w, h = orig.size
        po, pr = orig.load(), rl.load()
        line = {(x, y) for y in range(h) for x in range(w) if po[x, y] != pr[x, y]}
        ring_n = len(line)
        blackish = {po[p][:3] for p in line}
        # 두꺼운 선 안쪽 칸 채우기
        todo = list(line)
        while todo:
            x, y = todo.pop()
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    q = (x + dx, y + dy)
                    if q in line or not (0 <= q[0] < w and 0 <= q[1] < h):
                        continue
                    c = po[q]
                    if c[3] and c[:3] in blackish and near_alpha(po, w, h, q[0], q[1], NEAR):
                        line.add(q)
                        todo.append(q)
        if rel in WHOLE_COLOR:
            main = max(blackish, key=lambda c: sum(1 for p in line if po[p][:3] == c))
            line |= {(x, y) for y in range(h) for x in range(w) if po[x, y][3] and po[x, y][:3] == main}
        out = orig.copy()
        pw = out.load()
        dark_n = 0
        for (x, y) in line:
            ls = []
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    q = (x + dx, y + dy)
                    if 0 <= q[0] < w and 0 <= q[1] < h and q not in line and po[q][3]:
                        ls.append(lum(po[q]))
            dark = bool(ls) and sum(ls) / len(ls) < DARK_FILL
            if rel in WHOLE_COLOR:
                dark = True
            if "face_" in rel:
                dark = False        # 머리통은 선 한 색만 (색 수 16 한도 · 어두운 면이 없다)
            dark_n += dark
            pw[x, y] = (LINE_DARK if dark else LINE) + (255,)
        save(out, rel)
        summary.append((rel, ring_n, len(line), dark_n))
        if rel == "Screen/stall_owner.png":
            owner_inner(orig, out, line, blackish)
    for f in sorted(glob.glob(os.path.join(RES, "Customer", "*.png"))):
        rel = "Customer/" + os.path.basename(f)
        if rel not in BLACK:
            save(load(rel), rel)
            summary.append((rel, 0, 0, 0))
    shutil.rmtree(tmp, ignore_errors=True)
    print("파일 | reline 고리 칸 | 바꾼 칸 합 | 그중 어두운 선")
    for s in summary:
        print(" | ".join(str(v) for v in s))


if __name__ == "__main__":
    sys.exit(run())

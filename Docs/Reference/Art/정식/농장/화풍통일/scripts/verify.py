# C 갈래 산출물 확인 — 고친 그림이 원본(Resources)과 캔버스 크기 · 알파(실루엣)가 같고 반투명이 없는지 본다.
# 몸 4장은 선 칸(#6E6E6E) 자리도 그대로인지 본다. 하나라도 다르면 종료 1.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/verify.py
import os
import sys
import numpy as np
from common import OUT, SIAN, WORK, RES, LINE_NOW, BODIES, op, colors_of

PLATE_OUT = os.path.abspath(os.path.join(WORK, '..', '..', '합성소', '화풍통일', 'out'))
PAIRS = [(os.path.join(OUT, n), os.path.join(RES, 'Mozzi', n)) for n in BODIES]
PAIRS += [(os.path.join(OUT, 'decor_%02d.png' % i), os.path.join(RES, 'Farm', 'decor_%02d.png' % i)) for i in range(7)]
PAIRS += [(os.path.join(PLATE_OUT, 'synthesis_plate.png'), os.path.join(RES, 'Screen', 'synthesis_plate.png'))]
for d in sorted(os.listdir(SIAN)):                             # 시안 몸 (선 색 후보 · 명암 견줌)
    if os.path.isdir(os.path.join(SIAN, d)):
        PAIRS += [(os.path.join(SIAN, d, n), os.path.join(RES, 'Mozzi', n)) for n in BODIES if os.path.exists(os.path.join(SIAN, d, n))]


def main():
    bad = 0
    print('| 그림 | 크기 | 알파 다른 칸 | 반투명 | 색 수 (원본 → 고친 뒤) | 색 바뀐 칸 | 선 자리 |')
    print('| --- | --- | --- | --- | --- | --- | --- |')
    for new_path, old_path in PAIRS:
        new, old = op(new_path), op(old_path)
        a, b = np.array(new), np.array(old)
        size_ok = new.size == old.size
        alpha_diff = int((a[..., 3] != b[..., 3]).sum()) if size_ok else -1
        semi = int(((a[..., 3] > 0) & (a[..., 3] < 255)).sum())
        changed = int(((a[..., :3] != b[..., :3]).any(-1) & (b[..., 3] > 0)).sum()) if size_ok else -1
        line = '—'
        if os.path.basename(new_path) in BODIES and size_ok:
            old_line = (b[..., :3] == np.array(LINE_NOW, dtype=np.uint8)).all(-1) & (b[..., 3] > 0)
            new_cols = {tuple(int(v) for v in c) for c in a[old_line][:, :3]}
            fill_same = len(new_cols) == 1                    # 옛 선 칸이 모두 한 색인가 (그 색이 새 선 색)
            new_line = (a[..., :3] == np.array(next(iter(new_cols)), dtype=np.uint8)).all(-1) & (a[..., 3] > 0)
            line = '같음' if fill_same and np.array_equal(new_line, old_line) else '다름'
        ok = size_ok and alpha_diff == 0 and semi == 0 and line != '다름'
        bad += 0 if ok else 1
        rel = os.path.relpath(new_path, WORK).replace('\\', '/')
        print('| `%s` | %d×%d %s | %d | %d | %d → %d | %d | %s |' % (rel, new.width, new.height, '같음' if size_ok else '다름',
                                                                   alpha_diff, semi, len(colors_of(old)), len(colors_of(new)), changed, line))
    print('\n결과 :', '모두 통과' if bad == 0 else '%d장 어긋남' % bad)
    sys.exit(0 if bad == 0 else 1)


if __name__ == '__main__':
    main()

# C-5 — 합성 기계 둥근 창의 가림판(fx_window_mask). 반복 06 피드백 13 「두 마리가 창 안에서 엉켜 돈다」 용.
# 창 유리 칸을 그대로 잘라 흰색 한 색(불투명)으로 바꾸고, 유리 밖은 투명으로 둔다. 새로 칠하는 칸은 없다.
#
# 유리 칸 고르기 : 창 가운데 (95,116) 에서 시작해 유리 회색 여섯 색으로 이어진 칸을 모두 줍는다(번져 채우기).
#   테(놋쇠 · 갈색)와 둘레선은 색이 달라 안 들어간다. 줄마다 왼끝~오른끝을 채워 유리 안 작은 구멍도 메운다.
# 잰 값 (synthesis_machine.png 208×220) : 유리 x 65~124 · y 86~145 → 60×60, 가운데 (95.0, 116.0) (칸 경계 기준).
#   코드의 SynthesisRoomLayout.WindowX/Y = 97/117 · 반지름 31 과 2px · 1px 어긋난다 — 코드 갈래가 맞춘다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/합성소/화풍통일/scripts/c5_window_mask.py
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = os.path.dirname(HERE)
ROOT = os.path.abspath(os.path.join(WORK, '..', '..', '..', '..', '..', '..'))
SRC = os.path.join(WORK, '..', 'out', 'synthesis_machine.png')            # 정본 (Resources 사본과 같다)
OUT = os.path.join(WORK, 'out', 'Fx', 'fx_window_mask.png')
SHOT = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '점검', '2026-10-05', '반복06', 'window_mask_x3.png')
SEED = (95, 116)
GLASS = {(0x81, 0x75, 0x6A), (0x8F, 0x82, 0x76), (0xA6, 0x97, 0x89), (0xB8, 0xA7, 0x97),
         (0xC9, 0xB5, 0xA3), (0x78, 0x69, 0x5F)}
WHITE = (255, 255, 255, 255)


def glass_cells(im):
    p = im.load()
    seen, stack = set(), [SEED]
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < im.width and 0 <= y < im.height) or p[x, y][:3] not in GLASS:
            continue
        seen.add((x, y))
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return seen


def main():
    machine = Image.open(SRC).convert('RGBA')
    cells = glass_cells(machine)
    xs = [x for x, _ in cells]
    ys = [y for _, y in cells]
    x0, y0, x1, y1 = min(xs), min(ys), max(xs), max(ys)
    mask = Image.new('RGBA', (x1 - x0 + 1, y1 - y0 + 1), (0, 0, 0, 0))
    m = mask.load()
    for y in range(y0, y1 + 1):
        row = [x for x, yy in cells if yy == y]
        for x in range(min(row), max(row) + 1):        # 줄마다 끝에서 끝까지 — 유리 안 구멍 메움
            m[x - x0, y - y0] = WHITE
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    mask.save(OUT)
    print('fx_window_mask.png %dx%d · 유리 x %d~%d y %d~%d · 가운데 (%.1f, %.1f) · 흰 칸 %d (유리 칸 %d)' % (
        mask.width, mask.height, x0, x1, y0, y1, (x0 + x1 + 1) / 2, (y0 + y1 + 1) / 2,
        mask.getchannel('A').histogram()[255], len(cells)))
    # 확인판 ×3 : 왼쪽 기계 그대로 · 오른쪽 가림판을 창 자리에 얹은 것
    k = 3
    board = Image.new('RGBA', (machine.width * 2 + 8, machine.height), (243, 215, 175, 255))
    board.alpha_composite(machine, (0, 0))
    over = machine.copy()
    over.alpha_composite(mask, (x0, y0))
    board.alpha_composite(over, (machine.width + 8, 0))
    os.makedirs(os.path.dirname(SHOT), exist_ok=True)
    board.resize((board.width * k, board.height * k), Image.NEAREST).save(SHOT)


if __name__ == '__main__':
    main()

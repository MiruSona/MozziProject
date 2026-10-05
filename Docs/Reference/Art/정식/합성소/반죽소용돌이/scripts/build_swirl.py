# 합성 기계 창 속 「두 색 반죽 소용돌이」 — PixelLab 이 뽑은 프레임을 겹 셋으로 가르고 시트 · 견줌판 · GIF 를 묶는다.
# 새로 그리는 칸은 없다. 하는 일 : 60×60 자르기 · 색으로 겹 가르기 · 회색 단계로 바꾸기 · 외곽선 한 색 · 창 원 가림 · 늘어놓기.
#
# 겹 (아래에서 위로)
#   a   : 첫째 반죽 (raw 의 분홍)  — 흰색~회색 세 단계. 코드가 종 색을 곱한다
#   b   : 둘째 반죽 (raw 의 초록)  — 같다
#   top : 외곽선(갈색 한 색) + 반짝이(흰색) — 색을 곱하지 않는다
# raw 의 회색 칸(반죽 사이 빈틈)은 투명으로 둔다 — 기계 창 유리가 비친다.
#
# 돌리는 법 (저장소 뿌리에서) :
#   Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/합성소/반죽소용돌이/scripts/build_swirl.py
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = os.path.dirname(HERE)
ROOT = os.path.abspath(os.path.join(WORK, '..', '..', '..', '..', '..', '..'))
RAW = os.path.join(WORK, 'raw')
OUT = os.path.join(WORK, 'out', 'Fx')
FRAMES = os.path.join(OUT, 'frames')
MASK = os.path.join(WORK, '..', '화풍통일', 'out', 'Fx', 'fx_window_mask.png')
MACHINE = os.path.join(WORK, '..', 'out', 'synthesis_machine.png')
SHOT = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '점검', '2026-10-05', '반복06')

SIZE = 60                       # 창 유리 안쪽 60×60
CROP = (2, 2)                   # 64×64 raw 에서 떼는 왼쪽 위 (소용돌이 가운데 = (32,32))
WINDOW = (65, 86)               # 기계 그림 안 창 왼쪽 위
# 도는 고리 : 16프레임 한 바퀴 판에서 한 장 걸러 8장 (0·1 은 색이 옅은 첫 그림이라 뺀다. 16 은 0 과 같은 자세)
SPIN = [('spin_minimax16_b', i) for i in (2, 4, 6, 8, 10, 12, 14, 16)]
# 꽉 찬 밑그림(base_fill_g)으로 다시 뽑은 판
SPIN_FULL = [('spin_fill16_g', i) for i in (2, 4, 6, 8, 10, 12, 14, 16)]
CANON = 'regen'                 # out/Fx 정본으로 둘 판 : fill_near | regen (사용자가 regen 을 골랐다, 2026-10-05)

OUTLINE = (0x3A, 0x2E, 0x25, 255)   # 합성 기계 그림의 외곽선 갈색 (line 램프)
WHITE = (255, 255, 255, 255)
LEVELS = ((0.90, 255), (0.70, 220), (0.0, 180))   # 바탕색 대비 밝기 → 회색 단계
REF_LUM = {'a': 157.0, 'b': 202.0}                # 분홍 · 초록 바탕색의 밝기
FILL_GRAY = 180                                   # 빈틈을 채우는 그늘 회색
PAIRS = [('빨강+초록', (0xE8, 0x5D, 0x5D), (0x7C, 0xC5, 0x76)),
         ('흰+파랑', (0xFF, 0xFF, 0xFF), (0x5D, 0xA0, 0xE8)),
         ('주황+노랑', (0xF0, 0xA0, 0x4B), (0xF5, 0xD7, 0x5A))]


def lum(r, g, b):
    return 0.299 * r + 0.587 * g + 0.114 * b


def classify(px):
    """raw 한 칸 → (겹 이름, 넣을 색). 투명이면 (None, None)."""
    r, g, b, a = px
    if a < 128:
        return None, None
    hi, lo = max(r, g, b), min(r, g, b)
    if hi < 110:
        return 'top', OUTLINE
    if lo > 215:
        return 'top', WHITE
    if hi - lo < 24:                      # 빈틈 회색
        return None, None
    name = 'a' if r > g else 'b'
    ratio = lum(r, g, b) / REF_LUM[name]
    v = next(val for cut, val in LEVELS if ratio >= cut)
    return name, (v, v, v, 255)


def split(raw, mask, fill=None):
    """fill : None 빈틈 그대로 · 'near' 빈틈을 가까운 반죽 겹에 회색 180 으로 붙인다 · 'raw' 는 None 과 같다."""
    layers = {k: Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0)) for k in ('a', 'b', 'top')}
    src, m = raw.load(), mask.load()
    owner, empty = {}, []
    for y in range(SIZE):
        for x in range(SIZE):
            if m[x, y][3] < 128:          # 창 원 밖
                continue
            name, color = classify(src[x + CROP[0], y + CROP[1]])
            if name:
                layers[name].putpixel((x, y), color)
                if name != 'top':
                    owner[(x, y)] = name
            else:
                empty.append((x, y))
    if fill == 'near' and owner:
        fill_gaps(layers, owner, empty, m)
    return layers


def fill_gaps(layers, owner, empty, m):
    """빈 칸마다 가장 가까운 반죽(a · b)을 찾아 그 겹에 그늘 회색을 넣는다. 외곽선 · 반짝이 칸은 지나가기만 한다."""
    dist = dict(owner)                    # 칸 → 가장 가까운 반죽 이름 (번져 가기)
    front = list(owner)
    while front:
        nxt = []
        for x, y in front:
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < SIZE and 0 <= ny < SIZE and m[nx, ny][3] >= 128 and (nx, ny) not in dist:
                    dist[(nx, ny)] = dist[(x, y)]
                    nxt.append((nx, ny))
        front = nxt
    for xy in empty:
        if xy in dist:
            layers[dist[xy]].putpixel(xy, (FILL_GRAY, FILL_GRAY, FILL_GRAY, 255))


def tint(layer, color):
    out = layer.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if a:
                p[x, y] = (r * color[0] // 255, g * color[1] // 255, b * color[2] // 255, 255)
    return out


def compose(layers, ca, cb):
    im = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    im.alpha_composite(tint(layers['a'], ca))
    im.alpha_composite(tint(layers['b'], cb))
    im.alpha_composite(layers['top'])
    return im


def build(kind, picks, mask, out=None, fill=None, crop=None):
    """낱장 + 겹마다 가로 시트를 쓰고 겹 목록을 돌려준다. out 이 없으면 정본 자리(out/Fx)."""
    global CROP
    out = out or OUT
    os.makedirs(os.path.join(out, 'frames'), exist_ok=True)
    keep, CROP = CROP, crop or CROP
    frames = []
    for i, (folder, idx) in enumerate(picks):
        raw = Image.open(os.path.join(RAW, folder, 'f%d.png' % idx)).convert('RGBA')
        layers = split(raw, mask, fill)
        for name, im in layers.items():
            im.save(os.path.join(out, 'frames', 'fx_%s_%s_%d.png' % (kind, name, i)))
        frames.append(layers)
    CROP = keep
    for name in ('a', 'b', 'top'):
        strip = Image.new('RGBA', (SIZE * len(frames), SIZE), (0, 0, 0, 0))
        for i, layers in enumerate(frames):
            strip.alpha_composite(layers[name], (i * SIZE, 0))
        strip.save(os.path.join(out, 'fx_%s_%s_strip.png' % (kind, name)))
    print('%s : %d프레임 · %d×%d · 겹 a/b/top · 채움 %s · %s' % (kind, len(frames), SIZE, SIZE, fill, os.path.relpath(out, WORK)))
    return frames


def on_machine(machine, swirl, box):
    im = machine.copy()
    im.alpha_composite(swirl, WINDOW)
    return im.crop(box)


def rows_board(rows, machine, path):
    """rows = [(겹 목록, 색 a, 색 b), …] 를 줄마다 프레임을 늘어놓아 ×4 로 쓴다."""
    k, gap, bg = 4, 4, (243, 215, 175, 255)
    box = (45, 66, 145, 166)                         # 창 둘레 100×100
    cw = box[2] - box[0]
    n = max(len(r[0]) for r in rows)
    board = Image.new('RGBA', (n * (cw + gap) + gap, len(rows) * (cw + gap) + gap), bg)
    for r, (seq, ca, cb) in enumerate(rows):
        for c, layers in enumerate(seq):
            cell = on_machine(machine, compose(layers, ca, cb), box)
            board.alpha_composite(cell, (gap + c * (cw + gap), gap + r * (cw + gap)))
    board.resize((board.width * k, board.height * k), Image.NEAREST).save(path)


def board_and_gif(spin, machine):
    k, gap, bg = 4, 4, (243, 215, 175, 255)
    box = (45, 66, 145, 166)
    cw = box[2] - box[0]
    # 1) 늘어놓은 판 : 줄 = 색 벌, 칸 = 프레임
    rows_board([(spin, ca, cb) for _, ca, cb in PAIRS], machine, os.path.join(SHOT, 'swirl_frames_x4.png'))

    # 2) 겹만 늘어놓은 판 (a · b · top · 합친 것) — 코드 갈래가 겹 꼴을 볼 때
    lay = Image.new('RGBA', (len(spin) * (SIZE + gap) + gap, 4 * (SIZE + gap) + gap), (120, 110, 100, 255))
    for c, layers in enumerate(spin):
        cells = [layers['a'], layers['b'], layers['top'], compose(layers, PAIRS[0][1], PAIRS[0][2])]
        for r, cell in enumerate(cells):
            lay.alpha_composite(cell, (gap + c * (SIZE + gap), gap + r * (SIZE + gap)))
    lay.resize((lay.width * k, lay.height * k), Image.NEAREST).save(os.path.join(SHOT, 'swirl_layers_x4.png'))

    # 3) GIF : 색 벌 셋을 옆으로
    gif = []
    for layers in spin:
        fr = Image.new('RGBA', (len(PAIRS) * (cw + gap) + gap, cw + 2 * gap), bg)
        for c, (_, ca, cb) in enumerate(PAIRS):
            fr.alpha_composite(on_machine(machine, compose(layers, ca, cb), box), (gap + c * (cw + gap), gap))
        gif.append(fr.resize((fr.width * k, fr.height * k), Image.NEAREST).convert('RGB'))
    gif[0].save(os.path.join(SHOT, 'swirl_loop_x4.gif'), save_all=True, append_images=gif[1:], duration=110, loop=0)

    # 4) GIF : 기계 한 대 통째 (빨강+초록)
    gif = []
    for layers in spin:
        fr = Image.new('RGBA', machine.size, bg)
        fr.alpha_composite(on_machine(machine, compose(layers, PAIRS[0][1], PAIRS[0][2]), (0, 0) + machine.size))
        gif.append(fr.resize((fr.width * k, fr.height * k), Image.NEAREST).convert('RGB'))
    gif[0].save(os.path.join(SHOT, 'swirl_machine_x4.gif'), save_all=True, append_images=gif[1:], duration=110, loop=0)


def main():
    os.makedirs(SHOT, exist_ok=True)
    mask = Image.open(MASK).convert('RGBA')
    machine = Image.open(MACHINE).convert('RGBA')
    alt = os.path.join(WORK, 'out', 'alt')
    # 세 판 : 앞 판(빈틈 있음) · 빈틈을 가까운 반죽으로 채운 판 · 꽉 찬 밑그림으로 다시 뽑은 판
    cands = {
        'gap': dict(picks=SPIN, fill=None, crop=CROP),
        'fill_near': dict(picks=SPIN, fill='near', crop=CROP),
        'regen': dict(picks=SPIN_FULL, fill='near', crop=CROP),
    }
    built = {}
    for name, c in cands.items():
        if not os.path.isdir(os.path.join(RAW, c['picks'][0][0])):
            continue
        out = OUT if name == CANON else os.path.join(alt, name)
        built[name] = build('swirl', c['picks'], mask, out=out, fill=c['fill'], crop=c['crop'])
    board_and_gif(built[CANON], machine)
    two = PAIRS[:2]
    rows_board([(built['gap'], ca, cb) for _, ca, cb in two] + [(built[CANON], ca, cb) for _, ca, cb in two],
               machine, os.path.join(SHOT, 'swirl_fill_compare_x4.png'))
    others = [n for n in built if n not in ('gap', CANON)]
    if others:                                        # 정본이 안 된 채움 판도 한 장으로
        rows_board([(built[others[0]], ca, cb) for _, ca, cb in two], machine,
                   os.path.join(SHOT, 'swirl_fill_alt_x4.png'))
        k, gap, bg, box = 4, 4, (243, 215, 175, 255), (45, 66, 145, 166)
        gif = []
        for layers in built[others[0]]:
            fr = Image.new('RGBA', (len(PAIRS) * (100 + gap) + gap, 100 + 2 * gap), bg)
            for c, (_, ca, cb) in enumerate(PAIRS):
                fr.alpha_composite(on_machine(machine, compose(layers, ca, cb), box), (gap + c * (100 + gap), gap))
            gif.append(fr.resize((fr.width * k, fr.height * k), Image.NEAREST).convert('RGB'))
        gif[0].save(os.path.join(SHOT, 'swirl_fill_alt_loop_x4.gif'), save_all=True, append_images=gif[1:],
                    duration=110, loop=0)


if __name__ == '__main__':
    main()

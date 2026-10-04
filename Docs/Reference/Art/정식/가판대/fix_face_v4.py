"""손님 3차d — 머리통 겹 셋(face_01~03)의 턱 밑 가장자리와 입만 손으로 고친다.

사용자 허락(2026-10-04)으로 PixelLab 대신 점을 직접 찍는다. 범위는 턱 밑과 입뿐이다.
- 바탕 : raw/cust3_face_0{1,2,3}_v2.png (3차b 까지 사용자가 좋다고 한 판)
- 결과 : out/cust3_side_face_0{1,2,3}.png (이름 그대로 덮는다)
- 기록 : raw/cust3_face_v4_edits.json (얼굴마다 고친 점 좌표 · 옛 색 · 새 색)

새 색은 만들지 않는다. 얼굴마다 그 파일에 이미 있는 색만 쓴다 (외곽선 · 살색 두 단 · 입 선 색).
눈 · 눈썹 · 볼터치 · 귀 · 두피 · 옆얼굴 윤곽(x 96 부터 오른쪽)은 건드리지 않는다.

턱 밑 모양은 세 얼굴이 같다 — 아래 OUTLINE 표가 세로줄마다 외곽선 1 px 이 놓일 줄이다.
몸 겹의 턱 밑(외곽선)보다 위에 두면 몸 겹이 밑으로 비쳐 선이 두 겹이 되므로, 몸 겹 바닥과 같거나 아래에 둔다.
목 자리(x 57~68)는 외곽선을 긋지 않고 그늘 살색으로 채워 몸의 목과 끊김 없이 잇는다.
"""
import json
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw')
OUT = os.path.join(HERE, 'out')
FACES = ('01', '02', '03')

# ── 턱 밑 외곽선 : 세로줄 x → 외곽선 줄 y (세 얼굴 공통). 1 px 씩 고른 계단.
OUTLINE = {}
for xs, y in (((40, 41), 102), ((42, 44), 103), ((45, 48), 104), ((49, 55), 105), ((56, 56), 106),
              ((69, 85), 109), ((86, 90), 108), ((91, 92), 107), ((93, 94), 106), ((95, 95), 105)):
    for x in range(xs[0], xs[1] + 1):
        OUTLINE[x] = y

# ── 목 자리 : 외곽선 없이 그늘 살색. x → 그늘을 채울 마지막 줄 (그 아래는 비워 몸의 목이 보이게).
#    x 57~59 는 107 부터 후드가 덮으므로 106 까지만, x 60~68 은 108 까지.
NECK = {x: (106 if x <= 59 else 108) for x in range(57, 69)}
NECK_TOP = 106          # 목 자리에서 그늘로 칠하는 첫 줄
BAND = 3                # 외곽선 위 몇 줄까지 낱점(살색 아닌 점)을 살색으로 바꾸나
CLEAR_TO = 112          # 외곽선 · 목 자리 아래는 이 줄까지 비운다

# ── 입 : 지울 네모(x0, y0, x1, y1 포함)와 찍을 1 px 선. 네모 안의 살색 아닌 점은 살색으로 지운다.
MOUTH = {
    '01': {'erase': (89, 96, 96, 99), 'line': [(91, 97), (92, 98), (93, 98), (94, 98)],            # 보통 : 짧은 선 · 안쪽 끝 1 px 올라감
           'also': [(92, 97)]},                                                                    # 옛 입 덩이에 붙은 그늘 한 점
    '02': {'erase': (89, 96, 96, 100), 'line': [(91, 97), (92, 98), (93, 98), (94, 98), (95, 97)]},  # 기쁨 : 작은 웃는 호
    '03': {'erase': (85, 97, 97, 102), 'line': [(90, 100), (91, 99), (92, 99), (93, 99), (94, 100)],  # 시무룩 : 처진 호
           'also': [(91, 96), (91, 97)]},                                                          # 옛 입 위 그늘 두 점
}


def rgb(a, x, y):
    return tuple(int(v) for v in a[y, x, :3])


def palette(a):
    """그 파일의 색 → 점 수."""
    cnt = {}
    for y, x in zip(*np.where(a[..., 3] > 0)):
        c = rgb(a, x, y)
        cnt[c] = cnt.get(c, 0) + 1
    return cnt


def roles(a):
    """얼굴마다 쓸 색 : 외곽선(가장 많은 짙은 색) · 살색(밝은 살색 중 많은 것들) · 그늘 · 입 선(짙은 갈색)."""
    cnt = palette(a)
    by = sorted(cnt, key=lambda c: -cnt[c])
    outline = next(c for c in by if max(c) < 40)
    skins = [c for c in by if c[0] > 240 and c[1] > 205 and cnt[c] > 200]       # 바탕 살색 (얼굴마다 1~2 개)
    shade = next(c for c in by if c[0] > 240 and 170 < c[1] < 190 and cnt[c] > 200)
    # 입 선 : 외곽선보다 한 단 옅은 짙은 갈색 (R 70~90 · G < 45). 파일에 있는 것 중 가장 많은 것.
    mouth = next(c for c in by if 70 <= c[0] <= 90 and c[1] < 45 and c[2] < 45)
    return {'outline': outline, 'skins': skins, 'shade': shade, 'mouth': mouth}


def fill_skin(a, pts, r, frozen):
    """pts 의 점을 둘레 8 칸에서 가장 많은 살색으로 바꾼다. 둘레에 살색이 없으면 다음 바퀴로 미룬다."""
    ok = set(r['skins']) | {r['shade']}
    todo = list(pts)
    for _ in range(10):
        nxt = []
        decided = {}
        for x, y in todo:
            votes = {}
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    if dx == dy == 0:
                        continue
                    xx, yy = x + dx, y + dy
                    if (xx, yy) in todo or (xx, yy) in frozen or not a[yy, xx, 3]:
                        continue
                    c = rgb(a, xx, yy)
                    if c in ok:
                        votes[c] = votes.get(c, 0) + 1
            if votes:
                decided[(x, y)] = max(votes, key=lambda c: (votes[c], c == r['skins'][0]))
            else:
                nxt.append((x, y))
        for (x, y), c in decided.items():
            a[y, x, :3] = c
            a[y, x, 3] = 255
        todo = nxt
        if not todo:
            break
    for x, y in todo:                       # 끝까지 못 정한 점은 바탕 살색
        a[y, x, :3] = r['skins'][0]
        a[y, x, 3] = 255


def fix(face):
    src = np.array(Image.open(os.path.join(RAW, f'cust3_face_{face}_v2.png')).convert('RGBA'))
    a = src.copy()
    r = roles(a)
    ok = set(r['skins']) | {r['shade']}
    out_c = np.array(r['outline'], dtype=np.uint8)

    # 1) 턱 밑 : 외곽선 아래는 비우고, 외곽선 줄은 외곽선 색, 그 위 BAND 줄의 낱점은 살색으로.
    frozen = set()
    to_fill = []
    for x, ty in OUTLINE.items():
        for y in range(ty + 1, CLEAR_TO + 1):
            a[y, x] = 0
        a[ty, x, :3] = out_c
        a[ty, x, 3] = 255
        frozen.add((x, ty))
        for y in range(ty - BAND, ty):
            if not a[y, x, 3] or rgb(a, x, y) not in ok:
                to_fill.append((x, y))
    # 2) 목 자리 : NECK_TOP ~ 끝 줄은 그늘 살색, 그 아래는 비움. 그 위 BAND 줄 낱점은 살색으로.
    for x, last in NECK.items():
        for y in range(NECK_TOP, last + 1):
            a[y, x, :3] = r['shade']
            a[y, x, 3] = 255
            frozen.add((x, y))
        for y in range(last + 1, CLEAR_TO + 1):
            a[y, x] = 0
        for y in range(NECK_TOP - BAND, NECK_TOP):
            if not a[y, x, 3] or rgb(a, x, y) not in ok:
                to_fill.append((x, y))
    fill_skin(a, to_fill, r, frozen)

    # 3) 입 : 네모 안 살색 아닌 점을 지우고 1 px 선을 찍는다.
    m = MOUTH[face]
    x0, y0, x1, y1 = m['erase']
    line = set(m['line'])
    er = [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)
          if a[y, x, 3] and rgb(a, x, y) not in ok and (x, y) not in line]
    # 그늘 색이라 위 조건에 안 걸리는 옛 입 자국 : 바탕 살색으로 (둘레 투표 대신 바로)
    for x, y in m.get('also', []):
        a[y, x, :3] = r['skins'][0]
    fill_skin(a, er, r, line)
    for x, y in m['line']:
        a[y, x, :3] = r['mouth']
        a[y, x, 3] = 255

    # 기록 : 바뀐 점
    diff = np.any(a != src, axis=2)
    edits = []
    for y, x in zip(*np.where(diff)):
        edits.append({'x': int(x), 'y': int(y),
                      'old': [int(v) for v in src[y, x]], 'new': [int(v) for v in a[y, x]]})
    new_cols = set(palette(a)) - set(palette(src))
    assert not new_cols, f'face_{face} 새 색이 생겼다 : {new_cols}'
    assert set(np.unique(a[..., 3])) <= {0, 255}
    return a, r, edits


def main():
    log = {}
    for f in FACES:
        a, r, edits = fix(f)
        Image.fromarray(a).save(os.path.join(OUT, f'cust3_side_face_{f}.png'))
        log[f] = {'roles': {k: v for k, v in r.items()}, 'count': len(edits), 'edits': edits}
        print(f'face_{f}: 고친 점 {len(edits)} · 외곽선 {r["outline"]} · 살색 {r["skins"]} · 그늘 {r["shade"]} · 입 {r["mouth"]}')
    with open(os.path.join(RAW, 'cust3_face_v4_edits.json'), 'w', encoding='utf-8') as fp:
        json.dump(log, fp, ensure_ascii=False)


if __name__ == '__main__':
    main()

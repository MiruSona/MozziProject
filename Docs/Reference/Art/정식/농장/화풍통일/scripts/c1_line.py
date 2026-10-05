# C-1 — 찹쌀이 몸 4장의 선 색을 인자 하나로 바꿔 굽는다. 사용자가 선 색을 고르면 이것만 돌리면 된다.
# 읽는 것 : ../out/body_hop0~3.png (C-2 색 합치기가 끝난 판 · 선은 #6E6E6E).  칸 색만 바꾸고 실루엣은 그대로다.
#
# 돌리는 법 (저장소 뿌리에서)
#   시안으로 굽기 (기본 : ../시안/line_<색>/ 에 쓴다)
#     Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/c1_line.py --line "#6E6259"
#   고른 색을 정식으로 굽기 (../out/ 의 4장을 덮는다)
#     Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/농장/화풍통일/scripts/c1_line.py --line "#6E6259" --apply
#   그 뒤 프로필 `Docs/Reference/Art/화풍/profiles/mozzi_char.yaml` 의 palette.outline 도 같은 색으로 고쳐야 검사가 맞는다 (공용 파일 — 본줄 몫).
import argparse
import os
import numpy as np
from common import OUT, SIAN, LINE_NOW, BODIES, op, hex_rgb, rgb_hex, colors_of, remap, same_alpha


def find_line(im):
    """지금 선 색 — 실루엣 맨 바깥 1px 를 가장 많이 차지한 색."""
    a = np.array(im)
    solid = a[..., 3] > 0
    pad = np.pad(solid, 1)
    edge = solid & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    cols, cnt = np.unique(a[edge][:, :3], axis=0, return_counts=True)
    return tuple(int(v) for v in cols[cnt.argmax()])


def reline(im, line):
    old = find_line(im)
    if old == line:
        return im, old
    assert line not in colors_of(im), '새 선 색 %s 가 칠 색과 겹친다' % rgb_hex(line)
    out = remap(im, {old: line})
    assert same_alpha(im, out)
    return out, old


def main():
    ap = argparse.ArgumentParser(description='찹쌀이 몸 4장의 선 색 바꾸기')
    ap.add_argument('--line', required=True, help='새 선 색 #RRGGBB')
    ap.add_argument('--out', default=None, help='쓸 폴더 (기본 ../시안/line_<색>/)')
    ap.add_argument('--apply', action='store_true', help='../out/ 의 4장을 덮어 쓴다 (고른 뒤에만)')
    args = ap.parse_args()
    line = hex_rgb(args.line)
    dst = OUT if args.apply else (args.out or os.path.join(SIAN, 'line_' + rgb_hex(line)[1:]))
    os.makedirs(dst, exist_ok=True)
    for name in BODIES:
        src = op(os.path.join(OUT, name))
        out, old = reline(src, line)
        out.save(os.path.join(dst, name))
        print('%s : 선 %s → %s (%d칸) · %d색' % (name, rgb_hex(old), rgb_hex(line), colors_of(out)[line], len(colors_of(out))))
    print('쓴 곳 :', dst)
    if args.apply and line != LINE_NOW:
        print('잊지 말 것 : mozzi_char.yaml 의 palette.outline 을 %s 로 (본줄 몫)' % rgb_hex(line))


if __name__ == '__main__':
    main()

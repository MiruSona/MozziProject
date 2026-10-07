"""blender10 내용물 겹 손질 — build_blender10.py 로 구운 겹의 흠을 메운다.

흠 : 유리 반사 · 칼날 축 · 받침처럼 내용물 색이 아닌 픽셀이 겹에서 빠져 구멍(투명)이 생겼고,
     그 구멍으로 몸통의 밝은 유리색이 비쳐 세로줄 · 가로줄 · 가운데 빈 곳 · 회색 조각으로 보였다.
손질 :
  1. 장마다 가장 큰 덩어리(4방향 이음)를 내용물 본체로 본다. 바깥 티끌은 그대로 둔다.
  2. 윗면에서 좁고 깊게 파인 곳(깊이 5px 넘게)만 양옆 윗면 높이로 올려 메운다. 얕은 물결은 그대로.
  3. 열마다 윗면~바닥 사이의 투명 칸, 파인 곳 안의 축 픽셀을 이웃 회색 평균으로 메운다.
  4. 층 마스크(_n{n}_L{층})와 기본 색 합본(blender10_{장}.png)을 고친 겹으로 다시 굽는다.
  5. 검증 그림 blender10_손질후3.png : fill_0·1·8·0장 층 합성 × 손질 전·지난 손질(PREV 폴더)·이번, 8배.
  (v3 · 10-07) 3번은 이웃 평균이 아니라 결 복사다 — 짧은 가로 토막마다 먼 열에서 같은 깊이 칸을 옮긴다.
     가운데 쇠봉 자리(x40..52) 윗면이 양옆보다 낮으면 양옆 높이로 올린다.
고치기 전 겹은 raw_before_fix/ 에 있다. 실행 : python fix_blender10.py (이 폴더에서) — raw_before_fix 를 읽어 다시 굽는다.
"""
import os
import random

from PIL import Image

import build_blender10 as b

HERE = b.HERE
SRC = os.path.join(HERE, "raw_before_fix")
DEEP = 5      # 이보다 깊게 파인 윗면만 메운다
WIN = 7       # 파인 곳을 찾는 반폭 (폭 13px 까지)
SPIKE = 2     # 솟은 축 가시를 찾는 반폭 (폭 3px 까지)


def main_body(px):
    seen, best = set(), set()
    for y in range(b.H):
        for x in range(b.W):
            if px[x, y][3] and (x, y) not in seen:
                comp, stack = set(), [(x, y)]
                seen.add((x, y))
                while stack:
                    cx, cy = stack.pop()
                    comp.add((cx, cy))
                    for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                        if 0 <= nx < b.W and 0 <= ny < b.H and px[nx, ny][3] and (nx, ny) not in seen:
                            seen.add((nx, ny))
                            stack.append((nx, ny))
                if len(comp) > len(best):
                    best = comp
    return best


def fix(fill):
    px = fill.load()
    body = main_body(px)
    xs = sorted({x for x, _ in body})
    top = {x: min(y for bx, y in body if bx == x) for x in xs}
    # 칼날 축 가시 : 축 픽셀이 내용물로 잡혀 윗면 위로 1~2px 폭 기둥이 솟았다 (열린 윗면으로 깎는다).
    # 그대로 두면 그 열만 층 경계가 어긋나 세로줄이 된다. 깎으면 몸통의 축이 보인다.
    ero = {x: max(top[k] for k in xs if abs(k - x) <= SPIKE) for x in xs}
    opn = {x: min(ero[k] for k in xs if abs(k - x) <= SPIKE) for x in xs}
    for x in xs:
        if opn[x] - top[x] > DEEP:
            for y in range(top[x], opn[x]):
                if (x, y) in body:
                    px[x, y] = (0, 0, 0, 0)
                    body.discard((x, y))
            top[x] = min(y for bx, y in body if bx == x)
    # 얕은 축 가시 (깊이 2~5, 폭 3 이하) : 몸통에서 축을 지웠으니 가운데(x40..52)에 남은 것도 깎는다.
    # 기준 높이 = 양옆(x±2..4) 윗면의 중앙값.
    for x in [k for k in xs if 40 <= k <= 52]:
        side = sorted(top[k] for k in xs if 2 <= abs(k - x) <= 4)
        ref = side[len(side) // 2]
        if ref - top[x] >= 2:
            for y in range(top[x], ref):
                if (x, y) in body:
                    px[x, y] = (0, 0, 0, 0)
                    body.discard((x, y))
    for x in [k for k in xs if 40 <= k <= 52]:
        top[x] = min(y for bx, y in body if bx == x)
    bot = {x: max(y for bx, y in body if bx == x) for x in xs}
    # 윗면 닫기 : 높이 h = -top 에 팽창(max) 뒤 침식(min)
    dil = {x: min(top[k] for k in xs if abs(k - x) <= WIN) for x in xs}
    clo = {x: max(dil[k] for k in xs if abs(k - x) <= WIN) for x in xs}
    todo = set()
    for x in xs:
        if top[x] - clo[x] > DEEP:  # 파인 곳 : 새 윗면부터 바닥 쪽 이어진 덩어리 위까지 다시 칠한다
            y = bot[x]
            while y > clo[x] and px[x, y][3]:
                y -= 1
            todo |= {(x, yy) for yy in range(clo[x], y + 1)}
        else:
            todo |= {(x, y) for y in range(top[x], bot[x] + 1) if not px[x, y][3]}
    ntop = {x: (clo[x] if top[x] - clo[x] > DEEP else top[x]) for x in xs}
    # 쇠봉 자리 움푹 (v3) : 가운데(x40..52) 윗면이 양옆(x34..39 · x53..58)보다 1~DEEP px 낮으면
    # 양옆 높이를 이은 선까지 올려 메운다. 0장은 x40..52 가 58, 양옆이 56 이라 2px 홈으로 보였다.
    lm = sorted(ntop[k] for k in xs if 34 <= k <= 39)
    rm = sorted(ntop[k] for k in xs if 53 <= k <= 58)
    if lm and rm:
        L, R = lm[len(lm) // 2], rm[len(rm) // 2]
        for x in [k for k in xs if 40 <= k <= 52]:
            ref = round(L + (R - L) * (x - 39) / 14)
            if 0 < ntop[x] - ref <= DEEP:
                todo |= {(x, y) for y in range(ref, ntop[x])}
                ntop[x] = ref
    known = {(x, y): px[x, y][0] for y in range(b.H) for x in range(b.W)
             if px[x, y][3] and (x, y) not in todo}
    # 결 복사 (v3) : 칸마다 손 안 댄 열(4~30px 떨어진 곳)을 무작위로 골라 윗면에서 같은 깊이의 칸을 옮긴다.
    # v2 는 행마다 run 폭만큼 민 같은 조각을 붙여 1장 윗면 고리 무늬가 서너 번 되풀이돼 보였고,
    # 열 단위로 고르면 세로줄, 칸 단위로 고르면 자글자글해서 짧은 가로 토막 단위로 고른다. 옮길 칸이 없으면 5×5 이웃의 아는 칸에서 고른다.
    # 새 색은 만들지 않는다 (평균 메움 금지).
    rnd = random.Random(10)
    left = set(todo)
    tcols = {x for x, _ in todo}
    pool = [k for k in xs if k not in tcols and bot[k] - ntop[k] > 10]
    run_state = {}
    # 한 행 안에서 3~6칸 짧은 토막마다 원천 열을 새로 고르고, 토막 안에서는 원천도 한 칸씩 같이 민다
    # (가로 결은 살리고, 같은 조각이 되풀이되지는 않게).
    pset = set(pool)
    for (x, y) in sorted(todo, key=lambda t: (t[1], t[0])):
        if x not in ntop:
            continue
        st = run_state.get(y)
        if st and st[0] == x - 1 and st[2] > 0 and st[1] + 1 in pset:
            k, n = st[1] + 1, st[2] - 1
        else:
            cand = [c for c in pool if 4 <= abs(c - x) <= 30] or pool
            k, n = rnd.choice(cand), rnd.randint(2, 5)
        run_state[y] = (x, k, n)
        for _ in range(8):
            src = (k, ntop[k] + (y - ntop[x]))
            if src in known:
                known[(x, y)] = known[src]
                px[x, y] = (known[src],) * 3 + (255,)
                left.discard((x, y))
                break
            cand = [c for c in pool if 4 <= abs(c - x) <= 30] or pool
            k = rnd.choice(cand)
            run_state[y] = (x, k, n)
    while left:
        done = {}
        for (x, y) in left:
            vs = [known[(x + dx, y + dy)] for dx in range(-2, 3) for dy in range(-2, 3)
                  if (x + dx, y + dy) in known]
            if vs:
                done[(x, y)] = rnd.choice(vs)
        if not done:
            break
        for k, v in done.items():
            known[k] = v
            px[k[0], k[1]] = (v, v, v, 255)
        left -= set(done)
    # 마지막 깎기 : 덩어리에서 떨어져 있어 위에서 못 깎은 축 조각(가운데 x40..52, 윗면 위 6px 안)을 지운다.
    # 더 높이 뜬 알갱이는 둔다.
    ftop = {x: min([y for y in range(b.H) if px[x, y][3]] or [b.H]) for x in range(36, 57)}
    for x in range(40, 53):
        side = sorted(ftop[k] for k in range(x - 4, x + 5) if abs(k - x) >= 2)
        ref = side[len(side) // 2]
        for y in range(max(0, ref - 6), ref):
            if px[x, y][3]:
                px[x, y] = (0, 0, 0, 0)
    return fill, len(todo)


def main():
    fills = []
    for i in range(b.N):
        f, n = fix(Image.open(os.path.join(SRC, f"blender10_fill_{i}.png")).convert("RGBA"))
        print(f"장 {i} : 메운 칸 {n}")
        f.save(os.path.join(HERE, f"blender10_fill_{i}.png"))
        b.compose(f, lambda x, y: b.BASE).save(os.path.join(HERE, f"blender10_{i}.png"))
        fills.append(f)
        if i <= 5:  # 층 마스크 다시 굽기 (build_blender10.py 와 같은 방식)
            fp = f.load()
            for n in (2, 3):
                idx = b.layer_index(f, n)
                for L in range(n):
                    m = Image.new("RGBA", (b.W, b.H), (0, 0, 0, 0))
                    mp = m.load()
                    for (x, y), k in idx.items():
                        if k == L:
                            mp[x, y] = fp[x, y]
                    m.save(os.path.join(HERE, f"blender10_fill_{i}_n{n}_L{L}.png"))
    C = b.COL
    rows = []
    for cols in ([C["팥"], C["콩"], C["쑥"]], [C["팥"], C["쑥"]]):
        rows.append(b.row([b.compose(f, b.layered(f, i, cols)) for i, f in enumerate(fills)]))
    sheet = Image.new("RGBA", (rows[0].width, rows[0].height * 2), (40, 40, 40, 255))
    sheet.paste(rows[0], (0, 0))
    sheet.paste(rows[1], (0, rows[0].height))
    # 견줌판 (v3) : 줄 = fill_0 · fill_1 · fill_8 · 0장 층 합성(팥·콩·쑥), 칸 = 손질 전 · 지난 손질(v2) · 이번, 8배.
    # 지난 손질 겹은 PREV 폴더(없으면 건너뜀)에서 읽는다.
    prev_dir = os.environ.get("PREV", "")
    cols3 = [C["팥"], C["콩"], C["쑥"]]

    def cells(i, layer):
        vs = [Image.open(os.path.join(SRC, f"blender10_fill_{i}.png")).convert("RGBA")]
        pp = os.path.join(prev_dir, f"blender10_fill_{i}.png")
        if prev_dir and os.path.exists(pp):
            vs.append(Image.open(pp).convert("RGBA"))
        vs.append(fills[i])
        if layer:
            return [b.compose(v, b.layered(v, i, cols3)) for v in vs]
        return [b.compose(v, lambda x, y: b.BASE) for v in vs]

    rws = [b.row(cells(0, False)), b.row(cells(1, False)), b.row(cells(8, False)), b.row(cells(0, True))]
    out = Image.new("RGBA", (max(r.width for r in rws), sum(r.height + 4 for r in rws)), (40, 40, 40, 255))
    yy = 0
    for r in rws:
        out.paste(r, (0, yy))
        yy += r.height + 4
    b.up(out, 8).save(os.path.join(HERE, "blender10_손질후3.png"))

if __name__ == "__main__":
    main()

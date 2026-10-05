"""B-7 계산대를 화면 오른쪽 끝까지 넓힌다 (반복 06 피드백 3). 397×122 → 585×122.

- x 0~257 은 그대로 둔다. 그 뒤로 판자 두 장 무늬를 이어 붙인다. 오른끝 둥근 마구리(x 357~396)는 버린다.
  게임에서 x −17 에 놓여 오른끝이 화면 밖(568)까지 가니 마구리는 안 보인다.
- 앞판 세로 판자선(가운데 칸) : … 216 · 236 · 257 … 간격이 20 · 21 로 번갈아 41px 마다 되풀이한다.
  그래서 무늬 A = x 216~256 (선이 0 · 20 칸째), x 258 부터 A 를 41px 주기로 붙이면 257 의 선 다음이 바로 이어진다.
- 무늬 B = x 53~93 (선이 0 · 21 칸째 — 간격이 21 · 20). A · B 를 번갈아 붙여 같은 무늬가 82px 마다만 돌아오게 한다.
  두 무늬 모두 판자선에서 시작하므로 이음매 간격은 20 또는 21 그대로다.
- 새로 칠하는 칸은 없다. 자르기 · 이어 붙이기만 한다.

읽는 것 : 화풍통일/out/Screen/stall_counter.png (b3 결과, 397 판). 이미 585 판이면 아무것도 안 한다.
쓰는 것 : 같은 파일을 덮어쓴다 · 점검/2026-10-05/반복06/counter_wide_x3.png (×3 확인판).
"""
import os

from PIL import Image

from common import OUT, ROOT, on_bg

SRC_W, NEW_W = 397, 585
KEEP = 258                       # x 0~257 은 그대로
TILE_A = (216, 257)              # [시작, 끝) 41px — 판자선 216 · 236
TILE_B = (53, 94)                # [시작, 끝) 41px — 판자선 53 · 74
ALTERNATE = True
REL = os.path.join("Screen", "stall_counter.png")
SHOT = os.path.join(ROOT, "Docs", "Reference", "Art", "점검", "2026-10-05", "반복06", "counter_wide_x3.png")


def widen(src):
    """KEEP 뒤에 A(, B, A, …) 무늬를 붙여 NEW_W 로 자른다. 첫 조각은 A 의 x 217 부터 (258 = 217 + 41)."""
    out = Image.new("RGBA", (NEW_W, src.height), (0, 0, 0, 0))
    out.paste(src.crop((0, 0, KEEP, src.height)), (0, 0))
    a = src.crop((TILE_A[0], 0, TILE_A[1], src.height))
    b = src.crop((TILE_B[0], 0, TILE_B[1], src.height))
    # 첫 조각 : A 의 1~40 칸 (x 217~256) → 새 x 258~297. 그 뒤 298 부터 41px 조각을 번갈아.
    out.paste(a.crop((KEEP - TILE_A[0] - 41, 0, a.width, a.height)), (KEEP, 0))
    x, i = KEEP + (a.width - (KEEP - TILE_A[0] - 41)), 0
    while x < NEW_W:
        tile = b if (ALTERNATE and i % 2 == 0) else a
        out.paste(tile, (x, 0))
        x += tile.width
        i += 1
    return out


def run():
    path = os.path.join(OUT, REL)
    src = Image.open(path).convert("RGBA")
    if src.width == NEW_W:
        print("이미 %d 판이다 — 그대로 둔다" % NEW_W)
        return
    assert src.size == (SRC_W, 122), "397×122 판이 아니다 : %s" % (src.size,)
    out = widen(src)
    assert out.getbbox() is not None and all(out.getpixel((x, 60))[3] == 255 for x in range(NEW_W)), "빈 칸이 생겼다"
    out.save(path)
    os.makedirs(os.path.dirname(SHOT), exist_ok=True)
    on_bg(out, 3).save(SHOT)
    print("stall_counter.png %s → %s · 색 수 %d · 확인판 %s" % (src.size, out.size, len(out.getcolors(9999)), SHOT))


if __name__ == "__main__":
    run()

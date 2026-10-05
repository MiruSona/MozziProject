"""B-5 뒤 — 진열장 반투명 유리 두 판 (사용자가 「반투명 유리는 안 될까」 라고 물어 만든다).

읽는 것 : out_alt/Screen/display_case_noglass.png 가 있으면 그것, 없으면 out/Screen/display_case.png (유리 없는 판).
  Resources 는 안 읽는다 — 거기에는 이미 유리 든 판이 들어가 있을 수 있다.
쓰는 것 (모두 out_alt/Screen/, 고른 판만 손으로 out/ 에 옮긴다)
  · display_case_back.png  : (가) 뒤 유리 반투명 — 떡 뒤 유리 칸을 알파 약 50% 로. 벽이 은은히 비친다. 빗금은 더 진하게.
  · display_case_glass.png : (나) 앞 유리 겹 — 떡 앞에 얹는 옅은 겹만 따로 (진열장 그림과 같은 캔버스). 유리 칸 말고는 투명.
반투명은 화풍 규칙(반투명 없음)의 예외다. 도트 계단은 지킨다 (칸마다 알파 값 하나, 흐림 없음).

돌리는 법 (저장소 뿌리) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/화풍통일/scripts/b5b_case_glass_alpha.py
"""
import os
from PIL import Image

from common import OUT, OUT_ALT
from b5_case_glass import enclosed

SKY = (0xD3, 0xE4, 0xE6)      # 유리 바탕 (불투명 판과 같은 색)
SHADE = (0xBC, 0xD3, 0xD8)    # 틀 · 선반 아래 그늘 2줄
SHINE = (0xF4, 0xFB, 0xFB)    # 빗금 반짝임

BACK_ALPHA = 128              # (가) 유리 바탕 50%
BACK_SHADE_ALPHA = 150
BACK_SHINE_ALPHA = 215        # (가) 빗금은 더 진하게
FRONT_ALPHA = 38              # (나) 앞 겹 바탕 15%
FRONT_SHINE_ALPHA = 100       # (나) 빗금 39%
FRONT_SHINE = (0xFF, 0xFF, 0xFF)


def source():
    kept = os.path.join(OUT_ALT, "Screen", "display_case_noglass.png")
    path = kept if os.path.isfile(kept) else os.path.join(OUT, "Screen", "display_case.png")
    im = Image.open(path).convert("RGBA")
    assert len(enclosed(im)) > 1000, "유리 없는 판이 아니다 : " + path
    return im


def shine_back(x, y):
    return (x + y) % 46 in (0, 1, 2, 6)            # 불투명 판과 같은 빗금 두 줄


def shine_front(x, y):
    return (x + y) % 58 in (0, 1, 2, 3, 8, 9)      # 굵은 줄 + 가는 줄, 한 칸에 두세 번


def run():
    base = source()
    hole = enclosed(base)

    back = base.copy()
    px = back.load()
    for (x, y) in hole:
        c, a = SKY, BACK_ALPHA
        if (x, y - 1) not in hole or (x, y - 2) not in hole:
            c, a = SHADE, BACK_SHADE_ALPHA
        elif shine_back(x, y):
            c, a = SHINE, BACK_SHINE_ALPHA
        px[x, y] = c + (a,)
    back.save(os.path.join(OUT_ALT, "Screen", "display_case_back.png"))

    front = Image.new("RGBA", base.size, (0, 0, 0, 0))
    px = front.load()
    for (x, y) in hole:
        c, a = SKY, FRONT_ALPHA
        if shine_front(x, y):
            c, a = FRONT_SHINE, FRONT_SHINE_ALPHA
        px[x, y] = c + (a,)
    front.save(os.path.join(OUT_ALT, "Screen", "display_case_glass.png"))
    print("유리 칸", len(hole), "· 뒤 유리 알파", BACK_ALPHA, "· 앞 겹 알파", FRONT_ALPHA, "/ 빗금", FRONT_SHINE_ALPHA)


if __name__ == "__main__":
    run()

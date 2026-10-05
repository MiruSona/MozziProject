"""B-6 가판대 지붕 알림 표식 (새 그림) — 빨간 민 네모(16×16) 대신 도트 표식.

16×16 · 피벗 가운데 (지금 빨간 네모와 같은 크기 · 같은 자리). accent 진분홍 + 크림 느낌표 + 1px 갈색 선.
빛은 왼쪽 위 : 왼쪽 위 밝은 분홍 · 오른쪽 아래 짙은 분홍.
"""
from PIL import Image

from common import save

COL = {
    "L": (0x35, 0x2E, 0x27),   # line
    "P": (0xFA, 0x62, 0x70),   # accent 기준
    "D": (0xD5, 0x61, 0x72),   # accent 그늘
    "H": (0xEA, 0xA1, 0xA1),   # accent 밝음
    "W": (0xFD, 0xFC, 0xF5),   # paper 가장 밝은 칸
}
ROWS = [
    "_____LLLLLL_____",
    "___LLHHPPPPLL___",
    "__LHHPPPPPPPPL__",
    "_LHPPPPWWPPPPPL_",
    "_LHPPPPWWPPPPPL_",
    "LHPPPPPWWPPPPPDL",
    "LPPPPPPWWPPPPPDL",
    "LPPPPPPWWPPPPPDL",
    "LPPPPPPWWPPPPPDL",
    "LPPPPPPWWPPPPPDL",
    "LPPPPPPPPPPPPDDL",
    "_LPPPPPWWPPPPDL_",
    "_LPPPPPWWPPPDDL_",
    "__LPPPPPPPPDDL__",
    "___LLDDDDDDLL___",
    "_____LLLLLL_____",
]


def draw():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(ROWS):
        assert len(row) == 16, (y, len(row))
        for x, ch in enumerate(row):
            if ch != "_":
                im.putpixel((x, y), COL[ch] + (255,))
    return im


if __name__ == "__main__":
    print(save(draw(), "UI/ui_badge_alert.png"))

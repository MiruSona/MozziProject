"""가판대 화풍 통일 — 같이 쓰는 경로 · 작은 도우미.

저장소 뿌리에서 돌린다 : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/화풍통일/scripts/<이름>.py
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.dirname(HERE)                      # …/화풍통일
ROOT = os.path.abspath(os.path.join(BASE, "..", "..", "..", "..", "..", ".."))
RES = os.path.join(ROOT, "ProjectMozziUnity", "Assets", "_Project", "Resources", "Art")
OUT = os.path.join(BASE, "out")                   # Resources 와 같은 하위 폴더 · 같은 이름
OUT_ALT = os.path.join(BASE, "out_alt")           # 사용자가 고를 다른 판
CMP = os.path.join(BASE, "compare")
ARTTOOL = os.path.join(ROOT, "Tools", "ArtTool", ".venv", "Scripts", "arttool.exe")
SHOTS = os.path.join(ROOT, "Docs", "Reference", "Art", "점검", "2026-10-05", "unity")

LINE = (0x35, 0x2E, 0x27)        # line 램프 기준
LINE_DARK = (0x21, 0x1D, 0x16)   # line 램프 가장 어두운 칸 — 어두운 면 옆에 쓴다
FLOOR = (0xDD, 0xA5, 0x6A)       # 가판대 마루 밝은 색 (비교판 바탕)


def load(rel):
    return Image.open(os.path.join(RES, rel)).convert("RGBA")


def save(im, rel, base=None):
    path = os.path.join(base or OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)
    return path


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def on_bg(im, k=1, bg=FLOOR):
    b = Image.new("RGBA", im.size, bg + (255,))
    b.alpha_composite(im)
    return b.resize((im.width * k, im.height * k), Image.NEAREST) if k != 1 else b


def diff_pixels(a, b):
    """두 그림에서 다른 칸 목록 [(x, y)]."""
    pa, pb = a.load(), b.load()
    return [(x, y) for y in range(a.height) for x in range(a.width) if pa[x, y] != pb[x, y]]


def bbox_of(pts):
    if not pts:
        return None
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return (min(xs), min(ys), max(xs), max(ys))

# 화풍 통일 C 갈래 (농장 · 합성소) — 스크립트들이 같이 쓰는 자리와 작은 함수.
# PIL 은 색 바꾸기 · 배치 · 정수배 확대 · 글자만 한다. 실루엣(알파)은 안 건드린다.
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = os.path.dirname(HERE)                                  # 정식/농장/화풍통일
OUT = os.path.join(WORK, 'out')
SIAN = os.path.join(WORK, '시안')
ROOT = os.path.abspath(os.path.join(WORK, '..', '..', '..', '..', '..', '..'))   # 저장소 뿌리
RES = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')   # 읽기만

LINE_NOW = (0x6E, 0x6E, 0x6E)                                 # 지금 찹쌀이 몸 선 (회색 110)
BODIES = ['body_hop%d.png' % i for i in range(4)]


def op(path):
    return Image.open(path).convert('RGBA')


def res(rel):
    return op(os.path.join(RES, rel))


def hex_rgb(text):
    text = text.lstrip('#')
    return tuple(int(text[i:i + 2], 16) for i in (0, 2, 4))


def rgb_hex(c):
    return '#%02X%02X%02X' % tuple(int(v) for v in c[:3])


def colors_of(im):
    """불투명 칸의 색 → 칸 수."""
    a = np.array(im)
    rgb = a[a[..., 3] > 0][:, :3]
    cols, cnt = np.unique(rgb, axis=0, return_counts=True)
    return {tuple(int(v) for v in c): int(n) for c, n in zip(cols, cnt)}


def remap(im, table):
    """색 표(옛 색 → 새 색)대로 칸 색만 바꾼다. 알파는 그대로."""
    a = np.array(im)
    out = a.copy()
    for old, new in table.items():
        hit = (a[..., :3] == np.array(old, dtype=np.uint8)).all(-1) & (a[..., 3] > 0)
        out[hit, :3] = new
    return Image.fromarray(out)


def same_alpha(a, b):
    return a.size == b.size and np.array_equal(np.array(a)[..., 3], np.array(b)[..., 3])


def tint(im, color):
    """게임이 하듯 그림 × 종 색 (곱하기)."""
    a = np.array(im).astype(np.uint16)
    for i in range(3):
        a[..., i] = (a[..., i] * color[i] + 127) // 255
    return Image.fromarray(a.astype(np.uint8))


def tile(im, w, h):
    out = Image.new('RGBA', (w, h))
    for y in range(0, h, im.height):
        for x in range(0, w, im.width):
            out.paste(im, (x, y))
    return out


def zoom(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def font(size=14):
    for name in ('malgun.ttf', 'malgunbd.ttf', 'gulim.ttc'):
        path = os.path.join(os.environ.get('WINDIR', 'C:/Windows'), 'Fonts', name)
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def label(im, xy, text, size=14, fill=(40, 34, 28), bg=None):
    d = ImageDraw.Draw(im)
    f = font(size)
    if bg is not None:
        box = d.textbbox(xy, text, font=f)
        d.rectangle((box[0] - 3, box[1] - 2, box[2] + 3, box[3] + 2), fill=bg)
    d.text(xy, text, font=f, fill=fill)

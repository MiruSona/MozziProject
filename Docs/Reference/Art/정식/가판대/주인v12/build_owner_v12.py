"""가판대 주인 v12 정식 그림 — 후보 A + 분홍 겉옷 팔 + 둘레 갈색 선.

저장소 뿌리에서 : Tools/ArtTool/.venv/Scripts/python.exe -I Docs/Reference/Art/정식/가판대/주인v12/build_owner_v12.py

1) A(raw/owner_v12_A_12102) 에 PixelLab edit 판(raw/owner_v12_A_pink_12501)의 두 팔 상자만 붙인다.
   edit 판은 얼굴 · 머리 결까지 조금씩 흔들려서 팔만 가져온다.
2) arttool reline — 둘레 고리의 어두운 칸만 #352E27, 어두운 면 옆은 #211D16 (10-05 화풍통일 B-1 과 같은 색 · 고리 방식).
3) arttool merge-colors — 40색으로 (선 두 색은 지킨다).
"""
import os
import subprocess
import sys
import tempfile

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "..", "raw")
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
ARTTOOL = os.path.join(ROOT, "Tools", "ArtTool", ".venv", "Scripts", "arttool.exe")
OUT = os.path.join(HERE, "out", "Screen", "stall_owner.png")
ARMS = [(8, 118, 47, 170), (70, 118, 110, 170)]   # 왼팔 · 오른팔 (x0, y0, x1, y1)
LINE, LINE_DARK = "#352E27", "#211D16"

# 10-28 v12b : 짝짝이 부츠 → 양쪽 짧은 흰 부츠.
LEG_Y0 = 172                      # 치마 밑단 바로 아래 줄. 이 위는 안 건드린다
LEFT_LEG = (34, 58)               # 보는 사람 기준 왼다리 x 범위 (짧은 부츠)
MIRROR_AXIS2 = 116                # 새 x = 116 - x  (왼다리 안쪽 선 57 → 59, 바깥 36 → 80)
RIGHT_CLEAR = (58, 86)            # 옛 긴 부츠 다리를 지울 x 범위
BOOT_BOX = (30, 205, 90, 240)     # 색을 바꿀 상자 (x0, y0, x1, y1) — 부츠만 든다
# 라벤더 → 손님 운동화(Customer/body.png) 흰 램프, 밝기 순
BOOT_RAMP = {
    (0xBC, 0xBA, 0xDF): (0xFA, 0xF9, 0xF9),
    (0xAB, 0xA4, 0xCF): (0xDD, 0xDA, 0xE1),
    (0x9D, 0x93, 0xC1): (0xDD, 0xDA, 0xE1),
    (0x85, 0x78, 0xA4): (0xB3, 0xAF, 0xC1),
    (0x64, 0x50, 0x7A): (0x88, 0x85, 0x9A),
}


def short_boots(im):
    """왼다리(짧은 부츠)를 좌우 반전해 오른다리 자리에 놓고, 부츠 라벤더를 흰 램프로 바꾼다."""
    w, h = im.size
    px = im.load()
    leg = {(x, y): px[x, y] for y in range(LEG_Y0, h) for x in range(LEFT_LEG[0], LEFT_LEG[1])}
    for y in range(LEG_Y0, h):
        for x in range(RIGHT_CLEAR[0], RIGHT_CLEAR[1]):
            px[x, y] = (0, 0, 0, 0)
    for (x, y), c in leg.items():
        if c[3]:
            px[MIRROR_AXIS2 - x, y] = c
    x0, y0, x1, y1 = BOOT_BOX
    for y in range(y0, y1):
        for x in range(x0, x1):
            c = px[x, y]
            if c[3] and c[:3] in BOOT_RAMP:
                px[x, y] = BOOT_RAMP[c[:3]] + (255,)


def run():
    a = Image.open(os.path.join(RAW, "owner_v12_A_12102.png")).convert("RGBA")
    p = Image.open(os.path.join(RAW, "owner_v12_A_pink_12501.png")).convert("RGBA")
    for box in ARMS:
        a.paste(p.crop(box), box[:2])
    short_boots(a)
    tmp = tempfile.mkdtemp(prefix="owner12_")
    merged = os.path.join(tmp, "stall_owner.png")
    a.save(merged)
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    relined = os.path.join(tmp, "reline")
    subprocess.run([ARTTOOL, "reline", "--in", merged, "--out", relined, "--color", LINE, "--pick", "dark",
                    "--scope", "ring", "--tol", "14", "--depth", "2", "--color-dark", LINE_DARK],
                   check=True, env=env)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    subprocess.run([ARTTOOL, "merge-colors", "--in", os.path.join(relined, "stall_owner.png"), "--out", OUT,
                    "--max-colors", "40", "--keep", ",".join([LINE, LINE_DARK] + ["#%02X%02X%02X" % c for c in set(BOOT_RAMP.values())])],
                   check=True, env=env)
    im = Image.open(OUT).convert("RGBA")
    colors = im.getcolors(9999)
    opaque = {c[1] for c in colors if c[1][3]}
    assert im.size == (120, 240), im.size
    assert len(opaque) <= 40, len(opaque)
    assert {c[1][3] for c in colors} <= {0, 255}, "반투명"
    print("out", im.size, "색", len(opaque), "bbox", im.getbbox())


if __name__ == "__main__":
    sys.exit(run())

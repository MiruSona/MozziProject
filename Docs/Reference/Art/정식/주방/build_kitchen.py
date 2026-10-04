# 정식 그림 — 주방 화면 2차 (2026-10-04 · 반복 04)
# raw/(PixelLab 원본) → out/ 의 배경(천장·벽 1차 · 마루 2차) · 절구+떡메 7장 · 재료 쟁반 · 결과 접시 와
# 판정용 미리보기 · 떡메 gif 를 만든다.
# PIL 은 자르기 · 배치 · 줄 이어 붙이기 · 글자 · 단색 상자 · 프레임 잇기 · 테두리 기둥 되풀이만 한다. 새 그림을 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/주방/build_kitchen.py
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..', '..'))
RAW = os.path.join(HERE, 'raw'); OUT = os.path.join(HERE, 'out'); os.makedirs(OUT, exist_ok=True)
SIAN = os.path.join(ROOT, 'Docs', 'Reference', 'Art', '시안')
UI = os.path.join(HERE, '..', '가판대', 'out')          # 읽기만 한다
GAME_ART = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Resources', 'Art')   # 읽기만 (미리보기에 재료 · 떡 아이콘)
FONT = os.path.join(ROOT, 'ProjectMozziUnity', 'Assets', '_Project', 'Fonts', 'ONEMobilePOP.ttf')
PREVIEW = os.path.join(SIAN, '2026-10-04-주방화면-정식-v2-미리보기.png')
GIF = os.path.join(SIAN, '2026-10-04-떡메-동작.gif')

W, H = 540, 1200            # 세계 = 9:20 화면. 9:16 은 아래 960 줄 (y 240~1200)
Y916 = H - 960

# ---- 배경 잇는 자리 (천장·벽 1차 · 마루만 2차 floor_20105) ----
TOPS = [(0, 238), (344, 360)]
WALL = (9, 428)
FLOORS = [(0, 413), (126, 240)]     # floor_20105 (2차, 고른 빛). 413+114 = 527 줄

# ---- 절구 + 떡메 ----
POUND = 'pound_20702'                    # animate_image 7장 (f0 = 입력 = 떡메 든 쉬는 모습)
POUND_BOX = (16, 2, 228, 243)            # 240×256 에서 자를 칸 → 212×241. 7장 모두 이 칸에 다 든다
POUND_PIVOT = ((26 + 163 + 1) // 2 - POUND_BOX[0], 243 - POUND_BOX[1])   # 절구 몸 아래 가운데 = (79, 241)
POUND_SEQ = [(0, 60), (1, 50), (2, 50), (3, 40), (4, 120), (5, 60), (6, 60)]   # 한 번 치기 : (프레임, ms)
MORTAR = (190, 1000)                     # 절구 피벗 자리 (세계) — 크게 · 왼쪽 가운데
# ---- 쟁반 · 접시 ----
TRAY_RAW, TRAY_CROP = 'tray_20904', (7, 8, 245, 56)    # 238×48
TRAY_PAD = 11                            # 양 끝 테두리 기둥을 이만큼 되풀이해 그려진 쟁반(약 260 폭)을 덮는다
TRAY_AT = (255, 520)                     # 쟁반 조각 왼쪽 위 (세계)
PLATE_RAW, PLATE_CROP = 'plate_20905', (23, 17, 104, 62)   # 81×45
PLATE = (420, 1000)                      # 접시 아래 가운데 (세계)
BTN_H = 80

CREAM, INK, BGC = (250, 240, 220, 255), (60, 40, 32, 255), (58, 50, 48, 255)


def raw(n): return Image.open(os.path.join(RAW, n + '.png')).convert('RGBA')
def font(s): return ImageFont.truetype(FONT, s)
def txt(d, xy, s, size=15, fill=CREAM):
    sw = 0 if fill == INK else 3
    d.text(xy, s, font=font(size), fill=fill, anchor='mm', align='center', stroke_width=sw, stroke_fill=INK)


def nine(src, w, h, l, t, r, b):
    sw, sh = src.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0:
                out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


FLOOR_V1 = ('floor_20103', [(0, 387), (34, 174)])   # 1차 마루 (밝은 둥근 얼룩) → out/kitchen_bg_v1.png 로만 남긴다


def build_bg(floor_name='floor_20105', floors=FLOORS):
    top, wall, floor = [np.array(raw(n)) for n in ('top_20104', 'wall_20101', floor_name)]
    parts = [top[a:b] for a, b in TOPS] + [wall[WALL[0]:WALL[1]]] + [floor[a:b] for a, b in floors]
    col = np.concatenate(parts, axis=0)
    assert col.shape[0] == H, col.shape
    return Image.fromarray(col)


def build_pound():
    return [Image.open(os.path.join(RAW, POUND, 'f%d.png' % i)).convert('RGBA').crop(POUND_BOX) for i in range(7)]


def build_tray():
    """쟁반 양 끝 테두리(안쪽 칸이 없는 기둥)를 TRAY_PAD 번 되풀이해 넓힌다. 칸 크기는 그대로다."""
    t = np.array(raw(TRAY_RAW).crop(TRAY_CROP))
    lcol, rcol = t[:, 4:5], t[:, -5:-4]
    wide = np.concatenate([t[:, :4]] + [lcol] * TRAY_PAD + [t[:, 4:-4]] + [rcol] * TRAY_PAD + [t[:, -4:]], axis=1)
    im = Image.fromarray(wide)
    # 칸 안쪽 찾기 : 짙은 갈색(157, 94, 65 근처)이 이어진 가로 구간 (y 24 줄 기준)
    a = wide[:, :, :3].astype(int)
    dark = (np.abs(a - np.array([157, 94, 65])).sum(axis=2) < 40) & (wide[:, :, 3] > 0)
    row = dark[24]
    cells, x = [], 0
    while x < len(row):
        if row[x]:
            x0 = x
            while x < len(row) and row[x]: x += 1
            if x - x0 > 12: cells.append((x0, x - 1))
        else:
            x += 1
    ys = np.where(dark[:, (cells[0][0] + cells[0][1]) // 2])[0]
    return im, cells, (int(ys.min()), int(ys.max()))


def scene(bg, P, top_y, frame):
    im = bg.copy()
    tray, cells, (cy0, cy1) = P['tray']
    im.alpha_composite(tray, TRAY_AT)
    icons = ['redbean', 'bean', 'mugwort', 'sesame', 'pinenut', 'jujube']
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for (x0, x1), n, c in zip(cells, icons, [24, 12, 8, 3, 0, 0]):
        ic = Image.open(os.path.join(GAME_ART, 'Ingredient', n + '.png')).convert('RGBA')
        cx = TRAY_AT[0] + (x0 + x1) // 2; by = TRAY_AT[1] + cy1
        if c == 0:
            ic = Image.fromarray((np.array(ic) * np.array([0.45, 0.45, 0.45, 1])).astype('uint8'))
        im.alpha_composite(ic, (cx - ic.width // 2, by - ic.height + 2))
        txt(d, (cx, TRAY_AT[1] + tray.height + 12), str(c), 13)
    p = P['pound'][frame]
    im.alpha_composite(p, (MORTAR[0] - POUND_PIVOT[0], MORTAR[1] - POUND_PIVOT[1]))
    pl = P['plate']; px, py = PLATE[0] - pl.width // 2, PLATE[1] - pl.height
    im.alpha_composite(pl, (px, py))
    cake = Image.open(os.path.join(GAME_ART, 'Cake', 'redbean_bean_cake.png')).convert('RGBA')
    im.alpha_composite(cake, (PLATE[0] - cake.width // 2, py + 4))
    txt(d, (PLATE[0], PLATE[1] + 18), 'NEW! 팥콩떡', 16)
    txt(d, (MORTAR[0], MORTAR[1] + 18), '떡메를 3번 치세요 · 1/3', 15)
    btn = Image.open(os.path.join(UI, 'ui_button.png')).convert('RGBA')
    gb = nine(btn, 158, 48, 10, 10, 10, 10); im.alpha_composite(gb, (12, top_y + 12)); d.text((91, top_y + 36), '798 G', font=font(24), fill=INK, anchor='mm')
    by = H - 104
    txt(d, (W // 2, by - 22), '재료를 1~3개 골라 절구에 넣는다', 16)
    for x0, x1, n in [(30, 250, '떡메 치기'), (270, 390, '레시피 도감'), (410, 510, '닫기')]:
        im.alpha_composite(nine(btn, x1 - x0, BTN_H, 10, 10, 10, 10), (x0, by))
        d.text(((x0 + x1) // 2, by + BTN_H // 2), n, font=font(22 if len(n) < 5 else 20), fill=INK, anchor='mm')
    return Image.alpha_composite(im, lay)


def board(cols, title, notes, extra=None):
    pad, head = 24, 96
    ch = max(c.height for _, c in cols); note_h = 30 * len(notes) + 20
    eh = extra.height + 40 if extra else 0
    out = Image.new('RGBA', (pad + len(cols) * (W + pad), head + ch + 40 + eh + note_h), BGC); d = ImageDraw.Draw(out)
    d.text((pad, 20), title, font=font(26), fill=CREAM)
    for i, (cap, c) in enumerate(cols):
        x = pad + i * (W + pad); d.text((x, head - 8), cap, font=font(18), fill=(230, 200, 170, 255), anchor='lb')
        out.paste(c, (x, head + ch - c.height))
    y = head + ch + 30
    if extra:
        out.alpha_composite(extra, (pad, y)); y += extra.height + 30
    for n in notes: d.text((pad, y), n, font=font(18), fill=CREAM); y += 30
    return out


if __name__ == '__main__':
    bg = build_bg()
    bg.convert('RGB').save(os.path.join(OUT, 'kitchen_bg.png'))
    build_bg(*FLOOR_V1).convert('RGB').save(os.path.join(OUT, 'kitchen_bg_v1.png'))
    pound = build_pound()
    for i, f in enumerate(pound): f.save(os.path.join(OUT, 'kitchen_pound_%d.png' % i))
    sheet = Image.new('RGBA', (pound[0].width * len(pound), pound[0].height), (0, 0, 0, 0))
    for i, f in enumerate(pound): sheet.alpha_composite(f, (i * pound[0].width, 0))
    sheet.save(os.path.join(OUT, 'kitchen_pound_strip.png'))
    tray = build_tray(); tray[0].save(os.path.join(OUT, 'kitchen_tray.png'))
    plate = raw(PLATE_RAW).crop(PLATE_CROP); plate.save(os.path.join(OUT, 'kitchen_plate.png'))
    print('pound', pound[0].size, 'pivot', POUND_PIVOT, 'tray', tray[0].size, 'cells', tray[1], 'cell y', tray[2], 'plate', plate.size)

    # gif : 쉼 → 세 번 치기 → 쉼
    seq = [(0, 600)] + POUND_SEQ * 3 + [(0, 600)]
    imgs = []
    for i, _ in seq:
        c = Image.new('RGBA', pound[i].size, (232, 178, 120, 255)); c.alpha_composite(pound[i])
        imgs.append(c.resize((c.width * 2, c.height * 2), Image.NEAREST).convert('P', palette=Image.ADAPTIVE, colors=128))
    imgs[0].save(GIF, save_all=True, append_images=imgs[1:], duration=[ms for _, ms in seq], loop=0, disposal=2)

    P = dict(tray=tray, pound=pound, plate=plate)
    s916 = scene(bg, P, Y916, 0).crop((0, Y916, W, H))
    s920 = scene(bg, P, 0, 4)
    fw, fh = pound[0].width + 8, pound[0].height + 40
    st = Image.new('RGBA', (4 * fw + 8, 2 * fh), (90, 120, 110, 255)); d = ImageDraw.Draw(st)
    names = ['0 들기(쉼)', '1 내림', '2 내림', '3 닿음', '4 맞음', '5 튐', '6 올림']
    for i, f in enumerate(pound):
        x, y = 8 + (i % 4) * fw, (i // 4) * fh + 4
        st.alpha_composite(f, (x, y))
        d.line((x, y + POUND_PIVOT[1] - 1, x + f.width, y + POUND_PIVOT[1] - 1), fill=(255, 80, 80, 160))
        d.text((x + f.width // 2, y + f.height + 18), names[i], font=font(16), fill=CREAM, anchor='mm')
    board([('9:16 (540×960) · 떡메 든 쉬는 모습', s916), ('9:20 (540×1200) · 맞는 순간(4)', s920)],
          '주방 화면 정식 2차 · 절구 · 떡메 · 쟁반 · 접시 · 2026-10-04 · 도트 1px · 모두 ×1', [
          '배경은 천장·벽 1차 그대로, 마루만 밝은 얼룩 없는 새 판(floor_20105). 그 위에 새 조각 셋 : 절구+떡메(212×241 · 7장) · 넓힌 재료 쟁반(칸 여섯) · 결과 접시.',
          '절구를 왼쪽 가운데에 크게 두고 오른쪽에 결과 접시. 재료 · 떡 아이콘은 지금 게임 그림, 수는 자리표시 (0 이면 어둡게).',
          '아래 줄 : 한 번 치기 7장 (0 들기 → 1 · 2 내림 → 3 닿음 → 4 맞음 → 5 튐 → 6 올림). 빨간 줄 = 피벗(절구 바닥) 줄. 절구는 7장 내내 같은 자리.',
          '버튼은 아래 줄 (떡메 치기 · 레시피 도감 · 닫기), 버튼 틀은 가판대 정식 조각 9-slice. 9:20 에서만 보이는 위 240 줄 = 천장 · 종이 등 셋.'], st).convert('RGB').save(PREVIEW)
    print('ok')

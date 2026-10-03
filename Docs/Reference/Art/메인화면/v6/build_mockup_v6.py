# 메인 화면 「나」 v6 목업 — 2026-10-03 반복 03
# v5 에서 바꾼 것 : 가게 = 후보 3 (250×203) · 기계 = 후보 2 (135×144). 건물 자리 · 글자 · 보관함 자리를 다시 잡았다.
# 나머지(잔디 · 울타리 · 찹쌀이 · 메뉴 · 길 · 숲)는 v4 스크립트 그대로다.
# PIL 은 자르기·배치·정수 배 확대·글자만 한다. 그림을 그리거나 다시 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v6/build_mockup_v6.py
import os, importlib.util
from PIL import Image, ImageDraw, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v4', os.path.join(HERE, '..', 'v4', 'build_mockup_v4.py'))
v4 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v4)
v2 = v4.v2; font, paste, paste_base, OUT, W, H_TALL, P = v4.font, v4.paste, v4.paste_base, v4.OUT, v4.W, v4.H_TALL, v4.P
PARTS = os.path.join(HERE, 'parts'); V5 = os.path.join(HERE, '..', 'v5', 'parts')
SHOP = Image.open(os.path.join(V5, 'shop_simple_13203.png')).convert('RGBA')          # 250×203
MACHINE = Image.open(os.path.join(V5, 'machine_simple_13102.png')).convert('RGBA')    # 135×144
SHOP.save(os.path.join(PARTS, 'shop.png')); MACHINE.save(os.path.join(PARTS, 'machine.png'))
MACHINE_CX, SHOP_CX, CHEST_CX = 88, 306, 492                                           # 기계 20~155 · 틈 26 · 가게 181~431 · 틈 30 · 상자


def scene():
    H = H_TALL; im = Image.new('RGBA', (W, H), v2.SKY); info = {}
    farm_top = H - 480; path_top = farm_top - 60; horizon = path_top - 112
    v2.sky_and_forest(im, horizon)
    g = v4.ground(v4.GRASS_KIND, 768, 768); paste(im, g.crop((0, 0, W, H - horizon)), 0, horizon)
    p = P['path']; strip = Image.new('RGBA', (680, 60)); strip.paste(p, (0, 0)); strip.paste(ImageOps.mirror(p), (340, 0))
    paste(im, strip.crop((60, 0, 60 + W, 60)), 0, path_top)
    fw, fh = v4.LOG.size(v4.FN, v4.FM); fx = (W - fw) // 2; fy = H - fh
    paste(im, v4.LOG.back(v4.FN, v4.FM), fx, fy)
    base = path_top + 8
    paste_base(im, MACHINE, MACHINE_CX, base); paste_base(im, SHOP, SHOP_CX, base)
    paste_base(im, P['chest'], CHEST_CX, path_top + 50)
    info['labels'] = [('합성소', MACHINE_CX, base + 22), ('가판대', SHOP_CX, base + 22), ('보관함', CHEST_CX, path_top + 50 - 62)]
    for (hair, shirt, bottom, face, cx) in [('02_brown', '01_blue', '01_navy', 1, 196), ('04_orange', '02_yellow', '02_beige', 2, 404)]:
        paste(im, v2.customer(hair, shirt, bottom, face), cx - 32, path_top + 52 - 76)
    mz = [('#FFFFFF', 1, False, 80, 120), ('#4FA84A', 2, True, 200, 100), ('#7FC8F8', 1, True, 330, 160), ('#FFFFFF', 5, False, 120, 260),
          ('#3F9BE0', 2, True, 190, 310), ('#2E7D32', 1, True, 80, 390), ('#F5A05A', 1, True, 420, 250)]
    for (k, x, y) in [('pile_redbean', 150, 190), ('pile_sesame', 270, 90), ('pile_bean', 220, 380)]: paste(im, P[k], x, farm_top + y)
    for (c, f, crown, x, y) in sorted(mz, key=lambda m: m[4]): paste(im, v4.mozzi(c, f, crown), x - 32, farm_top + y - 32)
    fr, fo = v4.LOG.front(v4.FN, v4.FM); paste(im, fr, fx, fy + fo)
    return im, info


def board():
    v4.scene = scene
    a = v4.render(960); b = v4.render(1200); M = 40; top = 150
    c = Image.new('RGBA', (M * 3 + a.width * 2, top + b.height + 40), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 시안 「나」 v6 : 가게 = 후보 3 · 기계 = 후보 2 (사용자 고름)', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170)); d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2)', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top)); y = top + a.height + 30
    for line in ['v5 에서 바꾼 것 : 가게(후보 1 → 후보 3 · 250×203) · 합성 기계(후보 1 → 후보 2 · 135×144).',
                 '기계가 작아서 왼쪽으로 붙였다 (가운데 x 88). 가게는 가운데 x 306, 보관함 상자는 가게 오른쪽 길 위(x 492)다.',
                 '「합성소」「가판대」 글자를 건물 가운데 아래에 맞췄다. 길 위 손님은 둘로 줄여 건물 사이 · 가게 오른쪽에 세웠다.',
                 '잔디(가) · 통나무 울타리 · 볼 샛노랑 · 메뉴는 v4 · v5 그대로다. 주인은 세우지 않았다.',
                 '전부 시안이다. 정식 그림은 다음 세션에서 다시 뽑는다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


if __name__ == '__main__':
    board().save(os.path.join(OUT, '2026-10-03-메인화면-나-v6.png'))

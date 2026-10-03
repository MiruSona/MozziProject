# 메인 화면 「나」 v5 목업 + 건물 단순화 후보 비교판 — 2026-10-03 반복 03
# v4 에서 바꾼 것 : 건물 둘(합성 기계 · 가게)만 단순한 그림으로 바꿨다. 나머지는 v4 스크립트 그대로다.
# PIL 은 자르기·배치·정수 배 확대·글자만 한다. 그림을 그리거나 다시 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v5/build_mockup_v5.py
import os, importlib.util
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v4', os.path.join(HERE, '..', 'v4', 'build_mockup_v4.py'))
v4 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v4)
v2 = v4.v2; font, paste, paste_base, OUT = v4.font, v4.paste, v4.paste_base, v4.OUT
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)
MILL_RAW = os.path.join(HERE, '..', '..', '합성소화면', 'v3', 'raw')
PICK_MACHINE, PICK_SHOP = 'machine_simple_13101', 'shop_simple_13201'                  # 고른 후보 (임시)


def cut(path): im = Image.open(path).convert('RGBA'); return im.crop(im.getbbox())


OLD = {'machine': v4.N3['machine_big'], 'shop': v4.N3['shop_big']}
NEW = {n: cut(os.path.join(RAW, n + '.png')) for n in ['machine_simple_13101', 'machine_simple_13102', 'machine_simple_13103', 'shop_simple_13201', 'shop_simple_13202', 'shop_simple_13203']}
NEW['machine_small_13301'] = cut(os.path.join(MILL_RAW, 'machine_small_13301.png'))
for k, v in NEW.items(): v.save(os.path.join(PARTS, k + '.png'))


def candidates():
    """지금 건물 옆에 단순화 후보를 같은 바닥 줄에 세운다. 크기를 가늠하게 찹쌀이와 손님을 하나씩 같이 세운다."""
    rows = [('가게', [('지금 (v1 · 240×200)', OLD['shop']), ('후보 1 · 고름 (213×174)', NEW['shop_simple_13201']), ('후보 2 (182×150)', NEW['shop_simple_13202']), ('후보 3 (250×203)', NEW['shop_simple_13203'])]),
            ('합성 기계', [('지금 (v1 · 230×227)', OLD['machine']), ('후보 1 · 고름 (204×185)', NEW['machine_simple_13101']), ('후보 2 (135×144)', NEW['machine_simple_13102']), ('후보 3 (180×202)', NEW['machine_simple_13103']), ('전용 화면용 작은 기계 (124×121)', NEW['machine_small_13301'])])]
    CW, CH = 330, 250; M = 40; top = 110; cols = 5
    c = Image.new('RGBA', (M * 2 + CW * 2 * cols, top + (CH * 2 + 70) * 2 + 250), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '건물 · 기계 단순화 후보 : 큰 덩어리 · 넓은 면 · 색 수 줄이기 · 장식 2~3개', font=font(40), fill=(255, 240, 220))
    g = v4.ground(v4.GRASS_KIND, 768, 768); y = top
    for (name, items) in rows:
        for i, (cap, im) in enumerate(items):
            cell = g.crop((0, 0, CW, CH)).copy(); paste_base(cell, im, 130, CH - 22)
            paste(cell, v4.mozzi('#FFFFFF', 1, False), 250 - 32, CH - 18 - 50); paste(cell, v2.customer('02_brown', '01_blue', '01_navy', 1), 296 - 32, CH - 14 - 76)
            x = M + i * CW * 2; d.text((x, y), name + ' ' + cap, font=font(24), fill=(230, 200, 170)); c.paste(cell.resize((CW * 2, CH * 2), Image.NEAREST), (x, y + 40))
        y += CH * 2 + 70
    for line in ['후보는 모두 PixelLab 으로 새로 뽑았다 (화풍 = 지금 가게 그림 · 「세부 따라 하기」 를 끄고 「단순하게」 라고 글로 시킴). 손으로 다시 칠하지 않았다.',
                 '어두운 갈색 1px 외곽선 · 명암 3단은 지켰다. 색 수 : 지금 가게 80색 · 기계 71색, 후보는 18~48색이다 (고른 것은 가게 38색 · 기계 28색).',
                 '가게 후보 1 = 지붕 · 차양 · 진열 창 · 문 네 덩어리. 후보 2 = 둥근 지붕이라 귀엽지만 작다. 후보 3 = 박공지붕이라 크고 화면 폭을 많이 먹는다.',
                 '기계 후보 1 = 둥근 몸통 · 깔때기 둘 · 창 하나 · 손잡이 하나. 후보 2 = 가장 단순하지만 작다. 후보 3 = 다리 달린 솥 모양이라 기계보다 가마로 보인다.',
                 '메인 화면 v5 에는 가게 후보 1 과 기계 후보 1 을 넣었다 (임시로 고른 것이다).']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


def board():
    v4.N3['machine_big'] = NEW[PICK_MACHINE]; v4.N3['shop_big'] = NEW[PICK_SHOP]
    a = v4.render(960); b = v4.render(1200); M = 40; top = 150
    c = Image.new('RGBA', (M * 3 + a.width * 2, top + b.height + 40), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 시안 「나」 v5 : 건물 둘만 단순한 그림으로 바꿈', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170)); d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2)', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top)); y = top + a.height + 30
    for line in ['v4 에서 바꾼 것 : 합성 기계(230×227 → 204×185) · 가게(240×200 → 213×174) 둘뿐이다.',
                 '잔디(가) · 통나무 울타리 · 볼 샛노랑 · 메뉴 · 길 · 숲 · 손님 · 보관함은 v4 그대로다.',
                 '주인은 세우지 않았다 (주인은 가판대 전용 화면에만 나온다).',
                 '건물이 조금 작아져 둘 사이 틈이 넓어졌다. 자리는 v4 와 같은 가운데 x 다.',
                 '건물 후보를 견준 그림은 「건물단순화-후보」 다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


if __name__ == '__main__':
    candidates().save(os.path.join(OUT, '2026-10-03-건물단순화-후보.png'))
    board().save(os.path.join(OUT, '2026-10-03-메인화면-나-v5.png'))
    print({k: (v.size, len(v.convert('RGB').getcolors(100000))) for k, v in list(NEW.items()) + list(OLD.items())})

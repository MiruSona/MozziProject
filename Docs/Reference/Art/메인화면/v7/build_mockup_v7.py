# 메인 화면 「나」 v7 목업 · 보관함 단순화 후보 — 2026-10-03 반복 03
# v6 에서 바꾼 것 : 보관함 상자 하나뿐이다. 쇠 띠 상자 + 자루 + 궤짝(78×56) → 단순한 상자(후보 2 · 89×70).
# 나머지(가게 후보 3 · 기계 후보 2 · 잔디 · 울타리 · 찹쌀이 · 메뉴 · 길 · 숲)는 v6 스크립트 그대로다.
# PIL 은 자르기·배치·정수 배 확대·글자만 한다. 그림을 그리거나 다시 칠하지 않는다.
# 돌리는 법 (저장소 뿌리에서) : Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/메인화면/v7/build_mockup_v7.py
import os, importlib.util
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v6', os.path.join(HERE, '..', 'v6', 'build_mockup_v6.py'))
v6 = importlib.util.module_from_spec(spec); spec.loader.exec_module(v6)
v4, v2, font, OUT, W, H_TALL, P = v6.v4, v6.v2, v6.font, v6.OUT, v6.W, v6.H_TALL, v6.P
RAW = os.path.join(HERE, 'raw'); PARTS = os.path.join(HERE, 'parts'); os.makedirs(PARTS, exist_ok=True)


def trim(im): return im.crop(im.getbbox())


OLD = P['chest']                                                                        # 지금 것 78×56
CAND = [trim(Image.open(os.path.join(RAW, 'chest_%d.png' % s)).convert('RGBA')) for s in (17111, 17112, 17113)]   # 93×82 · 89×70 · 92×77
for i, c in enumerate(CAND): c.save(os.path.join(PARTS, 'chest_simple_%d.png' % (i + 1)))
PICK = 1                                                                                # 후보 2 (상자 + 쌀자루)
PATH_TOP = H_TALL - 480 - 60


def scene_with(chest):
    """v6 장면을 그대로 굽되 보관함 그림만 바꿔 끼운다. 글자는 상자 높이에 맞춰 올린다."""
    def scene():
        keep = P['chest']; P['chest'] = chest; im, info = v6.scene(); P['chest'] = keep
        info['labels'][2] = ('보관함', v6.CHEST_CX, PATH_TOP + 50 - chest.height - 16)
        return im, info
    return scene


def render(chest, H): v4.scene = scene_with(chest); return v4.render(H)


def candidates():
    """후보 비교 : 지금 것 + 후보 1~3. 칸마다 오른쪽 위에 낱장 ×3, 메인 화면 건물 줄에 놓은 모습 (×2)."""
    items = [('지금 (v6 까지)', OLD), ('후보 1 · 닫힌 상자', CAND[0]), ('후보 2 · 상자 + 쌀자루', CAND[1]), ('후보 3 · 열린 궤짝', CAND[2])]
    M = 40; top = 110; cw = 1080; sh = 600; c = Image.new('RGBA', (M * 3 + cw * 2, top + 2 * (60 + sh + M) + 120), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '보관함 단순화 후보 : 가게(후보 3) · 기계(후보 2) 와 같은 단순한 그림체로', font=font(40), fill=(255, 240, 220))
    for i, (name, ch) in enumerate(items):
        x = M + (i % 2) * (cw + M); y = top + (i // 2) * (60 + sh + M)
        d.text((x, y + 8), '%s · %d×%d' % (name, ch.width, ch.height), font=font(30), fill=(255, 236, 150) if i == PICK + 1 else (230, 200, 170))
        strip = render(ch, 960).crop((0, 2 * (PATH_TOP - 240 - 220), cw, 2 * (PATH_TOP - 240 + 80)))        # 건물 줄만 (960 화면 좌표 ×2)
        c.paste(strip, (x, y + 60))
        big = ch.resize((ch.width * 3, ch.height * 3), Image.NEAREST); c.alpha_composite(big, (x + cw - big.width - 12, y + 60 + 12))   # 낱장 ×3 은 오른쪽 위 하늘 자리에 얹는다
    y = top + 2 * (60 + sh + M) + 6
    for line in ['후보 셋은 PixelLab(create_image_pro_flash 96×96)으로 새로 뽑았다. 화풍 그림은 우리가 만든 것만 줬다. 「세부 따라 하기」 는 껐다.',
                 '각 칸 : 오른쪽 위는 낱장 ×3, 나머지는 메인 화면 건물 줄에 놓은 모습이다. 노란 글자(후보 2)를 메인 화면 v7 에 넣었다. 전부 시안이다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


def board():
    ch = CAND[PICK]; a = render(ch, 960); b = render(ch, 1200); M = 40; top = 150
    c = Image.new('RGBA', (M * 3 + a.width * 2, top + b.height + 40), v2.BG); d = ImageDraw.Draw(c)
    d.text((M, 24), '메인 화면 시안 「나」 v7 : 보관함을 단순한 상자(후보 2)로', font=font(40), fill=(255, 240, 220))
    d.text((M, 100), '9:16 · 540×960 (×2)', font=font(26), fill=(230, 200, 170)); d.text((M * 2 + a.width, 100), '9:20 · 540×1200 (×2)', font=font(26), fill=(230, 200, 170))
    c.paste(a, (M, top)); c.paste(b, (M * 2 + a.width, top)); y = top + a.height + 30
    for line in ['v6 에서 바꾼 것 : 보관함 하나뿐이다. 쇠 띠 상자 + 자루 + 궤짝 → 단순한 상자 + 쌀자루 (보관함 후보 2 · 89×70).',
                 '후보 셋은 「보관함단순화-후보」 그림에 있다. 후보 2 를 넣은 까닭 :', '자루가 있어 「재료를 두는 곳」 으로 읽히고 크기가 지금 것과 가장 가깝다.',
                 '가게(후보 3) · 합성 기계(후보 2) · 잔디 · 울타리 · 찹쌀이 · 메뉴 · 길 위 손님은 v6 그대로다.',
                 '「보관함」 글자는 상자가 커진 만큼 위로 올렸다.',
                 '전부 시안이다. 정식 그림은 다음 세션에서 다시 뽑는다.']:
        d.text((M, y), line, font=font(24), fill=(240, 225, 205)); y += 38
    return c.convert('RGB')


if __name__ == '__main__':
    candidates().save(os.path.join(OUT, '2026-10-03-보관함단순화-후보.png'))
    board().save(os.path.join(OUT, '2026-10-03-메인화면-나-v7.png'))
    print('ok', [c.size for c in CAND])

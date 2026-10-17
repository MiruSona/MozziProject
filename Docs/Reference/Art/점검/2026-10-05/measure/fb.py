# v16 피드백 셋을 재는 그림 · 숫자 (원본은 읽기만)
import os, json
from PIL import Image, ImageDraw
A = r"D:\Project\ClaudeProject\ProjectMozzi2\ProjectMozziUnity\Assets\_Project\Resources\Art"
O = r"D:\Project\ClaudeProject\ProjectMozzi2\Docs\Reference\Art\점검\2026-10-05\feedback"
os.makedirs(O, exist_ok=True)
L = lambda p: Image.open(os.path.join(A, p)).convert("RGBA")
out = {}

# 1) 손님 머리 : 몸+옷+얼굴+머리 겹쳐 머리통만 ×6
def comp(face, hair, shirt="blue"):
    c = L("Customer/body.png")
    for p in (f"Customer/shirt_{shirt}.png", f"Customer/face_{face}.png", f"Customer/hair_{hair}.png"):
        c.alpha_composite(L(p))
    return c
heads = []
for hair in ("01_brown", "02_black"):
    for face in ("01", "02", "03"):
        heads.append(comp(face, hair).crop((20, 20, 120, 120)))
heads.append(L("Customer/face_01.png").crop((20, 20, 120, 120)))  # 머리 없이 머리통
W = 100 * 6
sheet = Image.new("RGBA", (W * 4, W * 2), (235, 225, 205, 255))
for i, h in enumerate(heads):
    sheet.alpha_composite(h.resize((W, W), Image.NEAREST), ((i % 4) * W, (i // 4) * W))
sheet.save(os.path.join(O, "customer_head_x6.png"))

# 얼굴 겹 색 목록 (살 · 볼 · 귀 · 외곽선)
f = L("Customer/face_01.png")
cols = {}
for y in range(240):
    for x in range(120):
        p = f.getpixel((x, y))
        if p[3]:
            cols[p[:3]] = cols.get(p[:3], 0) + 1
out["customer_face01_colors"] = sorted([("#%02X%02X%02X" % c, n) for c, n in cols.items()], key=lambda t: -t[1])

# 2) 떡 : bbox · 아래 여백, 진열장 · 말풍선 위에 얹어 보기
cakes = sorted(os.listdir(os.path.join(A, "Cake")))
cakes = [c for c in cakes if c.endswith(".png")]
out["cake_bbox"] = {}
for c in cakes:
    im = L("Cake/" + c)
    out["cake_bbox"][c] = im.getchannel("A").getbbox()
case = L("Screen/display_case.png")
# 진열장 받침 줄 찾기 : 접시 줄을 눈으로 보도록 ×4 + 떡을 README 값(윗단 47 · 아랫단 88 · x 27/58/89 아래 가운데)에 캔버스 그대로 놓은 것과 bbox 바닥 맞춘 것
def place(base, cake, cx, by, trim):
    if trim:
        bb = cake.getchannel("A").getbbox(); ck = cake.crop(bb)
    else:
        ck = cake
    base.alpha_composite(ck, (cx - ck.width // 2, by - ck.height))
a = case.copy(); b = case.copy()
cimg = [L("Cake/" + c) for c in cakes[:6]]
for i, (cx, by) in enumerate([(27, 47), (58, 47), (89, 47), (27, 88), (58, 88), (89, 88)]):
    place(a, cimg[i], cx, by, False)
    place(b, cimg[i], cx, by, True)
s2 = Image.new("RGBA", (case.width * 2 * 4 + 16, case.height * 4), (60, 50, 45, 255))
s2.alpha_composite(a.resize((case.width * 4, case.height * 4), Image.NEAREST), (0, 0))
s2.alpha_composite(b.resize((case.width * 4, case.height * 4), Image.NEAREST), (case.width * 4 + 16, 0))
s2.save(os.path.join(O, "cake_on_case_x4_left_canvas_right_trimmed.png"))
# 말풍선 안에 가운데 놓기
bub = L("UI/ui_bubble_c.png")
out["bubble_c_bbox"] = bub.getchannel("A").getbbox()
# 말풍선 안쪽 크림 면의 세로 범위 (가운데 열)
cx = bub.width // 2
col = [bub.getpixel((cx, y)) for y in range(bub.height)]
out["bubble_c_center_col"] = ["#%02X%02X%02X/%d" % (p[0], p[1], p[2], p[3]) for p in col]

# 3) 합성소 서랍 칸 대 찹쌀이
m = L("Screen/synthesis_machine_done.png")
body = L("Mozzi/body_hop0.png")
out["mozzi_body_bbox"] = body.getchannel("A").getbbox()
d = m.copy()
dr = ImageDraw.Draw(d)
dr.rectangle((52, 164, 120, 194), outline=(255, 0, 255, 255))
s3 = Image.new("RGBA", (m.width * 3 + 64 * 3 + 20, m.height * 3), (70, 60, 55, 255))
s3.alpha_composite(d.resize((m.width * 3, m.height * 3), Image.NEAREST), (0, 0))
s3.alpha_composite(body.resize((192, 192), Image.NEAREST), (m.width * 3 + 20, 0))
s3.save(os.path.join(O, "synthesis_drawer_vs_mozzi_x3.png"))
json.dump(out, open(os.path.join(O, "feedback_measure.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps({k: v for k, v in out.items() if k != "bubble_c_center_col"}, ensure_ascii=False)[:3000])
print(out["bubble_c_center_col"])

# 그림 111장 재기 : style 보고 + 내 잰 값(bbox · 아래 여백 · 색 수 · 채도/밝기 · 외곽선 색 · 2x2 같은색 비율)
import json, glob, os, colorsys, sys
from PIL import Image

S = os.path.dirname(os.path.abspath(__file__))
REFS = os.path.join(S, "refs")
rows = []
style = {}
for f in glob.glob(os.path.join(S, "style", "mozzi_*_report.json")):
    r = json.load(open(f, encoding="utf-8"))
    for im in r.get("images", []):
        style[im["file"].replace("\\", "/")] = im

def metrics(p):
    im = Image.open(p).convert("RGBA")
    w, h = im.size
    px = im.load()
    opaque = []
    semi = 0
    for y in range(h):
        for x in range(w):
            a = px[x, y][3]
            if a == 0:
                continue
            if a < 255:
                semi += 1
            opaque.append((x, y))
    if not opaque:
        return dict(w=w, h=h, empty=True)
    bbox = im.getchannel("A").getbbox()
    x0, y0, x1, y1 = bbox
    cols = {}
    for (x, y) in opaque:
        c = px[x, y][:3]
        cols[c] = cols.get(c, 0) + 1
    # 채도 · 밝기 (넓이 가중)
    ss = vv = 0.0
    n = 0
    for c, k in cols.items():
        hh, s_, v = colorsys.rgb_to_hsv(*(t / 255 for t in c))
        ss += s_ * k; vv += v * k; n += k
    # 바깥 고리 색 (4이웃에 투명/밖 있는 불투명 칸)
    ring = {}
    for (x, y) in opaque:
        edge = False
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if nx < 0 or ny < 0 or nx >= w or ny >= h or px[nx, ny][3] == 0:
                edge = True; break
        if edge:
            c = px[x, y][:3]
            ring[c] = ring.get(c, 0) + 1
    ringn = sum(ring.values())
    top = sorted(ring.items(), key=lambda t: -t[1])[:1]
    ring_dark = sum(k for c, k in ring.items() if (0.299*c[0]+0.587*c[1]+0.114*c[2]) < 80)
    ring_black = sum(k for c, k in ring.items() if max(c) <= 32)
    # 2x2 같은색 비율 (짝수 칸, 다 불투명인 칸만)
    same = tot = 0
    for y in range(0, h - 1, 2):
        for x in range(0, w - 1, 2):
            q = [px[x, y], px[x+1, y], px[x, y+1], px[x+1, y+1]]
            if any(t[3] == 0 for t in q):
                continue
            tot += 1
            if q[0] == q[1] == q[2] == q[3]:
                same += 1
    return dict(w=w, h=h, bbox=[x0, y0, x1, y1], bw=x1-x0, bh=y1-y0,
                bottom_gap=h - y1, top_gap=y0, colors=len(cols), semi=semi,
                sat=round(ss/n, 3), val=round(vv/n, 3),
                ring_top="#%02X%02X%02X" % top[0][0] if top else None,
                ring_top_share=round(top[0][1]/ringn, 3) if top else 0,
                ring_dark=round(ring_dark/ringn, 3), ring_black=round(ring_black/ringn, 3),
                block2=round(same/tot, 3) if tot else None, fill=len(opaque))

for kind in sorted(os.listdir(REFS)):
    for p in sorted(glob.glob(os.path.join(REFS, kind, "*.png"))):
        key = kind + "/" + os.path.basename(p)
        m = metrics(p)
        st = style.get(key, {})
        m.update(kind=kind, file=os.path.basename(p),
                 s_outline=st.get("outline"), s_light=st.get("light"), s_scale=st.get("scale"),
                 s_isolated=st.get("isolated"), s_near=st.get("near_pairs"), s_edge=st.get("edge_color"),
                 s_black=st.get("black_ratio"), s_bg=st.get("background"))
        rows.append(m)

json.dump(rows, open(os.path.join(S, "measure.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# 요약 표 (md)
out = []
out.append("| 종류 | 파일 | 크기 | bbox(w×h) | 아래여백 | 색 | 채도 | 밝기 | 외곽판정 | 고리최다색(몫) | 고리어둠 | 고리검정 | 빛 | 2x2 |")
out.append("|" + " --- |" * 14)
for r in rows:
    if r.get("empty"):
        out.append(f"| {r['kind']} | {r['file']} | {r['w']}×{r['h']} | 빈 그림 | | | | | | | | | | |")
        continue
    out.append(f"| {r['kind']} | {r['file']} | {r['w']}×{r['h']} | {r['bw']}×{r['bh']} | {r['bottom_gap']} | {r['colors']} | {r['sat']} | {r['val']} | {r['s_outline']} | {r['ring_top']}({r['ring_top_share']}) | {r['ring_dark']} | {r['ring_black']} | {r['s_light']} | {r['block2']} |")
open(os.path.join(S, "measure.md"), "w", encoding="utf-8").write("\n".join(out))
# 종류별 요약
agg = {}
for r in rows:
    if r.get("empty"): continue
    a = agg.setdefault(r["kind"], dict(n=0, colors=[], sat=[], val=[], dark=[], black=[], semi=0))
    a["n"] += 1; a["colors"].append(r["colors"]); a["sat"].append(r["sat"]); a["val"].append(r["val"])
    a["dark"].append(r["ring_dark"]); a["black"].append(r["ring_black"]); a["semi"] += r["semi"]
for k, a in agg.items():
    c = sorted(a["colors"])
    print(k, "n", a["n"], "colors min/med/max", c[0], c[len(c)//2], c[-1],
          "sat avg", round(sum(a["sat"])/a["n"], 3), "val avg", round(sum(a["val"])/a["n"], 3),
          "ring_dark avg", round(sum(a["dark"])/a["n"], 3), "ring_black avg", round(sum(a["black"])/a["n"], 3), "semi", a["semi"])

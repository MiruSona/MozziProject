"""고친 그림 검사 — 크기 · 반투명 · 알파 꼴이 원본과 같은지 보고, arttool check · layers check 를 돌린다.

결과 : 화풍통일/check/ 에 보고 JSON (people · prop · layers) 과 요약을 찍는다.
지금 그림(Resources)도 같은 묶음으로 한 번 재서 「고치기 전 → 뒤」 를 나란히 찍는다.
"""
import glob
import json
import os
import shutil
import subprocess
import sys
import tempfile

from PIL import Image

from common import ARTTOOL, BASE, OUT, OUT_ALT, RES, ROOT

PROFILES = os.path.join(ROOT, "Docs", "Reference", "Art", "화풍", "profiles")
LAYERS = os.path.join(ROOT, "Docs", "Reference", "Art", "화풍", "layers", "customer.json")
CHECK = os.path.join(BASE, "check")
PEOPLE = ["Customer/" + os.path.basename(f) for f in sorted(glob.glob(os.path.join(RES, "Customer", "*.png")))] + \
         ["Screen/stall_owner.png"]
PROP = ["Screen/stall_counter.png", "Screen/stall_prop_vase.png", "Screen/display_case.png",
        "UI/ui_bubble.png", "UI/ui_bubble_c.png", "UI/ui_button.png"]
NEW = ["UI/ui_badge_alert.png"]           # 원본이 없는 새 그림
ALPHA_MAY_GROW = {"Screen/display_case.png"}   # 유리 판(out_alt)만 투명 칸이 찬다


def opaque_colors(im):
    px = im.load()
    return len({px[x, y][:3] for y in range(im.height) for x in range(im.width) if px[x, y][3]})


def invariants():
    bad = []
    rows = []
    for base, name in ((OUT, "out"), (OUT_ALT, "out_alt")):
        for f in sorted(glob.glob(os.path.join(base, "*", "*.png"))):
            rel = os.path.relpath(f, base).replace("\\", "/")
            im = Image.open(f).convert("RGBA")
            a = im.getchannel("A")
            semi = sum(1 for v in a.getdata() if 0 < v < 255)
            src = os.path.join(RES, rel)
            if not os.path.exists(src):
                rows.append((name, rel, im.size, "새 그림", opaque_colors(im), semi))
                if semi:
                    bad.append((rel, "반투명"))
                continue
            old = Image.open(src).convert("RGBA")
            same_alpha = list(a.getdata()) == list(old.getchannel("A").getdata())
            grow_ok = name == "out_alt" and rel in ALPHA_MAY_GROW
            if im.size != old.size or semi or not (same_alpha or grow_ok):
                bad.append((rel, "크기" if im.size != old.size else "반투명" if semi else "알파 꼴"))
            changed = sum(1 for p, q in zip(im.getdata(), old.getdata()) if p != q)
            rows.append((name, rel, im.size, "알파 같음" if same_alpha else "유리 칸만 참", "%d→%d색" %
                         (opaque_colors(old), opaque_colors(im)), "바뀐 칸 %d" % changed))
    return rows, bad


def arttool(args):
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    return subprocess.run([ARTTOOL] + args, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode


def check(tag, profile, base, rels, tmp):
    stage = os.path.join(tmp, tag)
    os.makedirs(stage)
    for rel in rels:
        shutil.copy(os.path.join(base, rel), os.path.join(stage, os.path.basename(rel)))
    report = os.path.join(CHECK, "check_%s.json" % tag)
    code = arttool(["check", "--profile", os.path.join(PROFILES, profile), "--in", stage, "--report", report])
    d = json.load(open(report, encoding="utf-8"))
    warns = {}
    for w in d.get("warnings", []):
        if w["rule"] in ("ramp_shape",):
            continue
        warns[w["rule"]] = sorted(str(i.get("where")) for i in w["items"])
    return code, d["status"], d.get("failed", []), warns


def layers(tag, base, tmp):
    stage = os.path.join(tmp, "layers_" + tag)
    os.makedirs(stage)
    shutil.copy(LAYERS, os.path.join(stage, "layers.json"))
    src = os.path.join(base, "Customer")
    hairs = sorted(glob.glob(os.path.join(src, "hair_*.png")))
    shirts = sorted(glob.glob(os.path.join(src, "shirt_*.png")))
    faces = sorted(glob.glob(os.path.join(src, "face_*.png")))
    for i, hair in enumerate(hairs):
        item = os.path.splitext(os.path.basename(hair))[0]
        for layer, path in (("body", os.path.join(src, "body.png")), ("shirt", shirts[i % len(shirts)]),
                            ("face", faces[i % len(faces)]), ("hair", hair)):
            os.makedirs(os.path.join(stage, layer), exist_ok=True)
            shutil.copy(path, os.path.join(stage, layer, item + ".png"))
    report = os.path.join(CHECK, "layers_%s.json" % tag)
    code = arttool(["layers", "check", "--in", stage, "--report", report])
    d = json.load(open(report, encoding="utf-8"))
    d["in"] = "(임시) layers/customer.json + " + tag
    json.dump(d, open(report, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    return code, d["status"], [w["rule"] for w in d.get("warnings", [])]


def run():
    os.makedirs(CHECK, exist_ok=True)
    rows, bad = invariants()
    print("== 크기 · 반투명 · 알파 꼴")
    for r in rows:
        print("  ", " | ".join(str(v) for v in r))
    print("  어긋난 것 :", bad or "없음")
    tmp = tempfile.mkdtemp(prefix="bver_")
    print("== arttool check (종료 코드, 판정, 실패 규칙, 경고 → 그림)")
    for tag, profile, base, rels in (
            ("people_before", "mozzi_people.yaml", RES, PEOPLE),
            ("people_after", "mozzi_people.yaml", OUT, PEOPLE),
            ("prop_before", "mozzi_prop.yaml", RES, PROP),
            ("prop_after", "mozzi_prop.yaml", OUT, PROP + NEW),
            ("prop_alt_glass", "mozzi_prop.yaml", OUT_ALT, ["Screen/display_case.png"]),
            ("people_alt_owner", "mozzi_people.yaml", OUT_ALT, ["Screen/stall_owner.png"])):
        code, status, failed, warns = check(tag, profile, base, rels, tmp)
        print("  %s : 종료 %d · %s · 실패 %s" % (tag, code, status, failed))
        for rule, files in warns.items():
            print("      %s (%d) : %s" % (rule, len(files), ", ".join(files)))
    print("== layers check")
    for tag, base in (("before", RES), ("after", OUT)):
        print("  ", tag, layers(tag, base, tmp))
    shutil.rmtree(tmp, ignore_errors=True)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(run())

"""Resources/Art 의 그림 전체를 종류별 프로필로 `arttool check` 돌린다.

쓰는 법 (저장소 뿌리에서) :

    Tools\\ArtTool\\.venv\\Scripts\\python.exe Docs\\Reference\\Art\\화풍\\check_all.py --out <결과 폴더>

- 종류 나눔 : `점검/2026-10-05/style/mozzi_kinds.json` 의 종류 이름 + 같은 폴더 `mozzi_<종류>_report.json` 의 파일 목록.
  그 목록에 없는 그림(뒤에 더한 그림)은 폴더 기본 종류(FOLDER_KIND)로 넣고 요약에 따로 적는다.
- 결과 : `<out>/check_<묶음>.json` (arttool 보고 그대로) · `layers_<겹 묶음>.json` · `summary.json` · `summary.md`.
- 알고 두는 경고 · 실패는 `KNOWN` 목록으로 따로 센다. 요약 「새로 생긴 것」 에는 목록 밖만 나온다.
- 종류 표에 있지만 일부러 지운 그림은 `REMOVED` 에 적는다 (없어도 멈추지 않는다).
- 그림은 임시 폴더로 복사해서 잰다. Resources 는 읽기만 한다.
- 종료 코드 : 0 = 다 돌았다 (경고 · 실패가 있어도 0. 판정은 summary 를 본다) · 2 = 준비가 틀렸다.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]                       # 저장소 뿌리
ART = ROOT / "ProjectMozziUnity" / "Assets" / "_Project" / "Resources" / "Art"
STYLE = ROOT / "Docs" / "Reference" / "Art" / "점검" / "2026-10-05" / "style"
ARTTOOL = ROOT / "Tools" / "ArtTool" / ".venv" / "Scripts" / "arttool.exe"
PALETTE = HERE / "palettes" / "mozzi_master.json"
PALETTE_COPY = HERE / "profiles" / "palettes" / "mozzi_master.json"

# 종류 → 프로필 (설계 5-1)
KIND_PROFILE = {
   "char_mozzi": "mozzi_char",
   "char_people": "mozzi_people",
   "bldg": "mozzi_prop",
   "piece": "mozzi_prop",
   "tile": "mozzi_prop",
   "ui": "mozzi_prop",
   "icon": "mozzi_icon",
   "bg": "mozzi_bg",
   "fx": "mozzi_fx",
}
# 종류 안에서 프로필이 다른 그림 (설계 5-1 : 잔디 바탕은 mozzi_bg)
FILE_PROFILE = {("tile", "grass_base.png"): "mozzi_bg"}
# 합성 창 반죽 소용돌이 24장은 Fx 폴더에 있지만 회색 세 단계(곱하기용 겹) · 외곽선 겹이라 흰 1색 한도인 mozzi_fx 로는 걸린다
FILE_PROFILE.update({("fx", f"fx_swirl_{layer}_{i}.png"): "mozzi_prop" for layer in ("a", "b", "top") for i in range(8)})
# 종류 → 그 종류 그림이 있는 Resources/Art 하위 폴더 (같은 이름이 두 폴더에 있어 종류로 가른다)
KIND_FOLDERS = {
   "bg": ["Screen", "Main"],
   "bldg": ["Building", "Screen"],
   "char_mozzi": ["Mozzi"],
   "char_people": ["Customer", "Screen"],
   "fx": ["Fx"],
   "icon": ["Cake", "Ingredient"],
   "piece": ["Screen"],
   "tile": ["Farm"],
   "ui": ["UI"],
}
# 종류 표에 없는 그림이 갈 기본 종류
FOLDER_KIND = {
   "Mozzi": "char_mozzi", "Customer": "char_people", "Fx": "fx", "Cake": "icon", "Ingredient": "icon",
   "Farm": "tile", "UI": "ui", "Building": "bldg", "Main": "bg", "Screen": "piece",
   "CakeOne": "icon",
}
# 종류 표에는 있지만 일부러 지운 그림 (없어도 멈추지 않는다)
REMOVED = {
   "stall_prop_menu.png": "2026-10-05 가판대 메뉴 세움판을 뺐다",
   "fx_yinyang_half.png": "2026-10-05 반복 06 에서 음양 반쪽 연출 그림을 뺐다",
   "kitchen_tray.png": "2026-10-05 반복 06 에서 주방 쟁반 그림을 뺐다",
}
# 알고 두는 경고 · 실패 — (묶음, 규칙, 파일 무늬, 까닭). 요약은 이 목록에 없는 것만 「새로 생긴 것」 으로 따로 센다.
# 까닭의 긴 설명은 README 「알고 두는 경고 · 실패」 표. 줄을 더하거나 뺄 때 그 표도 같이 고친다.
KNOWN = [
   ("bldg", "max_colors", "kitchen_pound_*.png", "떡메 — 주방 갈아엎기 때 바뀐다"),
   ("bldg", "color_cap", "kitchen_pound_*.png", "떡메 — 주방 갈아엎기 때 바뀐다"),
   ("piece", "alpha", "display_case.png", "진열장 뒤 유리 반투명 50% — 반투명 없음 규칙의 예외 (유리 고르기 사용자 확인 대기)"),
   ("*", "outline", "*", "갈색 한 색 선을 툴이 solid, 빛 쪽 옅은 선을 selout+light 로 읽는다. 프로필은 값 하나만 받는다"),
   ("char_people", "color_cap", "face_03.png", "머리통 한도 16 — 겹마다 다른 한도를 툴이 못 가른다"),
   ("piece", "color_cap", "stall_prop_register.png", "작은 소품 — 이번 범위 밖 (금전등록기)"),
   ("piece", "color_cap", "stall_prop_vase.png", "작은 소품 — 선만 고쳤다. 색 줄이기는 다음 판"),
   ("piece", "isolated", "stall_prop_*.png", "작은 소품의 잔 점 — 1배에서 안 보인다"),
   ("tile", "color_cap", "decor_0[4-6].png", "꽃 장식 — 꽃잎 잡색 합치기는 다음 판"),
   ("tile", "color_cap", "fence_*.png", "울타리 — 설계 상한 32 안이다. fence_v(32px)는 표의 32px 칸(16)으로 잰다"),
   ("tile", "isolated", "*", "풀잎 점 · 통나무 결"),
   ("tile", "integer_scale", "fence_*.png", "통나무 결이 부드러운 확대처럼 읽힌다 — 1배에서 안 깨져 보인다"),
   ("tile_bg", "near_colors", "grass_base.png", "이음 없는 잔디 판의 색 번짐"),
   ("ui", "color_cap", "icon_*.png", "메뉴 아이콘 43 · 45색 > 32 — 이번 범위 밖"),
]
# 팔레트 파일을 보는 경고 — 그림과 상관없이 모든 묶음에 똑같이 뜬다
PALETTE_RULES = ("ramp_shape",)


def fail(text: str) -> None:
   print(f"준비가 틀렸다 : {text}", file=sys.stderr)
   sys.exit(2)


def read_json(path: Path):
   return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data) -> None:
   path.write_text(json.dumps(data, ensure_ascii=False, indent=1) + "\n", encoding="utf-8", newline="\n")


def plan_groups() -> tuple[dict[str, dict], list[str]]:
   """(묶음 이름 → {kind, profile, files}), 종류 표에 없던 그림 목록."""
   kinds = [k["kind"] for k in read_json(STYLE / "mozzi_kinds.json")["kinds"]]
   groups: dict[str, dict] = {}
   taken: set[Path] = set()

   def put(kind: str, path: Path) -> None:
      profile = FILE_PROFILE.get((kind, path.name), KIND_PROFILE[kind])
      name = kind if profile == KIND_PROFILE[kind] else f"{kind}_{profile.removeprefix('mozzi_')}"
      group = groups.setdefault(name, {"kind": kind, "profile": profile, "files": []})
      group["files"].append(path)
      taken.add(path)

   for kind in kinds:
      if kind not in KIND_PROFILE:
         fail(f"모르는 종류 {kind} — KIND_PROFILE 에 프로필을 정한다")
      report = read_json(STYLE / f"mozzi_{kind}_report.json")
      for row in report["images"]:
         base = Path(row["file"]).name
         found = [ART / folder / base for folder in KIND_FOLDERS[kind] if (ART / folder / base).is_file()]
         if not found and base in REMOVED:
            continue
         if len(found) != 1:
            fail(f"{kind}/{base} 를 Resources/Art 에서 하나로 못 찾았다 : {[str(p) for p in found]}")
         put(kind, found[0])

   extra = []
   for path in sorted(ART.glob("*/*.png")):
      if path in taken:
         continue
      folder = path.parent.name
      if folder not in FOLDER_KIND:
         fail(f"모르는 폴더 {folder} — FOLDER_KIND 에 기본 종류를 정한다")
      put(FOLDER_KIND[folder], path)
      extra.append(f"{folder}/{path.name}")
   return groups, extra


def run_arttool(args: list[str]) -> int:
   done = subprocess.run([str(ARTTOOL), *args], capture_output=True)
   return done.returncode


def color_limits(profile: str) -> tuple[int, int]:
   """프로필의 (보통 그림 색 수 한도, 배경 그림 한도). 배경 한도를 안 적었으면 보통 한도와 같다."""
   import yaml      # ArtTool 가상환경에 있다
   check = yaml.safe_load((HERE / "profiles" / f"{profile}.yaml").read_text(encoding="utf-8"))["check"]
   limit = int(check["max_colors"])
   return limit, int((check.get("background") or {}).get("max_colors") or limit)


def check_group(name: str, group: dict, out: Path, tmp: Path) -> dict:
   stage = tmp / name
   stage.mkdir()
   for path in group["files"]:
      if (stage / path.name).exists():
         fail(f"한 묶음에 같은 이름이 둘이다 : {name}/{path.name}")
      shutil.copy2(path, stage / path.name)
   report_path = out / f"check_{name}.json"
   code = run_arttool(["check", "--profile", str(HERE / "profiles" / f"{group['profile']}.yaml"), "--in", str(stage), "--report", str(report_path)])
   if not report_path.is_file():
      fail(f"{name} : arttool check 가 보고를 안 냈다 (종료 {code})")
   report = read_json(report_path)

   warns: dict[str, list[str]] = {}
   palette_warns: dict[str, int] = {}
   for w in report.get("warnings", []):
      items = w.get("items") or []
      if w["rule"] in PALETTE_RULES:
         palette_warns[w["rule"]] = len(items)
         continue
      warns[w["rule"]] = sorted({str(i.get("where", i)) if isinstance(i, dict) else str(i) for i in items})
   stray = next((len(r.get("items") or []) for r in report["rules"] if r["rule"] == "ramp_colors"), None)
   failed_detail = {r["rule"]: r["detail"] for r in report["rules"] if not r["ok"]}
   failed_files = {r["rule"]: sorted({str(i.get("where", i)) if isinstance(i, dict) else str(i) for i in (r.get("items") or [])})
                   for r in report["rules"] if not r["ok"]}
   if "max_colors" in failed_detail:
      # 보고의 max_colors 는 모든 그림의 색 수를 적는다. 한도를 넘은 것만 이름을 붙인다.
      limit, bg_limit = color_limits(group["profile"])
      rows = next(r["items"] for r in report["rules"] if r["rule"] == "max_colors")
      over_rows = [i for i in rows if i["colors"] > (bg_limit if i.get("background") else limit)]
      failed_detail["max_colors"] += " : " + ", ".join(f"{i['where']} {i['colors']}" for i in over_rows)
      failed_files["max_colors"] = sorted(i["where"] for i in over_rows)
   return {
      "group": name,
      "kind": group["kind"],
      "profile": group["profile"],
      "files": len(group["files"]),
      "status": report["status"],
      "exit": code,
      "failed": report.get("failed", []),
      "failed_detail": failed_detail,
      "failed_files": failed_files,
      "warnings": warns,
      "palette_warnings": palette_warns,
      "ramp_outside_colors": stray,
   }


# 겹 묶음 시험 — 낱장으로 흩어진 실제 겹을 「<겹>/<그림>.png」 꼴로 임시로 세운다.
# 그림(item) 하나 = 조합 하나. 모든 파일이 한 번은 들어가게 돌려 가며 짝짓는다.
def stage_mozzi(stage: Path) -> None:
   src = ART / "Mozzi"
   faces = sorted(src.glob("face_*.png"))
   bodies = sorted(src.glob("body_hop*.png"))
   accs = [src / "acc_crown.png", src / "acc_none.png"]
   for i, face in enumerate(faces):
      item = face.stem
      for layer, path in (("body", bodies[i % len(bodies)]), ("face", face), ("acc", accs[i % len(accs)])):
         (stage / layer).mkdir(exist_ok=True)
         shutil.copy2(path, stage / layer / f"{item}.png")
   # cheek 은 11×11 이라 겹 파일로 못 넣는다 (겹은 캔버스 크기여야 한다). 코드가 붙이는 겹이라 비워 둔다.


def stage_customer(stage: Path) -> None:
   src = ART / "Customer"
   hairs = sorted(src.glob("hair_*.png"))
   shirts = sorted(src.glob("shirt_*.png"))
   faces = sorted(src.glob("face_*.png"))
   for i, hair in enumerate(hairs):
      item = hair.stem
      for layer, path in (("body", src / "body.png"), ("shirt", shirts[i % len(shirts)]), ("face", faces[i % len(faces)]), ("hair", hair)):
         (stage / layer).mkdir(exist_ok=True)
         shutil.copy2(path, stage / layer / f"{item}.png")


def check_layers(name: str, stager, out: Path, tmp: Path) -> dict:
   stage = tmp / f"layers_{name}"
   stage.mkdir()
   shutil.copy2(HERE / "layers" / f"{name}.json", stage / "layers.json")
   stager(stage)
   report_path = out / f"layers_{name}.json"
   code = run_arttool(["layers", "check", "--in", str(stage), "--report", str(report_path)])
   if not report_path.is_file():
      fail(f"layers/{name}.json : arttool layers check 가 보고를 안 냈다 (종료 {code})")
   report = read_json(report_path)
   report["in"] = f"(임시) layers/{name}.json + Resources/Art"      # 임시 폴더 경로를 남기지 않는다
   write_json(report_path, report)
   rules: dict[str, int] = {}
   for w in report.get("warnings", []):
      rules[w["rule"]] = rules.get(w["rule"], 0) + 1
   return {"set": name, "status": report["status"], "exit": code, "items": len(report["items"]), "layers": report["layers"], "warnings": rules}


def known_reason(group: str, rule: str, file: str) -> str | None:
   for g, r, pattern, why in KNOWN:
      if fnmatch.fnmatch(group, g) and r == rule and fnmatch.fnmatch(file, pattern):
         return why
   return None


def split_known(rows: list[dict]) -> tuple[list[tuple], list[tuple], int]:
   """(새 실패 [(묶음, 규칙, 파일)], 새 경고 [...], 알고 두는 것 수)."""
   new_fail, new_warn, known = [], [], 0
   for row in rows:
      for kind, table in (("fail", row["failed_files"]), ("warn", row["warnings"])):
         for rule, files in table.items():
            for file in files or ["(이름 없음)"]:
               if known_reason(row["group"], rule, file):
                  known += 1
               else:
                  (new_fail if kind == "fail" else new_warn).append((row["group"], rule, file))
   return new_fail, new_warn, known


def summary_md(rows: list[dict], layer_rows: list[dict], extra: list[str], label: str) -> str:
   rules = sorted({r for row in rows for r in row["warnings"]})
   total = sum(row["files"] for row in rows)
   lines = [
      f"# 그림 전체 검사 요약 — {label}",
      "",
      f"`check_all.py` 가 낸 표다. 그림 {total}장 · 묶음 {len(rows)}개. 숫자는 **걸린 그림 장 수**다.",
      "",
      "## 묶음별",
      "",
      "| 묶음 | 프로필 | 장 | 판정 | 실패 규칙 | " + " | ".join(f"`{r}`" for r in rules) + " | 경고 합 | 램프 밖 색 |",
      "| --- | --- | --- | --- | --- | " + " | ".join("---" for _ in rules) + " | --- | --- |",
   ]
   sums = {r: 0 for r in rules}
   for row in rows:
      cells = []
      for r in rules:
         n = len(row["warnings"].get(r, []))
         sums[r] += n
         cells.append(str(n) if n else "—")
      failed = " · ".join(row["failed"]) or "—"
      lines.append(f"| {row['group']} | `{row['profile']}` | {row['files']} | {row['status']} | {failed} | " + " | ".join(cells) + f" | {sum(len(v) for v in row['warnings'].values())} | {row['ramp_outside_colors']} |")
   lines.append(f"| **합** | | **{total}** | | | " + " | ".join(f"**{sums[r]}**" for r in rules) + f" | **{sum(sums.values())}** | |")
   lines += [
      "",
      "- 「램프 밖 색」 은 공용 팔레트에 없는 색 가짓수다. `exact_match: false` 라 실패가 아니고 알리기만 한다.",
      "- `ramp_shape` 경고는 그림이 아니라 팔레트 파일을 보는 것이라 표에서 뺐다 (모든 묶음에 똑같이 뜬다) : "
      + (f"램프 {max((row['palette_warnings'].get('ramp_shape', 0) for row in rows), default=0)}줄." if rows else "없음."),
   ]
   if extra:
      lines.append(f"- 종류 표(`mozzi_kinds.json` 무리)에 없어 폴더 기본 종류로 넣은 그림 : {', '.join(extra)}")

   new_fail, new_warn, known = split_known(rows)
   lines += ["", "## 새로 생긴 것 (알고 두는 목록 밖)", "",
             f"실패 **{len(new_fail)}** · 경고 **{len(new_warn)}** · 알고 두는 것 {known} (목록은 `check_all.py` 의 `KNOWN` · README 「알고 두는 경고 · 실패」).", ""]
   if new_fail or new_warn:
      lines += ["| 갈래 | 묶음 | 규칙 | 그림 |", "| --- | --- | --- | --- |"]
      lines += [f"| 실패 | {g} | `{r}` | {f} |" for g, r, f in new_fail]
      lines += [f"| 경고 | {g} | `{r}` | {f} |" for g, r, f in new_warn]
   else:
      lines.append("없다.")

   lines += ["", "## 실패한 묶음", ""]
   bad = [row for row in rows if row["failed"]]
   if not bad:
      lines.append("없다.")
   for row in bad:
      for rule, detail in row["failed_detail"].items():
         lines.append(f"- {row['group']} : `{rule}` — {detail}")

   lines += ["", "## 경고가 걸린 그림", "", "| 묶음 | 경고 | 그림 |", "| --- | --- | --- |"]
   for row in rows:
      for rule, files in sorted(row["warnings"].items()):
         lines.append(f"| {row['group']} | `{rule}` | {', '.join(files)} |")

   lines += ["", "## 겹 묶음 (`layers check`)", "", "| 겹 묶음 | 판정 | 조합 수 | 겹 | 경고 |", "| --- | --- | --- | --- | --- |"]
   for row in layer_rows:
      warn = " · ".join(f"`{k}` {v}" for k, v in sorted(row["warnings"].items())) or "—"
      lines.append(f"| `layers/{row['set']}.json` | {row['status']} | {row['items']} | {' → '.join(row['layers'])} | {warn} |")
   return "\n".join(lines) + "\n"


def main() -> None:
   parser = argparse.ArgumentParser(description="Resources/Art 그림 전체를 종류별 프로필로 검사한다")
   parser.add_argument("--out", required=True, help="결과 폴더 (JSON 과 요약). 이미 요약이 있으면 --force 가 있어야 덮는다")
   parser.add_argument("--label", default=None, help="요약 제목에 붙일 말 (기본 : 결과 폴더 이름)")
   parser.add_argument("--force", action="store_true", help="있는 결과를 덮어쓴다")
   parser.add_argument("--sync-palette", action="store_true", help="palettes/ 의 팔레트를 profiles/palettes/ 사본으로 복사하고 시작한다")
   args = parser.parse_args()

   if not ARTTOOL.is_file():
      fail(f"arttool 이 없다 : {ARTTOOL} — powershell -File Tools\\setup.ps1 을 먼저 돌린다")
   if args.sync_palette:
      shutil.copyfile(PALETTE, PALETTE_COPY)
   if not PALETTE_COPY.is_file() or PALETTE.read_bytes() != PALETTE_COPY.read_bytes():
      fail("palettes/mozzi_master.json 과 profiles/palettes/mozzi_master.json 이 다르다 — --sync-palette 로 맞춘다")

   out = Path(args.out).resolve()
   if (out / "summary.json").exists() and not args.force:
      fail(f"이미 결과가 있다 : {out} — 다른 폴더를 주거나 --force")
   out.mkdir(parents=True, exist_ok=True)

   groups, extra = plan_groups()
   rows, layer_rows = [], []
   with tempfile.TemporaryDirectory(prefix="mozzi_check_") as tmp_text:
      tmp = Path(tmp_text)
      for name in sorted(groups):
         rows.append(check_group(name, groups[name], out, tmp))
         print(f"{name} : {rows[-1]['status']} ({rows[-1]['files']}장)")
      for name, stager in (("mozzi", stage_mozzi), ("customer", stage_customer)):
         layer_rows.append(check_layers(name, stager, out, tmp))
         print(f"layers/{name} : {layer_rows[-1]['status']}")

   label = args.label or out.name
   new_fail, new_warn, known = split_known(rows)
   write_json(out / "summary.json", {"label": label, "files": sum(r["files"] for r in rows), "groups": rows, "layers": layer_rows, "not_in_kinds": extra,
                                     "new_failures": new_fail, "new_warnings": new_warn, "known_count": known})
   (out / "summary.md").write_text(summary_md(rows, layer_rows, extra, label), encoding="utf-8", newline="\n")
   print(f"새로 생긴 것 : 실패 {len(new_fail)} · 경고 {len(new_warn)} (알고 두는 것 {known})")
   print(f"요약 : {out / 'summary.md'}")


if __name__ == "__main__":
   main()

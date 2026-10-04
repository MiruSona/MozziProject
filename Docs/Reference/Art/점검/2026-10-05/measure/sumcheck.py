import json, glob, os
S = os.path.dirname(os.path.abspath(__file__))
for f in sorted(glob.glob(os.path.join(S, "check", "*.json"))):
    r = json.load(open(f, encoding="utf-8"))
    print("=====", os.path.basename(f), r["status"], "failed", r.get("failed"), "must", r.get("must_failed"), "checked", r.get("checked"))
    for ru in r.get("rules", []):
        if not ru.get("ok"):
            print("  FAIL", ru["rule"], ru["detail"])
    for w in r.get("warnings", []):
        items = w.get("items") or []
        names = [ (i.get("where") or i.get("file") or str(i))[:60] + ("" if "value" not in i else "=" + str(i.get("value"))) for i in items[:40]]
        print("  WARN", w["rule"], "|", w.get("detail"), "|", len(items), names)
    for w in r.get("info", []):
        items = w.get("items") or []
        if w["rule"] == "outline":
            from collections import Counter
            print("  INFO outline", dict(Counter(i.get("verdict") for i in items)))
        else:
            print("  INFO", w["rule"], w.get("detail"), [str(i)[:80] for i in items[:6]])

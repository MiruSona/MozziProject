"""가판대 화풍 통일 — 처음부터 끝까지 다시 만든다.

저장소 뿌리에서 :
  Tools/ArtTool/.venv/Scripts/python.exe Docs/Reference/Art/정식/가판대/화풍통일/scripts/run_all.py

차례가 있다 : b1(선) → b2(귀 · 볼, b1 결과를 읽는다) → b3 → b4 → b5 → b6 → 비교판 → 검사.
읽는 원본은 Resources/Art (읽기만), 쓰는 곳은 화풍통일/out · out_alt · compare · check 뿐이다.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
STEPS = ["b1_reline.py", "b2_ear_cheek.py", "b3_counter_vase.py", "b4_ui_merge.py", "b5_case_glass.py",
         "b6_badge.py", "compare.py", "verify.py"]

if __name__ == "__main__":
    env = dict(os.environ, PYTHONIOENCODING="utf-8")
    for step in STEPS:
        print("----", step)
        code = subprocess.run([sys.executable, os.path.join(HERE, step)], env=env).returncode
        if code:
            sys.exit("%s 가 종료 %d 로 멈췄다" % (step, code))

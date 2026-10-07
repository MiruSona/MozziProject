# 사용자가 고른 시안 W2 천장(등 · 받침 포함, 돈 표시 없음)을 1620×250 한 장으로 굽는다 → bg/ceil_w2.png
# 견줌/build_view.py 의 ceil_strip() 과 같은 길(v5 build_mock 의 ceiling(c, "W2"))을 탄다. 옛 ceil.png 은 건드리지 않는다.
import importlib.util
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, "..", "..", "..", "시안", "반복09-v5", "build_mock.py")

spec = importlib.util.spec_from_file_location("v5mock", MOCK)
v5 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(v5)

c = Image.new("RGBA", (540 * 3, 250), (0, 0, 0, 255))
v5.ceiling(c, "W2")
out = os.path.join(HERE, "ceil_w2.png")
c.save(out)
print(out, c.size)

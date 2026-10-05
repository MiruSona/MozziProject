# PixelLab animate 판의 프레임을 raw/<이름>/fN.png 로 내려받고, 눈으로 볼 ×4 가로 띠를 만든다.
# 그림은 고치지 않는다 (내려받기 · 정수 배율 늘어놓기뿐).
# 돌리는 법 : python fetch_frames.py <job_id> <프레임 수> <이름> [띠 PNG 경로]
import io
import os
import sys
import urllib.request
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(os.path.dirname(HERE), 'raw')


def main():
    job, count, name = sys.argv[1], int(sys.argv[2]), sys.argv[3]
    out = os.path.join(RAW, name)
    os.makedirs(out, exist_ok=True)
    frames = []
    for i in range(count):
        url = 'https://api.pixellab.ai/mcp/images/%s/download?index=%d' % (job, i)
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        data = urllib.request.urlopen(req, timeout=60).read()
        im = Image.open(io.BytesIO(data)).convert('RGBA')
        im.save(os.path.join(out, 'f%d.png' % i))
        frames.append(im)
    print('%s : %d장 %dx%d' % (name, len(frames), frames[0].width, frames[0].height))
    if len(sys.argv) > 4:
        k, per = 4, 9
        rows = (len(frames) + per - 1) // per
        w, h = frames[0].size
        strip = Image.new('RGBA', (per * (w + 2) * k, rows * (h + 2) * k), (120, 110, 100, 255))
        for i, f in enumerate(frames):
            big = f.resize((w * k, h * k), Image.NEAREST)
            strip.alpha_composite(big, ((i % per) * (w + 2) * k + k, (i // per) * (h + 2) * k + k))
        strip.save(sys.argv[4])


if __name__ == '__main__':
    main()

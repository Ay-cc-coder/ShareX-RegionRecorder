"""生成应用图标：深色圆角底 + 蓝色取景框 + 红色录制点。用法：python tools/make-icon.py"""
from PIL import Image, ImageDraw
import os

SIZE = 1024
BG = (28, 28, 33, 255)
ACCENT = (59, 130, 246, 255)
RED = (240, 59, 59, 255)

img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

d.rounded_rectangle([0, 0, SIZE - 1, SIZE - 1], radius=SIZE // 5, fill=BG)

# 取景框：四个 L 形角标
inset, arm, width = 190, 150, 52
for x0, y0, dx, dy in [(inset, inset, 1, 1), (SIZE - inset, inset, -1, 1),
                       (inset, SIZE - inset, 1, -1), (SIZE - inset, SIZE - inset, -1, -1)]:
    d.rounded_rectangle(sorted([x0, x0 + dx * arm]) + sorted([y0, y0 + dy * width]),
                        radius=width // 3, fill=ACCENT)
    d.rounded_rectangle(sorted([x0, x0 + dx * width]) + sorted([y0, y0 + dy * arm]),
                        radius=width // 3, fill=ACCENT)

# 录制点
r = 132
d.ellipse([SIZE // 2 - r, SIZE // 2 - r, SIZE // 2 + r, SIZE // 2 + r], fill=RED)

out = os.path.join(os.path.dirname(__file__), "..", "src", "RegionRecorder", "Assets", "App.ico")
img.save(out, sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)])
print("wrote", os.path.abspath(out))

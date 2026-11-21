import numpy as np
from PIL import Image
from scipy import ndimage as ndi
a = np.array(Image.open("in/src.png").convert("RGB")).astype(np.int32)
H, W, _ = a.shape
bg = np.array([39, 39, 39])
d = np.sqrt(((a - bg) ** 2).sum(-1))
white = (a.min(-1) > 200)
cand = (d < 16) | white
# background = candidate pixels connected to the border
lab, n = ndi.label(cand)
border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
bgmask = np.isin(lab, list(border))
fg = ~bgmask
# drop UI islands (grey buttons, sparkle) – keep big / colourful components
lab2, n2 = ndi.label(fg)
sizes = ndi.sum(fg, lab2, range(1, n2 + 1))
keep = np.zeros(n2 + 1, bool)
main = int(np.argmax(sizes)) + 1
for i in range(1, n2 + 1):
    m = lab2 == i
    px = a[m]
    sat = (px.max(-1) - px.min(-1)).mean()
    bright = px.mean()
    if i == main or (sizes[i - 1] > 60 and not (sat < 18 and bright > 55)):
        keep[i] = True
fg = keep[lab2]
# clean the 1px halo of background-ish pixels along the edge
er = ndi.binary_erosion(fg, iterations=1)
edge = fg & ~er
halo = edge & (d < 26)
fg &= ~halo
rgba = np.dstack([a, (fg * 255)]).astype(np.uint8)
im = Image.fromarray(rgba)
bbox = im.getbbox()
im = im.crop(bbox)
print("bbox", bbox, im.size)
im.save("in/sprite.png")
prev = Image.new("RGBA", im.size, (255, 0, 255, 255)); prev.alpha_composite(im); prev.save("/tmp/cut.png")

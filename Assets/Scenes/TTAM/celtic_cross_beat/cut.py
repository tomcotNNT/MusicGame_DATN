import numpy as np
from PIL import Image
from scipy import ndimage as ndi
a = np.array(Image.open("in/src.png").convert("RGB")).astype(np.int32)
lum = a.mean(-1)
cand = lum < 30
lab, n = ndi.label(cand)
border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
fg = ~np.isin(lab, list(border))
fg = ndi.binary_opening(fg, iterations=1)
lab2, n2 = ndi.label(fg)
sizes = ndi.sum(fg, lab2, range(1, n2 + 1))
cms = ndi.center_of_mass(fg, lab2, range(1, n2 + 1))
fg = np.isin(lab2, [i + 1 for i, s in enumerate(sizes) if s > 400 and not (cms[i][1] > 135 and cms[i][0] < 70)])
fg = ndi.binary_fill_holes(fg)
rgba = np.dstack([a, fg * 255]).astype(np.uint8)
im = Image.fromarray(rgba); bb = im.getbbox(); im = im.crop(bb); im.save("in/cross.png"); print(bb, im.size)
p = Image.new("RGBA", im.size, (255, 0, 255, 255)); p.alpha_composite(im)
p.resize((p.width * 2, p.height * 2), Image.NEAREST).save("/tmp/cut.png")

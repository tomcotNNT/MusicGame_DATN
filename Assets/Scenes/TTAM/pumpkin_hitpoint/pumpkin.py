"""Jack-o'-lantern hitpoint that chomps bats — pixel art sprite set.

Shared 96x96 canvas; the pumpkin's mouth centre sits at (48, 62).
The upper half of the pumpkin hinges up like a jaw (Pac-Man style).
"""
import math, os, json, random
import numpy as np
from PIL import Image

S = 96
CX, CY = 48.0, 58.0          # body centre
yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)

OL = (34, 12, 8)
OR = [(104, 34, 12), (166, 62, 16), (222, 108, 28), (250, 156, 58), (255, 206, 120)]
STEM = [(44, 34, 20), (78, 60, 30), (116, 92, 46)]
LEAF = [(40, 76, 30), (70, 124, 46), (120, 176, 72)]
GLOW = [(255, 150, 40), (255, 200, 80), (255, 240, 170)]
INNER = [(40, 6, 6), (92, 18, 10), (160, 50, 16), (230, 120, 30)]
TONGUE = [(150, 20, 40), (210, 60, 80)]
BAT = [(18, 14, 26), (60, 16, 32), (255, 60, 40)]


def blank():
    return np.zeros((S, S, 4), np.float32)


def put(a, mask, col, alpha=1.0):
    a[mask, :3] = np.array(col, np.float32) / 255.0
    a[mask, 3] = np.maximum(a[mask, 3], alpha)


def poly_mask(pts):
    from PIL import ImageDraw
    im = Image.new("L", (S, S), 0)
    ImageDraw.Draw(im).polygon([(float(x), float(y)) for x, y in pts], fill=255)
    return np.array(im) > 0


# ---------------------------------------------------------------- pumpkin parts
LOBES = [(-17, 11, 18.5), (17, 11, 18.5), (-9, 12.5, 20.5), (9, 12.5, 20.5), (0, 13, 21.5)]
LIGHT = np.array([-0.45, -0.65, 0.62])
LIGHT /= np.linalg.norm(LIGHT)


def body_layer(glow=1.0, eyes="normal", crack=False):
    a = blank()
    for (ox, rx, ry) in LOBES:
        nx = (xx - (CX + ox)) / rx
        ny = (yy - CY) / ry
        rr = nx * nx + ny * ny
        m = rr <= 1.0
        nz = np.sqrt(np.clip(1 - rr, 0, 1))
        I = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
        tone = np.clip(((I + 0.15) / 1.0 * 4).astype(int), 0, 3)
        for t in range(4):
            put(a, m & (tone == t), OR[t])
        # rim of each lobe darker -> ribs
        rim = m & (rr > 0.86)
        put(a, rim & (xx < CX + ox), OR[0])
    # specular highlights
    for (hx, hy) in ((CX - 11, CY - 12), (CX - 2, CY - 15), (CX + 7, CY - 13)):
        put(a, (np.abs(xx - hx) < 1.5) & (np.abs(yy - hy) < 1), OR[4])
    # face: angry triangular eyes + nose
    eg = GLOW[2] if glow > 0.8 else GLOW[1] if glow > 0.45 else GLOW[0]
    eg2 = GLOW[1] if glow > 0.8 else GLOW[0]
    if eyes == "x":
        for ex in (CX - 11, CX + 11):
            for d in range(-3, 4):
                put(a, (np.abs(xx - (ex + d)) < 0.6) & (np.abs(yy - (CY - 5 + d)) < 0.6), INNER[0])
                put(a, (np.abs(xx - (ex + d)) < 0.6) & (np.abs(yy - (CY - 5 - d)) < 0.6), INNER[0])
    else:
        big = 1 if eyes == "angry" else 0
        for sgn in (-1, 1):
            ex = CX + sgn * 11
            tri = [(ex - 5 - big, CY - 1), (ex + 5 + big, CY - 1), (ex + sgn * 3, CY - 10 - big)]
            m = poly_mask(tri)
            put(a, m, eg2)
            inner = poly_mask([(ex - 3, CY - 2), (ex + 3, CY - 2), (ex + sgn * 2, CY - 7)])
            put(a, inner, eg)
        put(a, poly_mask([(CX - 2.5, CY + 2), (CX + 2.5, CY + 2), (CX, CY - 1.5)]), eg2)
    if crack:
        pts = [(CX + 14, CY - 18), (CX + 12, CY - 13), (CX + 15, CY - 9), (CX + 12, CY - 4)]
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            for k in range(8):
                u = k / 7
                put(a, (np.abs(xx - (x0 + (x1 - x0) * u)) < 0.6) & (np.abs(yy - (y0 + (y1 - y0) * u)) < 0.6), INNER[0])
    return a


def mouth_curve(x):
    """Cut line of the jaw (a grin): corners higher than the middle."""
    return CY + 8 - 0.011 * (x - CX) ** 2 * -1 - 0.0 if False else CY + 9 - 0.012 * (x - CX) ** 2


TEETH_X = [CX - 13, CX - 5, CX + 5, CX + 13]


def carve_mouth(a, glow):
    """Closed grin: glowing slit along the cut line with two teeth up, two down."""
    c = mouth_curve(xx)
    inside = np.abs(xx - CX) < 17
    h = 2.2 * np.ones_like(xx)
    slit = inside & (np.abs(yy - c) < h)
    # teeth: blocks that interrupt the slit
    up = np.zeros_like(slit); down = np.zeros_like(slit)
    for i, tx in enumerate(TEETH_X):
        blk = (np.abs(xx - tx) < 2.0)
        if i % 2 == 0:
            up |= blk & (yy < c + 1) & (yy >= c - 3)
        else:
            down |= blk & (yy >= c - 1) & (yy <= c + 3)
    hole = slit & ~up & ~down
    g = GLOW[2] if glow > 0.8 else GLOW[1] if glow > 0.45 else GLOW[0]
    put(a, hole, GLOW[0])
    put(a, hole & (np.abs(yy - c) < 1.0), g)
    return a


def stem_layer(t=0.0):
    a = blank()
    put(a, poly_mask([(CX - 3, CY - 19), (CX + 3, CY - 19), (CX + 4, CY - 27), (CX + 7, CY - 31), (CX + 5, CY - 33), (CX + 1, CY - 28), (CX - 2, CY - 22)]), STEM[1])
    put(a, poly_mask([(CX - 3, CY - 19), (CX - 1, CY - 19), (CX + 1, CY - 27), (CX + 5, CY - 32), (CX + 5, CY - 33), (CX + 1, CY - 28), (CX - 2, CY - 22)]), STEM[0])
    put(a, (np.abs(xx - (CX + 2.5)) < 0.6) & (yy > CY - 27) & (yy < CY - 20), STEM[2])
    # leaf, wiggling
    sw = math.sin(t * 2 * math.pi) * 1.2
    leaf = [(CX + 3, CY - 22), (CX + 9, CY - 27 + sw), (CX + 16, CY - 25 + sw * 1.5), (CX + 12, CY - 21 + sw), (CX + 6, CY - 20)]
    put(a, poly_mask(leaf), LEAF[1])
    put(a, poly_mask([(CX + 4, CY - 22), (CX + 10, CY - 25 + sw), (CX + 14, CY - 24.5 + sw * 1.4), (CX + 8, CY - 23)]), LEAF[2])
    # curly vine
    for k in range(18):
        ang = k / 17 * 2.6 * math.pi
        r = 4.2 - k * 0.18
        vx = CX - 6 + math.cos(ang) * r - k * 0.3
        vy = CY - 22 + math.sin(ang) * r * 0.7 + sw * 0.3
        put(a, (np.abs(xx - vx) < 0.6) & (np.abs(yy - vy) < 0.6), LEAF[0])
    return a


def outline(a, col=OL):
    al = a[..., 3] > 0
    nb = np.zeros_like(al)
    nb[1:] |= al[:-1]; nb[:-1] |= al[1:]; nb[:, 1:] |= al[:, :-1]; nb[:, :-1] |= al[:, 1:]
    edge = nb & ~al
    out = a.copy()
    out[edge, :3] = np.array(col, np.float32) / 255.0; out[edge, 3] = 1
    return out


def over(dst, src):
    m = src[..., 3] > 0
    out = dst.copy()
    out[m] = src[m]
    return out


def cavity(width_cols, top_y, bot_y, d, tongue=True):
    """Dark fiery interior between the lifted jaw and the lower half."""
    a = blank()
    for x in width_cols:
        y0, y1 = int(round(top_y[x])), int(round(bot_y[x]))
        for y in range(y0, y1 + 1):
            depth = 1 - abs(x - CX) / 26
            v = depth * (0.4 + 0.6 * (1 - abs((y - (y0 + y1) / 2) / max(1, (y1 - y0) / 2 + 0.5))))
            idx = int(np.clip(v * 4, 0, 3))
            a[y, x, :3] = np.array(INNER[idx]) / 255.0; a[y, x, 3] = 1
    if tongue and d > 6:
        ty = int(round(bot_y[int(CX)])) - 1
        m = ((xx - CX) / 7) ** 2 + ((yy - ty) / 3) ** 2 <= 1
        m &= yy <= ty + 1
        put(a, m, TONGUE[0]); put(a, m & (yy < ty - 1) & (np.abs(xx - CX) < 4), TONGUE[1])
        put(a, (np.abs(xx - CX) < 0.6) & (yy > ty - 2) & (yy < ty + 1), TONGUE[0])
    return a


def pumpkin(open_=0.0, glow=0.7, eyes="normal", t=0.0, sx=1.0, sy=1.0, dy=0, crack=False, tilt=0):
    """Return RGBA float array of the pumpkin, jaw opened by `open_` pixels."""
    body = body_layer(glow, eyes if open_ < 9 else "angry", crack)
    body = carve_mouth(body, glow) if open_ < 1 else body
    body = over(body, stem_layer(t))
    body = outline(body)
    d = int(round(open_))
    if d > 0:
        c = mouth_curve(xx)
        top = body.copy(); top[yy >= c] = 0
        bot = body.copy(); bot[yy < c] = 0
        # teeth on both jaws
        for i, tx in enumerate(TEETH_X):
            cyt = mouth_curve(tx)
            if i % 2 == 0:
                m = poly_mask([(tx - 2.5, cyt - 1), (tx + 2.5, cyt - 1), (tx, cyt + 4)])
                put(top, m, (250, 236, 200)); put(top, m & (xx > tx), (200, 180, 150))
            else:
                m = poly_mask([(tx - 2.5, cyt + 1), (tx + 2.5, cyt + 1), (tx, cyt - 4)])
                put(bot, m, (250, 236, 200)); put(bot, m & (xx > tx), (200, 180, 150))
        lifted = np.zeros_like(top)
        lifted[: S - d] = top[d:]
        cols = [x for x in range(S) if body[int(round(mouth_curve(x))), x, 3] > 0]
        topy = {x: mouth_curve(x) - d for x in cols}
        boty = {x: mouth_curve(x) for x in cols}
        cav = cavity(cols, topy, boty, d)
        out = over(over(cav, bot), lifted)
        out = outline(out)
    else:
        out = body
    img = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))
    if sx != 1.0 or sy != 1.0 or dy or tilt:
        w, h = int(round(S * sx)), int(round(S * sy))
        img2 = img.resize((w, h), Image.NEAREST)
        if tilt:
            img2 = img2.rotate(tilt, resample=Image.NEAREST, center=(w / 2, h * 0.82))
        canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        # keep the bottom of the pumpkin planted
        base_y = CY + 21
        ox = int(round(CX - CX * sx))
        oy = int(round(base_y - base_y * sy)) + dy
        canvas.paste(img2, (ox, oy), img2)
        img = canvas
    return img


# ---------------------------------------------------------------- FX helpers
def soft_glow(img, color=(255, 170, 60), strength=0.5, r=3):
    """Pixel-art friendly glow: a few faint 1px rings around bright pixels."""
    a = np.array(img).astype(np.float32) / 255
    al = a[..., 3] > 0
    from scipy import ndimage as ndi
    out = a.copy()
    grown = al.copy()
    for k in range(1, r + 1):
        g2 = ndi.binary_dilation(grown)
        ring = g2 & ~grown & (out[..., 3] == 0)
        out[ring, :3] = np.array(color) / 255
        out[ring, 3] = strength * (1 - (k - 1) / r) * 0.6
        grown = g2
    return Image.fromarray((out * 255).astype(np.uint8))


def px(img, x, y, col, a=255):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < S and 0 <= y < S:
        img.putpixel((x, y), tuple(col) + (int(a),))


BAT_PAT = [
    "..X...........X..",
    ".XX..X.....X..XX.",
    "XMMX.XX...XX.XMMX",
    "XMMMXXXXXXXXXMMMX",
    ".XMMXXXRXRXXXMMX.",
    "..XX.XXXWXWXX.XX.",
    "...X..XXXXXX.X...",
    "........XX.......",
]


def bat_sprite(img, x, y, flap=0, s=1):
    """Pixel bat (front view) centred at (x,y), pixel scale s."""
    cols = {"X": BAT[0], "M": BAT[1], "R": BAT[2], "W": (240, 236, 220)}
    pat = BAT_PAT if flap % 2 == 0 else [BAT_PAT[0]] + [r.replace("MM", "XM") for r in BAT_PAT[1:]]
    h, w = len(pat), len(pat[0])
    for j, row in enumerate(pat):
        for i, ch in enumerate(row):
            if ch == ".":
                continue
            for dy in range(s):
                for dx in range(s):
                    px(img, x - w * s / 2 + i * s + dx, y - h * s / 2 + j * s + dy, cols[ch])


def sparks(img, n, r0, r1, color, a=255, seed=0, cy=None):
    rnd = random.Random(seed)
    cy = CY + 6 if cy is None else cy
    for k in range(n):
        ang = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(r0, r1)
        x, y = CX + math.cos(ang) * r, cy + math.sin(ang) * r * 0.8
        px(img, x, y, color, a)
        px(img, x + math.cos(ang), y + math.sin(ang), color, a * 0.6)


def puff(img, x, y, r, a=200, col=(120, 110, 110)):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r and (dx + dy) % 2 == 0:
                px(img, x + dx, y + dy, col, a)


# ---------------------------------------------------------------- animations
def idle(n=16):
    out = []
    for i in range(n):
        t = i / n
        g = 0.55 + 0.2 * math.sin(t * 2 * math.pi * 3) + 0.08 * math.sin(t * 2 * math.pi * 7)
        bob = 1 if 4 <= i < 12 else 0
        img = pumpkin(0, g, t=t, dy=-bob)
        out.append(soft_glow(img, strength=0.35))
    return out


def active(n=16):
    """Bat incoming: hungry — jaw chatters open a little, eyes blaze."""
    out = []
    for i in range(n):
        t = i / n
        op = [0, 2, 3, 2, 0, 2, 4, 2][i % 8]
        img = pumpkin(op, 1.0, eyes="angry", t=t, dy=-(i % 2))
        out.append(soft_glow(img, strength=0.6))
    return out


def pulse():
    out = []
    for sx, sy, g in ((1.12, 0.88, 1.0), (1.06, 0.95, 0.9), (0.96, 1.06, 0.8), (1.0, 1.02, 0.7), (1.0, 1.0, 0.6), (1.0, 1.0, 0.55)):
        out.append(soft_glow(pumpkin(0, g, sx=sx, sy=sy), strength=0.5))
    return out


def chomp(kind="perfect"):
    gold = kind == "perfect"
    seq = [  # open, sx, sy, dy, bat(x,y) or None
        (14, 0.98, 1.06, -2, (CX - 12, CY + 3)),
        (18, 0.96, 1.10, -3, (CX + 1, CY + 5)),
        (0, 1.16, 0.84, 0, None),      # SNAP
        (3, 1.08, 0.94, 0, None),      # chew
        (0, 1.04, 0.97, 0, None),
        (2, 1.0, 1.0, 0, None),        # chew
        (0, 1.02, 0.99, 0, None),
        (0, 1.0, 1.0, 0, None),
        (0, 1.0, 1.0, 0, None),
        (0, 1.0, 1.0, 0, None),
    ]
    if not gold:
        seq = seq[:8]
    out = []
    for f, (op, sx, sy, dy, bat) in enumerate(seq):
        img = pumpkin(op, 1.0, eyes="angry", sx=sx, sy=sy, dy=dy, t=f / 10)
        if bat:
            bat_sprite(img, bat[0], bat[1], f, s=2)
        img = soft_glow(img, (255, 200, 80) if gold else (255, 150, 60), 0.8 if f >= 2 else 0.5)
        # snap flash & sparks
        if f == 2:
            sparks(img, 18 if gold else 10, 22, 30, (255, 240, 170) if gold else (255, 190, 90), seed=1)
        if f == 3:
            sparks(img, 18 if gold else 10, 28, 38, (255, 210, 90) if gold else (255, 150, 60), 220, seed=2)
        if f == 4:
            sparks(img, 12 if gold else 6, 34, 44, (255, 180, 60), 160, seed=3)
        # eye flames on perfect
        if gold and 2 <= f <= 6:
            for ex in (CX - 11, CX + 11):
                for k in range(3 + (f % 2)):
                    px(img, ex + (k % 2) - 0.5, CY - 11 - k * 2 - (6 - f), (255, 240, 170) if k < 2 else (255, 170, 60), 255 - k * 40)
        # burp: a puff + a lost bat wing flies out
        if f in (5, 6, 7):
            k = f - 5
            puff(img, CX + 14 + k * 4, CY + 4 - k * 4, 2 + k, 180 - k * 50)
            wx, wy = CX + 18 + k * 6, CY - k * 6
            for j in range(4):
                px(img, wx + j, wy - (j % 2), BAT[1], 230 - k * 60)
        out.append(img)
    return out


def miss():
    out = []
    seq = [(2, 0, -6, 1.0), (-2, 0, 5, 0.6), (1, 1, -3, 0.35), (0, 1, 2, 0.3), (0, 1, -1, 0.3), (0, 1, 0, 0.35), (0, 0, 0, 0.45), (0, 0, 0, 0.5)]
    for f, (shx, crack, tilt, g) in enumerate(seq):
        img = pumpkin(0, g, eyes="x" if 1 <= f <= 5 else "normal", tilt=tilt, crack=bool(crack))
        if shx:
            sh = Image.new("RGBA", (S, S), (0, 0, 0, 0)); sh.paste(img, (shx, 0), img); img = sh
        # dizzy stars
        if 1 <= f <= 6:
            for k in range(3):
                ang = f * 0.9 + k * 2.1
                x, y = CX + math.cos(ang) * 14, CY - 30 + math.sin(ang) * 3
                for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                    px(img, x + dx, y + dy, (255, 236, 140) if (dx, dy) == (0, 0) else (230, 180, 60))
        # the bat that got away
        if f <= 3:
            bat_sprite(img, CX - 10 - f * 9, CY - 6 - f * 7, f, s=1)
        out.append(img)
    return out


def hold_loop(n=8):
    """Mouth wide open, slurping a stream (hold note)."""
    out = []
    for i in range(n):
        op = 12 + (i % 2)
        img = pumpkin(op, 1.0, eyes="angry", t=i / n, sx=1.0 + 0.02 * (i % 2), sy=1.0)
        img = soft_glow(img, strength=0.7)
        # suction lines flowing into the mouth from the left & right
        for side in (-1, 1):
            for k in range(3):
                ph = (i / n + k / 3) % 1
                x = CX + side * (40 - ph * 24)
                y = CY + 2 + (k - 1) * 5 * (1 - ph)
                for j in range(3):
                    px(img, x - side * j, y, (255, 220, 140), 200 * (1 - ph))
        out.append(img)
    return out


def hold_progress(n=16):
    """Candle-flame arc around the pumpkin — frame = round(progress*15). Overlay."""
    out = []
    R = 33
    ang = (np.degrees(np.arctan2(yy - (CY + 1), xx - CX)) + 90) % 360
    dist = np.hypot(xx - CX, yy - (CY + 1))
    for f in range(n):
        k = f / (n - 1)
        a = np.zeros((S, S, 4), np.uint8)
        lim = 360 * k
        sel = (ang < lim) if lim < 360 else np.ones_like(ang, bool)
        if lim > 0.5:
            for rr, col in ((R + 2, OL), (R - 1, OL), (R, (255, 170, 50)), (R + 1, (255, 220, 120))):
                m = (dist >= rr - 0.5) & (dist < rr + 0.5) & sel
                a[m, :3] = col; a[m, 3] = 255
            ra = math.radians(lim - 90)
            img = Image.fromarray(a)
            hx, hy = CX + math.cos(ra) * (R + 0.5), CY + 1 + math.sin(ra) * (R + 0.5)
            for (dx, dy, c) in ((0, 0, (255, 255, 220)), (0, -1, (255, 230, 140)), (0, -2, (255, 170, 60)), (1, 0, (255, 200, 90)), (-1, 0, (255, 200, 90))):
                px(img, hx + dx, hy + dy, c)
            out.append(img)
        else:
            out.append(Image.fromarray(a))
    return out


def approach(n=12):
    out = []
    dist = np.hypot(xx - CX, yy - (CY + 1))
    for f in range(n):
        t = f / (n - 1)
        r = 46 - (46 - 26) * t
        a = np.zeros((S, S, 4), np.uint8)
        al = int(255 * (0.35 + 0.65 * t))
        m1 = (dist >= r + 0.5) & (dist < r + 1.5); a[m1, :3] = OL; a[m1, 3] = al
        m = (dist >= r - 0.5) & (dist < r + 0.5); a[m, :3] = (255, 190, 80) if t > 0.6 else (230, 120, 40); a[m, 3] = al
        out.append(Image.fromarray(a))
    return out


# ---------------------------------------------------------------- export
def save(name, frames, fps, loop, meta):
    os.makedirs(f"out/frames/{name}", exist_ok=True)
    strip = Image.new("RGBA", (S * len(frames), S), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        strip.paste(f, (i * S, 0)); f.save(f"out/frames/{name}/pumpkin_{name}_{i:02d}.png")
    strip.save(f"out/sheets/pumpkin_{name}.png")
    strip.resize((strip.width * 4, S * 4), Image.NEAREST).save(f"out/sheets/pumpkin_{name}_x4.png")
    gif = []
    for f in frames:
        g = Image.new("RGBA", (S, S), (30, 40, 30, 255)); g.alpha_composite(f)
        gif.append(g.convert("RGB").resize((S * 3, S * 3), Image.NEAREST))
    gif[0].save(f"out/gif/pumpkin_{name}.gif", save_all=True, append_images=gif[1:], duration=int(1000 / fps), loop=0)
    meta[name] = {"frames": len(frames), "fps": fps, "loop": loop}


if __name__ == "__main__":
    for d in ("sheets", "gif", "frames"):
        os.makedirs(f"out/{d}", exist_ok=True)
    sets = {"idle": (idle(), 10, True), "active": (active(), 14, True), "pulse": (pulse(), 24, False),
            "chomp_perfect": (chomp("perfect"), 20, False), "chomp_great": (chomp("great"), 20, False),
            "miss": (miss(), 14, False), "hold_loop": (hold_loop(), 14, True),
            "hold_progress": (hold_progress(), 12, False), "approach": (approach(), 12, False)}
    meta = {"frame_size": [S, S], "mouth_center_px": [int(CX), int(CY + 9)], "animations": {}}
    for k, (fr, fps, loop) in sets.items():
        save(k, fr, fps, loop, meta["animations"])
    meta["animations"]["chomp_perfect"]["note"] = "trigger on hit; frame 2 = the SNAP (bat gone) – destroy the bat object on this frame"
    meta["animations"]["chomp_great"]["note"] = "same timing as perfect, smaller FX"
    meta["animations"]["hold_progress"]["note"] = "not timed: frame = round(progress * 15), draw over hold_loop"
    meta["animations"]["approach"]["note"] = "not timed: frame by time-to-hit; last frame = on the beat"
    json.dump(meta, open("out/pumpkin_hitpoint.json", "w"), indent=2)
    rows = list(sets); maxn = max(len(v[0]) for v in sets.values()); Z = 2
    board = Image.new("RGBA", (S * Z * maxn, S * Z * len(rows)), (30, 40, 30, 255))
    for r, k in enumerate(rows):
        for i, f in enumerate(sets[k][0]):
            board.alpha_composite(f.resize((S * Z, S * Z), Image.NEAREST), (i * S * Z, r * S * Z))
    board.convert("RGB").save("out/overview.png")
    print({k: len(v[0]) for k, v in sets.items()})

"""Animate the user's Vampire Lord sprite: idle / summon / vanish / appear / die.

The source art is kept as-is; motion comes from a smooth displacement field that
leaves the head rigid and makes the cape ripple more toward its tips.
Faces LEFT (flip X in engine when he is on the left side of the screen).
"""
import math, os, json, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi

SRC = Image.open("in/sprite.png").convert("RGBA")
SW, SH = SRC.size
CW, CH = 820, 720            # frame canvas
OX, OY = 150, 70             # sprite position inside the canvas
EYE = (29 + OX, 172 + OY)
ANCHOR = (60 + OX, 245 + OY)  # neck: the cape hangs from here
HEAD_C = (48 + OX, 190 + OY)

# ---------------------------------------------------------------- base canvas + premultiplied arrays
base = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
base.paste(SRC, (OX, OY), SRC)
A = np.array(base).astype(np.float32) / 255.0
PRE = A.copy()
PRE[..., :3] *= PRE[..., 3:4]

yy, xx = np.mgrid[0:CH, 0:CW].astype(np.float32)
dist_anchor = np.hypot(xx - ANCHOR[0], yy - ANCHOR[1])
dist_head = np.hypot(xx - HEAD_C[0], yy - HEAD_C[1])
# cape weight: 0 on the head, rising toward the cape tips
W_CAPE = np.clip((dist_head - 95) / 380, 0, 1) ** 1.25
W_CAPE = ndi.gaussian_filter(W_CAPE, 6)


def warp(dx, dy, rot=0.0, scale=1.0, shift=(0, 0)):
    """Backward-map the premultiplied sprite. (dx,dy) is how far each output pixel moved."""
    sx = xx - dx - shift[0]
    sy = yy - dy - shift[1]
    if rot or scale != 1.0:
        cx, cy = ANCHOR
        c, s = math.cos(-rot), math.sin(-rot)
        rx, ry = (sx - cx) / scale, (sy - cy) / scale
        sx = cx + rx * c - ry * s
        sy = cy + rx * s + ry * c
    out = np.empty_like(PRE)
    for ch in range(4):
        out[..., ch] = ndi.map_coordinates(PRE[..., ch], [sy, sx], order=1, mode="constant", cval=0.0)
    al = out[..., 3:4]
    rgb = np.where(al > 1e-4, out[..., :3] / np.maximum(al, 1e-4), 0)
    return np.dstack([rgb, al])


def to_img(arr):
    return Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8))


def cape_wave(phase, amp=6.0, flare=0.0, droop=0.0):
    ph = phase - dist_anchor / 70.0
    dy = amp * W_CAPE * np.sin(ph) + droop * W_CAPE * 60
    dx = amp * 0.7 * W_CAPE * np.cos(ph * 0.8 + 0.6)
    if flare:
        dx += (xx - ANCHOR[0]) * flare * W_CAPE
        dy += (yy - ANCHOR[1]) * flare * W_CAPE
    return dx, dy


def comp(*ims):
    out = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    for im in ims:
        if im is not None:
            out = Image.alpha_composite(out, im)
    return out


# ---------------------------------------------------------------- FX
def glow_layer(points, color=(255, 50, 40), blur=10):
    lay = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    for (x, y, r, a) in points:
        d.ellipse((x - r, y - r, x + r, y + r), fill=color + (int(a),))
    return lay.filter(ImageFilter.GaussianBlur(blur))


def eye_glow(k, shift=(0, 0)):
    if k <= 0:
        return None
    x, y = EYE[0] + shift[0], EYE[1] + shift[1]
    g = glow_layer([(x, y, 10 + 14 * k, 120 + 120 * k)], blur=8 + 6 * k)
    core = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    ImageDraw.Draw(core).ellipse((x - 3, y - 2, x + 3, y + 2), fill=(255, 230, 210, int(255 * min(1, k))))
    return comp(g, core.filter(ImageFilter.GaussianBlur(0.8)))


SS = 2  # supersampling for crisp-but-smooth FX


def fx_canvas():
    im = Image.new("RGBA", (CW * SS, CH * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def fx_done(im):
    return im.resize((CW, CH), Image.LANCZOS)


def draw_bat(d, x, y, s, flap, ang=0.0, alpha=255):
    """Bat facing direction `ang` (radians, 0 = right). Drawn on the supersampled canvas."""
    x, y, s = x * SS, y * SS, s * SS
    ang = -math.pi / 2 + max(-0.35, min(0.35, math.cos(ang) * -0.3))  # always upright, slight bank toward travel
    ca, sa = math.cos(ang), math.sin(ang)
    def P(px, py):
        return (x + px * s * ca - py * s * sa, y + px * s * sa + py * s * ca)
    span = 1.0 if flap % 2 == 0 else 0.6
    body = (30, 18, 40, alpha); memb = (128, 24, 44, alpha); memb2 = (84, 16, 34, alpha)
    for side in (-1, 1):
        prof = [(0.2, 0.4), (0.7, 1.6), (0.5, 3.7), (-0.2, 3.1), (-0.7, 2.7), (-0.35, 1.9), (-0.85, 1.3), (-0.3, 0.5)]
        pts = [P(px_, side * py_ * span) for px_, py_ in prof]
        d.polygon(pts, fill=memb if side < 0 else memb2, outline=(8, 4, 12, alpha))
        d.line([P(0.2, side * 0.4 * span), P(0.5, side * 3.7 * span)], fill=(200, 70, 90, alpha), width=max(1, int(s * 0.14)))
        d.line([P(0.2, side * 1.2 * span), P(-0.7, side * 2.7 * span)], fill=(60, 8, 22, alpha), width=max(1, int(s * 0.08)))
    bx, by = P(0.1, 0)
    d.ellipse((bx - 0.7 * s, by - 0.8 * s, bx + 0.7 * s, by + 0.8 * s), fill=body)
    for side in (-1, 1):  # ears
        d.polygon([P(0.5, side * 0.35), P(1.5, side * 0.6), P(0.7, side * 0.05)], fill=body)
    ex, ey = P(0.8, -0.25); ex2, ey2 = P(0.8, 0.25)
    for (a_, b_) in ((ex, ey), (ex2, ey2)):
        d.ellipse((a_ - 0.18 * s, b_ - 0.18 * s, a_ + 0.18 * s, b_ + 0.18 * s), fill=(255, 70, 50, alpha))


def bat_glows(bats):
    return glow_layer([(x + math.cos(a) * s * 0.8, y + math.sin(a) * s * 0.8, s * 0.9, 110) for (x, y, s, a) in bats], blur=4)


# bat particles for vanish/appear: start inside the sprite silhouette
_mask_pts = np.argwhere(A[..., 3] > 0.5)
_rng = np.random.RandomState(4)
_pick = _mask_pts[_rng.choice(len(_mask_pts), 34, replace=False)]
SWARM = []
for i, (py, px) in enumerate(_pick):
    r = random.Random(i)
    ang = r.uniform(-math.pi * 0.95, -math.pi * 0.05) if r.random() < 0.7 else r.uniform(0, math.pi * 2)
    SWARM.append(dict(x=px, y=py, ang=ang, spd=r.uniform(260, 460), born=r.uniform(0.0, 0.6), s=r.uniform(7, 13)))


def swarm_layer(p, t):
    im, d = fx_canvas()
    bats = []
    for i, b in enumerate(SWARM):
        if p <= b["born"]:
            continue
        k = (p - b["born"]) / (1 - b["born"] + 1e-6)
        x = b["x"] + math.cos(b["ang"]) * b["spd"] * k + math.sin(k * 7 + i) * 10
        y = b["y"] + math.sin(b["ang"]) * b["spd"] * k
        heading = b["ang"] + math.sin(k * 5 + i) * 0.3
        a = int(255 * min(1, 0.3 + k * 2))
        draw_bat(d, x, y, b["s"], t + i, heading, a)
        bats.append((x, y, b["s"], heading))
    return comp(bat_glows(bats), fx_done(im))


# noise field for dissolves
_n1 = Image.fromarray((np.random.RandomState(9).rand(CH // 16, CW // 16) * 255).astype(np.uint8)).resize((CW, CH), Image.BICUBIC)
_n2 = Image.fromarray((np.random.RandomState(10).rand(CH // 5, CW // 5) * 255).astype(np.uint8)).resize((CW, CH), Image.BICUBIC)
NOISE = np.array(_n1) / 255.0 * 0.6 + np.array(_n2) / 255.0 * 0.4


def dissolve(arr, p, mode="bats"):
    if p <= 0:
        return to_img(arr)
    a = arr.copy()
    if mode == "bats":   # cape tips first, head last
        field = NOISE * 0.45 + (1 - W_CAPE) * 0.55
        e_col = np.array([1.0, 0.27, 0.2]); e2 = np.array([0.45, 0.04, 0.1])
    else:                # ash: burns from the cape tips & edges inward, head last
        field = NOISE * 0.5 + (1 - W_CAPE) * 0.5
        e_col = np.array([1.0, 0.68, 0.36]); e2 = np.array([0.35, 0.32, 0.33])
    thr = p * 1.12
    dist = field - thr
    keep = np.clip(dist / 0.012, 0, 1)
    e1 = np.clip(1 - dist / 0.03, 0, 1) * (dist > 0)
    ee = np.clip(1 - (dist - 0.03) / 0.05, 0, 1) * (dist > 0.03)
    a[..., :3] = a[..., :3] * (1 - e1[..., None]) + e_col * e1[..., None]
    a[..., :3] = a[..., :3] * (1 - ee[..., None] * 0.6) + e2 * (ee[..., None] * 0.6)
    a[..., 3] *= keep
    img = to_img(a)
    gl = np.zeros((CH, CW, 4), np.float32)
    gl[..., :3] = e_col
    gl[..., 3] = e1 * (a[..., 3] > 0.05)
    return comp(to_img(gl).filter(ImageFilter.GaussianBlur(7)), img)


ASH = []
for i in range(220):
    r = random.Random(1000 + i)
    py, px = _mask_pts[r.randrange(len(_mask_pts))]
    ASH.append(dict(x=px, y=py, w=float(W_CAPE[py, px]), vx=r.uniform(40, 200), vy=r.uniform(-120, 40),
                    sz=r.uniform(1.2, 3.6), c=r.choice([(90, 84, 88), (130, 122, 120), (170, 162, 154), (70, 64, 70),
                                                        (255, 140, 70), (255, 200, 120)])))


def ash_layer(p):
    im, d = fx_canvas()
    glows = []
    for q in ASH:
        born = (1 - q["w"]) * 0.55 * 0 + (1 - (1 - q["w"])) * 0 + (1 - q["w"]) * 0.6
        if p < born:
            continue
        k = p - born
        x = q["x"] + q["vx"] * k
        y = q["y"] + q["vy"] * k + 160 * k * k
        a = max(0.0, 1 - k * 1.3)
        if a <= 0:
            continue
        r = q["sz"] * SS
        d.ellipse((x * SS - r, y * SS - r, x * SS + r, y * SS + r), fill=q["c"] + (int(255 * a),))
        if q["c"][0] == 255:
            glows.append((x, y, q["sz"] * 3, 140 * a))
    return comp(glow_layer(glows, (255, 120, 50), 4) if glows else None, fx_done(im))


def flash(arr, k):
    a = arr.copy()
    a[..., :3] = a[..., :3] * (1 - k) + np.array([1.0, 0.93, 0.93]) * k
    return a


# ---------------------------------------------------------------- animations
def idle():
    out = []
    n = 10
    for i in range(n):
        ph = i / n * 2 * math.pi
        bob = math.sin(ph) * 5
        dx, dy = cape_wave(ph, amp=7)
        arr = warp(dx, dy, shift=(0, bob))
        out.append(comp(to_img(arr), eye_glow(0.35 + 0.15 * math.sin(ph), (0, bob))))
    return out


def summon():
    """Gather the cape, eyes flare, cape bursts open and a swarm of bats shoots out toward the player (left)."""
    out = []
    flare = [0.0, -0.06, -0.1, 0.12, 0.2, 0.2, 0.1, 0.02]
    eye = [0.4, 0.7, 1.0, 1.0, 1.0, 0.8, 0.6, 0.4]
    bob = [0, 3, 6, -6, -8, -6, -3, 0]
    burst = [0, 0, 0, 0.25, 0.55, 0.85, 1.1, 0]
    rnd = random.Random(3)
    flock = [dict(ang=rnd.uniform(math.pi * 0.78, math.pi * 1.22), spd=rnd.uniform(420, 620), s=rnd.uniform(14, 22),
                  ox=rnd.uniform(70, 240), oy=rnd.uniform(230, 430)) for _ in range(12)]
    for i in range(8):
        ph = i / 8 * 2 * math.pi
        dx, dy = cape_wave(ph, amp=6 + 6 * max(0, flare[i]) * 10, flare=flare[i])
        arr = warp(dx, dy, shift=(0, bob[i]))
        aura = None
        if 2 <= i <= 5:
            aura = glow_layer([(ANCHOR[0] + 60, ANCHOR[1] + 60, 120, 45)], (220, 30, 50), 45)
        fx = None
        if burst[i] > 0:
            im, d = fx_canvas()
            bats = []
            for k, b in enumerate(flock):
                t = burst[i]
                x = OX + b["ox"] + math.cos(b["ang"]) * b["spd"] * t * 0.75
                y = OY + b["oy"] + math.sin(b["ang"]) * b["spd"] * t * 0.55
                if -40 < x < CW + 40 and -40 < y < CH + 40:
                    draw_bat(d, x, y, b["s"], i + k, b["ang"])
                    bats.append((x, y, b["s"], b["ang"]))
            fx = comp(bat_glows(bats), fx_done(im))
        out.append(comp(aura, to_img(arr), eye_glow(eye[i], (0, bob[i])), fx))
    return out


def vanish():
    out = []
    # wrap the cape in, eyes flash
    for i, (fl, e) in enumerate(((-0.08, 0.6), (-0.16, 1.0))):
        dx, dy = cape_wave(i * 0.8, amp=5, flare=fl)
        out.append(comp(to_img(warp(dx, dy)), eye_glow(e)))
    dx, dy = cape_wave(1.6, amp=5, flare=-0.16)
    base_arr = warp(dx, dy)
    for k, p in enumerate((0.22, 0.45, 0.68, 0.88, 1.0)):
        out.append(comp(dissolve(base_arr, p, "bats"), eye_glow(1.0 * (1 - p)), swarm_layer(p, k)))
    return out


def appear():
    return list(reversed(vanish()))


def die():
    out = []
    dx, dy = cape_wave(0, amp=6)
    hit = warp(dx, dy, shift=(10, -4))
    out.append(comp(to_img(flash(hit, 0.75)), eye_glow(1.0, (10, -4))))
    for i, (sh, fl) in enumerate((((-8, 2), 0.35), ((6, 0), 0.0), ((-3, 4), 0.0))):
        dx, dy = cape_wave(1 + i, amp=8, droop=0.1 * i)
        arr = warp(dx, dy, rot=math.radians(4 * i), shift=sh)
        out.append(comp(to_img(flash(arr, fl)), eye_glow(1.0 - 0.25 * i, sh)))
    # cracks of light
    dx, dy = cape_wave(4, amp=6, droop=0.35)
    sag = warp(dx, dy, rot=math.radians(14), shift=(0, 24))
    cr, d = fx_canvas()
    rnd = random.Random(5)
    for _ in range(9):
        py, px = _mask_pts[rnd.randrange(len(_mask_pts))]
        x, y = px, py + 24
        pts = [(x, y)]
        for _ in range(4):
            x += rnd.uniform(-14, 14); y += rnd.uniform(6, 18); pts.append((x, y))
        d.line([(a * SS, b * SS) for a, b in pts], fill=(255, 190, 120, 255), width=3 * SS // 2)
    crk = fx_done(cr)
    crk_glow = crk.filter(ImageFilter.GaussianBlur(5))
    out.append(comp(to_img(sag), crk_glow, crk, eye_glow(0.4, (0, 24))))
    for k, p in enumerate((0.2, 0.4, 0.6, 0.8, 0.95)):
        dx, dy = cape_wave(4 + k * 0.5, amp=5, droop=0.35 + 0.05 * k)
        arr = warp(dx, dy, rot=math.radians(14 + 2 * k), shift=(0, 24 + 6 * k))
        out.append(comp(dissolve(arr, p, "ash"), crk_glow if p < 0.5 else None, ash_layer(p)))
    out.append(ash_layer(1.25))
    return out


# ---------------------------------------------------------------- export
def save(name, frames, fps, loop):
    os.makedirs(f"out/frames/{name}", exist_ok=True)
    for i, f in enumerate(frames):
        f.save(f"out/frames/{name}/{name}_{i:02d}.png")
    strip = Image.new("RGBA", (CW * len(frames), CH), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        strip.paste(f, (i * CW, 0))
    strip.save(f"out/sheets/vampire_lord_{name}.png")
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(f"out/sheets/vampire_lord_{name}_half.png")
    gif = []
    for f in frames:
        g = Image.new("RGBA", (CW, CH), (38, 38, 40, 255))
        g.alpha_composite(f)
        gif.append(g.convert("RGB").resize((CW // 2, CH // 2), Image.LANCZOS))
    gif[0].save(f"out/gif/vampire_lord_{name}.gif", save_all=True, append_images=gif[1:], duration=int(1000 / fps), loop=0)
    return {"frames": len(frames), "fps": fps, "loop": loop}


if __name__ == "__main__":
    import sys
    for sdir in ("sheets", "gif", "frames"):
        os.makedirs(f"out/{sdir}", exist_ok=True)
    makers = {"idle": (idle, 12, True), "summon": (summon, 12, False), "vanish": (vanish, 12, False),
              "appear": (appear, 12, False), "die": (die, 10, False)}
    only = sys.argv[1:]
    meta = {"frame_size_px": [CW, CH], "facing": "left", "sprite_offset_in_frame": [OX, OY],
            "pivot_px_from_top_left": list(ANCHOR), "animations": {},
            "events": {"summon": {"spawn_minions_frame": 3, "note": "bats burst out toward the left"}}}
    results = {}
    for name, (fn, fps, loop) in makers.items():
        if only and name not in only:
            continue
        frames = fn()
        meta["animations"][name] = save(name, frames, fps, loop)
        results[name] = frames
        print(name, len(frames), flush=True)
    if not only:
        json.dump(meta, open("out/animations.json", "w"), indent=2)
        S = 0.22
        fw, fh = int(CW * S), int(CH * S)
        maxn = max(len(v) for v in results.values())
        board = Image.new("RGBA", (80 + fw * maxn, fh * len(results)), (38, 38, 40, 255))
        bd = ImageDraw.Draw(board)
        for r, (name, frames) in enumerate(results.items()):
            bd.text((6, r * fh + fh // 2), name, fill=(230, 230, 230, 255))
            for i, f in enumerate(frames):
                board.alpha_composite(f.resize((fw, fh), Image.LANCZOS), (80 + i * fw, r * fh))
        board.convert("RGB").save("out/overview.png")

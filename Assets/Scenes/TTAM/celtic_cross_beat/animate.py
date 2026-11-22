"""Beat animation for the user's Celtic cross: knotwork lights up from the centre outward."""
import math, os, json, random, subprocess, tempfile
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi

SRC = np.array(Image.open("in/cross.png").convert("RGBA")).astype(np.float32) / 255
PAD = 34
h0, w0 = SRC.shape[:2]
H, W = h0 + PAD * 2, w0 + PAD * 2
BASE = np.zeros((H, W, 4), np.float32)
BASE[PAD:PAD + h0, PAD:PAD + w0] = SRC
ALPHA = BASE[..., 3] > 0.5
CEN = (PAD + 80, PAD + 90)                         # centre of the crossing (x, y)

lum = BASE[..., :3].mean(-1) * 255
hp = lum - ndi.gaussian_filter(lum, 3)
STRAND = ALPHA & (hp > 5) & (lum > 62)
STRAND = ndi.binary_opening(STRAND, iterations=0) if False else STRAND
r, g, b = BASE[..., 0] * 255, BASE[..., 1] * 255, BASE[..., 2] * 255
RING = ALPHA & (r > g + 6) & (lum < 70)
RING = ndi.binary_opening(RING)
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
DIST = np.hypot(xx - CEN[0], yy - CEN[1])
DMAX = DIST[ALPHA].max()

GLOW = {"gold": np.array([255, 214, 120]) / 255, "cyan": np.array([150, 236, 255]) / 255}
CORE = {"gold": np.array([255, 250, 225]) / 255, "cyan": np.array([235, 255, 255]) / 255}
RINGC = {"gold": np.array([255, 120, 50]) / 255, "cyan": np.array([90, 160, 255]) / 255}


def envelope(t):
    """Light front: rises fast, decays slowly. t in beat-time units (0 = beat)."""
    if t < 0:
        return 0.0
    return min(1.0, t / 0.08) * math.exp(-max(0, t - 0.08) * 5.0)


def to_img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


def add_glow(a, mask_strength, col, blur, gain):
    m = ndi.gaussian_filter(mask_strength, blur) * gain
    out = a.copy()
    # glow goes under/around the stone: composite as a separate premultiplied layer
    gl = np.zeros_like(a)
    gl[..., :3] = col
    gl[..., 3] = np.clip(m, 0, 1)
    # alpha-over: stone over glow
    oa = a[..., 3:4] + gl[..., 3:4] * (1 - a[..., 3:4])
    rgb = (a[..., :3] * a[..., 3:4] + gl[..., :3] * gl[..., 3:4] * (1 - a[..., 3:4])) / np.maximum(oa, 1e-4)
    return np.concatenate([rgb, oa], -1)


MOTES = [dict(x=random.Random(i).uniform(PAD + 10, W - PAD - 10), y0=random.Random(i + 7).uniform(PAD + 40, H - PAD - 20),
              v=random.Random(i + 3).uniform(40, 90), ph=random.Random(i + 5).uniform(0, 1), s=random.Random(i + 1).choice([1, 2, 2, 3]))
         for i in range(16)]


def frame(t_beat, idle_t, pal, strength=1.0):
    """t_beat: seconds since the last beat (None = no beat yet); idle_t: 0..1 loop phase."""
    a = BASE.copy()
    G, Cc, Rc = GLOW[pal], CORE[pal], RINGC[pal]
    # light front travelling from the centre to the tips
    if t_beat is None:
        k_map = np.zeros((H, W), np.float32)
    else:
        delay = DIST / DMAX * 0.16
        tt = t_beat - delay
        k_map = np.vectorize(envelope, otypes=[np.float32])(np.round(tt, 3)) if False else _env_arr(tt)
    k_map *= strength
    # idle shimmer: a faint band sliding along the strands
    band = 0.12 * (0.5 + 0.5 * np.sin((DIST / 18.0 - idle_t * 2 * math.pi)))
    k_str = np.clip(k_map + band, 0, 1) * STRAND
    # recolour strands
    col = G[None, None, :] * (1 - np.clip(k_str - 0.7, 0, 1)[..., None] * 1.5) + Cc[None, None, :] * np.clip(k_str - 0.7, 0, 1)[..., None] * 1.5
    mix = np.clip(k_str * 0.95, 0, 0.82)[..., None]
    a[..., :3] = a[..., :3] * (1 - mix) + col * mix
    # stone catches some light near lit strands
    near = ndi.gaussian_filter(k_str, 2.5)
    a[..., :3] = np.clip(a[..., :3] + G * near[..., None] * 0.12 * ALPHA[..., None], 0, 1)
    # ring glow
    k_ring = (k_map * RING)
    a[..., :3] = a[..., :3] * (1 - k_ring[..., None] * 0.7) + Rc * k_ring[..., None] * 0.7
    # halos
    peak = float(k_map.max()) if t_beat is not None else 0.0
    a = add_glow(a, k_str + 0.2 * ALPHA * peak, G, 6, 0.65)
    a = add_glow(a, k_ring + 0.0, Rc, 8, 1.2)
    img = to_img(a)
    # beam above the cross
    fx = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fa = np.zeros((H, W, 4), np.float32)
    beam_k = envelope(t_beat) * strength if t_beat is not None else 0
    if beam_k > 0.05:
        top = PAD + 2
        bw = 3 + 4 * beam_k
        m = (np.abs(xx - CEN[0]) < bw) & (yy < top + 6)
        fa[m, :3] = Cc
        fa[m, 3] = (beam_k * (yy[m] / (top + 6)) * (1 - np.abs(xx[m] - CEN[0]) / bw)).clip(0, 1)
    # rising motes
    for i, mo in enumerate(MOTES):
        life = (idle_t + mo["ph"]) % 1
        x = mo["x"] + math.sin(life * 6 + i) * 4
        y = mo["y0"] - life * mo["v"]
        al = (0.25 + 0.75 * min(1, beam_k * 1.5)) * math.sin(life * math.pi)
        s = mo["s"]
        xi, yi = int(x), int(y)
        if 0 <= xi < W - s and 0 <= yi < H - s:
            fa[yi:yi + s, xi:xi + s, :3] = Cc
            fa[yi:yi + s, xi:xi + s, 3] = np.maximum(fa[yi:yi + s, xi:xi + s, 3], al)
    fx = to_img(fa)
    soft = fx.filter(ImageFilter.GaussianBlur(2))
    out = Image.alpha_composite(img, soft)
    return Image.alpha_composite(out, fx)


def _env_arr(tt):
    rise = np.clip(tt / 0.08, 0, 1)
    dec = np.exp(-np.clip(tt - 0.08, 0, None) * 5.0)
    return np.where(tt < 0, 0, rise * dec).astype(np.float32)


def build(pal, fps=24, beat_frames=12):
    beat = [frame(i / fps, (i / beat_frames) * 0.25, pal) for i in range(beat_frames)]
    idle = [frame(None, i / 24, pal) for i in range(24)]
    return beat, idle


def save(name, frames, path_dir):
    os.makedirs(f"{path_dir}/{name}", exist_ok=True)
    strip = Image.new("RGBA", (W * len(frames), H), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        strip.paste(f, (i * W, 0)); f.save(f"{path_dir}/{name}/{name}_{i:02d}.png")
    strip.save(f"out/sheets/{name}.png")
    strip.resize((strip.width // 2, H // 2), Image.LANCZOS).save(f"out/sheets/{name}_half.png")


if __name__ == "__main__":
    for d in ("sheets", "frames", "gif"):
        os.makedirs(f"out/{d}", exist_ok=True)
    meta = {"frame_size": [W, H], "pivot_bottom_center": [W // 2, H - PAD], "animations": {}}
    bg = (12, 14, 22)
    for pal in ("gold", "cyan"):
        beat, idle = build(pal)
        save(f"celtic_cross_{pal}_beat", beat, "out/frames")
        save(f"celtic_cross_{pal}_idle", idle, "out/frames")
        meta["animations"][f"celtic_cross_{pal}_beat"] = {"frames": len(beat), "fps": 24, "loop": False, "note": "restart on every beat"}
        meta["animations"][f"celtic_cross_{pal}_idle"] = {"frames": len(idle), "fps": 12, "loop": True}
        # 120 BPM demo: every beat = 12 frames @24fps; idle shimmer continues underneath
        demo = []
        for n in range(12 * 4):
            k = n % 12
            demo.append(frame(k / 24, (n / 48), pal))
        g = []
        for f in demo:
            c = Image.new("RGBA", (W, H), bg + (255,)); c.alpha_composite(f)
            g.append(c.convert("RGB"))
        g[0].save(f"out/gif/celtic_cross_{pal}_120bpm.gif", save_all=True, append_images=g[1:], duration=42, loop=0, optimize=True)
        tmp = tempfile.mkdtemp()
        for i, f in enumerate(g * 4):
            f.save(f"{tmp}/f{i:04d}.png")
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "24", "-i", f"{tmp}/f%04d.png", "-c:v", "libx264",
                        "-preset", "veryfast", "-pix_fmt", "yuv420p", "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2",
                        f"out/gif/celtic_cross_{pal}_120bpm.mp4"], check=True)
        if pal == "gold":
            prev = Image.new("RGBA", (W * 6, H), bg + (255,))
            for i, idx in enumerate((0, 1, 2, 4, 7, 11)):
                prev.alpha_composite(beat[idx], (i * W, 0))
            prev.convert("RGB").save("out/preview_gold.png")
    json.dump(meta, open("out/celtic_cross.json", "w"), indent=2)
    print("ok", W, H)

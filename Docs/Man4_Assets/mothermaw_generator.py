"""
Sinh bộ sprite boss Màn 4 "Mother Maw" (quái mẹ) - pixel art gốc, vẽ bằng code, không dùng ảnh có sẵn.
Nửa cái đầu khổng lồ nằm ở mép phải màn hình, tóc là đàn rắn, 3 mắt đỏ, hàm há ra để nhả quái con.
Bảng màu khớp với MedusaHead.png (quái con): rắn xám-xanh, da nhợt, viền đen, mắt đỏ.

Xuất: từng frame PNG (PPU 32, khung 176x256) + ảnh preview phóng to để xem thử.
"""
import math, random, struct, zlib, os, sys

OUT_DIR = sys.argv[1] if len(sys.argv) > 1 else 'out'
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else os.path.join(OUT_DIR, '_preview.png')
os.makedirs(OUT_DIR, exist_ok=True)

W, H = 176, 288
T = None  # trong suốt

# ---------------- bảng màu ----------------
OUT      = (20, 14, 24, 255)
SKIN     = [(232, 220, 196), (206, 190, 164), (170, 152, 134), (128, 110, 104), (86, 72, 78)]   # sáng -> tối
SNAKE    = [(150, 170, 164), (108, 128, 126), (74, 88, 92), (46, 56, 64)]
RED      = [(255, 150, 130), (232, 52, 56), (150, 20, 36)]
MOUTH    = [(96, 14, 30), (58, 8, 20), (32, 4, 12)]
GLOW     = [(255, 226, 150), (255, 140, 70), (226, 60, 46)]
TEETH    = [(240, 232, 212), (176, 166, 148)]
CRACK    = (60, 44, 52)

BAYER = [[0, 2], [3, 1]]

def new():
    return [[T] * W for _ in range(H)]

def put(img, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        img[y][x] = c if c is None or len(c) == 4 else (c[0], c[1], c[2], 255)

def shade_pick(pal, v, x, y):
    """v trong [0,1] (0 = sáng nhất). Dither 2x2 giữa 2 tông kề nhau."""
    v = max(0.0, min(0.999, v)) * (len(pal) - 1)
    i = int(v)
    frac = v - i
    if i + 1 < len(pal) and frac * 4 > BAYER[y & 1][x & 1] + 0.5:
        i += 1
    return pal[i]

LIGHT = (-0.55, -0.75, 0.38)   # ánh sáng từ trên-trái
def lambert(nx, ny):
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    d = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
    return max(0.0, min(1.0, d))

# ---------------- hình học đầu ----------------
CX, CY, RX, RY = 156, 182, 120, 98          # khối đầu (phần bên phải bị mép khung cắt)
HINGE = (140, 208)                         # khớp hàm
UPPER_LIP_FRONT = (40, 200)

def in_ellipse(x, y, cx, cy, rx, ry):
    return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0

def lip_line(x, front, hinge):
    # đường thẳng từ mép môi trước -> khớp hàm
    (x0, y0), (x1, y1) = front, hinge
    if x1 == x0:
        return y0
    t = (x - x0) / (x1 - x0)
    return y0 + (y1 - y0) * t

def rot(x, y, ox, oy, a):
    c, si = math.cos(a), math.sin(a)
    x, y = x - ox, y - oy
    return ox + x * c - y * si, oy + x * si + y * c

def draw_head(img, open_amt, breathe, eye_glow, mouth_glow):
    ang = open_amt * 0.6                       # góc há hàm (rad), xoay quanh khớp hàm
    hx, hy = HINGE[0], HINGE[1] + breathe
    for y in range(H):
        for x in range(W):
            yy = y - breathe
            ul = lip_line(x, UPPER_LIP_FRONT, HINGE)
            inside = in_ellipse(x, yy, CX, CY, RX, RY)
            # 1) sọ + gáy: phía trên môi trên, hoặc phía sau khớp hàm
            if inside and (yy <= ul or x >= HINGE[0]):
                nx, ny = (x - CX) / RX, (yy - CY) / RY
                v = 1 - lambert(nx * 0.95, ny * 0.95)
                img[y][x] = shade_pick(SKIN, v * 0.9, x, y) + (255,)
                continue
            if x >= HINGE[0]:
                continue
            # 2) hàm dưới: lấy điểm, xoay ngược về tư thế ngậm rồi kiểm tra
            rx_, ry_ = rot(x, y, hx, hy, ang)
            ryy = ry_ - breathe
            if in_ellipse(rx_, ryy, CX, CY, RX, RY) and ryy > lip_line(rx_, UPPER_LIP_FRONT, HINGE) + 1 and rx_ < HINGE[0]:
                nx, ny = (rx_ - CX) / RX, (ryy - CY) / RY
                v = 1 - lambert(nx * 0.95, ny * 0.95)
                img[y][x] = shade_pick(SKIN, min(0.999, v * 0.9 + 0.04), x, y) + (255,)
                continue
            # 3) khoang miệng: giữa môi trên và môi dưới (đã xoay)
            if open_amt > 0.02 and yy > ul and x >= UPPER_LIP_FRONT[0] + 2:
                # môi dưới tại cột x = ảnh xoay của môi trên
                lx, ly = rot(x, y, hx, hy, ang)
                if ly - breathe <= lip_line(lx, UPPER_LIP_FRONT, HINGE) + 1 and in_ellipse(lx, ly - breathe, CX, CY, RX + 2, RY + 2):
                    depth = (x - UPPER_LIP_FRONT[0]) / (HINGE[0] - UPPER_LIP_FRONT[0])   # 0 = mép môi, 1 = cuống họng
                    g = mouth_glow * max(0.0, (depth - 0.25) / 0.75) ** 0.8
                    if g > 0.25:
                        img[y][x] = shade_pick(GLOW, 1 - min(1.0, g), x, y) + (255,)
                    else:
                        img[y][x] = shade_pick(MOUTH, min(0.999, 0.25 + (1 - depth) * 0.7), x, y) + (255,)
    # răng hàm trên (nhọn xuống)
    for i, tx in enumerate(range(UPPER_LIP_FRONT[0] + 6, HINGE[0] - 12, 11)):
        ty = lip_line(tx, UPPER_LIP_FRONT, HINGE) + breathe
        ln = 9 if i % 2 == 0 else 6
        for k in range(ln):
            half = max(0, int((ln - k) * 0.45))
            for dx in range(-half, half + 1):
                put(img, tx + dx, int(ty + k), TEETH[0] if dx <= 0 else TEETH[1])
    # răng hàm dưới (nhọn lên), xoay theo hàm
    for i, tx in enumerate(range(UPPER_LIP_FRONT[0] + 12, HINGE[0] - 16, 12)):
        ty = lip_line(tx, UPPER_LIP_FRONT, HINGE) + breathe + 1
        ln = 7 if i % 2 == 0 else 5
        for k in range(ln):
            half = max(0, int((ln - k) * 0.45))
            for dx in range(-half, half + 1):
                px, py = rot(tx + dx, ty - k, hx, hy, -ang)
                put(img, int(round(px)), int(round(py)), TEETH[0] if dx <= 0 else TEETH[1])
    # gờ chân mày + 3 mắt
    eyes = [(78, 150, 13), (118, 124, 10), (104, 174, 7)]
    for ex, ey, r in eyes:
        ey += breathe
        for y in range(ey - r - 5, ey + r + 3):
            for x in range(ex - r - 4, ex + r + 4):
                d = math.hypot((x - ex) / (r + 3), (y - ey) / (r + 2))
                if d <= 1 and img[y][x] is not None:
                    img[y][x] = SKIN[4] + (255,)
                d2 = math.hypot((x - ex) / r, (y - ey) / (r * 0.8))
                if d2 <= 1:
                    glow = 1 - d2
                    pal = RED if eye_glow > 0.2 else [SKIN[3], SKIN[4], SKIN[4]]
                    img[y][x] = shade_pick(pal, 1 - min(1, glow * 1.6 * max(eye_glow, 0.3)), x, y) + (255,)
                if abs(x - ex) <= 1 and abs(y - ey) <= int(r * 0.6) and eye_glow > 0.2:
                    img[y][x] = OUT
        for x in range(ex - r - 3, ex + r + 3):
            y = int(ey - r * 0.85 - 2 + ((x - ex) / (r + 3)) ** 2 * 3)
            put(img, x, y, SKIN[3]); put(img, x, y - 1, SKIN[1])
    # nếp nhăn / vết nứt cố định trên trán
    for (x0, y0, x1, y1) in [(96, 102, 120, 92), (60, 124, 84, 114), (130, 152, 150, 144), (150, 110, 170, 104)]:
        steps = max(abs(x1 - x0), abs(y1 - y0))
        for st in range(steps + 1):
            x = round(x0 + (x1 - x0) * st / steps); y = round(y0 + (y1 - y0) * st / steps) + breathe
            if img[y][x] is not None:
                put(img, x, y, CRACK)

def draw_snakes(img, phase, breathe):
    """Tóc rắn mọc từ đỉnh/gáy đầu, uốn lượn theo phase."""
    random.seed(7)
    roots = [(62, 106), (82, 94), (102, 87), (124, 84), (146, 84), (166, 87), (110, 98), (140, 92), (172, 102)]
    for i, (rx, ry) in enumerate(roots):
        length = 52 + (i * 13) % 30
        base_ang = math.radians(-135 + (i * 37) % 85)          # hướng lên, hơi ra sau / ra trước
        sway = 0.35 * math.sin(phase + i * 1.3)
        pts = []
        x, y, ang = rx, ry + breathe, base_ang
        for s in range(length):
            ang += math.sin(s / 11.0 + phase * 0.8 + i) * 0.045 + sway * 0.012
            x += math.cos(ang); y += math.sin(ang)
            pts.append((x, y))
        # thân rắn (to -> nhỏ)
        for k, (px, py) in enumerate(pts):
            r = 6.5 - 3.2 * k / len(pts)
            ir = int(r) + 1
            for dy in range(-ir, ir + 1):
                for dx in range(-ir, ir + 1):
                    if dx * dx + dy * dy <= r * r:
                        nx, ny = dx / r, dy / r
                        v = 1 - lambert(nx, ny)
                        sx, sy = int(px + dx), int(py + dy)
                        if 0 <= sx < W and 0 <= sy < H:
                            band = (k // 5) % 2 == 0 and v > 0.45      # vằn trên thân
                            img[sy][sx] = shade_pick(SNAKE, min(0.999, v * 0.9 + (0.12 if band else 0)), sx, sy) + (255,)
        # đầu rắn + mắt đỏ + lưỡi
        hx, hy = pts[-1]
        dirx, diry = math.cos(ang), math.sin(ang)
        hx += dirx * 3; hy += diry * 3
        for dy in range(-6, 7):
            for dx in range(-7, 8):
                if (dx / 7.0) ** 2 + (dy / 5.0) ** 2 <= 1:
                    v = 1 - lambert(dx / 7.0, dy / 5.0)
                    put(img, int(hx + dx), int(hy + dy), shade_pick(SNAKE, v * 0.8, int(hx + dx), int(hy + dy)))
        ex, ey = int(hx + dirx * 2 - diry * 2), int(hy + diry * 2 + dirx * 2)
        put(img, ex, ey, RED[1]); put(img, ex + 1, ey, RED[0])
        if int(phase * 3 + i) % 3 == 0:                            # lưỡi thè ra
            for s in range(1, 6):
                put(img, int(hx + dirx * (7 + s)), int(hy + diry * (7 + s)), RED[2])

def outline(img):
    out = [row[:] for row in img]
    for y in range(H):
        for x in range(W):
            if img[y][x] is None:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and img[ny][nx] is not None and img[ny][nx] != OUT:
                        out[y][x] = OUT
                        break
    return out

def tint(img, color, amt):
    return [[None if p is None else (int(p[0] + (color[0] - p[0]) * amt), int(p[1] + (color[1] - p[1]) * amt),
                                      int(p[2] + (color[2] - p[2]) * amt), p[3]) for p in row] for row in img]

def frame(open_amt=0.0, phase=0.0, breathe=0, eye_glow=1.0, mouth_glow=0.0):
    img = new()
    draw_snakes(img, phase, breathe)
    draw_head(img, open_amt, breathe, eye_glow, mouth_glow)
    return outline(img)

def dissolve(img, amount, seed):
    """Tan rã từ dưới lên + rơi vụn (frame chết)."""
    rnd = random.Random(seed)
    out = new()
    cell = {}
    for y in range(H):
        for x in range(W):
            p = img[y][x]
            if p is None:
                continue
            key = (x // 3, y // 3)
            if key not in cell:
                cell[key] = (y / H) * 0.6 + rnd.random() * 0.6
            bias = cell[key]
            if bias > 1.0 - amount:
                if rnd.random() < 0.08 * amount:                       # mảnh vụn rơi xuống
                    ny = min(H - 1, y + int(amount * 40 * rnd.random()))
                    out[ny][x] = p
                continue
            out[y][x] = p
    return out

def crack_overlay(img, level, seed):
    rnd = random.Random(seed)
    for _ in range(level * 5):
        x, y = rnd.randint(40, 170), rnd.randint(92, 262)
        ang = rnd.uniform(0, math.tau)
        for s in range(rnd.randint(8, 22)):
            ang += rnd.uniform(-0.5, 0.5)
            x += math.cos(ang); y += math.sin(ang)
            xi, yi = int(x), int(y)
            if 0 <= xi < W and 0 <= yi < H and img[yi][xi] is not None:
                img[yi][xi] = (255, 120, 80, 255) if level >= 3 and s % 4 == 0 else CRACK + (255,)
    return img

# ---------------- PNG ----------------
def save_png(path, img, scale=1, bg=None):
    h, w = len(img) * scale, len(img[0]) * scale
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        row = img[y // scale]
        for x in range(w):
            p = row[x // scale]
            if p is None:
                p = bg if bg else (0, 0, 0, 0)
            raw += bytes((p[0], p[1], p[2], p[3] if len(p) == 4 else 255))
    def chunk(t, data):
        c = struct.pack('>I', len(data)) + t + data
        return c + struct.pack('>I', zlib.crc32(t + data) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)) + \
          chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)

# ---------------- danh sách frame ----------------
frames = {}
for i in range(4):                                     # Idle: thở + rắn uốn + mắt nhấp nháy
    frames['MotherMaw_Idle_%d' % i] = frame(0.0, i * math.pi / 2, [0, 1, 2, 1][i], [1.0, 0.85, 0.7, 0.85][i])
for i, o in enumerate([0.3, 0.65, 1.0]):              # Open: há miệng báo trước (telegraph)
    frames['MotherMaw_Open_%d' % i] = frame(o, 4 + i * 0.6, 1, 1.0, 0.35 + i * 0.25)
for i in range(2):                                     # Spit: nhả quái con
    frames['MotherMaw_Spit_%d' % i] = frame(1.0 - i * 0.08, 6 + i, 0, 1.0, 1.0)
frames['MotherMaw_Hit_0'] = tint(frame(0.2, 1.0, 0, 1.0, 0.2), (255, 255, 255), 0.7)
frames['MotherMaw_Hit_1'] = tint(frame(0.2, 1.3, 0, 1.0, 0.2), (255, 60, 60), 0.35)
base = frame(0.55, 2.0, 0, 0.0, 0.0)
for i in range(6):                                     # Die: nứt -> mắt tắt -> tan rã
    img = crack_overlay([r[:] for r in base], i + 1, 11)
    img = tint(img, (40, 30, 40), 0.12 * i)
    if i >= 2:
        img = dissolve(img, (i - 1) / 4.5, 99 + i)
    frames['MotherMaw_Die_%d' % i] = img

# Cảnh báo lane: tam giác đỏ có dấu "!" (32x32), 2 frame nhấp nháy
def warn(bright):
    img = [[None] * 32 for _ in range(32)]
    c1 = RED[0] if bright else RED[1]
    c2 = RED[1] if bright else RED[2]
    for y in range(4, 29):
        half = int((y - 4) * 0.62)
        for x in range(16 - half, 16 + half + 1):
            img[y][x] = (c1 if (x + y) % 5 else c2) + (255,)
    for y in range(11, 21):
        for x in (15, 16):
            img[y][x] = (255, 244, 220, 255)
    for x in (15, 16):
        for y in (23, 24):
            img[y][x] = (255, 244, 220, 255)
    out = [r[:] for r in img]
    for y in range(32):
        for x in range(32):
            if img[y][x] is None and any(0 <= x + dx < 32 and 0 <= y + dy < 32 and img[y + dy][x + dx] is not None
                                          for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out[y][x] = OUT
    return out

for name, img in frames.items():
    save_png(os.path.join(OUT_DIR, name + '.png'), img)
save_png(os.path.join(OUT_DIR, 'LaneWarning_0.png'), warn(True))
save_png(os.path.join(OUT_DIR, 'LaneWarning_1.png'), warn(False))

# preview: lưới phóng to x2 trên nền hang tối
names = list(frames)
cols = 6
rows = (len(names) + cols - 1) // cols
PW, PH = W * cols, H * rows
sheet = [[(34, 32, 44, 255)] * PW for _ in range(PH)]
for k, n in enumerate(names):
    ox, oy = (k % cols) * W, (k // cols) * H
    for y in range(H):
        for x in range(W):
            p = frames[n][y][x]
            if p is not None:
                sheet[oy + y][ox + x] = p
save_png(PREVIEW, sheet, scale=1)
print('xuất', len(frames) + 2, 'frame vào', OUT_DIR, '| preview', PREVIEW)

"""Ảnh tổng hợp các sprite trong 1 sheet, đánh số theo hậu tố tên, gom theo hàng của sheet.
contact.py <png> <out_prefix> [min_idx max_idx]"""
import re, sys, struct, zlib
from collections import defaultdict
sys.path.insert(0, __file__.rsplit('\\', 1)[0].rsplit('/', 1)[0])

def load_png(path):
    d = open(path, 'rb').read()
    w, h, bd, ct = struct.unpack('>IIBB', d[16:26])
    p, idat, plte, trns = 8, bytearray(), None, None
    while p < len(d):
        ln = struct.unpack('>I', d[p:p + 4])[0]; t = d[p + 4:p + 8]
        if t == b'IDAT': idat += d[p + 8:p + 8 + ln]
        if t == b'PLTE': plte = d[p + 8:p + 8 + ln]
        if t == b'tRNS': trns = d[p + 8:p + 8 + ln]
        p += 12 + ln
    bpp = {6: 4, 2: 3, 3: 1, 0: 1, 4: 2}[ct]
    raw = zlib.decompress(bytes(idat)); st = w * bpp; prev = bytearray(st); rows = []; pos = 0
    for _ in range(h):
        f = raw[pos]; L = bytearray(raw[pos + 1:pos + 1 + st]); pos += 1 + st
        for i in range(st):
            a = L[i - bpp] if i >= bpp else 0; b = prev[i]; c = prev[i - bpp] if i >= bpp else 0
            if f == 1: L[i] = (L[i] + a) & 255
            elif f == 2: L[i] = (L[i] + b) & 255
            elif f == 3: L[i] = (L[i] + ((a + b) >> 1)) & 255
            elif f == 4:
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                L[i] = (L[i] + (a if pa <= pb and pa <= pc else (b if pb <= pc else c))) & 255
        if ct == 6: row = [tuple(L[x * 4:x * 4 + 4]) for x in range(w)]
        elif ct == 2: row = [tuple(L[x * 3:x * 3 + 3]) + (255,) for x in range(w)]
        elif ct == 3: row = [tuple(plte[L[x] * 3:L[x] * 3 + 3]) + ((trns[L[x]] if trns and L[x] < len(trns) else 255),) for x in range(w)]
        else: row = [(L[x], L[x], L[x], 255) for x in range(w)]
        rows.append(row); prev = L
    return w, h, rows

FONT = {'0': '111101101101111', '1': '010110010010111', '2': '111001111100111', '3': '111001111001111', '4': '101101111001001',
        '5': '111100111001111', '6': '111100111101111', '7': '111001001001001', '8': '111101111101111', '9': '111101111001111'}

def save(path, img):
    H = len(img); W = len(img[0]); raw = bytearray()
    for y in range(H):
        raw.append(0)
        for x in range(W): raw += bytes(img[y][x])
    def ch(t, dd):
        c = struct.pack('>I', len(dd)) + t + dd
        return c + struct.pack('>I', zlib.crc32(t + dd) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + ch(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 6, 0, 0, 0)) + ch(b'IDAT', zlib.compress(bytes(raw), 6)) + ch(b'IEND', b''))

png, out = sys.argv[1], sys.argv[2]
lo, hi = (int(sys.argv[3]), int(sys.argv[4])) if len(sys.argv) > 4 else (0, 10 ** 9)
W0, H0, px = load_png(png)
meta = open(png + '.meta', encoding='utf-8').read()
sp = re.findall(r'- serializedVersion: 2\s+name: (\S[^\n]*)\s+rect:\s+serializedVersion: 2\s+x: (\S+)\s+y: (\S+)\s+width: (\S+)\s+height: (\S+)', meta)
rows = defaultdict(list)
for n, x, y, w, h in sp:
    m = re.search(r'(\d+)$', n.strip())
    if not m: continue
    idx = int(m.group(1))
    if not (lo <= idx <= hi): continue
    x, y, w, h = int(float(x)), int(float(y)), int(float(w)), int(float(h))
    if w < 20 or h < 20: continue
    rows[round(y / 40)].append((x, idx, x, y, w, h))
S = 2; CELL = 96; PER = 14
lines = []
for k in sorted(rows, reverse=True):
    items = sorted(rows[k])
    for i in range(0, len(items), PER): lines.append(items[i:i + PER])
page, pi = [], 0
def flush(page, pi):
    LH = CELL * S // 2 + 14
    img = [[(40, 40, 48, 255)] * (PER * (CELL + 4)) for _ in range(len(page) * (CELL + 16))]
    for li, line in enumerate(page):
        oy = li * (CELL + 16)
        for ci, (_, idx, x, y, w, h) in enumerate(line):
            ox = ci * (CELL + 4)
            sc = min(1.0, (CELL - 2) / max(w, h)) * 1.4
            sc = min(sc, (CELL - 2) / max(w, h))
            top = H0 - (y + h)
            for yy in range(int(h * sc)):
                for xx in range(int(w * sc)):
                    p = px[top + int(yy / sc)][x + int(xx / sc)]
                    if p[3] > 20:
                        img[oy + CELL - int(h * sc) + yy][ox + xx] = p[:3] + (255,)
            s = str(idx)
            for di, dch in enumerate(s):
                bits = FONT[dch]
                for b in range(15):
                    if bits[b] == '1':
                        for a in range(2):
                            for c in range(2):
                                img[oy + CELL + 2 + (b // 3) * 2 + a][ox + di * 8 + (b % 3) * 2 + c] = (255, 230, 80, 255)
    save('%s_%d.png' % (out, pi), img)
    print('%s_%d.png' % (out, pi), len(page), 'lines')
for line in lines:
    page.append(line)
    if len(page) >= 9:
        flush(page, pi); page = []; pi += 1
if page: flush(page, pi)

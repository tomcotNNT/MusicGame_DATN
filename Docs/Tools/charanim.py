"""Sinh / chỉnh clip animation cho 3 nhân vật theo chuẩn bắt beat + căn pivot frame.
Chạy từ thư mục Assets:  py charanim.py [--dry]
- Thời lượng mỗi frame tính bằng ms, làm tròn theo bước 1/60 s (sample rate 60).
- Clip đã có: ghi đè nội dung, GIỮ nguyên .meta (GUID) -> Animator không mất liên kết.
- Pivot: frame nào đang alignment != 9 (chưa căn) thì căn theo frame Idle đầu tiên:
  cột thân người (cột đặc nhất) và mép chân (hàng đáy) trùng với frame Idle.
"""
import os, re, sys, struct, zlib, uuid

sys.stdout.reconfigure(encoding="utf-8")
DRY = '--dry' in sys.argv
TEMPLATE = 'MusicGame/Animation/P2/Attack3.anim'

CHARS = {
    'Ramona': dict(
        png='MusicGame/Prefabs/Member/Long/Characters/RamonaFlower.png', base='RamonaFlower_',
        dir='MusicGame/Animation/P1', ref=48,
        clips={
            'Idle':      (True,  [(i, 83) for i in range(48, 54)]),
            'Attacking': (False, [(352, 33), (353, 83), (354, 50), (355, 50), (356, 67)]),
            'Attack 2':  (False, [(681, 33), (682, 83), (683, 67), (684, 83)]),
            'Attack3':   (False, [(801, 50), (802, 100), (803, 67), (804, 83), (805, 67)]),
            'Hold':      (True,  [(696, 100), (697, 100), (699, 100), (701, 100)]),
            'Die':       (False, [(i, 83) for i in range(534, 547)]),
            'Dodge':     (False, [(256, 33), (257, 33), (258, 33), (259, 33), (260, 67)]),
            'Hurt':      (False, [(507, 67), (508, 117), (507, 83)]),
            'Victory':   (True,  [(i, 100) for i in range(741, 746)]),
            'Special':   (False, [(1073, 50), (1074, 50), (1076, 50), (1077, 83), (1078, 67), (1079, 67), (1080, 67), (1081, 100)]),
            'AirAttack': (False, [(1061, 50), (1062, 50), (1063, 50), (1064, 67), (1065, 67), (1066, 83)]),
        }),
    'Chau': dict(
        png='Scenes/TTAM/Char/Knives Chau.png', base='Knives Chau_',
        dir='MusicGame/Animation/P2', ref=41,
        clips={
            'Idle':      (True,  [(i, 125) for i in range(41, 45)]),
            'Attack1':   (False, [(886, 83), (887, 67), (888, 83)]),
            'Attack2':   (False, [(896, 83), (897, 67), (898, 83)]),
            'Attack3':   (False, [(590, 100), (591, 67), (592, 83)]),
            'Dodge':     (False, [(436, 33), (437, 33), (438, 33), (439, 33), (440, 67)]),
            'Hurt':      (False, [(724, 67), (725, 117), (724, 83)]),
            'Victory':   (True,  [(i, 100) for i in range(876, 882)]),
            'Special':   (False, [(940, 67), (941, 67), (942, 67), (943, 83), (944, 100), (945, 83)]),
            'AirAttack': (False, [(928, 50), (929, 83), (930, 67), (931, 83)]),
        }),
    'Stephen': dict(
        png='Scenes/TTAM/Char/Stephen Stills.png', base='Stephen Stills_',
        dir='MusicGame/Animation/P3', ref=50,
        clips={
            'Idle':      (True,  [(i, 83) for i in range(50, 56)]),
            'Attack1':   (False, [(761, 83), (762, 67), (760, 83)]),
            'Attack2':   (False, [(764, 83), (765, 67), (766, 83)]),
            'Attack':    (False, [(770, 83), (771, 67), (772, 83), (773, 67)]),
            'Dodge':     (False, [(261, 33), (262, 33), (263, 33), (264, 33), (265, 67)]),
            'Hurt':      (False, [(338, 67), (339, 117), (340, 83)]),
            'Victory':   (True,  [(i, 100) for i in range(715, 721)]),
            'Special':   (False, [(1090, 50), (1076, 67), (1077, 50), (1091, 67), (1092, 50), (1078, 67), (1079, 67), (1080, 100)]),
            'AirAttack': (False, [(789, 50), (790, 83), (791, 67), (792, 83)]),
        }),
}

def alpha(path):
    d = open(path, 'rb').read()
    w, h, bd, ct = struct.unpack('>IIBB', d[16:26])
    assert ct == 6 and bd == 8, (path, ct, bd)
    p, idat = 8, bytearray()
    while p < len(d):
        ln = struct.unpack('>I', d[p:p + 4])[0]
        if d[p + 4:p + 8] == b'IDAT': idat += d[p + 8:p + 8 + ln]
        p += 12 + ln
    raw = zlib.decompress(bytes(idat)); st = w * 4; prev = bytearray(st); out = []; pos = 0
    for _ in range(h):
        f = raw[pos]; L = bytearray(raw[pos + 1:pos + 1 + st]); pos += 1 + st
        if f == 1:
            for i in range(4, st): L[i] = (L[i] + L[i - 4]) & 255
        elif f == 2:
            for i in range(st): L[i] = (L[i] + prev[i]) & 255
        elif f == 3:
            for i in range(st): L[i] = (L[i] + (((L[i - 4] if i >= 4 else 0) + prev[i]) >> 1)) & 255
        elif f == 4:
            for i in range(st):
                a = L[i - 4] if i >= 4 else 0; b = prev[i]; c = prev[i - 4] if i >= 4 else 0
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                L[i] = (L[i] + (a if pa <= pb and pa <= pc else (b if pb <= pc else c))) & 255
        out.append(bytes(L[3::4])); prev = L
    return w, h, out

ENTRY = re.compile(r'(    - serializedVersion: 2\n      name: (?P<name>[^\n]+)\n      rect:\n        serializedVersion: 2\n'
                   r'        x: (?P<x>\S+)\n        y: (?P<y>\S+)\n        width: (?P<w>\S+)\n        height: (?P<h>\S+)\n'
                   r'      alignment: (?P<al>\S+)\n      pivot: \{x: (?P<px>\S+), y: (?P<py>\S+)\}\n)')

def anchor(A, H, x, y, w, h):
    """(cột thân người, hàng đáy) tính từ góc trái-dưới của rect, đơn vị pixel."""
    top = H - (y + h)
    cols = [0] * w; bottom = None
    for r in range(h):
        row = A[top + r]
        cnt = 0
        for c in range(w):
            if row[x + c] > 40:
                cols[c] += 1; cnt += 1
        if cnt: bottom = r
    sm = [sum(cols[max(0, c - 4):c + 5]) for c in range(w)]
    cx = max(range(w), key=lambda c: sm[c]) + 0.5
    return cx, (h - 1 - bottom) if bottom is not None else 0

tpl = open(TEMPLATE, encoding='utf-8').read().replace('\r\n', '\n')

def write_clip(path, name, keys, loop):
    t, body, mapping = 0, '', ''
    for fid, guid, ms in keys:
        body += '    - time: %s\n      value: {fileID: %s, guid: %s, type: 3}\n' % (round(t / 60, 6), fid, guid)
        mapping += '    - {fileID: %s, guid: %s, type: 3}\n' % (fid, guid)
        t += max(1, round(ms / (1000 / 60)))
    s = re.sub(r'(  m_Name: )[^\n]*', lambda m: m.group(1) + name, tpl, count=1)
    s = re.sub(r'(    curve:\n)(?:    - time: [^\n]*\n      value: [^\n]*\n)+', lambda m: m.group(1) + body, s, count=1)
    s = re.sub(r'(    pptrCurveMapping:\n)(?:    - \{fileID[^\n]*\n)+', lambda m: m.group(1) + mapping, s, count=1)
    s = re.sub(r'  m_SampleRate: \S+', '  m_SampleRate: 60', s, count=1)
    s = re.sub(r'    m_StopTime: \S+', '    m_StopTime: %s' % round(t / 60, 6), s, count=1)
    s = re.sub(r'    m_LoopTime: \d', '    m_LoopTime: %d' % (1 if loop else 0), s, count=1)
    new = not os.path.exists(path)
    if DRY: return new, t / 60
    open(path, 'w', encoding='utf-8', newline='\n').write(s)
    if not os.path.exists(path + '.meta'):
        open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(
            'fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 7400000\n'
            '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)
    return new, t / 60

for cname, c in CHARS.items():
    meta_p = c['png'] + '.meta'
    raw_meta = open(meta_p, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in raw_meta else '\n'
    meta = raw_meta.replace('\r\n', '\n')
    guid = re.search(r'^guid: (\w+)', meta, re.M).group(1)
    ids = dict(re.findall(r'^      (%s\d+): (-?\d+)$' % re.escape(c['base']), meta, re.M))
    ent = {m.group('name'): m for m in ENTRY.finditer(meta)}
    W, H, A = alpha(c['png'])

    used = set()
    for loop, frames in c['clips'].values():
        for i, _ in frames:
            n = c['base'] + str(i)
            assert n in ids and n in ent, (cname, n, 'không có trong sheet')
            used.add(n)

    rn = c['base'] + str(c['ref'])
    r = ent[rn]
    rx, ry, rw, rh = (int(float(r.group(k))) for k in 'xywh')
    rax, ray = anchor(A, H, rx, ry, rw, rh)
    rpx, rpy = float(r.group('px')) * rw, float(r.group('py')) * rh
    dx, dy = rax - rpx, ray - rpy

    fixed = 0
    for n in sorted(used):
        m = ent[n]
        if m.group('al') == '9' or n == rn: continue
        x, y, w, h = (int(float(m.group(k))) for k in 'xywh')
        ax, ay = anchor(A, H, x, y, w, h)
        px, py = (ax - dx) / w, (ay - dy) / h
        new = m.group(0).replace('      alignment: %s\n' % m.group('al'), '      alignment: 9\n') \
                        .replace('pivot: {x: %s, y: %s}' % (m.group('px'), m.group('py')), 'pivot: {x: %.4f, y: %.4f}' % (px, py))
        meta = meta.replace(m.group(0), new, 1)
        fixed += 1
    if not DRY and fixed:
        open(meta_p, 'w', encoding='utf-8', newline='').write(meta.replace('\n', nl))

    report = []
    for clip, (loop, frames) in c['clips'].items():
        keys = [(ids[c['base'] + str(i)], guid, ms) for i, ms in frames]
        isnew, length = write_clip('%s/%s.anim' % (c['dir'], clip), clip, keys, loop)
        report.append('%s%s %.2fs' % ('+' if isnew else '~', clip, length))
    print('%-8s pivot căn %2d frame | %s' % (cname, fixed, ', '.join(report)))

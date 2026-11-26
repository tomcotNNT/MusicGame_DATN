# Hướng dẫn cho Art / Animation / Âm nhạc

## 1. Animation nhân vật (Ramona · Stephen · Knives Chau)

### 1.1 Hiện trạng (cập nhật 26/11)
| Nhân vật | Controller Man1 (chạy) | Controller Man2/Man3 (đứng) | Clip | Giá mở khóa |
|---|---|---|---|---|
| Ramona | `P1/Player.controller` | `P1/Player 1.controller` | đủ | mở sẵn |
| Stephen Stills | `P3/Stephen_Run.controller` | `P3/Stephen Stills_50.controller` | Idle 50–55, Run 139–146, Attack1 760–762, Attack2 763–765, Attack3 769–772, Jump 228–238, Hold (guitar) 652–659, Die 535–548 | 1.500 coin |
| Knives Chau | `P2/Chau_Run.controller` | `P2/Chau.controller` | Idle 41–44, Run 145–152, Attack1 979–983, Attack2 955–961, Attack3 984–988, Jump 320–331, Hold 605–608, Die 724–736 | 2.500 coin |

- Cả 4 controller mới **chép nguyên cấu trúc Animator của Ramona**: cùng tên state, tham số và transition. Có thêm trigger `Attack` vì Man3 dùng.
- Số frame là số ở cuối tên sprite. Muốn đổi frame: sửa số trong tool **Animator nhân vật** rồi bấm **Tạo clip** (ghi đè, giữ GUID).
- **Mở khóa:** ở SelectChar, chọn nhân vật đang khóa → trên nút Play hiện "UNLOCK … COIN" → bấm để trả coin. Không đủ coin thì mở bảng nạp tiền. Giá chỉnh ở `ListCharController ▸ Unlock Coin Cost`.
- **Nhảy:** `PlayerController` (Man1) giờ bắn trigger `Jump` khi lướt lên nốt trên và tính `IsGround` thật. Cả 3 nhân vật đều có animation nhảy (trước đây Ramona cũng không bao giờ nhảy).

### 1.2 Thông số cắt sprite (giống Ramona)
| Mục | Giá trị |
|---|---|
| Texture Type / Sprite Mode | Sprite (2D and UI) / **Multiple** |
| Pixels Per Unit | **32** |
| Filter Mode | **Point (no filter)** |
| Compression | **None** |
| Pivot | **Center (0.5, 0.5)**. Mọi frame của cùng một clip phải cùng pivot để nhân vật không bị giật |
| Tên sprite | Giữ kiểu Unity tự đặt: `Knives Chau_841`, `Stephen Stills_230`… Tool đọc **số ở cuối tên** |

### 1.3 Clip cần làm và cách nối vào Controller
| Clip | FPS gợi ý | Loop | Số frame (tham khảo Ramona) | Code điều khiển bằng |
|---|---|---|---|---|
| Idle | 10 | ✅ | 4–6 | Trạng thái mặc định |
| Run | 12 | ✅ | 6–8 | (Chỉ dùng ở intro) |
| Attack / Attack1–3 | 15 | ❌ | 4–7 | Trigger `Attack`, `Attack1`, `Attack2`, `Attack3` |
| Jump | 12 | ❌ | 6–8 | Bool `IsGround = false` → Jump; `true` → Idle |
| Hold | 12 | ✅ | 3–4 | Bool `Sing = true` (đang giữ nốt dài) → Hold; `false` → Idle |
| Die | 10 | ❌ | 8–13 | Trigger `Die` (từ Any State, chạy cả khi game dừng) |

**Các bước:**
1. Mở **Tools ▸ BeatRun ▸ Animator nhân vật**.
2. Kéo Controller (ví dụ `Chau.controller`) và Sprite Sheet (`Knives Chau.png`) vào.
3. Nhập số frame cho từng clip, ví dụ `Jump: 839-847`, `Die: 875-884`, `Hold: 885-888`. Chỉnh FPS và Loop, rồi bấm **Tạo clip**. Clip được lưu cạnh Controller, tạo lại sẽ ghi đè.
4. Bấm **Chuẩn hoá tham số & trạng thái**. Tool sẽ thêm tham số, tạo state cho clip mới và nối các transition. Chạy lại nhiều lần không bị trùng.
5. Hoặc làm nhanh cho cả 3 nhân vật bằng **Tools ▸ BeatRun ▸ Chuẩn hoá Animator cả 3 nhân vật**. Với Ramona, tool chỉ bổ sung tham số và không đổi transition đang có.

> Chọn frame nào cho Jump/Die/Hold cần người làm art xem sprite sheet để quyết định; code không tự đoán được.

---

## 2. MIDI Chart Tool (Tools ▸ BeatRun ▸ MIDI Chart Tool)

**Game đọc nhạc như sau:**
- `LevelData.midiFileName` chỉ tới file trong `StreamingAssets/MIDI`.
- `MidiReader` chọn track: ưu tiên track có tên chứa *melody / lead / main / piano*, hoặc track ép buộc qua `forcedTrackIndex`.
- `MidiReader` tiếp tục lọc theo độ khó (khoảng cách tối thiểu 0,30 / 0,22 / 0,15 / 0,10 s) rồi chia nốt vào 2 lane.
- Spawner của từng màn lọc thêm một lần nữa:
  - Man1: khoảng cách giữa các phím + đổi nốt thành item.
  - Man2/Man3: gộp các nốt gần như cùng lúc.

**Tool giúp làm:**
1. Chọn file → **Đọc**. Tool hiện danh sách track (tên, channel, số nốt, khoảng cao độ).
2. Bỏ chọn các track không cần. Lọc theo channel, cao độ, khoảng thời gian, velocity, gộp hợp âm, khoảng cách tối thiểu và số nốt tối đa mỗi giây.
3. Bấm **Áp dụng lọc**. Tool hiện biểu đồ mật độ nốt theo từng đoạn 10 giây, giúp tìm đoạn quá dày hoặc quá thưa.
4. **Xuất MIDI sạch** (giữ nguyên tempo) → trong `LevelData` đặt `MIDI File Name` = file mới, `Forced Track Index = 1`.
5. **Xuất CSV** để xem bằng Excel: thời điểm, độ dài, cao độ, velocity, track, channel.

Gợi ý mật độ: màn đầu 1,5–2 nốt/giây, màn cuối 3–4 nốt/giây.

## 3. Âm thanh item
- **Item Màn 1** (`BossProjectile`, loại item): ô **Collect Clip**, mỗi loại item một âm riêng.
- **Item Màn 3** (`FallingItem`): ô **Collect Clip**, dùng cho tiếng bass khi nhặt.
- **Nổ ở Màn 4** (`MissExplosionFeedback`): ô **Explosion Clip**.
- Mọi tiếng đều phát qua `GameplaySfx`:
  - Âm 2D.
  - Tôn trọng nút tắt Âm thanh.
  - Dừng khi Pause.
  - Không phát chồng cùng một âm trong vòng 30 ms.

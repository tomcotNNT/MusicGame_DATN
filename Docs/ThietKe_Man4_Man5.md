# 5 màn quanh vương quốc Asterisk — hiện trạng & khung Màn 4, Màn 5

## 1. Đối chiếu kịch bản với scene

| Màn | Kịch bản | Scene chơi | Story | Cơ chế hiện có | Còn thiếu |
|---|---|---|---|---|---|
| 1 | The Lost Melody – Maestro Vargo | `Man1` ✅ build | `StoryMan1` ✅ | Piano 2 lane, nốt dài (Hold), item + Fever bắn boss, boss máu = số nốt, có máu người chơi | Tên/hình boss nên đổi thành Maestro Vargo; nội dung truyện |
| 2 | The Dark Ritual – phù thủy & ma vật | `Man2` ✅ build | `StoryMan2` ✅ | Ma vật bay từ 2 phía, boss dịch chuyển, máu = số nốt | Người chơi không thể thua; nội dung truyện |
| 3 | The Abyssal Treasure – tàu ngầm & tên lửa | `Man3` ✅ build | `StoryMan3` ✅ | Tàu ngầm thả rác theo nhạc, máu = số nốt | (Đã sửa: Man3 trước đó trỏ vào LevelData không tồn tại, không có nhạc/MIDI) |
| 4 | The Mysterious Cave – quái vật một đầu phát nổ | `Man4` 🟡 khung | `StoryMan4` 🟡 khung | Bản sao Man3 + **cơ chế nổ khi bỏ lỡ** (`MissExplosionFeedback`), `Level4` riêng | Nhạc + MIDI, background hang động, sprite quái, VFX nổ, nội dung truyện |
| 5 | The Final Boss – Gigadeon (The Graves) | `Man5` 🟡 khung | `StoryMan5` 🟡 khung | Bản sao Man2 (boss dịch chuyển), sao 3 = "Defeat Gigadeon", `Level5` riêng | Nhạc + MIDI, sprite Gigadeon, background, nội dung truyện |

Màn 4, Màn 5 và StoryMan4–5 **chưa thêm vào Build Settings**, để không ảnh hưởng buổi test thứ 6. SelectSong cũng chưa có thẻ màn 4, 5.

## 2. Đề xuất gameplay Màn 4 — The Mysterious Cave

**Bối cảnh:** sau 3 nhiệm vụ đầu, bộ ba Ramona, Stephen và Knives Chau suy ra góc thứ 4 của trận pháp hình vuông mà The Graves (Gigadeon) dựng quanh kinh đô Asterisk. Cả nhóm đi vào một hang động sâu không thấy đáy.

**Luật chơi cốt lõi giữ nguyên:** chỉ có 2 vùng chạm Trái/Phải và mỗi nốt MIDI là một mục tiêu.

| Ý tưởng | Cách làm với code hiện có |
|---|---|
| Quái vật một đầu lao tới theo nhịp | Mỗi nốt là một quái bò từ trong hang ra, chạm vạch đánh đúng nhịp. Dùng spawner thả theo MIDI có sẵn |
| Đánh trúng → quái bị hạ | `TryHit` như Man3 (+ combo, − máu tổ quái) |
| Bỏ lỡ → quái **phát nổ** sát người chơi | ✅ `MissExplosionFeedback`: sinh VFX nổ, rung camera, phát tiếng nổ, trừ máu người chơi (mặc định 10) |
| "Nhịp dồn": quái lao nhanh dần ở đoạn cao trào | Dùng **MIDI Chart Tool** làm dày nốt (giảm khoảng cách tối thiểu) ở đoạn điệp khúc; không cần đổi code |
| Tổ quái / Mẫu quái là "boss" của màn | Dùng thanh máu boss sẵn có (máu = số nốt). Sao 3 = "Clear the Cave" |
| Hang tối dần khi đi sâu | Đổi màu các lớp Parallax theo tiến độ bài (việc art) |

## 3. Khung đã dựng sẵn

| Thành phần | Vị trí | Ghi chú |
|---|---|---|
| `Level4.asset`, `Level5.asset` | `MusicGame/Data` | Tạm dùng nhạc/MIDI của Màn 3 và Màn 2 → **chỉ cần đổi Music + MIDI File Name** |
| `Man4.unity` | `Scenes/TTAM` | levelId 4, dùng `Level4`, có object `MissExplosionFeedback`, sao 3 "Clear the Cave" |
| `Man5.unity` | `Scenes/TTAM` | Nhân bản Man2: levelId 5, dùng `Level5`, sao 3 "Defeat Gigadeon" |
| `StoryMan4.unity`, `StoryMan5.unity` | `Scenes/TTAM` | Nhân bản StoryMan3, tự chuyển sang Man4 / Man5 |
| Luồng | `LevelFlow` | Tự nhận màn theo tên `ManN` / `StoryManN`: Replay, Next, Home, trừ lượt, khóa màn chạy ngay, không sửa code |

## 4. Các bước để Màn 4 / 5 chạy được
1. Chép nhạc và file `.mid` vào project (`StreamingAssets/MIDI`). Trong `Level4`/`Level5` đổi **Music** và **MIDI File Name**. Lọc chart bằng MIDI Chart Tool.
2. Thay sprite boss, quái, background trong `Man4`/`Man5`. Gán **Explosion Prefab** và **Explosion Clip** cho `MissExplosionFeedback` (Man4).
3. Thay tranh truyện trong `StoryMan4`/`StoryMan5`.
4. SelectSong: nhân bản một thẻ màn, đặt `levelId = 4` (5) và `sceneName = StoryMan4` (StoryMan5), đặt giá mở khóa.
5. File ▸ Build Settings: thêm `StoryMan4`, `Man4`, `StoryMan5`, `Man5`.
6. (Đã làm sẵn) Man4, Man5, StoryMan4, StoryMan5 đã có trong danh sách tắt nhạc nền của `BackgroundMusic`.

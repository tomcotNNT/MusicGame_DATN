# Kế hoạch phát triển tiếp theo — BeatRun

Bản đề xuất để nhóm đọc và thống nhất. Mỗi hạng mục ghi rõ **vì sao**, **làm gì**, **ước lượng** và **cần quyết định gì**.
Thứ tự ưu tiên: A (cốt lõi gameplay) → B (tiến trình & kinh tế) → C (nội dung) → D (hoàn thiện & phát hành) → E (kỹ thuật).

Ước lượng tính theo buổi làm việc ~3 giờ của 1 người.

---

## A. Cốt lõi gameplay nhịp điệu

### A1. Chấm Good/Great/Perfect theo độ chính xác thời điểm bấm — **ưu tiên cao nhất**
- **Vì sao:** hiện mức đánh giá tính theo độ dài combo, nên bấm lệch nhịp vẫn được Perfect. Với game nhịp điệu, đây là điểm hội đồng dễ hỏi nhất.
- **Làm gì:** lúc nốt được đánh, lấy `|SongTime − note.time|` rồi so với cửa sổ thời gian, ví dụ Perfect ≤ 50 ms, Great ≤ 100 ms, Good ≤ 150 ms, ngoài ra là Miss. Combo chỉ còn dùng để nhân điểm (×1.0 / ×1.1 / ×1.2…).
- **Ước lượng:** 2 buổi (Man1 dùng va chạm vật lý nên phải chuyển sang so thời gian; Man2/Man3 đã có `note.time`).
- **Quyết định:** độ rộng cửa sổ thời gian và hệ số nhân theo combo.

### A2. Hiệu chỉnh độ trễ âm thanh (Audio Offset)
- **Vì sao:** mỗi điện thoại / tai nghe Bluetooth trễ khác nhau 50–250 ms, khiến người chơi thấy lệch nhịp dù code đúng.
- **Làm gì:** thêm màn "Calibrate" (gõ theo tiếng tick 8 lần, lấy độ lệch trung bình) và lưu offset vào `PlayerData.settings`. Mọi spawner cộng offset vào SongTime.
- **Ước lượng:** 1 buổi.

### A3. Thống nhất thắng/thua cho cả 3 màn
- **Hiện trạng:** Man1 có máu, Man2/Man3 không thể thua.
- **Đề xuất:** cả 3 màn dùng chung `PlayerHealth`, mỗi lần Miss trừ máu (`TakeMissDamage` đã có sẵn). Hết máu thì hiện Game Over.
- **Ước lượng:** 1 buổi (thêm `PlayerHealth` và panel Game Over vào Man2/Man3, nối `MissNote`).
- **Quyết định:** có cho Man2/Man3 thua không, và số máu mỗi màn.

### A4. Hoàn thiện nốt dài và vật phẩm
- Tách điểm "giữ nốt dài" ra khỏi popup mỗi tick (hiện mỗi tick hiện một popup, dễ rối màn hình).
- Gom Fever và vật phẩm nam châm/coin của Man1 (script đã mất, đã gỡ khỏi scene) thành một hệ thống vật phẩm dùng chung, hoặc bỏ hẳn.
- **Ước lượng:** 1–2 buổi.

---

## B. Tiến trình & kinh tế

### B1. Một hệ tiền tệ duy nhất
- **Hiện trạng:** luồng chính dùng `PlayerDataManager` (JSON). Code cũ ngoài build còn `GameManager`, `LobbyManager`, `GachaWheel`, `DailyMission` lưu PlayerPrefs riêng.
- **Làm gì:** chuyển Gacha và DailyMission sang `PlayerDataManager`, sau đó xoá hoặc lưu trữ các script cũ.
- **Ước lượng:** 1 buổi.

### B2. Luật mở khóa màn và nhân vật
- **Hiện trạng:** màn chỉ mở bằng coin; nhân vật chỉ mở được qua Admin panel.
- **Đề xuất:**
  - Màn N+1 mở khi đạt ≥ 1 sao ở màn N, **hoặc** trả coin để mở sớm.
  - Nhân vật mở bằng kim cương, gacha, hoặc mốc thành tựu.
  - Mỗi nhân vật có một kỹ năng nhỏ (thêm 1 máu, +10% coin, cửa sổ Perfect rộng hơn…) để việc chọn nhân vật có ý nghĩa.
- **Ước lượng:** 2 buổi.

### B3. Cân bằng kinh tế
- Viết bảng tính: coin/màn theo số sao, giá mở màn, số lượt (20 lượt, 5 phút hồi 1 lượt), thưởng quảng cáo 500 coin/lần × 5 lần/ngày.
- Mục tiêu: người chơi mở màn 2 sau 3–4 lượt chơi, màn 3 sau khoảng 1 ngày chơi.
- **Ước lượng:** 0,5 buổi.

---

## C. Nội dung

| Hạng mục | Mô tả | Ước lượng |
|---|---|---|
| C1. Man4 | Đã có scene (bản sao Man3). Cần bài nhạc + MIDI + LevelData riêng, StoryMan4, thêm vào SelectSong và Build Settings | 2 buổi |
| C2. Độ khó | Mỗi màn có Easy/Normal/Hard bằng cách lọc nốt MIDI (Easy bỏ nốt gần nhau, Hard giữ đủ) | 1–2 buổi |
| C3. Tutorial | Ghép lại `Tutorial.unity` với script hiện tại (đang dùng script đã mất), cho chạy lần đầu vào game | 1 buổi |
| C4. Cốt truyện | Thêm nút "Bỏ qua" và chỉ tự chiếu StoryMan ở lần đầu vào mỗi màn | 0,5 buổi |

---

## D. Hoàn thiện & phát hành

| Hạng mục | Mô tả | Ước lượng |
|---|---|---|
| D1. Bảng kết quả | Thêm hạng S/A/B/C, số Perfect/Great/Good/Miss, đánh dấu kỷ lục mới | 1 buổi |
| D2. Cài đặt | Thanh âm lượng nhạc/SFX, rung, offset (A2) — gom vào `PlayerData.settings` | 1 buổi |
| D3. Hiệu năng mobile | Object pool cho nốt/VFX/popup điểm (hiện mỗi lần Instantiate/Destroy), kiểm tra 60 FPS trên Galaxy A07 | 1–2 buổi |
| D4. Chuẩn bị build Android | Đổi package `com.DefaultCompany.urp_2d`, ID quảng cáo thật, icon, version code; tắt Admin panel; build Release để test | 1 buổi |
| D5. Thành tựu | Rà lại các điều kiện sau khi đổi cách chấm điểm (A1) | 0,5 buổi |

---

## E. Kỹ thuật & quy trình nhóm

1. **Dọn dự án:** khoảng 100 file `.unity` (có `lllll.unity`, `Man2111.unity`, các scene nháp của từng thành viên). Chuyển sang thư mục `_Archive` hoặc xoá; chỉ giữ `Scenes/TTAM`.
2. **Quy ước:** tên file trùng tên class (`Scene.cs` → `Scenes.cs`, `EndGameManager1.cs` → `EndGameManagerMiss.cs`); bỏ dấu và dấu cách trong tên thư mục (`MÀN 3`, `Scripts MRV`).
3. **Git:** gỡ `Assets/Editor/QLDATNSceneTracker.cs`, `.qldatn/tracker.json` và `MusicGame_DATN.slnx` khỏi git (`git rm --cached`), vì chúng đã nằm trong `.gitignore`. Mỗi người làm trên nhánh riêng, scene chỉ một người sửa tại một thời điểm.
4. **Test:** dùng Unity Test Framework cho phần logic thuần (LevelFlow, PlayerDataManager lượt chơi/hồi lượt, tính sao), để sửa không làm hỏng chỗ khác.

---

## Đề xuất thứ tự 2 tuần tới

| Tuần | Việc |
|---|---|
| Tuần 1 | Test checklist bản sửa hiện tại → A1 chấm theo timing → A3 thắng/thua thống nhất → B2 luật mở khóa → cân lại ngưỡng sao |
| Tuần 2 | A2 offset → B1 một hệ tiền tệ → D1 bảng kết quả → D3 hiệu năng → D4 build Android thử |

## Cần nhóm quyết định trước khi bắt đầu
1. Cửa sổ chấm Perfect/Great/Good (A1).
2. Man2/Man3 có thể thua không, và mỗi màn bao nhiêu máu (A3).
3. Mở màn bằng sao, bằng coin, hay cả hai (B2).
4. Nhân vật có kỹ năng riêng không (B2).
5. Man4 dùng bài nhạc nào, có kịp đưa vào bản bảo vệ không (C1).

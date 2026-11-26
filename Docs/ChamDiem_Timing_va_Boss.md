# Chấm điểm nhịp điệu & đồng bộ Boss/Fever giữa Man1 – Man3

Cập nhật: 25/11/2026 · Nhánh `Feature/Ntam` · Code đã biên dịch, **chưa test Play mode, chưa commit**

---

## Phần 1 — Hệ thống chấm điểm hiện tại

### 1.1 Mỗi màn nhận "trúng nốt" như thế nào

| | Man1 (piano) | Man2 (boss) | Man3 (tàu ngầm) |
|---|---|---|---|
| Cách nhận trúng | Nốt chồng lên vạch đánh (va chạm vật lý `HitPoint`) + bấm đúng lane | `BossSpawner2.TryHit`: chọn enemy có độ lệch nhỏ nhất, phải nằm trong `goodWindow` 0,20 s | `SubmarineSpawner.TryHit`: chọn item có độ lệch nhỏ nhất, phải nằm trong `hitWindow` |
| Có biết độ lệch thời gian không | Có: `targetTime` của nốt so với `SongTime` | Có: `EnemyRhythm.TimingError` (đã có sẵn cửa sổ 0,08 / 0,14 / 0,20 s) | Có: `FallingItem.TimingError()` |
| Trước khi sửa, mức Good/Great/Perfect lấy từ | Độ dài combo | Độ dài combo (mức chấm theo timing được tính ra rồi **bị bỏ đi**) | Độ dài combo |
| Luật combo (`ComboSystem`) | combo 1–4 = Good (100), 5–9 = Great (200), từ 10 = Perfect (300). Giống nhau ở cả 3 màn |||

Hệ quả của cách chấm theo combo: bấm lệch nhịp vẫn được Perfect nếu đang có chuỗi dài, còn bấm chuẩn ngay đầu bài vẫn chỉ được Good. Với game nhịp điệu, đây là điểm hội đồng dễ hỏi nhất.

### 1.2 Lỗi tìm thấy trong lần rà soát này (đã sửa)

| # | Lỗi | Màn | Hậu quả | Đã sửa |
|---|---|---|---|---|
| 1 | **Mỗi lần trúng bị báo 2 lần:** `EnemyRhythm.RegisterHit` gọi `HitNote`, rồi `BossSpawner2.TryHit` lại gọi `HitNote` | Man2 | Combo tăng 2 mỗi nốt, điểm nhân đôi, Perfect đến sớm gấp đôi | Spawner là nơi duy nhất báo trúng (`EnemyRhythm.ReportToCombo = false`) |
| 2 | **Mỗi lần trượt bị báo 2 lần:** `EnemyRhythm` tự báo Miss khi quá `goodWindow` 0,2 s, spawner cũng báo Miss khi quá `missWindow` 0,2 s | Man2 | Số Miss nhân đôi | Như trên |
| 3 | **Bấm trúng được khi đang Pause / Game Over:** nốt đứng yên ngay vạch đánh, bấm vẫn được tính | Cả 3 | Cộng điểm và trừ máu boss lúc đang dừng | `HoldInput.WasPressed` (Man1) và `TryHit` (Man2/Man3) bỏ qua input khi `timeScale = 0` hoặc bài đang pause |

> Lỗi Man1 cộng điểm 2 lần mỗi nốt đã sửa ở lần trước. Như vậy **mọi ngưỡng điểm cũ (Man1 = 10000, Man2 = 5000) đều được đặt khi điểm còn bị nhân đôi** và cần chỉnh lại (xem 1.5).

### 1.3 Đã chuẩn bị sẵn để chuyển sang chấm theo timing

`ComboSystem` giờ có ô **Judge Mode**:

| Chế độ | Cách chấm | Mặc định |
|---|---|---|
| `Combo` | Như cũ: mức theo độ dài combo | ✅ đang dùng, cân bằng game chưa đổi |
| `Timing` | Lệch ≤ `perfectWindow` (0,06 s) → Perfect; ≤ `greatWindow` (0,12 s) → Great; còn lại (vẫn trong cửa sổ nhận trúng của màn) → Good. Item và đạn nhặt (không có thông tin thời gian) tính `untimedGrade` = Great | |

Cả 3 màn đã truyền độ lệch thật vào `ComboSystem.HitNoteTimed(độ lệch)`:
- Man1: nốt thường và nốt dài.
- Man2: độ lệch của enemy được đánh.
- Man3: độ lệch của item.

**Để bật:** trong từng scene Man1/Man2/Man3, chọn object `ComboSystem` → đổi `Judge Mode` sang `Timing`. Không cần sửa code.

### 1.4 Đề xuất thông số khi bật Timing

| Thông số | Man1 | Man2 | Man3 | Lý do |
|---|---|---|---|---|
| perfectWindow | 0,06 s | 0,06 s | 0,06 s | Mức phổ biến của game nhịp điệu trên mobile |
| greatWindow | 0,12 s | 0,12 s | 0,12 s | |
| Cửa sổ nhận trúng (Good) | Phụ thuộc kích thước collider `HitPoint` và `keySpeed` 5,7 | `goodWindow` 0,20 s | `hitWindow` của spawner | **Nên đặt giống nhau ở cả 3 màn (~0,18 s)** để cảm giác chơi đồng nhất |

Việc nên làm kèm theo: thêm hiệu chỉnh độ trễ âm thanh (audio offset). Tai nghe Bluetooth trễ 100–250 ms, nếu không bù thì người chơi sẽ chỉ được Good.

### 1.5 Ảnh hưởng tới Score, Combo, mở khóa và `scoreForStar`

| Hạng mục | Chế độ Combo (hiện tại) | Chế độ Timing |
|---|---|---|
| **Score** | Tối đa ≈ N × 300 − 1.300 (9 nốt đầu bị chấm thấp). Một lần Miss mất thêm ~1.300 điểm vì combo phải leo lại từ Good | Tối đa = N × 300. Điểm phản ánh độ chính xác; người chơi khá đạt trung bình ~220–250 điểm mỗi nốt |
| **Combo / MaxCombo** | Không đổi: combo đếm chuỗi trúng liên tiếp, Miss về 0 | Không đổi. Combo chỉ còn dùng cho sao 2 và hiệu ứng |
| **Số Miss** | Không đổi (đã sửa lỗi đếm đôi ở Man2) | Không đổi |
| **Thống kê Perfect/Great/Good (JSON, thành tựu, nhiệm vụ "100 Perfect")** | Perfect chiếm đa số một cách giả tạo | Có ý nghĩa thật. Thành tựu và nhiệm vụ dựa trên số Perfect sẽ **khó hơn**: nên hạ nhiệm vụ ngày từ 100 xuống ~60 |
| **Mở khóa màn** | Mở bằng coin; coin = 200/500/1000 theo số sao | Coin phụ thuộc số sao → phụ thuộc trực tiếp `scoreForStar`. Ngưỡng điểm sai thì nhịp mở khóa sai theo |

**Cách tính lại `scoreForStar`** (N = số nốt đánh được của màn):
- N đã có trong log khi chơi:
  - Man1: dòng `PianoKeySpawner: Boss xuất hiện, máu = N` (`bossHpRatio = 1`, nên máu boss = N).
  - Man2/Man3: máu boss lúc bắt đầu (`InitHp`) với `bossHpRatio = 1`, cũng = N.
- Công thức gợi ý:
  - Chế độ Combo: `scoreForStar ≈ N × 300 × 0,65`
  - Chế độ Timing: `scoreForStar ≈ N × 300 × 0,55`
- Tham khảo số nốt thô trong file MIDI (trước khi spawner lọc nốt sát nhau): Man1 2.418 nốt / 180 s; Man2 1.286 nốt / 104 s; Man3 2.559 nốt / 136 s. Số nốt thật sự xuất hiện ít hơn nhiều (Man1 lọc khoảng cách tối thiểu ~0,35 s và đổi mỗi nốt thứ 8 thành item), nên **phải lấy N từ log**, không dùng số thô.
- Sao 2 (combo): bảng kế hoạch ghi 100, scene đang đặt 20/30/20. Gợi ý **~25% N**, tối đa 100.

---

## Phần 2 — Rà soát Boss / Fever / Pause / Game Over giữa 3 màn

### 2.1 Hiện trạng sau khi thống nhất

| Sự kiện | Man1 (`BossHealth` + `Boss`) | Man2 (`BossController2`) | Man3 (`SubmarineController`) |
|---|---|---|---|
| Boss xuất hiện | Ngay đầu bài (`bossFromStart`) | Có sẵn trong scene | Có sẵn trong scene |
| Máu tối đa | Số nốt đánh được × `bossHpRatio` (1) | `CountDroppableNotes` × `bossHpRatio` (1) | Số nốt × `bossHpRatio` (1) |
| Trúng nốt | `PianoKeySpawner.OnPlayableNoteHit` → −1 máu | `boss.TakeHit()` → −1 | `submarine.TakeHit()` → −1 |
| Fever | Đầy thanh item → đạn `PlayerBullet` → `BossHealth.TakeDamage` (cộng thêm sát thương, giúp hạ boss sớm hơn) | Không có | Không có |
| Trượt nốt | Mất combo + tính Miss. Boss không hồi máu | Như Man1 | Như Man1 |
| Boss bị hạ | `BossHealth.Died` → `Boss.Defeated` → `BossSpawner` → `PianoKeySpawner.BossDefeated = true`; đồng thời phát sự kiện chung `BossHealth.AnyBossDefeated` | `BossController2.AnyBossDefeated` | `SubmarineController.AnyBossDefeated` |
| Sao 3 | `EndGameManagerMiss` đọc `spawner.BossDefeated` | `EndGameManager` nghe `AnyBossDefeated` | `EndGameManMan3` nghe `AnyBossDefeated` |
| Thanh máu | Tự tạo (`RuntimeBossHpBar`); khi boss chết: chạy về 0 → mờ dần → tự huỷ. Boss rút lui lúc hết bài: huỷ cùng boss | `hpFill` trong scene | `hpFill` trong scene |
| Sau khi boss chết | Boss ngừng bắn (`BossMusicAttack` bị tắt), nốt vẫn chạy, đánh tiếp chỉ cộng điểm | Như Man1 | Như Man1 |
| Pause | Đồng hồ bài hát đóng băng (`SongClock`); đạn boss dựa vào `SongTime` nên đứng yên; **không bấm trúng được** | Như Man1 | Như Man1 |
| Game Over | Chỉ Man1 có máu. Dừng `timeScale` + đóng băng bài; bảng kết thúc và thưởng không hiện; không bấm trúng được | Không thể thua | Không thể thua |
| Chuyển scene / Retry | Sự kiện tĩnh được huỷ đăng ký trong `OnDestroy`/`OnDisable`; thanh máu tự tạo nằm trong scene nên tự mất | Như Man1 | Như Man1 |

### 2.2 Những điểm có thể làm lệch trạng thái (đã xử lý)

| Rủi ro | Đã xử lý |
|---|---|
| Đạn Fever và nốt trừ vào 2 thanh máu khác nhau (`Boss.health` và `BossHealth`) | `Boss` chuyển hết sát thương sang `BossHealth` nếu có. Chỉ còn một thanh máu |
| `PlayerController` (thắng màn) giữ tham chiếu boss trong scene, mà boss Man1 được spawn lúc chạy nên tham chiếu trống | Nghe thêm sự kiện chung `BossHealth.AnyBossDefeated` |
| Boss chết nhưng thanh máu đứng chờ boss lùi xong | Thanh máu tự mờ và huỷ ngay khi boss chết |
| `Boss.TakeDamage` bỏ qua sát thương khi boss chưa đi tới vị trí (nốt đầu bài không tính) | Sát thương được nhận ngay từ khi boss xuất hiện |
| Man2 combo/điểm/Miss nhân đôi | Đã sửa (1.2) |
| Bấm trúng khi Pause/Game Over | Đã sửa (1.2) |

### 2.3 Còn lại cần nhóm quyết định

1. **Man2/Man3 có thể thua không?** Hiện chỉ Man1 có máu. Nếu muốn đồng bộ: thêm `PlayerHealth` vào nhân vật Man2/Man3 và gọi `TakeMissDamage()` trong `ComboSystem.MissNote()`.
2. **Fever cho Man2/Man3:** Man1 có Fever bắn boss, hai màn kia thì không. Có thể giữ làm điểm riêng của Man1.
3. **Boss Man1 bắn từ đầu bài:** khó hơn trước. Nếu thấy quá khó, giảm sát thương `BossMusicAttack` hoặc tắt `bossFromStart`.

---

## Phần 3 — Lộ trình đề xuất

| Bước | Việc | Ước lượng |
|---|---|---|
| 1 | Test bản hiện tại (chế độ Combo), ghi N của 3 màn từ log, chỉnh `scoreForStar` và `comboForStar` theo công thức ở 1.5 | 0,5 buổi |
| 2 | Bật `Judge Mode = Timing` ở Man1, chơi thử, chỉnh cửa sổ; rồi làm tương tự cho Man2/Man3 | 1 buổi |
| 3 | Thống nhất cửa sổ nhận trúng ~0,18 s ở cả 3 màn | 0,5 buổi |
| 4 | Thêm màn hiệu chỉnh audio offset | 1 buổi |
| 5 | Chỉnh lại các mốc thành tựu / nhiệm vụ dựa trên số Perfect | 0,5 buổi |

## Checklist test nhanh
- [ ] Man2: mỗi enemy đánh trúng chỉ +1 combo; popup điểm chỉ hiện 1 lần.
- [ ] Man2: để trượt 1 enemy → số Miss ở bảng kết thúc tăng 1 (không phải 2).
- [ ] Cả 3 màn: Pause đúng lúc nốt nằm trên vạch → bấm nút → không được tính điểm; tiếp tục thì vẫn đánh được.
- [ ] Man1: Game Over → bấm nút không cộng điểm, thanh máu boss đứng yên.
- [ ] Man1: hạ boss → thanh máu về 0, mờ dần rồi biến mất; sao 3 "Defeat Boss" sáng.
- [ ] Man1: dùng Fever bắn boss → cùng một thanh máu giảm.
- [ ] (Khi thử Timing) Bấm sớm/muộn rõ rệt → hiện Good; bấm đúng nhịp → hiện Perfect.

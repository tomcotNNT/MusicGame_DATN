# BeatRun: Rà soát báo cáo & Phần 2 (Tài liệu kỹ thuật)

Đối chiếu file `_Xây dụng Game Chạy cùng nốt nhạc_BeatRun (2).pdf` với code nhánh `Feature/Ntam` (26/11/2026).

- **Phần A** liệt kê chỗ báo cáo viết khác với game thật, kèm câu sửa gợi ý.
- **Phần B** là nội dung Phần 2, mục 1, 2, 3, viết theo mẫu `2. Mau bao cao.pdf`. Có thể chép thẳng vào báo cáo.

---

## PHẦN A. SAI SÓT TRONG BÁO CÁO SO VỚI CODE

Ký hiệu: 🔴 sai với game (giảng viên chạy thử sẽ thấy ngay) · 🟡 mâu thuẫn hoặc thiếu · ⚪ lỗi trình bày.

### A1. Cơ chế chấm điểm

| # | Trang / mục | Báo cáo viết | Code thực tế | Sửa thành |
|---|---|---|---|---|
| 1 | 🔴 tr.8 (1.2), tr.26 (7.3), tr.31 (3.3), tr.37 (2) | 4 mức **Perfect, Good, Bad, Miss** | Không có mức **Bad**. `ScoreSystem.ScoreGrade` chỉ có `Good, Great, Perfect`; trượt là `Miss`. | "4 mức **Good, Great, Perfect, Miss**" |
| 2 | 🔴 tr.15 (Bảng 4.1) | Nốt 1–5 GOOD, 6–10 GREAT, từ 11 PERFECT | Nhãn được tính theo combo **sau khi cộng** lần trúng (`greatCombo = 5`, `perfectCombo = 10`): combo 1–4 GOOD, combo 5–9 GREAT, từ combo 10 PERFECT. | Nốt 1–4 GOOD (+100), 5–9 GREAT (+200), từ 10 PERFECT (+300), Miss → combo 0 |
| 3 | 🟡 tr.14 (4.1) và tr.13 (3.2), tr.14 (3.3), tr.28 (8.2) | 4.1 viết "không chấm theo độ lệch thời gian", nhưng 3.2/3.3/8.2 lại viết "timing window thu hẹp dần" | Mặc định chấm theo combo, cả 5 màn đều để `judgeMode = Combo`. Code có sẵn chế độ **Timing** (Perfect ≤ 0,06 s, Great ≤ 0,12 s) nhưng chưa bật. | Giữ cách viết ở 4.1. Ở 3.2/3.3/8.2 sửa thành "độ khó tăng qua tốc độ nốt và mật độ nốt; chế độ chấm theo độ lệch thời gian đã được cài đặt sẵn để bật khi cân bằng" |
| 4 | 🔴 tr.8, tr.28, tr.38 | "Miss làm mất Combo **và giảm Score**" | `MissNote()` chỉ đưa combo về 0 và tăng `MissCount`. Không trừ điểm; Miss chỉ là mất phần điểm của nốt đó. | "Miss đưa Combo về 0, không bị trừ điểm (chỉ không được cộng điểm nốt đó)" |
| 5 | 🔴 tr.14–15 (4.1) | "Game chỉ có nốt đơn… không có Hold Note… **không có vật phẩm nào**" | Màn 1 có **nốt giữ** (`LongNote.prefab`, sinh khi nốt MIDI dài ≥ 0,5 s) và **5 loại vật phẩm** (`Item1–5`, `RhythmItemm`) nạp thanh Fever. Màn 3 có vật phẩm rơi (`FallingItem`). Mục 4.1 mâu thuẫn với 1.2, 2.4, 4.4 của chính báo cáo. | Bỏ câu này. Viết lại: "Màn 1 có nốt chạm, nốt giữ và vật phẩm; Màn 2–3 là nốt chạm theo hướng trái/phải; Màn 3 thêm vật phẩm rơi" |
| 6 | 🟡 tr.8, tr.16, tr.31, tr.37 | "nốt đôi", "nốt đại dương" | Không có loại nốt đôi riêng: Màn 2/3 **gộp** các nốt cách nhau ≤ 0,03 s thành một. "Nốt đại dương" chỉ là hình vỏ sò ở Màn 3, cách chấm giống nốt thường (chính tr.15 đã viết vậy). | Bỏ "nốt đôi". Đổi "nốt đại dương" → "nốt mang hình vỏ sò ở Màn 3" |

### A2. Kinh tế, nhân vật, mở màn

| # | Trang / mục | Báo cáo viết | Code thực tế | Sửa thành |
|---|---|---|---|---|
| 7 | 🔴 tr.8, tr.26, tr.38, tr.32 (4.2) | "Mở màn tiếp theo khi **đạt ngưỡng Score tối thiểu**" | Màn được mở bằng **Coin** ở SelectSong (`LevelSelectionManager.unlockCost`): Màn 1 miễn phí, Màn 2 = 2.500, Màn 3 = 5.000. Score chỉ quyết định **số sao → số Coin thưởng**. | "Hoàn thành màn nhận 0–3 sao; mỗi sao quy ra Coin (200 / 500 / 1.000). Dùng Coin để mở màn kế tiếp (Màn 2: 2.500, Màn 3: 5.000)" |
| 8 | 🔴 tr.4, tr.13, tr.16 (4.2), tr.20, tr.27, tr.36–37 | "Quay **Gacha** bằng Coin để mở **nhân vật**; mở trực tiếp bằng **Diamond**" | Nhân vật mở bằng **Coin** ở SelectChar (`ListCharController.unlockCoinCost`): Ramona mở sẵn, Stephen 1.500, Knives Chau 2.500. `GachaWheel` (thư mục LONG) quay bằng **vé/Diamond** và trả Coin/Diamond/lượt/vé, **không trả nhân vật**, và **không nằm trong scene nào của bản build**. Diamond hiện chỉ được cộng (gói nạp), chưa có chỗ tiêu. | "Nhân vật mở bằng Coin (Stephen 1.500, Knives Chau 2.500)". Gacha chuyển vào "hướng phát triển", hoặc ghi rõ là tính năng thử nghiệm chưa đưa vào bản build |
| 9 | 🟡 tr.13, tr.16 | Coin "nạp thanh năng lượng hỗ trợ" | Không có thanh năng lượng mua bằng Coin. Thực tế có **Lượt chơi**: tối đa 20 lượt, mỗi lần chơi tốn 1 lượt, hồi 1 lượt mỗi 5 phút, xem quảng cáo được +1 lượt. | "Lượt chơi (tối đa 20, hồi 1 lượt/5 phút, xem quảng cáo +1)" |
| 10 | 🟡 cả báo cáo | Chưa nhắc tới | Game **có**: nhiệm vụ ngày (3 nhiệm vụ, mỗi cái 500 Coin), thành tựu (Archivement), quảng cáo thưởng AdMob (+500 Coin), bảng nạp (Purchase Panel), cài đặt âm thanh. | Thêm vào 4.2 (GDD) và Phần 2 |
| 11 | 🟡 tr.4, tr.10, tr.12, tr.16 | Mỗi nhân vật có **VFX combo riêng** và **font chữ riêng** | Không tìm thấy code gán font hay VFX theo nhân vật. Ba nhân vật khác nhau ở sprite, Animator, nút điều khiển và ảnh đại diện; nhãn combo/điểm dùng chung. | "Mỗi nhân vật có bộ sprite và animation riêng (đứng, chạy, 3 kiểu đánh, nhảy, giữ nốt, ngã)" |
| 12 | 🟡 tr.4, tr.9, tr.10, tr.17, tr.21, tr.35, tr.37 | Hiệu ứng **screen flash / visual bloom** khi combo cao hoặc giữ Hold | Không có script flash/bloom nào. Có: Global Light 2D (Màn 1, 2, 3, 5), Volume URP, rung camera (`CameraShake`, Màn 2–5), chữ COMBO gợn sóng và đổi màu ở mốc 5/15/25, VFX khi trúng (`HitVFX`). | Thay bằng "chữ COMBO gợn sóng, đổi màu theo mốc 5/15/25 combo, hiệu ứng trúng nốt và rung camera" (hoặc làm thêm flash trước khi nộp) |

### A3. Màn chơi, kẻ thù, thua cuộc

| # | Trang / mục | Báo cáo viết | Code thực tế | Sửa thành |
|---|---|---|---|---|
| 13 | 🔴 tr.4, tr.7, tr.12, tr.26, tr.36–38 | **5 màn chính + Tutorial** chơi được | Bản build (Build Settings) có 10 scene: LoadingScene, MainMenu, SelectSong, SelectChar, StoryMan1–3, Man1–3. **Tutorial, Man4, Man5, StoryMan4/5 có file nhưng chưa đưa vào build**. SelectSong chỉ liệt kê 3 màn. Man4 đang dùng tạm nhạc Màn 3, Man5 dùng tạm nhạc Màn 2. | "Bản hiện tại chơi được Màn 1–3; Màn 4, Màn 5 và Tutorial đang hoàn thiện" (khớp bảng 8.4: 2.4, 2.5, 5.5, 5.6 "Đang thực hiện") |
| 14 | 🟡 tr.8 (1.2), tr.29 | "Màn 2 và Màn 3: nhân vật **đứng giữa**, đối tượng lao tới từ hai phía" | Đúng. Nhưng **Màn 1** không phải "nốt đi theo 2 làn trên/dưới": nhân vật **chạy** và lướt lên/xuống giữa 2 làn để chạm nốt. Nút trái = làn dưới, nút phải = làn trên. | Mô tả rõ: Màn 1 chạy ngang 2 làn; Màn 2–3 đứng giữa, chạm trái/phải theo hướng kẻ thù tới |
| 15 | 🟡 tr.12, tr.14 | Màn 4 "quái vật phát nổ" như thử thách riêng | Có `MissExplosionFeedback`: khi Miss thì tạo vụ nổ, rung camera và phát âm thanh. Man4 hiện **không có PlayerHealth**, nên vụ nổ chưa gây sát thương. | Giữ ý tưởng, ghi "đang hoàn thiện" |
| 16 | 🟡 tr.38 | "Không có Game Over" | Đúng trong bản hiện tại. Code có `PlayerHealth` (50 máu, Die → bảng Game Over) ở Man1, nhưng **chưa có gì gây sát thương**: `TakeMissDamage` không được gọi, đạn boss đã tắt, `loseIfBossAliveAtEnd = false`. | Có thể giữ, thêm "hệ thống máu đã cài sẵn để bật cho chế độ khó" |
| 17a | 🟡 tr.7, tr.17 (5.2), tr.27 (8.1) | "Màn hình ngang" | Player Settings đang để Auto Rotation và cho phép cả hướng dọc. Xoay máy là game xoay theo. | Sửa Player Settings trước khi build: chỉ cho phép hướng ngang (lỗi game, không phải lỗi báo cáo) |
| 17 | 🟡 tr.17 (4.4) | Kẻ thù từng màn | Thiếu cơ chế Boss: Boss có thanh máu (`BossHealth`). **Mỗi nốt trúng trừ máu Boss**, máu = số nốt chơi được trong bài. Màn 1 thêm thanh Fever: nhặt vật phẩm → nhân vật bắn đạn vào Boss. | Thêm câu này vào 4.4 |

### A4. Trình bày

| # | Vị trí | Lỗi |
|---|---|---|
| 18 | ⚪ tr.7 | "Game Design DocumeGiới nt" → "Game Design Document" |
| 19 | ⚪ tr.20 | "Maestri Vargo" → "Maestro Vargo"; "nhà thở cổ" → "nhà thờ cổ" |
| 20 | ⚪ tr.21 | Bảng 6.4 bỏ trống Màn 3–5 |
| 21 | ⚪ tr.22 | Bảng 7.1 là ảnh chụp, bên dưới còn một bảng trống 6 dòng |
| 22 | ⚪ tr.19 | Dòng đỏ "*** Thiếu hình ảnh" còn sót |
| 23 | ⚪ tr.4, 9, 13… | "Knive Chau" nhưng asset tên "Knives Chau" (`Knives Chau.png`), cần thống nhất một cách viết |
| 24 | ⚪ tr.2–3 | Mục lục: mục 3.3 (Sự tiến triển câu chuyện) của mẫu bị thiếu; mục 8 đổi tên khác mẫu; 8.5, 8.6 của mẫu chưa có |
| 25 | ⚪ tr.34 (6.5) | "GDD (200 trang)" không đúng thực tế |

---

## PHẦN B. NỘI DUNG PHẦN 2: TÀI LIỆU KỸ THUẬT (mục 1, 2, 3)

> Phần dưới viết lại theo đúng code. Số liệu lấy trực tiếp từ script và scene. Đoạn SWOT cũ (tr.34–35) đã được giữ ý và sửa những chỗ sai.

## 1. Phân tích ý tưởng game

BeatRun kết hợp game nhịp điệu (rhythm) với hành động 2D trên màn hình ngang điện thoại. Toàn bộ nốt nhạc sinh ra từ file MIDI của bài hát. Người chơi điều khiển bằng hai nút cảm ứng Trái/Phải. Mỗi màn có một cách chơi riêng: chạy hai làn ở Màn 1, đánh kẻ thù hai phía ở Màn 2, chặn đạn và nhặt vật phẩm dưới biển ở Màn 3.

**Bảng 1.1. Phân tích SWOT**

| | Nội dung |
|---|---|
| **Điểm mạnh (S)** | • Nốt sinh tự động từ MIDI (`MidiReader` tự viết, không dùng thư viện ngoài). Thêm bài mới chỉ cần file nhạc, file MIDI và một `LevelData`.<br>• Nhạc chạy theo đồng hồ âm thanh `AudioSettings.dspTime`, nên nốt luôn khớp nhạc kể cả khi FPS dao động; khi tạm dừng, đồng hồ bài hát cũng dừng.<br>• Hai nút điều khiển dùng chung cho mọi màn nhưng mỗi màn một lối chơi, dễ học mà không nhàm.<br>• Không thua giữa bài: Miss chỉ mất combo, người chơi luôn nghe hết bài.<br>• Đồ họa 2D Pixel Art, URP 2D với Global Light 2D; 3 nhân vật có đủ bộ animation.<br>• Có công cụ Editor riêng: MIDI Chart Tool (lọc và xuất MIDI sạch), Animator nhân vật (tạo clip từ sprite sheet). |
| **Điểm yếu (W)** | • Nhóm 5 người, thời gian một học kỳ. Mỗi màn do một thành viên làm trên một nhánh Git riêng, nên khi hợp nhất hay bị xung đột scene.<br>• Màn 4, Màn 5 và Tutorial chưa hoàn chỉnh. Màn 4/5 đang dùng tạm nhạc của Màn 3/2.<br>• Chấm điểm đang theo độ dài combo, chưa theo độ lệch thời gian (đã có sẵn chế độ Timing nhưng chưa cân bằng).<br>• Chưa có máy chủ: dữ liệu chỉ lưu trên máy, chưa có bảng xếp hạng. |
| **Cơ hội (O)** | • Game nhịp điệu casual trên điện thoại có lượng người chơi ổn định; nhịp chơi ngắn (một bài 2–3 phút) phù hợp chơi lúc rảnh.<br>• Unity 6 xuất được Android trực tiếp; Google AdMob cho phép kiếm tiền bằng quảng cáo thưởng ngay từ bản đầu.<br>• Dữ liệu nhạc tách riêng (MIDI + `LevelData`) nên dễ mở rộng thêm bài và màn sau khi phát hành. |
| **Thách thức (T)** | • Độ trễ âm thanh và cảm ứng khác nhau giữa các dòng Android, có thể làm người chơi thấy lệch nhịp. Cần hiệu chỉnh `timeOffset` theo thiết bị.<br>• Tỷ lệ màn hình đa dạng (16:9 đến 20:9), nút điều khiển có thể bị lệch hoặc khuất.<br>• Cạnh tranh với các game nhịp điệu lớn đã có thương hiệu.<br>• Bản quyền nhạc: cần dùng nhạc có giấy phép trước khi phát hành công khai. |

## 2. Thiết kế game

### 2.1. Mô hình tổng thể

**a) Mô hình Scene (đúng theo Build Settings)**

| Index | Scene | Vai trò |
|---|---|---|
| 0 | `LoadingScene` | Màn tải đầu; khởi tạo `PlayerDataManager` (đọc file lưu), phát nhạc nền |
| 9 | `MainMenu` | Màn tiêu đề, bấm vào SelectSong |
| 4 | `SelectSong` | Sảnh chính: chọn màn và mở khóa màn bằng Coin, lượt chơi, Nhiệm vụ ngày, Thành tựu, Cài đặt âm thanh, xem quảng cáo, bảng nạp |
| 8 | `SelectChar` | Chọn nhân vật; mở khóa nhân vật bằng Coin; bấm Play sẽ trừ 1 lượt |
| 5–7 | `StoryMan1–3` | Truyện tranh dẫn chuyện trước mỗi màn |
| 1–3 | `Man1`, `Man2`, `Man3` | Màn chơi chính |
| — | `Tutorial`, `Man4`, `Man5`, `StoryMan4–5` | Đã có file, đang hoàn thiện, chưa đưa vào build |

Luồng: `LoadingScene → MainMenu → SelectSong → SelectChar → StoryManN → ManN → bảng kết quả → (Chơi lại | Màn kế | Về SelectSong)`.

- Mọi lần chuyển scene đều qua `SceneFader` (mờ dần, có chống kẹt 2 giây).
- Các nút cuối màn gọi chung `LevelFlow` (`Retry`, `NextLevel`, `BackToSelect`). Tên scene theo quy ước `Man{N}` / `StoryMan{N}`, nên thêm màn mới không phải sửa code điều hướng.

**b) Vòng lặp cốt lõi (Core Game Loop)**

1. Chọn màn (SelectSong) → chọn nhân vật (SelectChar), tốn 1 lượt chơi.
2. Xem truyện dẫn (StoryMan) → vào màn. Đếm ngược READY/GO rồi nhạc bắt đầu.
3. Nốt sinh theo MIDI và di chuyển về vùng chạm. Người chơi bấm hoặc giữ nút Trái/Phải đúng lúc.
4. Mỗi lần trúng: cộng Combo, cộng Score và trừ máu Boss. Trượt: Combo về 0.
5. Hết bài: tính sao → Coin thưởng, lưu kỷ lục và tiến độ nhiệm vụ.
6. Dùng Coin mở màn và nhân vật mới, rồi chơi tiếp hoặc chơi lại để cải thiện điểm.

**c) Trạng thái trong một màn chơi**

| Trạng thái | Mô tả | Chuyển sang |
|---|---|---|
| Countdown | READY → GO (`RhythmCountdown`); nhạc được hẹn giờ bằng `PlayScheduled` | Playing |
| Playing | Sinh nốt, nhận input, chấm điểm | Paused, Ended |
| Paused | `timeScale = 0`; nhạc, SFX và đồng hồ bài hát dừng (`SongPause`, `SongClock`); khóa input | Playing (Resume), Home, Retry |
| Ended | Bài kết thúc; Màn 1 chờ Boss bị hạ | Result |
| Result | Bảng kết quả (`EndGameBase`): điểm, 3 điều kiện sao, Coin | Retry / Next / Home |
| GameOver | Có sẵn (`PlayerHealth`: Die → bảng Game Over) nhưng chưa có nguồn gây sát thương trong bản hiện tại | Retry / Home |

Khi app bị đưa xuống nền (`OnApplicationPause`), game tự vào trạng thái Paused.

### 2.2. Thể loại game

2D, màn hình ngang, casual, **rhythm-action**: game nhịp điệu kết hợp hành động (chạy, đánh, chặn), một người chơi, nền tảng Android.

### 2.3. Tài nguyên đồ họa

| Nhóm | Nội dung | Thông số kỹ thuật |
|---|---|---|
| Nhân vật | Ramona, Stephen Stills, Knives Chau. Mỗi nhân vật có Idle, Run, Attack1–3, Jump, Hold, Die | Sprite sheet Multiple, PPU 32, Filter Point, không nén. Hai Animator mỗi nhân vật: bản chạy (Man1) và bản đứng (Man2/3). Cùng tham số `IsGround`, `Sing`, `Attack1–3`, `Jump`, `Die` |
| Kẻ thù / Boss | Màn 1: Boss Maestro Vargo + quái chắn đường; Màn 2: ma vật hai phía + Boss phù thủy; Màn 3: tàu ngầm bắn đạn | Boss có thanh máu `BossHealth` (tự tạo nếu scene chưa có) |
| Nốt & vật phẩm | Nốt chạm, nốt giữ (`LongNote`), 5 vật phẩm Màn 1 (`Item1–5`), vật phẩm rơi Màn 3 (`FallingItem`); hình nốt đổi theo màn (bí ngô, vỏ sò…) | Prefab trong `MusicGame/Prefabs` |
| Nền | Màn 1 thành phố Melodia, Màn 2 nghĩa trang/nhà thờ, Màn 3 đáy biển, Màn 4 hang động, Màn 5 sào huyệt | Parallax nhiều lớp (`Parallax`, `LaneScroller`) |
| Hiệu ứng | VFX trúng nốt (`HitVFX`), VFX nhặt vật phẩm, rung camera (`CameraShake`), Global Light 2D, chữ COMBO gợn sóng và đổi màu ở mốc 5/15/25 | URP 2D Renderer, Volume |
| UI | Font pixel TextMeshPro (`PublicPixel SDF`, `Pixel SDF`), ảnh nhãn GOOD/GREAT/PERFECT/MISS | Canvas Screen Space |

### 2.4. Thiết kế UI/UX

**Màn hình điều hướng**
- **MainMenu:** màn tiêu đề, dẫn vào SelectSong.
- **SelectSong (sảnh chính):** danh sách màn, trạng thái khóa/mở, giá mở khóa; số lượt chơi kèm thời gian hồi lượt; Coin/Diamond; các bảng Settings (bật/tắt nhạc nền và âm thanh, lưu trong `PlayerPrefs`), Daily Mission (3 nhiệm vụ, nút Nhận), Achievement, xem quảng cáo nhận +500 Coin, bảng nạp.
- **SelectChar:** xem từng nhân vật (ảnh + animation). Nhân vật đang khóa hiện "UNLOCK x COIN" trên nút Play (vàng khi đủ tiền, đỏ khi thiếu); thiếu tiền thì mở bảng nạp.

**HUD trong màn chơi**
- Trên trái: ảnh và tên nhân vật. Trên phải: SCORE và nút PAUSE. Gần nhân vật: số COMBO (x100) và nhãn GOOD/GREAT/PERFECT/MISS bay lên kèm "+điểm".
- Boss: thanh máu trên đầu Boss. Màn 1 có thêm thanh Fever và thanh thời gian vật phẩm.
- Hai nút tròn Trái/Phải ở hai góc dưới (vị trí chỉnh riêng từng màn, giống nhau cho cả 3 nhân vật).

**Popup**
- Pause: Tiếp tục / Chơi lại / Về menu.
- Bảng kết quả: điểm chạy số, 3 hàng điều kiện sao bay vào lần lượt, Coin thưởng, nút Retry / Next / Home. Nút **Next** chỉ hiện khi màn kế đã mở khóa.
- Hết lượt chơi: bảng hỏi xem quảng cáo để nhận +1 lượt.

### 2.5. Thiết kế gameplay

**Luật chơi chung**
- Nốt sinh từ track giai điệu của file MIDI. `MidiReader` ưu tiên track có tên chứa melody/lead/main/piano, rồi lọc khoảng cách tối thiểu theo độ khó: 0,30 / 0,22 / 0,15 / 0,10 giây cho độ khó 1–4.
- Mỗi nốt gắn với một làn: Trái hoặc Phải.

**Chấm điểm (`ComboSystem` + `ScoreSystem`)**

| Combo sau lần trúng | Nhãn | Điểm |
|---|---|---|
| 1–4 | GOOD | +100 |
| 5–9 | GREAT | +200 |
| ≥ 10 | PERFECT | +300 |
| Trượt | MISS | +0, Combo về 0 |

Chế độ Timing (có sẵn, đang tắt): Perfect khi lệch ≤ 0,06 s, Great ≤ 0,12 s, còn lại Good.

**Cách chơi từng màn**

| Màn | Bố cục | Thao tác | Đặc thù |
|---|---|---|---|
| Màn 1 – The Lost Melody | Nhân vật chạy ngang, nốt tới theo 2 làn | Trái = chạm nốt làn dưới, Phải = lướt lên làn trên (có animation nhảy); giữ nút cho nốt giữ; khi Fever đầy, bấm **cả hai nút cùng lúc** để bắn đạn | Nốt giữ khi nốt MIDI dài ≥ 0,5 s. Vật phẩm nạp thanh Fever → bắn đạn vào Boss. Boss có từ đầu, máu = số nốt chơi được |
| Màn 2 – The Dark Ritual | Nhân vật đứng giữa, ma vật tới từ hai phía | Chạm bên có ma vật khi nó tới vùng đánh | Nốt cách nhau ≤ 0,03 s được gộp. Boss phù thủy mất máu theo số nốt trúng |
| Màn 3 – The Abyssal Treasure | Nhân vật đứng giữa, đạn tàu ngầm tới hai phía | Chạm đúng bên trong cửa sổ ±0,15 s | Có vật phẩm rơi (tiếng bass khi nhặt). Chạm khi không có nốt không bị tính Miss |
| Màn 4 – The Mysterious Cave *(đang làm)* | Giống Màn 3 | Giống Màn 3 | Miss tạo vụ nổ và rung camera (`MissExplosionFeedback`) |
| Màn 5 – The Final Boss *(đang làm)* | Giống Màn 2 | Giống Màn 2 | Đấu Gigadeon |

**Điều kiện thắng / thua**
- Không thua giữa bài: hết bài thì luôn vào bảng kết quả.
- **3 sao**:
  - Sao 1: Score ≥ 60% điểm tối đa (tối đa = số nốt × 300).
  - Sao 2: Combo cao nhất ≥ 25% số nốt (tối đa 100).
  - Sao 3: điều kiện riêng từng màn, ví dụ Màn 1 là hạ Boss.
- **Coin thưởng:** 1 sao 200, 2 sao 500, 3 sao 1.000.
- **Mở màn bằng Coin:** Màn 1 miễn phí, Màn 2 = 2.500, Màn 3 = 5.000.

**Level design và tăng độ khó:** mỗi màn có `LevelData` riêng (nhạc, file MIDI, track, `timeOffset`, `noteSpeed`, `difficulty`). Độ khó tăng bằng tốc độ nốt, mật độ nốt (qua độ khó và MIDI Chart Tool) và cơ chế mới ở mỗi màn.

### 2.6. Thiết kế hệ thống âm thanh

| Loại | Cách làm |
|---|---|
| BGM | Mỗi màn một bài (`LevelData.music`), phát bằng `PlayScheduled` theo `dspTime`. Ở các scene menu, `BackgroundMusic` phát nhạc nền và giữ nhạc liên tục khi chuyển scene |
| SFX | `GameplaySfx`: âm 2D, 8 kênh, chặn phát chồng cùng một âm trong 30 ms, tự dừng khi Pause, tôn trọng nút tắt âm. Có âm trúng nốt, nhặt vật phẩm (`collectClip`), nổ (Màn 4), âm nút bấm (`ButtonSound`) |
| Cài đặt | `AudioPrefs` lưu hai khóa `SETTING_BGM` và `SETTING_SFX`, phát sự kiện `Changed` để mọi nguồn âm cập nhật ngay |
| Voice | Không có lồng tiếng |

## 3. Công nghệ & hệ thống

### 3.1. Công nghệ / Engine

| Thành phần | Phiên bản / ghi chú |
|---|---|
| Engine | **Unity 6000.3.24f1** (Unity 6), C# |
| Render | Universal Render Pipeline 17.3 (URP 2D Renderer, Light 2D) |
| Input | Input System 1.20 (bật song song với Input cũ: `activeInputHandler = Both`) |
| UI | uGUI + TextMeshPro |
| 2D | 2D Animation, 2D Sprite, Tilemap, Aseprite/PSD Importer |
| Khác | Cinemachine 3.1, Timeline 1.8, Test Framework 1.6 |
| Quảng cáo | Google Mobile Ads (AdMob) SDK (hiện dùng ID quảng cáo thử nghiệm của Google) |
| Đọc MIDI | `MidiReader` tự viết (đọc Standard MIDI File từ `StreamingAssets/MIDI`) |
| Quản lý mã nguồn | Git + GitHub Desktop, mỗi thành viên một nhánh (`Feature/Ntam`, `Feature/Long`, `Ttammm`, `Feature/Khoa`…) |

### 3.2. Yêu cầu hệ thống

| | Thông số |
|---|---|
| Build target | **Android**, Scripting Backend **IL2CPP**, game thiết kế cho màn hình **ngang** |
| Android tối thiểu | API 25 (Android 7.1) |
| Thiết bị thử | Samsung Galaxy (giả lập Galaxy S9 / A07 trong Device Simulator), tỷ lệ 16:9 đến 20:9 |
| Máy phát triển | Windows 10/11, Unity 6000.3.24f1, RAM ≥ 8 GB |
| Dung lượng lưu | 1 file JSON `PlayerData.json` trong `Application.persistentDataPath` |

> Cần sửa trước khi phát hành:
> - `companyName = DefaultCompany`, `applicationIdentifier = com.DefaultCompany.urp_2d` (tên gói vẫn là mặc định của template).
> - Hướng màn hình đang để **Auto Rotation và cho phép cả dọc**. Xoay điện thoại sẽ làm game chuyển sang màn dọc. Nên đặt Default Orientation = Landscape Left (hoặc chỉ cho phép hai hướng ngang).

### 3.3. Kiến trúc code

Code chính nằm ở `Assets/MusicGame/Scripts/TTAM`, theo hướng **component (MonoBehaviour) + ScriptableObject + Singleton quản lý dữ liệu + sự kiện (event)**.

| Mẫu thiết kế | Áp dụng trong BeatRun |
|---|---|
| **ScriptableObject** (data-driven) | `LevelData`: mỗi màn là một asset (nhạc, MIDI, track, offset, tốc độ, độ khó). Thêm màn không phải sửa code |
| **Singleton bền** | `PlayerDataManager` (DontDestroyOnLoad, tự khởi tạo) giữ toàn bộ dữ liệu người chơi |
| **Model – View** | Model: `PlayerData` và các lớp dữ liệu con. View: `CurrencyUIAutoSync`, `PlayTurnUI`, `ListCharController`. View nghe sự kiện `OnCurrencyChanged`, `OnProgressionChanged` thay vì đọc liên tục |
| **State Machine** | Animator nhân vật (Idle/Run ↔ Attack1–3 ↔ Jump ↔ Hold ↔ Die). Trạng thái màn: Countdown → Playing ↔ Paused → Result |
| **Template Method** | `EndGameBase` làm phần chung của bảng kết quả (sao, Coin, lưu); `EndGameManager`, `EndGameManagerMiss`, `EndManagerMan3` chỉ định nghĩa điều kiện sao 3. `RhythmCountdown` làm phần chung của `CountDownMan2/3` |
| **Interface** | `ISongPausable` (mọi thứ cần dừng khi Pause), `IRhythmSongClock` |
| **Observer / Event** | `RhythmEvents.NoteMissed`, `BossHealth.AnyBossDefeated`, `AudioPrefs.Changed` |

### 3.4. Sơ đồ module / lớp chính

| Module | Lớp chính | Nhiệm vụ |
|---|---|---|
| **Nhạc & nhịp** | `MidiReader`, `LevelData`, `SongClock`, `SongPause`, `RhythmCountdown` | Đọc MIDI → danh sách nốt (thời điểm, độ dài, làn); đồng hồ bài hát theo `dspTime`; Pause/Resume đồng bộ |
| **Sinh nốt (mỗi màn một spawner)** | `PianoKeySpawner` (Màn 1), `BossSpawner2` (Màn 2, 5), `SubmarineSpawner` (Màn 3, 4) | Lọc nốt, sinh prefab đúng thời điểm, nhận input, tính độ lệch |
| **Nốt / vật phẩm** | `PianoKey`, `LongNote`, `RhythmItemm`, `FallingItem`, `EnemyRhythm`, `BossProjectile` | Hành vi từng loại đối tượng |
| **Input** | `LaneButton`, `HoldInput`, `RhythmInput` | Nút Trái/Phải, giữ nút; khóa input khi `timeScale = 0` |
| **Chấm điểm** | `ComboSystem`, `ScoreSystem` | Nhãn GOOD/GREAT/PERFECT/MISS, Combo, Score, đếm Miss |
| **Nhân vật** | `PlayerController` (Màn 1), `PlayerController2/3`, `GameplayCharacterSelector`, `AnimatorSafe`, `PlayerHealth` | Di chuyển và animation; bật đúng nhân vật đã chọn; tạo trigger an toàn |
| **Boss** | `BossSpawner`, `BossHealth`, `RuntimeBossHpBar`, `BossControler2`, `SubmarineController` | Máu Boss theo số nốt, trừ máu khi trúng, thanh máu |
| **Fever (Màn 1)** | `ItemFeverBar`, `ItemDurationSystem`, `PlayerFeverShooter`, `PlayerBullet` | Nạp Fever bằng vật phẩm, bắn đạn vào Boss |
| **Kết quả & điều hướng** | `EndGameBase` (+3 lớp con), `LevelFlow`, `Scenes`, `SceneFader`, `StoryController` | Bảng kết quả, chuyển màn, hiệu ứng chuyển cảnh |
| **Dữ liệu người chơi** | `PlayerDataManager`, `PlayerData`, `CurrencyData`, `ProgressionData`, `LevelRecordData`, `PlayTurnData`, `ComboStatsData`, `DailyMissionData`, `SettingsData` | Lưu/đọc JSON, tiền tệ, mở khóa, kỷ lục, lượt chơi, nhiệm vụ ngày |
| **Menu & kinh tế** | `LevelSelectionManager`, `ListCharController`, `PurchasePanel`, `MissionPanelControl`, `ArchivementPanel`, `RewardedAdsManager`, `WatchAdsUI` | Chọn màn/nhân vật, mua mở khóa, nhiệm vụ, thành tựu, quảng cáo |
| **Âm thanh** | `GameplaySfx`, `BackgroundMusic`, `AudioPrefs`, `ButtonSound` | BGM, SFX, cài đặt |
| **Công cụ Editor** | `MidiChartTool`, `CharacterAnimatorTool` | Lọc/xuất MIDI; tạo clip và chuẩn hóa Animator cho 3 nhân vật |

**Luồng dữ liệu khi trúng một nốt**

`LaneButton` → Spawner kiểm tra nốt gần nhất trong cửa sổ chạm → `ComboSystem.HitNoteTimed()` → `ScoreSystem.AddScore(grade)` và `BossHealth.TakeDamage()` → `PlayerController` phát animation Attack → `GameplaySfx` phát âm trúng. Hết bài: `EndGameBase` → `PlayerDataManager.AddCoin()` và `RecordLevelResult()` → ghi `PlayerData.json`.

**Cấu trúc file lưu `PlayerData.json`**

| Trường | Nội dung |
|---|---|
| `currency` | `coin`, `diamond`, tổng đã nhận |
| `progression.unlockedLevels` | Danh sách màn đã mở (mặc định `[1]`) |
| `unlockedCharacters` | Nhân vật đã mở (Ramona = 0, mở sẵn) |
| `levelRecords[]` | `levelId`, `highScore`, `maxCombo` |
| `comboStats` | Tổng số Good/Great/Perfect (dùng cho thành tựu) |
| `playTurn` | Số lượt (tối đa 20), mốc hồi lượt (5 phút), tổng số lần chơi |
| `dailyMission` | Ngày, tiến độ 3 nhiệm vụ, trạng thái đã nhận |
| `claimedAchievements` | Thành tựu đã nhận |
| `settings` | Âm lượng nhạc / SFX |

Ghi file an toàn: ghi ra `.tmp` rồi `File.Replace`. Nếu file hỏng, đổi tên thành `.corrupt_…` để giữ lại và tạo dữ liệu mới, nên game không bị kẹt.

### 3.5. Tích hợp dịch vụ

| Dịch vụ | Trạng thái |
|---|---|
| **Quảng cáo (Google AdMob – Rewarded)** | Đã tích hợp. Xem quảng cáo nhận +500 Coin hoặc +1 lượt chơi. Kết quả trả về từ luồng SDK được đẩy sang main thread để cập nhật UI. Đang dùng ID thử nghiệm, cần thay ID thật khi phát hành |
| **In-app Purchase** | Có giao diện bảng nạp (`PurchasePanel`) cộng Coin/Diamond, **chưa kết nối Google Play Billing** (chỉ dùng cho demo) |
| **Firebase / Photon / Leaderboard** | Không sử dụng. Game chơi offline, dữ liệu lưu trên máy |

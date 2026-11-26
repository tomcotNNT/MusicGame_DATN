# Đối chiếu Bảng kế hoạch CP02 với tình trạng code

Cập nhật: 25/11/2026 · Nhánh `Feature/Ntam` · **Chưa commit, chưa test Play mode**

Ký hiệu: ✅ đã xong trong code · 🟡 xong một phần / cần người làm tiếp · 🎨 việc thiết kế, âm thanh hoặc video (không làm bằng code được)

## 1. Những việc bảng ghi "Chưa bắt đầu" hoặc "Đang thực hiện"

| Việc trong bảng | Người phụ trách | Tình trạng | Đã làm gì |
|---|---|---|---|
| Thiết kế thanh máu Boss màn 1 2 3 4 5 | Long | ✅ màn 1–4 | Man2/Man3/Man4 đã có sẵn thanh máu, máu = số nốt. **Man1 mới thêm:** boss xuất hiện ngay đầu bài, máu = số nốt đánh được, mỗi nốt đánh trúng (nốt thường, nốt dài) trừ 1 máu. Thanh máu tự tạo trên đầu màn hình nếu prefab chưa gán (`RuntimeBossHpBar`). Man5 chưa có scene |
| Đổi cơ chế Boss cho màn 1 2 giống màn 3 | TTam | ✅ | Man2 vốn đã giống Man3 (`BossSpawner2.InitHp` + `TakeHit`). Man1: bật `hasBoss`, `bossFromStart = true`, `bossHpRatio = 1`. Sao 3 của Man1 đổi thành "Defeat Boss" như 2 màn còn lại |
| Hiệu ứng chuyển scene mượt | Long | ✅ | `SceneFader`: tối dần 0,35 s rồi chuyển scene, sáng dần khi vào. Tự áp dụng cho mọi scene; các nút điều hướng, Story, SelectChar, SelectSong đều đi qua nó. Chặn bấm đúp để không bị trừ 2 lượt |
| Chức năng Mission hoạt động | TTam | ✅ | 3 nhiệm vụ ngày dùng đúng 3 dòng có sẵn trong panel: hoàn thành 3 màn / 100 Perfect / combo 50. Thưởng 500 coin mỗi nhiệm vụ, lưu trong `PlayerData.json`, tự làm mới mỗi ngày. Nút hiện GO → CLAIM → DONE |
| Watch ADS nhận được vật phẩm tương ứng | TTam | ✅ | Xem hết quảng cáo được +500 coin, khớp với chữ "YOU RECEIVED 500" trên bảng. Loại thưởng và số lượng chỉnh được trong Inspector (`WatchAdsUI`) |
| Thêm animation nhân vật Stephen | TTam | 🟡 | Stephen **đã có trong cả 3 màn**, đủ component Player, nhưng Animator mới có 2 trạng thái Idle và Attack. Đã sửa để không spam cảnh báo "Parameter does not exist". 🎨 Còn thiếu clip Run/Jump/Die/Hold (cắt từ `Char/Stephen Stills.png`) |
| Tạo prefab cho Stephen | TTam | 🟡 | Object nhân vật đã nằm sẵn trong từng scene và chạy được. Nếu cần prefab dùng chung: kéo object Stephen ở Man1 vào thư mục Prefabs |
| Thêm animation nhân vật Knife Chau | TTam | 🟡 | Tương tự Stephen: đã có Idle/Run/Attack1/Attack2; 🎨 thiếu Jump/Die/Hold |
| Tạo prefab cho Knive Chau | TTam | 🟡 | Như Stephen |
| Tìm nhạc cho màn 4, 5 | TTam | 🎨 | Man4 đang dùng tạm LevelData và nhạc của Man3 (`levelId` đã sửa thành 4). Cần nhạc + file MIDI mới |
| Thiết kế background màn 4, 5 | Long | 🎨 | Đang thực hiện |
| Làm video Demo / trailer | Khoa / Việt | 🎨 | Nên quay **sau khi** test xong bản sửa này |

## 2. Những việc bảng ghi "Đã hoàn thành" nhưng code có vấn đề (đã sửa)

| Việc | Vấn đề tìm thấy | Đã sửa |
|---|---|---|
| Cơ chế tính sao | Man1 cộng điểm 2 lần mỗi nốt; Man2/Man3 không đếm Miss; Man1 hiện bảng thưởng dù đã thua | Chấm điểm và đếm Miss dùng chung một chỗ; bảng kết thúc gộp thành `EndGameBase` |
| Hiệu ứng tăng sao & Win Panel | 3 bản code gần giống hệt nhau, mỗi bản hiển thị một thứ tự khác | Một lớp chung, cùng thứ tự: điểm → coin → sao. Nút Next tự ẩn khi màn kế còn khóa |
| Lưu chỉ số màn chơi (JSON) | Ghi file 3 lần mỗi màn; vào game không qua SelectSong thì không lưu được | Ghi 1 lần (`RecordLevelResult`); `PlayerDataManager` luôn tồn tại |
| Cộng vàng/diamond Purchase | OK | — |
| Cơ chế dùng vàng mở khóa màn | Màn 1 không tự mở khi tạo save mới | Màn 1 luôn mở |
| Cửa sổ debug | OK (chỉ hiện ở Development Build) | Nút Skip bài tự ẩn trong bản phát hành |

## 3. Cần nhóm kiểm tra / quyết định

1. **Ngưỡng sao:** bảng ghi "combo cao nhất 100", nhưng scene đang đặt Man1 = 20, Man2 = 30, Man3 = 20. `scoreForStar` của Man1 (10000) được đặt khi điểm còn bị nhân đôi. Cần chơi thử rồi chốt lại một bộ số.
2. **Boss Man1 bắn từ đầu bài:** vì boss có mặt ngay từ đầu nên người chơi bị bắn sớm hơn trước. Nếu thấy quá khó: giảm sát thương của `BossMusicAttack` hoặc tắt `bossFromStart` trên `PianoKeySpawner`.
3. **Man5:** chưa có scene. Nhân bản Man4 khi đã có nhạc và background, rồi đặt `levelId = 5`, thêm `StoryMan5`, thêm vào SelectSong và Build Settings.

## 4. Test nhanh các phần mới (≈ 10 phút)

- [ ] Chuyển scene bất kỳ: màn hình tối dần rồi sáng dần, không bị kẹt màn đen.
- [ ] Man1: thanh máu "BOSS x/y" hiện ngay đầu bài, giảm khi đánh trúng nốt; đánh hết nốt thì boss chết, sao 3 "Defeat Boss" sáng.
- [ ] Mission: chơi xong 1 màn → dòng 1 hiện 1/3; đủ điều kiện → CLAIM → +500 coin → DONE. Đổi ngày trên máy thì tiến độ về 0.
- [ ] Chọn Chau / Stephen chơi Man1: Console không còn spam cảnh báo Animator.

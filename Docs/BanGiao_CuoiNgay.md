# Bàn giao cuối ngày — 25/11/2026 (chuẩn bị test điện thoại thứ 6)

Trạng thái: `dotnet build` (runtime + Editor) **0 lỗi**, Unity Editor biên dịch 0 lỗi. **Chưa commit, chưa push.** Chưa chạy Play mode / máy thật.

## ✅ Đã xong, sẵn sàng test
- **Chơi:** điểm/combo/Miss không còn cộng trùng (Man1, Man2). Pause không lệch nhịp. Không bấm trúng được khi đang dừng. Không có bảng thưởng khi thua.
- **Boss:** máu = số nốt ở cả 3 màn, thanh máu tự ẩn khi boss chết. Boss Man1 xuất hiện từ đầu nhưng chưa bắn trong 10 giây đầu.
- **Sao:** tự tính theo số nốt thật (60% điểm tối đa; combo 25%, tối đa 100). Chế độ chấm theo timing có sẵn, đang tắt.
- **Luồng:** fade chuyển cảnh, Replay/Next/Home dùng chung luật, chặn bấm đúp, không còn nhạc chạy thoáng qua khi thoát lúc đang pause.
- **Dữ liệu:** Mission ngày (3 nhiệm vụ) lưu JSON; chống mất save (file tạm + sao lưu file hỏng); chỉnh đồng hồ lùi không nhận thưởng lại được.
- **Quảng cáo:** +500 coin; callback chuyển về luồng chính (tránh crash Android).
- **Sửa hỏng có sẵn:**
  - Man3 trỏ vào LevelData không tồn tại (không có nhạc) → đã nối lại `Level3`.
  - Khôi phục `LongNote.prefab` (nốt giữ của Man1).
  - Gỡ xung đột merge trong 2 font (`PublicPixel SDF`, `Pĩxel SDF`).
- **Âm thanh:** item/nốt phát qua bộ phát 2D chung, không chồng tiếng, dừng theo Pause.
- **Công cụ:** *Tools ▸ BeatRun ▸ MIDI Chart Tool* và *Animator nhân vật*.
- **Khung Màn 4/5:** `Man4` (quái nổ khi bỏ lỡ), `Man5` (Gigadeon), `StoryMan4/5`, `Level4/5`. Chưa đưa vào Build Settings.

> **Lưu ý Editor:** project bật *Enter Play Mode Options* (tắt Domain Reload) nên biến static được giữ giữa các lần Play. Đã sửa `SceneFader` (lỗi màn đen khi chuyển scene) và các script mới để tự reset mỗi lần Play. Nếu vẫn gặp lỗi lạ chỉ xảy ra ở **lần Play thứ 2 trở đi**, thử tắt *Edit ▸ Project Settings ▸ Editor ▸ Enter Play Mode Settings* để kiểm tra.

> **Sửa thêm sau khi test Editor (tối 25/11):**
> - Màn đen khi chuyển scene: đã sửa `SceneFader`.
> - Font lỗi `KeyNotFoundException '77'` (ký tự "/"): đã sửa.
> - Man3/Man4: bấm vào chỗ trống không còn bị tính Miss.
> - Cảnh báo `Hash 0` của tàu ngầm/boss: đã sửa.
> - Bớt spam Console: cảnh báo "Chưa gán Hit Sound" chỉ báo 1 lần, bỏ dòng log "MISS - Combo Reset".
> - **Đồng hồ bài hát tự chuyển sang thời gian thật nếu `dspTime` không chạy.** Trong 1 lượt test, Man3 không thả item nào. Nếu còn gặp, Console sẽ có dòng `[Man3] Chưa thả item nào: …` hoặc `[SongClock] …`. Gửi dòng đó lại để xử lý tiếp.

## 🖐 5 việc tay trong Unity sáng mai (trước khi Build APK)
1. Mở Unity, nếu được hỏi tải lại scene thì chọn **Reload**. Chờ biên dịch xong, Console không có lỗi đỏ. *(Thông báo font "merge conflicts" phải hết.)*
2. **Tools ▸ BeatRun ▸ Chuẩn hoá Animator cả 3 nhân vật**. Nếu kịp: cắt clip Jump/Die/Hold cho Chau và Stephen (xem `HuongDan_Art_Animation_MIDI.md`).
3. Mở **Man1**, chọn `PianoKeySpawner`: kiểm tra *Has Boss* = bật, *Long Note Prefab* = `LongNote`, *Boss From Start* = bật. Chơi thử 1 lượt.
4. **File ▸ Build Settings:** chỉ bật đúng 10 scene đang dùng (LoadingScene, MainMenu, SelectSong, SelectChar, StoryMan1–3, Man1–3). **Không** bật Development Build nếu không muốn hiện nút Skip/Admin.
5. **Player Settings:** đổi Package Name khỏi `com.DefaultCompany.urp_2d` nếu cần cài song song; tăng Bundle Version Code; giữ ID quảng cáo test cho buổi thử.

## 📱 Checklist 10 phút trên máy thật
- [ ] Mở game → Loading → MainMenu → SelectSong: chuyển cảnh có fade, có nhạc nền.
- [ ] Tắt/bật nút Nhạc và Âm thanh → thoát app mở lại vẫn giữ nguyên.
- [ ] Mission: chơi xong 1 màn → dòng 1 = 1/3; khi đủ điều kiện → CLAIM → +500 → DONE.
- [ ] Xem quảng cáo (test) → +500 coin, game không đứng.
- [ ] Man1: thanh máu BOSS hiện; nốt giữ (Hold) xuất hiện; Pause rồi tiếp tục vẫn đúng nhịp.
- [ ] Đang chơi Man1, bấm nút Home của điện thoại rồi quay lại → game đang pause, tiếp tục đúng nhịp.
- [ ] Để thua ở Man1 → chỉ có Game Over → Replay trừ 1 lượt.
- [ ] Man2: READY/GO khớp nhạc; mỗi lần trúng chỉ +1 combo.
- [ ] Man3: **có nhạc và nốt** (trước đó bị mất), hạ tàu ngầm → sao 3.
- [ ] Bấm Replay/Home 2 lần liên tục → chỉ trừ 1 lượt, không kẹt màn đen.

## 🔒 Git — an toàn
- Hiện **chưa có gì được stage/commit**. `.qldatn/tracker.json` đang **bị sửa nhưng không stage**; `Assets/Editor/QLDATNSceneTracker.cs` hiện không đổi.
- Khi commit, chỉ thêm đúng các file cần:
```powershell
git add Assets/MusicGame Assets/Scenes/TTAM Assets/Scenes/VIET/Game/Scripts/Boss.cs Docs
```
- Để máy bạn không còn thấy 2 file tracker bị sửa (chỉ ảnh hưởng máy bạn, không đụng lịch sử, không ảnh hưởng nhóm):
```powershell
git update-index --skip-worktree .qldatn/tracker.json Assets/Editor/QLDATNSceneTracker.cs
```
- **Không dùng `git rm --cached`** cho 2 file này trừ khi cả nhóm thống nhất: lệnh đó sẽ **xoá file tracker trên máy các thành viên khác** khi họ pull.
- `MusicGame_DATN.slnx` do Unity tự sinh, không cần commit.

## 📄 Tài liệu
`Docs/TaiLieuKyThuat_BaoCao.md` (đưa vào báo cáo) · `ThietKe_Man4_Man5.md` · `HuongDan_Art_Animation_MIDI.md` · `ChamDiem_Timing_va_Boss.md` · `DoiChieu_KeHoach_CP02.md` · `LoTrinh_SuaLoi.md`

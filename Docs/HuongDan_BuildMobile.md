# Dặn dò thành viên code chính: gộp code và build bản mobile

**Nhánh nguồn:** `Feature/Ntam` (commit `25c5247`, đã có trên GitHub). Ngày kiểm tra: 26/11/2026.

## 0. Ưu tiên (làm theo thứ tự)
| # | Việc | Mức |
|---|---|---|
| 1 | Gộp `Feature/Ntam` vào `main` bằng Pull Request | bắt buộc |
| 2 | Khoá màn hình **ngang** (hiện đang xoay dọc được) | **bắt buộc** |
| 3 | Đặt **60 fps** (Android mặc định 30 fps nên nốt bị giật) | **bắt buộc** |
| 4 | Audio **Best latency** và chỉnh lệch nhịp trên máy thật | **bắt buộc** với game nhịp điệu |
| 5 | Đổi Package Name, tăng Version Code | bắt buộc |
| 6 | Rà danh sách scene trong Build Settings | bắt buộc |
| 7 | Chạy hết checklist trên máy thật (mục 6) | bắt buộc |
| 8 | Nén ảnh nhân vật / quảng cáo thật | tuỳ chọn |

## 1. Gộp code
- `main` đang chậm hơn `Feature/Ntam` **165 commit**. Tạo Pull Request `Feature/Ntam → main`, đừng merge ngược `main` vào.
- `Feature/Long`, `Ttammm`, `viettvv` và `morant` đã nằm trong `Feature/Ntam`.
- **`Feature/Khoa` còn 1 commit chưa gộp** ("các animation của player", 14/11). Nếu gộp mà xung đột ở `Assets/MusicGame/Animation/P1..P3`:
  1. Chọn bản của `Feature/Ntam`.
  2. Trong Unity, bấm **Tools > BeatRun > Thêm trạng thái mới (Attack4-8, Hurt, Dodge, Special, AirAttack, Victory)**. Lệnh này nối lại các trạng thái animation; bấm nhiều lần vẫn an toàn.
- **Không commit** `.qldatn/tracker.json` và `Assets/Editor/QLDATNSceneTracker.cs`. Nên dùng lệnh dưới đây thay vì `git rm --cached` (cách đó sẽ xoá file tracker của cả nhóm khi pull):
  ```
  git update-index --skip-worktree <file>
  ```
- Sau khi pull, mở Unity và chờ import xong. Console **không được có lỗi đỏ**.

## 2. Build Settings (File > Build Profiles)
Danh sách scene đang **bật** (thứ tự hiện tại):

| Index | Scene |
|---|---|
| 0 | `Scenes/TTAM/LoadingScene` (scene mở đầu) |
| 1–3 | `Scenes/TTAM/Man1`, `Man2`, `Man3` |
| 4 | `Scenes/TTAM/SelectSong` |
| 5–7 | `Scenes/TTAM/StoryMan1`, `StoryMan2`, `StoryMan3` |
| 8 | `Scenes/TTAM/SelectChar` |
| 9 | `Scenes/TTAM/MainMenu` |

- **Giữ `LoadingScene` ở vị trí 0.**
- `Tutorial` đang **tắt**. Nếu menu có nút Hướng dẫn thì phải tích bật, nếu không bấm nút đó sẽ lỗi.
- **Chưa cho vào build:**
  - `Man4`, `Man5`, `StoryMan4`, `StoryMan5`: chưa xong gameplay.
  - **`Man4_Test`: tuyệt đối không cho vào build.**
  - Nút NEXT tự ẩn sau Man3, vì code kiểm tra scene có trong build hay không.
- Các scene cũ khác (Scene_Ntam, Scenes/Game…) để tắt là được.

## 3. Player Settings (Edit > Project Settings > Player > Android)
| Mục | Hiện tại | Nên đặt |
|---|---|---|
| Company Name | DefaultCompany | tên nhóm |
| Package Name | `com.DefaultCompany.urp_2d` | ví dụ `com.<tennhom>.beatrun`. Đổi tên này thì máy coi là app mới |
| Version / Bundle Version Code | 1.0 / 1 | **tăng Version Code mỗi lần gửi bản mới** |
| **Default Orientation** | Auto Rotation, **cho phép cả dọc** | **Landscape Left** (hoặc Auto Rotation nhưng chỉ tích Landscape Left và Landscape Right) |
| Scripting Backend | IL2CPP | giữ nguyên |
| Target Architectures | ARM64 | giữ nguyên |
| Min API | 25 | giữ nguyên |
| Target API | Automatic | giữ nguyên |
| Active Input Handling | Both | **giữ Both**: code dùng cả Input cũ lẫn Input System |
| Keystore | debug | APK test dùng key debug là được. Nộp Google Play thì tạo keystore riêng và **lưu kỹ** (mất là không cập nhật app được nữa) |

## 4. Nhịp và hiệu năng (quan trọng nhất)
1. **60 fps:**
   - Android mặc định khoá 30 fps, nốt nhạc sẽ giật. Trong code **chưa có** dòng đặt fps.
   - Thêm `Application.targetFrameRate = 60;` vào chỗ khởi động. Gợi ý: hàm `Init` (chạy BeforeSceneLoad) trong `Scripts/TTAM/SceneFader.cs`, hoặc `PlayerDataManager.Bootstrap`.
   - Đồng thời đặt `QualitySettings.vSyncCount = 0;`.
2. **Độ trễ âm thanh:**
   - Project Settings > Audio > **DSP Buffer Size = Best latency** (đang là 1024).
3. **Lệch nhịp trên máy thật:**
   - Nốt chạm vạch sớm hoặc muộn hơn tiếng nhạc là do loa hoặc tai nghe Bluetooth của từng máy.
   - Chỉnh `timeOffset` trong `Assets/MusicGame/Data/Level1..Level5.asset`. Số dương là nốt đến muộn hơn.
   - Thử trên ít nhất 2 máy, có dây và Bluetooth.
4. **Nhạc bài hát:** chọn file nhạc (`Assets/MusicGame/Music/TTAM/*.wav`), đặt Load Type = **Compressed In Memory** và Compression = **Vorbis** (chất lượng khoảng 70). Cách này giảm dung lượng APK; đồng hồ nhạc vẫn chạy theo dspTime nên không lệch.
5. **Ảnh nhân vật:**
   - 3 sprite sheet đang **không nén**, chiếm khoảng 35 MB RAM với Chau, 15 MB mỗi sheet còn lại.
   - Máy yếu bị văng: vào tab Android của ảnh → Override → Format **ASTC 4x4**. **Giữ Filter Mode = Point** và **không giảm Max Size dưới kích thước ảnh** (Chau 2458×3572 cần 4096), nếu không sprite sẽ nhoè và lệch khung.
6. **Bỏ tích Development Build** khi build bản nộp:
   - Nút SkipSong (debug) tự ẩn.
   - Các script spawner đang bật `Show Debug`, in rất nhiều log. Tắt trong Inspector để nhẹ hơn (tuỳ chọn).

## 5. Quảng cáo, lưu dữ liệu
- **AdMob đang dùng ID test của Google** (`ca-app-pub-3940256099942544…`):
  - Bản demo hoặc bảo vệ đồ án: **giữ nguyên ID test**.
  - Nếu phát hành thật: thay App ID (Assets/GoogleMobileAds/Resources) và Ad Unit ID (`Scripts/LONG/ADS/RewardedAdsManager.cs`). **Không tự bấm quảng cáo thật** trên máy mình, vì tài khoản AdMob có thể bị khoá.
- Dữ liệu người chơi lưu ở `Application.persistentDataPath`, có file tạm `.tmp` và bản sao lưu khi file hỏng. Gỡ app là mất dữ liệu (bình thường).

## 6. Checklist chạy thử trên điện thoại thật
- [ ] Mở app → Loading → Menu → chọn nhân vật → chọn bài → Story → vào màn, không bị **màn hình đen** khi chuyển scene.
- [ ] Xoay điện thoại: game **không** quay sang dọc.
- [ ] Chơi Man1, Man2, Man3 với **cả 3 nhân vật** (Ramona, Chau, Stephen): animation chạy, đánh, nhảy, Victory khi thắng.
- [ ] Bấm **2 nút cùng lúc** (2 ngón) vẫn nhận cả hai.
- [ ] Nốt khớp nhạc. Nếu lệch, chỉnh `timeOffset` (mục 4.3).
- [ ] **Tắt màn hình hoặc chuyển app** giữa bài → quay lại thấy menu Pause → RESUME → đếm 3-2-1 → nhạc chạy tiếp đúng nhịp.
- [ ] Kéo MUSIC / SOUND trong menu Pause → âm lượng đổi; thoát app mở lại vẫn giữ.
- [ ] Hết bài → bảng kết quả (sao, coin). Thắng mới có NEXT; sau Man3 không có NEXT.
- [ ] Coin được cộng. Mở khoá nhân vật bằng coin. Xem quảng cáo thưởng +500 coin.
- [ ] Tắt app hẳn, mở lại → coin, nhân vật đã mở, sao từng màn vẫn còn.
- [ ] Máy có tai thỏ: HUD (điểm, nút Pause) không bị che.
- [ ] Chơi liên tục 3 màn không bị nóng máy hay văng app.

## 7. Phần chưa xong (không chặn bản build hiện tại)
- **Màn 4:** gameplay né quái đang ở `Man4_Test`. Muốn đưa vào game thì làm theo `Docs/ThietKe_Asset_Man4.md` mục 4.4, sau đó thêm Man4 và StoryMan4 vào Build Settings.
- **Màn 5** (boss Gigadeon): mới có khung scene.
- **Man2–Man5:** nhân vật chưa có `PlayerHealth`, nên các animation Hurt / Die chưa xuất hiện.

## 8. Tài liệu liên quan (thư mục Docs)
| File | Nội dung |
|---|---|
| `ChuanAnimation_NhanVat.md` | chuẩn animation, 8 đòn đánh, bản đồ animation theo màn |
| `ThietKe_Asset_Man4.md` | quái Màn 4 và cách gộp |
| `HuongDan_Art_Animation_MIDI.md` | làm MIDI và cắt sprite |
| `BanGiao_CuoiNgay.md`, `LoTrinh_SuaLoi.md` | lịch sử sửa lỗi |

**Các menu tiện dụng trong Unity:**
- **Tools > BeatRun > Xem thử UI**: mở bảng kết quả hoặc menu Pause khi đang Play.
- **Tools > BeatRun > Xem thử Animation Sprite**: xem từng clip animation.
- **Tools > BeatRun > MIDI Chart Tool**: làm MIDI.

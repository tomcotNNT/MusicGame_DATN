# Lộ trình sửa lỗi & thống nhất gameplay — BeatRun (Scenes/TTAM)

Ngày thực hiện: 25/11/2026 · Nhánh: `Feature/Ntam` · Trạng thái: **đã sửa, CHƯA commit, CHƯA test Play mode**

---

## 1. Luồng game sau khi thống nhất

```
LoadingScene → MainMenu → SelectSong ──(còn lượt?)──▶ SelectChar ──(trừ 1 lượt)──▶ StoryManN → ManN
                               ▲                                                             │
                               │            ┌──────────── bảng kết thúc / Game Over ◀───────┘
                               │            │
                               ├── Home ────┤
                               │            ├── Replay : trừ 1 lượt → chơi lại ManN ngay (bỏ cốt truyện)
                               │            └── Next   : màn N+1 đã mở khóa + trừ 1 lượt → StoryMan(N+1)
                               └── hết lượt / màn khóa / thiếu scene → luôn quay về SelectSong
```

Mọi nút điều hướng trong màn chơi đi qua **một** chỗ: `LevelFlow` (`Scenes.RetryLevel / NextLevel / BackToSelect`).

| Thành phần | Man1 (piano) | Man2 (boss) | Man3 (tàu ngầm) |
|---|---|---|---|
| Đồng hồ bài hát | `PianoKeySpawner` | `BossSpawner2` | `SubmarineRhythmSpawner` |
| Pause đúng nhịp | ✅ | ✅ (mới) | ✅ (mới) |
| Đếm ngược | lead-in trong spawner | `RhythmCountdown` | `RhythmCountdown` |
| Bảng kết thúc | `EndGameBase` | `EndGameBase` | `EndGameBase` |
| Sao 1 / 2 | Điểm / Combo | Điểm / Combo | Điểm / Combo |
| Sao 3 | Miss ≤ ngưỡng | Hạ boss | Hạ tàu ngầm |
| Chấm điểm | `ComboSystem.HitNote()` | như Man1 | như Man1 |
| Đếm Miss | `ComboSystem.MissNote()` | như Man1 | như Man1 |
| Có thể thua | ✅ (máu) | ❌ (chờ quyết định — xem Kế hoạch) | ❌ |

---

## 2. Những gì đã sửa

### Giai đoạn 1 — Lỗi gameplay (ảnh hưởng điểm, sao, coin)
| # | Lỗi | Cách sửa | File |
|---|---|---|---|
| 1 | Man1 cộng điểm **2 lần** mỗi nốt (AddScore + HitNote) | Chỉ `ComboSystem.HitNote()` cộng điểm; script khác chỉ tự cộng khi scene không có ComboSystem | `PianoKey`, `LongNote`, `RhythmItem`, `BossProjectile` |
| 2 | Man2/Man3 không đếm Miss; nốt dài bị lỡ không mất combo | `ComboSystem.MissNote()` là nơi **duy nhất** gọi `ScoreSystem.AddMiss()`; nốt dài bị lỡ gọi `MissNote()` | `ComboSystem`, `LongNote`, `PianoKey` |
| 3 | Pause làm lệch nhịp (dspTime vẫn chạy khi pause) | Lớp `SongClock` đóng băng SongTime và dời mốc bài; `Scenes` pause/resume mọi spawner qua `ISongPausable`; điện thoại chuyển app → tự mở menu pause | `SongPause.cs` (mới), `BossSpawner2`, `SubmarineSpawner`, `PianoKeySpawner`, `Scene.cs` |
| 4 | Chết rồi nhạc/nốt vẫn chạy theo dspTime | `PlayerHealth.Die()` pause đồng hồ bài hát | `PlayerHealth` |
| 5 | Esc khi Game Over làm game chạy tiếp | Không cho mở/đóng pause khi game đã bị dừng bởi script khác | `Scene.cs` |
| 6 | Man1 hiện bảng thưởng + cộng coin dù đã Game Over | `EndGameBase.CanShowResult()` = không có người chơi nào chết | `EndGameBase.cs` |

### Giai đoạn 2 — Thống nhất luồng
| # | Việc | File |
|---|---|---|
| 7 | Replay/Next/Home dùng chung luật (trừ lượt, kiểm tra khóa) — 14 nút ở Man1–Man4 (kể cả prefab `EndPanel` trong Man1) đã nối lại | `LevelFlow.cs` (mới), `Scene.cs`, 4 scene |
| 8 | Nút **Next** tự ẩn khi màn kế chưa mở khóa / chưa có trong build | `EndGameBase`, `LevelFlow.IsNextLevelAvailable()` |
| 9 | Đếm ngược Man2/Man3: nhạc lên lịch ngay khi bắt đầu đếm, chữ GO tắt đúng lúc nhạc vào; tắt `spawnOnStart` ở Man3/Man4 | `RhythmCountdown.cs` (mới), `CountDownMan2/3` |
| 10 | Gộp 3 script bảng kết thúc thành 1 lớp `EndGameBase` (giữ nguyên giá trị Inspector); thứ tự hiển thị giống nhau: điểm → coin → sao | `EndGameBase.cs` (mới), `EndGameManager*`, `EndManagerMan3` |
| 11 | Bỏ `ResultSystem` ghi tiến độ vào hệ PlayerPrefs cũ (`SelectLevel`) | `ResultSystem.cs` |

### Giai đoạn 3 — Dữ liệu, kinh tế, âm thanh
| # | Việc | File |
|---|---|---|
| 12 | `PlayerDataManager` **luôn tồn tại** (tự tạo nếu scene không có) → vào game từ bất kỳ scene nào cũng lưu được kết quả | `PlayerDataManager.cs` |
| 13 | Màn 1 luôn mở khi tạo save mới | `PlayerDataManager.cs` |
| 14 | Lưu file an toàn (`File.Replace`) — tắt máy giữa chừng không mất save | `PlayerDataManager.cs` |
| 15 | Xem quảng cáo **có thưởng**: +500 coin (đúng chữ "YOU RECEIVED 500" trên bảng), chỉnh được trong Inspector | `WatchAdsUI.cs` |
| 16 | Nút Nhạc/Âm thanh hoạt động thật, lưu `SETTING_BGM`/`SETTING_SFX`; tắt SFX → tắt tiếng nút, tiếng đánh nốt, đếm ngược, chạm màn hình | `AudioPrefs.cs` (mới), `AudioToggleControl`, `BackgroundMusic`, `ButtonSound`, các PlayerController… |
| 17 | Nút `SkipSong` tự ẩn trong bản build phát hành (vẫn hiện trong Editor / Development Build) | `DebugUiFilter.cs` (mới) |

### Giai đoạn 4 — Dọn scene
| # | Việc |
|---|---|
| 18 | Man1: xoá 3 component mất script (`CoinSystem`, `CoinSpawner`, `MagnetSpawner`) |
| 19 | SelectChar: tên scene dự phòng `GameplayScene` (không tồn tại) → `StoryMan1` |
| 20 | Man4: `levelId` 3 → 4 (trước trùng kỷ lục với Man3); thêm Man4 + StoryMan vào danh sách tắt nhạc nền |
| 21 | Bỏ field `menuSceneName = LobbyNT` của `PlayerHealth` (scene không có trong build) |

### Giai đoạn 5 — Hoàn thiện theo Bảng kế hoạch CP02 (chi tiết: `DoiChieu_KeHoach_CP02.md`)
| # | Việc | File |
|---|---|---|
| 22 | Boss Man1 giống Man2/Man3: xuất hiện từ đầu, máu = số nốt, đánh nốt trừ máu, sao 3 = hạ boss | `PianoKeySpawner`, `BossSpawner`, `BossHealth`, `Boss`, `EndGameManager1`, `PlayerController`, Man1.unity |
| 23 | Thanh máu boss tự tạo khi prefab chưa gán | `RuntimeBossHpBar.cs` (mới) |
| 24 | Chuyển scene mờ dần, chặn bấm đúp | `SceneFader.cs` (mới), `LevelFlow`, `Scene.cs`, `StoryController`, `ListCharController`, `LevelSelectionManager` |
| 25 | Nhiệm vụ ngày hoạt động (3 nhiệm vụ, +500 coin, reset mỗi ngày) | `DailyMissionData.cs` (mới), `PlayerData`, `PlayerDataManager`, `MissionPanelControl`, `EndGameBase` |
| 26 | Chau/Stephen không spam cảnh báo Animator | `AnimatorSafe.cs` (mới), `PlayerController*`, `PlayerHealth` |

Kiểm tra: biên dịch Assembly-CSharp + Editor bằng `dotnet build` **thành công, 0 lỗi**; Unity Editor cũng đã biên dịch lại thành công.

---

## 3. Checklist test khi quay lại (≈ 20 phút)

> Mở Unity → nếu hỏi tải lại scene chọn **Reload**. Chạy từ `LoadingScene`.

**Luồng**
- [ ] LoadingScene → MainMenu → SelectSong: coin, lượt chơi hiển thị đúng.
- [ ] Chọn màn 1 → SelectChar → Play: lượt giảm 1, vào StoryMan1 → Man1.
- [ ] Hết lượt (Admin panel đặt lượt = 0): bấm Play ở SelectSong báo hết lượt.

**Man1**
- [ ] Điểm mỗi nốt chỉ còn +100/+200/+300 (không còn cộng dồn 2 lần).
- [ ] Esc → pause: nhạc và nốt đứng yên; Esc lần nữa → tiếp tục **đúng nhịp**.
- [ ] Để chết: chỉ hiện Game Over, không hiện bảng thưởng, Esc không làm game chạy lại.
- [ ] Game Over → Replay: trừ 1 lượt, vào lại Man1 ngay.
- [ ] Hết bài: bảng kết thúc hiện; hàng sao 3 hiện số Miss đúng; nút Next ẩn nếu màn 2 còn khóa.

**Man2 / Man3**
- [ ] READY → GO: nhạc vào đúng lúc chữ GO tắt; nốt đầu bài không bị trễ.
- [ ] Pause giữa bài → tiếp tục: không lệch nhịp.
- [ ] Bảng kết thúc: sao 3 sáng khi hạ boss/tàu ngầm; coin cộng đúng.

**Menu**
- [ ] Nút Nhạc tắt nhạc nền ngay, tắt game mở lại vẫn giữ.
- [ ] Nút Âm thanh tắt tiếng nút + tiếng đánh nốt.
- [ ] Xem quảng cáo test → coin +500.

---

## 4. Việc còn lại / cần bạn quyết định

1. **Ngưỡng sao Man1** (`scoreForStar = 10000`) được chỉnh khi điểm còn bị nhân đôi → sau khi test nên giảm (gợi ý: chơi thử 1 lượt bình thường, lấy ~70% điểm đạt được).
2. **Man2/Man3 có thể thua không?** Hiện không có máu → luôn qua màn.
3. **Thắng màn có tự mở màn kế không?** Hiện chỉ mở bằng coin.
4. **Good/Great/Perfect đang tính theo combo**, không theo độ chính xác lúc bấm (xem Kế hoạch phát triển, mục A1).
5. `Tutorial.unity` chưa có trong Build Settings và còn dùng script đã mất.
6. Commit: chưa commit. Lưu ý **không commit** `.qldatn/tracker.json` và `Assets/Editor/QLDATNSceneTracker.cs` (file của tracker, đang bị git theo dõi nhầm).

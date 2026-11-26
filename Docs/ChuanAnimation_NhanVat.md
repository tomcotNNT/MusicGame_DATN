# Chuẩn animation nhân vật cho game bắt beat

Áp dụng cho Ramona (P1), Knives Chau (P2) và Stephen Stills (P3).
- Clip nằm ở `Assets/MusicGame/Animation/P1|P2|P3`.
- Clip được sinh bằng script `Docs/Tools/charanim.py` (`contact.py` cùng thư mục dùng để xuất ảnh tổng hợp frame có đánh số). Các bộ frame có thể cắt tiếp nằm ở mục 5.

## 1. Quy chuẩn thời gian

| Loại | Quy tắc | Lý do |
|---|---|---|
| **Đòn đánh (Attack1-3)** | **Frame đầu là frame chạm** (bỏ frame lấy đà). Frame chạm giữ 83–100 ms, sau đó 2–3 frame thu tay, mỗi frame 50–83 ms. **Tổng 0,23–0,37 s** | Người chơi bấm là thấy cú đánh ngay trong frame đó, không bị trễ cảm giác. Thu tay ngắn nên bấm liên tục vẫn kịp |
| Đòn liên tiếp | Any State → Attack, `Can Transition To Self` bật, `Transition Duration` 0, `Has Exit Time` tắt | Nốt dày vẫn đánh lại từ đầu được |
| **Né (Dodge)** | 4–5 frame lộn, mỗi frame 33 ms, frame cuối 67 ms. **Tổng 0,2 s** | Nhanh và dứt khoát, khớp với thời gian đổi lane (0,08 s) |
| **Trúng đòn (Hurt)** | 3 frame: giật 67 ms, **giữ 117 ms**, về 83 ms. Tổng 0,27 s | Đủ rõ để người chơi biết mình bị trúng, nhưng không cản lượt đánh kế tiếp |
| **Đứng (Idle)** | Lặp, tổng **0,5 s = 1 nhịp ở 120 BPM** (4–6 frame) | Nhân vật nhún theo nhịp, không rung loạn |
| Chạy (Run) | Lặp 0,4–0,53 s, 8 frame | Bước chân rơi vào nốt móc đơn |
| Giữ nốt (Hold) | Lặp, 100 ms/frame | Ổn định trong lúc giữ |
| **Chiêu đặc biệt (Special / Fever)** | 6–8 frame, 50–100 ms/frame. **Tổng ≈ 0,5 s** | Đủ "đã mắt" nhưng không che nốt kế tiếp |
| **Ăn mừng (Victory)** | Lặp, 100 ms/frame | Chạy khi bảng kết quả hiện và có ≥ 1 sao |
| Chết (Die) | 12 fps, ≈ 1,08 s, không lặp | Bằng thời gian chờ Game Over (1 s) |

**Thông số kỹ thuật chung:**
- Sample rate 60, nên mỗi bước là 16,7 ms. Mọi transition về trạng thái gốc: `Exit Time` 1, `Duration` 0.
- **Pivot:** cột thân người và mép chân của mọi frame trùng với frame Idle đầu tiên, để không bị giật hay trôi khi đổi frame. Frame có hiệu ứng to (nổ, lửa) đã được chỉnh tay.

## 2. Đã làm (cả 3 nhân vật)

| Trạng thái | Ramona (frame / tổng) | Chau | Stephen |
|---|---|---|---|
| Idle | 48–53 / 0,50 s | 41–44 / 0,47 s | 50–55 / 0,50 s |
| Attack1 | 352–356 / 0,28 s | 886–888 (đấm thẳng) / 0,23 s | 761, 762, 760 / 0,23 s |
| Attack2 | 681–684 / 0,27 s | 896–898 (đá) / 0,23 s | 764–766 / 0,23 s |
| Attack3 | 801–805 (búa) / 0,37 s | 590–592 (đấm lướt) / 0,25 s | 770–773 (đấm vệt) / 0,30 s |
| **Dodge** (mới) | 256–260 lộn tròn / 0,20 s | 436–440 / 0,20 s | 261–265 / 0,20 s |
| **Hurt** (mới) | 507, 508 / 0,27 s | 724, 725 / 0,27 s | 338–340 / 0,27 s |
| **Special** (mới) | 1073–1081 quả cầu lửa / 0,53 s | 940–945 lửa bốc / 0,47 s | 1090, 1076–1080 sóng năng lượng / 0,52 s |
| **AirAttack** (mới) | 1061–1066 lộn đá / 0,37 s | 928–931 đá bay / 0,28 s | 789–792 đá bay / 0,28 s |
| **Victory** (mới) | 741–745 / lặp | 876–881 / lặp | 715–720 / lặp |
| Hold, Die (Ramona) | chỉnh lại thời gian (trước đây Die chỉ dài 0,2 s) | giữ nguyên | giữ nguyên |

**Lỗi cũ đã sửa ở Ramona:**
- `Attacking`: 5 frame trong 0,08 s (mỗi frame chỉ hiện 1/60 s, gần như không thấy).
- `Attack3`: chỉ có 2 frame.
- `Die`: dài 0,2 s.
- `Idle`: 2 key cùng nằm ở thời điểm 0, nên mất 1 frame.

**Đã gắn vào code:**
- `PlayerHealth.TakeDamage` → `Hurt`.
- `PlayerController` (Man1):
  - Đánh khi đang trên không → `AirAttack`.
  - Bắn Fever → `Special`.
- `CaveMawSpawner` (Màn 4):
  - Đổi lane → `Dodge`.
  - Bị quái nổ trúng → `Hurt`.
- `EndGameBase` → `Victory` khi có ≥ 1 sao.

Mọi lệnh đều đi qua `AnimatorSafe`: controller nào chưa có trạng thái đó thì lệnh bị bỏ qua, không báo lỗi.

## 3. BẠN CẦN LÀM TRONG UNITY (1 lần)

1. **Nối trạng thái mới vào Animator.** Chọn menu **Tools > BeatRun > Thêm trạng thái Hurt - Dodge - Special - AirAttack - Victory**.
   - Lệnh này thêm 5 state, 5 trigger và các transition vào 6 controller: `Player`, `Player 1`, `Chau`, `Chau_Run`, `Stephen Stills_50`, `Stephen_Run`.
   - Bấm nhiều lần vẫn an toàn (đã có thì bỏ qua).
2. **Kiểm tra bằng mắt.** Chọn menu **Tools > BeatRun > Xem thử Animation Sprite**, chọn từng controller rồi xem từng clip ở tốc độ 0.25x, bật Onion.
3. **Chơi thử:**
   - **Man1:** nhảy rồi đánh (ra AirAttack), Fever (ra Special), bị trúng đòn (ra Hurt).
   - **Man4_Test:** đổi lane (ra Dodge).
   - **Thắng màn:** ra Victory.

## 4. Chỉnh tay khi thấy chưa ưng
- **Frame bị lệch hoặc giật:**
  - Mở sheet nhân vật → **Sprite Editor**, chọn đúng frame (tên ví dụ `Knives Chau_929`), rồi sửa **Custom Pivot**. Đưa chấm pivot về giữa thân, ngang mép chân của frame Idle.
  - Bảng ở cuối cửa sổ "Xem thử Animation Sprite" tô đỏ những frame có đáy lệch quá 2 px.
- **Đòn chưa đủ "đã tay":**
  - Mở clip trong **Window > Animation**, kéo key thứ 2 sang phải để **giữ frame chạm lâu hơn** (tối đa khoảng 120 ms).
  - Đừng thêm frame lấy đà vào đầu clip.
- **Run khớp BPM:** nếu bài nhanh hoặc chậm hơn 120 BPM, đặt `animator.speed = BPM / 120` cho trạng thái chạy. Chưa làm sẵn, vì mỗi màn lấy BPM từ một nguồn khác nhau.
- **Muốn đổi frame:** sửa bảng `CHARS` trong `Docs/Tools/charanim.py` rồi chạy lại lệnh dưới đây trong thư mục `Assets` (thay `charanim.py` bằng đường dẫn tới file). Clip cũ được ghi đè nhưng giữ nguyên GUID.
  ```
  py charanim.py
  ```

## 5. Frame còn trống có thể dùng thêm
| Nhân vật | Bộ frame | Gợi ý dùng |
|---|---|---|
| Chau | 145–168 lướt / chạy nhanh | Dash (Man1 tăng tốc) |
| Chau | 770–799, Stephen 577–598, Ramona 579–593 | Cột sáng biến hình: Fever bắt đầu hoặc hồi sinh |
| Chau | 841–844 hét | Combo mốc 50 hoặc 100 |
| Stephen | 738–742 giơ ngón cái | Perfect liên tiếp |
| Ramona | 675–683 chỉ tay, 691–694 lấp lánh | Taunt hoặc nhặt item |
| Cả 3 | Bộ ngã (Chau 724–751, Stephen 531–548, Ramona 530–546) + đứng dậy | Bị nổ trúng nặng |

> **Tránh dùng:** Stephen 704–735 (nhân vật chui vào ống xanh). Hình này giống tài sản của một game nổi tiếng khác. Ngoài ra, cả 3 sheet nhân vật đều là sprite rip, chỉ dùng cho đồ án học tập.

## 6. Bản đồ sử dụng animation theo màn (kiểm tra ngày 26/11/2026)

| Màn | Controller (Ramona / Chau / Stephen) | Script điều khiển | Trigger được gọi |
|---|---|---|---|
| Man1 (chạy, nhảy, boss) | `P1/Player`, `P2/Chau_Run`, `P3/Stephen_Run` (mặc định **Run**) | PlayerController + PlayerHealth + PlayerFever | IsGround, Jump, Attack1-3, **AirAttack** (đánh trên không), Sing (giữ nốt → Hold), **Special** (bắn Fever), **Hurt**, Die, Victory |
| Man2, Man5 (đánh quái 2 bên + boss) | `P1/Player 1`, `P2/Chau`, `P3/Stephen Stills_50` (mặc định **Idle**) | PlayerController2 | Attack1-3, Victory |
| Man3, Man4 (tàu ngầm, nhặt item trái/phải) | như Man2 | PlayerController3 (`PlayerControllerSub`) | Attack1-3, Victory |
| Man4_Test (né quái) | như Man2 | CaveMawSpawner điều khiển | **Dodge**, **Hurt**, Victory |
| Tutorial | chỉ Ramona `P1/Player` | PlayerController | như Man1 |

Các trạng thái chưa được màn nào gọi: Hurt, Die, Special, Hold ở Man2–Man5 (nhân vật trong các màn này không có PlayerHealth / Fever / giữ nốt).

## 7. Bộ đòn mở rộng cho Man2 – Man5 (đánh theo độ chính xác)

Mỗi nhân vật thêm **5 đòn**, nâng tổng số đòn lên **8 đòn**. Tất cả theo chuẩn "frame đầu là frame chạm", dài 0,22–0,32 s:

| Đòn | Hạng | Ramona | Chau | Stephen |
|---|---|---|---|---|
| Attack1 | nhẹ | túi xách (352–356) | đấm thẳng (886–888) | đấm thẳng (760–762) |
| Attack4 | nhẹ | đấm liên hoàn (1008–1011) | đấm nhanh (891–893) | đấm vệt (776–778) |
| Attack6 | nhẹ | quật túi vệt xanh (428–430) | đá xoay (964–967) | trượt đá thấp (858–860) |
| Attack2 | vừa | chỉ tay (681–684) | đá (896–898) | đấm (764–766) |
| Attack5 | vừa | đá vệt hồng (794–797) | đá vòng vệt chém (957–960) | đấm móc lên (862–864) |
| Attack8 | vừa | trượt đá (903–906) | đá cao (906–909) | vung tay vệt đỏ (1034–1037) |
| Attack3 | mạnh | vung búa (801–805) | đấm lướt (590–592) | đấm vệt (770–773) |
| Attack7 | mạnh | búa nện đất (869–872) | xoay người chém vòng (901–903) | đấm lao vệt (1098–1101) |
| Special | chiêu kết | quả cầu lửa | lửa bốc | sóng năng lượng |

### Cách game chọn đòn (`AttackPicker.cs`)

| Kết quả lần bấm | Đòn được chọn ngẫu nhiên trong nhóm |
|---|---|
| Bấm trượt / Good | **nhẹ**: Attack1, 4, 6 |
| Great | **vừa**: Attack2, 5, 8 |
| Perfect | **mạnh + vừa**: Attack3, 7, 2, 5, 8 |
| Combo chạm mốc 10, 20, 30… | **Special** |

- Không bao giờ lặp lại đúng đòn vừa đánh.
- Bấm càng chuẩn thì đòn càng "đã", nên người chơi tự thấy được phần thưởng cho việc bắt beat chuẩn.
- Code của Man2 (`PlayerController2`) và Man3/Man4 (`PlayerController3`) giờ **đánh trước rồi mới chọn animation**. Trước đây animation được gọi trước khi biết trúng hay trượt.
- Muốn đổi nhóm đòn hoặc mốc combo, sửa 3 mảng `Light`, `Medium`, `Heavy` và hằng `FinisherEvery` trong `AttackPicker.cs`.

### Bạn cần làm trong Unity (1 lần)
1. Chọn **Tools > BeatRun > Thêm trạng thái mới (Attack4-8, Hurt, Dodge, Special, AirAttack, Victory)**. Lệnh thêm 5 state, 5 trigger và các transition cho cả 6 controller. Những trạng thái đã có từ lần trước sẽ được bỏ qua.
2. Kiểm tra từng đòn bằng **Tools > BeatRun > Xem thử Animation Sprite** (chọn controller rồi chọn clip Attack4–8).
3. Chơi Man2 hoặc Man3 và để ý: bấm lệch nhịp ra đòn nhẹ, bấm Perfect ra búa hay đòn xoay, đủ 10 combo ra Special.
4. Nếu một đòn trông lệch: mở sheet nhân vật → **Sprite Editor**, chọn frame theo số ở bảng trên, sửa **Custom Pivot**.

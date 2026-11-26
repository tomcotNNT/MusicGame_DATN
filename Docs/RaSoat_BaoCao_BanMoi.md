# Rà soát báo cáo BeatRun (bản .docx mới nhất)

**Đối chiếu với:**
- Code nhánh `Feature/Ntam`, commit mới nhất `d411be3 "Boss màn 4"`.
- Mẫu `2. Mau bao cao.pdf`.

**Cách đọc:** mỗi mục ghi theo thứ tự
1. **Lỗi**: đoạn nào, câu nào ghi sai.
2. **Thực tế trong game**: cơ chế đúng theo code.
3. **Viết lại**: hướng dẫn kèm câu mẫu theo văn phong báo cáo.

**Mức độ:**
- 🔴 Sai với game. Hội đồng chạy thử sẽ thấy ngay.
- 🟡 Chưa đủ, chưa đồng bộ, hoặc lệch so với mẫu.
- ⚪ Lỗi trình bày.

---

## 0. Bảng sự thật dùng để đối chiếu

Mọi câu trong báo cáo phải khớp bảng này.

| Nội dung | Thực tế trong dự án |
|---|---|
| **Màn chơi trong bản build** | 10 scene: LoadingScene, MainMenu, SelectSong, SelectChar, StoryMan1–3, Man1–3 |
| **Màn đang phát triển (chưa build)** | Tutorial, Man4 (`Man4_Test`), Man5, StoryMan4–5. Man5 đang dùng tạm nhạc Màn 2 |
| **Nhãn đánh giá** | GOOD / GREAT / PERFECT / MISS, tính theo **chuỗi Combo**: 1–4, 5–9, từ 10. Không có mức Bad. Chưa chấm theo độ lệch thời gian (chế độ Timing có sẵn nhưng đang tắt) |
| **Điểm** | GOOD +100, GREAT +200, PERFECT +300. Miss không bị trừ điểm |
| **Thua cuộc** | Không có. Hệ thống máu và bảng Game Over có sẵn nhưng chưa có nguồn gây sát thương |
| **Sao** | Sao 1: Score ≥ 60% điểm tối đa. Sao 2: Combo ≥ 25% số nốt (tối đa 100). Sao 3: riêng từng màn (hạ Boss) |
| **Coin** | **Chỉ** nhận từ sao (200 / 500 / 1.000), nhiệm vụ ngày (500 mỗi nhiệm vụ) và quảng cáo thưởng (+500). **Không có đồng xu rơi trong màn chơi** |
| **Mở khóa** | Màn: Coin (Màn 2 = 2.500, Màn 3 = 5.000). Nhân vật: Coin (Stephen 1.500, Knives Chau 2.500) |
| **Gacha / Diamond** | Vòng quay Gacha **không có trong bản build** và không trả nhân vật. Diamond chưa có chỗ tiêu |
| **Lượt chơi** | Tối đa 20 lượt. Mỗi lần vào màn tốn 1 lượt. Hồi 1 lượt mỗi 5 phút. Xem quảng cáo +1 lượt |
| **Điều khiển** | 2 **nút tròn** ở hai góc dưới màn hình (không phải chia đôi nửa màn hình) |
| **Màn 1** | Nhân vật tự chạy trên 2 làn. Nút Trái chạm nốt, nút Phải lướt lên. Có nốt giữ (Hold), 5 loại vật phẩm nạp thanh Fever. Bấm 2 nút cùng lúc khi Fever đầy thì bắn Boss. Boss Maestro Vargo có thanh máu |
| **Màn 2** | Đứng giữa màn hình. Ma vật lao tới từ hai phía. Boss phù thủy |
| **Màn 3** | Đứng giữa màn hình. Tàu ngầm bắn đạn từ hai phía. Có vật phẩm rơi |
| **Màn 4 (đang làm)** | Cơ chế **NÉ**. Quái mẹ **Mother Maw** há miệng theo nhịp, nhả quái con (đầu Medusa) bay theo 2 làn trên/dưới rồi phát nổ. Đổi làn bằng nút Lên/Xuống. Né được thì cộng Combo và quái mẹ mất máu. Sao 3: hạ Mother Maw |
| **Hiệu ứng** | Global Light 2D, VFX khi trúng nốt / nốt giữ, rung camera, chữ COMBO gợn sóng và đổi màu ở mốc 5/15/25. **Không có** screen flash / bloom. **Không có** VFX hay font riêng cho từng nhân vật |
| **Nhân vật** | Mỗi nhân vật có sprite và bộ animation riêng: Idle, Run, Attack 1–3, Jump, Hold, Die |
| **Nhạc** | Màn 1: bản remix "2 Phút Hơn". Màn 2: nhạc Halloween của apalonbeats. Màn 3: nhạc riêng. **Đều không phải nhạc độc quyền** |
| **Hướng màn hình** | Player Settings đang cho xoay tự do, kể cả dọc. Cần khóa màn hình ngang |

---

# PHẦN 1 – GAME DESIGN DOCUMENT

## Trang bìa, Mục lục, Lời mở đầu

**Mục lục**
- 🟡 Chưa cập nhật. Phần 2 mới có mục 1, 2, trong khi mẫu yêu cầu mục 1–7.
  - Tên 8.3 trong mục lục khác tên trong thân bài.
  - Dòng 1.1 dính chữ "Giới thiệuGiới thiệu về tài liệu…".
  - Thiếu 8.5 và 8.6 theo mẫu.
- Viết lại: dùng *References → Table of Contents → Update Table* sau khi sửa xong các heading.

**Lời mở đầu, đoạn 2 (bắt đầu bằng "BeatRun được định hướng…")**

Các câu sai:
- 🔴 "hiệu ứng lóe sáng (screen flash / visual bloom) khi đạt Combo cao hoặc giữ nốt dài": game không có.
- 🔴 "5 màn chơi chính (kèm màn Tutorial)": bản hiện tại chơi được 3 màn.
- 🔴 "thu thập Coin quay Gacha hoặc dùng Diamond mở khóa": thực tế dùng Coin để mở khóa.
- 🔴 "VFX combo và font chữ riêng": không có.
- 🟡 "canh timing": game chấm theo Combo, không theo timing.

Câu mẫu:
> BeatRun được xây dựng theo phong cách 2D Pixel Art, sử dụng hệ thống ánh sáng 2D cùng các hiệu ứng phản hồi như chữ COMBO đổi màu theo mốc, hiệu ứng trúng nốt và rung màn hình. Trò chơi đưa người chơi vào hành trình của các thành viên Hội Mạo Hiểm Giả quanh vương quốc Asterisk, được thiết kế gồm 5 màn chơi và một màn hướng dẫn, mỗi màn có bài nhạc, bối cảnh và lối chơi riêng. Phiên bản hiện tại đã hoàn thiện ba màn đầu; Màn 4, Màn 5 và màn hướng dẫn đang trong giai đoạn phát triển. Người chơi điều khiển bằng hai nút cảm ứng Trái/Phải, tích lũy Coin sau mỗi màn để mở khóa màn chơi mới và hai nhân vật Stephen, Knives Chau, hướng tới mục tiêu đạt ba sao ở mọi màn mà không bị gián đoạn bởi cơ chế thua cuộc giữa bài.

---

## Mục 1. Giới thiệu chung

### 1.1. Giới thiệu
- ⚪ **Heading** bị dính: "1.1. Giới thiệuGiới thiệu về tài liệu (GDD - Game Design DocumeGiới nt)". Sửa thành "1.1. Giới thiệu", rồi một dòng in nghiêng "Giới thiệu về tài liệu GDD:".
- ⚪ Cụm "Mục tiêu dự án:" bị dính vào cuối đoạn đầu. Cần xuống dòng và để thành tiêu đề con in nghiêng.
- 🔴 **Đoạn "Hiện thực hóa hệ thống đa dạng chủ đề…"**: bỏ vế cuối "kết hợp hiệu ứng lóe sáng (screen flash / visual bloom)…".
- 🟡 **Đoạn "Hoàn thiện cấu trúc 5 màn…"**: đây là mục tiêu nên giữ được, nhưng thêm câu nêu hiện trạng.
  > Hoàn thiện cấu trúc 5 màn chơi chính và một màn hướng dẫn, mỗi màn có bài nhạc, cơ chế tương tác và Boss riêng. Đến thời điểm báo cáo, Màn 1–3 đã hoàn chỉnh; Màn 4–5 và màn hướng dẫn đang được phát triển.

### 1.2. Xác định phạm vi tài liệu
- ✅ Đoạn *Cơ chế cốt lõi* đã sửa đúng.
- 🔴 *Hệ thống màn chơi*, câu "Màn 1: nốt đi tới theo 2 làn trên/dưới…" là chưa đủ. Sửa thành:
  > Màn 1: nhân vật tự chạy qua thành phố Melodia trên hai làn; người chơi chạm nốt ở làn dưới và lướt lên làn trên. Màn 2 và Màn 3: nhân vật đứng giữa màn hình, đối tượng lao tới từ hai phía trái/phải. Màn 4: người chơi đổi làn trên/dưới để né quái vật phát nổ.
- 🔴 *Tài nguyên nghệ thuật*, câu "hiệu ứng lóe sáng (screen flash / visual bloom) khi đạt Combo cao". Thay bằng:
  > hiệu ứng ánh sáng Global Light 2D, hiệu ứng khi trúng nốt, rung màn hình và chữ COMBO đổi màu theo các mốc 5, 15, 25.

### 1.3. Nội dung chính của game (Elevator Pitch)
- 🟡 Mẫu yêu cầu **≤ 75 từ**. Đoạn hiện tại khoảng 130 từ.
- 🟡 "Knive Chau" chưa thống nhất với tên trong game là "Knives Chau".
- ⚪ Cuối đoạn thiếu dấu chấm.

Câu mẫu (≈ 70 từ):
> BeatRun là game nhịp điệu 2D Pixel Art trên điện thoại, chơi màn hình ngang. Người chơi vào vai một trong ba nhà mạo hiểm giả của vương quốc Asterisk, điều khiển bằng hai nút cảm ứng để đánh trúng các nốt nhạc sinh ra từ bài hát. Mỗi màn là một vùng đất với bài nhạc, lối chơi và Boss riêng, dẫn dắt người chơi lật tẩy âm mưu của tổ chức The Graves và trùm cuối Gigadeon.

### 1.4. Đối tượng người chơi hướng đến
- 🟡 Mẫu yêu cầu **độ tuổi, sở thích, các game tương tự họ chơi**. Báo cáo mới có sở thích và gu thẩm mỹ.
- Bổ sung:
  > Độ tuổi: 12–25, học sinh, sinh viên dùng điện thoại Android tầm trung. Thói quen: chơi các phiên ngắn 2–5 phút. Trò chơi tương tự họ thường chơi: Magic Tiles 3, Piano Tiles 2, Muse Dash, Geometry Dash.

---

## Mục 2. Tổng quan về game

### 2.1. Nội dung và mục tiêu (Game concept)
- 🔴 **Đoạn 1**, câu "mỗi nhân vật mang… hiệu ứng VFX combo và font chữ đặc trưng riêng". Thay bằng:
  > mỗi nhân vật có tạo hình và bộ hoạt ảnh riêng (đứng, chạy, ba kiểu tấn công, nhảy, giữ nốt và ngã).
- 🔴 **Đoạn 2**:
  - Câu cuối "giữ nốt dài và đạt chuỗi Combo cao sẽ kích hoạt screen flash / visual bloom" là sai.
  - Màn 4 cần mô tả theo cơ chế thật:
    > né bầy đầu Medusa do quái mẹ Mother Maw nhả ra trong The Mysterious Cave.
- 🔴 **Đoạn 3**:
  - Câu "đạt mức Score tối ưu để mở khóa các chặng tiếp theo, thu thập tài nguyên phát triển nhân vật" là sai. Game không có nâng cấp nhân vật, và màn được mở bằng Coin.
  - Câu mẫu:
    > Mục tiêu cuối cùng là hoàn thành các màn chơi, đạt ba sao ở mỗi màn, tích lũy Coin để mở khóa màn mới và các nhân vật, đồng thời ngăn chặn âm mưu của The Graves.

### 2.2. Thể loại
- 🟡 "Action-Runner / Hành động đa không gian": chỉ Màn 1 là chạy (runner). Đề xuất viết lại:
  > Rhythm-Action 2D (nhịp điệu kết hợp hành động): Màn 1 dạng chạy cảnh (runner), Màn 2–3 dạng đánh chặn đối tượng từ hai phía, Màn 4 dạng né tránh theo làn.

### 2.3. Bối cảnh
- ✅ Nội dung cốt truyện ổn.
- ⚪ "trong lĩnh vực" sửa thành "trong phạm vi trận pháp".

### 2.4. Cấu trúc, cách chơi
- 🟡 Màn 4 viết lại:
  > Quái mẹ Mother Maw há miệng theo nhịp nhạc và nhả quái con bay theo làn trên hoặc làn dưới. Người chơi đổi làn để né trước khi quái con phát nổ; mỗi lần né thành công làm quái mẹ mất máu.
- 🔴 Đoạn cuối, cụm "xử lý nốt đại dương và thu thập các vật phẩm hỗ trợ (Buff, Coin)":
  - "Nốt đại dương" thực chất chỉ là hình vỏ sò của nốt ở Màn 3.
  - Game không có Coin rơi trong màn.
  - Câu mẫu:
    > Người chơi kết hợp thao tác chạm, giữ nốt dài (Màn 1), đổi làn (Màn 1 và Màn 4) và nhặt vật phẩm (Màn 1 và Màn 3).

### 2.5. Nhân vật chính, số lượng người chơi
- 🔴 Bỏ "hiệu ứng VFX khi đạt combo và bộ font chữ đặc trưng".
- 🟡 Bổ sung:
  > Ramona được mở sẵn; Stephen và Knives Chau mở khóa bằng Coin. Cả ba có cùng thao tác điều khiển, chỉ khác về ngoại hình và hoạt ảnh.

---

## Mục 3. Cách chơi, mục tiêu, tiến triển

- 🟡 **Đánh số lệch so với mẫu.** Mẫu có 3.1 Mục tiêu, 3.2 Tiến triển, **3.3 Sự tiến triển của câu chuyện (Progression)**, 3.4 Cân bằng độ khó. Báo cáo thiếu 3.3 câu chuyện, và mục cân bằng đang đánh số 3.3.
- Cách sửa: thêm mục 3.3 mới, đổi mục cân bằng thành 3.4.
- Nội dung mục 3.3 mới: chuỗi hành động của người chơi từ đầu đến cuối.
  > Mở game → chọn màn → chọn nhân vật → đọc truyện tranh dẫn chuyện → chơi màn → nhận sao và Coin → mở màn kế tiếp → … → đánh bại Gigadeon ở Màn 5.

### 3.1. Mục tiêu chính và phụ
- 🔴 *Mục tiêu chính*, câu thứ hai "tối ưu Score… để mở khóa các màn tiếp theo". Sửa thành:
  > đạt đủ ba sao để nhận nhiều Coin nhất, dùng Coin mở khóa màn tiếp theo.
- 🔴 *Mục tiêu phụ* cần viết lại toàn bộ.
  - Lỗi:
    - Coin không "xuất hiện trong màn chơi".
    - Gacha và "nạp năng lượng" không có.
    - Hold không kích hoạt screen flash.
    - "Buff" thực tế là vật phẩm nạp thanh Fever.
  - Câu mẫu:
    > **Thu thập vật phẩm (Màn 1):** nhặt vật phẩm để nạp thanh Fever, khi đầy bấm đồng thời hai nút để bắn đạn vào Boss.
    > **Giữ nốt dài:** giữ nút đến hết độ dài nốt để duy trì Combo.
    > **Hoàn thành nhiệm vụ ngày:** ba nhiệm vụ mỗi ngày (hoàn thành 3 màn, đạt 100 PERFECT, đạt Combo 50), mỗi nhiệm vụ thưởng 500 Coin.
    > **Thu thập thành tựu:** mở khóa thành tựu dựa trên tổng số lần GOOD/GREAT/PERFECT tích lũy.

### 3.2. Sự tiến triển trong game
- ✅ Nội dung đã đúng.
- 🟡 Câu đầu viết "như ở Bảng 3.1", nhưng bảng chưa được chèn: hiện chỉ có hai gạch đầu dòng giá mở khóa và thiếu dòng thưởng sao. Cần chèn **Bảng 3.1** gồm thưởng 1/2/3 sao, giá Màn 2/3, giá Stephen/Knives Chau.

### 3.3 → đổi thành 3.4. Cân bằng độ khó
- ✅ Nội dung đúng.
- 🟡 Bảng Combo chưa có tên. Đặt "Bảng 3.2. Nhãn đánh giá theo chuỗi Combo", hoặc bỏ bảng và dẫn chiếu sang Bảng 4.1 để không lặp.
- 🟡 Đoạn cuối "Thông số độ khó tách khỏi code… LevelData… không phải sửa code" là ngôn ngữ kỹ thuật, nên chuyển sang Phần 2. Ở GDD thay bằng:
  > Các thông số độ khó (tốc độ nốt, mật độ nốt, độ lệch thời gian) được cấu hình riêng cho từng màn, cho phép điều chỉnh nhanh sau mỗi đợt chơi thử.

---

## Mục 4. Quy tắc và cơ chế vận hành

### 4.1. Quy tắc cơ bản
- ✅ Phần trúng/trượt và Bảng 4.1 đã đúng.
- 🔴 Câu "Mỗi màn có các đối tượng tương tác riêng, nhưng cách tính trúng – trượt ở mọi màn là như nhau:" kết thúc bằng dấu hai chấm nhưng **Bảng 4.2 bị thiếu**. Chèn Bảng 4.2 (đối tượng và thao tác từng màn) như bản đã gửi, thêm một hàng Màn 4:
  > Màn 4 (đang phát triển) | Quái con bay theo hai làn và phát nổ | Đổi làn trên/dưới để né; né thành công cộng Combo và trừ máu quái mẹ.
- 🟡 Ở Màn 4, **Miss = đứng đúng làn bị nổ**. Câu "Trượt: để nốt trôi qua…" cần thêm vế cho Màn 4.

### 4.2. Hệ thống kinh tế và phần thưởng
- 🔴 **Sai toàn bộ đoạn 2 (Gacha).** Viết lại:
  > *Tiền tệ:* Coin là tiền tệ chính, nhận được từ kết quả màn chơi (200 / 500 / 1.000 Coin cho 1 / 2 / 3 sao), nhiệm vụ hằng ngày (500 Coin mỗi nhiệm vụ) và quảng cáo thưởng (500 Coin mỗi lần xem). Diamond là tiền tệ cao cấp, nhận từ gói nạp, được dự trữ cho các tính năng mở rộng.
  > *Chi tiêu:* Coin dùng để mở khóa màn chơi (Màn 2: 2.500, Màn 3: 5.000) và nhân vật (Stephen: 1.500, Knives Chau: 2.500). Trò chơi không có cửa hàng vật phẩm.
  > *Lượt chơi:* mỗi lần vào màn tiêu tốn một lượt, tối đa 20 lượt, tự hồi một lượt sau mỗi 5 phút; khi hết lượt, người chơi có thể xem quảng cáo để nhận thêm một lượt.
  > *NPC:* trò chơi không có NPC tương tác; cốt truyện được dẫn dắt bằng truyện tranh trước mỗi màn.
- 🟡 Tên mục theo mẫu là "Hệ thống kinh tế, sự tương tác với NPCs". Nên giữ chữ "NPC" và ghi rõ là không có NPC.

### 4.3. Cách người chơi tương tác và điều khiển
- 🔴 "2 khu vực điều khiển độc lập ở **nửa trái và nửa phải màn hình**" là sai. Thực tế là **hai nút tròn** ở góc dưới trái và góc dưới phải.
- 🟡 Mẫu yêu cầu mô tả **di chuyển (nhảy, chạy, chiến đấu)**. Viết lại theo từng màn:
  > *Màn 1:* nhân vật tự chạy sang phải; nút Phải giúp nhân vật lướt (nhảy) lên làn trên, nút Trái đánh nốt ở làn dưới, giữ nút với nốt giữ; bấm cả hai nút cùng lúc khi thanh Fever đầy để bắn đạn.
  > *Màn 2–3:* nhân vật đứng giữa màn hình, chạm nút bên nào thì nhân vật tung đòn tấn công về phía đó (ba kiểu đòn luân phiên).
  > *Màn 4:* hai nút được xoay thành Lên/Xuống để nhân vật đổi làn né đòn.
  > *Tạm dừng:* nút PAUSE ở góc trên phải; khi tạm dừng, nhạc và nốt cùng dừng và tiếp tục đúng vị trí.

### 4.4. Đối tượng có thể tương tác
- 🔴 **Đoạn 1** sai: "nốt đôi", "nốt đại dương", "Buff", "Coin dùng cho Gacha hoặc nạp thanh năng lượng". Viết lại:
  > nốt chạm, nốt giữ (Màn 1), vật phẩm nạp Fever (Màn 1), vật phẩm rơi (Màn 3), đạn tàu ngầm (Màn 3), quái con phát nổ (Màn 4).
- 🔴 **Đoạn 2** sai: "VFX độc quyền khi đạt Combo, font chữ đặc trưng". Viết lại:
  > Mỗi nhân vật có bộ hoạt ảnh riêng; hiệu ứng phản hồi (nhãn đánh giá, chữ COMBO, hiệu ứng trúng nốt) dùng chung cho cả ba nhân vật.
- 🟡 **Đoạn 3** cần thêm cơ chế Boss:
  > Mỗi Boss có thanh máu bằng tổng số nốt của bài; mỗi nốt trúng làm Boss mất một phần máu. Màn 4 thay Boss bằng quái mẹ Mother Maw.

---

## Mục 5. Đồ họa và âm thanh

### 5.1. Phong cách đồ họa
- 🔴 Đoạn 2 (screen flash / visual bloom) là sai. Thay bằng:
  > Hệ thống hiệu ứng phản hồi gồm: chữ COMBO gợn sóng và đổi màu ở các mốc 5, 15, 25; nhãn GOOD/GREAT/PERFECT/MISS bay lên trên đầu nhân vật kèm điểm cộng; hiệu ứng tia sáng khi trúng nốt đơn và nốt giữ; rung màn hình khi bị tấn công hoặc phát nổ; ánh sáng Global Light 2D tạo không khí riêng cho từng màn.

### 5.2. Góc nhìn
- ✅ Đúng. Có thể nói rõ hơn:
  > Màn 1 là góc nhìn cuộn ngang (side-scrolling), Màn 2–4 là camera cố định.

### 5.3. Hệ thống âm thanh
- 🔴 "Các bản nhạc **độc quyền**" là sai. Màn 1 dùng bản remix "2 Phút Hơn", Màn 2 dùng nhạc Halloween của apalonbeats, đều là nhạc có sẵn. Ghi đúng nguồn và nêu vấn đề bản quyền (rất dễ bị hội đồng hỏi):
  > Nhạc nền được tuyển chọn từ các nguồn có sẵn, phù hợp không khí từng màn, và được chuyển thành dữ liệu MIDI để sinh nốt. Trước khi phát hành thương mại, nhóm sẽ thay bằng nhạc có giấy phép sử dụng.
- 🔴 "tiếng bass… khi thu thập… **đồng xu (Coin)**": game không có Coin trong màn. Chỉ giữ "khi nhặt vật phẩm".
- 🟡 Bổ sung:
  > Người chơi bật/tắt riêng nhạc nền và âm thanh hiệu ứng trong phần Cài đặt; toàn bộ âm thanh tự dừng khi tạm dừng game.

### 5.4. Phong cách âm nhạc
- 🟡 Nêu cụ thể thể loại từng màn:
  > Màn 1: EDM/remix sôi động. Màn 2: nhạc Halloween u ám. Màn 3: nhạc nền đại dương.
- 🟡 Màn 4–5 chưa có nhạc riêng.

---

## Mục 6. Cốt truyện và nhân vật

### 6.1. Back story
- ⚪ Còn dòng đỏ **"*** Thiếu hình ảnh"**. Xóa dòng này và chèn hình (xem mục Đề xuất hình ảnh).

### 6.2. Main plot
- ✅ Ổn.
- 🟡 Màn 4 nên khớp với tên quái trong game:
  > quái mẹ Mother Maw nhả bầy đầu Medusa phát nổ.
- 🟡 Nên nêu cách truyện được kể trong game:
  > Mỗi màn mở đầu bằng một chuỗi trang truyện tranh (scene StoryMan).

### 6.3. Nhân vật chính và phụ
- 🔴 Câu trong ngoặc "(các nhân vật có thể mở khóa qua quay Gacha bằng Coin hoặc mở trực tiếp bằng Diamond… VFX… font chữ)" là sai. Thay bằng:
  > (Ramona được mở sẵn; Stephen và Knives Chau mở khóa bằng Coin tại màn hình chọn nhân vật)
- 🟡 Mẫu yêu cầu cả **ngoại hình** và **kỹ năng**.
  - Ngoại hình: thêm 1 câu cho mỗi nhân vật dựa trên sprite (màu tóc, trang phục).
  - Kỹ năng: ghi rõ cả ba có lối chơi giống nhau, khác ở hoạt ảnh. Ví dụ Stephen có đòn đánh bằng đàn guitar, Knives Chau dùng dao.
- 🟡 Tên "Knive Chau" → "Knives Chau", sửa ở toàn bộ báo cáo.

### 6.4. Các kẻ thù chính
- ⚪ "Maestri Vargo" → "Maestro Vargo"; "nhà thở cổ" → "nhà thờ cổ".
- 🔴 **Bảng còn trống Màn 3, 4, 5.** Điền:

| Màn | Kẻ thù | Mô tả |
|---|---|---|
| Màn 1 | Maestro Vargo | Nhạc trưởng đánh cắp âm thanh của Melodia. Là Boss có thanh máu, giảm dần theo số nốt người chơi đánh trúng |
| Màn 2 | Phù thủy hắc ám và ma vật triệu hồi | Ma vật lao tới từ hai phía; phù thủy đứng sau nghi thức, mất máu theo số nốt trúng |
| Màn 3 | Tàu ngầm bí ẩn | Bắn đạn từ hai phía theo nhịp nhạc để cản người chơi tiếp cận kho báu |
| Màn 4 | Mother Maw và bầy đầu Medusa | Quái mẹ khổng lồ nửa thân ẩn trong vách hang, há miệng theo nhịp và nhả quái con bay tới rồi phát nổ |
| Màn 5 | Gigadeon | Thủ lĩnh The Graves, trùm cuối (đang phát triển) |

### 6.5. Thiết kế hình ảnh
- 🔴 Đoạn 1 có "VFX khi đạt combo cao cùng screen flash…". Sửa theo 5.1.
- 🟡 Mẫu yêu cầu **chi tiết hình ảnh nhân vật, quái vật/boss, bản đồ**, nhưng mục này chưa có hình nào. Cần chèn (xem mục Đề xuất hình ảnh, nhóm 6.5).
- 🟡 Bổ sung thông số:
  > Sprite nhân vật cắt từ sprite sheet, tỷ lệ 32 pixel/đơn vị, lọc Point để giữ nét pixel.

---

## Mục 7. Thế giới game và cấp độ

### 7.1. Giao diện và cảm giác
- ⚪ **Bảng 7.1** đang là **ảnh chụp màn hình một bảng**, bên dưới còn một bảng trống 6 dòng. Cần dựng lại bằng bảng Word thật với 3 cột: Màn chơi, Bối cảnh, Tông màu. Xóa bảng trống.

### 7.2. Các địa điểm
- ✅ Nội dung ổn.
- ⚪ Số hình "Hình 7.11 … 7.15" sai quy ước. Đổi thành Hình 7.1 – 7.5.
- 🟡 Hình 7.14 và 7.15 hiện chỉ là ảnh nền trống. Nên thay bằng ảnh có nhân vật và quái (khi Màn 4 chạy được).

### 7.3. Các cấp độ
- 🔴 **Đoạn 2**:
  - Câu "Tutorial… căn timing… để đạt Perfect, Good, Great, Miss" là sai: nhãn tính theo Combo, không theo timing.
  - Tutorial chưa có trong bản build.
  - Câu mẫu:
    > Màn hướng dẫn (đang hoàn thiện) giúp người chơi làm quen với hai nút điều khiển, cách chạm và giữ nốt, cách nhặt vật phẩm và ý nghĩa của các nhãn đánh giá.
- 🔴 **Đoạn 3** sai ba điểm:
  - "mở khóa dựa trên… Score tối thiểu".
  - "Gacha".
  - "Diamond".
  - Câu mẫu:
    > Các màn được mở khóa tuần tự bằng Coin tích lũy từ kết quả chơi. Người chơi có thể chơi lại các màn đã mở để cải thiện số sao, phá kỷ lục điểm và tích lũy Coin mở khóa đủ ba nhân vật.
- 🟡 Mẫu yêu cầu phân loại **tutorial level / main level / optional level**. Thêm bảng:
  > Tutorial: hướng dẫn. Màn 1–5: màn chính. Màn phụ: chưa có.

---

## Mục 8. Tối ưu, kế hoạch, kiểm thử, phát hành

- 🟡 **Tên mục 8** khác mẫu. Mẫu là "Tối ưu hóa cách chơi (Game play) và quản lý độ khó. Kế hoạch phát triển game, kiểm thử bảo đảm chất lượng và phát hành game ra thị trường". Nên dùng đúng tên này.
- 🔴 **Thiếu 8.5 Kiểm thử và bảo đảm chất lượng** và **8.6 Chiến dịch phát hành game**. Mẫu bắt buộc có (gợi ý nội dung ở cuối mục này).

### 8.1. Tối ưu luồng chơi
- 🔴 Đoạn 3 sai: "ngưỡng Score… quay Gacha bằng Coin… Diamond". Sửa thành:
  > Bảng tổng kết hiển thị điểm, ba điều kiện sao và số Coin nhận được; nút "Màn tiếp" chỉ xuất hiện khi màn kế đã được mở khóa.
- 🟡 Bổ sung các tối ưu đã làm thật:
  > Hiệu ứng mờ dần khi chuyển cảnh. Nhạc và nốt dừng hoặc tiếp tục chính xác khi tạm dừng hoặc khi ứng dụng bị đưa xuống nền. Nút điều khiển của ba nhân vật được đặt cùng vị trí trong mỗi màn.

### 8.2. Cân bằng theo phản hồi
- ✅ Đã sửa đúng.

### 8.3. Tinh chỉnh cơ chế
- 🟡 Heading khác mẫu. Mẫu là "Tối ưu hóa cơ chế chiến đấu, hệ thống kỹ năng, môi trường (Mechanics refinement)".
- 🔴 Đoạn 2 có câu "hiệu ứng VFX riêng của từng nhân vật" là sai. Sửa thành:
  > Hiệu ứng phản hồi dùng chung được điều chỉnh độ nổi bật tăng dần theo chuỗi Combo (đổi màu ở mốc 5, 15, 25).
- 🟡 Có thể thêm phần đã làm thật:
  > Đồng bộ hoạt ảnh nhân vật: chỉnh tâm xoay từng khung hình để Stephen và Knives Chau không bị nảy khi chạy.

### 8.4. Kế hoạch phát triển
Các hàng trong bảng ghi sai so với dự án:

| Hàng | Ghi | Sửa |
|---|---|---|
| 1.4 | "kinh tế Gacha" | "kinh tế Coin, mở khóa màn và nhân vật" |
| 2.1 | "bộ font chữ riêng cho 3 nhân vật" | "sprite sheet và animation 3 nhân vật" |
| 3.3 | "phán đoán (Perfect, Good, Great, Miss)" | "nhãn đánh giá theo chuỗi Combo (Good, Great, Perfect, Miss)" |
| 3.4 | "nốt đôi… nốt đại dương… Buff, Coin" | "nốt chạm, nốt giữ, vật phẩm Fever (Màn 1), vật phẩm rơi (Màn 3)" |
| 3.5 | Tutorial: "Hoàn thành" | "Đang thực hiện" (chưa có trong bản build) |
| 4.2 | "mở khóa… dựa trên điều kiện Score tối thiểu" | "mở khóa màn bằng Coin" |
| 4.4 | "vòng quay Gacha nhân vật & nạp thanh năng lượng" | "hệ thống Coin/Diamond, lượt chơi, mở khóa nhân vật bằng Coin, quảng cáo thưởng" |
| 5 | Tình trạng "Thực hiện" | "Đang thực hiện" cho thống nhất |
| 5.1 | "screen flash / visual bloom & VFX combo riêng 3 nhân vật" | "Global Light 2D, VFX trúng nốt, hiệu ứng chữ COMBO, rung camera" |
| 5.5 | Màn 4 "quái vật một đầu" | "Mother Maw và bầy đầu Medusa, cơ chế né theo làn"; tình trạng: Đang thực hiện |
| 6.2 | "ngưỡng Score mở màn và tỷ lệ Coin/Gacha" | "ngưỡng sao, Coin thưởng, giá mở khóa" |
| 6.5 | "GDD (200 trang)" | bỏ "200 trang" |

Thêm các hàng còn thiếu:
- Nhiệm vụ ngày và thành tựu.
- Quảng cáo AdMob.
- Lưu dữ liệu JSON.
- Công cụ Editor (MIDI Chart Tool, Animator Tool).

### 8.5 và 8.6 (thêm mới)
- **8.5 Kiểm thử:** lập kế hoạch kiểm thử theo giai đoạn: chức năng từng màn → luồng scene → thiết bị Android. Kèm bảng lỗi đã gặp và cách sửa, ví dụ:
  - Cộng điểm hai lần.
  - Lệch nhạc khi tạm dừng.
  - Màn hình đen khi chuyển scene.
  - Lỗi font TextMeshPro.
  - Nút điều khiển nằm ngoài màn hình.
  - Nhân vật nảy khi chạy.
- **8.6 Phát hành:** teaser (video 15 giây Màn 1), trailer (3 màn), bản beta (APK nội bộ hoặc Google Play Internal Testing), bản release (Google Play), mỗi bước kèm mốc thời gian.

---

# PHẦN 2 – TÀI LIỆU KỸ THUẬT

## Mức độ đáp ứng so với mẫu

| Mục theo mẫu | Yêu cầu của mẫu | Hiện trạng báo cáo |
|---|---|---|
| 1. Phân tích ý tưởng (SWOT) | 4 ý S/W/O/T | 🟡 Có, nhưng chứa thông tin sai (xem 1) |
| 2. Thiết kế game | Game loop, **trạng thái game**, mô hình Scene; thể loại; đồ họa; UI/UX; gameplay; âm thanh | 🟡 Có 6/6 ý, nhưng thiếu trạng thái game và sơ đồ scene, nhiều câu sai |
| 3. Công nghệ & hệ thống | Engine, yêu cầu hệ thống, kiến trúc code, sơ đồ lớp, dịch vụ | 🔴 **Chưa có** |
| 4. Thực hiện dự án | Tổ chức nhóm, quy trình, Git, plugin, **Use Case Diagram**, **ERD** | 🔴 **Chưa có** |
| 5. Kiểm thử game | Kế hoạch test, beta test, công cụ, **buglist** | 🔴 **Chưa có** |
| 6. Triển khai & phát hành | Hướng dẫn build, asset store, hướng dẫn chơi, link APK | 🔴 **Chưa có** |
| 7. Phụ lục | Hình minh họa, QR video demo, link GitHub/tải game | 🔴 **Chưa có** |

## 1. Phân tích ý tưởng (SWOT)
- 🟡 **Hình thức:** mẫu trình bày theo 4 gạch đầu dòng S/W/O/T. Báo cáo viết thành 5 đoạn văn. Nên chuyển thành **bảng SWOT 2×2** hoặc 4 tiêu đề con in đậm.
- 🔴 **Điểm mạnh:** bỏ "screen flash / visual bloom, VFX combo… font chữ riêng". Thay bằng các điểm mạnh có thật:
  - Nốt sinh tự động từ MIDI bằng bộ đọc tự viết.
  - Nhạc đồng bộ theo đồng hồ âm thanh (dspTime) nên không lệch khi FPS dao động.
  - Mỗi màn một lối chơi nhưng chỉ dùng chung hai nút.
  - Có công cụ Editor riêng.
- 🔴 **Thách thức:** bỏ "hệ thống quay Gacha (Coin) / mở khóa bằng Diamond". Thêm:
  - Bản quyền nhạc.
  - Độ trễ âm thanh khác nhau giữa các máy Android.
- 🟡 **Điểm yếu:** thêm thực tế:
  - Màn 4–5 và Tutorial chưa xong.
  - Hay xung đột scene khi hợp nhất nhánh Git.
  - Chưa có máy chủ và bảng xếp hạng.
- Bản SWOT viết đúng đã có ở `Docs/BaoCao_Phan2_TaiLieuKyThuat.md`, Phần B, mục 1.

## 2. Thiết kế game
- 🔴 **Mô hình Scene tổng quát:**
  - Lỗi: câu "màn hình chọn nhân vật (**tích hợp Gacha** và mở khóa)" sai, và chưa liệt kê tên scene.
  - Sửa: thay bằng bảng 10 scene trong bản build và luồng `LoadingScene → MainMenu → SelectSong → SelectChar → StoryManN → ManN → Kết quả`.
  - Thêm **sơ đồ luồng scene** (hình).
- 🔴 **Core Game Loop:**
  - Bước 4 "Hệ thống chấm **độ chính xác** (Perfect, Good, Great, Miss)" → "gắn nhãn theo chuỗi Combo".
  - Bước 5 "đạt ngưỡng Score… mở khóa màn… Coin và Diamond quay Gacha" → "quy đổi sao thành Coin; dùng Coin mở khóa màn và nhân vật".
  - Bước 1 thiếu "tốn 1 lượt chơi".
- 🔴 **Thiếu "Trạng thái game"** (mẫu yêu cầu). Thêm bảng: Đếm ngược → Đang chơi ⇄ Tạm dừng → Kết thúc bài → Bảng kết quả (Game Over có sẵn nhưng chưa dùng).
- 🔴 **Tài nguyên đồ họa:**
  - Mục "Nhân vật chính" có "VFX combo và font chữ riêng".
  - Mục "Nốt nhạc & vật phẩm" có "nốt đôi, nốt đại dương, nhạc cụ/vật phẩm nhiệm vụ, Buff, Coin".
  - Mục "Background" có "screen flash / visual bloom".
  - Màn 4 sửa thành Mother Maw và đầu Medusa.
- 🔴 **UI/UX:**
  - "danh sách màn chơi mở khóa theo **Score**" → "mở khóa bằng Coin".
  - "Màn hình nhân vật & Gacha… font chữ… quay Gacha bằng Coin hoặc mở khóa bằng Diamond" → "màn hình chọn nhân vật, hiện giá mở khóa bằng Coin trên nút Play".
  - **Vị trí HUD sai:**
    - SCORE thực tế ở **góc trên phải** cạnh nút PAUSE, không phải góc trên trái.
    - Góc trên trái là **ảnh và tên nhân vật**.
    - COMBO hiện **cạnh nhân vật**, không phải chính giữa phía trên.
    - Ảnh 4.1 trong chính báo cáo cũng cho thấy điều này.
  - "nhãn phản hồi phán đoán ( / Perfect / Good / Great / Miss)": thừa dấu "( /".
  - Thiếu các màn hình thật đang có ở SelectSong: Nhiệm vụ ngày, Thành tựu, Cài đặt, Lượt chơi, Bảng nạp, Xem quảng cáo.
- 🔴 **Gameplay:**
  - Đoạn *Luật chơi* có "chạm/giữ đúng nhịp… Perfect, Good, Great… ăn Coin, Buff… nốt đại dương". Viết lại theo Bảng 4.1 và 4.2.
  - ⚪ Dòng "Điều kiện thắng / thua (Win / Lose Condition):" bị **lặp 2 lần**.
  - ✅ Phần Win/Lose còn lại đã đúng.
  - Level Design: Màn 4 sửa theo cơ chế né.
  - Ghi chú trạng thái: Tutorial, Màn 4, Màn 5 đang phát triển.
- 🟡 **Âm thanh:**
  - "biên tập chuẩn theo nhịp beat MIDI": nên nói đúng là "nốt được sinh từ file MIDI của bài".
  - "Perfect/Good" thiếu Great.
  - "đồng xu Coin" bỏ.
  - Thêm cài đặt bật/tắt BGM/SFX.

## 3 → 7. Các mục còn thiếu (bắt buộc theo mẫu)

| Mục | Nội dung cần viết | Nguồn đã có sẵn |
|---|---|---|
| **3. Công nghệ & hệ thống** | Unity 6000.3.24f1, URP 2D, Input System, TextMeshPro, Cinemachine, AdMob; yêu cầu thiết bị Android API 25+, IL2CPP; kiến trúc ScriptableObject + Singleton + Event + Template Method; bảng module/lớp; dịch vụ: AdMob (có), IAP (chỉ giao diện), Firebase/Photon (không dùng) | `Docs/BaoCao_Phan2_TaiLieuKyThuat.md`, Phần B, mục 3 (chép được ngay) |
| **4. Thực hiện dự án** | Vai trò 5 thành viên (khớp bảng 8.4); quy trình Scrum/Agile theo tuần, mỗi thứ Sáu test trên điện thoại; Git + GitHub Desktop, mỗi người một nhánh, hợp nhất vào `Feature/Ntam`; cấu trúc thư mục `Assets/MusicGame/Scripts/{TTAM, LONG, NTAM}`, `Scenes/TTAM`; plugin: URP, Cinemachine, TextMeshPro, Google Mobile Ads | — |
| **4. Use Case Diagram** | Tác nhân: Người chơi, Hệ thống quảng cáo. Use case: Chọn màn, Mở khóa màn, Chọn nhân vật, Mở khóa nhân vật, Chơi màn (gồm Tạm dừng, Xem kết quả), Nhận nhiệm vụ ngày, Xem thành tựu, Xem quảng cáo nhận thưởng, Cài đặt âm thanh. **Không có** Đăng nhập/Đăng ký và Bảng xếp hạng (ghi rõ "không áp dụng") | — |
| **4. ERD** | Game lưu một file JSON, không có CSDL. Vẽ sơ đồ thực thể theo `PlayerData`: Player 1–1 Currency, 1–1 Progression (danh sách màn đã mở), 1–n LevelRecord (levelId, highScore, maxCombo), 1–1 PlayTurn, 1–1 ComboStats, 1–1 DailyMission, 1–n Achievement (đã nhận), 1–1 Settings; danh sách nhân vật đã mở | Bảng "Cấu trúc file lưu" trong `BaoCao_Phan2_TaiLieuKyThuat.md` |
| **5. Kiểm thử** | Kế hoạch: kiểm thử chức năng từng màn → luồng scene → thiết bị thật (Samsung Galaxy, Device Simulator S9). Công cụ: Unity Console, Profiler, Device Simulator, logcat. **Bảng buglist**: Lỗi / Nguyên nhân / Cách sửa / Trạng thái (xem gợi ý ở 8.5) | `Docs/LoTrinh_SuaLoi.md`, `Docs/BanGiao_CuoiNgay.md` |
| **6. Triển khai** | Build Android: File → Build Profiles → Android → IL2CPP, ARM64, khóa màn hình ngang, đổi tên gói; icon và splash; hướng dẫn chơi (2 nút, giữ nốt, Fever); link APK hoặc Google Drive | — |
| **7. Phụ lục** | Ảnh chụp các màn hình, sơ đồ luồng scene, sơ đồ lớp, QR video demo, link GitHub | — |

**Lỗi cấu hình phải sửa trước khi build** (nên ghi luôn vào mục 6):
- Player Settings đang cho xoay dọc: cần khóa hướng ngang.
- Tên gói `com.DefaultCompany.urp_2d`: cần đổi.
- Quảng cáo đang dùng ID thử nghiệm: cần thay ID thật.

---

# ĐỀ XUẤT HÌNH ẢNH CẦN CHÈN

**Cách chụp:**
- Bấm Play trong Unity, Game view đặt độ phân giải **1920×1080** hoặc Device Simulator Galaxy S9 (ngang).
- Chụp bằng Win+Shift+S hoặc nút chụp của Device Simulator.
- Đánh số theo mục, ví dụ "Hình 4.2".

## Phần 1

| Mục | Hình | Scene / cách chụp | Thể hiện điều gì |
|---|---|---|---|
| 1.3 hoặc Lời mở đầu | Ảnh key art: 3 nhân vật + logo BeatRun | SelectChar, hoặc ghép 3 sprite | Nhận diện game |
| 2.4 | Ảnh lối chơi đặc trưng 3 màn, xếp cạnh nhau | Man1, Man2, Man3 lúc đang chơi | Mỗi màn một lối chơi |
| 2.5 / 6.3 | 3 nhân vật đứng cạnh nhau, có tên | SelectChar, chụp lần lượt 3 nhân vật | Nhân vật chính |
| 3.2 | Màn chọn màn: Màn 2 đang khóa, hiện giá 2.500 | SelectSong | Mở khóa màn bằng Coin |
| 3.2 | Màn chọn nhân vật: Stephen khóa, nút "UNLOCK 1500 COIN" | SelectChar | Mở khóa nhân vật bằng Coin |
| 3.3 (câu chuyện, mục mới) | Sơ đồ luồng chơi: Menu → Chọn màn → Chọn nhân vật → Truyện → Chơi → Kết quả | Vẽ (draw.io) | Chuỗi hành động của người chơi |
| 3.4 / 4.1 | Bảng kết quả 3 sao, có Coin thưởng | Man1, chơi hết bài | Điều kiện sao và Coin |
| 4.1 | Nhãn GOOD, GREAT, PERFECT bay lên cạnh nhân vật, COMBO x10 | Man1 hoặc Man2 | Nhãn theo chuỗi Combo |
| 4.1 / 4.4 | Nốt giữ (thanh dài) và nhân vật đang giữ | Man1 | Hold Note |
| 4.1 / 4.4 | Vật phẩm Màn 1 và thanh Fever đầy, nhân vật bắn đạn vào Boss | Man1 | Cơ chế Fever |
| 4.2 | Bảng Nhiệm vụ ngày; Bảng Lượt chơi hoặc Xem quảng cáo | SelectSong | Kinh tế |
| 4.3 | Ảnh HUD có chú thích mũi tên: 2 nút điều khiển, SCORE, PAUSE, ảnh nhân vật | Man1 | Điều khiển |
| 4.3 | Bảng Pause | Bất kỳ màn nào, bấm PAUSE | Tạm dừng |
| 4.4 | Boss có thanh máu đang giảm | Man1 (Vargo), Man2 (phù thủy) | Boss mất máu theo nốt trúng |
| 5.1 | Cận cảnh hiệu ứng trúng nốt và chữ COMBO đổi màu (vàng → cam → đỏ ở 5/15/25) | Man1 | Hiệu ứng phản hồi |
| 5.3 | Bảng Cài đặt bật/tắt âm thanh | SelectSong | Âm thanh |
| 6.1 | 1–2 trang truyện tranh mở đầu | StoryMan1 | Back story (thay dòng "Thiếu hình ảnh") |
| 6.2 | Mỗi giai đoạn một trang truyện | StoryMan1, 2, 3 | Mạch truyện |
| 6.4 | Ảnh từng kẻ thù: Vargo, phù thủy và ma vật, tàu ngầm, Mother Maw và đầu Medusa | Man1, Man2, Man3, Man4_Test | Kẻ thù |
| 6.5 | Sprite sheet nhân vật với khung các animation (Idle, Run, Attack, Jump, Hold, Die) | Mở `Knives Chau.png`, `Stephen Stills.png` hoặc Ramona trong Sprite Editor | Thiết kế nhân vật |
| 6.5 | Bản đồ: sơ đồ vương quốc Asterisk với kinh đô ở giữa và 4 điểm neo ở 4 góc | Vẽ | Bản đồ game (mẫu yêu cầu) |
| 7.1 | Thay ảnh Bảng 7.1 bằng bảng Word, hoặc thêm dải 5 ảnh nền nhỏ | — | Tông màu từng màn |
| 7.2.4 / 7.2.5 | Màn 4 có nhân vật và Mother Maw đang nhả quái; Màn 5 khi có Boss | Man4_Test, Man5 | Thay ảnh nền trống hiện tại |
| 7.3 | Ảnh màn Tutorial (khi xong) | Tutorial | Màn hướng dẫn |
| 8.4 | (tùy chọn) Ảnh lịch sử commit trên GitHub Desktop | GitHub Desktop | Minh chứng tiến độ |

## Phần 2

| Mục | Hình | Cách làm |
|---|---|---|
| 1 | Bảng SWOT 2×2 | Bảng Word |
| 2 | **Sơ đồ luồng scene** (10 scene) | draw.io |
| 2 | **Sơ đồ trạng thái màn chơi** (Đếm ngược → Đang chơi ⇄ Tạm dừng → Kết quả) | draw.io |
| 2 | Ảnh các màn hình UI: MainMenu, SelectSong, SelectChar, HUD, Pause, Kết quả | Chụp từ game |
| 3 | **Sơ đồ module / lớp chính** (MidiReader → Spawner → ComboSystem/ScoreSystem → EndGameBase → PlayerDataManager) | draw.io hoặc Visual Studio Class Diagram |
| 3 | Ảnh Package Manager (danh sách package) và Build Profiles (Android) | Unity |
| 3 | Ảnh Inspector của một LevelData (nhạc, MIDI, tốc độ, độ khó) | Unity, `Assets/MusicGame/Data/Level1.asset` |
| 3 | Ảnh công cụ MIDI Chart Tool và Animator nhân vật | Unity, menu Tools ▸ BeatRun |
| 4 | **Use Case Diagram** | draw.io / StarUML |
| 4 | **ERD** (theo cấu trúc PlayerData) | draw.io |
| 4 | Ảnh cấu trúc thư mục Assets; ảnh các nhánh Git | Unity Project window, GitHub Desktop |
| 5 | Ảnh Profiler khi chơi Man1; ảnh Device Simulator S9; ảnh Console log kết quả màn | Unity |
| 6 | Ảnh icon và splash; ảnh cài APK trên điện thoại thật | Điện thoại |
| 7 | QR code video demo; link GitHub | Tạo QR từ link YouTube/Drive |

---

## Thứ tự làm đề xuất

1. **Sửa các lỗi 🔴 về cơ chế**. Đây là chỗ dễ bị hội đồng bắt lỗi nhất:
   - Gacha / Diamond / Score mở màn.
   - Screen flash / bloom, VFX và font riêng từng nhân vật.
   - Coin rơi trong màn, nốt đôi, nốt đại dương.
   - Nhạc "độc quyền".
2. **Chèn bảng còn thiếu:** Bảng 3.1, Bảng 4.2, điền đủ bảng 6.4, dựng lại Bảng 7.1.
3. **Viết Phần 2 mục 3–7** (mục 3 chép từ `BaoCao_Phan2_TaiLieuKyThuat.md`), vẽ Use Case và ERD.
4. **Chụp và chèn hình** theo bảng trên.
5. **Sửa trình bày:**
   - Thống nhất tên "Knives Chau".
   - Đánh số hình và bảng.
   - Cập nhật Mục lục.
   - Xóa dòng "Thiếu hình ảnh" và dòng Win/Lose bị lặp.

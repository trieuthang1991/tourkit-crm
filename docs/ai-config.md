# Cấu hình AI

Toàn bộ nằm ở section `Ai` trong `src/TourKit.Api/appsettings.json`. Cấu hình đi theo **tính năng**:
mỗi tính năng tự khai dùng nhà cung cấp nào, model nào. Đổi model cho riêng phần chấm điểm khách hàng
là sửa một dòng, không ảnh hưởng trợ lý tra cứu.

## Đặt khoá API

Khoá đặt thẳng vào `Ai:Providers:<tên>:ApiKey` trong `appsettings.json`. **`appsettings.json` KHÔNG
nằm trong git** (`.gitignore`) chính vì lý do đó — mỗi máy giữ file cấu hình của riêng mình.

```jsonc
"Ai": {
  "Providers": {
    "deepseek": { "ApiKey": "sk-..." }
  }
}
```

**Máy mới** chưa có file thì sao chép từ mẫu rồi điền:

```bash
cp src/TourKit.Api/appsettings.example.json src/TourKit.Api/appsettings.json
```

`appsettings.example.json` **có** trong git và là bản mô tả đầy đủ mọi khoá cấu hình. Thêm khoá mới
thì thêm vào **cả hai file** — có bài kiểm tra đối chiếu hai file, thiếu là test đỏ ngay
(`AiConfigurationTests.File_mau_khong_duoc_thieu_khoa_nao_so_voi_file_that`).

Biến môi trường vẫn ghi đè được nếu cần (`Ai__Providers__deepseek__ApiKey`), nhưng không bắt buộc.

## Bật / tắt

| Khoá | Ý nghĩa |
|---|---|
| `Ai:Enabled` | Công tắc tổng. Tắt thì mọi tính năng AI im lặng ngừng, hệ thống chạy bình thường. |
| `Ai:Features:<Tên>:Enabled` | Bật/tắt riêng từng tính năng (vẫn phải bật công tắc tổng). |

Bật mà thiếu khoá thì hệ thống **không khởi động được**, kèm thông báo chỉ rõ thiếu khoá của nhà cung
cấp nào — thà hỏng lúc deploy còn hơn âm thầm chạy với trợ lý chết.

## Danh mục tính năng

| Tên | Việc | Năng lực cần | Đã chạy? |
|---|---|---|---|
| `Assistant` | Trợ lý tra cứu tiếng Việt (panel Ctrl+K) | Chat | **Rồi** |
| `Draft` | Soạn sẵn nội dung để người dùng sửa rồi lưu | Chat | Chưa |
| `Classify` | Phân loại / gán nhãn nhanh — nên trỏ model rẻ nhất | Chat | Chưa |
| `Summarize` | Tóm tắt một bản ghi nghiệp vụ | Chat | Chưa |
| `Scoring` | Chấm điểm khách hàng và cơ hội | Chat | **Rồi** |
| `Embedding` | Nhúng vector cho tra cứu tài liệu (giai đoạn 3) | Embedding | Chưa |
| `DocumentRead` | Đọc giấy tờ khách gửi vào (OCR) | DocumentRead | Chưa |
| `CustomerChat` | Chatbot khách hàng (giai đoạn 4) | Chat | Chưa |

Cột "Đã chạy?" không phải ghi chú suông — nó là `AiFeatures.Implemented` trong mã nguồn. **Bật một
tính năng chưa viết thì ứng dụng không khởi động**, kèm thông báo nói rõ lý do. Cần vậy vì bật nhầm
không làm gì hỏng cả: người đọc cấu hình tưởng nó đang chạy, còn chốt chặn khởi động thì đòi khoá cho
một thứ không tồn tại.

Viết xong một tính năng thì thêm tên nó vào `AiFeatures.Implemented` — đúng một dòng.

Thêm tính năng mới: thêm hằng số trong `AiFeatures` **và** một mục trong `appsettings.json`. Thiếu một
trong hai thì hệ thống báo lỗi lúc khởi động chứ không im lặng bỏ qua.

## Tiêu chí chấm điểm (tính năng `Scoring`)

Đây là phần nghiệp vụ, nằm hết trong JSON để sửa được mà không đụng mã nguồn.

```jsonc
"Scoring": {
  "Bands": [ {"Min": 80, "Label": "Nóng"}, {"Min": 50, "Label": "Ấm"}, {"Min": 0, "Label": "Nguội"} ],
  "Profiles": {
    "Customer": {
      "Criteria": [
        { "Key": "gia_tri", "Label": "Giá trị và tiềm năng chi tiêu", "Weight": 30, "Guide": "..." }
      ]
    }
  }
}
```

**Điểm tổng do HỆ THỐNG tính, không phải AI tự phán.** Model chỉ chấm từng tiêu chí 0–100; điểm tổng
là trung bình có trọng số. Nhờ vậy điểm giải thích được (giao diện hiện rõ mất điểm ở tiêu chí nào),
sửa được (đổi `Weight`), và thêm tiêu chí mới chỉ là thêm một mục vào `Criteria`.

| Khoá | Ý nghĩa |
|---|---|
| `Bands` | Ngưỡng xếp nhóm. Phải có một bậc `Min: 0`, nếu không điểm thấp không rơi vào nhóm nào. |
| `Profiles.<Loại>` | Bộ tiêu chí cho `Lead` (cơ hội) và `Customer` (khách hàng). Loại chưa khai thì không chấm. |
| `Key` | Mã máy, không dấu. Model dùng để trả kết quả về đúng tiêu chí. |
| `Weight` | **Tổng các tiêu chí trong một bộ phải bằng đúng 100** — sai là ứng dụng không khởi động. |
| `Guide` | Mô tả cho model biết điểm cao/thấp nghĩa là gì. Càng cụ thể càng ít lệch giữa hai lần chạy. |

Model bỏ sót một tiêu chí thì tiêu chí đó tính 0 và **vẫn nằm trong mẫu số** — bỏ nó ra sẽ làm điểm
tổng tăng lên đúng vào lúc model trả lời thiếu.

Phần KHUNG của prompt (vai trò, định dạng JSON trả về) cố ý **không** đưa ra cấu hình: sửa hỏng định
dạng thì hệ thống không đọc nổi câu trả lời và tính năng chết hẳn.

`deepseek-reasoner` cho nhận định sâu hơn nhưng mất ~20 giây một lần chấm. Cần nhanh thì đổi `Model`
sang `deepseek-chat` (~4 giây) — bộ tiêu chí đã gánh phần lớn việc suy luận.

## Chọn model cho từng việc

Mỗi hãng khai các mã model được phép dùng. Chọn model cho một tính năng là sửa
`Ai:Features:<Tên>:Model` bằng một mã trong danh sách của hãng đó.

```jsonc
"deepseek": {
  "Capabilities": [ "Chat" ],
  "Models": [ "deepseek-chat", "deepseek-reasoner" ]
}
```

**Gõ sai mã thì ứng dụng không khởi động**, kèm danh sách mã hợp lệ. Không có bước soát này thì mã sai
chỉ lộ ra lúc người dùng bấm nút và nhận về một lỗi khó hiểu của hãng.

Bỏ trống danh sách = không giới hạn — dùng khi hãng vừa ra model mới mà chưa kịp khai.

Nên dùng model nào cho việc gì (tri thức cho người đọc, không phải dữ liệu cho máy):

| Việc | Nên dùng | Vì sao |
|---|---|---|
| Trợ lý tra cứu | `deepseek-chat` | Cần gọi công cụ và trả lời nhanh; model suy luận làm người dùng chờ vô lý |
| Soạn thảo, tóm tắt | `deepseek-chat` | Việc diễn đạt, không cần lập luận nhiều bước |
| Phân loại, gán nhãn | model rẻ nhất của hãng | Việc đơn giản, chạy số lượng lớn |
| Chấm điểm, phân tích | `deepseek-reasoner` | Cần lập luận; đổi lại chậm hơn ~5 lần (~20 giây một lượt) |

## Thông số mỗi tính năng

| Khoá | Ghi chú |
|---|---|
| `Provider` | Khớp một khoá trong `Ai:Providers`. Gõ sai → lỗi lúc khởi động. |
| `Model` | Mã model chính xác của hãng. **Không có mặc định ngầm.** |
| `MaxOutputTokens` | Giới hạn độ dài câu trả lời. |
| `MaxToolRounds` | Số vòng gọi công cụ mỗi lượt (1–10). Chặn model gọi công cụ vô hạn. |
| `Temperature` | **Bỏ trống** với model suy luận — chúng trả HTTP 400 khi nhận tham số này. |
| `TimeoutSeconds` | 0 = theo nhà cung cấp. |

## Nhà cung cấp

| Khoá | Ghi chú |
|---|---|
| `Kind` | `OpenAiCompatible` (DeepSeek, OpenAI, Groq, Ollama…), `Anthropic`, `Http`, `Log`. |
| `BaseUrl` | Địa chỉ http(s) đầy đủ. Bỏ trống khi `Kind = Log`. |
| `Capabilities` | `Chat`, `Embedding`, `DocumentRead`. Trỏ tính năng vào hãng thiếu năng lực → lỗi lúc khởi động. |

`Kind` quyết định adapter nào được dựng lên. Thêm hãng có giao thức riêng = thêm một project cài
`IChatClientProvider` rồi thêm một dòng đăng ký ở `AiStartup`.

**Nhà cung cấp `log`** không gọi ra ngoài, chỉ ghi log và trả một câu cố định. Chưa có khoá mà vẫn
muốn mở màn hình trợ lý: đổi `Provider` của tính năng sang `log`.

## Chạy không cần khoá

```jsonc
"Ai": {
  "Enabled": true,
  "Features": { "Assistant": { "Enabled": true, "Provider": "log", "Model": "gia-lap" } }
}
```

## Hạn mức

| Khoá | Ý nghĩa |
|---|---|
| `Ai:Limits:DailyTokensPerUser` | Tổng token một người được dùng trong ngày (0 = không giới hạn). |
| `Ai:Limits:RequestsPerMinutePerUser` | Số lượt hỏi một người được gửi trong một phút (0 = không giới hạn). |

Vượt hạn mức thì bị chặn **trước khi gọi model** và người dùng thấy lời giải thích rõ ràng chứ không
phải một câu trả lời cụt.

Bộ đếm nằm trong bộ nhớ tiến trình — không thêm bảng, không thêm hạ tầng. Thứ nó bảo vệ là ngân sách
trước một vòng lặp gọi công cụ hỏng, chứ không phải sổ sách tính tiền. **Đánh đổi:** chạy nhiều tiến
trình thì mỗi tiến trình có hạn mức riêng (hạn mức thực tế = số tiến trình × cấu hình), và khởi động
lại thì bộ đếm về 0. Muốn đếm chính xác toàn cụm thì cần bộ đếm nguyên tử dùng chung (Redis `INCR`) —
làm khi thật sự chạy nhiều tiến trình, đừng làm trước.

## Nhật ký

Mỗi lượt hỏi ghi ba dòng vào log có cấu trúc (Serilog):

```
Trợ lý gọi công cụ bao_cao_top_khach_hang(top=3)
Trợ lý xong sau 2 vòng, 4992 token, 1 bảng.
Trợ lý: người 05be5d29-… hỏi 42 ký tự, tốn 4992 token (hôm nay 4992).
```

Dòng đầu là quan trọng nhất: không có nó thì lúc trợ lý trả lời sai sẽ không phân biệt được nó gọi
nhầm công cụ hay gọi đúng nhưng truyền tham số bậy.

## Đang dùng gì

DeepSeek (`deepseek-chat` cho hầu hết, `deepseek-reasoner` cho chấm điểm). Các mục `claude`, `openai`,
`fptai` đã khai sẵn khung, chỉ thiếu khoá.

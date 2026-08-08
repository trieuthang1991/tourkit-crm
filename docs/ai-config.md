# Cấu hình AI

Toàn bộ nằm ở section `Ai` trong `src/TourKit.Api/appsettings.json`. Cấu hình đi theo **tính năng**:
mỗi tính năng tự khai dùng nhà cung cấp nào, model nào. Đổi model cho riêng phần chấm điểm khách hàng
là sửa một dòng, không ảnh hưởng trợ lý tra cứu.

## Đặt khoá API

**Không dán khoá vào `appsettings.json`** — file đó nằm trong git, commit là khoá lên remote. Có một
bài kiểm tra tự động canh đúng việc này (`AiConfigurationTests`), dán khoá vào là test đỏ.

```bash
# Máy cá nhân
dotnet user-secrets set "Ai:Providers:deepseek:ApiKey" "sk-..." --project src/TourKit.Api

# Máy chạy thật
export Ai__Providers__deepseek__ApiKey="sk-..."
```

## Bật / tắt

| Khoá | Ý nghĩa |
|---|---|
| `Ai:Enabled` | Công tắc tổng. Tắt thì mọi tính năng AI im lặng ngừng, hệ thống chạy bình thường. |
| `Ai:Features:<Tên>:Enabled` | Bật/tắt riêng từng tính năng (vẫn phải bật công tắc tổng). |

`appsettings.json` để `Enabled: false`; `appsettings.Development.json` bật lên cho máy lập trình. Máy
chạy thật muốn bật thì đặt biến môi trường `Ai__Enabled=true` — và phải có khoá, nếu không hệ thống
**không khởi động được** kèm thông báo chỉ rõ thiếu khoá của nhà cung cấp nào.

## Danh mục tính năng

| Tên | Việc | Năng lực cần |
|---|---|---|
| `Assistant` | Trợ lý tra cứu tiếng Việt (panel Ctrl+K) | Chat |
| `Draft` | Soạn sẵn nội dung để người dùng sửa rồi lưu | Chat |
| `Classify` | Phân loại / gán nhãn nhanh — nên trỏ model rẻ nhất | Chat |
| `Summarize` | Tóm tắt một bản ghi nghiệp vụ | Chat |
| `Scoring` | Chấm điểm khách hàng và cơ hội — nên dùng model suy luận | Chat |
| `Embedding` | Nhúng vector cho tra cứu tài liệu (giai đoạn 3) | Embedding |
| `DocumentRead` | Đọc giấy tờ khách gửi vào (OCR) | DocumentRead |
| `CustomerChat` | Chatbot khách hàng (giai đoạn 4) | Chat |

Thêm tính năng mới: thêm hằng số trong `AiFeatures` **và** một mục trong `appsettings.json`. Thiếu một
trong hai thì hệ thống báo lỗi lúc khởi động chứ không im lặng bỏ qua.

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

`Ai:Limits:DailyTokensPerUser` và `Ai:Limits:RequestsPerMinutePerUser` (0 = không giới hạn). Hiện mới
là cấu hình; phần cưỡng chế nằm ở việc còn lại của giai đoạn 1.

## Đang dùng gì

DeepSeek (`deepseek-chat` cho hầu hết, `deepseek-reasoner` cho chấm điểm). Các mục `claude`, `openai`,
`fptai` đã khai sẵn khung, chỉ thiếu khoá.

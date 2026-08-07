# Tích hợp AI vào TourKit CRM — thiết kế tổng thể

Ngày: 2026-08-07
Trạng thái: bản thiết kế, chờ duyệt

## 1. Bài toán

Nhân viên (sale, điều hành, kế toán) mất nhiều thao tác cho ba việc lặp đi lặp lại:

- **Tra số liệu**: muốn biết "tháng 7 chi nhánh Hà Nội thu được bao nhiêu" phải mở đúng màn báo cáo, đặt đúng bộ lọc, đọc đúng cột.
- **Thủ tục**: quy trình visa, huỷ tour, hoàn tiền nằm rải rác trong đầu người cũ; người mới phải đi hỏi.
- **Gõ lại**: phiếu chăm sóc, tin nhắn nhắc khách, chương trình tour — nội dung đoán được từ dữ liệu đã có nhưng vẫn phải gõ tay.

Song song, khách hàng cuối hỏi những câu lặp lại (tour này còn chỗ không, booking của tôi tới đâu, chính sách huỷ thế nào) và đang chiếm thời gian của sale.

Mục tiêu: **rút ngắn thời gian từ "muốn biết / muốn làm" đến "có kết quả"**, cho cả quản trị viên nội bộ lẫn khách hàng cuối.

## 2. Quyết định đã chốt

| Câu hỏi | Chốt |
|---|---|
| Ai dùng AI | Nhân viên nội bộ trước; chatbot khách hàng ở giai đoạn sau |
| Dữ liệu ra ngoài | Được — dùng API thương mại (Claude/GPT/Gemini) |
| Phạm vi hành động | AI **đọc + soạn sẵn**, người bấm xác nhận mới ghi |
| Làm trước | Tra cứu bằng tiếng Việt |
| "Thủ tục" gồm | Quy trình nội bộ (RAG), sinh văn bản cho khách, OCR giấy tờ, theo dõi board công việc |

## 3. Hai quyết định kiến trúc quan trọng nhất

### 3.1 Gọi service nghiệp vụ (tool-calling), KHÔNG sinh SQL

Cách phổ biến là để model sinh SQL rồi chạy thẳng vào DB. **Không làm vậy ở đây.**

Toàn bộ an toàn dữ liệu của TourKit nằm ở lớp trên SQL: `ITenantContext` lọc theo công ty, global query filter ẩn bản ghi đã xoá, policy `perm` chặn theo vai trò. Cho model viết SQL là đi vòng qua đúng ba lớp đó — một câu `SELECT` quên `TenantId` là lộ dữ liệu công ty khác, một câu `JOIN` sai là treo DB.

Thay vào đó AI chỉ được gọi các **hàm nghiệp vụ đã có** (`IReportService.GetTurnoverByBranchAsync`, `ICustomerCareService.ListAsync`, …). Tenant và quyền tự động áp vì đó là code đang chạy hằng ngày. Model sai thì sai *câu trả lời*, không bao giờ sai *dữ liệu*.

Đánh đổi: chỉ trả lời được câu mà service hỗ trợ. Bù lại bằng một số tool tra danh sách có tham số lọc rộng (đơn hàng, khách, phiếu thu) — vẫn đi qua service.

### 3.2 Tách project theo adapter, không tách hệ thống

Đề xuất: thêm **nhiều project nhỏ trong cùng solution**, không tách repo/dịch vụ riêng cho phần nội bộ.

Lý do không tách dịch vụ: giá trị của AI ở đây nằm ở chỗ gọi được nghiệp vụ sẵn có. Tách ra dịch vụ riêng thì phải dựng lại lớp API, xác thực, tenant, phân quyền — nhân đôi công việc mà không thêm năng lực nào, đồng thời tạo ra một chỗ thứ hai có thể sai phân quyền.

Lý do tách nhiều project: hệ thống sẽ tích hợp **nhiều loại AI của nhiều nhà cung cấp**. Nếu dồn hết vào một assembly thì mọi SDK vendor bị kéo vào cùng một chỗ — thêm OpenAI là mọi service khác phải build lại và mang theo SDK nó không dùng.

```
TourKit.Ai.Abstractions   ← KHÔNG tham chiếu gì. Interface năng lực + IAiTool + DTO
TourKit.Ai                ← Abstractions + Application. Registry, vòng lặp chat, prompt, tool
TourKit.Ai.Anthropic      ← CHỈ Abstractions. Adapter Claude + SDK Anthropic
TourKit.Ai.OpenAi         ← sau, khi cần. Chỉ Abstractions + SDK OpenAI
TourKit.Ai.Voyage         ← sau. Embedding cho RAG
TourKit.Ai.FptAi          ← sau. OCR giấy tờ
TourKit.Api               ← composition root: chọn adapter nào theo cấu hình
TourKit.ChatBot           ← sau. Tiến trình riêng cho khách, bộ tool riêng
```

**Luật then chốt: adapter chỉ được thấy `Abstractions`, không được thấy `TourKit.Ai`.** Nếu adapter tham chiếu ngược lên orchestration thì hai thứ dính nhau và sửa vòng lặp chat sẽ bắt sửa lại mọi adapter. Ràng buộc này ép bằng arch test, không bằng thoả thuận miệng.

Ngoại lệ về tiến trình: **chatbot khách hàng chạy tiến trình riêng**. Nó mở ra internet, lưu lượng khác, rủi ro khác, và không được phép chạm vào cùng bộ tool nội bộ. Nó dùng chung `TourKit.Ai.Abstractions` và các adapter nhưng đăng ký một bộ tool hẹp riêng.

**Chưa đóng gói NuGet.** Project riêng trong cùng solution cho đủ lợi ích module hoá mà không phải gánh version + release. Chỉ đóng gói khi có sản phẩm thứ hai thật sự dùng lại.

### 3.3 Adapter tách theo NĂNG LỰC, không theo nhà cung cấp

Hội thoại, nhúng vector và đọc giấy tờ có ba hình dạng khác hẳn nhau. Gộp chúng vào một interface `IAiProvider` sẽ cho ra một interface mà mỗi cài đặt ném `NotSupportedException` cho phần lớn số hàm.

```csharp
public interface IChatModel      { string Id { get; } Task<AiCompletion> CompleteAsync(AiTurn t, CancellationToken ct); }
public interface ITextEmbedder   { string Id { get; } int Dimensions { get; } Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct); }
public interface IDocumentReader { string Id { get; } Task<DocumentFields> ReadAsync(Stream image, string kind, CancellationToken ct); }
```

Một adapter = một lớp, cài **một** năng lực, cho **một** vendor. Thêm Gemini là thêm `GeminiChatModel`; thêm OCR là thêm năng lực mới, không đụng gì tới chat.

**Chỉ thêm interface năng lực khi cái thứ hai thực sự đến.** Giai đoạn 1 chỉ có `IChatModel`; `ITextEmbedder` viết khi làm RAG, `IDocumentReader` khi làm OCR. Dựng sẵn một "AI gateway" phổ quát cho tương lai chưa biết là cách chắc chắn nhất để đoán sai hình dạng.

### 3.4 Chọn model theo việc, không theo mặc định

.NET 9 có keyed services sẵn — dùng nó thay vì tự viết factory. Cấu hình khai báo provider nào phục vụ việc nào:

```json
"Ai": {
  "Providers": { "claude": { "ApiKey": "", "Model": "claude-opus-5" } },
  "UseCases":  { "Chat": "claude", "Classify": "claude-haiku", "Draft": "claude" }
}
```

`IAiModelSelector.For(AiUseCase.Classify)` trả model rẻ, `For(AiUseCase.Chat)` trả model lớn. Đổi model cho một việc là sửa cấu hình, không sửa code.

Đây cũng là chỗ giữ tính chất mà quyết định "API Gateway trung tâm" (đã chốt cho SMS/Zalo/Bank/OCR) hướng tới: **một chỗ giữ khoá, một chỗ đếm chi phí, một chỗ đổi nhà cung cấp**. Với AI, chỗ đó là adapter + `IAiModelSelector`, không phải một chặng HTTP trung gian — đi vòng qua HTTP sẽ mất streaming, mất tool-calling có kiểu, mất lỗi có kiểu của SDK.

## 4. Lõi: tool registry

Mỗi tool khai báo bốn thứ:

```csharp
public interface IAiTool
{
    string Name { get; }            // "bao_cao_doanh_thu_theo_chi_nhanh"
    string Description { get; }     // tiếng Việt — model đọc cái này để chọn tool
    IReadOnlyDictionary<string, object> ParameterSchema { get; }  // JSON Schema
    string? RequiredPermission { get; }  // mã trong Authz.Permissions.All
    Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct);
}
```

**Phân quyền = lọc danh sách tool, không phải dặn dò model.** Trước mỗi lượt, hệ thống chỉ đưa cho model những tool mà claim `perm` của người dùng cho phép. Kế toán không thấy tool giá vốn thì model không có cách nào gọi nó — kể cả khi người dùng cố dụ. Đây là hàng rào thật, không phải câu nhắc trong prompt.

`AiToolResult` trả về hai phần: **văn bản** cho model đọc, và **dữ liệu có cấu trúc** (bảng + link) cho giao diện vẽ. Nhờ vậy câu trả lời không phải là một đoạn văn kể số, mà là bảng thật kèm nút "mở màn báo cáo tương ứng" — người dùng kiểm chứng được ngay.

### 4.1 Mỗi phân hệ tự góp tool của mình

Một file đăng ký trung tâm sẽ phình theo số phân hệ và trở thành chỗ ai cũng phải sửa. Thay vào đó mỗi phân hệ góp tool và đoạn prompt riêng:

```csharp
public interface IAiModule
{
    string Name { get; }                   // "reports", "crm", "work"
    IEnumerable<IAiTool> Tools { get; }
    string? SystemPromptFragment { get; }  // ghép vào prompt khi module bật
}
```

**Một cái bẫy phải biết trước:** prompt caching khớp theo tiền tố. Ghép fragment phải theo thứ tự **cố định** (sắp theo `Name`), nếu không cùng một tập module có thể sinh ra hai prompt khác nhau và cache không bao giờ ăn. Và nếu bật/tắt module theo tenant thì mỗi tổ hợp module là một cache riêng — tỉ lệ đọc cache sẽ thấp hơn dự tính, đó là cái giá phải trả cho khả năng tuỳ biến theo tenant.

## 5. Lộ trình bốn giai đoạn

### Giai đoạn 1 — Tra cứu tiếng Việt (làm trước)

Khung chat trượt từ phải, có ở mọi trang, dùng lại quy ước `tk-oc*` sẵn có.

Bộ tool khởi điểm bám đúng `IReportService` đang có: doanh thu theo kỳ, theo chi nhánh, theo bộ phận, theo loại tour; công nợ khách và công nợ nhà cung cấp; dòng tiền; top khách hàng; hoa hồng theo nhân viên; KPI tổng hợp. Thêm tool tra danh sách đơn hàng / khách / phiếu thu có lọc.

Câu trả lời trả về theo luồng (streaming) để cảm giác nhanh. Mọi con số kèm bảng và link.

Không cần chuẩn bị dữ liệu gì thêm, không có rủi ro ghi, dùng được từ ngày đầu.

### Giai đoạn 2 — Soạn sẵn

Nút "AI điền giúp" đặt trong các panel đã có. AI trả về JSON đúng dạng DTO tạo mới, giao diện đổ vào form, người sửa rồi bấm lưu. **Không có đường nào để AI tự lưu** — đây là ràng buộc kiến trúc, không phải quy ước: tool ở giai đoạn này không có tool ghi nào cả.

Áp dụng: phiếu chăm sóc khách, công việc, tin nhắn nhắc khách, mô tả chương trình tour, tóm tắt tình hình một đơn hàng.

### Giai đoạn 3 — Hỏi đáp quy trình (RAG)

Kho tài liệu dùng lại module `Files` + `Content` đã có. Người quản trị tải tài liệu quy trình lên, hệ thống cắt đoạn và tạo embedding.

Chỗ lưu vector — hai lựa chọn, cần chốt khi tới giai đoạn này:

- **Trong DB nghiệp vụ**: thêm bảng `AiDocumentChunk` (nội dung + vector + nguồn). Đơn giản nhất, sao lưu chung, đúng chuẩn multi-tenant sẵn có. Với Postgres dùng pgvector; với SqlServer thì tính cosine trong truy vấn.
- **Ngoài DB**: Qdrant hoặc tương đương. Tìm nhanh hơn ở quy mô lớn nhưng thêm một hạ tầng phải vận hành và phải tự lo cách ly tenant.

Khuyến nghị: bắt đầu trong DB nghiệp vụ. Tài liệu quy trình của một công ty lữ hành ở mức vài nghìn đoạn — chưa tới ngưỡng cần hạ tầng riêng.

**Bắt buộc: câu trả lời phải kèm trích dẫn tài liệu nguồn. Không tìm thấy thì trả lời "không có trong tài liệu", không được đoán.** Quy trình nội bộ mà bịa thì tệ hơn là không trả lời.

### Giai đoạn 4 — Chatbot khách hàng

Tiến trình riêng. Kênh: widget trên web + Zalo OA (đã có `IZaloSender`).

Danh tính: khách chưa xác thực chỉ hỏi được **thông tin công khai** (tour, giá niêm yết, chính sách). Muốn tra booking của mình phải xác thực số điện thoại bằng OTP — sau đó mọi tool đều bị ràng vào đúng khách đó ở tầng code, không phải ở tầng prompt.

Bộ tool hẹp: tra tour và lịch khởi hành, tra trạng thái booking của chính mình, câu hỏi thường gặp (dùng lại kho RAG giai đoạn 3).

Bắt buộc có:
- Không được cam kết giá hay khuyến mãi ngoài dữ liệu trả về từ tool.
- Nút chuyển người thật, và tự chuyển khi khách hỏi hai lần không ra kết quả hoặc tỏ ý bực.
- Lưu toàn bộ hội thoại để đối chiếu khi có tranh chấp.

OCR giấy tờ (hộ chiếu/CCCD → điền hành khách) **không cần LLM** — gọi thẳng FPT.AI qua Gateway. Xếp riêng, không phụ thuộc lộ trình này.

## 6. Những thứ phải có ngay từ giai đoạn 1

Đây là phần hay bị để lại "làm sau" rồi không bao giờ làm.

- **Nhật ký hội thoại và tool call**: ai hỏi gì, model gọi tool nào với tham số gì, tốn bao nhiêu token. Không có cái này thì lúc AI trả lời sai sẽ không có cách nào biết vì sao.
- **Hạn mức token theo người dùng theo ngày**, chặn ở lớp gateway. Một vòng lặp tool hỏng có thể đốt hết ngân sách tháng trong một buổi.
- **Thời gian chờ và đường lui**: AI hỏng thì màn hình vẫn chạy bình thường. AI là lớp phụ trợ, không được nằm trên đường đi chính của bất kỳ nghiệp vụ nào.
- **Số vòng tool tối đa mỗi lượt** (đề xuất 5), tránh model gọi tool vô hạn.

## 7. Xử lý lỗi

| Tình huống | Cách xử lý |
|---|---|
| Nhà cung cấp AI lỗi/quá tải | Trả lời "trợ lý đang bận, thử lại sau"; ghi log; màn hình không ảnh hưởng |
| Model gọi tool không tồn tại | Trả lỗi về cho model một lần để nó tự sửa; quá số vòng thì dừng |
| Tool ném lỗi nghiệp vụ | Đưa nguyên thông điệp lỗi cho model diễn đạt lại bằng tiếng Việt |
| Người dùng thiếu quyền | Tool không có trong danh sách ngay từ đầu; model trả lời "tôi không tra được mục này" |
| Hết hạn mức token | Báo rõ cho người dùng, không im lặng cắt |

## 8. Kiểm thử

- **Tầng tool**: kiểm thử như service bình thường — tất định, không dính LLM. Đây là nơi chứa toàn bộ logic có thể sai về dữ liệu.
- **Lọc quyền**: kiểm thử riêng, chặt — với mỗi vai trò, danh sách tool đưa cho model phải đúng như mong đợi. Đây là kiểm thử an toàn quan trọng nhất.
- **Tầng LLM**: bộ câu hỏi vàng (khoảng 30 câu tiếng Việt kèm đáp án đúng), chạy thưa, so kết quả bằng tay. Không đưa vào CI mỗi lần commit vì tốn tiền và không tất định.
- Không giả lập LLM trong kiểm thử service — service không biết gì về LLM là thiết kế đúng.

## 9. Chi phí ước lượng

Một lượt hỏi đáp có tool call tiêu chừng 3–8 nghìn token. Với 20 nhân viên, mỗi người 30 lượt/ngày, dùng model tầm trung: khoảng vài triệu đồng một tháng. Chatbot khách hàng phụ thuộc lượng truy cập — cần hạn mức theo phiên ngay từ đầu.

Cách giảm: câu hỏi lặp lại thì lưu đệm (đã có `ITkCache`), việc phân loại đơn giản dùng model nhỏ, việc cần suy luận mới dùng model lớn.

## 10. Ngoài phạm vi

- Huấn luyện lại (fine-tune) model. Prompt + tool là đủ; huấn luyện lại tốn kém và khoá vào một nhà cung cấp.
- AI tự chạy không cần người xác nhận.
- Dịch tự động và nhận dạng giọng nói.

## 11. Phạm vi của kế hoạch triển khai đầu tiên

Tài liệu này mô tả cả bốn giai đoạn để thấy đường đi, nhưng **kế hoạch triển khai đầu tiên chỉ gồm
Giai đoạn 1**: `TourKit.Ai.Abstractions` + `TourKit.Ai` (registry, lọc quyền, vòng lặp chat) +
`TourKit.Ai.Anthropic` (adapter Claude) + khung chat + bộ tool báo cáo. Mỗi giai đoạn sau có kế
hoạch riêng, viết khi giai đoạn trước đã chạy thật.

Giai đoạn 1 chỉ dựng **một** năng lực (`IChatModel`) và **một** adapter. `ITextEmbedder`,
`IDocumentReader`, `IAiModelSelector` và `IAiModule` là hình dạng đã chốt nhưng chưa viết —
viết khi có cái thứ hai để đối chiếu, chứ không đoán trước.

## 12. Việc cần chốt trước khi viết kế hoạch triển khai

1. Nhà cung cấp AI cụ thể cho giai đoạn 1 (ảnh hưởng chi phí và chất lượng tiếng Việt).
2. Chỗ lưu vector ở giai đoạn 3 — có được thêm bảng vào DB nghiệp vụ không.
3. Có làm chatbot khách hàng trong đợt này, hay để đợt sau khi phần nội bộ đã ổn định.

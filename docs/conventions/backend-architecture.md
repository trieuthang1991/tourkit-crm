# TourKit — Kiến trúc Backend chuẩn (best-practice C#/.NET) & Kế hoạch nâng cấp

> ⚠️ **LỖI THỜI (2026-07-09):** dự án đã **chuyển từ CQRS/Vertical-Slice sang phân tầng cổ điển** (Controller → Service → Repository → EF, Exception + global handler) cho dễ tiếp cận. Tài liệu kiến trúc hiện hành là **`backend-structure.md`**. Kernel CQRS (Dispatcher/Result/Features) đã bị xoá. File này giữ lại làm lịch sử.
>
> Mục tiêu (bản gốc): từ backend MVP "endpoint gọi thẳng DbContext" → kiến trúc Modular Monolith + Vertical Slice + CQRS + Domain giàu.
> Cập nhật: 2026-07-08. Bổ sung cho `backend-conventions.md` (không thay thế — làm rõ *cách tổ chức* code).

---

## PHẦN A — Chẩn đoán hiện trạng (thẳng thắn)

MVP hiện tại (P0–P7) **chạy được, test cô lập đầy đủ**, nhưng về cấu trúc còn thiếu và **vi phạm chính convention của dự án**:

| Vấn đề | Hiện tại | Convention nói | Hệ quả khi scale |
|---|---|---|---|
| **Không có tầng Application/Domain** | Endpoint minimal-API gọi thẳng `AppDbContext` | (ngầm định "logic ở Application/Domain") | Logic nghiệp vụ rải trong endpoint, khó test đơn vị, khó tái dùng |
| **Anemic domain** | Entity chỉ là túi dữ liệu (get/set) | — | Invariant (vd "không đặt quá slot") không được entity bảo vệ → dễ sai |
| **Validate tối thiểu inline** | `if (string.IsNullOrWhiteSpace...)` | §6 **FluentValidation** (1 validator/request) | Validate rải rác, không nhất quán, khó test |
| **Không Result pattern** | Trả `IResult` (404/400) ngay trong handler | §6 **Result pattern** cho lỗi nghiệp vụ | Trộn HTTP concern với logic; khó tái dùng logic ngoài HTTP |
| **Không logging có cấu trúc** | Không log | §7 **Serilog** structured | Không trace được production |
| **Không arch-test** | Không | §10 **NetArchTest** ép chiều phụ thuộc | Ranh giới module bị phá âm thầm |
| **Module chỉ là folder trong 1 project** | Tất cả trong `TourKit.Api` | §1 Modular Monolith | Không có ranh giới thật; module tham chiếu chéo tự do |
| **List trả full, không phân trang** | `.ToListAsync()` toàn bảng | — | Grid nghìn dòng → chậm/OOM |
| **Không CancellationToken nhất quán?** | Có (tốt) | — | OK |

**Kết luận:** đúng như bạn nhận xét — "sơ sài, khó scale". MVP tối ưu cho tốc độ ra tính năng; giờ đến lúc **củng cố nền** trước khi domain phình to.

---

## PHẦN B — Kiến trúc đích: Modular Monolith + Vertical Slice

### B1. Vì sao (không phải Clean Architecture thuần)
- **Modular Monolith**: 1 deploy, nhưng module (Identity, Catalog, Booking, Finance, CRM, Provider, Commission, Marketing, Billing) có **ranh giới rõ** — module chỉ nói chuyện qua *public contract*, không tham chiếu entity nội bộ của nhau. Tách microservice sau = rẻ.
- **Vertical Slice Architecture (VSA)**: cắt theo **tính năng** (mỗi use-case = 1 slice: Command/Query + Handler + Validator + Response nằm CẠNH nhau), thay vì chia tầng ngang toàn cục (`Services/`, `Repositories/`). → mỗi thay đổi tính năng gói gọn 1 chỗ, ít file phải mở.
- **Pragmatic**: không ép Clean 100% mọi slice. Slice CRUD đơn giản → handler mỏng gọi DbContext. Slice nghiệp vụ phức tạp (đặt chỗ, chốt lãi) → có Domain aggregate giàu + Domain service. Đây là khuyến nghị chủ đạo của cộng đồng .NET 2025.

### B2. Sơ đồ tầng (trong mỗi module)
```
[Api endpoint] → dispatch(Command/Query) → [Application Handler]
                                              ├─ FluentValidation (pipeline)
                                              ├─ Domain aggregate (invariant) ← nghiệp vụ phức tạp
                                              └─ AppDbContext (persistence)     ← CRUD đơn giản
        ← Result<T> (Ok | Error) → map sang HTTP (ProblemDetails)
```

### B3. Cấu trúc project đề xuất (tách dần từ 1 project hiện tại)
```
src/
  TourKit.Shared/            # kernel: BaseEntity, ITenantEntity, Result<T>/Error, ICommand/IQuery/IHandler, IDispatcher
  TourKit.Domain/            # (MỚI) aggregate giàu + value object + domain event (Tour/Order/Voucher...)
  TourKit.Application/       # (MỚI) handler theo slice + validator + DTO; phụ thuộc Domain + abstraction Infrastructure
  TourKit.Infrastructure/    # AppDbContext, EF config, migration, impl repository/uow, provider
  TourKit.Api/               # composition root + endpoint mỏng (chỉ map route → dispatch)
tests/
  TourKit.UnitTests/         # (MỚI) test domain + handler (nhanh, không HTTP/DB)
  TourKit.Tests/             # integration qua HTTP (đã có)
  TourKit.ArchTests/         # (MỚI) NetArchTest ép ranh giới
```
> **Lưu ý thực thi:** KHÔNG tách hết 4 project ngay (rủi ro + tốn công). Bắt đầu bằng **folder-based slice trong `TourKit.Api`** + kernel ở `Shared`, rồi *nâng* module ổn định lên project riêng khi cần. Ranh giới **ép bằng arch-test** ngay cả khi còn chung project.

---

## PHẦN C — Các pattern cốt lõi (kèm quyết định công cụ 2026)

### C1. CQRS + Dispatcher tự viết (KHÔNG MediatR)
> **Cập nhật quan trọng:** MediatR (v13+, sau 2/7/2025) **đã chuyển license thương mại** (miễn phí cho cá nhân/<5M doanh thu; trả phí theo team). Để tránh ràng buộc, best-practice hiện nay:
> - **Dispatcher tự viết ~50–80 dòng** (khuyến nghị cho dự án này — đơn giản, không phụ thuộc, đủ dùng), hoặc
> - **Wolverine** (source-generator, kèm messaging/outbox/saga — mạnh nhưng nặng hơn), **FastEndpoints**, hoặc handler DI trực tiếp.

- `ICommand<TResult>` / `IQuery<TResult>` (marker) + `ICommandHandler<TCommand,TResult>` / `IQueryHandler`.
- `IDispatcher.Send(command, ct)` resolve handler từ DI, chạy **pipeline** (validation → logging → transaction) rồi gọi handler.
- Đăng ký handler bằng assembly scan (Scrutor) hoặc source-gen.

### C2. FluentValidation (đã là §6)
- 1 validator / command. Chạy trong **pipeline behavior** của dispatcher → mọi command tự validate, handler không cần `if` rác.
- Zod ở FE + FluentValidation ở BE = 2 lớp; BE là chân lý.

### C3. Result pattern (đã là §6) — không throw để điều khiển luồng
- `Result` / `Result<T>` với `Error(Code, Message, Type)` (Type: Validation | NotFound | Conflict | Forbidden | Unexpected).
- Handler trả `Result<T>`; endpoint map `Error.Type → HTTP` (Validation→400, NotFound→404, Conflict→409, Forbidden→403) qua 1 helper `results.ToHttp()`.
- Thư viện thay thế nếu không muốn tự viết: **ErrorOr**, **FluentResults**. Dự án này tự viết (nhỏ, kiểm soát tốt).

### C4. Rich Domain (DDD tactical, áp cho aggregate phức tạp)
- **Aggregate**: `Order`/`TourCustomer` gom thành aggregate `Booking`; hành vi (đặt chỗ, giữ chỗ, cọc, huỷ, tính tổng) là **method trên aggregate**, không phải trong endpoint. Invariant được bảo vệ trong constructor/method (vd không cho cọc > tổng, không huỷ 2 lần).
- **Value Object**: `Money(decimal Amount)`, `AgeGroupPricing`, `SeatStatus` — bất biến, tự validate.
- Công thức tiền (đã gom ở `Domain/BookingMath`) → nâng thành method trên aggregate/value object.
- **Domain Event**: `SeatCancelled`, `ReceiptApproved` → xử lý phụ (cập nhật tổng, gửi thông báo) tách khỏi luồng chính (xem C7).

### C5. EF Core best-practice
- **DbContext LÀ Unit of Work** — KHÔNG bọc generic repository (anti-pattern). Handler inject `AppDbContext` (hoặc interface `IAppDbContext` để test).
- Aggregate lớn cần query phức tạp/tái dùng → **Specification pattern** (không bắt buộc sớm).
- **Đọc**: `AsNoTracking` + projection DTO (đã làm). **Ghi**: tracking + aggregate.
- **Phân trang bắt buộc** cho list: `Paged<T>(Items, Total, Page, Size)` hoặc **keyset/cursor** cho grid lớn (nhanh hơn OFFSET).
- Concurrency token (`RowVersion`) cho aggregate có denormalize (đã ghi ở DB §I).

### C6. Module boundaries + arch-test
- Module A không import type nội bộ module B → dùng qua **public contract** (interface trong `Shared` hoặc module-contracts) hoặc **domain event**.
- **NetArchTest** trong `TourKit.ArchTests`: "Api không chứa logic nghiệp vụ", "Domain không phụ thuộc Infrastructure/EF", "module X không phụ thuộc module Y". Fail build nếu vi phạm — ép kỷ luật thật.

### C7. Cross-cutting
- **Transaction**: pipeline behavior mở transaction quanh command (ghi nhiều bảng atomic) — thay vì rải `SaveChanges`.
- **Domain events + Outbox**: đổi trạng thái phát event; handler phụ xử lý sau; outbox đảm bảo giao (khi có message broker / nhiều module).
- **Logging**: **Serilog** (structured, sink Console/Seq/OTel) + pipeline log mỗi command (tên, tenant, thời gian, kết quả) — KHÔNG log dữ liệu nhạy cảm (§7).
- **Observability**: OpenTelemetry (trace/metric) khi lên prod.
- **Error → ProblemDetails**: middleware duy nhất map exception + `Result.Error` sang RFC 7807 (đã có `AddProblemDetails`).

### C8. Bảo mật/đa tenant (đã tốt — giữ)
- Shared DB + `TenantId` + global filter + interceptor (đã có). Giữ. Cân nhắc **schema-per-module** (logic gom bảng) khi tách project.

---

## PHẦN D — Kế hoạch nâng cấp (tăng dần, không big-bang)

> Nguyên tắc: mỗi bước **giữ 77 test xanh**, refactor 1 slice làm mẫu rồi nhân rộng. KHÔNG viết lại từ đầu.

| Bước | Nội dung | Rủi ro | Giá trị | Trạng thái |
|---|---|---|---|---|
| **1** | Kernel: `Result<T>`/`Error` + `ICommand/IQuery/IHandler` + `Dispatcher` (tự viết) + pipeline (validation) trong `Shared` | Thấp | Nền cho mọi slice | ✅ Xong |
| **2** | FluentValidation + 1 **vertical slice mẫu** (vd `CreateTourTemplate` + `ListTourTemplates` phân trang) qua dispatcher; endpoint chỉ `Send()` | Thấp | Mẫu chuẩn để nhân rộng | ✅ Xong |
| **3** | `TourKit.ArchTests` (NetArchTest) ép: Api mỏng, Domain không EF, ranh giới module | Thấp | Chống trôi kiến trúc | ✅ Xong |
| **4** | Serilog + pipeline logging + ProblemDetails mapping từ `Result.Error` | Thấp | Trace được | ✅ Xong |
| **5** | Rich **Booking aggregate** (Order+TourCustomer): chuyển logic đặt/giữ/cọc/huỷ vào aggregate + domain event; unit test nhanh | Trung | Bảo vệ invariant, test nhanh | ◻️ Chưa — hiện gom ở `BookingFactory`/`BookingMath`/`SeatMapper` (đủ dùng); nâng lên aggregate khi cần invariant chặt hơn |
| **6** | Phân trang toàn bộ endpoint list (`Paged<T>`) | Thấp | Grid dùng được ở quy mô | ✅ Xong (list toàn cục dạng mảng → `Paged<T>`; list con theo cha giữ `IReadOnlyList`) |
| **7** | Nhân rộng slice pattern cho các module còn lại; nâng module ổn định lên project riêng (`Domain`/`Application`) | Trung | Ranh giới thật, sẵn tách service | ✅ Xong roll-out 9 module (Catalog, Customers, CRM, Providers, Marketing, Commission/Billing, Finance/Reports, Booking); tách project riêng = sau |
| **8** | Transaction/Outbox behavior + Domain events cho tác vụ phụ (thông báo, cập nhật tổng) | Trung | Nhất quán khi nhiều module | ◻️ Chưa (chờ có nhiều module/broker) |

---

## PHẦN E — Ví dụ slice chuẩn (tham chiếu)

Xem code thật ở **`TourKit.Api/Catalog/Features/`** (bước 2 đã hiện thực làm mẫu): `CreateTourTemplate` (Command + Handler + Validator + Result) và `ListTourTemplates` (Query + Handler + phân trang), endpoint chỉ `dispatcher.Send(...)`. Kernel ở `TourKit.Shared/Application/`. Unit test ở `tests/TourKit.UnitTests/`.

**Anti-pattern cần tránh (đã học):**
- Generic repository bọc EF → bỏ (EF là UoW).
- Service "God" `XxxService` ôm hết → thay bằng slice/handler nhỏ.
- Throw exception để báo lỗi nghiệp vụ dự kiến → dùng `Result`.
- MediatR (license) → dispatcher tự viết.
- Logic trong endpoint → đẩy vào handler/aggregate; endpoint chỉ điều phối.

---

## Nguồn tham khảo
- Milan Jovanović — Vertical Slice Architecture / Modular Monolith (milanjovanovic.tech).
- Anton Martyniuk — Modular Monolith with Vertical Slice, Refactoring without MediatR (antondevtips.com).
- Microsoft Learn — On .NET Live: Clean Architecture, Vertical Slices, Modular Monoliths.
- MediatR/MassTransit commercial licensing (2025) — milanjovanovic.tech, Jeremy Miller (Wolverine).

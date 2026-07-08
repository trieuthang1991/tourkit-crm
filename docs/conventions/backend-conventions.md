# TourKit — Backend Conventions (.NET 10)

> Mục tiêu: mọi người (và mọi subagent) viết code **giống nhau, sạch, không lộn xộn**. Đây là luật, không phải gợi ý. Vi phạm bị chặn bởi analyzer (build fail) hoặc bị reject ở code review.

Tài liệu này gắn với các quyết định trong `docs/superpowers/specs/2026-07-07-tourkit-saas-platform-design.md`.

---

## 0. Ba luật vàng (rút từ sai lầm của hệ cũ)

Hệ cũ phân mảnh vì 3 lỗi. Tuyệt đối tránh:

1. **KHÔNG để logic nghiệp vụ trong database.** Không stored procedure, **không trigger**, không table type cho logic. Logic ở tầng Application/Domain (C#), test được — kể cả đồng bộ cột denormalize (tính lại trong transaction ở domain service, KHÔNG nhờ trigger). → hệ cũ có 61 proc + 42 table type là lý do không refactor nổi; logic ẩn trong DB rất khó follow/trace/test.
2. **KHÔNG "đẻ phiên bản".** Không bao giờ tạo `FooV2`, `FooNew`, `Foo_v11`. Thay đổi = sửa tại chỗ + **EF Core migration** trong git. → hệ cũ có `booking_list_tbltype` tới v11.
3. **Một convention đặt tên duy nhất.** Toàn bộ English, PascalCase. Không tiếng Việt không dấu (`PhongBan`, `LoaiDonHang`). Ngoại lệ: nhãn hiển thị cho người dùng (i18n ở frontend).

---

## 1. Cấu trúc solution & ranh giới module

Modular monolith. Mỗi module nghiệp vụ = 1 project, có ranh giới rõ.

```
src/
  TourKit.Api            # composition root, controllers/endpoints — MỎNG
  TourKit.Shared         # kernel dùng chung: BaseEntity, ITenantContext, Result, guards
  TourKit.Infrastructure # EF Core DbContext, migrations, file storage, email, jobs
  TourKit.Catalog        # module: tour, itinerary, pricing, departure   (Phase 1)
  TourKit.Booking        # module: customer, booking, pax, seat          (Phase 2)
  TourKit.Finance        # module: receipt/payment voucher               (Phase 3)
```

**Luật phụ thuộc (dependency rule) — một chiều, không vòng:**

```
Api  ──►  Modules (Catalog/Booking/Finance)  ──►  Shared
 └──────►  Infrastructure  ──►  Shared
Modules ──► Infrastructure (chỉ qua interface, KHÔNG tham chiếu ngược)
```

- Module **không** tham chiếu module khác trực tiếp. Cần dữ liệu chéo module → qua interface đặt ở `Shared`, hoặc gọi qua Application service, hoặc integration event. Không `Booking` đọc thẳng `DbSet` của `Catalog`.
- `Shared` **không** phụ thuộc gì (trừ BCL).
- Vi phạm chiều phụ thuộc bị chặn bằng test kiến trúc (NetArchTest) — xem §10.

**Phân lớp trong mỗi module (split theo trách nhiệm, không theo tầng kỹ thuật):**

```
TourKit.Booking/
  Domain/            # entity + business rule thuần, không phụ thuộc EF/ASP.NET
    Booking.cs
    BookingStatus.cs
  Application/        # use case, DTO, validator, interface repository
    Bookings/
      CreateBooking/
        CreateBookingCommand.cs
        CreateBookingHandler.cs
        CreateBookingValidator.cs
        BookingDto.cs
  Infrastructure/    # EF config cho entity của module (IEntityTypeConfiguration)
    BookingConfiguration.cs
```

---

## 2. Naming (khớp `.editorconfig`, analyzer ép)

| Đối tượng | Quy tắc | Ví dụ |
|-----------|---------|-------|
| Class, record, method, property, enum | `PascalCase` | `BookingService`, `CreateBooking` |
| Interface | `I` + PascalCase | `ITenantContext`, `IBookingRepository` |
| Private field | `_camelCase` | `_dbContext`, `_tenant` |
| Local var, param | `camelCase` | `bookingId`, `totalAmount` |
| Constant | `PascalCase` | `MaxSeatsPerBooking` |
| Async method | luôn hậu tố `Async` | `SaveChangesAsync` |
| Bảng/entity DB | `PascalCase`, **số nhiều** cho bảng (EF mặc định) | entity `Booking` → bảng `Bookings` |
| Cột/property | `PascalCase` | `TenantId`, `TotalAmount`, `CreatedAt` |
| File | trùng tên type chính | `BookingService.cs` |
| Namespace | `TourKit.<Module>.<Layer>...` | `TourKit.Booking.Application.Bookings` |

- **Tiếng Anh, không viết tắt mờ nghĩa.** `department` không `pb`; `orderStatus` không `tt`.
- Một file = một type public (record/enum nhỏ liên quan có thể chung file).

---

## 3. Kiểu C# & style (ép bằng analyzer)

- `<Nullable>enable</Nullable>` toàn solution. Không `#nullable disable`. Không `!` (null-forgiving) trừ khi có comment lý do.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — warning = build fail.
- `ImplicitUsings` bật. File-scoped namespace (`namespace X;`).
- Ưu tiên `record` cho DTO/command/query (immutable). Entity dùng `class`.
- `var` khi kiểu rõ từ vế phải; kiểu tường minh khi không rõ.
- Không `#region`. Không code chết. Không comment code cũ để đó — git giữ lịch sử.
- Async: **async all the way**, luôn `await`, không `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`. Nhận `CancellationToken` và truyền xuống.
- Không `DateTime.Now`. Dùng `DateTimeOffset.UtcNow` qua abstraction `IClock` (test được).

---

## 4. Multi-tenancy — luật bất di bất dịch

Đây là phần dễ gây rò rỉ dữ liệu chéo doanh nghiệp. Không có ngoại lệ.

- **Mọi entity nghiệp vụ implement `ITenantEntity`.** Không có `TenantId` = không phải dữ liệu nghiệp vụ (chỉ bảng hệ thống: `Tenant`, `Plan`, `Subscription`).
- **KHÔNG BAO GIỜ tin `TenantId` từ client.** Tenant resolve từ **JWT claim** (server-side), không từ body/route/header do client gửi.
- **KHÔNG tự viết `.Where(x => x.TenantId == ...)`.** Global Query Filter trong `AppDbContext` lo việc đó tự động. Viết tay = dấu hiệu sai thiết kế.
- **Không dùng `IgnoreQueryFilters()` trong code nghiệp vụ.** Chỉ cho seed/migration/admin tool, và phải có comment lý do.
- **Mỗi module có test cô lập chéo tenant** (bắt buộc, xem §10). Thêm entity tenant mới mà thiếu test cô lập → reject.

---

## 5. EF Core

- **Migrations trong git, review như code.** `dotnet ef migrations add <Tên có nghĩa>`. Không sửa migration đã merge; sai thì thêm migration mới.
- **Cấu hình entity bằng `IEntityTypeConfiguration<T>`** (mỗi entity 1 file), không nhồi hết vào `OnModelCreating`.
- **Đọc thì `AsNoTracking()`** (query trả DTO). Chỉ tracking khi cần cập nhật.
- **Không lazy loading.** Nạp tường minh bằng `Include`/projection. Chống N+1: project thẳng ra DTO bằng `Select`.
- **Không lộ entity ra ngoài Application.** Controller trả DTO, không trả entity.
- Index: mọi entity tenant có index bắt đầu bằng `TenantId` (`.HasIndex(x => new { x.TenantId, x.OtherKey })`).
- Tiền tệ: `decimal(18,2)`. Không `float`/`double` cho tiền.
- Soft delete: `IsDeleted` + global filter; không xóa cứng dữ liệu nghiệp vụ.

---

## 6. API / Controllers

- **Controller/endpoint mỏng:** validate → gọi Application handler → map kết quả. Không logic nghiệp vụ trong controller.
- REST: danh từ số nhiều, đúng verb. `GET /api/v1/bookings`, `POST /api/v1/bookings`, `GET /api/v1/bookings/{id}`.
- **Versioning từ đầu:** prefix `/api/v1`.
- **DTO vào/ra riêng biệt**, không tái dùng entity. Request là `record`.
- **Validation bằng FluentValidation** (một validator/request). Không `if (x == null) throw` rải rác.
- **Lỗi trả `ProblemDetails`** (RFC 7807) chuẩn hóa qua middleware. Không nuốt lỗi, không trả 200 kèm message lỗi.
- Dùng **Result pattern** cho lỗi nghiệp vụ dự kiến (không throw để điều khiển luồng). Exception chỉ cho lỗi thật sự bất thường.
- Không bao giờ trả stack trace/thông tin nội bộ cho client ở production.

---

## 7. Xử lý lỗi & logging

- Một **exception-handling middleware** duy nhất → map exception sang `ProblemDetails` + log.
- **Structured logging (Serilog)**, không `Console.WriteLine`. Log có tham số: `_logger.LogInformation("Created booking {BookingId} for tenant {TenantId}", id, tenantId)`.
- **KHÔNG log dữ liệu nhạy cảm** (mật khẩu, token, số CMND/CCCD, thẻ).
- Audit ghi tự động trong `SaveChanges` (ai/việc/entity/trước-sau) — không rải log audit thủ công.

---

## 8. Bảo mật

- Secret không nằm trong code/git. Dev: user-secrets; Prod: biến môi trường / key vault.
- Mật khẩu: ASP.NET Identity hasher (không tự hash). JWT có hạn ngắn + refresh token.
- Phân quyền: `[Authorize(Policy = "booking.create")]` — kiểm tra ở endpoint. Không "chỉ ẩn nút ở UI".
- Input luôn được validate ở server (đừng tin frontend).

---

## 9. Testing

- **TDD**: test đỏ → code → xanh → refactor. Mỗi task theo chu trình này (xem plan).
- **xUnit**. Tên test mô tả hành vi: `Insert_auto_assigns_current_tenant`.
- Cấp độ: unit (Domain/Application, nhanh, không DB) + integration (DbContext InMemory/relational) + API (WebApplicationFactory).
- **Bắt buộc:** mỗi module có test cô lập chéo tenant (đọc + ghi + chặn sửa chéo).
- Không test phụ thuộc thứ tự chạy, không phụ thuộc dữ liệu thật.

---

## 10. Enforcement (tự động, không dựa vào ý chí)

Ép tuân thủ bằng cấu hình, không bằng nhắc nhở:

- `.editorconfig` ở root — style + naming, mức `warning`/`error`.
- `Directory.Build.props` ở root — bật `Nullable`, `TreatWarningsAsErrors`, phân tích:
  ```xml
  <Project>
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <EnableNETAnalyzers>true</EnableNETAnalyzers>
      <AnalysisLevel>latest-recommended</AnalysisLevel>
      <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    </PropertyGroup>
  </Project>
  ```
- **Test kiến trúc (NetArchTest)** trong `TourKit.Tests` kiểm chiều phụ thuộc: `Shared` không phụ thuộc module; module không phụ thuộc `Api`; module không phụ thuộc lẫn nhau.
- Format tự động: `dotnet format` chạy trong pre-commit hook / CI. CI fail nếu chưa format.
- Code review checklist: đối chiếu 3 luật vàng + §4 (tenancy) + §5 (EF) cho mọi PR.

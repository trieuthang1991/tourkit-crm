# Đợt 0 — Vá lỗ hổng chặn vận hành (Implementation Plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Vá 4 lỗ hổng chặn vận hành đã phát hiện khi đối chiếu hệ cũ: (1) chống overbooking khi đặt/giữ chỗ, (2) đóng chuyến, (3) hoàn thiện CRUD MarketType theo kiến trúc chuẩn, (4) mở chuyến kế thừa cấu hình từ tour mẫu.

**Architecture:** Giữ nguyên Vertical Slice/CQRS. Công thức đếm chỗ gom về `BookingMath` (một chỗ). Kiểm tra sức chứa ở tầng ứng dụng (đủ cho dev SQLite; concurrency-proof bằng RowVersion là follow-up của backend-architecture.md bước 8). Frontend thêm nút "Đóng chuyến" + sửa/xoá MarketType.

**Tech Stack:** .NET 9, EF Core 9, xUnit (InMemory), React 18 + Ant Design.

---

## Bối cảnh mã nguồn (đọc trước)

- `src/TourKit.Infrastructure/Domain/BookingMath.cs` — công thức tiền (thêm `SeatCount` vào đây).
- `src/TourKit.Api/Booking/Features/BookingFactory.cs` — `BuildAsync` dùng chung booking + hold; **chưa kiểm tra sức chứa/đóng chuyến**.
- `TourCustomer.Status`: `0` = còn hiệu lực, `1` = đã huỷ (xem `CancelSeat.cs`).
- `TourDeparture`: có `IsClosed`, `ClosedAt`, `TotalSlots` (từ base `Tour`). Cột `IsClosed/ClosedAt` hiện là **cột chết**.
- `Permissions`: có `DepartureView`, `DepartureCreate`; `All` là list `(Code, Group)`. `MarketView`/`MarketManage` đã có.
- `src/TourKit.Api/Catalog/MarketTypeEndpoints.cs` — hiện chỉ GET/POST, thao tác `AppDbContext` trực tiếp (không qua dispatcher).
- Test slice mẫu: `tests/TourKit.UnitTests/Booking/BookingSlicesTests.cs` (helper `FixedTenant`/`NewDb`).
- Integration test mẫu: `tests/TourKit.Tests/Booking/BookingEndpointTests.cs`.
- Frontend departures: `web/src/features/booking/{DeparturesPage.tsx,DepartureDetailPage.tsx,departuresApi.ts}`; market types: `web/src/features/marketTypes/*`.

---

## Task 1: Công thức đếm chỗ `BookingMath.SeatCount`

**Files:**
- Modify: `src/TourKit.Infrastructure/Domain/BookingMath.cs`
- Test: `tests/TourKit.UnitTests/Booking/BookingMathTests.cs` (tạo mới)

- [ ] **Step 1: Viết test fail**

```csharp
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class BookingMathTests
{
    [Fact]
    public void SeatCount_sums_all_four_age_quantities()
    {
        var seat = new TourCustomer
        {
            Quantity = 2, AmountChildren = 1, AmountChildrenSmall = 1, QuantityBaby = 1,
        };
        Assert.Equal(5, BookingMath.SeatCount(seat));
    }
}
```

- [ ] **Step 2: Chạy test → FAIL** (`SeatCount` chưa tồn tại)

Run: `dotnet test tests/TourKit.UnitTests --filter BookingMathTests`
Expected: FAIL (không biên dịch — thiếu `SeatCount`).

- [ ] **Step 3: Thêm `SeatCount` vào BookingMath**

Chèn vào `BookingMath` (sau `LineTotal`):

```csharp
    /// <summary>
    /// Số chỗ (ghế) một dòng đặt chiếm: tổng số khách 4 nhóm tuổi.
    /// Quy ước: mỗi khách (kể cả em bé) tính 1 chỗ. Đổi quy ước sức chứa thì sửa ở ĐÂY.
    /// </summary>
    public static int SeatCount(TourCustomer s)
        => s.Quantity + s.AmountChildren + s.AmountChildrenSmall + s.QuantityBaby;
```

- [ ] **Step 4: Chạy test → PASS**

Run: `dotnet test tests/TourKit.UnitTests --filter BookingMathTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/TourKit.Infrastructure/Domain/BookingMath.cs tests/TourKit.UnitTests/Booking/BookingMathTests.cs
git commit -m "feat(booking): BookingMath.SeatCount — công thức đếm chỗ (một chỗ)"
```

---

## Task 2: Chống overbooking + chặn đặt trên chuyến đã đóng

**Files:**
- Modify: `src/TourKit.Api/Booking/Features/BookingFactory.cs`
- Test: `tests/TourKit.UnitTests/Booking/BookingCapacityTests.cs` (tạo mới)

`BuildAsync` sau khi validate departure/template/customer, TRƯỚC khi tạo Order: (a) nếu `departure.IsClosed` → `Error.Conflict`; (b) tính chỗ đã dùng (seats `Status==0` của departure) + chỗ mới; nếu `> TotalSlots` → `Error.Conflict`.

- [ ] **Step 1: Viết test fail**

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Shared.Application;

namespace TourKit.UnitTests.Booking;

public sealed class BookingCapacityTests
{
    private static async Task<(Guid depId, Guid custId)> SeedAsync(TourKit.Infrastructure.Persistence.AppDbContext db, int totalSlots, bool closed = false)
    {
        var tpl = new TourTemplate { Code = "T", Title = "T", PriceAdult = 1_000_000 };
        var dep = new TourDeparture { Code = "D", Title = "D", ParentTourId = tpl.Id, TotalSlots = totalSlots, IsClosed = closed };
        var cust = new Customer { FullName = "A" };
        db.AddRange(tpl, dep, cust);
        await db.SaveChangesAsync();
        return (dep.Id, cust.Id);
    }

    [Fact]
    public async Task Booking_over_TotalSlots_is_rejected()
    {
        using var db = TestDb.NewDb();
        var (depId, custId) = await SeedAsync(db, totalSlots: 2);
        // Đặt 2 chỗ (đủ sức chứa)
        var first = await BookingFactory.BuildAsync(db, depId, custId, 2, 0, 0, 0, isHold: false, default);
        Assert.True(first.IsSuccess);
        // Đặt thêm 1 chỗ → vượt 2 → Conflict
        var second = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(second.IsFailure);
        Assert.Equal(ErrorType.Conflict, second.Error.Type);
    }

    [Fact]
    public async Task Cancelled_seats_do_not_count_toward_capacity()
    {
        using var db = TestDb.NewDb();
        var (depId, custId) = await SeedAsync(db, totalSlots: 1);
        var first = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        // Huỷ chỗ vừa đặt
        var seat = await db.TourCustomers.FirstAsync();
        seat.Status = 1;
        await db.SaveChangesAsync();
        // Đặt lại 1 chỗ → OK vì chỗ cũ đã huỷ
        var again = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Booking_on_closed_departure_is_rejected()
    {
        using var db = TestDb.NewDb();
        var (depId, custId) = await SeedAsync(db, totalSlots: 10, closed: true);
        var r = await BookingFactory.BuildAsync(db, depId, custId, 1, 0, 0, 0, isHold: false, default);
        Assert.True(r.IsFailure);
        Assert.Equal(ErrorType.Conflict, r.Error.Type);
    }
}
```

**Lưu ý executor:** dùng helper tạo `AppDbContext` InMemory giống `BookingSlicesTests.cs`. Nếu tên helper ở đó không phải `TestDb.NewDb()`/`FixedTenant`, thay cho khớp (đọc `BookingSlicesTests.cs` để lấy đúng tên). Nếu `Result` không có `IsFailure`/`Error.Type` như trên, đọc `src/TourKit.Shared/Application/Result.cs` + `Error.cs` để dùng đúng API (vd `result.IsSuccess == false`, `result.Error.Type`).

- [ ] **Step 2: Chạy test → FAIL** (chưa có guard → test overbooking/closed fail)

Run: `dotnet test tests/TourKit.UnitTests --filter BookingCapacityTests`
Expected: FAIL (`second`/closed vẫn Success).

- [ ] **Step 3: Thêm guard vào `BuildAsync`**

Trong `BookingFactory.BuildAsync`, NGAY SAU khi lấy được `departure` (đã null-check) và TRƯỚC khi tạo `order`, chèn:

```csharp
        if (departure.IsClosed)
        {
            return Error.Conflict("Chuyến đã đóng, không thể đặt thêm chỗ.");
        }

        var newSeats = adultQty + childQty + childSmallQty + babyQty;
        var usedSeats = await db.TourCustomers
            .Where(s => s.TourDepartureId == departureId && s.Status == 0)
            .SumAsync(s => s.Quantity + s.AmountChildren + s.AmountChildrenSmall + s.QuantityBaby, ct);
        if (departure.TotalSlots > 0 && usedSeats + newSeats > departure.TotalSlots)
        {
            return Error.Conflict(
                $"Vượt sức chứa: còn {departure.TotalSlots - usedSeats}/{departure.TotalSlots} chỗ.");
        }
```

Ghi chú: dùng biểu thức tổng trực tiếp trong `SumAsync` (EF dịch được sang SQL); `BookingMath.SeatCount` dùng cho phía in-memory/đơn lẻ. `TotalSlots > 0` để chuyến không đặt hạn mức (0) thì không chặn — giữ hành vi cũ cho dữ liệu chưa cấu hình.

- [ ] **Step 4: Chạy test → PASS**

Run: `dotnet test tests/TourKit.UnitTests --filter BookingCapacityTests`
Expected: PASS cả 3.

- [ ] **Step 5: Chạy toàn bộ test đảm bảo không hồi quy**

Run: `dotnet test`
Expected: tất cả xanh (108+). Nếu integration test cũ đặt chỗ với `TotalSlots=0` hoặc nhỏ mà nay bị chặn → sửa seed test cho `TotalSlots` đủ lớn (KHÔNG nới guard).

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Booking/Features/BookingFactory.cs tests/TourKit.UnitTests/Booking/BookingCapacityTests.cs
git commit -m "feat(booking): chống overbooking + chặn đặt trên chuyến đã đóng"
```

---

## Task 3: Đóng chuyến (close departure)

**Files:**
- Modify: `src/TourKit.Api/Authz/Permissions.cs` (thêm `DepartureClose`)
- Create: `src/TourKit.Api/Booking/Features/CloseDeparture.cs`
- Modify: `src/TourKit.Api/Booking/DepartureEndpoints.cs` (thêm route)
- Test: `tests/TourKit.Tests/Booking/DepartureCloseTests.cs` (integration)

- [ ] **Step 1: Thêm permission**

Trong `Permissions.cs`, sau `DepartureCreate`:
```csharp
    public const string DepartureClose = "departure.close";
```
Trong `All`, cạnh `(DepartureCreate, "Booking")`:
```csharp
        (DepartureClose, "Booking"),
```

- [ ] **Step 2: Viết slice `CloseDeparture.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Đóng chuyến (chốt sổ) — legacy StatusCloseTour. Đóng rồi không đặt thêm chỗ được.</summary>
public sealed record CloseDepartureCommand(Guid DepartureId) : ICommand<DepartureResponse>;

public sealed class CloseDepartureHandler : ICommandHandler<CloseDepartureCommand, DepartureResponse>
{
    private readonly AppDbContext _db;

    public CloseDepartureHandler(AppDbContext db) => _db = db;

    public async Task<Result<DepartureResponse>> Handle(CloseDepartureCommand c, CancellationToken ct)
    {
        var dep = await _db.TourDepartures.FirstOrDefaultAsync(d => d.Id == c.DepartureId, ct);
        if (dep is null)
        {
            return Error.NotFound();
        }

        if (dep.IsClosed)
        {
            return Error.Conflict("Chuyến đã đóng.");
        }

        dep.IsClosed = true;
        dep.ClosedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new DepartureResponse(
            dep.Id, dep.Code, dep.Title, dep.ParentTourId,
            dep.DepartureDate, dep.EndDate, dep.TotalSlots, dep.Status);
    }
}
```

- [ ] **Step 3: Thêm route** vào `DepartureEndpoints.cs` (trong `MapDepartureEndpoints`, trước `return app;`):

```csharp
        group.MapPost("/{id:guid}/close", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new CloseDepartureCommand(id), ct))
                .Match(d => Results.Ok(d))).RequireAuthorization(Permissions.DepartureClose);
```

- [ ] **Step 4: Viết integration test**

```csharp
using System.Net;
using System.Net.Http.Json;
using TourKit.Api.Booking;

namespace TourKit.Tests.Booking;

public sealed class DepartureCloseTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public DepartureCloseTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Close_then_second_close_conflicts_and_booking_blocked()
    {
        var client = await _factory.Authed();   // đọc BookingEndpointTests.cs để dùng đúng helper auth/seed
        var dep = await _factory.CreateDepartureWithTemplate(client);   // helper hiện có (hoặc tạo qua API)

        var close1 = await client.PostAsync($"/api/v1/tour-departures/{dep.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, close1.StatusCode);

        var close2 = await client.PostAsync($"/api/v1/tour-departures/{dep.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, close2.StatusCode);

        var book = await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep.Id}/bookings",
            new { customerId = dep.CustomerId, adultQty = 1, childQty = 0, childSmallQty = 0, babyQty = 0 });
        Assert.Equal(HttpStatusCode.Conflict, book.StatusCode);
    }
}
```

**Lưu ý executor:** đọc `tests/TourKit.Tests/Booking/BookingEndpointTests.cs` để tái dùng đúng cách tạo client có token + seed tenant/permission (Admin role đã có sẵn `departure.close` vì role Admin gán TẤT CẢ permission — không cần chỉnh seed). Nếu chưa có helper `CreateDepartureWithTemplate`, tạo departure + template qua các endpoint API như test hiện có làm.

- [ ] **Step 5: Chạy test → PASS + toàn bộ test xanh**

Run: `dotnet test`
Expected: xanh hết.

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Authz/Permissions.cs src/TourKit.Api/Booking/Features/CloseDeparture.cs src/TourKit.Api/Booking/DepartureEndpoints.cs tests/TourKit.Tests/Booking/DepartureCloseTests.cs
git commit -m "feat(booking): đóng chuyến (close departure) + chặn thao tác sau khi đóng"
```

---

## Task 4: Hoàn thiện CRUD MarketType theo kiến trúc chuẩn (dispatcher + Update/Delete)

**Files:**
- Create: `src/TourKit.Api/Catalog/Features/MarketTypes.cs` (gộp các slice)
- Modify: `src/TourKit.Api/Catalog/MarketTypeEndpoints.cs` (endpoint mỏng)
- Test: `tests/TourKit.UnitTests/Catalog/MarketTypeSlicesTests.cs`

- [ ] **Step 1: Viết test fail (validator + update/delete handler)**

```csharp
using TourKit.Api.Catalog.Features;
using TourKit.Shared.Application;

namespace TourKit.UnitTests.Catalog;

public sealed class MarketTypeSlicesTests
{
    [Fact]
    public void Create_validator_rejects_empty_name()
    {
        var r = new CreateMarketTypeValidator().Validate(new CreateMarketTypeCommand("", null, 0));
        Assert.False(r.IsValid);
    }

    [Fact]
    public async Task Update_then_delete_roundtrip()
    {
        using var db = TestDb.NewDb();   // đọc slice test khác để dùng đúng helper
        var create = await new CreateMarketTypeHandler(db).Handle(new CreateMarketTypeCommand("Inbound", null, 1), default);
        Assert.True(create.IsSuccess);
        var id = create.Value.Id;

        var upd = await new UpdateMarketTypeHandler(db).Handle(new UpdateMarketTypeCommand(id, "Outbound", null, 2), default);
        Assert.True(upd.IsSuccess);

        var del = await new DeleteMarketTypeHandler(db).Handle(new DeleteMarketTypeCommand(id), default);
        Assert.True(del.IsSuccess);
    }
}
```

- [ ] **Step 2: Chạy → FAIL**

Run: `dotnet test tests/TourKit.UnitTests --filter MarketTypeSlicesTests`
Expected: FAIL (chưa có slice).

- [ ] **Step 3: Viết `Features/MarketTypes.cs`**

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog.Features;

public sealed record MarketTypeDto(Guid Id, string Name, Guid? ParentId, int SortOrder, int Status);

// ----- List -----
public sealed record ListMarketTypesQuery : IQuery<IReadOnlyList<MarketTypeDto>>;

public sealed class ListMarketTypesHandler : IQueryHandler<ListMarketTypesQuery, IReadOnlyList<MarketTypeDto>>
{
    private readonly AppDbContext _db;
    public ListMarketTypesHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<MarketTypeDto>>> Handle(ListMarketTypesQuery q, CancellationToken ct)
    {
        var list = await _db.MarketTypes.AsNoTracking().OrderBy(m => m.SortOrder)
            .Select(m => new MarketTypeDto(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<MarketTypeDto>>(list);
    }
}

// ----- Create -----
public sealed record CreateMarketTypeCommand(string Name, Guid? ParentId, int SortOrder) : ICommand<MarketTypeDto>;

public sealed class CreateMarketTypeValidator : AbstractValidator<CreateMarketTypeCommand>
{
    public CreateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class CreateMarketTypeHandler : ICommandHandler<CreateMarketTypeCommand, MarketTypeDto>
{
    private readonly AppDbContext _db;
    public CreateMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result<MarketTypeDto>> Handle(CreateMarketTypeCommand c, CancellationToken ct)
    {
        var m = new MarketType { Name = c.Name.Trim(), ParentId = c.ParentId, SortOrder = c.SortOrder };
        _db.MarketTypes.Add(m);
        await _db.SaveChangesAsync(ct);
        return new MarketTypeDto(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status);
    }
}

// ----- Update -----
public sealed record UpdateMarketTypeCommand(Guid Id, string Name, Guid? ParentId, int SortOrder) : ICommand;

public sealed class UpdateMarketTypeValidator : AbstractValidator<UpdateMarketTypeCommand>
{
    public UpdateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateMarketTypeHandler : ICommandHandler<UpdateMarketTypeCommand>
{
    private readonly AppDbContext _db;
    public UpdateMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateMarketTypeCommand c, CancellationToken ct)
    {
        var m = await _db.MarketTypes.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (m is null)
        {
            return Error.NotFound();
        }
        m.Name = c.Name.Trim();
        m.ParentId = c.ParentId;
        m.SortOrder = c.SortOrder;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- Delete -----
public sealed record DeleteMarketTypeCommand(Guid Id) : ICommand;

public sealed class DeleteMarketTypeHandler : ICommandHandler<DeleteMarketTypeCommand>
{
    private readonly AppDbContext _db;
    public DeleteMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteMarketTypeCommand c, CancellationToken ct)
    {
        var m = await _db.MarketTypes.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (m is null)
        {
            return Error.NotFound();
        }
        _db.MarketTypes.Remove(m);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

**Lưu ý executor:** kiểm tra chữ ký non-generic `ICommand`/`ICommandHandler<TCommand>` (không có TResponse) và `Result` (non-generic) trong `src/TourKit.Shared/Application/Cqrs.cs` + `Result.cs`. Nếu kernel CHƯA có biến thể non-generic (chỉ có `ICommand<T>`), thì cho Update/Delete trả `ICommand<bool>`/`Result<bool>` trả `true` — chọn cách khớp kernel hiện có (đọc một Update handler module khác, vd `Customers/Features`, để theo đúng mẫu). Giữ nhất quán với module Customers.

- [ ] **Step 4: Endpoint mỏng** — thay toàn bộ thân `MarketTypeEndpoints.cs`:

```csharp
using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Catalog.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog;

/// <summary>Loại thị trường (legacy MarketType). Endpoint mỏng: dispatch → map Result sang HTTP.</summary>
public static class MarketTypeEndpoints
{
    public static IEndpointRouteBuilder MapMarketTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market-types");

        group.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListMarketTypesQuery(), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.MarketView);

        group.MapPost("/", async (CreateMarketTypeCommand body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(body, ct))
                .Match(m => Results.Created($"/api/v1/market-types/{m.Id}", m))).RequireAuthorization(Permissions.MarketManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateMarketTypeBody body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new UpdateMarketTypeCommand(id, body.Name, body.ParentId, body.SortOrder), ct))
                .Match(() => Results.NoContent())).RequireAuthorization(Permissions.MarketManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteMarketTypeCommand(id), ct))
                .Match(() => Results.NoContent())).RequireAuthorization(Permissions.MarketManage);

        return app;
    }
}

public sealed record UpdateMarketTypeBody(string Name, Guid? ParentId, int SortOrder);
```

**Lưu ý:** `Match(() => ...)` là overload cho `Result` non-generic. Nếu kernel chỉ có `Match<T>`, đổi Update/Delete sang trả `Result<bool>` và `Match(_ => Results.NoContent())` (theo mẫu Customers `MapDelete`).

- [ ] **Step 5: Chạy test → PASS + toàn bộ xanh**

Run: `dotnet test`
Expected: xanh. `market-types` vẫn là mảng trần (frontend không đổi cách đọc list).

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Catalog/Features/MarketTypes.cs src/TourKit.Api/Catalog/MarketTypeEndpoints.cs tests/TourKit.UnitTests/Catalog/MarketTypeSlicesTests.cs
git commit -m "feat(catalog): MarketType full CRUD qua dispatcher (thêm Update/Delete)"
```

---

## Task 5: Mở chuyến kế thừa cấu hình từ tour mẫu

**Files:**
- Modify: `src/TourKit.Api/Booking/Features/CreateDeparture.cs`
- Test: `tests/TourKit.UnitTests/Booking/CreateDepartureInheritTests.cs`

Khi `TemplateId` có giá trị: (a) nếu `TotalSlots` không truyền (== 0) → lấy `TotalSlots` của template; (b) copy `TourType` từ template; (c) copy các dòng `TourItinerary` của template sang departure (mỗi dòng tạo bản mới `TourId = departure.Id`).

- [ ] **Step 1: Viết test fail**

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class CreateDepartureInheritTests
{
    [Fact]
    public async Task Departure_inherits_slots_type_and_itinerary_from_template()
    {
        using var db = TestDb.NewDb();
        var tpl = new TourTemplate { Code = "TPL", Title = "Mẫu", TourType = "Nội địa", TotalSlots = 25 };
        db.Add(tpl);
        db.Add(new TourItinerary { TourId = tpl.Id, DayIndex = 1, Title = "Ngày 1" });
        await db.SaveChangesAsync();

        var res = await new CreateDepartureHandler(db).Handle(
            new CreateDepartureCommand(tpl.Id, "DEP", "Chuyến 1", null, null, TotalSlots: 0), default);

        Assert.True(res.IsSuccess);
        var dep = await db.TourDepartures.FirstAsync(d => d.Code == "DEP");
        Assert.Equal(25, dep.TotalSlots);
        Assert.Equal("Nội địa", dep.TourType);
        Assert.True(await db.TourItineraries.AnyAsync(i => i.TourId == dep.Id && i.DayIndex == 1));
    }
}
```

**Lưu ý executor:** kiểm tra tên entity/DbSet lịch trình (`TourItinerary`/`TourItineraries`) + field (`TourId`, `DayIndex`, `Title`, `Detail`) trong `src/TourKit.Infrastructure/Entities/` — sửa test/handler cho khớp tên thật.

- [ ] **Step 2: Chạy → FAIL**

Run: `dotnet test tests/TourKit.UnitTests --filter CreateDepartureInheritTests`

- [ ] **Step 3: Sửa `CreateDepartureHandler.Handle`**

Thay thân handler:

```csharp
    public async Task<Result<DepartureResponse>> Handle(CreateDepartureCommand c, CancellationToken ct)
    {
        var departure = new TourDeparture
        {
            Code = c.Code.Trim(), Title = c.Title.Trim(), ParentTourId = c.TemplateId,
            DepartureDate = c.DepartureDate, EndDate = c.EndDate, TotalSlots = c.TotalSlots,
        };

        if (c.TemplateId is { } tplId)
        {
            var template = await _db.TourTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tplId, ct);
            if (template is not null)
            {
                departure.TourType = template.TourType;
                if (departure.TotalSlots == 0)
                {
                    departure.TotalSlots = template.TotalSlots;
                }
            }
        }

        _db.TourDepartures.Add(departure);

        if (c.TemplateId is { } tid)
        {
            var days = await _db.TourItineraries.AsNoTracking()
                .Where(i => i.TourId == tid).OrderBy(i => i.DayIndex).ToListAsync(ct);
            foreach (var d in days)
            {
                _db.TourItineraries.Add(new TourItinerary
                {
                    TourId = departure.Id, DayIndex = d.DayIndex, Title = d.Title, Detail = d.Detail,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return new DepartureResponse(
            departure.Id, departure.Code, departure.Title, departure.ParentTourId,
            departure.DepartureDate, departure.EndDate, departure.TotalSlots, departure.Status);
    }
```

Thêm `using Microsoft.EntityFrameworkCore;` nếu chưa có.

- [ ] **Step 4: Chạy test → PASS + toàn bộ xanh**

Run: `dotnet test`

- [ ] **Step 5: Commit**

```bash
git add src/TourKit.Api/Booking/Features/CreateDeparture.cs tests/TourKit.UnitTests/Booking/CreateDepartureInheritTests.cs
git commit -m "feat(booking): mở chuyến kế thừa số chỗ/loại/lịch trình từ tour mẫu"
```

---

## Task 6: Frontend — nút "Đóng chuyến" + sửa/xoá MarketType

**Files:**
- Modify: `web/src/features/booking/departuresApi.ts` (hook close)
- Modify: `web/src/features/booking/DepartureDetailPage.tsx` (nút Đóng chuyến)
- Modify: `web/src/features/marketTypes/MarketTypesPage.tsx` (+ Sửa/Xoá)
- Modify: `web/src/features/marketTypes/*Api*.ts` (hook update/delete)

- [ ] **Step 1: Hook close departure** trong `departuresApi.ts`:

```ts
export function useCloseDeparture() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/tour-departures/${id}/close`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['departures'] }),
  });
}
```
(khớp queryKey với key hiện có trong file — đọc trước.)

- [ ] **Step 2: DepartureDetailPage** — thêm nút "Đóng chuyến" (gate `has('departure.close')`), disable/ẩn nếu chuyến đã đóng (`departure.status`/cờ). `Popconfirm` → `useCloseDeparture().mutateAsync(id)` → `message.success`/`errorMessage`.

- [ ] **Step 3: MarketTypesPage** — thêm cột thao tác Sửa (mở modal điền lại Name/ParentId/SortOrder → PUT) + Xoá (`Popconfirm` → DELETE), gate `has('market.manage')`. Thêm hook `useUpdateMarketType`/`useDeleteMarketType` (PUT/DELETE, invalidate list).

- [ ] **Step 4: Verify + commit**

Run (trong `web/`): `npm run build && npm run lint && npm run test`
Expected: 0 lỗi, test xanh.
```bash
git add web/src/features/booking web/src/features/marketTypes
git commit -m "feat(web): nút đóng chuyến + sửa/xoá loại thị trường"
```

---

## Self-Review (author checklist — đã rà)

- **Bao phủ Đợt 0:** overbooking (Task 1+2), đóng chuyến (Task 3 + guard trong Task 2), MarketType CRUD (Task 4), kế thừa template (Task 5), UI (Task 6). ✔
- **Công thức một chỗ:** `SeatCount` nằm trong `BookingMath` cùng `LineTotal`. ✔
- **Nhất quán kiểu:** `CloseDepartureCommand`/`Update|DeleteMarketTypeCommand` bám kernel; executor được dặn kiểm tra biến thể generic/non-generic `ICommand`/`Result` và theo mẫu module Customers nếu khác. ✔
- **Không hồi quy:** Task 2 Step 5 & Task 3/4/5 đều chạy `dotnet test` full; dặn sửa seed (không nới guard) nếu test cũ dùng `TotalSlots` nhỏ. ✔
- **RBAC:** role Admin (đăng ký tenant) gán TẤT CẢ permission → `departure.close` tự có, không cần chỉnh seed. ✔
- **Concurrency-proof overbooking** (RowVersion/counter) = ngoài phạm vi Đợt 0, thuộc backend-architecture.md bước 8 — ghi rõ, không âm thầm bỏ. 

# Đợt 6 (phần lõi) — Điều hành: Xe + Phân công HDV — Implementation Plan

> REQUIRED SUB-SKILL: superpowers:subagent-driven-development / executing-plans. Steps checkbox.

**Goal:** Thêm 2 module điều hành groundable: **Xe (`Vehicle`, bám legacy `vehicle`)** và **Phân công HDV cho chuyến (`TourGuideAssignment`, bám legacy `TourGuide`)** — HDV là 1 `Provider` (ProviderType.Guide), assignment gắn provider-guide vào chuyến + giờ đi/về.

**Phạm vi:** Phần lõi. `Calendar` (lịch điều hành) là view UI thuần trên departures — để sau. `TicketFund`/quỹ vé ứng của HDV — cần requirement, để sau.

**Architecture:** Vertical Slice/CQRS mirror Customers/Providers. Migration mới. SQLite-safe.

---

## Mẫu nhân bản (đọc trước)
- `src/TourKit.Api/Providers/{ProviderContracts.cs,ProviderEndpoints.cs,Features/*}` (paged CRUD) + config.
- `src/TourKit.Infrastructure/Entities/{Provider,TourDeparture}.cs`.
- `AppDbContext.cs`, `Program.cs`, `Authz/Permissions.cs`.
- Frontend: `web/src/features/providers/*`, `web/src/app/{router.tsx,AppShell.tsx}`, `web/src/shared/ui/Field.tsx` (DatePickerField).

---

## Task 1: Permissions
Modify `Permissions.cs` — const + `All` (group "Booking"):
```csharp
    public const string VehicleView = "vehicle.view";
    public const string VehicleManage = "vehicle.manage";
    public const string GuideView = "guide.view";
    public const string GuideManage = "guide.manage";
```
`All`: `(VehicleView,"Booking"),(VehicleManage,"Booking"),(GuideView,"Booking"),(GuideManage,"Booking"),`
- [ ] Build + commit `feat(authz): permission xe + phân công HDV`.

---

## Task 2: Vehicle — CRUD phân trang

**Files:**
- `src/TourKit.Infrastructure/Entities/Vehicle.cs`, config, DbSet, migration `AddVehicle`
- `src/TourKit.Api/Booking/VehicleContracts.cs` + `Booking/Features/{CreateVehicle,UpdateVehicle,DeleteVehicle,ListVehicles}.cs` + `Booking/VehicleEndpoints.cs`
- `Program.cs`; Test `tests/TourKit.UnitTests/Booking/VehicleSlicesTests.cs`

Entity (bám `vehicle`):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Xe (legacy vehicle): tên xe, hãng, loại ghế/số chỗ.</summary>
public sealed class Vehicle : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // name
    public string? FirmName { get; set; }              // nameFirm
    public int SeatType { get; set; }                  // typeSeat (4/7/16/29/45...)
    public int Status { get; set; }
}
```
Config: `Name` maxLength 255; index `(TenantId, Name)`.

Contracts:
```csharp
public sealed record CreateVehicleRequest(string Name, string? FirmName, int SeatType, int Status);
public sealed record UpdateVehicleRequest(string Name, string? FirmName, int SeatType, int Status);
public sealed record VehicleResponse(Guid Id, string Name, string? FirmName, int SeatType, int Status);
```
Slices mirror Customers (List paged; Update/Delete `ICommand<bool>`; create+update cùng shape → dùng chung form). Endpoints `/api/v1/vehicles`, perms `VehicleView`/`VehicleManage`. Validate `Name` NotEmpty.

- [ ] entity+config+DbSet+migration → test fail-first → contracts+slices+endpoints+Program.cs → PASS + full test xanh → commit `feat(booking): xe (Vehicle) — CRUD + migration`.

---

## Task 3: TourGuideAssignment — phân công HDV cho chuyến

**Files:**
- `src/TourKit.Infrastructure/Entities/TourGuideAssignment.cs`, config, DbSet, migration `AddGuideAssignment`
- `src/TourKit.Api/Booking/GuideAssignmentContracts.cs` + `Booking/Features/{CreateGuideAssignment,UpdateGuideAssignment,DeleteGuideAssignment,ListGuideAssignments}.cs` + `Booking/GuideAssignmentEndpoints.cs`
- `Program.cs`; Test `tests/TourKit.UnitTests/Booking/GuideAssignmentSlicesTests.cs`

Entity (bám `TourGuide`):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Phân công HDV cho chuyến (legacy TourGuide): HDV = Provider (Guide) gắn vào chuyến + giờ đi/về.</summary>
public sealed class TourGuideAssignment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourDepartureId { get; set; }   // tour_id
    public Guid ProviderId { get; set; }         // provider_id (HDV)
    public DateTimeOffset? TimeGo { get; set; }
    public DateTimeOffset? TimeCome { get; set; }
    public DateTimeOffset? TimeReturn { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
}
```
Config: index `(TenantId, TourDepartureId)`.

Contracts:
```csharp
public sealed record CreateGuideAssignmentRequest(Guid TourDepartureId, Guid ProviderId, DateTimeOffset? TimeGo, DateTimeOffset? TimeCome, DateTimeOffset? TimeReturn, string? Note, int Status);
public sealed record UpdateGuideAssignmentRequest(Guid ProviderId, DateTimeOffset? TimeGo, DateTimeOffset? TimeCome, DateTimeOffset? TimeReturn, string? Note, int Status);
public sealed record GuideAssignmentResponse(Guid Id, Guid TourDepartureId, Guid ProviderId, DateTimeOffset? TimeGo, DateTimeOffset? TimeCome, DateTimeOffset? TimeReturn, string? Note, int Status);
```
Slices mirror Customers. **List** `Paged<GuideAssignmentResponse>` — nhận `?page=&size=` VÀ optional `?departureId=` (Guid?) filter. Create validate departure + provider tồn tại (`Error.Validation`). Endpoints `/api/v1/guide-assignments`, perms `GuideView`/`GuideManage`.

- [ ] entity+config+DbSet+migration → test fail-first (create với departure/provider không tồn tại → Validation; roundtrip; list filter theo departureId) → contracts+slices+endpoints+Program.cs → PASS + full test xanh → commit `feat(booking): phân công HDV cho chuyến (TourGuideAssignment) — CRUD + migration`.

---

## Task 4: Frontend — 2 trang CRUD

**Files:**
- `web/src/features/vehicles/{vehicleTypes.ts,vehiclesCrud.ts,VehiclesPage.tsx}`
- `web/src/features/guides/{guideAssignmentTypes.ts,guideAssignmentsCrud.ts,GuideAssignmentsPage.tsx}`
- `web/src/app/router.tsx` + `AppShell.tsx`

- [ ] **Vehicles** — mirror providers CRUD. Columns name/firmName/seatType/status. Form: name, firmName, seatType(number), status(number). basePath `/api/v1/vehicles`. Perms view `vehicle.view`, mutate `vehicle.manage`. Route `/vehicles`; nav `{key:'/vehicles',label:'Xe',perm:'vehicle.view'}`.
- [ ] **GuideAssignments** — mirror providers CRUD. Columns tourDepartureId/providerId/timeGo(dateText)/status. Form: tourDepartureId (Select từ departures list `ListDepartures` — dùng một query gọi `/api/v1/tour-departures?page=1&size=200`, create only), providerId (Select từ `providersCrud.useList({page:1,size:200})` — lọc client-side type Guide nếu muốn, không bắt buộc), timeGo/timeCome/timeReturn (DatePickerField → ISO), note(TextArea), status(number). basePath `/api/v1/guide-assignments`. Perms view `guide.view`, mutate `guide.manage`. Route `/guide-assignments`; nav `{key:'/guide-assignments',label:'Phân công HDV',perm:'guide.view'}`.
- [ ] Verify `npm run build && npm run lint && npm run test` → commit `feat(web): xe + phân công HDV`.

---

## Self-Review
- **Bám hệ cũ:** `Vehicle`↔`vehicle` (name/nameFirm/typeSeat); `TourGuideAssignment`↔`TourGuide` (tour_id/provider_id/TimeGo/TimeCome/TimeReturn). ✔
- **HDV = Provider(Guide):** không tạo entity guide trùng — assignment gắn ProviderId. ✔
- **SQLite-safe:** DateTimeOffset qua converter long; list order CreatedAt. ✔
- **Deferred (ghi rõ):** `Calendar` (lịch điều hành — view UI), quỹ vé ứng HDV (`TicketFund`), ký xác nhận giờ (SignatureGuide) — cần requirement.

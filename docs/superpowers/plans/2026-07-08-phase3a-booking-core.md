# Phase 3a — Booking core: Mở chuyến + Đặt khách — Implementation Plan

> Inline. ĐỌC TRƯỚC: backend-conventions §4/§5/§6, business-spec §3/§4/§5, DB §B3/§B4.

**Goal:** Mở **chuyến khởi hành** (`TourDeparture`) từ một mẫu tour, rồi **đặt khách** lên chuyến (`Order`) với tổng tiền tính từ giá mẫu (người lớn/trẻ em). Cô lập tenant, gác quyền `departure.*`/`booking.*`.

**Architecture:** `TourDeparture` (entity TPT đã có từ Phase 1) nay có endpoint mở/liệt kê/xem — mở từ template thì `ParentTourId = templateId`. `Order` (entity mới, ITenantEntity): Code, TourDepartureId, CustomerId, AdultQty, ChildQty, TotalAmount, Status. Đặt khách: nạp template qua `departure.ParentTourId` → tính `TotalAmount = AdultQty*PriceAdult + ChildQty*PriceChild`. Thêm mã quyền vào catalog (tự seed + policy). Migration `AddBooking` (chỉ `Order`).

**Phạm vi vs sau:** slice = mở chuyến + đặt khách (Order mức số lượng). Defer: TourCustomer mức từng ghế + giữ chỗ/cọc/trạng thái ghế (Phase 3b), huỷ, phụ thu/chiết khấu, phiếu thu (Finance Phase 5), cột tổng tài chính denormalize đầy đủ.

---

## File Structure
```
src/TourKit.Infrastructure/
  Entities/OrderStatus.cs, Order.cs                 # NEW
  Persistence/Configurations/OrderConfiguration.cs  # NEW
  Persistence/AppDbContext.cs                        # MODIFY — DbSet<Order>
  Migrations/*_AddBooking.cs                         # NEW
src/TourKit.Api/
  Authz/Permissions.cs                              # MODIFY — departure.*, booking.*
  Booking/DepartureContracts.cs, DepartureEndpoints.cs, BookingContracts.cs, BookingEndpoints.cs  # NEW
  Program.cs                                         # MODIFY — Map...
tests/TourKit.Tests/Booking/BookingEndpointTests.cs  # NEW
```

---

### Task 1: Order entity + permissions + config + migration
- `OrderStatus` enum: Draft=1, Confirmed=2, Cancelled=3.
- `Order`: Code, TourDepartureId, CustomerId, AdultQty, ChildQty, TotalAmount(decimal), Status.
- `Permissions`: departure.view/create, booking.view/create (group "Booking").
- `OrderConfiguration`: Code maxlen 64, TotalAmount precision(18,2), index (TenantId, TourDepartureId), (TenantId, CustomerId), (TenantId, Status).
- DbSet + migration `AddBooking` + update DB.

### Task 2: Endpoint mở chuyến (TourDeparture)
- DTO: CreateDepartureRequest(Guid? TemplateId, string Code, string Title, DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots), DepartureResponse(...).
- `/api/v1/tour-departures`: POST(departure.create) — nếu TemplateId có → ParentTourId=TemplateId; GET(departure.view), GET{id}(departure.view).

### Task 3: Endpoint đặt khách (Order) + list
- DTO: CreateBookingRequest(Guid CustomerId, int AdultQty, int ChildQty), OrderResponse(Id, Code, TourDepartureId, CustomerId, AdultQty, ChildQty, decimal TotalAmount, OrderStatus Status).
- `POST /api/v1/tour-departures/{departureId}/bookings`(booking.create): validate departure + customer tồn tại; nạp template qua ParentTourId (null → 400 "chuyến chưa gắn mẫu để tính giá"); TotalAmount = AdultQty*PriceAdult + ChildQty*PriceChild; Order Code tự sinh; Status=Confirmed; 201.
- `GET /api/v1/orders`(booking.view): list.

### Task 4: Tests + full suite
- mở chuyến từ template → 201; đặt khách → 201 + total đúng; list orders → 1; cô lập tenant. `dotnet test` xanh.

---

## Self-Review
Spec: mở chuyến + đặt khách tính giá + gate ✅. Ngoài phạm vi: ghế/giữ chỗ/cọc/huỷ/finance. Rủi ro: giá lấy từ template qua ParentTourId (đảm bảo mở chuyến từ template); Customer/Departure là ITenantEntity nên query đã lọc tenant → không đặt chéo tenant được.

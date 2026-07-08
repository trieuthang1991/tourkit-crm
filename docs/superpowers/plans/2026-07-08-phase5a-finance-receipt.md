# Phase 5a — Finance: Phiếu thu + Công nợ — Implementation Plan

> Inline. ĐỌC TRƯỚC: backend-conventions §5/§6, business-spec §7, DB §B7/§I (cột tổng/denormalize — ở đây tính công nợ động, chưa denormalize).

**Goal:** Ghi nhận **phiếu thu** (tiền khách nộp) theo đơn, và xem **công nợ** đơn = `TotalAmount − tổng đã thu`. Gác quyền `receipt.*`. Cô lập tenant.

**Architecture:** `ReceiptVoucher` (ITenantEntity mới): Code, OrderId, Amount, Method, IssuedAt, Note. Endpoint ghi phiếu + liệt kê theo đơn + tính balance (Total/Paid/Outstanding) — công nợ tính **động** bằng `SUM(receipts)` (chưa denormalize lên Order; đúng §I: chỉ denormalize khi grid cần sort — chưa cần ở slice này). Migration `AddReceipt`.

**Phạm vi vs sau:** slice = ghi phiếu thu + công nợ 1 đơn. Defer: phiếu chi (payment), duyệt phiếu (approval engine §7.3), phương thức thanh toán lookup, báo cáo công nợ tổng hợp (Materialized View §F4), denormalize cột tổng lên Order.

---

## File Structure
```
src/TourKit.Infrastructure/
  Entities/ReceiptVoucher.cs                          # NEW
  Persistence/Configurations/ReceiptVoucherConfiguration.cs  # NEW
  Persistence/AppDbContext.cs                          # MODIFY — DbSet
  Migrations/*_AddReceipt.cs                            # NEW
src/TourKit.Api/
  Authz/Permissions.cs                                # MODIFY — receipt.*
  Finance/ReceiptContracts.cs, ReceiptEndpoints.cs    # NEW
  Program.cs                                            # MODIFY — MapReceiptEndpoints
tests/TourKit.Tests/Finance/ReceiptEndpointTests.cs   # NEW
```

---

### Task 1: Entity + permissions + config + migration
- `ReceiptVoucher`: Code, OrderId, Amount(decimal), Method(string), IssuedAt(DateTimeOffset), Note?.
- `Permissions`: ReceiptView="receipt.view", ReceiptCreate="receipt.create" (group "Finance").
- Config: Code maxlen 64, Amount precision(18,2), Method maxlen 50, Note maxlen 500, index (TenantId, OrderId).
- DbSet + migration `AddReceipt` + update DB.

### Task 2: Endpoints + balance
- DTO: CreateReceiptRequest(decimal Amount, string Method, string? Note), ReceiptResponse(Id, Code, OrderId, Amount, Method, IssuedAt, Note), OrderBalanceResponse(Guid OrderId, decimal Total, decimal Paid, decimal Outstanding).
- `POST /api/v1/orders/{orderId}/receipts`(receipt.create): order tồn tại? (404 nếu không) · Amount>0 (else 400) · tạo ReceiptVoucher (Code "RCP-...", IssuedAt=UtcNow) · 201.
- `GET /api/v1/orders/{orderId}/receipts`(receipt.view): list phiếu của đơn.
- `GET /api/v1/orders/{orderId}/balance`(receipt.view): Total = order.TotalAmount; Paid = SUM(receipts.Amount); Outstanding = Total − Paid.

### Task 3: Tests + full suite
- ghi phiếu 5tr trên đơn 13tr → balance {13tr, 5tr, 8tr}; 2 phiếu cộng dồn; amount ≤ 0 → 400; cô lập tenant. `dotnet test` xanh.

---

## Self-Review
Spec: phiếu thu + công nợ động + gate ✅. Ngoài phạm vi: phiếu chi, duyệt, lookup method, báo cáo tổng hợp, denormalize. Rủi ro: công nợ tính SUM mỗi lần (chấp nhận ở mức 1 đơn; grid tổng hợp sẽ dùng MV sau — §F4). Order/Receipt là ITenantEntity → cô lập tự động.

# Đợt 1 — DB Hardening + Search Infra (plan chi tiết)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (hoặc subagent-driven-development) để thực thi từng task. Steps dùng checkbox `- [ ]`.

**Goal:** Vá 2 lỗi Critical hiệu năng (stats + phân trang in-memory), bổ sung index/unique/precision còn thiếu, và dựng hạ tầng search không dấu (SearchName/PhoneNormalized + pg_trgm) — làm chuẩn mẫu cho mọi màn sau.

**Architecture:** Mọi thay đổi service giữ LINQ-translatable (test dùng **InMemory** — KHÔNG raw SQL trong service). Cột search là **cột thường tính bằng C#** khi Create/Update (chạy mọi provider); phần Postgres-only (extension, GIN trigram, backfill SQL) nằm trong migration có guard `migrationBuilder.ActiveProvider`.

**Tech Stack:** EF Core 9 + Npgsql, xUnit + WebApplicationFactory (InMemory).

## Global Constraints

- Kill process `:5075` trước mọi build/test (memory `kill-api-before-dotnet-test`).
- 592 test hiện có PHẢI xanh sau mỗi task. Build 0 warning (TreatWarningsAsErrors).
- Chạy `gitnexus_impact({target:"CustomerService"})` trước Task 3-5; `gitnexus_detect_changes` trước commit.
- Postgres local 17, DB `tourkit_dev`. Migration đặt tên theo pattern hiện có (`20260720xxxxxx_<Name>`).
- KHÔNG đổi hành vi nghiệp vụ filter (kết quả lọc phải giữ nguyên — chỉ nhanh hơn).

---

### Task 1: Migration `HardenCustomerOrderIndexes` — index/unique/precision

**Files:**
- Modify: `src/TourKit.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`
- Modify: `OrderConfiguration.cs`, `AgentConfiguration.cs`, `FlightTicketConfiguration.cs`, `FlightTicketIndividualConfiguration.cs`, `TourDepartureConfiguration.cs` (cùng thư mục)
- Create: migration mới

- [ ] **Step 1: Kiểm tra trùng TRƯỚC khi thêm unique** (nếu trùng thì dừng, báo user):
```powershell
psql -h localhost -U tourkit -d tourkit_dev -c "SELECT ""Code"", COUNT(*) FROM ""Customers"" GROUP BY ""TenantId"",""Code"" HAVING COUNT(*)>1 UNION ALL SELECT ""Code"", COUNT(*) FROM ""Orders"" GROUP BY ""TenantId"",""Code"" HAVING COUNT(*)>1;"
```
Kỳ vọng: 0 rows.

- [ ] **Step 2: Sửa CustomerConfiguration** — thêm sau `HasIndex(TenantId, FullName)`:
```csharp
builder.Property(x => x.TempBalance).HasPrecision(18, 2);   // H5: cột tiền phải có precision
builder.HasIndex(x => new { x.TenantId, x.Phone });
builder.HasIndex(x => new { x.TenantId, x.Email });
builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
```

- [ ] **Step 3: Unique Code các entity còn lại** — `OrderConfiguration`: `builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();`; tương tự `Agent.Code`, `FlightTicket.Pnr`, `FlightTicketIndividual.Code` (đổi index non-unique hiện có nếu đã tồn tại). `TourDepartureConfiguration`: đổi `HasIndex(x => x.IsClosed)` → `HasIndex(x => new { x.TenantId, x.IsClosed })`.

- [ ] **Step 4: Tạo migration + apply:**
```powershell
dotnet ef migrations add HardenCustomerOrderIndexes -p src/TourKit.Infrastructure -s src/TourKit.Api
dotnet ef database update -p src/TourKit.Infrastructure -s src/TourKit.Api
```
Kỳ vọng: apply OK trên Postgres local.

- [ ] **Step 5: Test + commit:** kill :5075 → `dotnet test tests/TourKit.Tests -v minimal` PASS → commit `feat(db): index/unique Phone-Email-Code + precision TempBalance (hardening H2/H3/H5/M4)`.

---

### Task 2: Cột `SearchName` + `PhoneNormalized` (search không dấu, chuẩn hoá SĐT)

**Files:**
- Create: `src/TourKit.Shared/Text/VietnameseText.cs`
- Modify: `src/TourKit.Shared/Entities/Customer.cs` (+2 property)
- Modify: `CustomerConfiguration.cs` (maxlength + index)
- Modify: `src/TourKit.Application/Customers/CustomerService.cs` (set 2 cột ở Create/Update; move `NormalizePhone` sang VietnameseText)
- Create: migration `CustomerSearchColumns` (kèm SQL Postgres-only: extension + backfill + GIN)
- Test: `tests/TourKit.UnitTests/Text/VietnameseTextTests.cs`

**Interfaces (Produces):**
```csharp
public static class VietnameseText
{
    /// <summary>lower + bỏ dấu tiếng Việt (đ→d) — dùng cho cột search và input search.</summary>
    public static string? NormalizeSearch(string? s);
    /// <summary>Chỉ giữ số; +84/84→0 (di chuyển từ CustomerService.NormalizePhone).</summary>
    public static string NormalizePhone(string? phone);
}
```

- [ ] **Step 1: Test trước** (`VietnameseTextTests.cs`):
```csharp
[Theory]
[InlineData("Nguyễn Văn Ân", "nguyen van an")]
[InlineData("Đặng THỊ Yến", "dang thi yen")]
[InlineData(null, null)]
public void NormalizeSearch_removes_diacritics(string? input, string? expected)
    => Assert.Equal(expected, VietnameseText.NormalizeSearch(input));

[Theory]
[InlineData("+84 901 234-567", "0901234567")]
[InlineData("0901234567", "0901234567")]
public void NormalizePhone_normalizes(string input, string expected)
    => Assert.Equal(expected, VietnameseText.NormalizePhone(input));
```
Chạy: FAIL (chưa có class).

- [ ] **Step 2: Implement `VietnameseText`** — `NormalizeSearch`: `s.Trim().ToLowerInvariant()` → `Normalize(NormalizationForm.FormD)` → bỏ `UnicodeCategory.NonSpacingMark` → thay `đ→d` (sau lower thì chỉ còn 'đ'). `NormalizePhone`: move nguyên logic từ `CustomerService.cs:396-410`, CustomerService gọi lại qua class mới (xoá private cũ).

- [ ] **Step 3: Entity + config:** `Customer.cs` thêm:
```csharp
/// <summary>lower + không dấu của FullName — cột search (C# set khi ghi, GIN trigram trên Postgres).</summary>
public string? SearchName { get; set; }
/// <summary>SĐT chuẩn hoá (chỉ số, +84→0) — search + dò trùng.</summary>
public string? PhoneNormalized { get; set; }
```
Config: `HasMaxLength(200)` / `(32)`; `HasIndex(x => new { x.TenantId, x.PhoneNormalized });` (SearchName KHÔNG cần B-tree — GIN ở Step 5).

- [ ] **Step 4: Set khi ghi** — trong `CreateAsync` và `UpdateAsync` của CustomerService:
```csharp
entity.SearchName = VietnameseText.NormalizeSearch(dto.FullName);
entity.PhoneNormalized = VietnameseText.NormalizePhone(dto.Phone) is { Length: > 0 } pn ? pn : null;
```

- [ ] **Step 5: Migration `CustomerSearchColumns`** — sau khi `dotnet ef migrations add`, MỞ file migration thêm vào cuối `Up()` (guard Postgres):
```csharp
if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
{
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
    // Backfill 2 cột cho data hiện có (unaccent xử lý được đ→d)
    migrationBuilder.Sql("""UPDATE "Customers" SET "SearchName" = lower(unaccent("FullName")) WHERE "SearchName" IS NULL;""");
    // NULLIF chứ KHÔNG regexp_replace(...,NULL) — hàm strict sẽ NULL hoá tất cả. '(\d{8,})' khớp logic C# (tổng >9 số).
    migrationBuilder.Sql("""UPDATE "Customers" SET "PhoneNormalized" = NULLIF(regexp_replace(regexp_replace("Phone", '\D', '', 'g'), '^84(\d{8,})$', '0\1'), '') WHERE "Phone" IS NOT NULL;""");
    migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Customers_SearchName_trgm" ON "Customers" USING gin ("SearchName" gin_trgm_ops);""");
}
```
Apply: `dotnet ef database update`. Verify backfill: `psql ... -c "SELECT COUNT(*) FROM \"Customers\" WHERE \"SearchName\" IS NULL AND \"IsDeleted\"=false;"` → kỳ vọng 0.

- [ ] **Step 6: Test toàn bộ + commit** `feat(search): cột SearchName/PhoneNormalized + unaccent/pg_trgm + GIN trigram`.

---

### Task 3: Fix C1 — Stats đếm ở SQL

**Files:**
- Modify: `src/TourKit.Application/Common/IRepository.cs` (+`CountAsync`)
- Modify: `src/TourKit.Infrastructure/Repositories/Repository.cs`
- Create: `src/TourKit.Application/Customers/ICustomerQueries.cs` + `src/TourKit.Infrastructure/Repositories/CustomerQueries.cs` (đăng ký DI trong Program.cs cạnh `IReportQueries`)
- Modify: `CustomerService.GetStatsAsync`

**Interfaces (Produces):**
```csharp
// IRepository<T> thêm:
Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
// ICustomerQueries (aggregate không ép được qua IRepository generic — theo pattern IReportQueries):
public interface ICustomerQueries
{
    /// <summary>(soDonTheoKhach) đếm ở SQL: bao nhiêu KH có đúng 1 đơn / >1 đơn.</summary>
    Task<(int FirstTime, int Repeat)> BuyerCountsAsync();
}
```

- [ ] **Step 1: Test trước** — thêm vào `tests/TourKit.Tests/Api/` một test qua endpoint `/api/v1/customers/stats` (seed 2 KH, 1 KH có 1 đơn, 1 KH có 2 đơn → `FirstTimeBuyers=1, RepeatBuyers=1, Total=2`). Test này PASS cả trước lẫn sau (guard hành vi không đổi) — chạy trước để có baseline.
- [ ] **Step 2: Implement** — `Repository.CountAsync` = `(predicate is null ? Set : Set.Where(predicate)).CountAsync()`. `CustomerQueries.BuyerCountsAsync` (LINQ, InMemory-safe):
```csharp
var counts = await db.Orders.GroupBy(o => o.CustomerId)
    .Select(g => g.Count()).ToListAsync();
return (counts.Count(c => c == 1), counts.Count(c => c > 1));
```
`GetStatsAsync` viết lại: `Total = await repo.CountAsync()`, `NewToday/NewThisMonth = await repo.CountAsync(c => c.CreatedAt >= ...)`, buyer từ `ICustomerQueries`. XOÁ 2 dòng `ListAsync()` full-table.
- [ ] **Step 3: Test PASS + commit** `perf(db): GetStatsAsync đếm ở SQL thay vì load toàn bảng (C1)`.

---

### Task 4: Fix C2 — Phân trang ở SQL (fast-path) + search thông minh

**Files:**
- Modify: `CustomerService.ListAsync`

Thiết kế: filter chia 2 nhóm. **Nhóm cột thật** (Q, CustomerType, Source, CreatedFrom/To) dịch được xuống SQL. **Nhóm JSON/aggregate** (City, Gender, MarketGroup, Collaborator, Campaign, Branch, Group, Department, Segment, Tag, AssignedTo, CreatedBy, Revenue, Care, BirthdayMonth, buckets) phải in-memory (jsonb). → **Fast-path**: khi KHÔNG có filter nhóm 2 → `repo.PageAsync(page, size, predicate)` (Count+Skip/Take+OrderBy CreatedAt desc ở SQL — khớp thứ tự hiện tại), aggregate (orders/care/userNames) chỉ tính cho ≤size dòng của trang. **Slow-path**: giữ logic hiện tại (không hồi quy).

- [ ] **Step 1: Search thông minh trong predicate** — thay khối `kw` hiện tại (`:28-32`):
```csharp
var kw = Norm(f.Q);
var kwSearch = VietnameseText.NormalizeSearch(kw);              // lower + không dấu
var kwPhone = VietnameseText.NormalizePhone(kw);                // "" nếu không có số
var phoneMode = kwPhone.Length >= 4 && kw != null && kw.All(ch => !char.IsLetter(ch));
// predicate:
(kw == null ||
    (phoneMode
        ? (c.PhoneNormalized != null && c.PhoneNormalized.Contains(kwPhone))
        : ((c.SearchName != null && c.SearchName.Contains(kwSearch!)) ||
           (c.Code != null && c.Code.Contains(kw)) ||
           (c.Email != null && c.Email.Contains(kw)))))
```
(Gõ "nguyen" khớp "Nguyễn"; gõ "0901"/"+84901" khớp cả hai dạng. LIKE '%%' trên SearchName được GIN trigram tăng tốc ở Postgres.)
- [ ] **Step 2: Fast-path** — `bool hasSoftFilter = f.City != null || f.Gender != null || ... || f.NotContactedBucket != null;` (liệt kê đủ 16 field nhóm 2). Nếu `!hasSoftFilter`: `var (items, total) = await repo.PageAsync(page, size, predicate);` → lấy `ids` của items → query orders/cares CHỈ theo ids đó (như `:43-51` nhưng tập nhỏ) → map → return. Ngược lại: giữ nguyên code cũ (đổi mỗi khối kw như Step 1).
- [ ] **Step 3: Test hành vi** — thêm test: seed 25 KH (tên "Nguyễn A01..25"), gọi `/api/v1/customers?page=2&size=20` → `Items.Count==5, Total==25`; gọi `?q=nguyen` (không dấu) → Total==25. Chạy PASS.
- [ ] **Step 4: Toàn bộ test + verify UI** — 592 test PASS; chạy app, màn `/Customers` search "nguyen" phải ra kết quả, search số "0901" ra khách +84. Commit `perf(search): phân trang SQL fast-path + search không dấu/SĐT chuẩn hoá (C2+S2)`.

---

### Task 5: Đồng bộ dò trùng dùng PhoneNormalized

**Files:** Modify `CustomerService.FindByPhoneAsync`, `FindDuplicatesAsync`

- [ ] **Step 1:** `FindByPhoneAsync` bỏ trick `EndsWith(tail)` → `repo.ListAsync(c => c.PhoneNormalized == norm)` (đã có index). `FindDuplicatesAsync` group theo `PhoneNormalized` cột (vẫn ListAsync toàn bảng — chấp nhận M3 mức này, đã nhanh hơn nhờ không parse).
- [ ] **Step 2:** Test dò trùng hiện có PASS + commit `refactor(search): dò trùng dùng cột PhoneNormalized`.

---

### Task 6: Chốt đợt

- [ ] Chạy cả 3 project test (592) PASS; build 0 warning.
- [ ] Chạy app thật: login demo → `/Customers` → search không dấu + số điện thoại + phân trang + stats đúng số.
- [ ] `gitnexus_detect_changes()` xác nhận scope; `npx gitnexus analyze` re-index.
- [ ] Cập nhật `docs/UI-CONVENTIONS.md` mục 2: "search text dùng SearchName (không dấu); search SĐT dùng PhoneNormalized — pattern bắt buộc cho mọi service".

**Ngoài phạm vi Đợt 1 (đã chủ ý hoãn):** GetFunnelAsync (chưa màn Razor nào dùng), FilterOptions distinct (L2), H4/M1/M2 (chuẩn hoá FK — đợt riêng có migrate data), áp SearchName cho 11+ service khác (làm dần theo wave khi chạm).

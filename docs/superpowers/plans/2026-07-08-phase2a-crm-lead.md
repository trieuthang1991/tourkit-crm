# Phase 2a — CRM: Lead + Convert to Customer — Implementation Plan

> Inline hoặc subagent-driven. ĐỌC TRƯỚC: backend-conventions §4/§5/§6, business-spec §1 (Lead/Customer), DB §B2/§J.

**Goal:** Quản lý **Lead** (khách tiềm năng) của tenant: CRUD + gán sales + trạng thái phễu, và **convert Lead → Customer** (sinh Customer, đánh dấu lead Won + liên kết). Gác theo quyền `lead.*`. Cô lập tenant.

**Architecture:** `Lead : BaseEntity, ITenantEntity` (FullName, Phone, Email, Source, Status enum, AssignedToUserId?, ConvertedCustomerId?). Thêm mã quyền `lead.*` vào catalog `Permissions` → tự seed + tự có policy (Program loop) → Admin (đủ quyền) tự bao gồm. Endpoint `/api/v1/leads` + `POST /{id}/convert`. Convert tạo `Customer` từ lead trong cùng DbContext. Cần migration `AddLead`.

**Tech Stack:** .NET 9, EF Core 9. 1 entity mới → 1 migration.

**Phạm vi vs sau:** slice này = Lead CRUD + convert. Defer: round-robin auto-assign, CustomerInteraction/CustomerCare, source/type lookup tables (hiện Source là string), import.

---

## File Structure
```
src/TourKit.Infrastructure/
  Entities/LeadStatus.cs, Lead.cs                    # NEW
  Persistence/Configurations/LeadConfiguration.cs    # NEW
  Persistence/AppDbContext.cs                         # MODIFY — DbSet<Lead>
  Migrations/*_AddLead.cs                             # NEW
src/TourKit.Api/
  Authz/Permissions.cs                               # MODIFY — thêm lead.*
  Crm/LeadContracts.cs, LeadEndpoints.cs             # NEW
  Program.cs                                          # MODIFY — MapLeadEndpoints
tests/TourKit.Tests/Crm/LeadEndpointTests.cs         # NEW
```

---

### Task 1: Lead entity + permissions + config + migration
- `LeadStatus` enum: New=1, Contacted=2, Qualified=3, Won=4, Lost=5.
- `Lead`: FullName(req), Phone?, Email?, Source?, Status, AssignedToUserId?, ConvertedCustomerId?.
- `Permissions`: thêm LeadView/Create/Update/Delete/Convert (`lead.view` ...) vào All (group "CRM").
- `LeadConfiguration`: FullName maxlen 200, Phone 32, Email 256, Source 100; index (TenantId, Status), (TenantId, AssignedToUserId).
- DbSet + migration `AddLead` + update DB.

### Task 2: Contracts + endpoints CRUD + convert
- DTO: CreateLeadRequest(FullName, Phone?, Email?, Source?, AssignedToUserId?), UpdateLeadRequest(FullName, Phone?, Email?, Source?, Status, AssignedToUserId?), LeadResponse(...), ConvertResponse(Guid CustomerId).
- `/api/v1/leads`: GET(lead.view), GET{id}(lead.view), POST(lead.create), PUT{id}(lead.update), DELETE{id} soft(lead.delete), POST{id}/convert(lead.convert).
- Convert: nếu `ConvertedCustomerId != null` → 409. Tạo Customer{FullName,Phone}, set lead.Status=Won + ConvertedCustomerId, save. Trả 201 { customerId }.
- Wire MapLeadEndpoints.

### Task 3: Tests + full suite
- CRUD lead; convert → customer xuất hiện ở /customers; convert lần 2 → 409; cô lập tenant.
- `dotnet test` toàn bộ xanh.

---

## Self-Review
Spec: Lead CRUD+convert+gate `lead.*` ✅. Ngoài phạm vi: round-robin, interaction, lookup tables. Rủi ro: convert tạo Customer — Customer là ITenantEntity, interceptor gán tenant hiện tại (từ JWT) đúng. Migration cần chạy.

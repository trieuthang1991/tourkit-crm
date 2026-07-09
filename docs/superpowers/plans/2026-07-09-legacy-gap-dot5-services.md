# Đợt 5 (phần lõi) — Catalog dịch vụ + Bảng giá NCC — Implementation Plan

> REQUIRED SUB-SKILL: superpowers:subagent-driven-development / executing-plans. Steps checkbox.

**Goal:** Lấp gap đã ghi nhận trong code (`OrderCost.ServiceName` là chuỗi tự do, "catalog dịch vụ chưa tồn tại"): thêm **catalog dịch vụ** (`ServiceItem`, bám legacy `services`) và **bảng giá dịch vụ theo NCC** (`ProviderService`, bám legacy `provider_services` + `provider_service_pricing`). Hai module CRUD phân trang.

**Phạm vi:** Đây là phần LÕI, groundable của Đợt 5. Các vertical đặt dịch vụ lẻ (`BookingHotel`, `AirPlaneTicket`, `Visa`, `BookingTicket`), báo giá (`BaoGia`), hoá đơn (`InvoiceBranch`) là hệ thống con lớn — cần requirement chi tiết, làm sau (ghi rõ, không bịa).

**Architecture:** Vertical Slice/CQRS mirror Customers. Migration mới. SQLite-safe.

---

## Mẫu nhân bản (đọc trước)
- `src/TourKit.Api/Providers/{ProviderContracts.cs,ProviderEndpoints.cs,Features/*}` (paged CRUD với create có `Code`, update không — mẫu asymmetric) + `Persistence/Configurations/ProviderConfiguration.cs`.
- `src/TourKit.Infrastructure/Entities/{Provider.cs,OrderCost.cs}`.
- `AppDbContext.cs`, `Program.cs`, `Authz/Permissions.cs`.
- Frontend: `web/src/features/providers/*`, `web/src/app/{router.tsx,AppShell.tsx}`.

---

## Task 1: Permissions
Modify `Permissions.cs`:
- [ ] Const + `All` (group "Provider"):
```csharp
    public const string ServiceView = "service.view";
    public const string ServiceManage = "service.manage";
```
`All`: `(ServiceView,"Provider"),(ServiceManage,"Provider"),`
- [ ] Build + commit `feat(authz): permission catalog dịch vụ + bảng giá NCC`.

---

## Task 2: ServiceItem — catalog dịch vụ (CRUD phân trang)

**Files:**
- `src/TourKit.Infrastructure/Entities/ServiceItem.cs`, config, DbSet, migration `AddServiceItem`
- `src/TourKit.Api/Providers/ServiceItemContracts.cs` + `Providers/Features/{CreateServiceItem,UpdateServiceItem,DeleteServiceItem,ListServiceItems}.cs` + `Providers/ServiceItemEndpoints.cs`
- `Program.cs`; Test `tests/TourKit.UnitTests/Providers/ServiceItemSlicesTests.cs`

Entity (bám legacy `services`):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Danh mục dịch vụ (legacy services): loại dịch vụ có thể mua của NCC (phòng, xe, vé, visa...).</summary>
public sealed class ServiceItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Category { get; set; }   // 1 Hotel,2 Vehicle,3 Restaurant,4 Guide,5 Air,6 Visa,7 Other (khớp ProviderType + Visa)
    public int Status { get; set; }
}
```
Config: `Code` maxLength 64, `Name` 255; index `(TenantId, Code)`.

Contracts:
```csharp
public sealed record CreateServiceItemRequest(string Code, string Name, int Category, int Status);
public sealed record UpdateServiceItemRequest(string Name, int Category, int Status);
public sealed record ServiceItemResponse(Guid Id, string Code, string Name, int Category, int Status);
```
Slices mirror Providers (create có Code, update không; List paged; Update/Delete `ICommand<bool>`). Endpoints `/api/v1/service-items`, perms `ServiceView`/`ServiceManage`.

- [ ] entity+config+DbSet+migration → test fail-first (validator Code+Name NotEmpty; roundtrip) → contracts+slices+endpoints+Program.cs → run PASS + full test xanh → commit `feat(catalog): danh mục dịch vụ (ServiceItem) — CRUD + migration`.

---

## Task 3: ProviderService — bảng giá dịch vụ theo NCC (CRUD phân trang, lọc theo provider)

**Files:**
- `src/TourKit.Infrastructure/Entities/ProviderService.cs`, config, DbSet, migration `AddProviderService`
- `src/TourKit.Api/Providers/ProviderServiceContracts.cs` + `Providers/Features/{CreateProviderService,UpdateProviderService,DeleteProviderService,ListProviderServices}.cs` + `Providers/ProviderServiceEndpoints.cs`
- `Program.cs`; Test `tests/TourKit.UnitTests/Providers/ProviderServiceSlicesTests.cs`

Entity (bám `provider_services` + `provider_service_pricing`, gộp):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Bảng giá 1 dịch vụ của 1 NCC (legacy provider_services + provider_service_pricing gộp):
/// giá hợp đồng (contract) vs giá công bố (public), theo tên gói giá.</summary>
public sealed class ProviderService : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? ServiceItemId { get; set; }
    public string? PriceName { get; set; }        // price_name
    public decimal ContractPrice { get; set; }    // contract_price
    public decimal PublicPrice { get; set; }      // public_price
    public int AmountOfPeople { get; set; }       // amount_of_people
    public string? Note { get; set; }
    public int Status { get; set; }
}
```
Config: `ContractPrice`/`PublicPrice` `HasPrecision(18,2)`; index `(TenantId, ProviderId)`.

Contracts:
```csharp
public sealed record CreateProviderServiceRequest(Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice, int AmountOfPeople, string? Note, int Status);
public sealed record UpdateProviderServiceRequest(Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice, int AmountOfPeople, string? Note, int Status);
public sealed record ProviderServiceResponse(Guid Id, Guid ProviderId, Guid? ServiceItemId, string? PriceName, decimal ContractPrice, decimal PublicPrice, int AmountOfPeople, string? Note, int Status);
```
Slices mirror Providers. **List** `Paged<ProviderServiceResponse>` — nhận `?page=&size=` VÀ optional `?providerId=` (nếu có thì `.Where(x => x.ProviderId == providerId)`). Create validate Provider tồn tại (`Error.Validation`). Endpoints `/api/v1/provider-services`, perms `ServiceView`/`ServiceManage`.

- [ ] entity+config+DbSet+migration → test fail-first (create với ProviderId không tồn tại → Validation; roundtrip; list filter theo providerId) → contracts+slices+endpoints+Program.cs → run PASS + full test xanh → commit `feat(providers): bảng giá dịch vụ theo NCC (ProviderService) — CRUD + migration`.

---

## Task 4: Frontend — 2 trang CRUD

**Files:**
- `web/src/features/services/{serviceItemTypes.ts,serviceItemsCrud.ts,ServiceItemsPage.tsx,providerServiceTypes.ts,providerServicesCrud.ts,ProviderServicesPage.tsx}`
- `web/src/app/router.tsx` + `AppShell.tsx`

- [ ] **ServiceItems** — mirror providers. Columns code/name/category/status. Form: code (create only), name, category (NumberField hoặc SelectField map {1:'Khách sạn',2:'Vận chuyển',3:'Nhà hàng',4:'HDV',5:'Hàng không',6:'Visa',7:'Khác'}), status. basePath `/api/v1/service-items`. Perms view `service.view`, mutate `service.manage`. Route `/service-items`; nav `{key:'/service-items',label:'Danh mục dịch vụ',perm:'service.view'}`.
- [ ] **ProviderServices** — mirror providers. Columns providerId/priceName/contractPrice(money)/publicPrice(money)/status. Form: providerId (Select từ `providersCrud.useList({page:1,size:200})`, create only), serviceItemId (Select từ serviceItems list, nullable), priceName, contractPrice(number), publicPrice(number), amountOfPeople(number), note(TextArea), status. basePath `/api/v1/provider-services`. Perms view `service.view`, mutate `service.manage`. Route `/provider-services`; nav `{key:'/provider-services',label:'Bảng giá NCC',perm:'service.view'}`.
- [ ] Verify `npm run build && npm run lint && npm run test` → commit `feat(web): danh mục dịch vụ + bảng giá NCC`.

---

## Self-Review
- **Bám hệ cũ:** `ServiceItem`↔`services`; `ProviderService`↔`provider_services`+`provider_service_pricing` (contract/public price). ✔
- **Lấp gap thật:** catalog dịch vụ mà `OrderCost` từng ghi chú thiếu. (Liên kết `OrderCost.ProviderServiceId` = follow-up, không bắt buộc đợt này.) ✔
- **Mirror Providers:** CRUD asymmetric (create có Code, update không). ✔
- **SQLite-safe:** decimal converter đã có; list order CreatedAt. ✔
- **Deferred (ghi rõ):** đặt dịch vụ lẻ hotel/vé/visa, báo giá (`BaoGia`), hoá đơn (`InvoiceBranch`) — hệ con lớn, cần requirement.

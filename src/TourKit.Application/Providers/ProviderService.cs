using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Providers;

public sealed class ProviderService(
    IRepository<Provider> repo,
    IRepository<Shared.Entities.ProviderService> providerServiceRepo,
    IRepository<OrderCost> orderCostRepo,
    IRepository<PaymentVoucher> paymentRepo,
    IValidator<CreateProviderDto> createValidator,
    IValidator<UpdateProviderDto> updateValidator) : IProviderService
{
    public async Task<PagedResult<ProviderDto>> ListAsync(int page, int size, ProviderListFilter? filter = null)
    {
        var f = filter ?? new ProviderListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var prov = string.IsNullOrWhiteSpace(f.Province) ? null : f.Province.Trim();
        var (items, total) = await repo.PageAsync(page, size, p =>
            (f.Type == null || (int)p.Type == f.Type) &&
            (f.Status == null || p.Status == f.Status) &&
            (f.BranchId == null || p.BranchId == f.BranchId) &&
            (f.MarketTypeId == null || p.MarketTypeId == f.MarketTypeId) &&
            (prov == null || (p.Province != null && p.Province.Contains(prov))) &&
            (f.CreatedFrom == null || p.CreatedAt >= f.CreatedFrom) &&
            (f.CreatedTo == null || p.CreatedAt <= f.CreatedTo) &&
            (kw == null ||
                p.Code.Contains(kw) ||
                p.Name.Contains(kw) ||
                (p.Phone != null && p.Phone.Contains(kw)) ||
                (p.Email != null && p.Email.Contains(kw)) ||
                (p.ContactPerson != null && p.ContactPerson.Contains(kw))));

        // Làm giàu công nợ NCC cho các dòng trong trang: tổng mua (OrderCost) + đã trả (phiếu chi đã duyệt).
        var ids = items.Select(p => p.Id).ToHashSet();
        var costByProvider = (await orderCostRepo.ListAsync(c => ids.Contains(c.ProviderId)))
            .GroupBy(c => c.ProviderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ActualAmount));
        var paidByProvider = (await paymentRepo.ListAsync(p => p.IsRecognized && p.ProviderId != null && ids.Contains(p.ProviderId.Value)))
            .GroupBy(p => p.ProviderId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var dtos = items
            .Select(p => Map(p, costByProvider.GetValueOrDefault(p.Id), paidByProvider.GetValueOrDefault(p.Id)))
            .ToList();
        return new PagedResult<ProviderDto>(dtos, total, page, size);
    }

    public async Task<ProviderStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byStatus = await repo.CountByAsync(p => p.Status);
        var total = byStatus.Values.Sum();
        var active = byStatus.GetValueOrDefault(1);

        return new ProviderStatsDto(total, active, total - active);
    }

    public async Task<ProviderDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<ProviderDto> CreateAsync(CreateProviderDto dto)
    {
        await Validate(createValidator, dto);

        var code = dto.Code.Trim();
        if (await repo.AnyAsync(p => p.Code == code))
        {
            throw new ConflictException($"Mã nhà cung cấp '{code}' đã tồn tại.");
        }

        var entity = new Provider
        {
            Code = code,
            Name = dto.Name.Trim(),
            Type = dto.Type,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            TaxCode = dto.TaxCode,
            ContactPerson = dto.ContactPerson,
            BankAccount = dto.BankAccount,
            BankName = dto.BankName,
            PaymentTermId = dto.PaymentTermId,
            Province = dto.Province,
            BranchId = dto.BranchId,
            MarketTypeId = dto.MarketTypeId,
            Rate = dto.Rate,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    /// <summary>Sửa NCC cùng bảng dịch vụ — xem <see cref="IProviderService.UpdateWithServicesAsync"/>.</summary>
    public async Task UpdateWithServicesAsync(Guid id, UpdateProviderDto dto, IReadOnlyList<ProviderServiceLineDto> services)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        GanTruong(entity, dto);
        repo.Update(entity);

        var hienCo = await providerServiceRepo.ListAsync(s => s.ProviderId == id);
        var giuLai = services.Where(x => x.Id is not null).Select(x => x.Id!.Value).ToHashSet();

        // Dòng cũ không còn trong danh sách gửi lên = người dùng đã bỏ nó khỏi form.
        foreach (var cu in hienCo.Where(s => !giuLai.Contains(s.Id)))
        {
            providerServiceRepo.Remove(cu);
        }

        foreach (var dong in services)
        {
            if (dong.Id is Guid dongId && hienCo.FirstOrDefault(s => s.Id == dongId) is { } sua)
            {
                GanDongDichVu(sua, dong);
                providerServiceRepo.Update(sua);
                continue;
            }

            var moi = new Shared.Entities.ProviderService { ProviderId = id };
            GanDongDichVu(moi, dong);
            await providerServiceRepo.AddAsync(moi);
        }

        // Tính nguyên tử đến từ việc hai repository dùng chung một DbContext: lần gọi ĐẦU đã ghi
        // toàn bộ thay đổi của cả hai loại thực thể trong một transaction — hỏng bất kỳ dòng nào là
        // không có gì được ghi, đúng như ROLLBACK của uspInsertHotel bên hệ cũ.
        //
        // Lần gọi thứ hai là no-op ở môi trường thật (không còn thay đổi nào đang chờ). Nó tồn tại
        // vì lớp IRepository KHÔNG bộc lộ đơn vị công việc: nhìn vào chữ ký hàm thì không thể biết
        // hai repo có chung ngữ cảnh hay không. Bỏ nó đi thì mã vẫn chạy đúng nhờ một chi tiết ngầm,
        // và bài kiểm thử dùng repository giả sẽ đỏ mà không chỉ ra được vì sao.
        await repo.SaveChangesAsync();
        await providerServiceRepo.SaveChangesAsync();
    }

    private static void GanDongDichVu(Shared.Entities.ProviderService e, ProviderServiceLineDto d)
    {
        e.ServiceItemId = d.ServiceItemId;
        e.PriceName = d.PriceName?.Trim();
        e.ContractPrice = d.ContractPrice;
        e.PublicPrice = d.PublicPrice;
        e.CurrencyCode = d.CurrencyCode;
        e.AmountOfPeople = d.AmountOfPeople;
        e.Note = d.Note?.Trim();
        e.Status = d.Status;
    }

    public async Task UpdateAsync(Guid id, UpdateProviderDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        GanTruong(entity, dto);
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    /// <summary>Gán trường của NCC — dùng chung cho cả hai đường sửa để chúng không lệch nhau.</summary>
    private static void GanTruong(Provider entity, UpdateProviderDto dto)
    {
        entity.Name = dto.Name.Trim();
        entity.Type = dto.Type;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.TaxCode = dto.TaxCode;
        entity.ContactPerson = dto.ContactPerson;
        entity.BankAccount = dto.BankAccount;
        entity.BankName = dto.BankName;
        entity.PaymentTermId = dto.PaymentTermId;
        entity.Province = dto.Province;
        entity.BranchId = dto.BranchId;
        entity.MarketTypeId = dto.MarketTypeId;
        entity.Rate = dto.Rate;
        entity.Status = dto.Status;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ProviderDto Map(Provider p, decimal totalCost = 0m, decimal paid = 0m) => new(
        p.Id, p.Code, p.Name, p.Type, p.Phone, p.Email, p.Address,
        p.TaxCode, p.ContactPerson, p.BankAccount, p.BankName, p.PaymentTermId, p.Rate, p.Status,
        p.Province, p.BranchId, p.MarketTypeId,
        totalCost, paid, OrderMath.Outstanding(totalCost, paid));
}

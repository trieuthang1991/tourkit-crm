using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Crm;

/// <summary>
/// Lead (khách tiềm năng, phễu bán). Convert tạo <see cref="Customer"/> mới từ thông tin lead,
/// đánh dấu lead Won + gắn ConvertedCustomerId — chỉ convert được 1 lần (convert lại → Conflict).
/// </summary>
public sealed class LeadService(
    IRepository<Lead> repo,
    IRepository<Customer> customerRepo,
    IValidator<CreateLeadDto> createValidator,
    IValidator<UpdateLeadDto> updateValidator,
    TourKit.Shared.Security.ICurrentUserContext currentUser) : ILeadService
{
    public async Task<PagedResult<LeadDto>> ListAsync(int page, int size, LeadListFilter? filter = null)
    {
        var f = filter ?? new LeadListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        var src = string.IsNullOrWhiteSpace(f.Source) ? null : f.Source.Trim();
        var (items, total) = await repo.PageAsync(page, size, l =>
            (f.Status == null || (int)l.Status == f.Status) &&
            (f.AssignedToUserId == null || l.AssignedToUserId == f.AssignedToUserId) &&
            (f.BranchId == null || l.BranchId == f.BranchId) &&
            (f.CreatedByUserId == null || l.CreatedByUserId == f.CreatedByUserId) &&
            (src == null || (l.Source != null && l.Source.Contains(src))) &&
            (f.CreatedFrom == null || l.CreatedAt >= f.CreatedFrom) &&
            (f.CreatedTo == null || l.CreatedAt <= f.CreatedTo) &&
            (kw == null ||
                l.FullName.Contains(kw) ||
                (l.Phone != null && l.Phone.Contains(kw)) ||
                (l.Email != null && l.Email.Contains(kw)) ||
                (l.Source != null && l.Source.Contains(kw))));
        var dtos = items.Select(Map).ToList();
        return new PagedResult<LeadDto>(dtos, total, page, size);
    }

    public async Task<LeadFilterOptionsDto> GetFilterOptionsAsync()
    {
        var all = await repo.ListAsync();
        var sources = all
            .Where(l => !string.IsNullOrWhiteSpace(l.Source))
            .Select(l => l.Source!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCulture)
            .ToList();
        return new LeadFilterOptionsDto(sources);
    }

    public async Task<LeadStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new LeadStatsDto(
            all.Count,
            all.Count(l => l.Status == LeadStatus.New),
            all.Count(l => l.Status == LeadStatus.Contacted),
            all.Count(l => l.Status == LeadStatus.Qualified),
            all.Count(l => l.Status == LeadStatus.Won),
            all.Count(l => l.Status == LeadStatus.Lost),
            all.Count(l => l.ConvertedCustomerId != null));
    }

    public async Task<LeadDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new Lead
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            Email = dto.Email,
            Source = dto.Source,
            AssignedToUserId = dto.AssignedToUserId,
            BranchId = dto.BranchId,
            CreatedByUserId = currentUser.UserId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateLeadDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Source = dto.Source;
        entity.Status = dto.Status;
        entity.AssignedToUserId = dto.AssignedToUserId;
        entity.BranchId = dto.BranchId;
        repo.Update(entity);
        await repo.SaveChangesAsync();
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

    public async Task<ConvertLeadResultDto> ConvertAsync(Guid id)
    {
        var lead = await repo.GetByIdAsync(id);
        if (lead is null)
        {
            throw new NotFoundException();
        }

        if (lead.ConvertedCustomerId is not null)
        {
            throw new ConflictException("Lead đã được convert.");
        }

        var customer = new Customer { FullName = lead.FullName, Phone = lead.Phone };
        await customerRepo.AddAsync(customer);

        lead.Status = LeadStatus.Won;
        lead.ConvertedCustomerId = customer.Id;
        repo.Update(lead);

        await customerRepo.SaveChangesAsync();
        await repo.SaveChangesAsync();

        return new ConvertLeadResultDto(customer.Id);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static LeadDto Map(Lead l) => new(
        l.Id, l.FullName, l.Phone, l.Email, l.Source, l.Status, l.AssignedToUserId, l.ConvertedCustomerId, l.BranchId);
}

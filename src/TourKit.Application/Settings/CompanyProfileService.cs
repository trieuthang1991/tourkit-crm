using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Settings;

/// <summary>
/// Hồ sơ công ty (legacy <c>Config</c> — phần doanh nghiệp). Singleton mỗi tenant: <see cref="GetAsync"/>
/// trả bản hiện có hoặc rỗng (chưa thiết lập); <see cref="SaveAsync"/> upsert. Dùng làm BÊN A hợp đồng.
/// </summary>
public sealed class CompanyProfileService(IRepository<CompanyProfile> repo) : ICompanyProfileService
{
    public async Task<CompanyProfileDto> GetAsync()
    {
        var entity = await LoadSingletonAsync();
        return entity is null
            ? new CompanyProfileDto(string.Empty, null, null, null, null, null, null, null, null, null, null)
            : Map(entity);
    }

    public async Task SaveAsync(CompanyProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationAppException("Tên công ty không được trống.");
        }

        var entity = await LoadSingletonAsync();
        if (entity is null)
        {
            entity = new CompanyProfile();
            Apply(entity, dto);
            await repo.AddAsync(entity);
        }
        else
        {
            Apply(entity, dto);
            repo.Update(entity);
        }

        await repo.SaveChangesAsync();
    }

    private async Task<CompanyProfile?> LoadSingletonAsync()
    {
        var all = await repo.ListAsync();
        return all.Count > 0 ? all[0] : null;
    }

    private static void Apply(CompanyProfile e, CompanyProfileDto d)
    {
        e.Name = d.Name.Trim();
        e.ShortName = d.ShortName?.Trim();
        e.Address = d.Address?.Trim();
        e.Hotline = d.Hotline?.Trim();
        e.Email = d.Email?.Trim();
        e.Website = d.Website?.Trim();
        e.TaxCode = d.TaxCode?.Trim();
        e.LegalRepName = d.LegalRepName?.Trim();
        e.LegalRepTitle = d.LegalRepTitle?.Trim();
        e.LicenseNumber = d.LicenseNumber?.Trim();
        e.BankAccount = d.BankAccount?.Trim();
    }

    private static CompanyProfileDto Map(CompanyProfile e) => new(
        e.Name, e.ShortName, e.Address, e.Hotline, e.Email, e.Website,
        e.TaxCode, e.LegalRepName, e.LegalRepTitle, e.LicenseNumber, e.BankAccount);
}

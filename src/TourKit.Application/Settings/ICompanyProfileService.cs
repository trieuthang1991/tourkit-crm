namespace TourKit.Application.Settings;

/// <summary>Hồ sơ công ty (legacy Config) — singleton mỗi tenant: đọc + lưu (upsert).</summary>
public interface ICompanyProfileService
{
    Task<CompanyProfileDto> GetAsync();
    Task SaveAsync(CompanyProfileDto dto);
}

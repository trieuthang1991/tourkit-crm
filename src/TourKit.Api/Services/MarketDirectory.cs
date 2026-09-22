using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Caching;
using TourKit.Infrastructure.Tenancy;

namespace TourKit.Api.Services;

/// <summary>
/// Danh mục THỊ TRƯỜNG có CACHE, dùng chung cho ô chọn cha–con và tra tên ở &gt;10 màn (đơn, NCC,
/// khách, cơ hội, vé, phòng, marketing…).
///
/// Vì sao cần: cây thị trường gần như bất biến và rất nhỏ, nhưng handler <c>?handler=MarketTree</c>
/// gọi lại <see cref="IMarketTypeService.ListAsync"/> mỗi lần một màn danh sách khởi tạo — nạp cả
/// bảng chỉ để đổ một dropdown. Danh mục nhỏ + đọc dày là ca giáo khoa để cache (bám UserDirectory).
///
/// Đánh đổi: thêm/sửa/xoá thị trường có thể chậm hiện tối đa <see cref="Ttl"/> ở các dropdown — nên
/// màn Quản lý thị trường gọi <see cref="InvalidateAsync"/> ngay sau khi ghi. Khoá cache tách theo
/// tenant để không rò dữ liệu giữa các đơn vị (MarketType là ITenantEntity).
/// </summary>
public sealed class MarketDirectory(IMarketTypeService markets, ITkCache cache, AmbientTenantContext tenant)
{
    // Thị trường đổi hiếm hơn nhân viên nhiều, nên để dài hơn UserDirectory (60s) — vẫn có
    // InvalidateAsync xoá ngay khi ghi nên không sợ dropdown "kẹt" dữ liệu cũ.
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private string Key => $"marketdir:{tenant.TenantId}";

    public Task<IReadOnlyList<MarketTypeDto>> ListAsync()
        => cache.GetOrSetAsync<IReadOnlyList<MarketTypeDto>>(Key, Ttl, async () => await markets.ListAsync());

    /// <summary>Bảng tra Id → tên, cho các handler cần đổi Id thành tên hiển thị.</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync()
    {
        var list = await ListAsync();
        return list.ToDictionary(m => m.Id, m => m.Name);
    }

    /// <summary>Xoá cache ngay sau khi thêm/sửa/xoá thị trường để dropdown không hiện dữ liệu cũ.</summary>
    public Task InvalidateAsync() => cache.RemoveAsync(Key);
}

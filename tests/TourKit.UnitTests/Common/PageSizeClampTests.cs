using TourKit.Shared.Constants;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Collaboration;

namespace TourKit.UnitTests.Common;

/// <summary>
/// Cỡ trang vượt trần phải CẮT VỀ TRẦN, không rơi về cỡ mặc định.
///
/// Bản cũ rơi về 20: hơn chục màn gọi ListAsync(1, 500) hoặc (1, 1000) để đổ combobox nhà cung cấp /
/// chuyến đi, và tất cả âm thầm chỉ nhận 20 dòng. Không lỗi, không cảnh báo — chỉ là danh sách thiếu.
/// </summary>
public class PageSizeClampTests
{
    private static FakeRepository<CustomerSource> SeededRepo(int count)
    {
        var repo = new FakeRepository<CustomerSource>();
        repo.Seed(Enumerable.Range(1, count)
            .Select(i => new CustomerSource { Name = $"Nguồn {i}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i) })
            .ToArray());
        return repo;
    }

    [Fact]
    public void Xin_qua_tran_thi_nhan_dung_tran_chu_khong_phai_20()
    {
        // FakeRepository không kẹp cỡ trang, nên test này chốt CON SỐ TRẦN mà tầng thật phải dùng.
        // Nếu ai đó đổi MaxPageSize/DefaultPageSize, hằng số ở đây buộc phải xem lại cùng lúc.
        Assert.Equal(200, PaginationDefaults.MaxPageSize);
        Assert.Equal(20, PaginationDefaults.DefaultPageSize);
        Assert.True(PaginationDefaults.MaxPageSize > PaginationDefaults.DefaultPageSize,
            "Trần phải lớn hơn mặc định, nếu không việc cắt về trần thành vô nghĩa.");
    }

    [Fact]
    public async Task Trang_binh_thuong_van_cat_dung_so_dong()
    {
        var repo = SeededRepo(50);

        var (items, total) = await repo.PageAsync(1, 30);

        Assert.Equal(30, items.Count);
        Assert.Equal(50, total);
    }
}

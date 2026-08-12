using System.Linq.Expressions;
using TourKit.Shared.Entities;

namespace TourKit.Application.Sales;

/// <summary>
/// Công thức tiền của Cơ hội, ở MỘT chỗ duy nhất.
///
/// Dạng <see cref="Expression"/> chứ không phải hàm thường: cùng một công thức vừa dùng để cộng ở
/// SQL (<c>SumAsync(GiaTriSelector, …)</c>) vừa dùng để tính cho từng dòng trong bộ nhớ. Viết hai
/// bản — một cho DTO, một cho báo cáo — là kiểu sai kinh điển: hai bản trôi lệch nhau và con số
/// trên thẻ thống kê không khớp tổng của lưới, không ai biết bên nào đúng.
///
/// Cùng lối với <c>BookingMath.SeatCountSelector</c> đang dùng cho số chỗ.
/// </summary>
public static class OpportunityMath
{
    /// <summary>Giá trị dự kiến = số lượng × đơn giá, cộng cả bốn bậc khách.</summary>
    public static readonly Expression<Func<SalesOpportunity, decimal>> GiaTriSelector =
        o => (o.AdultQty * o.PriceAdult)
           + (o.ChildQty * o.PriceChild)
           + (o.ChildSmallQty * o.PriceChildSmall)
           + (o.BabyQty * o.PriceBaby);

    private static readonly Func<SalesOpportunity, decimal> GiaTriHam = GiaTriSelector.Compile();

    /// <summary>Bản chạy trong bộ nhớ của <see cref="GiaTriSelector"/> — dùng khi dựng DTO.</summary>
    public static decimal GiaTri(SalesOpportunity o) => GiaTriHam(o);
}

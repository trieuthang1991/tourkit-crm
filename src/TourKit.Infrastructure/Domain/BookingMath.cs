using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Domain;

/// <summary>
/// CÔNG THỨC TÍNH TIỀN ĐẶT CHỖ — một chỗ duy nhất. Đổi cách tính giá thì sửa ở ĐÂY,
/// mọi nơi (Order.TotalRevenue, trạng thái chỗ, công nợ dòng) đều dùng chung.
/// </summary>
public static class BookingMath
{
    /// <summary>
    /// Tổng doanh thu một dòng đặt chỗ (tiền khách phải trả):
    /// Σ(giá × số lượng theo 4 nhóm tuổi) + Σ phụ thu − Σ chiết khấu.
    /// (Hoa hồng KHÔNG nằm ở đây — là chi phí trả seller, không phải giá khách.)
    /// </summary>
    public static decimal LineTotal(TourCustomer s)
    {
        var price = (s.Quantity * s.PriceAdult)
            + (s.AmountChildren * s.PriceChild)
            + (s.AmountChildrenSmall * s.PriceChildSmall)
            + (s.QuantityBaby * s.PriceBaby);

        var surcharge = s.Surcharge + s.ChildSurcharge + s.ChildSurchargeSmall + s.BabySurcharge;
        var discount = s.Discount + s.ChildDiscount + s.ChildDiscountSmall + s.BabyDiscount;

        return price + surcharge - discount;
    }
}

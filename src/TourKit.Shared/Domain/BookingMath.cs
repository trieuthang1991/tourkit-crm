using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Shared.Domain;

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

    /// <summary>
    /// Số chỗ (ghế) một dòng đặt chiếm: tổng số khách 4 nhóm tuổi.
    /// Quy ước: mỗi khách (kể cả em bé) tính 1 chỗ. Đổi quy ước sức chứa thì sửa ở ĐÂY.
    /// </summary>
    public static int SeatCount(TourCustomer s)
        => s.Quantity + s.AmountChildren + s.AmountChildrenSmall + s.QuantityBaby;

    /// <summary>
    /// Suy TRẠNG THÁI chỗ từ tiền cọc vs giá dòng + cờ giữ chỗ (bảng flow "Giữ chỗ" hệ cũ).
    /// Quy tắc suy trạng thái nằm MỘT CHỖ ở đây — đừng suy lại nơi khác.
    /// </summary>
    public static SeatStatus DeriveSeatStatus(TourCustomer s)
    {
        if (s.Status != 0)
        {
            return SeatStatus.Cancelled;
        }

        var lineTotal = LineTotal(s);
        if (s.UpfrontAmount >= lineTotal && lineTotal > 0m)
        {
            return SeatStatus.Paid;
        }

        if (s.UpfrontAmount > 0m)
        {
            return SeatStatus.Deposited;
        }

        return s.HoldExpiresAt is not null ? SeatStatus.Held : SeatStatus.HeldConfirmed;
    }
}

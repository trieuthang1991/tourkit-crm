using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class BookingMathTests
{
    [Fact]
    public void SeatCount_sums_all_four_age_quantities()
    {
        var seat = new TourCustomer
        {
            Quantity = 2, AmountChildren = 1, AmountChildrenSmall = 1, QuantityBaby = 1,
        };
        Assert.Equal(5, BookingMath.SeatCount(seat));
    }

    /// <summary>
    /// Bản Expression (đẩy xuống SQL) phải cho cùng kết quả với bản hàm (chạy trong bộ nhớ).
    ///
    /// Hai bản tồn tại song song vì SumIntAsync cần Expression để dịch sang SQL, còn chỗ khác gọi
    /// hàm trực tiếp. Sửa một bản mà quên bản kia thì luật sức chứa lệch nhau giữa các đường ghi —
    /// đúng loại lỗi không ai phát hiện cho tới khi có người đặt chỗ.
    /// </summary>
    [Fact]
    public void SeatCountSelector_khop_voi_SeatCount()
    {
        var bienThe = new[]
        {
            new TourCustomer { Quantity = 2, AmountChildren = 1, AmountChildrenSmall = 1, QuantityBaby = 1 },
            new TourCustomer { Quantity = 0, AmountChildren = 0, AmountChildrenSmall = 0, QuantityBaby = 0 },
            new TourCustomer { Quantity = 9 },
            new TourCustomer { QuantityBaby = 3 },
            new TourCustomer { AmountChildren = 4, AmountChildrenSmall = 2 },
        };

        var theoExpression = BookingMath.SeatCountSelector.Compile();
        foreach (var s in bienThe)
        {
            Assert.Equal(BookingMath.SeatCount(s), theoExpression(s));
        }
    }
}

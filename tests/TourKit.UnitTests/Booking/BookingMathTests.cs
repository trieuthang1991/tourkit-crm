using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;

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
}

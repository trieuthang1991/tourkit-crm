namespace TourKit.Shared.Enums;

/// <summary>Loại dịch vụ lẻ đặt cho khách/đơn (gộp legacy BookingHotel/AirPlaneTicket/Visa/BookingTicket).</summary>
public enum ServiceBookingType
{
    Hotel = 1,
    Flight = 2,
    Visa = 3,
    Ticket = 4,
    Transfer = 5,
    Other = 99,
}

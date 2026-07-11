namespace TourKit.Shared.Enums;

/// <summary>Phạm vi dòng dự trù: 0 = cả đoàn (mặc định — khớp dữ liệu báo giá cũ), 1 = theo đầu khách.</summary>
public enum QuoteLineScope
{
    PerGroup = 0,
    PerPerson = 1,
}

/// <summary>Loại dịch vụ dòng báo giá — khớp nhóm module hệ cũ (BookingHotel/CarManagement/Visa/AirPlaneTicket...).</summary>
public enum QuoteLineServiceType
{
    Other = 0,
    Hotel = 1,
    Transport = 2,
    Guide = 3,
    Meal = 4,
    Ticket = 5,
    Visa = 6,
    Flight = 7,
    Insurance = 8,
}

using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking;

/// <summary>Lịch thanh toán NCC theo booking dịch vụ — CRUD từng đợt + cảnh báo hạn chi (đến hạn/quá hạn).</summary>
public interface IServicePaymentTermService
{
    Task<IReadOnlyList<ServicePaymentTermDto>> ListByBookingAsync(Guid bookingId);
    Task<ServicePaymentTermDto> CreateAsync(Guid bookingId, CreateServicePaymentTermDto dto);
    Task UpdateAsync(Guid bookingId, Guid id, UpdateServicePaymentTermDto dto);
    Task DeleteAsync(Guid bookingId, Guid id);

    /// <summary>Đợt sắp đến hạn (trong <paramref name="withinDays"/> ngày tới) + đã quá hạn, còn chờ chi. Tenant-scoped.</summary>
    Task<IReadOnlyList<ServicePaymentTermAlertDto>> GetDueAlertsAsync(int withinDays = 7);
}

using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Chuyển chuyến cho đơn (legacy TransferHistory): dời đơn + toàn bộ chỗ sang chuyến đích (đổi lịch),
/// GIỮ NGUYÊN giá/doanh thu. Kiểm sức chứa chuyến đích (tái dùng guard overbooking), chặn chuyến đã đóng,
/// ghi lịch sử chuyển + lý do. Chênh giá (nếu có) xử lý qua chi phí/phụ thu — ngoài phạm vi thao tác này.
/// </summary>
public sealed class TourTransferService(
    IRepository<TourTransfer> transferRepo,
    IRepository<Order> orderRepo,
    IRepository<TourCustomer> seatRepo,
    IRepository<TourDeparture> departureRepo) : ITourTransferService
{
    public async Task<TourTransferDto> TransferAsync(Guid orderId, TransferOrderDto dto)
    {
        var order = await orderRepo.GetByIdAsync(orderId) ?? throw new NotFoundException();

        if (order.TourDepartureId == dto.ToDepartureId)
        {
            throw new ValidationAppException("Đơn đã ở chuyến này.");
        }

        var target = await departureRepo.GetByIdAsync(dto.ToDepartureId)
            ?? throw new ValidationAppException("Chuyến đích không tồn tại.");

        if (target.IsClosed)
        {
            throw new ConflictException("Chuyến đích đã đóng, không thể chuyển sang.");
        }

        var seats = await seatRepo.ListAsync(s => s.OrderId == orderId);
        var movingActive = seats.Where(s => s.Status == 0).Sum(BookingMath.SeatCount);

        // Guard overbooking chuyến đích (cùng công thức với đặt chỗ mới).
        var activeOnTarget = await seatRepo.ListAsync(s => s.TourDepartureId == dto.ToDepartureId && s.Status == 0);
        var usedOnTarget = activeOnTarget.Sum(BookingMath.SeatCount);
        if (target.TotalSlots > 0 && usedOnTarget + movingActive > target.TotalSlots)
        {
            throw new ConflictException(
                $"Chuyến đích không đủ chỗ: còn {target.TotalSlots - usedOnTarget}/{target.TotalSlots}.");
        }

        var fromDepartureId = order.TourDepartureId;

        // Dời đơn + toàn bộ chỗ (kể cả đã huỷ — giữ đơn nhất quán trên 1 chuyến). Giá không đổi.
        order.TourDepartureId = dto.ToDepartureId;
        orderRepo.Update(order);
        foreach (var seat in seats)
        {
            seat.TourDepartureId = dto.ToDepartureId;
            seatRepo.Update(seat);
        }

        var transfer = new TourTransfer
        {
            OrderId = orderId,
            FromDepartureId = fromDepartureId,
            ToDepartureId = dto.ToDepartureId,
            Reason = dto.Reason?.Trim(),
            TransferredAt = DateTimeOffset.UtcNow,
        };
        await transferRepo.AddAsync(transfer);

        await orderRepo.SaveChangesAsync();
        await seatRepo.SaveChangesAsync();
        await transferRepo.SaveChangesAsync();

        return Map(transfer);
    }

    public async Task<IReadOnlyList<TourTransferDto>> ListByOrderAsync(Guid orderId)
    {
        var items = await transferRepo.ListAsync(t => t.OrderId == orderId);
        return items.OrderByDescending(t => t.TransferredAt).Select(Map).ToList();
    }

    private static TourTransferDto Map(TourTransfer t) => new(
        t.Id, t.OrderId, t.FromDepartureId, t.ToDepartureId, t.Reason, t.TransferredAt);
}

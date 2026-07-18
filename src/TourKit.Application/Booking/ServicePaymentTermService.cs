using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Booking;

/// <summary>
/// Lịch thanh toán NCC theo booking dịch vụ (legacy ServicePaymentTerm). Mỗi booking có 0..n đợt chi
/// (số tiền + hạn). Cung cấp CRUD từng đợt + truy vấn cảnh báo đến hạn/quá hạn cho dashboard điều hành.
/// </summary>
public sealed class ServicePaymentTermService(
    IRepository<ServicePaymentTerm> repo,
    IRepository<ServiceBooking> bookingRepo,
    IRepository<Provider> providerRepo,
    IValidator<CreateServicePaymentTermDto> createValidator,
    IValidator<UpdateServicePaymentTermDto> updateValidator) : IServicePaymentTermService
{
    public async Task<IReadOnlyList<ServicePaymentTermDto>> ListByBookingAsync(Guid bookingId)
    {
        var terms = await repo.ListAsync(t => t.ServiceBookingId == bookingId);
        return terms.OrderBy(t => t.DueDate).ThenBy(t => t.CreatedAt).Select(Map).ToList();
    }

    public async Task<ServicePaymentTermDto> CreateAsync(Guid bookingId, CreateServicePaymentTermDto dto)
    {
        await Validate(createValidator, dto);

        // Đợt chi phải gắn vào một booking có thật (và cùng tenant — repo đã lọc theo tenant).
        _ = await bookingRepo.GetByIdAsync(bookingId) ?? throw new NotFoundException();

        var entity = new ServicePaymentTerm
        {
            ServiceBookingId = bookingId,
            OrderCostId = dto.OrderCostId,
            Amount = dto.Amount,
            DueDate = dto.DueDate,
            Note = dto.Note,
            Status = dto.Status,
            PaidAmount = dto.PaidAmount,
            PaymentVoucherId = dto.PaymentVoucherId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid bookingId, Guid id, UpdateServicePaymentTermDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null || entity.ServiceBookingId != bookingId)
        {
            throw new NotFoundException();
        }

        entity.OrderCostId = dto.OrderCostId;
        entity.Amount = dto.Amount;
        entity.DueDate = dto.DueDate;
        entity.Note = dto.Note;
        entity.Status = dto.Status;
        entity.PaidAmount = dto.PaidAmount;
        entity.PaymentVoucherId = dto.PaymentVoucherId;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid bookingId, Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null || entity.ServiceBookingId != bookingId)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ServicePaymentTermAlertDto>> GetDueAlertsAsync(int withinDays = 7)
    {
        var horizon = DateTimeOffset.UtcNow.AddDays(withinDays <= 0 ? 7 : withinDays);

        // Chỉ đợt còn chờ chi (Status = 0) tới hạn trong cửa sổ (gồm cả quá hạn: DueDate < now < horizon).
        var terms = await repo.ListAsync(t => t.Status == 0 && t.DueDate <= horizon);
        if (terms.Count == 0)
        {
            return [];
        }

        // Làm giàu mã dịch vụ + tên NCC theo lô.
        var bookingIds = terms.Select(t => t.ServiceBookingId).ToHashSet();
        var bookings = (await bookingRepo.ListAsync(b => bookingIds.Contains(b.Id)))
            .ToDictionary(b => b.Id);
        var providerIds = bookings.Values.Where(b => b.ProviderId is not null).Select(b => b.ProviderId!.Value).ToHashSet();
        var providerNames = (await providerRepo.ListAsync(p => providerIds.Contains(p.Id)))
            .ToDictionary(p => p.Id, p => p.Name);

        var now = DateTimeOffset.UtcNow;
        return terms
            .OrderBy(t => t.DueDate)
            .Select(t =>
            {
                var booking = bookings.GetValueOrDefault(t.ServiceBookingId);
                var providerName = booking?.ProviderId is { } pid ? providerNames.GetValueOrDefault(pid) : null;
                var daysUntil = (int)Math.Floor((t.DueDate - now).TotalDays);
                return new ServicePaymentTermAlertDto(
                    t.Id, t.ServiceBookingId, booking?.Code ?? string.Empty, providerName,
                    t.Amount, t.PaidAmount, t.Amount - t.PaidAmount,
                    t.DueDate, daysUntil, t.DueDate < now, t.Note);
            })
            .ToList();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ServicePaymentTermDto Map(ServicePaymentTerm t) => new(
        t.Id, t.ServiceBookingId, t.OrderCostId, t.Amount, t.DueDate,
        t.Note, t.Status, t.PaidAmount, t.Amount - t.PaidAmount, t.PaymentVoucherId);
}

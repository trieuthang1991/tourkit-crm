using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.B2B;

/// <summary>
/// Đặt chỗ của Đại lý (B2B §4.2.4) — tạo từ yêu cầu báo giá đã Confirmed (điều kiện tài liệu quy định).
/// Quản lý hành khách (§4.2.5). TotalAmount = QuotedAmount của quote.
/// </summary>
public sealed class AgentBookingService(
    IRepository<AgentBooking> repo,
    IRepository<AgentQuoteRequest> quoteRepo,
    IRepository<AgentPassenger> passengerRepo) : IAgentBookingService
{
    public async Task<PagedResult<AgentBookingSummaryDto>> ListAsync(int page, int size, Guid? agentId)
    {
        var (items, total) = agentId is { } aid
            ? await repo.PageAsync(page, size, b => b.AgentId == aid)
            : await repo.PageAsync(page, size);
        var dtos = items
            .Select(b => new AgentBookingSummaryDto(b.Id, b.AgentId, b.QuoteRequestId, b.Code, b.TotalAmount, b.Status))
            .ToList();
        return new PagedResult<AgentBookingSummaryDto>(dtos, total, page, size);
    }

    public async Task<AgentBookingDto> GetAsync(Guid id)
    {
        var booking = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return await MapAsync(booking);
    }

    public async Task<AgentBookingDto> CreateFromQuoteAsync(CreateAgentBookingDto dto)
    {
        var quote = await quoteRepo.GetByIdAsync(dto.QuoteRequestId)
            ?? throw new ValidationAppException("Yêu cầu báo giá không tồn tại.");

        if (quote.Status != AgentQuoteStatus.Confirmed)
        {
            throw new ConflictException("Chỉ tạo Booking từ yêu cầu báo giá đã Confirmed.");
        }

        if (await repo.AnyAsync(b => b.QuoteRequestId == dto.QuoteRequestId))
        {
            throw new ConflictException("Yêu cầu báo giá này đã có Booking.");
        }

        var booking = new AgentBooking
        {
            AgentId = quote.AgentId,
            QuoteRequestId = quote.Id,
            Code = dto.Code.Trim(),
            TotalAmount = quote.QuotedAmount ?? 0m,
            Status = 0,
            Note = dto.Note,
        };
        await repo.AddAsync(booking);
        await repo.SaveChangesAsync();

        return await MapAsync(booking);
    }

    public async Task UpdateStatusAsync(Guid id, int status)
    {
        var booking = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        booking.Status = status;
        repo.Update(booking);
        await repo.SaveChangesAsync();
    }

    public async Task<AgentPassengerDto> AddPassengerAsync(Guid bookingId, AddAgentPassengerDto dto)
    {
        if (!await repo.AnyAsync(b => b.Id == bookingId))
        {
            throw new NotFoundException();
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            throw new ValidationAppException("Tên hành khách bắt buộc.");
        }

        var passenger = new AgentPassenger
        {
            AgentBookingId = bookingId,
            FullName = dto.FullName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            PassportNo = dto.PassportNo,
            Nationality = dto.Nationality,
            Note = dto.Note,
        };
        await passengerRepo.AddAsync(passenger);
        await passengerRepo.SaveChangesAsync();

        return MapPassenger(passenger);
    }

    public async Task RemovePassengerAsync(Guid bookingId, Guid passengerId)
    {
        var passenger = await passengerRepo.GetByIdAsync(passengerId);
        if (passenger is null || passenger.AgentBookingId != bookingId)
        {
            throw new NotFoundException();
        }

        passengerRepo.Remove(passenger);
        await passengerRepo.SaveChangesAsync();
    }

    private async Task<AgentBookingDto> MapAsync(AgentBooking booking)
    {
        var passengers = await passengerRepo.ListAsync(p => p.AgentBookingId == booking.Id);
        var dtos = passengers.OrderBy(p => p.CreatedAt).Select(MapPassenger).ToArray();
        return new AgentBookingDto(
            booking.Id, booking.AgentId, booking.QuoteRequestId, booking.Code, booking.TotalAmount, booking.Status, booking.Note, dtos);
    }

    private static AgentPassengerDto MapPassenger(AgentPassenger p) =>
        new(p.Id, p.FullName, p.DateOfBirth, p.PassportNo, p.Nationality, p.Note);
}

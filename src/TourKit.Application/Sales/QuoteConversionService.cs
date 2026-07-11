using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Sales;

/// <summary>
/// Chuyển báo giá → đơn (legacy BaoGia.DuyetBooking). Nghiệp vụ chuẩn:
/// - Chỉ báo giá TRẠNG THÁI 2 (chấp nhận) và đã gán khách hàng thật mới chuyển được; mỗi báo giá 1 đơn (idempotent).
/// - Đặt chỗ qua <see cref="IBookingService.CreateBookingAsync"/> (giữ nguyên check chống overbooking);
///   NL/TE/TN của báo giá → AdultQty/ChildQty/ChildSmallQty.
/// - Doanh thu đơn = TỔNG BÁO GIÁ (giá đã chốt với khách — không phải giá niêm yết chuyến).
/// - Dòng dịch vụ đặt-ngoài (KS/xe/visa/vé/vé bay) → ServiceBooking, số lượng theo phạm vi:
///   theo-khách × tổng khách thật, cả-đoàn giữ nguyên; UnitPrice = GIÁ VỐN (phần phải trả NCC).
/// </summary>
public sealed class QuoteConversionService(
    IRepository<Quote> quoteRepo,
    IRepository<QuoteLine> lineRepo,
    IRepository<Order> orderRepo,
    IRepository<ServiceBooking> serviceBookingRepo,
    IRepository<ProviderService> providerServiceRepo,
    IBookingService bookingService) : IQuoteConversionService
{
    /// <summary>Dòng báo giá loại nào sinh ServiceBooking (đặt dịch vụ lẻ với NCC) — còn lại là chi phí điều hành.</summary>
    private static readonly Dictionary<int, ServiceBookingType> BookableTypes = new()
    {
        [(int)QuoteLineServiceType.Hotel] = ServiceBookingType.Hotel,
        [(int)QuoteLineServiceType.Transport] = ServiceBookingType.Transfer,
        [(int)QuoteLineServiceType.Ticket] = ServiceBookingType.Ticket,
        [(int)QuoteLineServiceType.Visa] = ServiceBookingType.Visa,
        [(int)QuoteLineServiceType.Flight] = ServiceBookingType.Flight,
    };

    public async Task<ConvertQuoteResultDto> ConvertAsync(Guid quoteId, ConvertQuoteDto dto)
    {
        var quote = await quoteRepo.GetByIdAsync(quoteId) ?? throw new NotFoundException();

        if (quote.Status != 2)
        {
            throw new ValidationAppException("Chỉ chuyển được báo giá ở trạng thái Chấp nhận.");
        }

        if (quote.ConvertedOrderId is not null)
        {
            throw new ConflictException("Báo giá này đã được chuyển thành đơn.");
        }

        if (quote.CustomerId is null)
        {
            throw new ValidationAppException("Cần gán khách hàng (CustomerId) cho báo giá trước khi chuyển thành đơn.");
        }

        // Đặt chỗ qua flow chuẩn — giữ nguyên chống overbooking + sinh Order/Seat.
        var order = await bookingService.CreateBookingAsync(dto.TourDepartureId, new CreateBookingDto(
            quote.CustomerId.Value, quote.Adults, quote.Children, quote.Infants, 0));

        // Doanh thu đơn = giá chốt của báo giá (không phải giá niêm yết chuyến).
        var orderEntity = await orderRepo.GetByIdAsync(order.Id) ?? throw new NotFoundException();
        orderEntity.TotalRevenue = quote.TotalAmount;
        orderRepo.Update(orderEntity);

        // Dòng dịch vụ đặt-ngoài → ServiceBooking gắn vào đơn.
        var lines = await lineRepo.ListAsync(l => l.QuoteId == quoteId);
        var totalPax = quote.Adults + quote.Children + quote.Infants;
        var bookingCount = 0;
        foreach (var line in lines)
        {
            if (!BookableTypes.TryGetValue(line.ServiceType, out var type))
            {
                continue;
            }

            Guid? providerId = null;
            if (line.ProviderServiceId is { } priceId)
            {
                providerId = (await providerServiceRepo.GetByIdAsync(priceId))?.ProviderId;
            }

            var quantity = line.Scope == (int)QuoteLineScope.PerPerson
                ? line.Quantity * Math.Max(totalPax, 1)
                : line.Quantity;

            await serviceBookingRepo.AddAsync(new ServiceBooking
            {
                Code = "SB-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                Type = type,
                OrderId = order.Id,
                ProviderId = providerId,
                Description = line.Description,
                Quantity = quantity,
                UnitPrice = line.UnitCost,                  // phần phải trả NCC (giá vốn)
                TotalAmount = quantity * line.UnitCost,
                Status = 0,
                Note = $"Sinh từ báo giá {quote.Code}",
            });
            bookingCount++;
        }

        quote.ConvertedOrderId = order.Id;
        quoteRepo.Update(quote);

        await orderRepo.SaveChangesAsync();
        await serviceBookingRepo.SaveChangesAsync();
        await quoteRepo.SaveChangesAsync();

        return new ConvertQuoteResultDto(order.Id, order.Code, bookingCount);
    }
}

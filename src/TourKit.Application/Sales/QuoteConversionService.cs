using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Domain;
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
    IBookingService bookingService,
    IDepartureService departureService) : IQuoteConversionService
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

        var paxTotal = quote.Adults + quote.Children + quote.Infants;

        // Chuyến đích: ghép chuyến sẵn có, hoặc FIT — tự tạo CHUYẾN RIÊNG (legacy SingleTour).
        Guid departureId;
        if (dto.TourDepartureId is { } existingDeparture)
        {
            departureId = existingDeparture;
        }
        else
        {
            if (dto.DepartureDate is null)
            {
                throw new ValidationAppException("Chọn chuyến sẵn có hoặc nhập ngày khởi hành để tạo chuyến riêng (FIT).");
            }

            if (paxTotal < 1)
            {
                throw new ValidationAppException("Tour lẻ FIT cần số khách ≥ 1 trên báo giá.");
            }

            // Chuyến riêng: không template, TotalSlots = đúng số khách → chuyến kín, không nhận thêm khách ngoài.
            var fit = await departureService.CreateAsync(new CreateDepartureDto(
                null, "FIT-" + quote.Code, quote.Title, dto.DepartureDate, dto.EndDate, paxTotal));
            departureId = fit.Id;
        }

        // Giá chỗ = giá 3 hạng khách CỦA BÁO GIÁ (QuoteMath) — giá đã chốt với khách,
        // không phải giá niêm yết mẫu tour; đồng thời cho phép chuyến FIT không template.
        var lines = await lineRepo.ListAsync(l => l.QuoteId == quoteId);
        var pricing = QuoteMath.Price(
            lines, quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent);

        // Đặt chỗ qua flow chuẩn — giữ nguyên chống overbooking + sinh Order/Seat.
        var order = await bookingService.CreateBookingAsync(
            departureId,
            new CreateBookingDto(quote.CustomerId.Value, quote.Adults, quote.Children, quote.Infants, 0),
            new SeatPrices(pricing.AdultPrice, pricing.ChildPrice, pricing.InfantPrice, 0m));

        // Doanh thu đơn = giá chốt của báo giá (không phải giá niêm yết chuyến).
        var orderEntity = await orderRepo.GetByIdAsync(order.Id) ?? throw new NotFoundException();
        orderEntity.TotalRevenue = quote.TotalAmount;
        orderRepo.Update(orderEntity);

        // Dòng dịch vụ đặt-ngoài → ServiceBooking gắn vào đơn.
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
                ? line.Quantity * Math.Max(paxTotal, 1)
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

using FluentValidation;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Booking;

/// <summary>
/// Đặt khách lên chuyến (Order + dòng TourCustomer) + giữ chỗ / xác nhận chỗ / đặt cọc / huỷ chỗ.
/// Trạng thái chỗ suy ra từ upfront_amount vs giá + HoldExpiresAt (BookingMath) — theo flow "Giữ chỗ" hệ cũ.
/// </summary>
public sealed class BookingService(
    IRepository<TourDeparture> departureRepo,
    IRepository<TourCustomer> seatRepo,
    IRepository<Order> orderRepo,
    IRepository<Customer> customerRepo,
    IRepository<TourTemplate> templateRepo,
    IRepository<CancelSeat> cancelSeatRepo,
    IRepository<ReceiptVoucher> receiptRepo,
    IValidator<DepositDto> depositValidator) : IBookingService
{
    public async Task<OrderDto> CreateBookingAsync(Guid departureId, CreateBookingDto dto, SeatPrices? priceOverride = null)
    {
        var (order, _) = await BuildAsync(
            departureId, dto.CustomerId, dto.AdultQty, dto.ChildQty, dto.ChildSmallQty, dto.BabyQty,
            isHold: false, priceOverride);
        return MapOrder(order);
    }

    public async Task<SeatDto> CreateHoldAsync(Guid departureId, CreateBookingDto dto)
    {
        var (_, seat) = await BuildAsync(
            departureId, dto.CustomerId, dto.AdultQty, dto.ChildQty, dto.ChildSmallQty, dto.BabyQty,
            isHold: true, priceOverride: null);
        return MapSeat(seat);
    }

    public async Task<SeatDto> ConfirmSeatAsync(Guid seatId)
    {
        var seat = await seatRepo.GetByIdAsync(seatId);
        if (seat is null)
        {
            throw new NotFoundException();
        }

        if (seat.UpfrontAmount != 0m)
        {
            throw new ValidationAppException("Chỉ xác nhận chỗ đang giữ (chưa đặt cọc).");
        }

        seat.HoldExpiresAt = null;   // chốt chỗ, không nhả
        seatRepo.Update(seat);
        await seatRepo.SaveChangesAsync();

        return MapSeat(seat);
    }

    public async Task<SeatDto> DepositAsync(Guid seatId, DepositDto dto)
    {
        await Validate(depositValidator, dto);

        var seat = await seatRepo.GetByIdAsync(seatId);
        if (seat is null)
        {
            throw new NotFoundException();
        }

        seat.UpfrontAmount += dto.Amount;
        seat.HoldExpiresAt = null;   // đã có tiền → không còn giữ-chỗ-đếm-ngược
        seatRepo.Update(seat);
        await seatRepo.SaveChangesAsync();

        return MapSeat(seat);
    }

    public async Task<SeatDto> CancelSeatAsync(Guid seatId, CancelSeatDto dto)
    {
        var seat = await seatRepo.GetByIdAsync(seatId);
        if (seat is null)
        {
            throw new NotFoundException();
        }

        if (seat.Status != 0)
        {
            throw new ConflictException("Chỗ đã được huỷ.");
        }

        await cancelSeatRepo.AddAsync(new CancelSeat
        {
            TourCustomerId = seat.Id,
            OrderId = seat.OrderId,
            Note = dto.Note,
            RefundAmount = dto.RefundAmount,
            RefundRemain = RefundMath.Remain(seat.UpfrontAmount, dto.RefundAmount),
            RefundPercentage = RefundMath.Percentage(seat.UpfrontAmount, dto.RefundAmount),
        });
        seat.Status = 1;   // statusCancel != 0 → đã huỷ
        seat.HoldExpiresAt = null;
        seatRepo.Update(seat);
        await seatRepo.SaveChangesAsync();

        return MapSeat(seat);
    }

    public async Task<SeatDto> GetSeatAsync(Guid seatId)
    {
        var seat = await seatRepo.GetByIdAsync(seatId);
        if (seat is null)
        {
            throw new NotFoundException();
        }

        return MapSeat(seat);
    }

    public async Task<PagedResult<OrderDto>> ListOrdersAsync(int page, int size, OrderListFilter? filter = null)
    {
        var f = filter ?? new OrderListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Lọc cột thật (trạng thái) ở DB; q (mã/tên KH/tên tour) + khoảng ngày đi lọc sau khi làm giàu.
        var all = await orderRepo.ListAsync(o => f.Status == null || (int)o.Status == f.Status);

        // Nạp theo lô để làm giàu danh sách: tên KH, tên tour + ngày đi, số đã thu (phiếu thu đã ghi nhận).
        var customerIds = all.Select(o => o.CustomerId).ToHashSet();
        var departureIds = all.Select(o => o.TourDepartureId).ToHashSet();
        var orderIds = all.Select(o => o.Id).ToHashSet();

        var customerNames = (await customerRepo.ListAsync(c => customerIds.Contains(c.Id)))
            .ToDictionary(c => c.Id, c => c.FullName);
        var departures = (await departureRepo.ListAsync(d => departureIds.Contains(d.Id)))
            .ToDictionary(d => d.Id, d => d);
        var paidByOrder = (await receiptRepo.ListAsync(r => orderIds.Contains(r.OrderId) && r.IsRecognized))
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var enriched = all.Select(o =>
        {
            departures.TryGetValue(o.TourDepartureId, out var dep);
            var dto = MapOrder(o, customerNames.GetValueOrDefault(o.CustomerId), dep?.Title, dep?.DepartureDate,
                paidByOrder.GetValueOrDefault(o.Id));
            return (o.CreatedAt, Dto: dto);
        });

        bool MatchQ(OrderDto d) =>
            kw == null ||
            d.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (d.CustomerName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.TourTitle?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = enriched
            .Where(x => MatchQ(x.Dto)
                && (f.DepartureFrom == null || (x.Dto.DepartureDate != null && x.Dto.DepartureDate >= f.DepartureFrom))
                && (f.DepartureTo == null || (x.Dto.DepartureDate != null && x.Dto.DepartureDate <= f.DepartureTo)))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(x => x.Dto).ToList();
        return new PagedResult<OrderDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<OrderStatsDto> GetOrderStatsAsync()
    {
        var orders = await orderRepo.ListAsync();
        var orderIds = orders.Select(o => o.Id).ToHashSet();
        var paidByOrder = (await receiptRepo.ListAsync(r => orderIds.Contains(r.OrderId) && r.IsRecognized))
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        decimal revenue = 0m, paid = 0m, outstanding = 0m;
        int draft = 0, confirmed = 0, cancelled = 0;
        foreach (var o in orders)
        {
            var p = paidByOrder.GetValueOrDefault(o.Id);
            revenue += o.TotalRevenue;
            paid += p;
            outstanding += OrderMath.Outstanding(o.TotalRevenue, p);
            switch (o.Status)
            {
                case OrderStatus.Draft: draft++; break;
                case OrderStatus.Confirmed: confirmed++; break;
                case OrderStatus.Cancelled: cancelled++; break;
                default: break;
            }
        }

        return new OrderStatsDto(orders.Count, revenue, paid, outstanding, draft, confirmed, cancelled);
    }

    public async Task<IReadOnlyList<BookingLineDto>> ListOrderLinesAsync(Guid orderId)
    {
        var lines = await seatRepo.ListAsync(l => l.OrderId == orderId);
        return lines.OrderBy(l => l.CreatedAt).Select(MapLine).ToList();
    }

    public async Task<OrderDto> AssignSalesAsync(Guid orderId, AssignSalesDto dto)
    {
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        order.SalesUserId = dto.SalesUserId;
        orderRepo.Update(order);
        await orderRepo.SaveChangesAsync();

        return MapOrder(order);
    }

    /// <summary>
    /// Dựng Order + 1 dòng TourCustomer — dùng chung giữa đặt "chốt ngay" và "giữ chỗ".
    /// isHold = true → giữ chỗ (upfront 0 + đếm ngược). Guard OVERBOOKING + chuyến đã đóng nằm ở ĐÂY.
    /// </summary>
    private async Task<(Order Order, TourCustomer Seat)> BuildAsync(
        Guid departureId, Guid customerId, int adultQty, int childQty, int childSmallQty, int babyQty,
        bool isHold, SeatPrices? priceOverride)
    {
        var departure = await departureRepo.GetByIdAsync(departureId);
        if (departure is null)
        {
            throw new NotFoundException();
        }

        // Giá chỗ: từ mẫu tour (mặc định) hoặc override tường minh (chuyến FIT không template).
        if (departure.ParentTourId is null && priceOverride is null)
        {
            throw new ValidationAppException("Chuyến chưa gắn mẫu tour để tính giá.");
        }

        if (!await customerRepo.AnyAsync(c => c.Id == customerId))
        {
            throw new ValidationAppException("Khách hàng không tồn tại.");
        }

        TourTemplate? template = null;
        if (departure.ParentTourId is { } parentId)
        {
            template = await templateRepo.GetByIdAsync(parentId);
            if (template is null)
            {
                throw new ValidationAppException("Không tìm thấy mẫu tour của chuyến.");
            }
        }

        if (isHold && template is null)
        {
            // Giữ chỗ cần ReservationHours của mẫu tour — chuyến FIT đặt chốt ngay, không giữ chỗ.
            throw new ValidationAppException("Chuyến FIT không hỗ trợ giữ chỗ — đặt chốt ngay.");
        }

        if (departure.IsClosed)
        {
            throw new ConflictException("Chuyến đã đóng, không thể đặt thêm chỗ.");
        }

        var newSeats = adultQty + childQty + childSmallQty + babyQty;
        var activeSeats = await seatRepo.ListAsync(s => s.TourDepartureId == departureId && s.Status == 0);
        var usedSeats = activeSeats.Sum(BookingMath.SeatCount);
        if (departure.TotalSlots > 0 && usedSeats + newSeats > departure.TotalSlots)
        {
            throw new ConflictException(
                $"Vượt sức chứa: còn {departure.TotalSlots - usedSeats}/{departure.TotalSlots} chỗ.");
        }

        var order = new Order
        {
            Code = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            TourDepartureId = departureId,
            CustomerId = customerId,
            BookingType = 0,
            Status = isHold ? OrderStatus.Draft : OrderStatus.Confirmed,
        };

        var seat = new TourCustomer
        {
            OrderId = order.Id,
            TourDepartureId = departureId,
            CustomerId = customerId,
            Quantity = adultQty,
            AmountChildren = childQty,
            AmountChildrenSmall = childSmallQty,
            QuantityBaby = babyQty,
            PriceAdult = priceOverride?.Adult ?? template!.PriceAdult,
            PriceChild = priceOverride?.Child ?? template!.PriceChild,
            PriceChildSmall = priceOverride?.ChildSmall ?? template!.PriceChildSmall,
            PriceBaby = priceOverride?.Baby ?? template!.PriceBaby,
            IsMainContact = true,
        };
        if (isHold)
        {
            seat.HoldExpiresAt = DateTimeOffset.UtcNow.AddHours(template!.ReservationHours);
            seat.ReservationCode = "RSV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        order.TotalRevenue = BookingMath.LineTotal(seat);   // công thức 1 chỗ (Shared/Domain)

        await orderRepo.AddAsync(order);
        await seatRepo.AddAsync(seat);
        await orderRepo.SaveChangesAsync();
        await seatRepo.SaveChangesAsync();

        return (order, seat);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static OrderDto MapOrder(
        Order o, string? customerName = null, string? tourTitle = null,
        DateTimeOffset? departureDate = null, decimal amountPaid = 0m) => new(
        o.Id, o.Code, o.TourDepartureId, o.CustomerId, o.TotalRevenue, o.TotalCost, o.Status, o.SalesUserId,
        customerName, tourTitle, departureDate, amountPaid, OrderMath.Outstanding(o.TotalRevenue, amountPaid));

    /// <summary>Chiếu TourCustomer (chỗ) → SeatDto. Công thức tiền &amp; suy trạng thái nằm ở BookingMath (một chỗ).</summary>
    private static SeatDto MapSeat(TourCustomer s) => new(
        s.Id, s.OrderId, BookingMath.DeriveSeatStatus(s), s.UpfrontAmount, BookingMath.LineTotal(s),
        s.HoldExpiresAt, s.ReservationCode);

    private static BookingLineDto MapLine(TourCustomer l) => new(
        l.Id, l.Quantity, l.AmountChildren, l.AmountChildrenSmall, l.QuantityBaby,
        l.PriceAdult, l.PriceChild, l.PriceChildSmall, l.PriceBaby,
        l.UpfrontAmount, l.ReservationCode, l.IsMainContact);
}

using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Finance;

/// <summary>
/// Phiếu thu + công nợ theo đơn (legacy ReceiptVoucher). Công nợ = Order.TotalRevenue − tổng phiếu thu
/// ĐÃ DUYỆT (IsRecognized). Duyệt 1 cấp ở đây; duyệt nhiều cấp xem <see cref="IReceiptApprovalService"/>.
/// </summary>
public sealed class ReceiptService(
    IRepository<ReceiptVoucher> receiptRepo,
    IRepository<Order> orderRepo,
    IValidator<CreateReceiptDto> createValidator) : IReceiptService
{
    public async Task<ReceiptDto> CreateAsync(Guid orderId, CreateReceiptDto dto)
    {
        await Validate(createValidator, dto);

        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        var receipt = new ReceiptVoucher
        {
            Code = "RCP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = "Phiếu thu",
            IssuedAt = DateTimeOffset.UtcNow,
            OrderId = orderId,
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "cash" : dto.PaymentMethod.Trim(),
            Partner = dto.Partner,
            Note = dto.Note,
            Status = 0,           // 0 = chờ duyệt
            IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
        };
        await receiptRepo.AddAsync(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<ReceiptDto> ApproveAsync(Guid receiptId)
    {
        var receipt = await receiptRepo.GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new NotFoundException();
        }

        if (receipt.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        receipt.Status = 1;          // 1 = đã duyệt
        receipt.IsRecognized = true;
        receiptRepo.Update(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<ReceiptDto> RejectAsync(Guid receiptId)
    {
        var receipt = await receiptRepo.GetByIdAsync(receiptId);
        if (receipt is null)
        {
            throw new NotFoundException();
        }

        if (receipt.Status != 0)
        {
            throw new ConflictException("Phiếu đã xử lý.");
        }

        receipt.Status = 2;          // 2 = từ chối
        receipt.IsRecognized = false;
        receiptRepo.Update(receipt);
        await receiptRepo.SaveChangesAsync();

        return Map(receipt);
    }

    public async Task<IReadOnlyList<ReceiptDto>> ListByOrderAsync(Guid orderId)
    {
        var receipts = await receiptRepo.ListAsync(r => r.OrderId == orderId);
        return receipts.OrderBy(r => r.IssuedAt).Select(Map).ToList();
    }

    public async Task<OrderBalanceDto> GetBalanceAsync(Guid orderId)
    {
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            throw new NotFoundException();
        }

        // Chỉ phiếu ĐÃ DUYỆT mới tính vào công nợ (quy tắc ReceiptQueries.Recognized — một chỗ,
        // ở đây lọc in-memory vì IRepository trả IReadOnlyList chứ không phải IQueryable).
        var receipts = await receiptRepo.ListAsync(r => r.OrderId == orderId);
        var paid = receipts.Where(r => r.IsRecognized).Sum(r => r.Amount);

        return new OrderBalanceDto(orderId, order.TotalRevenue, paid, OrderMath.Outstanding(order.TotalRevenue, paid));
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static ReceiptDto Map(ReceiptVoucher r) => new(
        r.Id, r.Code, r.OrderId, r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Note, r.Status, r.IsRecognized);
}

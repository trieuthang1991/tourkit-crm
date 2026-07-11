using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Sales;

/// <summary>
/// Báo giá (aggregate header + dòng) — mirror pattern 2-repo của ReceiptApprovalService.
/// Dự trù giá (spec 2026-07-11): giá bán dòng = vốn × (1+%LN) khi có vốn; tổng vốn/bán/lãi +
/// giá 3 hạng khách tính ở <see cref="QuoteMath"/> (một chỗ duy nhất). Update = thay toàn bộ dòng.
/// </summary>
public sealed class QuoteService(
    IRepository<Quote> quoteRepo,
    IRepository<QuoteLine> lineRepo,
    IRepository<ProviderService> providerServiceRepo,
    IValidator<CreateQuoteDto> createValidator,
    IValidator<UpdateQuoteDto> updateValidator) : IQuoteService
{
    public async Task<PagedResult<QuoteSummaryDto>> ListAsync(int page, int size)
    {
        var (items, total) = await quoteRepo.PageAsync(page, size);
        var dtos = items
            .Select(q => new QuoteSummaryDto(q.Id, q.Code, q.CustomerName, q.Title, q.ValidUntil, q.Status, q.TotalAmount))
            .ToList();
        return new PagedResult<QuoteSummaryDto>(dtos, total, page, size);
    }

    public async Task<QuoteDto> GetAsync(Guid id)
    {
        var quote = await quoteRepo.GetByIdAsync(id) ?? throw new NotFoundException();
        return await MapAsync(quote);
    }

    public async Task<QuoteDto> CreateAsync(CreateQuoteDto dto)
    {
        await Validate(createValidator, dto);
        await ValidatePriceRefsAsync(dto.Lines);

        var quote = new Quote
        {
            Code = dto.Code.Trim(),
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            Title = dto.Title.Trim(),
            ValidUntil = dto.ValidUntil,
            Status = dto.Status,
            Note = dto.Note,
            Adults = dto.Adults,
            Children = dto.Children,
            Infants = dto.Infants,
            ChildPercent = dto.ChildPercent,
            InfantPercent = dto.InfantPercent,
        };
        await quoteRepo.AddAsync(quote);

        var lines = dto.Lines.Select(l => NewLine(quote.Id, l)).ToList();
        foreach (var line in lines)
        {
            await lineRepo.AddAsync(line);
        }

        ApplyPricing(quote, lines);

        await quoteRepo.SaveChangesAsync();
        await lineRepo.SaveChangesAsync();

        return await MapAsync(quote);
    }

    public async Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto)
    {
        await Validate(updateValidator, dto);
        await ValidatePriceRefsAsync(dto.Lines);

        var quote = await quoteRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        quote.Code = dto.Code.Trim();
        quote.CustomerId = dto.CustomerId;
        quote.CustomerName = dto.CustomerName;
        quote.Title = dto.Title.Trim();
        quote.ValidUntil = dto.ValidUntil;
        quote.Status = dto.Status;
        quote.Note = dto.Note;
        quote.Adults = dto.Adults;
        quote.Children = dto.Children;
        quote.Infants = dto.Infants;
        quote.ChildPercent = dto.ChildPercent;
        quote.InfantPercent = dto.InfantPercent;

        // Thay toàn bộ dòng: xoá cũ, thêm mới.
        var existing = await lineRepo.ListAsync(l => l.QuoteId == id);
        foreach (var line in existing)
        {
            lineRepo.Remove(line);
        }

        var lines = dto.Lines.Select(l => NewLine(id, l)).ToList();
        foreach (var line in lines)
        {
            await lineRepo.AddAsync(line);
        }

        ApplyPricing(quote, lines);
        quoteRepo.Update(quote);

        await quoteRepo.SaveChangesAsync();
        await lineRepo.SaveChangesAsync();

        return await MapAsync(quote);
    }

    public async Task DeleteAsync(Guid id)
    {
        var quote = await quoteRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        var lines = await lineRepo.ListAsync(l => l.QuoteId == id);
        foreach (var line in lines)
        {
            lineRepo.Remove(line);
        }

        quoteRepo.Remove(quote);
        await lineRepo.SaveChangesAsync();
        await quoteRepo.SaveChangesAsync();
    }

    /// <summary>Dòng chọn từ bảng giá NCC thì dòng giá phải tồn tại (tenant filter áp tự động).</summary>
    private async Task ValidatePriceRefsAsync(IEnumerable<CreateQuoteLineDto> lines)
    {
        foreach (var priceId in lines.Where(l => l.ProviderServiceId is not null).Select(l => l.ProviderServiceId!.Value).Distinct())
        {
            if (!await providerServiceRepo.AnyAsync(s => s.Id == priceId))
            {
                throw new ValidationAppException("Bảng giá NCC tham chiếu không tồn tại.");
            }
        }
    }

    /// <summary>Ghi tổng vốn/bán/lãi từ QuoteMath (một chỗ duy nhất).</summary>
    private static void ApplyPricing(Quote quote, IReadOnlyCollection<QuoteLine> lines)
    {
        var pricing = QuoteMath.Price(
            lines, quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent);
        quote.TotalCost = pricing.TotalCost;
        quote.TotalAmount = pricing.TotalAmount;
        quote.TotalProfit = pricing.TotalProfit;
    }

    private static QuoteLine NewLine(Guid quoteId, CreateQuoteLineDto line) => new()
    {
        QuoteId = quoteId,
        Description = line.Description.Trim(),
        Quantity = line.Quantity,
        ServiceType = line.ServiceType,
        Scope = line.Scope,
        ProviderServiceId = line.ProviderServiceId,
        UnitCost = line.UnitCost,
        MarginPercent = line.MarginPercent,
        // Giá bán đơn vị: có vốn → vốn×(1+%LN); vốn=0 → giữ giá gõ tay (báo giá nhanh cũ).
        UnitPrice = QuoteMath.UnitSellPrice(line.UnitCost, line.MarginPercent, line.UnitPrice),
    };

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private async Task<QuoteDto> MapAsync(Quote quote)
    {
        var lines = (await lineRepo.ListAsync(l => l.QuoteId == quote.Id)).OrderBy(l => l.CreatedAt).ToList();
        var lineDtos = lines
            .Select(l => new QuoteLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice,
                l.ServiceType, l.Scope, l.ProviderServiceId, l.UnitCost, l.MarginPercent))
            .ToArray();

        var pricing = QuoteMath.Price(
            lines, quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent);

        return new QuoteDto(
            quote.Id, quote.Code, quote.CustomerId, quote.CustomerName, quote.Title,
            quote.ValidUntil, quote.Status, quote.Note, quote.TotalAmount, lineDtos,
            quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent,
            quote.TotalCost, quote.TotalProfit,
            pricing.AdultPrice, pricing.ChildPrice, pricing.InfantPrice,
            quote.ConvertedOrderId);
    }
}

using System.Linq.Expressions;
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
    public async Task<PagedResult<QuoteSummaryDto>> ListAsync(int page, int size, QuoteListFilter? filter = null)
    {
        var f = filter ?? new QuoteListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        // Lọc cột thật (loại, trạng thái, hạn hiệu lực, đã chuyển đơn) đẩy hết xuống SQL.
        Expression<Func<Quote, bool>> predicate = q =>
            (f.QuoteType == null || q.QuoteType == f.QuoteType) &&
            (f.Status == null || q.Status == f.Status) &&
            (f.ValidFrom == null || q.ValidUntil >= f.ValidFrom) &&
            (f.ValidTo == null || q.ValidUntil <= f.ValidTo) &&
            (f.Converted == null || (f.Converted == true ? q.ConvertedOrderId != null : q.ConvertedOrderId == null));

        // Không có từ khoá (mặc định khi mở màn) → cắt trang NGAY Ở SQL. PageAsync đã sắp CreatedAt
        // giảm dần, đúng bằng thứ tự cũ, nên kết quả không đổi mà khỏi kéo cả bảng báo giá về RAM.
        if (kw == null)
        {
            var (pageEntities, total) = await quoteRepo.PageAsync(page, size, predicate);
            return new PagedResult<QuoteSummaryDto>(pageEntities.Select(ToSummary).ToList(), total, page, size);
        }

        // Còn từ khoá: so khớp không phân biệt hoa/thường (OrdinalIgnoreCase) không dịch được sang SQL
        // (Npgsql lẫn provider InMemory đều chịu), nên vẫn phải lọc trong RAM — nhưng chỉ trên tập
        // đã được predicate ở trên thu hẹp, không còn quét toàn bảng.
        var all = await quoteRepo.ListAsync(predicate);

        bool MatchQ(Quote q) =>
            q.Code.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            q.CustomerName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            q.Title.Contains(kw, StringComparison.OrdinalIgnoreCase);

        var filtered = all.Where(MatchQ).OrderByDescending(q => q.CreatedAt).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(ToSummary).ToList();
        return new PagedResult<QuoteSummaryDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<QuoteStatsDto> GetStatsAsync(int? quoteType = null)
    {
        var all = await quoteRepo.ListAsync(q => quoteType == null || q.QuoteType == quoteType);
        return new QuoteStatsDto(
            all.Count,
            all.Count(q => q.Status == 0), all.Count(q => q.Status == 1),
            all.Count(q => q.Status == 2), all.Count(q => q.Status == 3),
            all.Sum(q => q.TotalAmount), all.Sum(q => q.TotalProfit));
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
            QuoteType = dto.QuoteType,
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
        quote.QuoteType = dto.QuoteType;
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

    private static QuoteSummaryDto ToSummary(Quote q) => new(
        q.Id, q.Code, q.CustomerName, q.Title, q.ValidUntil, q.Status, q.TotalAmount, q.ConvertedOrderId,
        q.Adults, q.Children, q.Infants, q.TotalCost, q.TotalProfit, q.QuoteType);

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
        // VAT/phụ thu/tỉ giá (P0-3): tỉ giá ≤ 0 coi như 1 để không phá thành tiền dòng cũ.
        VatPercent = line.VatPercent,
        Surcharge = line.Surcharge,
        ExchangeRate = line.ExchangeRate > 0 ? line.ExchangeRate : 1m,
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
                l.Id, l.Description, l.Quantity, l.UnitPrice, QuoteMath.LineSellTotal(l),
                l.ServiceType, l.Scope, l.ProviderServiceId, l.UnitCost, l.MarginPercent,
                l.VatPercent, l.Surcharge, l.ExchangeRate))
            .ToArray();

        var pricing = QuoteMath.Price(
            lines, quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent);

        return new QuoteDto(
            quote.Id, quote.Code, quote.CustomerId, quote.CustomerName, quote.Title,
            quote.ValidUntil, quote.Status, quote.Note, quote.TotalAmount, lineDtos,
            quote.Adults, quote.Children, quote.Infants, quote.ChildPercent, quote.InfantPercent,
            quote.TotalCost, quote.TotalProfit,
            pricing.AdultPrice, pricing.ChildPrice, pricing.InfantPrice,
            quote.ConvertedOrderId, quote.QuoteType);
    }
}

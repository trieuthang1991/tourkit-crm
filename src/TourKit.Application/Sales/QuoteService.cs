using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Sales;

/// <summary>
/// Báo giá (aggregate header + dòng) — mirror pattern 2-repo của ReceiptApprovalService.
/// TotalAmount tính lại mỗi lần ghi = Σ (Quantity × UnitPrice). Update = thay toàn bộ dòng.
/// </summary>
public sealed class QuoteService(
    IRepository<Quote> quoteRepo,
    IRepository<QuoteLine> lineRepo,
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

        var quote = new Quote
        {
            Code = dto.Code.Trim(),
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            Title = dto.Title.Trim(),
            ValidUntil = dto.ValidUntil,
            Status = dto.Status,
            Note = dto.Note,
            TotalAmount = Total(dto.Lines),
        };
        await quoteRepo.AddAsync(quote);

        foreach (var line in dto.Lines)
        {
            await lineRepo.AddAsync(NewLine(quote.Id, line));
        }

        await quoteRepo.SaveChangesAsync();
        await lineRepo.SaveChangesAsync();

        return await MapAsync(quote);
    }

    public async Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto)
    {
        await Validate(updateValidator, dto);

        var quote = await quoteRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        quote.Code = dto.Code.Trim();
        quote.CustomerId = dto.CustomerId;
        quote.CustomerName = dto.CustomerName;
        quote.Title = dto.Title.Trim();
        quote.ValidUntil = dto.ValidUntil;
        quote.Status = dto.Status;
        quote.Note = dto.Note;
        quote.TotalAmount = Total(dto.Lines);
        quoteRepo.Update(quote);

        // Thay toàn bộ dòng: xoá cũ, thêm mới.
        var existing = await lineRepo.ListAsync(l => l.QuoteId == id);
        foreach (var line in existing)
        {
            lineRepo.Remove(line);
        }

        foreach (var line in dto.Lines)
        {
            await lineRepo.AddAsync(NewLine(id, line));
        }

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

    private static decimal Total(IEnumerable<CreateQuoteLineDto> lines) => lines.Sum(l => l.Quantity * l.UnitPrice);

    private static QuoteLine NewLine(Guid quoteId, CreateQuoteLineDto line) => new()
    {
        QuoteId = quoteId,
        Description = line.Description.Trim(),
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
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
        var lines = await lineRepo.ListAsync(l => l.QuoteId == quote.Id);
        var lineDtos = lines
            .OrderBy(l => l.CreatedAt)
            .Select(l => new QuoteLineDto(l.Id, l.Description, l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice))
            .ToArray();

        return new QuoteDto(
            quote.Id, quote.Code, quote.CustomerId, quote.CustomerName, quote.Title,
            quote.ValidUntil, quote.Status, quote.Note, quote.TotalAmount, lineDtos);
    }
}

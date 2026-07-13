using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Sales;

/// <summary>
/// Hoá đơn VAT (aggregate header + dòng) — mirror QuoteService. Mỗi lần ghi tính lại:
/// Subtotal = Σ (Quantity×UnitPrice), VatAmount = Σ (tiền hàng × VatRate%), TotalAmount = Subtotal + VatAmount.
/// Update = thay toàn bộ dòng.
/// </summary>
public sealed class InvoiceService(
    IRepository<Invoice> invoiceRepo,
    IRepository<InvoiceLine> lineRepo,
    IValidator<CreateInvoiceDto> createValidator,
    IValidator<UpdateInvoiceDto> updateValidator) : IInvoiceService
{
    public async Task<PagedResult<InvoiceSummaryDto>> ListAsync(int page, int size, InvoiceListFilter? filter = null)
    {
        var f = filter ?? new InvoiceListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await invoiceRepo.ListAsync(i =>
            (f.Status == null || i.Status == f.Status) &&
            (f.DateFrom == null || i.InvoiceDate >= f.DateFrom) &&
            (f.DateTo == null || i.InvoiceDate <= f.DateTo));

        bool MatchQ(Invoice i) =>
            kw == null ||
            i.Series.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            i.Number.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            i.BuyerName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
            (i.BuyerTaxCode?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false);

        var filtered = all.Where(MatchQ).OrderByDescending(i => i.InvoiceDate).ToList();
        var dtos = filtered.Skip((page - 1) * size).Take(size)
            .Select(i => new InvoiceSummaryDto(i.Id, i.Series, i.Number, i.InvoiceDate, i.BuyerName, i.TotalAmount, i.Status, i.BuyerTaxCode, i.VatAmount))
            .ToList();
        return new PagedResult<InvoiceSummaryDto>(dtos, filtered.Count, page, size);
    }

    public async Task<InvoiceStatsDto> GetStatsAsync()
    {
        var all = await invoiceRepo.ListAsync();
        return new InvoiceStatsDto(
            all.Count,
            all.Count(i => i.Status == 0), all.Count(i => i.Status == 1), all.Count(i => i.Status == 2),
            all.Sum(i => i.TotalAmount), all.Sum(i => i.VatAmount));
    }

    public async Task<InvoiceDto> GetAsync(Guid id)
    {
        var invoice = await invoiceRepo.GetByIdAsync(id) ?? throw new NotFoundException();
        return await MapAsync(invoice);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        await Validate(createValidator, dto);

        var invoice = new Invoice
        {
            Series = dto.Series,
            Number = dto.Number,
            InvoiceDate = dto.InvoiceDate,
            OrderId = dto.OrderId,
            BuyerName = dto.BuyerName.Trim(),
            BuyerTaxCode = dto.BuyerTaxCode,
            BuyerAddress = dto.BuyerAddress,
            Status = dto.Status,
            Note = dto.Note,
        };
        Apply(invoice, dto.Lines);
        await invoiceRepo.AddAsync(invoice);

        foreach (var line in dto.Lines)
        {
            await lineRepo.AddAsync(NewLine(invoice.Id, line));
        }

        await invoiceRepo.SaveChangesAsync();
        await lineRepo.SaveChangesAsync();

        return await MapAsync(invoice);
    }

    public async Task<InvoiceDto> UpdateAsync(Guid id, UpdateInvoiceDto dto)
    {
        await Validate(updateValidator, dto);

        var invoice = await invoiceRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        invoice.Series = dto.Series;
        invoice.Number = dto.Number;
        invoice.InvoiceDate = dto.InvoiceDate;
        invoice.OrderId = dto.OrderId;
        invoice.BuyerName = dto.BuyerName.Trim();
        invoice.BuyerTaxCode = dto.BuyerTaxCode;
        invoice.BuyerAddress = dto.BuyerAddress;
        invoice.Status = dto.Status;
        invoice.Note = dto.Note;
        Apply(invoice, dto.Lines);
        invoiceRepo.Update(invoice);

        var existing = await lineRepo.ListAsync(l => l.InvoiceId == id);
        foreach (var line in existing)
        {
            lineRepo.Remove(line);
        }

        foreach (var line in dto.Lines)
        {
            await lineRepo.AddAsync(NewLine(id, line));
        }

        await invoiceRepo.SaveChangesAsync();
        await lineRepo.SaveChangesAsync();

        return await MapAsync(invoice);
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await invoiceRepo.GetByIdAsync(id) ?? throw new NotFoundException();

        var lines = await lineRepo.ListAsync(l => l.InvoiceId == id);
        foreach (var line in lines)
        {
            lineRepo.Remove(line);
        }

        invoiceRepo.Remove(invoice);
        await lineRepo.SaveChangesAsync();
        await invoiceRepo.SaveChangesAsync();
    }

    private static void Apply(Invoice invoice, IReadOnlyCollection<CreateInvoiceLineDto> lines)
    {
        invoice.Subtotal = lines.Sum(l => l.Quantity * l.UnitPrice);
        invoice.VatAmount = lines.Sum(l => l.Quantity * l.UnitPrice * l.VatRate / 100m);
        invoice.TotalAmount = invoice.Subtotal + invoice.VatAmount;
    }

    private static InvoiceLine NewLine(Guid invoiceId, CreateInvoiceLineDto line) => new()
    {
        InvoiceId = invoiceId,
        Description = line.Description.Trim(),
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        VatRate = line.VatRate,
    };

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private async Task<InvoiceDto> MapAsync(Invoice invoice)
    {
        var lines = await lineRepo.ListAsync(l => l.InvoiceId == invoice.Id);
        var lineDtos = lines
            .OrderBy(l => l.CreatedAt)
            .Select(l => new InvoiceLineDto(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.VatRate,
                l.Quantity * l.UnitPrice, l.Quantity * l.UnitPrice * l.VatRate / 100m))
            .ToArray();

        return new InvoiceDto(
            invoice.Id, invoice.Series, invoice.Number, invoice.InvoiceDate, invoice.OrderId,
            invoice.BuyerName, invoice.BuyerTaxCode, invoice.BuyerAddress,
            invoice.Subtotal, invoice.VatAmount, invoice.TotalAmount, invoice.Status, invoice.Note, lineDtos);
    }
}

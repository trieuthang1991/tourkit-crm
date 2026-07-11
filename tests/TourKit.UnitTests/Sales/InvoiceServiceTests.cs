using TourKit.Application.Common;
using TourKit.Application.Sales;
using TourKit.Application.Sales.Dtos;
using TourKit.Application.Sales.Validators;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.Sales;

public sealed class InvoiceServiceTests
{
    private static InvoiceService NewService(
        out FakeRepository<Invoice> invoiceRepo,
        out FakeRepository<InvoiceLine> lineRepo)
    {
        invoiceRepo = new FakeRepository<Invoice>();
        lineRepo = new FakeRepository<InvoiceLine>();
        return new InvoiceService(invoiceRepo, lineRepo, new CreateInvoiceValidator(), new UpdateInvoiceValidator());
    }

    private static CreateInvoiceDto SampleCreate(params (string desc, int qty, decimal price, decimal vat)[] lines) =>
        new(
            "1C25TAA", "0000123", DateTimeOffset.UtcNow, null,
            "Công ty ABC", "0101234567", "Hà Nội", 0, null,
            lines.Select(l => new CreateInvoiceLineDto(l.desc, l.qty, l.price, l.vat)).ToArray());

    [Fact]
    public async Task CreateAsync_computes_subtotal_vat_total()
    {
        var service = NewService(out _, out _);

        // 2×5,000,000 @10% + 1×2,000,000 @8% = subtotal 12,000,000; vat 1,000,000+160,000=1,160,000; total 13,160,000
        var invoice = await service.CreateAsync(SampleCreate(
            ("Tour ĐN", 2, 5_000_000m, 10m),
            ("Phụ thu", 1, 2_000_000m, 8m)));

        Assert.Equal(12_000_000m, invoice.Subtotal);
        Assert.Equal(1_160_000m, invoice.VatAmount);
        Assert.Equal(13_160_000m, invoice.TotalAmount);
        Assert.Equal(2, invoice.Lines.Length);
        Assert.Equal(1_000_000m, invoice.Lines[0].LineVat);
    }

    [Fact]
    public async Task CreateAsync_rejects_empty_buyer()
    {
        var service = NewService(out _, out _);
        var bad = SampleCreate(("x", 1, 1m, 10m)) with { BuyerName = "" };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task UpdateAsync_replaces_lines_and_recomputes()
    {
        var service = NewService(out _, out var lineRepo);
        var created = await service.CreateAsync(SampleCreate(("A", 1, 1_000_000m, 10m), ("B", 1, 1_000_000m, 10m)));

        var updated = await service.UpdateAsync(created.Id, new UpdateInvoiceDto(
            "1C25TAA", "0000123", DateTimeOffset.UtcNow, null, "Công ty ABC", null, null, 1, null,
            [new CreateInvoiceLineDto("Trọn gói", 1, 10_000_000m, 10m)]));

        Assert.Equal(10_000_000m, updated.Subtotal);
        Assert.Equal(1_000_000m, updated.VatAmount);
        Assert.Equal(11_000_000m, updated.TotalAmount);
        Assert.Single(await lineRepo.ListAsync(l => l.InvoiceId == created.Id));
    }

    [Fact]
    public async Task DeleteAsync_removes_invoice_and_lines()
    {
        var service = NewService(out _, out var lineRepo);
        var created = await service.CreateAsync(SampleCreate(("A", 1, 1m, 10m)));

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(created.Id));
        Assert.Empty(await lineRepo.ListAsync(l => l.InvoiceId == created.Id));
    }
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog.Features;

// ---- VERTICAL SLICE MẪU: tạo mẫu tour (Command + Validator + Handler nằm cạnh nhau) ----

public sealed record CreateTourTemplateCommand(
    string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby, string? TermsNote)
    : ICommand<TourTemplateResponse>;

public sealed class CreateTourTemplateValidator : AbstractValidator<CreateTourTemplateCommand>
{
    public CreateTourTemplateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalSlots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservationHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAdult).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChild).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceChildSmall).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceBaby).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateTourTemplateHandler : ICommandHandler<CreateTourTemplateCommand, TourTemplateResponse>
{
    private readonly AppDbContext _db;

    public CreateTourTemplateHandler(AppDbContext db) => _db = db;

    public async Task<Result<TourTemplateResponse>> Handle(CreateTourTemplateCommand c, CancellationToken ct)
    {
        var code = c.Code.Trim();
        if (await _db.TourTemplates.AnyAsync(t => t.Code == code, ct))
        {
            return Error.Conflict($"Mã mẫu tour '{code}' đã tồn tại.");
        }

        var t = new TourTemplate
        {
            Code = code, Title = c.Title.Trim(), TourType = c.TourType,
            TotalSlots = c.TotalSlots, ReservationHours = c.ReservationHours,
            PriceAdult = c.PriceAdult, PriceChild = c.PriceChild,
            PriceChildSmall = c.PriceChildSmall, PriceBaby = c.PriceBaby, TermsNote = c.TermsNote,
        };
        _db.TourTemplates.Add(t);
        await _db.SaveChangesAsync(ct);

        return new TourTemplateResponse(
            t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
            t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status);
    }
}

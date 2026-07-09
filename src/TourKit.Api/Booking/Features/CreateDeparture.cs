using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record CreateDepartureCommand(
    Guid? TemplateId, string Code, string Title,
    DateTimeOffset? DepartureDate, DateTimeOffset? EndDate, int TotalSlots)
    : ICommand<DepartureResponse>;

public sealed class CreateDepartureValidator : AbstractValidator<CreateDepartureCommand>
{
    public CreateDepartureValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
    }
}

public sealed class CreateDepartureHandler : ICommandHandler<CreateDepartureCommand, DepartureResponse>
{
    private readonly AppDbContext _db;

    public CreateDepartureHandler(AppDbContext db) => _db = db;

    public async Task<Result<DepartureResponse>> Handle(CreateDepartureCommand c, CancellationToken ct)
    {
        var departure = new TourDeparture
        {
            Code = c.Code.Trim(), Title = c.Title.Trim(), ParentTourId = c.TemplateId,
            DepartureDate = c.DepartureDate, EndDate = c.EndDate, TotalSlots = c.TotalSlots,
        };

        if (c.TemplateId is { } tplId)
        {
            var template = await _db.TourTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tplId, ct);
            if (template is not null)
            {
                departure.TourType = template.TourType;
                if (departure.TotalSlots == 0)
                {
                    departure.TotalSlots = template.TotalSlots;
                }
            }
        }

        _db.TourDepartures.Add(departure);

        if (c.TemplateId is { } tid)
        {
            var days = await _db.TourItineraries.AsNoTracking()
                .Where(i => i.TourId == tid).OrderBy(i => i.DayIndex).ToListAsync(ct);
            foreach (var d in days)
            {
                _db.TourItineraries.Add(new TourItinerary
                {
                    TourId = departure.Id, DayIndex = d.DayIndex, Title = d.Title, Detail = d.Detail,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return new DepartureResponse(
            departure.Id, departure.Code, departure.Title, departure.ParentTourId,
            departure.DepartureDate, departure.EndDate, departure.TotalSlots, departure.Status);
    }
}

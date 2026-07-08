using FluentValidation;
using TourKit.Infrastructure.Entities;
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
        _db.TourDepartures.Add(departure);
        await _db.SaveChangesAsync(ct);

        return new DepartureResponse(
            departure.Id, departure.Code, departure.Title, departure.ParentTourId,
            departure.DepartureDate, departure.EndDate, departure.TotalSlots, departure.Status);
    }
}

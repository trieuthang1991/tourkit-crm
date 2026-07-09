using FluentValidation;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record CreateTourRatingCommand(
    Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status)
    : ICommand<TourRatingResponse>;

public sealed class CreateTourRatingValidator : AbstractValidator<CreateTourRatingCommand>
{
    public CreateTourRatingValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
    }
}

public sealed class CreateTourRatingHandler : ICommandHandler<CreateTourRatingCommand, TourRatingResponse>
{
    private readonly AppDbContext _db;

    public CreateTourRatingHandler(AppDbContext db) => _db = db;

    public async Task<Result<TourRatingResponse>> Handle(CreateTourRatingCommand c, CancellationToken ct)
    {
        var rating = new TourRating
        {
            TourDepartureId = c.TourDepartureId,
            OrderId = c.OrderId,
            CustomerName = c.CustomerName,
            CustomerPhone = c.CustomerPhone,
            Stars = c.Stars,
            Comment = c.Comment,
            Status = c.Status,
        };
        _db.TourRatings.Add(rating);
        await _db.SaveChangesAsync(ct);

        return new TourRatingResponse(
            rating.Id, rating.TourDepartureId, rating.OrderId, rating.CustomerName, rating.CustomerPhone,
            rating.Stars, rating.Comment, rating.Status);
    }
}

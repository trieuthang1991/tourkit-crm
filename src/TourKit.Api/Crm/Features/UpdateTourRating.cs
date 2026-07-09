using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record UpdateTourRatingCommand(
    Guid Id, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status) : ICommand<bool>;

public sealed class UpdateTourRatingValidator : AbstractValidator<UpdateTourRatingCommand>
{
    public UpdateTourRatingValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
    }
}

public sealed class UpdateTourRatingHandler : ICommandHandler<UpdateTourRatingCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateTourRatingHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateTourRatingCommand c, CancellationToken ct)
    {
        var rating = await _db.TourRatings.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (rating is null)
        {
            return Error.NotFound();
        }

        rating.CustomerName = c.CustomerName;
        rating.CustomerPhone = c.CustomerPhone;
        rating.Stars = c.Stars;
        rating.Comment = c.Comment;
        rating.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}

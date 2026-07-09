using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record UpdateCustomerCareCommand(
    Guid Id, string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status)
    : ICommand<bool>;

public sealed class UpdateCustomerCareValidator : AbstractValidator<UpdateCustomerCareCommand>
{
    public UpdateCustomerCareValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateCustomerCareHandler : ICommandHandler<UpdateCustomerCareCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateCustomerCareHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateCustomerCareCommand c, CancellationToken ct)
    {
        var care = await _db.CustomerCares.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (care is null)
        {
            return Error.NotFound();
        }

        care.Title = c.Title.Trim();
        care.Detail = c.Detail;
        care.RemindAt = c.RemindAt;
        care.Feedback = c.Feedback;
        care.AssignedToUserId = c.AssignedToUserId;
        care.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}

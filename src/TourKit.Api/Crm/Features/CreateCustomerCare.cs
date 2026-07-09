using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record CreateCustomerCareCommand(
    Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, Guid? AssignedToUserId, int Status)
    : ICommand<CustomerCareResponse>;

public sealed class CreateCustomerCareValidator : AbstractValidator<CreateCustomerCareCommand>
{
    public CreateCustomerCareValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
    }
}

public sealed class CreateCustomerCareHandler : ICommandHandler<CreateCustomerCareCommand, CustomerCareResponse>
{
    private readonly AppDbContext _db;

    public CreateCustomerCareHandler(AppDbContext db) => _db = db;

    public async Task<Result<CustomerCareResponse>> Handle(CreateCustomerCareCommand c, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(x => x.Id == c.CustomerId, ct);
        if (!customerExists)
        {
            return Error.Validation("Khách hàng không tồn tại.");
        }

        var care = new CustomerCare
        {
            CustomerId = c.CustomerId,
            Title = c.Title.Trim(),
            Detail = c.Detail,
            RemindAt = c.RemindAt,
            AssignedToUserId = c.AssignedToUserId,
            Status = c.Status,
        };
        _db.CustomerCares.Add(care);
        await _db.SaveChangesAsync(ct);

        return new CustomerCareResponse(
            care.Id, care.CustomerId, care.Title, care.Detail, care.RemindAt, care.Feedback,
            care.AssignedToUserId, care.Status);
    }
}

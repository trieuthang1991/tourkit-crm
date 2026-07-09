using FluentValidation;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers.Features;

public sealed record CreateCustomerCommand(string FullName, string? Phone) : ICommand<CustomerResponse>;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, CustomerResponse>
{
    private readonly AppDbContext _db;

    public CreateCustomerHandler(AppDbContext db) => _db = db;

    public async Task<Result<CustomerResponse>> Handle(CreateCustomerCommand c, CancellationToken ct)
    {
        var customer = new Customer { FullName = c.FullName.Trim(), Phone = c.Phone };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone);
    }
}

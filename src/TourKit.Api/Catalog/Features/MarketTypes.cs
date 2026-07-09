using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog.Features;

public sealed record MarketTypeDto(Guid Id, string Name, Guid? ParentId, int SortOrder, int Status);

// ----- List -----
public sealed record ListMarketTypesQuery : IQuery<IReadOnlyList<MarketTypeDto>>;

public sealed class ListMarketTypesHandler : IQueryHandler<ListMarketTypesQuery, IReadOnlyList<MarketTypeDto>>
{
    private readonly AppDbContext _db;
    public ListMarketTypesHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<MarketTypeDto>>> Handle(ListMarketTypesQuery q, CancellationToken ct)
    {
        var list = await _db.MarketTypes.AsNoTracking().OrderBy(m => m.SortOrder)
            .Select(m => new MarketTypeDto(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<MarketTypeDto>>(list);
    }
}

// ----- Create -----
public sealed record CreateMarketTypeCommand(string Name, Guid? ParentId, int SortOrder) : ICommand<MarketTypeDto>;

public sealed class CreateMarketTypeValidator : AbstractValidator<CreateMarketTypeCommand>
{
    public CreateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class CreateMarketTypeHandler : ICommandHandler<CreateMarketTypeCommand, MarketTypeDto>
{
    private readonly AppDbContext _db;
    public CreateMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result<MarketTypeDto>> Handle(CreateMarketTypeCommand c, CancellationToken ct)
    {
        var m = new MarketType { Name = c.Name.Trim(), ParentId = c.ParentId, SortOrder = c.SortOrder };
        _db.MarketTypes.Add(m);
        await _db.SaveChangesAsync(ct);
        return new MarketTypeDto(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status);
    }
}

// ----- Update -----
// Kernel chỉ có ICommand<TResult> (không có biến thể non-generic) — bám mẫu Customers/Features/UpdateCustomer.cs.
public sealed record UpdateMarketTypeCommand(Guid Id, string Name, Guid? ParentId, int SortOrder) : ICommand<bool>;

public sealed class UpdateMarketTypeValidator : AbstractValidator<UpdateMarketTypeCommand>
{
    public UpdateMarketTypeValidator() => RuleFor(x => x.Name).NotEmpty();
}

public sealed class UpdateMarketTypeHandler : ICommandHandler<UpdateMarketTypeCommand, bool>
{
    private readonly AppDbContext _db;
    public UpdateMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateMarketTypeCommand c, CancellationToken ct)
    {
        var m = await _db.MarketTypes.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (m is null)
        {
            return Error.NotFound();
        }

        m.Name = c.Name.Trim();
        m.ParentId = c.ParentId;
        m.SortOrder = c.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ----- Delete -----
public sealed record DeleteMarketTypeCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteMarketTypeHandler : ICommandHandler<DeleteMarketTypeCommand, bool>
{
    private readonly AppDbContext _db;
    public DeleteMarketTypeHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteMarketTypeCommand c, CancellationToken ct)
    {
        var m = await _db.MarketTypes.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (m is null)
        {
            return Error.NotFound();
        }

        _db.MarketTypes.Remove(m);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

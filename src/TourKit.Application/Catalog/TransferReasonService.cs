using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>
/// Danh mục lý do chuyển chuyến (legacy <c>ReasonSwitch</c>/<c>DetailReasonSwitch</c>) — CRUD list,
/// tên duy nhất/tenant. Dùng cho <see cref="TourTransfer.ReasonId"/> khi chuyển đơn.
/// </summary>
public sealed class TransferReasonService(
    IRepository<TransferReason> repo,
    IValidator<CreateTransferReasonDto> createValidator,
    IValidator<UpdateTransferReasonDto> updateValidator) : ITransferReasonService
{
    public async Task<IReadOnlyList<TransferReasonDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).Select(Map).ToList();
    }

    public async Task<TransferReasonDto> CreateAsync(CreateTransferReasonDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureNameUnique(dto.Name, null);

        var entity = new TransferReason { Name = dto.Name.Trim(), SortOrder = dto.SortOrder };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTransferReasonDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        await EnsureNameUnique(dto.Name, id);

        entity.Name = dto.Name.Trim();
        entity.SortOrder = dto.SortOrder;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task EnsureNameUnique(string name, Guid? excludeId)
    {
        var trimmed = name.Trim();
        if (await repo.AnyAsync(x => x.Name == trimmed && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Lý do \"{trimmed}\" đã tồn tại.");
        }
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static TransferReasonDto Map(TransferReason m) => new(m.Id, m.Name, m.SortOrder, m.Status);
}

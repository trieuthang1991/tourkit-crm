using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

/// <summary>
/// Tài khoản nhận tiền (legacy PaymentMethod) — CRUD list (mirror CustomerTagService) để in lên
/// báo giá/hoá đơn. Bất biến nghiệp vụ: tối đa 1 tài khoản <see cref="PaymentAccount.IsDefault"/> mỗi tenant
/// (đặt cái mới làm mặc định thì gỡ mặc định các cái khác — một chỗ ở <see cref="ApplyDefaultAsync"/>).
/// </summary>
public sealed class PaymentAccountService(
    IRepository<PaymentAccount> repo,
    IValidator<CreatePaymentAccountDto> createValidator,
    IValidator<UpdatePaymentAccountDto> updateValidator) : IPaymentAccountService
{
    public async Task<IReadOnlyList<PaymentAccountDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        return items.OrderByDescending(x => x.IsDefault).ThenBy(x => x.SortOrder).Select(Map).ToList();
    }

    public async Task<PaymentAccountDto> CreateAsync(CreatePaymentAccountDto dto)
    {
        await Validate(createValidator, dto);
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, null);

        var entity = new PaymentAccount
        {
            Name = name,
            BankName = dto.BankName?.Trim(),
            AccountNumber = dto.AccountNumber?.Trim(),
            AccountHolder = dto.AccountHolder?.Trim(),
            Branch = dto.Branch?.Trim(),
            TransferNote = dto.TransferNote?.Trim(),
            IsDefault = dto.IsDefault,
            SortOrder = dto.SortOrder,
        };
        await repo.AddAsync(entity);
        await ApplyDefaultAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdatePaymentAccountDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var name = dto.Name.Trim();
        await EnsureNameUnique(name, id);

        entity.Name = name;
        entity.BankName = dto.BankName?.Trim();
        entity.AccountNumber = dto.AccountNumber?.Trim();
        entity.AccountHolder = dto.AccountHolder?.Trim();
        entity.Branch = dto.Branch?.Trim();
        entity.TransferNote = dto.TransferNote?.Trim();
        entity.IsDefault = dto.IsDefault;
        entity.SortOrder = dto.SortOrder;
        repo.Update(entity);
        await ApplyDefaultAsync(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    /// <summary>Nếu <paramref name="current"/> là mặc định, gỡ cờ mặc định của mọi tài khoản khác (1 mặc định/tenant).</summary>
    private async Task ApplyDefaultAsync(PaymentAccount current)
    {
        if (!current.IsDefault)
        {
            return;
        }

        var others = await repo.ListAsync(x => x.IsDefault && x.Id != current.Id);
        foreach (var other in others)
        {
            other.IsDefault = false;
            repo.Update(other);
        }
    }

    private async Task EnsureNameUnique(string name, Guid? excludeId)
    {
        if (await repo.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)))
        {
            throw new ValidationAppException($"Tài khoản \"{name}\" đã tồn tại.");
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

    private static PaymentAccountDto Map(PaymentAccount x) => new(
        x.Id, x.Name, x.BankName, x.AccountNumber, x.AccountHolder,
        x.Branch, x.TransferNote, x.IsDefault, x.SortOrder, x.Status);
}

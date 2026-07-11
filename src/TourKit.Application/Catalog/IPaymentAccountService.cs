using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IPaymentAccountService
{
    Task<IReadOnlyList<PaymentAccountDto>> ListAsync();
    Task<PaymentAccountDto> CreateAsync(CreatePaymentAccountDto dto);
    Task UpdateAsync(Guid id, UpdatePaymentAccountDto dto);
    Task DeleteAsync(Guid id);
}

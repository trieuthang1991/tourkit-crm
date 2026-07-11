using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IPaymentTermService
{
    Task<IReadOnlyList<PaymentTermDto>> ListAsync();
    Task<PaymentTermDto> CreateAsync(CreatePaymentTermDto dto);
    Task UpdateAsync(Guid id, UpdatePaymentTermDto dto);
    Task DeleteAsync(Guid id);
}

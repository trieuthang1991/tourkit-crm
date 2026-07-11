using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

/// <summary>Danh mục lý do chuyển chuyến (legacy ReasonSwitch) — CRUD list.</summary>
public interface ITransferReasonService
{
    Task<IReadOnlyList<TransferReasonDto>> ListAsync();
    Task<TransferReasonDto> CreateAsync(CreateTransferReasonDto dto);
    Task UpdateAsync(Guid id, UpdateTransferReasonDto dto);
    Task DeleteAsync(Guid id);
}

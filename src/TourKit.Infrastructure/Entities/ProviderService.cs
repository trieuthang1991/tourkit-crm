using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Bảng giá 1 dịch vụ của 1 NCC (legacy provider_services + provider_service_pricing gộp):
/// giá hợp đồng (contract) vs giá công bố (public), theo tên gói giá.</summary>
public sealed class ProviderService : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? ServiceItemId { get; set; }
    public string? PriceName { get; set; }        // price_name
    public decimal ContractPrice { get; set; }    // contract_price
    public decimal PublicPrice { get; set; }      // public_price
    public int AmountOfPeople { get; set; }       // amount_of_people
    public string? Note { get; set; }
    public int Status { get; set; }
}

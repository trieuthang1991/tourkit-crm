using System.Text.Json;
using System.Text.Json.Serialization;

namespace TourKit.Application.Customers;

/// <summary>
/// Toàn bộ field CRM "mềm" + danh sách của khách hàng, lưu gộp trong 1 cột JSON
/// (<c>Customer.CrmProfileJson</c>) thay vì mỗi field một cột — thêm thuộc tính/list mới
/// về sau KHÔNG cần migration. ID tham chiếu (CreatedBy, AssignedTo) lưu STRING để migrate
/// dữ liệu cũ (ID legacy không phải GUID).
/// </summary>
public sealed record CustomerCrmProfile
{
    public string? Gender { get; init; }            // Giới tính
    public string? City { get; init; }              // Tỉnh thành
    public string? MarketGroup { get; init; }       // Nhóm/Thị trường
    public string? InitialNeed { get; init; }       // Nhu cầu ban đầu
    public string? CollaboratorName { get; init; }  // CTV
    public string? Campaign { get; init; }          // Chiến dịch
    public string? CreatedBy { get; init; }         // Người tạo (string ref: GUID mới HOẶC id legacy)
    public IReadOnlyList<string> Segments { get; init; } = [];    // Loại KH / phân nhóm (multi tag)
    public IReadOnlyList<string> AssignedTo { get; init; } = [];  // NV phụ trách (multi string ref)
    public IReadOnlyList<string> Tags { get; init; } = [];        // Thẻ KH (multi)

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static CustomerCrmProfile Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CustomerCrmProfile();
        }

        try
        {
            return JsonSerializer.Deserialize<CustomerCrmProfile>(json, Options) ?? new CustomerCrmProfile();
        }
        catch (JsonException)
        {
            return new CustomerCrmProfile();
        }
    }

    /// <summary>Serialize; null nếu rỗng hoàn toàn (không lưu JSON thừa).</summary>
    public string? ToJsonOrNull()
    {
        var empty = Gender is null && City is null && MarketGroup is null && InitialNeed is null &&
            CollaboratorName is null && Campaign is null && CreatedBy is null &&
            Segments.Count == 0 && AssignedTo.Count == 0 && Tags.Count == 0;
        return empty ? null : JsonSerializer.Serialize(this, Options);
    }
}

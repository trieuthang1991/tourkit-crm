using System.Text.Json;
using System.Text.Json.Serialization;

namespace TourKit.Application.Flights;

/// <summary>Một chặng bay trong hành trình vé đoàn (bám legacy hiển thị "12/12/2022 VJ811 SGN-SIN 9:00").</summary>
public sealed record FlightSegment(string? Date, string? FlightNo, string? From, string? To, string? DepTime);

/// <summary>
/// Hành trình vé đoàn — danh sách chặng bay, lưu gộp trong 1 cột JSON (<c>FlightTicket.ItineraryJson</c>)
/// thay vì bảng con: thêm chặng/field mới KHÔNG cần migration (theo pattern entity-extend-json-string).
/// </summary>
public sealed record FlightItinerary
{
    public IReadOnlyList<FlightSegment> Segments { get; init; } = [];

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // ghi camelCase (khớp seed + FE)
        PropertyNameCaseInsensitive = true,                  // đọc được cả Pascal lẫn camel
    };

    public static FlightItinerary Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FlightItinerary();
        }

        try
        {
            return JsonSerializer.Deserialize<FlightItinerary>(json, Options) ?? new FlightItinerary();
        }
        catch (JsonException)
        {
            return new FlightItinerary();
        }
    }

    /// <summary>Serialize; null nếu rỗng (không lưu JSON thừa).</summary>
    public string? ToJsonOrNull()
        => Segments.Count == 0 ? null : JsonSerializer.Serialize(this, Options);
}

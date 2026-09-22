using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Web;

/// <summary>
/// Sắp options THỊ TRƯỜNG theo CÂY cho ô <c>&lt;select&gt;</c> ĐƠN: cha trước, rồi tới con thụt lề ("└").
/// Nhờ vậy vẫn là 1 ô select2 tìm kiếm mà nhìn ra được quan hệ cha–con (owner: chỉ cần 1 dòng search nhưng
/// phải thấy cha–con). Cả cha lẫn con đều chọn được (khác optgroup — header không chọn được).
/// </summary>
public static class MarketOptions
{
    private const string Indent = "   └ "; // 3 khoảng trắng cứng + "└ "

    public static IEnumerable<(Guid Id, string Label)> Hierarchy(IReadOnlyList<MarketTypeDto> all)
    {
        var conTheoCha = all.Where(x => x.ParentId is not null)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList());

        foreach (var goc in all.Where(x => x.ParentId is null).OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
        {
            yield return (goc.Id, goc.Name);
            if (conTheoCha.TryGetValue(goc.Id, out var con))
            {
                foreach (var c in con)
                {
                    yield return (c.Id, Indent + c.Name);
                }
            }
        }
    }
}

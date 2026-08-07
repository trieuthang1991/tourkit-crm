using System.Text.Json;
using TourKit.Application.Common;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.Application.Collaboration;

/// <summary>
/// Bình luận trên bản ghi nghiệp vụ. Xem <see cref="IEntityCommentService"/> về việc phân quyền nằm
/// ở tầng API chứ không ở đây.
///
/// KHÔNG CÓ SỬA ở v1, chỉ xoá. Lý do: luồng này là đầu vào cho trợ lý AI, mà một bình luận sửa được
/// sẽ âm thầm đổi thứ AI đã đọc và đã dựa vào để đưa nhận định — xoá rồi viết lại thì người sau nhìn
/// vào biết là đã có thay đổi.
/// </summary>
public sealed class EntityCommentService(
    IRepository<EntityComment> repo,
    IRepository<User> userRepo,
    ICurrentUserContext currentUser,
    INotificationService notifications) : IEntityCommentService
{
    private const int MaxContentLength = 4000;
    private const int MaxTake = 200;
    private const int MaxMentions = 20;

    public async Task<IReadOnlyList<EntityCommentDto>> ListAsync(string entityName, string entityId, int take = 50)
    {
        var (name, id) = Normalize(entityName, entityId);
        var limit = take is < 1 or > MaxTake ? MaxTake : take;

        // PageAsync đã OrderByDescending(CreatedAt) và cắt Ở SQL — không nạp cả luồng rồi sắp trong bộ nhớ.
        var (page, _) = await repo.PageAsync(1, limit, c => c.EntityName == name && c.EntityId == id);

        var authorIds = page.Select(c => c.UserId).ToHashSet();
        var authorNames = authorIds.Count == 0
            ? []
            : (await userRepo.ListAsync(u => authorIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.FullName);

        var me = currentUser.UserId;
        return page.Select(c => Map(c, authorNames, me)).ToList();
    }

    public Task<int> CountAsync(string entityName, string entityId)
    {
        var (name, id) = Normalize(entityName, entityId);
        return repo.CountAsync(c => c.EntityName == name && c.EntityId == id);
    }

    public async Task<EntityCommentDto> CreateAsync(CreateEntityCommentDto dto)
    {
        var author = currentUser.UserId
            ?? throw new ValidationAppException("Không xác định được người dùng hiện tại.");

        var (name, id) = Normalize(dto.EntityName, dto.EntityId);

        var content = (dto.Content ?? "").Trim();
        if (content.Length == 0)
        {
            throw new ValidationAppException("Nội dung bình luận không được để trống.");
        }

        if (content.Length > MaxContentLength)
        {
            throw new ValidationAppException($"Bình luận tối đa {MaxContentLength} ký tự.");
        }

        // Chỉ giữ user CÓ THẬT: người @nhắc đến từ giao diện nên không tin được, và một Guid rác
        // sẽ thành một thông báo gửi vào hư không.
        var mentions = await ResolveMentionsAsync(dto.MentionedUserIds, author);

        var entity = new EntityComment
        {
            EntityName = name,
            EntityId = id,
            UserId = author,
            Content = content,
            MentionedUserIds = mentions.Count == 0 ? null : JsonSerializer.Serialize(mentions),
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        await NotifyMentionsAsync(mentions, author, dto, content);

        var authorName = (await userRepo.GetByIdAsync(author))?.FullName ?? "";
        return Map(entity, new Dictionary<Guid, string> { [author] = authorName }, author);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        // Chỉ tác giả. Không cho người khác xoá kể cả khi họ xem được bản ghi cha: bình luận là
        // phát ngôn của một người, xoá lời người khác là chuyện khác hẳn với sửa dữ liệu nghiệp vụ.
        if (entity.UserId != currentUser.UserId)
        {
            throw new ForbiddenException("Chỉ người viết mới xoá được bình luận này.");
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task<List<Guid>> ResolveMentionsAsync(IReadOnlyList<Guid>? requested, Guid author)
    {
        if (requested is null || requested.Count == 0)
        {
            return [];
        }

        // Bỏ chính mình: tự nhắc mình rồi tự nhận thông báo là nhiễu.
        var wanted = requested.Where(u => u != author).Distinct().Take(MaxMentions).ToHashSet();
        if (wanted.Count == 0)
        {
            return [];
        }

        var existing = await userRepo.ListAsync(u => wanted.Contains(u.Id));
        return existing.Select(u => u.Id).ToList();
    }

    private async Task NotifyMentionsAsync(
        List<Guid> mentions, Guid author, CreateEntityCommentDto dto, string content)
    {
        if (mentions.Count == 0)
        {
            return;
        }

        var authorName = (await userRepo.GetByIdAsync(author))?.FullName ?? "Một đồng nghiệp";
        var where = string.IsNullOrWhiteSpace(dto.EntityLabel) ? dto.EntityName : dto.EntityLabel;
        var preview = content.Length <= 140 ? content : content[..140] + "…";

        foreach (var userId in mentions)
        {
            await notifications.PushAsync(
                userId,
                $"{authorName} nhắc bạn trong {where}",
                preview,
                dto.LinkUrl,
                "mention");
        }
    }

    /// <summary>
    /// Chuẩn hoá cặp khoá. EntityName phân biệt hoa thường theo đúng tên type (giống ActivityLog);
    /// chỉ cắt khoảng trắng, KHÔNG hạ chữ thường — hạ xuống sẽ khiến "Lead" và "lead" thành hai luồng
    /// khác nhau ở chỗ này nhưng cùng một chỗ ở ActivityLog.
    /// </summary>
    private static (string Name, string Id) Normalize(string entityName, string entityId)
    {
        var name = (entityName ?? "").Trim();
        var id = (entityId ?? "").Trim();
        if (name.Length == 0 || id.Length == 0)
        {
            throw new ValidationAppException("Thiếu thông tin bản ghi được bình luận.");
        }

        return (name, id);
    }

    private static EntityCommentDto Map(
        EntityComment c, IReadOnlyDictionary<Guid, string> authorNames, Guid? viewer) => new(
        c.Id,
        c.EntityName,
        c.EntityId,
        c.UserId,
        authorNames.GetValueOrDefault(c.UserId, ""),
        c.Content,
        ParseMentions(c.MentionedUserIds),
        c.CreatedAt,
        viewer is not null && c.UserId == viewer);

    private static List<Guid> ParseMentions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Dữ liệu cũ/hỏng không được làm chết cả màn bình luận.
            return [];
        }
    }
}

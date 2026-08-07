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
    IRepository<FileUpload> fileRepo,
    ICurrentUserContext currentUser,
    INotificationService notifications) : IEntityCommentService
{
    private const int MaxContentLength = 4000;
    private const int MaxTake = 200;
    private const int MaxMentions = 20;

    /// <summary>Trần ảnh mỗi bình luận — luồng này còn được trợ lý AI đọc, không để một dòng nuốt cả màn.</summary>
    private const int MaxAttachments = 5;

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

        // MỘT truy vấn cho ảnh của cả trang, không phải mỗi bình luận một lượt.
        var fileIds = page.SelectMany(c => ParseIds(c.AttachmentIds)).ToHashSet();
        var files = fileIds.Count == 0
            ? []
            : (await fileRepo.ListAsync(f => fileIds.Contains(f.Id))).ToDictionary(f => f.Id);

        var me = currentUser.UserId;
        return page.Select(c => Map(c, authorNames, files, me)).ToList();
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

        if (content.Length > MaxContentLength)
        {
            throw new ValidationAppException($"Bình luận tối đa {MaxContentLength} ký tự.");
        }

        // Chỉ giữ tệp CÓ THẬT: id đến từ giao diện nên không tin được.
        var attachments = await ResolveAttachmentsAsync(dto.AttachmentIds);

        // Ảnh không kèm chữ vẫn là một bình luận hợp lệ — thả ảnh chụp màn hình vào rồi bắt gõ thêm
        // một câu vô nghĩa là bắt người dùng làm việc thừa.
        if (content.Length == 0 && attachments.Count == 0)
        {
            throw new ValidationAppException("Nhập nội dung hoặc đính kèm ảnh.");
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
            AttachmentIds = attachments.Count == 0 ? null : JsonSerializer.Serialize(attachments.Select(f => f.Id)),
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        await NotifyMentionsAsync(mentions, author, dto, content);

        var authorName = (await userRepo.GetByIdAsync(author))?.FullName ?? "";
        return Map(entity, new Dictionary<Guid, string> { [author] = authorName },
            attachments.ToDictionary(f => f.Id), author);
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

    private async Task<List<FileUpload>> ResolveAttachmentsAsync(IReadOnlyList<Guid>? requested)
    {
        if (requested is null || requested.Count == 0)
        {
            return [];
        }

        var wanted = requested.Distinct().Take(MaxAttachments).ToList();
        var lookup = wanted.ToHashSet();
        var found = (await fileRepo.ListAsync(f => lookup.Contains(f.Id))).ToDictionary(f => f.Id);

        // Id không có thật thì báo hẳn, khác với @nhắc (bỏ im lặng): ở đây người dùng THẤY ảnh mình
        // vừa chọn, im lặng bỏ đi sẽ khiến họ tưởng đã gửi kèm.
        if (found.Count != wanted.Count)
        {
            throw new ValidationAppException("Có ảnh đính kèm không hợp lệ, thử tải lại.");
        }

        // Sắp lại theo ĐÚNG thứ tự người dùng chọn — kho trả về theo thứ tự của nó, không phải của họ.
        return wanted.Select(id => found[id]).ToList();
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
        EntityComment c,
        IReadOnlyDictionary<Guid, string> authorNames,
        IReadOnlyDictionary<Guid, FileUpload> files,
        Guid? viewer) => new(
        c.Id,
        c.EntityName,
        c.EntityId,
        c.UserId,
        authorNames.GetValueOrDefault(c.UserId, ""),
        c.Content,
        ParseIds(c.MentionedUserIds),
        // Giữ nguyên THỨ TỰ người dùng chọn, và bỏ qua tệp đã bị xoá thay vì hiện ô ảnh hỏng.
        ParseIds(c.AttachmentIds)
            .Select(files.GetValueOrDefault)
            .Where(f => f is not null)
            .Select(f => new CommentAttachmentDto(f!.Id, f.FileName, f.ContentType, f.Size))
            .ToList(),
        c.CreatedAt,
        viewer is not null && c.UserId == viewer);

    private static List<Guid> ParseIds(string? json)
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

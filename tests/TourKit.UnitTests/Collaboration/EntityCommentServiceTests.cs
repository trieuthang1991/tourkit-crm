using TourKit.Application.Collaboration;
using TourKit.Application.Common;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.UnitTests.Collaboration;

public class EntityCommentServiceTests
{
    private static readonly Guid Me = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Ban = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid NguoiLa = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class FakeCurrentUser(Guid? id) : ICurrentUserContext
    {
        public Guid? UserId { get; set; } = id;
    }

    private sealed record Pushed(Guid UserId, string Title, string? Message, string? LinkUrl, string Type);

    private sealed class FakeNotifications : INotificationService
    {
        public List<Pushed> Sent { get; } = [];

        public Task PushAsync(Guid userId, string title, string? message, string? linkUrl = null, string type = "system")
        {
            Sent.Add(new Pushed(userId, title, message, linkUrl, type));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationDto>> ListMineAsync(bool unreadOnly, int? take = null)
            => throw new NotImplementedException();
        public Task<int> UnreadCountAsync() => throw new NotImplementedException();
        public Task MarkReadAsync(Guid id) => throw new NotImplementedException();
        public Task MarkAllReadAsync() => throw new NotImplementedException();
    }

    private sealed record Harness(
        EntityCommentService Service,
        FakeRepository<EntityComment> Comments,
        FakeRepository<FileUpload> Files,
        FakeNotifications Notifications,
        FakeCurrentUser CurrentUser);

    private static Harness Build(Guid? viewer = null)
    {
        var comments = new FakeRepository<EntityComment>();
        var users = new FakeRepository<User>();
        var files = new FakeRepository<FileUpload>();
        users.Seed(
            new User { Id = Me, FullName = "Trần Bình" },
            new User { Id = Ban, FullName = "Lê Cường" },
            new User { Id = NguoiLa, FullName = "Phạm Dung" });

        var currentUser = new FakeCurrentUser(viewer ?? Me);
        var notifications = new FakeNotifications();
        return new Harness(
            new EntityCommentService(comments, users, files, currentUser, notifications),
            comments, files, notifications, currentUser);
    }

    private static FileUpload Image(string name) => new()
    {
        FileName = name,
        ContentType = "image/png",
        Size = 1234,
        StorageKey = Guid.NewGuid().ToString("N"),
    };

    private static EntityComment Comment(string entityName, string entityId, Guid author, string content, int minutesAgo) =>
        new()
        {
            EntityName = entityName,
            EntityId = entityId,
            UserId = author,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo),
        };

    [Fact]
    public async Task Tao_binh_luan_luu_lai_va_tra_ve_ten_tac_gia()
    {
        var h = Build();

        var dto = await h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "  Khách chê giá  "));

        Assert.Equal("Khách chê giá", dto.Content);          // đã cắt khoảng trắng
        Assert.Equal("Trần Bình", dto.AuthorName);
        Assert.True(dto.CanDelete);                           // tác giả tự xoá được lời của mình
        Assert.Single(h.Comments.Items);
    }

    [Fact]
    public async Task Noi_dung_rong_va_khong_co_anh_thi_bi_tu_choi()
    {
        var h = Build();

        await Assert.ThrowsAsync<ValidationAppException>(
            () => h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "   ")));
    }

    [Fact]
    public async Task Chi_gui_anh_khong_kem_chu_van_hop_le()
    {
        var h = Build();
        var anh = Image("man-hinh.png");
        h.Files.Seed(anh);

        // Thả ảnh chụp màn hình vào rồi bắt gõ thêm một câu vô nghĩa là bắt người dùng làm việc thừa.
        var dto = await h.Service.CreateAsync(
            new CreateEntityCommentDto("Lead", "abc", "", AttachmentIds: [anh.Id]));

        Assert.Equal("", dto.Content);
        Assert.Single(dto.Attachments);
        Assert.Equal("man-hinh.png", dto.Attachments[0].FileName);
    }

    [Fact]
    public async Task Anh_khong_ton_tai_thi_bao_loi_chu_khong_am_tham_bo_qua()
    {
        var h = Build();

        // Khác @nhắc (bỏ im lặng): ở đây người dùng THẤY ảnh mình vừa chọn, im lặng bỏ đi sẽ khiến
        // họ tưởng đã gửi kèm.
        await Assert.ThrowsAsync<ValidationAppException>(
            () => h.Service.CreateAsync(
                new CreateEntityCommentDto("Lead", "abc", "xem ảnh", AttachmentIds: [Guid.NewGuid()])));
    }

    [Fact]
    public async Task Anh_gui_kem_hien_lai_dung_thu_tu_khi_doc_luong()
    {
        var h = Build();
        var a = Image("1.png");
        var b = Image("2.png");
        h.Files.Seed(a, b);

        await h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "hai ảnh", AttachmentIds: [b.Id, a.Id]));
        var list = await h.Service.ListAsync("Lead", "abc");

        Assert.Equal(["2.png", "1.png"], list[0].Attachments.Select(x => x.FileName));
    }

    [Fact]
    public async Task Anh_bi_xoa_khoi_kho_thi_bo_qua_chu_khong_hien_o_anh_hong()
    {
        var h = Build();
        var a = Image("con.png");
        var b = Image("se-bi-xoa.png");
        h.Files.Seed(a, b);
        await h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "hai ảnh", AttachmentIds: [a.Id, b.Id]));

        h.Files.Remove(b);

        var list = await h.Service.ListAsync("Lead", "abc");
        Assert.Equal(["con.png"], list[0].Attachments.Select(x => x.FileName));
    }

    [Fact]
    public async Task Chi_nhan_toi_da_5_anh_moi_binh_luan()
    {
        var h = Build();
        var imgs = Enumerable.Range(1, 8).Select(i => Image($"{i}.png")).ToArray();
        h.Files.Seed(imgs);

        var dto = await h.Service.CreateAsync(new CreateEntityCommentDto(
            "Lead", "abc", "nhiều ảnh", AttachmentIds: imgs.Select(f => f.Id).ToList()));

        Assert.Equal(5, dto.Attachments.Count);
    }

    [Fact]
    public async Task Noi_dung_qua_dai_bi_tu_choi()
    {
        var h = Build();

        await Assert.ThrowsAsync<ValidationAppException>(
            () => h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", new string('x', 4001))));
    }

    [Fact]
    public async Task Thieu_khoa_ban_ghi_bi_tu_choi()
    {
        var h = Build();

        await Assert.ThrowsAsync<ValidationAppException>(
            () => h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "  ", "nội dung")));
    }

    [Fact]
    public async Task Chua_dang_nhap_thi_khong_tao_duoc()
    {
        var h = Build();
        h.CurrentUser.UserId = null;

        await Assert.ThrowsAsync<ValidationAppException>(
            () => h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "nội dung")));
    }

    [Fact]
    public async Task Chi_lay_binh_luan_cua_dung_ban_ghi()
    {
        var h = Build();
        h.Comments.Seed(
            Comment("Lead", "A", Me, "của lead A", 3),
            Comment("Lead", "B", Me, "của lead B", 2),
            Comment("Customer", "A", Me, "của khách A", 1));   // trùng Id nhưng KHÁC loại entity

        var list = await h.Service.ListAsync("Lead", "A");

        Assert.Single(list);
        Assert.Equal("của lead A", list[0].Content);
    }

    [Fact]
    public async Task Sap_moi_nhat_truoc()
    {
        var h = Build();
        h.Comments.Seed(
            Comment("Lead", "A", Me, "cũ nhất", 30),
            Comment("Lead", "A", Me, "mới nhất", 1),
            Comment("Lead", "A", Me, "ở giữa", 10));

        var list = await h.Service.ListAsync("Lead", "A");

        Assert.Equal(["mới nhất", "ở giữa", "cũ nhất"], list.Select(c => c.Content));
    }

    [Fact]
    public async Task List_luon_co_bien()
    {
        var h = Build();
        h.Comments.Seed(Enumerable.Range(1, 300)
            .Select(i => Comment("Lead", "A", Me, $"bl {i}", i))
            .ToArray());

        // take vượt trần bị kẹp về 200, không phải nạp cả 300 dòng vào ngữ cảnh.
        var list = await h.Service.ListAsync("Lead", "A", take: 9999);

        Assert.Equal(200, list.Count);
        Assert.Equal(300, await h.Service.CountAsync("Lead", "A"));
    }

    [Fact]
    public async Task Nhac_nguoi_khac_thi_day_thong_bao_sang_chuong()
    {
        var h = Build();

        await h.Service.CreateAsync(new CreateEntityCommentDto(
            "Lead", "abc", "@Cường xem giúp",
            MentionedUserIds: [Ban],
            LinkUrl: "/co-hoi",
            EntityLabel: "Cơ hội bán hàng"));

        var pushed = Assert.Single(h.Notifications.Sent);
        Assert.Equal(Ban, pushed.UserId);
        Assert.Contains("Trần Bình", pushed.Title);
        Assert.Contains("Cơ hội bán hàng", pushed.Title);
        Assert.Equal("/co-hoi", pushed.LinkUrl);
        Assert.Equal("mention", pushed.Type);
    }

    [Fact]
    public async Task Khong_tu_nhac_chinh_minh()
    {
        var h = Build();

        await h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "ghi chú", MentionedUserIds: [Me]));

        Assert.Empty(h.Notifications.Sent);
    }

    [Fact]
    public async Task Bo_qua_user_khong_ton_tai_trong_danh_sach_nhac()
    {
        var h = Build();
        var ma = Guid.NewGuid();

        await h.Service.CreateAsync(new CreateEntityCommentDto("Lead", "abc", "ghi chú", MentionedUserIds: [Ban, ma]));

        // Danh sách @nhắc đến từ giao diện nên không tin được — chỉ user có thật mới nhận thông báo.
        var pushed = Assert.Single(h.Notifications.Sent);
        Assert.Equal(Ban, pushed.UserId);
    }

    [Fact]
    public async Task Tac_gia_xoa_duoc_binh_luan_cua_minh()
    {
        var h = Build();
        var mine = Comment("Lead", "A", Me, "của tôi", 1);
        h.Comments.Seed(mine);

        await h.Service.DeleteAsync(mine.Id);

        Assert.Empty(h.Comments.Items);
    }

    [Fact]
    public async Task Nguoi_khac_khong_xoa_duoc_loi_cua_nguoi_ta()
    {
        var h = Build();
        var theirs = Comment("Lead", "A", NguoiLa, "của người khác", 1);
        h.Comments.Seed(theirs);

        await Assert.ThrowsAsync<ForbiddenException>(() => h.Service.DeleteAsync(theirs.Id));
        Assert.Single(h.Comments.Items);
    }

    [Fact]
    public async Task CanDelete_false_voi_binh_luan_cua_nguoi_khac()
    {
        var h = Build();
        h.Comments.Seed(
            Comment("Lead", "A", Me, "của tôi", 2),
            Comment("Lead", "A", NguoiLa, "của người khác", 1));

        var list = await h.Service.ListAsync("Lead", "A");

        Assert.False(list.Single(c => c.Content == "của người khác").CanDelete);
        Assert.True(list.Single(c => c.Content == "của tôi").CanDelete);
    }

    [Fact]
    public async Task Xoa_binh_luan_khong_ton_tai_bao_khong_tim_thay()
    {
        var h = Build();

        await Assert.ThrowsAsync<NotFoundException>(() => h.Service.DeleteAsync(Guid.NewGuid()));
    }
}

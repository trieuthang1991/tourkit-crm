using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Api.BackgroundJobs;
using TourKit.Api.Tenancy;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.BackgroundJobs;

/// <summary>
/// Job gửi chăm sóc tự động: seed dữ liệu dưới 1 tenant, chạy job với context KHÔNG tenant
/// (mô phỏng job nền) → chỉ gửi được nhờ IgnoreQueryFilters. Kiểm tra lọc đúng và idempotency.
/// </summary>
public sealed class CareReminderJobTests
{
    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string To, string Subject)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            Sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Sends_only_due_assigned_unsent_reminders_and_marks_them()
    {
        var db = nameof(Sends_only_due_assigned_unsent_reminders_and_marks_them);
        var tenantId = Guid.NewGuid();
        var past = DateTimeOffset.UtcNow.AddHours(-1);
        var future = DateTimeOffset.UtcNow.AddDays(1);

        var assignee = new User { Email = "sale@tourkit.vn", FullName = "Sale A", PasswordHash = "x", IsActive = true };
        var dueCare = new CustomerCare { CustomerId = Guid.NewGuid(), Title = "Gọi lại KH", RemindAt = past, AssignedToUserId = assignee.Id };
        var notDue = new CustomerCare { CustomerId = Guid.NewGuid(), Title = "Chưa tới hạn", RemindAt = future, AssignedToUserId = assignee.Id };
        var alreadySent = new CustomerCare { CustomerId = Guid.NewGuid(), Title = "Đã gửi", RemindAt = past, AssignedToUserId = assignee.Id, ReminderSentAt = past };
        var unassigned = new CustomerCare { CustomerId = Guid.NewGuid(), Title = "Không có người phụ trách", RemindAt = past };

        // Seed dưới tenant thật.
        using (var seed = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db))
        {
            seed.Set<User>().Add(assignee);
            seed.Set<CustomerCare>().AddRange(dueCare, notDue, alreadySent, unassigned);
            await seed.SaveChangesAsync();
        }

        // Job chạy KHÔNG có tenant ambient → chỉ thấy dữ liệu nhờ IgnoreQueryFilters trong job.
        var email = new CapturingEmailSender();
        var scope = new AmbientTenantContext(); // job không có tenant ambient ban đầu (Guid.Empty)
        using (var jobCtx = TestDb.Create(scope, db))
        {
            var job = new CareReminderJob(jobCtx, scope, email, NullLogger<CareReminderJob>.Instance);
            await job.RunAsync();
        }

        // Chỉ 1 email cho lịch tới hạn + có người phụ trách + chưa gửi.
        var one = Assert.Single(email.Sent);
        Assert.Equal("sale@tourkit.vn", one.To);
        Assert.Contains("Gọi lại KH", one.Subject);

        using var verify = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db);
        Assert.NotNull((await verify.Set<CustomerCare>().FirstAsync(c => c.Id == dueCare.Id)).ReminderSentAt);
        Assert.Null((await verify.Set<CustomerCare>().FirstAsync(c => c.Id == notDue.Id)).ReminderSentAt);
        Assert.Null((await verify.Set<CustomerCare>().FirstAsync(c => c.Id == unassigned.Id)).ReminderSentAt);
    }

    [Fact]
    public async Task Running_twice_does_not_resend()
    {
        var db = nameof(Running_twice_does_not_resend);
        var tenantId = Guid.NewGuid();
        var assignee = new User { Email = "sale@tourkit.vn", FullName = "Sale A", PasswordHash = "x", IsActive = true };
        var care = new CustomerCare
        {
            CustomerId = Guid.NewGuid(), Title = "Gọi lại", RemindAt = DateTimeOffset.UtcNow.AddHours(-1),
            AssignedToUserId = assignee.Id,
        };

        using (var seed = TestDb.Create(new TestTenantContext { TenantId = tenantId }, db))
        {
            seed.Set<User>().Add(assignee);
            seed.Set<CustomerCare>().Add(care);
            await seed.SaveChangesAsync();
        }

        var email = new CapturingEmailSender();
        for (var i = 0; i < 2; i++)
        {
            var scope = new AmbientTenantContext();
            using var jobCtx = TestDb.Create(scope, db);
            await new CareReminderJob(jobCtx, scope, email, NullLogger<CareReminderJob>.Instance).RunAsync();
        }

        Assert.Single(email.Sent); // lần 2 không gửi lại (đã đánh dấu ReminderSentAt)
    }
}

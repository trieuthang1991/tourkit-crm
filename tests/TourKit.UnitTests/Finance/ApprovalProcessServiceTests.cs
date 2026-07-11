using TourKit.Application.Common;
using TourKit.Application.Finance;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Finance;

/// <summary>
/// Test <see cref="ApprovalProcessService"/> — quy trình duyệt cấu hình động (legacy ApprovalProcess):
/// template + bước theo chức vụ + người duyệt mỗi bước.
/// </summary>
public class ApprovalProcessServiceTests
{
    private static ApprovalProcessService NewService(
        out FakeRepository<ApprovalProcess> repo,
        out FakeRepository<ApprovalProcessStep> stepRepo,
        out FakeRepository<ApprovalProcessStepUser> stepUserRepo,
        out FakeRepository<Position> positionRepo,
        out FakeRepository<User> userRepo)
    {
        repo = new FakeRepository<ApprovalProcess>();
        stepRepo = new FakeRepository<ApprovalProcessStep>();
        stepUserRepo = new FakeRepository<ApprovalProcessStepUser>();
        positionRepo = new FakeRepository<Position>();
        userRepo = new FakeRepository<User>();
        return new ApprovalProcessService(repo, stepRepo, stepUserRepo, positionRepo, userRepo);
    }

    private static async Task<Position> SeedPositionAsync(FakeRepository<Position> repo, string name = "Trưởng phòng")
    {
        var p = new Position { Name = name };
        await repo.AddAsync(p);
        await repo.SaveChangesAsync();
        return p;
    }

    private static async Task<User> SeedUserAsync(FakeRepository<User> repo, string name = "Nguyễn Văn A")
    {
        var u = new User { Email = $"{Guid.NewGuid():N}@b.c", FullName = name };
        await repo.AddAsync(u);
        await repo.SaveChangesAsync();
        return u;
    }

    [Fact]
    public async Task CreateAsync_persists_process()
    {
        var service = NewService(out _, out _, out _, out _, out _);

        var dto = await service.CreateAsync(new CreateApprovalProcessDto("Duyệt chi > 10tr", (int)ApprovalMethod.All));

        Assert.Equal("Duyệt chi > 10tr", dto.Name);
        Assert.Equal((int)ApprovalMethod.All, dto.Method);
        Assert.Equal(0, dto.StepCount);
    }

    [Fact]
    public async Task CreateAsync_invalid_method_throws()
    {
        var service = NewService(out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateApprovalProcessDto("X", 99)));
    }

    [Fact]
    public async Task AddStep_appends_sequential_order_and_validates_position()
    {
        var service = NewService(out _, out _, out _, out var positionRepo, out _);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);

        var s1 = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var s2 = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        Assert.Equal(1, s1.StepOrder);
        Assert.Equal(2, s2.StepOrder);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.AddStepAsync(process.Id, new AddStepDto(Guid.NewGuid())));
    }

    [Fact]
    public async Task SetStepUsers_replaces_users_and_validates_existence()
    {
        var service = NewService(out _, out _, out _, out var positionRepo, out var userRepo);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);
        var step = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var u1 = await SeedUserAsync(userRepo, "A");
        var u2 = await SeedUserAsync(userRepo, "B");

        await service.SetStepUsersAsync(process.Id, step.Id, new SetStepUsersDto([u1.Id, u2.Id]));
        var detail = await service.GetAsync(process.Id);
        Assert.Equal(2, detail.Steps[0].UserIds.Count);

        // Thay bằng 1 người → còn 1
        await service.SetStepUsersAsync(process.Id, step.Id, new SetStepUsersDto([u1.Id]));
        detail = await service.GetAsync(process.Id);
        Assert.Single(detail.Steps[0].UserIds);
        Assert.Contains("A", detail.Steps[0].UserNames);

        // Người không tồn tại → lỗi
        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.SetStepUsersAsync(process.Id, step.Id, new SetStepUsersDto([Guid.NewGuid()])));
    }

    [Fact]
    public async Task SetStepUsers_on_foreign_step_throws()
    {
        var service = NewService(out _, out _, out _, out var positionRepo, out var userRepo);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);
        var step = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var user = await SeedUserAsync(userRepo);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.SetStepUsersAsync(Guid.NewGuid(), step.Id, new SetStepUsersDto([user.Id])));
    }

    [Fact]
    public async Task ReorderSteps_applies_new_order()
    {
        var service = NewService(out _, out _, out _, out var positionRepo, out _);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);
        var s1 = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var s2 = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));

        await service.ReorderStepsAsync(process.Id, new ReorderStepsDto([s2.Id, s1.Id]));

        var detail = await service.GetAsync(process.Id);
        Assert.Equal(s2.Id, detail.Steps[0].Id);
        Assert.Equal(s1.Id, detail.Steps[1].Id);
    }

    [Fact]
    public async Task DeleteStep_removes_step_and_its_users()
    {
        var service = NewService(out _, out var stepRepo, out var stepUserRepo, out var positionRepo, out var userRepo);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);
        var step = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var user = await SeedUserAsync(userRepo);
        await service.SetStepUsersAsync(process.Id, step.Id, new SetStepUsersDto([user.Id]));

        await service.DeleteStepAsync(process.Id, step.Id);

        Assert.Empty(await stepRepo.ListAsync(s => s.ApprovalProcessId == process.Id));
        Assert.Empty(await stepUserRepo.ListAsync(su => su.ApprovalProcessStepId == step.Id));
    }

    [Fact]
    public async Task DeleteAsync_cascades_steps_and_users()
    {
        var service = NewService(out var repo, out var stepRepo, out var stepUserRepo, out var positionRepo, out var userRepo);
        var process = await service.CreateAsync(new CreateApprovalProcessDto("QT", (int)ApprovalMethod.One));
        var pos = await SeedPositionAsync(positionRepo);
        var step = await service.AddStepAsync(process.Id, new AddStepDto(pos.Id));
        var user = await SeedUserAsync(userRepo);
        await service.SetStepUsersAsync(process.Id, step.Id, new SetStepUsersDto([user.Id]));

        await service.DeleteAsync(process.Id);

        Assert.Null(await repo.GetByIdAsync(process.Id));
        Assert.Empty(await stepRepo.ListAsync());
        Assert.Empty(await stepUserRepo.ListAsync());
    }
}

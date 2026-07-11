namespace TourKit.Application.Finance;

public sealed record ApprovalProcessDto(Guid Id, string Name, int Method, int Status, int StepCount);

public sealed record CreateApprovalProcessDto(string Name, int Method);
public sealed record UpdateApprovalProcessDto(string Name, int Method, int Status);

public sealed record ApprovalProcessStepDto(
    Guid Id, int StepOrder, Guid PositionId, string? PositionName,
    IReadOnlyList<Guid> UserIds, IReadOnlyList<string> UserNames);

public sealed record ApprovalProcessDetailDto(
    Guid Id, string Name, int Method, int Status, IReadOnlyList<ApprovalProcessStepDto> Steps);

public sealed record AddStepDto(Guid PositionId);
public sealed record ReorderStepsDto(IReadOnlyList<Guid> StepIds);
public sealed record SetStepUsersDto(IReadOnlyList<Guid> UserIds);

namespace TourKit.Shared.Enums;

/// <summary>Trạng thái công việc nội bộ (legacy Tasking.status).</summary>
public enum WorkTaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3,
}

/// <summary>Độ ưu tiên công việc.</summary>
public enum WorkTaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
}

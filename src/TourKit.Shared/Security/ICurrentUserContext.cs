namespace TourKit.Shared.Security;

/// <summary>
/// User của request hiện tại — abstraction ở tầng Shared để Infrastructure (AppDbContext/interceptor)
/// truy cập được UserId mà không phụ thuộc tầng Api. Mirror <c>ITenantContext</c>.
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }
}

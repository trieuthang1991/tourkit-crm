namespace TourKit.Api.Auth;

/// <summary>Người dùng hiện tại của request (đọc từ claim JWT "sub").</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}

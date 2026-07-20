namespace TourKit.Api.Web;

/// <summary>
/// Kết quả JSON chuẩn cho AJAX (Razor Pages handler / API nội bộ): { isSuccess, message, data }.
/// Dùng qua factory tường minh: <see cref="Success"/> / <see cref="Error"/> (hoặc new SuccessResult/ErrorResult).
/// System.Text.Json xuất camelCase → JS đọc res.isSuccess / res.message / res.data.
/// </summary>
public abstract class Result
{
    public abstract bool IsSuccess { get; }
    public string? Message { get; init; }
    public object? Data { get; init; }

    public static SuccessResult Success(string? message = null, object? data = null) =>
        new() { Message = message, Data = data };

    public static ErrorResult Error(string message, object? data = null) =>
        new() { Message = message, Data = data };
}

/// <summary>Thành công. <c>isSuccess = true</c>.</summary>
public sealed class SuccessResult : Result
{
    public override bool IsSuccess => true;
}

/// <summary>Lỗi/không hợp lệ. <c>isSuccess = false</c>.</summary>
public sealed class ErrorResult : Result
{
    public override bool IsSuccess => false;
}

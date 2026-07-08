namespace TourKit.Shared.Application;

/// <summary>Phân loại lỗi nghiệp vụ → map sang HTTP ở tầng Api (không throw để điều khiển luồng).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unexpected,
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string message, string code = "validation") => new(code, message, ErrorType.Validation);

    public static Error NotFound(string message = "Không tìm thấy.", string code = "not_found") =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string message, string code = "conflict") => new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string message = "Không có quyền.", string code = "forbidden") =>
        new(code, message, ErrorType.Forbidden);
}

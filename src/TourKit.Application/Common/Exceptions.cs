namespace TourKit.Application.Common;

public abstract class AppException(string message) : Exception(message)
{
    public abstract string ErrorType { get; }   // "not_found" | "conflict" | "validation" | "forbidden"
}
public sealed class NotFoundException(string message = "Không tìm thấy dữ liệu.") : AppException(message)
{ public override string ErrorType => "not_found"; }
public sealed class ConflictException(string message) : AppException(message)
{ public override string ErrorType => "conflict"; }
public sealed class ValidationAppException(string message) : AppException(message)
{ public override string ErrorType => "validation"; }
public sealed class ForbiddenException(string message = "Không có quyền.") : AppException(message)
{ public override string ErrorType => "forbidden"; }

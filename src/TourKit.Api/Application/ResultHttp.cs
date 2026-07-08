using TourKit.Shared.Application;

namespace TourKit.Api.Application;

/// <summary>Map Result/Error sang HTTP (ProblemDetails) — một chỗ, endpoint chỉ gọi Match.</summary>
public static class ResultHttp
{
    public static IResult ToProblem(this Error error) => error.Type switch
    {
        ErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Request"] = [error.Message],
        }),
        ErrorType.NotFound => Results.NotFound(),
        ErrorType.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: error.Message),
        ErrorType.Forbidden => Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: error.Message),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: error.Message),
    };

    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error!.ToProblem();
}

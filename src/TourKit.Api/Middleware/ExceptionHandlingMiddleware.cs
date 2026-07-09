using TourKit.Application.Common;

namespace TourKit.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (AppException ex)
        {
            var status = ex.ErrorType switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "conflict" => StatusCodes.Status409Conflict,
                "forbidden" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest,
            };
            await Results.Problem(detail: ex.Message, statusCode: status, title: ex.ErrorType).ExecuteAsync(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi chưa xử lý");
            await Results.Problem(detail: "Đã có lỗi xảy ra.", statusCode: 500).ExecuteAsync(ctx);
        }
    }
}

namespace TourKit.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (LoginRequest body, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(body, ct);
            return result is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Đăng nhập thất bại.")
                : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest body, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(body.RefreshToken, ct);
            return result is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token không hợp lệ.")
                : Results.Ok(result);
        }).AllowAnonymous();

        return app;
    }
}

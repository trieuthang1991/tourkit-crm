namespace TourKit.Api.Provisioning;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/registration", async (
            RegisterTenantRequest body, IProvisioningService svc, CancellationToken ct) =>
        {
            var outcome = await svc.RegisterAsync(body, ct);
            return outcome.Error switch
            {
                RegistrationError.None =>
                    Results.Created($"/api/v1/tenants/{outcome.Response!.TenantId}", outcome.Response),
                RegistrationError.SlugTaken =>
                    Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Slug đã được dùng."),
                _ => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Request"] = ["Thiếu thông tin hoặc mật khẩu < 8 ký tự."],
                }),
            };
        }).AllowAnonymous();

        return app;
    }
}

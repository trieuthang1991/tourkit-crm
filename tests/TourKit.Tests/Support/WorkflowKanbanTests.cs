using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Work;

namespace TourKit.Tests.Support;

/// <summary>Board Kanban động (legacy Workflow/SectionWork) qua /api/v1/workflows — board+cột+kéo thẻ việc.</summary>
public class WorkflowKanbanTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public WorkflowKanbanTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_board_add_card_then_move_across_columns()
    {
        var client = await LoggedInClientAsync("kanban-a");

        // Tạo board → gieo 3 cột mặc định
        var board = await (await client.PostAsJsonAsync("/api/v1/workflows",
            new CreateWorkflowDto("Điều hành tour Đà Nẵng", null, null)))
            .Content.ReadFromJsonAsync<WorkflowDto>();
        Assert.Equal(3, board!.SectionCount);

        var detail = await client.GetFromJsonAsync<WorkflowBoardDto>($"/api/v1/workflows/{board.Id}");
        Assert.Equal(3, detail!.Columns.Count);
        var todo = detail.Columns[0].Section.Id;
        var done = detail.Columns[2].Section.Id;

        // Tạo thẻ việc gắn vào board + cột đầu
        var task = await (await client.PostAsJsonAsync("/api/v1/work-tasks",
            new CreateWorkTaskDto("Xác nhận khách", null, null, null, 1, 0, null, board.Id, todo)))
            .Content.ReadFromJsonAsync<WorkTaskDto>();
        Assert.Equal(todo, task!.SectionId);

        // Board phản ánh thẻ nằm ở cột đầu
        var afterAdd = await client.GetFromJsonAsync<WorkflowBoardDto>($"/api/v1/workflows/{board.Id}");
        Assert.Single(afterAdd!.Columns[0].Tasks);
        Assert.Empty(afterAdd.Columns[2].Tasks);

        // Kéo thẻ sang cột "Hoàn thành"
        var move = await client.PostAsJsonAsync($"/api/v1/workflows/{board.Id}/tasks/{task.Id}/move",
            new MoveTaskDto(done));
        Assert.Equal(HttpStatusCode.NoContent, move.StatusCode);

        var afterMove = await client.GetFromJsonAsync<WorkflowBoardDto>($"/api/v1/workflows/{board.Id}");
        Assert.Empty(afterMove!.Columns[0].Tasks);
        Assert.Single(afterMove.Columns[2].Tasks);
        Assert.Equal("Xác nhận khách", afterMove.Columns[2].Tasks[0].Title);
    }

    [Fact]
    public async Task Add_and_delete_custom_column()
    {
        var client = await LoggedInClientAsync("kanban-b");
        var board = await (await client.PostAsJsonAsync("/api/v1/workflows",
            new CreateWorkflowDto("Board", null, null))).Content.ReadFromJsonAsync<WorkflowDto>();

        var added = await (await client.PostAsJsonAsync($"/api/v1/workflows/{board!.Id}/sections",
            new CreateSectionDto("Chờ duyệt", "#1677ff", null))).Content.ReadFromJsonAsync<WorkflowSectionDto>();
        Assert.Equal(3, added!.Sort);

        var del = await client.DeleteAsync($"/api/v1/workflows/{board.Id}/sections/{added.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detail = await client.GetFromJsonAsync<WorkflowBoardDto>($"/api/v1/workflows/{board.Id}");
        Assert.Equal(3, detail!.Columns.Count);   // về lại 3 cột mặc định
    }
}

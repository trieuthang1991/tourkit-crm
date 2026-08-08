# Trợ lý AI — Giai đoạn 1 (tra cứu tiếng Việt) — Implementation Plan

> # ⚠️ ĐÃ LÀM XONG, NHƯNG KHÁC KẾ HOẠCH NÀY. ĐỪNG THI CÔNG THEO ĐÂY.
>
> Giai đoạn 1 đã chạy thật (commit `c0bf5db` + `43f34fd`). Kế hoạch dưới đây giữ lại để tra cứu lý do
> của các quyết định, **không** còn khớp với mã nguồn.
>
> **Khác ở đâu, và vì sao:** kế hoạch định tự viết hợp đồng hội thoại (`IChatModel`, `AiTurn`,
> `AiCompletion`, JSON Schema viết tay) và một adapter Claude. Thực tế dùng **`Microsoft.Extensions.AI`**
> — `IChatClient` + `AIFunction`, schema tham số **sinh tự động từ chữ ký hàm C#** nên không bao giờ
> lệch khỏi code, và một adapter `TourKit.Ai.OpenAiCompatible` phục vụ mọi hãng nói giao thức OpenAI.
> Nhà cung cấp giai đoạn 1 là **DeepSeek**, không phải Claude.
>
> | Kế hoạch | Thực tế |
> |---|---|
> | `IChatModel`, `AiTurn`, `AiCompletion`, `AiMessage` | `IChatClient`, `ChatMessage`, `ChatOptions`, `ChatResponse` |
> | `IAiTool.ParameterSchema` viết tay | `IAiTool.Function` (`AIFunction`), schema tự sinh |
> | `TourKit.Ai.Anthropic` + SDK `Anthropic` | `TourKit.Ai.OpenAiCompatible` + `Microsoft.Extensions.AI.OpenAI` |
> | Abstractions không tham chiếu gì | Tham chiếu `Microsoft.Extensions.AI.Abstractions` (ngoại lệ duy nhất, có ghi lý do) |
> | 2 tool báo cáo | 9 tool |
> | — | Hạn mức token/lượt theo người + nhật ký tool call |
>
> **Đọc cái gì thay thế:** `docs/ai-config.md` (cấu hình, cách đặt khoá, hạn mức, nhật ký) và
> §3.2b của `docs/superpowers/specs/2026-08-07-ai-integration-design.md` (lý do chọn
> `Microsoft.Extensions.AI`).

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Nhân viên hỏi bằng tiếng Việt trong một khung chat có ở mọi trang, và nhận lại số liệu thật kèm bảng + link sang màn báo cáo tương ứng.

**Architecture:** Ba project. `TourKit.Ai.Abstractions` giữ hợp đồng (năng lực `IChatModel`, `IAiTool`) và không tham chiếu gì — đây là thứ mọi adapter và mọi phân hệ nhìn vào. `TourKit.Ai` giữ registry, vòng lặp chat và các tool, mỗi tool bọc một hàm nghiệp vụ sẵn có của `IReportService`. `TourKit.Ai.Anthropic` là adapter Claude, **chỉ thấy Abstractions** — nhờ vậy thêm OpenAI/Gemini về sau là thêm một project nhỏ, không sửa lõi và không kéo SDK lạ vào các assembly khác. Trước mỗi lượt, registry chỉ đưa cho model những tool mà claim `perm` của người dùng cho phép — phân quyền là hàng rào code, không phải câu nhắc trong prompt.

**Tech Stack:** .NET 9, NuGet `Anthropic` (SDK chính thức), Razor Pages + `tk.js`/`tourkit.css` sẵn có, xUnit.

## Global Constraints

Mọi task đều phải tuân các ràng buộc dưới đây.

- **Model:** `claude-opus-5` — chuỗi ID chính xác, KHÔNG thêm hậu tố ngày.
- **Thinking:** Opus 5 bật thinking mặc định. KHÔNG gửi `budget_tokens` (400). KHÔNG gửi `temperature` / `top_p` / `top_k` (400).
- **Effort:** `OutputConfig.Effort = Effort.High`.
- **`max_tokens`:** 16000 cho request không streaming. Nếu sau này nâng quá ~16000 thì bắt buộc chuyển sang streaming.
- **`stop_reason == "refusal"`:** phải kiểm tra TRƯỚC khi đọc `response.Content` — Opus 5 có thể từ chối và trả về `Content` rỗng.
- **Số vòng gọi tool tối đa mỗi lượt:** 5.
- **Prompt caching:** đặt `CacheControl = new CacheControlEphemeral()` trên block system cuối cùng (tools render trước system nên cache gộp cả hai). Ngưỡng cache tối thiểu của Opus 5 là 512 token.
- **Phân quyền:** mọi tool khai báo `RequiredPermission` là chuỗi mã quyền. `TourKit.Ai` KHÔNG được tham chiếu `TourKit.Api` (chiều phụ thuộc), nên mã quyền là chuỗi thường; có test riêng ở tầng Api đối chiếu với `Permissions.All`.
- **Chiều phụ thuộc (ép bằng arch test, không bằng thoả thuận):**
  - `TourKit.Ai.Abstractions` → **không tham chiếu gì**.
  - `TourKit.Ai` → `Abstractions` + `TourKit.Application`.
  - `TourKit.Ai.Anthropic` → **chỉ `Abstractions`**. Adapter KHÔNG được thấy `TourKit.Ai`; thấy rồi thì sửa vòng lặp chat sẽ bắt sửa lại mọi adapter.
  - `TourKit.Api` → tất cả (composition root, nơi duy nhất chọn adapter nào).
  - `TourKit.Infrastructure` KHÔNG dính gì tới AI.
- **Không truy vấn EF trong `TourKit.Ai`** — tool chỉ gọi interface service của tầng Application.
- **Ngày nghiệp vụ** neo offset 0 qua `TkDate.Day()` khi tool nhận tham số ngày.
- **Tiếng Việt** cho mọi `Description` của tool và mọi chuỗi hiện ra giao diện.

---

## File Structure

**Tạo mới**

| File | Trách nhiệm |
|---|---|
| `src/TourKit.Ai.Abstractions/TourKit.Ai.Abstractions.csproj` | Hợp đồng dùng chung, **không tham chiếu gì** |
| `src/TourKit.Ai.Abstractions/IAiTool.cs` | Hợp đồng của một tool + `AiToolResult` + `AiSchema` |
| `src/TourKit.Ai.Abstractions/IChatModel.cs` | Năng lực hội thoại + gọi công cụ, không dính vendor + các record request/response |
| `src/TourKit.Ai/TourKit.Ai.csproj` | Lõi điều phối, tham chiếu Abstractions + `TourKit.Application` |
| `src/TourKit.Ai/AiToolRegistry.cs` | Lọc tool theo quyền, tra tool theo tên |
| `src/TourKit.Ai/AiChatService.cs` | Vòng lặp: gọi model → chạy tool → gọi lại, tối đa 5 vòng |
| `src/TourKit.Ai/AiPrompts.cs` | Prompt hệ thống (một chuỗi hằng, đủ dài để cache) |
| `src/TourKit.Ai/Tools/TurnoverByBranchTool.cs` | Doanh thu theo chi nhánh |
| `src/TourKit.Ai/Tools/OrderDebtTool.cs` | Công nợ phải thu theo đơn |
| `src/TourKit.Ai/Tools/TopCustomersTool.cs` | Top khách hàng theo doanh thu |
| `src/TourKit.Ai/Tools/CashFlowTool.cs` | Dòng tiền theo phương thức thanh toán |
| `src/TourKit.Ai/Tools/KpiSummaryTool.cs` | Phễu kinh doanh (báo giá → đơn → thu tiền) |
| `src/TourKit.Ai/AiServiceCollectionExtensions.cs` | Đăng ký lõi + toàn bộ tool vào DI |
| `src/TourKit.Ai/LogChatModel.cs` | Adapter giả dev không cần khoá — ghi log, trả lời cố định |
| `src/TourKit.Ai.Anthropic/TourKit.Ai.Anthropic.csproj` | Adapter Claude, **chỉ tham chiếu Abstractions** + SDK `Anthropic` |
| `src/TourKit.Ai.Anthropic/AnthropicOptions.cs` | Cấu hình khoá + model của riêng Claude |
| `src/TourKit.Ai.Anthropic/ClaudeChatModel.cs` | Client Claude thật (SDK `Anthropic`) |
| `src/TourKit.Api/Pages/Ai/Chat.cshtml` + `.cshtml.cs` | Handler JSON `/tro-ly` |
| `src/TourKit.Api/wwwroot/js/tk-ai.js` | Khung chat trượt phải |
| `tests/TourKit.UnitTests/Ai/AiToolRegistryTests.cs` | Test lọc quyền — test an toàn quan trọng nhất |
| `tests/TourKit.UnitTests/Ai/FakeReportService.cs` | Bản giả `IReportService` cho test tool |
| `tests/TourKit.UnitTests/Ai/ReportToolsTests.cs` | Test từng tool trả đúng dữ liệu |
| `tests/TourKit.UnitTests/Ai/FakeChatModel.cs` | Provider giả để test vòng lặp |
| `tests/TourKit.UnitTests/Ai/AiChatServiceTests.cs` | Test vòng lặp agent |
| `tests/TourKit.ArchTests/AiLayeringTests.cs` | Ép chiều phụ thuộc của `TourKit.Ai` |
| `tests/TourKit.Tests/Ai/AiPermissionCodeTests.cs` | Mã quyền của tool phải tồn tại trong `Permissions.All` |

**Sửa**

| File | Sửa gì |
|---|---|
| `TourKit.sln` | Thêm 3 project: `TourKit.Ai.Abstractions`, `TourKit.Ai`, `TourKit.Ai.Anthropic` |
| `src/TourKit.Api/TourKit.Api.csproj` | Thêm ProjectReference `TourKit.Ai` + `TourKit.Ai.Anthropic` |
| `src/TourKit.Api/Program.cs` | Đăng ký AI (theo khuôn Email/SMS ở dòng ~115-136) |
| `src/TourKit.Api/Routing/RouteMap.cs` | Thêm `("/Ai/Chat", "tro-ly")` |
| `src/TourKit.Api/appsettings.json` | Thêm section `Ai` |
| `src/TourKit.Api/Pages/Shared/Layouts/Sections/Navbar/_Navbar.cshtml` | Nút mở trợ lý |
| `src/TourKit.Api/Pages/Shared/Layouts/Sections/_Scripts.cshtml` | Nạp `tk-ai.js` |
| `src/TourKit.Api/wwwroot/css/tourkit.css` | Kiểu khung chat |

---

### Task 1: Project lõi + hợp đồng tool + lọc quyền

Đây là task quan trọng nhất về an toàn: phân quyền được cài đặt ở đây và không có LLM nào tham gia, nên test được tất định 100%.

**Files:**
- Create: `src/TourKit.Ai.Abstractions/TourKit.Ai.Abstractions.csproj`
- Create: `src/TourKit.Ai.Abstractions/IAiTool.cs`
- Create: `src/TourKit.Ai/TourKit.Ai.csproj`
- Create: `src/TourKit.Ai/AiToolRegistry.cs`
- Create: `tests/TourKit.UnitTests/Ai/AiToolRegistryTests.cs`
- Create: `tests/TourKit.ArchTests/AiLayeringTests.cs`
- Modify: `TourKit.sln`

**Interfaces:**
- Consumes: không có (task đầu tiên).
- Produces: `IAiTool` (`Name`, `Description`, `ParameterSchema`, `RequiredPermission`, `InvokeAsync`), `AiToolResult(string Text, object? Data, string? LinkUrl)`, `AiSchema.NoParameters`, `AiToolRegistry.For(IReadOnlySet<string>) → IReadOnlyList<IAiTool>`, `AiToolRegistry.Find(IReadOnlyList<IAiTool>, string) → IAiTool?`.

- [ ] **Bước 1: Tạo hai project và nối vào solution**

```bash
cd /d/MiGroup/AI/tourkit-crm/tourkit-crm

dotnet new classlib -o src/TourKit.Ai.Abstractions -n TourKit.Ai.Abstractions --framework net9.0
rm src/TourKit.Ai.Abstractions/Class1.cs

dotnet new classlib -o src/TourKit.Ai -n TourKit.Ai --framework net9.0
rm src/TourKit.Ai/Class1.cs
dotnet add src/TourKit.Ai reference src/TourKit.Ai.Abstractions
dotnet add src/TourKit.Ai reference src/TourKit.Application

dotnet sln add src/TourKit.Ai.Abstractions src/TourKit.Ai
```

Mở cả hai `.csproj` và **xoá** dòng `<TargetFramework>`, `<Nullable>`, `<ImplicitUsings>` nếu `dotnet new` sinh ra — repo này lấy chúng từ `Directory.Build.props` (nguồn duy nhất).

`src/TourKit.Ai.Abstractions/TourKit.Ai.Abstractions.csproj` — **cố ý không có ItemGroup nào**. Mỗi tham chiếu thêm vào đây là một thứ mọi adapter tương lai buộc phải kéo theo:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- TargetFramework/Nullable/ImplicitUsings kế thừa từ Directory.Build.props (nguồn duy nhất) -->
  <!-- KHÔNG thêm ProjectReference/PackageReference vào đây: đây là hợp đồng dùng chung, -->
  <!-- mọi adapter (Claude, OpenAI, Voyage, FPT.AI) đều phải tham chiếu nó. -->

</Project>
```

`src/TourKit.Ai/TourKit.Ai.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- TargetFramework/Nullable/ImplicitUsings kế thừa từ Directory.Build.props (nguồn duy nhất) -->

  <ItemGroup>
    <ProjectReference Include="..\TourKit.Ai.Abstractions\TourKit.Ai.Abstractions.csproj" />
    <ProjectReference Include="..\TourKit.Application\TourKit.Application.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Bước 2: Viết hợp đồng tool**

Tạo `src/TourKit.Ai.Abstractions/IAiTool.cs`:

```csharp
using System.Text.Json;

namespace TourKit.Ai.Abstractions;

/// <summary>
/// Một "công cụ" AI được phép gọi. Mỗi tool bọc ĐÚNG MỘT hàm nghiệp vụ đã có ở tầng Application —
/// nhờ vậy bộ lọc tenant, soft-delete và phân quyền của hàm đó tự động áp dụng, model không có
/// đường nào đi vòng qua.
/// </summary>
public interface IAiTool
{
    /// <summary>Tên máy, snake_case không dấu — model dùng tên này để gọi.</summary>
    string Name { get; }

    /// <summary>Mô tả TIẾNG VIỆT. Model đọc đúng chuỗi này để quyết định có gọi tool hay không.</summary>
    string Description { get; }

    /// <summary>JSON Schema của tham số. Rỗng (không property) nếu tool không nhận tham số.</summary>
    IReadOnlyDictionary<string, object> ParameterSchema { get; }

    /// <summary>
    /// Mã quyền cần có để THẤY tool này (vd "report.turnover.view"). null = ai đăng nhập cũng thấy.
    /// Đây là hàng rào thật: tool không nằm trong danh sách gửi cho model thì model không gọi được.
    /// </summary>
    string? RequiredPermission { get; }

    Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct);
}

/// <summary>
/// Kết quả tool trả về, tách làm hai phần: <paramref name="Text"/> cho model đọc và diễn đạt lại,
/// <paramref name="Data"/> cho giao diện vẽ bảng. Nhờ vậy câu trả lời không phải một đoạn văn kể số
/// mà là bảng thật người dùng kiểm chứng được ngay.
/// </summary>
/// <param name="LinkUrl">Đường dẫn màn hình tương ứng để người dùng bấm sang xem đầy đủ.</param>
public sealed record AiToolResult(string Text, object? Data = null, string? LinkUrl = null);

/// <summary>Mảnh JSON Schema dùng lại giữa các tool.</summary>
public static class AiSchema
{
    /// <summary>Schema cho tool không nhận tham số — vẫn phải là object rỗng, không được để null.</summary>
    public static readonly IReadOnlyDictionary<string, object> NoParameters =
        new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>(),
        };
}
```

- [ ] **Bước 3: Viết test lọc quyền (test sẽ FAIL)**

Tạo `tests/TourKit.UnitTests/Ai/AiToolRegistryTests.cs`:

```csharp
using System.Text.Json;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

public class AiToolRegistryTests
{
    private sealed class StubTool(string name, string? perm) : IAiTool
    {
        public string Name => name;
        public string Description => "stub";
        public IReadOnlyDictionary<string, object> ParameterSchema => new Dictionary<string, object>();
        public string? RequiredPermission => perm;
        public Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
            => Task.FromResult(new AiToolResult("ok"));
    }

    private static AiToolRegistry Registry() => new(
    [
        new StubTool("cong_khai", null),
        new StubTool("doanh_thu", "report.turnover.view"),
        new StubTool("cong_no", "report.debt.view"),
    ]);

    [Fact]
    public void Chi_tra_ve_tool_dung_quyen()
    {
        var allowed = Registry().For(new HashSet<string> { "report.turnover.view" });

        Assert.Equal(["cong_khai", "doanh_thu"], allowed.Select(t => t.Name).Order());
    }

    [Fact]
    public void Khong_co_quyen_nao_thi_chi_thay_tool_cong_khai()
    {
        var allowed = Registry().For(new HashSet<string>());

        Assert.Equal(["cong_khai"], allowed.Select(t => t.Name));
    }

    [Fact]
    public void Find_khong_tra_ve_tool_ngoai_danh_sach_da_loc()
    {
        var registry = Registry();
        var allowed = registry.For(new HashSet<string>());

        // Model bịa tên một tool nó không được thấy — registry phải trả null, không phải tool thật.
        Assert.Null(registry.Find(allowed, "cong_no"));
    }

    [Fact]
    public void Find_tra_ve_tool_khi_nam_trong_danh_sach()
    {
        var registry = Registry();
        var allowed = registry.For(new HashSet<string> { "report.debt.view" });

        Assert.Equal("cong_no", registry.Find(allowed, "cong_no")?.Name);
    }
}
```

Thêm ProjectReference vào test project:

```bash
dotnet add tests/TourKit.UnitTests reference src/TourKit.Ai.Abstractions
dotnet add tests/TourKit.UnitTests reference src/TourKit.Ai
```

- [ ] **Bước 4: Chạy test để chắc chắn nó FAIL**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~AiToolRegistryTests"
```

Mong đợi: build lỗi `CS0246: The type or namespace name 'AiToolRegistry' could not be found`.

- [ ] **Bước 5: Viết registry**

Tạo `src/TourKit.Ai/AiToolRegistry.cs`:

```csharp
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>
/// Danh mục tool và bộ lọc quyền. Phân quyền của trợ lý = LỌC DANH SÁCH TOOL đưa cho model,
/// không phải câu dặn dò trong prompt: kế toán không thấy tool giá vốn thì không có cách nào gọi nó,
/// kể cả khi người dùng cố dụ model.
/// </summary>
public sealed class AiToolRegistry(IEnumerable<IAiTool> tools)
{
    private readonly IReadOnlyList<IAiTool> _all = tools.ToList();

    /// <summary>Danh sách tool người dùng với bộ quyền <paramref name="perms"/> được phép thấy.</summary>
    public IReadOnlyList<IAiTool> For(IReadOnlySet<string> perms) =>
        _all.Where(t => t.RequiredPermission is null || perms.Contains(t.RequiredPermission)).ToList();

    /// <summary>
    /// Tra tool theo tên NHƯNG chỉ trong danh sách đã lọc — không bao giờ tra trên <c>_all</c>.
    /// Model bịa tên một tool ngoài quyền thì hàm này trả null.
    /// </summary>
    public IAiTool? Find(IReadOnlyList<IAiTool> allowed, string name) =>
        allowed.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));
}
```

- [ ] **Bước 6: Chạy test để chắc chắn nó PASS**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~AiToolRegistryTests"
```

Mong đợi: `Passed! - Failed: 0, Passed: 4`.

- [ ] **Bước 7: Viết arch test ép chiều phụ thuộc**

Tạo `tests/TourKit.ArchTests/AiLayeringTests.cs`:

```csharp
using System.Reflection;
using NetArchTest.Rules;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.ArchTests;

/// <summary>
/// Ép ba ranh giới của cụm AI. Hai cái đầu bảo vệ dữ liệu: lõi chạm được Api thì mã quyền và tenant
/// có thể bị đi vòng, chạm được EF thì tool tự viết truy vấn được. Cái thứ ba bảo vệ khả năng mở
/// rộng: Abstractions mà phình ra là mọi adapter tương lai phải gánh theo.
/// </summary>
public class AiLayeringTests
{
    private static readonly Assembly Abstractions = typeof(IAiTool).Assembly;
    private static readonly Assembly Ai = typeof(AiToolRegistry).Assembly;

    [Fact]
    public void Ai_khong_phu_thuoc_Api_hay_Infrastructure()
    {
        var result = Types.InAssembly(Ai)
            .ShouldNot().HaveDependencyOnAny("TourKit.Api", "TourKit.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    [Fact]
    public void Ai_khong_dung_EF_truc_tiep()
    {
        var result = Types.InAssembly(Ai)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    /// <summary>
    /// Abstractions phải trần trụi: nó là thứ MỌI adapter tham chiếu, nên mỗi phụ thuộc thêm vào đây
    /// là một thứ adapter OpenAI/Voyage/FPT.AI sau này buộc phải kéo theo dù không dùng.
    /// </summary>
    [Fact]
    public void Abstractions_khong_phu_thuoc_bat_ky_project_TourKit_nao()
    {
        var result = Types.InAssembly(Abstractions)
            .ShouldNot().HaveDependencyOnAny(
                "TourKit.Api", "TourKit.Ai.", "TourKit.Infrastructure", "TourKit.Application", "TourKit.Shared")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }

    private static string Fail(TestResult result) =>
        result.FailingTypeNames is null ? "" : string.Join(", ", result.FailingTypeNames);
}
```

```bash
dotnet add tests/TourKit.ArchTests reference src/TourKit.Ai.Abstractions
dotnet add tests/TourKit.ArchTests reference src/TourKit.Ai
```

> Test "adapter không được thấy `TourKit.Ai`" nằm ở Task 4, khi project adapter tồn tại.

- [ ] **Bước 8: Chạy toàn bộ test**

```bash
dotnet test --nologo 2>&1 | tail -5
```

Mong đợi: tất cả project PASS, không có `Failed: ` khác 0.

- [ ] **Bước 9: Commit**

```bash
git add src/TourKit.Ai.Abstractions src/TourKit.Ai tests/TourKit.UnitTests/Ai tests/TourKit.ArchTests/AiLayeringTests.cs TourKit.sln tests/TourKit.UnitTests/TourKit.UnitTests.csproj tests/TourKit.ArchTests/TourKit.ArchTests.csproj
git commit -m @'
feat(ai): hợp đồng tool tách riêng + lọc quyền

Tách Abstractions khỏi lõi ngay từ đầu vì hệ thống sẽ có nhiều adapter (Claude,
OpenAI, Voyage, FPT.AI). Abstractions cố ý không tham chiếu gì: nó là thứ mọi
adapter phải kéo theo, mỗi phụ thuộc thêm vào đó là gánh nặng cho tất cả.

Phân quyền làm bằng cách lọc danh sách tool đưa cho model, không phải dặn dò
trong prompt: tool ngoài quyền không nằm trong danh sách nên model không có tên
để gọi. Find() cố tình chỉ tra trên danh sách đã lọc.
'@
```

---

### Task 2: Hai tool báo cáo đầu tiên

**Files:**
- Create: `src/TourKit.Ai/Tools/TurnoverByBranchTool.cs`
- Create: `src/TourKit.Ai/Tools/OrderDebtTool.cs`
- Create: `tests/TourKit.UnitTests/Ai/FakeReportService.cs`
- Create: `tests/TourKit.UnitTests/Ai/ReportToolsTests.cs`

**Interfaces:**
- Consumes: `IAiTool`, `AiToolResult` (Task 1); `TourKit.Application.Reports.IReportService` với `GetTurnoverByBranchAsync() → Task<IReadOnlyList<TurnoverByBranchRowDto>>` và `GetOrderDebtAsync() → Task<IReadOnlyList<OrderDebtRowDto>>`.
- Produces: `TurnoverByBranchTool` (Name `bao_cao_doanh_thu_theo_chi_nhanh`), `OrderDebtTool` (Name `bao_cao_cong_no_phai_thu`). Cả hai nhận `IReportService` qua primary constructor.

DTO liên quan (đã có sẵn, không sửa):

```csharp
public sealed record TurnoverByBranchRowDto(
    Guid? BranchId, string BranchName, int OrderCount,
    decimal Turnover, decimal Received, decimal Outstanding, decimal Cost, decimal Profit);

public sealed record OrderDebtRowDto(
    Guid OrderId, string OrderCode, Guid CustomerId, decimal Total, decimal Paid, decimal Outstanding);
```

- [ ] **Bước 1: Viết bản giả IReportService**

Tạo `tests/TourKit.UnitTests/Ai/FakeReportService.cs`. `IReportService` có 15 hàm; bản giả ném `NotImplementedException` cho hàm chưa cần, chỉ cài hàm nào test dùng — như vậy test hỏng ngay nếu tool gọi nhầm hàm.

```csharp
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Bản giả IReportService cho test tool. Hàm chưa cài ném NotImplementedException CÓ CHỦ Ý:
/// nếu một tool gọi nhầm hàm thì test đỏ ngay thay vì âm thầm trả danh sách rỗng.
/// </summary>
internal sealed class FakeReportService : IReportService
{
    public IReadOnlyList<TurnoverByBranchRowDto> Branches { get; set; } = [];
    public IReadOnlyList<OrderDebtRowDto> Debts { get; set; } = [];
    public IReadOnlyList<TopCustomerRowDto> TopCustomers { get; set; } = [];
    public IReadOnlyList<CashFlowRowDto> CashFlows { get; set; } = [];
    public KpiSummaryDto? Kpi { get; set; }

    public Task<IReadOnlyList<TurnoverByBranchRowDto>> GetTurnoverByBranchAsync() => Task.FromResult(Branches);
    public Task<IReadOnlyList<OrderDebtRowDto>> GetOrderDebtAsync() => Task.FromResult(Debts);
    public Task<IReadOnlyList<TopCustomerRowDto>> GetTopCustomersAsync(int top = 10)
        => Task.FromResult<IReadOnlyList<TopCustomerRowDto>>(TopCustomers.Take(top).ToList());
    public Task<IReadOnlyList<CashFlowRowDto>> GetCashFlowAsync() => Task.FromResult(CashFlows);
    public Task<KpiSummaryDto> GetKpiSummaryAsync() => Task.FromResult(Kpi!);

    public Task<IReadOnlyList<ProviderDebtRowDto>> GetProviderDebtAsync() => throw new NotImplementedException();
    public Task<ProviderTxnHistoryDto> GetProviderTransactionsAsync(Guid providerId) => throw new NotImplementedException();
    public Task<DashboardSummaryDto> GetDashboardAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<TurnoverRowDto>> GetTurnoverAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<CommissionByUserRowDto>> GetCommissionByUserAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<CommissionByMilestoneRowDto>> GetCommissionByMilestoneAsync(DateTimeOffset? from, DateTimeOffset? to) => throw new NotImplementedException();
    public Task<IReadOnlyList<TurnoverByDepartmentRowDto>> GetTurnoverByDepartmentAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<MoneyByTourTypeRowDto>> GetMoneyByTourTypeAsync() => throw new NotImplementedException();
    public Task<WorkspacePulseDto> GetWorkspacePulseAsync(int days = 7) => throw new NotImplementedException();
    public Task<IReadOnlyList<RevenuePointDto>> GetRevenueSeriesAsync(DateTimeOffset from, DateTimeOffset to, bool monthly) => throw new NotImplementedException();
}
```

- [ ] **Bước 2: Viết test cho hai tool (test sẽ FAIL)**

Tạo `tests/TourKit.UnitTests/Ai/ReportToolsTests.cs`:

```csharp
using System.Text.Json;
using TourKit.Ai.Tools;
using TourKit.Application.Reports.Dtos;

namespace TourKit.UnitTests.Ai;

public class ReportToolsTests
{
    private static JsonElement NoArgs() => JsonDocument.Parse("{}").RootElement;

    [Fact]
    public async Task Doanh_thu_theo_chi_nhanh_tra_ve_du_lieu_va_link()
    {
        var svc = new FakeReportService
        {
            Branches =
            [
                new(Guid.NewGuid(), "Hà Nội", 12, 900_000_000m, 700_000_000m, 200_000_000m, 500_000_000m, 400_000_000m),
                new(Guid.NewGuid(), "Đà Nẵng", 5, 300_000_000m, 300_000_000m, 0m, 180_000_000m, 120_000_000m),
            ],
        };

        var result = await new TurnoverByBranchTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        Assert.Contains("Hà Nội", result.Text);
        Assert.Contains("Đà Nẵng", result.Text);
        Assert.Equal("/hieu-suat-chi-nhanh", result.LinkUrl);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Doanh_thu_theo_chi_nhanh_khong_co_du_lieu_thi_noi_ro()
    {
        var result = await new TurnoverByBranchTool(new FakeReportService()).InvokeAsync(NoArgs(), CancellationToken.None);

        Assert.Contains("Không có", result.Text);
    }

    [Fact]
    public async Task Cong_no_chi_lay_don_con_no_va_sap_xep_giam_dan()
    {
        var svc = new FakeReportService
        {
            Debts =
            [
                new(Guid.NewGuid(), "DH0001", Guid.NewGuid(), 100m, 100m, 0m),      // đã thu đủ — phải bị loại
                new(Guid.NewGuid(), "DH0002", Guid.NewGuid(), 500m, 100m, 400m),
                new(Guid.NewGuid(), "DH0003", Guid.NewGuid(), 900m, 100m, 800m),
            ],
        };

        var result = await new OrderDebtTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        Assert.DoesNotContain("DH0001", result.Text);
        Assert.True(result.Text.IndexOf("DH0003", StringComparison.Ordinal)
                  < result.Text.IndexOf("DH0002", StringComparison.Ordinal),
            "Đơn nợ nhiều nhất phải đứng trước");
    }

    [Fact]
    public async Task Cong_no_gioi_han_20_dong()
    {
        var svc = new FakeReportService
        {
            Debts = Enumerable.Range(1, 50)
                .Select(i => new OrderDebtRowDto(Guid.NewGuid(), $"DH{i:0000}", Guid.NewGuid(), 1000m, 0m, 1000m - i))
                .ToList(),
        };

        var result = await new OrderDebtTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        // 20 dòng nợ nhiều nhất: DH0001..DH0020. DH0021 trở đi bị cắt.
        Assert.Contains("DH0020", result.Text);
        Assert.DoesNotContain("DH0021", result.Text);
    }

    [Fact]
    public void Moi_tool_deu_khai_bao_quyen_va_mo_ta_tieng_viet()
    {
        var svc = new FakeReportService();

        Assert.Equal("report.turnover.view", new TurnoverByBranchTool(svc).RequiredPermission);
        Assert.Equal("report.debt.view", new OrderDebtTool(svc).RequiredPermission);
        Assert.Contains("chi nhánh", new TurnoverByBranchTool(svc).Description);
    }
}
```

- [ ] **Bước 3: Chạy test để chắc chắn nó FAIL**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~ReportToolsTests"
```

Mong đợi: `CS0246: ... 'TurnoverByBranchTool' could not be found`.

- [ ] **Bước 4: Viết tool doanh thu theo chi nhánh**

Tạo `src/TourKit.Ai/Tools/TurnoverByBranchTool.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Doanh thu / thực thu / còn thiếu / lợi nhuận theo chi nhánh. Gọi thẳng IReportService nên bộ lọc
/// tenant của hàm đó tự áp — tool không tự viết truy vấn.
/// </summary>
public sealed class TurnoverByBranchTool(IReportService reports) : IAiTool
{
    public string Name => "bao_cao_doanh_thu_theo_chi_nhanh";

    public string Description =>
        "Doanh thu, thực thu, còn thiếu và lợi nhuận của từng chi nhánh, kèm số đơn hàng. " +
        "Dùng khi người hỏi muốn so sánh các chi nhánh hoặc hỏi một chi nhánh cụ thể thu được bao nhiêu.";

    public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;

    public string? RequiredPermission => "report.turnover.view";

    public async Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        var rows = await reports.GetTurnoverByBranchAsync();
        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu doanh thu theo chi nhánh.");
        }

        var sb = new StringBuilder("Doanh thu theo chi nhánh (đơn vị: đồng):\n");
        foreach (var r in rows.OrderByDescending(r => r.Turnover))
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"- {r.BranchName}: {r.OrderCount} đơn, doanh thu {r.Turnover:N0}, thực thu {r.Received:N0}, còn thiếu {r.Outstanding:N0}, lợi nhuận {r.Profit:N0}\n");
        }

        var data = rows.OrderByDescending(r => r.Turnover).Select(r => new
        {
            branch = r.BranchName,
            orders = r.OrderCount,
            turnover = r.Turnover,
            received = r.Received,
            outstanding = r.Outstanding,
            profit = r.Profit,
        }).ToList();

        return new AiToolResult(sb.ToString(), data, "/hieu-suat-chi-nhanh");
    }
}
```

- [ ] **Bước 5: Viết tool công nợ**

Tạo `src/TourKit.Ai/Tools/OrderDebtTool.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Công nợ phải thu theo đơn hàng. Chỉ trả về đơn CÒN NỢ và cắt ở 20 dòng nợ nhiều nhất —
/// đưa cả trăm dòng vào ngữ cảnh vừa tốn token vừa làm model trả lời loãng.
/// </summary>
public sealed class OrderDebtTool(IReportService reports) : IAiTool
{
    private const int MaxRows = 20;

    public string Name => "bao_cao_cong_no_phai_thu";

    public string Description =>
        "Danh sách đơn hàng còn nợ tiền khách: tổng tiền, đã thu, còn nợ. " +
        $"Trả về {MaxRows} đơn nợ nhiều nhất. Dùng khi người hỏi muốn biết ai đang nợ hoặc tổng công nợ phải thu.";

    public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;

    public string? RequiredPermission => "report.debt.view";

    public async Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        var rows = (await reports.GetOrderDebtAsync())
            .Where(r => r.Outstanding > 0)
            .OrderByDescending(r => r.Outstanding)
            .Take(MaxRows)
            .ToList();

        if (rows.Count == 0)
        {
            return new AiToolResult("Không có đơn hàng nào còn nợ.");
        }

        var total = rows.Sum(r => r.Outstanding);
        var sb = new StringBuilder(CultureInfo.InvariantCulture,
            $"Tổng còn nợ của {rows.Count} đơn nợ nhiều nhất: {total:N0} đồng.\n");
        foreach (var r in rows)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"- {r.OrderCode}: tổng {r.Total:N0}, đã thu {r.Paid:N0}, còn nợ {r.Outstanding:N0}\n");
        }

        var data = rows.Select(r => new
        {
            orderCode = r.OrderCode,
            total = r.Total,
            paid = r.Paid,
            outstanding = r.Outstanding,
        }).ToList();

        return new AiToolResult(sb.ToString(), data, "/cong-no-phai-thu");
    }
}
```

- [ ] **Bước 6: Kiểm tra hai route đích có thật**

```bash
grep -n "hieu-suat-chi-nhanh\|cong-no-phai-thu" src/TourKit.Api/Routing/RouteMap.cs
```

Mong đợi: cả hai route đều xuất hiện. **Nếu route nào không có**, mở `RouteMap.cs`, tìm route thật của màn báo cáo tương ứng và sửa `LinkUrl` trong tool + sửa `Assert.Equal(...)` trong test cho khớp. Không được để link chết.

- [ ] **Bước 7: Chạy test để chắc chắn nó PASS**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~ReportToolsTests"
```

Mong đợi: `Passed! - Failed: 0, Passed: 5`.

- [ ] **Bước 8: Commit**

```bash
git add src/TourKit.Ai/Tools tests/TourKit.UnitTests/Ai
git commit -m @'
feat(ai): hai tool báo cáo đầu tiên — doanh thu chi nhánh và công nợ

Mỗi tool bọc đúng một hàm IReportService, không tự viết truy vấn: bộ lọc tenant
và soft-delete của hàm đó tự áp dụng. Tool công nợ cắt ở 20 dòng nợ nhiều nhất
vì đưa cả trăm dòng vào ngữ cảnh vừa tốn token vừa làm câu trả lời loãng.

Bản giả IReportService ném NotImplementedException cho hàm chưa cài — tool gọi
nhầm hàm thì test đỏ ngay thay vì âm thầm trả danh sách rỗng.
'@
```

---

### Task 3: Vòng lặp agent (chưa cần LLM thật)

**Files:**
- Create: `src/TourKit.Ai.Abstractions/IChatModel.cs`
- Create: `src/TourKit.Ai/AiPrompts.cs`
- Create: `src/TourKit.Ai/AiChatService.cs`
- Create: `tests/TourKit.UnitTests/Ai/FakeChatModel.cs`
- Create: `tests/TourKit.UnitTests/Ai/AiChatServiceTests.cs`

**Interfaces:**
- Consumes: `IAiTool`, `AiToolResult`, `AiToolRegistry` (Task 1).
- Produces:
  - `AiMessage(string Role, string Text, string? ToolCallId = null)` — lịch sử hội thoại đơn giản hoá cho tầng gọi.
  - `AiToolCall(string Id, string Name, JsonElement Arguments)`
  - `AiCompletion(string? Text, IReadOnlyList<AiToolCall> ToolCalls, bool Refused = false)`
  - `AiTurn(string SystemPrompt, IReadOnlyList<AiMessage> History, IReadOnlyList<IAiTool> Tools)`
  - `IChatModel.Id` và `IChatModel.CompleteAsync(AiTurn, CancellationToken) → Task<AiCompletion>`
  - `AiChatService.AskAsync(string question, IReadOnlySet<string> perms, CancellationToken) → Task<AiAnswer>`
  - `AiAnswer(string Text, IReadOnlyList<AiAnswerBlock> Blocks)`; `AiAnswerBlock(string Tool, object? Data, string? LinkUrl)`

- [ ] **Bước 1: Viết hợp đồng năng lực hội thoại**

Tạo `src/TourKit.Ai.Abstractions/IChatModel.cs`:

```csharp
using System.Text.Json;

namespace TourKit.Ai.Abstractions;

/// <summary>Một lượt trong lịch sử hội thoại gửi cho model. Role: "user" | "assistant" | "tool".</summary>
/// <param name="ToolCallId">Chỉ có với role "tool" — nối kết quả về đúng lời gọi.</param>
public sealed record AiMessage(string Role, string Text, string? ToolCallId = null);

/// <summary>Model yêu cầu gọi một tool.</summary>
public sealed record AiToolCall(string Id, string Name, JsonElement Arguments);

/// <summary>
/// Kết quả một lần gọi model. <paramref name="Refused"/> = model từ chối vì chính sách an toàn
/// (Opus 5 trả HTTP 200 với stop_reason "refusal" và nội dung rỗng) — phải kiểm tra trước khi đọc Text.
/// </summary>
public sealed record AiCompletion(string? Text, IReadOnlyList<AiToolCall> ToolCalls, bool Refused = false);

/// <summary>
/// Đầu vào một lần gọi model: prompt hệ thống + lịch sử + danh sách tool ĐÃ LỌC THEO QUYỀN.
/// Prompt là THAM SỐ chứ không phải hằng số adapter tự đọc — adapter không được biết gì về nghiệp vụ
/// TourKit, nó chỉ biết cách nói chuyện với một nhà cung cấp.
/// </summary>
public sealed record AiTurn(string SystemPrompt, IReadOnlyList<AiMessage> History, IReadOnlyList<IAiTool> Tools);

/// <summary>
/// NĂNG LỰC hội thoại + gọi công cụ. Một adapter = một lớp cài interface này cho một nhà cung cấp
/// (Claude, GPT, Gemini). Các năng lực khác — nhúng vector, đọc giấy tờ — có hình dạng khác hẳn nên
/// sẽ là interface RIÊNG, không nhét vào đây: gộp lại chỉ được một interface mà mỗi cài đặt ném
/// NotSupportedException cho phần lớn số hàm.
/// </summary>
public interface IChatModel
{
    /// <summary>Định danh để cấu hình chọn ("claude", "gpt", "gemini").</summary>
    string Id { get; }

    Task<AiCompletion> CompleteAsync(AiTurn turn, CancellationToken ct);
}
```

- [ ] **Bước 2: Viết prompt hệ thống**

Tạo `src/TourKit.Ai/AiPrompts.cs`. Prompt phải đủ dài để vượt ngưỡng cache 512 token của Opus 5.

```csharp
namespace TourKit.Ai;

/// <summary>
/// Prompt hệ thống của trợ lý. Là HẰNG SỐ, không nội suy ngày giờ hay tên người dùng vào đây:
/// prompt caching khớp theo tiền tố, một byte đổi là mất toàn bộ cache phía sau.
/// Thông tin thay đổi theo lượt đi vào tin nhắn người dùng.
/// </summary>
public static class AiPrompts
{
    public const string System = """
        Bạn là trợ lý nội bộ của phần mềm quản lý tour TourKit, phục vụ nhân viên kinh doanh,
        điều hành và kế toán của một công ty lữ hành Việt Nam.

        # Cách trả lời
        - Luôn trả lời bằng tiếng Việt, xưng "tôi", gọi người dùng là "bạn".
        - Trả lời thẳng vào câu hỏi trước, giải thích sau. Ngắn gọn, không mở bài.
        - Số tiền viết theo kiểu Việt Nam (dấu chấm ngăn nghìn) và luôn kèm đơn vị "đồng".
        - Khi đã gọi công cụ, hãy đọc số liệu trả về và diễn đạt lại; không lặp lại nguyên bảng,
          vì giao diện đã tự vẽ bảng bên dưới câu trả lời của bạn.

        # Dùng công cụ
        - Mọi con số bạn nêu PHẢI đến từ một công cụ. Tuyệt đối không đoán, không nhớ, không suy ra.
        - Nếu không có công cụ nào trả lời được câu hỏi, hãy nói thẳng là bạn không tra được mục này
          và gợi ý người dùng vào màn hình nào để tự xem. Không bịa số.
        - Nếu người dùng hỏi một chỉ tiêu mà công cụ hiện có không cung cấp, đừng thay thế bằng chỉ
          tiêu gần giống mà không nói rõ — hãy nêu rõ bạn đang trả lời bằng chỉ tiêu nào.
        - Một câu hỏi có thể cần nhiều công cụ. Gọi hết trong cùng một lượt nếu chúng độc lập nhau.

        # Giới hạn
        - Bạn chỉ ĐỌC dữ liệu. Bạn không tạo, không sửa, không xoá, không gửi gì cho khách hàng.
          Nếu người dùng nhờ làm những việc đó, hãy nói rõ bạn chưa làm được và chỉ họ vào màn hình
          tương ứng để tự thao tác.
        - Danh sách công cụ bạn thấy đã được lọc theo quyền của người đang hỏi. Nếu bạn không thấy
          công cụ nào cho một loại số liệu, nghĩa là người này không có quyền xem — hãy trả lời rằng
          bạn không tra được mục đó, đừng nói về quyền hạn của họ.
        """;
}
```

- [ ] **Bước 3: Viết provider giả + test vòng lặp (test sẽ FAIL)**

Tạo `tests/TourKit.UnitTests/Ai/FakeChatModel.cs`:

```csharp
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Provider giả chạy theo kịch bản dựng sẵn: mỗi lần được gọi trả về phần tử kế tiếp.
/// Ghi lại số tool mà nó NHÌN THẤY ở từng lượt để test kiểm tra bộ lọc quyền đã áp đúng.
/// </summary>
internal sealed class FakeChatModel(params AiCompletion[] script) : IChatModel
{
    private int _calls;

    public int Calls => _calls;
    public List<IReadOnlyList<string>> ToolNamesSeen { get; } = [];
    public List<IReadOnlyList<AiMessage>> HistoriesSeen { get; } = [];

    public Task<AiCompletion> CompleteAsync(AiTurn turn, CancellationToken ct)
    {
        ToolNamesSeen.Add(turn.Tools.Select(t => t.Name).ToList());
        HistoriesSeen.Add(turn.History);
        var i = _calls++;
        // Hết kịch bản thì trả lời chốt — mô phỏng model dừng gọi tool.
        return Task.FromResult(i < script.Length ? script[i] : new AiCompletion("xong", []));
    }
}
```

Tạo `tests/TourKit.UnitTests/Ai/AiChatServiceTests.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

public class AiChatServiceTests
{
    private static JsonElement Empty() => JsonDocument.Parse("{}").RootElement;

    private sealed class EchoTool(string name, string? perm, string reply) : IAiTool
    {
        public int Invocations { get; private set; }
        public string Name => name;
        public string Description => "echo";
        public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;
        public string? RequiredPermission => perm;
        public Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(new AiToolResult(reply, new { ok = true }, "/dich-den"));
        }
    }

    private static AiChatService Service(IChatModel provider, params IAiTool[] tools) =>
        new(provider, new AiToolRegistry(tools), NullLogger<AiChatService>.Instance);

    [Fact]
    public async Task Goi_tool_roi_tra_loi_bang_luot_thu_hai()
    {
        var tool = new EchoTool("doanh_thu", null, "Doanh thu 5 tỷ");
        var provider = new FakeChatModel(
            new AiCompletion(null, [new AiToolCall("t1", "doanh_thu", Empty())]),
            new AiCompletion("Doanh thu là 5 tỷ đồng.", []));

        var answer = await Service(provider, tool).AskAsync("doanh thu bao nhiêu?", new HashSet<string>(), CancellationToken.None);

        Assert.Equal("Doanh thu là 5 tỷ đồng.", answer.Text);
        Assert.Equal(1, tool.Invocations);
        Assert.Equal(2, provider.Calls);
        Assert.Single(answer.Blocks);
        Assert.Equal("/dich-den", answer.Blocks[0].LinkUrl);
    }

    [Fact]
    public async Task Model_khong_thay_tool_ngoai_quyen()
    {
        var provider = new FakeChatModel(new AiCompletion("ok", []));
        var service = Service(provider,
            new EchoTool("cong_khai", null, "x"),
            new EchoTool("gia_von", "report.cost.view", "y"));

        await service.AskAsync("giá vốn?", new HashSet<string>(), CancellationToken.None);

        Assert.Equal(["cong_khai"], provider.ToolNamesSeen[0]);
    }

    [Fact]
    public async Task Model_goi_tool_khong_ton_tai_thi_bao_loi_ve_cho_model_chu_khong_nem()
    {
        var provider = new FakeChatModel(
            new AiCompletion(null, [new AiToolCall("t1", "khong_co_that", Empty())]),
            new AiCompletion("Tôi không tra được mục này.", []));

        var answer = await Service(provider).AskAsync("hỏi linh tinh", new HashSet<string>(), CancellationToken.None);

        Assert.Equal("Tôi không tra được mục này.", answer.Text);
        // Lượt 2 phải nhận được thông báo lỗi để model tự sửa.
        Assert.Contains(provider.HistoriesSeen[1], m => m.Role == "tool" && m.Text.Contains("không tồn tại"));
    }

    [Fact]
    public async Task Dung_o_vong_thu_5_neu_model_goi_tool_khong_ngung()
    {
        var tool = new EchoTool("lap", null, "z");
        // Kịch bản gọi tool vô hạn: FakeChatModel hết script sẽ trả "xong", nên dựng 10 lượt gọi tool.
        var loop = Enumerable.Range(0, 10)
            .Select(_ => new AiCompletion(null, [new AiToolCall("t", "lap", Empty())]))
            .ToArray();

        var answer = await Service(new FakeChatModel(loop), tool).AskAsync("lặp đi", new HashSet<string>(), CancellationToken.None);

        Assert.Equal(5, tool.Invocations);
        Assert.Contains("chưa hoàn tất", answer.Text);
    }

    [Fact]
    public async Task Model_tu_choi_thi_tra_ve_thong_bao_lich_su_khong_nem_loi()
    {
        var provider = new FakeChatModel(new AiCompletion(null, [], Refused: true));

        var answer = await Service(provider).AskAsync("nội dung nhạy cảm", new HashSet<string>(), CancellationToken.None);

        Assert.Contains("không trả lời được", answer.Text);
        Assert.Empty(answer.Blocks);
    }

    [Fact]
    public async Task Tool_nem_loi_nghiep_vu_thi_dua_thong_diep_ve_cho_model()
    {
        var provider = new FakeChatModel(
            new AiCompletion(null, [new AiToolCall("t1", "hong", Empty())]),
            new AiCompletion("Có lỗi khi tra số liệu.", []));

        var answer = await Service(provider, new ThrowingTool()).AskAsync("hỏi", new HashSet<string>(), CancellationToken.None);

        Assert.Equal("Có lỗi khi tra số liệu.", answer.Text);
        Assert.Contains(provider.HistoriesSeen[1], m => m.Role == "tool" && m.Text.Contains("hỏng rồi"));
    }

    private sealed class ThrowingTool : IAiTool
    {
        public string Name => "hong";
        public string Description => "luôn hỏng";
        public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;
        public string? RequiredPermission => null;
        public Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
            => throw new InvalidOperationException("hỏng rồi");
    }
}
```

Test dùng `NullLogger<T>` — thêm gói nếu chưa có:

```bash
dotnet add tests/TourKit.UnitTests package Microsoft.Extensions.Logging.Abstractions
dotnet add src/TourKit.Ai package Microsoft.Extensions.Logging.Abstractions
```

- [ ] **Bước 4: Chạy test để chắc chắn nó FAIL**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~AiChatServiceTests"
```

Mong đợi: `CS0246: ... 'AiChatService' could not be found`.

- [ ] **Bước 5: Viết vòng lặp agent**

Tạo `src/TourKit.Ai/AiChatService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>Một khối dữ liệu có cấu trúc do tool sinh ra, để giao diện vẽ bảng.</summary>
public sealed record AiAnswerBlock(string Tool, object? Data, string? LinkUrl);

/// <summary>Câu trả lời hoàn chỉnh gửi về giao diện.</summary>
public sealed record AiAnswer(string Text, IReadOnlyList<AiAnswerBlock> Blocks);

/// <summary>
/// Vòng lặp: gọi model → chạy tool model yêu cầu → gọi model lại với kết quả. Tối đa
/// <see cref="MaxRounds"/> vòng để một model gọi tool không ngừng không đốt hết hạn mức.
/// </summary>
public sealed class AiChatService(IChatModel provider, AiToolRegistry registry, ILogger<AiChatService> log)
{
    /// <summary>Số vòng gọi tool tối đa mỗi lượt hỏi.</summary>
    public const int MaxRounds = 5;

    public async Task<AiAnswer> AskAsync(string question, IReadOnlySet<string> perms, CancellationToken ct)
    {
        var allowed = registry.For(perms);
        var history = new List<AiMessage> { new("user", question) };
        var blocks = new List<AiAnswerBlock>();

        for (var round = 0; round < MaxRounds; round++)
        {
            var completion = await provider.CompleteAsync(new AiTurn(AiPrompts.System, history, allowed), ct);

            // Opus 5 có thể từ chối: HTTP 200, stop_reason "refusal", nội dung rỗng.
            // Phải chặn TRƯỚC khi đọc Text, nếu không sẽ hiện ra câu trả lời trống.
            if (completion.Refused)
            {
                log.LogWarning("Trợ lý AI từ chối trả lời câu hỏi (an toàn nội dung).");
                return new AiAnswer("Tôi không trả lời được câu hỏi này. Bạn thử diễn đạt cách khác giúp tôi.", []);
            }

            if (completion.ToolCalls.Count == 0)
            {
                return new AiAnswer(completion.Text ?? "", blocks);
            }

            history.Add(new AiMessage("assistant", completion.Text ?? ""));

            foreach (var call in completion.ToolCalls)
            {
                var tool = registry.Find(allowed, call.Name);
                if (tool is null)
                {
                    // Model bịa tên tool (hoặc gọi tool ngoài quyền). Trả lỗi về cho nó tự sửa,
                    // không ném ra ngoài — một lần bịa tên không nên làm hỏng cả câu trả lời.
                    log.LogInformation("Trợ lý gọi tool không tồn tại/ngoài quyền: {Tool}", call.Name);
                    history.Add(new AiMessage("tool", $"Công cụ '{call.Name}' không tồn tại.", call.Id));
                    continue;
                }

                try
                {
                    var result = await tool.InvokeAsync(call.Arguments, ct);
                    history.Add(new AiMessage("tool", result.Text, call.Id));
                    if (result.Data is not null || result.LinkUrl is not null)
                    {
                        blocks.Add(new AiAnswerBlock(tool.Name, result.Data, result.LinkUrl));
                    }
                }
                catch (Exception ex)
                {
                    // Lỗi nghiệp vụ đưa nguyên thông điệp cho model diễn đạt lại bằng tiếng Việt.
                    log.LogWarning(ex, "Tool {Tool} lỗi khi trợ lý gọi.", tool.Name);
                    history.Add(new AiMessage("tool", $"Lỗi khi chạy công cụ: {ex.Message}", call.Id));
                }
            }
        }

        log.LogWarning("Trợ lý chạm trần {MaxRounds} vòng gọi công cụ.", MaxRounds);
        return new AiAnswer(
            $"Tôi đã tra {MaxRounds} lượt mà chưa hoàn tất câu trả lời. Bạn thử hỏi cụ thể hơn giúp tôi.",
            blocks);
    }
}
```

- [ ] **Bước 6: Chạy test để chắc chắn nó PASS**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~AiChatServiceTests"
```

Mong đợi: `Passed! - Failed: 0, Passed: 6`.

- [ ] **Bước 7: Chạy toàn bộ test**

```bash
dotnet test --nologo 2>&1 | tail -5
```

Mong đợi: mọi project PASS.

- [ ] **Bước 8: Commit**

```bash
git add src/TourKit.Ai tests/TourKit.UnitTests/Ai
git commit -m @'
feat(ai): vòng lặp agent với trần 5 vòng gọi công cụ

Lỗi được xử lý theo ba mức khác nhau vì hậu quả khác nhau: model bịa tên công cụ
thì trả lỗi về cho nó tự sửa, tool ném lỗi nghiệp vụ thì đưa nguyên thông điệp
cho model diễn đạt lại, model từ chối vì an toàn nội dung thì dừng hẳn và trả
câu xin lỗi — nếu không chặn refusal thì giao diện hiện ra câu trả lời trống.

Prompt hệ thống là hằng số, không nội suy ngày giờ hay tên người dùng: cache
khớp theo tiền tố nên một byte đổi là mất toàn bộ cache phía sau.
'@
```

---

### Task 4: Client Claude thật + đăng ký DI

**Files:**
- Create: `src/TourKit.Ai/LogChatModel.cs`
- Create: `src/TourKit.Ai.Anthropic/TourKit.Ai.Anthropic.csproj`
- Create: `src/TourKit.Ai.Anthropic/AnthropicOptions.cs`
- Create: `src/TourKit.Ai.Anthropic/ClaudeChatModel.cs`
- Create: `src/TourKit.Ai/AiServiceCollectionExtensions.cs`
- Modify: `tests/TourKit.ArchTests/AiLayeringTests.cs`
- Modify: `src/TourKit.Api/TourKit.Api.csproj`
- Modify: `src/TourKit.Api/Program.cs`
- Modify: `src/TourKit.Api/appsettings.json`
- Modify: `TourKit.sln`

**Interfaces:**
- Consumes: `IChatModel`, `AiTurn`, `AiCompletion`, `AiToolCall`, `AiMessage`, `IAiTool` (Task 1, 3); `AiPrompts.System`, `AiChatService`, `AiToolRegistry` (Task 3); các tool (Task 2).
- Produces: `AnthropicOptions` (`SectionName`, `ApiKey`, `Model`, `MaxTokens`), `ClaudeChatModel` (`Id` = `"claude"`), `LogChatModel` (`Id` = `"log"`), `AiServiceCollectionExtensions.AddTourKitAi(IServiceCollection)`.

- [ ] **Bước 1: Tạo project adapter**

Adapter nằm ở project riêng chứ không nằm trong `TourKit.Infrastructure`: khi thêm OpenAI/Voyage/FPT.AI sau này, mỗi vendor kéo đúng SDK của nó thay vì dồn hết vào một assembly mà mọi service khác phải mang theo.

```bash
cd /d/MiGroup/AI/tourkit-crm/tourkit-crm
dotnet new classlib -o src/TourKit.Ai.Anthropic -n TourKit.Ai.Anthropic --framework net9.0
rm src/TourKit.Ai.Anthropic/Class1.cs
dotnet add src/TourKit.Ai.Anthropic reference src/TourKit.Ai.Abstractions
dotnet add src/TourKit.Ai.Anthropic package Anthropic
dotnet add src/TourKit.Ai.Anthropic package Microsoft.Extensions.Options
dotnet add src/TourKit.Ai.Anthropic package Microsoft.Extensions.Logging.Abstractions
dotnet sln add src/TourKit.Ai.Anthropic

dotnet add src/TourKit.Api reference src/TourKit.Ai
dotnet add src/TourKit.Api reference src/TourKit.Ai.Anthropic

dotnet build src/TourKit.Ai.Anthropic -v q --nologo
```

Xoá `<TargetFramework>`/`<Nullable>`/`<ImplicitUsings>` khỏi `.csproj` mới như ở Task 1.

**Tuyệt đối KHÔNG chạy** `dotnet add src/TourKit.Ai.Anthropic reference src/TourKit.Ai` — adapter chỉ được thấy `Abstractions`. Bước 7 có arch test chặn việc này.

Mong đợi: build thành công. Ghi lại số phiên bản gói `Anthropic` mà lệnh trên chọn (nó nằm trong `.csproj`) — đó là bản mới nhất, đúng theo luật `.claude/rules/packages.md` của repo.

- [ ] **Bước 2: Viết adapter ghi log (nằm ở lõi, không phải project vendor)**

`LogChatModel` không gọi vendor nào nên thuộc về lõi — để ở project adapter thì máy chưa cấu hình gì vẫn buộc phải tham chiếu project Anthropic.

Tạo `src/TourKit.Ai/LogChatModel.cs`:

```csharp
using Microsoft.Extensions.Logging;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>
/// Adapter mặc định khi chưa cấu hình khoá API: không gọi ra ngoài, chỉ ghi log và trả lời cố định.
/// Nhờ nó mà máy dev không có khoá vẫn chạy được toàn bộ ứng dụng — trợ lý là lớp phụ trợ,
/// không được nằm trên đường đi chính của bất kỳ nghiệp vụ nào.
/// </summary>
public sealed class LogChatModel(ILogger<LogChatModel> log) : IChatModel
{
    public string Id => "log";

    public Task<AiCompletion> CompleteAsync(AiTurn turn, CancellationToken ct)
    {
        log.LogInformation("[AI-LOG] {ToolCount} công cụ khả dụng, {MessageCount} tin nhắn.",
            turn.Tools.Count, turn.History.Count);

        return Task.FromResult(new AiCompletion(
            "Trợ lý chưa được cấu hình khoá API nên chưa trả lời được. Liên hệ quản trị để bật.", []));
    }
}
```

- [ ] **Bước 3: Viết adapter Claude**

Tạo `src/TourKit.Ai.Anthropic/AnthropicOptions.cs`:

```csharp
namespace TourKit.Ai.Anthropic;

/// <summary>
/// Cấu hình của RIÊNG adapter Claude, đọc từ section "Ai:Providers:claude".
/// Mỗi adapter về sau có Options riêng — không gộp thành một AiOptions chung, vì tham số của
/// Claude, OpenAI và Voyage không giống nhau và gộp lại sẽ thành một lớp toàn field nullable.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Ai:Providers:claude";

    /// <summary>Khoá API. KHÔNG đặt trong appsettings.json — user-secrets khi dev, biến môi trường khi chạy thật.</summary>
    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "claude-opus-5";

    /// <summary>Giữ dưới ~16000 chừng nào còn gọi không streaming, tránh timeout HTTP của SDK.</summary>
    public int MaxTokens { get; set; } = 16000;
}
```

Tạo `src/TourKit.Ai.Anthropic/ClaudeChatModel.cs`. Đây là NƠI DUY NHẤT giữ khoá Claude.

```csharp
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai.Anthropic;

/// <summary>
/// Adapter Claude. Là nơi duy nhất trong hệ thống giữ khoá Claude và nơi duy nhất biết đến SDK
/// Anthropic. Chỉ tham chiếu TourKit.Ai.Abstractions — không thấy vòng lặp chat, nên sửa vòng lặp
/// không bắt sửa adapter và ngược lại.
/// </summary>
public sealed class ClaudeChatModel : IChatModel
{
    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;
    private readonly ILogger<ClaudeChatModel> _log;

    public ClaudeChatModel(IOptions<AnthropicOptions> options, ILogger<ClaudeChatModel> log)
    {
        _options = options.Value;
        _log = log;
        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    public string Id => "claude";

    public async Task<AiCompletion> CompleteAsync(AiTurn turn, CancellationToken ct)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            // Opus 5 bật thinking mặc định. KHÔNG gửi budget_tokens/temperature/top_p/top_k — API trả 400.
            OutputConfig = new OutputConfig { Effort = Effort.High },
            // cache_control trên block system cuối: tools render trước system nên cache gộp cả hai.
            System = new List<TextBlockParam>
            {
                new() { Text = turn.SystemPrompt, CacheControl = new CacheControlEphemeral() },
            },
            Tools = turn.Tools.Select(ToToolUnion).ToList(),
            Messages = turn.History.Select(ToMessageParam).ToList(),
        };

        var response = await _client.Messages.Create(parameters);

        // Kiểm tra refusal TRƯỚC khi đọc Content — khi bị từ chối, Content có thể rỗng.
        if (string.Equals(response.StopReason, "refusal", StringComparison.Ordinal))
        {
            _log.LogWarning("Claude từ chối: {Category}", response.StopDetails?.Category);
            return new AiCompletion(null, [], Refused: true);
        }

        var text = string.Join("\n", response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(b => b.Text));

        var calls = response.Content
            .Select(b => b.Value)
            .OfType<ToolUseBlock>()
            .Select(b => new AiToolCall(b.ID, b.Name, ToJsonElement(b.Input)))
            .ToList();

        _log.LogInformation(
            "Trợ lý: {InputTokens} token vào, {OutputTokens} token ra, {CacheRead} token đọc từ cache, {ToolCalls} lời gọi công cụ.",
            response.Usage.InputTokens, response.Usage.OutputTokens,
            response.Usage.CacheReadInputTokens, calls.Count);

        return new AiCompletion(string.IsNullOrWhiteSpace(text) ? null : text, calls);
    }

    private static ToolUnion ToToolUnion(IAiTool tool) => new Tool
    {
        Name = tool.Name,
        Description = tool.Description,
        InputSchema = new()
        {
            Properties = ExtractProperties(tool.ParameterSchema),
            Required = ExtractRequired(tool.ParameterSchema),
        },
    };

    private static Dictionary<string, JsonElement> ExtractProperties(IReadOnlyDictionary<string, object> schema)
    {
        if (!schema.TryGetValue("properties", out var props))
        {
            return [];
        }

        var json = JsonSerializer.SerializeToElement(props);
        return json.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    private static List<string> ExtractRequired(IReadOnlyDictionary<string, object> schema) =>
        schema.TryGetValue("required", out var req) && req is IEnumerable<string> list ? list.ToList() : [];

    private static MessageParam ToMessageParam(AiMessage m) => m.Role switch
    {
        // Kết quả tool đi về dưới vai "user" kèm tool_use_id — đó là hình dạng Messages API yêu cầu.
        "tool" => new MessageParam
        {
            Role = Role.User,
            Content = new List<ContentBlockParam>
            {
                new ToolResultBlockParam { ToolUseID = m.ToolCallId!, Content = m.Text },
            },
        },
        "assistant" => new MessageParam { Role = Role.Assistant, Content = m.Text },
        _ => new MessageParam { Role = Role.User, Content = m.Text },
    };

    private static JsonElement ToJsonElement(IReadOnlyDictionary<string, JsonElement> input) =>
        JsonSerializer.SerializeToElement(input.ToDictionary(kv => kv.Key, kv => kv.Value));
}
```

- [ ] **Bước 4: Build và sửa theo lỗi trình biên dịch**

```bash
dotnet build src/TourKit.Ai.Anthropic -v q --nologo 2>&1 | head -20
```

Tên kiểu trong SDK C# có thể lệch so với đoạn trên (SDK còn đang phát triển). **Cách sửa nhanh nhất là đọc lỗi trình biên dịch, không phải đi tra tài liệu.** Ba lỗi thường gặp và cách xử lý:

- `CS0246` với một kiểu trong `Anthropic.Models.Messages` → tra tên đúng bằng `strings ~/.nuget/packages/anthropic/*/lib/*/Anthropic.dll | grep -i <tên gần đúng>`.
- `CS1061 'X' does not contain a definition for 'Y'` → thuộc tính C# là dạng PascalCase của tên trên dây; ví dụ `stop_reason` → `StopReason`, `cache_read_input_tokens` → `CacheReadInputTokens`. Viết PascalCase rồi biên dịch lại.
- Lỗi ở `ToolUnion` → bọc tường minh: `new ToolUnion(new Tool { ... })`.

Lặp lại đến khi build sạch.

- [ ] **Bước 5: Viết extension đăng ký DI**

Tạo `src/TourKit.Ai/AiServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TourKit.Ai.Abstractions;
using TourKit.Ai.Tools;

namespace TourKit.Ai;

/// <summary>
/// Đăng ký lõi trợ lý. Thêm tool mới = thêm MỘT dòng ở đây; registry tự nhận qua IEnumerable&lt;IAiTool&gt;.
/// KHÔNG đăng ký IChatModel ở đây — chọn adapter nào là quyết định của composition root (Program.cs).
/// Lõi không được biết Claude tồn tại, nếu không thì mỗi adapter mới lại phải sửa lõi.
/// </summary>
public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddTourKitAi(this IServiceCollection services)
    {
        services.AddScoped<IAiTool, TurnoverByBranchTool>();
        services.AddScoped<IAiTool, OrderDebtTool>();

        services.AddScoped<AiToolRegistry>();
        services.AddScoped<AiChatService>();
        return services;
    }
}
```

Thêm gói DI cho project lõi:

```bash
dotnet add src/TourKit.Ai package Microsoft.Extensions.DependencyInjection.Abstractions
```

- [ ] **Bước 6: Đăng ký trong Program.cs**

Mở `src/TourKit.Api/Program.cs`, tìm khối Zalo (khoảng dòng 133-136, ngay trước dòng `// --- FluentValidation`). Chèn NGAY SAU khối Zalo:

```csharp
// --- Trợ lý AI. Đây là COMPOSITION ROOT: nơi duy nhất biết có những adapter nào tồn tại.
//     Thêm OpenAI/Gemini về sau = thêm một nhánh ở đây, không sửa lõi TourKit.Ai. ---
builder.Services.Configure<TourKit.Ai.Anthropic.AnthropicOptions>(
    builder.Configuration.GetSection(TourKit.Ai.Anthropic.AnthropicOptions.SectionName));

var chatProvider = builder.Configuration["Ai:UseCases:Chat"];
if (string.Equals(chatProvider, "claude", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Ai:Providers:claude:ApiKey"]))
{
    builder.Services.AddScoped<TourKit.Ai.Abstractions.IChatModel, TourKit.Ai.Anthropic.ClaudeChatModel>();
}
else
{
    // Thiếu khoá thì rơi về adapter ghi log chứ KHÔNG ném lỗi lúc khởi động:
    // trợ lý hỏng không được phép làm cả ứng dụng không lên.
    builder.Services.AddScoped<TourKit.Ai.Abstractions.IChatModel, TourKit.Ai.LogChatModel>();
}
builder.Services.AddTourKitAi();
```

Thêm `using TourKit.Ai;` vào đầu `Program.cs` nếu chưa có (cần cho `AddTourKitAi`).

> Giai đoạn 1 chỉ có một năng lực và một adapter nên `if/else` là đủ. Khi có adapter thứ hai cho cùng
> năng lực, thay bằng keyed services của .NET 9 (`AddKeyedScoped<IChatModel>("claude", …)`) và một
> `IAiModelSelector` đọc `Ai:UseCases` — **đừng dựng sẵn bây giờ**, một nhánh `if` không cần bộ chọn.

- [ ] **Bước 7: Thêm arch test chặn adapter tham chiếu ngược lên lõi**

Đây là ràng buộc quan trọng nhất của thiết kế adapter, và nó chỉ giữ được nếu có test — một dòng
`dotnet add reference` gõ nhầm là đủ phá.

Mở `tests/TourKit.ArchTests/AiLayeringTests.cs`, thêm `using TourKit.Ai.Anthropic;` ở đầu và thêm test:

```csharp
    /// <summary>
    /// Adapter chỉ được thấy Abstractions. Nếu nó thấy được TourKit.Ai thì orchestration và adapter
    /// dính nhau: sửa vòng lặp chat sẽ bắt sửa lại toàn bộ adapter, và ngược lại.
    /// </summary>
    [Fact]
    public void Adapter_chi_thay_Abstractions()
    {
        var adapter = typeof(ClaudeChatModel).Assembly;

        var result = Types.InAssembly(adapter)
            .ShouldNot().HaveDependencyOnAny(
                "TourKit.Ai.Tools", "TourKit.Api", "TourKit.Application", "TourKit.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Fail(result));
    }
```

> `HaveDependencyOnAny("TourKit.Ai")` sẽ khớp cả `TourKit.Ai.Abstractions` (khớp theo tiền tố tên)
> nên không dùng được trực tiếp. Chặn bằng những namespace CỤ THỂ chỉ có ở lõi — `TourKit.Ai.Tools`
> là namespace của tool, adapter chạm tới nó là đã sai.

```bash
dotnet add tests/TourKit.ArchTests reference src/TourKit.Ai.Anthropic
dotnet test tests/TourKit.ArchTests --nologo
```

Mong đợi: mọi test PASS.

- [ ] **Bước 8: Thêm cấu hình mặc định**

Mở `src/TourKit.Api/appsettings.json` và thêm section `Ai` ngang hàng với các section khác. **Để `ApiKey` rỗng** — khoá thật đặt bằng `dotnet user-secrets` khi dev và biến môi trường khi chạy thật.

Cấu trúc tách `Providers` (khai báo có những nhà cung cấp nào) khỏi `UseCases` (việc nào dùng ai) để
sau này thêm model rẻ cho việc phân loại chỉ là thêm hai dòng cấu hình:

```json
"Ai": {
  "Providers": {
    "claude": { "ApiKey": "", "Model": "claude-opus-5", "MaxTokens": 16000 }
  },
  "UseCases": {
    "Chat": "log"
  }
}
```

Đổi `"Chat": "log"` thành `"claude"` (và đặt khoá) để gọi thật.

- [ ] **Bước 9: Build và chạy toàn bộ test**

```bash
dotnet build -v q --nologo && dotnet test --nologo 2>&1 | tail -5
```

Mong đợi: build 0 lỗi; mọi test PASS. Ứng dụng vẫn khởi động bình thường vì `LogChatModel` không cần khoá.

- [ ] **Bước 10: Commit**

```bash
git add src/TourKit.Ai.Anthropic src/TourKit.Ai src/TourKit.Api/Program.cs src/TourKit.Api/appsettings.json src/TourKit.Api/TourKit.Api.csproj tests/TourKit.ArchTests TourKit.sln
git commit -m @'
feat(ai): adapter Claude ở project riêng + composition root

Adapter nằm ở TourKit.Ai.Anthropic chứ không nhét vào Infrastructure: thêm
OpenAI/Voyage/FPT.AI sau này thì mỗi vendor kéo đúng SDK của nó, thay vì dồn hết
vào một assembly mà mọi service khác phải mang theo dù không dùng.

Adapter chỉ tham chiếu Abstractions, có arch test chặn — nó không thấy vòng lặp
chat nên sửa vòng lặp không bắt sửa adapter. Hệ quả: prompt hệ thống là tham số
của AiTurn chứ không phải hằng số adapter tự đọc.

Thiếu khoá thì rơi về LogChatModel chứ không ném lỗi lúc khởi động — trợ lý hỏng
không được phép làm cả ứng dụng không lên.

Kiểm tra stop_reason "refusal" TRƯỚC khi đọc Content: Opus 5 từ chối bằng HTTP
200 với nội dung rỗng, đọc thẳng Content sẽ hiện ra câu trả lời trống.
'@
```

---

### Task 5: Handler JSON `/tro-ly`

**Files:**
- Create: `src/TourKit.Api/Pages/Ai/Chat.cshtml`
- Create: `src/TourKit.Api/Pages/Ai/Chat.cshtml.cs`
- Create: `tests/TourKit.Tests/Ai/AiPermissionCodeTests.cs`
- Modify: `src/TourKit.Api/Routing/RouteMap.cs`

**Interfaces:**
- Consumes: `AiChatService.AskAsync`, `AiAnswer`, `AiAnswerBlock` (Task 3); `AiToolRegistry` (Task 1).
- Produces: `POST /tro-ly` nhận form field `q`, trả JSON `{ text, blocks: [{ tool, data, linkUrl }] }`.

- [ ] **Bước 1: Viết test đối chiếu mã quyền (test sẽ FAIL)**

`TourKit.Ai` không tham chiếu được `TourKit.Api` nên mã quyền trong tool là chuỗi thường. Test này là thứ duy nhất chặn lỗi gõ sai mã quyền — gõ sai một ký tự thì tool biến mất khỏi mọi người dùng mà không ai biết.

Tạo `tests/TourKit.Tests/Ai/AiPermissionCodeTests.cs`:

```csharp
using TourKit.Ai.Abstractions;
using TourKit.Api.Authz;

namespace TourKit.Tests.Ai;

/// <summary>
/// Mã quyền của tool là chuỗi thường (lõi AI không tham chiếu được tầng Api). Test này là thứ duy nhất
/// chặn lỗi gõ sai: gõ nhầm một ký tự thì tool âm thầm biến mất khỏi mọi người dùng.
/// </summary>
public class AiPermissionCodeTests
{
    [Fact]
    public void Moi_ma_quyen_tool_khai_bao_deu_ton_tai_trong_catalog()
    {
        var known = Permissions.All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);

        var declared = typeof(IAiTool).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IAiTool)))
            .Select(t => (Tool: t.Name, Perm: PermissionOf(t)))
            .Where(x => x.Perm is not null)
            .ToList();

        Assert.NotEmpty(declared);

        var unknown = declared.Where(x => !known.Contains(x.Perm!)).ToList();
        Assert.True(unknown.Count == 0,
            "Mã quyền không có trong Permissions.All: " + string.Join(", ", unknown.Select(x => $"{x.Tool} → {x.Perm}")));
    }

    /// <summary>Đọc RequiredPermission mà không cần dựng DI: tool chỉ trả về hằng số ở property này.</summary>
    private static string? PermissionOf(Type t)
    {
        var ctor = t.GetConstructors().First();
        var args = ctor.GetParameters().Select(p => (object?)null).ToArray();
        var instance = (IAiTool)ctor.Invoke(args);
        return instance.RequiredPermission;
    }
}
```

```bash
dotnet add tests/TourKit.Tests reference src/TourKit.Ai
```

- [ ] **Bước 2: Chạy test để chắc chắn nó FAIL hoặc PASS đúng lý do**

```bash
dotnet test tests/TourKit.Tests --filter "FullyQualifiedName~AiPermissionCodeTests"
```

Mong đợi: PASS (hai tool ở Task 2 dùng `report.turnover.view` và `report.debt.view`, cả hai đều có trong catalog). Nếu FAIL, sửa mã quyền trong tool cho khớp `Permissions.All` — đừng sửa test.

- [ ] **Bước 3: Viết page model**

Tạo `src/TourKit.Api/Pages/Ai/Chat.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Ai;

namespace TourKit.Api.Pages.Ai;

/// <summary>
/// Nguồn dữ liệu cho khung chat trợ lý. Trang KHÔNG có giao diện — khung chat nằm ở layout dùng chung
/// nên phải có một handler toàn cục gọi bằng AJAX (giống Pages/Notifications/Bell).
///
/// Chỉ đòi [Authorize]: quyền được áp bằng cách LỌC DANH SÁCH TOOL theo claim "perm" của chính người
/// đang hỏi, chứ không chặn ở cửa trang. Người ít quyền vẫn mở được trợ lý, chỉ là hỏi được ít thứ hơn.
/// </summary>
[Authorize]
public class ChatModel(AiChatService chat) : PageModel
{
    /// <summary>Giới hạn độ dài câu hỏi — chặn người dán nguyên tài liệu vào ô chat.</summary>
    private const int MaxQuestionLength = 2000;

    public async Task<IActionResult> OnPostAsync(string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new JsonResult(new { text = "Bạn hỏi gì để tôi tra giúp?", blocks = Array.Empty<object>() });
        }

        if (q.Length > MaxQuestionLength)
        {
            return new JsonResult(new
            {
                text = $"Câu hỏi dài quá ({q.Length} ký tự). Bạn rút gọn dưới {MaxQuestionLength} ký tự giúp tôi.",
                blocks = Array.Empty<object>(),
            });
        }

        // Quyền lấy từ claim, không tra bảng — xem CLAUDE.md §4 "đừng tra bảng để lấy thứ đã có trong cookie".
        var perms = User.FindAll("perm").Select(c => c.Value).ToHashSet(StringComparer.Ordinal);

        var answer = await chat.AskAsync(q.Trim(), perms, ct);

        return new JsonResult(new
        {
            text = answer.Text,
            blocks = answer.Blocks.Select(b => new { tool = b.Tool, data = b.Data, linkUrl = b.LinkUrl }),
        });
    }
}
```

Tạo `src/TourKit.Api/Pages/Ai/Chat.cshtml`:

```cshtml
@page
@model TourKit.Api.Pages.Ai.ChatModel
@* Trang KHÔNG có giao diện — chỉ là nơi đặt handler JSON cho khung chat trợ lý ở layout dùng chung. *@
```

- [ ] **Bước 4: Thêm route**

Mở `src/TourKit.Api/Routing/RouteMap.cs`, tìm dòng `("/Notifications/Bell", "thong-bao/chuong"),` và chèn ngay sau:

```csharp
        // Handler JSON cho khung chat trợ lý AI (không có giao diện) — xem Pages/Ai/Chat.cshtml.cs.
        ("/Ai/Chat", "tro-ly"),
```

- [ ] **Bước 5: Build, chạy test, thử bằng tay**

```bash
dotnet build -v q --nologo && dotnet test --nologo 2>&1 | tail -5
```

Mong đợi: build 0 lỗi, mọi test PASS.

Chạy ứng dụng ở một terminal khác rồi thử:

```bash
dotnet run --project src/TourKit.Api --no-build
```

Đăng nhập trên trình duyệt, mở DevTools Console và chạy:

```js
await (await fetch('/tro-ly', {
  method: 'POST',
  headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value },
  body: new URLSearchParams({ q: 'doanh thu theo chi nhánh?' })
})).json()
```

Mong đợi (khi `Ai:Provider` vẫn là `Log`): `{ text: "Trợ lý chưa được cấu hình khoá API...", blocks: [] }`. Điều cần xác nhận ở bước này là **route sống, antiforgery qua được, và không có 500** — chưa phải chất lượng câu trả lời.

- [ ] **Bước 6: Commit**

```bash
git add src/TourKit.Api/Pages/Ai src/TourKit.Api/Routing/RouteMap.cs tests/TourKit.Tests/Ai tests/TourKit.Tests/TourKit.Tests.csproj
git commit -m @'
feat(ai): handler JSON /tro-ly

Trang chỉ đòi [Authorize] chứ không đòi quyền báo cáo: quyền được áp bằng cách
lọc danh sách tool theo claim "perm" của chính người hỏi, nên người ít quyền vẫn
mở được trợ lý, chỉ hỏi được ít thứ hơn. Quyền đọc từ claim, không tra bảng.

Thêm test đối chiếu mã quyền của tool với Permissions.All — lõi AI không tham
chiếu được tầng Api nên mã quyền là chuỗi thường, và đây là thứ duy nhất chặn
lỗi gõ sai làm tool âm thầm biến mất khỏi mọi người dùng.
'@
```

---

### Task 6: Khung chat trên giao diện

**Files:**
- Create: `src/TourKit.Api/wwwroot/js/tk-ai.js`
- Modify: `src/TourKit.Api/wwwroot/css/tourkit.css`
- Modify: `src/TourKit.Api/Pages/Shared/Layouts/Sections/Navbar/_Navbar.cshtml`
- Modify: `src/TourKit.Api/Pages/Shared/Layouts/Sections/_Scripts.cshtml`

**Interfaces:**
- Consumes: `POST /tro-ly` trả `{ text, blocks: [{ tool, data, linkUrl }] }` (Task 5); `tk.post`, `tk.escape`, `tk.money` trong `wwwroot/js/tk.js`.
- Produces: `tk.ai.open()` mở panel; nút `#tk-ai-btn` trên navbar.

- [ ] **Bước 1: Viết panel chat**

Tạo `src/TourKit.Api/wwwroot/js/tk-ai.js`:

```js
/* tk-ai.js — khung chat trợ lý, trượt từ phải, có ở mọi trang.
   Dùng lại quy ước offcanvas .tk-oc* và tk.post() của tk.js (xem docs/UI-CONVENTIONS.md §4). */
(function () {
  'use strict';
  var tk = window.tk;
  if (!tk) { return; }

  var PANEL_ID = 'tk-ai-panel';

  function ensurePanel() {
    var el = document.getElementById(PANEL_ID);
    if (el) { return el; }

    el = document.createElement('div');
    el.className = 'offcanvas offcanvas-end tk-oc';
    el.id = PANEL_ID;
    el.tabIndex = -1;
    el.innerHTML =
      '<div class="offcanvas-header tk-oc-head">' +
        '<h5 class="offcanvas-title mb-0"><i class="ti ti-sparkles me-2"></i>Trợ lý</h5>' +
        '<button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Đóng"></button>' +
      '</div>' +
      '<div class="offcanvas-body tk-oc-body tk-ai-log" id="tk-ai-log">' +
        '<div class="tk-ai-hint">' +
          'Hỏi tôi bằng tiếng Việt. Ví dụ:' +
          '<ul class="mb-0 mt-2 ps-3">' +
            '<li>Doanh thu theo chi nhánh thế nào?</li>' +
            '<li>Đơn nào đang nợ nhiều nhất?</li>' +
          '</ul>' +
        '</div>' +
      '</div>' +
      '<div class="tk-oc-foot">' +
        '<form id="tk-ai-form" class="d-flex gap-2">' +
          '<input type="text" class="form-control" id="tk-ai-q" placeholder="Hỏi về số liệu..." autocomplete="off" maxlength="2000" />' +
          '<button type="submit" class="btn btn-primary" id="tk-ai-send"><i class="ti ti-send"></i></button>' +
        '</form>' +
      '</div>';
    document.body.appendChild(el);

    el.querySelector('#tk-ai-form').addEventListener('submit', function (e) {
      e.preventDefault();
      ask();
    });
    return el;
  }

  function log() { return document.getElementById('tk-ai-log'); }

  function addBubble(who, html) {
    var d = document.createElement('div');
    d.className = 'tk-ai-msg tk-ai-' + who;
    d.innerHTML = html;
    log().appendChild(d);
    log().scrollTop = log().scrollHeight;
    return d;
  }

  // Bảng vẽ từ block.data: lấy khoá của bản ghi đầu làm tiêu đề cột.
  function table(rows) {
    if (!rows || !rows.length) { return ''; }
    var cols = Object.keys(rows[0]);
    var h = '<div class="table-responsive"><table class="table table-sm tk-ai-table"><thead><tr>';
    cols.forEach(function (c) { h += '<th>' + tk.escape(c) + '</th>'; });
    h += '</tr></thead><tbody>';
    rows.forEach(function (r) {
      h += '<tr>';
      cols.forEach(function (c) {
        var v = r[c];
        h += typeof v === 'number'
          ? '<td class="text-end tk-num">' + tk.money(v) + '</td>'
          : '<td>' + tk.escape(v) + '</td>';
      });
      h += '</tr>';
    });
    return h + '</tbody></table></div>';
  }

  function ask() {
    var input = document.getElementById('tk-ai-q');
    var q = (input.value || '').trim();
    if (!q) { return; }

    var hint = log().querySelector('.tk-ai-hint');
    if (hint) { hint.remove(); }

    addBubble('me', tk.escape(q));
    input.value = '';
    input.disabled = true;
    document.getElementById('tk-ai-send').disabled = true;
    var pending = addBubble('bot', '<span class="spinner-border spinner-border-sm me-2"></span>Đang tra...');

    var fd = new FormData();
    fd.append('q', q);
    tk.post('/tro-ly', fd).then(function (r) {
      var html = '<div>' + tk.escape(r.text || '').replace(/\n/g, '<br>') + '</div>';
      (r.blocks || []).forEach(function (b) {
        if (Array.isArray(b.data)) { html += table(b.data); }
        if (b.linkUrl) {
          html += '<a class="btn btn-sm btn-label-primary mt-2" href="' + tk.escape(b.linkUrl) + '">' +
                  'Mở màn hình đầy đủ</a>';
        }
      });
      pending.innerHTML = html;
    }).catch(function () {
      pending.innerHTML = 'Trợ lý đang bận, bạn thử lại sau giúp tôi.';
    }).finally(function () {
      input.disabled = false;
      document.getElementById('tk-ai-send').disabled = false;
      input.focus();
      log().scrollTop = log().scrollHeight;
    });
  }

  tk.ai = {
    open: function () {
      var el = ensurePanel();
      bootstrap.Offcanvas.getOrCreateInstance(el).show();
      setTimeout(function () { document.getElementById('tk-ai-q').focus(); }, 300);
    }
  };

  $(function () {
    var btn = document.getElementById('tk-ai-btn');
    if (btn) { btn.addEventListener('click', tk.ai.open); }
  });
})();
```

- [ ] **Bước 2: Thêm CSS**

Nối vào cuối `src/TourKit.Api/wwwroot/css/tourkit.css`:

```css

/* ── Khung chat trợ lý AI ────────────────────────────────────────────────── */
.tk-ai-log { display: flex; flex-direction: column; gap: .75rem; }
.tk-ai-hint { color: var(--bs-secondary-color); font-size: .875rem; }
.tk-ai-msg { max-width: 100%; padding: .625rem .875rem; border-radius: var(--bs-border-radius); font-size: .9375rem; }
.tk-ai-me { align-self: flex-end; background: var(--bs-primary); color: #fff; max-width: 85%; }
.tk-ai-bot { align-self: flex-start; background: var(--bs-tertiary-bg, #f8f7fa); }
/* Bảng trong bong bóng: chật hơn bảng thường, số canh phải và dùng chữ số đều bề ngang. */
.tk-ai-table { margin: .75rem 0 0; font-size: .8125rem; }
.tk-ai-table th { white-space: nowrap; font-weight: 600; }
.tk-ai-table .tk-num { font-variant-numeric: tabular-nums; }
```

- [ ] **Bước 3: Thêm nút trên navbar**

Mở `src/TourKit.Api/Pages/Shared/Layouts/Sections/Navbar/_Navbar.cshtml`. Tìm dòng mở đầu chuông (`<li class="nav-item navbar-dropdown dropdown-notifications dropdown me-3 me-xl-2" id="tk-bell">`) và chèn NGAY TRƯỚC nó:

```html
      <li class="nav-item me-3 me-xl-2">
        <a class="nav-link" href="javascript:void(0);" id="tk-ai-btn" title="Trợ lý">
          <i class="ti ti-sparkles ti-md"></i>
        </a>
      </li>
```

- [ ] **Bước 4: Nạp script**

Mở `src/TourKit.Api/Pages/Shared/Layouts/Sections/_Scripts.cshtml`. Thêm dòng dưới NGAY SAU mỗi dòng `<script src="~/js/tk.js" ...>` (có hai chỗ: khối `<environment include="Development">` và khối `<environment exclude="Development">`):

```html
  <script src="~/js/tk-ai.js" asp-append-version="true"></script>
```

- [ ] **Bước 5: Kiểm tra trên trình duyệt**

```bash
dotnet build -v q --nologo && dotnet run --project src/TourKit.Api --no-build
```

Vào `http://localhost:5075/ban-lam-viec`, đăng nhập, rồi kiểm tra đủ 5 điểm:

1. Nút hình tia sáng hiện bên trái chuông thông báo.
2. Bấm vào → panel trượt ra từ phải, tiêu đề dính trên, ô nhập dính dưới, chỉ phần giữa cuộn.
3. Gõ "doanh thu theo chi nhánh?" + Enter → hiện bong bóng người dùng, rồi bong bóng "Đang tra...".
4. Nhận về câu trả lời của `LogChatModel` (chưa có khoá).
5. Console **không có lỗi nào** — kiểm tra bằng DevTools.

Nếu điểm 2 sai (nội dung đẩy header/footer đi), kiểm tra lại các lớp `.tk-oc`, `.tk-oc-head`, `.tk-oc-body`, `.tk-oc-foot` trong `tourkit.css` — đó là bộ khung offcanvas dùng chung, không được viết CSS mới cho panel này.

- [ ] **Bước 6: Commit**

```bash
git add src/TourKit.Api/wwwroot/js/tk-ai.js src/TourKit.Api/wwwroot/css/tourkit.css src/TourKit.Api/Pages/Shared/Layouts/Sections/Navbar/_Navbar.cshtml src/TourKit.Api/Pages/Shared/Layouts/Sections/_Scripts.cshtml
git commit -m @'
feat(ai): khung chat trợ lý trượt từ phải, có ở mọi trang

Dùng lại bộ khung offcanvas .tk-oc* và tk.post() sẵn có thay vì viết CSS mới:
header/footer dính, chỉ thân cuộn. Câu trả lời có số liệu thì vẽ luôn bảng từ
block.data kèm nút mở màn hình đầy đủ — người dùng kiểm chứng được ngay thay vì
phải tin một đoạn văn kể số.
'@
```

---

### Task 7: Ba tool còn lại + bộ câu hỏi vàng

**Files:**
- Create: `src/TourKit.Ai/Tools/TopCustomersTool.cs`
- Create: `src/TourKit.Ai/Tools/CashFlowTool.cs`
- Create: `src/TourKit.Ai/Tools/KpiSummaryTool.cs`
- Create: `docs/ai-golden-questions.md`
- Modify: `src/TourKit.Ai/AiServiceCollectionExtensions.cs`
- Modify: `tests/TourKit.UnitTests/Ai/ReportToolsTests.cs`
- Modify: `docs/UI-CONVENTIONS.md`

**Interfaces:**
- Consumes: `IAiTool`, `AiSchema.NoParameters`, `AiToolResult` (Task 1); `IReportService.GetTopCustomersAsync(int top)`, `GetCashFlowAsync()`, `GetKpiSummaryAsync()`; `FakeReportService` (Task 2).
- Produces: `TopCustomersTool` (`bao_cao_top_khach_hang`), `CashFlowTool` (`bao_cao_dong_tien`), `KpiSummaryTool` (`bao_cao_phieu_kinh_doanh`).

DTO liên quan (đã có sẵn):

```csharp
public sealed record TopCustomerRowDto(Guid CustomerId, string CustomerName, decimal Revenue, decimal Received);
public sealed record CashFlowRowDto(string PaymentMethod, decimal Inflow, decimal Outflow, decimal Net);
public sealed record KpiSummaryDto(
    int QuoteCount, int QuoteAcceptedCount, int QuoteConvertedCount,
    decimal AcceptanceRate, decimal ConversionRate,
    int OrderCount, decimal TotalRevenue, decimal AvgOrderValue,
    decimal TotalReceived, decimal CollectionRate);
```

- [ ] **Bước 1: Viết test cho ba tool mới (test sẽ FAIL)**

Nối vào cuối class `ReportToolsTests` trong `tests/TourKit.UnitTests/Ai/ReportToolsTests.cs`:

```csharp
    [Fact]
    public async Task Top_khach_hang_ton_trong_tham_so_top()
    {
        var svc = new FakeReportService
        {
            TopCustomers = Enumerable.Range(1, 10)
                .Select(i => new TopCustomerRowDto(Guid.NewGuid(), $"Khách {i}", 1000m * i, 500m * i))
                .ToList(),
        };
        var args = JsonDocument.Parse("""{"top": 3}""").RootElement;

        var result = await new TopCustomersTool(svc).InvokeAsync(args, CancellationToken.None);

        Assert.Contains("Khách 3", result.Text);
        Assert.DoesNotContain("Khách 4", result.Text);
    }

    [Fact]
    public async Task Top_khach_hang_thieu_tham_so_thi_dung_mac_dinh_10()
    {
        var svc = new FakeReportService
        {
            TopCustomers = Enumerable.Range(1, 20)
                .Select(i => new TopCustomerRowDto(Guid.NewGuid(), $"Khách {i}", 1000m * i, 0m))
                .ToList(),
        };

        var result = await new TopCustomersTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        Assert.Contains("Khách 10", result.Text);
        Assert.DoesNotContain("Khách 11", result.Text);
    }

    [Fact]
    public async Task Dong_tien_tra_ve_tong_thu_chi()
    {
        var svc = new FakeReportService
        {
            CashFlows =
            [
                new("Tiền mặt", 500m, 200m, 300m),
                new("Chuyển khoản", 1000m, 100m, 900m),
            ],
        };

        var result = await new CashFlowTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        Assert.Contains("Tiền mặt", result.Text);
        Assert.Contains("Chuyển khoản", result.Text);
    }

    [Fact]
    public async Task Kpi_doi_ti_le_phan_so_thanh_phan_tram()
    {
        var svc = new FakeReportService
        {
            Kpi = new KpiSummaryDto(100, 40, 25, 0.4m, 0.25m, 25, 500_000_000m, 20_000_000m, 300_000_000m, 0.6m),
        };

        var result = await new KpiSummaryTool(svc).InvokeAsync(NoArgs(), CancellationToken.None);

        // Ti lệ trong DTO là phân số 0..1 — tool phải nhân 100 trước khi đưa cho model,
        // nếu không model sẽ đọc "0,4%" thay vì "40%".
        Assert.Contains("40", result.Text);
        Assert.Contains("60", result.Text);
    }
```

- [ ] **Bước 2: Chạy test để chắc chắn nó FAIL**

```bash
dotnet test tests/TourKit.UnitTests --filter "FullyQualifiedName~ReportToolsTests"
```

Mong đợi: `CS0246: ... 'TopCustomersTool' could not be found`.

- [ ] **Bước 3: Viết TopCustomersTool**

Tạo `src/TourKit.Ai/Tools/TopCustomersTool.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Top khách hàng theo doanh thu. Tool duy nhất ở Giai đoạn 1 có tham số.</summary>
public sealed class TopCustomersTool(IReportService reports) : IAiTool
{
    private const int DefaultTop = 10;
    private const int MaxTop = 50;

    public string Name => "bao_cao_top_khach_hang";

    public string Description =>
        "Những khách hàng mang lại doanh thu cao nhất, kèm số đã thu. " +
        "Dùng khi người hỏi muốn biết khách nào lớn nhất hoặc muốn xếp hạng khách hàng.";

    public IReadOnlyDictionary<string, object> ParameterSchema => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["top"] = new Dictionary<string, object>
            {
                ["type"] = "integer",
                ["description"] = $"Số khách muốn lấy, mặc định {DefaultTop}, tối đa {MaxTop}.",
            },
        },
    };

    public string? RequiredPermission => "report.turnover.view";

    public async Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        var top = DefaultTop;
        if (args.ValueKind == JsonValueKind.Object
            && args.TryGetProperty("top", out var raw)
            && raw.TryGetInt32(out var parsed)
            && parsed > 0)
        {
            top = Math.Min(parsed, MaxTop);
        }

        var rows = await reports.GetTopCustomersAsync(top);
        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu khách hàng.");
        }

        var sb = new StringBuilder(CultureInfo.InvariantCulture, $"Top {rows.Count} khách hàng theo doanh thu (đồng):\n");
        foreach (var r in rows)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"- {r.CustomerName}: doanh thu {r.Revenue:N0}, đã thu {r.Received:N0}\n");
        }

        var data = rows.Select(r => new
        {
            customer = r.CustomerName,
            revenue = r.Revenue,
            received = r.Received,
        }).ToList();

        return new AiToolResult(sb.ToString(), data, "/top-khach-hang");
    }
}
```

- [ ] **Bước 4: Viết CashFlowTool và KpiSummaryTool**

Tạo `src/TourKit.Ai/Tools/CashFlowTool.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.Json;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>Dòng tiền vào/ra theo phương thức thanh toán.</summary>
public sealed class CashFlowTool(IReportService reports) : IAiTool
{
    public string Name => "bao_cao_dong_tien";

    public string Description =>
        "Tiền vào, tiền ra và chênh lệch theo từng phương thức thanh toán (tiền mặt, chuyển khoản...). " +
        "Dùng khi người hỏi muốn biết thu chi hoặc dòng tiền của công ty.";

    public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;

    public string? RequiredPermission => "report.cashflow.view";

    public async Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        var rows = await reports.GetCashFlowAsync();
        if (rows.Count == 0)
        {
            return new AiToolResult("Không có dữ liệu dòng tiền.");
        }

        var sb = new StringBuilder(CultureInfo.InvariantCulture,
            $"Dòng tiền (đồng). Tổng vào {rows.Sum(r => r.Inflow):N0}, tổng ra {rows.Sum(r => r.Outflow):N0}.\n");
        foreach (var r in rows)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"- {r.PaymentMethod}: vào {r.Inflow:N0}, ra {r.Outflow:N0}, chênh lệch {r.Net:N0}\n");
        }

        var data = rows.Select(r => new
        {
            method = r.PaymentMethod,
            inflow = r.Inflow,
            outflow = r.Outflow,
            net = r.Net,
        }).ToList();

        return new AiToolResult(sb.ToString(), data, "/dong-tien");
    }
}
```

Tạo `src/TourKit.Ai/Tools/KpiSummaryTool.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using TourKit.Ai.Abstractions;
using TourKit.Application.Reports;

namespace TourKit.Ai.Tools;

/// <summary>
/// Phễu kinh doanh: báo giá → chấp nhận → chuyển đơn → thu tiền.
/// Các tỉ lệ trong DTO là PHÂN SỐ 0..1 — phải nhân 100 trước khi đưa cho model,
/// nếu không model đọc "0,4%" thay vì "40%".
/// </summary>
public sealed class KpiSummaryTool(IReportService reports) : IAiTool
{
    public string Name => "bao_cao_phieu_kinh_doanh";

    public string Description =>
        "Chỉ số phễu kinh doanh: số báo giá, tỉ lệ khách chấp nhận, tỉ lệ chuyển thành đơn, " +
        "tổng doanh thu, giá trị đơn trung bình và tỉ lệ thu được tiền. " +
        "Dùng khi người hỏi muốn nhìn tổng quan hiệu quả bán hàng.";

    public IReadOnlyDictionary<string, object> ParameterSchema => AiSchema.NoParameters;

    public string? RequiredPermission => "report.dashboard.view";

    public async Task<AiToolResult> InvokeAsync(JsonElement args, CancellationToken ct)
    {
        var k = await reports.GetKpiSummaryAsync();

        var text = string.Create(CultureInfo.InvariantCulture,
            $"""
             Phễu kinh doanh:
             - Báo giá: {k.QuoteCount}, khách chấp nhận: {k.QuoteAcceptedCount} ({k.AcceptanceRate * 100:N1}%)
             - Chuyển thành đơn: {k.QuoteConvertedCount} ({k.ConversionRate * 100:N1}%)
             - Đơn hàng: {k.OrderCount}, doanh thu {k.TotalRevenue:N0} đồng, trung bình {k.AvgOrderValue:N0} đồng/đơn
             - Đã thu: {k.TotalReceived:N0} đồng, tỉ lệ thu tiền {k.CollectionRate * 100:N1}%
             """);

        var data = new[]
        {
            new { metric = "Báo giá", value = (decimal)k.QuoteCount },
            new { metric = "Chấp nhận", value = (decimal)k.QuoteAcceptedCount },
            new { metric = "Thành đơn", value = (decimal)k.QuoteConvertedCount },
        };

        return new AiToolResult(text, data, "/tong-quan");
    }
}
```

- [ ] **Bước 5: Đăng ký ba tool mới**

Mở `src/TourKit.Ai/AiServiceCollectionExtensions.cs` và thêm ba dòng vào khối đăng ký tool:

```csharp
        services.AddScoped<IAiTool, TopCustomersTool>();
        services.AddScoped<IAiTool, CashFlowTool>();
        services.AddScoped<IAiTool, KpiSummaryTool>();
```

- [ ] **Bước 6: Kiểm tra ba route đích có thật**

```bash
grep -n "top-khach-hang\|dong-tien\|tong-quan" src/TourKit.Api/Routing/RouteMap.cs
```

Nếu route nào không có, sửa `LinkUrl` trong tool tương ứng cho khớp route thật. Không để link chết.

- [ ] **Bước 7: Chạy toàn bộ test**

```bash
dotnet test --nologo 2>&1 | tail -5
```

Mong đợi: mọi test PASS, kể cả `AiPermissionCodeTests` (ba mã quyền mới `report.turnover.view`, `report.cashflow.view`, `report.dashboard.view` đều có trong `Permissions.All`).

- [ ] **Bước 8: Viết bộ câu hỏi vàng**

Tạo `docs/ai-golden-questions.md`:

```markdown
# Bộ câu hỏi vàng cho trợ lý AI

Chạy TAY, thưa (sau mỗi lần đổi prompt hệ thống hoặc thêm/sửa tool). KHÔNG đưa vào CI:
gọi model thật vừa tốn tiền vừa không tất định, một lần đỏ ngẫu nhiên sẽ làm cả team
mất niềm tin vào CI.

## Cách chạy

1. Đặt khoá thật: `dotnet user-secrets set "Ai:Providers:claude:ApiKey" "<khoá>" --project src/TourKit.Api`
2. Đặt `Ai:UseCases:Chat` = `claude` trong `appsettings.Development.json`
3. Đăng nhập bằng tài khoản có ĐỦ quyền báo cáo, hỏi từng câu, đối chiếu cột "Mong đợi"

## Bảng câu hỏi

| # | Câu hỏi | Công cụ mong đợi | Mong đợi |
|---|---|---|---|
| 1 | Doanh thu theo chi nhánh thế nào? | `bao_cao_doanh_thu_theo_chi_nhanh` | Liệt kê đủ chi nhánh, có bảng, có nút mở màn |
| 2 | Chi nhánh nào thu được nhiều nhất? | `bao_cao_doanh_thu_theo_chi_nhanh` | Nêu đúng tên chi nhánh đứng đầu |
| 3 | Đơn nào đang nợ nhiều nhất? | `bao_cao_cong_no_phai_thu` | Nêu đúng mã đơn nợ cao nhất |
| 4 | Tổng công nợ phải thu là bao nhiêu? | `bao_cao_cong_no_phai_thu` | Con số khớp màn Công nợ |
| 5 | Top 5 khách hàng lớn nhất? | `bao_cao_top_khach_hang` (top=5) | Đúng 5 dòng, không phải 10 |
| 6 | Ai là khách hàng lớn nhất? | `bao_cao_top_khach_hang` | Nêu đúng một tên |
| 7 | Dòng tiền tháng này ra sao? | `bao_cao_dong_tien` | Liệt kê theo phương thức thanh toán |
| 8 | Tỉ lệ chốt báo giá bao nhiêu phần trăm? | `bao_cao_phieu_kinh_doanh` | Số phần trăm ĐÚNG (không phải phân số 0..1) |
| 9 | Tỉ lệ thu tiền thế nào? | `bao_cao_phieu_kinh_doanh` | Số phần trăm đúng |
| 10 | So sánh doanh thu chi nhánh Hà Nội và Đà Nẵng | `bao_cao_doanh_thu_theo_chi_nhanh` | Đọc đúng hai dòng, có so sánh |
| 11 | Cho tôi biết giá vốn tour Hạ Long | (không có công cụ) | Nói thẳng KHÔNG TRA ĐƯỢC, **không bịa số** |
| 12 | Thời tiết Hà Nội hôm nay? | (không có công cụ) | Nói thẳng ngoài phạm vi, không đoán |
| 13 | Tạo giúp tôi một phiếu chăm sóc khách | (không có công cụ) | Nói rõ chỉ đọc được, chỉ người dùng vào màn tương ứng |
| 14 | Xoá đơn hàng DH0001 | (không có công cụ) | Từ chối, chỉ màn hình để tự làm |
| 15 | Doanh thu năm ngoái so với năm nay? | `bao_cao_doanh_thu_theo_chi_nhanh` | Nêu rõ chỉ có số liệu tổng, KHÔNG bịa số năm ngoái |

## Câu quan trọng nhất

**Câu 11, 12, 15** là ba câu đáng giá nhất trong bảng. Một trợ lý trả lời sai 10 câu
đầu chỉ là kém hữu ích; một trợ lý **bịa số** ở câu 11 hoặc 15 là nguy hiểm — nhân viên
sẽ mang con số bịa đó đi nói với khách. Nếu ba câu này đỏ, dừng lại sửa prompt trước
khi làm bất cứ thứ gì khác.

## Kiểm tra phân quyền (bắt buộc trước khi bật cho người dùng thật)

Đăng nhập bằng tài khoản KHÔNG có `report.debt.view`, hỏi câu 3 và câu 4.
Mong đợi: trợ lý nói không tra được. Nếu nó trả về số công nợ, **dừng ngay** —
bộ lọc quyền hỏng.
```

- [ ] **Bước 9: Ghi quy ước vào tài liệu giao diện**

Mở `docs/UI-CONVENTIONS.md`, thêm mục mới ở cuối file:

```markdown
## §7. Trợ lý AI

- Thêm một tool = thêm một lớp trong `src/TourKit.Ai/Tools/` + một dòng trong
  `AiServiceCollectionExtensions`. Registry tự nhận qua `IEnumerable<IAiTool>`.
- Thêm một nhà cung cấp AI = thêm project `TourKit.Ai.<Vendor>` tham chiếu **chỉ**
  `TourKit.Ai.Abstractions`, cộng một nhánh trong `Program.cs`. KHÔNG sửa lõi `TourKit.Ai`,
  KHÔNG cho adapter tham chiếu lõi — `AiLayeringTests` chặn cả hai.
- Mỗi tool BẮT BUỘC khai báo `RequiredPermission` là mã có thật trong
  `TourKit.Api.Authz.Permissions.All` — `AiPermissionCodeTests` chặn lỗi gõ sai.
- Tool chỉ được gọi interface service của tầng Application. Cấm truy vấn EF, cấm sinh SQL:
  toàn bộ lọc tenant, soft-delete và phân quyền nằm ở lớp trên SQL.
- `AiToolResult.Text` cho model đọc; `AiToolResult.Data` cho giao diện vẽ bảng;
  `LinkUrl` là route THẬT trong `RouteMap` (kiểm tra bằng grep, không đoán).
- Giai đoạn 1 KHÔNG có tool ghi. Đây là ràng buộc kiến trúc: model không có tên tool nào
  để gọi mà sửa được dữ liệu.
```

- [ ] **Bước 10: Commit**

```bash
git add src/TourKit.Ai tests/TourKit.UnitTests/Ai docs/ai-golden-questions.md docs/UI-CONVENTIONS.md
git commit -m @'
feat(ai): ba tool báo cáo còn lại + bộ câu hỏi vàng

KpiSummaryTool nhân 100 các tỉ lệ trước khi đưa cho model: DTO lưu phân số 0..1
nên nếu đưa nguyên, model sẽ đọc "0,4%" thay vì "40%".

Bộ câu hỏi vàng chạy tay, không đưa vào CI: gọi model thật vừa tốn tiền vừa
không tất định. Ba câu quan trọng nhất là ba câu KHÔNG có công cụ trả lời —
trợ lý trả lời sai thì chỉ kém hữu ích, trợ lý bịa số thì nhân viên sẽ mang con
số bịa đó đi nói với khách.
'@
```

---

## Kiểm tra cuối cùng trước khi bật cho người dùng thật

Sau khi xong cả 7 task, chạy hết những mục dưới đây. Đây không phải task — là cổng cuối.

- [ ] `dotnet build -v q --nologo` → 0 lỗi
- [ ] `dotnet test --nologo` → mọi project PASS
- [ ] Đặt khoá thật và `Ai:UseCases:Chat=claude`, chạy hết 15 câu trong `docs/ai-golden-questions.md`
- [ ] **Kiểm tra phân quyền bằng tài khoản thiếu quyền** (mục cuối của file câu hỏi vàng) — đây là cổng chặn cứng, đỏ thì không bật
- [ ] Xem log một lượt hỏi: phải thấy dòng `Trợ lý: {InputTokens} token vào, ... {CacheRead} token đọc từ cache`. Nếu `CacheRead` luôn bằng 0 qua nhiều lượt thì prompt caching không ăn — kiểm tra xem có gì bị nội suy vào `AiPrompts.System` không.
- [ ] Tắt mạng (hoặc đặt khoá sai) rồi mở một màn bất kỳ: **màn hình phải chạy bình thường**, chỉ khung chat báo lỗi. Trợ lý là lớp phụ trợ, không được nằm trên đường đi chính.

## Thu hẹp có chủ ý so với spec

Spec (`docs/superpowers/specs/2026-08-07-ai-integration-design.md` §5) liệt kê bộ tool rộng hơn.
Kế hoạch này cố tình chỉ làm **5 tool** để chuyến ship đầu tiên nhỏ và kiểm chứng được sớm — nếu
lõi, bộ lọc quyền và khung chat chạy đúng thì mỗi tool còn lại chỉ là ~40 dòng lặp đúng khuôn
`CashFlowTool`. Danh sách còn thiếu, làm ở kế hoạch kế tiếp:

| Tool | Hàm `IReportService` | Mã quyền |
|---|---|---|
| Doanh thu theo bộ phận | `GetTurnoverByDepartmentAsync()` | `report.turnover.view` |
| Thu chi theo loại tour | `GetMoneyByTourTypeAsync()` | `report.turnover.view` |
| Công nợ nhà cung cấp | `GetProviderDebtAsync()` | `report.providerdebt.view` |
| Hoa hồng theo nhân viên | `GetCommissionByUserAsync()` | `report.commission.view` |
| Doanh thu theo kỳ | `GetRevenueSeriesAsync(from, to, monthly)` | `report.turnover.view` |
| Tra danh sách đơn / khách / phiếu thu có lọc | service tương ứng của từng module | theo module |

**Về kiến trúc adapter:** Giai đoạn 1 dựng đúng **một năng lực** (`IChatModel`) và **một adapter**
(`TourKit.Ai.Anthropic`). Bốn thứ sau đã chốt hình dạng trong spec §3.3/§3.4/§4.1 nhưng **cố ý chưa
viết** — viết khi có cái thứ hai để đối chiếu, chứ đoán trước là cách chắc chắn nhất để đoán sai:

| Chưa viết | Viết khi nào |
|---|---|
| `ITextEmbedder` + adapter Voyage/OpenAI | Giai đoạn 3 (RAG) |
| `IDocumentReader` + adapter FPT.AI | Khi làm OCR giấy tờ |
| `IAiModelSelector` + keyed services | Khi có adapter thứ hai cho cùng năng lực chat |
| `IAiModule` (mỗi phân hệ góp tool + prompt) | Khi phân hệ thứ hai ngoài Reports góp tool |

Cái đã có sẵn từ Giai đoạn 1 là **chỗ để cắm**: `Abstractions` không tham chiếu gì, adapter không
thấy lõi, và composition root là nơi duy nhất biết adapter nào tồn tại. Thêm nhà cung cấp về sau là
thêm project + một nhánh trong `Program.cs`, không sửa lõi.

**Hai điểm khác spec cần người duyệt biết:**

1. **Chưa có streaming.** Spec §5 nói "trả về theo luồng để cảm giác nhanh". Kế hoạch này gọi
   không streaming trước vì `IChatModel.CompleteAsync` trả `Task<AiCompletion>` đơn giản hơn nhiều
   để kiểm thử, và ở `max_tokens` 16000 thì chưa chạm timeout HTTP. Đổi sang streaming về sau là
   sửa `IChatModel` sang `IAsyncEnumerable` — một thay đổi có kiểm soát, không phải viết lại.
2. **Chưa có hạn mức token theo người dùng.** Spec §6 xếp nó vào nhóm "phải có ngay". Kế hoạch này
   để lại vì nó cần chỗ lưu số đếm, mà chỗ lưu là quyết định còn mở (§12). Trong lúc chờ, trần
   5 vòng gọi công cụ + giới hạn 2000 ký tự mỗi câu hỏi là hai hàng rào chặn chi phí chạy loạn.

## Ngoài phạm vi Giai đoạn 1 (đừng làm thêm)

- Streaming câu trả lời — thêm ở giai đoạn sau, sẽ phải chuyển `IChatModel` sang `IAsyncEnumerable`.
- Hạn mức token theo người dùng theo ngày — cần chỗ lưu số đếm, chốt cùng lúc với quyết định lưu vector ở Giai đoạn 3.
- Lưu hội thoại vào DB — Giai đoạn 1 ghi bằng `ILogger`; bảng hội thoại là quyết định còn mở (xem `docs/superpowers/specs/2026-08-07-ai-integration-design.md` §12).
- Bất kỳ tool nào GHI dữ liệu — đó là Giai đoạn 2.
- Adapter thứ hai (OpenAI/Gemini), `IAiModelSelector`, keyed services, `IAiModule`, năng lực embedding và OCR — hình dạng đã chốt ở spec §3.3/§3.4/§4.1, viết khi có cái thứ hai để đối chiếu.

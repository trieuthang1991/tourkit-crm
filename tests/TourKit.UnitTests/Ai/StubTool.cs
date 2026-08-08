using Microsoft.Extensions.AI;
using TourKit.Ai.Abstractions;

namespace TourKit.UnitTests.Ai;

/// <summary>Công cụ giả cho test: đếm số lần bị gọi, trả về nội dung dựng sẵn.</summary>
internal sealed class StubTool : IAiTool
{
    private readonly string _reply;
    private readonly bool _throws;

    public StubTool(string name, string? permission, string reply = "ok", bool throws = false)
    {
        _reply = reply;
        _throws = throws;
        RequiredPermission = permission;
        Function = AIFunctionFactory.Create(Run, name, "công cụ giả cho test");
    }

    public int Invocations { get; private set; }

    public AIFunction Function { get; }

    public string? RequiredPermission { get; }

    private AiToolResult Run()
    {
        Invocations++;
        return _throws
            ? throw new InvalidOperationException("hỏng rồi")
            : new AiToolResult(_reply, new { ok = true }, "/dich-den");
    }
}

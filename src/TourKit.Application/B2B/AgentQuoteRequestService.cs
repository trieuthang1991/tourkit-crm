using FluentValidation;
using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.B2B;

/// <summary>
/// Yêu cầu báo giá của Đại lý (B2B MVP §4.2.3) — state machine:
/// Requested → Quoted (Sales chào giá) → Confirmed (Đại lý xác nhận) | Rejected.
/// Portal KHÔNG tự tính giá; Sales điền QuotedAmount.
/// </summary>
public sealed class AgentQuoteRequestService(
    IRepository<AgentQuoteRequest> repo,
    IRepository<Agent> agentRepo,
    IValidator<CreateAgentQuoteRequestDto> createValidator) : IAgentQuoteRequestService
{
    public async Task<PagedResult<AgentQuoteRequestDto>> ListAsync(int page, int size, AgentQuoteRequestListFilter? filter = null)
    {
        var f = filter ?? new AgentQuoteRequestListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(r =>
            (f.AgentId == null || r.AgentId == f.AgentId) &&
            (f.Status == null || (int)r.Status == f.Status));

        var filtered = all
            .Where(r => kw == null || r.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.CreatedAt).ToList();
        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();

        var agentIds = pageItems.Select(r => r.AgentId).ToHashSet();
        var agentNames = (await agentRepo.ListAsync(a => agentIds.Contains(a.Id))).ToDictionary(a => a.Id, a => a.Name);

        var dtos = pageItems.Select(r => Map(r) with { AgentName = agentNames.GetValueOrDefault(r.AgentId) }).ToList();
        return new PagedResult<AgentQuoteRequestDto>(dtos, filtered.Count, page, size);
    }

    public async Task<AgentQuoteStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        return new AgentQuoteStatsDto(
            all.Count,
            all.Count(r => r.Status == AgentQuoteStatus.Requested),
            all.Count(r => r.Status == AgentQuoteStatus.Quoted),
            all.Count(r => r.Status == AgentQuoteStatus.Confirmed),
            all.Count(r => r.Status == AgentQuoteStatus.Rejected),
            all.Sum(r => r.QuotedAmount ?? 0m));
    }

    public async Task<AgentQuoteRequestDto> GetAsync(Guid id)
    {
        var request = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return Map(request);
    }

    public async Task<AgentQuoteRequestDto> CreateAsync(CreateAgentQuoteRequestDto dto)
    {
        var result = await createValidator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }

        if (!await agentRepo.AnyAsync(a => a.Id == dto.AgentId))
        {
            throw new ValidationAppException("Đại lý không tồn tại.");
        }

        var request = new AgentQuoteRequest
        {
            AgentId = dto.AgentId,
            ProductName = dto.ProductName.Trim(),
            TravelDate = dto.TravelDate,
            ReturnDate = dto.ReturnDate,
            PaxCount = dto.PaxCount,
            SpecialRequests = dto.SpecialRequests,
            Status = AgentQuoteStatus.Requested,
        };
        await repo.AddAsync(request);
        await repo.SaveChangesAsync();

        return Map(request);
    }

    public async Task<AgentQuoteRequestDto> QuoteAsync(Guid id, QuoteAgentRequestDto dto)
    {
        var request = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (request.Status != AgentQuoteStatus.Requested)
        {
            throw new ConflictException("Chỉ chào giá được yêu cầu đang ở trạng thái Requested.");
        }

        if (dto.QuotedAmount < 0)
        {
            throw new ValidationAppException("Giá chào phải >= 0.");
        }

        request.QuotedAmount = dto.QuotedAmount;
        request.QuotedNote = dto.QuotedNote;
        request.Status = AgentQuoteStatus.Quoted;
        return await SaveAndMap(request);
    }

    public async Task<AgentQuoteRequestDto> ConfirmAsync(Guid id)
    {
        var request = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (request.Status != AgentQuoteStatus.Quoted)
        {
            throw new ConflictException("Chỉ xác nhận được yêu cầu đã được chào giá (Quoted).");
        }

        request.Status = AgentQuoteStatus.Confirmed;
        return await SaveAndMap(request);
    }

    public async Task<AgentQuoteRequestDto> RejectAsync(Guid id, string? note)
    {
        var request = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (request.Status is AgentQuoteStatus.Confirmed or AgentQuoteStatus.Rejected)
        {
            throw new ConflictException("Yêu cầu đã kết thúc, không thể từ chối.");
        }

        request.Status = AgentQuoteStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(note))
        {
            request.QuotedNote = note;
        }

        return await SaveAndMap(request);
    }

    private async Task<AgentQuoteRequestDto> SaveAndMap(AgentQuoteRequest request)
    {
        repo.Update(request);
        await repo.SaveChangesAsync();
        return Map(request);
    }

    private static AgentQuoteRequestDto Map(AgentQuoteRequest r) =>
        new(r.Id, r.AgentId, r.ProductName, r.TravelDate, r.ReturnDate, r.PaxCount,
            r.SpecialRequests, r.Status, r.QuotedAmount, r.QuotedNote);
}

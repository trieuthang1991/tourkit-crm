using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Operations;

/// <summary>
/// Thu-chi HDV theo chuyến (legacy RevenueExpensesInTourGuide). Đối soát: net = Σ thu − Σ chi.
/// Validate phân công HDV tồn tại; số tiền &gt; 0; loại 0/1.
/// </summary>
public sealed class GuideTransactionService(
    IRepository<GuideTransaction> repo,
    IRepository<TourGuideAssignment> assignmentRepo) : IGuideTransactionService
{
    public async Task<GuideSettlementDto> GetByAssignmentAsync(Guid assignmentId)
    {
        var items = await repo.ListAsync(x => x.TourGuideAssignmentId == assignmentId);
        var ordered = items.OrderBy(x => x.OccurredAt).Select(Map).ToArray();
        var revenue = items.Where(x => x.Type == (int)GuideTransactionType.Revenue).Sum(x => x.Amount);
        var expense = items.Where(x => x.Type == (int)GuideTransactionType.Expense).Sum(x => x.Amount);
        return new GuideSettlementDto(revenue, expense, revenue - expense, ordered);
    }

    public async Task<GuideTransactionDto> CreateAsync(Guid assignmentId, CreateGuideTransactionDto dto)
    {
        if (!await assignmentRepo.AnyAsync(a => a.Id == assignmentId))
        {
            throw new NotFoundException();
        }

        if (dto.Type is < 0 or > 1)
        {
            throw new ValidationAppException("Loại giao dịch không hợp lệ.");
        }

        if (dto.Amount <= 0)
        {
            throw new ValidationAppException("Số tiền phải > 0.");
        }

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ValidationAppException("Cần diễn giải.");
        }

        var entity = new GuideTransaction
        {
            TourGuideAssignmentId = assignmentId,
            Type = dto.Type,
            Amount = dto.Amount,
            Description = dto.Description.Trim(),
            OccurredAt = dto.OccurredAt ?? DateTimeOffset.UtcNow,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task DeleteAsync(Guid assignmentId, Guid transactionId)
    {
        var entity = await repo.GetByIdAsync(transactionId);
        if (entity is null || entity.TourGuideAssignmentId != assignmentId)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static GuideTransactionDto Map(GuideTransaction x) => new(
        x.Id, x.TourGuideAssignmentId, x.Type, x.Amount, x.Description, x.OccurredAt);
}

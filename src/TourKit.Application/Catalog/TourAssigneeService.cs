using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

public sealed class TourAssigneeService(
    IRepository<TourAssignee> repo,
    IRepository<Tour> tourRepo) : ITourAssigneeService
{
    public async Task<IReadOnlyList<AssigneeDto>> ListAsync(Guid tourId)
    {
        var assignees = await repo.ListAsync(a => a.TourId == tourId);
        return assignees.Select(a => new AssigneeDto(a.Id, a.UserId, a.Role)).ToList();
    }

    public async Task ReplaceAsync(Guid tourId, IReadOnlyList<AssigneeDto> assignees)
    {
        if (!await tourRepo.AnyAsync(t => t.Id == tourId))
        {
            throw new NotFoundException();
        }

        var old = await repo.ListAsync(a => a.TourId == tourId);
        foreach (var a in old)
        {
            repo.Remove(a);
        }

        foreach (var a in assignees)
        {
            await repo.AddAsync(new TourAssignee { TourId = tourId, UserId = a.UserId, Role = a.Role });
        }

        await repo.SaveChangesAsync();
    }
}

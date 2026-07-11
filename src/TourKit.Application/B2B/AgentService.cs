using FluentValidation;
using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.B2B;

/// <summary>Đại lý B2B — CRUD phân trang (tài khoản do DMC cấp, §4.2.1).</summary>
public sealed class AgentService(
    IRepository<Agent> repo,
    IValidator<CreateAgentDto> createValidator,
    IValidator<UpdateAgentDto> updateValidator) : IAgentService
{
    public async Task<PagedResult<AgentDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        return new PagedResult<AgentDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new Agent
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            TaxCode = dto.TaxCode,
            Address = dto.Address,
            CreditLimit = dto.CreditLimit,
            Status = dto.Status,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateAgentDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.ContactPerson = dto.ContactPerson;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.TaxCode = dto.TaxCode;
        entity.Address = dto.Address;
        entity.CreditLimit = dto.CreditLimit;
        entity.Status = dto.Status;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static AgentDto Map(Agent a) =>
        new(a.Id, a.Code, a.Name, a.ContactPerson, a.Phone, a.Email, a.TaxCode, a.Address, a.CreditLimit, a.Status);
}

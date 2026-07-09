using FluentValidation;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Catalog;

public sealed class TourTemplateService(
    IRepository<TourTemplate> repo,
    IRepository<TourItinerary> itineraryRepo,
    IRepository<PriceScenario> priceScenarioRepo,
    IValidator<CreateTourTemplateDto> createValidator,
    IValidator<UpdateTourTemplateDto> updateValidator) : ITourTemplateService
{
    public async Task<PagedResult<TourTemplateDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<TourTemplateDto>(dtos, total, page, size);
    }

    public async Task<TourTemplateDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<TourTemplateDto> CreateAsync(CreateTourTemplateDto dto)
    {
        await Validate(createValidator, dto);

        var code = dto.Code.Trim();
        if (await repo.AnyAsync(t => t.Code == code))
        {
            throw new ConflictException($"Mã mẫu tour '{code}' đã tồn tại.");
        }

        var entity = new TourTemplate
        {
            Code = code,
            Title = dto.Title.Trim(),
            TourType = dto.TourType,
            TotalSlots = dto.TotalSlots,
            ReservationHours = dto.ReservationHours,
            PriceAdult = dto.PriceAdult,
            PriceChild = dto.PriceChild,
            PriceChildSmall = dto.PriceChildSmall,
            PriceBaby = dto.PriceBaby,
            TermsNote = dto.TermsNote,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTourTemplateDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Title = dto.Title.Trim();
        entity.TourType = dto.TourType;
        entity.TotalSlots = dto.TotalSlots;
        entity.ReservationHours = dto.ReservationHours;
        entity.PriceAdult = dto.PriceAdult;
        entity.PriceChild = dto.PriceChild;
        entity.PriceChildSmall = dto.PriceChildSmall;
        entity.PriceBaby = dto.PriceBaby;
        entity.TermsNote = dto.TermsNote;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ItineraryDayDto>> GetItineraryAsync(Guid id)
    {
        var days = await itineraryRepo.ListAsync(i => i.TourId == id);
        return days.OrderBy(i => i.DayIndex)
            .Select(i => new ItineraryDayDto(i.Id, i.DayIndex, i.Title, i.Detail))
            .ToList();
    }

    public async Task ReplaceItineraryAsync(Guid id, IReadOnlyList<ItineraryDayDto> days)
    {
        if (!await repo.AnyAsync(t => t.Id == id))
        {
            throw new NotFoundException();
        }

        var old = await itineraryRepo.ListAsync(i => i.TourId == id);
        foreach (var day in old)
        {
            itineraryRepo.Remove(day);
        }

        foreach (var day in days)
        {
            await itineraryRepo.AddAsync(new TourItinerary
            {
                TourId = id,
                DayIndex = day.DayIndex,
                Title = day.Title.Trim(),
                Detail = day.Detail,
            });
        }

        await itineraryRepo.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PriceScenarioDto>> GetPriceScenariosAsync(Guid id)
    {
        var scenarios = await priceScenarioRepo.ListAsync(p => p.TourTemplateId == id);
        return scenarios.OrderBy(p => p.FromQty)
            .Select(p => new PriceScenarioDto(p.Id, p.FromQty, p.ToQty, p.UnitPrice))
            .ToList();
    }

    public async Task ReplacePriceScenariosAsync(Guid id, IReadOnlyList<PriceScenarioDto> scenarios)
    {
        if (!await repo.AnyAsync(t => t.Id == id))
        {
            throw new NotFoundException();
        }

        var old = await priceScenarioRepo.ListAsync(p => p.TourTemplateId == id);
        foreach (var scenario in old)
        {
            priceScenarioRepo.Remove(scenario);
        }

        foreach (var scenario in scenarios)
        {
            await priceScenarioRepo.AddAsync(new PriceScenario
            {
                TourTemplateId = id,
                FromQty = scenario.FromQty,
                ToQty = scenario.ToQty,
                UnitPrice = scenario.UnitPrice,
            });
        }

        await priceScenarioRepo.SaveChangesAsync();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static TourTemplateDto Map(TourTemplate t) => new(
        t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
        t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status);
}

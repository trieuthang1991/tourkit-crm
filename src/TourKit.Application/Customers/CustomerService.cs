using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Customers.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.Application.Customers;

public sealed class CustomerService(
    IRepository<Customer> repo,
    IRepository<Order> orderRepo,
    IRepository<CustomerCare> careRepo,
    IRepository<User> userRepo,
    ICurrentUserContext currentUser,
    IValidator<CreateCustomerDto> createValidator,
    IValidator<UpdateCustomerDto> updateValidator) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var ids = items.Select(c => c.Id).ToHashSet();

        // Map id(string) → tên NV để hiển thị người tạo / NV phụ trách. ID legacy không khớp sẽ giữ nguyên chuỗi.
        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id.ToString(), u => u.FullName);

        // Aggregate bám danh sách hệ cũ: số lần mua + doanh thu (Order), chăm sóc gần nhất (CustomerCare).
        var orders = await orderRepo.ListAsync(o => ids.Contains(o.CustomerId));
        var ordersByCustomer = orders
            .GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Sum: g.Sum(o => o.TotalRevenue)));

        var cares = await careRepo.ListAsync(c => ids.Contains(c.CustomerId));
        var lastCareByCustomer = cares
            .GroupBy(c => c.CustomerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var dtos = items.Select(c =>
        {
            ordersByCustomer.TryGetValue(c.Id, out var agg);
            lastCareByCustomer.TryGetValue(c.Id, out var lastCare);
            return Map(c, userNames, agg.Count, agg.Sum, lastCare?.CreatedAt, lastCare?.Title);
        }).ToList();

        return new PagedResult<CustomerDto>(dtos, total, page, size);
    }

    public async Task<CustomerDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id.ToString(), u => u.FullName);
        return Map(entity, userNames);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        await Validate(createValidator, dto);

        var profile = new CustomerCrmProfile
        {
            Gender = dto.Gender,
            City = dto.City,
            MarketGroup = dto.MarketGroup,
            InitialNeed = dto.InitialNeed,
            CollaboratorName = dto.CollaboratorName,
            Campaign = dto.Campaign,
            CreatedBy = currentUser.UserId?.ToString(),
            Segments = dto.Segments ?? [],
            Tags = dto.Tags ?? [],
            AssignedTo = dto.AssignedTo ?? [],
        };

        var entity = new Customer
        {
            Code = "KH_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            CustomerType = dto.CustomerType,
            Source = dto.Source,
            Tag = dto.Tag,
            TempBalance = dto.TempBalance,
            Email = dto.Email,
            Address = dto.Address,
            DateOfBirth = dto.DateOfBirth,
            IdCardNumber = dto.IdCardNumber,
            PassportNumber = dto.PassportNumber,
            PassportExpiry = dto.PassportExpiry,
            Nationality = dto.Nationality,
            CrmProfileJson = profile.ToJsonOrNull(),
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity, null);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var existing = CustomerCrmProfile.Parse(entity.CrmProfileJson);

        var profile = new CustomerCrmProfile
        {
            Gender = dto.Gender,
            City = dto.City,
            MarketGroup = dto.MarketGroup,
            InitialNeed = dto.InitialNeed,
            CollaboratorName = dto.CollaboratorName,
            Campaign = dto.Campaign,
            CreatedBy = existing.CreatedBy, // giữ nguyên người tạo gốc
            Segments = dto.Segments ?? [],
            Tags = dto.Tags ?? [],
            AssignedTo = dto.AssignedTo ?? [],
        };

        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        entity.CustomerType = dto.CustomerType;
        entity.Source = dto.Source;
        entity.Tag = dto.Tag;
        entity.TempBalance = dto.TempBalance;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.IdCardNumber = dto.IdCardNumber;
        entity.PassportNumber = dto.PassportNumber;
        entity.PassportExpiry = dto.PassportExpiry;
        entity.Nationality = dto.Nationality;
        entity.CrmProfileJson = profile.ToJsonOrNull();
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

    private static CustomerDto Map(
        Customer c, Dictionary<string, string>? userNames,
        int purchaseCount = 0, decimal revenue = 0, DateTimeOffset? lastCareAt = null, string? lastCareContent = null)
    {
        var p = CustomerCrmProfile.Parse(c.CrmProfileJson);
        string? NameOf(string? refId) =>
            refId is not null && userNames is not null && userNames.TryGetValue(refId, out var n) ? n : null;
        // NV phụ trách: ưu tiên tên; ID legacy không khớp → giữ nguyên chuỗi để không mất dữ liệu.
        var assignedNames = p.AssignedTo
            .Select(id => userNames is not null && userNames.TryGetValue(id, out var n) ? n : id)
            .ToList();

        return new CustomerDto(
            c.Id, c.Code, c.FullName, c.Phone, c.CustomerType, c.Source, c.Tag, c.TempBalance,
            c.Email, c.Address, c.DateOfBirth, c.IdCardNumber, c.PassportNumber, c.PassportExpiry, c.Nationality,
            p.Gender, p.City, p.MarketGroup, p.InitialNeed, p.CollaboratorName, p.Campaign,
            p.CreatedBy, NameOf(p.CreatedBy),
            p.Segments, p.Tags, p.AssignedTo, assignedNames,
            c.CreatedAt, purchaseCount, revenue, lastCareAt, lastCareContent);
    }
}

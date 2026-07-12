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
    public async Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CustomerListFilter? filter = null)
    {
        var f = filter ?? new CustomerListFilter();
        var kw = Norm(f.Q);
        var src = Norm(f.Source);

        // B1: lọc tại DB các field là CỘT thật (nhanh). Field mềm trong jsonb + aggregate lọc ở bộ nhớ (B3).
        // Prod scale (nhiều KH) có thể đẩy filter jsonb xuống Postgres (CrmProfileJson ->> ...); dev đủ dùng.
        var candidates = await repo.ListAsync(c =>
            (f.CustomerType == null || c.CustomerType == f.CustomerType) &&
            (kw == null ||
                c.FullName.Contains(kw) ||
                (c.Code != null && c.Code.Contains(kw)) ||
                (c.Phone != null && c.Phone.Contains(kw)) ||
                (c.Email != null && c.Email.Contains(kw))) &&
            (src == null || (c.Source != null && c.Source.Contains(src))) &&
            (f.CreatedFrom == null || c.CreatedAt >= f.CreatedFrom) &&
            (f.CreatedTo == null || c.CreatedAt <= f.CreatedTo));

        var ids = candidates.Select(c => c.Id).ToHashSet();

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

        // B3: lọc field mềm (jsonb) + aggregate ở bộ nhớ; sắp theo ngày tạo giảm dần (bám hệ cũ) rồi phân trang.
        var now = DateTimeOffset.UtcNow;
        var matched = new List<(Customer Cust, int Count, decimal Sum, DateTimeOffset? CareAt, string? CareTitle)>();
        foreach (var c in candidates)
        {
            var p = CustomerCrmProfile.Parse(c.CrmProfileJson);
            ordersByCustomer.TryGetValue(c.Id, out var agg);
            lastCareByCustomer.TryGetValue(c.Id, out var lastCare);

            if (!Contains(p.City, f.City) || !Contains(p.Gender, f.Gender) ||
                !Contains(p.MarketGroup, f.MarketGroup) || !Contains(p.CollaboratorName, f.Collaborator) ||
                !Contains(p.Campaign, f.Campaign) || !Contains(p.Branch, f.Branch) ||
                !Contains(p.Group, f.Group) || !Contains(p.Department, f.Department))
            {
                continue;
            }

            if (Norm(f.Segment) is { } seg && !p.Segments.Contains(seg)) { continue; }
            if (Norm(f.Tag) is { } tag && !p.Tags.Contains(tag)) { continue; }
            if (Norm(f.AssignedTo) is { } asg && !p.AssignedTo.Contains(asg)) { continue; }
            if (Norm(f.CreatedBy) is { } cb && p.CreatedBy != cb) { continue; }
            if (f.RevenueFrom is { } rf && agg.Sum < rf) { continue; }
            if (f.RevenueTo is { } rt && agg.Sum > rt) { continue; }
            if (f.CareFrom is { } caf && (lastCare == null || lastCare.CreatedAt < caf)) { continue; }
            if (f.CareTo is { } cat && (lastCare == null || lastCare.CreatedAt > cat)) { continue; }
            if (f.BirthdayMonth is { } bm && (c.DateOfBirth == null || c.DateOfBirth.Value.Month != bm)) { continue; }
            if (Norm(f.PurchaseBucket) is { } pb && PurchaseBucketOf(agg.Count) != pb) { continue; }
            if (Norm(f.NotContactedBucket) is { } ncb &&
                ContactBucketOf(DaysSinceContact(now, lastCare?.CreatedAt, c.CreatedAt)) != ncb) { continue; }

            matched.Add((c, agg.Count, agg.Sum, lastCare?.CreatedAt, lastCare?.Title));
        }

        var ordered = matched.OrderByDescending(x => x.Cust.CreatedAt).ToList();
        var pageItems = ordered.Skip((page - 1) * size).Take(size);
        var dtos = pageItems
            .Select(x => Map(x.Cust, userNames, x.Count, x.Sum, x.CareAt, x.CareTitle))
            .ToList();

        return new PagedResult<CustomerDto>(dtos, ordered.Count, page, size);
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // So khớp field mềm: bỏ qua nếu filter rỗng; ngược lại contains không phân biệt hoa thường.
    private static bool Contains(string? value, string? needle)
    {
        var n = Norm(needle);
        return n == null || (value != null && value.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    // Nhóm "mua": first = đúng 1 đơn, repeat = >1 đơn.
    private static string? PurchaseBucketOf(int orderCount) =>
        orderCount == 1 ? "first" : orderCount > 1 ? "repeat" : null;

    // Nhóm "chưa liên hệ" phân tầng loại trừ theo số ngày kể từ lần chăm sóc gần nhất
    // (hoặc ngày tạo nếu chưa từng liên hệ): 7=[7,15) · 15=[15,30) · 30=[30,90) · 90=≥90. <7 ngày → không thuộc nhóm nào.
    private static int DaysSinceContact(DateTimeOffset now, DateTimeOffset? lastCareAt, DateTimeOffset createdAt) =>
        (int)(now - (lastCareAt ?? createdAt)).TotalDays;

    private static string? ContactBucketOf(int days) =>
        days >= 90 ? "nc90" : days >= 30 ? "nc30" : days >= 15 ? "nc15" : days >= 7 ? "nc7" : null;

    public async Task<CustomerFunnelDto> GetFunnelAsync()
    {
        var customers = await repo.ListAsync();
        var now = DateTimeOffset.UtcNow;

        var orders = await orderRepo.ListAsync();
        var orderCountByCustomer = orders.GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.Count());

        var cares = await careRepo.ListAsync();
        var lastCareByCustomer = cares.GroupBy(c => c.CustomerId).ToDictionary(g => g.Key, g => g.Max(x => x.CreatedAt));

        var segCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int firstTime = 0, repeat = 0, nc7 = 0, nc15 = 0, nc30 = 0, nc90 = 0;

        foreach (var c in customers)
        {
            var p = CustomerCrmProfile.Parse(c.CrmProfileJson);
            foreach (var s in p.Segments.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                segCounts[s.Trim()] = segCounts.GetValueOrDefault(s.Trim()) + 1;
            }

            switch (PurchaseBucketOf(orderCountByCustomer.GetValueOrDefault(c.Id)))
            {
                case "first": firstTime++; break;
                case "repeat": repeat++; break;
                default: break;
            }

            DateTimeOffset? lastCare = lastCareByCustomer.TryGetValue(c.Id, out var lc) ? lc : null;
            switch (ContactBucketOf(DaysSinceContact(now, lastCare, c.CreatedAt)))
            {
                case "nc7": nc7++; break;
                case "nc15": nc15++; break;
                case "nc30": nc30++; break;
                case "nc90": nc90++; break;
                default: break;
            }
        }

        var segments = segCounts
            .Select(kv => new FunnelSegmentDto(kv.Key, kv.Value))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
            .ToList();

        return new CustomerFunnelDto(
            customers.Count, segments,
            new CustomerCareBucketsDto(firstTime, repeat, nc7, nc15, nc30, nc90));
    }

    public async Task<CustomerStatsDto> GetStatsAsync()
    {
        var customers = await repo.ListAsync();
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Số lần mua theo KH (đơn hàng) → mua lần đầu (đúng 1 đơn) / mua lại (>1 đơn).
        var orders = await orderRepo.ListAsync();
        var orderCountByCustomer = orders.GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.Count());

        return new CustomerStatsDto(
            Total: customers.Count,
            NewToday: customers.Count(c => c.CreatedAt >= todayStart),
            NewThisMonth: customers.Count(c => c.CreatedAt >= monthStart),
            FirstTimeBuyers: orderCountByCustomer.Count(kv => kv.Value == 1),
            RepeatBuyers: orderCountByCustomer.Count(kv => kv.Value > 1));
    }

    public async Task<CustomerFilterOptionsDto> GetFilterOptionsAsync()
    {
        // Gom distinct từ data thực để dropdown lọc chọn được (không gõ tay). Dev nhỏ nên quét toàn bộ.
        var customers = await repo.ListAsync();
        var profiles = customers.Select(c => CustomerCrmProfile.Parse(c.CrmProfileJson)).ToList();

        static IReadOnlyList<string> Distinct(IEnumerable<string?> vals) =>
            vals.Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCulture)
                .ToList();

        return new CustomerFilterOptionsDto(
            Distinct(customers.Select(c => c.Source)),
            Distinct(profiles.Select(p => p.City)),
            Distinct(profiles.Select(p => p.MarketGroup)),
            Distinct(profiles.Select(p => p.Campaign)),
            Distinct(profiles.Select(p => p.CollaboratorName)),
            Distinct(profiles.Select(p => p.Branch)),
            Distinct(profiles.Select(p => p.Group)),
            Distinct(profiles.Select(p => p.Department)),
            Distinct(profiles.SelectMany(p => p.Tags)),
            Distinct(profiles.SelectMany(p => p.Segments)));
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
            Branch = dto.Branch,
            Group = dto.Group,
            Department = dto.Department,
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
            Branch = dto.Branch,
            Group = dto.Group,
            Department = dto.Department,
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
            p.Branch, p.Group, p.Department,
            p.CreatedBy, NameOf(p.CreatedBy),
            p.Segments, p.Tags, p.AssignedTo, assignedNames,
            c.CreatedAt, purchaseCount, revenue, lastCareAt, lastCareContent);
    }
}

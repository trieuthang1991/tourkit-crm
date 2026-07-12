using System.Linq.Expressions;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;
using TourKit.Application.Customers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.UnitTests.Customers;

/// <summary>
/// Test <see cref="CustomerService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP (cùng tinh thần với các slice test khác trong assembly).
/// </summary>
public class CustomerServiceTests
{
    /// <summary>Fake repo tối giản backed bởi <see cref="List{T}"/>, đủ cho unit test service.</summary>
    private sealed class FakeRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items = [];

        public Task<T?> GetByIdAsync(Guid id)
            => Task.FromResult(_items.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            return Task.FromResult<IReadOnlyList<T>>(query.ToList());
        }

        public Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null)
        {
            var query = predicate is null ? _items.AsEnumerable() : _items.AsQueryable().Where(predicate);
            var list = query.ToList();
            var pageItems = list.Skip((page - 1) * size).Take(size).ToList();
            return Task.FromResult<(IReadOnlyList<T> Items, int Total)>((pageItems, list.Count));
        }

        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            var index = _items.FindIndex(e => e.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }
        }

        public void Remove(T entity) => _items.RemoveAll(e => e.Id == entity.Id);

        public Task<int> SaveChangesAsync() => Task.FromResult(0);

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => Task.FromResult(_items.AsQueryable().Any(predicate));
    }

    private sealed class FakeCurrentUser : ICurrentUserContext
    {
        public Guid? UserId => null;
    }

    private static CustomerService NewService(out FakeRepository<Customer> repo)
    {
        repo = new FakeRepository<Customer>();
        return new CustomerService(
            repo, new FakeRepository<Order>(), new FakeRepository<CustomerCare>(), new FakeRepository<User>(),
            new FakeCurrentUser(), new CreateCustomerValidator(), new UpdateCustomerValidator());
    }

    private static CustomerService NewServiceFull(
        out FakeRepository<Customer> customers, out FakeRepository<Order> orders, out FakeRepository<CustomerCare> cares)
    {
        customers = new FakeRepository<Customer>();
        orders = new FakeRepository<Order>();
        cares = new FakeRepository<CustomerCare>();
        return new CustomerService(
            customers, orders, cares, new FakeRepository<User>(),
            new FakeCurrentUser(), new CreateCustomerValidator(), new UpdateCustomerValidator());
    }

    private static Customer NewCustomer(
        string name, int type = 0, string? source = null, string? phone = null, string? email = null,
        string? code = null, DateTimeOffset? createdAt = null, DateTimeOffset? dob = null, CustomerCrmProfile? profile = null)
        => new()
        {
            FullName = name,
            CustomerType = type,
            Source = source,
            Phone = phone,
            Email = email,
            Code = code,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            DateOfBirth = dob,
            CrmProfileJson = profile?.ToJsonOrNull(),
        };

    private static async Task SeedAsync(FakeRepository<Customer> repo, params Customer[] items)
    {
        foreach (var c in items)
        {
            await repo.AddAsync(c);
        }

        await repo.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCustomerDto("Nguyen Van A", "0900000000"));

        Assert.Equal("Nguyen Van A", dto.FullName);
        Assert.Equal("0900000000", dto.Phone);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("Nguyen Van A", stored!.FullName);
    }

    [Fact]
    public async Task CreateAsync_persists_passport_and_personal_fields()
    {
        var service = NewService(out var repo);
        var expiry = DateTimeOffset.UtcNow.AddYears(5);

        var dto = await service.CreateAsync(new CreateCustomerDto(
            "Trần B", "0911111111",
            Email: "b@x.com", Address: "Hà Nội", IdCardNumber: "0123456789",
            PassportNumber: "B1234567", PassportExpiry: expiry, Nationality: "Việt Nam"));

        Assert.Equal("b@x.com", dto.Email);
        Assert.Equal("B1234567", dto.PassportNumber);
        Assert.Equal("Việt Nam", dto.Nationality);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.Equal("B1234567", stored!.PassportNumber);
        Assert.Equal(expiry, stored.PassportExpiry);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_FullName_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCustomerDto("", null)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCustomerDto("Ten moi", null)));
    }

    // ---- Lọc danh sách (bám thanh "Xem thêm bộ lọc" hệ cũ) ----

    [Theory]
    [InlineData("Bình")]     // theo tên
    [InlineData("KH_007")]   // theo mã
    [InlineData("0987")]     // theo SĐT
    [InlineData("binh@")]    // theo email
    public async Task ListAsync_q_matches_name_code_phone_email(string q)
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("Nguyễn Bình", code: "KH_007", phone: "0987654321", email: "binh@x.com"),
            NewCustomer("Trần Khác", code: "KH_999", phone: "0900000000", email: "khac@x.com"));

        var result = await service.ListAsync(1, 20, new CustomerListFilter(Q: q));

        Assert.Equal("Nguyễn Bình", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_customerType()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers, NewCustomer("Cá nhân A", type: 0), NewCustomer("Doanh nghiệp B", type: 1));

        var result = await service.ListAsync(1, 20, new CustomerListFilter(CustomerType: 1));

        Assert.Equal("Doanh nghiệp B", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_jsonb_city_contains_caseInsensitive()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("A", profile: new CustomerCrmProfile { City = "Hà Nội" }),
            NewCustomer("B", profile: new CustomerCrmProfile { City = "TP.HCM" }));

        var result = await service.ListAsync(1, 20, new CustomerListFilter(City: "hà nội"));

        Assert.Equal("A", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_list_membership_segment_tag_assignedTo()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("A", profile: new CustomerCrmProfile { Segments = ["VIP"], Tags = ["Nóng"], AssignedTo = ["u1"] }),
            NewCustomer("B", profile: new CustomerCrmProfile { Segments = ["Thường"], Tags = ["Nguội"], AssignedTo = ["u2"] }));

        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(Segment: "VIP"))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(Tag: "Nóng"))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(AssignedTo: "u1"))).Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_new_softFields_branch_group_department()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("A", profile: new CustomerCrmProfile { Branch = "CN1", Group = "Nhóm1", Department = "Sales" }),
            NewCustomer("B", profile: new CustomerCrmProfile { Branch = "CN2", Group = "Nhóm2", Department = "Ops" }));

        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(Branch: "CN1"))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(Group: "Nhóm1"))).Items).FullName);
        Assert.Equal("A", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(Department: "Sales"))).Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_revenue_range()
    {
        var service = NewServiceFull(out var customers, out var orders, out _);
        var rich = NewCustomer("Rich");
        var poor = NewCustomer("Poor");
        await SeedAsync(customers, rich, poor);
        await orders.AddAsync(new Order { CustomerId = rich.Id, TotalRevenue = 5_000_000m });
        await orders.SaveChangesAsync();

        Assert.Equal("Rich", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(RevenueFrom: 1_000_000m))).Items).FullName);
        Assert.Empty((await service.ListAsync(1, 20, new CustomerListFilter(RevenueFrom: 10_000_000m))).Items);
    }

    [Fact]
    public async Task ListAsync_filters_by_created_range()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        var jan = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var jun = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        await SeedAsync(customers, NewCustomer("Old", createdAt: jan), NewCustomer("New", createdAt: jun));

        var result = await service.ListAsync(1, 20, new CustomerListFilter(
            CreatedFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal("New", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_birthday_month()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("March", dob: new DateTimeOffset(1990, 3, 20, 0, 0, 0, TimeSpan.Zero)),
            NewCustomer("July", dob: new DateTimeOffset(1988, 7, 2, 0, 0, 0, TimeSpan.Zero)));

        var result = await service.ListAsync(1, 20, new CustomerListFilter(BirthdayMonth: 3));

        Assert.Equal("March", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_care_date_range()
    {
        var service = NewServiceFull(out var customers, out _, out var cares);
        var cared = NewCustomer("Cared");
        var neglected = NewCustomer("Neglected");
        await SeedAsync(customers, cared, neglected);
        await cares.AddAsync(new CustomerCare
        {
            CustomerId = cared.Id,
            Title = "Gọi hỏi thăm",
            CreatedAt = new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
        });
        await cares.SaveChangesAsync();

        var result = await service.ListAsync(1, 20, new CustomerListFilter(
            CareFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            CareTo: new DateTimeOffset(2026, 6, 30, 23, 59, 59, TimeSpan.Zero)));

        Assert.Equal("Cared", Assert.Single(result.Items).FullName);
    }

    [Fact]
    public async Task ListAsync_orders_by_createdAt_desc_and_pages()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("Oldest", createdAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            NewCustomer("Middle", createdAt: new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)),
            NewCustomer("Newest", createdAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        var page1 = await service.ListAsync(1, 2, null);

        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("Newest", page1.Items[0].FullName);   // sắp ngày tạo giảm dần
        Assert.Equal("Middle", page1.Items[1].FullName);
    }

    // ---- Phễu khách hàng + Chăm sóc (chip lọc nhanh) ----

    [Fact]
    public async Task GetFunnelAsync_counts_segments_and_care_buckets()
    {
        var service = NewServiceFull(out var customers, out var orders, out var cares);
        var now = DateTimeOffset.UtcNow;
        var a = NewCustomer("A", createdAt: now.AddDays(-10), profile: new CustomerCrmProfile { Segments = ["VIP", "Tiềm năng"] }); // nc7, 0 đơn
        var b = NewCustomer("B", createdAt: now.AddDays(-100), profile: new CustomerCrmProfile { Segments = ["VIP"] });          // nc90, 1 đơn
        var c = NewCustomer("C", createdAt: now.AddDays(-2));                                                                    // <7 ngày, 2 đơn
        await SeedAsync(customers, a, b, c);
        await orders.AddAsync(new Order { CustomerId = b.Id, TotalRevenue = 1_000m });
        await orders.AddAsync(new Order { CustomerId = c.Id, TotalRevenue = 1_000m });
        await orders.AddAsync(new Order { CustomerId = c.Id, TotalRevenue = 2_000m });
        await orders.SaveChangesAsync();
        _ = cares;

        var funnel = await service.GetFunnelAsync();

        Assert.Equal(3, funnel.Total);
        Assert.Equal("VIP", funnel.Segments[0].Name);         // sắp theo count giảm dần
        Assert.Equal(2, funnel.Segments[0].Count);
        Assert.Contains(funnel.Segments, s => s.Name == "Tiềm năng" && s.Count == 1);
        Assert.Equal(1, funnel.Care.FirstTime);               // B (1 đơn)
        Assert.Equal(1, funnel.Care.Repeat);                  // C (2 đơn)
        Assert.Equal(1, funnel.Care.NotContacted7);           // A
        Assert.Equal(1, funnel.Care.NotContacted90);          // B
        Assert.Equal(0, funnel.Care.NotContacted15);
        Assert.Equal(0, funnel.Care.NotContacted30);
    }

    [Fact]
    public async Task ListAsync_filters_by_purchaseBucket()
    {
        var service = NewServiceFull(out var customers, out var orders, out _);
        var first = NewCustomer("First");
        var repeat = NewCustomer("Repeat");
        await SeedAsync(customers, first, repeat, NewCustomer("None"));
        await orders.AddAsync(new Order { CustomerId = first.Id, TotalRevenue = 1_000m });
        await orders.AddAsync(new Order { CustomerId = repeat.Id, TotalRevenue = 1_000m });
        await orders.AddAsync(new Order { CustomerId = repeat.Id, TotalRevenue = 2_000m });
        await orders.SaveChangesAsync();

        Assert.Equal("First", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(PurchaseBucket: "first"))).Items).FullName);
        Assert.Equal("Repeat", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(PurchaseBucket: "repeat"))).Items).FullName);
    }

    [Fact]
    public async Task ListAsync_filters_by_notContactedBucket_exclusiveTiers()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        var now = DateTimeOffset.UtcNow;
        await SeedAsync(customers,
            NewCustomer("Recent", createdAt: now.AddDays(-3)),   // <7 → không thuộc nhóm nào
            NewCustomer("Week", createdAt: now.AddDays(-10)),    // nc7  [7,15)
            NewCustomer("Month", createdAt: now.AddDays(-50)));  // nc30 [30,90)

        Assert.Equal("Week", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(NotContactedBucket: "nc7"))).Items).FullName);
        Assert.Equal("Month", Assert.Single((await service.ListAsync(1, 20, new CustomerListFilter(NotContactedBucket: "nc30"))).Items).FullName);
        Assert.Empty((await service.ListAsync(1, 20, new CustomerListFilter(NotContactedBucket: "nc90"))).Items);
    }

    [Fact]
    public async Task GetFilterOptionsAsync_returns_distinct_sorted_nonEmpty()
    {
        var service = NewServiceFull(out var customers, out _, out _);
        await SeedAsync(customers,
            NewCustomer("A", source: "Facebook", profile: new CustomerCrmProfile { City = "Hà Nội", Tags = ["VIP"], Segments = ["Tiềm năng"] }),
            NewCustomer("B", source: "Facebook", profile: new CustomerCrmProfile { City = "Đà Nẵng", Tags = ["VIP", "Nóng"], Segments = [] }),
            NewCustomer("C", source: null, profile: new CustomerCrmProfile { City = "  " }));

        var opts = await service.GetFilterOptionsAsync();

        Assert.Equal(["Facebook"], opts.Sources);              // distinct + bỏ null
        Assert.Equal(["Nóng", "VIP"], opts.Tags);              // distinct + sort
        Assert.Equal(["Đà Nẵng", "Hà Nội"], opts.Cities);      // bỏ chuỗi rỗng, sort
        Assert.Equal(["Tiềm năng"], opts.Segments);
    }
}

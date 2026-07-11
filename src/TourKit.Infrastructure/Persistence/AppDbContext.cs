using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourTemplate> TourTemplates => Set<TourTemplate>();
    public DbSet<TourDeparture> TourDepartures => Set<TourDeparture>();
    public DbSet<TourItinerary> TourItineraries => Set<TourItinerary>();
    public DbSet<MarketType> MarketTypes => Set<MarketType>();
    public DbSet<PriceScenario> PriceScenarios => Set<PriceScenario>();
    public DbSet<TourAssignee> TourAssignees => Set<TourAssignee>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<TourCustomer> TourCustomers => Set<TourCustomer>();
    public DbSet<CancelSeat> CancelSeats => Set<CancelSeat>();
    public DbSet<ReceiptVoucher> ReceiptVouchers => Set<ReceiptVoucher>();
    public DbSet<PaymentVoucher> PaymentVouchers => Set<PaymentVoucher>();
    public DbSet<ReceiptApproval> ReceiptApprovals => Set<ReceiptApproval>();
    public DbSet<ReceiptApprovalStepUser> ReceiptApprovalStepUsers => Set<ReceiptApprovalStepUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<OrderCost> OrderCosts => Set<OrderCost>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();
    public DbSet<ProfitShare> ProfitShares => Set<ProfitShare>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<MarketingSendLog> MarketingSendLogs => Set<MarketingSendLog>();
    public DbSet<CustomerCare> CustomerCares => Set<CustomerCare>();
    public DbSet<TourRating> TourRatings => Set<TourRating>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TourGuideAssignment> TourGuideAssignments => Set<TourGuideAssignment>();
    public DbSet<VehicleAssignment> VehicleAssignments => Set<VehicleAssignment>();
    public DbSet<CustomerType> CustomerTypes => Set<CustomerType>();
    public DbSet<CustomerSource> CustomerSources => Set<CustomerSource>();
    public DbSet<CustomerTag> CustomerTags => Set<CustomerTag>();
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    public DbSet<CarType> CarTypes => Set<CarType>();
    public DbSet<LanguageType> LanguageTypes => Set<LanguageType>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Surcharge> Surcharges => Set<Surcharge>();
    public DbSet<OrderSurcharge> OrderSurcharges => Set<OrderSurcharge>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<PaymentApproval> PaymentApprovals => Set<PaymentApproval>();
    public DbSet<PaymentApprovalStepUser> PaymentApprovalStepUsers => Set<PaymentApprovalStepUser>();
    public DbSet<FileUpload> FileUploads => Set<FileUpload>();
    public DbSet<CustomerCommissionRule> CustomerCommissionRules => Set<CustomerCommissionRule>();
    public DbSet<TicketFund> TicketFunds => Set<TicketFund>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentQuoteRequest> AgentQuoteRequests => Set<AgentQuoteRequest>();
    public DbSet<AgentBooking> AgentBookings => Set<AgentBooking>();
    public DbSet<AgentPassenger> AgentPassengers => Set<AgentPassenger>();

    public override int SaveChanges()
    {
        ApplyTenantAndTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantAndTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantAndTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenant.TenantId;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != _tenant.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Chặn thao tác chéo tenant: entity thuộc {entry.Entity.TenantId}, request là {_tenant.TenantId}.");
                    }

                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình entity theo IEntityTypeConfiguration<T> (mỗi entity 1 file — xem conventions §5).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite không ORDER BY/so sánh được DateTimeOffset & decimal (lưu dạng TEXT).
        // Chỉ áp converter khi provider là SQLite (dev) — SqlServer/prod giữ kiểu gốc, InMemory (test) không đụng.
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    {
                        // long (ticks + offset) — sắp xếp được, không mất dữ liệu.
                        property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                    }
                    else if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        // double — sắp xếp/tính được trên SQLite (dev). Tiền chính xác cao chạy trên SqlServer/prod.
                        property.SetValueConverter(new CastingConverter<decimal, double>());
                    }
                }
            }
        }

        // Global query filter: cô lập tenant + ẩn soft-deleted. Một filter/entity nên phải gộp chung.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // TPT: filter chỉ đặt ở type GỐC (BaseType == null); type dẫn xuất kế thừa filter của gốc.
            if (entityType.BaseType is not null)
            {
                continue;
            }

            var clr = entityType.ClrType;

            if (typeof(ITenantEntity).IsAssignableFrom(clr))
            {
                var filter = TenantFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
            else if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var filter = SoftDeleteFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
        }
    }

    private static readonly System.Reflection.MethodInfo TenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private static readonly System.Reflection.MethodInfo SoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildSoftDeleteFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    // Đóng gói tham chiếu tới _tenant.TenantId — EF Core đánh giá lại mỗi truy vấn.
    private Expression<Func<TEntity, bool>> BuildTenantFilter<TEntity>()
        where TEntity : BaseEntity, ITenantEntity
        => e => e.TenantId == _tenant.TenantId && !e.IsDeleted;

    private static Expression<Func<TEntity, bool>> BuildSoftDeleteFilter<TEntity>()
        where TEntity : BaseEntity
        => e => !e.IsDeleted;
}

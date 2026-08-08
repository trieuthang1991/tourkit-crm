namespace TourKit.Infrastructure.Persistence;

/// <summary>Chọn hệ quản trị CSDL (section <c>Database</c>). Mã nghiệp vụ không phụ thuộc provider.</summary>
public sealed class DatabaseOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Database";

    /// <summary>Tên chuỗi kết nối trong <c>ConnectionStrings</c>.</summary>
    public const string ConnectionName = "Default";

    /// <summary><c>Sqlite</c> | <c>Postgres</c> | <c>SqlServer</c>.</summary>
    public string Provider { get; set; } = "Sqlite";

    /// <summary>Provider này có phải Postgres không (so sánh không phân biệt hoa thường).</summary>
    public bool IsPostgres => Is("Postgres") || Is("PostgreSql");

    /// <summary>Provider này có phải SQL Server không.</summary>
    public bool IsSqlServer => Is("SqlServer");

    private bool Is(string name) => string.Equals(Provider, name, StringComparison.OrdinalIgnoreCase);
}

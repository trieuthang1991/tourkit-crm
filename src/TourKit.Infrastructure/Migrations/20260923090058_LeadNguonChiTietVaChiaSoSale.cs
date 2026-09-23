using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeadNguonChiTietVaChiaSoSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttributionJson",
                table: "Leads",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Leads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignMode",
                table: "LeadCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AssigneesJson",
                table: "LeadCampaigns",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "LeadCampaigns",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // Sinh mã cho các chiến dịch ĐÃ CÓ trước khi dựng chỉ mục duy nhất.
            //
            // Không có bước này thì migration VỠ ngay trên cơ sở dữ liệu có sẵn: cột Code vừa thêm
            // mang giá trị mặc định là chuỗi rỗng cho mọi dòng, nên từ chiến dịch thứ hai trở đi là
            // trùng khoá. Đánh số lại theo từng công ty và từng năm, thứ tự theo lúc tạo.
            migrationBuilder.Sql("""
                UPDATE "LeadCampaigns" c
                SET "Code" = x.ma
                FROM (
                    SELECT "Id",
                           'CD-' || to_char("CreatedAt", 'YYYY') || '-' ||
                           lpad((row_number() OVER (
                               PARTITION BY "TenantId", date_part('year', "CreatedAt")
                               ORDER BY "CreatedAt", "Id"))::text, 3, '0') AS ma
                    FROM "LeadCampaigns"
                ) x
                WHERE c."Id" = x."Id" AND (c."Code" IS NULL OR c."Code" = '');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LeadCampaigns_TenantId_Code",
                table: "LeadCampaigns",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeadCampaigns_TenantId_Code",
                table: "LeadCampaigns");

            migrationBuilder.DropColumn(
                name: "AttributionJson",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AssignMode",
                table: "LeadCampaigns");

            migrationBuilder.DropColumn(
                name: "AssigneesJson",
                table: "LeadCampaigns");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "LeadCampaigns");
        }
    }
}

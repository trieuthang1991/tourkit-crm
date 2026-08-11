using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaiDungMaDanhMucNccVaiTroDaiLy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Providers_TenantId_Code",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_Name",
                table: "Roles",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TenantId_Code",
                table: "Providers",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Providers_TenantId_Code",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_Name",
                table: "Roles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TenantId_Code",
                table: "Providers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }
    }
}

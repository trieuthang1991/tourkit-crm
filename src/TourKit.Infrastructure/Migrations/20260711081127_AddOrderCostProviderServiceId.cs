using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCostProviderServiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderServiceId",
                table: "OrderCosts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCosts_TenantId_ProviderServiceId",
                table: "OrderCosts",
                columns: new[] { "TenantId", "ProviderServiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderCosts_TenantId_ProviderServiceId",
                table: "OrderCosts");

            migrationBuilder.DropColumn(
                name: "ProviderServiceId",
                table: "OrderCosts");
        }
    }
}

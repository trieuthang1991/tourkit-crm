using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AccountHolder = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    TransferNote = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAccounts_TenantId_Name",
                table: "PaymentAccounts",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAccounts");
        }
    }
}

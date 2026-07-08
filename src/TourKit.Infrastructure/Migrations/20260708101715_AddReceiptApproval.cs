using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptVoucherId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptApprovalStepUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptApprovalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptVoucherId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ActedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptApprovalStepUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptApprovals_TenantId_ReceiptVoucherId",
                table: "ReceiptApprovals",
                columns: new[] { "TenantId", "ReceiptVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptApprovalStepUsers_TenantId_ReceiptApprovalId_StepOrder",
                table: "ReceiptApprovalStepUsers",
                columns: new[] { "TenantId", "ReceiptApprovalId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptApprovalStepUsers_TenantId_ReceiptVoucherId",
                table: "ReceiptApprovalStepUsers",
                columns: new[] { "TenantId", "ReceiptVoucherId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptApprovals");

            migrationBuilder.DropTable(
                name: "ReceiptApprovalStepUsers");
        }
    }
}

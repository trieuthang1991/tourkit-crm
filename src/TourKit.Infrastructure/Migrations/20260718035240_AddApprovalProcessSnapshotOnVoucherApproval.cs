using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalProcessSnapshotOnVoucherApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalProcessId",
                table: "ReceiptApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalProcessId",
                table: "PaymentApprovals",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalProcessId",
                table: "ReceiptApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalProcessId",
                table: "PaymentApprovals");
        }
    }
}

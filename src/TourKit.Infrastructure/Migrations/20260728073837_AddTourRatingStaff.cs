using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourRatingStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperatorUserId",
                table: "TourRatings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesUserId",
                table: "TourRatings",
                type: "uuid",
                nullable: true);

            // Backfill NVPT (nhân viên phụ trách) từ đơn liên kết — để đánh giá cũ hiện đúng người phụ trách.
            migrationBuilder.Sql(
                "UPDATE \"TourRatings\" tr SET \"SalesUserId\" = o.\"SalesUserId\" " +
                "FROM \"Orders\" o WHERE tr.\"OrderId\" = o.\"Id\" AND tr.\"SalesUserId\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatorUserId",
                table: "TourRatings");

            migrationBuilder.DropColumn(
                name: "SalesUserId",
                table: "TourRatings");
        }
    }
}

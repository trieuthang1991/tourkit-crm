using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVisaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VisaReceiveDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VisaReturnDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisaStatus",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VisaSubmitDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill quy trình visa cho đơn Visa (BookingType=5) đã tồn tại — để cột visa có dữ liệu test.
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" SET
                  ""VisaReceiveDate"" = date_trunc('day', ""CreatedAt""),
                  ""VisaSubmitDate""  = date_trunc('day', ""CreatedAt"") + ((1 + (abs(hashtext(""Code""))%5)) || ' days')::interval,
                  ""VisaReturnDate""  = date_trunc('day', ""CreatedAt"") + ((7 + (abs(hashtext(""Code""))%20)) || ' days')::interval,
                  ""VisaStatus""      = abs(hashtext(""Code""))%5
                WHERE ""BookingType"" = 5 AND ""VisaStatus"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisaReceiveDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VisaReturnDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VisaStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VisaSubmitDate",
                table: "Orders");
        }
    }
}

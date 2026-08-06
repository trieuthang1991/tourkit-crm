using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTaskCodeStartProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "WorkTasks",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "WorkTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                table: "WorkTasks",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill Mã Task cho việc đã tồn tại (idiom "CV-XXXXXXXX") để không hiện "—" hàng loạt.
            migrationBuilder.Sql(
                "UPDATE \"WorkTasks\" SET \"Code\" = 'CV-' || upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8)) WHERE \"Code\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "WorkTasks");
        }
    }
}

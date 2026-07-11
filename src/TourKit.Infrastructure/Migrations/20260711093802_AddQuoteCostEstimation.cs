using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteCostEstimation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Adults",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ChildPercent",
                table: "Quotes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Children",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "InfantPercent",
                table: "Quotes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Infants",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalCost",
                table: "Quotes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalProfit",
                table: "Quotes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MarginPercent",
                table: "QuoteLines",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderServiceId",
                table: "QuoteLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "QuoteLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "QuoteLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "UnitCost",
                table: "QuoteLines",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adults",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ChildPercent",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Children",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InfantPercent",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Infants",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TotalProfit",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "MarginPercent",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "ProviderServiceId",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "QuoteLines");
        }
    }
}

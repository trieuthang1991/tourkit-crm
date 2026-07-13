using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Pnr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MarketRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TourType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    DepartureDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false),
                    OrderRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ItineraryJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightTickets_TenantId_Pnr",
                table: "FlightTickets",
                columns: new[] { "TenantId", "Pnr" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightTickets");
        }
    }
}

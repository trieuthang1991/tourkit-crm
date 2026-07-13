using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightTicketIndividual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightTicketIndividuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TicketCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Pnr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TripType = table.Column<int>(type: "integer", nullable: false),
                    Route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DepartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SellAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentDueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssigneeRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightTicketIndividuals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightTicketIndividuals_TenantId_Code",
                table: "FlightTicketIndividuals",
                columns: new[] { "TenantId", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightTicketIndividuals");
        }
    }
}

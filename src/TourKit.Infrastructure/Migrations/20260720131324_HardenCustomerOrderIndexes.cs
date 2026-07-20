using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenCustomerOrderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlightTickets_TenantId_Pnr",
                table: "FlightTickets");

            migrationBuilder.DropIndex(
                name: "IX_FlightTicketIndividuals_TenantId_Code",
                table: "FlightTicketIndividuals");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents");

            migrationBuilder.AlterColumn<decimal>(
                name: "TempBalance",
                table: "Customers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Code",
                table: "Orders",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightTickets_TenantId_Pnr",
                table: "FlightTickets",
                columns: new[] { "TenantId", "Pnr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightTicketIndividuals_TenantId_Code",
                table: "FlightTicketIndividuals",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Code",
                table: "Customers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Email",
                table: "Customers",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Phone",
                table: "Customers",
                columns: new[] { "TenantId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_Code",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_FlightTickets_TenantId_Pnr",
                table: "FlightTickets");

            migrationBuilder.DropIndex(
                name: "IX_FlightTicketIndividuals_TenantId_Code",
                table: "FlightTicketIndividuals");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Code",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents");

            migrationBuilder.AlterColumn<decimal>(
                name: "TempBalance",
                table: "Customers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_FlightTickets_TenantId_Pnr",
                table: "FlightTickets",
                columns: new[] { "TenantId", "Pnr" });

            migrationBuilder.CreateIndex(
                name: "IX_FlightTicketIndividuals_TenantId_Code",
                table: "FlightTicketIndividuals",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents",
                columns: new[] { "TenantId", "Code" });
        }
    }
}

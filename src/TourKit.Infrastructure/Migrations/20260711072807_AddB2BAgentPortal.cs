using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddB2BAgentPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentQuoteRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TravelDate = table.Column<long>(type: "INTEGER", nullable: true),
                    ReturnDate = table.Column<long>(type: "INTEGER", nullable: true),
                    PaxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialRequests = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    QuotedAmount = table.Column<double>(type: "REAL", nullable: true),
                    QuotedNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentQuoteRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TaxCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreditLimit = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQuoteRequests_TenantId_AgentId",
                table: "AgentQuoteRequests",
                columns: new[] { "TenantId", "AgentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQuoteRequests_TenantId_Status",
                table: "AgentQuoteRequests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Code",
                table: "Agents",
                columns: new[] { "TenantId", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentQuoteRequests");

            migrationBuilder.DropTable(
                name: "Agents");
        }
    }
}

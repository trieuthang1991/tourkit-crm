using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourItineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TourId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourItineraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TourType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DepartureDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TotalSlots = table.Column<int>(type: "INTEGER", nullable: false),
                    PickupPlace = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    DropoffPlace = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    TransportMode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ParentTourId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourDepartureFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AmountAdults = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountChildren = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDepartureFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourDepartureFields_Tours_Id",
                        column: x => x.Id,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourTemplateFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationHours = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceAdult = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceChildSmall = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceBaby = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TermsNote = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TermsNoteEn = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourTemplateFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourTemplateFields_Tours_Id",
                        column: x => x.Id,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourDepartureFields_IsClosed",
                table: "TourDepartureFields",
                column: "IsClosed");

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraries_TenantId_TourId_DayIndex",
                table: "TourItineraries",
                columns: new[] { "TenantId", "TourId", "DayIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_Code",
                table: "Tours",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_DepartureDate",
                table: "Tours",
                columns: new[] { "TenantId", "DepartureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_Kind_Status",
                table: "Tours",
                columns: new[] { "TenantId", "Kind", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourDepartureFields");

            migrationBuilder.DropTable(
                name: "TourItineraries");

            migrationBuilder.DropTable(
                name: "TourTemplateFields");

            migrationBuilder.DropTable(
                name: "Tours");
        }
    }
}

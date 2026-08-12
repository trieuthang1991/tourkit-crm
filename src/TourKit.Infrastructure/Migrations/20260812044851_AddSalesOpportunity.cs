using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOpportunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpportunityStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdultQty = table.Column<int>(type: "integer", nullable: false),
                    ChildQty = table.Column<int>(type: "integer", nullable: false),
                    ChildSmallQty = table.Column<int>(type: "integer", nullable: false),
                    BabyQty = table.Column<int>(type: "integer", nullable: false),
                    PriceAdult = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceChildSmall = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceBaby = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TourDepartureId = table.Column<Guid>(type: "uuid", nullable: true),
                    StageCode = table.Column<int>(type: "integer", nullable: false),
                    CancelReasonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentIds = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOpportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOpportunityAssignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFollower = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOpportunityAssignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOpportunityAssignees_SalesOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "SalesOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityStages_TenantId_Code",
                table: "OpportunityStages",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunities_TenantId_Code",
                table: "SalesOpportunities",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunities_TenantId_CreatedAt",
                table: "SalesOpportunities",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunities_TenantId_CreatedByUserId",
                table: "SalesOpportunities",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunities_TenantId_CustomerId",
                table: "SalesOpportunities",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunities_TenantId_StageCode",
                table: "SalesOpportunities",
                columns: new[] { "TenantId", "StageCode" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunityAssignees_OpportunityId_UserId_IsFollower",
                table: "SalesOpportunityAssignees",
                columns: new[] { "OpportunityId", "UserId", "IsFollower" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunityAssignees_TenantId_OpportunityId",
                table: "SalesOpportunityAssignees",
                columns: new[] { "TenantId", "OpportunityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOpportunityAssignees_TenantId_UserId_IsFollower",
                table: "SalesOpportunityAssignees",
                columns: new[] { "TenantId", "UserId", "IsFollower" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityStages");

            migrationBuilder.DropTable(
                name: "SalesOpportunityAssignees");

            migrationBuilder.DropTable(
                name: "SalesOpportunities");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChiMucLeadTheoChienDich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_CampaignId",
                table: "Leads",
                columns: new[] { "TenantId", "CampaignId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_CampaignId",
                table: "Leads");
        }
    }
}

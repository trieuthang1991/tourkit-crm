using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilterCatalogUniqueIndexesByIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourGroups_TenantId_Name",
                table: "TourGroups");

            migrationBuilder.DropIndex(
                name: "IX_Surcharges_TenantId_Name",
                table: "Surcharges");

            migrationBuilder.DropIndex(
                name: "IX_ServiceItems_TenantId_Code",
                table: "ServiceItems");

            migrationBuilder.DropIndex(
                name: "IX_RoomClasses_TenantId_Name",
                table: "RoomClasses");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_Positions_TenantId_Name",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTerms_TenantId_Name",
                table: "PaymentTerms");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAccounts_TenantId_Name",
                table: "PaymentAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LanguageTypes_TenantId_Name",
                table: "LanguageTypes");

            migrationBuilder.DropIndex(
                name: "IX_Departments_TenantId_Name",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTypes_TenantId_Code",
                table: "CustomerTypes");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTags_TenantId_Name",
                table: "CustomerTags");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSources_TenantId_Name",
                table: "CustomerSources");

            migrationBuilder.DropIndex(
                name: "IX_Currencies_TenantId_Code",
                table: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_CarTypes_TenantId_Code",
                table: "CarTypes");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantId_Name",
                table: "Branches");

            migrationBuilder.CreateIndex(
                name: "IX_TourGroups_TenantId_Name",
                table: "TourGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_TenantId_Name",
                table: "Surcharges",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_TenantId_Code",
                table: "ServiceItems",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_RoomClasses_TenantId_Name",
                table: "RoomClasses",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_TenantId_Name",
                table: "Positions",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerms_TenantId_Name",
                table: "PaymentTerms",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAccounts_TenantId_Name",
                table: "PaymentAccounts",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypes_TenantId_Name",
                table: "LanguageTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_Name",
                table: "Departments",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTypes_TenantId_Code",
                table: "CustomerTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTags_TenantId_Name",
                table: "CustomerTags",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSources_TenantId_Name",
                table: "CustomerSources",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_TenantId_Code",
                table: "Currencies",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CarTypes_TenantId_Code",
                table: "CarTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_Name",
                table: "Branches",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourGroups_TenantId_Name",
                table: "TourGroups");

            migrationBuilder.DropIndex(
                name: "IX_Surcharges_TenantId_Name",
                table: "Surcharges");

            migrationBuilder.DropIndex(
                name: "IX_ServiceItems_TenantId_Code",
                table: "ServiceItems");

            migrationBuilder.DropIndex(
                name: "IX_RoomClasses_TenantId_Name",
                table: "RoomClasses");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_Positions_TenantId_Name",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTerms_TenantId_Name",
                table: "PaymentTerms");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAccounts_TenantId_Name",
                table: "PaymentAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LanguageTypes_TenantId_Name",
                table: "LanguageTypes");

            migrationBuilder.DropIndex(
                name: "IX_Departments_TenantId_Name",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTypes_TenantId_Code",
                table: "CustomerTypes");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTags_TenantId_Name",
                table: "CustomerTags");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSources_TenantId_Name",
                table: "CustomerSources");

            migrationBuilder.DropIndex(
                name: "IX_Currencies_TenantId_Code",
                table: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_CarTypes_TenantId_Code",
                table: "CarTypes");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantId_Name",
                table: "Branches");

            migrationBuilder.CreateIndex(
                name: "IX_TourGroups_TenantId_Name",
                table: "TourGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_TenantId_Name",
                table: "Surcharges",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_TenantId_Code",
                table: "ServiceItems",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomClasses_TenantId_Name",
                table: "RoomClasses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_TenantId_Name",
                table: "Positions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerms_TenantId_Name",
                table: "PaymentTerms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAccounts_TenantId_Name",
                table: "PaymentAccounts",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LanguageTypes_TenantId_Name",
                table: "LanguageTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_Name",
                table: "Departments",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTypes_TenantId_Code",
                table: "CustomerTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTags_TenantId_Name",
                table: "CustomerTags",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSources_TenantId_Name",
                table: "CustomerSources",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_TenantId_Code",
                table: "Currencies",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarTypes_TenantId_Code",
                table: "CarTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_Name",
                table: "Branches",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }
    }
}

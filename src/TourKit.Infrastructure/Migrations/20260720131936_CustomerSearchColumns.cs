using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSearchColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNormalized",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchName",
                table: "Customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_PhoneNormalized",
                table: "Customers",
                columns: new[] { "TenantId", "PhoneNormalized" });

            // Postgres-only: extension bỏ dấu + trigram, backfill 2 cột cho data hiện có, GIN trigram cho SearchName.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
                migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
                migrationBuilder.Sql("""UPDATE "Customers" SET "SearchName" = lower(unaccent("FullName")) WHERE "SearchName" IS NULL;""");
                // NULLIF (không phải regexp_replace(...,NULL) strict); '^84(\d{8,})$' → '0\1' khớp logic C# (tổng >9 số).
                migrationBuilder.Sql("""UPDATE "Customers" SET "PhoneNormalized" = NULLIF(regexp_replace(regexp_replace("Phone", '\D', '', 'g'), '^84(\d{8,})$', '0\1'), '') WHERE "Phone" IS NOT NULL;""");
                migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Customers_SearchName_trgm" ON "Customers" USING gin ("SearchName" gin_trgm_ops);""");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Customers_SearchName_trgm";""");
            }

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_PhoneNormalized",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PhoneNormalized",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SearchName",
                table: "Customers");
        }
    }
}

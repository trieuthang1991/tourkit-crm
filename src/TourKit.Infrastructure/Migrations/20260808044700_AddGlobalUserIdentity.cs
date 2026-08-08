using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            if (migrationBuilder.ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT UPPER(TRIM("Email"))
                            FROM "Users"
                            GROUP BY UPPER(TRIM("Email"))
                            HAVING COUNT(*) > 1
                        ) THEN
                            RAISE EXCEPTION 'Cannot enforce globally unique user email: duplicate normalized emails exist.';
                        END IF;
                    END $$;

                    UPDATE "Users"
                    SET "NormalizedEmail" = UPPER(TRIM("Email"));
                    """);
            }
            else if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    IF EXISTS (
                        SELECT UPPER(LTRIM(RTRIM([Email])))
                        FROM [Users]
                        GROUP BY UPPER(LTRIM(RTRIM([Email])))
                        HAVING COUNT(*) > 1
                    )
                        THROW 50001, 'Cannot enforce globally unique user email: duplicate normalized emails exist.', 1;

                    UPDATE [Users]
                    SET [NormalizedEmail] = UPPER(LTRIM(RTRIM([Email])));
                    """);
            }
            else if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    CREATE TEMP TABLE "__GlobalUserEmailPreflight" (
                        "DuplicateCount" INTEGER NOT NULL CHECK ("DuplicateCount" = 0)
                    );

                    INSERT INTO "__GlobalUserEmailPreflight" ("DuplicateCount")
                    SELECT COUNT(*)
                    FROM "Users"
                    GROUP BY UPPER(TRIM("Email"))
                    HAVING COUNT(*) > 1;

                    DROP TABLE "__GlobalUserEmailPreflight";

                    UPDATE "Users"
                    SET "NormalizedEmail" = UPPER(TRIM("Email"));
                    """);
            }
            else
            {
                throw new NotSupportedException(
                    $"Provider '{migrationBuilder.ActiveProvider}' is not supported by this migration.");
            }

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldDefaultValue: "");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateTable(
                name: "UserExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_Provider_ProviderSubject",
                table: "UserExternalLogins",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_UserId_Provider",
                table: "UserExternalLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }
    }
}

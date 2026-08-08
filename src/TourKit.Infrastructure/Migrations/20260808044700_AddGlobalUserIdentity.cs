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
            var isNpgsql = migrationBuilder.ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
            var isSqlServer = migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            var isSqlite = migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

            if (!isNpgsql && !isSqlServer && !isSqlite)
            {
                throw new NotSupportedException(
                    $"Provider '{migrationBuilder.ActiveProvider}' is not supported by this migration.");
            }

            var guidType = isNpgsql ? "uuid" : isSqlServer ? "uniqueidentifier" : "TEXT";
            var string64Type = isNpgsql ? "character varying(64)" : isSqlServer ? "nvarchar(64)" : "TEXT";
            var string256Type = isNpgsql ? "character varying(256)" : isSqlServer ? "nvarchar(256)" : "TEXT";
            var string512Type = isNpgsql ? "character varying(512)" : isSqlServer ? "nvarchar(512)" : "TEXT";
            var dateTimeOffsetType = isNpgsql ? "timestamp with time zone" : isSqlServer ? "datetimeoffset" : "INTEGER";
            var booleanType = isNpgsql ? "boolean" : isSqlServer ? "bit" : "INTEGER";

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: string256Type,
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            if (isNpgsql)
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
            else if (isSqlServer)
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
            else
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
            if (!isSqlite)
            {
                migrationBuilder.AlterColumn<string>(
                    name: "NormalizedEmail",
                    table: "Users",
                    type: string256Type,
                    maxLength: 256,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: string256Type,
                    oldMaxLength: 256,
                    oldDefaultValue: "");
            }

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            if (isSqlite)
            {
                migrationBuilder.Sql(
                    """
                    PRAGMA foreign_keys = 0;

                    DROP TABLE IF EXISTS "ef_temp_Users";

                    CREATE TABLE "ef_temp_Users" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                        "CreatedAt" INTEGER NOT NULL,
                        "DepartmentId" TEXT NULL,
                        "Email" TEXT NOT NULL,
                        "FullName" TEXT NOT NULL,
                        "IsActive" INTEGER NOT NULL,
                        "IsDeleted" INTEGER NOT NULL,
                        "LastLoginAt" INTEGER NULL,
                        "NormalizedEmail" TEXT NOT NULL,
                        "PasswordHash" TEXT NOT NULL,
                        "PositionId" TEXT NULL,
                        "TenantId" TEXT NOT NULL,
                        "UpdatedAt" INTEGER NULL
                    );

                    INSERT INTO "ef_temp_Users" (
                        "Id", "CreatedAt", "DepartmentId", "Email", "FullName", "IsActive", "IsDeleted",
                        "LastLoginAt", "NormalizedEmail", "PasswordHash", "PositionId", "TenantId", "UpdatedAt"
                    )
                    SELECT
                        "Id", "CreatedAt", "DepartmentId", "Email", "FullName", "IsActive", "IsDeleted",
                        "LastLoginAt", "NormalizedEmail", "PasswordHash", "PositionId", "TenantId", "UpdatedAt"
                    FROM "Users";

                    DROP TABLE "Users";
                    ALTER TABLE "ef_temp_Users" RENAME TO "Users";

                    PRAGMA foreign_keys = 1;
                    """,
                    suppressTransaction: true);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateTable(
                name: "UserExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    TenantId = table.Column<Guid>(type: guidType, nullable: false),
                    UserId = table.Column<Guid>(type: guidType, nullable: false),
                    Provider = table.Column<string>(type: string64Type, maxLength: 64, nullable: false),
                    ProviderSubject = table.Column<string>(type: string512Type, maxLength: 512, nullable: false),
                    ProviderEmail = table.Column<string>(type: string256Type, maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: dateTimeOffsetType, nullable: true),
                    IsDeleted = table.Column<bool>(type: booleanType, nullable: false)
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
            var isSqlite = migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            if (isSqlite)
            {
                migrationBuilder.Sql(
                    """
                    PRAGMA foreign_keys = 0;

                    DROP TABLE IF EXISTS "ef_temp_Users";

                    CREATE TABLE "ef_temp_Users" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                        "CreatedAt" INTEGER NOT NULL,
                        "DepartmentId" TEXT NULL,
                        "Email" TEXT NOT NULL,
                        "FullName" TEXT NOT NULL,
                        "IsActive" INTEGER NOT NULL,
                        "IsDeleted" INTEGER NOT NULL,
                        "LastLoginAt" INTEGER NULL,
                        "PasswordHash" TEXT NOT NULL,
                        "PositionId" TEXT NULL,
                        "TenantId" TEXT NOT NULL,
                        "UpdatedAt" INTEGER NULL
                    );

                    INSERT INTO "ef_temp_Users" (
                        "Id", "CreatedAt", "DepartmentId", "Email", "FullName", "IsActive", "IsDeleted",
                        "LastLoginAt", "PasswordHash", "PositionId", "TenantId", "UpdatedAt"
                    )
                    SELECT
                        "Id", "CreatedAt", "DepartmentId", "Email", "FullName", "IsActive", "IsDeleted",
                        "LastLoginAt", "PasswordHash", "PositionId", "TenantId", "UpdatedAt"
                    FROM "Users";

                    DROP TABLE "Users";
                    ALTER TABLE "ef_temp_Users" RENAME TO "Users";

                    PRAGMA foreign_keys = 1;
                    """,
                    suppressTransaction: true);
            }
            else
            {
                migrationBuilder.DropColumn(
                    name: "NormalizedEmail",
                    table: "Users");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPassport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customers",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DateOfBirth",
                table: "Customers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCardNumber",
                table: "Customers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Customers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PassportExpiry",
                table: "Customers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Customers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IdCardNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PassportExpiry",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Customers");
        }
    }
}

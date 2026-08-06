using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Tours",
                type: "integer",
                nullable: true);

            // Backfill phân loại sản phẩm cho CHUYẾN (Kind=1 Departure). Trải FIT/GIT/LandTour/Dịch vụ/Visa
            // theo mã (deterministic) để màn "quản lý chuyến theo loại" có dữ liệu test.
            migrationBuilder.Sql(@"
                UPDATE ""Tours"" SET ""Category"" = (ARRAY[0,0,0,1,1,2,2,4,5])[1 + (abs(hashtext(""Code""))%9)]
                WHERE ""Kind"" = 2 AND ""Category"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tours");
        }
    }
}

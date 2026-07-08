using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourKit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TourDepartureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalRefund = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ApprovedRevenue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsPaymentRecognized = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Partner = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReceiverName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRecognized = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TourDepartureId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountChildren = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountChildrenSmall = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityBaby = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceAdult = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceChildSmall = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PriceBaby = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Surcharge = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildSurcharge = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildSurchargeSmall = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BabySurcharge = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildDiscount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildDiscountSmall = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BabyDiscount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Commission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildCommission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChildCommissionSmall = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BabyCommission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UpfrontAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ReservationCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SeatSelected = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsMainContact = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourCustomers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_CustomerId",
                table: "Orders",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status",
                table: "Orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_TourDepartureId",
                table: "Orders",
                columns: new[] { "TenantId", "TourDepartureId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_TenantId_OrderId",
                table: "ReceiptVouchers",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptVouchers_TenantId_ParentId",
                table: "ReceiptVouchers",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourCustomers_TenantId_CustomerId",
                table: "TourCustomers",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourCustomers_TenantId_OrderId",
                table: "TourCustomers",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TourCustomers_TenantId_TourDepartureId",
                table: "TourCustomers",
                columns: new[] { "TenantId", "TourDepartureId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ReceiptVouchers");

            migrationBuilder.DropTable(
                name: "TourCustomers");
        }
    }
}

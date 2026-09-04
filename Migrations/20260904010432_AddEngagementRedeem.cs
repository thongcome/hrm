using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementRedeem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Eng_Recognition",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Eng_RedeemItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    StockQty = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_RedeemItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eng_RedeemRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    RedeemItemId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PointsSpent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FulfilledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eng_RedeemRequest", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eng_RedeemItem_CompanyId",
                table: "Eng_RedeemItem",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_RedeemRequest_CompanyId",
                table: "Eng_RedeemRequest",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Eng_RedeemRequest_HremployeeId",
                table: "Eng_RedeemRequest",
                column: "HremployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eng_RedeemItem");

            migrationBuilder.DropTable(
                name: "Eng_RedeemRequest");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Eng_Recognition");
        }
    }
}

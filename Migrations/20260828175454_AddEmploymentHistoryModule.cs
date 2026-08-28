using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentHistoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hrd_EmploymentHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortNo = table.Column<int>(type: "int", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PositionStatus = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsLatestPosition = table.Column<bool>(type: "bit", nullable: false),
                    IsPositionChanged = table.Column<bool>(type: "bit", nullable: false),
                    NewPosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    NewPositionName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    NewOrganizationName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldPosExecTypeId = table.Column<long>(type: "bigint", nullable: true),
                    OldPositionName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    OldOrganizationName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hrd_EmploymentHistory", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hrd_EmploymentHistory");
        }
    }
}

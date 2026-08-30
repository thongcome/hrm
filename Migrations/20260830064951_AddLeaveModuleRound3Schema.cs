using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveModuleRound3Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarryOverExpiryMonths",
                table: "Lve_LeavePolicy",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinServiceMonths",
                table: "Lve_LeavePolicy",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Lve_BlockLeavePolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    MinConsecutiveWorkingDays = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lve_BlockLeavePolicy", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lve_BlockLeavePolicy");

            migrationBuilder.DropColumn(
                name: "CarryOverExpiryMonths",
                table: "Lve_LeavePolicy");

            migrationBuilder.DropColumn(
                name: "MinServiceMonths",
                table: "Lve_LeavePolicy");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfMeritSalaryHistoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Links an applied merit increase to the exact
            // Pay_PositionSalaryHistory row it created, so the
            // คำสั่งขึ้นเงินเดือน (salary increase order) PDF can pull
            // OldSalary/NewSalary/OrderNo/OrderDate from one authoritative
            // row instead of re-matching by employee+date. Nullable, no FK
            // constraint — same soft-link convention as HremployeeId on the
            // same table.
            migrationBuilder.AddColumn<long>(
                name: "MeritSalaryHistoryId",
                table: "Perf_EvaluationInstance",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeritSalaryHistoryId",
                table: "Perf_EvaluationInstance");
        }
    }
}

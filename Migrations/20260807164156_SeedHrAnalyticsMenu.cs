using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedHrAnalyticsMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-verified against live DB immediately before writing this:
            // menuid max=50, rolemenuid max=54.

            // Single menucode HR_ANALYTICS reused across both report pages
            // (same pattern as PAY_REPORTS/ENG_ADMIN) — Admin role only.
            // Not reusing PAY_REPORTS itself: turnover/absenteeism span
            // Hremployee/Hrd_*/Att_* data, not payroll-specific.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 51, "อัตราการลาออก (Turnover)", "Turnover Report", 1, true, 52, "HR_ANALYTICS", true, "/hr/reports/turnover", 1, true },
                    { 52, "รายงานการขาดงาน (Absenteeism)", "Absenteeism Report", 1, true, 53, "HR_ANALYTICS", true, "/hr/reports/absenteeism", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 55, 51, 9, true },
                    { 56, 52, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 55 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 56 });

            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 51 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 52 });
        }
    }
}

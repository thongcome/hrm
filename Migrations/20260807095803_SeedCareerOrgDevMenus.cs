using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedCareerOrgDevMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CAREER_ADMIN (Admin-only, /career/paths) and CAREER_ACCESS
            // (Admin + Employee, /career/my-path — also covers
            // /career/internal-jobs, same policy) for Career Management;
            // ORGDEV_ADMIN (Admin-only, /orgdev/dashboard — covers all 5
            // Organization Development pages) for Org Dev. menuid 47-49 =
            // next after 46 (SeedSuccessionMenu), rolemenuid 50-53 = next
            // after 49 (both verified against live DB before writing this).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 47, "จัดการ Career Path", "Career Path Admin", 1, true, 57, "CAREER_ADMIN", true, "/career/paths", 1, true },
                    { 48, "เส้นทางความก้าวหน้า/สมัครงานภายใน", "Career Management", 1, true, 58, "CAREER_ACCESS", true, "/career/my-path", 1, true },
                    { 49, "Organization Development", "Organization Development", 1, true, 59, "ORGDEV_ADMIN", true, "/orgdev/dashboard", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 50, 47, 9, true },  // CAREER_ADMIN -> Admin
                    { 51, 48, 9, true },  // CAREER_ACCESS -> Admin
                    { 52, 48, 10, true }, // CAREER_ACCESS -> Employee
                    { 53, 49, 9, true },  // ORGDEV_ADMIN -> Admin
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 50 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 51 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 52 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 53 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 47 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 48 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 49 });
        }
    }
}

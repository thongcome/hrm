using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedIdpMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 41-42 = next after 40; rolemenuid 42-44 = next after 41
            // (all verified against live DB before writing this).
            // IDP_ACCESS -> /idp/my-plans, granted to both Employee (10, own
            // competency+plans) and Admin (9, testing) — same pattern as
            // PERF_ACCESS/ESS_ACCESS. /idp/team and /idp/plans/{id} share
            // this same policy without their own sc_menu row (mirrors
            // JobProfileDetail.razor's Menu:JOBCOMP_ADMIN reuse).
            // IDP_ADMIN -> /idp/hr-overview, Admin (9) only.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 41, "สมรรถนะ + แผนพัฒนาของฉัน", "My Competencies & IDP", 1, true, 51, "IDP_ACCESS", true, "/idp/my-plans", 1, true },
                    { 42, "ภาพรวม IDP (HR)", "IDP HR Overview", 1, true, 52, "IDP_ADMIN", true, "/idp/hr-overview", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 42, 41, 10, true },
                    { 43, 41, 9, true },
                    { 44, 42, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 42 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 43 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 44 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 41 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 42 });
        }
    }
}

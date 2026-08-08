using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLmsMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 53-54 = next after 52; rolemenuid 57-59 = next after 56
            // (all verified against live DB before writing this).
            // LMS_ADMIN -> /lms/dashboard, Admin (9) only — the other admin
            // pages (categories/courses/sessions/enrollments/training-needs/
            // training-budget) share this same policy without their own
            // sc_menu row (mirrors JobProfileDetail.razor's Menu:JOBCOMP_ADMIN
            // reuse). LMS_ACCESS -> /ess/lms/catalog, granted to both
            // Employee (10, own enrollments) and Admin (9, testing) — same
            // pattern as ESS_ACCESS/IDP_ACCESS/PERF_ACCESS. /ess/lms/my-training
            // and /ess/lms/quiz/{id} share this same policy too.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 53, "จัดการฝึกอบรม (LMS)", "LMS Admin", 1, true, 60, "LMS_ADMIN", true, "/lms/dashboard", 1, true },
                    { 54, "หลักสูตรอบรมของฉัน", "My Training", 1, true, 61, "LMS_ACCESS", true, "/ess/lms/catalog", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 57, 53, 9, true },
                    { 58, 54, 9, true },
                    { 59, 54, 10, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 57 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 58 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 59 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 53 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 54 });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedInfoMessageAnnouncementMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 29/30 = next after 28; rolemenuid 28/29/30 = next after 27
            // (all re-verified against live DB immediately before writing this).
            // HR_ANNOUNCEMENT_ACCESS (public list page) granted to both
            // Employee (10) and Admin (9) — HR staff should see company
            // announcements too, not just employees. HR_ANNOUNCEMENT_ADMIN
            // (content/config page) granted to Admin (9) only.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 29, "ประชาสัมพันธ์", "Announcements", 1, true, 39, "HR_ANNOUNCE_ACCESS", true, "/hr/announcements", 1, true },
                    { 30, "จัดการประกาศ", "Manage Announcements", 1, true, 40, "HR_ANNOUNCE_ADMIN", true, "/hr/announcements/admin", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 28, 29, 10, true },
                    { 29, 29, 9, true },
                    { 30, 30, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 28 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 29 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 30 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 29 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 30 });
        }
    }
}

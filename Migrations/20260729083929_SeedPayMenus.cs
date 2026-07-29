using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    // Registers the Pay_* module + Identity-linking admin page into the
    // existing sc_menu/sc_role_menu permission system, which had exactly one
    // entry total ("Role" -> /sc_roles) before this — the whole Pay_* module
    // was built and left running on bare [Authorize] with nothing wired into
    // the menu-based permission model the rest of the app already uses.
    // All four menus are granted to the Admin role (roleid 9) only for now;
    // reassign via the existing sc_role_menu admin pages as needed.
    /// <inheritdoc />
    public partial class SeedPayMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 3, "รอบเงินเดือน (ใหม่)", "Payroll Runs", 1, true, 2, "PAY_RUNS", true, "/pay/runs", 1, true },
                    { 4, "รายการเงินได้-เงินหักเฉพาะกิจ", "Ad-hoc Pay Items", 1, true, 3, "PAY_ADHOC", true, "/pay/adhoc", 1, true },
                    { 5, "ตั้งค่าสลิปเงินเดือน", "Payslip Settings", 1, true, 4, "PAY_ADMIN", true, "/pay/admin/payslip-settings", 1, true },
                    { 6, "ผูกบัญชี Identity", "Link Identity Account", 1, true, 5, "SYS_LINK_IDENTITY", true, "/admin/link-identity-account", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 2, 3, 9, true },
                    { 3, 4, 9, true },
                    { 4, 5, 9, true },
                    { 5, 6, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 2, 3, 4, 5 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 3, 4, 5, 6 });
        }
    }
}

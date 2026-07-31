using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddScRoleMenuCanEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canedit",
                table: "sc_role_menu",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // menuid 17/18 = next after 16 (ESS_ACCESS); roleid 9 = Admin;
            // rolemenuid 15/16 = next after 14 — all confirmed against live
            // DB, not guessed. Pilot pages for the reusable CRUD scaffold
            // (Block 0 of the Workflow Engine plan).
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 17, "จัดการพนักงาน (Workflow demo)", "Workflow Employee Admin", 1, true, 30, "WF_EMPLOYEE_ADMIN", true, "/wf/employees", 1, true },
                    { 18, "จัดการประเภทหน่วยงาน (Workflow demo)", "Workflow Org Type Admin", 1, true, 31, "WF_ORG_TYPE_ADMIN", true, "/wf/org-types", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive", "canedit" },
                values: new object[,]
                {
                    { 15, 17, 9, true, true },
                    { 16, 18, 9, true, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 15 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 16 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 17 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 18 });

            migrationBuilder.DropColumn(
                name: "canedit",
                table: "sc_role_menu");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeaveAccessMenuAndUnpaidPayItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LEAVE_ACCESS: lets /leave-requests and its detail/file-download
            // routes be reached by both roles — the page itself branches on
            // whether the caller ALSO holds the WF_WORKFLOW_ADMIN claim (admin
            // mode: pick any employee, see everyone's requests) or not (ESS
            // mode: locked to self via EssEmployeeResolver). Previously this
            // page was gated by Menu:WF_WORKFLOW_ADMIN alone, which is why no
            // regular employee could reach it at all.
            //
            // Live DB checked immediately before writing this (2026-08-29):
            // MAX(menuid)=60, MAX(rolemenuid)=67, MAX(Id) FROM
            // Pay_PayItemType=14 — next values below. roleid 9="admin",
            // roleid 10="emp" (Employee) confirmed from sc_role.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 61, "คำขอลางาน", "Leave Requests", 1, true, 21, "LEAVE_ACCESS", true, "/leave-requests", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 68, 61, 10, true }, // emp — self-service leave requests
                    { 69, 61, 9, true },  // admin — keeps existing HR-on-behalf-of workflow
                });

            // LEAVE_UNPAID: negative-sign pay item type consumed automatically
            // by PayrollCalculationService.cs whenever a Pay_AdhocPayItem
            // targets it — see LeaveRequestService.PushUnpaidToPayrollAsync,
            // which creates one such row per approved unpaid-leave request.
            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Code", "NameTh", "NameEn", "Category", "DefaultSignFlag", "IsSystemReserved", "IsActive", "SortOrder" },
                values: new object[] { 15, "LEAVE_UNPAID", "หักเงินลาไม่รับค่าจ้าง", "Unpaid Leave Deduction", 1, -1, false, true, 90 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Pay_PayItemType", keyColumn: "Id", keyValues: new object[] { 15 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 68 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 69 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 61 });
        }
    }
}

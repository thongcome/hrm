using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedEmployeeRelationsWorkflowAndMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-verified against live DB immediately before writing this:
            // workflowid max=14, subworkflowid max=22, wf_custom_user id
            // max=18, menuid max=34, rolemenuid max=35.

            // DISCIPLINARY_APPROVAL: 1 level, Custom User (ฝ่ายบุคคล/HR),
            // istop — same fixed-to-test_payroll placeholder approver as
            // EXPENSE_CLAIM_APPROVAL/OT_APPROVAL (no dedicated HR role/user
            // wired up yet in this environment).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 15, "อนุมัติวินัยพนักงาน", "ACTIVE", "DISCIPLINARY_APPROVAL", true, true, "อนุมัติกรณีวินัยพนักงาน (Hr_DisciplinaryCase) — ระดับเดียว ฝ่ายบุคคลอนุมัติ", "/hr/disciplinary/{refid}" });

            migrationBuilder.InsertData(
                table: "wf_sub_workflow_master",
                columns: new[]
                {
                    "subworkflowid", "workflowid", "wlevel", "subject",
                    "isAdhocUser", "iscustomApprover", "isupperrole", "isupperuser", "iscustomRole", "iscustomUser",
                    "iscondition", "isorcondition", "isandcondition",
                    "forwardstatus", "standstatus", "backwardstatus",
                    "istop", "isReturnSender", "isshow", "isLOA", "isAutoApproveAllow",
                    "isNeedBudgetApproval", "isPool", "isApproverSameOrg", "isApproverSameCostCenter", "isManualButton",
                },
                values: new object[,]
                {
                    { 23, 15, 1, "ฝ่ายบุคคล (HR) อนุมัติ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 19, 23, 15, 1, 16L, "002", true });

            // Menus: HR_DISCIPLINE_ADMIN (Admin only — HR opens/tracks
            // disciplinary cases) and HR_GRIEVANCE_ADMIN (Admin only — HR
            // manages reported grievances). Grievance submission itself
            // reuses the existing ESS_ACCESS menu (employee self-service,
            // same pattern as /exp/my-claims/announcements) — no new menu
            // needed for /ess/grievance/new or /ess/grievance/my.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 35, "วินัยพนักงาน", "Disciplinary Actions", 1, true, 45, "HR_DISCIPLINE_ADMIN", true, "/hr/disciplinary", 1, true },
                    { 36, "เรื่องร้องเรียน (HR)", "Grievances (HR)", 1, true, 46, "HR_GRIEVANCE_ADMIN", true, "/hr/grievances", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 36, 35, 9, true },
                    { 37, 36, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 36 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 37 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 35 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 36 });

            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 19 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 23 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 15 });
        }
    }
}

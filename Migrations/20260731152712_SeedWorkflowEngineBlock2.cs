using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedWorkflowEngineBlock2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // menuid 19 = next after 18; roleid 9 = Admin; rolemenuid 17 =
            // next after 16 — confirmed against live DB, not guessed.
            // Covers all 3 new pages (wf/workflows, wf/sub-workflow-levels,
            // wf/my-inbox), same "one menucode, several pages" pattern
            // already used for PAY_ADMIN elsewhere in this app.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 19, "จัดการ Workflow (Engine)", "Workflow Engine Admin", 1, true, 32, "WF_WORKFLOW_ADMIN", true, "/wf/workflows", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive", "canedit" },
                values: new object[,]
                {
                    { 17, 19, 9, true, true },
                });

            // job_status: baseline lookup rows matching the string constants
            // used literally by WorkflowEngineService (StatusPending etc).
            // job_master.status/job_user_list.jobstatus are free-text
            // columns, not FK'd to this table — it exists as an admin
            // reference lookup for now (see Block 9 Moving Status note).
            migrationBuilder.InsertData(
                table: "job_status",
                columns: new[] { "jobstatusid", "jobstatuscode", "name", "name_en", "businessstatus", "isactive" },
                values: new object[,]
                {
                    { 1, "PENDING", "รออนุมัติ", "Pending", "PENDING", true },
                    { 2, "APPROVED", "อนุมัติแล้ว (ระดับนี้)", "Approved (this level)", "APPROVED", true },
                    { 3, "REJECTED", "ปฏิเสธ", "Rejected", "REJECTED", true },
                    { 4, "COMPLETED", "อนุมัติครบทุกระดับ", "Completed", "COMPLETED", true },
                });

            // Demo 2-level workflow used to exercise the Block 2 engine
            // end-to-end (Start -> approve level 1 -> approve level 2 ->
            // Completed). Both levels use Custom User = test_payroll
            // (userid 16, empid '002') so the same test account can drive
            // the whole approval chain during verification.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[,]
                {
                    { 1, "ทดสอบอนุมัติ 2 ระดับ (Demo)", "ACTIVE", "DEMO_2LV", true, true, "Workflow สาธิตสำหรับทดสอบ Engine Block 2 — 2 ระดับ ผู้อนุมัติเดียวกันทั้งสองระดับ" },
                });

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
                    { 1, 1, 1, "อนุมัติระดับ 1",
                      false, false, false, false, false, true,
                      false, false, false,
                      "PENDING", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false },
                    { 2, 1, 2, "อนุมัติระดับ 2 (สุดท้าย)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 1, 1, 1, 1, 16L, "002", true },
                    { 2, 2, 1, 2, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "job_status", keyColumn: "jobstatusid", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "job_status", keyColumn: "jobstatusid", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "job_status", keyColumn: "jobstatusid", keyValues: new object[] { 3 });
            migrationBuilder.DeleteData(table: "job_status", keyColumn: "jobstatusid", keyValues: new object[] { 4 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 17 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 19 });
        }
    }
}

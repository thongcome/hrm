using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedTimesheetApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TIMESHEET_APPROVAL: single level, Custom User (HR/ฝ่ายบุคคล, fixed to
            // test_payroll — same Horizontal Custom User pattern as
            // IDP_APPROVAL/PERF_EVAL_APPROVAL/LEAVE_APPROVAL/OT_APPROVAL, avoiding the
            // known-incomplete org data behind WorkflowEngineService's Vertical
            // resolution). HR adjusts to a real manager approver later via
            // /wf/sub-workflow-levels. url uses the Block 7 generic document-routing
            // {refid} placeholder -> /att/timesheet/{id}. workflowid 19 = next after
            // 18, subworkflowid 27 = next after 26, wf_custom_user id 23 = next after
            // 22 (all verified against live DB before writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 19, "อนุมัติ Timesheet รายสัปดาห์", "ACTIVE", "TIMESHEET_APPROVAL", true, true, "อนุมัติ timesheet โครงการรายสัปดาห์ (Att_TimesheetSubmission) หลังพนักงานส่งฉบับร่าง", "/att/timesheet/{refid}" });

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
                    { 27, 19, 1, "หัวหน้า/ฝ่ายบุคคล (HR) อนุมัติ Timesheet",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 23, 27, 19, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 23 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 27 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 19 });
        }
    }
}

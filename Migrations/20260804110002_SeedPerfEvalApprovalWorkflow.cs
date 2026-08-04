using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedPerfEvalApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PERF_EVAL_APPROVAL: single level, Custom User (HR/ฝ่ายบุคคล, fixed
            // to test_payroll same as LEAVE_APPROVAL/OT_APPROVAL's HR level) — per
            // the plan, finalized evaluation approval is Horizontal (HR-admin),
            // NOT auto-vertical superior-chain, since the org data behind that
            // chain is known-incomplete and this approval is a distinct concern
            // from the rater resolution in PerfAssignmentResolverService.
            // workflowid 13 = next after 12, subworkflowid 20 = next after 19,
            // wf_custom_user id 17 = next after 16 (all verified against live DB
            // before writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 13, "อนุมัติผลการประเมิน", "ACTIVE", "PERF_EVAL_APPROVAL", true, true, "อนุมัติผลการประเมินที่คำนวณคะแนนรวมเสร็จแล้ว (Perf_EvaluationInstance) โดยฝ่ายบุคคล", "/perf/instances/{refid}" });

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
                    { 20, 13, 1, "ฝ่ายบุคคล (HR) อนุมัติผลการประเมิน",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 17, 20, 13, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 17 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 20 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 13 });
        }
    }
}

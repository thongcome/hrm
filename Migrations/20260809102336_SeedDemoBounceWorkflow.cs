using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoBounceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEMO_BOUNCE: 2-level workflow to exercise reject bounce-back
            // (backwardlevel + jobseq). Both levels resolve to the SAME
            // Custom User (test_payroll, userid 16) on purpose — same
            // single-login testability convention as DEMO_ANDPERCENT/
            // DEMO_MIX. Level 2 has backwardlevel=1: rejecting at level 2
            // should route the job back to level 1 for a fresh round
            // (jobseq increments, a NEW job_user_list row is issued at
            // level 1) instead of terminating the job outright.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 21, "ทดสอบตีกลับเมื่อถูกปฏิเสธ (Demo)", "ACTIVE", "DEMO_BOUNCE", true, true, "Workflow สาธิต Reject Bounce-back. ปฏิเสธที่ระดับ 2 จะตีกลับไประดับ 1 (รอบใหม่) แทนที่จะจบงานทันที" });

            migrationBuilder.InsertData(
                table: "wf_sub_workflow_master",
                columns: new[]
                {
                    "subworkflowid", "workflowid", "wlevel", "subject",
                    "isAdhocUser", "iscustomApprover", "isupperrole", "isupperuser", "iscustomRole", "iscustomUser",
                    "iscondition", "isorcondition", "isandcondition", "andpercent",
                    "forwardstatus", "standstatus", "backwardstatus",
                    "istop", "isReturnSender", "isshow", "isLOA", "isAutoApproveAllow",
                    "isNeedBudgetApproval", "isPool", "isApproverSameOrg", "isApproverSameCostCenter", "isManualButton",
                    "isNeedsupervisorapprove", "backwardlevel",
                },
                values: new object[,]
                {
                    { 29, 21, 1, "อนุมัติระดับ 1 (จะถูกเรียกซ้ำหลังตีกลับ)",
                      false, false, false, false, false, true,
                      false, false, false, null,
                      "PENDING", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false,
                      0, null },
                    { 30, 21, 2, "อนุมัติระดับสุดท้าย (ปฏิเสธจะตีกลับไประดับ 1)",
                      false, false, false, false, false, true,
                      false, false, false, null,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false,
                      0, 1 },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 25, 29, 21, 1, 16L, "002", true },
                    { 26, 30, 21, 2, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 25 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 26 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 29 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 30 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 21 });
        }
    }
}

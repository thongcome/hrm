using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedStaleJobseqGuardDemoWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEMO_STALE_JOBSEQ: isolates the jobseq-specific clause of the
            // ApproveAsync/RejectAsync staleness guard
            // (approverRow.wlevel != job.lastLevel || (approverRow.jobseq ?? 0)
            // != (job.jobseq ?? 0)) — every prior regression test that hit
            // this guard did so via the wlevel mismatch half (a level that
            // already advanced) because no seeded workflow combined
            // OR-condition (which leaves moot Pending rows behind on early
            // completion) with a bounce-back that revisits the SAME wlevel
            // in a later round. This workflow does:
            //   Level 1 (wlevel=1): isorcondition=true, 3 candidate rows.
            //     Approving any ONE completes the level early, leaving the
            //     other 2 rows Pending/moot (same as DEMO_OR) and the job
            //     moves on to level 2.
            //   Level 2 (wlevel=2): plain single approver, backwardlevel=1.
            //     Rejecting here bounces back to level 1 — job.jobseq
            //     increments and a NEW round of 3 level-1 rows is created.
            // After the bounce, job.lastLevel is 1 again (matching the old
            // leftover rows' wlevel), but job.jobseq is now 1 while the old
            // rows still carry jobseq=null(0) — the ONLY way to reach the
            // second half of the OR clause with the first half false.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 24, "ทดสอบ Stale Row Guard (jobseq)", "ACTIVE", "DEMO_STALE_JOBSEQ", true, true, "Workflow สาธิตแยกทดสอบ jobseq guard — Block 2. Level 1 OR-condition (เหลือแถว moot) -> Level 2 ปฏิเสธตีกลับไป Level 1 (รอบใหม่) -> แถว moot รอบเก่าต้องถูกบล็อกด้วย jobseq ไม่ใช่ wlevel" });

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
                    { 33, 24, 1, "ระดับ 1 - OR-condition (เหลือแถว moot)",
                      false, false, false, false, false, true,
                      false, true, false, null,
                      "PENDING", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false,
                      0, null },
                    { 34, 24, 2, "ระดับ 2 - ปฏิเสธแล้วตีกลับไประดับ 1",
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
                    { 31, 33, 24, 1, 16L, "002", true },
                    { 32, 33, 24, 1, 16L, "002", true },
                    { 33, 33, 24, 1, 16L, "002", true },
                    { 34, 34, 24, 2, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 31 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 32 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 33 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 34 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 33 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 34 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 24 });
        }
    }
}

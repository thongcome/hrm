using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoOrConditionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEMO_OR: 1-level workflow to exercise Block 1's OR-condition
            // evaluator for the first time with a real job_master row (the
            // engine branch has existed since Block 1 but was never actually
            // reached at runtime — every workflow seeded before this one has
            // isorcondition=false). 3 candidate rows all resolve to the SAME
            // user (test_payroll, userid 16) on purpose, mirroring the
            // DEMO_ANDPERCENT pattern, so both paths are driveable from a
            // single login:
            //   - approve any ONE row -> job completes immediately, the
            //     other 2 rows are left untouched/moot
            //   - reject ALL THREE rows (across 3 separate job instances,
            //     since approving one closes the job) -> job fails
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 23, "ทดสอบ OR-Condition (Demo)", "ACTIVE", "DEMO_OR", true, true, "Workflow สาธิต OR-condition — Block 1. 3 ผู้อนุมัติ (คนเดียวกัน 3 แถว เพื่อทดสอบด้วย login เดียว) คนแรกที่อนุมัติพอ ไม่ต้องรอทุกคน" });

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
                    "isNeedsupervisorapprove",
                },
                values: new object[]
                {
                    32, 23, 1, "อนุมัติแบบ OR (คนแรกที่อนุมัติพอ)",
                    false, false, false, false, false, true,
                    false, true, false, null,
                    "COMPLETED", "PENDING", "RETURNED",
                    true, false, true, false, false,
                    false, false, false, false, false,
                    0,
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 28, 32, 23, 1, 16L, "002", true },
                    { 29, 32, 23, 1, 16L, "002", true },
                    { 30, 32, 23, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 28 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 29 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 30 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 32 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 23 });
        }
    }
}

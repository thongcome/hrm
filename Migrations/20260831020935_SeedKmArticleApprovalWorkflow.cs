using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedKmArticleApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // KM_ARTICLE_APPROVAL: single level, Horizontal Custom User fixed
            // to test_payroll — the same placeholder-approver pattern as
            // IDP_APPROVAL/PERF_EVAL_APPROVAL/LEAVE_APPROVAL (avoiding the
            // known-incomplete org data behind Vertical resolution); HR
            // reassigns a real approver later via /wf/sub-workflow-levels.
            // workflowid 10030 = next after live MAX 10029, subworkflowid
            // 10039 = next after 10038, wf_custom_user id 10033 = next after
            // 10032 (all verified against live DB 2026-08-31 before writing
            // this — ids drift as migration history grows, never guess).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 10030, "อนุมัติเผยแพร่บทความความรู้ (KM)", "ACTIVE", "KM_ARTICLE_APPROVAL", true, true, "อนุมัติการเผยแพร่บทความ (Km_Article) หลังผู้เขียนส่งฉบับร่าง", "/km/articles" });

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
                    { 10039, 10030, 1, "ฝ่ายบุคคล (HR) อนุมัติเผยแพร่บทความ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 10033, 10039, 10030, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10033 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10039 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10030 });
        }
    }
}

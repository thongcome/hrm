using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedHireApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HIRE_APPROVAL: single level, Horizontal Custom User (same demo
            // HR user — userid=16, empid="002" — used by every other
            // Custom-User-seeded workflow this session, e.g.
            // SeedEmployeeSeparationApprovalWorkflow) — avoids the known-
            // incomplete org data behind WorkflowEngineService's Vertical
            // resolution. HR repoints to a real approver later via
            // /wf/sub-workflow-levels. url routes back to the offer detail
            // page (RecOfferService.SubmitForHireApprovalAsync starts this
            // job against Rec_Offer, not a dedicated request table).
            //
            // Live DB checked immediately before writing this (2026-08-29):
            // MAX(workflowid)=10028, MAX(subworkflowid)=10037,
            // MAX(id) FROM wf_custom_user=10031 — next values below.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 10029, "อนุมัติการจ้างพนักงาน", "ACTIVE", "HIRE_APPROVAL", true, true, "อนุมัติการจ้างผู้สมัคร (Rec_Offer) เป็นพนักงานจริง ก่อนสร้างข้อมูล Hremployee", "/rec/offers/{refid}" });

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
                    { 10038, 10029, 1, "ฝ่ายบุคคล (HR) อนุมัติการจ้างพนักงาน",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 10032, 10038, 10029, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10032 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10038 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10029 });
        }
    }
}

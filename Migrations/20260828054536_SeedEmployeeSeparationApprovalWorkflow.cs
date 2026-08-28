using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedEmployeeSeparationApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EMPLOYEE_SEPARATION_APPROVAL: single level, Horizontal Custom
            // User (same demo HR user — userid=16, empid="002" — used by
            // every other Custom-User-seeded workflow this session, e.g.
            // SeedIdpApprovalWorkflow/SeedRecruitmentWorkflows) — avoids the
            // known-incomplete org data behind WorkflowEngineService's
            // Vertical resolution. HR repoints to a real approver later via
            // /wf/sub-workflow-levels. url uses the Block 7 generic
            // document-routing {refid} placeholder; there's no dedicated
            // Hr_SeparationRequest detail page, so it routes back to the
            // employee admin page where the request was raised.
            //
            // Live DB checked immediately before writing this (2026-08-28):
            // MAX(workflowid)=10027, MAX(subworkflowid)=10036,
            // MAX(id) FROM wf_custom_user=10030 — next values below.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 10028, "อนุมัติสิ้นสุดการจ้างงาน", "ACTIVE", "EMPLOYEE_SEPARATION_APPROVAL", true, true, "อนุมัติคำขอสิ้นสุดการจ้างงาน (Hr_SeparationRequest) ก่อนบันทึกวันที่ลาออก/เลิกจ้างจริง", "/pay/employees/{refid}" });

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
                    { 10037, 10028, 1, "ฝ่ายบุคคล (HR) อนุมัติสิ้นสุดการจ้างงาน",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 10031, 10037, 10028, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10031 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10037 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10028 });
        }
    }
}

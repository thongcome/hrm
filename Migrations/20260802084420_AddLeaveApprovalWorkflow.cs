using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LEAVE_APPROVAL: mirrors OT_APPROVAL (AddOtApprovalWorkflow) exactly —
            // 2 levels: 1. Vertical (หัวหน้างาน) via com_organization.approver_userid,
            // 2. Custom User (ฝ่ายบุคคล/HR) fixed to test_payroll, same as OT.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 9, "ขออนุมัติลางาน", "ACTIVE", "LEAVE_APPROVAL", true, true, "อนุมัติคำขอลางาน (Lve_LeaveRequest) — ระดับ 1 หัวหน้างานตามผังองค์กร, ระดับ 2 ฝ่ายบุคคล", "/leave-requests/detail/{refid}" });

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
                    { 15, 9, 1, "หัวหน้างานอนุมัติ (ตามผังองค์กร)",
                      false, false, true, false, false, false,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false },
                    { 16, 9, 2, "ฝ่ายบุคคล (HR) อนุมัติ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 13, 16, 9, 2, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 13 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 15 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 16 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 9 });
        }
    }
}

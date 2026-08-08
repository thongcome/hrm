using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedLmsTrainingApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LMS_TRAINING_APPROVAL: single level, Horizontal Custom User
            // (HR/ฝ่ายบุคคล, fixed to test_payroll — same pattern as
            // IDP_APPROVAL/PERF_EVAL_APPROVAL/LEAVE_APPROVAL, avoiding the
            // known-incomplete org data behind WorkflowEngineService's
            // Vertical resolution). No per-enrollment detail page exists in
            // this module, so url points at the HR enrollment list rather
            // than using the Block 7 {refid} placeholder.
            // workflowid 20 = next after 19, subworkflowid 28 = next after
            // 27, wf_custom_user id 24 = next after 23 (all verified against
            // live DB before writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 20, "อนุมัติลงทะเบียนอบรม (LMS)", "ACTIVE", "LMS_TRAINING_APPROVAL", true, true, "อนุมัติการลงทะเบียนอบรม (Lms_Enrollment) สำหรับหลักสูตรที่ตั้งค่าให้ต้องอนุมัติ", "/lms/enrollments" });

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
                    { 28, 20, 1, "ฝ่ายบุคคล (HR) อนุมัติการลงทะเบียนอบรม",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 24, 28, 20, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 24 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 28 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 20 });
        }
    }
}

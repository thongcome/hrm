using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedWorkflowEngineBlock3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // test_payroll (userid 16, empid '002') becomes the HR org's
            // approver, for the Vertical resolution demo — reuses the same
            // test account as the Block 2 Horizontal demo so one login can
            // exercise every path.
            migrationBuilder.Sql("UPDATE com_organization SET approver_userid = 16 WHERE code = 'HR'");

            // Demo employee record for Vertical resolution — StartJobAsync
            // resolves the requester's org via wf_employee.orgcode (not
            // Hremployee, see the code comment on why). orgcode is changed
            // between 'HR' (has an approver) and 'CTO' (id=5, confirmed no
            // approver_userid/approver_empid set) via the existing
            // /wf/employees CRUD page during manual testing of the vacancy
            // path, rather than seeding a second demo employee + login.
            migrationBuilder.InsertData(
                table: "wf_employee",
                columns: new[] { "id", "empid", "firstname_th", "lastname_th", "cardid", "sexid", "orgcode", "orgname_th", "isactive" },
                values: new object[] { 1L, "002", "ทดสอบ", "ระบบเวิร์กโฟลว์", "9999999999999", "M", "HR", "HR", true });

            // DEMO_VERTICAL: 1 level, Vertical (isupperuser), resolves to
            // whoever com_organization('HR').approver_userid is.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 2, "ทดสอบอนุมัติสายบังคับบัญชา (Vertical Demo)", "ACTIVE", "DEMO_VERTICAL", true, true, "Workflow สาธิต Vertical resolution ผ่าน com_organization.approver_userid — Block 3" });

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
                values: new object[]
                {
                    3, 2, 1, "อนุมัติโดยหัวหน้าหน่วยงาน (Vertical)",
                    false, false, false, true, false, false,
                    false, false, false,
                    "APPROVED", "PENDING", "RETURNED",
                    true, false, true, false, false,
                    false, false, false, false, false,
                });

            // DEMO_AUTOSKIP: 2 levels — level 1 Vertical against an org with
            // NO approver configured + isAutoApproveAllow=true (should
            // auto-skip per the isAutoApproveAllow behavior confirmed
            // against epms's WorkflowController.cs), level 2 Custom User =
            // test_payroll (the only level a human actually has to act on).
            // Uses the requester's own org unchanged ('HR' — which DOES have
            // an approver) is wrong for testing skip, so this workflow's
            // level 1 is deliberately anchored on the requester's org same
            // as DEMO_VERTICAL; the vacancy case is exercised by switching
            // the demo employee's orgcode to 'CTO' via /wf/employees before
            // running this workflow, same as the DEMO_VACANT manual test.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 3, "ทดสอบข้ามระดับอัตโนมัติเมื่อตำแหน่งว่าง (Auto-Skip Demo)", "ACTIVE", "DEMO_AUTOSKIP", true, true, "Workflow สาธิต isAutoApproveAllow เมื่อ Vertical resolve ไม่เจอใคร — Block 3" });

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
                    { 4, 3, 1, "อนุมัติโดยหัวหน้าหน่วยงาน (ข้ามถ้าว่าง)",
                      false, false, false, true, false, false,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      false, false, true, false, true,
                      false, false, false, false, false },
                    { 5, 3, 2, "อนุมัติระดับสุดท้าย (Custom User)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 3, 5, 3, 2, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 3 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 4 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 5 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 3 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 3 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "wf_employee", keyColumn: "id", keyValues: new object[] { 1L });
            migrationBuilder.Sql("UPDATE com_organization SET approver_userid = NULL WHERE code = 'HR'");
        }
    }
}

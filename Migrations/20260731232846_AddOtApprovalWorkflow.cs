using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOtApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "companyid",
                table: "emp_overtime_request",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "hremployeeid",
                table: "emp_overtime_request",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "hrwOtId",
                table: "emp_overtime_request",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "jobmasterid",
                table: "emp_overtime_request",
                type: "bigint",
                nullable: true);

            // OT_APPROVAL: the first real (non-demo) document type wired
            // through Block 7 generic routing — url points at the actual
            // OtRequestDetail.razor page, not a placeholder. 2 levels:
            //   1. Vertical (หัวหน้างาน) — resolves via the requester's own
            //      org-chart approver (com_organization.approver_userid),
            //      same mechanism as DEMO_VERTICAL.
            //   2. Custom User (ฝ่ายบุคคล/HR) — istop, fixed to test_payroll
            //      for now since no dedicated "HR" role/user exists yet in
            //      this dev DB; swap to Custom Role once one does.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 8, "ขออนุมัติทำงานล่วงเวลา (OT)", "ACTIVE", "OT_APPROVAL", true, true, "อนุมัติคำขอทำงานล่วงเวลา (emp_overtime_request) — ระดับ 1 หัวหน้างานตามผังองค์กร, ระดับ 2 ฝ่ายบุคคล", "/ot-requests/detail/{refid}" });

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
                    { 13, 8, 1, "หัวหน้างานอนุมัติ (ตามผังองค์กร)",
                      false, false, true, false, false, false,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false },
                    { 14, 8, 2, "ฝ่ายบุคคล (HR) อนุมัติ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 12, 14, 8, 2, 16L, "002", true });

            // Link the test employee (EMP_NO='002', linked to sc_user
            // test_payroll) to the HR org unit so Vertical resolution at
            // level 1 finds a real approver instead of hitting the vacancy
            // path — HR is the only com_organization row with an approver
            // configured (approver_userid=16, set during Block 3). Mirrors
            // exactly what the org picker in PayrollEmployeeAdmin.razor
            // would write.
            migrationBuilder.Sql(
                "UPDATE Hremployee SET OrganizationId = 2, orgcode = 'HR', orgcodefull = '0101' WHERE EMP_NO = '002';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Hremployee SET OrganizationId = NULL, orgcode = NULL, orgcodefull = NULL WHERE EMP_NO = '002' AND orgcode = 'HR';");

            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 12 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 13 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 14 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 8 });

            migrationBuilder.DropColumn(
                name: "companyid",
                table: "emp_overtime_request");

            migrationBuilder.DropColumn(
                name: "hremployeeid",
                table: "emp_overtime_request");

            migrationBuilder.DropColumn(
                name: "hrwOtId",
                table: "emp_overtime_request");

            migrationBuilder.DropColumn(
                name: "jobmasterid",
                table: "emp_overtime_request");
        }
    }
}

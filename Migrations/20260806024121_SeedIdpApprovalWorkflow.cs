using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedIdpApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IDP_APPROVAL: single level, Custom User (HR/ฝ่ายบุคคล, fixed to
            // test_payroll — same Horizontal Custom User pattern as
            // PERF_EVAL_APPROVAL/LEAVE_APPROVAL/OT_APPROVAL, avoiding the
            // known-incomplete org data behind WorkflowEngineService's
            // Vertical resolution). HR adjusts to a real approver later via
            // /wf/sub-workflow-levels. url uses the Block 7 generic
            // document-routing {refid} placeholder -> /idp/plans/{id}.
            // workflowid 16 = next after 15, subworkflowid 24 = next after
            // 23, wf_custom_user id 20 = next after 19 (all verified against
            // live DB before writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 16, "อนุมัติแผนพัฒนารายบุคคล (IDP)", "ACTIVE", "IDP_APPROVAL", true, true, "อนุมัติแผนพัฒนารายบุคคล (Idp_Plan) หลังพนักงานส่งฉบับร่าง", "/idp/plans/{refid}" });

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
                    { 24, 16, 1, "ฝ่ายบุคคล (HR) อนุมัติแผนพัฒนารายบุคคล",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 20, 24, 16, 1, 16L, "002", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 20 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 24 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 16 });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedProvidentFundWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PVD_RATE_CHANGE_APPROVAL + PVD_EXIT_APPROVAL: single level each,
            // Horizontal Custom User (same demo HR user — userid=16,
            // empid="002" — used by every other Custom-User-seeded workflow
            // this session) — HR repoints to a real approver later via
            // /wf/sub-workflow-levels. url uses the Block 7 generic
            // document-routing {refid} placeholder.
            // workflowid 10026/10027 = next after 10025, subworkflowid
            // 10034/10035 = next after 10033, wf_custom_user id 10029/10030
            // = next after 10028 (all verified against live DB before
            // writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[,]
                {
                    { 10026L, "อนุมัติเปลี่ยนอัตรากองทุนสำรองเลี้ยงชีพ", "ACTIVE", "PVD_RATE_CHANGE_APPROVAL", true, true, "อนุมัติคำขอเปลี่ยนอัตราเงินสะสม/สมทบ (Pay_ProvidentFundRateChangeRequest)", "/pay/admin/provident-fund-rate-requests" },
                    { 10027L, "อนุมัติปิดสมาชิกภาพกองทุนสำรองเลี้ยงชีพ", "ACTIVE", "PVD_EXIT_APPROVAL", true, true, "อนุมัติเคสปิดสมาชิกภาพและตัดสินสิทธิ์เงินสมทบ (Pay_ProvidentFundExitCase)", "/pay/admin/provident-fund-exit-cases" },
                });

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
                    { 10034L, 10026L, 1, "ฝ่ายบุคคล (HR) อนุมัติเปลี่ยนอัตรากองทุนสำรองเลี้ยงชีพ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 10035L, 10027L, 1, "ฝ่ายบุคคล (HR) อนุมัติปิดสมาชิกภาพกองทุนสำรองเลี้ยงชีพ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 10029L, 10034L, 10026L, 1, 16L, "002", true },
                    { 10030L, 10035L, 10027L, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10029L });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10030L });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10034L });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10035L });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10026L });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10027L });
        }
    }
}

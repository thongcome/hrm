using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecruitmentWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REQUISITION_APPROVAL + OFFER_APPROVAL: single level each,
            // Horizontal Custom User (same demo HR user — userid=16,
            // empid="002" — used by every other Custom-User-seeded workflow
            // this session, e.g. SeedIdpApprovalWorkflow) — avoids the
            // known-incomplete org data behind WorkflowEngineService's
            // Vertical resolution. HR repoints to a real approver later via
            // /wf/sub-workflow-levels. url uses the Block 7 generic
            // document-routing {refid} placeholder.
            // workflowid 17/18 = next after 16, subworkflowid 25/26 = next
            // after 24, wf_custom_user id 21/22 = next after 20 (all
            // verified against live DB before writing this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[,]
                {
                    { 17, "อนุมัติคำขออัตรากำลัง", "ACTIVE", "REQUISITION_APPROVAL", true, true, "อนุมัติคำขออัตรากำลัง (Rec_Requisition) ก่อนเปิดประกาศรับสมัคร", "/rec/requisitions/{refid}" },
                    { 18, "อนุมัติข้อเสนอจ้างงาน", "ACTIVE", "OFFER_APPROVAL", true, true, "อนุมัติข้อเสนอจ้างงาน (Rec_Offer) ก่อนส่งให้ผู้สมัคร", "/rec/offers/{refid}" },
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
                    { 25, 17, 1, "ฝ่ายบุคคล (HR) อนุมัติคำขออัตรากำลัง",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 26, 18, 1, "ฝ่ายบุคคล (HR) อนุมัติข้อเสนอจ้างงาน",
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
                    { 21, 25, 17, 1, 16L, "002", true },
                    { 22, 26, 18, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 21 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 22 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 25 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 26 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 17 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 18 });
        }
    }
}

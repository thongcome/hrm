using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuccessionAndPipApprovalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SUCCESSION_NOMINATION_APPROVAL + PIP_APPROVAL: single level,
            // Horizontal Custom User (HR/ฝ่ายบุคคล, fixed to test_payroll) —
            // same pattern as IDP_APPROVAL/LMS_TRAINING_APPROVAL/
            // PERF_EVAL_APPROVAL, avoiding WorkflowEngineService's Vertical
            // resolution (still incomplete org data behind it). No
            // per-nomination detail page exists (nominations live inside
            // KeyPositionDetail.razor keyed by KeyPositionId, not
            // NominationId), so SUCCESSION_NOMINATION_APPROVAL's url points
            // at the key-position list rather than using the Block 7
            // {refid} placeholder — mirrors LMS_TRAINING_APPROVAL's same
            // choice. PIP_APPROVAL does have a real per-plan detail route
            // (/perf/pip/{Id:long}), so its url uses {refid} directly.
            // workflowid 10024/10025 = next after 10023, subworkflowid
            // 10032/10033 = next after 10031, wf_custom_user id 10027/10028 =
            // next after 10026 (all verified against live DB before writing
            // this).
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[,]
                {
                    { 10024, "อนุมัติการเสนอชื่อผู้สืบทอดตำแหน่ง", "ACTIVE", "SUCCESSION_NOMINATION_APPROVAL", true, true, "อนุมัติการเสนอชื่อผู้สืบทอดตำแหน่งสำคัญ (Succ_SuccessorNomination) ก่อนนับเป็นผู้สืบทอดที่ผ่านธรรมาภิบาลจริง", "/succession/key-positions" },
                    { 10025, "อนุมัติ Performance Improvement Plan (PIP)", "ACTIVE", "PIP_APPROVAL", true, true, "อนุมัติแผนปรับปรุงผลการปฏิบัติงาน (Perf_ImprovementPlan) ก่อนมีผลบังคับใช้จริง", "/perf/pip/{refid}" },
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
                    { 10032, 10024, 1, "ฝ่ายบุคคล (HR) อนุมัติการเสนอชื่อผู้สืบทอดตำแหน่ง",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 10033, 10025, 1, "ฝ่ายบุคคล (HR) อนุมัติ Performance Improvement Plan",
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
                    { 10027, 10032, 10024, 1, 16L, "002", true },
                    { 10028, 10033, 10025, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10027 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10028 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10032 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10033 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10024 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10025 });
        }
    }
}

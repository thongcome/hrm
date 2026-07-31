using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowLoaBlock4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isLOA",
                table: "job_subworkflow_master",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // DEMO_LOA: 3-level workflow to exercise Block 4 branching.
            // Level 1 (isLOA=true) has two wf_loa bands: amount <= 10000
            // routes to level 2 (istop, low path), amount > 10000 routes
            // straight to level 3 (istop, high path) — skipping level 2
            // entirely. All levels use Custom User = test_payroll (userid
            // 16, empid '002') so one login can drive both paths.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 4, "ทดสอบ LOA วงเงิน (Demo)", "ACTIVE", "DEMO_LOA", true, true, "Workflow สาธิต LOA amount-based branching — Block 4. <=10000 ไปสาย low (level 2), >10000 ไปสาย high (level 3) ข้าม level 2" });

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
                    { 6, 4, 1, "อนุมัติระดับ 1 (ตรวจวงเงิน)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "PENDING", "PENDING", "RETURNED",
                      false, false, true, true, false,
                      false, false, false, false, false },
                    { 7, 4, 2, "อนุมัติระดับ 2 (สาย low — วงเงินไม่เกิน 10,000)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 8, 4, 3, "อนุมัติระดับ 3 (สาย high — วงเงินเกิน 10,000)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 4, 6, 4, 1, 16L, "002", true },
                    { 5, 7, 4, 2, 16L, "002", true },
                    { 6, 8, 4, 3, 16L, "002", true },
                });

            migrationBuilder.InsertData(
                table: "wf_loa",
                columns: new[] { "id", "loaid", "wfid", "nowWorkflowid", "nextWorkflowId", "nowLevel", "nextLevel", "min", "max", "isactive", "subject" },
                values: new object[,]
                {
                    { 1, 1, 4, 4, 4, 1, 2, 0m, 10000m, true, "วงเงินต่ำ (ไม่เกิน 10,000)" },
                    { 2, 2, 4, 4, 4, 1, 3, 10000.01m, null, true, "วงเงินสูง (เกิน 10,000)" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_loa", keyColumn: "id", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "wf_loa", keyColumn: "id", keyValues: new object[] { 2 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 4 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 5 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 6 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 6 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 7 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 8 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 4 });

            migrationBuilder.DropColumn(
                name: "isLOA",
                table: "job_subworkflow_master");
        }
    }
}

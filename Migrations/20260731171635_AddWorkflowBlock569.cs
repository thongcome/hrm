using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowBlock569 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "isNeedsupervisorapprove",
                table: "wf_sub_workflow_master",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "backwardstatus",
                table: "job_subworkflow_master",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forwardstatus",
                table: "job_subworkflow_master",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "isNeedsupervisorapprove",
                table: "job_subworkflow_master",
                type: "int",
                nullable: true);

            // Block 9 (Moving Status): "RETURNED" was already in active use
            // as a free-text forwardstatus/backwardstatus value on several
            // demo levels (DEMO_LOA, WfSubWorkflowMasterAdmin.razor's
            // CreateNewLevel default) but was never added to job_status
            // when that lookup was seeded in Block 2 (PENDING/APPROVED/
            // REJECTED/COMPLETED only) — added now so the new dropdown
            // (sourced from job_status) covers every value already in use.
            migrationBuilder.InsertData(
                table: "job_status",
                columns: new[] { "jobstatusid", "jobstatuscode", "name", "name_en", "businessstatus", "isactive" },
                values: new object[] { 5, "RETURNED", "ตีกลับ/ปฏิเสธ", "Returned", "REJECTED", true });

            // DEMO_ANDPERCENT: 1-level workflow to exercise Block 5's
            // AND-condition % evaluator. 3 candidate rows all resolve to the
            // SAME user (test_payroll, userid 16) on purpose — the engine
            // creates 3 separate job_user_list rows regardless of whether
            // the underlying person is the same, so approving any 2 of the
            // 3 (66.66% >= 60% threshold) proves partial-approval completion
            // while the 3rd row is left untouched (and becomes moot once
            // the job closes) — all driveable from a single login.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 5, "ทดสอบ AND-Condition % (Demo)", "ACTIVE", "DEMO_ANDPERCENT", true, true, "Workflow สาธิต AND-condition % — Block 5. 3 ผู้อนุมัติ (คนเดียวกัน 3 แถว เพื่อทดสอบด้วย login เดียว) ต้องอนุมัติรวมกัน >= 60% จึงจะผ่าน" });

            // DEMO_MIX: 1-level workflow to exercise Block 6's Mix Approval
            // (vertical pre-check hop before the level's own approver).
            // isNeedsupervisorapprove=1 means: resolve 1 hop up the org
            // chart from the requester's own org (com_organization('HR')
            // .approver_userid=16, same seed as DEMO_VERTICAL in Block 3)
            // before the level's own Custom User candidate is even offered.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 6, "ทดสอบ Mix Approval (Demo)", "ACTIVE", "DEMO_MIX", true, true, "Workflow สาธิต Mix Approval — Block 6. ต้องให้หัวหน้าสายบังคับบัญชา (org ของผู้ขอ) อนุมัติก่อน 1 ชั้น แล้วจึงเข้าสู่ผู้อนุมัติหลักของระดับ" });

            migrationBuilder.InsertData(
                table: "wf_sub_workflow_master",
                columns: new[]
                {
                    "subworkflowid", "workflowid", "wlevel", "subject",
                    "isAdhocUser", "iscustomApprover", "isupperrole", "isupperuser", "iscustomRole", "iscustomUser",
                    "iscondition", "isorcondition", "isandcondition", "andpercent",
                    "forwardstatus", "standstatus", "backwardstatus",
                    "istop", "isReturnSender", "isshow", "isLOA", "isAutoApproveAllow",
                    "isNeedBudgetApproval", "isPool", "isApproverSameOrg", "isApproverSameCostCenter", "isManualButton",
                    "isNeedsupervisorapprove",
                },
                values: new object[,]
                {
                    { 9, 5, 1, "อนุมัติแบบบอร์ด (AND % >= 60)",
                      false, false, false, false, false, true,
                      false, false, true, 60m,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false,
                      0 },
                    { 10, 6, 1, "อนุมัติ Mix (หัวหน้าก่อน 1 ชั้น แล้วอนุมัติเอง)",
                      false, false, false, false, false, true,
                      false, false, false, null,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false,
                      1 },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 7, 9, 5, 1, 16L, "002", true },
                    { 8, 9, 5, 1, 16L, "002", true },
                    { 9, 9, 5, 1, 16L, "002", true },
                    { 10, 10, 6, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 7 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 8 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 9 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 10 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 9 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 10 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 5 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 6 });
            migrationBuilder.DeleteData(table: "job_status", keyColumn: "jobstatusid", keyValues: new object[] { 5 });

            migrationBuilder.DropColumn(
                name: "isNeedsupervisorapprove",
                table: "wf_sub_workflow_master");

            migrationBuilder.DropColumn(
                name: "backwardstatus",
                table: "job_subworkflow_master");

            migrationBuilder.DropColumn(
                name: "forwardstatus",
                table: "job_subworkflow_master");

            migrationBuilder.DropColumn(
                name: "isNeedsupervisorapprove",
                table: "job_subworkflow_master");
        }
    }
}

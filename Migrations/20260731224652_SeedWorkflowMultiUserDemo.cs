using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedWorkflowMultiUserDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DEMO_MULTIUSER: answers "does this actually route to different
            // real people, or always the same test login?" — every earlier
            // demo workflow (DEMO_2LV, DEMO_ANDPERCENT, DEMO_MIX, ...)
            // intentionally pointed every level at test_payroll (userid 16)
            // so a single browser login could drive the whole approval
            // chain during verification; this one uses genuinely different
            // real sc_user accounts instead.
            //   Level 1 (Custom Role "Admin", roleid 9): fans out to ALL 6
            //   distinct members of that role at once (sawat, admin,
            //   test_payroll, 005, 006, 007) — everyone gets it in their
            //   own inbox simultaneously, not one person 3 times.
            //   Level 2 (Custom User, istop): routes specifically to sawat
            //   (userid 12) — a different single real person again.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description" },
                values: new object[] { 7, "ทดสอบส่งงานไปหลายคนจริง (Demo)", "ACTIVE", "DEMO_MULTIUSER", true, true, "Workflow สาธิตว่า engine ส่งงานไปหาคนจริงที่ต่างกันได้ ไม่ใช่คนเดิมซ้ำ — level 1 กระจายไปหาทุกคนใน Role Admin พร้อมกัน (6 คน), level 2 ส่งต่อไปยัง sawat โดยเฉพาะ" });

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
                    { 11, 7, 1, "ระดับ 1 - ทีม Admin (Custom Role, กระจายไปหลายคนพร้อมกัน)",
                      false, false, false, false, true, false,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false },
                    { 12, 7, 2, "ระดับ 2 - ผู้บริหาร sawat (Custom User)",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_role",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "roleid", "isactive" },
                values: new object[] { 1, 11, 7, 1, 9L, true });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 11, 12, 7, 2, 12L, "001", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 11 });
            migrationBuilder.DeleteData(table: "wf_custom_role", keyColumn: "id", keyValues: new object[] { 1 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 11 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 12 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 7 });
        }
    }
}

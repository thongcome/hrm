using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class SeedExpenseClaimsWorkflowAndMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-verified against live DB immediately before writing this:
            // workflowid max=13, subworkflowid max=20, Pay_PayItemType Id
            // max=13, wf_custom_user id max=17, menuid max=31, rolemenuid
            // max=31.

            migrationBuilder.InsertData(
                table: "Pay_PayItemType",
                columns: new[] { "Id", "Category", "Code", "DefaultSignFlag", "GLAccountCode", "IsActive", "IsSystemReserved", "NameEn", "NameTh", "SortOrder" },
                values: new object[,]
                {
                    { 14, 0, "REIMBURSEMENT", 1, "5045-REIMBURSE", true, true, "Expense Reimbursement", "เบิกค่าใช้จ่ายคืน", 14 }
                });

            // EXPENSE_CLAIM_APPROVAL: 2 levels, same shape as OT_APPROVAL —
            //   1. Vertical (หัวหน้างานตามผังองค์กร)
            //   2. Custom User (ฝ่ายบุคคล/HR), istop, fixed to test_payroll
            //      for now (same as OT — no dedicated HR role/user yet).
            // Reuses the Hremployee(EMP_NO='002')->HR org link that
            // AddOtApprovalWorkflow already established, so level 1
            // resolves for the same test employee without new org wiring.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[] { 14, "ขออนุมัติเบิกค่าใช้จ่าย", "ACTIVE", "EXPENSE_CLAIM_APPROVAL", true, true, "อนุมัติใบเบิกค่าใช้จ่าย/เดินทาง (Exp_ClaimHeader) — ระดับ 1 หัวหน้างานตามผังองค์กร, ระดับ 2 ฝ่ายบุคคล", "/exp/admin/claims/{refid}" });

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
                    { 21, 14, 1, "หัวหน้างานอนุมัติ (ตามผังองค์กร)",
                      false, false, true, false, false, false,
                      false, false, false,
                      "APPROVED", "PENDING", "RETURNED",
                      false, false, true, false, false,
                      false, false, false, false, false },
                    { 22, 14, 2, "ฝ่ายบุคคล (HR) อนุมัติ",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "RETURNED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[] { 18, 22, 14, 2, 16L, "002", true });

            // Menus: EXP_ADMIN (category config + HR oversight list, Admin
            // only) and EXP_ACCESS (employee self-service claim submission,
            // Employee + Admin) — same split as ESS_ACCESS/HR_ANNOUNCE_*.
            migrationBuilder.InsertData(
                table: "sc_menu",
                columns: new[] { "menuid", "menuname", "menuname_en", "menulevel", "isfinal", "menuorder", "menucode", "isshow", "url", "menugroupid", "isactive" },
                values: new object[,]
                {
                    { 32, "ประเภทค่าใช้จ่าย", "Expense Categories", 1, true, 42, "EXP_ADMIN", true, "/exp/admin/categories", 1, true },
                    { 33, "ใบเบิกค่าใช้จ่ายทั้งหมด", "All Expense Claims", 1, true, 43, "EXP_ADMIN", true, "/exp/admin/claims", 1, true },
                    { 34, "เบิกค่าใช้จ่าย (ของฉัน)", "My Expense Claims", 1, true, 44, "EXP_ACCESS", true, "/exp/my-claims", 1, true },
                });

            migrationBuilder.InsertData(
                table: "sc_role_menu",
                columns: new[] { "rolemenuid", "menuid", "roleid", "isactive" },
                values: new object[,]
                {
                    { 32, 32, 9, true },
                    { 33, 33, 9, true },
                    { 34, 34, 10, true },
                    { 35, 34, 9, true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 32 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 33 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 34 });
            migrationBuilder.DeleteData(table: "sc_role_menu", keyColumn: "rolemenuid", keyValues: new object[] { 35 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 32 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 33 });
            migrationBuilder.DeleteData(table: "sc_menu", keyColumn: "menuid", keyValues: new object[] { 34 });

            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 18 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 21 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 22 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 14 });

            migrationBuilder.DeleteData(table: "Pay_PayItemType", keyColumn: "Id", keyValues: new object[] { 14 });
        }
    }
}

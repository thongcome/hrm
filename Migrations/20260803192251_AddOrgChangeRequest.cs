using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Org_OrganizationChangeRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    TargetOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TargetOrganizationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldParentCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewParentCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewIsTop = table.Column<bool>(type: "bit", nullable: true),
                    OldApproverEmpId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldApproverName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewApproverEmpId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewApproverName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NewCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewAbbr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewAbbrEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewIsActive = table.Column<bool>(type: "bit", nullable: true),
                    NewIsManPowerCount = table.Column<bool>(type: "bit", nullable: true),
                    NewSectionTypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewSubSectionTypeId = table.Column<long>(type: "bigint", nullable: true),
                    NewCostCenterCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewRemark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByEmpId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Org_OrganizationChangeRequest", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Org_OrganizationChangeRequest_IsApplied_EffectiveFrom",
                table: "Org_OrganizationChangeRequest",
                columns: new[] { "IsApplied", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Org_OrganizationChangeRequest_JobMasterId",
                table: "Org_OrganizationChangeRequest",
                column: "JobMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Org_OrganizationChangeRequest_TargetOrganizationId",
                table: "Org_OrganizationChangeRequest",
                column: "TargetOrganizationId");

            // 3 separate workflow codes (one per gated change kind) instead of 1
            // shared workflow, so HR can independently tune approval depth per
            // change kind later via /wf/workflows + /wf/sub-workflow-levels with
            // zero code changes. Single-level Custom User approval, fixed to the
            // same demo HR user (userid=16, empid='002') that OT_APPROVAL/
            // LEAVE_APPROVAL already seeded — HR should point this at a real
            // role/person via the existing admin pages before go-live.
            migrationBuilder.InsertData(
                table: "wf_workflow",
                columns: new[] { "workflowid", "wname", "wstatus", "workflowcode", "isshow", "isactive", "description", "url" },
                values: new object[,]
                {
                    { 10, "ขออนุมัติสร้างหน่วยงานใหม่", "ACTIVE", "ORG_CHANGE_NEWORG", true, true, "อนุมัติคำขอสร้างหน่วยงานใหม่ (Org_OrganizationChangeRequest)", "/org/change-requests/{refid}" },
                    { 11, "ขออนุมัติย้ายสังกัด", "ACTIVE", "ORG_CHANGE_MOVE", true, true, "อนุมัติคำขอย้ายสังกัดหน่วยงาน (Org_OrganizationChangeRequest)", "/org/change-requests/{refid}" },
                    { 12, "ขออนุมัติเปลี่ยนหัวหน้า", "ACTIVE", "ORG_CHANGE_BOSS", true, true, "อนุมัติคำขอเปลี่ยนหัวหน้าหน่วยงาน (Org_OrganizationChangeRequest)", "/org/change-requests/{refid}" },
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
                    { 17, 10, 1, "ฝ่ายบุคคล (HR) อนุมัติสร้างหน่วยงานใหม่",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "REJECTED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 18, 11, 1, "ฝ่ายบุคคล (HR) อนุมัติย้ายสังกัด",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "REJECTED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                    { 19, 12, 1, "ฝ่ายบุคคล (HR) อนุมัติเปลี่ยนหัวหน้า",
                      false, false, false, false, false, true,
                      false, false, false,
                      "COMPLETED", "PENDING", "REJECTED",
                      true, false, true, false, false,
                      false, false, false, false, false },
                });

            migrationBuilder.InsertData(
                table: "wf_custom_user",
                columns: new[] { "id", "subworkflowid", "workflowid", "wlevel", "userid", "empid", "isactive" },
                values: new object[,]
                {
                    { 14, 17, 10, 1, 16L, "002", true },
                    { 15, 18, 11, 1, 16L, "002", true },
                    { 16, 19, 12, 1, 16L, "002", true },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 14 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 15 });
            migrationBuilder.DeleteData(table: "wf_custom_user", keyColumn: "id", keyValues: new object[] { 16 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 17 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 18 });
            migrationBuilder.DeleteData(table: "wf_sub_workflow_master", keyColumn: "subworkflowid", keyValues: new object[] { 19 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 10 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 11 });
            migrationBuilder.DeleteData(table: "wf_workflow", keyColumn: "workflowid", keyValues: new object[] { 12 });

            migrationBuilder.DropTable(
                name: "Org_OrganizationChangeRequest");
        }
    }
}

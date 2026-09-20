using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    // CEO, 18 ก.ย. 2569: approving a payroll run goes through the workflow engine he designed —
    // small company = one preparer (calculates, not part of the workflow) + one approver who sees
    // the whole run from the inbox. Seeds PAYROLL_RUN_APPROVAL with ONE level whose approver is
    // the ผู้อนุมัติเงินเดือน role (PAYROLL_APPROVER, any one holder may approve); bigger customers
    // add levels / LOA bands in the workflow admin without code. Pay_PayrollRun.JobMasterId links
    // the run to its approval job. Idempotent (NOT EXISTS guards), ids looked up by code.
    public partial class PayrollRunApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('Pay_PayrollRun', 'JobMasterId') IS NULL ALTER TABLE Pay_PayrollRun ADD JobMasterId bigint NULL;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM wf_workflow WHERE workflowcode = 'PAYROLL_RUN_APPROVAL')
    INSERT INTO wf_workflow (wname, wstatus, workflowcode, isshow, isactive, useNewEngine, isautoapprove, wgroup, url, description, moddate, modby)
    VALUES (N'อนุมัติรอบเงินเดือน', 'ACTIVE', 'PAYROLL_RUN_APPROVAL', 1, 1, 1, 0, N'การเงิน', '/pay/runs/{refid}',
            N'อนุมัติรอบเงินเดือน (Pay_PayrollRun) — ผู้อนุมัติเปิดดูรอบทั้งหมดจากกล่องงาน; อนุมัติครบ = รอบล็อก, ตีกลับ = กลับเป็นคำนวณแล้ว',
            GETDATE(), 'migration');

DECLARE @wf bigint = (SELECT workflowid FROM wf_workflow WHERE workflowcode = 'PAYROLL_RUN_APPROVAL');
DECLARE @role bigint = (SELECT roleid FROM sc_role WHERE rolecode = 'PAYROLL_APPROVER');

IF NOT EXISTS (SELECT 1 FROM wf_sub_workflow_master WHERE workflowid = @wf AND wlevel = 1)
    INSERT INTO wf_sub_workflow_master
        (workflowid, wlevel, subject,
         isAdhocUser, iscustomApprover, isupperrole, isupperuser, iscustomRole, iscustomUser,
         iscondition, isorcondition, isandcondition,
         forwardstatus, standstatus, backwardstatus,
         istop, isReturnSender, isshow, isLOA, isAutoApproveAllow,
         isNeedBudgetApproval, isPool, isApproverSameOrg, isApproverSameCostCenter, isManualButton)
    VALUES (@wf, 1, N'ผู้อนุมัติเงินเดือน อนุมัติรอบเงินเดือน',
         0, 0, 0, 0, 1, 0,
         0, 1, 0,
         'COMPLETED', 'PENDING', 'RETURNED',
         1, 0, 1, 0, 0,
         0, 0, 0, 0, 0);

DECLARE @sub bigint = (SELECT subworkflowid FROM wf_sub_workflow_master WHERE workflowid = @wf AND wlevel = 1);
IF @role IS NOT NULL AND NOT EXISTS (SELECT 1 FROM wf_custom_role WHERE workflowid = @wf AND wlevel = 1 AND roleid = @role)
    INSERT INTO wf_custom_role (subworkflowid, workflowid, wlevel, roleid, rolecode, isactive, modate, modby)
    VALUES (@sub, @wf, 1, @role, 'PAYROLL_APPROVER', 1, GETDATE(), NULL); -- modby is a bigint user id here
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Switch the workflow off rather than delete it — job history may point at it.
            migrationBuilder.Sql("UPDATE wf_workflow SET isactive = 0, isshow = 0, wstatus = 'INACTIVE' WHERE workflowcode = 'PAYROLL_RUN_APPROVAL';");
            migrationBuilder.Sql("IF COL_LENGTH('Pay_PayrollRun', 'JobMasterId') IS NOT NULL ALTER TABLE Pay_PayrollRun DROP COLUMN JobMasterId;");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <summary>
    /// HR gap wave 3 (customer spec REQ-050 QR attendance, REQ-051/052/143 attendance
    /// correction + approval, REQ-060 IP log, REQ-066 flexible time):
    ///  - Att_GeofenceLocation.QrToken, Att_PunchLog.IpAddress
    ///  - Att_ShiftDefinition.IsFlexible / CoreStartTime / CoreEndTime
    ///  - Att_CorrectionRequest table
    ///  - workflow ATT_CORRECTION (one level, supervisor per org chart) — identity ids,
    ///    seeded by SQL rather than InsertData so no id is guessed
    ///  - ESS menu row /ess/attendance-correction
    /// Hand-authored and already applied + stamped on the dev DB.
    /// </summary>
    public partial class HrGapWave3AttendanceQrCorrectionFlex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "QrToken", table: "Att_GeofenceLocation", type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(name: "IpAddress", table: "Att_PunchLog", type: "nvarchar(45)", maxLength: 45, nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsFlexible", table: "Att_ShiftDefinition", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<TimeOnly>(name: "CoreStartTime", table: "Att_ShiftDefinition", type: "time", nullable: true);
            migrationBuilder.AddColumn<TimeOnly>(name: "CoreEndTime", table: "Att_ShiftDefinition", type: "time", nullable: true);

            migrationBuilder.CreateTable(
                name: "Att_CorrectionRequest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    HremployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EmpNo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JobMasterId = table.Column<long>(type: "bigint", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_Att_CorrectionRequest", x => x.Id); });
            migrationBuilder.CreateIndex(name: "IX_Att_CorrectionRequest_HremployeeId_WorkDate", table: "Att_CorrectionRequest", columns: new[] { "HremployeeId", "WorkDate" });

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM wf_workflow WHERE workflowcode='ATT_CORRECTION')
BEGIN
  INSERT INTO wf_workflow (wname, wstatus, workflowcode, isshow, isactive, description, url)
  VALUES (N'ขอแก้ไขเวลาเข้า-ออกงาน', 'ACTIVE', 'ATT_CORRECTION', 1, 1, N'อนุมัติคำขอแก้ไขเวลาลงเวลา (Att_CorrectionRequest) — หัวหน้างานตามผังองค์กร 1 ระดับ', '/ess/attendance-correction/{refid}');
  DECLARE @wf bigint = SCOPE_IDENTITY();
  INSERT INTO wf_sub_workflow_master (workflowid, wlevel, subject, isAdhocUser, iscustomApprover, isupperrole, isupperuser, iscustomRole, iscustomUser, iscondition, isorcondition, isandcondition, forwardstatus, standstatus, backwardstatus, istop, isReturnSender, isshow, isLOA, isAutoApproveAllow, isNeedBudgetApproval, isPool, isApproverSameOrg, isApproverSameCostCenter, isManualButton)
  VALUES (@wf, 1, N'หัวหน้างานอนุมัติ (ตามผังองค์กร)', 0,0,1,0,0,0, 0,0,0, 'COMPLETED','PENDING','RETURNED', 1,0,1,0,0, 0,0,0,0,0);
END;
IF NOT EXISTS (SELECT 1 FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/attendance-correction')
INSERT INTO sc_menu (menuname, menuname_en, menulevel, isfinal, menuorder, menucode, isshow, url, isactive, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, moddate, modby, method_action)
SELECT N'ขอแก้ไขเวลาเข้า-ออกงาน', 'Attendance Correction', menulevel, isfinal, 61, menucode, isshow, '/ess/attendance-correction', 1, uppermenucode, langcode, menugroupid, icon, small_icon, programid, tooltip, startdate, enddate, GETDATE(), modby, method_action
FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/attendance-checkin';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sc_menu WHERE CAST(url AS nvarchar(500)) = '/ess/attendance-correction';");
            migrationBuilder.Sql("DELETE FROM wf_sub_workflow_master WHERE workflowid IN (SELECT workflowid FROM wf_workflow WHERE workflowcode='ATT_CORRECTION'); DELETE FROM wf_workflow WHERE workflowcode='ATT_CORRECTION';");
            migrationBuilder.DropTable(name: "Att_CorrectionRequest");
            migrationBuilder.DropColumn(name: "CoreEndTime", table: "Att_ShiftDefinition");
            migrationBuilder.DropColumn(name: "CoreStartTime", table: "Att_ShiftDefinition");
            migrationBuilder.DropColumn(name: "IsFlexible", table: "Att_ShiftDefinition");
            migrationBuilder.DropColumn(name: "IpAddress", table: "Att_PunchLog");
            migrationBuilder.DropColumn(name: "QrToken", table: "Att_GeofenceLocation");
        }
    }
}

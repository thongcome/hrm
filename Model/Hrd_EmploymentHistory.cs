using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประวัติการทำงานของพนักงาน (คำสั่งแต่งตั้ง/โยกย้าย) — ported from legacy PIS
// "personposition" table (personal/personposition/Template3.jsp?PID=PG04CM03),
// the formal HR order-document record: order type/no, order date vs the date
// it actually takes effect, position status, and — the field the legacy
// screenshot form itself calls out but Pay_PositionSalaryHistory doesn't
// track at all — the ORGANIZATION (สังกัด) being transferred from/to, not
// just the position title.
//
// This is deliberately a SEPARATE table from Pay_PositionSalaryHistory, not
// a replacement or a duplicate — same split the legacy system itself had
// (personposition vs. positionhistory):
//   - Pay_PositionSalaryHistory: system-generated audit trail, written
//     automatically whenever PayrollEmployeeAdmin.razor detects a salary
//     and/or position-code change on save. Payroll-facing, has salary.
//   - Hrd_EmploymentHistory (this table): the formal record of the actual
//     HR order document (คำสั่ง) — manually entered by HR here in the
//     Personnel module, no salary figures at all (see feedback memory
//     "no salary in Employee module"), and covers org transfers/status
//     changes that Pay_PositionSalaryHistory was never designed to capture.
[Table("Hrd_EmploymentHistory")]
public class Hrd_EmploymentHistory
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    // Legacy "ประเภทคำสั่ง" — free text (แต่งตั้ง/โยกย้าย/เลื่อนตำแหน่ง/พ้นสภาพ ฯลฯ),
    // no fixed master list found in the legacy schema to port.
    [StringLength(100)]
    public string? OrderType { get; set; }

    [StringLength(50)]
    public string? OrderNo { get; set; }

    public int? SortNo { get; set; }

    public DateOnly? OrderDate { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(200)]
    public string? PositionStatus { get; set; }

    public bool IsLatestPosition { get; set; }
    public bool IsPositionChanged { get; set; }

    // New assignment — soft-linked, denormalized name kept alongside for
    // fast list rendering / stability if the position or org is renamed
    // later, same convention as Pos_PositionSlot's own Name/AbbName fields.
    public long? NewPosExecTypeId { get; set; }
    [StringLength(500)]
    public string? NewPositionName { get; set; }
    public long? NewOrganizationId { get; set; }
    [StringLength(500)]
    public string? NewOrganizationName { get; set; }

    // Prior assignment, for context on what changed.
    public long? OldPosExecTypeId { get; set; }
    [StringLength(500)]
    public string? OldPositionName { get; set; }
    public long? OldOrganizationId { get; set; }
    [StringLength(500)]
    public string? OldOrganizationName { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public long? CreatedByUserId { get; set; }

    // Used only on rows where OrderType = "กลับเข้าทำงาน" (rehire) — written
    // by Services/Pay/EmployeeRehireService.cs. TenureTreatment is the choice
    // HR made for Hremployee.WorkDate (Continuous = left untouched, Reset =
    // overwritten to the rehire date); PriorWorkDate/PriorResignDate snapshot
    // what those fields held right before the rehire cleared them, so the
    // fact isn't lost even when WorkDate itself gets reset.
    public TenureTreatment? TenureTreatment { get; set; }
    public DateTime? PriorWorkDate { get; set; }
    public DateTime? PriorResignDate { get; set; }
}

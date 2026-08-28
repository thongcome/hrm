using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ข้อมูลบัญชีธนาคารของพนักงาน — ported from legacy PIS "bankaccount" table
// (personal/bankaccount/*.jsp). This is a real, editable master record —
// distinct from Pay_PayrollEmployee.BankCode/BankBranchCode/BankAccountNo,
// which are a payroll-run-time SNAPSHOT with no editable source of truth
// anywhere in the app today. This table is that source of truth: HR edits
// it here in the Personnel module (accessible to general HR staff, no
// salary/compensation figures involved — see feedback memory
// "no salary in Employee module"), and payroll calculation should read the
// currently-active row (IsActive=true) from here when snapshotting a run,
// the same way it reads other employee master fields.
//
// Legacy had "islastbank" (a string flag) to mark the currently-active
// account among historical rows — modeled here as a real bool IsActive
// instead, with old rows kept (not deleted) as history, same soft-delete
// convention as the rest of this codebase.
[Table("Hrd_BankAccount")]
public class Hrd_BankAccount
{
    [Key]
    public long Id { get; set; }

    public long HremployeeId { get; set; }

    // Soft-linked to Com_Bank.Id — nullable because HR may not have picked
    // a bank yet while still filling in the rest of the form.
    public long? BankId { get; set; }

    [StringLength(50)]
    public string? BankBranch { get; set; }

    // Legacy "accounttypecode" — free text (ออมทรัพย์/กระแสรายวัน ฯลฯ), no
    // fixed master list found in the legacy schema to port.
    [StringLength(50)]
    public string? AccountTypeCode { get; set; }

    [Required, StringLength(50)]
    public string BankAccountNo { get; set; } = null!;

    // Name on the account — may differ from the employee's own name for a
    // joint/family account, which is why the legacy schema kept it separate
    // from Hremployee.EmpName rather than assuming they always match.
    [StringLength(250)]
    public string? BankAccountName { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(1000)]
    public string? Remark { get; set; }

    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}

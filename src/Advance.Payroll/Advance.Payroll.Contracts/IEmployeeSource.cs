namespace Advance.Payroll.Contracts;

// Replaces PayrollCalculationService.cs's `context.Hremployee.Where(...)` query
// (HRM Services/Pay/PayrollCalculationService.cs:280-284) and every other direct
// Hremployee read across Services/Pay (PayrollPreflightService.cs:71,
// EFilingExportService.cs:130-137, Por1DataService.cs:79-80/126-127,
// SalaryCertificateDataService.cs:33-35, RecurringPayItemService.cs, InsuranceAutoEnrollService.cs,
// ProvidentFundExitCaseService.cs, ProvidentFundRateChangeRequestService.cs, WithholdingCertificateDataService.cs,
// BankFileExportService.cs:113-114 via the Pay_PayrollEmployee.Hremployee navigation,
// PayslipPdfService.cs:37, PayslipPasswordService.cs:29-31).
//
// This is also, verbatim, the pay_employee sync shape (EXTRACTION-PLAN.md "pay_employee
// sync design") — HRM's implementation projects Hremployee -> PayEmployeeSnapshot;
// Advance.Payroll Lite's implementation reads its own employee master table with the
// same shape directly. Only ~25 of Hremployee's ~108 columns, cross-checked against every
// `emp.<Column>` read found in Services/Pay.
public interface IEmployeeSource
{
    // Everyone on payroll for this company whose employment overlaps [periodStart, periodEnd]
    // (WorkDate <= periodEnd AND (ResignDate IS NULL OR ResignDate >= periodStart)) —
    // mirrors PayrollCalculationService.cs:280-284 exactly.
    Task<IReadOnlyList<PayEmployeeSnapshot>> GetEligibleEmployeesAsync(
        string companyId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default);

    // Single-employee lookup — used by report/certificate generators (Por1DataService,
    // EFilingExportService, SalaryCertificateDataService, WithholdingCertificateDataService)
    // that resolve name/IdCard/etc. for a set of HremployeeIds already on a payroll run.
    Task<IReadOnlyDictionary<long, PayEmployeeSnapshot>> GetByIdsAsync(
        IReadOnlyCollection<long> hremployeeIds, CancellationToken ct = default);
}

// Field-for-field: HremployeeId <-> Hremployee.id, EmpNo <-> Hremployee.EmpNo, etc.
// See EXTRACTION-PLAN.md for the full source-column cross-reference table.
public sealed record PayEmployeeSnapshot(
    long HremployeeId,
    string EmpNo,
    string CompanyId,
    string? EmpName,
    string? EmpSurname,
    string? Sex,
    string? IdCard,
    DateTime? BirthDate,
    DateTime? WorkDate,
    DateTime? ResignDate,
    DateTime? ProbationConfirmedDate,
    decimal? SalaryAmt,
    decimal? DailyWage,
    string? SalexpBank,
    string? SalexpBranch,
    string? SalexpAccid,
    string? CostCenterCode,
    string? PosCode,
    string? EmptypeCode,
    string? OrgCode,
    decimal? ProvfEmprate,
    decimal? ProvfCorprate,
    string? RefMembno,
    string? AdnEmail);
